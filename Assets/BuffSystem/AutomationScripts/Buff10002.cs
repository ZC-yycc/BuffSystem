using UnityEngine;

public class Buff10002 : StackableEffectBase
{
    public override void Init()
    {
        Debug.Log("Buff10002 Init");
        base.Init();
    }

    public override void Iteration()
    {
        Debug.Log("Buff10002 Iteration");
        base.Iteration(); 
    }

    public override void Release()
    {
        Debug.Log("Buff10002 Release");
        base.Release();
    }

    public override void Update()
    {
        Debug.Log("Buff10002 Update");
        base.Update();
    }

    protected override void OnAddLayer()
    {
        Debug.Log("Buff10002 OnAddLayer");
        base.OnAddLayer();
    }

    protected override void OnLayerEffectTrigger()
    {
        Debug.Log("Buff10002 OnLayerEffectTrigger");
        base.OnLayerEffectTrigger();
    }

    protected override void OnRemoveLayer(int index)
    {
        Debug.Log("Buff10002 OnRemoveLayer");
        base.OnRemoveLayer(index);
    }
}
