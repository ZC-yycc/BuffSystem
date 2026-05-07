using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BuffHandleGenerater
{
    private static readonly                     Regex extract_number_regex_ = new(@"\d+");                              // 提取所有连续数字
    private const string                        FILE_PATH = "Assets/BuffSystem/Tool/BuffHandle.cs";
    private const string                        FOLDER_PATH = "Assets/BuffSystem/AutomationScripts";                  // 默认文件夹路径


    [MenuItem("Tools/生成BuffHandle脚本")]
    static void GenerateBuffToolFile() 
    {
        string code = GenerateCode();
        try
        {
            if (File.Exists(FILE_PATH)) File.Delete(FILE_PATH);
            File.WriteAllText(FILE_PATH, code);
        }
        catch (Exception ex)
        {
            Debug.LogError($"文件操作失败: {ex.Message}");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("文件创建成功");
    }
    private static string GenerateCode()
    {
        string[] script_files = Directory.GetFiles(FOLDER_PATH, "*.cs", SearchOption.AllDirectories);

        string[] script_names = new string[script_files.Length];

        for(int i = 0; i < script_files.Length; ++i)
        {
            script_names[i] = Path.GetFileNameWithoutExtension(script_files[i]);
            Debug.Log($"脚本名称: {script_names[i]}");
        }
        var writer = new GeneraterHelper.Writer { buffer = new StringBuilder() };
        GeneraterHelper.WriterGeneraterInfo<BuffHandleGenerater>(ref writer);
        writer.WriteLine("\n");
        writer.WriteLine("using UnityEngine;");
        writer.WriteLine($"public static class BuffHandle");
        writer.BeginBlock();
        writer.WriteLine($"public static bool AddBuff({nameof(BuffComponent)} comp, int buff_id, float duration = -1, {nameof(BuffComponent)} imposer = null)");
        writer.BeginBlock();
        writer.WriteLine($"{nameof(IBuffHelper)} i_comp = comp;");
        writer.WriteLine($"switch(buff_id)");
        writer.WriteLine("{");
        foreach (string item in script_names)
        {
            string id = extract_number_regex_.Match(item).Value;
            if (item.Equals("Null")) continue;
            writer.WriteLine($"\tcase {id}: ");
            writer.WriteLine($"\t\ti_comp.AddBuff<{item}>(imposer, buff_id, duration);");
            writer.WriteLine($"\t\treturn true;");
        }
        writer.WriteLine($"\tdefault:");
        writer.WriteLine($"\t\tDebug.LogError($\"Buff ID {{buff_id}} 找不到对应 Buff\");");
        writer.WriteLine($"\t\treturn false;");
        writer.WriteLine("};");
        writer.EndBlock();
        writer.EndBlock();
        return writer.ToString();
    }
}
