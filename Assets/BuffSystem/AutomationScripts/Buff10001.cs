using UnityEngine;

public class Buff10001 : IndependentEffectBase
{
    public override void Init()
    {
        Debug.Log("Buff10001 Init");
        base.Init();
    }

    public override void Iteration()
    {
        Debug.Log("Buff10001 Iteration");
        base.Iteration();
    }

    public override void Release()
    {
        Debug.Log("Buff10001 Release");
        base.Release();
    }

    public override void Update()
    {
        Debug.Log("Buff10001 Update");
        base.Update();
    }
}