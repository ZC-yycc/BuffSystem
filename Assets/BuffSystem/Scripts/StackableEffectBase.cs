using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 独立叠加（可同时存在多个相同ID的Buff，各自互不影响触发）Buff
/// </summary>
public class StackableEffectBase : BuffEffectBase
{
    /// <summary>
    /// 单层BUFF数据
    /// </summary>
    protected struct LayerData
    {
        public float last_trigger_time;     // 触发计时器
        public float layer_duration;        // 剩余时间计时器
    }

    protected float                             interval_ = 1.0f;                   // 持续触发间隔
    protected List<LayerData>                   layer_datas_ = new();               // 各独立层的数据

    /// <summary>
    /// Buff在组件中创建时进行初始化，只调用一次不响应多次附加Buff，用于初始化数据
    /// </summary>
    public override void Init()
    {
        base.Init();
    }

    /// <summary>
    /// Buff更新，非独立的每层Buff的更新
    /// </summary>
    public override void Update()
    {
        base.Update();

        UpdateLayer();

        if (layer_datas_.Count == 0)
        {
            Break();
            return;
        }
        if (data_.target_ == null)
        {
            Break();
            return;
        }
    }
    /// <summary>
    /// Buff迭代，响应多次附加Buff，用于叠加层数等，每次附加都会调用
    /// </summary>
    public override void Iteration()
    {
        OnAddLayer();
    }
    /// <summary>
    /// Buff释放，Buff被移除时调用，只调用一次不响应多次移除Buff，用于清理数据，Buff中独立层的移除不会调用该函数
    /// </summary>
    public override void Release()
    {
        base.Release();
        layer_datas_.Clear();
        data_.Count = 0;
    }

    /// <summary>
    /// 添加Buff独立层，每次附加Buff都会调用
    /// </summary>
    protected virtual void OnAddLayer()
    {
        if (data_.IsFull)
        {
            layer_datas_.RemoveAt(0);
        }
        LayerData new_layer_data = new LayerData
        {
            last_trigger_time = Time.time,
            layer_duration = data_.duration_
        };
        layer_datas_.Add(new_layer_data);
        data_.Count++;
    }
    /// <summary>
    /// Buff独立层计时更新
    /// </summary>
    public void UpdateLayer()
    {
        if (layer_datas_.Count == 0) return;        // 个数为0，不执行
        for (int i = layer_datas_.Count - 1; i >= 0; --i)
        {
            if (layer_datas_.Count <= i) 
                continue;

            LayerData layer_data = layer_datas_[i];
            layer_data.layer_duration -= Time.deltaTime;
            layer_datas_[i] = layer_data;
            if (layer_data.layer_duration < 0)
                OnRemoveLayer(i);
        }

        float time = Time.time;
        for (int i = layer_datas_.Count - 1; i >= 0; --i)
        {
            if (layer_datas_.Count <= i)
                continue;
            
            LayerData layer_data = layer_datas_[i];
            if (time - layer_data.last_trigger_time < interval_) 
                continue;

            layer_data.last_trigger_time = time;
            layer_datas_[i] = layer_data;
            OnLayerEffectTrigger();
        }
    }
    /// <summary>
    /// Buff独立层移除时调用
    /// </summary>
    /// <param name="index"></param>
    protected virtual void OnRemoveLayer(int index)
    {
        layer_datas_.RemoveAt(index);
        data_.Count--;
        if(data_.target_ != null)
        {
            data_.target_.OnRemoveBuffLayer?.Invoke(data_);
        }
    }
    /// <summary>
    /// Buff独立层持续效果，每个独立层在间隔时间到达时调用
    /// </summary>
    protected virtual void OnLayerEffectTrigger() { }
}