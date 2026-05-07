using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public interface IBuffHelper
{
    void AddBuff<T>(BuffComponent imposer, int buff_id, float duration = -1) where T : IBuffEffect, new();
}


/// <summary>
/// Buff 数据创建工厂
/// </summary>
/// <typeparam name="T">BuffEffectBase</typeparam>
public static class BuffDataFactory 
{
    public static BuffData CreateEffect<T>(int buff_id) where T : IBuffEffect, new()
    {
        if (BuffConfigs.TryGetBuffConfig(buff_id, out BuffConfig config))
        {
            BuffData data = new BuffData(config);
            data.effect_ = new T();
            return data;
        }
        Debug.LogWarning($"未找到Buff配置，BuffID: {buff_id}");
        return default;
    }
}



/// <summary>
/// 管理Buff配置
/// </summary>
public static class BuffConfigs
{
    /// <summary>
    /// Buff配置字典，键为Buff ID，值为对应的BuffData对象，所有BuffDataSO都在这里面。
    /// </summary>
    private static readonly Dictionary<int, BuffConfig>         buff_configs_ = new Dictionary<int, BuffConfig>();
    private const string                                        CONFIG_PATH = "BuffData/";

    static BuffConfigs()
    {
        BuffConfig[] configs = Resources.LoadAll<BuffConfig>(CONFIG_PATH);
        foreach (var item in configs)
        {
            buff_configs_.Add(item.buff_id_, item);
        }
    }


    public static bool TryGetBuffConfig(int buff_id, out BuffConfig config)
    {
        return buff_configs_.TryGetValue(buff_id, out config);
    }
}

/// <summary>
/// 管理 Buff 的组件，管理 Buff 的生命周期
/// </summary>
public class BuffComponent : MonoBehaviour, IBuffHelper
{
    /// <summary>
    /// 存储目标身上的Buff数据映射表，键为Buff ID，值为对应的BuffData对象
    /// </summary>
    private readonly Dictionary<int, BuffData>                                  target_buff_map_ = new();

    /// <summary>
    /// 存储阻挡类型Buff的协程字典，键为Buff ID，值为对应的协程对象
    /// </summary>
    private readonly Dictionary<int, Coroutine>                                 block_buff_coro_dic_ = new();

    /// <summary>
    /// 存储无法施加的Buff ID集合，用于记录被阻挡或禁用的Buff
    /// </summary>
    private readonly HashSet<int>                                               block_buffs_ = new();       // 无法施加的BuffID列表

    /// <summary>
    /// 当添加Buff层数时触发的回调事件
    /// </summary>
    public Action<BuffData>                                                     OnAddBuffLayer;

    /// <summary>
    /// 当移除Buff层数时触发的回调事件
    /// </summary>
    public Action<BuffData>                                                     OnRemoveBuffLayer;

    /// <summary>
    /// 当Buff数据完全移除时触发的回调事件
    /// </summary>
    public Action<BuffData>                                                     OnBuffDataRemoved;

    /// <summary>
    /// 当前活跃的Buff数量发生变化时触发的回调事件
    /// </summary>
    public Action                                                               OnActiveBuffCountChange;    //当前活跃的Buff数量变化时触发

    /// <summary>
    /// Buff特效的目标Transform组件，用于播放Buff相关的视觉效果
    /// </summary>
    public Transform                                                            buff_effect_target_;


    void Update()
    {
        List<BuffData> list = target_buff_map_.Values.ToList();

        foreach (var item in list)
        {
            item.effect_.Update();
        }
    }

    /// <summary>
    /// 获取正在作用的 Buff 个数
    /// </summary>
    /// <returns></returns>
    public int GetActiveBuffCount()
    {
        if (target_buff_map_ == null)
        {
            return 0;
        }
        return target_buff_map_.Count;
    }

    /// <summary>
    /// 根据id获取该组件拥有者的Buff
    /// </summary>
    /// <param name="buff_id"></param>
    /// <returns></returns>
    public bool TryGetBuffData(int buff_id, out BuffData data)
    {
        return target_buff_map_.TryGetValue(buff_id, out data);
    }

    /// <summary>
    /// 通过BuffID添加Buff
    /// </summary>
    public void AddBuff(int buff_id, float duration = -1, BuffComponent imposer = null, int count = 1)
    {
        /// 检查是否被阻挡
        if (block_buffs_.Contains(buff_id))
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            BuffHandle.AddBuff(this, buff_id, duration, imposer);
        }
    }

    /// <summary>
    /// 根据类型以及buffID创建buff
    /// </summary>
    /// <typeparam name="T">Buff类型</typeparam>
    /// <param name="imposer">施加者</param>
    /// <param name="buff_id"></param>
    /// <param name="duration">Buff持续时长</param>
    void IBuffHelper.AddBuff<T>(BuffComponent imposer, int buff_id, float duration)
    {
        imposer ??= this;

        /// duration 小于 0 则使用配置中的时长
        if (duration < 0)
        {
            if (BuffConfigs.TryGetBuffConfig(buff_id, out BuffConfig config))
            {
                duration = config.duration_;
            }
            else
            {
                duration = 0;
            }
        }

        /// 检查是否存在相同的 Buff
        if (TryGetBuffData(buff_id, out BuffData data))
        {
            /// 迭代 Buff
            BuffIteration(imposer, data, duration);
            return;
        }

        /// 找不到相同 Buff 则为新添加的 Buff
        AddNewBuffData<T>(imposer, buff_id, duration);
    }

    /// <summary>
    /// 创建新的Buff数据
    /// </summary>
    /// <typeparam name="T">Buff类型</typeparam>
    /// <param name="imposer">施加者</param>
    /// <param name="buff_id"></param>
    /// <param name="duration"></param>
    private void AddNewBuffData<T>(BuffComponent imposer, int buff_id, float duration) where T : IBuffEffect, new()
    {
        BuffData buff_data = BuffDataFactory.CreateEffect<T>(buff_id);

        if(buff_data == null)
        {
            Debug.LogError($"Buff ID {buff_id} 创建失败，无法添加Buff");
            return;
        }

        buff_data.duration_ = duration;
        buff_data.target_ = this;
        buff_data.imposer_ = imposer;
        buff_data.effect_.SetBuffData(buff_data);
        buff_data.effect_.Init();                   /// 初始化
        buff_data.effect_.Iteration();              /// 迭代一次

        target_buff_map_.Add(buff_id, buff_data);

        /// 执行回调，通知 UI 更新
        OnAddBuffLayer?.Invoke(buff_data);
        OnActiveBuffCountChange?.Invoke();
    }

    /// <summary>
    /// Buff迭代
    /// </summary>
    /// <param name="imposer">施加者</param>
    /// <param name="buff_data"></param>
    /// <param name="duration"></param>
    private void BuffIteration(BuffComponent imposer, BuffData buff_data, float duration)
    {
        buff_data.duration_ = duration;
        buff_data.target_ = this;
        buff_data.imposer_ = imposer;
        buff_data.effect_.SetBuffData(buff_data);
        buff_data.effect_.Iteration();

        /// 执行回调，通知 UI 更新
        OnAddBuffLayer?.Invoke(buff_data);
    }

    /// <summary>
    /// 移除Buff
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool RemoveBuff(BuffData data)
    {
        data.effect_.Release();
        target_buff_map_.Remove(data.buff_id_);
        OnBuffDataRemoved?.Invoke(data);
        OnActiveBuffCountChange?.Invoke();
        return true;
    }

    /// <summary>
    /// 根据 Buff ID 移除 Buff
    /// </summary>
    /// <param name="buff_id"></param>
    /// <returns></returns>
    public bool RemoveBuffByID(int buff_id)
    {
        if (TryGetBuffData(buff_id, out BuffData data))
        {
            return RemoveBuff(data);
        }
            
        return false;
    }

    /// <summary>
    /// 移除目标身上的所有Buff
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public void RemoveAllBuffs()
    {
        if (target_buff_map_ == null || target_buff_map_.Count == 0) return;
        List<BuffData> all_buff = target_buff_map_.Values.ToList();
        foreach (var buff_data in all_buff)
        {
            RemoveBuff(buff_data);
        }
    }

    /// <summary>
    /// 添加禁止施加的 BuffID
    /// </summary>
    /// <param name="buff_id"></param>
    public void AddBlockBuffID(int buff_id, float delay = 0)
    {
        if (block_buff_coro_dic_.Remove(buff_id, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
        }

        RemoveBuffByID(buff_id);
        block_buffs_.Add(buff_id);

        if (delay > 0)//delay大于0存在延时移除
        {
            block_buff_coro_dic_[buff_id] = StartCoroutine(RemoveBlockBuffDelay(buff_id, delay));
        }
    }

    /// <summary>
    /// 移除禁止施加的BuffID
    /// </summary>
    /// <param name="buff_id"></param>
    public void RemoveBlockBuffID(int buff_id)
    {
        block_buffs_.Remove(buff_id);

        if (block_buff_coro_dic_.Remove(buff_id, out var coroutine))
        {
            StopCoroutine(coroutine);//清理协程
        }
    }

    private IEnumerator RemoveBlockBuffDelay(int buff_id, float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        RemoveBlockBuffID(buff_id);
    }

    /// <summary>
    /// 清除所有受到的Buff效果和禁止施加的BuffID
    /// </summary>
    public void Clear()
    {
        StopAllCoroutines();
        RemoveAllBuffs();
        block_buffs_.Clear();
    }
}