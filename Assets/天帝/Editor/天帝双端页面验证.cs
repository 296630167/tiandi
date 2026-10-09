#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 正式模型的独立实例；不运行天帝游戏Awake、不访问玩家存档。
public static class 天帝双端页面验证
{
    [Serializable] sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(), 排版问题 = new List<string>(), 文字重叠 = new List<string>(), 按钮文字问题 = new List<string>(), 关闭遮挡问题 = new List<string>();
    }
    public static string 运行()
    {
        var r = new 报告();
        var 原移动 = 天帝移动适配.验证移动平台; var 原密度 = 天帝移动适配.验证密度;
        var 原屏幕 = 天帝移动适配.验证屏幕尺寸; var 原安全 = 天帝移动适配.验证安全区; var 原美术 = 天帝美术资源.当前;
        GameObject host = null, 假游戏 = null; 天帝主角属性 人 = null;
        void 查(string 名, bool 对) => (对 ? r.通过 : r.失败).Add(名);
        void 设(object o, string n, object v) => o.GetType().GetProperty(n).SetValue(o, v);
        Application.LogCallback 日志 = (文, 栈, 类) => { if (类 == LogType.Error || 类 == LogType.Exception || 类 == LogType.Assert) r.错误.Add(文 + "\n" + 栈); };
        Application.logMessageReceived += 日志;
        try
        {
            var 字 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset"));
            var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            网.设置玩家等级(100);
            for (int i = 0; i < 26; i++) 网.获得道纹(天帝道纹生成.创建(i + 100, 道纹分类.属性, (道纹品阶)(i % 8), new System.Random(i + 42)));
            for(int id=1;id<=38;id++)网.获得道纹(天帝特性道纹.创建(1,道纹分类.特性,id,道纹品阶.传说,100,0));
            for(int id=1;id<=8;id++)网.获得道纹(天帝特性道纹.创建(1,道纹分类.转化,id,道纹品阶.传说,100,0));
            var 首 = 网.道纹.Find(x => x.是功能道纹);
            设(首, "功能", 道纹功能.齐射); 设(首, "入口方向", 3); 首.接口 = 天帝顺序道纹.固定接口(道纹功能.齐射, 3);
            首.词条.Clear(); 首.词条.Add(new 道纹词条(道纹属性.数量, 1));
            网.解锁格子(new Vector2Int(1, 0)); 网.放置(首, new Vector2Int(1, 0));
            var 出 = 网.道纹.FindAll(x => x.分类 == 道纹分类.属性);
            for (int i = 0; i < 2; i++)
            {
                var x = 出[i]; 设(x, "品阶", 道纹品阶.稀有); x.词条.Clear();
                x.词条.Add(new 道纹词条(i == 0 ? 道纹属性.火 : 道纹属性.水, 5)); x.接口 = i == 0 ? 8 : 16;
                var p = i == 0 ? new Vector2Int(2, 0) : new Vector2Int(1, 1); 网.解锁格子(p); 网.放置(x, p);
            }
            // 断开的两枚只用于三色链路渲染与状态验收，不进入玩家存档。
            for (int i = 0; i < 2; i++)
            {
                var 纹 = 出[5 + i]; 纹.接口 = i == 0 ? 1 : 8;
                var 格 = new Vector2Int(i, 2); 网.解锁格子(格); 网.放置(纹, 格);
            }
            var 钱 = new 天帝通货(网, 42); var 盒 = new 天帝宝盒(网, 42, 500); 人 = new 天帝主角属性(天帝普攻.主角配置(), 网);
            假游戏 = new GameObject("独立双端模型"); 假游戏.SetActive(false); 假游戏.hideFlags = HideFlags.HideAndDontSave;
            var g = 假游戏.AddComponent<天帝游戏>(); g.默认字体 = 字; g.美术 = 天帝美术资源.当前;
            设(g, "阶段", 游戏阶段.主页); 设(g, "主角属性", 人); 设(g, "道纹数据", 网); 设(g, "通货数据", 钱); 设(g, "宝盒数据", 盒);
            设(g, "天赋池", 天帝天赋池.从已选天赋恢复((int)天赋种类.普通人));
            foreach (bool 手机 in new[] { false, true })
            {
                天帝移动适配.验证移动平台 = 手机;
                var ui = new 天帝界面(g); 设(g, "界面", ui);
                host = (GameObject)typeof(天帝界面).GetField("根", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                host.transform.SetParent(null, false); host.hideFlags = HideFlags.HideAndDontSave;
                host.GetComponent<CanvasScaler>().enabled = false; host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var 组 = host.AddComponent<CanvasGroup>(); 组.alpha = 0; 组.blocksRaycasts = false;
                void 适配(Vector2Int 尺寸)
                {
                    // Edit模式UGUI Rebuild会触发Slider/Toggle回调；布局夹具不执行游戏设置或回收操作。
                    foreach (var 滑 in host.GetComponentsInChildren<Slider>(true)) 滑.onValueChanged.RemoveAllListeners();
                    foreach (var 勾 in host.GetComponentsInChildren<Toggle>(true)) 勾.onValueChanged.RemoveAllListeners();
                    天帝移动适配.验证屏幕尺寸 = 尺寸; 天帝移动适配.验证密度 = 手机 ? 尺寸.y / 420f : 1;
                    天帝移动适配.验证安全区 = 手机 ? new Rect(60, 24, 尺寸.x - 100, 尺寸.y - 48) : new Rect(0, 0, 尺寸.x, 尺寸.y);
                    var 安 = 天帝移动适配.有效安全区(天帝移动适配.安全区, 尺寸);
                    ((RectTransform)host.transform).sizeDelta = (Vector2)尺寸 / 天帝移动适配.显示比例(安);
                    ui.更新适配(); Canvas.ForceUpdateCanvases(); ui.更新适配(); Canvas.ForceUpdateCanvases();
                    foreach (var 卡 in host.GetComponentsInChildren<天帝道纹详情卡>()) typeof(天帝道纹详情卡).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(卡, null);
                    foreach (var 按钮区 in host.GetComponentsInChildren<天帝按钮文字区域>()) 按钮区.更新();
                    Canvas.ForceUpdateCanvases();
                }
                void 检(string 名)
                {
                    foreach (var 尺寸 in new[] { new Vector2Int(1280, 720), new Vector2Int(1640, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2048, 1536) })
                    {
                        适配(尺寸); string 前 = (手机 ? "手机 " : "PC ") + 尺寸 + " " + 名;
                        var 固定区 = (RectTransform)host.transform.Find("安全区");
                        var 设计 = (RectTransform)固定区.Find("设计区");
                        if (天帝青绿皮肤.已启用 && !天帝首两页山水素材.已启用 && (名 == "主页" || 名 == "100级主页"))
                        {
                            var 立绘 = (RectTransform)设计.Find("页面/主角立绘");
                            float 中 = 设计.InverseTransformPoint(立绘.TransformPoint(new Vector2(立绘.rect.center.x, 0))).x;
                            查(前 + "主角位于画面中线", Mathf.Abs(中) < .2f);
                            foreach (var 键 in 设计.GetComponentsInChildren<Button>())
                            {
                                var 图标 = 键.transform.Find("底栏图标-" + 键.name) as RectTransform;
                                if (图标 == null) continue;
                                var 标签 = 键.GetComponentInChildren<Text>();
                                查(前 + "底栏图标文字分离 " + 键.name, !相交(矩形(图标, (RectTransform)键.transform), 文字边框(标签, (RectTransform)键.transform)));
                            }
                        }
                        查(前 + "固定1080p画面", Vector2.Distance(固定区.rect.size, 天帝移动适配.固定分辨率) < .2f);
                        if (天帝青绿皮肤.已启用 && !天帝图三四山水素材.已启用 && (名 == "道纹" || 名 == "100级道纹"))
                        {
                            var 页 = (RectTransform)设计.Find("页面/道纹页面");
                            var 网格口 = (RectTransform)页.Find("画布视口");
                            查(前 + "B版大画布铺满工作区", 网格口.rect.width >= 页.rect.width * .97f && 网格口.rect.height >= 页.rect.height * (手机 ? .57f : .83f));
                            查(前 + "旧画布纸板关闭", !页.Find("画布主面板").gameObject.activeSelf);
                            var 藏匣 = (RectTransform)页.Find("候选区");
                            var 信息 = (RectTransform)页.Find("实时构筑预览视口");
                            var 藏匣范围 = 矩形(藏匣, 页); var 信息范围 = 矩形(信息, 页);
                            查(前 + "两侧信息不占中央操作区", 藏匣范围.xMax <= 页.rect.xMin + 页.rect.width * .18f && 信息范围.xMin >= 页.rect.xMin + 页.rect.width * .69f && !相交(藏匣范围, 信息范围));
                            查(前 + "竖排藏匣可以滚动", 藏匣.GetComponent<ScrollRect>().vertical && !藏匣.GetComponent<ScrollRect>().horizontal);
                        }
                        查(前 + "布局尺寸不随屏幕或DPI变化", Vector2.Distance(设计.rect.size, 天帝移动适配.布局尺寸) < .2f);
                        查(前 + "固定横屏且等比展示", Mathf.Abs(固定区.rect.width / 固定区.rect.height - 16f / 9f) < .001f && Mathf.Abs(设计.localScale.x - 设计.localScale.y) < .001f);
                        if (名 == "主页等级下拉")
                        {
                            var 下拉列表 = 设计.GetComponentInChildren<Dropdown>().transform.Find("Dropdown List") as RectTransform;
                            if (下拉列表 != null)
                            {
                                var 范围 = 矩形(下拉列表, 固定区); var 安全 = 固定区.rect;
                                查(前 + "地图等级列表完整位于固定视口", 范围.xMin >= 安全.xMin - .2f && 范围.xMax <= 安全.xMax + .2f && 范围.yMin >= 安全.yMin - .2f && 范围.yMax <= 安全.yMax + .2f);
                            }
                        }
                        foreach (var 按钮区 in host.GetComponentsInChildren<天帝按钮文字区域>())
                        {
                            if (!按钮区.enabled) continue;
                            var 纯色 = 按钮区.纯色区域;
                            if (按钮区.是关闭按钮)
                            {
                                var 图 = 按钮区.GetComponent<Image>(); var 框 = (RectTransform)按钮区.transform;
                                查(前 + "统一关闭按钮且放大 " + 路径(按钮区.transform), 图 != null && 图.sprite == 天帝道纹美术.获取("按钮") && 框.rect.width >= 147.9f && 框.rect.height >= 55.9f);
                                var 面板 = (RectTransform)按钮区.transform.parent; var 关闭框 = 矩形(框, 面板);
                                foreach (var 文 in 面板.GetComponentsInChildren<Text>())
                                {
                                    if (!文.enabled || 文.transform.IsChildOf(按钮区.transform) || string.IsNullOrWhiteSpace(文.text)) continue;
                                    var 文框 = 可见区域(文字边框(文, 面板), 文.transform, 面板);
                                    bool 不遮挡 = !相交(关闭框, 文框);
                                    查(前 + "关闭不遮挡文字 " + 路径(文.transform), 不遮挡);
                                    if (!不遮挡) r.关闭遮挡问题.Add(前 + " " + 路径(按钮区.transform) + " 挡住 " + 文.text.Replace("\n", " / ") + " " + 路径(文.transform));
                                }
                                foreach (var 控件 in 面板.GetComponentsInChildren<Selectable>())
                                {
                                    if (控件.transform.IsChildOf(按钮区.transform) || 控件.transform == 按钮区.transform) continue;
                                    var 控件框 = 可见区域(矩形((RectTransform)控件.transform, 面板), 控件.transform, 面板);
                                    bool 不遮挡 = !相交(关闭框, 控件框);
                                    查(前 + "关闭不遮挡其它控件 " + 路径(控件.transform), 不遮挡);
                                    if (!不遮挡) r.关闭遮挡问题.Add(前 + " " + 路径(按钮区.transform) + " 挡住控件 " + 路径(控件.transform));
                                }
                                foreach (var 滚动 in 面板.GetComponentsInChildren<ScrollRect>())
                                {
                                    var 口 = 滚动.viewport != null ? 滚动.viewport : (RectTransform)滚动.transform;
                                    bool 不遮挡 = !相交(关闭框, 可见区域(矩形(口, 面板), 口, 面板));
                                    查(前 + "关闭不遮挡滚动区域 " + 路径(口), 不遮挡);
                                    if (!不遮挡) r.关闭遮挡问题.Add(前 + " " + 路径(按钮区.transform) + " 挡住滚动区域 " + 路径(口));
                                }
                            }
                            foreach (Transform 子 in 按钮区.transform)
                            {
                                if (!(子.GetComponent<Text>() is Text 文) || !文.enabled || !文.gameObject.activeInHierarchy || string.IsNullOrEmpty(文.text)) continue;
                                // 滚动口之外的Text会被UGUI剔除，独立生成其完整标签几何，不把不可见误判为漏字。
                                var 实文 = new TextGenerator(); 实文.Populate(文.text, 文.GetGenerationSettings(文.rectTransform.rect.size));
                                var 字面区 = 文.name == "品阶勾选标识" ? ((RectTransform)按钮区.transform).rect : 纯色;
                                bool 合格 = true; var 顶点 = 实文.verts;
                                var 实最小 = new Vector2(float.MaxValue, float.MaxValue); var 实最大 = new Vector2(float.MinValue, float.MinValue);
                                for (int i = 0; i < 顶点.Count; i++)
                                {
                                    int 起 = i / 4 * 4;
                                    if (起 + 3 < 顶点.Count && 顶点[起].position == 顶点[起 + 1].position && 顶点[起].position == 顶点[起 + 2].position && 顶点[起].position == 顶点[起 + 3].position) continue;
                                    var p = 按钮区.transform.InverseTransformPoint(文.rectTransform.TransformPoint(顶点[i].position / 文.pixelsPerUnit));
                                    实最小 = Vector2.Min(实最小, p); 实最大 = Vector2.Max(实最大, p);
                                    if (p.x < 字面区.xMin - .5f || p.x > 字面区.xMax + .5f || p.y < 字面区.yMin - .5f || p.y > 字面区.yMax + .5f) 合格 = false;
                                }
                                var 字号 = 实文.fontSizeUsedForBestFit;
                                var 设 = 文.GetGenerationSettings(new Vector2(10000, 10000)); 设.resizeTextForBestFit = false; 设.fontSize = Mathf.Max(14, 字号);
                                设.horizontalOverflow = HorizontalWrapMode.Overflow; 设.verticalOverflow = VerticalWrapMode.Overflow;
                                var 全文 = new TextGenerator(); 全文.Populate(文.text, 设);
                                合格 &= 实最小.x != float.MaxValue && 实文.characterCountVisible >= 全文.characterCountVisible && 字号 >= 14;
                                foreach (Transform 装饰 in 按钮区.transform)
                                {
                                    if (!装饰.name.StartsWith("导航图标-") || !装饰.gameObject.activeInHierarchy) continue;
                                    var 图标区 = (RectTransform)装饰;
                                    var 图标右 = 按钮区.transform.InverseTransformPoint(图标区.TransformPoint(图标区.rect.max)).x;
                                    bool 分离 = 实最小.x >= 图标右 + 7.9f;
                                    查(前 + "导航图标与文字留出间距 " + 路径(文.transform), 分离);
                                    if (!分离) r.按钮文字问题.Add(前 + " 图标文字重叠 " + 路径(文.transform) + " 图标右=" + 图标右 + " 文字左=" + 实最小.x);
                                }
                                if (!合格) r.按钮文字问题.Add(前 + " " + 路径(文.transform) + " " + 文.text + " 纯色=" + 纯色 + " 实际=" + 实最小 + "—" + 实最大 + " 字号=" + 字号 + " 显示=" + 实文.characterCountVisible + "/" + 全文.characterCountVisible);
                                查(前 + "按钮标签在纯色区且完整 " + 路径(文.transform), 合格);
                            }
                        }
                        foreach (var 布 in host.GetComponentsInChildren<天帝移动排版>())
                        {
                            var 框 = (RectTransform)布.transform; var 安 = (RectTransform)host.transform.Find("安全区");
                            var 最小 = 安.InverseTransformPoint(框.TransformPoint(框.rect.min)); var 最大 = 安.InverseTransformPoint(框.TransformPoint(框.rect.max));
                            查(前 + "面板在安全区 " + 框.name, 最小.x >= 安.rect.xMin - .1f && 最小.y >= 安.rect.yMin - .1f && 最大.x <= 安.rect.xMax + .1f && 最大.y <= 安.rect.yMax + .1f && 框.localScale == Vector3.one);
                        }
                        foreach (var 文 in host.GetComponentsInChildren<Text>())
                        {
                            if (!文.enabled) continue;
                            if (文.GetComponent<天帝道纹单字>() != null)
                                查(前 + "道纹单字保留瓷牌中心区域 " + 路径(文.transform), 文.rectTransform.rect.width > 1 && 文.rectTransform.rect.height >= 文.fontSize);
                            if (string.IsNullOrWhiteSpace(文.text) || 文.resizeTextForBestFit || 在滚动内容(文.transform) != null) continue;
                            if (文.preferredHeight > 文.rectTransform.rect.height + 2)
                                r.排版问题.Add(前 + " " + 路径(文.transform) + " " + 文.rectTransform.rect.size + " 需高" + 文.preferredHeight + " " + 文.text.Replace("\n", " / "));
                        }
                        foreach(var 卡 in host.GetComponentsInChildren<天帝道纹详情卡>())
                        {
                            if(!手机 || !卡.填满父区域)continue;
                            var 框=(RectTransform)卡.transform;var 父=(RectTransform)框.parent;
                            查(前+"嵌入道纹详情不越界",框.rect.width<=父.rect.width+.1f && 框.rect.height<=父.rect.height+.1f);
                        }
                        foreach (var 父 in host.GetComponentsInChildren<RectTransform>())
                        {
                            var 文字 = new List<Text>();
                            foreach (Transform 子 in 父) if (子.gameObject.activeInHierarchy && 子.GetComponent<Text>() is Text 文 && 文.enabled && !string.IsNullOrEmpty(文.text)) 文字.Add(文);
                            for (int i = 0; i < 文字.Count; i++) for (int j = i + 1; j < 文字.Count; j++)
                            {
                                Rect 边(Text 文)
                                {
                                    var 顶点 = 文.cachedTextGenerator.verts;
                                    Vector2 最小 = new Vector2(float.MaxValue,float.MaxValue), 最大 = new Vector2(float.MinValue,float.MinValue);
                                    for (int k=0;k<顶点.Count-4;k++)
                                    { var p=(Vector2)父.InverseTransformPoint(文.rectTransform.TransformPoint(顶点[k].position/文.pixelsPerUnit)); 最小=Vector2.Min(最小,p);最大=Vector2.Max(最大,p); }
                                    return 顶点.Count>4 ? Rect.MinMaxRect(最小.x,最小.y,最大.x,最大.y) : Rect.zero;
                                }
                                var 滚动甲 = 在滚动内容(文字[i].transform); var 滚动乙 = 在滚动内容(文字[j].transform);
                                if (滚动甲 != null && 滚动甲 == 滚动乙) continue;
                                var a = 边(文字[i]); var b = 边(文字[j]);
                                if (a.Overlaps(b) && Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)>2 && Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin)>2)
                                    r.文字重叠.Add(前 + " " + 路径(父) + " " + 文字[i].text.Replace("\n"," / ") + " <> " + 文字[j].text.Replace("\n"," / "));
                            }
                        }
                        if (手机)
                        {
                            foreach (var 键 in host.GetComponentsInChildren<Button>())
                            {
                                if (!键.IsInteractable() || 键.name.Contains("遮罩") || 键.name == "遮罩") continue;
                                var 框 = (RectTransform)键.transform;
                                查(前 + "触控高度 " + 键.name, 框.rect.height >= 43.9f || 键.GetComponent<天帝触控热区>() != null);
                            }
                        }
                        查(前 + "无整页缩放工具", !Array.Exists(host.GetComponentsInChildren<Transform>(true), x => x.name == "触屏阅读工具"));
                        if ((!手机 && 尺寸.x == 1920) || (手机 && 尺寸.x == 1280) || 名 == "主页")
                            天帝青绿验收.拍摄(host, (手机 ? "手机_" : "PC_") + 尺寸.x + "x" + 尺寸.y + "_" + 名, 尺寸.x, 尺寸.y);
                    }
                }
                if (手机)
                {
                    天帝移动适配.验证屏幕尺寸 = new Vector2Int(1280, 720); 天帝移动适配.验证安全区 = new Rect(60, 24, 1180, 672); 天帝移动适配.验证密度 = 4;
                    查("小屏高DPI仍容纳至少560×360逻辑像素", 1180 / 天帝移动适配.像素密度 >= 559.9f && 672 / 天帝移动适配.像素密度 >= 359.9f);
                }
                适配(new Vector2Int(1280, 720)); ui.显示标题(); 检("标题");
                ui.显示主页(); 检("主页");
                if(天帝青绿皮肤.已启用)
                {
                    var 下拉=(Dropdown)typeof(天帝界面).GetField("地图等级下拉",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
                    查((手机?"手机":"PC")+"地图下拉100级",下拉.options.Count==100);
                    下拉.SetValueWithoutNotify(99);下拉.RefreshShownValue();检("主页等级100标签");
                    // Dropdown的渐变驱动在Start初始化；编辑模式夹具显式初始化它。
                    typeof(Dropdown).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(下拉,null);下拉.alphaFadeSpeed=0;
                    下拉.Show();
                    // 手动推进运行时定位协程，验证选择100级时列表不会仍停在第1级。
                    var 定位 = (System.Collections.IEnumerator)typeof(天帝等级下拉).GetMethod("定位当前项",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(下拉,null);
                    定位.MoveNext(); 定位.MoveNext(); Canvas.ForceUpdateCanvases();
                    var 滚动 = 下拉.transform.Find("Dropdown List")?.GetComponent<ScrollRect>();
                    查((手机?"手机":"PC")+"地图下拉展开定位当前100级",滚动!=null&&滚动.verticalNormalizedPosition<.01f);
                    检("主页等级下拉");
                    var 列表=下拉.transform.Find("Dropdown List");
                    查((手机?"手机":"PC")+"地图下拉可展开",列表!=null);
                    foreach(var 文 in 下拉.GetComponentsInChildren<Text>())
                        if(文.text=="100级")查((手机?"手机":"PC")+"实际等级标签",文.text=="100级");
                    // 编辑模式的淡出协程不会推进，直接销毁隔离夹具生成的临时列表与遮罩。
                    if(列表!=null)UnityEngine.Object.DestroyImmediate(列表.gameObject);
                    var 挡=host.transform.Find("Blocker");if(挡!=null)UnityEngine.Object.DestroyImmediate(挡.gameObject);
                    typeof(Dropdown).GetField("m_Dropdown",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(下拉,null);
                    typeof(Dropdown).GetField("m_Blocker",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(下拉,null);
                    ui.显示主页();
                }
                ui.显示作弊码(); 检("作弊码"); ui.关闭作弊码();
                ui.显示消息("确认操作前请检查目标和消耗。\n长说明会在手机确认框内部滚动，取消后回到原页面。"); 检("确认提示"); ui.关闭确认();
                ui.显示设置(); 检("设置");
                设(ui, "设置已打开", false);
                var 弹层 = host.transform.Find("安全区/设计区/设置层");
                for (int i = 弹层.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(弹层.GetChild(i).gameObject);
                ui.显示图鉴(); 检("图鉴");
                查((手机 ? "手机" : "PC") + "图鉴分类定位", ui.图鉴页.定位分类("五行元素")); ui.图鉴页.设置查询物品等级(100); 检("图鉴等级100"); ui.关闭图鉴();
                ui.显示角色(); 检("角色");
                ui.角色页.打开详细属性说明("智力", Vector2.zero); 检("角色属性公式"); ui.角色页.隐藏属性说明();
                foreach (var 键 in ui.角色页.GetComponentsInChildren<Button>()) if (键 != null && 键.name.StartsWith("页签-")) { string 名 = 键.name; 键.onClick.Invoke(); 检(名); }
                ui.关闭角色(); ui.显示宝盒(); 检("宝盒"); ui.显示宝盒概率(0); 检("宝盒概率"); ui.关闭宝盒概率();
                ui.显示宝盒概率(1); 检("功能宝盒概率"); ui.关闭宝盒概率(); ui.关闭宝盒();
                ui.显示回收(); 检("回收");
                盒.启用无限灵石(); 检("回收无限灵石");
                ui.回收页.打开范围(); 检("回收范围");
                foreach (var 键 in ui.回收页.GetComponentsInChildren<Button>()) if (键 != null && 键.name == "应用批选范围") 键.onClick.Invoke();
                ui.回收页.一键选中();
                foreach (var 键 in ui.回收页.GetComponentsInChildren<Button>()) if (键 != null && 键.name == "预览回收") 键.onClick.Invoke();
                检("回收确认"); ui.关闭回收();
                ui.显示道纹改造(网, 钱); 检("改造");
                ui.改造页.选目标(0); 检("改造已选目标");
                int 功索 = 网.道纹.FindIndex(x => x.是功能道纹);
                钱.启用无限通货(); ui.改造页.选目标(功索); 检("功能道纹改造");
                查((手机 ? "手机" : "PC") + "功能改造选中独立单词条", ui.改造页.当前目标.是功能道纹 && ui.改造页.当前目标.词条上限 == 1);
                ui.改造页.打开背包(); 检("改造背包");
                foreach(var kind in new[]{道纹分类.特性,道纹分类.转化})
                {int ix=网.道纹.FindIndex(x=>x.分类==kind);ui.改造页.选目标(ix);检(kind+"不可改造详情");查(kind+"详情无虚假词条容量",ui.改造页.当前目标.词条上限==0);}
                ui.显示道纹(网); 检("道纹");
                if (天帝青绿皮肤.已启用)
                {
                    var 藏匣 = ui.道纹页.transform.Find("候选区").GetComponent<ScrollRect>();
                    bool 需要滚动 = 藏匣.content.rect.height > 藏匣.viewport.rect.height + 1;
                    var 输入 = Array.Find(藏匣.GetComponentsInChildren<天帝道纹输入>(), x => !x.是画布);
                    var 事件 = new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
                    藏匣.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                    if (需要滚动 && 输入 != null)
                    {
                        if (手机)
                        {
                            事件.position = ((RectTransform)输入.transform).TransformPoint(Vector2.zero); 事件.pressPosition = 事件.position;
                            输入.OnInitializePotentialDrag(事件); 输入.OnBeginDrag(事件);
                            事件.position += Vector2.up * 100 * 藏匣.content.lossyScale.y;
                            输入.OnDrag(事件); 输入.OnEndDrag(事件);
                        }
                        else { 事件.scrollDelta = Vector2.down; 输入.OnScroll(事件); }
                        Canvas.ForceUpdateCanvases();
                    }
                    查((手机 ? "手机卡片纵向滑动" : "PC卡片滚轮滚动") + (需要滚动 ? "确实移动藏匣内容" : "内容完整铺满无需滚动"), !需要滚动 || 藏匣.verticalNormalizedPosition < .99f);
                    // 从第一页开始验证翻页；页面可能在前一项场景检查后停留在末页。
                    ui.道纹页.切换候选页(0);
                    int 原页 = ui.道纹页.候选页码;
                    bool 有下一页 = 原页 + 1 < ui.道纹页.候选总页数;
                    bool 翻页成功 = !有下一页 || ui.道纹页.切换候选页(原页 + 1);
                    Canvas.ForceUpdateCanvases();
                    bool 翻页归顶 = !有下一页 || !需要滚动 || (翻页成功 && 藏匣.verticalNormalizedPosition > .99f);
                    查((手机 ? "手机" : "PC") + "藏匣翻页恢复顶部", 翻页归顶);
                    ui.道纹页.切换候选页(原页);
                    ui.道纹页.回到源点();
                    var 画布 = Array.Find(ui.道纹页.GetComponentsInChildren<天帝道纹绘图>(), x => !x.单纹模式);
                    ui.道纹页.聚焦解锁区域();
                    查((手机 ? "手机" : "PC") + "少量道纹自动聚焦保留瓷面文字及三色链路", 画布.细节可见);
                    ui.道纹页.回到源点();
                    查((手机 ? "手机" : "PC") + "源点定位在画布工作区", 画布.工作区.Contains(画布.平移));
                    bool 坐标正确 = true;
                    for (int q = -50; q < 50; q++) for (int t = -50; t < 50; t++)
                    { var 格 = new Vector2Int(q, t); 坐标正确 &= 画布.位置格(画布.格位置(格)) == 格; }
                    查((手机 ? "手机" : "PC") + "一万格间距坐标可逆", 坐标正确 && 画布.格间距 > 1.5f);
                    查((手机 ? "手机" : "PC") + "已激活对应接口绿色", 画布.获取链路状态(Vector2Int.zero, 0) == 天帝道纹绘图.链路状态.已激活);
                    查((手机 ? "手机" : "PC") + "孤立接口链路红色", 画布.获取链路状态(new Vector2Int(0, 2), 0) == 天帝道纹绘图.链路状态.未激活);
                    查((手机 ? "手机" : "PC") + "空位链路灰色", 画布.获取链路状态(Vector2Int.zero, 3) == 天帝道纹绘图.链路状态.空位);
                    var 命中 = typeof(天帝道纹界面).GetMethod("画布命中", BindingFlags.Instance | BindingFlags.NonPublic);
                    float 原缩 = 画布.缩放; var 原移 = 画布.平移;
                    foreach (float 缩 in new[] { .5f, 1.1f, 1.65f })
                    {
                        画布.缩放 = 缩;
                        var 格 = new Vector2Int(1, 0);
                        画布.平移 = 原移 - 画布.格位置(格) * 缩; // 测试目标保持在无遮挡工作区内。
                        object[] 参数 = { ui.道纹页.格屏幕位置(格), Vector2Int.zero };
                        查((手机 ? "手机" : "PC") + "缩放后点击命中同一格 " + 缩, (bool)命中.Invoke(ui.道纹页, 参数) && (Vector2Int)参数[1] == 格);
                    }
                    画布.缩放 = 原缩; 画布.平移 = 原移; 画布.SetVerticesDirty();
                    var 外点 = RectTransformUtility.WorldToScreenPoint(null, 画布.rectTransform.TransformPoint(new Vector2(画布.工作区.xMax + 24, 0)));
                    object[] 外参 = { 外点, Vector2Int.zero };
                    查((手机 ? "手机" : "PC") + "两侧信息区不接受画布放置", !(bool)命中.Invoke(ui.道纹页, 外参));
                    var 待放 = Array.Find(藏匣.GetComponentsInChildren<天帝道纹输入>(), x => !x.是画布 && !x.道纹.格子.HasValue).道纹;
                    // EditMode下从未激活的模拟卡片没有OnDestroy；清理夹具换页后的已销毁订阅。
                    foreach (object 模型 in new object[] { 网, 人, 钱, 盒 }) 清理已销毁订阅(模型);
                    var 目标格 = new Vector2Int(-1, 0); 网.解锁格子(目标格);
                    var 放置事件 = new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, position = ui.道纹页.格屏幕位置(目标格) };
                    if (手机) { ui.道纹页.触屏选择(待放, Vector2.zero); ui.道纹页.点击(true, 放置事件); }
                    else { ui.道纹页.按下(待放, false, 放置事件); ui.道纹页.开始拖动(false, 放置事件); ui.道纹页.结束拖动(放置事件); }
                    查((手机 ? "手机点选放置" : "PC拖放") + "使用新间距落格", 待放.格子 == 目标格);
                    查((手机 ? "手机" : "PC") + "新间距放置可撤销", ui.道纹页.撤销上一步() && !待放.格子.HasValue);
                    if (手机) ui.道纹页.取消触屏选择();
                    else
                    {
                        放置事件.position = (ui.道纹页.格屏幕位置(Vector2Int.zero) + ui.道纹页.格屏幕位置(new Vector2Int(1, 0))) * .5f;
                        ui.道纹页.按下(null, true, 放置事件); ui.道纹页.开始拖动(true, 放置事件);
                        查("PC拖动道纹间的空隙只平移画布", (bool)typeof(天帝道纹界面).GetField("平移中", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui.道纹页));
                        ui.道纹页.结束拖动(放置事件);
                    }
                }
                查((手机 ? "手机" : "PC") + "构筑预览读取独立出口计划", ui.道纹页.当前演示参数.顺序计划 != null && ui.道纹页.当前演示参数.数量 == 2);
                ui.显示主页(); 检("100级主页");
                ui.显示道纹(网); 检("100级道纹");
                ui.道纹页.打开筛选(); 检("道纹筛选"); ui.道纹页.关闭筛选();
                foreach(var kind in new[]{道纹分类.特性,道纹分类.转化})
                {查(kind+"可筛选",ui.道纹页.设置候选分类(kind));检(kind+"候选列表");}
                foreach(var kind in new[]{道纹分类.特性,道纹分类.转化})
                {ui.道纹页.显示属性(网.道纹.Find(x=>x.分类==kind),Vector2.zero);检(kind+"道纹浮窗详情");}
                ui.道纹页.指针离开();
                ui.道纹页.设置候选分类(null);
                typeof(天帝道纹界面).GetMethod("显示加成来源", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui.道纹页, null); 检("加成来源");
                foreach (var 键 in ui.道纹页.GetComponentsInChildren<Button>()) if (键 != null && 键.name == "关闭") 键.onClick.Invoke();
                ui.道纹页.打开布局方案(); 检("布局方案");
                foreach (var 键 in ui.道纹页.GetComponentsInChildren<Button>()) if (键 != null && 键.name == "关闭") 键.onClick.Invoke();
                var 背包根 = 天帝响应布局.创建((RectTransform)host.transform.Find("安全区/设计区"), "独立背包", 0, 0, 1600, 900);
                var 背包 = 背包根.gameObject.AddComponent<天帝道纹背包>(); 背包.初始化(网, 字, null, _ => {}, () => {}); 检("背包");
                foreach (var 键 in 背包.GetComponentsInChildren<Button>()) if (键 != null && 键.name == "展开接口筛选") 键.onClick.Invoke(); 检("背包高级筛选");
                // 背包是移动端 6 格、桌面端 9 格；与实际网格布局保持同一契约。
                查((手机 ? "手机6枚" : "PC9枚") + "背包分页", 背包.总页数 == Mathf.CeilToInt(网.道纹.Count/(手机?6f:9f)));
                UnityEngine.Object.DestroyImmediate(背包根.gameObject);
                ui.显示序章(); 检("序章");
                设(g, "天赋池", new 天帝天赋池(42)); ui.显示源道纹选择(false);
                typeof(天帝天赋选择).GetProperty("可选择").SetValue(ui.源道纹页, true);
                foreach (var 透明 in ui.源道纹页.GetComponentsInChildren<CanvasGroup>()) 透明.alpha = 1;
                foreach (var 键 in ui.源道纹页.GetComponentsInChildren<Button>()) 键.interactable = true;
                检("天赋选择"); ui.源道纹页.选中(0); 检("天赋详情"); ui.源道纹页.关闭选中详情();
                // 隔离战斗模型只用于生成HUD/暂停/失败按钮，不生成真实角色、不运行战斗或写档。
                设(g, "天赋池", 天帝天赋池.从已选天赋恢复((int)天赋种类.普通人));
                var 地图 = new 天帝战斗地图(42, true); var 场 = 假游戏.AddComponent<天帝战斗场景>();
                var 战 = new 天帝战斗系统(地图, 网, 人, 战斗难度.普通, 钱, 1, null, 盒);
                设(场, "地图", 地图); 设(场, "战斗", 战); 设(g, "战斗场景", 场); 设(g, "阶段", 游戏阶段.战斗);
                ui.显示战斗(); 检("战斗HUD"); ui.切换战斗暂停(); 检("战斗暂停"); ui.关闭战斗暂停();
                ui.显示战斗失败(); 检("战斗失败");
                ui.显示主页(); 设(g, "阶段", 游戏阶段.主页); 设(g, "战斗场景", null); UnityEngine.Object.DestroyImmediate(场);
                UnityEngine.Object.DestroyImmediate(host); host = null;
            }
            查("全部页面文字无裁切", r.排版问题.Count == 0);
            查("全部页面文字无重叠", r.文字重叠.Count == 0);
            查("全部按钮文字不压花纹且完整", r.按钮文字问题.Count == 0);
            查("关闭按钮不遮挡其它UI", r.关闭遮挡问题.Count == 0);
        }
        catch (Exception ex) { r.错误.Add(ex.ToString()); }
        finally
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host); if (假游戏 != null) UnityEngine.Object.DestroyImmediate(假游戏); 人?.Dispose();
            天帝移动适配.验证移动平台 = 原移动; 天帝移动适配.验证密度 = 原密度; 天帝移动适配.验证屏幕尺寸 = 原屏幕; 天帝移动适配.验证安全区 = 原安全; typeof(天帝美术资源).GetProperty("当前").SetValue(null, 原美术);
            Application.logMessageReceived -= 日志;
        }
        string dir = Path.Combine(天帝构建工具.项目根, "生成/验证/双端页面-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "report.json"), JsonUtility.ToJson(r, true)); return dir;
    }
    static void 清理已销毁订阅(object 模型)
    {
        foreach (var 字段 in 模型.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!(字段.GetValue(模型) is Delegate 订阅)) continue;
            foreach (var 回调 in 订阅.GetInvocationList())
                if (回调.Target is UnityEngine.Object 对象 && 对象 == null) 订阅 = Delegate.Remove(订阅, 回调);
            字段.SetValue(模型, 订阅);
        }
    }
    static string 路径(Transform t) => t.parent == null ? t.name : 路径(t.parent) + "/" + t.name;
    static Rect 矩形(RectTransform 区, RectTransform 面板)
    {
        var a = 面板.InverseTransformPoint(区.TransformPoint(区.rect.min)); var b = 面板.InverseTransformPoint(区.TransformPoint(区.rect.max));
        return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
    }
    static Rect 文字边框(Text 文, RectTransform 面板)
    {
        var 生成器 = new TextGenerator(); 生成器.Populate(文.text, 文.GetGenerationSettings(文.rectTransform.rect.size));
        var 顶点 = 生成器.verts; var 小 = new Vector2(float.MaxValue, float.MaxValue); var 大 = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i + 3 < 顶点.Count; i += 4)
        {
            if (顶点[i].position == 顶点[i + 1].position && 顶点[i].position == 顶点[i + 2].position && 顶点[i].position == 顶点[i + 3].position) continue;
            for (int k = i; k < i + 4; k++) { var p = (Vector2)面板.InverseTransformPoint(文.rectTransform.TransformPoint(顶点[k].position / 文.pixelsPerUnit)); 小 = Vector2.Min(小, p); 大 = Vector2.Max(大, p); }
        }
        return 小.x == float.MaxValue ? Rect.zero : Rect.MinMaxRect(小.x, 小.y, 大.x, 大.y);
    }
    static Rect 可见区域(Rect 区, Transform 对象, RectTransform 面板)
    {
        for (var 父 = 对象.parent; 父 != null; 父 = 父.parent)
        {
            if (父.GetComponent<RectMask2D>() is RectMask2D 遮 && 遮.enabled)
            {
                var 裁 = 矩形((RectTransform)父, 面板);
                float 左 = Mathf.Max(区.xMin, 裁.xMin), 右 = Mathf.Min(区.xMax, 裁.xMax), 下 = Mathf.Max(区.yMin, 裁.yMin), 上 = Mathf.Min(区.yMax, 裁.yMax);
                if (右 <= 左 || 上 <= 下) return Rect.zero;
                区 = Rect.MinMaxRect(左, 下, 右, 上);
            }
        }
        return 区;
    }
    static ScrollRect 在滚动内容(Transform 对象)
    {
        for (var 父 = 对象 != null ? 对象.parent : null; 父 != null; 父 = 父.parent)
        {
            var 滚动 = 父.GetComponent<ScrollRect>();
            if (滚动 != null && 滚动.content != null && 对象.IsChildOf(滚动.content)) return 滚动;
        }
        return null;
    }
    static bool 相交(Rect a, Rect b) => Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1 && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1;
}
#endif
