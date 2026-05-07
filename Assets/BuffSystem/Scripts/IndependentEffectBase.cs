using UnityEngine;

/// <summary>
/// 数值叠加并且刷新时间（相同ID的Buff只存在一层，持续时间刷新，叠加数值）Buff
/// </summary>
public class IndependentEffectBase : BuffEffectBase
{
    public override void Iteration()
    {
        base.Iteration();
        data_.Count++;
    }
    public override void Update()
    {
        base.Update();
        data_.duration_ -= Time.deltaTime;
        if (data_.duration_ <= 0)
            Break();
    }

    public override void Release() 
    {
        data_.Count = 0;
        base.Release();
    }
}