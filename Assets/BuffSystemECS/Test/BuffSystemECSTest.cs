using System.Text;
using BuffSystemECS;
using UnityEngine;

/// <summary>
/// BuffSystemECS 测试脚本 —— 测试三种 Buff 类型的基础行为
/// 使用方式：挂载到场景中任意 GameObject 上，使用 OnGUI 绘制调试面板
/// 需要场景中已有 BuffSystemECSManager 单例物体
/// </summary>
public class BuffSystemECSTest : MonoBehaviour
{
    [Header("调试显示")]
    [SerializeField] private int                        max_log_lines_ = 15;
    [SerializeField] private int                        gui_font_size_ = 14;

    [Header("测试目标")]
    [SerializeField] private GameObject                 test_target_;   // 受 Buff 的测试目标（可为自身）

    [Header("测试参数")]
    [SerializeField] private int                        refresh_buff_id_ = 10003;  // 不可叠加（Refreshable）
    [SerializeField] private int                        independent_buff_id_ = 10001; // 数值叠加（Independent）
    [SerializeField] private int                        stackable_buff_id_ = 10002;  // 独立计时（Stackable）

    private readonly StringBuilder                      log_builder_ = new StringBuilder();
    private bool                                        show_gui_ = true;

    // ──────────────────────────── Unity 生命周期 ────────────────────────────

    private void Start()
    {
        // 如未指定测试目标，使用自身
        if (test_target_ == null)
            test_target_ = gameObject;

        // 注册 BuffSystemECSManager 的所有事件
        var mgr = BuffSystemECSManager.Instance;
        if (mgr != null)
        {
            mgr.OnAddBuffLayer += OnAddBuffLayer;
            mgr.OnRemoveBuffLayer += OnRemoveBuffLayer;
            mgr.OnBuffDataRemoved += OnBuffRemoved;
            mgr.OnActiveBuffCountChange += OnBuffCountChanged;
            mgr.OnLayerEffectTrigger += OnLayerEffectTrigger;
        }

        Log("=== BuffSystemECS 测试启动 ===");
        Log("按键说明：");
        Log("  1 - 添加 Refreshable Buff（不可叠加）");
        Log("  2 - 添加 Independent Buff（数值叠加）");
        Log("  3 - 添加 Stackable Buff（独立计时）");
        Log("  R - 移除最后添加类型的 Buff");
        Log("  A - 移除所有 Buff");
        Log("  C - 清除日志");
        Log("===============================");
    }

    private void Update()
    {
        HandleInput();
    }

    private void OnGUI()
    {
        if (!show_gui_) return;

        GUIStyle box_style = new GUIStyle(GUI.skin.box);
        box_style.fontSize = gui_font_size_;
        box_style.alignment = TextAnchor.UpperLeft;

        GUIStyle label_style = new GUIStyle(GUI.skin.label);
        label_style.fontSize = gui_font_size_;

        // 左上角面板：按键提示
        float panel_x = 10;
        float panel_y = 10;
        float panel_w = 500;
        float panel_h = 200;
        GUI.Box(new Rect(panel_x, panel_y, panel_w, panel_h), "", box_style);
        GUI.Label(new Rect(panel_x + 10, panel_y + 5, panel_w - 20, 40),
            "=== BuffSystemECS 测试面板 (H 隐藏/显示) ===", label_style);
        GUI.Label(new Rect(panel_x + 10, panel_y + 25, panel_w - 20, 160),
            "1 - 添加 Refreshable  Buff（不可叠加）\n" +
            "2 - 添加 Independent   Buff（数值叠加+时间刷新）\n" +
            "3 - 添加 Stackable     Buff（每层独立计时）\n" +
            "R - 移除最后添加类型的 Buff\n" +
            "A - 移除所有 Buff\n" +
            "C - 清除日志显示", label_style);

        // 右侧 Buff 实时状态
        var mgr = BuffSystemECSManager.Instance;
        if (mgr != null && mgr.TryGetEntity(test_target_, out BuffEntity entity))
        {
            float info_x = panel_x + panel_w + 10;
            float info_y = panel_y;
            float info_w = 350;
            float info_h = 200;

            var buffs = mgr.World.EntityManager.GetEntityBuffs(entity);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"活跃 Buff 数量: {mgr.GetEntityBuffCount(entity)}");
            if (buffs != null)
            {
                foreach (var b in buffs)
                {
                    sb.AppendLine($"  ID={b.buff_id}  Name={b.buff_name}  Type={b.effect_type}  Count={b.count}/{b.max_count}  Duration={b.duration:F1}s");
                }
            }

            GUI.Box(new Rect(info_x, info_y, info_w, info_h), "", box_style);
            GUI.Label(new Rect(info_x + 10, info_y + 5, info_w - 20, info_h - 10), sb.ToString(), label_style);
        }

        // 底部日志面板
        float log_x = 10;
        float log_y = panel_y + panel_h + 10;
        float log_w = Screen.width - 20;
        float log_h = Screen.height - log_y - 10;
        GUI.Box(new Rect(log_x, log_y, log_w, log_h), "", box_style);
        GUI.Label(new Rect(log_x + 10, log_y + 5, log_w - 20, log_h - 10), log_builder_.ToString(), label_style);
    }

    private void OnDestroy()
    {
        var mgr = BuffSystemECSManager.Instance;
        if (mgr != null)
        {
            mgr.OnAddBuffLayer -= OnAddBuffLayer;
            mgr.OnRemoveBuffLayer -= OnRemoveBuffLayer;
            mgr.OnBuffDataRemoved -= OnBuffRemoved;
            mgr.OnActiveBuffCountChange -= OnBuffCountChanged;
            mgr.OnLayerEffectTrigger -= OnLayerEffectTrigger;
        }
    }

    // ──────────────────────────── 输入处理 ────────────────────────────

    private BuffEffectType last_added_type_ = BuffEffectType.Refreshable;

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            show_gui_ = !show_gui_;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            last_added_type_ = BuffEffectType.Refreshable;
            BuffSystemECSManager.Instance?.AddBuff(test_target_, refresh_buff_id_, BuffEffectType.Refreshable);
            Log($"<color=cyan>[输入]</color> 添加 Refreshable Buff (ID={refresh_buff_id_})");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            last_added_type_ = BuffEffectType.Independent;
            BuffSystemECSManager.Instance?.AddBuff(test_target_, independent_buff_id_, BuffEffectType.Independent);
            Log($"<color=yellow>[输入]</color> 添加 Independent Buff (ID={independent_buff_id_})");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            last_added_type_ = BuffEffectType.Stackable;
            BuffSystemECSManager.Instance?.AddBuff(test_target_, stackable_buff_id_, BuffEffectType.Stackable);
            Log($"<color=green>[输入]</color> 添加 Stackable Buff (ID={stackable_buff_id_})");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            int id_to_remove = GetBuffIdByType(last_added_type_);
            var mgr = BuffSystemECSManager.Instance;
            if (mgr != null && mgr.TryGetEntity(test_target_, out BuffEntity entity))
            {
                mgr.RemoveBuff(entity, id_to_remove);
                Log($"<color=red>[输入]</color> 移除 Buff (ID={id_to_remove}, Type={last_added_type_})");
            }
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            var mgr = BuffSystemECSManager.Instance;
            if (mgr != null && mgr.TryGetEntity(test_target_, out BuffEntity entity))
            {
                mgr.RemoveAllBuffs(entity);
                Log("<color=red>[输入]</color> 移除所有 Buff");
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            log_builder_.Clear();
            Debug.Log("[BuffTest] 日志已清除");
        }
    }

    private int GetBuffIdByType(BuffEffectType type)
    {
        return type switch
        {
            BuffEffectType.Refreshable => refresh_buff_id_,
            BuffEffectType.Independent => independent_buff_id_,
            BuffEffectType.Stackable => stackable_buff_id_,
            _ => refresh_buff_id_
        };
    }

    // ──────────────────────────── 事件回调 ────────────────────────────

    private void OnAddBuffLayer(BuffRuntimeData buff)
    {
        Log($"<color=cyan>[事件]</color> Buff 层数增加: ID={buff.buff_id}, Name={buff.buff_name}, Count={buff.count}, Type={buff.effect_type}");
    }

    private void OnRemoveBuffLayer(BuffRuntimeData buff)
    {
        Log($"<color=orange>[事件]</color> Buff 层数减少: ID={buff.buff_id}, Name={buff.buff_name}, Count={buff.count}, Type={buff.effect_type}");
    }

    private void OnBuffRemoved(BuffRuntimeData buff)
    {
        Log($"<color=red>[事件]</color> Buff 完全移除: ID={buff.buff_id}, Name={buff.buff_name}, Type={buff.effect_type}");
    }

    private void OnBuffCountChanged(BuffEntity entity)
    {
        var mgr = BuffSystemECSManager.Instance;
        int total = mgr != null ? mgr.GetEntityBuffCount(entity) : 0;
        Log($"<color=magenta>[事件]</color> 活跃 Buff 数量变化: {total}");
    }

    private void OnLayerEffectTrigger(BuffRuntimeData buff, int layerIndex)
    {
        Log($"<color=green>[事件]</color> 层效果触发: ID={buff.buff_id}, Layer={layerIndex}, Type={buff.effect_type}");
    }

    // ──────────────────────────── 日志工具 ────────────────────────────

    private void Log(string message)
    {
        string line = $"[{Time.time:F2}] {message}";
        log_builder_.AppendLine(line);
        Debug.Log($"[BuffTest] {message}");

        // 限制最大行数
        string full = log_builder_.ToString();
        string[] lines = full.Split('\n');
        if (lines.Length > max_log_lines_)
        {
            log_builder_.Clear();
            for (int i = lines.Length - max_log_lines_; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    log_builder_.AppendLine(lines[i]);
            }
        }
    }
}