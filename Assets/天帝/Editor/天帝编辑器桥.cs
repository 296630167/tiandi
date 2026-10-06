#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class 天帝编辑器桥
{
    [Serializable] public class 编辑请求 { public string 编号; public string 方法; public string 参数; }
    [Serializable] public class 编辑回报 { public string 编号; public bool 成功; public string 消息; public bool 编译中; public bool 运行中; }
    static double 下次检查;
    static double 下次刷新;
    static bool 执行中;
    static string 根 => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static 天帝编辑器桥() { EditorApplication.update += 更新; EditorApplication.delayCall += 心跳; }
    static void 心跳()
    {
        Directory.CreateDirectory(Path.Combine(根, "生成", "验证"));
        File.WriteAllText(Path.Combine(根, "生成", "验证", "editor-ready.json"), JsonUtility.ToJson(new 编辑回报 { 成功 = true, 消息 = Application.unityVersion, 编译中 = EditorApplication.isCompiling, 运行中 = EditorApplication.isPlaying }));
    }
    static void 更新()
    {
        if (执行中 || EditorApplication.timeSinceStartup < 下次检查) return;
        下次检查 = EditorApplication.timeSinceStartup + 0.5;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string 请求路径 = Path.Combine(根, "Temp", "天帝请求.json");
        if (File.Exists(请求路径))
        {
            执行中 = true;
            编辑请求 请求 = null;
            try
            {
                请求 = JsonUtility.FromJson<编辑请求>(File.ReadAllText(请求路径)); File.Delete(请求路径);
                string 消息;
                if (请求.方法 == "刷新") { AssetDatabase.Refresh(); 消息 = "已请求刷新"; }
                else if (请求.方法 == "停止") { EditorApplication.isPlaying = false; 消息 = "停止运行"; }
                else if (请求.方法 == "运行") { EditorApplication.isPlaying = true; 消息 = "开始运行"; }
                else
                {
                    Type 类型 = null;
                    foreach (var 程序集 in AppDomain.CurrentDomain.GetAssemblies()) { 类型 = 程序集.GetType("天帝构建工具"); if (类型 != null) break; }
                    if (类型 == null) throw new Exception("构建工具尚未编译");
                    var 方法 = 类型.GetMethod(请求.方法, BindingFlags.Static | BindingFlags.Public);
                    if (方法 == null) throw new Exception("未知方法：" + 请求.方法);
                    var 返回 = 方法.GetParameters().Length == 0 ? 方法.Invoke(null, null) : 方法.Invoke(null, new object[] { 请求.参数 });
                    消息 = 返回?.ToString() ?? "完成";
                }
                写回(请求, true, 消息);
            }
            catch (Exception 异常) { 写回(请求, false, 异常.ToString()); Debug.LogException(异常); }
            finally { 执行中 = false; }
        }
        if (!EditorApplication.isPlaying && EditorApplication.timeSinceStartup > 下次刷新)
        { 下次刷新 = EditorApplication.timeSinceStartup + 5; AssetDatabase.Refresh(); 心跳(); }
    }
    static void 写回(编辑请求 请求, bool 成功, string 消息)
    { if (请求 == null) return; Directory.CreateDirectory(Path.Combine(根, "生成", "验证")); File.WriteAllText(Path.Combine(根, "生成", "验证", "response-" + 请求.编号 + ".json"), JsonUtility.ToJson(new 编辑回报 { 编号 = 请求.编号, 成功 = 成功, 消息 = 消息, 编译中 = EditorApplication.isCompiling, 运行中 = EditorApplication.isPlaying }, true)); }
}
#endif
