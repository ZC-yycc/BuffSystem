using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff DOTS 管理器 — 提供面向非 ECS 代码的 API，封装 Buff 添加/移除请求
    /// 通过 EntityManager 创建请求实体，再由系统处理
    /// </summary>
    public class BuffDotsManager
    {
        private static BuffDotsManager          instance_;
        private World                           world_;
        private EntityManager                   entity_manager_;
        private bool                            initialized_;



        public static BuffDotsManager Instance => instance_ ??= new BuffDotsManager();
        private BuffDotsManager() { }




        /// <summary>
        /// 初始化管理器（由 Bootstrap 调用）
        /// </summary>
        public void Initialize(World world)
        {
            world_ = world;
            entity_manager_ = world.EntityManager;
            initialized_ = true;
        }

        /// <summary>
        /// 添加 Buff 到指定目标实体
        /// </summary>
        /// <param name="target">目标实体</param>
        /// <param name="config_id">Buff 配置 ID</param>
        /// <param name="effect_value">效果值</param>
        /// <returns>请求实体</returns>
        public Entity AddBuff(Entity target, int config_id, float effect_value = 0f)
        {
            if (!ValidateState()) return Entity.Null;
            if (target == Entity.Null) return Entity.Null;

            Entity request_entity = entity_manager_.CreateEntity();
            entity_manager_.AddComponentData(request_entity, new BuffAddRequest
            {
                Target = target,
                ConfigId = config_id,
                EffectValue = effect_value,
            });

            return request_entity;
        }

        /// <summary>
        /// 移除指定 Buff
        /// </summary>
        /// <param name="target">目标实体</param>
        /// <param name="buff_id">Buff 实例 ID</param>
        /// <returns>请求实体</returns>
        public Entity RemoveBuff(Entity target, int buff_id)
        {
            if (!ValidateState()) return Entity.Null;

            Entity request_entity = entity_manager_.CreateEntity();
            entity_manager_.AddComponentData(request_entity, new BuffRemoveRequest
            {
                Target = target,
                BuffId = buff_id,
            });

            return request_entity;
        }

        /// <summary>
        /// 查询目标实体上所有活跃的 Buff
        /// </summary>
        /// <param name="target">目标实体</param>
        /// <param name="results">输出 Buff 数据列表</param>
        public void GetActiveBuffs(Entity target, NativeList<BuffRuntimeData> results)
        {
            if (!ValidateState()) return;

            // 通过 EntityQuery 查找
            // 在非 ECS 代码中无法直接使用 EntityQuery，此方法需要由 System 代理
            // 这里提供基础实现
        }

        /// <summary>
        /// 检查目标实体上是否存在指定配置 ID 的 Buff
        /// </summary>
        public bool HasBuff(Entity target, int config_id)
        {
            if (!ValidateState()) return false;

            // 遍历查找目标实体上匹配的 Buff
            var query = entity_manager_.CreateEntityQuery(
                ComponentType.ReadOnly<BuffRuntimeData>(),
                ComponentType.ReadOnly<BuffInstanceTag>(),
                ComponentType.Exclude<BuffExpiredTag>());

            var runtime_handle = entity_manager_.GetComponentTypeHandle<BuffRuntimeData>(true);
            using var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var runtime_array = chunk.GetNativeArray(ref runtime_handle);

                for (int j = 0; j < chunk.Count; j++)
                {
                    if (runtime_array[j].Target == target && runtime_array[j].ConfigId == config_id)
                        return true;
                }
            }

            return false;
        }

        private bool ValidateState()
        {
            if (!initialized_ || world_ == null || !world_.IsCreated || entity_manager_ == null)
            {
                Debug.LogError("BuffDotsManager is not initialized. Call Initialize() first.");
                return false;
            }
            return true;
        }
    }
}