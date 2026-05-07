using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 玩家相关调试工具
/// </summary>
[Debugable]
public class BuffSystemDebug
{
    [DebugProperty]
    public int                      test_buff_id_ = 10001;


    [DebugMethod("添加Buff", "Buff管理")]
    public void AddBuff()
    {
        TestEntity entity = TestEntity.Instance;
        if (!entity.TryGetComponent(out BuffComponent buff_comp))
        {
            return;
        }

        if(DebugPanel.Instance.TryGetVariable(nameof(test_buff_id_), out object variable))
        {
            BuffHandle.AddBuff(buff_comp, (int)variable);
        }
    }

    [DebugMethod("移除Buff", "Buff管理")]
    public void RemoveBuff()
    {
        TestEntity entity = TestEntity.Instance;
        if (!entity.TryGetComponent(out BuffComponent buff_comp))
        {
            return;
        }

        if(DebugPanel.Instance.TryGetVariable(nameof(test_buff_id_), out object variable))
        {
            buff_comp.RemoveBuffByID((int)variable);
        }
    }
}