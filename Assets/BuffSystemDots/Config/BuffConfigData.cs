using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 配置数据 — ScriptableObject，用于在 Inspector 中配置 Buff 参数
    /// 运行时由 BuffDotsBootstrap 转换为 BlobAsset
    /// </summary>
    [CreateAssetMenu(fileName = "BuffConfig", menuName = "BuffSystemDots/Buff Config", order = 1)]
    public class BuffConfigData : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("配置唯一 ID")]
        public int                          config_id_;

        [Tooltip("Buff 名称")]
        public string                       buff_name_ = "New Buff";

        [Tooltip("Buff 生效类型")]
        public BuffEffectType               effect_type_ = BuffEffectType.Refreshable;

        [Tooltip("Buff 标签类型")]
        public BuffTagType                  tag_type_ = BuffTagType.Buff;

        [Header("持续时间")]
        [Tooltip("Buff 总持续时间（秒），负数或 0 表示无限持续")]
        [Min(0f)]
        public float                        duration_ = 5f;

        [Header("叠加规则")]
        [Tooltip("最大层数（仅 Stackable 类型有效）")]
        [Min(1)]
        public int                          max_count_ = 5;

        [Header("触发设置")]
        [Tooltip("持续效果触发间隔（秒），0 表示不触发")]
        [Min(0f)]
        public float                        trigger_interval_ = 1f;

        [Header("行为标志")]
        [Tooltip("是否可被同类型刷新")]
        public bool                         is_refreshable_ = true;

        [Tooltip("是否可被驱散")]
        public bool                         is_dispellable_ = true;
    }
}