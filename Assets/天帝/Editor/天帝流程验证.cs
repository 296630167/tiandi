#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public static class 天帝流程验证
{
    [Serializable] class 报告
    {
        public bool 标题仅开始; public bool 开始进入序章; public bool 跳过进入选择;
        public bool 飞入期间不可选; public bool 五枚源道纹; public bool 前三枚可选; public bool 封印提示;
        public bool 速度选择正确; public bool 智力选择正确;
        public bool 力量主角属性接入; public bool 速度主角属性接入; public bool 智力主角属性接入;
        public bool 封印选择拒绝; public bool 选择进入主页; public bool 重复选择拒绝; public bool 源道纹已接画布;
        public bool 主页仅三按钮; public bool 主角立绘存在; public bool 开始入口触发;
        public bool 设置开启; public bool 设置遮罩; public bool 音量响应; public bool 设置关闭;
        public bool 序章自然结束; public bool 场景无缺失脚本; public List<string> 错误 = new List<string>();
        public bool 进度与时长显示; public bool 点击进度跳转; public bool 回退更新段落; public bool 拖动进度跳转;
        public bool 拖动手柄不回弹; public bool 非法进度拒绝; public bool 后台禁止跳转; public bool 末尾进入选择; public bool 结束后跳转拒绝;
        public List<string> 点击诊断 = new List<string>();
    }
    static 报告 结果;
    static string 目录;
    static int 步骤;
    static double 下一步;
    static int 入口次数;
    static int 重试次数;
    static Vector2Int 上次屏幕;
    static int 上次帧;
    static bool 原设置存在;
    static float 原音量;
    static bool 原选项开启;
    static EnterPlayModeOptions 原进入选项;
    const string 设置键 = "Tiandi.Menu.Volume";
    public static string 启动()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止运行");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/精简主页-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        原设置存在 = PlayerPrefs.HasKey(设置键); 原音量 = PlayerPrefs.GetFloat(设置键, 0.6f);
        原选项开启 = EditorSettings.enterPlayModeOptionsEnabled; 原进入选项 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        结果 = new 报告(); 入口次数 = 0; 步骤 = 0; 重试次数 = 0; 上次屏幕 = Vector2Int.zero; 上次帧 = -1;
        结果.场景无缺失脚本 = EditorSceneManager.GetActiveScene().GetRootGameObjects().All(根 => 根.GetComponentsInChildren<Transform>(true).All(物 => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(物.gameObject) == 0));
        Application.logMessageReceived += 记录错误; EditorApplication.update += 更新;
        EditorApplication.isPlaying = true; 下一步 = EditorApplication.timeSinceStartup + 2;
        return 目录;
    }
    static void 记录错误(string 条件, string 栈, LogType 类型) { if (类型 == LogType.Error || 类型 == LogType.Exception) 结果.错误.Add(条件); }
    static Button[] 按钮(天帝游戏 游戏) => 游戏.GetComponentsInChildren<Button>().Where(键 => 键.gameObject.activeInHierarchy).ToArray();
    static bool 点击(天帝游戏 游戏, string 名)
    {
        Canvas.ForceUpdateCanvases(); var 键 = 按钮(游戏).FirstOrDefault(项 => 项.name == 名);
        if (键 == null || !键.interactable) { 结果.点击诊断.Add(名 + "：按钮不存在或禁用，阶段=" + 游戏.阶段); return false; }
        var 区 = (RectTransform)键.transform;
        var 指针 = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(区.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(指针, 命中);
        if (命中.Count == 0 || 命中[0].gameObject != 键.gameObject) { 结果.点击诊断.Add(名 + "：命中=" + (命中.Count > 0 ? 命中[0].gameObject.name : "空") + "，位置=" + 指针.position + "，阶段=" + 游戏.阶段); return false; }
        ExecuteEvents.Execute(键.gameObject, 指针, ExecuteEvents.pointerClickHandler); return true;
    }
    static void 更新()
    {
        if (EditorApplication.isPlaying) { Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); }
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        // 即使处于自然播放等待，也持续模拟前台；不能等32秒后才恢复焦点。
        if (EditorApplication.isPlaying && 游戏 != null && 游戏.阶段 == 游戏阶段.序章)
            游戏.GetComponent<天帝序章>().设置焦点(true);
        if (EditorApplication.timeSinceStartup < 下一步) return;
        if (EditorApplication.isPlaying && 游戏 != null && 游戏.界面 != null)
        {
            // 指针事件由本验证发送，隔离编辑器窗口的物理输入；模拟前台播放。
            var 模块 = 游戏.GetComponentInChildren<InputSystemUIInputModule>(); if (模块 != null) 模块.enabled = false;
            if (游戏.阶段 == 游戏阶段.序章) 游戏.GetComponent<天帝序章>().设置焦点(true);
            var 尺寸 = new Vector2Int(Screen.width, Screen.height);
            if (尺寸 != 上次屏幕) { 上次屏幕 = 尺寸; 上次帧 = Time.frameCount; 下一步 = EditorApplication.timeSinceStartup + 0.2; return; }
            if (Time.frameCount == 上次帧) return;
            var 画布 = 游戏.GetComponentInChildren<Canvas>();
            if (画布 != null && Mathf.Abs(画布.scaleFactor - Mathf.Sqrt(Screen.width / 1600f * (Screen.height / 900f))) > 0.02f) return;
            游戏.界面.更新适配(); Canvas.ForceUpdateCanvases();
        }
        try
        {
            switch (步骤)
            {
                case 0:
                    if (!EditorApplication.isPlaying || 游戏 == null || 游戏.界面 == null) { 下一步 += 0.5; return; }
                    Application.runInBackground = true;
                    结果.标题仅开始 = 游戏.阶段 == 游戏阶段.标题 && 按钮(游戏).Length == 1 && 按钮(游戏)[0].name == "开始游戏";
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "01-title.png")); break;
                case 1: 结果.开始进入序章 = 点击(游戏, "开始游戏") && 游戏.阶段 == 游戏阶段.序章; break;
                case 2:
                    var 序 = 游戏.GetComponent<天帝序章>();
                    var 滑条 = 游戏.GetComponentInChildren<天帝序章进度条>();
                    结果.进度与时长显示 = 序.总秒 == 28 && 滑条 != null && 滑条.interactable && 游戏.GetComponentsInChildren<Text>().Any(t => t.text.EndsWith(" / 00:28"));
                    结果.点击进度跳转 = 定位滑条(滑条, 0.65f, false) && Math.Abs(序.当前秒 - 18.2) < 0.1 && 游戏.GetComponentsInChildren<Text>().Any(t => t.text == "给我干哪来了，这还是国内吗？");
                    结果.回退更新段落 = 定位滑条(滑条, 0.1f, false) && Math.Abs(序.当前秒 - 2.8) < 0.1 && 游戏.GetComponentsInChildren<Text>().Any(t => t.text == "一个普通的午后");
                    结果.拖动进度跳转 = 定位滑条(滑条, 0.72f, true) && Math.Abs(序.当前秒 - 20.16) < 0.1;
                    结果.非法进度拒绝 = !游戏.调整序章进度(float.NaN) && !游戏.调整序章进度(float.PositiveInfinity);
                    序.设置后台(true); 结果.后台禁止跳转 = !游戏.调整序章进度(0.9f); 序.设置后台(false);
                    游戏.界面.更新序章进度(序.当前秒, 序.总秒, 序.可调整进度, 序.播放状态);
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "02-prologue.png")); break;
                case 3:
                    结果.跳过进入选择 = 点击(游戏, "跳过序章") && 游戏.阶段 == 游戏阶段.源道纹选择; 游戏.跳过序章();
                    结果.飞入期间不可选 = !游戏.界面.源道纹页.可选择 && !游戏.选择源道纹(0) && 游戏.初始源道纹编号 == -1;
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "05-arrival.png")); 下一步 = EditorApplication.timeSinceStartup + 3; 步骤++; return;
                case 4:
                    结果.五枚源道纹 = 游戏.GetComponentsInChildren<天帝源道纹绘图>().Length == 5;
                    结果.前三枚可选 = 按钮(游戏).Length == 5 && 按钮(游戏).Count(b => b.interactable) == 3 && 按钮(游戏).Where(b => b.interactable).Select(b => b.name).OrderBy(n => n).SequenceEqual(new[] { "源道纹-0", "源道纹-1", "源道纹-2" });
                    结果.封印提示 = 游戏.GetComponentsInChildren<Text>().Count(t => t.text == "封印 · 待开发") == 2;
                    结果.封印选择拒绝 = !游戏.选择源道纹(-1) && !游戏.选择源道纹(3) && !游戏.选择源道纹(4) && !游戏.选择源道纹(5) && 游戏.阶段 == 游戏阶段.源道纹选择 && 游戏.道纹数据 == null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "06-origin-choice.png")); break;
                case 5:
                    结果.选择进入主页 = 点击(游戏, "源道纹-0") && 游戏.阶段 == 游戏阶段.主页 && 游戏.初始源道纹编号 == 0;
                    结果.重复选择拒绝 = !游戏.选择源道纹(0);
                    结果.源道纹已接画布 = 验证源纹(游戏, 道纹属性.力量);
                    结果.力量主角属性接入 = 验证主角(游戏, 0);
                    break;
                case 6:
                    结果.主页仅三按钮 = 按钮(游戏).Select(键 => 键.name).OrderBy(名 => 名).SequenceEqual(new[] { "开始", "设置", "道纹" }.OrderBy(名 => 名));
                    结果.主角立绘存在 = 游戏.主角立绘 != null && 游戏.GetComponentsInChildren<Image>().Any(像 => 像.name == "主角立绘" && 像.sprite == 游戏.主角立绘);
                    游戏.新玩法开始.AddListener(() => 入口次数++);
                    结果.开始入口触发 = 点击(游戏, "开始") && 入口次数 == 1 && 游戏.阶段 == 游戏阶段.主页;
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "03-home.png")); break;
                case 7:
                    结果.设置开启 = 点击(游戏, "设置") && 游戏.界面.设置已打开;
                    break;
                case 8:
                    结果.设置遮罩 = !点击(游戏, "开始") && 入口次数 == 1;
                    var 滑 = 游戏.GetComponentInChildren<Slider>(); if (滑 != null) 滑.value = 0.25f;
                    结果.音量响应 = 滑 != null && Mathf.Abs(游戏.音量 - 0.25f) < 0.001f;
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "04-settings.png")); break;
                case 9:
                    结果.设置关闭 = 点击(游戏, "关闭") && !游戏.界面.设置已打开;
                    EditorApplication.isPlaying = false; break;
                case 10:
                    if (EditorApplication.isPlaying) { 下一步 += 0.5; return; }
                    恢复设置(); EditorApplication.isPlaying = true; 下一步 = EditorApplication.timeSinceStartup + 2; 步骤++; return;
                case 11:
                    if (!EditorApplication.isPlaying || 游戏 == null || 游戏.界面 == null) { 下一步 += 0.5; return; }
                    Application.runInBackground = true;
                    if (游戏.阶段 == 游戏阶段.标题 && !点击(游戏, "开始游戏") && 重试次数++ < 3) { 下一步 = EditorApplication.timeSinceStartup + 1; return; }
                    重试次数 = 0; break;
                case 12:
                    结果.末尾进入选择 = 定位滑条(游戏.GetComponentInChildren<天帝序章进度条>(), 1, true) && 游戏.阶段 == 游戏阶段.源道纹选择;
                    结果.结束后跳转拒绝 = !游戏.调整序章进度(0.5f) && !游戏.调整序章进度(1) && 游戏.初始源道纹编号 == -1;
                    下一步 = EditorApplication.timeSinceStartup + 3; 步骤++; return;
                case 13:
                    结果.速度选择正确 = 点击(游戏, "源道纹-1") && 游戏.初始源道纹编号 == 1 && 验证源纹(游戏, 道纹属性.速度);
                    结果.速度主角属性接入 = 验证主角(游戏, 1);
                    EditorApplication.isPlaying = false; break;
                case 14:
                    if (EditorApplication.isPlaying) { 下一步 += 0.5; return; }
                    EditorApplication.isPlaying = true; 下一步 = EditorApplication.timeSinceStartup + 2; 步骤++; return;
                case 15:
                    if (!EditorApplication.isPlaying || 游戏 == null || 游戏.界面 == null) { 下一步 += 0.5; return; }
                    Application.runInBackground = true;
                    if (游戏.阶段 == 游戏阶段.标题 && !点击(游戏, "开始游戏") && 重试次数++ < 3) { 下一步 = EditorApplication.timeSinceStartup + 1; return; }
                    重试次数 = 0; 下一步 = EditorApplication.timeSinceStartup + 32; 步骤++; return;
                case 16:
                    结果.序章自然结束 = 游戏 != null && 游戏.阶段 == 游戏阶段.源道纹选择 && 游戏.界面.源道纹页.可选择 && 游戏.初始源道纹编号 == -1 && 游戏.道纹数据 == null;
                    结果.智力选择正确 = 点击(游戏, "源道纹-2") && 游戏.初始源道纹编号 == 2 && 验证源纹(游戏, 道纹属性.智力);
                    结果.智力主角属性接入 = 验证主角(游戏, 2);
                    完成(); return;
            }
            步骤++; 下一步 = EditorApplication.timeSinceStartup + 0.65;
        }
        catch (Exception 异常) { 结果.错误.Add(异常.ToString()); 完成(); }
    }
    static bool 验证主角(天帝游戏 游戏, int 编号)
    {
        var 主角 = 游戏.主角属性; var 配置 = 游戏.主角初始属性;
        return 主角 != null && 主角.等级 == 游戏.道纹数据.玩家等级 &&
            主角.力量 == 配置.力量 + (编号 == 0 ? 10 : 0) && 主角.速度 == 配置.速度 + (编号 == 1 ? 10 : 0) && 主角.智力 == 配置.智力 + (编号 == 2 ? 10 : 0);
    }
    static bool 验证源纹(天帝游戏 游戏, 道纹属性 属性)
    {
        if (游戏.道纹数据 == null) return false;
        var 源 = 游戏.道纹数据.已放置[Vector2Int.zero];
        return 游戏.阶段 == 游戏阶段.主页 && 源.名称 == 属性 + "源道纹" && 源.接口 == 1 && 源.数值 == 10 && 源.属性 == 属性 && 游戏.道纹数据.生效加成[(int)属性] == 10;
    }
    static bool 定位滑条(天帝序章进度条 滑, float 比例, bool 拖动)
    {
        if (滑 == null || !滑.interactable) return false;
        Canvas.ForceUpdateCanvases();
        var 区 = (RectTransform)滑.handleRect.parent;
        Vector2 点(float 值) => RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(new Vector3(Mathf.Lerp(区.rect.xMin, 区.rect.xMax, 值), 区.rect.center.y)));
        var 指针 = new PointerEventData(EventSystem.current) { position = 点(拖动 ? 0.2f : 比例), button = PointerEventData.InputButton.Left };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(指针, 命中);
        if (命中.Count == 0 || ExecuteEvents.GetEventHandler<IPointerDownHandler>(命中[0].gameObject) != 滑.gameObject) return false;
        ExecuteEvents.ExecuteHierarchy(命中[0].gameObject, 指针, ExecuteEvents.pointerDownHandler);
        if (拖动)
        {
            ExecuteEvents.Execute(滑.gameObject, 指针, ExecuteEvents.initializePotentialDrag);
            指针.position = 点(比例); ExecuteEvents.Execute(滑.gameObject, 指针, ExecuteEvents.dragHandler);
            if (比例 < 1)
            {
                var 值 = 滑.value; UnityEngine.Object.FindAnyObjectByType<天帝游戏>().界面.更新序章进度(0, 28, true, "文字序章");
                结果.拖动手柄不回弹 = 滑.调整中 && Mathf.Abs(滑.value - 值) < 0.001f;
            }
        }
        ExecuteEvents.Execute(滑.gameObject, 指针, ExecuteEvents.pointerUpHandler);
        return true;
    }
    static void 恢复设置()
    { if (原设置存在) PlayerPrefs.SetFloat(设置键, 原音量); else PlayerPrefs.DeleteKey(设置键); PlayerPrefs.Save(); }
    static void 完成()
    {
        EditorApplication.update -= 更新; Application.logMessageReceived -= 记录错误;
        恢复设置(); File.WriteAllText(Path.Combine(目录, "home-smoke.json"), JsonUtility.ToJson(结果, true)); EditorApplication.isPlaying = false;
        EditorSettings.enterPlayModeOptionsEnabled = 原选项开启; EditorSettings.enterPlayModeOptions = 原进入选项;
    }
}
#endif
