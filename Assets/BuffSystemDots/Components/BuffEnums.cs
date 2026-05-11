namespace BuffSystemDots
{
    /// <summary>
    /// Buff 生效类型
    /// </summary>
    public enum BuffEffectType : byte
    {
        /// <summary>可刷新 — 叠加时刷新剩余时间</summary>
        Refreshable = 0,

        /// <summary>独立 — 叠加时各自独立计时</summary>
        Independent = 1,

        /// <summary>可叠加 — 多层独立计时，共用特效</summary>
        Stackable = 2,
    }

    /// <summary>
    /// Buff 配置标签类型（如增益、减益、控制等）
    /// </summary>
    public enum BuffTagType : byte
    {
        /// <summary>增益 Buff</summary>
        Buff = 0,

        /// <summary>减益 Debuff</summary>
        Debuff = 1,

        /// <summary>控制效果</summary>
        Control = 2,

        /// <summary>其他</summary>
        Other = 3,
    }
}