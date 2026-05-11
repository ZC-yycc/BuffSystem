using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 移除系统 — 处理 BuffRemoveRequest，移除指定 Buff 实例
    /// 将多层嵌套 Query 摊平到 OnUpdate 中，避免 Burst 上下文嵌套问题
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffDurationSystem))]
    [UpdateAfter(typeof(BuffApplySystem))]
    public partial struct BuffRemoveSystem : ISystem
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

            // Step 1: 收集所有 BuffRemoveRequest
            var requestIds = new NativeList<int>(64, Allocator.Temp);
            foreach (var (request, requestEntity) in
                     SystemAPI.Query<RefRO<BuffRemoveRequest>>()
                         .WithEntityAccess())
            {
                requestIds.Add(request.ValueRO.BuffId);
                ecb.DestroyEntity(requestEntity);
            }

            if (requestIds.Length == 0)
            {
                requestIds.Dispose();
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                return;
            }

            // Step 2: 标记所有匹配的 Buff 实体为过期
            var expiredBuffEntities = new NativeList<Entity>(64, Allocator.Temp);
            foreach (var (runtimeData, buffEntity) in
                     SystemAPI.Query<RefRO<BuffRuntimeData>>()
                         .WithAll<BuffInstanceTag>()
                         .WithNone<BuffExpiredTag>()
                         .WithEntityAccess())
            {
                for (int i = 0; i < requestIds.Length; i++)
                {
                    if (runtimeData.ValueRO.BuffId == requestIds[i])
                    {
                        ecb.AddComponent<BuffExpiredTag>(buffEntity);
                        if (runtimeData.ValueRO.EffectType == BuffEffectType.Stackable)
                        {
                            expiredBuffEntities.Add(buffEntity);
                        }
                        break;
                    }
                }
            }

            // Step 3: 对 Stackable Buff，标记所有子层过期
            if (expiredBuffEntities.Length > 0)
            {
                foreach (var (layerData, layerEntity) in
                         SystemAPI.Query<RefRO<BuffLayerData>>()
                             .WithAll<BuffLayerTag>()
                             .WithNone<BuffLayerExpiredTag>()
                             .WithEntityAccess())
                {
                    for (int i = 0; i < expiredBuffEntities.Length; i++)
                    {
                        if (layerData.ValueRO.ParentBuff == expiredBuffEntities[i])
                        {
                            ecb.AddComponent<BuffLayerExpiredTag>(layerEntity);
                            break;
                        }
                    }
                }
            }

            requestIds.Dispose();
            expiredBuffEntities.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
