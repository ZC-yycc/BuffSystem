using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 特效系统 — 处理特效的创建、位置更新和销毁
    /// 使用 SystemBase（类系统）处理涉及 GameObject 的操作
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffDurationSystem))]
    public partial class BuffEffectSystem : SystemBase
    {
        /// <summary>
        /// 特效预制体缓存（managed 对象必须放在类系统中）
        /// </summary>
        private System.Collections.Generic.Dictionary<int, GameObject> effect_prefab_cache_;

        /// <summary>
        /// 实体 → 特效实例的映射（用于追踪和销毁）
        /// </summary>
        private System.Collections.Generic.Dictionary<Entity, GameObject> entity_effect_map_;

        protected override void OnCreate()
        {
            effect_prefab_cache_ = new System.Collections.Generic.Dictionary<int, GameObject>();
            entity_effect_map_ = new System.Collections.Generic.Dictionary<Entity, GameObject>();
            RequireForUpdate<BuffConfigRegistry>();
        }

        /// <summary>
        /// 特效创建和过期清理的主循环
        /// </summary>
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 处理特效创建请求
            foreach (var (effect_ref, runtime_data, entity) in
                     SystemAPI.Query<RefRW<BuffEffectReference>, RefRO<BuffRuntimeData>>()
                         .WithAll<BuffInstanceTag>()
                         .WithNone<BuffExpiredTag>()
                         .WithEntityAccess())
            {
                if (effect_ref.ValueRO.EffectInstanceId == 0 && effect_ref.ValueRO.EffectPrefabHash != 0)
                {
                    if (effect_prefab_cache_.TryGetValue(effect_ref.ValueRO.EffectPrefabHash, out var prefab))
                    {
                        CreateEffectForBuff(entity, runtime_data.ValueRO, prefab, ref effect_ref.ValueRW);
                    }
                }
            }

            // 处理 Buff 过期时的特效清理
            foreach (var (effect_ref, entity) in
                     SystemAPI.Query<RefRW<BuffEffectReference>>()
                         .WithAll<BuffInstanceTag, BuffExpiredTag>()
                         .WithEntityAccess())
            {
                if (effect_ref.ValueRO.EffectInstanceId != 0)
                {
                    DestroyEffectForBuff(entity, effect_ref.ValueRO);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 注册特效预制体映射
        /// </summary>
        public void RegisterEffectPrefab(int hash, GameObject prefab)
        {
            if (prefab != null)
            {
                effect_prefab_cache_[hash] = prefab;
            }
        }

        /// <summary>
        /// 注销特效预制体映射
        /// </summary>
        public void UnregisterEffectPrefab(int hash)
        {
            effect_prefab_cache_.Remove(hash);
        }

        private void CreateEffectForBuff(Entity entity,
            BuffRuntimeData runtime_data, GameObject prefab, ref BuffEffectReference effect_ref)
        {
            if (prefab == null) return;

            var effect_instance = Object.Instantiate(prefab);
            if (effect_instance == null) return;

            int instance_id = effect_instance.GetInstanceID();
            effect_ref.EffectInstanceId = instance_id;

            // 记录映射关系
            entity_effect_map_[entity] = effect_instance;

            Debug.Log($"[BuffEffectSystem] Created effect (instanceId={instance_id}) for Buff (id={runtime_data.BuffId})");
        }

        private void DestroyEffectForBuff(Entity entity, BuffEffectReference effect_ref)
        {
            int instance_id = effect_ref.EffectInstanceId;
            if (instance_id != 0 && entity_effect_map_.TryGetValue(entity, out var go))
            {
                if (go != null)
                {
                    Object.Destroy(go);
                    Debug.Log($"[BuffEffectSystem] Destroyed effect (instanceId={instance_id}) for Buff");
                }

                entity_effect_map_.Remove(entity);
            }
        }

        protected override void OnDestroy()
        {
            effect_prefab_cache_?.Clear();
            entity_effect_map_?.Clear();
        }
    }
}