using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 叠加层管理系统 — 处理 Stackable 类型 Buff 的层生命周期
    /// 每层作为独立子实体存在，各自计时，共用父 Buff 的特效
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffApplySystem))]
    [UpdateBefore(typeof(BuffDurationSystem))]
    public partial struct BuffStackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BuffConfigRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Pass 1: 收集过期层信息（避免在迭代中跨实体访问 RefRW）
            var expiredParentBuffs = new NativeList<Entity>(64, Allocator.Temp);
            var triggerParentBuffs = new NativeList<Entity>(64, Allocator.Temp);

            foreach (var (layerData, layerEntity) in
                     SystemAPI.Query<RefRW<BuffLayerData>>()
                         .WithAll<BuffLayerTag>()
                         .WithNone<BuffLayerExpiredTag>()
                         .WithEntityAccess())
            {
                layerData.ValueRW.LayerDuration -= deltaTime;

                if (layerData.ValueRO.LayerDuration <= 0f)
                {
                    ecb.AddComponent<BuffLayerExpiredTag>(layerEntity);
                    var parentBuff = layerData.ValueRO.ParentBuff;
                    if (parentBuff != Entity.Null)
                    {
                        expiredParentBuffs.Add(parentBuff);
                    }
                }
                else if (currentTime - layerData.ValueRO.LastTriggerTime >= 1.0f)
                {
                    layerData.ValueRW.LastTriggerTime = currentTime;
                    var parentBuff = layerData.ValueRO.ParentBuff;
                    if (parentBuff != Entity.Null)
                    {
                        triggerParentBuffs.Add(parentBuff);
                    }
                }
            }

            // Pass 2: 处理过期层的父 Buff 计数递减
            if (expiredParentBuffs.Length > 0)
            {
                foreach (var (runtimeData, buffEntity) in
                         SystemAPI.Query<RefRW<BuffRuntimeData>>()
                             .WithAll<BuffInstanceTag>()
                             .WithNone<BuffExpiredTag>()
                             .WithEntityAccess())
                {
                    for (int i = 0; i < expiredParentBuffs.Length; i++)
                    {
                        if (buffEntity == expiredParentBuffs[i])
                        {
                            runtimeData.ValueRW.CurrentCount = math.max(0, runtimeData.ValueRW.CurrentCount - 1);
                        }
                    }
                }
            }

            // Pass 3: 处理触发效果
            if (triggerParentBuffs.Length > 0)
            {
                foreach (var (runtimeData, buffEntity) in
                         SystemAPI.Query<RefRO<BuffRuntimeData>>()
                             .WithAll<BuffInstanceTag>()
                             .WithNone<BuffExpiredTag>()
                             .WithEntityAccess())
                {
                    for (int i = 0; i < triggerParentBuffs.Length; i++)
                    {
                        if (buffEntity == triggerParentBuffs[i])
                        {
                            ecb.AddComponent(buffEntity, new BuffEffectTrigger
                            {
                                Target = buffEntity,
                                BuffId = runtimeData.ValueRO.BuffId,
                                ConfigId = runtimeData.ValueRO.ConfigId,
                                EffectValue = runtimeData.ValueRO.EffectValue,
                            });
                            break;
                        }
                    }
                }
            }

            expiredParentBuffs.Dispose();
            triggerParentBuffs.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
