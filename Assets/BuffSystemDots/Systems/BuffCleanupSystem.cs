using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 清理系统 — 销毁所有标记为过期的 Buff 实体和层实体
    /// 在所有其他 Buff 系统之后执行
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffRemoveSystem))]
    [UpdateAfter(typeof(BuffDurationSystem))]
    [UpdateAfter(typeof(BuffStackSystem))]
    public partial struct BuffCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuffConfigRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 销毁所有过期的 Buff 层实体
            foreach (var (layerData, layerEntity) in
                     SystemAPI.Query<RefRO<BuffLayerData>>()
                         .WithAll<BuffLayerExpiredTag>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(layerEntity);
            }

            // 销毁所有过期的 Buff 实例实体
            foreach (var (runtimeData, buffEntity) in
                     SystemAPI.Query<RefRO<BuffRuntimeData>>()
                         .WithAll<BuffExpiredTag, BuffInstanceTag>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(buffEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}