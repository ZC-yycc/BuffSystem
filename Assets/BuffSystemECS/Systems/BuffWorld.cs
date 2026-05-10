using System.Collections.Generic;

namespace BuffSystemECS
{
    /// <summary>
    /// ECS World — 管理所有 Entity 和 System 的顶层容器
    /// 每帧调用所有注册的 System 来驱动 Buff 逻辑
    /// </summary>
    public class BuffWorld
    {
        /// <summary>实体管理器</summary>
        public readonly BuffEntityManager EntityManager;

        /// <summary>所有注册的系统</summary>
        private readonly List<IBuffSystem> systems_ = new List<IBuffSystem>();

        /// <summary>系统执行顺序（名称 → 优先级）</summary>
        private readonly Dictionary<string, int> execution_order_ = new Dictionary<string, int>();

        /// <summary>是否已初始化</summary>
        private bool is_initialized_ = false;

        public BuffWorld()
        {
            EntityManager = new BuffEntityManager();
        }

        /// <summary>注册系统</summary>
        public void RegisterSystem<T>(T system, int priority = 0) where T : IBuffSystem
        {
            if (system == null) return;

            system.Initialize(this);
            systems_.Add(system);
            execution_order_[system.GetType().Name] = priority;

            // 按优先级排序
            systems_.Sort((a, b) =>
            {
                int pa = execution_order_.TryGetValue(a.GetType().Name, out int p1) ? p1 : 0;
                int pb = execution_order_.TryGetValue(b.GetType().Name, out int p2) ? p2 : 0;
                return pa.CompareTo(pb);
            });
        }

        /// <summary>注销系统</summary>
        public void UnregisterSystem<T>(T system) where T : IBuffSystem
        {
            systems_.Remove(system);
            execution_order_.Remove(system.GetType().Name);
        }

        /// <summary>每帧更新所有系统</summary>
        public void Update(float delta_time)
        {
            for (int i = 0; i < systems_.Count; i++)
            {
                systems_[i].Update(delta_time);
            }
        }

        /// <summary>清空所有系统和实体</summary>
        public void Clear()
        {
            systems_.Clear();
            execution_order_.Clear();
            EntityManager.Clear();
            is_initialized_ = false;
        }
    }
}