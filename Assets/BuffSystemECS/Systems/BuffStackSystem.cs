using System.Collections.Generic;
using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// 独立叠加层数据（仅 Stackable 类型使用）
    /// 各层独立时长，但整个 Buff 共用一个特效（特效由 BuffEffectSystem 管理）
    /// </summary>
    public struct BuffLayerData
    {
        /// <summary>
        /// 上一次触发持续效果的时间
        /// </summary>
        public float last_trigger_time;

        /// <summary>
        /// 该层的剩余时间
        /// </summary>
        public float layer_duration;
    }

    /// <summary>
    /// Buff 独立叠加系统
    /// 处理 StackableEffectBase 类型 Buff 的层计时和层生命周期
    /// </summary>
    public class BuffStackSystem : BuffSystemBase
    {
        /// <summary>
        /// 实体ID → { BuffID → 层数据列表 } 映射
        /// 每层独立计时，整个 Buff 共用一个特效实例
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, List<BuffLayerData>>> stack_data_map_ = new();

        /// <summary>
        /// 层持续触发间隔
        /// </summary>
        private const float DEFAULT_INTERVAL = 1.0f;

        public override void Update(float delta_time)
        {
            var entities = EntityManager.GetAllEntities();
            foreach (var entity in entities)
            {
                ProcessStackableBuffs(entity, delta_time);
            }
        }

        private void ProcessStackableBuffs(BuffEntity entity, float delta_time)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null || buffs.Count == 0) return;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];

                // 只处理 Stackable 类型
                if (buff.effect_type != BuffEffectType.Stackable)
                    continue;

                if (!stack_data_map_.TryGetValue(entity.id, out var buff_layers))
                {
                    buff_layers = new Dictionary<int, List<BuffLayerData>>();
                    stack_data_map_[entity.id] = buff_layers;
                }

                if (!buff_layers.TryGetValue(buff.buff_id, out var layers))
                {
                    layers = new List<BuffLayerData>();
                    buff_layers[buff.buff_id] = layers;
                }

                // 更新层时间
                UpdateLayers(entity, buff, layers, delta_time);

                // 如果所有层都已过期，移除该 Buff
                if (layers.Count == 0)
                {
                    RemoveStackBuff(entity, buff, i);
                }
            }
        }

        private void UpdateLayers(BuffEntity entity, BuffRuntimeData buff, List<BuffLayerData> layers, float delta_time)
        {
            // 倒序更新层持续时间
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                var layer = layers[i];
                layer.layer_duration -= delta_time;
                layers[i] = layer;

                if (layer.layer_duration <= 0)
                {
                    RemoveLayer(entity, buff, layers, i);
                }
            }

            // 触发间隔效果
            float current_time = Time.time;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (i >= layers.Count) continue;

                var layer = layers[i];
                if (current_time - layer.last_trigger_time < DEFAULT_INTERVAL)
                    continue;

                layer.last_trigger_time = current_time;
                layers[i] = layer;

                OnLayerEffectTrigger(buff, i);
            }
        }

        /// <summary>
        /// 添加一层（当 Stackable Buff 被迭代时调用）
        /// 特效不由层管理，由 BuffEffectSystem 统一管理共用的特效实例
        /// </summary>
        public void AddLayer(BuffEntity entity, BuffRuntimeData buff)
        {
            if (!stack_data_map_.TryGetValue(entity.id, out var buff_layers))
            {
                buff_layers = new Dictionary<int, List<BuffLayerData>>();
                stack_data_map_[entity.id] = buff_layers;
            }

            if (!buff_layers.TryGetValue(buff.buff_id, out var layers))
            {
                layers = new List<BuffLayerData>();
                buff_layers[buff.buff_id] = layers;
            }

            // 层数已满时移除最早的一层
            if (buff.IsFull && layers.Count > 0)
            {
                RemoveLayerAt(layers, 0);
            }

            var new_layer = new BuffLayerData
            {
                last_trigger_time = Time.time,
                layer_duration = buff.duration
            };

            layers.Add(new_layer);

            // 更新 Buff 数据层数
            buff.count = layers.Count;
        }

        private void RemoveLayer(BuffEntity entity, BuffRuntimeData buff, List<BuffLayerData> layers, int index)
        {
            RemoveLayerAt(layers, index);
            buff.count = layers.Count;

            // 通知层移除
            BuffSystemECSManager.Instance?.NotifyBuffLayerRemoved(buff);
        }

        private void RemoveLayerAt(List<BuffLayerData> layers, int index)
        {
            if (index < 0 || index >= layers.Count) return;
            layers.RemoveAt(index);
        }

        private void RemoveStackBuff(BuffEntity entity, BuffRuntimeData buff, int index)
        {
            var buffs = EntityManager.GetEntityBuffs(entity);
            if (buffs == null || index >= buffs.Count) return;

            // 清理层数据
            if (stack_data_map_.TryGetValue(entity.id, out var buff_layers))
            {
                buff_layers.Remove(buff.buff_id);
            }

            // 释放共用的特效实例
            if (EntityManager.TryGetEffectInstance(entity, buff.buff_id, out GameObject effect))
            {
                if (effect != null) Object.Destroy(effect);
                EntityManager.RemoveEffectInstance(entity, buff.buff_id);
            }

            // 从实体移除 Buff
            EntityManager.RemoveBuffFromEntity(entity, buff);

            BuffSystemECSManager.Instance?.NotifyBuffRemoved(buff);
            BuffSystemECSManager.Instance?.NotifyBuffCountChanged(entity);
        }

        private void OnLayerEffectTrigger(BuffRuntimeData buff, int layer_index)
        {
            // 触发层效果回调
            BuffSystemECSManager.Instance?.NotifyLayerEffectTrigger(buff, layer_index);
        }

        /// <summary>
        /// 获取指定 Buff 的层数
        /// </summary>
        public int GetLayerCount(BuffEntity entity, int buff_id)
        {
            if (stack_data_map_.TryGetValue(entity.id, out var buff_layers) &&
                buff_layers.TryGetValue(buff_id, out var layers))
            {
                return layers.Count;
            }
            return 0;
        }

        /// <summary>
        /// 清理实体所有 Stackable 层数据（不清理特效，特效由 BuffEffectSystem 统一释放）
        /// </summary>
        public void ClearEntityStackData(BuffEntity entity)
        {
            if (stack_data_map_.TryGetValue(entity.id, out var buff_layers))
            {
                stack_data_map_.Remove(entity.id);
            }
        }
    }
}