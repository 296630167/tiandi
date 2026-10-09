#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 独立 UI 与模型覆盖本轮交互回归，不启动游戏，也不读取玩家存档。
public static class 天帝第三轮界面验证
{
    [Serializable] public sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
    }
    static 报告 结果;
    static Font 字体;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static bool 近(float 左, float 右) => Mathf.Abs(左 - 右) < .15f;
    static T 字段<T>(object 目标, string 名) => (T)目标.GetType().GetField(名, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(目标);
    public static string 运行()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请在编辑模式验证界面回归。");
        结果 = new 报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/第三轮界面回归-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        var 原移动 = 天帝移动适配.验证移动平台;
        var 原美术 = 天帝美术资源.当前;
        Application.LogCallback 日志 = (文, 栈, 类) =>
        { if (类 == LogType.Error || 类 == LogType.Exception || 类 == LogType.Assert) 结果.错误.Add(文 + "\n" + 栈); };
        Application.logMessageReceived += 日志;
        try
        {
            字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset"));
            分组("等高两列正文", () => 正文重排(24, 24));
            分组("不同原高度两列正文", () => 正文重排(24, 34));
            分组("手机详情切换", 手机详情);
            分组("回收筛选与详情滚动", 回收详情);
            分组("图鉴重复初始化", 图鉴重复初始化);
            分组("转化图鉴与等级查询", 转化图鉴说明);
        }
        finally
        {
            天帝移动适配.验证移动平台 = 原移动;
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, 原美术);
            Application.logMessageReceived -= 日志;
            File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        }
        return 目录;
    }
    static void 分组(string 名, Action 执行)
    {
        var 原移动 = 天帝移动适配.验证移动平台; var 原密度 = 天帝移动适配.验证密度;
        var 原屏幕 = 天帝移动适配.验证屏幕尺寸; var 原安全区 = 天帝移动适配.验证安全区;
        var 原美术 = 天帝美术资源.当前;
        try { 执行(); }
        catch (Exception 异常) { 结果.错误.Add(名 + "：" + 异常); }
        finally
        {
            天帝移动适配.验证移动平台 = 原移动; 天帝移动适配.验证密度 = 原密度;
            天帝移动适配.验证屏幕尺寸 = 原屏幕; 天帝移动适配.验证安全区 = 原安全区;
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, 原美术);
        }
    }
    static RectTransform 区(RectTransform 父, string 名, float x, float y, float 宽, float 高)
    {
        var 根 = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        根.SetParent(父, false); 天帝双端页面布局.固定(根, x, y, 宽, 高);
        return 根;
    }
    static RectTransform 画布()
    {
        var 根 = new GameObject("独立第三轮界面回归", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        根.hideFlags = HideFlags.HideAndDontSave;
        根.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        return (RectTransform)根.transform;
    }
    static Text 文字(RectTransform 父, string 名, float x, float y, float 宽, float 高, string 文)
    {
        var 字 = 区(父, 名, x, y, 宽, 高).gameObject.AddComponent<Text>();
        字.font = 字体; 字.fontSize = 20; 字.text = 文; 字.resizeTextForBestFit = false;
        字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Truncate;
        字.raycastTarget = false; 字.alignment = TextAnchor.UpperLeft;
        return 字;
    }
    static void 正文重排(float 左原高, float 右原高)
    {
        天帝移动适配.验证移动平台 = true;
        var 根 = 画布();
        try
        {
            var 正文 = 区(根, "两列正文", 0, 0, 360, 160);
            var 背景 = 区(正文, "跨行背景", 0, 0, 360, 160).gameObject.AddComponent<Image>(); 背景.raycastTarget = false;
            var 左 = 文字(正文, "左列", 0, 0, 170, 左原高, "左列的长说明会在小屏幕自动换行，并且保留后续内容所需的空间。");
            var 右 = 文字(正文, "右列", 190, 0, 170, 右原高, "右列也需要自动换行。");
            float 原行底 = Mathf.Max(左原高, 右原高), 原下顶 = 原行底 + 10;
            var 下 = 文字(正文, "下一行", 0, 原下顶, 360, 30, "后续内容");
            var 排版 = 天帝双端页面布局.重排正文(正文);
            正文.sizeDelta = new Vector2(180, 160); Canvas.ForceUpdateCanvases(); 排版();
            float 新行底 = Mathf.Max(左.rectTransform.rect.height, 右.rectTransform.rect.height);
            string 前 = 左原高 == 右原高 ? "等高两列" : "不同原高两列";
            检查(前 + "确实触发两列换行", 左.rectTransform.rect.height > 左原高 && 右.rectTransform.rect.height > 右原高);
            检查(前 + "后续行保留10单位间隔且只按最高列推开", 近(-下.rectTransform.anchoredPosition.y, 新行底 + 10));
            检查(前 + "同排顶部对齐", 近(左.rectTransform.anchoredPosition.y, 右.rectTransform.anchoredPosition.y));
            float 下顶 = -下.rectTransform.anchoredPosition.y, 根高 = 正文.rect.height, 背景高 = 背景.rectTransform.rect.height;
            排版(); Canvas.ForceUpdateCanvases(); 排版();
            检查(前 + "重复排版不累计空白", 近(下顶, -下.rectTransform.anchoredPosition.y) && 近(根高, 正文.rect.height));
            // 同长度文字变更强制重排，排除缓存提前返回掩盖坐标累计的问题。
            左.text = 左.text.Replace("左列", "正文"); 排版();
            检查(前 + "正文变更后重排仍幂等", 近(下顶, -下.rectTransform.anchoredPosition.y) && 近(根高, 正文.rect.height));
            检查(前 + "跨行背景覆盖新增正文高度", 近(背景高, 根高));
            正文.sizeDelta = new Vector2(360, 正文.sizeDelta.y); 排版();
            检查(前 + "恢复宽度时收回换行空白", -下.rectTransform.anchoredPosition.y < 下顶 && 近(-下.rectTransform.anchoredPosition.y, Mathf.Max(左.rectTransform.rect.height, 右.rectTransform.rect.height) + 10));
            正文.sizeDelta = new Vector2(180, 正文.sizeDelta.y); 排版();
            检查(前 + "宽窄来回不漂移", 近(下顶, -下.rectTransform.anchoredPosition.y) && 近(根高, 正文.rect.height));
        }
        finally { UnityEngine.Object.DestroyImmediate(根.gameObject); }
    }
    static 道纹实例 长道纹(道纹品阶 品阶, int 种子)
    {
        var 纹 = 天帝道纹生成.创建(种子, 道纹分类.属性, 品阶, new System.Random(种子), 道纹属性分组.基础);
        纹.介绍 = string.Concat(System.Linq.Enumerable.Repeat("需要滚动查看的独立测试道纹说明。", 50));
        return 纹;
    }
    static void 手机详情()
    {
        天帝移动适配.验证移动平台 = true;
        var 根 = 画布();
        try
        {
            var 父 = 区(根, "手机详情区域", 10, 10, 300, 180);
            var 卡 = 区(父, "手机详情", 0, 0, 300, 180).gameObject.AddComponent<天帝道纹详情卡>();
            卡.填满父区域 = true; 卡.初始化(字体); 卡.gameObject.SetActive(true);
            var 旧 = 长道纹(道纹品阶.传说, 31); var 新 = 长道纹(道纹品阶.传说, 32);
            卡.设置(旧, "测试状态"); Canvas.ForceUpdateCanvases();
            var 滚 = 卡.GetComponent<ScrollRect>();
            检查("手机详情夹具确实有可滚动正文", 滚.content.rect.height > 滚.viewport.rect.height + 100);
            滚.verticalNormalizedPosition = .35f; 滚.velocity = new Vector2(0, 75);
            float 前位置 = 滚.verticalNormalizedPosition; Vector2 前速度 = 滚.velocity;
            卡.设置(旧, "变更状态"); Canvas.ForceUpdateCanvases();
            检查("同纹状态刷新保留阅读位置", Mathf.Abs(滚.verticalNormalizedPosition - 前位置) < .002f);
            检查("同纹状态刷新不强制停止已有滚动", Vector2.Distance(滚.velocity, 前速度) < .01f);
            卡.设置(新, "测试状态"); Canvas.ForceUpdateCanvases();
            检查("切换手机道纹详情返回正文顶部", Mathf.Abs(滚.verticalNormalizedPosition - 1) < .002f);
            检查("切换手机道纹详情停止旧惯性", 滚.velocity.sqrMagnitude < .0001f);
        }
        finally { UnityEngine.Object.DestroyImmediate(根.gameObject); }
    }
    static void 回收详情()
    {
        天帝移动适配.验证移动平台 = false;
        var 根 = 画布();
        GameObject 拍摄 = null; RenderTexture 目标 = null;
        try
        {
            var 网 = new 天帝道纹(41, 天帝天赋.获取((int)天赋种类.普通人));
            var 普通 = 长道纹(道纹品阶.普通, 41); var 优秀 = 长道纹(道纹品阶.优秀, 42);
            网.获得道纹(普通); 网.获得道纹(优秀);
            var 页 = 区(根, "独立回收页面", 5, 5, 1600, 900).gameObject.AddComponent<天帝道纹回收界面>();
            页.初始化(网, new 天帝宝盒(网, 41, 500), 字体, () => { });
            页.transform.localScale = Vector3.one * .2f;
            foreach (var 勾 in 页.GetComponentsInChildren<Toggle>(true)) 勾.onValueChanged.RemoveAllListeners();
            var 详情 = 字段<天帝道纹详情卡>(页, "详情");
            var 筛选 = 字段<Dropdown[]>(页, "筛选下拉")[0];
            检查("回收初始焦点对应第一张卡", 详情.当前道纹 == 普通);
            筛选.value = 2;
            检查("筛选后详情不再显示被过滤的旧焦点", 页.显示项(0) == 优秀 && 详情.当前道纹 == 优秀 && 详情.gameObject.activeSelf);
            筛选.value = 7;
            检查("筛选无结果时隐藏旧详情", 页.显示项(0) == null && !详情.gameObject.activeSelf);
            筛选.value = 0;
            检查("撤销筛选后恢复有效详情", 详情.gameObject.activeSelf && 详情.当前道纹 == 页.显示项(0));
            var 特性 = 天帝特性道纹.创建(43, 道纹分类.特性, 1, 道纹品阶.普通, 8, 0);
            网.获得道纹(特性); typeof(道纹实例).GetProperty("回收锁定").SetValue(特性, true);
            var 类型筛选 = 字段<Dropdown[]>(页, "筛选下拉")[1]; 类型筛选.value = 4;
            var 状态 = 字段<Text>(详情, "状态");
            检查("特性回收详情同时显示保护原因和机制状态", !网.可回收(特性, out string 保护原因) && 详情.当前道纹 == 特性
                && 状态.text.Contains(保护原因) && 状态.text.Contains(特性.特性状态) && !string.IsNullOrEmpty(特性.特性状态));
            类型筛选.value = 0; 筛选.value = 2;
            var 视口 = Array.Find(页.GetComponentsInChildren<RectTransform>(true), r => r.name == "山水回收详情视口");
            检查("山水回收独立详情视口已实装", 视口 != null);
            if (视口 == null) return;
            // 编辑模式 Overlay 的 Graphic 深度尚未渲染，使用独立摄像机完成真实命中所需的绘制。
            var 画 = 根.GetComponent<Canvas>(); 画.renderMode = RenderMode.WorldSpace;
            根.pivot = new Vector2(0, 1); 根.sizeDelta = new Vector2(1600, 900); 根.position = Vector3.zero; 根.localScale = Vector3.one;
            拍摄 = new GameObject("独立回归射线摄像机", typeof(Camera)); 拍摄.hideFlags = HideFlags.HideAndDontSave;
            var 摄 = 拍摄.GetComponent<Camera>(); 摄.orthographic = true; 摄.orthographicSize = 450; 摄.aspect = 16f / 9;
            摄.transform.position = new Vector3(800, -450, -20); 摄.clearFlags = CameraClearFlags.SolidColor;
            目标 = new RenderTexture(1024, 576, 16); 目标.Create(); 摄.targetTexture = 目标; 画.worldCamera = 摄;
            Canvas.ForceUpdateCanvases(); 摄.Render();
            var 滚 = 视口.GetComponent<ScrollRect>();
            检查("回收长详情确实超出视口", 滚 != null && 滚.content.rect.height > 视口.rect.height + 100);
            var 事件 = new PointerEventData(null)
            {
                position = RectTransformUtility.WorldToScreenPoint(摄, 视口.TransformPoint(视口.rect.center)),
                scrollDelta = new Vector2(0, -1)
            };
            var 命中 = new List<RaycastResult>(); 根.GetComponent<GraphicRaycaster>().Raycast(事件, 命中);
            检查("回收详情视口能实际命中射线", 命中.Exists(r => r.gameObject == 视口.gameObject));
            滚.verticalNormalizedPosition = 1;
            ExecuteEvents.Execute(视口.gameObject, 事件, ExecuteEvents.scrollHandler);
            检查("回收详情滚轮输入推进正文", 滚.verticalNormalizedPosition < .99f);
            // 桌面悬停卡继续透传，独立视口承接输入，避免覆盖背包卡片操作。
            检查("桌面详情卡保留悬停透传", !详情.GetComponent<CanvasGroup>().blocksRaycasts);
        }
        finally
        {
            if (拍摄 != null) { 拍摄.GetComponent<Camera>().targetTexture = null; UnityEngine.Object.DestroyImmediate(拍摄); }
            if (目标 != null) { 目标.Release(); UnityEngine.Object.DestroyImmediate(目标); }
            UnityEngine.Object.DestroyImmediate(根.gameObject);
        }
    }
    static void 图鉴重复初始化()
    {
        天帝移动适配.验证移动平台 = false;
        var 根 = 画布();
        try
        {
            var 页 = 区(根, "独立图鉴初始化", 0, 0, 1600, 900).gameObject.AddComponent<天帝道纹图鉴>();
            页.初始化(字体, () => { });
            var 分类 = 字段<Dictionary<string, Button>>(页, "分类按钮");
            分类["特性道纹"].onClick.Invoke();
            var 下页 = 字段<Button>(页, "剪纸下页"); if (下页 != null) 下页.onClick.Invoke();
            页.设置查询物品等级(73);
            var 旧 = new List<GameObject>(); foreach (Transform 子 in 页.transform) 旧.Add(子.gameObject);
            int 原节点数 = 页.GetComponentsInChildren<Transform>(true).Length;
            int 原查询数 = 字段<List<(道纹实例 纹, Text 文)>>(页, "特性查询").Count;
            检查("图鉴重复初始化夹具确实修改查询与分页", 页.查询物品等级 == 73 && 下页 != null && 字段<int>(页, "剪纸页") > 0);
            页.初始化(null, () => { });
            var 输入 = 字段<InputField>(页, "等级输入");
            检查("图鉴重新初始化恢复一级查询与输入", 页.查询物品等级 == 1 && 输入.text == "1");
            检查("图鉴重新初始化重置页码与默认分类", 字段<int>(页, "剪纸页") == 0 && 字段<string>(页, "剪纸当前分类") == "功能道纹");
            检查("图鉴重新初始化移除所有旧视图", 旧.TrueForAll(物 => 物 == null));
            检查("图鉴重新初始化不累积视图数量", 页.GetComponentsInChildren<Transform>(true).Length == 原节点数);
            var 查询 = 字段<List<(道纹实例 纹, Text 文)>>(页, "特性查询");
            检查("图鉴重新初始化不累积查询模型", 查询.Count == 原查询数 && 查询.TrueForAll(项 => 项.纹.物品等级 == 1));
            检查("图鉴缺默认字体使用可用字体回退", 字段<Font>(页, "字体") != null && Array.TrueForAll(页.GetComponentsInChildren<Text>(true), 文 => 文.font != null));
            页.设置查询物品等级(25);
            检查("图鉴重新初始化后的等级查询仍可工作", 页.查询物品等级 == 25 && 查询.TrueForAll(项 => 项.纹.物品等级 == 25));
        }
        finally { UnityEngine.Object.DestroyImmediate(根.gameObject); }
    }
    static void 转化图鉴说明()
    {
        天帝移动适配.验证移动平台 = false;
        var 根 = 画布();
        try
        {
            var 页 = 区(根, "独立转化图鉴", 0, 0, 1600, 900).gameObject.AddComponent<天帝道纹图鉴>();
            页.初始化(字体, () => { });
            var 项 = 字段<List<(道纹实例 纹, Text 文)>>(页, "特性查询");
            var 转化 = 项.FindAll(值 => 值.纹.分类 == 道纹分类.转化);
            检查("转化图鉴收录八种正式机制", 转化.Count == 8);
            void 查说明(string 前)
            {
                检查(前 + "转化说明展示半径与效率", 转化.TrueForAll(值 => 值.文.text.Contains("半径") && 值.文.text.Contains("转化效率") && 值.文.text.Contains("%")));
                检查(前 + "转化说明不混用特性门槛与同名规则", 转化.TrueForAll(值 => !值.文.text.Contains("普通品阶条件") && !值.文.text.Contains("同名最高激活品阶生效")));
                检查(前 + "转化说明保留冲突处理规则", 转化.TrueForAll(值 => 值.文.text.Contains("目标冲突保留原词条") && 值.文.text.Contains("同目标取最高效率")));
                var 特性 = 项.FindAll(值 => 值.纹.分类 == 道纹分类.特性);
                检查(前 + "真正特性仍展示激活门槛", 特性.Count == 38 && 特性.TrueForAll(值 => 值.文.text.Contains("普通品阶条件") && 值.文.text.Contains("同名最高激活品阶生效")));
            }
            查说明("初始化："); 页.设置查询物品等级(73); 查说明("查询73级后：");
            检查("等级查询更新所有特性和转化模型", 项.TrueForAll(值 => 值.纹.物品等级 == 73));
        }
        finally { UnityEngine.Object.DestroyImmediate(根.gameObject); }
    }
}
#endif
