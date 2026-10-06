#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// 防止只修改文档却仍试玩、发布旧参数；运行时不依赖工程外的Markdown文件。
[InitializeOnLoad]
public sealed class 天帝数值同步检查 : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    static 天帝数值同步检查() => EditorApplication.playModeStateChanged += 检查运行;
    public static void 校验()
    {
        string 内容 = File.ReadAllText(Path.Combine(天帝构建工具.项目根, "游戏数值配置.md"), Encoding.UTF8).Replace("\r\n", "\n");
        var 匹配 = Regex.Match(内容, @"<!-- 数值配置开始 -->\s*```json\s*(.*?)\s*```\s*<!-- 数值配置结束 -->", RegexOptions.Singleline);
        if (!匹配.Success) throw new InvalidOperationException("数值配置缺少权威JSON块");
        using (var 算法 = SHA256.Create())
        {
            string 指纹 = string.Concat(算法.ComputeHash(Encoding.UTF8.GetBytes(匹配.Groups[1].Value.Trim())).Select(值 => 值.ToString("x2")));
            if (指纹 != 天帝数值配置.原文指纹) throw new InvalidOperationException("数值代码尚未同步：先运行 python 工具/战斗数值计算.py 和 python 工具/导出游戏数值.py，再刷新Unity。禁止继续使用旧参数。");
        }
    }
    static void 检查运行(PlayModeStateChange 状态)
    {
        if (状态 != PlayModeStateChange.ExitingEditMode) return;
        try { 校验(); }
        catch (Exception 异常) { EditorApplication.isPlaying = false; UnityEngine.Debug.LogError(异常.Message); }
    }
    public void OnPreprocessBuild(BuildReport 报告)
    { try { 校验(); } catch (Exception 异常) { throw new BuildFailedException(异常.Message); } }
}
#endif
