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

// 覆盖统一属性道纹与默认扔石头的模型契约、真实UI及场景进出。
public static class 天帝属性道纹验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录;
    static 天帝游戏 游戏;
    static EditorWindow 窗口;
    static bool 原最大, 原后台, 原启用, 已完成;
    static int 原尺寸;
    static EnterPlayModeOptions 原模式;
    static double 截止;
    static readonly BindingFlags 反射 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static 天帝道纹 新网() => new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
    static 道纹实例 加纹(天帝道纹 网, 道纹属性 属性, int 数 = 1, int 口 = 9)
    {
        var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(42));
        纹.词条.Clear(); 纹.词条.Add(new 道纹词条(属性, 数)); 纹.接口 = 口;
        if (!网.获得道纹(纹)) throw new InvalidOperationException("验证夹具入库失败"); return 纹;
    }
    static void 模型()
    {
        检查("属性、中心天赋与独立分叉分类", Enum.GetNames(typeof(道纹分类)).SequenceEqual(new[] { "属性", "天赋", "分叉" }));
        检查("基础属性仅含力量速度智力", 天帝道纹属性.基础属性.SequenceEqual(new[] { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力 }) &&
            天帝道纹属性.基础属性.All(x => 天帝道纹属性.分组(x) == 道纹属性分组.基础));
        检查("普通属性包含血量防御护盾攻速移速", new[] { 道纹属性.血量, 道纹属性.防御, 道纹属性.护盾, 道纹属性.攻速, 道纹属性.移速 }.All(x =>
            天帝道纹属性.普通属性.Contains(x) && 天帝道纹属性.分组(x) == 道纹属性分组.普通));
        检查("旧存档可读取九种历史元素", Enum.GetNames(typeof(道纹属性)).Skip(10).SequenceEqual(new[] { "金", "木", "水", "火", "土", "冰", "雷", "时间", "空间" }));
        var 新词条随机 = new System.Random(20261004);
        var 新词条 = Enumerable.Range(0, 1000).Select(_ => 天帝道纹属性.抽词条(新词条随机).属性).ToArray();
        检查("新生成词条只含三基础六普通五形态与金木水火土", 新词条.All(x => 天帝道纹属性.当前属性.Contains(x))
            && new[] { 道纹属性.金, 道纹属性.木, 道纹属性.水, 道纹属性.火, 道纹属性.土 }.All(新词条.Contains));
        foreach (var 天赋 in 天帝天赋.全部)
        {
            var 网 = new 天帝道纹(42, 天赋); var 钱 = new 天帝通货(网, 42);
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                检查(天赋.名称 + "开局空候选零通货1级点数准确", 网.道纹.Count == 0 && 网.玩家等级 == 1 && 网.技能点 == 1 + 天帝天赋效果.开局技能点(天赋) && 网.已解锁格数 == 1 && 天帝通货.定义.All(x => 钱.数量(x.种类) == 0));
                检查(天赋.名称 + "无需道纹即可投石", 普攻参数.读取(网, 人).已激活);
            }
        }
        var 空 = 新网();
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 空))
        {
            var p = 普攻参数.读取(空, 人);
            检查("普通人裸装基础伤害1且单颗无形态", 人.攻击力 == 1 && p.伤害 == 1 && p.数量 == 1 && p.分裂 == 0 && p.连锁 == 0 && p.转向角度 == 0 && p.溅射半径 == 0);
            foreach (道纹属性 元 in Enum.GetValues(typeof(道纹属性)).Cast<道纹属性>().Skip(10))
            {
                var 纹 = 加纹(空, 元, 3, 8); 空.放置(纹, new Vector2Int(10, 10));
                检查(元 + "锁格不可放置", !纹.格子.HasValue);
                空.解锁格子(new Vector2Int(1, 0)); 空.放置(纹, new Vector2Int(1, 0));
                bool 五行 = 天帝道纹属性.是五行(元);
                var 参数 = 普攻参数.读取(空, 人);
                检查(元 + "接通后按五行规则增加伤害", 纹.生效 && 空.生效加成[(int)元] == 3 &&
                    参数.普通伤害 == 1 && 参数.五行额外伤害 == (五行 ? 3 : 0) && 参数.伤害 == (五行 ? 4 : 1) && 参数.连锁 == 0);
                空.旋转(纹);
                检查(元 + "旋转断链撤销额外伤害但保留普攻", !纹.生效 && 空.生效加成[(int)元] == 0 &&
                    普攻参数.读取(空, 人).伤害 == 1 && 普攻参数.读取(空, 人).已激活);
                空.收回(纹);
            }
        }
        var 图 = 新网(); 图.设置玩家等级(30);
        var 力 = 加纹(图, 道纹属性.力量, 2); var 量 = 加纹(图, 道纹属性.数量); var 链 = 加纹(图, 道纹属性.连锁);
        var 火 = 加纹(图, 道纹属性.火, 3, 8); typeof(道纹实例).GetProperty("品阶").SetValue(火, 道纹品阶.杰出); 火.词条.Add(new 道纹词条(道纹属性.火, 2));
        for (int x = 1; x <= 4; x++) 图.解锁格子(new Vector2Int(x, 0));
        foreach (var 项 in new[] { 力, 量, 链, 火 }.Select((纹, i) => new { 纹, 格 = new Vector2Int(i + 1, 0) })) 图.放置(项.纹, 项.格);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 图))
        {
            var p = 普攻参数.读取(图, 人);
            检查("旧版混合道纹重复五行词条相加", p.普通伤害 == 4 && p.五行额外伤害 == 5 && p.伤害 == 9 && p.数量 == 2 && p.连锁 == 1 && 图.生效加成[(int)道纹属性.火] == 5);
            for (int i = 0; i < 10; i++) 图.重算();
            检查("重复重算不叠加", 人.攻击力 == 4 && 图.生效数 == 4 && 图.生效加成[(int)道纹属性.火] == 5);
            图.收回(量);
            检查("卸载上游撤销下游元素形态保留投石", 普攻参数.读取(图, 人).已激活 && 普攻参数.读取(图, 人).数量 == 1 && 普攻参数.读取(图, 人).连锁 == 0 && 图.生效加成[(int)道纹属性.火] == 0);
            图.放置(量, new Vector2Int(2, 0));
        }
        var 钱袋 = new 天帝通货(图, 123, 8); int 旧口 = 火.接口;
        检查("元素升阶保留已有词条接口并扣1", 钱袋.使用(通货种类.蕴玄石, 火, 0, out _) && 火.词条[0].属性 == 道纹属性.火 && 火.数值 == 3 && 火.接口 == 旧口 && 钱袋.数量(通货种类.蕴玄石) == 7);
        检查("元素可追加一条统一属性词条", 钱袋.使用(通货种类.添蕴砂, 火, 0, out _) && 火.词条.Count == 3);
        检查("属性满容量拒绝添词不扣通货", !钱袋.使用(通货种类.添蕴砂, 火, 0, out _) && 钱袋.数量(通货种类.添蕴砂) == 7);
        int 旧条 = 火.词条.Count;
        检查("统一池洗练保条数接口品阶", 钱袋.使用(通货种类.重铸石, 火, 0, out _) && 火.词条.Count == 旧条 && 火.接口 == 旧口 && 火.品阶 == 道纹品阶.稀有);
        检查("中心改造拒绝", !钱袋.使用(通货种类.重铸石, 图.已放置[Vector2Int.zero], 0, out _));
        var 覆盖 = new HashSet<道纹属性>(); bool 合法 = true;
        for (int 阶 = 0; 阶 < 8; 阶++) for (int seed = 0; seed < 100; seed++)
        {
            var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, (道纹品阶)阶, new System.Random(seed));
            var 定 = 天帝道纹品阶.获取(纹.品阶);
            合法 &= (纹.是功能道纹 ? 纹.品阶 == 道纹品阶.稀有 && 纹.词条.Count == 1 && 纹.词条上限 == 1 && 天帝道纹属性.是功能(纹.属性) :
                纹.是五行道纹 ? 纹.词条.Count == 1 && 纹.词条上限 == 1 :
                纹.词条.Count >= 定.最少词条 && 纹.词条.Count <= 定.最多词条 &&
                纹.词条.All(x => !天帝道纹属性.是五行(x.属性))) && 纹.接口 >= 1 && 纹.接口 <= 63;
            foreach (var 词 in 纹.词条) 覆盖.Add(词.属性);
        }
        检查("八阶属性与三种固定稀有功能覆盖十七词条", 合法 && 覆盖.Count == 17);
        var 单五行网 = 新网(); var 单五行 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.杰出, new System.Random(7), 道纹属性分组.元素);
        单五行网.获得道纹(单五行); var 单五行通货 = new 天帝通货(单五行网, 7, 2);
        检查("五行升阶洗练不增加词条且不能添词", 单五行通货.使用(通货种类.蕴玄石, 单五行, 0, out _) &&
            单五行通货.使用(通货种类.易纹砂, 单五行, 0, out _) && 单五行.是五行道纹 && 单五行.词条.Count == 1 &&
            !单五行通货.使用(通货种类.添蕴砂, 单五行, 0, out _));
        检查("高阶单词条五行可存档回读", 天帝道纹.读取存档(单五行网.导出存档()).道纹.Single().是五行道纹);
        var 地图 = new 天帝战斗地图(42); var 裸网 = 新网();
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 裸网))
        {
            var 战 = new 天帝战斗系统(地图, 裸网, 人, 战斗难度.普通, new 天帝通货(裸网, 42));
            var 敌列 = (List<战斗敌人>)战.敌人; 敌列.Clear();
            var 玩家 = 地图.格中心(地图.小队[0].中心格.x, 地图.小队[0].中心格.y);
            var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 玩家 + Vector2.right * 3), 战斗难度.普通); 敌列.Add(敌);
            for (int i = 0; i < 16; i++) 战.推进(玩家, .025f);
            检查("裸装实际自动命中普通怪扣1血", 战.普通释放次数 == 1 && 敌.血量 == 敌.最大血量 - 1 && 敌.显示血条);
            检查("无默认雷电余辉及范围连锁伤害", 战.电弧.Count == 0 && 战.溅射命中数 == 0 && 战.连锁发生数 == 0);
            var 王 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.王级, 玩家), 战斗难度.普通); 敌列.Add(王);
            战.伤害敌人(王, 10000); int 落 = 战.掉落.地面.Count; 战.伤害敌人(王, 10000);
            检查("真实死亡只结算一次且王掉合法道纹", 战.掉落.地面.Count == 落 && 落 == 1 &&
                (战.掉落.地面[0].道纹.分类 == 道纹分类.属性 || 战.掉落.地面[0].道纹.分类 == 道纹分类.分叉));
            战.推进(玩家, .025f);
            检查("实际拾取进入会话库存", 裸网.道纹.Count == 1 && 战.掉落.拾取数 == 1 && 战.通货掉落.拾取总量 == 4);
        }
        var 五行网 = 新网(); var 金 = 加纹(五行网, 道纹属性.金, 3, 8);
        五行网.解锁格子(new Vector2Int(1, 0)); 五行网.放置(金, new Vector2Int(1, 0));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 五行网))
        {
            var 战 = new 天帝战斗系统(地图, 五行网, 人, 战斗难度.普通);
            var 敌列 = (List<战斗敌人>)战.敌人; 敌列.Clear();
            var 玩家 = 地图.格中心(地图.小队[0].中心格.x, 地图.小队[0].中心格.y);
            var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 玩家 + Vector2.right * 3), 战斗难度.普通); 敌列.Add(敌);
            for (int i = 0; i < 16; i++) 战.推进(玩家, .025f);
            检查("五行真实灵力弹命中扣除普通加额外伤害", 战.普通释放次数 == 1 && 敌.血量 == 敌.最大血量 - 4);
        }
    }
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("先停止并保存当前场景。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/属性道纹-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        结果 = new 报告(); 游戏 = null; 已完成 = false; 模型();
        原后台 = Application.runInBackground; 原启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        窗口 = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); 原最大 = 窗口.maximized; 窗口.maximized = true;
        原尺寸 = (int)窗口.GetType().GetProperty("selectedSizeIndex", 反射).GetValue(窗口);
        设置尺寸(1600, 900); 窗口.Focus();
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        截止 = EditorApplication.timeSinceStartup + 100; Application.logMessageReceived += 记错; EditorApplication.update += 等待; EditorApplication.isPlaying = true; return 目录;
    }
    static void 设置尺寸(int 宽, int 高)
    {
        var a = typeof(Editor).Assembly; var t = a.GetType("UnityEditor.GameViewSizes");
        var 单 = typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        var 组 = t.GetMethod("GetGroup").Invoke(单, new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeGroupType"), 0) });
        var 类 = a.GetType("UnityEditor.GameViewSize");
        var 大小 = 类.GetConstructor(反射, null, new[] { a.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }, null)
            .Invoke(new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"), 1), (object)宽, 高, "属性道纹检查" });
        组.GetType().GetMethod("AddCustomSize").Invoke(组, new[] { 大小 });
        int 索引 = (int)组.GetType().GetMethod("GetBuiltinCount").Invoke(组, null) + (int)组.GetType().GetMethod("GetCustomCount").Invoke(组, null) - 1;
        窗口.GetType().GetProperty("selectedSizeIndex", 反射).SetValue(窗口, 索引);
    }
    static void 等待()
    {
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("验证超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (游戏 != null) return;
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (游戏 != null) 游戏.StartCoroutine(保护(界面流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var 栈 = new Stack<IEnumerator>(); 栈.Push(流);
        while (栈.Count > 0)
        {
            bool 有; object 项 = null;
            try { 有 = 栈.Peek().MoveNext(); if (有) 项 = 栈.Peek().Current; }
            catch (Exception ex) { 结果.错误.Add(ex.ToString()); break; }
            if (!有) { 栈.Pop(); continue; }
            if (项 is IEnumerator 子) { 栈.Push(子); continue; } yield return 项;
        }
        完成();
    }
    static void 点(string 名)
    {
        Canvas.ForceUpdateCanvases(); var b = 游戏.GetComponentsInChildren<Button>().Single(x => x.name == 名);
        var r = (RectTransform)b.transform; var p = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { position = p }, ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 截图(string 名)
    {
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(目录, 名 + ".png"), t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
    }
    static IEnumerator 界面流程()
    {
        yield return new WaitForSecondsRealtime(.5f); 点("开始游戏"); yield return null; 点("跳过序章");
        while (!游戏.界面.源道纹页.可选择) yield return null;
        int 刷 = 0; while (!游戏.天赋池.候选.Any(x => x.种类 == 天赋种类.普通人) && 刷++ < 100) 游戏.刷新天赋();
        点("源道纹-" + 游戏.天赋池.候选.ToList().FindIndex(x => x.种类 == 天赋种类.普通人)); 点("确认天赋"); yield return null;
        检查("主页显示初始灵力评语与有效伤害", 游戏.GetComponentsInChildren<Text>().Any(x => x.text == 天帝实力评语.初始评语) && 游戏.GetComponentsInChildren<Text>().Any(x => x.text.Contains("灵力伤害 1")));
        yield return 截图("01-主页-1600x900"); 点("进入战斗");
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("空画布可以进入战斗且默认伤害1", 游戏.阶段 == 游戏阶段.战斗 && 游戏.战斗场景.战斗.当前普攻.伤害 == 1);
        检查("PC无摇杆且HUD标明灵力弹", 游戏.GetComponentInChildren<天帝移动摇杆>() == null && 游戏.GetComponentsInChildren<Text>().Any(x => x.text.StartsWith("灵力弹 · 基础1")));
        var 场 = 游戏.战斗场景; 场.enabled = false;
        var 营 = 场.地图.小队[0].中心格; var 目标 = 场.地图.格中心(营.x, 营.y) + Vector2.left * 3;
        typeof(天帝战斗场景).GetProperty("玩家位置").SetValue(场, 目标);
        for (int i = 0; i < 5; i++) 场.战斗一步(.025f);
        场.移动一步(Vector2.zero, false, .025f);
        yield return 截图("02-战斗默认投石");
        游戏.战斗场景.enabled = false; 游戏.战斗场景.战斗.伤害玩家(10000); 游戏.返回主页();
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("重入主页不补发道纹与点", 游戏.道纹数据.道纹.Count == 0 && 游戏.道纹数据.技能点 == 1);
        游戏.打开道纹(); yield return null;
        检查("空候选画布不报错且无技能筛选", 游戏.界面.道纹页.筛选结果数 == 0 && !游戏.GetComponentsInChildren<Text>().Any(x => x.text.Contains("技能类")));
        yield return 截图("03-空画布"); 游戏.返回主页();
        游戏.道纹数据.设置玩家等级(25);
        foreach (道纹属性 项 in Enum.GetValues(typeof(道纹属性))) 加纹(游戏.道纹数据, 项, 1);
        游戏.打开道纹(); yield return null; var 页 = 游戏.界面.道纹页;
        页.设置候选分组(道纹属性分组.元素);
        检查("元素筛选显示九枚且支持分页", 页.筛选结果数 == 9 && 页.候选总页数 == 2 && 页.候选显示项(0).属性 == 道纹属性.金);
        页.切换候选页(1); 检查("元素第二页有空间文字", 游戏.GetComponentsInChildren<Text>().Any(x => x.text == "空间"));
        页.切换候选页(0); var 火 = 游戏.道纹数据.道纹.Single(x => x.属性 == 道纹属性.火); int 序 = 游戏.道纹数据.道纹.IndexOf(火);
        var 终 = 页.格屏幕位置(new Vector2Int(1, 0)); var 起 = 页.候选屏幕位置(序);
        var e = new PointerEventData(EventSystem.current) { position = 终, button = PointerEventData.InputButton.Left };
        页.点击(true, e); e.position = 起; 页.按下(火, false, e); 页.开始拖动(false, e); e.position = 终; 页.拖动(e);
        检查("元素拖拽可放格绿色反馈", 页.放置反馈 == true); 页.结束拖动(e);
        检查("实际放置元素接通高光并汇总", 火.生效 && 游戏.道纹数据.生效加成[(int)道纹属性.火] == 1);
        foreach (var 纹 in 游戏.道纹数据.道纹.Where(x => x != 火))
        {
            int x = (int)纹.属性 + 2; var 格 = new Vector2Int(x, 0); 游戏.道纹数据.解锁格子(格); 游戏.道纹数据.放置(纹, 格);
        }
        // 排版最满情形：只为截图夹具连通全部词条，不作为正式赠送。
        foreach (var 纹 in 游戏.道纹数据.道纹) 纹.接口 = 63;
        var 全格 = 游戏.道纹数据.道纹.Select((纹, i) => new { 纹, 格 = new Vector2Int(i + 1, 0) }).ToArray();
        foreach (var 项 in 全格) { 游戏.道纹数据.解锁格子(项.格); 游戏.道纹数据.放置(项.纹, 项.格); }
        页.画布.平移 = new Vector2(-450, 0); 页.刷新();
        Canvas.ForceUpdateCanvases();
        检查("全十九词条汇总高度可容纳", 游戏.GetComponentsInChildren<Text>().Where(x => x.text.StartsWith("接通 ")).All(x => x.preferredHeight <= x.rectTransform.rect.height));
        yield return 截图("04-元素画布-1600x900");
        设置尺寸(1280, 720); yield return new WaitForSecondsRealtime(.3f); yield return 截图("05-元素画布-1280x720");
        游戏.返回主页(); yield return null; yield return 截图("06-主页-1280x720");
        游戏.通货数据.获得(通货种类.易纹砂, 1); 游戏.打开道纹改造(); yield return null;
        var 改 = 游戏.GetComponentInChildren<天帝通货界面>(); 改.选目标(序);
        检查("元素可以进入实际改造页", 游戏.GetComponentsInChildren<Text>().Any(x => x.text == "火道纹")); yield return 截图("07-元素改造");
    }
    static void 记错(string 文, string 栈, LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 结果.错误.Add(文 + "\n" + 栈); }
    static void 完成()
    {
        if (已完成) return; 已完成 = true; EditorApplication.update -= 等待; Application.logMessageReceived -= 记错;
        File.WriteAllText(Path.Combine(目录, "attribute-smoke.json"), JsonUtility.ToJson(结果, true)); EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => { EditorSettings.enterPlayModeOptionsEnabled = 原启用; EditorSettings.enterPlayModeOptions = 原模式; Application.runInBackground = 原后台; if (窗口 != null) { 窗口.GetType().GetProperty("selectedSizeIndex", 反射).SetValue(窗口, 原尺寸); 窗口.maximized = 原最大; } 游戏 = null; };
    }
}
#endif
