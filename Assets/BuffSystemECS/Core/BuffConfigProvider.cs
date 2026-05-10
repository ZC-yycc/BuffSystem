using System.Collections.Generic;
using UnityEngine;

namespace BuffSystemECS
{
    /// <summary>
    /// Buff 配置提供者，从 Resources 中加载 ScriptableObject 配置
    /// 在 ECS 中属于纯数据层，不包含行为
    /// </summary>
    public static class BuffConfigProvider
    {
        private const string                                        CONFIG_PATH = "BuffData/";

        /// <summary>
        /// Buff配置字典，键为Buff ID，值为对应的BuffConfig
        /// </summary>
        private static readonly Dictionary<int, BuffConfig>         configs_ = new Dictionary<int, BuffConfig>();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private static bool                                         is_initialized_ = false;

        static BuffConfigProvider()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化，加载所有 BuffConfig 资源
        /// </summary>
        public static void Initialize()
        {
            if (is_initialized_) return;
            is_initialized_ = true;

            configs_.Clear();
            BuffConfig[] configs = Resources.LoadAll<BuffConfig>(CONFIG_PATH);
            foreach (var config in configs)
            {
                if (config != null)
                {
                    configs_[config.buff_id_] = config;
                }
            }
        }

        /// <summary>
        /// 重新加载配置（供编辑器下热重载使用）
        /// </summary>
        public static void Reload()
        {
            is_initialized_ = false;
            Initialize();
        }

        /// <summary>
        /// 根据 BuffID 获取配置
        /// </summary>
        public static bool TryGetConfig(int buffId, out BuffConfig config)
        {
            return configs_.TryGetValue(buffId, out config);
        }

        /// <summary>
        /// 检查 BuffID 是否存在
        /// </summary>
        public static bool HasConfig(int buffId)
        {
            return configs_.ContainsKey(buffId);
        }
    }
}