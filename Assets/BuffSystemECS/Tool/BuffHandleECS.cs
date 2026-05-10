// 此文件由BuffHandleECSGenerater自动生成，请勿手动修改
// 生成器文件路径: Assets/BuffSystemECS/Editor/BuffHandleECSGenerater.cs

using UnityEngine;
using BuffSystemECS;

/// <summary>
/// Buff 便捷调用类（ECS版本）
/// 提供静态方法，通过 buff_id 快速添加/移除 Buff，内部使用 BuffSystemECSManager
/// </summary>
public static class BuffHandleECS
{
    /// <summary>
    /// 为目标对象添加Buff（使用默认配置的类型和时长）
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">Buff配置ID</param>
    public static void AddBuff(GameObject target, int buff_id)
    {
        if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))
        {
            BuffEffectType type = GetEffectTypeFromConfig(config);
            BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type);
        }
        else
        {
            Debug.LogError($"Buff ID {buff_id} 找不到对应配置");
        }
    }

    /// <summary>
    /// 为目标对象添加Buff（指定时长）
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">Buff配置ID</param>
    /// <param name="duration">Buff持续时长（秒）</param>
    public static void AddBuff(GameObject target, int buff_id, float duration)
    {
        if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))
        {
            BuffEffectType type = GetEffectTypeFromConfig(config);
            BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, duration);
        }
        else
        {
            Debug.LogError($"Buff ID {buff_id} 找不到对应配置");
        }
    }

    /// <summary>
    /// 为目标对象添加Buff（指定施加者）
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">Buff配置ID</param>
    /// <param name="imposer">Buff施加者</param>
    public static void AddBuff(GameObject target, int buff_id, GameObject imposer)
    {
        if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))
        {
            BuffEffectType type = GetEffectTypeFromConfig(config);
            BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, -1, imposer);
        }
        else
        {
            Debug.LogError($"Buff ID {buff_id} 找不到对应配置");
        }
    }

    /// <summary>
    /// 为目标对象添加Buff（完整参数）
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">Buff配置ID</param>
    /// <param name="type">Buff效果类型</param>
    /// <param name="duration">Buff持续时长（秒，-1使用配置值）</param>
    /// <param name="imposer">Buff施加者（可选）</param>
    /// <param name="effect_target">特效挂载点（可选）</param>
    public static void AddBuff(GameObject target, int buff_id, BuffEffectType type, float duration = -1, GameObject imposer = null, Transform effect_target = null)
    {
        BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, duration, imposer, effect_target);
    }

    /// <summary>
    /// 移除目标对象上的指定Buff
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">要移除的Buff配置ID</param>
    public static bool RemoveBuff(GameObject target, int buff_id)
    {
        if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))
        {
            return BuffSystemECSManager.Instance.RemoveBuff(entity, buff_id);
        }
        return false;
    }

    /// <summary>
    /// 移除目标对象上的所有Buff
    /// </summary>
    /// <param name="target">目标GameObject</param>
    public static void RemoveAllBuffs(GameObject target)
    {
        if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))
        {
            BuffSystemECSManager.Instance.RemoveAllBuffs(entity);
        }
    }

    /// <summary>
    /// 禁止施加指定Buff
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">要禁止的Buff配置ID</param>
    public static void AddBlockBuff(GameObject target, int buff_id)
    {
        if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))
        {
            BuffSystemECSManager.Instance.AddBlockBuff(buff_id, entity);
        }
    }

    /// <summary>
    /// 移除禁止施加指定Buff的限制
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <param name="buff_id">要解除禁止的Buff配置ID</param>
    public static void RemoveBlockBuff(GameObject target, int buff_id)
    {
        if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))
        {
            BuffSystemECSManager.Instance.RemoveBlockBuff(buff_id, entity);
        }
    }

    /// <summary>
    /// 获取目标对象上活跃Buff的数量
    /// </summary>
    /// <param name="target">目标GameObject</param>
    /// <returns>活跃Buff数量</returns>
    public static int GetActiveBuffCount(GameObject target)
    {
        if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))
        {
            return BuffSystemECSManager.Instance.GetEntityBuffCount(entity);
        }
        return 0;
    }

    private static BuffEffectType GetEffectTypeFromConfig(BuffConfig config)
    {
        // 默认使用 Refreshable 类型，可通过配置扩展
        return BuffEffectType.Refreshable;
    }
}
