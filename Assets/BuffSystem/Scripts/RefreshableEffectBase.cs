using UnityEngine;


/// <summary>
/// 不可叠加的 Buff 基类，只拥有一层 Buff，多次施加同一 Buff 只是刷新时间
/// </summary>
public class RefreshableEffectBase : BuffEffectBase
{
    public override void Init()
    {
        base.Init();
        data_.Count = 1;
    }

    public override void Update()
    {
        base.Update();
        data_.duration_ -= Time.deltaTime;
        if (data_.duration_ <= 0)
        {
            Break();
        }
    }

    public override void Release()
    {
        data_.Count = 0;
        base.Release();
    }
}