namespace BuffSystemECS
{
    /// <summary>
    /// Buff 系统接口
    /// 所有 Buff 逻辑都以 System 的形式存在，不绑定在任何实体上
    /// </summary>
    public interface IBuffSystem
    {
        /// <summary>
        /// 系统初始化，在注册到 World 时调用
        /// </summary>
        void Initialize(BuffWorld world);

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update(float deltaTime);
    }

    /// <summary>
    /// Buff 系统抽象基类
    /// </summary>
    public abstract class BuffSystemBase : IBuffSystem
    {
        /// <summary>
        /// 所属的 World
        /// </summary>
        protected BuffWorld World { get; private set; }

        /// <summary>
        /// 所属的 EntityManager 快捷访问
        /// </summary>
        protected BuffEntityManager EntityManager => World?.EntityManager;

        public virtual void Initialize(BuffWorld world)
        {
            World = world;
        }

        public abstract void Update(float deltaTime);
    }
}