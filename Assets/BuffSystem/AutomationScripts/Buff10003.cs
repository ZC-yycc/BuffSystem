using UnityEngine;

public class Buff10003 : RefreshableEffectBase
{
    public override void Init()
    {
        Debug.Log("Buff10003 Init");
        base.Init();
    }

    public override void Iteration()
    {
        Debug.Log("Buff10003 Iteration");
        base.Iteration();
    }

    public override void Release()
    {
        Debug.Log("Buff10003 Release");
        base.Release();
    }

    public override void Update()
    {
        Debug.Log("Buff10003 Update");
        base.Update();
    }
}
