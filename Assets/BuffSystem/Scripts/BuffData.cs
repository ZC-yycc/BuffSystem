using UnityEngine;


/// <summary>
/// Buff运行时数据，描述Buff，BuffID、Buff名称等。从SO中读取
/// </summary>
public class BuffData
{
    /// <summary>
    /// BuffID, 唯一标识符
    /// </summary>
    public int                          buff_id_;

    /// <summary>
    /// Buff名称
    /// </summary>
    public string                       name_;

    /// <summary>
    /// Buff图标
    /// </summary>
    public Sprite                       icon_;

    /// <summary>
    /// Buff描述
    /// </summary>
    public string                       detail_;

    /// <summary>
    /// Buff持续时间
    /// </summary>
    public float                        duration_;

    /// <summary>
    /// Buff层数
    /// </summary>
    private int                         count_;

    /// <summary>
    /// Buff层数上限
    /// </summary>
    private int                         max_count_;

    /// <summary>
    /// Buff值
    /// </summary>
    private float                       value_;

    /// <summary>
    /// Buff执行的逻辑
    /// </summary>
    public IBuffEffect                  effect_;

    /// <summary>
    /// BUFF在哪个对象身上
    /// </summary>
    public BuffComponent                target_;

    /// <summary>
    /// Buff的施加者
    /// </summary>
    public BuffComponent                imposer_;

    /// <summary>
    /// Buff特效的预制体
    /// </summary>
    public GameObject                   effect_prefab_;


    /// <summary>
    /// Buff层数是否已满
    /// </summary>
    public bool IsFull => count_ >= max_count_;

    /// <summary>
    /// Buff值
    /// </summary>
    public float Value { get { return value_; } set { value_ = value; } }

    /// <summary>
    /// Buff层数
    /// </summary>
    public int Count { get { return count_; } set { count_ = Mathf.Clamp(value, 0, max_count_); } }




    public BuffData() { }
    public BuffData(BuffConfig buff_data_so)
    {
        buff_id_ = buff_data_so.buff_id_;
        name_ = buff_data_so.name_;
        icon_ = buff_data_so.icon_;
        detail_ = buff_data_so.buff_description_;
        duration_ = buff_data_so.duration_;
        max_count_ = buff_data_so.stack_count_;
        value_ = buff_data_so.value_;
        effect_prefab_ = buff_data_so.effect_prefab_;
    }
}
