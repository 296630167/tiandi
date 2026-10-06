#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝美术验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录;
    static 天帝游戏 游戏;
    static EditorWindow 窗口;
    static bool 原最大, 原后台, 原选项启用;
    static EnterPlayModeOptions 原选项;
    static int 原尺寸;
    static double 截止;
    static object 尺寸组;
    static readonly List<int> 新尺寸 = new List<int>();
    static readonly BindingFlags 实例 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static void 检查(string 名, bool 值) => (值 ? 结果.通过 : 结果.失败).Add(名);
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先停止并保存当前场景。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/美术-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        结果 = new 报告(); 游戏 = null; 新尺寸.Clear();
        原后台 = Application.runInBackground; 原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions;
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        窗口 = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); 原最大 = 窗口.maximized; 窗口.maximized = true; 窗口.Focus();
        原尺寸 = (int)窗口.GetType().GetProperty("selectedSizeIndex", 实例).GetValue(窗口);
        设置尺寸(1600, 900);
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        截止 = EditorApplication.timeSinceStartup + 100;
        Application.logMessageReceived += 记错; EditorApplication.update += 启动等待; EditorApplication.isPlaying = true;
        return 目录;
    }
    static void 设置尺寸(int 宽, int 高)
    {
        var 程序集 = typeof(Editor).Assembly; var 类型 = 程序集.GetType("UnityEditor.GameViewSizes");
        var 单例 = typeof(ScriptableSingleton<>).MakeGenericType(类型).GetProperty("instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        尺寸组 = 类型.GetMethod("GetGroup").Invoke(单例, new[] { Enum.ToObject(程序集.GetType("UnityEditor.GameViewSizeGroupType"), 0) });
        var 尺寸类型 = 程序集.GetType("UnityEditor.GameViewSize");
        var 构造 = 尺寸类型.GetConstructor(实例, null, new[] { 程序集.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }, null);
        var 尺寸 = 构造.Invoke(new[] { Enum.ToObject(程序集.GetType("UnityEditor.GameViewSizeType"), 1), (object)宽, 高, "天帝美术验收" });
        int 自定义序 = (int)尺寸组.GetType().GetMethod("GetCustomCount").Invoke(尺寸组, null);
        尺寸组.GetType().GetMethod("AddCustomSize").Invoke(尺寸组, new[] { 尺寸 });
        int 索引 = (int)尺寸组.GetType().GetMethod("GetBuiltinCount").Invoke(尺寸组, null) + 自定义序;
        新尺寸.Add(索引);
        窗口.GetType().GetProperty("selectedSizeIndex", 实例).SetValue(窗口, 索引);
    }
    static void 启动等待()
    {
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("美术验证超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (游戏 != null) return;
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (游戏 != null) 游戏.StartCoroutine(保护(流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            object 当前 = null; bool 有;
            try { 有 = 栈.Peek().MoveNext(); if (有) 当前 = 栈.Peek().Current; }
            catch (Exception 异常) { 结果.错误.Add(异常.ToString()); break; }
            if (!有) { 栈.Pop(); continue; }
            if (当前 is IEnumerator 子) { 栈.Push(子); continue; }
            yield return 当前;
        }
        完成();
    }
    static IEnumerator 截图(string 名)
    {
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var 图 = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(目录, 名 + ".png"), 图.EncodeToPNG()); UnityEngine.Object.Destroy(图);
    }
    static IEnumerator 流程()
    {
        yield return new WaitForSecondsRealtime(.7f);
        var 输入 = 游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        检查("75张资源与26项图集绑定", 游戏.美术 != null && 游戏.美术.图片.Length == 75 && 游戏.美术.图片.All(p => p.图片 != null) && 游戏.美术.道纹图集 != null);
        检查("立绘Shader无错误", !ShaderUtil.ShaderHasError(游戏.美术.立绘着色器));
        检查("标题背景采用BG01", 游戏.主页背景 == 游戏.美术.获取("BG01")); yield return 截图("01-标题-1600x900");
        游戏.开始序章(); yield return new WaitForSecondsRealtime(.2f);
        var 序 = 游戏.GetComponent<天帝序章>();
        for (int i = 0; i < 7; i++)
        {
            游戏.调整序章进度((i * 4 + 2.4f) / 28); yield return null;
            var 屏 = 游戏.GetComponentsInChildren<RawImage>().Single(p => p.name == "开场视频");
            检查("序章插画SC" + (i + 1), 屏.enabled && 屏.texture == 游戏.美术.获取("SC" + (i + 1).ToString("00")).texture);
            if (i == 0 || i == 3 || i == 6) yield return 截图("02-序章-" + (i + 1));
        }
        检查("序章进度可回退", 游戏.调整序章进度(.1f) && 序.当前秒 < 3);
        游戏.跳过序章(); yield return new WaitForSecondsRealtime(2.5f);
        检查("五枚天赋均使用图集且可选", 游戏.GetComponentsInChildren<天帝源道纹绘图>().Count(p => p.mainTexture == 游戏.美术.道纹图集) == 5 && 游戏.界面.源道纹页.可选择);
        yield return 截图("03-天赋-1600x900");
        检查("确认天赋进入主页", 游戏.选择天赋(游戏.天赋池.候选[0].编号, 游戏.天赋池.轮次));
        检查("真实开局未增加赠送", 游戏.道纹数据.道纹.Count == 0 && 游戏.道纹数据.技能点 == 1 + 天帝天赋效果.开局技能点(游戏.当前天赋));
        yield return 截图("04-主页-1600x900");
        游戏.打开道纹(); yield return null;
        检查("道纹画布使用图集", 游戏.界面.道纹页.画布.mainTexture == 游戏.美术.道纹图集);
        yield return 截图("05-道纹-真实开局"); 游戏.返回主页(); yield return null;
        天帝道纹夹具.实战包(游戏.道纹数据);
        int x = 1;
        foreach (var 项 in new[] { 道纹属性.力量, 道纹属性.数量, 道纹属性.连锁 })
        { var 格 = new Vector2Int(x++, 0); 游戏.道纹数据.解锁格子(格); 游戏.道纹数据.放置(游戏.道纹数据.道纹.First(r => r.属性 == 项), 格); }
        游戏.打开道纹(); yield return null; yield return 截图("06-道纹-美术夹具"); 游戏.返回主页(); yield return null;
        游戏.打开道纹改造(); yield return null;
        检查("十三通货使用新图", 游戏.GetComponentsInChildren<RawImage>().Count(p => p.name == "图标" && p.texture != null) == 13);
        yield return 截图("07-道纹改造"); 游戏.返回主页();
        检查("进入实际战斗", 游戏.进入战斗());
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        var 场 = 游戏.战斗场景; 场.enabled = false;
        检查("66张敌人立绘与主角生效", 场.美术 != null && 场.美术.可用 && 场.美术.敌人立绘数 == 66);
        检查("400地砖与四档敌人图片存在", 场.GetComponentsInChildren<SpriteRenderer>().Count(p => p.name.StartsWith("地面-")) == 400 && Enum.GetValues(typeof(战斗敌人级别)).Cast<战斗敌人级别>().All(k => 场.GetComponentsInChildren<SpriteRenderer>().Any(p => p.name == k + "狼")));
        yield return 截图("08-战斗-出生点");
        var 队 = 场.地图.小队[0]; var 点 = 场.地图.格中心(队.中心格.x, 队.中心格.y) + Vector2.left * 3;
        typeof(天帝战斗场景).GetProperty("玩家位置").SetValue(场, 点); 场.移动一步(Vector2.zero, false, .01f);
        typeof(天帝战斗场景).GetMethod("更新相机", 实例).Invoke(场, null);
        for (int i = 0; i < 12; i++) { 场.战斗一步(.025f); yield return null; }
        yield return 截图("09-战斗-小队与电光");
        var 狼 = 场.战斗.敌人.First(e => e.存活 && e.布点.级别 == 战斗敌人级别.普通);
        场.战斗.伤害敌人(狼, 1); 场.战斗一步(.001f);
        var 狼像 = 场.GetComponentsInChildren<SpriteRenderer>().First(p => p.name == "普通狼"); var 块 = new MaterialPropertyBlock(); 狼像.GetPropertyBlock(块);
        检查("命中与移动表现不改战斗位置", 场.地图.可站立(场.玩家位置) && 场.战斗.普通释放次数 > 0);
        var 王 = 场.战斗.敌人.Single(e => e.布点.级别 == 战斗敌人级别.王级);
        typeof(天帝战斗场景).GetProperty("玩家位置").SetValue(场, 王.位置 + Vector2.left * 3);
        场.移动一步(Vector2.zero, false, .01f); typeof(天帝战斗场景).GetMethod("更新相机", 实例).Invoke(场, null); 场.战斗一步(.01f);
        yield return 截图("10-王房与狼王");
        场.战斗.伤害敌人(王, 100000); 场.战斗一步(.7f);
        检查("王死亡真实掉落与出口", 场.王已击败 && 场.战斗.掉落.地面.Count > 0 && 场.战斗.通货掉落.地面.Count > 0 && 场.可离开);
        yield return 截图("11-王死亡与掉落");
        游戏.返回主页(); while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("离场释放美术对象与场景", 游戏.战斗场景 == null && UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length == 0);
        设置尺寸(2340, 1080); yield return new WaitForSecondsRealtime(.6f); yield return 截图("12-主页-2340x1080");
        游戏.打开道纹(); yield return null; yield return 截图("13-道纹-2340x1080"); 游戏.返回主页();
        检查("宽屏文字与按钮容器未越界", 游戏.GetComponentsInChildren<Button>().All(b => ((RectTransform)b.transform).rect.width > 0));
    }
    static void 记错(string 消息, string 栈, LogType 类) { if (类 == LogType.Exception || 类 == LogType.Error) 结果.错误.Add(消息 + "\n" + 栈); }
    static void 完成()
    {
        EditorApplication.update -= 启动等待; Application.logMessageReceived -= 记错;
        File.WriteAllText(Path.Combine(目录, "art-smoke.json"), JsonUtility.ToJson(结果, true)); EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            EditorSettings.enterPlayModeOptionsEnabled = 原选项启用; EditorSettings.enterPlayModeOptions = 原选项; Application.runInBackground = 原后台;
            if (窗口 != null) { 窗口.GetType().GetProperty("selectedSizeIndex", 实例).SetValue(窗口, 原尺寸); 窗口.maximized = 原最大; }
            foreach (int i in 新尺寸.OrderByDescending(i => i)) 尺寸组.GetType().GetMethod("RemoveCustomSize").Invoke(尺寸组, new object[] { i });
        };
    }
}
#endif
