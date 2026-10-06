#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class 天帝宝盒验证
{
    [MenuItem("天帝/验证宝盒")]
    static void 菜单验证() => Debug.Log(运行());

    public static string 运行()
    {
        void 检查(bool 条件, string 名称)
        { if (!条件) throw new InvalidOperationException("宝盒验证失败：" + 名称); }

        检查(天帝宝盒.品阶概率(宝盒种类.属性).Sum() == 10000 &&
            天帝宝盒.品阶概率(宝盒种类.功能).Sum() == 10000 &&
            天帝宝盒.分叉概率().Sum() == 100, "概率总和");
        检查(天帝宝盒.抽品阶(宝盒种类.属性, 9999) == 道纹品阶.完美 &&
            天帝宝盒.抽品阶(宝盒种类.功能, 9999) == 道纹品阶.稀有 &&
            天帝宝盒.抽分叉接口数(99) == 6, "概率边界");

        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 新档 = new 天帝宝盒(网, 20261004, 天帝宝盒.开局灵石);
        检查(新档.灵石 == 500 && !新档.抽取(宝盒种类.分叉, out _, out _) && 新档.灵石 == 500, "开局与余额不足不扣费");
        检查(新档.抽取(宝盒种类.属性, out var 属性, out _) && 新档.灵石 == 400 &&
            属性.词条.All(x => (int)x.属性 < 5 || (int)x.属性 >= 10 && (int)x.属性 <= 14), "属性宝盒奖池与扣费");
        检查(!属性.词条.Any(x => 天帝道纹属性.是五行(x.属性)) || 属性.是五行道纹, "属性宝盒五行仅单词条");
        检查(new 天帝宝盒(网, 42, 500).抽取(宝盒种类.功能, out var 功能, out _) &&
            功能.是功能道纹 && 功能.品阶 == 道纹品阶.稀有 && 功能.词条.Count == 1 && 功能.词条上限 == 1 &&
            功能.词条.All(x => (int)x.属性 >= 5 && (int)x.属性 <= 9), "功能宝盒奖池");
        var 分叉盒 = new 天帝宝盒(网, 43, 1000);
        检查(分叉盒.抽取(宝盒种类.分叉, out var 分叉, out _) &&
            分叉.分类 == 道纹分类.分叉 && 分叉.词条.Count == 0 &&
            Enumerable.Range(0, 6).Count(分叉.有接口) >= 3 && 分叉盒.灵石 == 0, "分叉宝盒奖池与扣费");
        检查(网.道纹.Count == 3, "道纹直接进入背包");
        检查(新档.获得灵石(5) && 新档.灵石 == 405 && 天帝宝盒.击败奖励(战斗敌人级别.王级) == 200, "击败奖励");

        var 旧档 = new 天帝存档数据 { 版本 = 1, 序章已完成 = true, 主角 = 天帝普攻.主角配置(),
            画布 = 网.导出存档(), 通货 = new 天帝通货(网, 42).导出库存(), 灵石 = 0 };
        天帝存档.校验(旧档);
        旧档.版本 = 天帝存档.当前版本; 旧档.灵石 = 新档.灵石;
        天帝存档.校验(旧档);
        var 回读 = JsonUtility.FromJson<天帝存档数据>(JsonUtility.ToJson(旧档));
        天帝存档.校验(回读);
        检查(回读.灵石 == 新档.灵石 && 回读.画布.道纹.Count == 3, "灵石与道纹序列化回读");
        return "宝盒验证通过：分类、概率、扣费、击败奖励、v1/v2存档及回读";
    }

    [Serializable] sealed class 界面报告
    { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }

    public static string 查看界面()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("请先运行现有主场景。");
        var 游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
        if (游戏 == null) throw new InvalidOperationException("主场景游戏尚未就绪。");
        if (游戏.阶段 == 游戏阶段.标题 && !游戏.继续游戏()) throw new InvalidOperationException("现有存档无法继续。");
        if (游戏.阶段 != 游戏阶段.主页) throw new InvalidOperationException("仅从主页打开宝盒。");
        游戏.界面.显示宝盒(); EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        return "已打开现有存档的宝盒页面，未执行抽取。";
    }

    // 不激活游戏宿主，不运行Awake、不订阅自动保存；独立Canvas只送往离屏相机。
    public static string 验证界面()
    {
        var 报 = new 界面报告(); GameObject 宿主 = null, 相机物 = null; 天帝界面 页 = null;
        var 原美术 = 天帝美术资源.当前; float 原音量 = AudioListener.volume;
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/宝盒反馈-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录);
        void 检查(string 名, bool 对) => (对 ? 报.通过 : 报.失败).Add(名);
        void 设置(天帝游戏 游戏, string 名, object 值) => typeof(天帝游戏).GetProperty(名).SetValue(游戏, 值);
        try
        {
            天帝数值同步检查.校验();
            var 真实游戏 = UnityEngine.Object.FindAnyObjectByType<天帝游戏>();
            宿主 = new GameObject("独立宝盒反馈验证") { hideFlags = HideFlags.HideAndDontSave };
            宿主.SetActive(false); var 游戏 = 宿主.AddComponent<天帝游戏>();
            游戏.默认字体 = 真实游戏.默认字体; 游戏.主页背景 = 真实游戏.主页背景;
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, 真实游戏.美术);
            var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            网.设置玩家等级(100); var 盒 = new 天帝宝盒(网, 20261006, 5000);
            设置(游戏, "阶段", 游戏阶段.主页); 设置(游戏, "道纹数据", 网); 设置(游戏, "宝盒数据", 盒);
            页 = new 天帝界面(游戏);
            var 根 = 宿主.transform.Find("天帝界面").gameObject; 根.transform.SetParent(null, false);
            foreach (var 变 in 根.GetComponentsInChildren<Transform>(true)) 变.gameObject.hideFlags = HideFlags.HideAndDontSave;
            相机物 = new GameObject("宝盒离屏相机", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var 相机 = 相机物.GetComponent<Camera>(); 相机.enabled = false; 相机.orthographic = true;
            相机.cullingMask = 1 << 31; 相机.clearFlags = CameraClearFlags.SolidColor; 相机.backgroundColor = Color.black;
            相机.nearClipPlane = .1f; 相机.farClipPlane = 10;
            var 布 = 根.GetComponent<Canvas>(); 布.renderMode = RenderMode.ScreenSpaceCamera; 布.worldCamera = 相机; 布.planeDistance = 1;
            var 组 = 根.AddComponent<CanvasGroup>(); 组.blocksRaycasts = false;
            Text 文(string 名) => 根.GetComponentsInChildren<Text>().Single(x => x.name == 名);
            Button 抽(string 名) => 根.GetComponentsInChildren<Button>().Single(x => x.name == "抽取" && x.transform.parent.name == 名);
            天帝宝盒日志悬停[] 记录() => 根.GetComponentsInChildren<天帝宝盒日志悬停>();
            void 布局(string 状态)
            {
                页.更新适配(); Canvas.ForceUpdateCanvases();
                foreach (var 文本 in 根.GetComponentsInChildren<Text>().Where(x => x.enabled && !string.IsNullOrEmpty(x.text)))
                    检查(状态 + "文字完整 " + 文本.text + "（行高" + 文本.preferredHeight + "/容器" + 文本.rectTransform.rect.height + "）", 文本.preferredHeight <= 文本.rectTransform.rect.height + 1);
            }
            void 拍(string 名)
            {
                foreach (var 变 in 根.GetComponentsInChildren<Transform>(true)) 变.gameObject.layer = 31;
                var 纹理 = new RenderTexture(1920, 1080, 24); var 旧目标 = RenderTexture.active;
                Texture2D 图 = null;
                try
                {
                    相机.targetTexture = 纹理; Canvas.ForceUpdateCanvases(); 相机.Render(); RenderTexture.active = 纹理;
                    图 = new Texture2D(1920, 1080, TextureFormat.RGB24, false); 图.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); 图.Apply();
                    File.WriteAllBytes(Path.Combine(目录, 名 + ".png"), 图.EncodeToPNG());
                }
                finally { 相机.targetTexture = null; RenderTexture.active = 旧目标; if (图 != null) UnityEngine.Object.DestroyImmediate(图); 纹理.Release(); UnityEngine.Object.DestroyImmediate(纹理); }
            }
            页.显示宝盒(); 布局("空记录");
            检查("首次打开有抽取结果位置和引导", 文("宝盒结果标题").text.Contains("选择宝盒") && 记录().Length == 0);
            var 框 = 根.GetComponentsInChildren<RectTransform>().Single(x => x.name == "宝盒面板");
            var 结果框 = 框.Find("宝盒结果衬底") as RectTransform; var 日志框 = 框.Find("抽取日志") as RectTransform;
            var 卡 = 框.Find("属性宝盒") as RectTransform;
            var 结果范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(框, 结果框);
            var 卡范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(框, 卡);
            var 日志范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(框, 日志框);
            检查("结果栏、抽取卡和日志有独立间距", 结果范围.min.y > 卡范围.max.y && 卡范围.min.y > 日志范围.max.y);
            检查("结果栏沿用正式UI素材", 结果框.GetComponent<Image>().sprite != null);
            foreach (string 名 in new[] { "属性宝盒", "功能宝盒", "分叉宝盒" })
            {
                int 原钱 = 盒.灵石, 原数 = 网.道纹.Count, 原记录 = 记录().Length;
                抽(名).onClick.Invoke(); var 奖励 = 网.道纹.Last(); 布局(名);
                检查(名 + "点击后立即提示正确品质和名称", 文("宝盒结果标题").text.Contains(奖励.名称) && Regex.Replace(文("宝盒结果标题").text, "<.*?>", "").Contains(奖励.品阶.ToString()));
                检查(名 + "摘要包含真实词条和物品等级", 文("宝盒结果摘要").text.Contains(奖励.候选说明) && 文("宝盒结果摘要").text.Contains("物品等级 " + 奖励.物品等级));
                检查(名 + "奖励恰好一枚且显示准确消耗", 网.道纹.Count == 原数 + 1 && 记录().Length == 原记录 + 1 && 文("宝盒结果状态").text.Contains((原钱 - 盒.灵石).ToString()) && 文("宝盒结果状态").text.Contains("已放入"));
            }
            拍("01-三类宝盒与可读日志");
            var 最后一条 = 记录().Last();
            var 指针 = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(相机, 最后一条.transform.position) };
            ExecuteEvents.Execute(最后一条.gameObject, 指针, ExecuteEvents.pointerEnterHandler); Canvas.ForceUpdateCanvases();
            var 详情 = 根.GetComponentsInChildren<天帝道纹详情卡>().Single();
            检查("日志悬停仍展示对应道纹完整详情", 详情.gameObject.activeSelf && 详情.当前道纹.名称 == 网.道纹.Last().名称);
            拍("02-抽取记录完整详情"); ExecuteEvents.Execute(最后一条.gameObject, 指针, ExecuteEvents.pointerExitHandler);
            检查("移出日志收起详情", !详情.gameObject.activeSelf);
            for (int i = 0; i < 6; i++) 抽("属性宝盒").onClick.Invoke();
            var 滚 = 根.GetComponentsInChildren<ScrollRect>().Single(); Canvas.ForceUpdateCanvases();
            检查("连续抽取日志可滚动且最新结果可见", 记录().Length == 9 && 滚.content.rect.height > 滚.viewport.rect.height && 滚.verticalNormalizedPosition < .01f && 文("宝盒结果标题").text.Contains(网.道纹.Last().名称));
            string 原标题 = 文("宝盒结果标题").text;
            页.关闭宝盒(); 页.显示宝盒(); Canvas.ForceUpdateCanvases();
            检查("关闭重开保留最近结果和记录", 文("宝盒结果标题").text == 原标题 && 记录().Length == 9);
            页.显示宝盒概率(2); 检查("查看概率不覆盖结果栏", 页.宝盒概率已打开 && 文("宝盒结果标题").text == 原标题);
            页.关闭宝盒概率(); 检查("概率返回恢复可抽取按钮", !页.宝盒概率已打开 && 抽("属性宝盒").interactable);
            页.关闭宝盒();
            var 空盒 = new 天帝宝盒(网, 42, 0); 设置(游戏, "宝盒数据", 空盒); 页.显示宝盒();
            检查("余额不足按钮禁用且换账户清空旧日志", 根.GetComponentsInChildren<Button>().Where(x => x.name == "抽取").All(x => !x.interactable) && 记录().Length == 0);
            int 原道纹 = 网.道纹.Count; 抽("属性宝盒").onClick.Invoke(); 布局("余额不足");
            检查("抽取失败有显眼原因且不扣费不发奖", 文("宝盒结果标题").text.Contains("灵石不足") && 文("宝盒结果摘要").text.Contains("没有扣除") && 空盒.灵石 == 0 && 网.道纹.Count == 原道纹);
            拍("03-余额不足提示"); 页.关闭宝盒();
            var 无限盒 = new 天帝宝盒(网, 42, 0, true); 设置(游戏, "宝盒数据", 无限盒); 页.显示宝盒(); 抽("属性宝盒").onClick.Invoke();
            检查("无限模式仍明确提示奖励与未扣费", 文("宝盒结果标题").text.Contains("本次获得") && 文("宝盒结果状态").text.Contains("无限灵石 · 未扣费") && 无限盒.灵石 == int.MaxValue);
            // 八阶、最长摘要及改造后快照，检查实际UGUI在极端记录下的字体和对比度。
            var 日志 = (IList)typeof(天帝界面).GetField("宝盒日志", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(页);
            var 记录类型 = typeof(天帝界面).GetNestedType("宝盒记录", BindingFlags.NonPublic);
            日志.Clear();
            for (int i = 0; i < 8; i++)
            {
                var 纹 = 天帝道纹生成.创建(i + 1, 道纹分类.属性, (道纹品阶)i, new System.Random(i), 道纹属性分组.普通, 100);
                var 项 = Activator.CreateInstance(记录类型); 记录类型.GetField("种类").SetValue(项, 宝盒种类.功能); 记录类型.GetField("道纹").SetValue(项, 纹); 日志.Add(项);
            }
            页.关闭宝盒(); 页.显示宝盒(); 布局("八阶记录");
            foreach (var 行 in 记录())
            {
                Color 底 = 行.GetComponent<Image>().color;
                foreach (var 文本 in 行.GetComponentsInChildren<Text>())
                {
                    var 区 = (RectTransform)行.transform; var 范围 = RectTransformUtility.CalculateRelativeRectTransformBounds(区, 文本.transform);
                    检查("日志三列在行内完整显示 " + 文本.text, 范围.min.x >= 区.rect.xMin && 范围.max.x <= 区.rect.xMax && 范围.min.y >= 区.rect.yMin && 范围.max.y <= 区.rect.yMax);
                    检查("日志正文和摘要对比度 " + 文本.text, 对比度(底, 文本.color) >= 4.5f);
                    foreach (Match 匹配 in Regex.Matches(文本.text, "<color=#([A-Fa-f0-9]{6})>"))
                    { ColorUtility.TryParseHtmlString("#" + 匹配.Groups[1].Value, out var 色); 检查("日志品阶颜色对比度 " + 文本.text, 对比度(底, 色) >= 4.5f); }
                }
            }
            页.关闭宝盒(); 检查("关闭后结果与详情不会残留", !页.宝盒已打开 && 根.GetComponentsInChildren<天帝宝盒日志悬停>().Length == 0);
        }
        catch (Exception 异常) { 报.错误.Add(异常.ToString()); }
        finally
        {
            页?.销毁(); if (宿主 != null) UnityEngine.Object.DestroyImmediate(宿主); if (相机物 != null) UnityEngine.Object.DestroyImmediate(相机物);
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, 原美术); AudioListener.volume = 原音量;
        }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(报, true)); return 目录;
    }
    static float 对比度(Color a, Color b)
    {
        float 分量(float v) => v <= .04045f ? v / 12.92f : Mathf.Pow((v + .055f) / 1.055f, 2.4f);
        float 亮度(Color c) => .2126f * 分量(c.r) + .7152f * 分量(c.g) + .0722f * 分量(c.b);
        float x = 亮度(a), y = 亮度(b); return (Mathf.Max(x, y) + .05f) / (Mathf.Min(x, y) + .05f);
    }
}
#endif
