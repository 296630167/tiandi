#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 检查选择、存档迁移、波次和真实场景；仅在隔离存档中演练，不保存场景。
public static class 天帝选图波次验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录, 原存档目录;
    static bool 原后台, 原选项启用, 已启动, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static 天帝游戏 游戏;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 记错(string 文, string 栈, LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 结果.错误.Add(文 + "\n" + 栈); }
    public static string 启动()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("需停止运行，且当前没有未保存场景。");
        if (EditorSceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") throw new InvalidOperationException("请在主页场景运行检查。");
        var 美术 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        var 大图 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/天帝/美术/生成素材/青岚原_大地图.png");
        if (美术 == null || 大图 == null) throw new InvalidOperationException("青岚原大图或美术资源缺失。");
        var 项 = 美术.图片.FirstOrDefault(p => p.编号 == "MAP01");
        if (项 == null) 美术.图片 = 美术.图片.Concat(new[] { new 天帝美术资源.图片条目 { 编号 = "MAP01", 图片 = 大图 } }).ToArray(); else 项.图片 = 大图;
        EditorUtility.SetDirty(美术); AssetDatabase.SaveAssets();
        结果 = new 报告(); 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/选图波次-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        模型();
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 数据 = new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网, 42).导出库存(), 地图等级 = 1 };
        var 存 = new 天帝存档(Path.Combine(目录, "隔离存档")); if (!存.保存(数据)) throw new InvalidOperationException("隔离存档创建失败。");
        // 把真实旧格式字段缺省情况写在隔离目录，检查缺字段迁移和非法等级拒绝。
        var 旧 = new 天帝存档(Path.Combine(目录, "旧格式")); Directory.CreateDirectory(Path.GetDirectoryName(旧.路径));
        string 文 = JsonUtility.ToJson(数据).Replace("\"地图等级\":1,", ""); File.WriteAllText(旧.路径, 文);
        检查("旧存档缺地图等级迁移至1级", 旧.读取()?.地图等级 == 1);
        数据.地图等级 = 100; 检查("100级存档保存回读", 存.保存(数据) && 存.读取().地图等级 == 100);
        数据.地图等级 = 101; 检查("非法地图等级存档拒绝", !存.保存(数据)); 数据.地图等级 = 1; 存.保存(数据);
        原存档目录 = 天帝存档.验证目录; 天帝存档.验证目录 = Path.Combine(目录, "隔离存档");
        原后台 = Application.runInBackground; 原选项启用 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        已启动 = 已结束 = false; 截止 = EditorApplication.timeSinceStartup + 65;
        Application.logMessageReceived += 记错; EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 退出清理;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); EditorApplication.isPlaying = true;
        return 目录;
    }
    static void 模型()
    {
        try
        {
            var 图 = new 天帝战斗地图(42, true); 检查("固定大图中央出生可通行", 图.出生位置 == Vector2.zero && 图.可站立(图.出生位置));
            foreach (int 等级 in new[] { 1, 10, 100 })
                foreach (战斗敌人级别 类 in Enum.GetValues(typeof(战斗敌人级别)))
                {
                    var 敌 = new 战斗敌人(new 战斗敌人布点(类, Vector2.zero), 战斗难度.普通, 等级);
                    检查(等级 + "级地图的" + 类 + "实际等级", 敌.等级 == 等级 + new[] { 0, 1, 3, 5 }[(int)类]);
                    检查(等级 + "级地图的" + 类 + "数值有限有效", 敌.最大血量 > 0 && 敌.攻击力 > 0 && !float.IsInfinity(敌.最大血量));
                }
            var 低 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, Vector2.zero), 战斗难度.普通, 1);
            var 高 = new 战斗敌人(低.布点, 战斗难度.普通, 100); 检查("选择等级实际提高生命攻击防御", 高.最大血量 > 低.最大血量 && 高.攻击力 > 低.攻击力 && 高.防御 > 低.防御);
            var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var 战 = new 天帝战斗系统(图, 网, 人, 战斗难度.普通, new 天帝通货(网,42));
                检查("开局只生成8只普通，无精英头目BOSS", 战.场上敌人数量 == 8 && 战.分类在场(战斗敌人级别.普通) == 8 && 战.分类在场(战斗敌人级别.精英) == 0 && !战.BOSS已出现);
                检查("开局总剩余66和待刷新58", 战.剩余敌人数量 == 66 && 战.未生成敌人数量 == 58);
                检查("首批出生可站立且与玩家保持距离", 战.敌人.Where(e => e.存活).All(e => 图.可站立(e.位置) && Vector2.Distance(e.位置, Vector2.zero) >= 6));
                float 总 = 战.敌人.Where(e => e.布点.级别 != 战斗敌人级别.王级).Sum(e => e.最大血量);
                var 首 = 战.敌人.First(e => e.存活); 战.伤害敌人(首, 1000000);
                检查("过量伤害只计实际扣血且排除BOSS分母", Mathf.Abs(战.敌人损伤比例 - 首.最大血量 / 总) < .00001f && !战.BOSS已出现);
                int 击 = 战.击败数; 检查("重复死亡不重复扣剩余或掉落", !战.伤害敌人(首, 1000000) && 战.击败数 == 击);
                bool 安全 = true, 限额 = true, 无提前王 = true; var 各波 = new HashSet<int>();
                for (int i = 0; i < 800 && 战.剩余敌人数量 > 0; i++)
                {
                    各波.Add(战.当前波次);
                    foreach (var 敌 in 战.敌人.Where(e => e.存活).ToArray()) 战.伤害敌人(敌, 1000000);
                    战.推进(Vector2.zero, .1f);
                    安全 &= 战.敌人.Where(e => e.存活).All(e => 图.可站立(e.位置));
                    限额 &= 战.场上敌人数量 <= 天帝地图挑战.场上上限 + 1 && 战.本次寻路次数 <= 4;
                    无提前王 &= !战.BOSS已出现 || 战.敌人损伤比例 >= .8f;
                }
                检查("五波全部经过且普通60精英4头目1王1完整生成", 各波.Count == 5 && 战.敌人.All(e => e.已生成) && 战.击败数 == 66);
                检查("各波敌人位置合法与寻路预算有界", 安全 && 限额);
                检查("BOSS在80%实际生命损伤后出现", 战.BOSS已出现 && 无提前王);
                检查("清场总数与分类剩余都归零", 战.剩余敌人数量 == 0 && Enum.GetValues(typeof(战斗敌人级别)).Cast<战斗敌人级别>().All(e => 战.分类剩余(e) == 0));
                var 停 = new 天帝战斗系统(图, 网, 人, 战斗难度.普通); 停.伤害玩家(100000); int 生成前 = 停.未生成敌人数量;
                for(int i=0;i<100;i++) 停.推进(Vector2.zero,.25f);
                检查("玩家死亡停止后续刷新", 停.未生成敌人数量 == 生成前);
            }
        }
        catch(Exception ex) { 结果.错误.Add(ex.ToString()); }
    }
    static void 等待()
    {
        if (已结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("选图实战检查超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate();
        游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>(); if (游戏 == null || 已启动) return;
        已启动 = true; 游戏.StartCoroutine(捕获异常(实战()));
    }
    static IEnumerator 捕获异常(IEnumerator 动作)
    {
        while(true)
        {
            bool 继续 = false; object 当前 = null; Exception 异常 = null;
            try { 继续=动作.MoveNext(); if(继续) 当前=动作.Current; } catch(Exception ex) { 异常=ex; }
            if(异常 != null) { 结果.错误.Add(异常.ToString()); 完成(); yield break; }
            if(!继续) yield break; yield return 当前;
        }
    }
    static Button 找键(string 名) => 游戏.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == 名 && b.gameObject.activeInHierarchy);
    static void 点(string 名)
    {
        var 键 = 找键(名); if(键 == null || !键.interactable) throw new InvalidOperationException("按钮不可点击："+名);
        Canvas.ForceUpdateCanvases(); var r = (RectTransform)键.transform;
        var e = new PointerEventData(EventSystem.current) { position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)) };
        var 命 = new List<RaycastResult>(); EventSystem.current.RaycastAll(e,命);
        if(命.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(命[0].gameObject) != 键.gameObject)
            throw new InvalidOperationException("按钮被遮挡："+名+"；目标位置="+e.position+"；命中="+string.Join(",",命.Take(4).Select(m=>m.gameObject.name+"/"+m.gameObject.activeInHierarchy))+"；确认="+游戏.界面.确认已打开);
        ExecuteEvents.Execute(键.gameObject,e,ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 截图(string 名)
    {
        yield return new WaitForEndOfFrame();
        var 图=ScreenCapture.CaptureScreenshotAsTexture();
        if(图 != null) { File.WriteAllBytes(Path.Combine(目录,名+".png"),图.EncodeToPNG()); UnityEngine.Object.Destroy(图); }
    }
    static IEnumerator 实战()
    {
        检查("隔离存档进入主页", 游戏.继续游戏()); yield return null;
        var 下拉 = 游戏.GetComponentsInChildren<Dropdown>().Single(d => d.name == "地图等级下拉");
        检查("100档可选且默认1级", 下拉.options.Count == 100 && 下拉.options[0].text == "1级" && 下拉.options[99].text == "100级" && 下拉.value == 0);
        检查("三张方形地图卡常驻，后两张无选择控件", 游戏.GetComponentsInChildren<RectTransform>().Count(r=>r.name.StartsWith("地图卡")) == 3 && 游戏.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("地图卡")) == 1);
        检查("旧选图弹窗和模式按钮已移除", 找键("普通模式") == null && 找键("困难模式") == null && 找键("进入战斗") == null);
        检查("锁定地图和非法等级API拒绝", !游戏.选择战斗地图(1) && !游戏.选择战斗地图(2) && !游戏.选择地图等级(0) && !游戏.选择地图等级(101));
        下拉.value=99; 检查("100级同步详细等级", 游戏.当前地图等级 == 100 && 游戏.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("BOSS Lv.105")));
        检查("选中等级可保存回读", 游戏.保存进度() && new 天帝存档().读取().地图等级 == 100);
        下拉.value=36; ExecuteEvents.Execute(下拉.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler); yield return new WaitForSecondsRealtime(.2f);
        检查("真实Dropdown生成完整列表且选中37级", 下拉.transform.Find("Dropdown List")?.GetComponentsInChildren<Toggle>().Length >= 100 && 游戏.当前地图等级 == 37);
        var 滚=下拉.transform.Find("Dropdown List").GetComponent<ScrollRect>();
        var 选项=下拉.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>().Single(t=>t.gameObject.activeSelf && t.isOn);
        var 区=RectTransformUtility.CalculateRelativeRectTransformBounds(滚.viewport,选项.transform);
        检查("下拉滚动使当前37级选项可见", 区.center.y >= 滚.viewport.rect.yMin && 区.center.y <= 滚.viewport.rect.yMax);
        yield return 截图("01-等级下拉"); 下拉.Hide(); yield return new WaitForSecondsRealtime(.2f);
        点("开始游戏"); 检查("开始只打开确认不进入战斗", 游戏.界面.确认已打开 && 游戏.阶段 == 游戏阶段.主页);
        检查("确认显示选中地图等级和BOSS等级", 游戏.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("地图等级 37") && t.text.Contains("BOSS Lv.42")));
        检查("确认期间不能改等级或越过确认进入", !游戏.选择地图等级(80) && !游戏.进入战斗());
        检查("确认时主页控件禁用且键盘焦点在取消", !下拉.interactable && EventSystem.current.currentSelectedGameObject == 找键("取消").gameObject);
        yield return 截图("02-进入确认"); 点("取消");
        检查("取消保持主页与等级选择", !游戏.界面.确认已打开 && 游戏.当前地图等级 == 37 && 下拉.interactable);
        下拉.value=0; yield return 截图("03-主页常驻选图"); 点("开始游戏"); yield return new WaitForSecondsRealtime(.2f); 点("确认进入");
        while(游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("确认成功加载战斗", 游戏.阶段 == 游戏阶段.战斗 && 游戏.战斗场景 != null);
        var 场=游戏.战斗场景; 场.enabled=false;
        场.美术.更新(场.战斗,场.玩家位置,0);
        var 狼=场.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name.EndsWith("狼") && s.enabled).ToArray();
        检查("场景只显示已刷新8只狼，无未登场假尸体", 狼.Length == 8);
        检查("真实地图中央出生", 场.玩家位置 == Vector2.zero);
        检查("真实战斗使用选择等级和首组普通", 场.战斗.地图等级 == 1 && 场.战斗.分类在场(战斗敌人级别.普通) == 8 && 场.战斗.未生成敌人数量 == 58);
        游戏.界面.更新战斗状态(); Canvas.ForceUpdateCanvases();
        var HUD = 游戏.GetComponentsInChildren<Text>().Single(t=>t.text.Contains("待刷新") && t.text.Contains("清剿进度"));
        检查("HUD包含总剩余分类剩余场上待刷新", HUD.text.Contains("剩余 66 / 66") && HUD.text.Contains("普通 60") && HUD.text.Contains("精英 4") && HUD.text.Contains("头目 1") && HUD.text.Contains("BOSS 1"));
        检查("敌人统计文字没有越出面板", HUD.preferredHeight <= HUD.rectTransform.rect.height);
        yield return 截图("04-战斗统计");
        场.战斗.伤害玩家(100000); 游戏.返回主页(); while(游戏.阶段 == 游戏阶段.战斗加载) yield return null;
        检查("回主页仍锁定后两图且等级保留", 游戏.阶段 == 游戏阶段.主页 && 游戏.当前地图等级 == 1 && !游戏.选择战斗地图(2));
        完成();
    }
    static void 完成()
    {
        if(已结束) return; 已结束=true; EditorApplication.update-=等待; Application.logMessageReceived-=记错;
        File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); EditorApplication.isPlaying=false;
    }
    static void 退出清理(PlayModeStateChange 状态)
    {
        if(状态 != PlayModeStateChange.EnteredEditMode) return;
        if(!已结束) { 结果.错误.Add("检查提前结束"); File.WriteAllText(Path.Combine(目录,"report.json"),JsonUtility.ToJson(结果,true)); }
        已结束=true; EditorApplication.update-=等待; Application.logMessageReceived-=记错; EditorApplication.playModeStateChanged-=退出清理;
        天帝存档.验证目录=原存档目录; Application.runInBackground=原后台;
        EditorSettings.enterPlayModeOptionsEnabled=原选项启用; EditorSettings.enterPlayModeOptions=原选项;
    }
}

public static class 天帝选图试玩
{
    static string 原存档目录;
    static bool 原选项启用;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    public static string 打开()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("试玩需处于已保存的编辑状态。");
        if(EditorSceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity") EditorSceneManager.OpenScene("Assets/天帝/场景/天帝.unity");
        var 数据 = new 天帝存档().读取();
        if(数据 == null)
        {
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            数据=new 天帝存档数据 {序章已完成=true,主角=天帝普攻.主角配置(),画布=网.导出存档(),通货=new 天帝通货(网,42).导出库存(),地图等级=1};
        }
        var 目录=Path.Combine(天帝构建工具.项目根,"生成/验证/选图试玩-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),"隔离存档");
        if(!new 天帝存档(目录).保存(数据)) throw new InvalidOperationException("试玩存档副本创建失败。");
        原存档目录=天帝存档.验证目录;天帝存档.验证目录=目录;
        原选项启用=EditorSettings.enterPlayModeOptionsEnabled;原选项=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        截止=EditorApplication.timeSinceStartup+20;EditorApplication.update+=进入主页;EditorApplication.playModeStateChanged+=清理;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorApplication.isPlaying=true;
        return "已启动新主页试玩；右下角选择地图等级，再点击开始游戏。使用隔离存档副本。";
    }
    static void 进入主页()
    {
        if(EditorApplication.timeSinceStartup>截止) {EditorApplication.isPlaying=false;EditorApplication.update-=进入主页;return;}
        if(!EditorApplication.isPlaying)return;
        EditorApplication.QueuePlayerLoopUpdate();
        var 游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();if(游戏==null)return;
        if(游戏.继续游戏()) {EditorApplication.update-=进入主页;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();}
    }
    static void 清理(PlayModeStateChange 状态)
    {
        if(状态!=PlayModeStateChange.EnteredEditMode)return;
        EditorApplication.update-=进入主页;EditorApplication.playModeStateChanged-=清理;
        天帝存档.验证目录=原存档目录;EditorSettings.enterPlayModeOptionsEnabled=原选项启用;EditorSettings.enterPlayModeOptions=原选项;
    }
}
#endif
