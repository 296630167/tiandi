using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝通货界面 : MonoBehaviour
{
    天帝道纹 数据;
    天帝通货 通货;
    Font 字体;
    Action 关闭;
    RectTransform 面板, 洗练区;
    CanvasGroup 洗练显示;
    readonly Text[] 名称字 = new Text[11], 数量字 = new Text[11];
    readonly Button[] 通货键 = new Button[11], 词条键 = new Button[6];
    readonly Image[] 通货标记 = new Image[11];
    readonly Text[] 词条名称 = new Text[6], 接口字 = new Text[6];
    readonly Image[] 接口底 = new Image[6], 词条标记 = new Image[6];
    Text 说明字, 说明名, 目标字, 分类字, 容量字, 接口标题, 技能字, 词条字, 原因字, 结果字, 结果标题, 更换提示;
    readonly System.Collections.Generic.HashSet<int> 变化词条 = new System.Collections.Generic.HashSet<int>();
    Button 使用键, 目标按钮;
    Text 目标提示,目标图短名;
    天帝道纹绘图 目标图;
    天帝道纹背包 背包;
    道纹实例 目标;
    int 词条索引;
    static readonly Color 金 = 天帝道纹美术.金墨, 淡字 = 天帝道纹美术.次文;
    static readonly Color 卡底 = 天帝道纹美术.行底, 选底 = new Color(.92f, .84f, .59f);
    public 通货种类 当前通货 { get; private set; } = (通货种类)(-1);
    // 与面板从上到下、从左到右的顺序一致，问天石排在词条改造之前。
    static readonly 通货种类[] 材料顺序 = {
        通货种类.启灵石, 通货种类.点玄石, 通货种类.紫蕴石, 通货种类.无瑕玉,
        通货种类.凝华石, 通货种类.蕴玄石, 通货种类.琢天玉, 通货种类.问天石,
        通货种类.添蕴砂, 通货种类.易纹砂, 通货种类.重铸石
    };
    public 道纹实例 当前目标 => 数据 != null && 目标 != null && 数据.道纹.Contains(目标) ? 目标 : null;
    public bool 背包已打开 => 背包 != null && 背包.gameObject.activeSelf;
    public string 最近结果 => 结果字 != null ? 结果字.text : "";
    public bool 可执行 => 使用键 != null && 使用键.interactable;
    int 可用材料数 => 通货 == null ? 0 : Mathf.Min(天帝通货.可用种类.Count, 通货键.Length);

    public void 初始化(天帝道纹 数据, 天帝通货 通货, Font 字体, Action 关闭)
    {
        if (数据 == null || 通货 == null) return;
        this.数据 = 数据; this.通货 = 通货; this.字体 = 字体; this.关闭 = 关闭;
        var 根 = (RectTransform)transform;
        底(根, "遮罩", 0, 0, 1600, 900, new Color(0, 0, 0, .76f));
        面板 = 底(根, "通货改造面板", 160, 60, 1280, 780, new Color(.045f, .08f, .095f));
        字(面板, "道纹改造", 28, 20, 600, 44, 30).fontStyle = FontStyle.Bold;
        字(面板, "先选目标道纹，再选择材料，确认消耗后进行改造。", 28, 64, 780, 28, 16).color = 淡字;
        键(面板, "关闭通货", "关闭", 1100, 26, 148, 56, () => this.关闭());
        字(面板, "2 · 选择材料", 28, 101, 360, 26, 17).color = 淡字;
        字(面板, "1 · 选择目标道纹", 672, 101, 360, 26, 17).color = 天帝道纹美术.强调;
        var 工具区 = 底(面板, "通货工具面板", 28, 134, 588, 624, Color.white);
        天帝界面美术.面板(工具区.GetComponent<Image>(), "属性面板", Color.white);
        var 工具标题 = 字(工具区, "通货", 24, 12, 130, 38, 23); 工具标题.fontStyle = FontStyle.Bold;
        var 消耗提示 = 字(工具区, "每次1个 · 条件不符不消耗", 180, 20, 384, 26, 15);
        消耗提示.color = 淡字; 消耗提示.alignment = TextAnchor.MiddleRight;
        字(工具区, "升阶与赌阶", 24, 56, 300, 26, 17).color = 金;
        字(工具区, "词条改造", 24, 328, 300, 26, 17).color = 金;
        for (int i = 0; i < 可用材料数; i++)
        {
            var 种类 = 天帝通货.可用种类[i];
            bool 词条改造 = 种类 == 通货种类.添蕴砂 || 种类 == 通货种类.易纹砂 || 种类 == 通货种类.重铸石;
            int 槽 = 词条改造 ? i - 7 : 种类 == 通货种类.问天石 ? 7 : i;
            float x = 24 + 槽 % 2 * 278, y = (词条改造 ? 360 : 88) + 槽 / 2 * 60;
            var 定义 = 天帝通货.定义[(int)种类];
            通货键[i] = 键(工具区, "通货-" + 定义.名称, "", x, y, 262, 54, () => 选通货(种类));
            通货键[i].gameObject.AddComponent<CanvasGroup>();
            var 卡 = (RectTransform)通货键[i].transform;
            通货标记[i] = 底(卡, "材料选中标记", 0, 4, 3, 46, 天帝道纹美术.强调).GetComponent<Image>();
            通货标记[i].raycastTarget = false;
            var 图标 = 区块(卡, "图标", 10, 10, 34, 34).gameObject.AddComponent<RawImage>();
            图标.texture = 天帝美术资源.当前 != null ? 天帝美术资源.当前.获取("CU" + ((int)种类 + 1).ToString("00"))?.texture : Resources.Load<Texture2D>("通货图标/" + 定义.名称); 图标.raycastTarget = false;
            名称字[i] = 字(卡, 定义.名称, 56, 3, 140, 28, 17); 名称字[i].font=字体;名称字[i].fontStyle = FontStyle.Bold;
            名称字[i].alignment=TextAnchor.MiddleLeft;
            名称字[i].resizeTextForBestFit=true;名称字[i].resizeTextMinSize=14;名称字[i].resizeTextMaxSize=17;
            var 用途 = 字(卡, 通货用途(种类), 56, 25, 194, 24, 13); 用途.color = 淡字;
            用途.font=字体;用途.alignment=TextAnchor.MiddleLeft;
            用途.resizeTextForBestFit=true;用途.resizeTextMinSize=11;用途.resizeTextMaxSize=13;
            数量字[i] = 字(卡, "", 198, 7, 52, 22, 16); 数量字[i].alignment = TextAnchor.MiddleRight;
            数量字[i].resizeTextForBestFit=true;数量字[i].resizeTextMinSize=12;数量字[i].resizeTextMaxSize=16;
            数量字[i].gameObject.name = "数量";
        }
        var 右区 = 底(面板, "道纹详情", 648, 134, 604, 438, new Color(.065f, .115f, .13f));
        目标按钮 = 键(面板, "选择目标道纹", "点击这里，选择道纹", 672, 144, 556, 88, 打开背包);
        目标提示 = 目标按钮.GetComponentInChildren<Text>();
        目标提示.rectTransform.anchoredPosition = new Vector2(110, -14); 目标提示.rectTransform.sizeDelta = new Vector2(426, 56); 目标提示.fontSize = 23;
        目标提示.fontStyle = FontStyle.Bold; 目标提示.alignment = TextAnchor.MiddleCenter;
        var 图区 = 区块((RectTransform)目标按钮.transform, "目标道纹图标", 2, 2, 84, 76);
        目标图 = 图区.gameObject.AddComponent<天帝道纹绘图>(); 目标图.单纹模式 = true; 目标图.单纹半径 = 32; 目标图.raycastTarget = false;
        目标图.数据=数据;
        目标图短名=字(图区,"",8,0,68,76,24);目标图短名.alignment=TextAnchor.MiddleCenter;目标图短名.fontStyle=FontStyle.Bold;
        天帝道纹单字.绑定(目标图短名, 图区);
        目标字 = 字(面板, "", 782, 146, 300, 48, 27); 目标字.fontStyle = FontStyle.Bold;
        更换提示 = 字(面板, "更换目标 ›", 1100, 150, 128, 38, 16); 更换提示.color = 天帝道纹美术.强调;
        分类字 = 字(面板, "", 782, 200, 446, 30, 16);
        底(右区, "分隔", 24, 111, 556, 1, new Color(.19f, .26f, .27f));
        字(面板, "词条", 672, 251, 240, 25, 16).color = 淡字;
        容量字 = 字(面板, "", 982, 251, 244, 25, 16); 容量字.alignment = TextAnchor.MiddleRight; 容量字.color = 淡字;
        for (int i = 0; i < 6; i++)
        {
            int 编号 = i;
            词条键[i] = 键(面板, "词条-" + i, "", 672, 281 + i * 30, 556, 28, () => 选择词条(编号));
            var 行 = (RectTransform)词条键[i].transform;
            词条标记[i] = 底(行, "选中标记", 0, 0, 3, 28, 金).GetComponent<Image>(); 词条标记[i].raycastTarget = false;
            词条名称[i] = 字(行, "", 14, 0, 524, 28, 17);
        }
        技能字 = 字(面板, "", 686, 294, 526, 108, 18); 技能字.color = 淡字;
        接口标题 = 字(面板, "", 672, 470, 554, 25, 16); 接口标题.color = 淡字;
        for (int d = 0; d < 6; d++)
        {
            接口底[d] = 底(面板, "接口-" + d, 672 + d * 94, 502, 86, 29, 卡底).GetComponent<Image>();
            接口字[d] = 字(接口底[d].rectTransform, 天帝道纹.方向名[d], 0, 0, 86, 29, 15);
            接口字[d].alignment = TextAnchor.MiddleCenter;
        }
        洗练区 = 区块(面板, "洗练选择", 672, 540, 556, 38);
        洗练显示 = 洗练区.gameObject.AddComponent<CanvasGroup>();
        键(洗练区, "上一词条", "‹", 0, 0, 38, 38, () => 切换词条(-1));
        词条字 = 字(洗练区, "", 52, 0, 452, 38, 16); 词条字.color = 金;
        键(洗练区, "下一词条", "›", 518, 0, 38, 38, () => 切换词条(1));
        字(面板, "3 · 下一次改造", 672, 579, 556, 26, 17).color = 淡字;
        使用键 = 键(面板, "使用通货", "", 672, 610, 260, 52, 执行);
        使用键.GetComponentInChildren<Text>().fontSize = 21;
        原因字 = 字(面板, "", 950, 610, 280, 52, 16);
        var 结果框 = 底(面板, "本次改造结果面板", 648, 680, 604, 78, Color.white);
        结果标题 = 字(结果框, "本次结果", 24, 6, 556, 29, 18); 结果标题.fontStyle = FontStyle.Bold;
        结果字 = 字(结果框, "改造后在这里查看结果与消耗。", 24, 37, 556, 35, 16); 结果字.alignment = TextAnchor.UpperLeft; 结果字.color = 淡字;
        var 分割 = 底(工具区, "材料说明分隔", 24, 486, 540, 2, Color.white).GetComponent<Image>();
        天帝界面美术.面板(分割, "分割线", Color.white); 分割.raycastTarget = false;
        说明名 = 字(工具区, "", 24, 498, 540, 32, 22); 说明名.color = 天帝道纹美术.正文; 说明名.fontStyle = FontStyle.Bold;
        说明字 = 字(工具区, "", 24, 534, 540, 80, 17); 说明字.color = 天帝道纹美术.次文;
        if (天帝移动适配.启用)
        {
            天帝响应布局.比例(面板, .02f, .02f, .96f, .96f);
            var 左口 = 天帝响应布局.滚动正文(工具区);
            var 右口 = 天帝响应布局.滚动列(面板, "改造目标与操作", 648, 134, 604, 624);
            var 重排右 = 天帝双端页面布局.重排正文(右口.GetComponent<ScrollRect>().content);
            天帝双端页面布局.移动页(面板, 框 =>
            {
                天帝双端页面布局.页头(框, "关闭通货");
                float 宽 = 框.rect.width, 高 = 框.rect.height;
                天帝双端页面布局.固定(左口, 8, 天帝双端页面布局.页头高度, 宽 * .46f - 12, 高 - 76);
                天帝双端页面布局.固定(右口, 宽 * .46f + 4, 天帝双端页面布局.页头高度, 宽 * .54f - 12, 高 - 76);
                重排右();
                float 材宽 = 左口.rect.width - 16;
                工具区.sizeDelta = new Vector2(0, 1080);
                工具标题.gameObject.SetActive(false); 消耗提示.gameObject.SetActive(false);
                foreach (Transform 子 in 工具区)
                    if (子.GetComponent<Text>() != null && 子 != 说明名.transform && 子 != 说明字.transform) 子.gameObject.SetActive(false);
                for (int i = 0; i < 通货键.Length; i++)
                {
                    var 卡 = (RectTransform)通货键[i].transform;
                    天帝双端页面布局.固定(卡, 8, 8 + i * 74, 材宽, 68);
                    天帝双端页面布局.区域(卡, "图标", 10, 14, 34, 38);
                    天帝双端页面布局.固定(名称字[i].rectTransform, 54, 8, 材宽 - 116, 24); 名称字[i].fontSize = 16;
                    天帝双端页面布局.固定(数量字[i].rectTransform, 材宽 - 56, 8, 44, 24); 数量字[i].fontSize = 15;
                    foreach (var 文 in 卡.GetComponentsInChildren<Text>())
                        if (文 != 名称字[i] && 文 != 数量字[i] && !string.IsNullOrEmpty(文.text))
                        { 天帝双端页面布局.固定(文.rectTransform, 54, 34, 材宽 - 66, 26); 文.fontSize = 14; }
                }
                分割.gameObject.SetActive(false);
                天帝双端页面布局.固定(说明名.rectTransform, 8, 830, 材宽, 44);
                天帝双端页面布局.固定(说明字.rectTransform, 8, 878, 材宽, 194);
                // 选择说明进入各自正文，页头只保留标题和返回。
                foreach (Transform 子 in 框) if (子.GetComponent<Text>() is Text 文 && 文.text != "道纹改造") 文.gameObject.SetActive(false);
            });
        }
        天帝剪纸界面皮肤.装配(根,"改造",面板);
        装配首两页山水();
        通货.数量改变 += 刷新; 数据.状态改变 += 刷新; 刷新();
    }
    public void 选通货(通货种类 种类)
    {
        if ((int)种类 < 0 || (int)种类 >= 天帝通货.定义.Count ||
            种类 == 通货种类.通脉针 || 种类 == 通货种类.六通玉) return;
        if (!通货可选(种类)) return;
        当前通货 = 种类; 刷新();
    }
    public void 选目标(int 索引)
    {
        if (数据 == null || 索引 < 0 || 数据.道纹 == null || 索引 >= 数据.道纹.Count) return;
        目标 = 数据.道纹[索引]; 词条索引 = 0; 当前通货 = (通货种类)(-1); 变化词条.Clear();
        结果标题.text = "本次结果"; 结果标题.color = 天帝道纹美术.正文; 结果字.text = "改造后在这里查看结果与消耗。"; 结果字.color = 淡字; 刷新();
    }
    public void 打开背包()
    {
        if (背包已打开) return;
        if (背包 != null)
        {
            背包.gameObject.SetActive(true);
            背包.更新选择(当前目标);
            return;
        }
        var 区 = 区块((RectTransform)transform,"改造道纹背包",0,0,1600,900);
        背包 = 区.gameObject.AddComponent<天帝道纹背包>();
        背包.初始化(数据,字体,当前目标,纹 => { int i = 数据.道纹.IndexOf(纹); if (i >= 0) 选目标(i); 关闭背包(); },关闭背包);
    }
    public void 关闭背包()
    {
        if (背包 == null) return;
        // 本次改造中复用选择器，保留筛选；离开改造页时随父页面销毁。
        背包.gameObject.SetActive(false);
    }
    void 选择词条(int 编号)
    {
        if (当前通货 != 通货种类.易纹砂 || 编号 < 0 || 编号 >= (当前目标?.词条.Count ?? 0)) return;
        词条索引 = 编号; 刷新();
    }
    void 切换词条(int 方向)
    {
        int 数 = 当前目标?.词条.Count ?? 0;
        if (数 > 0) 词条索引 = (词条索引 + 方向 + 数) % 数;
        刷新();
    }
    public void 执行()
    {
        if (通货 == null) return;
        var 材料 = 当前通货; var 纹 = 当前目标;
        var 之前 = new string[6];
        if (纹 != null) for (int i = 0; i < 纹.词条.Count && i < 6; i++) 之前[i] = 纹.词条[i].属性 + ":" + 纹.词条[i].实际数值;
        bool 成功 = 通货.使用(材料, 纹, 词条索引, out var 结果);
        天帝声音.提示(成功 ? "YS04_改造成功" : "UI04_拒绝");
        变化词条.Clear();
        if (成功 && 纹 != null) for (int i = 0; i < 纹.词条.Count && i < 6; i++) if (之前[i] != 纹.词条[i].属性 + ":" + 纹.词条[i].实际数值) 变化词条.Add(i);
        string 名称 = (int)材料 >= 0 && (int)材料 < 天帝通货.定义.Count ? 天帝通货.定义[(int)材料].名称 : "改造";
        结果标题.text = (成功 ? "已完成 · " : "未完成 · ") + 名称 + (成功 ? 通货.无限通货 ? " · 无限未扣费" : " · 消耗1个" : " · 未消耗");
        结果标题.color = 结果字.color = 成功 ? 天帝道纹美术.成功色 : 天帝道纹美术.警示色;
        结果字.text = 结果; 刷新();
    }
    static string 简写数量(int 数) => 数 >= 100000000 ? (数 / 100000000f).ToString("0.#") + "亿" : 数 >= 10000 ? (数 / 10000f).ToString("0.#") + "万" : 数.ToString();
    static string 通货用途(通货种类 种类)
    {
        var 定义 = 天帝通货.定义[(int)种类];
        if (定义.目标.HasValue) return 定义.来源 + " → " + 定义.目标;
        switch (种类)
        {
            case 通货种类.添蕴砂: return "增加一条词条";
            case 通货种类.易纹砂: return "重抽所选词条";
            case 通货种类.重铸石: return "重抽全部词条";
            case 通货种类.问天石: return "普通道纹随机升阶";
            default: return "";
        }
    }
    bool 通货可选(通货种类 种类) => 通货 != null && 通货.可使用(种类, 当前目标, 词条索引, out _);
    public void 刷新()
    {
        if (数据 == null || 通货 == null) return;
        var 纹 = 当前目标;
        int 词数 = 纹?.词条.Count ?? 0;
        词条索引 = Mathf.Clamp(词条索引, 0, Mathf.Max(0, 词数 - 1));
        if (!通货可选(当前通货))
        {
            当前通货 = (通货种类)(-1);
            foreach (var 种类 in 材料顺序) if (通货可选(种类)) { 当前通货 = 种类; break; }
        }
        for (int i = 0; i < 可用材料数; i++)
        {
            var 种类 = 天帝通货.可用种类[i];
            bool 可选 = 通货可选(种类);
            int 数 = 通货.数量(种类); bool 选中 = 可选 && 种类 == 当前通货;
            通货键[i].interactable = 可选;
            // 禁用态只降低整张材料卡的透明度，保留原有纸面颜色和图标可读性。
            通货键[i].GetComponent<CanvasGroup>().alpha = 可选 ? 1f : .5f;
            var 按钮色 = 通货键[i].colors; 按钮色.disabledColor = Color.white; 通货键[i].colors = 按钮色;
            数量字[i].text = "×" + (通货.无限通货 ? 通货.数量显示(种类) : 简写数量(数));
            数量字[i].color = 数 == 0 ? 淡字 : 天帝道纹美术.强调;
            名称字[i].color = 选中 ? 天帝道纹美术.强调 : 可选 ? 天帝道纹美术.正文 : 淡字;
            var 行图 = 通货键[i].GetComponent<Image>();
            天帝道纹美术.应用(行图,"小信息框");
            if(天帝剪纸界面皮肤.已启用)行图.pixelsPerUnitMultiplier=3;
            行图.color = 选中 ? new Color(.78f, .90f, .86f) : new Color(1f, .99f, .95f, .70f);
            行图.raycastTarget = true; 通货标记[i].enabled = 选中;
        }
        var 定 = (int)当前通货 >= 0 ? 天帝通货.定义[(int)当前通货] : null;
        目标字.text = 纹 == null ? "" : 纹.名称;
        更换提示.gameObject.SetActive(纹 != null);
        天帝界面美术.选项(目标按钮.GetComponent<Image>(), 纹 == null);
        目标图.单纹 = 纹; 目标图.SetVerticesDirty();
        目标图短名.text=天帝道纹美术.单字(纹);
        目标提示.gameObject.SetActive(纹 == null);
        分类字.text = 纹 == null ? "尚未选择道纹  ·  点击上方按钮打开藏匣" : 天帝道纹品阶.彩色品阶文字(纹.品阶) + "    物品" + 纹.物品等级 + "级    " + (纹.格子.HasValue ? "已放置" : "未放置") + "    #" + 纹.编号;
        分类字.color = 纹 == null ? 天帝道纹美术.金墨 : 淡字;
        if (背包已打开) 背包.更新选择(纹);
        容量字.text = 纹 == null ? "" : 纹.分类 == 道纹分类.分叉 || 纹.是特性道纹 ? "不可改造" : 词数 + " / " + 纹.词条上限;
        bool 单洗 = 当前通货 == 通货种类.易纹砂 && 词数 > 0;
        for (int i = 0; i < 6; i++)
        {
            bool 有词 = i < 词数;
            bool 显示 = 纹 != null && 纹.可改造词条 && i < 纹.词条上限;
            词条键[i].gameObject.SetActive(显示);
            if (!显示) continue;
            bool 选中 = 有词 && 单洗 && i == 词条索引;
            var 行底图 = 词条键[i].GetComponent<Image>();
            if (天帝道纹美术.彩绘皮肤)
            {
                天帝道纹美术.应用(行底图,"小信息框");
                行底图.color = 选中 ? new Color(.95f,.87f,.64f) : Color.white; 行底图.raycastTarget = true;
            }
            else 天帝道纹美术.选中(行底图,选中);
            词条键[i].interactable = 有词 && 单洗;
            var 色 = 词条键[i].colors; 色.disabledColor = Color.white; 词条键[i].colors = 色;
            词条标记[i].enabled = 选中;
            词条名称[i].text = (i + 1).ToString("00") + "    " + (纹.是顺序功能 ? 纹.功能 + " · 按链路执行" : 有词 ? 天帝道纹属性.词条名称(纹.词条[i].属性) + " " + 天帝道纹属性.数值文字(纹.词条[i].属性, 纹.词条[i].实际数值) + (变化词条.Contains(i) ? "  · 已更新" : "") : "空词条位 · 可改造");
            词条名称[i].color = 有词 ? 变化词条.Contains(i) ? 天帝道纹美术.成功色 : 天帝道纹美术.正文 : 淡字;
        }
        技能字.gameObject.SetActive(纹 == null || 纹.分类 == 道纹分类.分叉 || 纹.是特性道纹);
        技能字.text = 纹 == null ? "尚未选择道纹" : 纹.是特性道纹 ? "固定机制与接口，不能使用通货改造。" : "分叉道纹不提供属性词条，不能进行改造。";
        int 口数 = 0; for (int d = 0; d < 6; d++) if (纹 != null && 纹.有接口(d)) 口数++;
        接口标题.text = "固定接口  " + 口数 + " / 6 · 对应连接";
        for (int d = 0; d < 6; d++)
        {
            bool 开 = 纹 != null && 纹.有接口(d);
            接口底[d].color = 开 ? new Color(.76f,.87f,.81f) : 天帝道纹美术.行底;
            接口字[d].color = 开 ? 天帝道纹美术.成功色 : 淡字;
            接口字[d].text = 天帝道纹.方向名[d];
        }
        洗练显示.alpha = 单洗 ? 1 : 0;
        洗练显示.interactable = 洗练显示.blocksRaycasts = 单洗;
        词条字.text = 单洗 ? "洗练第 " + (词条索引 + 1) + " 条 · " + 纹.词条[词条索引].属性 + "（可直接点击词条）" : "";
        bool 可用 = 通货.可使用(当前通货, 纹, 词条索引, out var 原因);
        使用键.interactable = 可用;
        天帝界面美术.按钮(使用键,可用);
        使用键.GetComponentInChildren<Text>().text = 定 == null ? 纹 == null ? "先选择目标道纹" : "没有可用材料" : "使用" + 定.名称 + (通货.无限通货 ? " · 无限" : " · 消耗1个");
        原因字.text = 可用 ? 纹.是功能道纹 ? "重抽功能 · 接口按数量调整" : "可以改造" : 纹 == null ? "请先选择目标道纹" : !纹.可改造词条 ? "此道纹不能改造" : "没有符合条件且数量足够的通货";
        原因字.color = 使用键.interactable ? 天帝道纹美术.成功色 : 天帝道纹美术.警示色;
        说明名.text = 定?.名称 ?? (纹 == null ? "先选择目标" : "材料不可用");
        string 限制 = 纹 != null && 纹.是功能道纹 ? "固定稀有、单功能；符合新功能接口范围则保方向，否则随机增减接口。" : 定 != null && (定.目标.HasValue || 当前通货 == 通货种类.问天石)
            ? "保留现有词条和接口，仅补足最低词条。"
            : "属性与功能道纹可改造；分叉、特性与转化道纹不可改造。";
        说明字.text = 定 == null ? (纹 == null ? "先从背包选择道纹，自动选中第一个可用材料。" : "数量为0或不符合当前道纹条件的材料已锁定。") : 定.说明 + "\n" + 限制;
        刷新山水改造状态();
    }
    RectTransform 底(RectTransform 父, string 名, float x, float y, float w, float h, Color 色)
    {
        var 区 = 区块(父, 名, x, y, w, h); var 图 = 区.gameObject.AddComponent<Image>(); 图.color = 色; 图.raycastTarget = true;
        天帝界面美术.自动面板(图,名,w,h,色);return 区;
    }
    RectTransform 区块(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    Text 字(RectTransform 父, string 文, float x, float y, float w, float h, int 大小)
    {
        var 字 = 区块(父, "文字", x, y, w, h).gameObject.AddComponent<Text>(); 字.font = 字体; 字.text = 文; 字.fontSize = 大小;
        字.color = 天帝道纹美术.正文; 字.raycastTarget = false; 字.verticalOverflow = VerticalWrapMode.Truncate; 天帝界面美术.文字(字); return 字;
    }
    Button 键(RectTransform 父, string 名, string 文, float x, float y, float w, float h, Action 点击)
    {
        var 区 = 底(父, 名, x, y, w, h, 卡底); var 按钮 = 区.gameObject.AddComponent<Button>();
        按钮.targetGraphic = 区.GetComponent<Image>(); 按钮.onClick.AddListener(() => 点击());
        if (名.StartsWith("通货-"))
        {
            天帝道纹美术.应用((Image)按钮.targetGraphic,"小信息框"); 按钮.targetGraphic.raycastTarget = true;
            var 色 = 按钮.colors; 色.normalColor = 色.selectedColor = Color.white;
            色.highlightedColor = new Color(.93f, .98f, .97f); 色.pressedColor = new Color(.77f, .88f, .84f); 按钮.colors = 色;
            return 按钮;
        }
        天帝界面美术.按钮(按钮);
        if (名.StartsWith("词条-")) return 按钮;
        var 文本 = 字(区, 文, 7, 4, w - 14, h - 8, 18); 文本.alignment = TextAnchor.MiddleCenter; 天帝界面美术.按钮(按钮); return 按钮;
    }
    void OnDestroy()
    { if (通货 != null) 通货.数量改变 -= 刷新; if (数据 != null) 数据.状态改变 -= 刷新; }
}
