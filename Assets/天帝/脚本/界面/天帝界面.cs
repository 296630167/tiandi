using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class 天帝界面
{
    readonly 天帝游戏 游戏;
    readonly GameObject 根;
    readonly Canvas 界面画布;
    readonly CanvasScaler 画布缩放;
    readonly 天帝响应布局 响应布局;
    bool 适配待刷新 = true, 上次移动平台;
    int 上次布局修订 = -1;
    Vector2 上次布局尺寸;
    public int 适配排版次数 { get; private set; }
    readonly RectTransform 安全区;
    readonly RectTransform 设计区;
    readonly RectTransform[] 留边 = new RectTransform[4];
    readonly RectTransform 页面;
    readonly RectTransform 弹层;
    readonly Image 页面背景;
    readonly Text 存档状态字;
    Text 战斗坐标;
    Text 战斗目标;
    string 移动目标上次文本;
    float 移动目标显示截止;
    Text 战斗波次;
    Text 战斗血量, 战斗配置字;
    Text 主页实力字, 主页构筑字, 主页灵石字;
    Text 掉落提示;
    天帝道纹掉落 拾取事件源;
    天帝通货掉落 通货事件源;
    天帝灵石掉落 灵石事件源;
    天帝拾取提示 拾取列表;
    Image 战斗血条;
    readonly List<飘字> 伤害字池 = new List<飘字>();
    readonly System.Text.StringBuilder 伤害文案缓冲 = new System.Text.StringBuilder(256);
    int 伤害飘字序号;
    sealed class 飘字 { public Text 字; public CanvasGroup 组; public Outline 描边; public Vector2 位置; public float 剩余, 横移, 起高, 弹幅; }
    Button 战斗离开;
    天帝地图预览 战斗小地图;
    public bool 地图已打开 { get; private set; }
    public bool 角色已打开 { get; private set; }
    public 天帝角色界面 角色页 { get; private set; }
    public bool 图鉴已打开 { get; private set; }
    public 天帝道纹图鉴 图鉴页 { get; private set; }
    public bool 宝盒已打开 { get; private set; }
    sealed class 宝盒记录 { public 宝盒种类 种类; public 道纹实例 道纹; }
    readonly List<宝盒记录> 宝盒日志 = new List<宝盒记录>();
    天帝宝盒 日志所属宝盒;
    static 道纹实例 道纹快照(道纹实例 原)
    {
        var 纹 = new 道纹实例 { 编号 = 原.编号, 分类 = 原.分类, 品阶 = 原.品阶,
            接口 = 原.接口, 功能 = 原.功能, 特性编号 = 原.特性编号, 入口方向 = 原.入口方向,
            特殊效果 = 原.特殊效果, 介绍 = 原.介绍, 物品等级 = 原.物品等级 };
        foreach (var 词 in 原.词条) 纹.词条.Add(词.副本());
        return 纹;
    }
    public 天帝移动摇杆 战斗摇杆 { get; private set; }
    public bool 触控跑步 { get; private set; }
    Rect 上次安全区;
    Vector2Int 上次尺寸;
    Text 序章标题;
    Text 序章正文;
    RectTransform 序章字卡;
    天帝序章进度条 序章进度;
    Text 序章时间;
    Text 序章进度提示;
    Text 序章暂停字;
    bool 序章有字幕;
    RectTransform 序章全屏媒体;
    RectTransform 主页人物;
    Image 主页人物图;
    float 主页待机秒;
    readonly Color 墨 = new Color(0.13f, 0.23f, 0.22f);
    readonly Color 次墨 = new Color(0.35f, 0.43f, 0.39f);
    readonly Color 朱 = new Color(0.60f, 0.25f, 0.20f);
    readonly Color 纸 = new Color(0.94f, 0.92f, 0.86f);
    public bool 设置已打开 { get; private set; }
    public bool 确认已打开 { get; private set; }
    public 天帝道纹界面 道纹页 { get; private set; }
    public 天帝通货界面 改造页 { get; private set; }
    public 天帝源道纹选择 源道纹页 { get; private set; }

    public 天帝界面(天帝游戏 游戏)
    {
        this.游戏 = 游戏;
        根 = new GameObject("天帝界面", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        根.transform.SetParent(游戏.transform, false);
        界面画布 = 根.GetComponent<Canvas>(); 画布缩放 = 根.GetComponent<CanvasScaler>();
        根.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var 适配 = 根.GetComponent<CanvasScaler>();
        适配.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        适配.referenceResolution = 天帝移动适配.固定分辨率;
        适配.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        // 适配到整数像素时，文字和纸材边线不再落在半像素上；非整数比例由
        // CanvasScaler 正常插值，避免改变 16:9 设计区的比例。
        界面画布.pixelPerfect = true;
        响应布局 = 根.AddComponent<天帝响应布局>();
        for (int i = 0; i < 留边.Length; i++)
        {
            留边[i] = 铺满((RectTransform)根.transform, "1080p留边" + i);
            var 黑 = 留边[i].gameObject.AddComponent<Image>(); 黑.color = Color.black; 黑.raycastTarget = false;
        }
        安全区 = 铺满((RectTransform)根.transform, "安全区");
        安全区.gameObject.AddComponent<RectMask2D>();
        var 底 = 铺满(安全区, "背景").gameObject.AddComponent<Image>();
        底.sprite = 天帝青绿皮肤.获取("BG01") ?? 游戏.主页背景; 底.color = 底.sprite != null ? Color.white : 纸; 底.raycastTarget = false;
        var 背景比例 = 底.gameObject.AddComponent<AspectRatioFitter>(); 背景比例.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; 背景比例.aspectRatio = 16f / 9;
        页面背景 = 底;
        设计区 = 铺满(安全区, "设计区"); 天帝响应布局.登记(设计区, new Vector2(1600, 900));
        设计区.GetComponent<天帝比例矩形>().待提交 = false;
        页面 = 铺满(设计区, "页面"); 弹层 = 铺满(设计区, "设置层");
        存档状态字 = 字(设计区, "", 40, 846, 1100, 44, 21, 朱); 存档状态字.alignment = TextAnchor.MiddleLeft;
        存档状态字.gameObject.SetActive(false);
        更新适配();
    }
    RectTransform 铺满(RectTransform 父, string 名)
    {
        var 区 = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        区.SetParent(父, false); 区.anchorMin = Vector2.zero; 区.anchorMax = Vector2.one; 区.offsetMin = 区.offsetMax = Vector2.zero;
        return 区;
    }
    RectTransform 区块(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        return 天帝响应布局.创建(父, 名, x, y, w, h);
    }
    Image 图(RectTransform 父, string 名, float x, float y, float w, float h, Color 色, Sprite 素材 = null)
    {
        var 区 = 区块(父, 名, x, y, w, h); var 像 = 区.gameObject.AddComponent<Image>();
        像.color = 色; 像.sprite = 素材; 像.raycastTarget = false;
        if (素材 == null) 天帝界面美术.自动面板(像,名,w,h,色,名=="确认面板"||名=="设置面板");
        return 像;
    }
    Text 字(RectTransform 父, string 内容, float x, float y, float w, float h, int 大小, Color 色)
    {
        var 文 = 区块(父, "文字", x, y, w, h).gameObject.AddComponent<Text>();
        文.font = 游戏.默认字体; 文.text = 内容; 文.fontSize = 大小; 文.color = 天帝道纹美术.纸面文字(色);
        文.alignment = TextAnchor.MiddleCenter; 文.raycastTarget = false;
        文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Overflow; 天帝界面美术.文字(文); return 文;
    }
    Button 按钮(RectTransform 父, string 名, float x, float y, float w, float h, Action 点击, bool 主 = false)
    {
        var 像 = 图(父, 名, x, y, w, h, 主 ? 朱 : new Color(0.88f, 0.89f, 0.83f)); 像.raycastTarget = true;
        var 键 = 像.gameObject.AddComponent<Button>(); 键.targetGraphic = 像;
        var 色 = 键.colors; 色.highlightedColor = new Color(1.1f, 1.1f, 1.05f); 色.pressedColor = new Color(0.8f, 0.85f, 0.8f); 键.colors = 色;
        字(像.rectTransform, 名, 8, 0, w - 16, h, 主 ? 29 : 23, 主 ? 纸 : 墨);
        键.onClick.AddListener(() => 点击?.Invoke());天帝界面美术.按钮(键,主);return 键;
    }
    void 清空(RectTransform 层)
    {
        适配待刷新 = true;
        for (int i = 层.childCount - 1; i >= 0; i--)
        { var 物 = 层.GetChild(i).gameObject; 物.SetActive(false); 删除界面对象(物); }
    }
    static void 删除界面对象(GameObject 物)
    { if (Application.isPlaying) UnityEngine.Object.Destroy(物); else UnityEngine.Object.DestroyImmediate(物); }
    void 换页(bool 保留序章画面 = false)
    {
        清理剪纸主页();
        作弊码已打开 = false;
        清理主页选图();
        清理拾取提示();
        清理战斗界面();
        页面背景.sprite = 天帝青绿皮肤.获取("BG01") ?? 游戏.主页背景; 页面背景.color = 页面背景.sprite != null ? Color.white : 纸;
        if (!保留序章画面 && 序章全屏媒体 != null) { 序章全屏媒体.gameObject.SetActive(false); 删除界面对象(序章全屏媒体.gameObject); 序章全屏媒体 = null; }
        var 旧排版 = 页面.GetComponent<天帝移动排版>(); if (旧排版 != null) 旧排版.排版 = null;
        清空(页面); 清空(弹层); 设置已打开 = 确认已打开 = 地图已打开 = 角色已打开 = 图鉴已打开 = 宝盒已打开 = 回收已打开 = false; 角色页 = null; 图鉴页 = null; 回收页 = null; 宝盒概率层 = null; 页面背景.enabled = !保留序章画面;
        战斗小地图 = null; 战斗坐标 = null; 战斗目标 = 战斗波次 = null; 移动目标上次文本 = null; 移动目标显示截止 = 0; 战斗离开 = null; 战斗摇杆 = null; 触控跑步 = false;
        战斗血量 = 战斗配置字 = null; 战斗血条 = null; 伤害字池.Clear();
        掉落提示 = null;
        序章字卡 = null; 序章标题 = 序章正文 = null;
        序章进度 = null; 序章时间 = 序章进度提示 = null;
        序章暂停字 = null; 序章有字幕 = false;
        主页人物 = null; 主页人物图 = null; 主页待机秒 = 0;
        主页实力字 = 主页构筑字 = 主页灵石字 = null;
        道纹页 = null; 改造页 = null;
        源道纹页 = null;
    }
    public void 显示标题()
    {
        if (显示剪纸标题()) return;
        换页(); var 标题 = 字(页面, 天帝游戏.全名.Replace("到我为", "\n到我为"), 220, 240, 1160, 230, 64, 墨);
        天帝界面美术.标题(标题);
        var 描边 = 标题.gameObject.AddComponent<Outline>(); 描边.effectColor = new Color(.94f, .94f, .85f, .8f); 描边.effectDistance = new Vector2(1, -1);
        按钮(页面, 游戏.可继续游戏 ? "继续游戏" : "开始游戏", 610, 570, 380, 76,
            () => { if (游戏.可继续游戏) 游戏.继续游戏(); else 游戏.开始序章(); }, true);
        if (游戏.可继续游戏) 按钮(页面, "新游戏", 680, 668, 240, 60, 游戏.开始序章);
        按钮(页面, "设置", 1388, 40, 166, 60, 显示设置);
        if (!天帝移动适配.启用) 按钮(页面, "退出游戏", 1388, 800, 166, 60, 游戏.退出游戏);
        字(页面, 游戏.存档提示, 260, 760, 1080, 65, 22, 朱);
        字(页面, "版本 " + Application.version, 40, 820, 430, 40, 18, 次墨).alignment = TextAnchor.MiddleLeft;
        if (天帝移动适配.启用)
        {
            var 驱动 = 页面.GetComponent<天帝移动排版>() ?? 页面.gameObject.AddComponent<天帝移动排版>();
            驱动.排版 = 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                天帝双端页面布局.固定(标题.rectTransform, 宽 * .1f, 高 * .18f, 宽 * .8f, 高 * .34f); 标题.fontSize = 28;
                天帝双端页面布局.按键(面板.Find(游戏.可继续游戏 ? "继续游戏" : "开始游戏") as RectTransform, 宽 * .5f - 130, 高 * .58f, 260, 52);
                天帝双端页面布局.按键(面板.Find("新游戏") as RectTransform, 宽 * .5f - 100, 高 * .58f + 58, 200);
                天帝双端页面布局.按键(面板.Find("设置") as RectTransform, 宽 - 112, 8, 100);
                foreach (var 文 in 面板.GetComponentsInChildren<Text>())
                    if (文.text.StartsWith("版本 ")) { 天帝双端页面布局.固定(文.rectTransform, 12, 高 - 30, 240, 24); 文.fontSize = 14; }
            };
        }
    }
    public void 显示新游戏确认()
    {
        显示确认("开始新游戏", "选定新的源道纹后将替换当前进度，并保留上一份备份。\n在选定前返回，原存档仍可继续。", "开始新游戏", 游戏.确认开始新游戏);
    }
    void 显示确认(string 标题, string 正文, string 确认文字, Action 确认, float 高度 = 420)
    {
        if (天帝剪纸界面皮肤.已启用) { 显示山水确认(标题, 正文, 确认文字, 确认, 高度); return; }
        if (确认已打开 || 设置已打开 || 地图已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        确认已打开 = true;
        foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = false;
        var 遮 = 图(弹层, "确认遮罩", 0, 0, 1600, 900, new Color(.02f, .04f, .04f, .68f)); 遮.raycastTarget = true;
        var 框 = 图(弹层, "确认面板", 400, (900 - 高度) / 2, 800, 高度, 纸).rectTransform;
        if (天帝移动适配.启用) 天帝响应布局.比例(框, .075f, .08f, .85f, .84f);
        var 标题文 = 字(框, 标题, 40, 28, 720, 70, 32, 墨);
        var 正文文 = 字(框, 正文, 55, 108, 690, 高度 - 236, 高度 > 420 ? 23 : 25, 次墨);
        var 取消键 = 按钮(框, "取消", 95, 高度 - 100, 270, 60, 关闭确认);
        var 确认键 = 按钮(框, 确认文字, 435, 高度 - 100, 270, 60, () => { 关闭确认(); 确认?.Invoke(); }, true);
        if (天帝移动适配.启用)
        {
            var 内容 = 区块(框, "确认正文", 55, 108, 690, 高度 - 236);
            正文文.rectTransform.SetParent(内容, false); 正文文.rectTransform.anchoredPosition = Vector2.zero;
            var 口 = 天帝响应布局.滚动正文(内容);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                天帝双端页面布局.固定(标题文.rectTransform, 12, 4, 宽 - 24, 44); 标题文.fontSize = 22;
                天帝双端页面布局.固定(口, 12, 52, 宽 - 24, 高 - 112);
                正文文.fontSize = 16; 天帝双端页面布局.固定(正文文.rectTransform, 0, 0, 宽 - 24, 1000);
                float 正文高 = Mathf.Ceil(正文文.preferredHeight) + 8;
                天帝双端页面布局.固定(正文文.rectTransform, 0, 0, 宽 - 24, 正文高); 内容.sizeDelta = new Vector2(0, Mathf.Max(正文高, 口.rect.height));
                天帝双端页面布局.按键((RectTransform)取消键.transform, 12, 高 - 52, (宽 - 30) / 2);
                天帝双端页面布局.按键((RectTransform)确认键.transform, 18 + (宽 - 30) / 2, 高 - 52, (宽 - 30) / 2);
            });
        }
        EventSystem.current?.SetSelectedGameObject(取消键.gameObject);
    }
    public void 关闭确认()
    {
        if (作弊码已打开) { 关闭作弊码(); return; }
        if (!确认已打开) return; 确认已打开 = false; 清空(弹层);
        foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = true;
    }
    public void 显示消息(string 正文)
    {
        if (设置已打开) 关闭设置(); if (地图已打开) 关闭地图选择(); if (确认已打开) 关闭确认();
        if (角色已打开) 关闭角色();
        if (图鉴已打开) 关闭图鉴();
        if (宝盒已打开) 关闭宝盒();
        if (回收已打开) 关闭回收();
        显示确认("提示", 正文, "知道了", () => { });
    }
    public void 更新存档状态(string 提示)
    { 存档状态字.text = 提示; 存档状态字.gameObject.SetActive(!string.IsNullOrEmpty(提示)); }
    public void 显示主页()
    {
        if (天帝首两页山水素材.已启用) { 显示剪纸主页(); return; }
        if (天帝青绿皮肤.已启用) { 显示青绿主页(); return; }
        换页();
        主页面板(页面, "主页导航留白", "导航留白", 0, 28, 384, 760);
        var 阴影 = 游戏.美术?.获取("DWUI_主页落地阴影");
        if (阴影 != null) 图(页面, "主角落地阴影", 688, 681, 224, 35, Color.white, 阴影);
        var 立绘 = 图(页面, "主角立绘", 575, 72, 450, 648, Color.white, 游戏.主角立绘); 立绘.preserveAspect = true;
        主页人物 = 立绘.rectTransform; 主页人物.pivot = new Vector2(.5f, .1f); 主页人物.anchoredPosition = new Vector2(800, -655.2f);
        if (游戏.美术 != null && 游戏.美术.主角移动动画 != null && 游戏.美术.主角移动动画.十组完整)
        {
            主页人物图 = 立绘; 立绘.sprite = 游戏.美术.主角移动动画.正面待机(0);
            主页人物.sizeDelta = new Vector2(800,800); 主页人物.pivot = new Vector2(.5f,48f/512);
            主页人物.anchoredPosition = new Vector2(800,-700); 主页人物.localScale = Vector3.one;
        }
        var 设置键 = 主页按钮(页面, "设置", "设置按钮", 1400, 24, 172, 56, 显示设置, 25, 天帝道纹美术.浅字);
        var 设置字 = 设置键.GetComponentInChildren<Text>(); 设置字.rectTransform.anchoredPosition = new Vector2(57, 0); 设置字.rectTransform.sizeDelta = new Vector2(91, 52);
        主页面板((RectTransform)设置键.transform, "设置图标", "图标设置", 30, 15, 26, 26);
        主页导航("角色", "角色", 0, 显示角色);
        主页导航("道纹", "道纹", 1, 游戏.打开道纹);
        主页导航("道纹改造", "道纹改造", 2, 游戏.打开道纹改造);
        主页导航("道纹图鉴", "道纹图鉴", 3, 显示图鉴);
        主页导航("宝盒", "宝盒", 4, 显示宝盒);
        主页导航("道纹回收", "道纹回收", 5, 显示回收);
        主页导航("作弊码", "道纹", 6, 显示作弊码);
        var 灵石底 = 主页面板(页面, "主页灵石纸面", "导航按钮", 1150, 24, 226, 56).rectTransform;
        主页面板(灵石底, "灵石图标", "图标灵石", 43, 13, 24, 30).preserveAspect = true;
        主页灵石字 = 主页字(灵石底, "灵石  " + (游戏.宝盒数据?.灵石显示 ?? "0"), 78, 10, 124, 36, 23, 主页石青字);
        主页灵石字.alignment = TextAnchor.MiddleLeft;
        主页灵石字.resizeTextForBestFit = true; 主页灵石字.resizeTextMinSize = 14; 主页灵石字.resizeTextMaxSize = 23;
        显示常驻选图();
        布局主页();
    }
    public void 显示角色()
    {
        if (游戏.阶段 != 游戏阶段.主页 || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开 || 游戏.主角属性 == null || 游戏.道纹数据 == null) return;
        角色已打开 = true;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        var 区 = 区块(弹层, "角色页面", 0, 0, 1600, 900);
        角色页 = 区.gameObject.AddComponent<天帝角色界面>();
        角色页.初始化(游戏.主角属性, 游戏.道纹数据, 游戏.默认字体, 关闭角色);
    }
    public void 关闭角色()
    {
        if (!角色已打开) return;
        角色已打开 = false; 清空(弹层); 角色页 = null;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = true;
    }
    public void 显示图鉴()
    {
        if (游戏.阶段 != 游戏阶段.主页 || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        图鉴已打开 = true;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        var 区 = 区块(弹层, "道纹图鉴页面", 0, 0, 1600, 900);
        图鉴页 = 区.gameObject.AddComponent<天帝道纹图鉴>();
        图鉴页.初始化(游戏.默认字体, 关闭图鉴);
    }
    public void 关闭图鉴()
    {
        if (!图鉴已打开) return;
        图鉴已打开 = false; 清空(弹层); 图鉴页 = null;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = true;
    }
    public void 显示宝盒()
    {
        if (天帝剪纸界面皮肤.已启用) { 显示山水宝盒(); return; }
        if (游戏.阶段 != 游戏阶段.主页 || 游戏.宝盒数据 == null || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        if (!ReferenceEquals(日志所属宝盒, 游戏.宝盒数据)) { 宝盒日志.Clear(); 日志所属宝盒 = 游戏.宝盒数据; }
        宝盒已打开 = true;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        var 遮 = 图(弹层, "宝盒遮罩", 0, 0, 1600, 900, new Color(.01f, .025f, .025f, .78f)); 遮.raycastTarget = true;
        var 框 = 图(弹层, "宝盒面板", 178, 40, 1244, 820, new Color(.055f, .105f, .11f)).rectTransform;
        字(框, "道纹宝盒", 40, 25, 600, 56, 38, 纸).alignment = TextAnchor.MiddleLeft;
        var 余额 = 字(框, "", 756, 30, 278, 48, 27, new Color(.96f, .82f, .48f)); 余额.alignment = TextAnchor.MiddleRight;
        按钮(框, "关闭", 1054, 27, 148, 56, 关闭宝盒);
        字(框, "击败敌人获得灵石；每次抽取一枚道纹，直接进入道纹背包。", 42, 83, 1100, 34, 19, new Color(.72f, .79f, .74f)).alignment = TextAnchor.MiddleLeft;

        var 按键 = new Button[3];
        var 种类 = new[] { 宝盒种类.属性, 宝盒种类.功能, 宝盒种类.分叉 };
        var 名称 = new[] { "属性宝盒", "功能宝盒", "分叉宝盒" };
        var 说明 = new[]
        {
            "初次构筑可先选属性盒\n随机属性增益 · 需接通源纹",
            "改变攻击形态，未必增加伤害\n" + 天帝顺序道纹.功能数量 + "种等概率 · 固定稀有\n单次消耗500灵石，先看功能详情",
            "固定普通品阶 · 无词条\n3–6接口，专门传导与分流"
        };
        var 结果框 = 图(框, "宝盒结果衬底", 42, 126, 1160, 84, new Color(.08f, .15f, .16f)).rectTransform;
        var 结果标题 = 字(结果框, "选择宝盒，抽取一枚道纹", 22, 8, 806, 36, 26, 纸);
        结果标题.gameObject.name = "宝盒结果标题"; 结果标题.alignment = TextAnchor.MiddleLeft;
        结果标题.font = 游戏.默认字体; 结果标题.fontStyle = FontStyle.Bold;
        var 结果摘要 = 字(结果框, "抽取后在这里查看本次获得，完整详情可查看下方记录。", 22, 45, 806, 28, 18, new Color(.72f, .79f, .74f));
        结果摘要.gameObject.name = "宝盒结果摘要"; 结果摘要.alignment = TextAnchor.MiddleLeft;
        var 结果状态 = 字(结果框, "每次获得一枚\n自动进入道纹背包", 840, 12, 296, 60, 19, new Color(.72f, .79f, .74f));
        结果状态.gameObject.name = "宝盒结果状态"; 结果状态.alignment = TextAnchor.MiddleRight;
        foreach (var 文 in new[] { 结果标题, 结果摘要, 结果状态 })
        { 文.resizeTextForBestFit = true; 文.resizeTextMinSize = 16; 文.resizeTextMaxSize = 文.fontSize; }
        // 最近结果常驻，不让玩家在连续抽取时寻找一行小字；记录仍可悬停查看完整词条。
        Action<宝盒记录> 显示结果 = 记录 =>
        {
            var 纹 = 记录.道纹; int 口数 = 0;
            for (int d = 0; d < 6; d++) if (纹.有接口(d)) 口数++;
            结果标题.color = 天帝道纹美术.纸面文字(纸);
            结果标题.text = "本次获得  " + 天帝道纹品阶.彩色品阶文字(纹.品阶) + " · " + 纹.名称;
            结果摘要.text = 纹.候选说明 + " · " + 口数 + "接口 · 物品等级 " + 纹.物品等级;
            结果状态.text = "已放入道纹背包\n" + (游戏.宝盒数据.无限灵石 ? "无限灵石 · 未扣费" : "消耗 " + 天帝宝盒.价格(记录.种类) + " 灵石");
        };
        if (宝盒日志.Count > 0) 显示结果(宝盒日志[宝盒日志.Count - 1]);
        var 日志框 = 图(框, "抽取日志", 42, 544, 1160, 234, new Color(.025f, .055f, .06f)).rectTransform;
        字(日志框, "最近抽取", 18, 8, 210, 40, 21, 纸).alignment = TextAnchor.MiddleLeft;
        var 状态字 = 字(日志框, 天帝移动适配.启用 ? "点记录查看完整详情" : "鼠标悬停记录查看完整详情", 230, 11, 910, 34, 18, new Color(.72f, .79f, .74f));
        状态字.alignment = TextAnchor.MiddleRight;
        var 视口 = 图(日志框, "日志视口", 18, 54, 1124, 158, Color.clear).rectTransform;
        视口.GetComponent<Image>().raycastTarget = true;
        视口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 区块(视口, "日志内容", 0, 0, 1124, 158);
        // ScrollRect管理内容高度；日志列按视口宽度伸展，避免再次转换累计偏移。
        天帝响应布局.动态(内容);
        内容.anchorMin = new Vector2(0, 1); 内容.anchorMax = Vector2.one;
        内容.sizeDelta = new Vector2(0, 158);
        var 滚动 = 视口.gameObject.AddComponent<ScrollRect>();
        滚动.content = 内容; 滚动.viewport = 视口; 滚动.horizontal = false; 滚动.vertical = true;
        滚动.movementType = ScrollRect.MovementType.Clamped; 滚动.scrollSensitivity = 30;
        var 空提示 = 字(内容, "暂无抽取记录", 14, 58, 1080, 42, 20, new Color(.55f, .65f, .62f));
        空提示.alignment = TextAnchor.MiddleCenter;
        var 详情框 = 区块(框, "日志道纹详情", 0, 0, 天帝道纹详情卡.宽度, 440);
        天帝响应布局.动态(详情框);
        var 详情卡 = 详情框.gameObject.AddComponent<天帝道纹详情卡>();
        详情卡.初始化(游戏.默认字体);
        详情卡.设置数据(游戏.道纹数据);
        Action<宝盒记录, bool> 添加日志 = (记录, 滚至底部) =>
        {
            int 行号 = 内容.childCount - 1;
            var 行 = 图(内容, "抽取记录-" + 行号, 6, 行号 * 46, 1098, 42,
                天帝道纹美术.彩绘皮肤 ? (行号 % 2 == 0 ? new Color(.94f, .95f, .88f) : new Color(1f, .97f, .88f))
                    : (行号 % 2 == 0 ? new Color(.075f, .13f, .14f) : new Color(.055f, .105f, .11f))).rectTransform;
            行.anchorMin = new Vector2(0, 1); 行.anchorMax = Vector2.one;
            行.sizeDelta = new Vector2(-26, 42);
            行.GetComponent<Image>().raycastTarget = true;
            var 纹 = 记录.道纹;
            int 口数 = 0;
            for (int d = 0; d < 6; d++) if (纹.有接口(d)) 口数++;
            string 色 = ColorUtility.ToHtmlStringRGB(天帝道纹美术.纸面文字(纹.品阶 == 道纹品阶.传说 ? 天帝道纹品阶.边颜色(纹.品阶, 0) : 天帝道纹品阶.获取(纹.品阶).颜色));
            string 摘要 = 纹.分类 == 道纹分类.分叉 ? 口数 + "接口" : 纹.候选说明 + " · " + 口数 + "接口";
            var 消耗字 = 字(行, (游戏.宝盒数据.无限灵石 ? "无限灵石，抽取" : "消耗" + 天帝宝盒.价格(记录.种类) + "灵石，抽取") + 记录.种类 + "宝盒，获得", 14, 3, 420, 36, 19, 纸);
            var 道纹字 = 字(行, 天帝道纹品阶.彩色品阶文字(纹.品阶) + "·<color=#" + 色 + ">" + 纹.名称 + "</color>", 434, 3, 302, 36, 19, 纸);
            var 摘要字 = 字(行, 摘要, 742, 3, 342, 36, 18, new Color(.72f, .79f, .74f));
            foreach (var 文 in new[] { 消耗字, 道纹字, 摘要字 })
            {
                float 左 = 文.rectTransform.anchoredPosition.x, 宽 = 文.rectTransform.sizeDelta.x;
                天帝响应布局.比例(文.rectTransform, 左 / 1098, 3f / 42, 宽 / 1098, 36f / 42);
                文.alignment = TextAnchor.MiddleLeft; 文.resizeTextForBestFit = true; 文.resizeTextMinSize = 14; 文.resizeTextMaxSize = 文.fontSize;
            }
            var 悬停 = 行.gameObject.AddComponent<天帝宝盒日志悬停>();
            悬停.初始化(纹, 框, 详情卡);
            内容.sizeDelta = new Vector2(0, Mathf.Max(158, (行号 + 1) * 46));
            空提示.gameObject.SetActive(false);
            if (滚至底部) { Canvas.ForceUpdateCanvases(); 滚动.verticalNormalizedPosition = 0; }
        };
        foreach (var 记录 in 宝盒日志) 添加日志(记录, false);
        if (宝盒日志.Count > 0) { Canvas.ForceUpdateCanvases(); 滚动.verticalNormalizedPosition = 0; }
        Action 更新余额 = () =>
        {
            余额.text = "灵石  " + 游戏.宝盒数据.灵石显示;
            for (int i = 0; i < 按键.Length; i++) 按键[i].interactable = 游戏.宝盒数据.灵石 >= 天帝宝盒.价格(种类[i]);
        };
        for (int i = 0; i < 3; i++)
        {
            int 序号 = i;
            float x = 42 + i * 390;
            var 卡 = 图(框, 名称[i], x, 228, 376, 294, new Color(.09f, .16f, .17f)).rectTransform;
            图(卡, "顶部标线", 0, 0, 376, 5, i == 0 ? new Color(.57f, .75f, .58f) : i == 1 ? new Color(.45f, .67f, .88f) : new Color(.93f, .69f, .42f));
            字(卡, 名称[i], 22, 12, 332, 48, 29, 纸);
            var 说明字 = 字(卡, 说明[i], 22, 66, 332, 86, 19, 天帝道纹美术.次文); 说明字.alignment = TextAnchor.UpperLeft;
            var 概率键 = 按钮(卡, "查看概率", 22, 156, 158, 38, () => 显示宝盒概率(序号)); 概率键.gameObject.name = "宝盒概率-" + 序号; 概率键.GetComponentInChildren<Text>().fontSize = 18;
            字(卡, 天帝宝盒.价格(种类[i]) + " 灵石 / 次", 22, 196, 332, 40, 28, new Color(.96f, .82f, .48f));
            按键[i] = 按钮(卡, "抽取", 22, 240, 332, 42, () =>
            {
                if (游戏.宝盒数据.抽取(种类[序号], out var 道纹, out var 提示))
                {
                    天帝声音.提示("YS01_宝盒开启");
                    天帝声音.提示(道纹.品阶 >= 道纹品阶.稀有 ? "YS03_高阶获得" : "YS02_普通获得");
                    var 记录 = new 宝盒记录 { 种类 = 种类[序号], 道纹 = 道纹快照(道纹) };
                    宝盒日志.Add(记录); 添加日志(记录, true); 显示结果(记录);
                    游戏.保存进度();
                }
                else
                {
                    结果标题.text = "未能抽取 · " + 提示;
                    天帝声音.提示("UI04_拒绝");
                    结果标题.color = 天帝道纹美术.彩绘皮肤 ? 天帝道纹美术.警示色 : new Color(1f, .65f, .45f);
                    结果摘要.text = "本次未获得道纹，也没有扣除灵石。";
                    结果状态.text = "请检查灵石余额\n或道纹背包状态";
                }
                更新余额();
            }, true);
        }
        更新余额();
        if (天帝剪纸界面皮肤.已启用)
        {
            foreach (string 名 in 名称)
            {
                var 卡 = 框.Find(名) as RectTransform;
                var 插画 = 图(卡,"剪纸宝盒插画",72,8,232,98,Color.white,天帝剪纸界面皮肤.素材(名+"插画"));插画.preserveAspect=true;
                foreach (Transform 子 in 卡)
                    if (子.GetComponent<Text>() is Text 文 && 文.text == 名)
                        天帝双端页面布局.固定(文.rectTransform,22,105,332,42);
                var 说明字 = 卡.GetComponentsInChildren<Text>().First(文 => 文.text == 说明[Array.IndexOf(名称,名)]);
                天帝双端页面布局.固定(说明字.rectTransform,22,146,332,80); 说明字.fontSize=17;
                天帝双端页面布局.固定(卡.Find("宝盒概率-"+Array.IndexOf(名称,名)) as RectTransform,22,232,158,36);
                foreach (Transform 子 in 卡)
                    if (子.GetComponent<Text>() is Text 文 && 文.text.EndsWith("灵石 / 次"))
                    { 天帝双端页面布局.固定(文.rectTransform,190,232,170,36); 文.fontSize=19; }
                天帝双端页面布局.固定(卡.Find("抽取") as RectTransform,22,280,332,48);
                卡.sizeDelta=new Vector2(376,346);
            }
            天帝双端页面布局.固定(日志框,42,596,1160,182);
            天帝双端页面布局.固定(视口,18,54,1124,106);
            天帝剪纸界面皮肤.装配(弹层,"宝盒",框);
        }
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "手机宝盒正文", 42, 126, 1160, 652);
            var 重排 = 天帝双端页面布局.重排正文(正文口.GetComponent<ScrollRect>().content);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                天帝双端页面布局.页头(面板, "关闭");
                天帝双端页面布局.固定(正文口, 8, 天帝双端页面布局.页头高度, 面板.rect.width - 16, 面板.rect.height - 76);
                天帝双端页面布局.固定(余额.rectTransform, 面板.rect.width - 318, 4, 150, 40); 余额.fontSize = 16;
                foreach (Transform 子 in 面板) if (子.GetComponent<Text>() is Text 文 && 文 != 余额 && 文.text != "道纹宝盒") 文.gameObject.SetActive(false);
                重排();
            });
        }
    }
    public void 关闭宝盒()
    {
        if (!宝盒已打开) return;
        宝盒已打开 = false; 宝盒概率层 = null; 清空(弹层);
        if (主页灵石字 != null) 主页灵石字.text = "灵石  " + (游戏.宝盒数据?.灵石显示 ?? "0");
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = true;
    }
    天帝地图预览 地图预览(RectTransform 父, float x, float y, float w, float h)
    {
        var 区 = 区块(父, "地图图像", x, y, w, h); var 预览 = 区.gameObject.AddComponent<天帝地图预览>(); 预览.raycastTarget = false; return 预览;
    }
    // 保留旧入口供编辑器工具调用；主页已常驻展示选图。
    public void 显示地图选择() => 请求进入地图();
    public void 关闭地图选择() { 关闭等级下拉(); 地图已打开 = false; }
    public void 显示战斗错误(string 原因) => 显示消息(原因);
    public void 显示战斗加载(string 内容)
    {
        换页(); 图(页面, "加载背景", 0, 0, 1600, 900, Color.white,天帝道纹美术.获取("页面背景"));
        字(页面, 内容, 250, 350, 1100, 160, 36, 纸);
    }
    public void 显示战斗()
    {
        换页(); 页面背景.enabled = false;
        建立紧凑战斗界面();
        更新战斗目标();
        更新战斗位置(游戏.战斗场景.玩家位置, 游戏.战斗场景.地图.所在格(游戏.战斗场景.玩家位置));
        更新战斗状态();
    }
    public void 更新主页实力()
    {
        刷新剪纸数值();
        if (主页实力字 != null) 主页实力字.text = 天帝实力评语.读取(游戏.道纹数据, 游戏.主角属性);
        if (主页构筑字 != null) 主页构筑字.text = 天帝移动适配.启用
            ? "生效 " + (游戏.道纹数据?.生效数 ?? 0) + " 枚 · 射击 " + (游戏.道纹数据?.射击通路数 ?? 1) + " 路"
            : 天帝实力评语.说明(游戏.道纹数据, 游戏.主角属性);
    }
    // 按运行平台判断；触屏PC也使用PC布局，不因Touchscreen设备而显示摇杆。
    public static bool 使用移动控件(bool 移动平台) => 移动平台;
    public void 更新战斗状态()
    {
        var 战 = 游戏.战斗场景?.战斗; var 人 = 游戏.主角属性;
        if (战 == null || 人 == null) return;
        刷新紧凑战斗状态(战, 人);
        if (战斗已暂停) return;
        foreach (var 浮 in 伤害字池)
        {
            if (浮.剩余 <= 0) continue;
            浮.剩余 -= Mathf.Min(Time.deltaTime, .1f);
            if (浮.剩余 <= 0) { 浮.字.gameObject.SetActive(false); continue; }
            Vector2 屏 = 游戏.战斗场景.俯视相机.WorldToScreenPoint(new Vector3(浮.位置.x, .3f, 浮.位置.y));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(页面, 屏, null, out var 点);
            点.x += 浮.横移;
            float 进度=1-浮.剩余/.7f;
            点.y += 48 + 浮.起高 + 进度 * 40;
            浮.字.rectTransform.anchoredPosition = 点;
            float 弹=1+浮.弹幅*Mathf.Sin(Mathf.Clamp01(进度/.3f)*Mathf.PI)*(1-进度);
            浮.字.rectTransform.localScale=Vector3.one*弹;
            浮.组.alpha = Mathf.Min(1, 浮.剩余 * 3);
        }
    }
    public void 显示伤害飘字(Vector2 位置, float 伤害, bool 玩家受伤)
        => 显示伤害飘字(位置, new 战斗伤害明细(伤害), 玩家受伤);
    public void 显示伤害飘字(Vector2 位置, 战斗伤害明细 明细, bool 玩家受伤, string 标记="", bool 重击=false)
    {
        if (游戏.阶段 != 游戏阶段.战斗 || !天帝数值.有限(明细.合计) || 明细.合计 <= 0) return;
        飘字 空 = null; foreach (var 浮 in 伤害字池) if (浮.剩余 <= 0) { 空 = 浮; break; }
        if (空 == null && 伤害字池.Count < 96)
        {
            空 = new 飘字 { 字 = 字(页面, "", 0, 0, 232, 48, 22, Color.white) };
            空.字.gameObject.name = "伤害来源飘字";
            空.字.rectTransform.anchorMin = 空.字.rectTransform.anchorMax = new Vector2(.5f, .5f);
            天帝响应布局.动态(空.字.rectTransform);
            空.字.rectTransform.pivot = new Vector2(.5f, 0);
            空.字.horizontalOverflow = HorizontalWrapMode.Overflow; 空.字.verticalOverflow = VerticalWrapMode.Overflow;
            空.字.alignment = TextAnchor.LowerCenter;
            空.组 = 空.字.gameObject.AddComponent<CanvasGroup>(); 空.组.blocksRaycasts = 空.组.interactable = false;
            空.描边 = 空.字.gameObject.AddComponent<Outline>(); 空.描边.effectDistance = new Vector2(1, -1);
            伤害字池.Add(空);
        }
        // 饱和时替换最旧的一组，保持有界复用，不吞掉最新命中的来源。
        if (空 == null) foreach (var 浮 in 伤害字池) if (空 == null || 浮.剩余 < 空.剩余) 空 = 浮;
        if (空 == null) return;
        空.字.text = 天帝伤害显示.文本(明细, 伤害文案缓冲);
        if(!string.IsNullOrEmpty(标记))空.字.text="<color=#"+(标记=="护盾"||标记=="破盾"?"7BD9FF":玩家受伤?"FF8270":"FFE39A")+">"+标记+"</color>\n"+空.字.text;
        int 行数 = 1; foreach (char 字符 in 空.字.text) if (字符 == '\n') 行数++;
        空.字.fontSize = 行数 > 3 ? 18 : 22;
        空.字.rectTransform.sizeDelta = new Vector2(232, 空.字.preferredHeight + 8);
        空.位置 = 位置; 空.剩余 = .7f; 空.横移 = (伤害飘字序号++ % 3 - 1) * 24;
        空.起高=(伤害飘字序号%3)*14;空.弹幅=天帝受击表现.取("text_pop")*(重击?1.5f:1);
        空.字.rectTransform.localScale=Vector3.one;
        空.组.alpha = 1; 空.描边.effectColor = 玩家受伤 ? new Color(.28f, .035f, .025f, .95f) : new Color(.02f, .04f, .025f, .95f);
        空.字.gameObject.SetActive(true);
    }
    public void 显示战斗失败()
    {
        if (天帝剪纸界面皮肤.已启用) { 显示山水失败(); return; }
        关闭战斗暂停();
        清空(弹层); 触控跑步 = false;
        if (战斗摇杆 != null) 战斗摇杆.gameObject.SetActive(false);
        var 遮 = 图(弹层, "战斗失败遮罩", 0, 0, 1600, 900, new Color(0, 0, 0, .64f)); 遮.raycastTarget = true;
        var 框=图(弹层,"战斗失败面板",360,230,880,440,new Color(.025f,.075f,.068f,.98f)).rectTransform;
        字(框, "身陨此地", 40, 34, 800, 90, 46, 纸);
        字(框, "本局已拾取的道纹、通货与灵石均保留。", 70, 155, 740, 46, 24, 纸);
        字(框, "回到主页调整构筑，下次进入时恢复生命与资源。", 70, 218, 740, 54, 20, 天帝道纹美术.次文);
        按钮(框, "返回主页", 250, 324, 380, 72, 游戏.返回主页, true);
        if (天帝移动适配.启用)
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float y = 12, 宽 = 面板.rect.width;
                foreach (Transform 子 in 面板)
                {
                    if (!(子.GetComponent<Text>() is Text 文)) continue;
                    文.fontSize = 文.text == "身陨此地" ? 26 : 16;
                    天帝双端页面布局.固定(文.rectTransform, 16, y, 宽 - 32, 160);
                    float 高 = Mathf.Ceil(文.preferredHeight) + 6;
                    天帝双端页面布局.固定(文.rectTransform, 16, y, 宽 - 32, 高); y += 高 + 8;
                }
                天帝双端页面布局.按键(面板.Find("返回主页") as RectTransform, 16, 面板.rect.height - 60, 宽 - 32, 48);
            });
        更新战斗目标();
    }
    public void 更新战斗目标()
    {
        bool 可离开 = 游戏.战斗场景 != null && 游戏.战斗场景.可离开;
        if (战斗离开 != null) 战斗离开.gameObject.SetActive(游戏.阶段 == 游戏阶段.战斗);
        var 战 = 游戏.战斗场景?.战斗;
        string 文本 = 战?.玩家死亡 == true ? "身陨此地 · 返回主页重新构筑" : 可离开 ? "狼王已败 · 可以返回主页"
            : !string.IsNullOrEmpty(战?.战术.王台词) ? "狼王：「" + 战.战术.王台词 + "」"
            : 游戏.战斗场景?.地图.生存大图 == true ? (战.BOSS已出现 ? "狼王来袭·避开技能预警" : "四面兽潮·保持移动\n清剿 " + (战.敌人损伤比例 * 100).ToString("0") + "% · 80%时狼王现身")
            : 游戏.战斗场景?.地图.横向区域 == true ? "向东探索·清营地\n跨桥前行·战BOSS"
            : 战 != null && 战.BOSS已出现 ? 战.刷新阶段 + "\n避开技能预警 · 留意形态转换" : 战?.刷新阶段 + "\n清剿 " + ((战?.敌人损伤比例 ?? 0) * 100).ToString("0") + "% · 80%时狼王现身";
        if (战斗目标 != null)
        {
            战斗目标.text = 文本;
            if (天帝移动适配.启用)
            {
                if (文本 != 移动目标上次文本)
                {
                    移动目标上次文本 = 文本;
                    移动目标显示截止 = Time.unscaledTime + (战?.玩家死亡 == true || 可离开 ? 8f : 3.5f);
                }
                战斗目标.gameObject.SetActive(Time.unscaledTime < 移动目标显示截止);
            }
            else 战斗目标.gameObject.SetActive(true);
        }
    }
    public void 更新战斗位置(Vector2 位置, Vector2Int 格)
    {
        if (战斗坐标 != null) 战斗坐标.text = "格坐标 " + (格.x + 1) + "，" + (格.y + 1) + "  ·  天赋：" + 游戏.当前天赋.名称;
        if (战斗小地图 != null && 战斗小地图.玩家位置 != 位置) { 战斗小地图.玩家位置 = 位置; 战斗小地图.SetVerticesDirty(); }
        if (战斗小地图 != null && 游戏.战斗场景?.地图.生存大图 == true)
        {
            var 列 = 游戏.战斗场景.战斗.敌人; var 王 = 列[列.Count - 1];
            战斗小地图.BOSS位置 = 王.存活 ? 王.位置 : (Vector2?)null;
        }
    }
    public void 显示道纹(天帝道纹 数据)
    {
        换页(); var 区 = 区块(页面, "道纹页面", 0, 0, 1600, 900);
        var 背景 = 天帝道纹美术.获取("页面背景");
        if (背景 != null) { 页面背景.sprite = 背景; 页面背景.color = Color.white; }
        道纹页 = 区.gameObject.AddComponent<天帝道纹界面>(); 道纹页.初始化(数据, 游戏.默认字体, 游戏.返回主页);
    }
    public void 显示道纹改造(天帝道纹 数据, 天帝通货 通货)
    {
        换页(); var 区 = 区块(页面, "道纹改造页面", 0, 0, 1600, 900);
        改造页 = 区.gameObject.AddComponent<天帝通货界面>(); 改造页.初始化(数据, 通货, 游戏.默认字体, 游戏.返回主页);
    }
    public void 显示源道纹选择(bool 衔接序章 = false)
    {
        换页(衔接序章); var 区 = 区块(页面, "源道纹选择", 0, 0, 1600, 900);
        源道纹页 = 区.gameObject.AddComponent<天帝源道纹选择>(); 源道纹页.初始化(游戏, 衔接序章);
    }
    public RawImage 显示序章(bool 实时 = false)
    {
        换页();
        页面背景.enabled = false;
        图(页面, "漫画序章底", 0, 0, 1600, 900, Color.white, 天帝道纹美术.获取("页面背景"));
        图(页面, "漫画纸边", 168, 43, 1264, 714, new Color(.86f, .84f, .73f));
        var 屏 = 区块(页面, "漫画画面", 176, 49, 1248, 702);
        var 像 = 屏.gameObject.AddComponent<RawImage>(); 像.raycastTarget = false; 像.enabled = false;
        序章字卡 = 区块(页面, "漫画字幕", 0, 0, 1600, 834);
        序章标题 = 字(序章字卡, "", 180, 5, 1240, 35, 23, 纸);
        序章正文 = 字(序章字卡, "", 130, 761, 1340, 67, 25, new Color(.88f, .90f, .84f));
        序章正文.alignment = TextAnchor.MiddleCenter;
        var 跳过 = 按钮(页面, "跳过序章", 1443, 50, 138, 46, 游戏.跳过序章);
        跳过.GetComponentInChildren<Text>().fontSize = 19;
        var 上格 = 按钮(页面, "上一格", 1443, 116, 138, 46, () => 游戏.序章翻格(-1));
        var 下格 = 按钮(页面, "下一格", 1443, 175, 138, 46, () => 游戏.序章翻格(1));
        上格.GetComponentInChildren<Text>().fontSize = 下格.GetComponentInChildren<Text>().fontSize = 19;
        var 进度底 = 图(页面, "序章进度底", 0, 834, 1600, 66, new Color(.015f, .03f, .035f, .96f)).rectTransform;
        var 暂停 = 按钮(进度底, "暂停序章", 140, 8, 80, 44, 游戏.切换序章暂停);
        序章暂停字 = 暂停.GetComponentInChildren<Text>(); 序章暂停字.text = "Ⅱ";
        var 字幕键 = 按钮(进度底, "字幕", 240, 8, 146, 44, () => 游戏.设置字幕(!游戏.字幕开启));
        var 字幕字 = 字幕键.GetComponentInChildren<Text>(); 字幕字.text = 游戏.字幕开启 ? "字幕：开" : "字幕：关";
        字幕键.onClick.AddListener(() => 字幕字.text = 游戏.字幕开启 ? "字幕：开" : "字幕：关");
        序章进度提示 = 字(进度底, 天帝移动适配.启用 ? "漫画序章 · 点暂停 / 拖动回看" : "漫画序章 · 按空格暂停 / 拖动回看", 410, 12, 760, 32, 20, new Color(0.72f, 0.77f, 0.72f)); 序章进度提示.alignment = TextAnchor.MiddleLeft;
        序章时间 = 字(进度底, "00:00 / --:--", 1250, 12, 300, 32, 21, 纸); 序章时间.alignment = TextAnchor.MiddleRight;
        序章进度提示.color = new Color(.80f, .86f, .82f);
        序章时间.color = 天帝道纹美术.浅字;
        var 轨区 = 区块(进度底, "序章进度条", 140, 43, 1320, 22);
        var 热区 = 轨区.gameObject.AddComponent<Image>(); 热区.color = Color.clear; 热区.raycastTarget = true;
        图(轨区, "进度轨道", 0, 8, 1320, 4, new Color(0.24f, 0.32f, 0.32f));
        var 填区 = 区块(轨区, "进度填充区", 0, 8, 1320, 4);
        var 填 = 图(填区, "进度填充", 0, 0, 1320, 4, new Color(0.84f, 0.73f, 0.47f));
        填.rectTransform.anchorMin = Vector2.zero; 填.rectTransform.anchorMax = Vector2.one; 填.rectTransform.offsetMin = 填.rectTransform.offsetMax = Vector2.zero;
        var 柄区 = 区块(轨区, "进度手柄区", 0, 0, 1320, 22);
        var 柄 = 图(柄区, "进度手柄", 0, 0, 14, 18, 纸); 柄.raycastTarget = true;
        柄.rectTransform.anchorMin = 柄.rectTransform.anchorMax = 柄.rectTransform.pivot = new Vector2(0.5f, 0.5f); 柄.rectTransform.anchoredPosition = Vector2.zero;
        序章进度 = 轨区.gameObject.AddComponent<天帝序章进度条>(); 序章进度.minValue = 0; 序章进度.maxValue = 1; 序章进度.interactable = false;
        序章进度.fillRect = 填.rectTransform; 序章进度.handleRect = 柄.rectTransform; 序章进度.targetGraphic = 柄;
        var 导航 = 序章进度.navigation; 导航.mode = Navigation.Mode.None; 序章进度.navigation = 导航;
        序章进度.onValueChanged.AddListener(值 => 游戏.调整序章进度(值));
        if (天帝移动适配.启用)
        {
            var 布局 = 页面.GetComponent<天帝移动排版>() ?? 页面.gameObject.AddComponent<天帝移动排版>();
            布局.排版 = 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                天帝双端页面布局.固定(屏, 12, 48, 宽 - 24, 高 - 174);
                天帝双端页面布局.固定(序章正文.rectTransform, 12, 高 - 120, 宽 - 24, 54); 序章正文.fontSize = 16;
                天帝双端页面布局.固定(序章标题.rectTransform, 12, 2, 宽 - 330, 40); 序章标题.fontSize = 16;
                天帝双端页面布局.按键((RectTransform)上格.transform, 宽 - 306, 2, 94);
                天帝双端页面布局.按键((RectTransform)下格.transform, 宽 - 206, 2, 94);
                天帝双端页面布局.按键((RectTransform)跳过.transform, 宽 - 106, 2, 94);
                天帝双端页面布局.固定(进度底, 0, 高 - 62, 宽, 62);
                天帝双端页面布局.按键((RectTransform)暂停.transform, 8, 2, 44);
                天帝双端页面布局.按键((RectTransform)字幕键.transform, 58, 2, 94);
                天帝双端页面布局.固定(序章进度提示.rectTransform, 162, 2, 宽 - 326, 40); 序章进度提示.fontSize = 14;
                天帝双端页面布局.固定(序章时间.rectTransform, 宽 - 158, 2, 150, 30); 序章时间.fontSize = 14;
                天帝双端页面布局.固定(轨区, 162, 32, 宽 - 172, 28);
                天帝响应布局.比例(填区, 0, .3f, 1, .15f); 天帝响应布局.比例(柄区, 0, 0, 1, 1);
            };
        }
        return 像;
    }
    public void 更新序章进度(double 当前, double 总, bool 可调整, string 状态)
    {
        if (序章进度 == null) return;
        if (序章暂停字 != null) 序章暂停字.text = 游戏.序章已暂停 ? "▶" : "Ⅱ";
        序章进度.interactable = 可调整;
        if (!序章进度.调整中) 序章进度.SetValueWithoutNotify(总 > 0 ? Mathf.Clamp01((float)(当前 / 总)) : 0);
        序章时间.text = 时间文字(当前) + " / " + (总 > 0 ? 时间文字(总) : "--:--");
        序章进度提示.text = 状态 == "已暂停" ? "已暂停 · 拖动回看" : 状态 == "后台暂停" ? "后台暂停" : 天帝移动适配.启用 ? "逐格漫画 · 点暂停 / 拖动回看" : "逐格漫画 · 空格暂停 / 拖动回看";
    }
    static string 时间文字(double 秒)
    {
        int 值 = double.IsNaN(秒) || double.IsInfinity(秒) ? 0 : (int)Math.Max(0, Math.Min(秒, int.MaxValue));
        return (值 / 60).ToString("00") + ":" + (值 % 60).ToString("00");
    }
    public void 更新序章文字(string 标题, string 正文)
    {
        if (序章字卡 == null) return;
        序章有字幕 = !string.IsNullOrEmpty(标题); 更新字幕显示();
        序章标题.text = 标题; 序章正文.text = 正文;
    }
    public void 更新字幕显示() { if (序章字卡 != null) 序章字卡.gameObject.SetActive(序章有字幕 && 游戏.字幕开启); }
    public void 显示设置()
    {
        if (天帝剪纸界面皮肤.已启用) { 显示山水设置(); return; }
        if ((游戏.阶段 != 游戏阶段.主页 && 游戏.阶段 != 游戏阶段.标题) || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        设置已打开 = true;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        var 遮 = 图(弹层, "遮罩", 0, 0, 1600, 900, new Color(0.06f, 0.1f, 0.09f, 0.5f)); 遮.raycastTarget = true;
        var 框 = 图(弹层, "设置面板", 400, 65, 800, 770, 纸).rectTransform;
        字(框, "设置", 40, 20, 720, 65, 36, 墨);
        音量滑条(框, "总音量", 104, 游戏.音量, 游戏.设置音量);
        音量滑条(框, "音乐", 208, 游戏.音乐音量, 游戏.设置音乐音量);
        音量滑条(框, "音效", 312, 游戏.音效音量, 游戏.设置音效音量);
        音量滑条(框, "剧情声音", 416, 游戏.剧情音量, 游戏.设置剧情音量);
        var 字幕区 = 区块(框, "字幕选项", 75, 530, 320, 50);
        var 字幕热区 = 字幕区.gameObject.AddComponent<Image>(); 字幕热区.color = Color.clear;
        var 勾底 = 图(字幕区, "字幕复选框", 0, 7, 34, 34, new Color(.78f, .81f, .74f)); 勾底.raycastTarget = true;
        var 勾 = 图(勾底.rectTransform, "字幕选中", 7, 7, 20, 20, 墨);
        var 开关 = 字幕区.gameObject.AddComponent<Toggle>(); 开关.targetGraphic = 勾底; 开关.graphic = 勾;
        开关.SetIsOnWithoutNotify(游戏.字幕开启); 开关.onValueChanged.AddListener(游戏.设置字幕);
        字(字幕区, "剧情字幕", 52, 0, 240, 50, 25, 墨).alignment = TextAnchor.MiddleLeft;
        var 重看 = 按钮(框, "重看序章", 415, 530, 310, 52, 游戏.重看序章); 重看.interactable = 游戏.序章已解锁;
        if (游戏.阶段 == 游戏阶段.主页) 按钮(框, "返回标题", 75, 604, 310, 52, 游戏.返回标题);
        按钮(框, "关闭", 415, 604, 310, 52, 关闭设置, true);
        if (!天帝移动适配.启用) 按钮(框, "退出游戏", 255, 686, 290, 40, 游戏.退出游戏);
        if (天帝移动适配.启用)
        {
            var 关闭键 = 框.Find("关闭") as RectTransform;
            var 正文口 = 天帝双端页面布局.滚动组(框, "设置选项正文", 75, 104, 650, 552);
            var 重排 = 天帝双端页面布局.重排正文(正文口.GetComponent<ScrollRect>().content);
            关闭键.SetParent(框, false);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                天帝双端页面布局.页头(面板, "关闭");
                天帝双端页面布局.固定(正文口, 12, 天帝双端页面布局.页头高度, 面板.rect.width - 24, 面板.rect.height - 76);
                重排();
            });
        }
        天帝剪纸界面皮肤.装配(弹层,"设置",框);
    }
    void 音量滑条(RectTransform 父, string 名称, float y, float 初值, Action<float> 设置)
    {
        var 数字 = 字(父, 名称 + "  " + Mathf.RoundToInt(初值 * 100) + "%", 75, y, 650, 38, 24, 次墨);
        数字.alignment = TextAnchor.MiddleLeft;
        var 滑区 = 区块(父, 名称, 75, y + 43, 650, 46);
        var 热区 = 滑区.gameObject.AddComponent<Image>(); 热区.color = Color.clear; 热区.raycastTarget = true;
        var 轨 = 图(滑区, "底", 0, 17, 650, 8, new Color(0.78f, 0.81f, 0.74f));
        var 填区 = 区块(滑区, "填充区", 0, 17, 650, 8);
        var 填 = 图(填区, "填充", 0, 0, 650, 8, 天帝剪纸界面皮肤.已启用 ? 天帝剪纸界面皮肤.朱红 : 墨);
        填.rectTransform.anchorMin = Vector2.zero; 填.rectTransform.anchorMax = Vector2.one;
        填.rectTransform.offsetMin = 填.rectTransform.offsetMax = Vector2.zero;
        var 柄区 = 区块(滑区, "手柄区", 0, 0, 650, 46);
        var 柄 = 图(柄区, "手柄", 0, 0, 22, 28, 天帝道纹美术.强调); 柄.raycastTarget = true;
        柄.rectTransform.anchorMin = 柄.rectTransform.anchorMax = 柄.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        柄.rectTransform.anchoredPosition = Vector2.zero;
        天帝响应布局.动态(柄.rectTransform);
        if (天帝剪纸界面皮肤.已启用)
        {
            柄.sprite=天帝剪纸界面皮肤.素材("音量纸雕滑块");柄.color=Color.white;
            柄.rectTransform.sizeDelta=new Vector2(28,28);柄.preserveAspect=true;
        }
        var 滑 = 滑区.gameObject.AddComponent<Slider>(); 滑.minValue = 0; 滑.maxValue = 1;
        滑.fillRect = 填.rectTransform; 滑.handleRect = 柄.rectTransform; 滑.targetGraphic = 柄;
        滑.SetValueWithoutNotify(初值);
        bool 剪纸横行=天帝剪纸界面皮肤.已启用&&!天帝移动适配.启用;
        if(剪纸横行)
        {
            字(父,名称,75,y,180,64,24,墨).alignment=TextAnchor.MiddleLeft;
            天帝双端页面布局.固定(数字.rectTransform,660,y,95,64);数字.text=Mathf.RoundToInt(初值*100)+"%";数字.alignment=TextAnchor.MiddleRight;
            天帝双端页面布局.固定(滑区,260,y,390,64);
            天帝双端页面布局.固定(轨.rectTransform,10,29,370,6);
            天帝双端页面布局.固定(填区,10,29,370,6);
            天帝双端页面布局.固定(柄区,10,0,370,64);
        }
        滑.onValueChanged.AddListener(值 => { 设置(值); 数字.text = (剪纸横行 ? "" : 名称+"  ") + Mathf.RoundToInt(值 * 100) + "%"; });
    }
    public void 关闭设置()
    {
        if (!设置已打开) return; 游戏.保存设置(); 设置已打开 = false; 清空(弹层);
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = true;
    }
    public void 更新适配()
    {
        if (主页人物 != null)
        {
            if (主页人物图 != null)
            {
                主页待机秒 += Time.unscaledDeltaTime;
                主页人物图.sprite = 游戏.美术.主角移动动画.正面待机(主页待机秒);
            }
            // 静态立绘固定脚底，避免整个人上下漂浮。
        }
        var 尺寸 = 天帝移动适配.屏幕尺寸;
        var 区 = 天帝移动适配.有效安全区(天帝移动适配.安全区, 尺寸);
        if (尺寸.x == 0 || 尺寸.y == 0) return;
        var 视口 = 天帝移动适配.横屏视口(区);
        更新剪纸主页();
        if (游戏.战斗场景?.俯视相机 != null)
        {
            var 相机区 = new Rect(视口.xMin / 尺寸.x, 视口.yMin / 尺寸.y, 视口.width / 尺寸.x, 视口.height / 尺寸.y);
            if (游戏.战斗场景.俯视相机.rect != 相机区) 游戏.战斗场景.俯视相机.rect = 相机区;
        }
        var 布局尺寸 = 天帝移动适配.布局尺寸;
        bool 变化 = 区 != 上次安全区 || 尺寸 != 上次尺寸 || 布局尺寸 != 上次布局尺寸 || 上次移动平台 != 天帝移动适配.启用;
        if (!变化 && !适配待刷新 && 上次布局修订 == 响应布局.修订号) return;
        适配待刷新 = false; 上次移动平台 = 天帝移动适配.启用; 上次布局尺寸 = 布局尺寸;
        适配排版次数++;
        // CanvasScaler 是唯一的屏幕缩放来源。这里仅调整安全区和设计区，避免
        // 每帧手写 scaleFactor 与 CanvasScaler 在不同执行顺序下互相覆盖。
        界面画布.pixelPerfect = 天帝移动适配.启用;
        if (区 != 上次安全区 || 尺寸 != 上次尺寸)
        {
            上次安全区 = 区; 上次尺寸 = 尺寸;
            安全区.anchorMin = new Vector2(视口.xMin / 尺寸.x, 视口.yMin / 尺寸.y); 安全区.anchorMax = new Vector2(视口.xMax / 尺寸.x, 视口.yMax / 尺寸.y);
            float 左 = 视口.xMin / 尺寸.x, 右 = 视口.xMax / 尺寸.x, 下 = 视口.yMin / 尺寸.y, 上 = 视口.yMax / 尺寸.y;
            天帝响应布局.比例(留边[0], 0, 0, 左, 1);
            天帝响应布局.比例(留边[1], 右, 0, 1 - 右, 1);
            天帝响应布局.比例(留边[2], 左, 0, 右 - 左, 1 - 上);
            天帝响应布局.比例(留边[3], 左, 1 - 下, 右 - 左, 下);
            Canvas.ForceUpdateCanvases();
        }
        设计区.anchorMin = 设计区.anchorMax = 设计区.pivot = new Vector2(.5f, .5f);
        设计区.anchoredPosition = Vector2.zero; 设计区.sizeDelta = 布局尺寸;
        设计区.localScale = Vector3.one * 安全区布局比例(布局尺寸);
        if (战斗界面层 != null) 战斗界面层.localScale = Vector3.one * 安全区布局比例(布局尺寸);
        响应布局.提交();
        foreach (var 布局 in 根.GetComponentsInChildren<天帝移动排版>()) 布局.更新();
        foreach (var 正文 in 根.GetComponentsInChildren<天帝正文排版>()) 正文.更新?.Invoke();
        更新移动战斗布局();
        响应布局.提交();
        // 动态正文与移动页面各自保留LateUpdate；新控件、换页、屏幕/安全区变化才做全树排版。
        上次布局修订 = 响应布局.修订号;
    }
    float 安全区布局比例(Vector2 布局尺寸)
    {
        float 比例 = 天帝移动适配.固定分辨率.x / 布局尺寸.x;
        if (界面画布.renderMode == RenderMode.WorldSpace) return 比例; // 隔离渲染使用独立相机尺寸。
        var 尺寸 = 天帝移动适配.屏幕尺寸;
        var 区 = 天帝移动适配.有效安全区(天帝移动适配.安全区, 尺寸);
        // CanvasScaler适配整屏，页面/HUD仅收缩一次至可用横屏安全区。
        return 比例 * 天帝移动适配.显示比例(区) / 天帝移动适配.显示比例(new Rect(0, 0, 尺寸.x, 尺寸.y));
    }
    public void 更新拾取提示(float 秒) { if (!战斗已暂停) 拾取列表?.更新(秒); }
    void 清理拾取提示()
    {
        if (拾取事件源 != null && 拾取列表 != null) 拾取事件源.获得道纹 -= 拾取列表.加入;
        if (通货事件源 != null && 拾取列表 != null) 通货事件源.获得通货 -= 拾取列表.加入;
        if (灵石事件源 != null && 拾取列表 != null) 灵石事件源.获得灵石 -= 拾取列表.加入灵石;
        灵石事件源 = null;
        通货事件源 = null; 拾取事件源 = null; 拾取列表?.Dispose(); 拾取列表 = null;
    }
    public void 销毁() { 清理拾取提示(); if (根 != null) 删除界面对象(根); }
}

public sealed class 天帝宝盒日志悬停 : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
{
    道纹实例 道纹;
    RectTransform 根;
    天帝道纹详情卡 详情;

    public void 初始化(道纹实例 纹, RectTransform 面板, 天帝道纹详情卡 详情卡)
    { 道纹 = 纹; 根 = 面板; 详情 = 详情卡; }

    void 显示(Vector2 屏幕位置)
    {
        if (道纹 == null || 根 == null || 详情 == null) return;
        if (!详情.gameObject.activeSelf) 详情.设置(道纹, "已收入道纹背包");
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根, 屏幕位置, null, out var 点);
        float x = 点.x + 20;
        if (x + 天帝道纹详情卡.宽度 > 根.rect.width - 12) x = 点.x - 天帝道纹详情卡.宽度 - 20;
        x = Mathf.Clamp(x, 12, 根.rect.width - 天帝道纹详情卡.宽度 - 12);
        float 上 = -点.y + 20;
        if (上 + 详情.高度 > 根.rect.height - 12) 上 = -点.y - 详情.高度 - 20;
        上 = Mathf.Clamp(上, 12, 根.rect.height - 详情.高度 - 12);
        var 浮窗 = (RectTransform)详情.transform;
        浮窗.anchoredPosition = new Vector2(x, -上);
        浮窗.SetAsLastSibling(); 详情.gameObject.SetActive(true);
    }
    public void OnPointerEnter(PointerEventData e) { if (!天帝移动适配.启用) 显示(e.position); }
    public void OnPointerMove(PointerEventData e) { if (!天帝移动适配.启用) 显示(e.position); }
    public void OnPointerExit(PointerEventData e) { if (!天帝移动适配.启用) 隐藏(); }
    public void OnPointerClick(PointerEventData e) { if (天帝移动适配.启用 && e.button == PointerEventData.InputButton.Left) { if (详情.gameObject.activeSelf) 隐藏(); else 显示(e.position); } }
    void OnDisable() => 隐藏();
    void 隐藏() { if (详情 != null) 详情.gameObject.SetActive(false); }
}

public sealed class 天帝序章进度条 : Slider
{
    public bool 调整中 { get; private set; }
    public override void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
        调整中 = true; base.OnPointerDown(e);
    }
    public override void OnPointerUp(PointerEventData e)
    { base.OnPointerUp(e); if (e.button == PointerEventData.InputButton.Left) 调整中 = false; }
    protected override void OnDisable() { 调整中 = false; base.OnDisable(); }
}
