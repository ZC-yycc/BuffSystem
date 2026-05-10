using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// Buff 运行时纯数据组件
    /// 在 ECS 中，Component 仅存储数据，不包含行为
    /// </summary>
    public struct BuffRuntimeData
    {
        /// <summary>Buff唯一ID</summary>
        public int                                  buff_id;

        /// <summary>Buff名称</summary>
        public string                               buff_name;

        /// <summary>Buff图标</summary>
        public Sprite                               icon;

        /// <summary>Buff描述</summary>
        public string                               description;

        /// <summary>Buff剩余时间</summary>
        public float                                duration;

        /// <summary>Buff效果值</summary>
        public float                                value;

        /// <summary>当前层数</summary>
        public int                                  count;

        /// <summary>最大层数</summary>
        public int                                  max_count;

        /// <summary>Buff效果类型枚举 (Independent, Refreshable, Stackable)</summary>
        public BuffEffectType                       effect_type;

        /// <summary>特效预制体</summary>
        public GameObject                           effect_prefab;

        /// <summary>特效的Transform挂载点</summary>
        public Transform                            effect_target;

        /// <summary>实体引用</summary>
        public BuffEntity                           target_entity;

        /// <summary>施加者实体引用</summary>
        public BuffEntity                           imposer_entity;

        /// <summary>层数是否已满</summary>
        public readonly bool IsFull => count >= max_count;

        /// <summary>
        /// 从 ScriptableObject 配置创建运行时数据（不含实体引用）
        /// </summary>
        public static BuffRuntimeData FromConfig(BuffConfig config)
        {
            return new BuffRuntimeData
            {
                buff_id = config.buff_id_,
                buff_name = config.name_,
                icon = config.icon_,
                description = config.buff_description_,
                duration = config.duration_,
                value = config.value_,
                max_count = config.stack_count_,
                effect_prefab = config.effect_prefab_,
                count = 0,
                effect_type = BuffEffectType.Refreshable
            };
        }
    }

    /// <summary>
    /// Buff 效果类型枚举 — 决定 System 如何处理该 Buff
    /// </summary>
    public enum BuffEffectType
    {
        /// <summary>不可叠加，仅刷新时间</summary>
        Refreshable = 0,

        /// <summary>可叠加层数，刷新时间</summary>
        Independent = 1,

        /// <summary>独立叠加，各层互不影响</summary>
        Stackable = 2
    }
}