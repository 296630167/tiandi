#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 定向验证：真实主页入口、全条目可读、等级查询与正式掉落区间；不向玩家发放测试物品。
public static class 天帝道纹图鉴验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 目录, 原存档, 真档, 真档指纹;
    static bool 原后台, 原选项开, 已开始, 已结束;
    static EnterPlayModeOptions 原选项;
    static double 截止;
    static 天帝游戏 游戏;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static string 指纹(string 路径) { using (var 算 = SHA256.Create()) return File.Exists(路径) ? Convert.ToBase64String(算.ComputeHash(File.ReadAllBytes(路径))) : "无文件"; }
    static void 错误(string 文, string 栈, LogType 类) { if (类 == LogType.Error || 类 == LogType.Exception) 结果.错误.Add(文 + "\n" + 栈); }
    public static string 启动()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty || EditorSceneManager.GetActiveScene().path != "Assets/天帝/场景/天帝.unity")
            throw new InvalidOperationException("需停止运行，在已保存的主页场景检查；不会覆盖当前场景。");
        目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/道纹图鉴-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录); 结果 = new 报告();
        真档 = new 天帝存档().路径; 真档指纹 = 指纹(真档);
        var 网 = new 天帝道纹(82, 天帝天赋.获取((int)天赋种类.普通人));
        var 存 = new 天帝存档(Path.Combine(目录, "隔离存档"));
        if (!存.保存(new 天帝存档数据 { 序章已完成 = true, 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网, 4).导出库存() })) throw new InvalidOperationException("隔离存档创建失败");
        原存档 = 天帝存档.验证目录; 天帝存档.验证目录 = Path.Combine(目录, "隔离存档");
        原后台 = Application.runInBackground; 原选项开 = EditorSettings.enterPlayModeOptionsEnabled; 原选项 = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        已开始 = 已结束 = false; 截止 = EditorApplication.timeSinceStartup + 60;
        Application.logMessageReceived += 错误; EditorApplication.update += 等待; EditorApplication.playModeStateChanged += 退出;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus(); EditorApplication.isPlaying = true; return 目录;
    }
    static void 等待()
    {
        if (已结束) return;
        if (EditorApplication.timeSinceStartup > 截止) { 结果.错误.Add("图鉴运行检查超时"); 完成(); return; }
        if (!EditorApplication.isPlaying) return;
        Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 != null && !已开始) { 已开始 = true; 游戏.StartCoroutine(捕获(运行())); }
    }
    static IEnumerator 捕获(IEnumerator 程)
    {
        while (true) { bool 下; object 物 = null; try { 下 = 程.MoveNext(); if (下) 物 = 程.Current; } catch (Exception ex) { 结果.错误.Add(ex.ToString()); 完成(); yield break; } if (!下) yield break; yield return 物; }
    }
    static void 点击(string 名)
    {
        var 键 = 游戏.GetComponentsInChildren<Button>().First(b => b.name == 名 && b.gameObject.activeInHierarchy); var 区 = (RectTransform)键.transform; Canvas.ForceUpdateCanvases();
        var e = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(区.rect.center)) };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, 命中);
        检查("指针可达:" + 名, 键.interactable && 命中.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(命中[0].gameObject) == 键.gameObject);
        ExecuteEvents.Execute(键.gameObject, e, ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 截(string 名)
    {
        yield return new WaitForEndOfFrame(); var 图 = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(目录, 名 + ".png"), 图.EncodeToPNG()); UnityEngine.Object.Destroy(图);
    }
    static void 文字检查(天帝道纹图鉴 页, string 阶段)
    {
        Canvas.ForceUpdateCanvases();
        var 超高 = 页.GetComponentsInChildren<Text>().Where(t => t.name != "悬停正文" && t.preferredHeight > t.rectTransform.rect.height + .1f).Select(t => t.transform.parent.name + "/" + t.name).ToArray();
        检查(阶段 + "文字无截断", 超高.Length == 0); foreach (var 名 in 超高) 结果.失败.Add("截断:" + 名);
    }
    static IEnumerator 运行()
    {
        yield return new WaitForSecondsRealtime(.2f); 检查("进入真实主页", 游戏.继续游戏()); yield return new WaitForSecondsRealtime(.2f);
        点击("道纹图鉴"); yield return null; var 页 = 游戏.GetComponentInChildren<天帝道纹图鉴>();
        检查("完整收录31个条目", 页 != null && 页.展示数量 == 31);
        var 行 = 页.GetComponentsInChildren<RectTransform>().Where(t => t.name.StartsWith("图鉴行-")).ToArray();
        检查("每枚道纹单独一行", 行.Length == 31 && 行.All(t => t.anchoredPosition.x == 0 && t.rect.width == 1320));
        检查("每行左图标右说明", 行.All(t => t.Find("道纹图标") != null && t.Find("道纹名称") != null));
        检查("13个天赋完整介绍", 行.Count(t => t.name.StartsWith("图鉴行-天赋-") && t.Find("天赋介绍") != null) == 13);
        for (int i = 1; i < 行.Length; i++) 检查("行无重叠:" + 行[i].name, -行[i].anchoredPosition.y >= -行[i - 1].anchoredPosition.y + 行[i - 1].rect.height);
        文字检查(页, "一级"); yield return 截("01-天赋道纹");
        点击("图鉴分类-基础属性"); yield return null; 检查("分类跳转正确", 页.GetComponentInChildren<ScrollRect>().content.anchoredPosition.y > 2000);
        yield return 截("02-属性道纹");
        var 输入 = 页.GetComponentInChildren<InputField>(); 输入.onEndEdit.Invoke("100"); 检查("等级输入实际刷新", 页.查询物品等级 == 100);
        文字检查(页, "百级"); yield return 截("03-百级区间");
        foreach (var 属性 in 天帝道纹属性.当前属性)
        {
            foreach (int 级 in new[] { 1, 50, 100, 105 })
            {
                天帝数值.词条定点范围(属性, 级, out int 下, out int 上); double 尺 = 天帝数值.取("rune.storage_scale"); var 随机 = new System.Random(级 * 7 + (int)属性);
                bool 合法 = true; for (int i = 0; i < 25; i++) { double 值 = 天帝数值.抽取词条(属性, 级, 随机).实际数值; 合法 &= 值 >= 下 / 尺 - 1e-8 && 值 <= 上 / 尺 + 1e-8; }
                检查("图鉴区间包含真实掉落:" + 属性 + "@" + 级, 合法);
            }
        }
        天帝数值.词条定点范围(道纹属性.力量, 100, out int 力下, out int 力上); 检查("百级力量按正式舍入39.82至95.55", 力下 == 3982 && 力上 == 9555);
        检查("五行各品阶固定1条", 行.Where(t => t.name.StartsWith("图鉴行-属性-") && 天帝道纹属性.五行属性.Any(a => t.name == "图鉴行-属性-" + a)).All(t => t.GetComponentsInChildren<Text>().Where(z => z.name == "品阶容量文字").All(z => z.text.EndsWith("  1条"))));
        输入.onEndEdit.Invoke("999"); 检查("等级上限钳制", 页.查询物品等级 == 100); 输入.onEndEdit.Invoke("0"); 检查("等级下限钳制", 页.查询物品等级 == 1); 输入.onEndEdit.Invoke(""); 检查("空输入恢复", 输入.text == "1");
        点击("图鉴分类-分叉道纹"); yield return null; yield return 截("04-分叉与连接规则");
        var 滚 = 页.GetComponentInChildren<ScrollRect>(); 滚.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
        检查("可滚到全部机制说明", Mathf.Abs(滚.content.anchoredPosition.y - (滚.content.rect.height - 滚.viewport.rect.height)) < 2);
        点击("关闭图鉴"); yield return null; 检查("返回关闭图鉴", 游戏.GetComponentInChildren<天帝道纹图鉴>() == null && 游戏.阶段 == 游戏阶段.主页);
        完成();
    }
    static void 完成() { if (已结束) return; 已结束 = true; File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true)); EditorApplication.isPlaying = false; }
    static void 退出(PlayModeStateChange 状态)
    {
        if (状态 != PlayModeStateChange.EnteredEditMode) return;
        Application.logMessageReceived -= 错误; EditorApplication.update -= 等待; EditorApplication.playModeStateChanged -= 退出;
        天帝存档.验证目录 = 原存档; Application.runInBackground = 原后台; EditorSettings.enterPlayModeOptionsEnabled = 原选项开; EditorSettings.enterPlayModeOptions = 原选项;
        检查("真实存档指纹未改变", 真档指纹 == 指纹(真档)); if (!已结束) 结果.错误.Add("图鉴检查提前停止"); File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
    }
}
#endif
