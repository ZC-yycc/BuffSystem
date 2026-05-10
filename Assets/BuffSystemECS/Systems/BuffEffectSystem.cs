using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// Buff 特效管理系统
    /// 负责创建、更新位置、销毁 Buff 关联的特效
    /// 对应 BuffEffectBase 的 CreateEffect / UpdateEffect / ReleaseEffect 逻辑
    /// </summary>
    public class BuffEffectSystem : BuffSystemBase
    {
        public override void Update(float delta_time)
        {
            var entities = EntityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                UpdateEntityEffects(entity);
            }
        }

        /// <summary>
        /// 为新增的 Buff 创建特效实例
        /// </summary>
        public void CreateEffect(BuffEntity entity, BuffRuntimeData buff)
        {
            if (buff.effect_prefab == null) return;

            Transform effect_target = EntityManager.GetEffectTarget(entity);
            if (effect_target == null)
            {
                Debug.LogWarning($"Buff特效挂载的对象未设置，BuffID: {buff.buff_id}");
                return;
            }

            // 这里可以使用对象池来优化性能，当前为简单实例化
            GameObject effect = Object.Instantiate(buff.effect_prefab);
            effect.transform.localScale = effect_target.localScale;
            effect.transform.SetPositionAndRotation(effect_target.position, effect_target.rotation);
            EntityManager.AddEffectInstance(entity, buff.buff_id, effect);
        }

        /// <summary>
        /// 更新特效位置
        /// </summary>
        private void UpdateEntityEffects(BuffEntity entity)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null) return;

            Transform effect_target = EntityManager.GetEffectTarget(entity);
            if (effect_target == null) return;

            // 更新所有类型 Buff 的特效（Stackable 共用特效也在此管理）
            foreach (var buff in buffs)
            {
                if (EntityManager.TryGetEffectInstance(entity, buff.buff_id, out GameObject effect))
                {
                    if (effect != null)
                    {
                        effect.transform.position = effect_target.position;
                    }
                }
            }
        }

        /// <summary>
        /// 释放指定 Buff 的特效实例
        /// </summary>
        public void ReleaseEffect(BuffEntity entity, BuffRuntimeData buff)
        {
            if (EntityManager.TryGetEffectInstance(entity, buff.buff_id, out GameObject effect))
            {
                // 使用对象池时这里应该是回收而不是销毁
                if (effect != null)
                {
                    Object.Destroy(effect);
                }
                EntityManager.RemoveEffectInstance(entity, buff.buff_id);
            }
        }

        /// <summary>
        /// 释放实体上所有 Buff 的特效实例
        /// </summary>
        public void ReleaseAllEffects(BuffEntity entity)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null) return;

            foreach (var buff in buffs)
            {
                ReleaseEffect(entity, buff);
            }
        }
    }
}