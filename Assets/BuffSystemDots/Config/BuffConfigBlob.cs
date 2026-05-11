using Unity.Collections;
using Unity.Entities;


namespace BuffSystemDots
{
    /// <summary>
    /// Buff 静态配置 Blob 数据 — 一次性创建，通过 BlobAssetReference 在多个实体间共享
    /// </summary>
    public struct BuffConfigBlob
    {
        /// <summary>配置 ID</summary>
        public int                              config_id; 

        /// <summary>Buff 名称</summary>
        public FixedString64Bytes               buff_name;

        /// <summary>生效类型</summary>
        public BuffEffectType                   effect_type;

        /// <summary>标签类型</summary>
        public BuffTagType                      tag_type;

        /// <summary>Buff 总持续时间</summary>
        public float                            duration;

        /// <summary>最大层数（仅 Stackable 有效）</summary>
        public int                              max_count;

        /// <summary>持续效果触发间隔</summary>
        public float                            trigger_interval;

        /// <summary>是否可被同类型刷新</summary>
        public bool                             is_refreshable;

        /// <summary>是否可被驱散</summary>
        public bool                             is_dispellable;
    }

    /// <summary>
    /// BlobAsset 引用组件 — 挂载在单例 Entity 上，持有所有 Buff 配置的 BlobArray
    /// </summary>
    public struct BuffConfigRegistry : IComponentData
    {
        public BlobAssetReference<BuffConfigBlobArray> configs;
    }

    /// <summary>
    /// BlobArray 包裹结构 — BlobAsset 的根类型
    /// </summary>
    public struct BuffConfigBlobArray
    {
        public BlobArray<BuffConfigBlob> items;
    }

    /// <summary>
    /// Buff 配置查找组件 — 通过 ConfigId 指向 BlobAsset 中的配置
    /// </summary>
    public struct BuffConfigRef : IComponentData
    {
        /// <summary>配置 ID</summary>
        public int config_id;
    }
}