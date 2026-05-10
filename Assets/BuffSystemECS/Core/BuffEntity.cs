using System.Collections.Generic;
using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// ECS Buff 实体，作为 Buff 的容器标识
    /// 每个拥有 Buff 的 GameObject 对应一个 BuffEntity
    /// </summary>
    public struct BuffEntity
    {
        /// <summary>
        /// 实体唯一 ID
        /// </summary>
        public int id;

        /// <summary>
        /// 实体是否有效
        /// </summary>
        public bool is_active;

        public BuffEntity(int id)
        {
            this.id = id;
            this.is_active = true;
        }

        public override readonly bool Equals(object obj) => obj is BuffEntity other && id == other.id;
        public override readonly int GetHashCode() => id.GetHashCode();

        /// <summary>
        /// 重载 == 和 != 运算符，比较实体 ID 是否相同（不比较 is_active，因为无效实体也可能存在于集合中）
        /// </summary>
        public static bool operator ==(BuffEntity a, BuffEntity b) => a.id == b.id;
        public static bool operator !=(BuffEntity a, BuffEntity b) => a.id != b.id;
    }

    /// <summary>
    /// Buff 实体管理器，负责实体的创建、销毁和组件存储
    /// </summary>
    public class BuffEntityManager
    {
        private int next_entity_id_ = 0;

        /// <summary>所有活跃实体</summary>
        private readonly HashSet<BuffEntity> entities_ = new HashSet<BuffEntity>();

        /// <summary>实体 → Buff运行时数据映射（一对多：一个实体可以有多个Buff）</summary>
        private readonly Dictionary<BuffEntity, List<BuffRuntimeData>> entity_buff_map_ = new Dictionary<BuffEntity, List<BuffRuntimeData>>();

        /// <summary>实体 → 阻挡BuffID集合映射</summary>
        private readonly Dictionary<BuffEntity, HashSet<int>> entity_block_map_ = new Dictionary<BuffEntity, HashSet<int>>();

        /// <summary>实体 → 施加者实体映射</summary>
        private readonly Dictionary<BuffEntity, BuffEntity> entity_imposer_map_ = new Dictionary<BuffEntity, BuffEntity>();

        /// <summary>实体 → (BuffID → 特效实例) 映射</summary>
        private readonly Dictionary<BuffEntity, Dictionary<int, GameObject>> entity_effect_map_ = new Dictionary<BuffEntity, Dictionary<int, GameObject>>();

        /// <summary>实体 → 特效挂载点 Transform 映射</summary>
        private readonly Dictionary<BuffEntity, Transform> entity_effect_target_map_ = new Dictionary<BuffEntity, Transform>();

        /// <summary>创建新实体</summary>
        public BuffEntity CreateEntity()
        {
            BuffEntity entity = new BuffEntity(++next_entity_id_);
            entities_.Add(entity);
            entity_buff_map_[entity] = new List<BuffRuntimeData>();
            entity_block_map_[entity] = new HashSet<int>();
            entity_effect_map_[entity] = new Dictionary<int, GameObject>();
            return entity;
        }

        /// <summary>销毁实体，清理所有关联数据</summary>
        public void DestroyEntity(BuffEntity entity)
        {
            // 清理特效实例
            if (entity_effect_map_.TryGetValue(entity, out var effects))
            {
                foreach (var kvp in effects)
                {
                    if (kvp.Value != null)
                        Object.Destroy(kvp.Value);
                }
            }

            entities_.Remove(entity);
            entity_buff_map_.Remove(entity);
            entity_block_map_.Remove(entity);
            entity_imposer_map_.Remove(entity);
            entity_effect_map_.Remove(entity);
            entity_effect_target_map_.Remove(entity);
        }

        /// <summary>检查实体是否有效</summary>
        public bool IsEntityValid(BuffEntity entity)
        {
            return entities_.Contains(entity) && entity.is_active;
        }

        /// <summary>获取实体上所有的 Buff 数据</summary>
        public List<BuffRuntimeData> GetEntityBuffs(BuffEntity entity)
        {
            if (entity_buff_map_.TryGetValue(entity, out var buffs))
                return buffs;
            return null;
        }

        /// <summary>根据 BuffID 获取实体上的 Buff 数据</summary>
        public bool TryGetBuffData(BuffEntity entity, int buff_id, out BuffRuntimeData data)
        {
            data = default;
            if (!entity_buff_map_.TryGetValue(entity, out var buffs))
                return false;

            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs[i].buff_id == buff_id)
                {
                    data = buffs[i];
                    return true;
                }
            }
            return false;
        }

        /// <summary>向实体添加 Buff 数据</summary>
        public void AddBuffToEntity(BuffEntity entity, in BuffRuntimeData data)
        {
            if (entity_buff_map_.TryGetValue(entity, out var buffs))
            {
                buffs.Add(data);
            }
        }

        /// <summary>从实体移除 Buff 数据</summary>
        public bool RemoveBuffFromEntity(BuffEntity entity, in BuffRuntimeData data)
        {
            if (entity_buff_map_.TryGetValue(entity, out var buffs))
            {
                return buffs.Remove(data);
            }
            return false;
        }

        /// <summary>获取实体的阻挡 BuffID 集合</summary>
        public HashSet<int> GetEntityBlocks(BuffEntity entity)
        {
            if (entity_block_map_.TryGetValue(entity, out var blocks))
                return blocks;
            return null;
        }

        /// <summary>设置实体的施加者</summary>
        public void SetImposer(BuffEntity entity, BuffEntity imposer)
        {
            entity_imposer_map_[entity] = imposer;
        }

        /// <summary>获取实体的施加者</summary>
        public BuffEntity GetImposer(BuffEntity entity)
        {
            if (entity_imposer_map_.TryGetValue(entity, out var imposer))
                return imposer;
            return default;
        }

        /// <summary>获取所有活跃实体（用于系统遍历）</summary>
        public HashSet<BuffEntity> GetAllEntities()
        {
            return entities_;
        }

        /// <summary>获取活跃实体数量</summary>
        public int GetEntityCount()
        {
            return entities_.Count;
        }

        /// <summary>设置实体的特效挂载点</summary>
        public void SetEffectTarget(BuffEntity entity, Transform target)
        {
            entity_effect_target_map_[entity] = target;
        }

        /// <summary>获取实体的特效挂载点</summary>
        public Transform GetEffectTarget(BuffEntity entity)
        {
            if (entity_effect_target_map_.TryGetValue(entity, out var target))
                return target;
            return null;
        }

        /// <summary>为实体添加特效实例</summary>
        public void AddEffectInstance(BuffEntity entity, int buff_id, GameObject effect)
        {
            if (entity_effect_map_.TryGetValue(entity, out var effects))
            {
                effects[buff_id] = effect;
            }
        }

        /// <summary>获取实体的特效实例</summary>
        public bool TryGetEffectInstance(BuffEntity entity, int buff_id, out GameObject effect)
        {
            effect = null;
            if (entity_effect_map_.TryGetValue(entity, out var effects))
            {
                return effects.TryGetValue(buff_id, out effect);
            }
            return false;
        }

        /// <summary>移除实体的特效实例</summary>
        public bool RemoveEffectInstance(BuffEntity entity, int buff_id)
        {
            if (entity_effect_map_.TryGetValue(entity, out var effects))
            {
                return effects.Remove(buff_id);
            }
            return false;
        }

        /// <summary>清空所有数据</summary>
        public void Clear()
        {
            entities_.Clear();
            entity_buff_map_.Clear();
            entity_block_map_.Clear();
            entity_imposer_map_.Clear();
            entity_effect_map_.Clear();
            entity_effect_target_map_.Clear();
            next_entity_id_ = 0;
        }
    }
}