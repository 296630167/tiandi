using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum 道纹候选排序 { 默认, 品阶, 类型 }

public sealed partial class 天帝道纹界面 : MonoBehaviour
{
    public 天帝道纹 数据 { get; private set; }
    public 天帝道纹绘图 画布 { get; private set; }
    public bool 浮窗显示 => 浮窗 != null && 浮窗.gameObject.activeSelf;
    public bool 引导讲解中 { get; set; }
    public bool 拖动中 => 拖纹 != null;
    public bool? 放置反馈 => 拖纹 == null || !画布.预览格.HasValue ? (bool?)null : 画布.预览可放;
    readonly List<天帝道纹绘图> 候选图 = new List<天帝道纹绘图>();
    readonly List<Text> 候选状态 = new List<Text>();
    readonly List<Text> 候选说明 = new List<Text>();
    readonly List<Text> 候选品阶 = new List<Text>();
    readonly List<Text> 候选短名 = new List<Text>();
    readonly List<CanvasGroup> 候选透明 = new List<CanvasGroup>();
    readonly List<GameObject> 候选卡 = new List<GameObject>();
    readonly List<天帝道纹锁定按钮> 候选锁 = new List<天帝道纹锁定按钮>();
    readonly List<Image> 候选状态图 = new List<Image>();
    int 每页数量 => 天帝青绿皮肤.已启用 ? (天帝移动适配.启用 ? 4 : 6) : (天帝移动适配.启用 ? 4 : 8);
    readonly List<int> 显示索引 = new List<int>();
    public 道纹候选排序 当前排序 { get; private set; }
    public 道纹分类? 当前分类 { get; private set; }
    public 道纹属性分组? 当前分组 { get; private set; }
    public int 筛选结果数 => 显示索引.Count;
    public bool 筛选已打开 => (筛选层 != null && 筛选层.gameObject.activeSelf) || (来源层 != null && 来源层.gameObject.activeSelf) || (方案层 != null && 方案层.gameObject.activeSelf);
    RectTransform 筛选层;
    Text 空列表提示;
    Button 上页按钮, 下页按钮;
    readonly Dictionary<string, Text> 筛选选项 = new Dictionary<string, Text>();
    public int 候选页码 { get; private set; }
    public int 候选总页数 => Mathf.Max(1, Mathf.CeilToInt(筛选结果数 / (float)每页数量));
    Text 页码字;
    readonly Dictionary<道纹实例, Text> 格标签 = new Dictionary<道纹实例, Text>();
    Font 字体;
    RectTransform 根;
    RectTransform 候选区;
    RectTransform 浮窗;
    天帝道纹详情卡 详情卡;
    RectTransform 拖影;
    Text 提示字;
    RectTransform 解锁提示框;
    float 解锁提示截止;
    bool 展开操作说明;
    Text 汇总字;
    Text 状态字;
    Text 成长字;
    天帝道纹绘图 拖影图;
    Text 拖影字;
    Image 拖放状态图;
    道纹实例 按下纹;
    道纹实例 拖纹;
    道纹实例 上次点击纹;
    bool 平移中;
    Vector2 指针位置;
    bool 有指针;
    Color 文字色 = 天帝道纹美术.正文;
    public void 初始化(天帝道纹 数据, Font 字体, Action 返回)
    {
        this.数据 = 数据; this.字体 = 字体; 根 = (RectTransform)transform;
        底(根, "道纹背景", 0, 0, 1600, 900, new Color(0.045f, 0.075f, 0.09f));
        var 标题纸=底(根,"页面标题衬纸",28,12,1310,50,Color.white).GetComponent<Image>();
        天帝道纹美术.应用(标题纸,"小信息框");标题纸.color=new Color(1,1,1,.90f);
        文字(根, "道纹构筑", 36, 15, 270, 46, 30, TextAnchor.MiddleLeft);
        成长字 = 文字(根, "", 300, 24, 940, 31, 18, TextAnchor.MiddleLeft);
        成长字.color = 天帝道纹美术.次文;
        按钮(根, "返回主页", 1380, 22, 188, 42, () => { 取消拖动(); 返回(); });
        底(根, "画布主面板", 28, 78, 1092, 564, Color.white);
        汇总字 = 文字(根, "", 48, 85, 360, 38, 17, TextAnchor.MiddleLeft);
        汇总字.color = 天帝道纹美术.次文;
        var 视口 = 底(根, "画布视口", 40, 134, 1068, 494, new Color(0.06f, 0.10f, 0.12f));
        视口.gameObject.AddComponent<RectMask2D>();
        var 网格 = new GameObject("六边形画布", typeof(RectTransform), typeof(CanvasRenderer), typeof(天帝道纹绘图));
        var 区 = 网格.GetComponent<RectTransform>(); 区.SetParent(视口, false); 区.anchorMin = Vector2.zero; 区.anchorMax = Vector2.one; 区.offsetMin = 区.offsetMax = Vector2.zero;
        画布 = 网格.GetComponent<天帝道纹绘图>(); 画布.数据 = 数据; 画布.raycastTarget = true; 画布.构筑美术 = true;
        天帝响应布局.动态(区);
        var 输入 = 网格.AddComponent<天帝道纹输入>(); 输入.页面 = this; 输入.是画布 = true;
        建格标签(数据.已放置[Vector2Int.zero]);
        foreach (var 纹 in 数据.道纹) 建格标签(纹);
        提示字 = 文字(根, "", 40, 646, 1396, 28, 15, TextAnchor.MiddleLeft); 提示字.color = 天帝道纹美术.次文;
        按钮(根, "操作说明", 1450, 646, 118, 28, () => { 展开操作说明 = !展开操作说明; 刷新(); });
        var 藏匣纸=底(根,"藏匣标题衬纸",28,682,1544,42,Color.white).GetComponent<Image>();
        天帝道纹美术.应用(藏匣纸,"小信息框");藏匣纸.color=new Color(1,1,1,.93f);
        状态字 = 文字(根, "道纹藏匣  /  拖入已解锁格", 36, 686, 470, 36, 20, TextAnchor.MiddleLeft);
        var 分隔 = 区块(根, "藏匣分割线", 36, 674, 1528, 5).gameObject.AddComponent<Image>(); 天帝道纹美术.应用(分隔, "分割线");
        按钮(根, "筛选 / 排序", 514, 682, 156, 40, 打开筛选);
        页码字 = 文字(根, "", 682, 686, 608, 36, 15, TextAnchor.MiddleRight); 页码字.color = 天帝道纹美术.次文;
        上页按钮 = 按钮(根, "上一页", 1308, 682, 118, 40, () => 切换候选页(候选页码 - 1));
        下页按钮 = 按钮(根, "下一页", 1442, 682, 118, 40, () => 切换候选页(候选页码 + 1));
        候选区 = 区块(根, "候选区", 32, 731, 1536, 155);
        for (int i = 0; i < 数据.道纹.Count; i++)
        {
            var 纹 = 数据.道纹[i]; float 间距 = 1536f / 每页数量, 宽 = 间距 - 12, x = (i % 每页数量) * 间距;
            var 卡 = 底(候选区, "候选道纹-" + 纹.编号, x + 2, 0, 宽, 153, new Color(0.09f, 0.14f, 0.16f));
            候选透明.Add(卡.gameObject.AddComponent<CanvasGroup>());
            候选卡.Add(卡.gameObject);
            候选品阶.Add(文字(卡, 天帝道纹品阶.彩色品阶文字(纹.品阶), 8, 5, 宽 - 48, 23, 13, TextAnchor.MiddleLeft));
            var 图区 = 区块(卡, "道纹图", (宽 - 102) / 2, 24, 102, 72);
            var 图 = 图区.gameObject.AddComponent<天帝道纹绘图>(); 图.单纹模式 = true; 图.单纹半径 = 30; 图.单纹 = 纹; 图.raycastTarget = false; 图.构筑美术 = true; 候选图.Add(图);
            图.数据=数据;
            var 选框 = 区块(卡, "道纹选中框", (宽 - 82) / 2, 18, 82, 82).gameObject.AddComponent<Image>();
            天帝道纹美术.应用(选框, "选中框", false); 选框.enabled = false;
            卡.gameObject.AddComponent<天帝道纹卡片美术>().选框 = 选框;
            var 色 = 天帝道纹绘图.品阶色(纹);
            var 短名字 = 文字(卡, 天帝道纹美术.单字(纹), (宽 - 60) / 2, 35, 60, 45, 27, TextAnchor.MiddleCenter); 短名字.color = 色; 候选短名.Add(短名字);
            天帝道纹单字.绑定(短名字, 图区);
            var 说明 = 文字(卡, 纹.候选说明, 8, 94, 宽 - 16, 28, 15, TextAnchor.MiddleCenter); 说明.color = 文字色; 候选说明.Add(说明);
            候选状态.Add(文字(卡, "", 30, 124, 宽 - 38, 21, 13, TextAnchor.MiddleLeft));
            var 状态像 = 区块(卡, "装备状态图", 10, 126, 18, 18).gameObject.AddComponent<Image>();
            天帝道纹美术.应用(状态像, "未装备", false); 候选状态图.Add(状态像);
            var 卡输入 = 卡.gameObject.AddComponent<天帝道纹输入>(); 卡输入.页面 = this; 卡输入.道纹 = 纹;
            var 锁=天帝道纹锁定按钮.创建(卡,数据);锁.设置(纹);候选锁.Add(锁);
        }
        浮窗 = 区块(根, "道纹属性浮窗", 0, 0, 天帝道纹详情卡.宽度, 440);
        详情卡 = 浮窗.gameObject.AddComponent<天帝道纹详情卡>(); 详情卡.初始化(字体, true);
        详情卡.设置数据(数据);
        拖影 = 区块(根, "拖动道纹", 0, 0, 108, 108); 拖影.pivot = new Vector2(0.5f, 0.5f);
        拖影图 = 拖影.gameObject.AddComponent<天帝道纹绘图>(); 拖影图.单纹模式 = true; 拖影图.幽灵 = true; 拖影图.raycastTarget = false; 拖影图.构筑美术 = true;
        拖影图.数据=数据;
        拖影字 = 文字(拖影, "", 0, 30, 108, 48, 30, TextAnchor.MiddleCenter); 拖影.gameObject.SetActive(false);
        天帝道纹单字.绑定(拖影字, 拖影);
        拖放状态图 = 区块(拖影, "放置状态", 88, 70, 24, 24).gameObject.AddComponent<Image>(); 天帝道纹美术.应用(拖放状态图, "可替换", false);
        空列表提示 = 文字(候选区, "此分类暂无道纹 · 可在筛选中选择“显示全部”", 0, 40, 1536, 70, 23, TextAnchor.MiddleCenter);
        建筛选菜单();
        建构筑预览();
        建操作工具();
        if (天帝青绿皮肤.已启用) 建悬浮布局(); else 建触屏操作();
        装配山水构筑();
        数据.状态改变 += 刷新; 刷新(); 聚焦解锁区域();
    }
    RectTransform 区块(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    RectTransform 底(RectTransform 父, string 名, float x, float y, float w, float h, Color 色)
    {
        var 区 = 区块(父, 名, x, y, w, h); var 像 = 区.gameObject.AddComponent<Image>(); 像.color = 色;
        var 资源名 = 天帝道纹美术.面板名(名);
        if (资源名 != null) 天帝道纹美术.应用(像, 资源名, 名 != "道纹背景");
        if (天帝道纹美术.彩绘皮肤 && (名 == "画布视口" || 名 == "攻击形态演示")) 像.color = 天帝道纹美术.画布石青;
        像.raycastTarget = 名.StartsWith("候选道纹-"); return 区;
    }
    Text 文字(RectTransform 父, string 文, float x, float y, float w, float h, int 大小, TextAnchor 对齐)
    {
        var 字 = 区块(父, "文字", x, y, w, h).gameObject.AddComponent<Text>(); 字.font = 字体; 字.text = 文; 字.fontSize = 大小;
        字.color = 文字色; 字.alignment = 对齐; 字.raycastTarget = false; 字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Overflow; 天帝界面美术.文字(字); return 字;
    }
    Button 按钮(RectTransform 父, string 名, float x, float y, float w, float h, Action 点击)
    {
        var 区 = 底(父, 名, x, y, w, h, new Color(0.15f, 0.24f, 0.25f)); var 像 = 区.GetComponent<Image>(); 像.raycastTarget = true;
        var 键 = 区.gameObject.AddComponent<Button>(); 键.targetGraphic = 像; 键.onClick.AddListener(() => 点击());
        var 文 = 文字(区, 名, 6, 0, w-12, h, h <= 38 ? 17 : 19, TextAnchor.MiddleCenter);
        bool 主=名.StartsWith("应用");
        天帝界面美术.按钮(键,主);
        string 图标 = 名 == "返回主页" ? "返回图标" : 名 == "回到源点" ? "源点图标" : 名 == "撤销上一步" ? "撤销图标" : 名 == "旋转 · R" ? "旋转图标" : null;
        if (图标 != null && 天帝道纹美术.已接入)
        {
            var 标 = 区块(区, 图标, 8, (h-20)/2, 20, 20).gameObject.AddComponent<Image>(); 天帝道纹美术.应用(标, 图标, false);
            文.rectTransform.anchoredPosition = new Vector2(25, 0); 文.rectTransform.sizeDelta = new Vector2(w-29,h);
        }
        像.raycastTarget = true;
        if(山水构筑){天帝图三四山水素材.按钮(键);文.font=字体;}
        return 键;
    }
    void 建筛选菜单()
    {
        筛选层 = 区块(根, "道纹筛选层", 0, 0, 1600, 900);
        var 遮 = 底(筛选层, "关闭筛选遮罩", 0, 0, 1600, 900, new Color(0, 0, 0, 0.18f));
        var 遮像 = 遮.GetComponent<Image>(); 遮像.raycastTarget = true;
        var 遮键 = 遮.gameObject.AddComponent<Button>(); 遮键.targetGraphic = 遮像;
        var 色 = 遮键.colors; 色.highlightedColor = 色.pressedColor = Color.white; 遮键.colors = 色;
        遮键.onClick.AddListener(关闭筛选);
        var 框 = 底(筛选层, "筛选排序菜单", 514, 100, 468, 530, new Color(0.055f, 0.105f, 0.12f));
        框.GetComponent<Image>().raycastTarget = true;
        文字(框, "候选道纹筛选与排序", 16, 8, 304, 34, 20, TextAnchor.MiddleLeft);
        string[] 名 = { "按品阶排序", "按属性分组排序", "显示全部", "基础属性", "普通属性", "形态属性", "元素属性", "分叉道纹", "特性道纹", "转化道纹" };
        Action[] 动作 = { () => 设置候选排序(道纹候选排序.品阶), () => 设置候选排序(道纹候选排序.类型),
            () => 设置候选分组(null), () => 设置候选分组(道纹属性分组.基础),
            () => 设置候选分组(道纹属性分组.普通), () => 设置候选分组(道纹属性分组.形态), () => 设置候选分组(道纹属性分组.元素),
            () => 设置候选分类(道纹分类.分叉),()=>设置候选分类(道纹分类.特性),()=>设置候选分类(道纹分类.转化) };
        文字(框, "排序", 16, 54, 436, 24, 16, TextAnchor.MiddleLeft).color = 天帝道纹美术.次文;
        文字(框, "道纹类别", 16, 144, 436, 24, 16, TextAnchor.MiddleLeft).color = 天帝道纹美术.次文;
        for (int i = 0; i < 名.Length; i++)
        {
            float x = i < 2 ? 16 + i * 224 : 16 + (i - 2) % 3 * 148;
            float y = i < 2 ? 86 : 178 + (i - 2) / 3 * 50;
            var 键 = 按钮(框, 名[i], x, y, i < 2 ? 212 : 140, 42, 动作[i]); 筛选选项[名[i]] = 键.GetComponentInChildren<Text>();
            if (名[i] == "形态属性") 筛选选项[名[i]].text = "功能道纹";
        }
        建接口筛选(框);
        if (天帝移动适配.启用)
        {
            var 重排 = 天帝双端页面布局.重排正文(框);
            var 口 = 天帝响应布局.滚动正文(框);
            天帝响应布局.比例(口, .1f, .04f, .8f, .92f);
            var 布局 = 口.gameObject.AddComponent<天帝移动排版>(); 布局.排版 = _ => 重排();
        }
        筛选层.gameObject.SetActive(false);
    }
    public void 打开筛选()
    {
        if (拖动中 || 平移中) return;
        指针离开(); 上次点击纹 = 按下纹 = null;
        更新接口筛选文字();
        筛选选项["按品阶排序"].text = (当前排序 == 道纹候选排序.品阶 ? "✓ " : "") + "按品阶排序 · 高到低";
        筛选选项["按属性分组排序"].text = (当前排序 == 道纹候选排序.类型 ? "✓ " : "") + "按属性分组排序";
        foreach (var 名 in new[] { "显示全部", "基础属性", "普通属性", "形态属性", "元素属性", "分叉道纹", "特性道纹", "转化道纹" })
        {
            bool 选中 = 名 == "显示全部" ? !当前分组.HasValue && !当前分类.HasValue :
                名 == "分叉道纹" ? 当前分类 == 道纹分类.分叉 : 名 == "特性道纹"?当前分类==道纹分类.特性:名=="转化道纹"?当前分类==道纹分类.转化:名 == 当前分组 + "属性";
            筛选选项[名].text = (选中 ? "✓ " : "") + (名 == "形态属性" ? "功能道纹" : 名);
        }
        筛选层.SetAsLastSibling(); 筛选层.gameObject.SetActive(true);
        if(山水构筑)foreach(var 键 in 筛选层.GetComponentsInChildren<Button>())if(键.name!="关闭筛选遮罩")天帝图三四山水素材.按钮(键);
    }
    public void 关闭筛选() { if (筛选层 != null) 筛选层.gameObject.SetActive(false); if (来源层 != null) 来源层.gameObject.SetActive(false); if (方案层 != null) 方案层.gameObject.SetActive(false); 指针离开(); }
    public bool 设置候选排序(道纹候选排序 排序)
    {
        if (拖动中 || 平移中 || 排序 < 道纹候选排序.默认 || 排序 > 道纹候选排序.类型) return false;
        当前排序 = 排序; 候选页码 = 0; 关闭筛选(); 刷新(); return true;
    }
    public bool 设置候选分类(道纹分类? 分类)
    {
        if (拖动中 || 平移中 || (分类.HasValue && 分类 != 道纹分类.属性 && 分类 != 道纹分类.分叉 && 分类 != 道纹分类.功能 && 分类 != 道纹分类.特性 && 分类 != 道纹分类.转化)) return false;
        当前分类 = 分类; 当前分组 = null; 候选页码 = 0; 关闭筛选(); 刷新(); return true;
    }
    public bool 设置候选分组(道纹属性分组? 分组)
    {
        if (拖动中 || 平移中 || (分组.HasValue && (分组 < 道纹属性分组.基础 || 分组 > 道纹属性分组.普通))) return false;
        当前分类 = null; 当前分组 = 分组; 候选页码 = 0; 关闭筛选(); 刷新(); return true;
    }
    public 道纹实例 候选显示项(int 显示序号)
        => 显示序号 >= 0 && 显示序号 < 显示索引.Count ? 数据.道纹[显示索引[显示序号]] : null;
    void 刷新显示索引()
    {
        显示索引.Clear();
        for (int i = 0; i < 数据.道纹.Count; i++)
            if ((!当前分类.HasValue || 数据.道纹[i].分类 == 当前分类) &&
                天帝道纹操作筛选.接口匹配(数据.道纹[i].接口, 当前接口筛选, 允许旋转筛选) &&
                (!当前分组.HasValue || 数据.道纹[i].词条.Exists(x => 天帝道纹属性.分组(x.属性) == 当前分组))) 显示索引.Add(i);
        if (当前排序 != 道纹候选排序.默认) 显示索引.Sort((a, b) =>
        {
            var 甲 = 数据.道纹[a]; var 乙 = 数据.道纹[b]; int 比;
            if (当前排序 == 道纹候选排序.品阶)
            { 比 = 乙.品阶.CompareTo(甲.品阶); if (比 == 0) 比 = 甲.分类.CompareTo(乙.分类); }
            else { 比 = 甲.分类 != 乙.分类 ? 甲.分类.CompareTo(乙.分类) : 天帝道纹属性.分组(甲.属性).CompareTo(天帝道纹属性.分组(乙.属性)); if (比 == 0) 比 = 乙.品阶.CompareTo(甲.品阶); }
            return 比 != 0 ? 比 : 甲.编号.CompareTo(乙.编号);
        });
        候选页码 = Mathf.Clamp(候选页码, 0, 候选总页数 - 1);
        foreach (var 卡 in 候选卡) 卡.SetActive(false);
        for (int j = 候选页码 * 每页数量; j < Mathf.Min(显示索引.Count, (候选页码 + 1) * 每页数量); j++)
        {
            var 卡 = 候选卡[显示索引[j]]; var 区 = (RectTransform)卡.transform;
            if (天帝青绿皮肤.已启用) 排悬浮候选卡(显示索引[j], j % 每页数量);
            else if (天帝移动适配.启用) 天帝双端页面布局.固定(区, j % 每页数量 * 160, 0, 154, 90);
            else 天帝响应布局.设计位置(区, new Vector2(2 + j % 每页数量 * (1536f / 每页数量), 0));
            卡.SetActive(true);
        }
        空列表提示.gameObject.SetActive(显示索引.Count == 0);
        空列表提示.text = 数据.道纹.Count == 0 ? "藏匣尚空 · 可回主页开属性宝盒，或去青岚原收集道纹" : "此分类暂无道纹 · 在筛选中选择“显示全部”";
        上页按钮.interactable = 候选页码 > 0; 下页按钮.interactable = 候选页码 + 1 < 候选总页数;
        上页按钮.GetComponentInChildren<Text>().color = 上页按钮.interactable ? 文字色 : 天帝道纹美术.次文;
        下页按钮.GetComponentInChildren<Text>().color = 下页按钮.interactable ? 文字色 : 天帝道纹美术.次文;
    }
    void 建格标签(道纹实例 纹)
    {
        var 字 = 文字(画布.rectTransform, 天帝道纹美术.单字(纹), 0, 0, 60, 46, 24, TextAnchor.MiddleCenter);
        天帝道纹单字.绑定(字);
        var 区 = 字.rectTransform; 区.anchorMin = 区.anchorMax = 区.pivot = new Vector2(0.5f, 0.5f); 格标签[纹] = 字;
    }
    public void 刷新()
    {
        if (画布 == null) return;
        成长字.text = "等级 " + 数据.玩家等级 + "   ·   技能点 " + 数据.技能点 + "   ·   已解锁 " + 数据.已解锁格数 + " / " + (天帝道纹.边长 * 天帝道纹.边长);
        画布.SetVerticesDirty(); 更新标签();
        刷新显示索引();
        for (int i = 0; i < 数据.道纹.Count; i++)
        {
            var 纹 = 数据.道纹[i]; 候选图[i].候选暗 = 纹.格子.HasValue; 候选图[i].SetVerticesDirty();
            候选品阶[i].text = 天帝道纹品阶.彩色品阶文字(纹.品阶);
            候选短名[i].text = 天帝道纹美术.单字(纹); 候选短名[i].color = 天帝道纹美术.正文;
            候选说明[i].color = 文字色;
            // 装备状态由标记与图案色区分，文字不跟着整卡褪色。
            候选透明[i].alpha = 1;
            天帝道纹美术.应用(候选状态图[i], 纹.格子.HasValue ? "已装备" : "未装备", false);
            候选说明[i].text = 纹.候选说明; 候选说明[i].verticalOverflow = VerticalWrapMode.Truncate;
            if (纹.词条.Count > 1)
                候选说明[i].text = 天帝道纹属性.词条名称(纹.词条[0].属性).Replace("属性额外伤害", "伤害") + " " + 天帝道纹属性.数值文字(纹.词条[0].属性, 纹.词条[0].实际数值) + "  ·  共" + 纹.词条.Count + "条";
            候选状态[i].text = 纹.是特性道纹? (纹.特性状态.StartsWith("已激活")?"已激活":纹.生效?"已接通 · 查看条件":"固定机制 · 不可改造") : 纹.格子.HasValue ? "已放置 · 可拖回" : 纹.分类 == 道纹分类.分叉 ? "分叉 · 固定接口" : "词条 " + 纹.词条.Count + "/" + 纹.词条上限;
            候选状态[i].color = 纹.格子.HasValue ? new Color(0.38f, 0.46f, 0.46f) : new Color(0.61f, 0.69f, 0.68f);
        }
        汇总字.text = "接通 " + 数据.生效数 + " / " + 数据.道纹.Count + " 枚 · 开放通路 " + 数据.开放通路数 + " / 6";
        重建构筑预览();
        更新操作状态();
        页码字.text = (当前分类.HasValue ? 当前分类 + "道纹" : 当前分组.HasValue ? 当前分组 + "属性" : "全部") + " · " +
            (当前排序 == 道纹候选排序.默认 ? "默认顺序" : 当前排序 == 道纹候选排序.品阶 ? "品阶↓" : "属性分组") +
            (当前接口筛选 == 0 ? "" : " · 接口筛选") + " · 第 " + (候选页码 + 1) + " / " + 候选总页数 + " 页 · " + 筛选结果数 + " / " + 数据.道纹.Count + " 枚";
        if (天帝移动适配.启用) 页码字.text = (候选页码 + 1) + " / " + 候选总页数 + " · " + 筛选结果数 + "枚";
        提示字.text = 天帝移动适配.启用 ? (天帝青绿皮肤.已启用 ? "点选后点格放置 · 拖空白移动 · 双指缩放" : 触屏默认提示) : 展开操作说明 ? "锁格消耗1技能点 · 拖拽时 R/右键旋转 · 双击卸载 · Ctrl+Z撤销 · 青框为当前通路，金链为悬停路径，白点为共享" : "点锁格解锁，拖入道纹并连接源纹 · 悬停查看链路与旋转后变化";
        更新触屏操作();
        if (天帝青绿皮肤.已启用) 更新悬浮配色();
        刷新山水构筑();
    }
    public bool 切换候选页(int 页)
    {
        if (页 < 0 || 页 >= 候选总页数 || 拖动中 || 筛选已打开) return false;
        指针离开(); 候选页码 = 页; 刷新(); return true;
    }
    void 更新标签()
    {
        foreach (var 项 in 格标签)
        {
            var 纹 = 项.Key;
            var 字 = 项.Value; var 点 = 纹.格子.HasValue ? 画布.格位置(纹.格子.Value) * 画布.缩放 + 画布.平移 : Vector2.zero;
            字.gameObject.SetActive(画布.细节可见 && 纹.格子.HasValue && 画布.工作区.Contains(点)); 字.rectTransform.anchoredPosition = 点;
            字.fontSize = Mathf.RoundToInt(24 * 画布.缩放); 字.rectTransform.sizeDelta = new Vector2(60,Mathf.Max(46,字.fontSize*1.8f));
            字.text = 天帝道纹美术.单字(纹);
            字.color = 纹.生效 ? 天帝道纹美术.正文 : new Color(0.37f, 0.43f, 0.44f);
        }
    }
    public Vector2 格屏幕位置(Vector2Int 格)
    { return RectTransformUtility.WorldToScreenPoint(null, 画布.rectTransform.TransformPoint(画布.格位置(格) * 画布.缩放 + 画布.平移)); }
    public Vector2 候选屏幕位置(int 索引)
    { var 区 = (RectTransform)候选图[索引].transform.parent; return RectTransformUtility.WorldToScreenPoint(null, 区.TransformPoint(区.rect.center)); }
    public RectTransform 引导候选区域(道纹实例 纹)
    {
        int 索引 = 数据.道纹.IndexOf(纹);
        return 索引 >= 0 && 索引 < 候选卡.Count && 候选卡[索引].activeInHierarchy ? (RectTransform)候选卡[索引].transform : null;
    }
    public RectTransform 定位引导候选(道纹实例 纹)
    {
        int 索引 = 数据.道纹.IndexOf(纹); if (索引 < 0) return null;
        // 只调整列表导航，旧档中的道纹也能被教程找到；不修改道纹或库存。
        当前分类 = null; 当前分组 = null; 当前接口筛选 = 0; 关闭筛选(); 刷新();
        候选页码 = 显示索引.IndexOf(索引) / 每页数量; 刷新();
        return 引导候选区域(纹);
    }
    bool 画布命中(Vector2 屏幕, out Vector2Int 格)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, 屏幕, null, out var 点);
        格 = 画布.位置格((点 - 画布.平移) / 画布.缩放); return 画布.工作区.Contains(点);
    }
    bool 点在道纹(Vector2 屏幕, Vector2Int 格)
    {
        if (!画布.轻透画布) return true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, 屏幕, null, out var 点);
        var 中心 = 画布.格位置(格) * 画布.缩放 + 画布.平移;
        return Vector2.Distance(点, 中心) <= 天帝道纹.半径 * 1.1f * 画布.缩放;
    }
    public void 指针移动(Vector2 点) { 指针位置 = 点; 有指针 = true; 更新悬停(); }
    public void 指针离开() { 有指针 = false; if (浮窗 != null) 浮窗.gameObject.SetActive(false); if (!拖动中) 清理连接预览(); }
    public void 点击(bool 在画布, PointerEventData e)
    {
        if (筛选已打开 || 拖纹 != null || 平移中 || e.dragging) return;
        if (天帝移动适配.启用 && (!触屏点击可操作(e) || 在画布 && 触屏点画布(e))) return;
        if (!在画布 || !画布命中(e.position, out var 格))
        { 上次点击纹 = null; return; }
        if (!画布.细节可见) { 定位格子(格); 提示字.text = "已放大此处，现在可编辑格子"; return; }
        if (!数据.已放置.TryGetValue(格, out var 纹))
        {
            上次点击纹 = null;
            if (e.button == PointerEventData.InputButton.Left && 天帝道纹.在范围(格))
            {
                尝试解锁格子(格);
            }
            return;
        }
        if (!点在道纹(e.position, 格)) { 上次点击纹 = null; return; }
        if (纹.是源纹) { 上次点击纹 = null; return; }
        bool 已修改 = false;
        if (e.button == PointerEventData.InputButton.Right)
        { 上次点击纹 = null; 记录画布操作(() => 已修改 = 数据.旋转(纹), "DW03_旋转"); }
        else if (e.button == PointerEventData.InputButton.Left)
        {
            // 网格共用一个命中对象；须确认连续点击同一枚，避免点击邻格误卸载。
            if (e.clickCount >= 2 && 上次点击纹 == 纹)
            { 记录画布操作(() => 数据.收回(纹), "DW04_卸下"); 上次点击纹 = 按下纹 = null; 已修改 = true; }
            else 上次点击纹 = 纹;
        }
        if (已修改) 刷新();
        指针移动(e.position);
    }
    public void 按下(道纹实例 候选, bool 在画布, PointerEventData e)
    {
        if (拖动中 && e.button == PointerEventData.InputButton.Right) { 旋转拖动道纹(); return; }
        if (筛选已打开 || e.button != PointerEventData.InputButton.Left) return;
        指针移动(e.position); 按下纹 = null;
        if (在画布 && 画布命中(e.position, out var 格) && 点在道纹(e.position, 格)) 数据.已放置.TryGetValue(格, out 按下纹);
        else if (候选 != null && !候选.格子.HasValue) 按下纹 = 候选;
        if (按下纹 != null && 按下纹.是源纹) 按下纹 = null;
        if (天帝移动适配.启用 && 在画布) 按下纹 = null;
    }
    public void 开始拖动(bool 在画布, PointerEventData e)
    {
        if (筛选已打开 || e.button != PointerEventData.InputButton.Left) return;
        上次点击纹 = null;
        指针移动(e.position); 平移中 = 在画布 && 按下纹 == null;
        if (按下纹 != null)
        {
            拖纹 = 按下纹; 画布.拖动纹 = 拖纹; 拖影纹 = 创建拖影(拖纹); 拖影图.单纹 = 拖影纹; 拖影字.text = 天帝道纹美术.单字(拖纹);
            拖影.gameObject.SetActive(true); 拖影图.SetVerticesDirty(); 浮窗.gameObject.SetActive(false);
        }
        更新拖动(e.position);
        更新操作状态();
    }
    public void 拖动(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (平移中)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, e.position, null, out var 现);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, e.position - e.delta, null, out var 前);
            画布.平移 += 现 - 前; 限制平移(); 画布.SetVerticesDirty(); 更新标签();
        }
        指针移动(e.position); 更新拖动(e.position);
    }
    void 更新拖动(Vector2 屏幕)
    {
        if (拖纹 == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根, 屏幕, null, out var 点); 拖影.anchoredPosition = 点;
        if (画布命中(屏幕, out var 格))
        {
            画布.预览格 = 格; 画布.预览可放 = 数据.可放置(拖纹, 格);
            天帝道纹美术.应用(拖放状态图, 画布.预览可放 ? "可替换" : "不可替换", false);
            拖放状态图.color = 画布.预览可放 ? 天帝道纹美术.强调 : new Color(.82f,.50f,.43f);
            拖放状态图.enabled = true;
            更新构筑预览(拖纹, 画布.预览可放 ? 格 : (Vector2Int?)null, false, false);
            提示字.text = 画布.预览可放 ? "可以放置  ·  绿色只代表格子可用，连到源纹才会生效" :
                天帝道纹.在范围(格) && !数据.格已解锁(格) ? "不能放置  ·  此格锁定，请先点击消耗1技能点解锁" : "不能放置  ·  格子已占用、位于源纹或超出画布边界";
        }
        else
        {
            拖放状态图.enabled = false;
            画布.预览格 = null; 提示字.text = 天帝青绿皮肤.已启用 ? "拖回左侧藏匣可收回；松开到其它位置则返回原位" : "拖回下方候选区可收回；松开到其它位置则返回原位";
            更新构筑预览(拖纹, null, false, 拖纹.格子.HasValue && RectTransformUtility.RectangleContainsScreenPoint(候选区, 屏幕, null));
        }
        画布.SetVerticesDirty();
    }
    public void 结束拖动(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (拖纹 != null)
        {
            if (画布命中(e.position, out var 格) && 数据.可放置(拖纹, 格))
            {
                var 项 = 数据.当前布局(); 项.RemoveAll(x => x.编号 == 拖纹.编号);
                项.Add(new 道纹布局项 { 编号 = 拖纹.编号, 格子 = 格, 接口 = 拖影纹.接口, 入口方向 = 拖影纹.是顺序功能 ? 拖影纹.入口方向 : -1 });
                记录画布操作(() => 数据.应用布局(项, out _));
            }
            else if (!画布命中(e.position, out _) && RectTransformUtility.RectangleContainsScreenPoint(候选区, e.position, null))
                记录画布操作(() => { 拖纹.接口 = 拖影纹.接口; 拖纹.入口方向 = 拖影纹.入口方向; 数据.收回(拖纹); }, "DW04_卸下");
        }
        取消拖动();
    }
    public void 松开() { if (拖纹 == null && !平移中) 按下纹 = null; }
    public void 取消拖动()
    {
        上次点击纹 = null;
        拖纹 = 按下纹 = 拖影纹 = null; 平移中 = false;
        if (画布 == null) return;
        画布.拖动纹 = null; 画布.预览格 = null; 拖影.gameObject.SetActive(false); 浮窗.gameObject.SetActive(false); 刷新();
    }
    public void 缩放画布(PointerEventData e)
    {
        if (拖纹 != null || !画布命中(e.position, out _)) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, e.position, null, out var 点);
        float 原 = 画布.缩放, 新 = Mathf.Clamp(原 * Mathf.Pow(1.12f, e.scrollDelta.y), .08f, 1.65f);
        画布.平移 = 点 - (点 - 画布.平移) * (新 / 原); 画布.缩放 = 新;
        限制平移(); 画布.SetVerticesDirty(); 更新标签();
    }
    void 限制平移()
    {
        // 随画布边界限制平移，保留两格越界红色反馈空间。
        var 中心格 = 画布.位置格(-画布.平移 / 画布.缩放);
        var 限制格 = new Vector2Int(Mathf.Clamp(中心格.x, 天帝道纹.最小坐标 - 2, 天帝道纹.最大坐标 + 2), Mathf.Clamp(中心格.y, 天帝道纹.最小坐标 - 2, 天帝道纹.最大坐标 + 2));
        if (限制格 != 中心格) 画布.平移 = -画布.格位置(限制格) * 画布.缩放;
    }
    void LateUpdate()
    {
        if (解锁提示框 != null && 解锁提示框.gameObject.activeSelf)
        {
            if (Time.unscaledTime >= 解锁提示截止) 解锁提示框.gameObject.SetActive(false);
            else 解锁提示框.sizeDelta = new Vector2(Mathf.Min(620, Mathf.Max(1, 根.rect.width - 32)), 88);
        }
        更新操作快捷键();
        更新悬停();
        更新触屏操作();
    }
    void 更新悬停()
    {
        if (画布 == null || 浮窗 == null) return;
        if (天帝移动适配.启用) return;
        if (筛选已打开 || !有指针 || 拖纹 != null || 平移中) { 浮窗.gameObject.SetActive(false); return; }
        道纹实例 悬停 = null;
        if (画布命中(指针位置, out var 格) && 点在道纹(指针位置, 格)) 数据.已放置.TryGetValue(格, out 悬停);
        else for (int i = 0; i < 候选图.Count; i++) if (候选卡[i].activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)候选图[i].transform.parent, 指针位置, null)) { 悬停 = 数据.道纹[i]; break; }
        显示属性(悬停, 指针位置);
    }
    public void 显示属性(道纹实例 纹, Vector2 屏幕)
    {
        更新构筑预览(纹, null, !天帝移动适配.启用 && 纹 != null && !纹.是源纹 && 纹.格子.HasValue, false);
        if (引导讲解中) { 浮窗.gameObject.SetActive(false); return; }
        if (纹 == null) { 浮窗.gameObject.SetActive(false); return; }
        详情卡.设置(纹, 连接诊断 == null ? null : 归属说明(数据, 纹, 连接诊断));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根, 屏幕, null, out var 点);
        float 指针x = 点.x - 根.rect.xMin, 指针y = 根.rect.yMax - 点.y;
        var 画布边界 = RectTransformUtility.CalculateRelativeRectTransformBounds(根, 画布.rectTransform);
        float 右界 = 画布边界.max.x - 根.rect.xMin;
        float x = 指针x + 52;
        if (x + 天帝道纹详情卡.宽度 > 右界 - 8) x = 指针x - 天帝道纹详情卡.宽度 - 52;
        x = Mathf.Clamp(x, 8, Mathf.Max(8, 右界 - 天帝道纹详情卡.宽度 - 8));
        float 上 = 指针y + 40;
        if (上 + 详情卡.高度 > 根.rect.height - 8) 上 = 指针y - 详情卡.高度 - 40;
        上 = Mathf.Clamp(上, 8, 根.rect.height - 详情卡.高度 - 8);
        浮窗.anchoredPosition = new Vector2(x, -上); 浮窗.SetAsLastSibling(); 浮窗.gameObject.SetActive(true);
        if (天帝移动适配.启用)
        {
            float 宽 = 浮窗.rect.width;
            浮窗.anchoredPosition = new Vector2(Mathf.Max(8, 根.rect.width - 宽 - 8), -Mathf.Clamp(根.rect.height * .12f, 8, Mathf.Max(8, 根.rect.height - 详情卡.高度 - 8)));
        }
    }
    void OnApplicationFocus(bool 焦点) { if (!焦点) { 有指针 = false; 取消触屏选择(); 清理触屏手势(); } }
    void OnEnable() { if (数据 != null) { 数据.状态改变 -= 刷新; 数据.状态改变 += 刷新; } }
    internal 道纹解锁结果 尝试解锁格子(Vector2Int 格)
    {
        var 结果 = 道纹解锁结果.超出范围;
        记录画布操作(() => 结果 = 数据.尝试解锁格子(格), "DW01_解锁");
        switch (结果)
        {
            case 道纹解锁结果.成功:
                提示字.text = "格子已解锁  ·  消耗1技能点，现在可以放置道纹";
                break;
            case 道纹解锁结果.已解锁:
                提示字.text = "格子已解锁  ·  可放置道纹；连到源纹才会生效";
                break;
            case 道纹解锁结果.技能点不足:
                显示技能点不足提示();
                break;
            case 道纹解锁结果.缺少相邻解锁格:
                提示字.text = "只有已解锁格子旁边的才能解锁";
                break;
            default:
                提示字.text = "此格无法解锁";
                break;
        }
        画布.SetVerticesDirty();
        return 结果;
    }
    void 显示技能点不足提示()
    {
        提示字.text = "没有技能点";
        if (解锁提示框 == null)
        {
            解锁提示框 = 区块(根, "技能点不足提示", 0, 0, 620, 88);
            天帝响应布局.动态(解锁提示框);
            解锁提示框.anchorMin = 解锁提示框.anchorMax = 解锁提示框.pivot = new Vector2(.5f, .5f);
            解锁提示框.anchoredPosition = Vector2.zero;
            var 底色 = 解锁提示框.gameObject.AddComponent<Image>();
            底色.color = new Color(.12f, .09f, .06f, .97f); 底色.raycastTarget = false;
            var 边 = 解锁提示框.gameObject.AddComponent<Outline>(); 边.effectColor = 天帝道纹美术.金墨;
            var 组 = 解锁提示框.gameObject.AddComponent<CanvasGroup>(); 组.blocksRaycasts = 组.interactable = false;
            var 文 = 文字(解锁提示框, "没有技能点", 16, 8, 588, 72, 天帝移动适配.启用 ? 18 : 20, TextAnchor.MiddleCenter);
            天帝响应布局.动态(文.rectTransform);
            文.rectTransform.anchorMin = Vector2.zero; 文.rectTransform.anchorMax = Vector2.one;
            文.rectTransform.offsetMin = new Vector2(16, 8); 文.rectTransform.offsetMax = new Vector2(-16, -8);
            文.color = new Color(1, .94f, .78f);
        }
        解锁提示框.sizeDelta = new Vector2(Mathf.Min(620, Mathf.Max(1, 根.rect.width - 32)), 88);
        解锁提示框.SetAsLastSibling(); 解锁提示框.gameObject.SetActive(true);
        解锁提示截止 = Time.unscaledTime + 3;
        天帝声音.提示("UI04_拒绝");
    }
    void OnDisable() { if (数据 != null) 数据.状态改变 -= 刷新; if (画布 != null) 取消触屏选择(); 清理触屏手势(); if (解锁提示框 != null) 解锁提示框.gameObject.SetActive(false); }
    void OnDestroy() { if (数据 != null) 数据.状态改变 -= 刷新; }
}

public sealed class 天帝道纹输入 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler
{
    public 天帝道纹界面 页面;
    public 道纹实例 道纹;
    public bool 是画布;
    public void OnPointerDown(PointerEventData e) { if (!天帝移动适配.启用 || 页面.触屏按下(是画布, e)) 页面.按下(道纹, 是画布, e); }
    public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) 页面.松开(); if (天帝移动适配.启用) 页面.触屏抬起(e); }
    public void OnPointerClick(PointerEventData e)
    { if (天帝移动适配.启用 && !页面.触屏点击可操作(e)) return; if (天帝移动适配.启用 && !是画布 && 道纹 != null && !e.dragging) 页面.触屏选择(道纹, e.position); else 页面.点击(是画布, e); }
    ScrollRect 候选滚动 => 天帝移动适配.启用 && !是画布 ? GetComponentInParent<ScrollRect>() : null;
    public void OnInitializePotentialDrag(PointerEventData e) => 候选滚动?.OnInitializePotentialDrag(e);
    public void OnBeginDrag(PointerEventData e) { if (候选滚动 != null) { 候选滚动.OnBeginDrag(e); return; } if (!天帝移动适配.启用 || 页面.触屏指针可操作(e)) 页面.开始拖动(是画布, e); }
    public void OnDrag(PointerEventData e) { if (候选滚动 != null) { 候选滚动.OnDrag(e); return; } if (天帝移动适配.启用 && 页面.触屏手势移动(是画布, e)) return; if (!天帝移动适配.启用 || 页面.触屏指针可操作(e)) 页面.拖动(e); }
    public void OnEndDrag(PointerEventData e) { if (候选滚动 != null) { 候选滚动.OnEndDrag(e); return; } if (!天帝移动适配.启用 || 页面.触屏点击可操作(e)) 页面.结束拖动(e); }
    public void OnScroll(PointerEventData e)
    {
        if (!是画布) { GetComponentInParent<ScrollRect>()?.OnScroll(e); return; }
        if (!天帝移动适配.启用) 页面.缩放画布(e);
    }
    public void OnPointerMove(PointerEventData e) { if (!天帝移动适配.启用) 页面.指针移动(e.position); }
    public void OnPointerEnter(PointerEventData e) { if (!天帝移动适配.启用) 页面.指针移动(e.position); }
    public void OnPointerExit(PointerEventData e) { if (!天帝移动适配.启用) 页面.指针离开(); }
}
