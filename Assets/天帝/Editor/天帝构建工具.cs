#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

public static class 天帝构建工具
{
    public static string 接入音频() => 天帝音频接入.导入();
    public static string 验证音频() => 天帝音频接入.验证素材();
    public static string 诊断音频() => 天帝音频接入.诊断();
    public static string 验证音频流程() => 天帝音频流程验证.启动();
    public static string 准备移动打包() => 天帝移动打包.准备();
    public static string 构建移动APK() => 天帝移动打包.构建();
    public static string 恢复打包平台() => 天帝移动打包.恢复平台();
    public static string 验证移动适配() => 天帝移动适配验证.运行();
    public static string 验证双端页面() => 天帝双端页面验证.运行();
    public static string 验证作弊码() => 天帝作弊码验证.启动();
    public static string 验证作弊码模型() => 天帝作弊码验证.验证模型();
    public static string 验证通货模型() => 天帝通货验证.验证模型();
    public static string 验证功能道纹() => 天帝功能道纹验证.运行();
    public static string 验证顺序道纹() => 天帝顺序道纹验证.运行();
    public static string 验证扩展道纹() => 天帝扩展道纹验证.运行();
    public static string 验证特性与形态() => 天帝特性与形态验证.运行();
    public static string 发放顺序道纹各二十() => 天帝试玩道纹发放.各二十();
    public static string 验证属性说明() => 天帝属性说明验证.运行();
    public static string 验证改造材料锁定() => 天帝改造背包验证.验证材料锁定();
    public static string 接入战斗扩展() => 天帝战斗扩展接入.导入();
    public static string 验证战斗扩展() => 天帝战斗扩展验证.运行();
    public static string 验证战斗扩展实战() => 天帝战斗扩展实战验证.启动();
    public static string 验证道纹回收() => 天帝道纹回收验证.运行();
    public static string 巡检全部页面() => 天帝全页巡检.启动();
    public static string 补拍信息层级() => 天帝全页巡检.启动层级补拍();
    public static string 巡检回收界面() => 天帝全页巡检.启动回收();
    public static string 接入漫画序章() => 天帝漫画序章接入.导入();
    public static string 验证漫画序章() => 天帝漫画序章验证.启动();
    public static string 项目根 => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    public static string 验证改造背包() => 天帝改造背包验证.启动();
    public static string 验证宝盒() => 天帝宝盒验证.运行();
    public static string 验证宝盒反馈() => 天帝宝盒验证.验证界面();
    public static string 查看宝盒界面() => 天帝宝盒验证.查看界面();
    public static string 验证选图波次() => 天帝选图波次验证.启动();
    public static string 验证自动吸附() => 天帝自动吸附验证.运行();
    public static string 验证灵石掉落() => 天帝灵石掉落验证.运行();
    public static string 验证真实数值() => 天帝真实数值验证.运行();
    public static string 验证真实数值实战() => 天帝真实数值实战验证.启动();
    public static string 验证战斗视野() => 天帝战斗视野验证.运行();
    public static string 验证伤害来源() => 天帝伤害来源验证.运行();
    public static string 查看选图主页() => 天帝选图试玩.打开();
    public static string 验证移动区域() => 天帝移动区域验证.运行();
    public static string 接入主角动画() => 天帝主角动画工具.接入();
    public static string 验证主角动画() => 天帝主角动画工具.验证();
    public static string 验证主角动画实战() => 天帝主角动画实战验证.启动();
    public static string 接入正面骨骼样例() => 天帝正面步态样例工具.接入();
    public static string 查看正面骨骼样例() => 天帝正面步态样例工具.查看();
    public static string 验证道纹详情() => 天帝道纹详情验证.运行();
    public static string 导入道纹界面美术() => 天帝道纹美术接入.导入();
    public static string 导入D概念主页() => 天帝道纹美术接入.导入主页();
    public static string 验证道纹界面美术() => 天帝道纹界面美术验证.启动();
    public static string 验证道纹图鉴() => 天帝道纹图鉴验证.启动();
    public static string 拍摄游戏截图() => 天帝游戏截图.启动();
    public static string 采集第二期日志() => 天帝第二期采集.启动();
    public static string 补采第二期宝盒() => 天帝第二期采集.补采宝盒();
    const string 主场景 = "Assets/天帝/场景/天帝.unity";
    [MenuItem("天帝/初始化精简主页")]
    public static void 初始化菜单() => 初始化();
    public static string 初始化()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("当前场景有未保存修改，请先保存。");
        AssetDatabase.Refresh();
        foreach (string 路径 in new[] { "Assets/天帝/美术/角色/主角立绘.png", "Assets/天帝/美术/主页背景.png" })
        {
            var 导入 = AssetImporter.GetAtPath(路径) as TextureImporter;
            if (导入 == null) throw new Exception("素材未导入：" + 路径);
            导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
            导入.mipmapEnabled = false; 导入.alphaIsTransparency = true; 导入.maxTextureSize = 2048;
            导入.textureCompression = TextureImporterCompression.Uncompressed; 导入.SaveAndReimport();
        }
        EditorSceneManager.OpenScene(主场景);
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null) 游戏 = new GameObject("天帝入口").AddComponent<天帝游戏>();
        游戏.默认字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
        游戏.主角立绘 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/天帝/美术/角色/主角立绘.png");
        游戏.主页背景 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/天帝/美术/主页背景.png");
        游戏.主页音乐 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/天帝/音频/music-0.wav");
        if (游戏.默认字体 == null || 游戏.主角立绘 == null) throw new Exception("字体或立绘引用不完整");
        var 相机 = Camera.main;
        if (相机 != null) { 相机.transform.position = new Vector3(0, 0, -10); 相机.backgroundColor = new Color(0.94f, 0.92f, 0.86f); }
        PlayerSettings.bundleVersion = "0.15.0"; PlayerSettings.productName = 天帝游戏.全名;
        PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        EditorUtility.SetDirty(游戏); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), 主场景);
        初始化战斗场景(); AssetDatabase.SaveAssets();
        return "精简场景已保存：开始游戏→序章→主页";
    }
    [MenuItem("天帝/构建Windows版本")]
    public static void Windows菜单() => 构建Windows();
    public static string 构建Windows()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式");
        PlayerSettings.bundleVersion = "0.15.0"; PlayerSettings.productName = 天帝游戏.全名;
        string 路径 = Path.Combine(项目根, "生成/Windows/天帝.exe"); Directory.CreateDirectory(Path.GetDirectoryName(路径));
        if (!File.Exists(天帝战斗地图.场景路径)) throw new InvalidOperationException("请先运行天帝/初始化战斗场景。");
        var 报告 = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { 主场景, 天帝战斗地图.场景路径 }, locationPathName = 路径, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        Directory.CreateDirectory(Path.Combine(项目根, "生成/验证"));
        File.WriteAllText(Path.Combine(项目根, "生成/验证/windows-build.json"), "{\"result\":\"" + 报告.summary.result + "\",\"errors\":" + 报告.summary.totalErrors + ",\"warnings\":" + 报告.summary.totalWarnings + ",\"bytes\":" + 报告.summary.totalSize + "}");
        if (报告.summary.result != BuildResult.Succeeded) throw new Exception("Windows构建失败：" + 报告.summary.result);
        return 路径;
    }
    public static string 运行主页验证() => 天帝流程验证.启动();
    public static string 运行道纹验证() => 天帝属性道纹验证.启动();
    public static string 运行主角属性验证() => 天帝主角属性验证.运行();
    public static string 运行天赋验证() => 天帝天赋验证.启动();
    public static string 运行道纹品阶验证() => 天帝属性道纹验证.启动();
    public static string 运行接口验证() => 天帝接口验证.运行();
    public static string 运行开局验证() => 天帝开局验证.启动();
    public static string 运行通货验证() => 天帝通货验证.启动();
    public static string 运行品阶画布验证() => 天帝属性道纹验证.启动();
    public static string 运行战斗验证() => 天帝属性道纹验证.启动();
    public static string 运行实战验证() => 天帝属性道纹验证.启动();
    public static string 运行属性道纹验证() => 天帝属性道纹验证.启动();
    public static string 录制试玩() => 天帝试玩录制.启动();
    public static string 运行通货掉落验证() => 天帝通货掉落验证.运行();
    public static string 运行AI掉落验证() => 天帝AI掉落验证.运行();
    public static string 运行道纹筛选验证() => 天帝属性道纹验证.启动();
    public static string 运行拾取提示验证() => 天帝拾取提示验证.运行();
    public static string 导入美术() => 天帝美术接入.导入();
    public static string 运行美术验证() => 天帝美术验证.启动();
    public static string 构建属性Windows()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止运行。");
        string 原版本 = PlayerSettings.bundleVersion;
        string 输出 = Path.Combine(项目根, "生成/Windows-属性道纹0.17.0/天帝.exe");
        string 验证目录 = Path.Combine(项目根, "生成/验证/属性道纹构建");
        Directory.CreateDirectory(Path.GetDirectoryName(输出)); Directory.CreateDirectory(验证目录);
        try
        {
            PlayerSettings.bundleVersion = "0.17.0"; PlayerSettings.productName = 天帝游戏.全名;
            var 报告 = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { 主场景, 天帝战斗地图.场景路径 }, locationPathName = 输出, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            File.WriteAllText(Path.Combine(验证目录, "windows-build.json"), "{\"result\":\"" + 报告.summary.result + "\",\"errors\":" + 报告.summary.totalErrors + ",\"warnings\":" + 报告.summary.totalWarnings + ",\"bytes\":" + 报告.summary.totalSize + "}");
            if (报告.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("属性道纹版构建失败。");
            return 输出;
        }
        finally { PlayerSettings.bundleVersion = 原版本; }
    }
    public static string 构建美术Windows()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止运行。");
        string 原版本 = PlayerSettings.bundleVersion;
        string 输出 = Path.Combine(项目根, "生成/Windows-美术0.16.0/天帝.exe"); Directory.CreateDirectory(Path.GetDirectoryName(输出));
        try
        {
            PlayerSettings.bundleVersion = "0.16.0";
            var 报告 = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { 主场景, 天帝战斗地图.场景路径 }, locationPathName = 输出, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            File.WriteAllText(Path.Combine(项目根, "生成/验证/美术接入/windows-build.json"), "{\"result\":\"" + 报告.summary.result + "\",\"errors\":" + 报告.summary.totalErrors + ",\"warnings\":" + 报告.summary.totalWarnings + ",\"bytes\":" + 报告.summary.totalSize + "}");
            if (报告.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("美术版构建失败。");
            return 输出;
        }
        finally { PlayerSettings.bundleVersion = 原版本; }
    }
    [MenuItem("天帝/初始化战斗场景")]
    public static void 初始化战斗菜单() => 初始化战斗场景();
    public static string 初始化战斗场景()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("运行中不创建场景，避免打断当前工作。");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("请先保存当前场景修改。");
        AssetDatabase.Refresh();
        string 材质路径 = "Assets/天帝/美术/战斗地图.mat";
        var 材质 = AssetDatabase.LoadAssetAtPath<Material>(材质路径);
        if (材质 == null)
        {
            var 着色器 = AssetDatabase.LoadAssetAtPath<Shader>("Assets/天帝/美术/地图顶点色.shader");
            if (着色器 == null || ShaderUtil.ShaderHasError(着色器)) throw new InvalidOperationException("地图着色器尚未导入或编译失败。");
            材质 = new Material(着色器); AssetDatabase.CreateAsset(材质, 材质路径);
        }
        if (!File.Exists(天帝战斗地图.场景路径))
        {
            var 原场景 = EditorSceneManager.GetActiveScene();
            var 新场景 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var 入口 = new GameObject("青岚原入口").AddComponent<天帝战斗场景>(); 入口.地图材质 = 材质;
                EditorSceneManager.SaveScene(新场景, 天帝战斗地图.场景路径);
            }
            finally { EditorSceneManager.CloseScene(新场景, true); if (原场景.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(原场景); }
        }
        var 列表 = EditorBuildSettings.scenes.ToList();
        if (!列表.Any(s => s.path == 主场景)) 列表.Insert(0, new EditorBuildSettingsScene(主场景, true));
        if (!列表.Any(s => s.path == 天帝战斗地图.场景路径)) 列表.Add(new EditorBuildSettingsScene(天帝战斗地图.场景路径, true));
        foreach (var 场景 in 列表) if (场景.path == 主场景 || 场景.path == 天帝战斗地图.场景路径) 场景.enabled = true;
        EditorBuildSettings.scenes = 列表.ToArray(); AssetDatabase.SaveAssets(); return "青岚原场景与构建入口已配置";
    }
    public static string 导出标题截图()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("请先运行场景");
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        游戏.界面.显示标题(); Canvas.ForceUpdateCanvases();
        string 路径 = Path.Combine(项目根, "生成/验证/界面预览-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(路径); EditorApplication.QueuePlayerLoopUpdate(); return 路径;
    }
    public static string 导出主页截图()
    {
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (!EditorApplication.isPlaying || 游戏 == null || 游戏.阶段 != 游戏阶段.主页)
            throw new InvalidOperationException("请先进入主页，截图不会切换页面或修改玩家数据。");
        Canvas.ForceUpdateCanvases();
        string 路径 = Path.Combine(项目根, "生成/验证/主页实机-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(路径); EditorApplication.QueuePlayerLoopUpdate(); return 路径;
    }
    public static string 导出主页等级截图()
    {
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (!EditorApplication.isPlaying || 游戏 == null || 游戏.阶段 != 游戏阶段.主页)
            throw new InvalidOperationException("请先进入主页。");
        var 下拉 = 游戏.GetComponentsInChildren<天帝等级下拉>().FirstOrDefault();
        if (下拉 == null) throw new InvalidOperationException("主页地图等级菜单不存在。");
        下拉.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
        Canvas.ForceUpdateCanvases();
        string 路径 = Path.Combine(项目根, "生成/验证/主页等级实机-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(路径); EditorApplication.QueuePlayerLoopUpdate(); return 路径;
    }
    public static string 查看改造界面并截图(string 通货名)
    {
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (!EditorApplication.isPlaying || 游戏 == null) throw new InvalidOperationException("请先运行隔离预览。");
        if (游戏.阶段 == 游戏阶段.主页) 游戏.打开道纹改造();
        if (游戏.阶段 != 游戏阶段.道纹改造) throw new InvalidOperationException("仅从主页或改造页取景。");
        if (!string.IsNullOrEmpty(通货名) && Enum.TryParse<通货种类>(通货名, out var 种类)) 游戏.界面.改造页.选通货(种类);
        Canvas.ForceUpdateCanvases();
        string 路径 = Path.Combine(项目根, "生成/验证/改造实机-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png");
        var 溢出 = 游戏.界面.改造页.GetComponentsInChildren<UnityEngine.UI.Text>().Where(x => x.enabled && !string.IsNullOrEmpty(x.text) && x.preferredHeight > x.rectTransform.rect.height + 1);
        File.WriteAllText(路径.Replace(".png", ".txt"), "文字溢出：\n" + string.Join("\n", 溢出.Select(x => x.text + " [" + x.preferredHeight + "/" + x.rectTransform.rect.height + "]")));
        ScreenCapture.CaptureScreenshot(路径); EditorApplication.QueuePlayerLoopUpdate(); return 路径;
    }
    [MenuItem("天帝/调试/玩家升1级")]
    public static void 升级调试()
    {
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        var 数据 = 游戏 != null ? 游戏.道纹数据 : null;
        if (!EditorApplication.isPlaying || 数据 == null) { Debug.Log("请在运行模式选定源道纹后使用升级调试。"); return; }
        if (数据.玩家等级 < int.MaxValue) 数据.设置玩家等级(数据.玩家等级 + 1);
    }
    public static string 状态()
    {
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        var 画布 = 游戏 != null ? 游戏.GetComponentInChildren<Canvas>() : null;
        return JsonUtility.ToJson(new 运行状态 { 编译中 = EditorApplication.isCompiling, 游戏中 = EditorApplication.isPlaying, 场景 = EditorSceneManager.GetActiveScene().path, 阶段 = 游戏 != null ? 游戏.阶段.ToString() : "编辑", 设置 = 游戏 != null && 游戏.界面 != null && 游戏.界面.设置已打开,
            暂停 = EditorApplication.isPaused, 宽 = Screen.width, 高 = Screen.height, 帧 = Time.frameCount, 比例 = 画布 != null ? 画布.scaleFactor : 0 });
    }
    [Serializable] class 运行状态 { public bool 编译中; public bool 游戏中; public string 场景; public string 阶段; public bool 设置; public bool 暂停; public int 宽; public int 高; public int 帧; public float 比例; }
}
#endif
