using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BuffSystemDots
{
    /// <summary>
    /// Buff 施加系统 — 处理 BuffAddRequest，创建 Buff 实例实体
    /// 支持 Refreshable / Independent / Stackable 三种类型的添加逻辑
    /// 使用 SystemBase（类系统）避免 Burst 嵌套 Query 限制
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BuffApplySystem : SystemBase
    {
        /// <summary>全局递增 Buff ID</summary>
        private int nextBuffId;

        protected override void OnCreate()
        {
            nextBuffId = 1;
            RequireForUpdate<BuffConfigRegistry>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var configRegistry = SystemAPI.GetSingleton<BuffConfigRegistry>();
            ref var configBlob = ref configRegistry.configs.Value;

            // 先收集所有请求（避免在 nested foreach 中嵌套 Query）
            var requests = new NativeList<BuffAddRequest>(64, Allocator.Temp);
            var requestEntities = new NativeList<Entity>(64, Allocator.Temp);
            foreach (var (request, entity) in
                     SystemAPI.Query<RefRO<BuffAddRequest>>()
                         .WithEntityAccess())
            {
                requests.Add(request.ValueRO);
                requestEntities.Add(entity);
            }

            // 处理请求
            for (int i = 0; i < requests.Length; i++)
            {
                ProcessAddRequest(ecb, requests[i], ref configBlob, ref nextBuffId);
                ecb.DestroyEntity(requestEntities[i]);
            }

            requests.Dispose();
            requestEntities.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void ProcessAddRequest(EntityCommandBuffer ecb,
            BuffAddRequest request, ref BuffConfigBlobArray configBlob, ref int buffIdCounter)
        {
            if (request.Target == Entity.Null) return;

            if (!TryGetConfig(ref configBlob, request.ConfigId, out var config))
            {
                Debug.LogWarning($"BuffApplySystem: ConfigId {request.ConfigId} not found.");
                return;
            }

            switch (config.effect_type)
            {
                case BuffEffectType.Refreshable:
                    ApplyRefreshableBuff(ecb, request, config, ref buffIdCounter);
                    break;
                case BuffEffectType.Independent:
                    ApplyIndependentBuff(ecb, request, config, ref buffIdCounter);
                    break;
                case BuffEffectType.Stackable:
                    ApplyStackableBuff(ecb, request, config, ref buffIdCounter);
                    break;
            }
        }

        private void ApplyRefreshableBuff(EntityCommandBuffer ecb,
            BuffAddRequest request, BuffConfigBlob config, ref int buffIdCounter)
        {
            if (TryFindExistingBuff(request.Target, config.config_id, out Entity existingBuff,
                    out BuffRuntimeData existingData))
            {
                // 使用 ECB 刷新持续时间
                ecb.SetComponent(existingBuff, new BuffRuntimeData
                {
                    Target = existingData.Target,
                    BuffId = existingData.BuffId,
                    ConfigId = existingData.ConfigId,
                    EffectType = existingData.EffectType,
                    Duration = config.duration,
                    TotalDuration = config.duration,
                    MaxCount = existingData.MaxCount,
                    CurrentCount = existingData.CurrentCount,
                    TriggerInterval = existingData.TriggerInterval,
                    LastTriggerTime = existingData.LastTriggerTime,
                    EffectValue = existingData.EffectValue,
                });
            }
            else
            {
                CreateNewBuffEntity(ecb, request, config, ref buffIdCounter);
            }
        }

        private void ApplyIndependentBuff(EntityCommandBuffer ecb,
            BuffAddRequest request, BuffConfigBlob config, ref int buffIdCounter)
        {
            CreateNewBuffEntity(ecb, request, config, ref buffIdCounter);
        }

        private void ApplyStackableBuff(EntityCommandBuffer ecb,
            BuffAddRequest request, BuffConfigBlob config, ref int buffIdCounter)
        {
            if (TryFindExistingBuff(request.Target, config.config_id, out Entity existingBuff,
                    out BuffRuntimeData existingData))
            {
                int newCount = math.min(existingData.CurrentCount + 1, config.max_count);

                // 使用 ECB 更新 Buff 实例
                ecb.SetComponent(existingBuff, new BuffRuntimeData
                {
                    Target = existingData.Target,
                    BuffId = existingData.BuffId,
                    ConfigId = existingData.ConfigId,
                    EffectType = existingData.EffectType,
                    Duration = config.duration,
                    TotalDuration = config.duration,
                    MaxCount = config.max_count,
                    CurrentCount = newCount,
                    TriggerInterval = existingData.TriggerInterval,
                    LastTriggerTime = existingData.LastTriggerTime,
                    EffectValue = request.EffectValue,
                });

                // 创建层实体
                Entity layerEntity = ecb.CreateEntity();
                ecb.AddComponent(layerEntity, new BuffLayerData
                {
                    ParentBuff = existingBuff,
                    LayerIndex = newCount - 1,
                    LayerDuration = config.duration,
                    LastTriggerTime = (float)SystemAPI.Time.ElapsedTime,
                });
                ecb.AddComponent<BuffLayerTag>(layerEntity);

                ecb.AddComponent(request.Target, new BuffCountChanged
                {
                    Target = request.Target,
                    TotalCount = newCount,
                });
            }
            else
            {
                CreateNewBuffEntity(ecb, request, config, ref buffIdCounter);
            }
        }

        private void CreateNewBuffEntity(EntityCommandBuffer ecb, BuffAddRequest request,
            BuffConfigBlob config, ref int buffIdCounter)
        {
            Entity buffEntity = ecb.CreateEntity();

            ecb.AddComponent(buffEntity, new BuffRuntimeData
            {
                Target = request.Target,
                BuffId = buffIdCounter++,
                ConfigId = config.config_id,
                EffectType = config.effect_type,
                Duration = config.duration,
                TotalDuration = config.duration,
                MaxCount = config.max_count,
                CurrentCount = 1,
                TriggerInterval = config.trigger_interval,
                LastTriggerTime = (float)SystemAPI.Time.ElapsedTime,
                EffectValue = request.EffectValue,
            });

            ecb.AddComponent<BuffInstanceTag>(buffEntity);
            ecb.AddComponent(buffEntity, new BuffConfigRef { config_id = config.config_id });

            if (config.effect_type == BuffEffectType.Stackable)
            {
                Entity layerEntity = ecb.CreateEntity();
                ecb.AddComponent(layerEntity, new BuffLayerData
                {
                    ParentBuff = buffEntity,
                    LayerIndex = 0,
                    LayerDuration = config.duration,
                    LastTriggerTime = (float)SystemAPI.Time.ElapsedTime,
                });
                ecb.AddComponent<BuffLayerTag>(layerEntity);
            }
        }

        private bool TryFindExistingBuff(Entity target, int configId,
            out Entity outEntity, out BuffRuntimeData outData)
        {
            outEntity = Entity.Null;
            outData = default;

            foreach (var (runtimeData, entity) in
                     SystemAPI.Query<RefRO<BuffRuntimeData>>()
                         .WithAll<BuffInstanceTag>()
                         .WithNone<BuffExpiredTag>()
                         .WithEntityAccess())
            {
                if (runtimeData.ValueRO.Target == target && runtimeData.ValueRO.ConfigId == configId)
                {
                    outEntity = entity;
                    outData = runtimeData.ValueRO;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetConfig(ref BuffConfigBlobArray configBlob, int configId, out BuffConfigBlob config)
        {
            for (int i = 0; i < configBlob.items.Length; i++)
            {
                if (configBlob.items[i].config_id == configId)
                {
                    config = configBlob.items[i];
                    return true;
                }
            }
            config = default;
            return false;
        }

        protected override void OnDestroy()
        {
        }
    }
}