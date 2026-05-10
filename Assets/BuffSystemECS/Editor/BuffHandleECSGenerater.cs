using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BuffSystemECS.Editor
{
    /// <summary>
    /// BuffHandleECS 自动生成器
    /// 扫描 BuffConfigProvider 注册的配置，生成 BuffHandleECS 便捷调用类
    /// </summary>
    public class BuffHandleECSGenerater
    {
        private const string FILE_PATH = "Assets/BuffSystemECS/Tool/BuffHandleECS.cs";

        [MenuItem("Tools/BuffSystemECS/生成BuffHandleECS脚本")]
        static void GenerateBuffHandleFile()
        {
            EnsureDirectoryExists();
            string code = GenerateCode();
            try
            {
                if (File.Exists(FILE_PATH)) File.Delete(FILE_PATH);
                File.WriteAllText(FILE_PATH, code);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BuffHandleECS 文件操作失败: {ex.Message}");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BuffHandleECS 脚本生成成功");
        }

        private static void EnsureDirectoryExists()
        {
            string dir = Path.GetDirectoryName(FILE_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static string GenerateCode()
        {
            var writer = new GeneraterHelper.Writer { buffer = new StringBuilder() };

            // 写入生成器信息
            writer.WriteLine("// 此文件由BuffHandleECSGenerater自动生成，请勿手动修改");
            writer.WriteLine("// 生成器文件路径: Assets/BuffSystemECS/Editor/BuffHandleECSGenerater.cs");
            writer.WriteLine();

            writer.WriteLine("using UnityEngine;");
            writer.WriteLine("using BuffSystemECS;");
            writer.WriteLine();

            writer.DocSummary("Buff 便捷调用类（ECS版本）");
            writer.DocSummary("提供静态方法，通过 buff_id 快速添加/移除 Buff，内部使用 BuffSystemECSManager");
            writer.WriteLine("public static class BuffHandleECS");
            writer.BeginBlock();

            // AddBuff 重载1: 通过 GameObject 添加
            writer.DocSummary("为目标对象添加Buff（使用默认配置的类型和时长）");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "Buff配置ID");
            writer.WriteLine("public static void AddBuff(GameObject target, int buff_id)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))");
            writer.BeginBlock();
            writer.WriteLine("BuffEffectType type = GetEffectTypeFromConfig(config);");
            writer.WriteLine("BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type);");
            writer.EndBlock();
            writer.WriteLine("else");
            writer.BeginBlock();
            writer.WriteLine("Debug.LogError($\"Buff ID {buff_id} 找不到对应配置\");");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // AddBuff 重载2: 指定时长
            writer.DocSummary("为目标对象添加Buff（指定时长）");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "Buff配置ID");
            writer.DocParam("duration", "Buff持续时长（秒）");
            writer.WriteLine("public static void AddBuff(GameObject target, int buff_id, float duration)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))");
            writer.BeginBlock();
            writer.WriteLine("BuffEffectType type = GetEffectTypeFromConfig(config);");
            writer.WriteLine("BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, duration);");
            writer.EndBlock();
            writer.WriteLine("else");
            writer.BeginBlock();
            writer.WriteLine("Debug.LogError($\"Buff ID {buff_id} 找不到对应配置\");");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // AddBuff 重载3: 指定施加者
            writer.DocSummary("为目标对象添加Buff（指定施加者）");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "Buff配置ID");
            writer.DocParam("imposer", "Buff施加者");
            writer.WriteLine("public static void AddBuff(GameObject target, int buff_id, GameObject imposer)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffConfigProvider.TryGetConfig(buff_id, out BuffConfig config))");
            writer.BeginBlock();
            writer.WriteLine("BuffEffectType type = GetEffectTypeFromConfig(config);");
            writer.WriteLine("BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, -1, imposer);");
            writer.EndBlock();
            writer.WriteLine("else");
            writer.BeginBlock();
            writer.WriteLine("Debug.LogError($\"Buff ID {buff_id} 找不到对应配置\");");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // AddBuff 重载4: 完整参数
            writer.DocSummary("为目标对象添加Buff（完整参数）");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "Buff配置ID");
            writer.DocParam("type", "Buff效果类型");
            writer.DocParam("duration", "Buff持续时长（秒，-1使用配置值）");
            writer.DocParam("imposer", "Buff施加者（可选）");
            writer.DocParam("effect_target", "特效挂载点（可选）");
            writer.WriteLine("public static void AddBuff(GameObject target, int buff_id, BuffEffectType type, float duration = -1, GameObject imposer = null, Transform effect_target = null)");
            writer.BeginBlock();
            writer.WriteLine("BuffSystemECSManager.Instance?.AddBuff(target, buff_id, type, duration, imposer, effect_target);");
            writer.EndBlock();

            writer.WriteLine();

            // RemoveBuff
            writer.DocSummary("移除目标对象上的指定Buff");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "要移除的Buff配置ID");
            writer.WriteLine("public static bool RemoveBuff(GameObject target, int buff_id)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))");
            writer.BeginBlock();
            writer.WriteLine("return BuffSystemECSManager.Instance.RemoveBuff(entity, buff_id);");
            writer.EndBlock();
            writer.WriteLine("return false;");
            writer.EndBlock();

            writer.WriteLine();

            // RemoveAllBuffs
            writer.DocSummary("移除目标对象上的所有Buff");
            writer.DocParam("target", "目标GameObject");
            writer.WriteLine("public static void RemoveAllBuffs(GameObject target)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))");
            writer.BeginBlock();
            writer.WriteLine("BuffSystemECSManager.Instance.RemoveAllBuffs(entity);");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // AddBlockBuff
            writer.DocSummary("禁止施加指定Buff");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "要禁止的Buff配置ID");
            writer.WriteLine("public static void AddBlockBuff(GameObject target, int buff_id)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))");
            writer.BeginBlock();
            writer.WriteLine("BuffSystemECSManager.Instance.AddBlockBuff(buff_id, entity);");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // RemoveBlockBuff
            writer.DocSummary("移除禁止施加指定Buff的限制");
            writer.DocParam("target", "目标GameObject");
            writer.DocParam("buff_id", "要解除禁止的Buff配置ID");
            writer.WriteLine("public static void RemoveBlockBuff(GameObject target, int buff_id)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))");
            writer.BeginBlock();
            writer.WriteLine("BuffSystemECSManager.Instance.RemoveBlockBuff(buff_id, entity);");
            writer.EndBlock();
            writer.EndBlock();

            writer.WriteLine();

            // GetActiveBuffCount
            writer.DocSummary("获取目标对象上活跃Buff的数量");
            writer.DocParam("target", "目标GameObject");
            writer.DocSummary("活跃Buff数量");
            writer.WriteLine("public static int GetActiveBuffCount(GameObject target)");
            writer.BeginBlock();
            writer.WriteLine("if (BuffSystemECSManager.Instance != null && BuffSystemECSManager.Instance.TryGetEntity(target, out BuffEntity entity))");
            writer.BeginBlock();
            writer.WriteLine("return BuffSystemECSManager.Instance.GetEntityBuffCount(entity);");
            writer.EndBlock();
            writer.WriteLine("return 0;");
            writer.EndBlock();

            writer.WriteLine();

            // 辅助方法：根据配置推断效果类型
            writer.WriteLine("private static BuffEffectType GetEffectTypeFromConfig(BuffConfig config)");
            writer.BeginBlock();
            writer.WriteLine("// 默认使用 Refreshable 类型，可通过配置扩展");
            writer.WriteLine("return BuffEffectType.Refreshable;");
            writer.EndBlock();

            writer.EndBlock(); // class

            return writer.ToString();
        }
    }
}