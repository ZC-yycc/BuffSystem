// 此文件由BuffHandleGenerater自动生成，请勿手动修改
// 生成器文件路径: Assets/BuffSystem/Editor/BuffHandleGenerater.cs


using UnityEngine;
public static class BuffHandle
{
    public static bool AddBuff(BuffComponent comp, int buff_id, float duration = -1, BuffComponent imposer = null)
    {
        IBuffHelper i_comp = comp;
        switch(buff_id)
        {
        	case 10001: 
        		i_comp.AddBuff<Buff10001>(imposer, buff_id, duration);
        		return true;
        	case 10002: 
        		i_comp.AddBuff<Buff10002>(imposer, buff_id, duration);
        		return true;
        	case 10003: 
        		i_comp.AddBuff<Buff10003>(imposer, buff_id, duration);
        		return true;
        	default:
        		Debug.LogError($"Buff ID {buff_id} 找不到对应 Buff");
        		return false;
        };
    }
}
