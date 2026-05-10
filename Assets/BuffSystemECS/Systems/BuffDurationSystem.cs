using System.Collections.Generic;
using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// Buff 持续时间系统
    /// 每帧更新所有实体上 Refreshable/Independent 类型 Buff 的剩余时间，移除过期 Buff
    /// 对应 RefreshableEffectBase 和 IndependentEffectBase 的 Update 逻辑
    /// </summary>
    public class BuffDurationSystem : BuffSystemBase
    {
        /// <summary>
        /// 待移除的 Buff 列表（避免在遍历中修改集合）
        /// </summary>
        private readonly List<(BuffEntity, int)> pending_removes_ = new List<(BuffEntity, int)>();

        public override void Update(float delta_time)
        {
            pending_removes_.Clear();

            var entities = EntityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                ProcessEntityBuffs(entity, delta_time);
            }

            // 执行待移除操作
            for (int i = pending_removes_.Count - 1; i >= 0; i--)
            {
                var (entity, buff_id) = pending_removes_[i];
                RemoveExpiredBuff(entity, buff_id);
            }
        }

        private void ProcessEntityBuffs(BuffEntity entity, float delta_time)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null || buffs.Count == 0) return;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];

                // Stackable 类型由 BuffStackSystem 处理层计时
                if (buff.effect_type == BuffEffectType.Stackable)
                    continue;

                // Refreshable 和 Independent：总时间递减
                buff.duration -= delta_time;
                buffs[i] = buff;

                if (buff.duration <= 0)
                {
                    pending_removes_.Add((entity, buff.buff_id));
                }
            }
        }

        private void RemoveExpiredBuff(BuffEntity entity, int buff_id)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null) return;

            // 找到并移除过期 Buff
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].buff_id == buff_id)
                {
                    var buff = buffs[i];

                    // 释放特效
                    if (EntityManager.TryGetEffectInstance(entity, buff.buff_id, out GameObject effect))
                    {
                        if (effect != null) Object.Destroy(effect);
                        EntityManager.RemoveEffectInstance(entity, buff.buff_id);
                    }

                    // 从实体移除
                    EntityManager.RemoveBuffFromEntity(entity, buff);

                    // 触发事件
                    BuffSystemECSManager.Instance?.NotifyBuffRemoved(buff);
                    BuffSystemECSManager.Instance?.NotifyBuffCountChanged(entity);
                    break;
                }
            }
        }
    }
}