using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 持续时间系统 — 使用 DOTS ISystem 结构
    /// 每帧更新所有 Refreshable/Independent 类型 Buff 的剩余时间，标记过期 Buff
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuffApplySystem))]
    public partial struct BuffDurationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 需要注册的组件类型由 query 自动管理
            state.RequireForUpdate<BuffConfigRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // 处理 Non-Stackable Buff 的持续时间
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var configLookup = SystemAPI.GetSingleton<BuffConfigRegistry>();

            // 遍历所有 BuffInstanceTag 且非 Stackable 的实体
            foreach (var (runtimeData, entity) in
                     SystemAPI.Query<RefRW<BuffRuntimeData>>()
                         .WithAll<BuffInstanceTag>()
                         .WithNone<BuffExpiredTag>()
                         .WithEntityAccess())
            {
                // Stackable 类型由 BuffStackSystem 处理
                if (runtimeData.ValueRO.EffectType == BuffEffectType.Stackable)
                    continue;

                // Refreshable / Independent 类型：时间递减
                runtimeData.ValueRW.Duration -= deltaTime;

                // 检查是否过期
                if (runtimeData.ValueRO.Duration <= 0f)
                {
                    // 标记为过期
                    ecb.AddComponent<BuffExpiredTag>(entity);

                    // 通知外部系统
                    var evt = new BuffEffectTrigger
                    {
                        Target = entity,
                        BuffId = runtimeData.ValueRO.BuffId,
                        ConfigId = runtimeData.ValueRO.ConfigId,
                        EffectValue = 0f,
                    };
                    ecb.AddComponent(entity, evt);
                }
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