#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class 天帝移动打包
{
    const string 原平台键 = "Tiandi.MobileBuild.OriginalTarget";
    static string 目录 => Path.Combine(天帝构建工具.项目根, Application.isBatchMode ? "MobileBuild" : "移动端打包");
    [Serializable] sealed class 打包报告
    {
        public string APK, 结果, 包名, 版本, 架构 = "ARM64", 签名 = "Android调试签名";
        public int 错误数, 警告数;
        public ulong 字节数;
        public double 耗时秒;
        public string[] 场景, 构建消息;
    }
    public static string 准备()
    {
        检查空闲();
        检查英文路径();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("当前Unity未安装Android构建模块。");
        Directory.CreateDirectory(目录);
        if (SessionState.GetInt(原平台键, int.MinValue) == int.MinValue)
            SessionState.SetInt(原平台键, (int)EditorUserBuildSettings.activeBuildTarget);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("切换Android构建平台失败。");
        return "移动端打包目录已创建，Android平台切换已请求。";
    }
    public static string 构建()
    {
        检查空闲();
        检查英文路径();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("请等待Android平台切换与资源导入完成。");
        string[] 场景 = { "Assets/天帝/场景/天帝.unity", 天帝战斗地图.场景路径 };
        if (场景.Any(x => !File.Exists(x))) throw new InvalidOperationException("正式构建场景缺失。");
        Directory.CreateDirectory(目录);
        string 输出 = Path.Combine(目录, (Application.isBatchMode ? "Tiandi-preview-" : "天帝-移动试玩-") + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".apk");
        bool 原Bundle = EditorUserBuildSettings.buildAppBundle;
        bool 原导出 = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        bool 原签名 = PlayerSettings.Android.useCustomKeystore;
        bool 原分包 = PlayerSettings.Android.buildApkPerCpuArchitecture;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            var 报告 = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = 场景, locationPathName = 输出,
                target = BuildTarget.Android, options = BuildOptions.None
            });
            var 摘要 = 报告.summary;
            var 信息 = new 打包报告
            {
                APK = 输出, 结果 = 摘要.result.ToString(), 错误数 = 摘要.totalErrors,
                警告数 = 摘要.totalWarnings, 字节数 = 摘要.totalSize, 耗时秒 = 摘要.totalTime.TotalSeconds,
                包名 = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android),
                版本 = PlayerSettings.bundleVersion, 场景 = 场景,
                构建消息 = 报告.steps.SelectMany(x => x.messages)
                    .Where(x => x.type == LogType.Warning || x.type == LogType.Error || x.type == LogType.Exception)
                    .Select(x => x.content).Distinct().ToArray()
            };
            File.WriteAllText(Path.ChangeExtension(输出, ".构建报告.json"), JsonUtility.ToJson(信息, true));
            if (摘要.result != BuildResult.Succeeded || !File.Exists(输出))
                throw new InvalidOperationException("APK构建失败，请查看移动端打包中的构建报告。");
            return 输出;
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = 原Bundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = 原导出;
            PlayerSettings.Android.useCustomKeystore = 原签名;
            PlayerSettings.Android.buildApkPerCpuArchitecture = 原分包;
        }
    }
    public static string 恢复平台()
    {
        检查空闲();
        int 原 = SessionState.GetInt(原平台键, int.MinValue);
        if (原 == int.MinValue) return "没有待恢复的构建平台。";
        var 目标 = (BuildTarget)原;
        if (EditorUserBuildSettings.activeBuildTarget != 目标 &&
            !EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(目标), 目标))
            throw new InvalidOperationException("恢复原构建平台失败。");
        SessionState.EraseInt(原平台键);
        return "已请求恢复原构建平台：" + 目标;
    }
    static void 检查空闲()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("请等待当前运行、编译或构建任务结束。");
    }
    static void 检查英文路径()
    {
        if (天帝构建工具.项目根.Any(x => x > 127))
            throw new InvalidOperationException("Android工具不支持中文工程路径，请运行工具/打包移动APK.py；它会在英文临时目录打包后将APK放回移动端打包目录。");
    }
}
#endif
