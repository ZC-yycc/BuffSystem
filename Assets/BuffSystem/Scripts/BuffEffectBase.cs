using UnityEngine;


public interface IBuffEffect
{
    void SetBuffData(BuffData data);
    void Init();
    void Update();
    void Release();
    void Iteration();
}



/// <summary>
/// Buff效果基类，定义Buff的生命周期
/// </summary>
public class BuffEffectBase : IBuffEffect
{
    /// <summary>
    /// Buff数据，创建 Buff 时读取 SO 数据到 data_，记录该 Buff 的配置信息
    /// </summary>
    protected BuffData                              data_;

    /// <summary>
    /// Buff特效的缓存，在 Buff 结束时销毁
    /// </summary>
    private GameObject                              effect_prefab_temp_;

    /// <summary>
    /// Buff的位置信息
    /// </summary>
    private Transform                               effect_target_;


    /// <summary>
    /// 初始化，在 Buff 创建时由 BuffComponent 调用，迭代时不调用
    /// </summary>
    public virtual void Init()
    {
        if (data_.target_ == null)
        {
            return;
        }

        CreateEffect();
    }

    /// <summary>
    /// Buff 更新函数
    /// </summary>
    public virtual void Update() 
    {
        UpdateEffect();
    }

    /// <summary>
    /// Buff 结束时调用
    /// </summary>
    public virtual void Release() 
    {
        ReleaseEffect();
    }

    /// <summary>
    /// Buff 迭代时调用，例如可叠加的 Buff 在增加层数时会调用该函数
    /// </summary>
    public virtual void Iteration() { }


    /// <summary>
    /// 设置 Buff 数据
    /// </summary>
    /// <param name="data"></param>
    public void SetBuffData(BuffData data)
    {
        data_ = data;
    }


    /// <summary>
    /// 手动结束该 Buff
    /// </summary>
    public void Break()
    {
        if (data_.target_ == null)
        {
            return;
        }

        data_.target_.RemoveBuff(data_);
    }

    /// <summary>
    /// 创建新特效，并挂载到目标对象上
    /// </summary>
    private void CreateEffect()
    {
        if (data_.effect_prefab_ == null)
        {
            return;
        }

        if (data_.target_ == null)
        {
            Debug.LogWarning($"目标对象上未找到BuffComponent组件，无法创建Buff特效，BuffID: {data_.buff_id_}");
            return;
        }

        if (data_.target_.buff_effect_target_ == null)
        {
            Debug.LogWarning($"Buff特效挂载的对象未设置，BuffID: {data_.buff_id_}");
            return;
        }

        effect_target_ = data_.target_.buff_effect_target_;
        effect_prefab_temp_ = GameObject.Instantiate(data_.effect_prefab_);
        effect_prefab_temp_.transform.localScale = effect_target_.localScale;
        effect_prefab_temp_.transform.rotation = effect_target_.rotation;
    }


    /// <summary>
    /// 更新特效位置，不包含旋转和缩放
    /// </summary>
    private void UpdateEffect()
    {
        if (effect_target_ != null)
        {
            effect_prefab_temp_.transform.position = effect_target_.transform.position;
        }
    }


    /// <summary>
    /// 回收特效资源
    /// </summary>
    private void ReleaseEffect()
    {
        if (effect_prefab_temp_ != null)
        {
            GameObject.Destroy(effect_prefab_temp_);
        }

        effect_target_ = null;
    }
}