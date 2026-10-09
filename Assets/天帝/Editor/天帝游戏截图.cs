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
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 仅供商店素材取景；演示库存只存在于本次PlayMode，不进入正式开局。
public static class 天帝游戏截图
{
    [Serializable] sealed class 报告
    {
        public string 说明 = "当前原型真实GameView。编辑器临时配置30级连接构筑及少量改造资源；寻路、战斗与掉落均走现有规则，无额外宣传合成或元素特效。";
        public List<string> 图片 = new List<string>(), 错误 = new List<string>();
        public int 生效道纹, 击败, 分裂, 连锁, 道纹拾取, 通货拾取;
        public bool 狼王击败, 编辑器已恢复;
    }
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static 天帝游戏 游戏;
    static string 目录;
    static 报告 结果;
    static EditorWindow 窗口;
    static SceneSetup[] 原场景;
    static bool 原最大, 原后台, 原启用, 已结束;
    static int 原尺寸, 原帧率, 自定义索引;
    static object 尺寸组;
    static EnterPlayModeOptions 原模式;
    static double 截止;
    static float 刷图评分 = -1, 王评分 = -1;
    static readonly System.Random 随机 = new System.Random(20261004);
    const string 刷图图 = "02_多发投石与连锁.jpg", 王图 = "03_青鬃狼王战斗.jpg";

    [MenuItem("天帝/素材/拍摄游戏截图")]
    public static void 菜单() => 启动();
    public static string 启动()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorSceneManager.GetSceneManagerSetup().Any(x => UnityEngine.SceneManagement.SceneManager.GetSceneByPath(x.path).isDirty))
            throw new InvalidOperationException("需停止运行并保存场景后截图。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/宣传素材/游戏截图-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录); 结果 = new 报告(); 游戏 = null; 已结束 = false; 刷图评分 = 王评分 = -1;
        原场景 = EditorSceneManager.GetSceneManagerSetup(); 原后台 = Application.runInBackground; 原帧率 = Time.captureFramerate;
        原启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        窗口 = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        原最大 = 窗口.maximized; 原尺寸 = (int)窗口.GetType().GetProperty("selectedSizeIndex", 隐).GetValue(窗口);
        设置尺寸(); 窗口.maximized = true; 窗口.Focus();
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        截止 = EditorApplication.timeSinceStartup + 240;
        Application.logMessageReceived += 日志; EditorApplication.update += 等待; EditorApplication.isPlaying = true;
        return 目录;
    }
    static void 设置尺寸()
    {
        var a = typeof(Editor).Assembly; var t = a.GetType("UnityEditor.GameViewSizes");
        var 单例 = typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        尺寸组 = t.GetMethod("GetGroup").Invoke(单例, new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeGroupType"), 0) });
        自定义索引 = (int)尺寸组.GetType().GetMethod("GetCustomCount").Invoke(尺寸组, null);
        var 类 = a.GetType("UnityEditor.GameViewSize");
        var 大小 = 类.GetConstructor(隐, null, new[] { a.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }, null)
            .Invoke(new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"), 1), (object)1920, 1080, "天帝商店截图" });
        尺寸组.GetType().GetMethod("AddCustomSize").Invoke(尺寸组, new[] { 大小 });
        int 索引 = (int)尺寸组.GetType().GetMethod("GetBuiltinCount").Invoke(尺寸组, null) + 自定义索引;
        窗口.GetType().GetProperty("selectedSizeIndex", 隐).SetValue(窗口, 索引);
    }
    static void 等待()
    {
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("截图超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (游戏 != null) return;
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 != null) 游戏.StartCoroutine(保护(流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            object 项 = null; bool 有;
            try { 有 = 栈.Peek().MoveNext(); if (有) 项 = 栈.Peek().Current; }
            catch (Exception ex) { 结果.错误.Add(ex.ToString()); break; }
            if (!有) { 栈.Pop(); continue; }
            if (项 is IEnumerator 子) { 栈.Push(子); continue; }
            yield return 项;
        }
        完成();
    }
    static void 点(string 名)
    {
        Canvas.ForceUpdateCanvases();
        var 按钮列 = 游戏.GetComponentsInChildren<Button>();
        var b = 按钮列.FirstOrDefault(x => x != null && x.name == 名);
        // 正式主页会根据是否有可继续存档在“开始游戏/继续游戏”之间切换节点名；
        // 取景夹具优先按语义回退，避免存档状态让截图流程在第一步中断。
        if (b == null && 名 == "开始游戏") b = 按钮列.FirstOrDefault(x => x != null && (x.name == "继续游戏" || x.GetComponentInChildren<Text>()?.text == "继续游戏"));
        if (b == null && 名 == "进入战斗") b = 按钮列.FirstOrDefault(x => x != null && (x.name == "开始游戏" || x.GetComponentInChildren<Text>()?.text == "开始游戏"));
        if (b == null && 名 == "跳过序章" && 游戏.阶段 != 游戏阶段.序章)
            return; // 当前正式入口已越过序章时，跳过按钮不会创建。
        if (b == null) throw new InvalidOperationException("找不到按钮：" + 名);
        if (!b.interactable) throw new InvalidOperationException("按钮不可用：" + 名);
        b.onClick.Invoke();
    }
    static IEnumerator 拍(string 名)
    {
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var 图 = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            if (图.width != 1920 || 图.height != 1080) throw new InvalidOperationException("GameView尺寸未生效：" + 图.width + "x" + 图.height);
            var 字节 = 图.EncodeToJPG(97);
            if (字节.Length >= 4 * 1024 * 1024) throw new InvalidOperationException("截图超过4MB");
            File.WriteAllBytes(Path.Combine(目录, 名), 字节);
            if (!结果.图片.Contains(名)) 结果.图片.Add(名);
        }
        finally { UnityEngine.Object.Destroy(图); }
    }
    static 道纹实例 造(道纹属性 属性, int 数, 道纹品阶 阶, int 口)
    {
        var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 阶, 随机);
        纹.词条.Clear(); 纹.词条.Add(new 道纹词条(属性, 数));
        for (int i = 1; i < 天帝道纹品阶.获取(阶).最少词条; i++) 纹.词条.Add(new 道纹词条(道纹属性.智力, 2));
        纹.接口 = 口;
        if (!游戏.道纹数据.获得道纹(纹)) throw new InvalidOperationException("临时道纹入库失败");
        return 纹;
    }
    static void 构筑()
    {
        游戏.道纹数据.设置玩家等级(30);
        foreach (var a in new[] { 道纹属性.雷, 道纹属性.金, 道纹属性.木, 道纹属性.水, 道纹属性.冰, 道纹属性.时间, 道纹属性.空间, 道纹属性.力量 })
            造(a, 2, (道纹品阶)(1 + ((int)a % 4)), 9 | (1 << ((int)a % 6)));
        var 格 = new[] { new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
            new Vector2Int(2,1), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2),
            new Vector2Int(4,-1), new Vector2Int(4,-2), new Vector2Int(5,-2), new Vector2Int(6,-3),
            new Vector2Int(1,-1), new Vector2Int(1,-2), new Vector2Int(2,-2), new Vector2Int(2,-3), new Vector2Int(3,-3),
            new Vector2Int(0,-1), new Vector2Int(-1,-1), new Vector2Int(-2,0) };
        var 属性 = new[] { 道纹属性.力量, 道纹属性.数量, 道纹属性.连锁, 道纹属性.分裂,
            道纹属性.速度, 道纹属性.数量, 道纹属性.连锁, 道纹属性.范围,
            道纹属性.数量, 道纹属性.弧度, 道纹属性.力量, 道纹属性.范围,
            道纹属性.智力, 道纹属性.分裂, 道纹属性.连锁, 道纹属性.血量, 道纹属性.速度,
            道纹属性.灵力, 道纹属性.力量, 道纹属性.火 };
        int[] 数 = { 3,2,2,1,3,1,1,2,2,2,3,2,3,2,1,3,3,3,3,3 };
        int[] 父 = { -1,0,1,2,1,4,5,6,2,8,9,10,-1,12,13,14,15,-1,17,18 };
        var 纹列 = new List<道纹实例>();
        for (int i = 0; i < 格.Length; i++) 纹列.Add(造(属性[i], 数[i], (道纹品阶)(i == 11 ? 4 : i % 4), 0));
        for (int i = 0; i < 格.Length; i++)
        {
            var 差 = 格[i] - (父[i] < 0 ? Vector2Int.zero : 格[父[i]]);
            int d = Array.IndexOf(天帝道纹.邻向, 差);
            if (d < 0) throw new InvalidOperationException("连接并非邻格");
            纹列[i].接口 |= 1 << ((d + 3) % 6);
            if (父[i] >= 0) 纹列[父[i]].接口 |= 1 << d;
        }
        for (int i = 0; i < 格.Length; i++)
        {
            游戏.道纹数据.解锁格子(格[i]); 游戏.道纹数据.放置(纹列[i], 格[i]);
            if (!纹列[i].生效) throw new InvalidOperationException("演示连接不生效：" + 格[i]);
        }
        var 断 = 造(道纹属性.空间, 2, 道纹品阶.杰出, 1);
        游戏.道纹数据.解锁格子(new Vector2Int(-3,2)); 游戏.道纹数据.放置(断, new Vector2Int(-3,2));
        结果.生效道纹 = 游戏.道纹数据.生效数;
        foreach (var k in new[] { 通货种类.启灵石, 通货种类.点玄石, 通货种类.添蕴砂, 通货种类.易纹砂, 通货种类.重铸石 }) 游戏.通货数据.获得(k, 3);
    }
    static void 推进(Vector2 方向)
    {
        var 场 = 游戏.战斗场景; 场.移动一步(方向, true, 1f / 30); 场.战斗一步(1f / 30);
        typeof(天帝战斗场景).GetMethod("更新相机", 隐).Invoke(场, null);
        if (场.战斗.玩家死亡) throw new InvalidOperationException("截图实战角色死亡");
    }
    static IEnumerator 挑战斗帧(bool 拍王)
    {
        var 场 = 游戏.战斗场景;
        int 附近 = 场.战斗.敌人.Count(x => x.存活 && Vector2.Distance(x.位置, 场.玩家位置) < 14);
        int 血条 = 场.战斗.敌人.Count(x => x.显示血条 && Vector2.Distance(x.位置, 场.玩家位置) < 14);
        if (拍王)
        {
            var 王 = 场.战斗.敌人.Single(x => x.布点.级别 == 战斗敌人级别.王级);
            if (!王.存活 || !王.显示血条 || 王.闪白秒 > 0 || Vector2.Distance(王.位置, 场.玩家位置) > 14) yield break;
            float 分 = 场.战斗.灵矢.Count * 2 + (王.行动 == 敌人行动.蓄力 ? 12 : 0) + 5 * (1 - 王.血量 / 王.最大血量);
            if (分 <= 王评分) yield break; 王评分 = 分; yield return 拍(王图);
        }
        else
        {
            int 闪白 = 场.战斗.敌人.Count(x => x.存活 && x.闪白秒 > 0 && Vector2.Distance(x.位置, 场.玩家位置) < 14);
            if (附近 < 3 || 血条 < 1 || 闪白 > 1 || 场.战斗.灵矢.Count < 3) yield break;
            float 分 = 附近 + 血条 * 3 + Math.Min(24, 场.战斗.灵矢.Count) * 2;
            if (分 <= 刷图评分) yield break; 刷图评分 = 分; yield return 拍(刷图图);
        }
    }
    static IEnumerator 走到(Vector2 终, bool 拍王 = false)
    {
        var 场 = 游戏.战斗场景; var 路 = new List<Vector2>();
        if (!new 天帝战斗寻路(场.地图).路径(场.玩家位置, 终, 路, false, false)) throw new InvalidOperationException("取景路线不可达");
        int 步 = 0;
        foreach (var p in 路) while (Vector2.Distance(场.玩家位置, p) > .35f)
        {
            if (++步 > 2200) throw new InvalidOperationException("取景移动卡住");
            推进((p - 场.玩家位置).normalized); yield return 挑战斗帧(拍王); yield return null;
        }
    }
    static IEnumerator 流程()
    {
        yield return new WaitForSecondsRealtime(.7f);
        var 输入 = 游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        点("开始游戏"); yield return null;
        bool 已有主页 = 游戏.阶段 == 游戏阶段.主页;
        if (!已有主页)
        {
            if (游戏.界面.确认已打开) 点("开始新游戏");
            yield return null; 点("跳过序章");
            while (游戏.界面 == null || 游戏.界面.源道纹页 == null || !游戏.界面.源道纹页.可选择) yield return null;
            int 次 = 0; while (!游戏.天赋池.候选.Any(x => x.种类 == 天赋种类.普通人) && 次++ < 100) 游戏.刷新天赋();
            点("源道纹-" + 游戏.天赋池.候选.ToList().FindIndex(x => x.种类 == 天赋种类.普通人)); 点("确认天赋");
            yield return new WaitForSecondsRealtime(.6f);
        }
        yield return 拍("06_主页与主角.jpg");
        Time.captureFramerate = 30; 构筑(); 游戏.打开道纹(); yield return null;
        var 页 = 游戏.界面.道纹页; 页.画布.缩放 = 1.15f; 页.画布.平移 = new Vector2(-130, 32); 页.刷新(); 页.指针离开();
        yield return 拍("01_道纹分支构筑.jpg"); 游戏.返回主页(); 点("进入战斗");
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        var 场 = 游戏.战斗场景; 场.enabled = false;
        var 营地 = 场.地图.小队.OrderBy(x => Vector2.Distance(场.玩家位置, 场.地图.格中心(x.中心格.x, x.中心格.y))).Take(2).ToArray();
        foreach (var 队 in 营地)
        {
            yield return 走到(场.地图.格中心(队.中心格.x, 队.中心格.y));
            for (int i = 0; i < 100; i++) { 推进(Vector2.zero); yield return 挑战斗帧(false); yield return null; }
            foreach (var 落 in 场.战斗.掉落.地面.Where(x => !x.已拾取 && Vector2.Distance(x.位置, 场.玩家位置) < 10).ToArray()) yield return 走到(落.位置);
            foreach (var 落 in 场.战斗.通货掉落.地面.Where(x => !x.已拾取 && Vector2.Distance(x.位置, 场.玩家位置) < 10).ToArray()) yield return 走到(落.位置);
        }
        yield return 走到(场.地图.格中心(场.地图.王房入口格.x, 场.地图.王房入口格.y));
        var 王 = 场.战斗.敌人.Single(x => x.布点.级别 == 战斗敌人级别.王级);
        yield return 走到(场.地图.王位置 + Vector2.down * 6, true);
        for (int i = 0; i < 1000 && 王.存活; i++)
        {
            var 差 = 场.玩家位置 - 王.位置; 推进(差.magnitude < 4 ? 差.normalized : Vector2.zero);
            yield return 挑战斗帧(true); yield return null;
        }
        if (!场.王已击败) throw new InvalidOperationException("狼王未击败");
        for (int i = 0; i < 20; i++) { 推进(Vector2.zero); yield return null; }
        yield return 拍("04_狼王掉落与收获.jpg");
        foreach (var 落 in 场.战斗.掉落.地面.Where(x => !x.已拾取 && Vector2.Distance(x.位置, 场.玩家位置) < 14).ToArray()) yield return 走到(落.位置, true);
        foreach (var 落 in 场.战斗.通货掉落.地面.Where(x => !x.已拾取 && Vector2.Distance(x.位置, 场.玩家位置) < 14).ToArray()) yield return 走到(落.位置, true);
        结果.狼王击败 = 场.王已击败; 结果.击败 = 场.战斗.击败数; 结果.分裂 = 场.战斗.分裂生成数; 结果.连锁 = 场.战斗.连锁发生数;
        结果.道纹拾取 = 场.战斗.掉落.拾取数; 结果.通货拾取 = 场.战斗.通货掉落.拾取总量;
        游戏.返回主页(); while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        游戏.打开道纹改造(); yield return null;
        var 改 = 游戏.GetComponentInChildren<天帝通货界面>(); 改.选目标(0); 改.选通货(通货种类.启灵石); 改.执行();
        yield return 拍("05_通货与道纹改造.jpg");
        if (结果.图片.Count != 6) throw new InvalidOperationException("缺少合格战斗取景");
    }
    static void 日志(string 文, string 栈, LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 结果.错误.Add(文 + "\n" + 栈); }
    static void 完成()
    {
        if (已结束) return; 已结束 = true; EditorApplication.update -= 等待;
        Time.captureFramerate = 原帧率; File.WriteAllText(Path.Combine(目录, "截图报告.json"), JsonUtility.ToJson(结果, true));
        EditorApplication.isPlaying = false; EditorApplication.update += 恢复;
    }
    static void 恢复()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= 恢复; Application.logMessageReceived -= 日志;
        Application.runInBackground = 原后台; EditorSettings.enterPlayModeOptionsEnabled = 原启用; EditorSettings.enterPlayModeOptions = 原模式;
        if (窗口 != null)
        {
            窗口.GetType().GetProperty("selectedSizeIndex", 隐).SetValue(窗口, 原尺寸); 窗口.maximized = 原最大;
            尺寸组.GetType().GetMethod("RemoveCustomSize").Invoke(尺寸组, new object[] { 自定义索引 });
        }
        EditorSceneManager.RestoreSceneManagerSetup(原场景); 游戏 = null; 结果.编辑器已恢复 = true;
        File.WriteAllText(Path.Combine(目录, "截图报告.json"), JsonUtility.ToJson(结果, true));
    }
}
#endif
