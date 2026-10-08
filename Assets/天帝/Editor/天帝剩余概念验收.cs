#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 在正式主场景Play中验收；仅验证目录与独立边界模型承接操作，不改玩家存档或场景。
public static partial class 天帝剩余概念验收
{
    [Serializable] public sealed class 页面记录
    {
        public string 名称;
        public int 文字数;
        public List<string> 新素材 = new List<string>();
        public List<string> 溢出 = new List<string>();
    }
    [Serializable] public sealed class 结果
    {
        public string 场景, 原存档, 结束存档, 原场景, 结束场景;
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
        public List<string> 编辑器诊断 = new List<string>();
        public List<页面记录> 页面 = new List<页面记录>();
        public bool 完成;
    }
    static 天帝游戏 游戏;
    static 结果 报告;
    static string 目录, 原目录, 真实存档, 场景文件;
    static bool 已开始, 已结束, 原选项开, 原后台;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static EditorWindow 窗口;
    static int 原尺寸;
    static Rect? 原安全区;
    static readonly string[] 浮点键 = { "Tiandi.Menu.Volume", "Tiandi.Menu.Music", "Tiandi.Menu.Effects", "Tiandi.Menu.Story" };
    static readonly string[] 整数键 = { "Tiandi.Menu.Subtitles", "Tiandi.Menu.PrologueSeen" };
    static Dictionary<string, float> 浮点原值;
    static Dictionary<string, int> 整数原值;
    static Dictionary<string, bool> 存在;
    static string 指纹(string 路径)
    {
        if (!File.Exists(路径)) return "无文件";
        using (var h = SHA256.Create()) return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(路径)));
    }
    public static void 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("需编辑模式");
        目录 = Environment.GetEnvironmentVariable("TIANDI_CAPTURE_DIR");
        if (string.IsNullOrEmpty(目录)) throw new InvalidOperationException("需独立验证输出目录");
        Directory.CreateDirectory(目录);
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        天帝数值同步检查.校验();
        foreach (var p in Directory.GetFiles("Assets/天帝/Resources/山水剩余界面", "*.png"))
            AssetDatabase.ImportAsset(p.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
        if (Directory.Exists("Assets/天帝/Resources/山水战斗HUD"))
            foreach (var p in Directory.GetFiles("Assets/天帝/Resources/山水战斗HUD", "*.png")) AssetDatabase.ImportAsset(p.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
        场景文件 = Path.GetFullPath("Assets/天帝/场景/天帝.unity");
        真实存档 = Path.Combine(Application.persistentDataPath, "天帝进度.json");
        报告 = new 结果 { 场景 = "Assets/天帝/场景/天帝.unity", 原场景 = 指纹(场景文件), 原存档 = 指纹(真实存档) };
        var 隔离 = Path.Combine(目录, "隔离存档"); Directory.CreateDirectory(隔离);
        if (File.Exists(真实存档)) File.Copy(真实存档, Path.Combine(隔离, "天帝进度.json"), true);
        else
        {
            var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            new 天帝存档(隔离).保存(new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网, 42).导出库存(), 灵石 = 天帝宝盒.开局灵石 });
        }
        原目录 = 天帝存档.验证目录; 天帝存档.验证目录 = 隔离;
        原选项开 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions;
        原后台 = Application.runInBackground;
        存在 = 浮点键.Concat(整数键).ToDictionary(x => x, PlayerPrefs.HasKey);
        浮点原值 = 浮点键.ToDictionary(x => x, x => PlayerPrefs.GetFloat(x));
        整数原值 = 整数键.ToDictionary(x => x, x => PlayerPrefs.GetInt(x));
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        设置视图();
        已开始 = 已结束 = false; 截止 = EditorApplication.timeSinceStartup + 360;
        EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 清理;
        Application.logMessageReceived += 记错; EditorApplication.isPlaying = true;
    }
    static void 设置视图()
    {
        var a = typeof(Editor).Assembly; var f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        窗口 = EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
        原尺寸 = (int)窗口.GetType().GetProperty("selectedSizeIndex", f).GetValue(窗口);
        var t = a.GetType("UnityEditor.GameViewSizes");
        var 单 = typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var 组 = t.GetMethod("GetGroup").Invoke(单, new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeGroupType"), 0) });
        var 大小 = a.GetType("UnityEditor.GameViewSize").GetConstructor(f, null, new[] { a.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }, null)
            .Invoke(new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"), 1), (object)1920, 1080, "剩余概念真实场景验收" });
        组.GetType().GetMethod("AddCustomSize").Invoke(组, new[] { 大小 });
        int i = (int)组.GetType().GetMethod("GetBuiltinCount").Invoke(组, null) + (int)组.GetType().GetMethod("GetCustomCount").Invoke(组, null) - 1;
        窗口.GetType().GetProperty("selectedSizeIndex", f).SetValue(窗口, i); 窗口.Focus();
        原安全区 = 天帝移动适配.验证安全区; 天帝移动适配.验证安全区 = new Rect(0, 0, 1920, 1080);
    }
    static void 等待()
    {
        if (已结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 报告.错误.Add("实际场景验收超时"); 结束(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (已开始) return;
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null || 游戏.阶段 != 游戏阶段.标题) return;
        已开始 = true;
        var 输入 = 游戏.GetComponentInChildren<InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        游戏.StartCoroutine(保护(流程()));
    }
    static IEnumerator 流程()
    {
        if (Environment.GetEnvironmentVariable("TIANDI_BATTLE_AUDIT") == "1")
        { yield return 主动战斗验证(); 报告.完成 = true; yield break; }
        foreach (var 名 in new[] { "图鉴背景", "回收背景", "宝盒背景", "标题背景", "天赋背景", "详情长卷", "松山弹窗", "红叶弹窗", "天赋卡片", "图录卡" })
            检查("独立素材已导入-" + 名, Resources.Load<Sprite>("山水剩余界面/" + 名) != null);
        yield return 标题天赋验证();
        if (游戏.阶段 == 游戏阶段.标题) 游戏.继续游戏();
        yield return null;
        if (Environment.GetEnvironmentVariable("TIANDI_UI_AUDIT") == "1") yield return 首四页自检();
        yield return 图录回收验证();
        yield return 弹窗验证();
        yield return 辅助页验证();
        yield return 拍("01-04_原验收主页保留");
        报告.完成 = true;
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            bool 有; object x = null;
            try { 有 = 栈.Peek().MoveNext(); if (有) x = 栈.Peek().Current; }
            catch (Exception ex) { 报告.错误.Add(ex.ToString()); break; }
            if (!有) { 栈.Pop(); continue; }
            if (x is IEnumerator 子) { 栈.Push(子); continue; }
            yield return x;
        }
        结束();
    }
    static IEnumerator 拍(string 名)
    {
        yield return null; 游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null;
        foreach (var 文域 in 游戏.GetComponentsInChildren<天帝按钮文字区域>()) if (文域.enabled) 文域.更新();
        Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var 页 = new 页面记录 { 名称 = 名 };
        foreach (var 图 in 游戏.GetComponentsInChildren<Image>())
            if (图.sprite != null && AssetDatabase.GetAssetPath(图.sprite).Contains("山水剩余界面/")) 页.新素材.Add(图.name + "=" + 图.sprite.name);
        var 范围 = 当前文字审查范围();
        foreach (var 文 in 范围.GetComponentsInChildren<Text>().Where(x => x.enabled && !string.IsNullOrEmpty(x.text)))
        {
            if (文.GetComponentsInParent<CanvasGroup>().Any(x => x.alpha < .1f)) continue;
            页.文字数++;
            if (!文.resizeTextForBestFit && 文.preferredHeight > 文.rectTransform.rect.height + 2)
                页.溢出.Add(文.transform.parent.name + "/" + 文.name + "：" + 文.text + " [" + 文.preferredHeight + "/" + 文.rectTransform.rect.height + "]");
        }
        报告.页面.Add(页);
        var 图像 = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(目录, 名 + ".jpg"), 图像.EncodeToJPG(88)); UnityEngine.Object.Destroy(图像);
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(报告, true));
        Debug.Log("剩余概念已拍：" + 名);
    }
    static Transform 当前文字审查范围()
    {
        var 全部 = 游戏.GetComponentsInChildren<Transform>();
        var 窗名 = new[] { "道纹背包窗口", "布局方案面板", "来源面板", "回收确认面板", "回收范围小窗", "作弊码面板", "设置面板", "确认面板", "概率面板", "战斗失败面板", "暂停详情", "宝盒面板" };
        foreach (var 名 in 窗名)
        {
            var 窗 = 全部.LastOrDefault(x => x.name == 名);
            if (窗 != null) return 窗;
        }
        if (游戏.界面.回收已打开) return 游戏.界面.回收页.transform;
        if (游戏.界面.图鉴已打开) return 游戏.界面.图鉴页.transform;
        if (游戏.阶段 == 游戏阶段.源道纹选择) return 游戏.界面.源道纹页.transform;
        return 游戏.transform;
    }
    static void 检查(string 名, bool 对) => (对 ? 报告.通过 : 报告.失败).Add(名);
    static void 点(string 名)
    {
        var b = 游戏.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == 名 && x.interactable);
        if (b == null) throw new InvalidOperationException("真实按钮不存在或不可点击：" + 名);
        点(b);
    }
    static void 点(Button b)
    {
        游戏.界面.更新适配();
        Canvas.ForceUpdateCanvases(); var r = (RectTransform)b.transform;
        var 画布 = b.GetComponentInParent<Canvas>();
        var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(画布.renderMode == RenderMode.ScreenSpaceOverlay ? null : 画布.worldCamera, r.TransformPoint(r.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, 命中);
        bool 对 = 命中.Count > 0 && (命中[0].gameObject == b.gameObject || 命中[0].gameObject.transform.IsChildOf(b.transform));
        检查("实际按钮射线-" + b.name, 对);
        if (!对) File.AppendAllText(Path.Combine(目录, "raycast.txt"), b.name + " => " + string.Join(",", 命中.Select(x => x.gameObject.name)) + "\n");
        ExecuteEvents.Execute(b.gameObject, e, ExecuteEvents.pointerClickHandler);
    }
    static void 勾(string 名)
    {
        var t = 游戏.GetComponentsInChildren<Toggle>().First(x => x.name == 名 && x.interactable);
        Canvas.ForceUpdateCanvases(); var r = (RectTransform)t.transform;
        var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, 命中);
        检查("实际复选框射线-" + 名, 命中.Count > 0 && (命中[0].gameObject == t.gameObject || 命中[0].gameObject.transform.IsChildOf(t.transform)));
        ExecuteEvents.Execute(t.gameObject, e, ExecuteEvents.pointerClickHandler);
    }
    static void 私调(object o, string 名) => o.GetType().GetMethod(名, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(o, null);
    static 天帝道纹 创建道纹夹具()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(50);
        var 随机 = new System.Random(8420);
        for (int i = 0; i < 37; i++) 网.获得道纹(天帝道纹生成.创建(1 + i, 道纹分类.属性, (道纹品阶)(i % 8), 随机, 道纹属性分组.基础, 50));
        网.解锁格子(new Vector2Int(1, 0)); 网.道纹[0].接口 = 9; 网.放置(网.道纹[0], new Vector2Int(1, 0));
        网.保存布局方案(0, "山岚"); 网.收回(网.道纹[0]);
        网.道纹[1].接口 = 9; 网.放置(网.道纹[1], new Vector2Int(1, 0));
        网.保存布局方案(1, "青峰"); 网.保存布局方案(2, "归流");
        return 网;
    }
    static void 记错(string 文, string 栈, LogType 类)
    {
        if (类 != LogType.Error && 类 != LogType.Exception) return;
        if (栈.Contains("UnityEditor.Search.SearchDatabase")) 报告.编辑器诊断.Add(文 + "\n" + 栈);
        else 报告.错误.Add(文 + "\n" + 栈);
    }
    static void 结束()
    { if (已结束) return; 已结束 = true; EditorApplication.update -= 等待; Application.logMessageReceived -= 记错; EditorApplication.isPlaying = false; }
    static void 清理(PlayModeStateChange 状态)
    {
        if (状态 != PlayModeStateChange.EnteredEditMode || !已结束) return;
        报告.结束存档 = 指纹(真实存档); 报告.结束场景 = 指纹(场景文件);
        检查("正式主场景未保存或覆盖", 报告.原场景 == 报告.结束场景);
        检查("真实玩家存档指纹不变", 报告.原存档 == 报告.结束存档); 检查("剩余概念流程完整完成", 报告.完成);
        天帝存档.验证目录 = 原目录; EditorSettings.enterPlayModeOptionsEnabled = 原选项开; EditorSettings.enterPlayModeOptions = 原选项;
        Application.runInBackground = 原后台; 天帝移动适配.验证安全区 = 原安全区;
        if (窗口 != null) 窗口.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(窗口, 原尺寸);
        foreach (var k in 浮点键) { if (存在[k]) PlayerPrefs.SetFloat(k, 浮点原值[k]); else PlayerPrefs.DeleteKey(k); }
        foreach (var k in 整数键) { if (存在[k]) PlayerPrefs.SetInt(k, 整数原值[k]); else PlayerPrefs.DeleteKey(k); }
        PlayerPrefs.Save(); EditorApplication.playModeStateChanged -= 清理;
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(报告, true));
        File.WriteAllText(Path.Combine(目录, "完成.txt"), $"通过{报告.通过.Count} 失败{报告.失败.Count} 错误{报告.错误.Count} 页面{报告.页面.Count}");
        EditorApplication.delayCall += () => EditorApplication.Exit(报告.完成 && 报告.失败.Count == 0 && 报告.错误.Count == 0 ? 0 : 1);
    }
}
#endif
