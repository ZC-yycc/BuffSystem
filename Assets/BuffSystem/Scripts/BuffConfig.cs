using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffData", menuName = "BuffData/CreateBuffData")]
public class BuffConfig : ScriptableObject
{
    public int                                      buff_id_;
    public string                                   name_;
    public Sprite                                   icon_;
    public float                                    duration_;
    public float                                    value_;         // 效果值（治疗量/攻击力增幅等）
    public int                                      stack_count_ = 1;
    public GameObject                               effect_prefab_; // 特效预制体
    [TextArea(3, 10)]
    public string                                   buff_description_;
}