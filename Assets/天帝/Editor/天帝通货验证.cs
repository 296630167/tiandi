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

public static class 天帝通货验证
{
    [Serializable] sealed class 报告
    { public List<string> 通过 = new List<string>(); public List<string> 失败 = new List<string>(); public List<string> 错误 = new List<string>(); public List<string> 排版诊断 = new List<string>(); }
    static 报告 结果;
    static string 目录;
    static int 步骤;
    static double 下步, 截止;
    static bool 原启用;
    static EnterPlayModeOptions 原模式;
    static Vector2Int 上次尺寸;
    static int 上次帧;
    static void 检查(string 名, bool 成立) => (成立 ? 结果.通过 : 结果.失败).Add(名);
    static int 口数(道纹实例 纹) => Enumerable.Range(0, 6).Count(纹.有接口);
    static void 定阶(道纹实例 纹, 道纹品阶 阶) => typeof(道纹实例).GetProperty("品阶").SetValue(纹, 阶);
    static string 快照(道纹实例 纹) => 纹.品阶 + ":" + 纹.接口 + ":" + 纹.格子 + ":" + string.Join(",", 纹.词条.Select(x => x.属性 + ":" + x.数值));
    static 道纹实例 夹具(天帝道纹 图, 道纹品阶 阶, bool 功能 = false)
    {
        var 纹 = 图.道纹.First(x => x.分类 == 道纹分类.属性);
        定阶(纹, 阶); 纹.接口 = 8; 纹.词条.Clear();
        for (int i = 0; i < 天帝道纹品阶.获取(阶).最少词条; i++) 纹.词条.Add(new 道纹词条(功能 ? 道纹属性.数量 : 道纹属性.力量, 功能 ? 1 : 2));
        return 纹;
    }
    public static string 验证模型()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("请在编辑模式检查独立模型。");
        目录=Path.Combine(天帝构建工具.项目根,"生成/验证/通货模型-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(目录);结果=new 报告();
        try{模型();}catch(Exception ex){结果.错误.Add(ex.ToString());}
        写报告();return 目录;
    }
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先停止运行并保存场景");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/通货-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        结果 = new 报告();
        try { 模型(); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); 写报告(); throw; }
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        原启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        步骤 = 0; 下步 = EditorApplication.timeSinceStartup + 2; 截止 = 下步 + 65;
        上次尺寸 = Vector2Int.zero; 上次帧 = -1;
        Application.logMessageReceived += 记错误; EditorApplication.update += 更新; EditorApplication.isPlaying = true; return 目录;
    }
    static void 模型()
    {
        检查("11种可用通货不含接口改造", 天帝通货.可用种类.Select(x => 天帝通货.定义[(int)x].名称).SequenceEqual(new[] { "启灵石", "点玄石", "紫蕴石", "无瑕玉", "凝华石", "蕴玄石", "琢天玉", "添蕴砂", "易纹砂", "重铸石", "问天石" }));
        foreach (var 定 in 天帝通货.定义.Take(7))
        {
            var 图 = 天帝道纹夹具.创建(4); var 纹 = 夹具(图, 定.来源.Value); var 货 = new 天帝通货(图, 8, 3); var 旧首 = 纹.词条[0];
            检查(定.名称 + "精准升阶保词保口仅补词条并扣1", 货.使用(定.种类, 纹, 0, out _) && 纹.品阶 == 定.目标 &&
                纹.词条[0].属性 == 旧首.属性 && 纹.词条[0].数值 == 旧首.数值 && 纹.有接口(3) &&
                纹.接口 == 8 && 纹.词条.Count == 天帝道纹品阶.获取(定.目标.Value).最少词条 && 货.数量(定.种类) == 2);
            var 原 = 快照(纹);
            检查(定.名称 + "重复升阶被拒绝且无消耗", !货.使用(定.种类, 纹, 0, out _) && 快照(纹) == 原 && 货.数量(定.种类) == 2);
        }
        var 网 = 天帝道纹夹具.创建(6); var a = 夹具(网, 道纹品阶.普通); var 钱 = new 天帝通货(网, 21, 6);
        检查("接口通货退出且不修改道纹", !钱.使用(通货种类.通脉针, a, 0, out _) &&
            !钱.使用(通货种类.六通玉, a, 0, out _) && a.接口 == 8 && 钱.数量(通货种类.通脉针) == 0 && 钱.数量(通货种类.六通玉) == 0);
        钱.使用(通货种类.启灵石, a, 0, out _);
        检查("升阶不改变接口", a.品阶 == 道纹品阶.优秀 && a.接口 == 8);
        检查("普通或优秀满词条拒绝添蕴砂", !钱.使用(通货种类.添蕴砂, a, 0, out _) && 钱.数量(通货种类.添蕴砂) == 6);
        定阶(a, 道纹品阶.完美); var 首 = a.词条[0];
        检查("添蕴砂仅加一条不改已有词条", 钱.使用(通货种类.添蕴砂, a, 0, out _) && a.词条.Count == 2 && a.词条[0].属性 == 首.属性 && a.词条[0].数值 == 首.数值 && a.接口 == 8);
        var 原首 = a.词条[0];
        检查("易纹砂只洗所选一条", 钱.使用(通货种类.易纹砂, a, 1, out _) && a.词条[0].属性 == 原首.属性 && a.词条[0].数值 == 原首.数值 && a.词条.Count == 2 && a.品阶 == 道纹品阶.完美 && a.接口 == 8);
        var 快 = 快照(a); int 余 = 钱.数量(通货种类.易纹砂);
        检查("单词条非法索引整次拒绝", !钱.使用(通货种类.易纹砂, a, -1, out _) && !钱.使用(通货种类.易纹砂, a, 2, out _) && 快照(a) == 快 && 钱.数量(通货种类.易纹砂) == 余);
        检查("重铸保条数品阶接口", 钱.使用(通货种类.重铸石, a, 0, out _) && a.词条.Count == 2 && a.品阶 == 道纹品阶.完美 && a.接口 == 8);
        bool 隔离 = true, 重复 = false, 可复现 = true, 赌保留 = true;
        for (int seed = 0; seed < 100; seed++)
        {
            var 图1 = 天帝道纹夹具.创建(seed); var 图2 = 天帝道纹夹具.创建(seed);
            var 纹1 = 夹具(图1, 道纹品阶.普通, seed % 2 == 0); var 纹2 = 夹具(图2, 道纹品阶.普通, seed % 2 == 0);
            var 货1 = new 天帝通货(图1, seed, 10); var 货2 = new 天帝通货(图2, seed, 10);
            foreach (var 货 in new[] { 货1, 货2 }) 货.使用(通货种类.问天石, 货 == 货1 ? 纹1 : 纹2, 0, out _);
            赌保留 &= 纹1.品阶 >= 道纹品阶.优秀 && 纹1.品阶 <= 道纹品阶.史诗 && 纹1.接口 == 8 && 纹1.数值 == (seed % 2 == 0 ? 1 : 2);
            定阶(纹1, 道纹品阶.史诗); 定阶(纹2, 道纹品阶.史诗);
            foreach (var 货 in new[] { 货1, 货2 })
            {
                var 纹 = 货 == 货1 ? 纹1 : 纹2;
                while (纹.词条.Count < 6) 货.使用(通货种类.添蕴砂, 纹, 0, out _);
                货.使用(通货种类.易纹砂, 纹, 2, out _); 货.使用(通货种类.重铸石, 纹, 0, out _);
            }
            可复现 &= 快照(纹1) == 快照(纹2);
            隔离 &= 纹1.词条.All(x =>
            {
                if((int)x.属性<0||(int)x.属性>=天帝道纹属性.数量||天帝道纹属性.分组(x.属性)==道纹属性分组.元素)return false;
                天帝数值.词条定点范围(x.属性,纹1.物品等级,out int 下,out int 上);
                double 定点=x.实际数值*天帝数值.取("rune.storage_scale");
                return 定点>=下-.0001&&定点<=上+.0001;
            });
            重复 |= 纹1.词条.GroupBy(x => x.属性).Any(g => g.Count() > 1);
        }
        检查("100种子统一属性洗练池合法", 隔离); 检查("洗练可生成重复词条", 重复); 检查("同种子操作序列可复现", 可复现); 检查("赌阶保留原词条接口且无普通传说结果", 赌保留);
        int[] 分布 = new int[8]; for (int i = 0; i < 100; i++) 分布[(int)天帝通货.赌阶结果(i)]++;
        检查("赌阶100个离散区间准确50/25/12/7/4/2", 分布.SequenceEqual(new[] { 0, 50, 25, 12, 7, 4, 2, 0 }));
        var 无钱 = new 天帝通货(网, 1); 快 = 快照(a);
        检查("空库存不改造", !无钱.使用(通货种类.重铸石, a, 0, out _) && 快照(a) == 快);
        检查("库存获取非法数量和溢出拒绝", !无钱.获得((通货种类)99, 1) && !无钱.获得(通货种类.重铸石, 0) && !无钱.获得(通货种类.重铸石, -1) && 无钱.获得(通货种类.重铸石, int.MaxValue) && !无钱.获得(通货种类.重铸石, 1));
        foreach (var 目标 in new[] { 网.已放置[Vector2Int.zero], 天帝道纹夹具.创建(8).道纹[0], null })
            检查("保护起点/外来/空目标" + (目标?.名称 ?? "空"), !钱.使用(通货种类.重铸石, 目标, 0, out _));
        检查("非法通货不修改目标", !钱.使用((通货种类)99, a, 0, out _) && 快照(a) == 快);
        var 天赋网 = 天帝道纹夹具.创建(5, 天帝天赋.获取(0)); var 天赋钱 = new 天帝通货(天赋网, 5, 3);
        检查("天赋起点不能使用通货", !天赋钱.使用(通货种类.重铸石, 天赋网.已放置[Vector2Int.zero], 0, out _) && 天赋钱.数量(通货种类.重铸石) == 3);
        var 链 = 天帝道纹夹具.创建(2,天帝天赋.获取((int)天赋种类.普通人)); 链.设置玩家等级(3); 链.解锁格子(new Vector2Int(1, 0)); 链.解锁格子(new Vector2Int(2, 0));
        var 桥 = 夹具(链, 道纹品阶.普通); var 下游 = 链.道纹.First(x => x.分类 == 道纹分类.属性 && x != 桥); 下游.词条.Clear(); 下游.属性 = 道纹属性.力量; 下游.数值 = 3; 下游.接口 = 8;
        链.放置(桥, new Vector2Int(1, 0)); 链.放置(下游, new Vector2Int(2, 0));
        using (var 人 = new 天帝主角属性(null, 链))
        {
            var 货 = new 天帝通货(链, 3, 3); int 通知 = 0; 链.状态改变 += () => 通知++;
            double 基础力量=天帝数值.取("player.initial_attribute")+天帝数值.取("growth.strength")*(天帝数值.成长(链.玩家等级)-1);
            检查("改造前下游断开", 桥.生效 && !下游.生效 && Math.Abs(人.力量-(基础力量+2))<.0001);
            桥.接口 = 9; 链.重算();
            检查("已放置接口布局重算即时同步主角", 下游.生效 && Math.Abs(人.力量-(基础力量+5))<.0001 && 通知 == 1 && 桥.格子 == new Vector2Int(1, 0));
            货.使用(通货种类.重铸石, 桥, 0, out _);
            double 桥力量=桥.词条.Where(x=>x.属性==道纹属性.力量).Sum(x=>x.实际数值);
            检查("重铸取消旧加成重算新加成", Math.Abs(人.力量-(基础力量+3+桥力量))<.0001 && 通知 == 2);
        }
    }
    static void 点击(Transform 根, string 名)
    {
        Canvas.ForceUpdateCanvases(); var 键 = 根.GetComponentsInChildren<Button>().Single(b => b.name == 名);
        var 区 = (RectTransform)键.transform;
        var 指 = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(区.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(指, 命中);
        if (命中.Count == 0 || 命中[0].gameObject != 键.gameObject || !键.interactable) throw new InvalidOperationException("按钮不可点击：" + 名 + "；命中=" + (命中.Count > 0 ? 命中[0].gameObject.name : "无") + "；位置=" + 指.position + "；屏幕=" + Screen.width + "x" + Screen.height);
        ExecuteEvents.Execute(键.gameObject, 指, ExecuteEvents.pointerClickHandler);
    }
    static void 更新()
    {
        if (EditorApplication.isPlaying) { Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); }
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("通货界面验证超时"); 结束(); return; }
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < 下步) return;
        EditorApplication.QueuePlayerLoopUpdate();
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (游戏 == null || 游戏.界面 == null) return;
        var 输入 = 游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        var 尺寸 = new Vector2Int(Screen.width, Screen.height);
        if (尺寸 != 上次尺寸) { 上次尺寸 = 尺寸; 上次帧 = Time.frameCount; 下步 = EditorApplication.timeSinceStartup + .3; return; }
        if (Time.frameCount == 上次帧) return;
        var 画布 = 游戏.GetComponentInChildren<Canvas>();
        float 目标比例 = Mathf.Sqrt(Screen.width / 1600f * (Screen.height / 900f));
        if (画布 != null && Mathf.Abs(画布.scaleFactor - 目标比例) > .02f) return;
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases();
        try
        {
            switch (步骤)
            {
                case 0: 检查("运行标题仍显示完整名", 游戏.GetComponentsInChildren<Text>().Any(t => t.text.Replace("\n", "") == 天帝游戏.全名)); 游戏.开始序章(); 游戏.跳过序章(); 下步 = EditorApplication.timeSinceStartup + 3; break;
                case 1:
                    if (!游戏.界面.源道纹页.可选择) return;
                    游戏.选择源道纹(0);
                    检查("正式开局零通货零普通道纹", 天帝通货.定义.All(x => 游戏.通货数据.数量(x.种类) == 0) && 游戏.道纹数据.道纹.Count == 0);
                    foreach (var 种类 in 天帝通货.可用种类) 游戏.通货数据.获得(种类, 3);
                    天帝道纹夹具.填充(游戏.道纹数据, 4, 游戏.道纹数据.技能点); break;
                case 2:
                    点击(游戏.transform, "道纹改造");
                    检查("主页入口打开独立改造页", 游戏.阶段 == 游戏阶段.道纹改造 && 游戏.界面.道纹页 == null);
                    检查("弹窗11种独立按钮", 游戏.界面.改造页.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("通货-")) == 11);
                    var 图标 = 游戏.界面.改造页.GetComponentsInChildren<RawImage>();
                    var 精灵 = 游戏.界面.改造页.GetComponentsInChildren<Image>().Where(x => x.name == "图标").ToArray();
                    检查("11枚图标加载且不拦截通货点击", 图标.Length + 精灵.Length == 11 && 图标.All(x => x.texture != null && !x.raycastTarget) && 精灵.All(x => x.sprite != null && !x.raycastTarget));
                    检查("数量独立大字显示", 游戏.界面.改造页.GetComponentsInChildren<Text>().Count(x => x.name == "数量" && x.fontSize == 34 && x.text == "×3") == 11);
                    break;
                case 3:
                    Canvas.ForceUpdateCanvases();
                    var 事件 = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, 游戏.界面.改造页.transform.TransformPoint(new Vector2(800, -450))) };
                    var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(事件, 命中);
                    检查("独立改造页面正确命中", 命中.Count > 0 && 命中[0].gameObject.transform.IsChildOf(游戏.界面.改造页.transform));
                    var 数据 = 游戏.道纹数据; var a = 夹具(数据, 道纹品阶.普通); 数据.设置玩家等级(2); 数据.解锁格子(new Vector2Int(1, 0)); 数据.放置(a, new Vector2Int(1, 0));
                    var 页 = 游戏.界面.改造页; 页.选目标(数据.道纹.IndexOf(a)); 点击(页.transform, "通货-无瑕玉"); 点击(页.transform, "使用通货");
                    检查("UI使用无瑕玉升完美扣1且刷新结果", a.品阶 == 道纹品阶.完美 && 页.最近结果.Contains("成功") && 游戏.通货数据.数量(通货种类.无瑕玉) == 2);
                    检查("升阶后不满足来源禁用按钮", !页.可执行);
                    检查("改造品阶详情即时刷新", 页.GetComponentsInChildren<Text>().Any(t => t.text.Contains(天帝道纹品阶.彩色品阶文字(道纹品阶.完美))));
                    点击(页.transform, "通货-添蕴砂"); 点击(页.transform, "使用通货"); 检查("UI添蕴砂加词条", a.词条.Count == 3 && 游戏.通货数据.数量(通货种类.添蕴砂) == 2);
                    点击(页.transform, "通货-易纹砂"); break;
                case 4:
                    var 通货页 = 游戏.界面.改造页;
                    点击(通货页.transform, "词条-1"); 点击(通货页.transform, "使用通货");
                    检查("UI单词洗练消耗及条数", 通货页.当前目标.词条.Count == 3 && 游戏.通货数据.数量(通货种类.易纹砂) == 2);
                    Canvas.ForceUpdateCanvases(); ScreenCapture.CaptureScreenshot(Path.Combine(目录, "currency-ui.png"));
                    点击(通货页.transform, "词条-0");
                    检查("直接点击词条选择洗练目标", 通货页.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("洗练第 1 条")));
                    var 满词 = 通货页.当前目标; 定阶(满词, 道纹品阶.传说); 满词.接口 = 63; 满词.词条.Clear();
                    for (int i = 0; i < 6; i++) 满词.词条.Add(new 道纹词条(道纹属性.智力, 3));
                    游戏.道纹数据.重算(); 通货页.选通货(通货种类.问天石); Canvas.ForceUpdateCanvases();
                    foreach (var t in 通货页.GetComponentsInChildren<Text>().Where(t => t.preferredHeight > t.rectTransform.rect.height + 1))
                        结果.排版诊断.Add(t.text + "：需要" + t.preferredHeight + "，高度" + t.rectTransform.rect.height + "，宽度" + t.rectTransform.rect.width);
                    检查("六词六接口与问天概率说明不溢出", 通货页.GetComponentsInChildren<Text>().All(t => t.preferredHeight <= t.rectTransform.rect.height + 1));
                    bool 所有说明 = true;
                    foreach (var 定 in 天帝通货.定义)
                    {
                        通货页.选通货(定.种类); Canvas.ForceUpdateCanvases();
                        所有说明 &= 通货页.GetComponentsInChildren<Text>().All(t => t.preferredHeight <= t.rectTransform.rect.height + 1);
                    }
                    检查("全部13通货底部说明与六词条排版不溢出", 所有说明);
                    通货页.选通货(通货种类.问天石);
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "six-affix-ui.png")); break;
                case 5:
                    点击(游戏.界面.改造页.transform, "关闭通货"); 检查("关闭回主页", 游戏.阶段 == 游戏阶段.主页 && 游戏.界面.改造页 == null);
                    游戏.打开道纹改造();
                    检查("重入改造不补库存且保留改造", 游戏.通货数据.数量(通货种类.无瑕玉) == 2 && 游戏.道纹数据.道纹.Any(x => x.品阶 == 道纹品阶.传说 && x.词条.Count == 6));
                    break;
                default: 结束(); return;
            }
            步骤++; if (步骤 != 1) 下步 = EditorApplication.timeSinceStartup + .6;
        }
        catch (Exception ex) { 结果.错误.Add(ex.ToString()); 结束(); }
    }
    static void 记错误(string 文, string 栈, LogType 类) { if (类 == LogType.Exception || 类 == LogType.Error || 类 == LogType.Assert) 结果.错误.Add(文 + "\n" + 栈); }
    static void 写报告() => File.WriteAllText(Path.Combine(目录, "currency-smoke.json"), JsonUtility.ToJson(结果, true));
    static void 结束()
    {
        EditorApplication.update -= 更新; Application.logMessageReceived -= 记错误;
        EditorApplication.isPlaying = false; EditorSettings.enterPlayModeOptionsEnabled = 原启用; EditorSettings.enterPlayModeOptions = 原模式;
        写报告(); Debug.Log("通货验收：" + 结果.通过.Count + "通过 / " + 结果.失败.Count + "失败 / " + 结果.错误.Count + "错误；" + 目录);
    }
}
#endif
