using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class GeneraterHelper
{
    // 类型缓存字典，避免重复反射
    private static readonly Dictionary<Type, List<Type>>            type_cache_ = new Dictionary<Type, List<Type>>();
    private static readonly Dictionary<Type, List<MethodInfo>>      method_cache_ = new Dictionary<Type, List<MethodInfo>>();

    #region Writer
    public struct Writer
    {
        public StringBuilder                            buffer;
        public int                                      indentLevel;
        private const int                               kSpacesPerIndentLevel = 4;
        public void BeginBlock()
        {
            WriteIndent();
            buffer.Append("{\n");
            ++indentLevel;
        }

        public void EndBlock()
        {
            --indentLevel;
            WriteIndent();
            buffer.Append("}\n");
        }

        public void WriteLine()
        {
            buffer.Append('\n');
        }

        public void WriteLine(string text)
        {
            if (!text.All(char.IsWhiteSpace))
            {
                WriteIndent();
                buffer.Append(text);
            }
            buffer.Append('\n');
        }

        public void Write(string text)
        {
            buffer.Append(text);
        }

        public void WriteIndent()
        {
            for (var i = 0; i < indentLevel; ++i)
            {
                for (var n = 0; n < kSpacesPerIndentLevel; ++n)
                    buffer.Append(' ');
            }
        }

        public void DocSummary(string text)
        {
            DocElement("summary", text);
        }

        public void DocParam(string paramName, string text)
        {
            WriteLine($"/// <param name=\"{paramName}\">{text}</param>");
        }

        public void DocRemarks(string text)
        {
            DocElement("remarks", text);
        }

        public void DocInherit(string cref)
        {
            DocReference("inheritdoc", cref);
        }

        public void DocSeeAlso(string cref)
        {
            DocReference("seealso", cref: cref);
        }

        public void DocExample(string code)
        {
            DocComment("<example>");
            DocComment("<code>");

            foreach (var line in code.Split('\n'))
                DocComment(line.Replace("<", "&lt;").Replace(">", "&gt;"));

            DocComment("</code>");
            DocComment("</example>");
        }

        private void DocComment(string text)
        {
            if (string.IsNullOrEmpty(text))
                WriteLine("///");
            else
                WriteLine(string.Concat("/// ", text));
        }

        private void DocElement(string tag, string text)
        {
            DocComment($"<{tag}>");
            DocComment(text);
            DocComment($"</{tag}>");
        }

        private void DocReference(string tag, string cref)
        {
            DocInlineElement(tag, "cref", cref);
        }

        private void DocInlineElement(string tag, string property, string value)
        {
            DocComment($"<{tag} {property}=\"{value}\" />");
        }

        /// <summary>
        /// 返回生成的字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return buffer.ToString();
        }
    }
    #endregion

    /// <summary>
    /// 查找所有标记了指定Attribute的类
    /// </summary>
    /// <typeparam name="T">Attribute类型</typeparam>
    /// <returns>标记了该Attribute的类的列表</returns>
    public static List<Type> FindTypesWithAttribute<T>() where T : Attribute
    {
        var attribute_type = typeof(T);

        // 检查缓存
        if (type_cache_.TryGetValue(attribute_type, out var value))
        {
            return new List<Type>(value);
        }

        var result = new List<Type>();

        // 获取所有已加载的程序集
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // 跳过Unity编辑器相关的程序集
            if (assembly.FullName.StartsWith("UnityEditor") ||
                assembly.FullName.StartsWith("UnityEngine") ||
                assembly.FullName.StartsWith("System"))
                continue;

            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // 检查是否标记了该Attribute
                    if (type.IsDefined(attribute_type, false))
                    {
                        result.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 某些类型可能加载失败，跳过无法加载的类型
                foreach (var type in ex.Types)
                {
                    if (type != null && type.IsDefined(attribute_type, false))
                    {
                        result.Add(type);
                    }
                }
            }
        }

        // 存入缓存
        type_cache_[attribute_type] = result;
        return new List<Type>(result);
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void ClearCache()
    {
        type_cache_.Clear();
        method_cache_.Clear();
    }

    /// <summary>
    /// 查找所有标记了指定Attribute的函数
    /// </summary>
    /// <typeparam name="T">Attribute类型</typeparam>
    /// <returns>标记了该Attribute的方法列表</returns>
    public static List<MethodInfo> FindMethodsWithAttribute<T>() where T : Attribute
    {
        var attribute_type = typeof(T);

        // 检查缓存
        if (method_cache_.TryGetValue(attribute_type, out var method_infos))
        {
            return new List<MethodInfo>(method_infos);
        }

        var result = new List<MethodInfo>();

        // 获取所有已加载的程序集
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (assembly.FullName.StartsWith("UnityEditor") ||
                assembly.FullName.StartsWith("UnityEngine"))
                continue;

            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // 获取该类型的所有方法（公共、实例、静态）
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.Static);

                    foreach (var method in methods)
                    {
                        if (method.IsDefined(attribute_type, false))
                        {
                            result.Add(method);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var type in ex.Types)
                {
                    if (type == null) continue;

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        if (method != null && method.IsDefined(attribute_type, false))
                        {
                            result.Add(method);
                        }
                    }
                }
            }
        }

        // 存入缓存
        method_cache_[attribute_type] = result;
        return new List<MethodInfo>(result);
    }

    /// <summary>
    /// 在指定类型中查找标记了Attribute的方法
    /// </summary>
    public static List<MethodInfo> FindMethodsWithAttributeInType<T>(Type target_type) where T : Attribute
    {
        var attribute_type = typeof(T);
        var result = new List<MethodInfo>();

        var methods = target_type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                            BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.IsDefined(attribute_type, false))
            {
                result.Add(method);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取所有直接或间接继承自 T 的叶子类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<Type> GetLeafTypes<T>() where T : class
    {
        // 获取所有已加载的程序集
        Assembly[] assemblie_arr = AppDomain.CurrentDomain.GetAssemblies();
        var base_type = typeof(T);
        var all_type_set = new HashSet<Type>();
        var leaf_type_list = new List<Type>();

        foreach (var assembly in assemblie_arr)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract &&
                        base_type.IsAssignableFrom(type))
                    {
                        all_type_set.Add(type);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略无法加载的类型
                continue;
            }
        }

        // 找出叶子类型（没有子类的类型）
        foreach (var type in all_type_set)
        {
            bool is_leaf = true;

            foreach (var item in all_type_set)
            {
                if (type != item && type.IsAssignableFrom(item))
                {
                    is_leaf = false;
                    break;
                }
            }

            if (is_leaf)
            {
                leaf_type_list.Add(type);
            }
        }

        return leaf_type_list;
    }

    /// <summary>
    /// 获取脚本文件相对路径路径
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string GetScriptPath<T>() where T : class
    {
        // 使用t:Script过滤器查找所有脚本
        string class_name = typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets("t:Script " + class_name);

        if (guids.Length == 0)
        {
            Debug.LogWarning($"未找到类名为 '{class_name}' 的脚本文件。");
            return null;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning($"找到多个名为 '{class_name}' 的脚本，请使用更精确的类名。");
            // 可以在这里记录所有找到的路径，帮助用户区分
            foreach (var item in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(item);
                Debug.LogWarning($"同名文件：{path}");
            }
            return null;
        }

        // 找到唯一文件，返回路径
        string script_path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return script_path;
    }

    /// <summary>
    /// 获取脚本所在的文件夹绝对路径
    /// </summary>
    public static string GetScriptDirectoryPath<T>(string class_name) where T : class
    {
        string script_path = GetScriptPath<T>();
        if (string.IsNullOrEmpty(script_path)) return null;

        // 使用Path.GetDirectoryName获取目录路径
        return Path.GetDirectoryName(script_path);
    }

    /// <summary>
    /// 写入生成器信息头注释
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="writer"></param>
    public static void WriterGeneraterInfo<T>(ref Writer writer) where T : class
    {
        writer.WriteLine($"// 此文件由{typeof(T).Name}自动生成，请勿手动修改");
        writer.WriteLine($"// 生成器文件路径: {GetScriptPath<T>()}");
        writer.WriteLine("\n");
    }

    /// <summary>
    /// 获取指定枚举类型的所有名称数组
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns>枚举名称列表</returns>
    public static string[] GetEnumNameArray<T>() where T : Enum
    {
        return Enum.GetNames(typeof(T));
    }
    public static string[] GetEnumNameArray(Type enum_type)
    {
        return Enum.GetNames(enum_type);
    }

    /// <summary>
    /// 获取枚举值的InspectorName
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetEnumInspectorName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attributes = field.GetCustomAttributes(typeof(InspectorNameAttribute), false);

        if (attributes.Length > 0)
            return ((InspectorNameAttribute)attributes[0]).displayName;

        return value.ToString();
    }

    /// <summary>
    /// 尝试将字符串转换为指定类型的枚举值
    /// </summary>
    /// <param name="enum_type_name">枚举类型名</param>
    /// <param name="enum_value_string">枚举值名称</param>
    /// <param name="enum_value">返回的枚举值</param>
    /// <param name="ignore_case">是否忽略大小写</param>
    /// <returns>转换成功或失败</returns>
    public static bool TryConvertStringToEnum(string enum_type_name, string enum_value_string, out object enum_value, bool ignore_case = true)
    {
        enum_value = null;
        Type enum_type = Type.GetType($"{enum_type_name}, Assembly-CSharp");
        if (enum_type == null)
            return false;

        if (!Enum.TryParse(enum_type, enum_value_string, ignore_case, out object result))
            return false;

        enum_value = result;
        return true;
    }

    /// <summary>
    /// 加载指定文件夹路径下的所有预制体，可通过正则表达式匹配名称过滤，预制体将添加到 prefab_list 中，不会清空原有内容
    /// </summary>
    /// <param name="folder_path">文件夹路径</param>
    /// <param name="prefab_list">返回的预制体列表</param>
    /// <param name="match_str">正则表达式</param>
    public static void LoadAllPrefabsInFolder(string folder_path, ref List<GameObject> prefab_list, string match_str = null)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder_path });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            if(match_str != null)
            {
                Match match = Regex.Match(prefab.name, match_str); 
                if (!match.Success) continue;
            }

            prefab_list.Add(prefab);
        }
    }

    /// <summary>
    /// 获取所有继承自指定泛型类型的所有类型
    /// </summary>
    /// <param name="generic_type"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static List<Type> FindAllConstructedTypes(Type generic_type)
    {
        var main_assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
        if (main_assembly == null)
            throw new Exception("δ�ҵ������� Assembly-CSharp");

        var result = new List<Type>();
        foreach (var type in main_assembly.GetTypes())
        {

            if (type.IsGenericType && type.GetGenericTypeDefinition() == generic_type && !type.IsGenericTypeDefinition)
            {
                result.Add(type);
            }

            var base_type = type.BaseType;
            if (base_type != null && base_type.IsGenericType && base_type.GetGenericTypeDefinition() == generic_type)
            {
                result.Add(base_type);
            }
        }
        return result;
    }
    public static void Flag(string key)
    {
        PlayerPrefs.SetInt(key, 1);                  //标记需要生成对象池配置
        PlayerPrefs.Save();
    }
    public static bool CheckFlag(string key)
    {
        if (PlayerPrefs.GetInt(key, 0) != 1)  //检查标记
            return false;
        return true;
    }
    public static void ClearFlag(string key)
    {
        PlayerPrefs.DeleteKey(key);                  //清除标记
        PlayerPrefs.Save();
    }
}
