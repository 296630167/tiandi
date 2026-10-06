#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Process = System.Diagnostics.Process;

// 仅编辑器采集。复制现有真实存档，正式UI、正式数值和真实掉落；不注入成长或资源。
public static class 天帝第二期采集
{
    [Serializable] public sealed class 片段 { public string 名; public int 开始帧, 结束帧; public float 建议秒; }
    [Serializable] public sealed class 采集报告
    {
        public string 方法 = "现有真实进度的隔离副本；正式数值；真实UI与战斗；无额外资源或直接伤害";
        public string 原存档指纹, 结束存档指纹, 原始视频;
        public bool 完成, 王已击败, 玩家死亡;
        public int 帧数, 宽, 高, 开始等级, 结束等级, 击败数;
        public List<片段> 片段 = new List<片段>();
        public List<string> 截图 = new List<string>(), 操作 = new List<string>(), 错误 = new List<string>();
    }
    static string 目录, 成品, 原验证目录, 真实路径;
    static 采集报告 报告;
    static 天帝游戏 游戏;
    static EditorWindow 窗口;
    static Process 编码;
    static Text 字幕;
    static Image 指针;
    static Canvas 标注;
    static bool 结束, 已开始, 原后台, 原选项启用, 原最大;
    static bool 仅宝盒;
    static int 原帧率, 原尺寸, 自定义序, 原序章值;
    static EnterPlayModeOptions 原模式;
    static double 截止;
    static readonly WaitForEndOfFrame 帧末 = new WaitForEndOfFrame();
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static string 指纹(string p) { if (!File.Exists(p)) return "无文件"; using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-", ""); }
    public static string 启动() { 仅宝盒 = false; return 准备(); }
    public static string 补采宝盒() { 仅宝盒 = true; return 准备(); }
    static string 准备()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("需停止播放且无未保存场景，采集不覆盖场景。");
        if (SceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("需在现有主场景采集。");
        天帝数值同步检查.校验();
        真实路径 = Path.Combine(Application.persistentDataPath, "天帝进度.json");
        if (!File.Exists(真实路径)) throw new InvalidOperationException("没有可供拍摄的真实进度。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/开发日志二期采集-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        成品 = Path.Combine(天帝构建工具.项目根, "开发日志第二期");
        Directory.CreateDirectory(Path.Combine(成品, "截图")); Directory.CreateDirectory(目录);
        var 隔离 = Path.Combine(目录, "隔离存档"); Directory.CreateDirectory(隔离);
        File.Copy(真实路径, Path.Combine(隔离, "天帝进度.json"));
        报告 = new 采集报告 { 原存档指纹 = 指纹(真实路径), 原始视频 = Path.Combine(目录, "原始录制.mp4") };
        原验证目录 = 天帝存档.验证目录; 天帝存档.验证目录 = 隔离;
        原后台 = Application.runInBackground; 原帧率 = Time.captureFramerate;
        原序章值 = PlayerPrefs.GetInt("Tiandi.Menu.PrologueSeen", 0);
        原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原模式 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        窗口 = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); 原最大 = 窗口.maximized;
        窗口.maximized = true; 设置尺寸(); 窗口.Focus();
        结束 = 已开始 = false; 游戏 = null; 编码 = null; 标注 = null; 字幕 = null; 指针 = null;
        截止 = EditorApplication.timeSinceStartup + 1500;
        EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 退出清理; Application.logMessageReceived += 记错;
        EditorApplication.isPlaying = true; return 目录;
    }
    static object 尺寸组()
    {
        var a = typeof(Editor).Assembly; var t = a.GetType("UnityEditor.GameViewSizes");
        var 单例 = typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        return t.GetMethod("GetGroup").Invoke(单例, new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeGroupType"), 0) });
    }
    static void 设置尺寸()
    {
        var a = typeof(Editor).Assembly; var g = 尺寸组(); var t = a.GetType("UnityEditor.GameViewSize");
        var c = t.GetConstructor(隐, null, new[] { a.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) }, null);
        自定义序 = (int)g.GetType().GetMethod("GetCustomCount").Invoke(g, null);
        g.GetType().GetMethod("AddCustomSize").Invoke(g, new[] { c.Invoke(new[] { Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"), 1), (object)1920, 1080, "第二期采集1080p" }) });
        var p = 窗口.GetType().GetProperty("selectedSizeIndex", 隐); 原尺寸 = (int)p.GetValue(窗口);
        p.SetValue(窗口, (int)g.GetType().GetMethod("GetBuiltinCount").Invoke(g, null) + 自定义序);
    }
    static void 等待()
    {
        if (结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 报告.错误.Add("采集超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        if (已开始) return;
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null || 游戏.阶段 != 游戏阶段.标题) return;
        已开始 = true; 游戏.StartCoroutine(保护(仅宝盒 ? 宝盒补采流程() : 流程()));
    }
    static IEnumerator 保护(IEnumerator 流)
    {
        var s = new Stack<IEnumerator>(); s.Push(流);
        while (s.Count > 0)
        {
            bool 有; object x = null;
            try { 有 = s.Peek().MoveNext(); if (有) x = s.Peek().Current; }
            catch (Exception ex) { 报告.错误.Add(ex.ToString()); break; }
            if (!有) { s.Pop(); continue; }
            if (x is IEnumerator 子) { s.Push(子); continue; }
            yield return x;
        }
        完成();
    }
    static void 建字幕()
    {
        var r = new GameObject("第二期录制说明_仅编辑器", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); r.transform.SetParent(游戏.transform, false);
        标注 = r.GetComponent<Canvas>(); 标注.renderMode = RenderMode.ScreenSpaceOverlay; 标注.sortingOrder = 500;
        var s = r.GetComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(1920, 1080);
        var b = new GameObject("字幕背景", typeof(RectTransform), typeof(Image)); b.transform.SetParent(r.transform, false);
        var rect = (RectTransform)b.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(1090, 44); rect.anchoredPosition = new Vector2(0, -2);
        b.GetComponent<Image>().color = new Color(.01f, .025f, .025f, .9f); b.GetComponent<Image>().raycastTarget = false;
        var n = new GameObject("字幕", typeof(RectTransform), typeof(Text)); n.transform.SetParent(b.transform, false);
        var nr = (RectTransform)n.transform; nr.anchorMin = Vector2.zero; nr.anchorMax = Vector2.one; nr.offsetMin = new Vector2(12, 0); nr.offsetMax = new Vector2(-12, 0);
        字幕 = n.GetComponent<Text>(); 字幕.font = 游戏.默认字体; 字幕.fontSize = 23; 字幕.color = new Color(.95f, .96f, .90f); 字幕.alignment = TextAnchor.MiddleCenter; 字幕.raycastTarget = false;
        var p = new GameObject("演示指针", typeof(RectTransform), typeof(Image)); p.transform.SetParent(r.transform, false); 指针 = p.GetComponent<Image>(); 指针.raycastTarget = false;
        指针.rectTransform.sizeDelta = new Vector2(13, 13); 指针.color = new Color(.95f, .8f, .33f); 指针.gameObject.SetActive(false);
    }
    static void 段(string 名, float 建议秒)
    {
        if (报告.片段.Count > 0) 报告.片段.Last().结束帧 = 报告.帧数;
        报告.片段.Add(new 片段 { 名 = 名, 开始帧 = 报告.帧数, 建议秒 = 建议秒 }); 字幕.text = 名;
        进度();
    }
    static void 进度() { File.WriteAllText(Path.Combine(目录, "progress.json"), JsonUtility.ToJson(报告, true)); }
    static void 指(Vector2 p) { 指针.gameObject.SetActive(true); 指针.rectTransform.position = p; }
    static Button 找按钮(string 名) => 游戏.GetComponentsInChildren<Button>().FirstOrDefault(b => b.gameObject.activeInHierarchy && b.name == 名) ?? throw new InvalidOperationException("找不到按钮：" + 名);
    static void 点(string 名)
    {
        Canvas.ForceUpdateCanvases(); var b = 找按钮(名); if (!b.interactable) throw new InvalidOperationException("按钮不可用：" + 名);
        var rect = (RectTransform)b.transform; var p = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); 指(p);
        b.onClick.Invoke(); 报告.操作.Add("UI：" + 名);
    }
    static IEnumerator 录(int 数, Action<int> 动作 = null)
    {
        for (int i = 0; i < 数; i++)
        {
            动作?.Invoke(i); yield return 帧末;
            var 图 = ScreenCapture.CaptureScreenshotAsTexture();
            if (编码 == null)
            {
                报告.宽 = 图.width; 报告.高 = 图.height;
                if (图.width != 1920 || 图.height != 1080) throw new InvalidOperationException("GameView尺寸不正确：" + 图.width + "x" + 图.height);
                编码 = new Process(); 编码.StartInfo.FileName = Path.Combine(天帝构建工具.项目根, "生成/录制工具/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe");
                编码.StartInfo.Arguments = "-hide_banner -loglevel error -y -f image2pipe -framerate 30 -vcodec mjpeg -i - -an -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p \"" + 报告.原始视频 + "\"";
                编码.StartInfo.UseShellExecute = false; 编码.StartInfo.CreateNoWindow = true; 编码.StartInfo.RedirectStandardInput = true; 编码.Start();
            }
            var bytes = 图.EncodeToJPG(90); 编码.StandardInput.BaseStream.Write(bytes, 0, bytes.Length); UnityEngine.Object.Destroy(图); 报告.帧数++;
        }
    }
    static IEnumerator 拍(string 名)
    {
        标注.enabled = false; yield return 帧末;
        var t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(成品, "截图", 名 + ".png"), t.EncodeToPNG()); UnityEngine.Object.Destroy(t);
        标注.enabled = true; 报告.截图.Add(名); 进度();
    }
    static IEnumerator 流程()
    {
        for (int i = 0; i < 10; i++) yield return 帧末;
        Time.captureFramerate = 30; 建字幕();
        var 输入 = 游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        段("第二期开发日志 · 从接口拼接到通路构筑", 3); yield return 录(90); yield return 拍("01_标题");
        游戏.确认开始新游戏(); 段("序章仍采用插画演出 · 进度可拖动，也可以跳过", 6); yield return 录(90); yield return 拍("02_序章进度");
        游戏.调整序章进度(.69f); yield return 录(90); 游戏.跳过序章();
        段("五枚源道纹从场景飞近 · 悬停查看天赋，再作选择", 7);
        int 等 = 0; while (!游戏.界面.源道纹页.可选择 && 等++ < 240) yield return 录(1);
        游戏.界面.源道纹页.显示详情(0, new Vector2(500, 500)); yield return 录(90); yield return 拍("03_源道纹天赋选择");
        游戏.返回标题(); 游戏.继续游戏(); 报告.开始等级 = 游戏.道纹数据.玩家等级;
        段("主页常驻三张地图卡 · 当前开放青岚原", 4); 指针.gameObject.SetActive(false); yield return 录(120); yield return 拍("04_主页地图");
        var 下拉 = 游戏.GetComponentsInChildren<Dropdown>().First(); 下拉.Show(); yield return 录(90); yield return 拍("05_地图等级下拉"); 下拉.Hide();
        游戏.选择地图等级(1); 游戏.界面.显示主页();
        游戏.打开道纹(); var 页 = 游戏.界面.道纹页;
        段("墨青画布与道纹藏匣 · 右侧实时查看通路参数", 5); yield return 录(150); yield return 拍("06_道纹构筑");
        页.显示属性(游戏.道纹数据.已放置[Vector2Int.zero], new Vector2(670, 580)); yield return 录(90); yield return 拍("07_源纹接口解封"); 页.指针离开();
        var 未接 = 游戏.道纹数据.道纹.First(x => !x.格子.HasValue && x.分类 == 道纹分类.属性);
        yield return 拖入(页, 未接, new Vector2Int(4, 0), -1, true);
        段("没有接通就不会生效 · 定位原因，右键旋转，双击收回", 6);
        页.定位未接通(); yield return 录(90); yield return 拍("08_未接通定位");
        页.点击(true, new PointerEventData(EventSystem.current) { position = 页.格屏幕位置(new Vector2Int(4, 0)), clickCount = 2, button = PointerEventData.InputButton.Left }); yield return 录(90); 页.聚焦解锁区域();
        段("筛选属性与接口方向 · 可以包含旋转后能匹配的道纹", 5); 页.打开筛选(); 页.设置接口筛选(8, true); yield return 录(150); yield return 拍("09_接口筛选"); 页.关闭筛选(); 页.设置接口筛选(0, false);
        段("布局方案保存实际道纹 · 三个命名槽位，载入前检查", 5); 页.打开布局方案();
        var 输入名 = 页.GetComponentsInChildren<InputField>().First(); 输入名.text = "青岚原初行"; 点("保存当前"); yield return 录(150); yield return 拍("10_布局方案"); 页.关闭筛选(); 游戏.返回主页();
        段("图鉴新增分类查询 · 直接阅读天赋、词条和正式数值区间", 8); 游戏.界面.显示图鉴(); yield return 录(90); yield return 拍("11_道纹图鉴天赋");
        游戏.界面.图鉴页.定位分类("普通属性"); yield return 录(75); yield return 拍("12_普通属性与成长");
        游戏.界面.图鉴页.定位分类("分叉道纹"); yield return 录(75); yield return 拍("13_分叉与连接规则"); 游戏.界面.关闭图鉴();
        段("进入前确认地图等级 · 精英 +1，头目 +3，BOSS +5", 5); 游戏.界面.请求进入地图(); yield return 录(150); yield return 拍("14_进入地图确认"); 点("确认进入");
        while (游戏.阶段 == 游戏阶段.战斗加载) yield return 录(1);
        var 场 = 游戏.战斗场景; 场.enabled = false; 指针.gameObject.SetActive(false);
        段("青岚原回归纯2D · 屏外刷怪，敌人追击玩家", 20);
        yield return 录(300, i => 战斗帧(场)); yield return 拍("15_纯2D战场与紧凑HUD");
        段("普通伤害与五行伤害分开飘字 · 颜色对应来源", 15);
        yield return 录(300, i => 战斗帧(场)); yield return 拍("16_伤害来源与敌人血条");
        段("道纹、通货、灵石全图自动入库 · 每条拾取提示独立停留", 15);
        yield return 录(300, i => 战斗帧(场)); yield return 拍("17_自动吸附拾取");
        if (!场.战斗.玩家死亡)
        {
            游戏.界面.切换战斗暂停(); 段("暂停后查看完整统计与构筑 · 战斗信息移到四角", 5); yield return 录(150); yield return 拍("18_战斗暂停详情"); 游戏.界面.关闭战斗暂停();
        }
        段("五组普通敌人穿插精英与头目 · 非BOSS血损80%召出狼王", 25);
        bool 拍王 = false; int 剩 = 0;
        while (!场.可离开 && 剩++ < 6600)
        {
            yield return 录(1, i => 战斗帧(场));
            if (剩 % 300 == 0) { 进度(); File.WriteAllText(Path.Combine(目录, "战斗进度.txt"), "秒=" + 剩 / 30 + " 等级=" + 游戏.主角属性.等级 + " 血=" + 游戏.主角属性.当前血量 + " 剩余=" + 场.战斗.剩余敌人数量 + " 王=" + 场.战斗.BOSS已出现); }
            var 王 = 场.战斗.敌人.FirstOrDefault(x => x.存活 && x.布点.级别 == 战斗敌人级别.王级);
            if (!拍王 && 王 != null && Vector2.Distance(场.玩家位置, 王.位置) < 13)
            { 拍王 = true; 段("青鬃狼王从地图北端登场 · 追击与范围攻击带来走位压力", 18); yield return 拍("19_青鬃狼王挑战"); }
        }
        if (!场.可离开)
        {
            // 拍摄时限不改战斗结果；从暂停页按正常离场入口结束本次尝试。
            游戏.界面.切换战斗暂停(); yield return 录(60);
            // 正常入口目前只允许死亡/王死离场，继续真实战斗到合法结束。
            游戏.界面.关闭战斗暂停();
            for (int i = 0; i < 3600 && !场.可离开; i++) yield return 录(1, n => 场.战斗一步(1f / 30));
        }
        报告.王已击败 = 场.王已击败; 报告.玩家死亡 = 场.战斗.玩家死亡;
        报告.击败数 = 场.战斗.敌人.Count(x => x.已生成 && x.血量 <= 0);
        报告.结束等级 = 游戏.道纹数据.玩家等级;
        段(场.王已击败 ? "挑战结束 · 已入库的战利品随玩家返回" : "这次挑战告一段落 · 保留已获得的战利品", 4); yield return 录(120); yield return 拍("20_战斗结果");
        if (!场.可离开) throw new InvalidOperationException("真实挑战未结束，未伪造结束状态。");
        游戏.返回主页(); while (游戏.阶段 == 游戏阶段.战斗) yield return 录(1);
        段("灵石用于开宝盒 · 属性、功能和分叉三种选择", 8); 游戏.界面.显示宝盒(); yield return 录(90);
        var 开 = 游戏.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("抽取") && b.interactable).ToArray();
        if (开.Length > 0) { 指针.gameObject.SetActive(true); 开[0].onClick.Invoke(); 报告.操作.Add("真实灵石宝盒抽取"); }
        yield return 录(150); yield return 拍("21_宝盒与抽取日志"); 游戏.界面.关闭宝盒();
        段("11种通货改造道纹 · 数量醒目，介绍下移", 8); 游戏.打开道纹改造(); var 改 = 游戏.界面.改造页;
        var 目标 = 游戏.道纹数据.道纹.First(x => x.品阶 == 道纹品阶.普通 && x.分类 == 道纹分类.属性 && !x.是五行道纹);
        改.选目标(游戏.道纹数据.道纹.IndexOf(目标)); 改.选通货(通货种类.启灵石); yield return 录(90); yield return 拍("22_通货改造界面");
        if (改.可执行) { 改.执行(); 报告.操作.Add("消耗真实启灵石：" + 改.最近结果); }
        yield return 录(150); yield return 拍("23_改造结果"); 游戏.返回主页();
        游戏.打开道纹(); 页 = 游戏.界面.道纹页;
        段("战斗获得经验与技能点 · 回来解锁格子、继续调整构筑", 10);
        var 格 = new Vector2Int(1, 1);
        if (游戏.道纹数据.技能点 > 0 && !游戏.道纹数据.格已解锁(格))
        { 页.点击(true, new PointerEventData(EventSystem.current) { position = 页.格屏幕位置(格), button = PointerEventData.InputButton.Left }); 报告.操作.Add("消耗升级技能点解锁(1,1)"); yield return 录(60); }
        if (游戏.道纹数据.格已解锁(格) && !游戏.道纹数据.已放置.ContainsKey(格))
        {
            var 纹 = 游戏.道纹数据.道纹.First(x => !x.格子.HasValue && x.分类 == 道纹分类.属性 && x.属性 == 道纹属性.木);
            yield return 拖入(页, 纹, 格, 4, false);
        }
        页.聚焦解锁区域(); 页.指针离开(); yield return 录(180); yield return 拍("24_成长后重新构筑"); 游戏.返回主页();
        段("目前仍是可玩的原型 · 继续打磨战斗、动画与成长节奏", 4); 指针.gameObject.SetActive(false); yield return 录(120);
        游戏.保存进度(); 报告.完成 = true;
    }
    static IEnumerator 宝盒补采流程()
    {
        for (int i = 0; i < 10; i++) yield return 帧末;
        Time.captureFramerate = 30; 建字幕();
        var 输入 = 游戏.GetComponentInChildren<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); if (输入 != null) 输入.enabled = false;
        游戏.继续游戏(); yield return 录(30);
        段("灵石用于开宝盒 · 属性、功能和分叉三种选择", 10);
        游戏.界面.显示宝盒(); yield return 录(120);
        var 键 = 游戏.GetComponentsInChildren<Button>().Where(b => b.name == "抽取" && b.interactable).ToArray();
        if (键.Length == 0) throw new InvalidOperationException("宝盒入口未显示可用抽取键");
        键[0].onClick.Invoke(); 报告.操作.Add("正常宝盒抽取：消耗100灵石"); yield return 录(120);
        yield return 拍("21_宝盒与抽取日志");
        var 悬 = 游戏.GetComponentsInChildren<天帝宝盒日志悬停>().FirstOrDefault();
        if (悬 != null)
        {
            var rect = (RectTransform)悬.transform; var p = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            ExecuteEvents.Execute(悬.gameObject, new PointerEventData(EventSystem.current) { position = p }, ExecuteEvents.pointerEnterHandler);
            yield return 录(90); yield return 拍("26_宝盒结果悬停详情");
        }
        报告.完成 = true; 游戏.保存进度();
    }
    static IEnumerator 拖入(天帝道纹界面 页, 道纹实例 纹, Vector2Int 格, int 接口, bool 拍预览)
    {
        int 序 = 游戏.道纹数据.道纹.IndexOf(纹); 页.设置候选排序(道纹候选排序.默认); 页.切换候选页(序 / 8); Canvas.ForceUpdateCanvases();
        var 起 = 页.候选屏幕位置(序); var 终 = 页.格屏幕位置(格);
        var e = new PointerEventData(EventSystem.current) { position = 起, button = PointerEventData.InputButton.Left };
        页.按下(纹, false, e); 页.开始拖动(false, e);
        if (接口 >= 0) for (int i = 0; i < 6 && (页.拖动接口.Value & (1 << 接口)) == 0; i++) 页.旋转拖动道纹();
        yield return 录(45, i => { var p = Vector2.Lerp(起, 终, (i + 1) / 45f); 指(p); 页.拖动(new PointerEventData(EventSystem.current) { position = p, button = PointerEventData.InputButton.Left }); });
        if (拍预览) { yield return 录(40); yield return 拍("25_拖放沙盘对比"); }
        页.结束拖动(new PointerEventData(EventSystem.current) { position = 终, button = PointerEventData.InputButton.Left });
        报告.操作.Add("放置实际道纹#" + 纹.编号 + "到" + 格); yield return 录(30);
    }
    static void 战斗帧(天帝战斗场景 场)
    {
        if (场.战斗.玩家死亡 || 场.王已击败) { 场.战斗一步(1f / 30); return; }
        var 人 = 场.玩家位置; var 敌 = 场.战斗.敌人.Where(x => x.存活).ToArray();
        Vector2 最好 = Vector2.zero; float 分 = float.MinValue;
        for (int i = 0; i <= 16; i++)
        {
            var 向 = i == 16 ? Vector2.zero : new Vector2(Mathf.Cos(i * Mathf.PI / 8), Mathf.Sin(i * Mathf.PI / 8));
            var 点 = 场.地图.移动(人, 向, 游戏.主角属性.跑步速度 * .45f);
            if (向 != Vector2.zero && (点 - 人).sqrMagnitude < .1f) continue;
            float s = -(点.magnitude * .028f); float 近 = 100;
            foreach (var x in 敌)
            {
                float 距 = Vector2.Distance(点, x.位置); 近 = Mathf.Min(近, 距);
                float 安 = x.布点.级别 == 战斗敌人级别.王级 ? 6 : x.布点.级别 == 战斗敌人级别.普通 ? 3 : 4.5f;
                if (距 < 安) s -= (安 - 距) * (安 - 距) * 14;
            }
            if (敌.Length > 0) s -= Mathf.Abs(近 - 6) * .9f;
            else s -= Vector2.Distance(点, Vector2.zero) * .4f;
            s += .12f * Mathf.Sin(报告.帧数 / 120f + i);
            if (s > 分) { 分 = s; 最好 = 向; }
        }
        场.移动一步(最好, true, 1f / 30); 场.战斗一步(1f / 30);
    }
    static void 记错(string 文, string 栈, LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 报告.错误.Add(文 + "\n" + 栈); }
    static void 完成()
    {
        if (结束) return; 结束 = true;
        if (报告.片段.Count > 0) 报告.片段.Last().结束帧 = 报告.帧数;
        if (编码 != null) { 编码.StandardInput.Close(); if (!编码.WaitForExit(10000)) { 编码.Kill(); 报告.错误.Add("编码超时"); } else if (编码.ExitCode != 0) 报告.错误.Add("编码失败" + 编码.ExitCode); 编码.Dispose(); 编码 = null; }
        EditorApplication.update -= 等待; Application.logMessageReceived -= 记错; Time.captureFramerate = 原帧率;
        File.WriteAllText(Path.Combine(目录, "录制报告.json"), JsonUtility.ToJson(报告, true));
        EditorApplication.isPlaying = false;
    }
    static void 退出清理(PlayModeStateChange 状态)
    {
        if (状态 != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= 退出清理;
        天帝存档.验证目录 = 原验证目录; Application.runInBackground = 原后台;
        Time.captureFramerate = 原帧率; EditorSettings.enterPlayModeOptionsEnabled = 原选项启用; EditorSettings.enterPlayModeOptions = 原模式;
        PlayerPrefs.SetInt("Tiandi.Menu.PrologueSeen", 原序章值); PlayerPrefs.Save();
        if (窗口 != null) { 窗口.GetType().GetProperty("selectedSizeIndex", 隐).SetValue(窗口, 原尺寸); 窗口.maximized = 原最大; }
        var g = 尺寸组(); g.GetType().GetMethod("RemoveCustomSize").Invoke(g, new object[] { 自定义序 });
        报告.结束存档指纹 = 指纹(真实路径); if (报告.结束存档指纹 != 报告.原存档指纹) 报告.错误.Add("真实存档指纹变化");
        File.WriteAllText(Path.Combine(目录, "录制报告.json"), JsonUtility.ToJson(报告, true)); 游戏 = null;
    }
}
#endif
