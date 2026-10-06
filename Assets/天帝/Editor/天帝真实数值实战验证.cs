#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 在现有两个场景运行正式开局与正式曲线；隔离存档，退出后恢复编辑器选项。
public static class 天帝真实数值实战验证
{
    static 天帝真实数值验证.报告 结果;
    static string 目录, 原存档目录, 真实路径, 原文件指纹;
    static bool 原选项启用, 原后台, 已启动, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static string 指纹(string 路径)
    { if (!File.Exists(路径)) return "无文件"; using (var 算法 = SHA256.Create()) return Convert.ToBase64String(算法.ComputeHash(File.ReadAllBytes(路径))); }
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static bool 近(double 实际, double 期望) => Math.Abs(实际 - 期望) < Math.Max(.0001, Math.Abs(期望) * .000003);
    public static string 启动()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(序 => SceneManager.GetSceneAt(序).isDirty)) throw new InvalidOperationException("真实数值实战检查需处于编辑模式且没有未保存场景。");
        if (SceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("请在已有主场景执行实战检查，不重建场景。");
        天帝数值同步检查.校验();
        结果 = new 天帝真实数值验证.报告(); 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/真实数值实战-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        真实路径 = Path.Combine(Application.persistentDataPath, "天帝进度.json"); 原文件指纹 = 指纹(真实路径);
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 数据 = new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网, 42).导出库存(), 灵石 = 天帝宝盒.开局灵石 };
        if (!new 天帝存档(Path.Combine(目录, "隔离存档")).保存(数据)) throw new InvalidOperationException("隔离存档创建失败");
        原存档目录 = 天帝存档.验证目录; 天帝存档.验证目录 = Path.Combine(目录, "隔离存档");
        原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions; 原后台 = Application.runInBackground;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload; Application.runInBackground = true;
        已启动 = 已结束 = false; 截止 = EditorApplication.timeSinceStartup + 55;
        var 游戏视图类型 = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (游戏视图类型 != null) EditorWindow.GetWindow(游戏视图类型).Focus();
        EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 清理; Application.logMessageReceived += 记错;
        EditorApplication.isPlaying = true; return 目录;
    }
    static void 等待()
    {
        if (已结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("实战检查超时"); 完成(); return; }
        if (已启动 || !EditorApplication.isPlaying) return;
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null || 游戏.阶段 != 游戏阶段.标题) return;
        已启动 = true; 游戏.StartCoroutine(演练(游戏));
    }
    static IEnumerator 演练(天帝游戏 游戏)
    {
        检查("隔离继续进入已有主页", 游戏.继续游戏()); yield return null;
        var 人 = 游戏.主角属性; var 网 = 游戏.道纹数据;
        检查("主页实际一级60生命1攻击且每1.25秒攻击", 人.等级 == 1 && 近(人.血量, 60) && 近(人.攻击力, 1) && 近(1 / 人.攻击速度, 1.25));
        检查("真实开局不发测试道纹通货点数", 网.道纹.Count == 0 && 网.技能点 == 1 && 天帝通货.可用种类.All(种 => 游戏.通货数据.数量(种) == 0) && !人.导出配置().使用测试数据);
        检查("实际场景进入一级地图", 游戏.进入战斗());
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        if (游戏.战斗场景 == null) { 结果.失败.Add("战斗场景未加载"); 完成(); yield break; }
        var 场 = 游戏.战斗场景; 场.enabled = false;
        var 战 = 场.战斗; var 普通 = 战.敌人.First(敌 => 敌.存活);
        检查("实机敌人读取重算正式生命且进攻参数不变", 近(普通.最大血量, 天帝数值配置.敌人(1, 0, 0) * 天帝敌种配置.物种(普通.物种, "hp_factor")) && 近(普通.攻击力, 3.6666666667) && 战.分类在场(战斗敌人级别.普通) == 8);
        var 视野 = 场.读取战斗视野();
        检查("首波使用真实相机全部刷在屏外", 战.敌人.Where(敌 => 敌.存活).All(敌 => !视野.Contains(敌.位置)));
        检查("实机屏外敌人立即处于追击状态", 战.敌人.Where(敌 => 敌.存活).All(敌 => 敌.行动 == 敌人行动.追击));
        Canvas.ForceUpdateCanvases();
        var HUD = 游戏.GetComponentsInChildren<RectTransform>().First(区 => 区.name == "战斗HUD");
        检查("战斗UI直接贴安全区而非中央设计区", HUD.parent.name == "安全区" && HUD.anchorMin == Vector2.zero && HUD.anchorMax == Vector2.one);
        var 中央 = new Rect(-HUD.rect.width * .25f, -HUD.rect.height * .3f, HUD.rect.width * .5f, HUD.rect.height * .6f);
        foreach (string 名 in new[] { "主角战斗状态", "波次敌人信息", "战斗小地图底", "暂停入口", "战斗目标", "拾取提示列表" })
        {
            var 区 = HUD.GetComponentsInChildren<RectTransform>().First(区块 => 区块.name == 名);
            var 范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(HUD, 区);
            检查("常驻HUD避开中央视野 " + 名, !中央.Overlaps(new Rect((Vector2)范围.min, (Vector2)范围.size)));
        }
        检查("移除战斗大块掉落详情", !游戏.GetComponentsInChildren<RectTransform>().Any(区 => 区.name == "附近道纹详情"));
        var 左框 = HUD.GetComponentsInChildren<RectTransform>().First(区 => 区.name == "主角战斗状态");
        var 右框 = HUD.GetComponentsInChildren<RectTransform>().First(区 => 区.name == "波次敌人信息");
        检查("常驻状态和统计面积缩至旧大面板约四分之一", 左框.rect.width * 左框.rect.height + 右框.rect.width * 右框.rect.height < (480 * 116 + 690 * 106 + 452 * 326 + 610 * 42) * .27);
        foreach (var 文 in 左框.GetComponentsInChildren<Text>().Concat(右框.GetComponentsInChildren<Text>()))
            检查("HUD文字完整行高 " + 文.text, 文.preferredHeight <= 文.rectTransform.rect.height + .1f);
        检查("精简统计仍含四类剩余场上和待刷", 右框.GetComponentInChildren<Text>().text.Contains("普通") && 右框.GetComponentInChildren<Text>().text.Contains("精英") && 右框.GetComponentInChildren<Text>().text.Contains("头目") && 右框.GetComponentInChildren<Text>().text.Contains("BOSS") && 右框.GetComponentInChildren<Text>().text.Contains("待刷"));
        yield return new WaitForEndOfFrame();
        var 画面 = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(目录, "紧凑战斗HUD.png"), 画面.EncodeToPNG()); UnityEngine.Object.Destroy(画面);
        游戏.界面.切换战斗暂停();
        Canvas.ForceUpdateCanvases();
        var 暂停页 = 游戏.GetComponentsInChildren<RectTransform>().First(区 => 区.name == "战斗暂停层");
        foreach (var 文 in 暂停页.GetComponentsInChildren<Text>())
            检查("暂停详情完整行高 " + 文.text, 文.preferredHeight <= 文.rectTransform.rect.height + .1f);
        var 原位 = 场.玩家位置; var 怪位 = 普通.位置; float 暂停血 = 人.当前血量; int 释放 = 战.普通释放次数;
        for (int i = 0; i < 4; i++) { 场.移动一步(Vector2.up, true, .25f); 场.战斗一步(.25f); }
        检查("暂停同时冻结玩家敌人攻击和刷新", 游戏.界面.战斗已暂停 && 场.玩家位置 == 原位 && 普通.位置 == 怪位 && 人.当前血量 == 暂停血 && 战.普通释放次数 == 释放 && 战.场上敌人数量 == 8);
        检查("暂停页可查看构筑与资源详情", 暂停页.GetComponentsInChildren<Text>().Any(文 => 文.text.Contains("当前单发伤害")) && 暂停页.GetComponentsInChildren<Text>().Any(文 => 文.text.Contains("道纹") && 文.text.Contains("通货") && 文.text.Contains("灵石")));
        游戏.界面.切换战斗暂停();
        检查("关闭暂停后恢复并清理遮罩", !游戏.界面.战斗已暂停 && !游戏.GetComponentsInChildren<RectTransform>().Any(区 => 区.name == "暂停遮罩" && 区.gameObject.activeInHierarchy));
        var 准备时间 = new System.Collections.Generic.List<float>(); var 释放时间 = new System.Collections.Generic.List<float>(); float 战斗秒 = 0;
        Action<Vector2> 记录准备 = _ => 准备时间.Add(战斗秒), 记录释放 = _ => 释放时间.Add(战斗秒);
        战.准备射击 += 记录准备; 战.射击释放 += 记录释放;
        for (int 帧 = 0; 帧 < 40 && 战.击败数 == 0; 帧++) { for (int 步 = 0; 步 < 20; 步++) { 战斗秒 += .025f; 场.战斗一步(.025f); } yield return null; }
        战.准备射击 -= 记录准备; 战.射击释放 -= 记录释放;
        File.WriteAllText(Path.Combine(目录, "开局攻速实测.json"), JsonUtility.ToJson(new 开局节奏记录 { 前摇 = 战.射击前摇, 准备时间 = 准备时间.ToArray(), 释放时间 = 释放时间.ToArray() }, true));
        检查("新开局真实准备与发射均按约1.25秒间隔", 准备时间.Count >= 2 && 释放时间.Count >= 2 && Mathf.Abs(准备时间[1] - 准备时间[0] - 1.25f) <= .04f && Mathf.Abs(释放时间[1] - 释放时间[0] - 1.25f) <= .04f);
        检查("开局实际先准备0.2秒再释放且不叠加周期", 准备时间.Count > 0 && 释放时间.Count > 0 && Mathf.Abs(释放时间[0] - 准备时间[0] - .2f) <= .03f);
        检查("正式普攻实际发射并击败普通怪", 战.普通释放次数 > 0 && 战.击败数 > 0);
        var 伤害字 = 游戏.GetComponentsInChildren<Text>(true).Where(文 => 文.name == "伤害来源飘字").ToArray();
        检查("实机普通伤害飘字包含明确来源", 伤害字.Length > 0 && 伤害字.All(文 => 文.text.Contains("普通-")));
        检查("来源文字独立淡出且完整显示", 伤害字.All(文 => 文.GetComponent<CanvasGroup>() != null && !文.raycastTarget && 文.preferredHeight <= 文.rectTransform.rect.height + .1f));
        检查("战斗击杀实际给经验", 战.本局经验 > 0 && 网.当前经验 > 0);
        游戏.界面.更新战斗状态();
        检查("真实HUD分栏显示经验与等级", HUD.GetComponentsInChildren<Text>().Any(文 => 文.text.Contains("Lv.1") && 文.text.Contains("生命")) && HUD.GetComponentsInChildren<Text>().Any(文 => 文.text.Contains("经验")));
        int 原级 = 网.玩家等级, 原点 = 网.技能点; float 原血 = 人.当前血量;
        网.获得经验(网.升级所需经验 - 网.当前经验 + 5); 游戏.界面.更新战斗状态();
        检查("运行升级即时成长不补血", 人.等级 == 原级 + 1 && 网.技能点 == 原点 + 1 && 网.当前经验 == 5 && 人.当前血量 == 原血 && 近(人.血量, 60 * 天帝数值.成长(人.等级)));
        检查("经验和等级写入隔离档", 游戏.保存进度() && new 天帝存档().读取().画布.当前经验 == 5);
        场.战斗.伤害玩家((float)天帝数值.取("damage.technical_hit_max")); 游戏.返回主页();
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("战斗离场不重发等级技能点", 游戏.阶段 == 游戏阶段.主页 && 网.技能点 == 原点 + 1);
        检查("离场清理独立HUD和暂停状态", !游戏.界面.战斗已暂停 && !游戏.GetComponentsInChildren<RectTransform>().Any(区 => 区.name == "战斗HUD" && 区.gameObject.activeInHierarchy));
        检查("可自由选择100级图", 游戏.选择地图等级(100) && 游戏.进入战斗());
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        场 = 游戏.战斗场景; 场.enabled = false;
        var 王 = 场.战斗.敌人.Single(敌 => 敌.布点.级别 == 战斗敌人级别.王级);
        检查("真正场景100图BOSS105级固定属性", 王.等级 == 105 && 近(王.最大血量, 天帝数值配置.敌人(100, 3, 0)));
        var 首怪 = 场.战斗.敌人.First();
        检查("敌人不随低级玩家构筑变弱", 首怪.等级 == 100 && 近(首怪.最大血量, 天帝数值配置.敌人(100, 0, 0) * 天帝敌种配置.物种(首怪.物种, "hp_factor")));
        // 隔离场景的命中边界输入，仅验证六来源UI，不发测试道纹或改正式敌人强度。
        var 受击 = 场.战斗.敌人.First(敌 => 敌.存活);
        检查("场景混合命中成功", 场.战斗.伤害敌人(受击, new 战斗伤害包(1, 5, 人.等级, 1, 1, 1, 1, new 五行伤害分量(1, 1, 1, 1, 1))));
        游戏.界面.更新战斗状态(); Canvas.ForceUpdateCanvases();
        var 混合字 = 游戏.GetComponentsInChildren<Text>().FirstOrDefault(文 => 文.name == "伤害来源飘字" && 文.text.Contains("火-"));
        检查("六种来源在实机按色分行显示", 混合字 != null && 混合字.text.Count(字 => 字 == '\n') == 5 && 混合字.text.Contains("<color=#FF6254>火-") && 混合字.text.Contains("<color=#FFD15C>金-") && 混合字.text.Contains("<color=#65D887>木-"));
        检查("混合伤害多行排版完整且有暗描边", 混合字 != null && 混合字.preferredHeight <= 混合字.rectTransform.rect.height + .1f && 混合字.GetComponent<Outline>() != null);
        完成();
    }
    [Serializable] sealed class 开局节奏记录 { public float 前摇; public float[] 准备时间, 释放时间; }
    static void 记错(string 消息, string 栈, LogType 类型)
    { if (类型 == LogType.Error || 类型 == LogType.Exception || 类型 == LogType.Assert) { 结果.错误.Add(消息 + "\n" + 栈); 完成(); } }
    static void 完成()
    {
        if (已结束) return; 已结束 = true; EditorApplication.update -= 等待; Application.logMessageReceived -= 记错;
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true)); EditorApplication.isPlaying = false;
    }
    static void 清理(PlayModeStateChange 状态)
    {
        if (状态 != PlayModeStateChange.EnteredEditMode) return;
        if (!已结束) { 结果.错误.Add("检查提前停止"); 已结束 = true; }
        检查("真实玩家存档未改写", 指纹(真实路径) == 原文件指纹);
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        EditorApplication.update -= 等待; Application.logMessageReceived -= 记错; EditorApplication.playModeStateChanged -= 清理;
        天帝存档.验证目录 = 原存档目录; EditorSettings.enterPlayModeOptionsEnabled = 原选项启用; EditorSettings.enterPlayModeOptions = 原选项; Application.runInBackground = 原后台;
    }
}
#endif
