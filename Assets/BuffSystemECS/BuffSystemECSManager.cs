using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// BuffSystemECS 顶层管理器（MonoBehaviour 桥接层）
    /// 负责创建 World、注册 System、实体与 GameObject 的绑定、提供对外 API
    /// 整个 ECS Buff 系统的入口点
    /// </summary>
    public class BuffSystemECSManager : MonoBehaviour
    {
        /// <summary>单例实例</summary>
        public static BuffSystemECSManager                      Instance { get; private set; }

        /// <summary>ECS World</summary>
        public BuffWorld                                        World { get; private set; }

        /// <summary>世界内各系统的引用</summary>
        public BuffDurationSystem                               DurationSystem { get; private set; }
        public BuffStackSystem                                  StackSystem { get; private set; }
        public BuffEffectSystem                                 EffectSystem { get; private set; }

        /// <summary>GameObject → 实体映射（反向查找）</summary>
        private readonly Dictionary<GameObject, BuffEntity>     game_object_to_entity_ = new Dictionary<GameObject, BuffEntity>();

        /// <summary>实体 → GameObject 映射</summary>
        private readonly Dictionary<BuffEntity, GameObject>     entity_to_game_object_ = new Dictionary<BuffEntity, GameObject>();

        /// <summary>实体 → 特效挂载点 映射</summary>
        private readonly Dictionary<BuffEntity, Transform>      entity_effect_target_ = new Dictionary<BuffEntity, Transform>();

        #region Events

        /// <summary>Buff 添加层数时触发</summary>
        public event Action<BuffRuntimeData>                    OnAddBuffLayer;

        /// <summary>Buff 移除层数时触发</summary>
        public event Action<BuffRuntimeData>                    OnRemoveBuffLayer;

        /// <summary>Buff 数据完全移除时触发</summary>
        public event Action<BuffRuntimeData>                    OnBuffDataRemoved;

        /// <summary>活跃 Buff 数量变化时触发</summary>
        public event Action<BuffEntity>                         OnActiveBuffCountChange;

        /// <summary>Stackable Buff 层效果触发时</summary>
        public event Action<BuffRuntimeData, int>               OnLayerEffectTrigger;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeWorld();
        }

        private void Update()
        {
            World?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ClearAll();
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region World Initialization

        /// <summary>初始化 ECS World 并注册所有系统</summary>
        private void InitializeWorld()
        {
            World = new BuffWorld();

            // 按优先级注册系统
            DurationSystem = new BuffDurationSystem();
            StackSystem = new BuffStackSystem();
            EffectSystem = new BuffEffectSystem();

            // 优先级：Duration → Stack → Effect
            World.RegisterSystem(DurationSystem, 0);
            World.RegisterSystem(StackSystem, 1);
            World.RegisterSystem(EffectSystem, 2);

            // 确保配置提供者已初始化
            BuffConfigProvider.Initialize();
        }

        #endregion

        #region Entity Management

        /// <summary>为 GameObject 创建/获取对应的 Buff 实体</summary>
        /// <param name="game_object">目标 GameObject</param>
        /// <param name="effect_target">特效挂载点（可选，默认为目标 Transform）</param>
        /// <returns>对应的 BuffEntity</returns>
        public BuffEntity GetOrCreateEntity(GameObject game_object, Transform effect_target = null)
        {
            if (game_object == null)
            {
                Debug.LogError("无法为 null GameObject 创建 BuffEntity");
                return default;
            }

            if (game_object_to_entity_.TryGetValue(game_object, out BuffEntity existing_entity))
            {
                if (World.EntityManager.IsEntityValid(existing_entity))
                {
                    // 更新特效挂载点（如果提供了新的）
                    if (effect_target != null)
                    {
                        World.EntityManager.SetEffectTarget(existing_entity, effect_target);
                    }
                    return existing_entity;
                }
            }

            // 创建新实体
            BuffEntity entity = World.EntityManager.CreateEntity();
            game_object_to_entity_[game_object] = entity;
            entity_to_game_object_[entity] = game_object;

            Transform target = effect_target ?? game_object.transform;
            World.EntityManager.SetEffectTarget(entity, target);
            entity_effect_target_[entity] = target;

            return entity;
        }

        /// <summary>获取 GameObject 对应的 Buff 实体</summary>
        public bool TryGetEntity(GameObject game_object, out BuffEntity entity)
        {
            return game_object_to_entity_.TryGetValue(game_object, out entity);
        }

        /// <summary>获取实体对应的 GameObject</summary>
        public GameObject GetGameObject(BuffEntity entity)
        {
            if (entity_to_game_object_.TryGetValue(entity, out var go))
                return go;
            return null;
        }

        /// <summary>移除 GameObject 的实体绑定（同时清理所有 Buff）</summary>
        public void RemoveEntity(GameObject game_object)
        {
            if (game_object_to_entity_.TryGetValue(game_object, out BuffEntity entity))
            {
                RemoveAllBuffs(entity);
                StackSystem?.ClearEntityStackData(entity);
                World.EntityManager.DestroyEntity(entity);

                game_object_to_entity_.Remove(game_object);
                entity_to_game_object_.Remove(entity);
                entity_effect_target_.Remove(entity);
            }
        }

        /// <summary>获取实体上的 Buff 数量</summary>
        public int GetEntityBuffCount(BuffEntity entity)
        {
            var buffs = World.EntityManager.GetEntityBuffs(entity);
            return buffs?.Count ?? 0;
        }

        #endregion

        #region Buff API

        /// <summary>为目标 GameObject 添加 Buff</summary>
        /// <param name="target">目标 GameObject</param>
        /// <param name="buff_id">Buff ID</param>
        /// <param name="effect_type">Buff 效果类型</param>
        /// <param name="duration">持续时间（-1 使用配置值）</param>
        /// <param name="imposer">施加者 GameObject（可选）</param>
        /// <param name="effect_target">特效挂载点（可选）</param>
        public void AddBuff(GameObject target, int buff_id, BuffEffectType effect_type = BuffEffectType.Refreshable,
            float duration = -1, GameObject imposer = null, Transform effect_target = null)
        {
            if (target == null)
            {
                Debug.LogError("目标对象为空，无法添加 Buff");
                return;
            }

            BuffEntity target_entity = GetOrCreateEntity(target, effect_target);
            BuffEntity imposer_entity = default;

            if (imposer != null)
            {
                imposer_entity = GetOrCreateEntity(imposer);
                World.EntityManager.SetImposer(target_entity, imposer_entity);
            }
            else
            {
                World.EntityManager.SetImposer(target_entity, target_entity);
            }

            // 检查是否被阻挡
            var blocks = World.EntityManager.GetEntityBlocks(target_entity);
            if (blocks != null && blocks.Contains(buff_id))
            {
                return;
            }

            // 获取配置
            if (!BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))
            {
                Debug.LogError($"Buff ID {buff_id} 找不到对应配置");
                return;
            }

            // 获取或计算持续时间
            float final_duration = duration >= 0 ? duration : config.duration_;

            // 检查是否已存在相同 BuffID 的 Buff
            if (World.EntityManager.TryGetBuffData(target_entity, buff_id, out BuffRuntimeData existing_buff))
            {
                // 迭代 Buff
                IterateBuff(target_entity, ref existing_buff, final_duration);
                return;
            }

            // 创建新的 Buff 数据
            CreateNewBuff(target_entity, buff_id, effect_type, final_duration);
        }

        /// <summary>创建新的 Buff</summary>
        private void CreateNewBuff(BuffEntity entity, int buff_id, BuffEffectType effect_type, float duration)
        {
            if (!BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))
                return;

            BuffRuntimeData buff_data = BuffRuntimeData.FromConfig(config);
            buff_data.duration = duration;
            buff_data.target_entity = entity;
            buff_data.imposer_entity = World.EntityManager.GetImposer(entity);
            buff_data.effect_type = effect_type;
            buff_data.effect_target = World.EntityManager.GetEffectTarget(entity);
            buff_data.buff_id = buff_id; // 确保 ID 不被覆盖

            // 初始化计数
            if (effect_type == BuffEffectType.Refreshable)
            {
                buff_data.count = 1;
            }

            // 添加到实体
            World.EntityManager.AddBuffToEntity(entity, buff_data);

            // 创建特效
            EffectSystem?.CreateEffect(entity, buff_data);

            // 如果是 Stackable 类型，添加第一层
            if (effect_type == BuffEffectType.Stackable)
            {
                StackSystem?.AddLayer(entity, buff_data);
            }

            // 触发事件
            OnAddBuffLayer?.Invoke(buff_data);
            OnActiveBuffCountChange?.Invoke(entity);
        }

        /// <summary>迭代已有 Buff（相同 BuffID 再次施加）</summary>
        private void IterateBuff(BuffEntity entity, ref BuffRuntimeData buff_data, float duration)
        {
            buff_data.duration = duration;
            buff_data.target_entity = entity;
            buff_data.imposer_entity = World.EntityManager.GetImposer(entity);

            switch (buff_data.effect_type)
            {
                case BuffEffectType.Refreshable:
                    // 刷新时间，不叠加
                    break;

                case BuffEffectType.Independent:
                    // 叠加层数
                    if (!buff_data.IsFull)
                    {
                        buff_data.count++;
                    }
                    break;

                case BuffEffectType.Stackable:
                    // 添加独立层
                    StackSystem?.AddLayer(entity, buff_data);
                    break;
            }

            // 原地更新实体列表中的 Buff 数据
            var buffs = World.EntityManager.GetEntityBuffs(entity);
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Count; i++)
                {
                    if (buffs[i].buff_id == buff_data.buff_id)
                    {
                        buffs[i] = buff_data;
                        break;
                    }
                }
            }

            // 触发事件
            OnAddBuffLayer?.Invoke(buff_data);
        }

        /// <summary>根据 BuffID 移除实体上的 Buff</summary>
        public bool RemoveBuff(BuffEntity entity, int buff_id)
        {
            var buffs = World.EntityManager.GetEntityBuffs(entity);
            if (buffs == null) return false;

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                if (buffs[i].buff_id == buff_id)
                {
                    var buff = buffs[i];

                    // 释放特效
                    EffectSystem?.ReleaseEffect(entity, buff);

                    // 清理 Stackable 数据
                    if (buff.effect_type == BuffEffectType.Stackable)
                    {
                        StackSystem?.ClearEntityStackData(entity);
                    }

                    // 从实体移除
                    World.EntityManager.RemoveBuffFromEntity(entity, buff);

                    // 触发事件
                    OnBuffDataRemoved?.Invoke(buff);
                    OnActiveBuffCountChange?.Invoke(entity);
                    return true;
                }
            }
            return false;
        }

        /// <summary>移除实体上所有 Buff</summary>
        public void RemoveAllBuffs(BuffEntity entity)
        {
            var buffs = World.EntityManager.GetEntityBuffs(entity);
            if (buffs == null) return;

            // 复制列表避免遍历中修改
            var buff_ids = new List<int>();
            foreach (var buff in buffs)
            {
                buff_ids.Add(buff.buff_id);
            }

            foreach (int buff_id in buff_ids)
            {
                RemoveBuff(entity, buff_id);
            }
        }

        /// <summary>添加禁止施加的 BuffID</summary>
        public void AddBlockBuff(int buff_id, BuffEntity entity, float delay = 0)
        {
            var blocks = World.EntityManager.GetEntityBlocks(entity);
            if (blocks == null) return;

            // 先移除已有的同 ID Buff
            RemoveBuff(entity, buff_id);
            blocks.Add(buff_id);

            // TODO: 支持延迟移除阻挡
        }

        /// <summary>移除禁止施加的 BuffID</summary>
        public void RemoveBlockBuff(int buff_id, BuffEntity entity)
        {
            var blocks = World.EntityManager.GetEntityBlocks(entity);
            blocks?.Remove(buff_id);
        }

        /// <summary>清除所有实体和数据</summary>
        public void ClearAll()
        {
            // 清理所有实体的特效
            foreach (var entity in World.EntityManager.GetAllEntities())
            {
                EffectSystem?.ReleaseAllEffects(entity);
                StackSystem?.ClearEntityStackData(entity);
            }

            World?.Clear();
            game_object_to_entity_.Clear();
            entity_to_game_object_.Clear();
            entity_effect_target_.Clear();
        }

        #endregion

        #region Notification Methods (被 System 调用)

        internal void NotifyBuffRemoved(BuffRuntimeData buff)
        {
            OnBuffDataRemoved?.Invoke(buff);
        }

        internal void NotifyBuffCountChanged(BuffEntity entity)
        {
            OnActiveBuffCountChange?.Invoke(entity);
        }

        internal void NotifyBuffLayerRemoved(BuffRuntimeData buff)
        {
            OnRemoveBuffLayer?.Invoke(buff);
        }

        internal void NotifyLayerEffectTrigger(BuffRuntimeData buff, int layer_index)
        {
            OnLayerEffectTrigger?.Invoke(buff, layer_index);
        }

        #endregion
    }
}