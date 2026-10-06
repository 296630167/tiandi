#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class 天帝天赋验证
{
    [Serializable] sealed class 报告
    { public List<string> 通过 = new List<string>(); public List<string> 失败 = new List<string>(); public List<string> 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录;
    static int 步骤;
    static double 下步, 截止;
    static bool 原启用;
    static EnterPlayModeOptions 原模式;
    static int 待选编号;
    static Vector2Int 上次尺寸;
    static int 上次帧;
    static void 检查(string 名, bool 成立) { (成立 ? 结果.通过 : 结果.失败).Add(名); }
    static bool 近(float a, float b) => Mathf.Abs(a - b) < 0.001f;
    public static string 启动()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("请先停止运行并保存场景，避免打断当前工作。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/天赋-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        结果 = new 报告();
        try { 模型(); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); 写报告(); throw; }
        EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        原启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        步骤 = 0; 下步 = EditorApplication.timeSinceStartup + 2; 截止 = 下步 + 70; 上次尺寸 = Vector2Int.zero; 上次帧 = -1;
        Application.logMessageReceived += 记录错误; EditorApplication.update += 更新; EditorApplication.isPlaying = true;
        return 目录;
    }
    static void 模型()
    {
        检查("十三种天赋编号名称效果唯一且完整", 天帝天赋.全部.Count == 13 && 天帝天赋.全部.Select(t => t.名称).Distinct().Count() == 13 && 天帝天赋.全部.All(t => !string.IsNullOrEmpty(t.效果)));
        检查("非法天赋编号返回空", 天帝天赋.获取(-1) == null && 天帝天赋.获取(天帝天赋.全部.Count) == null);
        var 覆盖 = new HashSet<int>(); bool 唯一 = true;
        var 池 = new 天帝天赋池(123);
        for (int i = 0; i < 1000; i++)
        { 唯一 &= 池.候选.Count == 5 && 池.候选.Select(t => t.编号).Distinct().Count() == 5; foreach (var t in 池.候选) 覆盖.Add(t.编号); 池.刷新(); }
        检查("一千轮每次五枚无重复且覆盖十三种", 唯一 && 覆盖.Count == 13 && 池.刷新次数 == 1000);
        int 旧轮 = 池.轮次; int 旧号 = 池.候选[0].编号; 池.刷新();
        检查("旧候选轮次拒绝确认", !池.确认(旧号, 旧轮));
        int 未显示 = 天帝天赋.全部.First(t => !池.候选.Contains(t)).编号;
        检查("非法及未展示天赋不能选择", !池.确认(-1, 池.轮次) && !池.确认(未显示, 池.轮次));
        检查("确认一枚后拒绝刷新及再次确认", 池.确认(池.候选[0].编号, 池.轮次) && !池.刷新() && !池.确认(池.候选[1].编号, 池.轮次));
        var 配置 = new 主角属性配置 { 力量 = 10, 速度 = 5, 智力 = 4 };
        配置.血量.基础值 = 100; 配置.灵力.基础值 = 20; 配置.防御.基础值 = 3;
        配置.灵气护盾.基础值 = 7; 配置.移动速度.基础值 = 3; 配置.跑步速度.基础值 = 5;
        foreach (var t in 天帝天赋.全部)
        {
            var 网 = 天帝道纹夹具.创建(7, t); var 源 = 网.已放置[Vector2Int.zero];
            检查(t.名称 + "初始点数且只解锁中心", 网.玩家等级 == 1 && 网.技能点 == (t.种类 == 天赋种类.穿越者 ? 3 : 0) && 网.已解锁格数 == 1);
            检查(t.名称 + "中心唯一且不进入普通候选", 网.天赋 == t && 源.是天赋 && 源.类型 == "天赋" && 网.道纹.All(r => !r.是天赋) && 网.生效加成.Sum() == 0);
            检查(t.名称 + "中心固定与一开五封", 源.接口 == 1 && !网.旋转(源) && !网.可放置(源, new Vector2Int(1, 0)));
            网.设置玩家等级(50); 检查(t.名称 + "等级解封不丢天赋", 源.接口 == 63 && 网.天赋 == t);
            using (var 人 = new 天帝主角属性(配置, 网))
            {
                检查(t.名称 + "主角天赋与属性基准接入", 人.天赋 == t && 人.力量 == 10 && 人.速度 == 5 && 人.智力 == 4);
                if (t.种类 == 天赋种类.灵海) 检查("灵海上限50%且初始填满", 人.灵力 == 30 && 人.当前灵力 == 30);
                if (t.种类 == 天赋种类.铁骨) 检查("铁骨防御翻倍", 人.防御 == 6);
                if (t.种类 == 天赋种类.凝光) 检查("凝光最终智力折算护盾", 人.灵气护盾 == 19 && 人.当前灵气护盾 == 19);
                if (t.种类 == 天赋种类.逐风) 检查("逐风仅两种移动速度20%", 近(人.移动速度, 3.6f) && 近(人.跑步速度, 6) && 人.速度 == 5);
            }
        }
        var 力网 = 天帝道纹夹具.创建(4, 天帝天赋.获取(0)); 力网.设置玩家等级(10); 力网.解锁格子(new Vector2Int(1, 0));
        var 力纹 = 力网.道纹.First(r => r.分类 == 道纹分类.属性); 力纹.词条.Clear(); 力纹.属性 = 道纹属性.力量; 力纹.数值 = 15; 力纹.接口 = 8;
        using (var 人 = new 天帝主角属性(配置, 力网))
        {
            力网.放置(力纹, new Vector2Int(1, 0)); 检查("万钧仅翻倍加成10加15乘2等于40", 人.力量 == 40);
            力网.重算(); 人.重算(); 检查("重复重算不叠加万钧", 人.力量 == 40);
            力网.旋转(力纹); 检查("力量断链即时取消翻倍加成", 人.力量 == 10);
            for (int i = 0; i < 5; i++) 力网.旋转(力纹); 检查("重接恢复万钧", 人.力量 == 40);
        }
        var 双 = 天帝天赋.获取(1); var 链 = 天帝天赋.获取(2); var 域 = 天帝天赋.获取(3); var 逆 = 天帝天赋.获取(8);
        检查("双生矢正常射击加一", 天帝天赋效果.射击数量(双, 1, true) == 2 && 天帝天赋效果.射击数量(双, 3, true) == 4);
        检查("双生矢不增加非射击或衍生数量", 天帝天赋效果.射击数量(双, 1, false) == 1 && 天帝天赋效果.射击数量(双, 1, true, true) == 1);
        检查("续雷只增加支持连锁的次数", 天帝天赋效果.连锁次数(链, 2, true) == 3 && 天帝天赋效果.连锁次数(链, 0, false) == 0);
        检查("数量次数极值无溢出", 天帝天赋效果.射击数量(双, int.MaxValue, true) == int.MaxValue && 天帝天赋效果.连锁次数(链, int.MaxValue, true) == int.MaxValue);
        检查("广域作用半径30%且非范围不改", 近(天帝天赋效果.作用半径(域, 10, true), 13) && 天帝天赋效果.作用半径(域, 10, false) == 10);
        检查("逆命满血半血零血倍率", 天帝天赋效果.技能伤害倍率(逆, 100, 100) == 1 && 近(天帝天赋效果.技能伤害倍率(逆, 50, 100), 1.3f) && 近(天帝天赋效果.技能伤害倍率(逆, 0, 100), 1.6f));
        检查("逆命非法或零上限安全", 天帝天赋效果.技能伤害倍率(逆, 0, 0) == 1 && 天帝天赋效果.技能伤害倍率(逆, float.NaN, 100) == 1 && 天帝天赋效果.技能伤害倍率(逆, 200, 100) == 1);
        var 响 = new 天帝余响计数(天帝天赋.获取(9)); bool 前四无 = true;
        for (int i = 0; i < 4; i++) 前四无 &= 响.记录释放(i % 2 == 0 ? "A" : "B") == null;
        var 请求 = 响.记录释放("B");
        检查("余响跨技能共享第五次返回同技能请求", 前四无 && 请求 != null && 请求.技能编号 == "B" && 请求.延迟秒 == 0.2f && !请求.消耗灵力 && !请求.推进计数 && 响.当前计数 == 0);
        检查("余响衍生失败空编号不计数", 响.记录释放("B", true, true) == null && 响.记录释放("A", false) == null && 响.记录释放("") == null && 响.当前计数 == 0);
        检查("其他天赋无余响计数", new 天帝余响计数(双).记录释放("A") == null);
        var 普 = 天帝天赋.获取((int)天赋种类.普通人); var 穿 = 天帝天赋.获取((int)天赋种类.穿越者); var 命 = 天帝天赋.获取((int)天赋种类.天命之子);
        var 穿网 = 天帝道纹夹具.创建(8, 穿);
        bool 三格 = 穿网.解锁格子(new Vector2Int(1, 0)) && 穿网.解锁格子(new Vector2Int(2, 0)) && 穿网.解锁格子(new Vector2Int(3, 0));
        检查("穿越者三点可解锁三格且第四格拒绝", 三格 && 穿网.技能点 == 0 && !穿网.解锁格子(new Vector2Int(4, 0)));
        穿网.重算(); 穿网.设置玩家等级(1); 穿网.设置玩家等级(2); 穿网.设置玩家等级(2); 穿网.重算();
        检查("穿越者重算同步不重复赠点正常升级给一点", 穿网.技能点 == 1 && 穿网.玩家等级 == 2 && 穿网.已放置[Vector2Int.zero].接口 == 1);
        检查("普通人无属性技能掉落加成", 天帝天赋效果.力量加成(普, 7) == 7 && 天帝天赋效果.灵力上限(普, 20) == 20 && 天帝天赋效果.防御(普, 3) == 3 && 天帝天赋效果.护盾上限(普, 7, 4) == 7 && 天帝天赋效果.移动速度(普, 3) == 3 && 天帝天赋效果.射击数量(普, 1, true) == 1 && 天帝天赋效果.连锁次数(普, 2, true) == 2 && 天帝天赋效果.作用半径(普, 10, true) == 10 && 天帝天赋效果.技能伤害倍率(普, 0, 100) == 1 && new 天帝余响计数(普).记录释放("A") == null && 天帝天赋效果.掉落概率(普, 0.2f) == 0.2f);
        检查("天命之子爆率50%且封顶100%", 近(天帝天赋效果.掉落概率(命, 0.2f), 0.3f) && 天帝天赋效果.掉落概率(命, 0.8f) == 1 && 天帝天赋效果.掉落概率(命, 0) == 0);
        检查("掉落概率负数非有限与越界安全", 天帝天赋效果.掉落概率(命, -1) == 0 && 天帝天赋效果.掉落概率(命, float.NaN) == 0 && 天帝天赋效果.掉落概率(命, float.PositiveInfinity) == 0 && 天帝天赋效果.掉落概率(普, 2) == 1);
        int 次数 = 0; var 幸运 = 天帝天赋效果.掉落品阶(命, () => ++次数 == 1 ? 道纹品阶.优秀 : 道纹品阶.完美);
        检查("天命之子品阶恰抽两次取较高", 次数 == 2 && 幸运 == 道纹品阶.完美);
        次数 = 0; 幸运 = 天帝天赋效果.掉落品阶(命, () => ++次数 == 1 ? 道纹品阶.传说 : 道纹品阶.普通);
        检查("天命之子首次较高不降阶", 次数 == 2 && 幸运 == 道纹品阶.传说);
        次数 = 0; 幸运 = 天帝天赋效果.掉落品阶(普, () => { 次数++; return 道纹品阶.杰出; });
        检查("其他天赋品阶只抽一次", 次数 == 1 && 幸运 == 道纹品阶.杰出);
        bool 空拒绝 = false, 坏拒绝 = false;
        try { 天帝天赋效果.掉落品阶(命, null); } catch (ArgumentNullException) { 空拒绝 = true; }
        try { 天帝天赋效果.掉落品阶(命, () => (道纹品阶)8); } catch (ArgumentOutOfRangeException) { 坏拒绝 = true; }
        检查("幸运抽样拒绝空委托与非法品阶", 空拒绝 && 坏拒绝);
        var 普网 = 天帝道纹夹具.创建(123, 普); var 命网 = 天帝道纹夹具.创建(123, 命);
        检查("幸运不篡改当前演示候选品阶", 普网.道纹.Select(r => r.品阶).SequenceEqual(命网.道纹.Select(r => r.品阶)));
        var 灵网 = 天帝道纹夹具.创建(7, 天帝天赋.获取(4)); 灵网.设置玩家等级(3); 灵网.解锁格子(new Vector2Int(1, 0));
        var 灵纹 = 灵网.道纹.First(r => r.分类 == 道纹分类.属性); 灵纹.词条.Clear(); 灵纹.属性 = 道纹属性.灵力; 灵纹.数值 = 10; 灵纹.接口 = 8;
        using (var 人 = new 天帝主角属性(配置, 灵网))
        {
            人.设置当前资源(100, 10, 7); 灵网.放置(灵纹, new Vector2Int(1, 0));
            检查("灵海应用于加成后的总灵力且提高上限不回复", 人.灵力 == 45 && 人.当前灵力 == 10);
            灵网.收回(灵纹); 灵网.放置(灵纹, new Vector2Int(1, 0)); 检查("天赋插拔不免费回复资源", 人.当前灵力 == 10);
        }
    }
    static void 记录错误(string 条件, string 栈, LogType 类型) { if (类型 == LogType.Error || 类型 == LogType.Exception) 结果.错误.Add(条件); }
    static bool 点击(天帝游戏 游戏, string 名)
    {
        Canvas.ForceUpdateCanvases(); var 键 = 游戏.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == 名);
        if (键 == null || !键.interactable) return false;
        var 区 = (RectTransform)键.transform;
        var 指 = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(区.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(指, 命中);
        if (命中.Count == 0 || 命中[0].gameObject != 键.gameObject) return false;
        ExecuteEvents.Execute(键.gameObject, 指, ExecuteEvents.pointerClickHandler); return true;
    }
    static void 更新()
    {
        if (EditorApplication.isPlaying) { Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); }
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("流程超时，步骤=" + 步骤); 完成(); return; }
        if (EditorApplication.timeSinceStartup < 下步) return;
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (!EditorApplication.isPlaying || 游戏 == null || 游戏.界面 == null) return;
        var 输入 = 游戏.GetComponentInChildren<InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        if (游戏.阶段 == 游戏阶段.序章) 游戏.GetComponent<天帝序章>().设置焦点(true);
        var 尺寸 = new Vector2Int(Screen.width, Screen.height);
        if (尺寸 != 上次尺寸) { 上次尺寸 = 尺寸; 上次帧 = Time.frameCount; 下步 = EditorApplication.timeSinceStartup + 0.2; return; }
        if (Time.frameCount == 上次帧) return;
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases();
        try
        {
            switch (步骤)
            {
                case 0: 检查("标题开始进入序章", 点击(游戏, "开始游戏") && 游戏.阶段 == 游戏阶段.序章); break;
                case 1:
                    检查("跳过到道纹面前即可交互", 点击(游戏, "跳过序章") && 游戏.阶段 == 游戏阶段.源道纹选择 && 游戏.界面.源道纹页.可选择 && 游戏.GetComponent<天帝序章>().可衔接道纹选择);
                    步骤++; 下步 = EditorApplication.timeSinceStartup + .5; return;
                case 2:
                    检查("五枚天赋全部可选且有刷新确认按钮", 游戏.界面.源道纹页.可选择 && 游戏.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("源道纹-") && b.interactable) == 5 && 游戏.GetComponentsInChildren<Button>().Any(b => b.name == "刷新天赋" && b.interactable));
                    检查("未选中时确认禁用", !点击(游戏, "确认天赋"));
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "01-choice.png")); break;
                case 3:
                    检查("点击卡片只选中不直接提交", 点击(游戏, "源道纹-0") && 游戏.阶段 == 游戏阶段.源道纹选择 && 游戏.界面.源道纹页.选中槽位 == 0 && 游戏.当前天赋 == null);
                    int 旧 = 游戏.天赋池.轮次; 检查("刷新清除选中并禁止旧轮确认", 点击(游戏, "刷新天赋") && 游戏.天赋池.轮次 == 旧 + 1 && 游戏.界面.源道纹页.选中槽位 == -1 && !点击(游戏, "确认天赋") && !游戏.选择天赋(游戏.天赋池.候选[0].编号, 旧));
                    break;
                case 4:
                    bool 刷新成功 = true; for (int i = 0; i < 30; i++) 刷新成功 &= 点击(游戏, "刷新天赋");
                    检查("多次实际UGUI刷新且主角未提前生成", 刷新成功 && 游戏.天赋池.刷新次数 == 31 && 游戏.主角属性 == null && 游戏.道纹数据 == null);
                    int 尝试 = 0;
                    while (游戏.天赋池.候选.Count(t => t.编号 >= (int)天赋种类.普通人) < 3 && 尝试++ < 1000) 点击(游戏, "刷新天赋");
                    检查("三种新天赋能同时出现在UI候选", 游戏.天赋池.候选.Count(t => t.编号 >= (int)天赋种类.普通人) == 3);
                    检查("天赋效果文本无卡片溢出", 游戏.GetComponentsInChildren<Text>().Where(t => 天帝天赋.全部.Any(d => d.效果 == t.text)).All(t => t.preferredHeight <= t.rectTransform.rect.height + 1));
                    游戏.界面.源道纹页.显示详情(0, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "02-tooltip.png")); break;
                case 5:
                    游戏.界面.源道纹页.隐藏详情(); 待选编号 = 游戏.天赋池.候选[4].编号;
                    检查("第五槽也可选择", 点击(游戏, "源道纹-4") && 游戏.界面.源道纹页.选中槽位 == 4);
                    int 穿槽 = 游戏.天赋池.候选.ToList().FindIndex(t => t.种类 == 天赋种类.穿越者); 待选编号 = (int)天赋种类.穿越者;
                    检查("点击穿越者卡片准备确认", 穿槽 >= 0 && 点击(游戏, "源道纹-" + 穿槽));
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "03-selected.png")); break;
                case 6:
                    检查("确认天赋进入主页并接入主角", 点击(游戏, "确认天赋") && 游戏.阶段 == 游戏阶段.主页 && 游戏.当前天赋.编号 == 待选编号 && 游戏.主角属性.天赋 == 游戏.当前天赋 && 游戏.道纹数据.天赋 == 游戏.当前天赋);
                    检查("主页拒绝刷新重选", !游戏.刷新天赋() && !游戏.选择天赋(待选编号, 游戏.天赋池.轮次));
                    检查("实际确认穿越者1级基础1点加额外三点", 游戏.道纹数据.技能点 == 4 && 游戏.道纹数据.玩家等级 == 1);
                    游戏.打开道纹(); break;
                case 7:
                    检查("中心显示本局天赋且无旧源纹10点", 游戏.阶段 == 游戏阶段.道纹 && 游戏.道纹数据.已放置[Vector2Int.zero].名称 == 游戏.当前天赋.名称 + "天赋道纹" && 游戏.道纹数据.生效加成.Sum() == 0);
                    ScreenCapture.CaptureScreenshot(Path.Combine(目录, "04-canvas.png")); break;
                case 8:
                    游戏.返回主页(); 游戏.打开道纹(); 检查("重入保留同一天赋与空候选", 游戏.当前天赋.编号 == 待选编号 && 游戏.道纹数据.道纹.Count == 0);
                    检查("重入画布不重复赠送穿越者技能点", 游戏.道纹数据.技能点 == 4); 完成(); return;
            }
            步骤++; 下步 = EditorApplication.timeSinceStartup + 0.65;
        }
        catch (Exception ex) { 结果.错误.Add(ex.ToString()); 完成(); }
    }
    static void 写报告() => File.WriteAllText(Path.Combine(目录, "talent-smoke.json"), JsonUtility.ToJson(结果, true));
    static void 完成()
    {
        EditorApplication.update -= 更新; Application.logMessageReceived -= 记录错误; 写报告();
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => { EditorSettings.enterPlayModeOptionsEnabled = 原启用; EditorSettings.enterPlayModeOptions = 原模式; };
    }
}
#endif
