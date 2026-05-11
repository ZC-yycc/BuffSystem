using Unity.Entities;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 运行时数据组件 — 挂载在代表 Buff 实例的 Entity 上
    /// </summary>
    public struct BuffRuntimeData : IComponentData
    {
        /// <summary>目标实体（被施加 Buff 的实体）</summary>
        public Entity Target;

        /// <summary>Buff 唯一 ID</summary>
        public int BuffId;

        /// <summary>Buff 配置 ID（指向 BlobAsset 中的 BuffConfig）</summary>
        public int ConfigId;

        /// <summary>生效类型：Refreshable / Independent / Stackable</summary>
        public BuffEffectType EffectType;

        /// <summary>总剩余时间（Refreshable / Independent 使用）</summary>
        public float Duration;

        /// <summary>初始总时间</summary>
        public float TotalDuration;

        /// <summary>最大层数（Stackable 使用）</summary>
        public int MaxCount;

        /// <summary>当前层数（Stackable 使用）</summary>
        public int CurrentCount;

        /// <summary>持续效果触发间隔</summary>
        public float TriggerInterval;

        /// <summary>上一次触发的时间</summary>
        public float LastTriggerTime;

        /// <summary>效果值（伤害/治疗等）</summary>
        public float EffectValue;
    }

    /// <summary>
    /// Buff 目标实体标签 — 挂载在被施加 Buff 的实体上
    /// </summary>
    public struct BuffTargetTag : IComponentData
    {
    }

    /// <summary>
    /// Buff 实例标签 — 挂载在代表 Buff 实例的实体上
    /// </summary>
    public struct BuffInstanceTag : IComponentData
    {
    }

    /// <summary>
    /// Stackable Buff 层数据组件 — 挂载在每一层对应的子实体上
    /// </summary>
    public struct BuffLayerData : IComponentData
    {
        /// <summary>所属 Buff 实例 Entity</summary>
        public Entity ParentBuff;

        /// <summary>层索引</summary>
        public int LayerIndex;

        /// <summary>该层的剩余时间</summary>
        public float LayerDuration;

        /// <summary>该层上一次触发效果的时间</summary>
        public float LastTriggerTime;
    }

    /// <summary>
    /// Buff 层标签 — 挂载在层实体上
    /// </summary>
    public struct BuffLayerTag : IComponentData
    {
    }

    /// <summary>
    /// 特效引用组件 — 挂载在 Buff 实例 Entity 上
    /// </summary>
    public struct BuffEffectReference : IComponentData
    {
        /// <summary>特效 GameObject 的辅助标识（使用 int 作为 key）</summary>
        public int EffectInstanceId;

        /// <summary>特效预制体 Hash</summary>
        public int EffectPrefabHash;
    }

    /// <summary>
    /// 特效目标 Transform 引用 — 挂载在被施加 Buff 的实体上（通过 IComponentData 存储引用）
    /// </summary>
    public struct BuffEffectTarget : IComponentData
    {
        /// <summary>是否已设置特效目标</summary>
        public bool IsValid;
    }

    /// <summary>
    /// 挂载点引用 — 存储目标 Transform 的 Entity 引用
    /// 特效目标通过一个专门的 Entity 来持有 GameObject 的 Transform
    /// </summary>
    public struct BuffTargetTransform : IComponentData
    {
        /// <summary>持有 Transform 的 Entity（通过 GameObjectEntity 或 CompanionLink）</summary>
        public Entity TransformEntity;
    }

    /// <summary>
    /// Buff 过期标签 — 标记需要被移除的 Buff
    /// </summary>
    public struct BuffExpiredTag : IComponentData
    {
    }

    /// <summary>
    /// 层过期标签 — 标记需要被移除的层
    /// </summary>
    public struct BuffLayerExpiredTag : IComponentData
    {
    }

    /// <summary>
    /// 请求添加 Buff 的组件 — 由外部系统写入，Buff 系统读取后处理
    /// </summary>
    public struct BuffAddRequest : IComponentData
    {
        /// <summary>目标实体</summary>
        public Entity Target;

        /// <summary>Buff 配置 ID</summary>
        public int ConfigId;

        /// <summary>效果值</summary>
        public float EffectValue;
    }

    /// <summary>
    /// 请求移除 Buff 的组件 — 由外部系统写入
    /// </summary>
    public struct BuffRemoveRequest : IComponentData
    {
        /// <summary>目标实体</summary>
        public Entity Target;

        /// <summary>要移除的 Buff ID</summary>
        public int BuffId;
    }

    /// <summary>
    /// Buff 计数变化通知组件
    /// </summary>
    public struct BuffCountChanged : IComponentData
    {
        public Entity Target;
        public int TotalCount;
    }

    /// <summary>
    /// Buff 效果触发通知组件
    /// </summary>
    public struct BuffEffectTrigger : IComponentData
    {
        public Entity Target;
        public int BuffId;
        public int ConfigId;
        public float EffectValue;
    }

    /// <summary>
    /// Buff 配置实体标签 — 标记持有 BuffConfigRegistry 的单例实体
    /// </summary>
    public struct ConfigEntityTag : IComponentData
    {
    }

    /// <summary>
    /// 请求清空目标上所有 Buff 的组件
    /// </summary>
    public struct BuffClearAllRequest : IComponentData
    {
        /// <summary>目标实体</summary>
        public Entity Target;
    }
}
