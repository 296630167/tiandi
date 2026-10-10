using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 获批概念06、10、11、13的运行时排版。纸材、插画与可编辑的数据控件分别装配。
public partial class 天帝界面
{
    Text 山水战况右字, 山水攻击右字;
    Sprite 余素材(string 名, string 备用 = "弹窗纸框")
        => Resources.Load<Sprite>("山水剩余界面/" + 名) ?? 天帝剪纸界面皮肤.素材(备用);

    RectTransform 余窗(RectTransform 父, string 名, float x, float y, float w, float h, string 素材)
    {
        var 图像 = 图(父, 名, x, y, w, h, Color.white, 余素材(素材));
        图像.overrideSprite = null; 图像.color = Color.white;
        图像.type = Image.Type.Sliced; 图像.pixelsPerUnitMultiplier = 2;
        return 图像.rectTransform;
    }
    Text 余字(RectTransform 父, string 文案, float x, float y, float w, float h, int 大小,
        bool 标题 = false, TextAnchor 对齐 = TextAnchor.MiddleLeft, bool 次要 = false)
    {
        var 文 = 字(父, 文案, x, y, w, h, 大小, 次要 ? 天帝剪纸界面皮肤.次墨 : 天帝剪纸界面皮肤.墨);
        文.font = 标题 ? 游戏.美术?.主页标题字体 ?? 游戏.默认字体 : 游戏.默认字体;
        文.fontStyle = FontStyle.Normal; 文.color = 次要 ? 天帝剪纸界面皮肤.次墨 : 天帝剪纸界面皮肤.墨;
        文.alignment = 对齐; 文.resizeTextForBestFit = false;
        文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Overflow;
        return 文;
    }
    Button 余键(RectTransform 父, string 文案, float x, float y, float w, float h, Action 回调,
        bool 主 = false, bool 危险 = false, int 大小 = 24)
    {
        var 键 = 按钮(父, 文案, x, y, w, h, 回调, 主);
        if (主)
        {
            var 底 = 键.targetGraphic as Image;
            底.sprite = 天帝剪纸界面皮肤.素材(危险 ? "朱红按钮" : "墨绿按钮");
            底.overrideSprite = null; 底.type = Image.Type.Simple; 底.color = Color.white;
            var 区 = 键.GetComponent<天帝按钮文字区域>(); if (区 != null) 区.enabled = false;
            var 色 = 键.colors; 色.normalColor = 色.selectedColor = 色.disabledColor = Color.white;
            色.highlightedColor = new Color(1.04f, 1.04f, 1.02f); 色.pressedColor = new Color(.84f, .89f, .81f); 键.colors = 色;
            天帝首两页山水素材.按钮透明度(键);
        }
        else 天帝首两页山水素材.轻按钮(键);
        var 文 = 键.GetComponentInChildren<Text>(); 文.font = 游戏.默认字体; 文.fontStyle = FontStyle.Normal;
        文.fontSize = 大小; 文.resizeTextForBestFit = false; 文.color = 主 ? new Color32(246, 235, 204, 255) : 天帝剪纸界面皮肤.墨;
        天帝响应布局.比例(文.rectTransform, .075f, 0, .85f, 1);
        文.verticalOverflow = VerticalWrapMode.Overflow; return 键;
    }
    void 余分隔(RectTransform 父, float x, float y, float w)
    {
        var 线 = 图(父, "分组分隔", x, y, w, 1, new Color(.23f, .39f, .29f, .24f));
        线.sprite = null; 线.overrideSprite = null; 线.type = Image.Type.Simple;
    }
    void 余遮(RectTransform 父, string 名, float 透明度 = .58f)
    {
        var 遮 = 图(父, 名, 0, 0, 1600, 900, new Color(.03f, .08f, .06f, 透明度));
        遮.sprite = null; 遮.overrideSprite = null; 遮.raycastTarget = true;
    }
    void 余滚动条(RectTransform 口)
    {
        var 滚 = 口.GetComponent<ScrollRect>();
        var 条底 = 图(口, "正文滚动轨", 0, 0, 5, 100, new Color(.23f, .39f, .29f, .13f));
        条底.sprite = null; 条底.overrideSprite = null;
        天帝响应布局.比例(条底.rectTransform, .99f, .02f, .01f, .96f);
        var 柄 = 铺满(条底.rectTransform, "正文滚动滑块"); var 柄图 = 柄.gameObject.AddComponent<Image>(); 柄图.color = new Color(.23f, .39f, .29f, .65f);
        var 条 = 条底.gameObject.AddComponent<Scrollbar>(); 条.direction = Scrollbar.Direction.BottomToTop; 条.handleRect = 柄; 条.targetGraphic = 柄图;
        滚.verticalScrollbar = 条; 滚.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    void 余手机字(Text 文, float 左, float 上, float 宽, float 高, int 大小 = 14)
    {
        if (文 == null) return;
        天帝双端页面布局.固定(文.rectTransform, 左, 上, 宽, 高);
        // 640x360 逻辑画布会以约 2 倍映射到常见 1280x720 手机截图；
        // 弹窗沿用桌面字号会让标题和正文变成两倍视觉重量，先压到手机阅读档。
        文.fontSize = Mathf.Clamp(Mathf.RoundToInt(大小 * .72f), 11, 18);
        文.resizeTextForBestFit = false;
        文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void 显示山水宝盒()
    {
        if (游戏.阶段 != 游戏阶段.主页 || 游戏.宝盒数据 == null || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        if (!ReferenceEquals(日志所属宝盒, 游戏.宝盒数据)) { 宝盒日志.Clear(); 日志所属宝盒 = 游戏.宝盒数据; }
        宝盒已打开 = true;
        foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        var 底 = 图(弹层, "宝盒山水背景", 0, 0, 1600, 900, Color.white, 余素材("宝盒背景", "宝盒背景")); 底.raycastTarget = true;
        var 框 = 区块(弹层, "宝盒面板", 0, 0, 1600, 900);
        var 标题 = 余字(框, "道纹宝盒", 148, 33, 600, 72, 48, true);
        var 提示 = 余字(框, "击败敌人获得灵石；每次抽取一枚道纹，直接进入道纹背包。", 150, 107, 1130, 34, 21, 次要: true);
        var 余额底 = 图(框, "宝盒灵石纸签", 1124, 40, 240, 60, Color.white, 天帝剪纸界面皮肤.素材("墨绿按钮"));
        var 余额 = 余字(余额底.rectTransform, "", 14, 0, 212, 60, 26, 对齐: TextAnchor.MiddleCenter);
        余额.color = new Color32(246, 235, 204, 255); 余额.fontStyle = FontStyle.Bold;
        var 关闭键 = 余键(框, "关闭", 1382, 40, 156, 60, 关闭宝盒);
        var 分组标题 = 余字(框, "选择宝盒，抽取一枚道纹", 150, 164, 900, 37, 27, true);
        var 分组说明 = 余字(框, "每次获得一枚 · 自动进入道纹背包", 1030, 164, 430, 37, 18, 对齐: TextAnchor.MiddleRight, 次要: true);
        余分隔(框, 150, 211, 1305);

        var 日志框 = 余窗(框, "抽取日志", 142, 641, 1320, 211, "红叶弹窗");
        var 日志标题 = 余字(日志框, "最近抽取", 34, 15, 250, 36, 23, true);
        var 最近字 = 余字(日志框, "暂无抽取记录", 316, 15, 965, 36, 21, 对齐: TextAnchor.MiddleRight, 次要: true);
        最近字.name = "宝盒结果标题";
        var 视口 = 图(日志框, "日志视口", 32, 62, 1254, 118, Color.clear).rectTransform;
        视口.GetComponent<Image>().sprite = null; 视口.GetComponent<Image>().raycastTarget = true; 视口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 区块(视口, "日志内容", 0, 0, 1254, 118); 天帝响应布局.动态(内容);
        内容.anchorMin = new Vector2(0, 1); 内容.anchorMax = Vector2.one; 内容.pivot = new Vector2(0, 1); 内容.sizeDelta = new Vector2(0, 118);
        var 滚 = 视口.gameObject.AddComponent<ScrollRect>(); 滚.content = 内容; 滚.viewport = 视口; 滚.horizontal = false; 滚.vertical = true;
        滚.movementType = ScrollRect.MovementType.Clamped; 滚.scrollSensitivity = 36;
        var 空字 = 余字(内容, "暂无抽取记录", 20, 27, 1204, 50, 22, 对齐: TextAnchor.MiddleCenter, 次要: true);
        天帝响应布局.比例(空字.rectTransform, .02f, .25f, .96f, .5f);
        var 详情框 = 区块(框, "日志道纹详情", 0, 0, 天帝道纹详情卡.宽度, 440); 天帝响应布局.动态(详情框);
        var 详情 = 详情框.gameObject.AddComponent<天帝道纹详情卡>(); 详情.初始化(游戏.默认字体); 详情.设置数据(游戏.道纹数据);
        Action<宝盒记录, bool> 写记录 = (记录, 新增) =>
        {
            int 序 = 内容.childCount - 1; float 行高 = 天帝移动适配.启用 ? 56 : 40, 行距 = 行高 + 3;
            var 行 = 区块(内容, "抽取记录-" + 序, 0, 序 * 行距, 1254, 行高); 天帝响应布局.动态(行);
            行.anchorMin = new Vector2(0, 1); 行.anchorMax = Vector2.one; 行.pivot = new Vector2(0, 1); 行.sizeDelta = new Vector2(0, 行高);
            行.anchoredPosition = new Vector2(0, -序 * 行距);
            var 行底 = 行.gameObject.AddComponent<Image>(); 行底.color = new Color(.18f, .38f, .25f, 序 % 2 == 0 ? .035f : .07f); 行底.raycastTarget = true;
            var 纹 = 记录.道纹; int 口 = 0; for (int d = 0; d < 6; d++) if (纹.有接口(d)) 口++;
            var 消耗 = 余字(行, (游戏.宝盒数据.无限灵石 ? "无限灵石" : "消耗 " + 天帝宝盒.价格(记录.种类) + " 灵石") + " · " + 记录.种类 + "宝盒", 8, 0, 342, 40, 19);
            var 获得 = 余字(行, 天帝道纹品阶.彩色品阶文字(纹.品阶) + " · " + 纹.名称, 360, 0, 376, 40, 20);
            var 摘要 = 余字(行, 纹.候选说明 + " · " + 口 + "接口 · 等级 " + 纹.物品等级, 748, 0, 482, 40, 18, 次要: true);
            天帝响应布局.比例(消耗.rectTransform, .01f, 0, .27f, 1); 天帝响应布局.比例(获得.rectTransform, .29f, 0, .3f, 1); 天帝响应布局.比例(摘要.rectTransform, .6f, 0, .38f, 1);
            foreach (var 文 in new[] { 消耗, 获得, 摘要 }) { 文.resizeTextForBestFit = true; 文.resizeTextMinSize = 14; 文.resizeTextMaxSize = 文.fontSize; }
            var 悬停 = 行.gameObject.AddComponent<天帝宝盒日志悬停>(); 悬停.初始化(纹, 框, 详情);
            内容.sizeDelta = new Vector2(0, Mathf.Max(118, (序 + 1) * 行距)); 空字.gameObject.SetActive(false);
            最近字.color = 天帝剪纸界面皮肤.墨;
            最近字.text = "本次获得  " + 天帝道纹品阶.彩色品阶文字(纹.品阶) + " · " + 纹.名称;
            if (新增) { Canvas.ForceUpdateCanvases(); 滚.verticalNormalizedPosition = 0; }
        };
        foreach (var 记录 in 宝盒日志) 写记录(记录, false);
        if (宝盒日志.Count == 0) 最近字.text = 天帝移动适配.启用 ? "点记录查看道纹详情" : "悬停记录查看道纹详情";
        var 按键 = new Button[3]; var 卡片 = new RectTransform[3];
        var 插画组 = new RectTransform[3]; var 卡标题 = new Text[3]; var 卡说明 = new Text[3]; var 卡价格 = new Text[3]; var 概率键 = new Button[3];
        var 名称 = new[] { "属性宝盒", "功能宝盒", "分叉宝盒" };
        var 说明 = new[] { "随机属性增益 · 需接通源纹", 天帝顺序道纹.功能数量 + "种等概率 · 改变攻击形态 · 未必增加伤害", "固定普通 · 无词条 · 3–6接口" };
        Action 刷余额 = () =>
        {
            余额.text = "灵石  " + 游戏.宝盒数据.灵石显示;
            for (int i = 0; i < 按键.Length; i++) if (按键[i] != null)
            { 按键[i].interactable = 游戏.宝盒数据.灵石 >= 天帝宝盒.价格((宝盒种类)i); 天帝首两页山水素材.按钮透明度(按键[i]); }
        };
        for (int i = 0; i < 3; i++)
        {
            int 序 = i; var 卡 = 余窗(框, 名称[i], 142 + i * 448, 230, 424, 384, "红叶弹窗");
            var 插画 = 图(卡, "宝盒独立插画", 64, 19, 296, 137, Color.white, 天帝剪纸界面皮肤.素材(名称[i] + "插画")); 插画.preserveAspect = true;
            卡标题[i] = 余字(卡, 名称[i], 28, 161, 368, 48, 34, true, TextAnchor.MiddleCenter);
            // 功能宝盒说明固定为两行；给正文留出真实字高，避免 PC 下被 44px 卡片裁切。
            卡说明[i] = 余字(卡, 说明[i], 27, 211, 370, 58, 18, 对齐: TextAnchor.MiddleCenter, 次要: true);
            var 概率 = 余键(卡, "查看概率", 28, 278, 148, 38, () => 显示宝盒概率(序), 大小: 19); 概率.name = "宝盒概率-" + 序;
            var 价格 = 余字(卡, 天帝宝盒.价格((宝盒种类)i) + " 灵石 / 次", 186, 279, 208, 38, 23, 对齐: TextAnchor.MiddleRight);
            价格.fontStyle = FontStyle.Bold;
            卡片[i] = 卡; 插画组[i] = 插画.rectTransform; 卡价格[i] = 价格; 概率键[i] = 概率;
            按键[i] = 余键(卡, "抽取", 28, 328, 368, 50, () =>
            {
                if (游戏.宝盒数据.抽取((宝盒种类)序, out var 纹, out var 提示))
                {
                    天帝声音.提示("YS01_宝盒开启"); 天帝声音.提示(纹.品阶 >= 道纹品阶.稀有 ? "YS03_高阶获得" : "YS02_普通获得");
                    var 记录 = new 宝盒记录 { 种类 = (宝盒种类)序, 道纹 = 道纹快照(纹) }; 宝盒日志.Add(记录); 写记录(记录, true); 游戏.保存进度();
                }
                else { 最近字.text = "未能抽取 · " + 提示; 最近字.color = 朱; 天帝声音.提示("UI04_拒绝"); }
                刷余额();
            }, true, 大小: 27);
        }
        刷余额();
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "宝盒正文", 142, 164, 1320, 688);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            分组标题.gameObject.SetActive(false); 分组说明.gameObject.SetActive(false);
            天帝双端页面布局.子区(正文, "分组分隔").gameObject.SetActive(false);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 内宽 = 宽 - 32, 卡宽 = (内宽 - 20) / 3;
                余手机字(标题, 16, 4, 180, 44, 22);
                天帝双端页面布局.固定(余额底.rectTransform, 宽 - 320, 10, 144, 40); 余额.fontSize = 16;
                天帝双端页面布局.按键(关闭键.transform as RectTransform, 宽 - 164, 4, 148, 56);
                余手机字(提示, 16, 62, 内宽, 28, 14);
                天帝双端页面布局.固定(正文口, 16, 96, 内宽, 高 - 108);
                正文.sizeDelta = new Vector2(0, 404);
                for (int i = 0; i < 卡片.Length; i++)
                {
                    天帝双端页面布局.固定(卡片[i], i * (卡宽 + 10), 0, 卡宽, 224);
                    天帝双端页面布局.固定(插画组[i], 16, 8, 卡宽 - 32, 54);
                    余手机字(卡标题[i], 12, 64, 卡宽 - 24, 28, 20);
                    余手机字(卡说明[i], 12, 96, 卡宽 - 24, 36, 12);
                    余手机字(卡价格[i], 12, 136, 卡宽 - 24, 24, 14); 卡价格[i].alignment = TextAnchor.MiddleCenter;
                    float 半宽 = (卡宽 - 30) / 2;
                    天帝双端页面布局.按键(概率键[i].transform as RectTransform, 12, 170, 半宽, 44);
                    天帝双端页面布局.按键(按键[i].transform as RectTransform, 18 + 半宽, 170, 半宽, 44);
                }
                天帝双端页面布局.固定(日志框, 0, 236, 内宽, 168);
                余手机字(日志标题, 14, 8, 110, 28, 16);
                余手机字(最近字, 132, 8, 内宽 - 148, 32, 14);
                天帝双端页面布局.固定(视口, 14, 48, 内宽 - 28, 104);
                空字.fontSize = 14;
            });
        }
    }

    void 显示山水宝盒概率(int 序号)
    {
        if (!宝盒已打开 || 宝盒概率已打开 || 序号 < 0 || 序号 > 2) return;
        foreach (var 控件 in 弹层.GetComponentsInChildren<Selectable>()) 控件.interactable = false;
        宝盒概率层 = 区块(弹层, "宝盒概率层", 0, 0, 1600, 900); 余遮(宝盒概率层, "概率遮罩", .5f);
        var 框 = 余窗(宝盒概率层, "概率面板", 500, 144, 600, 612, "红叶弹窗");
        string[] 名 = { "属性宝盒", "功能宝盒", "分叉宝盒" };
        var 标题 = 余字(框, 名[序号] + " · 概率", 38, 70, 524, 67, 34, true, TextAnchor.MiddleCenter);
        var 左标题 = 余字(框, 序号 < 2 ? "道纹品阶" : "接口数量", 58, 168, 300, 34, 19, 次要: true);
        var 右标题 = 余字(框, "每次概率", 370, 168, 170, 34, 19, 对齐: TextAnchor.MiddleRight, 次要: true); 余分隔(框, 58, 210, 484);
        var 名称 = new StringBuilder(); var 比例 = new StringBuilder();
        if (序号 < 2)
        { var p = 天帝宝盒.品阶概率((宝盒种类)序号); for (int i = 0; i < p.Length; i++) if (p[i] > 0) { 名称.AppendLine(天帝道纹品阶.彩色品阶文字((道纹品阶)i)); 比例.AppendLine((p[i] / 100f).ToString("0.##") + "%"); } }
        else
        { var p = 天帝宝盒.分叉概率(); for (int i = 0; i < p.Length; i++) { 名称.AppendLine((i + 3) + " 个接口"); 比例.AppendLine(p[i] + "%"); } }
        var 左 = 余字(框, 名称.ToString().TrimEnd(), 58, 231, 300, 210, 23, 对齐: TextAnchor.UpperLeft); 左.name = "概率品阶列"; 左.lineSpacing = 1.2f;
        var 右 = 余字(框, 比例.ToString().TrimEnd(), 370, 231, 170, 210, 23, 对齐: TextAnchor.UpperRight); 右.name = "概率百分列"; 右.lineSpacing = 1.2f; 右.fontStyle = FontStyle.Bold;
        string 补充 = 序号 == 1 ? 天帝顺序道纹.功能数量 + "种功能等概率，各" + (100f / 天帝顺序道纹.功能数量).ToString("0.##") + "%\n齐射、分裂固定3口，连锁固定2口\n其余功能1口 / 2口各50%\n接口方向随机，每次独立抽取一枚" : "每次独立抽取一枚道纹";
        var 补充字 = 余字(框, 补充, 50, 序号 == 1 ? 290 : 449, 500, 序号 == 1 ? 184 : 58, 19, 对齐: TextAnchor.MiddleCenter, 次要: true);
        补充字.lineSpacing = 1.2f;
        var 返回键 = 余键(框, "返回宝盒", 171, 524, 258, 54, 关闭宝盒概率, 大小: 24);
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "概率正文", 50, 168, 500, 346);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 内宽 = 宽 - 64, 左宽 = 内宽 * .62f;
                余手机字(标题, 32, 8, 内宽, 44, 22);
                天帝双端页面布局.固定(正文口, 32, 64, 内宽, 高 - 138);
                余手机字(左标题, 0, 0, 左宽, 28); 余手机字(右标题, 左宽 + 12, 0, 内宽 - 左宽 - 12, 28);
                var 分隔 = 天帝双端页面布局.子区(正文, "分组分隔");
                if (分隔 != null) 天帝双端页面布局.固定(分隔, 0, 32, 内宽, 1);
                余手机字(左, 0, 40, 左宽, 1, 16); 余手机字(右, 左宽 + 12, 40, 内宽 - 左宽 - 12, 1, 16);
                float 表高 = Mathf.Max(左.preferredHeight, 右.preferredHeight) + 4;
                余手机字(左, 0, 40, 左宽, 表高, 16); 余手机字(右, 左宽 + 12, 40, 内宽 - 左宽 - 12, 表高, 16);
                余手机字(补充字, 0, 52 + 表高, 内宽, 1); 补充字.alignment = TextAnchor.UpperLeft;
                float 补充高 = 补充字.preferredHeight + 6;
                余手机字(补充字, 0, 52 + 表高, 内宽, 补充高);
                正文.sizeDelta = new Vector2(0, 64 + 表高 + 补充高);
                天帝双端页面布局.按键(返回键.transform as RectTransform, (宽 - 172) / 2, 高 - 66, 172, 56);
            });
        }
    }

    void 显示山水确认(string 标题, string 正文, string 确认文字, Action 确认, float 高度)
    {
        if (确认已打开 || 设置已打开 || 地图已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        确认已打开 = true; foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = false;
        余遮(弹层, "确认遮罩", .5f);
        bool 入图 = 确认文字 == "确认进入", 新游戏 = 确认文字 == "开始新游戏";
        float 高 = 入图 || 新游戏 ? 760 : Mathf.Clamp(高度 + 60, 520, 800);
        var 框 = 余窗(弹层, "确认面板", 450, (900 - 高) / 2, 700, 高, "绿边确认窗");
        var 标题字 = 余字(框, 标题, 45, 88, 610, 91, 入图 ? 37 : 43, true, TextAnchor.MiddleCenter);
        float 文上 = 185, 文高 = 高 - 345;
        if (入图)
        {
            var 签 = 图(框, "地图等级纸签", 59, 185, 582, 55, Color.white, 天帝剪纸界面皮肤.素材("墨绿按钮"));
            var 标 = 余字(签.rectTransform, "青岚原 · 地图等级 " + 游戏.当前地图等级, 20, 0, 542, 55, 25, 对齐: TextAnchor.MiddleCenter); 标.color = new Color32(246, 235, 204, 255);
            余字(框, "目标：击败青岚狼王，保留本局拾取。", 65, 256, 570, 38, 22);
            余字(框, "敌人类别", 65, 305, 570, 33, 22, true);
            string[] 敌 = { "普通", "精英", "头目", "BOSS" };
            for (int i = 0; i < 敌.Length; i++)
            { var 签底 = 图(框, "敌人类别-" + i, 64 + i * 147, 347, 134, 42, Color.white); 天帝首两页山水素材.轻纸(签底); 余字(签底.rectTransform, 敌[i], 8, 0, 118, 42, 20, 对齐: TextAnchor.MiddleCenter); }
            余字(框, "战斗与掉落", 65, 411, 570, 33, 22, true);
            int 行 = 正文.IndexOf('\n'); if (行 >= 0) 正文 = 正文.Substring(行 + 1);
            文上 = 451; 文高 = 154;
        }
        var 内容 = 区块(框, "确认正文", 65, 文上, 570, 文高);
        var 正文字 = 余字(内容, 正文, 0, 0, 570, 文高, 入图 ? 19 : 25, 对齐: 入图 ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter, 次要: !入图);
        正文字.lineSpacing = 1.2f;
        var 口 = 天帝响应布局.滚动正文(内容);
        余滚动条(口);
        天帝响应布局.动态(正文字.rectTransform); 正文字.rectTransform.anchorMin = new Vector2(0, 1); 正文字.rectTransform.anchorMax = Vector2.one;
        正文字.rectTransform.pivot = new Vector2(0, 1); 正文字.rectTransform.anchoredPosition = Vector2.zero;
        float 文本高 = Mathf.Max(文高, 正文字.preferredHeight + 8); 正文字.rectTransform.sizeDelta = new Vector2(0, 文本高); 内容.sizeDelta = new Vector2(0, 文本高);
        var 取消键 = 余键(框, "取消", 62, 高 - 133, 260, 60, 关闭确认, 大小: 26);
        var 确认键 = 余键(框, 确认文字, 366, 高 - 133, 270, 60, () => { 关闭确认(); 确认?.Invoke(); }, true, 入图 || 新游戏, 26);
        if (天帝移动适配.启用)
        {
            RectTransform 详情口 = 口, 详情正文 = 内容;
            Action 重排 = null;
            if (入图)
            {
                // 把地图摘要与原来的正文并为一处滚动内容，避免手机出现两层滚动。
                口.GetComponent<ScrollRect>().content = null; 内容.SetParent(框, false);
                天帝响应布局.比例(正文字.rectTransform, 0, 0, 1, 1);
                天帝双端页面布局.固定(内容, 65, 文上, 570, Mathf.Max(154, 文本高));
                口.gameObject.SetActive(false);
                var 所有 = new List<RectTransform>();
                foreach (RectTransform 子 in 框) if (子 != 标题字.rectTransform && 子 != 口 && 子.GetComponent<Button>() == null) 所有.Add(子);
                foreach (var 子 in 所有) foreach (var 文 in 子.GetComponentsInChildren<Text>()) 文.fontSize = 14;
                详情口 = 天帝双端页面布局.滚动组(框, "进入地图正文", 59, 185, 582, 440);
                详情正文 = 详情口.GetComponent<ScrollRect>().content;
                重排 = 天帝双端页面布局.重排正文(详情正文);
            }
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高度值 = 面板.rect.height, 内宽 = 宽 - 64, 半宽 = (内宽 - 16) / 2;
                余手机字(标题字, 32, 10, 内宽, 44, 22);
                天帝双端页面布局.固定(详情口, 32, 64, 内宽, 高度值 - 138);
                if (入图) 重排();
                else
                {
                    余手机字(正文字, 0, 0, 内宽, 1, 16);
                    float 正文高 = Mathf.Max(详情口.rect.height, 正文字.preferredHeight + 8);
                    余手机字(正文字, 0, 0, 内宽, 正文高, 16);
                    详情正文.sizeDelta = new Vector2(0, 正文高);
                }
                天帝双端页面布局.按键(取消键.transform as RectTransform, 32, 高度值 - 64, 半宽, 48);
                天帝双端页面布局.按键(确认键.transform as RectTransform, 48 + 半宽, 高度值 - 64, 半宽, 48);
            });
        }
        EventSystem.current?.SetSelectedGameObject(取消键.gameObject);
    }

    void 显示山水设置()
    {
        if ((游戏.阶段 != 游戏阶段.主页 && 游戏.阶段 != 游戏阶段.标题) || 设置已打开 || 地图已打开 || 确认已打开 || 角色已打开 || 图鉴已打开 || 宝盒已打开 || 回收已打开) return;
        设置已打开 = true; foreach (var 键 in 页面.GetComponentsInChildren<Button>()) 键.interactable = false;
        余遮(弹层, "设置遮罩", .38f); var 框 = 余窗(弹层, "设置面板", 450, 80, 700, 740, "红叶弹窗");
        var 标题 = 余字(框, "设置", 40, 65, 620, 75, 42, true, TextAnchor.MiddleCenter);
        余音量(框, "总音量", 126, 游戏.音量, 游戏.设置音量);
        余音量(框, "音乐", 217, 游戏.音乐音量, 游戏.设置音乐音量);
        余音量(框, "音效", 308, 游戏.音效音量, 游戏.设置音效音量);
        余音量(框, "剧情声音", 399, 游戏.剧情音量, 游戏.设置剧情音量);
        余分隔(框, 64, 479, 570);
        var 字幕区 = 区块(框, "字幕选项", 65, 500, 260, 44); var 热区 = 字幕区.gameObject.AddComponent<Image>(); 热区.color = Color.clear;
        var 勾底 = 图(字幕区, "字幕复选框", 0, 4, 36, 36, Color.white); 天帝首两页山水素材.轻纸(勾底); 勾底.raycastTarget = true;
        var 勾 = 余字(勾底.rectTransform, "✓", 0, 0, 36, 36, 26, 对齐: TextAnchor.MiddleCenter);
        var 开关 = 字幕区.gameObject.AddComponent<Toggle>(); 开关.targetGraphic = 勾底; 开关.graphic = 勾;
        开关.SetIsOnWithoutNotify(游戏.字幕开启); 开关.onValueChanged.AddListener(游戏.设置字幕);
        var 字幕字 = 余字(字幕区, "剧情字幕", 52, 0, 200, 44, 24);
        var 重看 = 余键(框, "重看序章", 368, 498, 266, 48, 游戏.重看序章, 大小: 23); 重看.interactable = 游戏.序章已解锁; 天帝首两页山水素材.按钮透明度(重看);
        Button 返回键 = null;
        if (游戏.阶段 == 游戏阶段.主页) 返回键 = 余键(框, "返回标题", 65, 572, 260, 52, 游戏.返回标题, 大小: 24);
        var 关闭键 = 余键(框, "关闭", 368, 572, 266, 52, 关闭设置, 大小: 24);
        if (!天帝移动适配.启用) 余键(框, "退出游戏", 220, 635, 260, 54, 游戏.退出游戏, true, 大小: 24);
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "设置正文", 64, 126, 574, 420);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 内宽 = 宽 - 64;
                余手机字(标题, 32, 4, 宽 - 212, 44, 22); 标题.alignment = TextAnchor.MiddleLeft;
                天帝双端页面布局.按键(关闭键.transform as RectTransform, 宽 - 180, 4, 148, 56);
                天帝双端页面布局.固定(正文口, 32, 68, 内宽, 高 - (返回键 != null ? 140 : 84));
                var 音量名 = new[] { "总音量", "音乐", "音效", "剧情声音" };
                for (int i = 0; i < 音量名.Length; i++)
                {
                    float 上 = i * 48;
                    var 标签 = 天帝双端页面布局.子区(正文, 音量名[i] + "标签").GetComponent<Text>();
                    var 数字 = 天帝双端页面布局.子区(正文, 音量名[i] + "百分比").GetComponent<Text>();
                    余手机字(标签, 0, 上, 84, 44, 16); 余手机字(数字, 内宽 - 54, 上, 54, 44);
                    天帝双端页面布局.区域(正文, 音量名[i], 92, 上, 内宽 - 158, 44);
                }
                天帝双端页面布局.区域(正文, "分组分隔", 0, 194, 内宽, 1);
                天帝双端页面布局.固定(字幕区, 0, 204, 240, 44);
                天帝双端页面布局.固定(勾底.rectTransform, 0, 8, 28, 28); 余手机字(勾, 0, 0, 28, 28, 20);
                余手机字(字幕字, 40, 0, 190, 44, 16);
                天帝双端页面布局.按键(重看.transform as RectTransform, 内宽 - 200, 204, 200, 44);
                正文.sizeDelta = new Vector2(0, 252);
                if (返回键 != null) 天帝双端页面布局.按键(返回键.transform as RectTransform, (宽 - 224) / 2, 高 - 60, 224, 48);
            });
        }
    }
    void 余音量(RectTransform 父, string 名, float y, float 值, Action<float> 设置)
    {
        var 标签 = 余字(父, 名, 72, y, 166, 54, 24); 标签.name = 名 + "标签";
        var 数字 = 余字(父, Mathf.RoundToInt(值 * 100) + "%", 548, y, 89, 54, 24, 对齐: TextAnchor.MiddleRight);
        数字.name = 名 + "百分比";
        var 区 = 区块(父, 名, 246, y, 289, 54); var 热 = 区.gameObject.AddComponent<Image>(); 热.color = Color.clear; 热.raycastTarget = true;
        var 轨 = 图(区, "音量轨道", 12, 24, 265, 5, new Color(.24f, .41f, .29f, .23f)); 轨.sprite = null; 轨.overrideSprite = null;
        var 填区 = 区块(区, "填充区", 12, 24, 265, 5);
        var 填 = 图(填区, "音量填充", 0, 0, 265, 5, 天帝剪纸界面皮肤.墨); 填.sprite = null; 填.overrideSprite = null;
        天帝响应布局.比例(填.rectTransform, 0, 0, 1, 1);
        var 柄区 = 区块(区, "手柄区", 12, 0, 265, 54); var 柄 = 图(柄区, "音量纸雕手柄", 0, 0, 31, 31, Color.white, 天帝剪纸界面皮肤.素材("音量纸雕滑块"));
        天帝响应布局.动态(柄.rectTransform); 柄.rectTransform.anchorMin = 柄.rectTransform.anchorMax = 柄.rectTransform.pivot = new Vector2(.5f, .5f);
        柄.rectTransform.anchoredPosition = Vector2.zero; 柄.preserveAspect = true; 柄.raycastTarget = true;
        var 滑 = 区.gameObject.AddComponent<Slider>(); 滑.minValue = 0; 滑.maxValue = 1; 滑.fillRect = 填.rectTransform; 滑.handleRect = 柄.rectTransform; 滑.targetGraphic = 柄;
        滑.SetValueWithoutNotify(值); 滑.onValueChanged.AddListener(v => { 设置(v); 数字.text = Mathf.RoundToInt(v * 100) + "%"; });
    }

    void 显示山水作弊码()
    {
        关闭等级下拉(); 作弊码已打开 = 确认已打开 = true;
        foreach (var 控件 in 页面.GetComponentsInChildren<Selectable>()) 控件.interactable = false;
        余遮(弹层, "作弊码遮罩", .38f); var 框 = 余窗(弹层, "作弊码面板", 450, 80, 700, 740, "红叶弹窗");
        var 标题 = 余字(框, "作弊码", 40, 65, 620, 75, 42, true, TextAnchor.MiddleCenter);
        var 提示 = 余字(框, "点击启用即可获得作弊效果", 65, 125, 570, 40, 22, 对齐: TextAnchor.MiddleCenter, 次要: true);
        var 输入标题 = 余字(框, "请输入作弊码", 67, 204, 564, 38, 24, true);
        var 输入底 = 图(框, "作弊码输入", 64, 254, 572, 65, Color.white); 天帝首两页山水素材.轻纸(输入底); 输入底.raycastTarget = true;
        var 输入 = 输入底.gameObject.AddComponent<InputField>(); 输入.targetGraphic = 输入底;
        var 文 = 余字(输入底.rectTransform, "", 22, 0, 528, 65, 26); 文.supportRichText = false;
        var 占位 = 余字(输入底.rectTransform, "请输入作弊码", 22, 0, 528, 65, 24, 次要: true);
        输入.textComponent = 文; 输入.placeholder = 占位; 输入.contentType = InputField.ContentType.Standard;
        输入.lineType = InputField.LineType.SingleLine; 输入.characterLimit = 32; 输入.customCaretColor = true;
        输入.caretColor = 天帝剪纸界面皮肤.墨; 输入.selectionColor = new Color(.35f, .7f, .58f, .35f); 输入.text = "涌现";
        var 反馈标题 = 余字(框, "反馈信息", 67, 359, 564, 38, 24, true);
        var 反馈底 = 图(框, "作弊码反馈纸面", 64, 408, 572, 146, Color.white); 天帝首两页山水素材.轻纸(反馈底);
        var 反馈 = 余字(反馈底.rectTransform, "启用后在此显示实际结果。", 24, 18, 524, 110, 23, 对齐: TextAnchor.UpperLeft, 次要: true); 反馈.name = "作弊码反馈";
        var 关闭键 = 余键(框, "关闭", 85, 606, 237, 61, 关闭作弊码, 大小: 26);
        var 启用键 = 余键(框, "启用", 378, 606, 237, 61, () =>
        {
            bool 成功 = 游戏.兑换作弊码(输入.text, out string 结果); 反馈.text = 结果; 反馈.color = 成功 ? 天帝剪纸界面皮肤.墨 : 朱;
            if (成功) { 更新主页实力(); if (主页灵石字 != null) 主页灵石字.text = "灵石  " + 游戏.宝盒数据.灵石显示; }
            else { EventSystem.current?.SetSelectedGameObject(输入.gameObject); 输入.ActivateInputField(); }
        }, true, 大小: 27);
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "作弊码正文", 64, 125, 572, 429);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            Action 更新反馈 = () =>
            {
                float 内宽 = 正文.rect.width;
                余手机字(反馈, 12, 10, 内宽 - 24, 1, 14);
                float 高 = Mathf.Max(72, 反馈.preferredHeight + 6);
                余手机字(反馈, 12, 10, 内宽 - 24, 高, 14);
                天帝双端页面布局.固定(反馈底.rectTransform, 0, 164, 内宽, 高 + 20);
                正文.sizeDelta = new Vector2(0, 188 + 高);
            };
            var 反馈排版 = 正文.gameObject.AddComponent<天帝正文排版>(); 反馈排版.更新 = 更新反馈;
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 内宽 = 宽 - 64;
                余手机字(标题, 32, 4, 宽 - 212, 44, 22); 标题.alignment = TextAnchor.MiddleLeft;
                天帝双端页面布局.按键(关闭键.transform as RectTransform, 宽 - 180, 4, 148, 56);
                天帝双端页面布局.固定(正文口, 32, 68, 内宽, 高 - 140);
                余手机字(提示, 0, 0, 内宽, 28); 提示.alignment = TextAnchor.MiddleLeft;
                余手机字(输入标题, 0, 36, 内宽, 28, 16);
                天帝双端页面布局.固定(输入底.rectTransform, 0, 72, 内宽, 48);
                余手机字(文, 12, 0, 内宽 - 24, 48, 16); 余手机字(占位, 12, 0, 内宽 - 24, 48, 16);
                余手机字(反馈标题, 0, 128, 内宽, 28, 16); 更新反馈();
                天帝双端页面布局.按键(启用键.transform as RectTransform, (宽 - 224) / 2, 高 - 60, 224, 48);
            });
        }
        EventSystem.current?.SetSelectedGameObject(启用键.gameObject);
    }

    void 显示山水暂停()
    {
        战斗已暂停 = true; 触控跑步 = false; if (战斗摇杆 != null) 战斗摇杆.gameObject.SetActive(false);
        战斗暂停层 = 铺满(弹层, "战斗暂停层");
        var 遮 = 铺满(战斗界面层, "暂停遮罩").gameObject.AddComponent<Image>(); 遮.color = new Color(.015f, .04f, .025f, .65f); 遮.raycastTarget = true; 遮.transform.SetAsLastSibling();
        var 框 = 余窗(战斗暂停层, "暂停详情", 435, 25, 730, 850, "松山弹窗");
        var 标题 = 余字(框, "战斗暂停", 54, 96, 622, 72, 46, true, TextAnchor.MiddleCenter);
        var 战况标题 = 余字(框, "当前战况", 86, 180, 558, 34, 24, true); 余分隔(框, 86, 219, 558);
        战斗详细统计 = 余字(框, "", 90, 234, 270, 126, 20, 对齐: TextAnchor.UpperLeft); 战斗详细统计.lineSpacing = 1.1f;
        山水战况右字 = 余字(框, "", 390, 234, 260, 126, 20, 对齐: TextAnchor.UpperLeft); 山水战况右字.lineSpacing = 1.1f;
        var 攻击标题 = 余字(框, "当前攻击", 86, 382, 558, 34, 24, true); 余分隔(框, 86, 421, 558);
        战斗配置字 = 余字(框, "", 90, 436, 270, 140, 20, 对齐: TextAnchor.UpperLeft); 战斗配置字.name = "暂停攻击左列"; 战斗配置字.lineSpacing = 1.1f;
        山水攻击右字 = 余字(框, "", 390, 436, 260, 140, 20, 对齐: TextAnchor.UpperLeft); 山水攻击右字.name = "暂停攻击右列"; 山水攻击右字.lineSpacing = 1.1f;
        var 收获标题 = 余字(框, "本局收获", 86, 579, 558, 34, 24, true); 余分隔(框, 86, 618, 558);
        掉落提示 = 余字(框, "", 90, 630, 558, 36, 22, 对齐: TextAnchor.MiddleCenter);
        战斗坐标 = 余字(框, "", 86, 682, 558, 27, 19, 对齐: TextAnchor.MiddleCenter, 次要: true);
        var 操作字 = 余字(框, "自动观战 · 道纹构筑展示", 78, 716, 574, 28, 16, 对齐: TextAnchor.MiddleCenter, 次要: true);
        var 继续键 = 余键(框, "继续战斗", 86, 754, 258, 53, 关闭战斗暂停, true, 大小: 26);
        var 返回键 = 余键(框, "返回主页", 386, 754, 258, 53, 游戏.返回主页, 大小: 26);
        刷新战斗暂停详情(游戏.战斗场景.战斗, 游戏.主角属性);
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(框, "暂停正文", 78, 180, 574, 564);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            foreach (Transform 子 in 正文) if (子.name == "分组分隔") 子.gameObject.SetActive(false);
            float 上次宽 = -1; string 上次内容 = null;
            Action 更新正文 = () =>
            {
                float 内宽 = 正文.rect.width, 半宽 = (内宽 - 24) / 2;
                string 内容签名 = 战斗详细统计.text + 山水战况右字.text + 战斗配置字.text + 山水攻击右字.text + 掉落提示.text + 战斗坐标.text;
                if (Mathf.Abs(内宽 - 上次宽) < .1f && 内容签名 == 上次内容) return;
                上次宽 = 内宽; 上次内容 = 内容签名;
                余手机字(战况标题, 0, 0, 内宽, 28, 16);
                余手机字(战斗详细统计, 0, 34, 半宽, 1); 余手机字(山水战况右字, 半宽 + 24, 34, 半宽, 1);
                float 战况高 = Mathf.Max(战斗详细统计.preferredHeight, 山水战况右字.preferredHeight) + 4;
                余手机字(战斗详细统计, 0, 34, 半宽, 战况高); 余手机字(山水战况右字, 半宽 + 24, 34, 半宽, 战况高);
                float 攻击上 = 46 + 战况高;
                余手机字(攻击标题, 0, 攻击上, 内宽, 28, 16);
                余手机字(战斗配置字, 0, 攻击上 + 34, 半宽, 1); 余手机字(山水攻击右字, 半宽 + 24, 攻击上 + 34, 半宽, 1);
                float 攻击高 = Mathf.Max(战斗配置字.preferredHeight, 山水攻击右字.preferredHeight) + 4;
                余手机字(战斗配置字, 0, 攻击上 + 34, 半宽, 攻击高); 余手机字(山水攻击右字, 半宽 + 24, 攻击上 + 34, 半宽, 攻击高);
                float 收获上 = 攻击上 + 46 + 攻击高;
                余手机字(收获标题, 0, 收获上, 内宽, 28, 16);
                余手机字(掉落提示, 0, 收获上 + 34, 内宽, 1);
                float 收获高 = 掉落提示.preferredHeight + 4;
                余手机字(掉落提示, 0, 收获上 + 34, 内宽, 收获高);
                float 坐标上 = 收获上 + 42 + 收获高;
                余手机字(战斗坐标, 0, 坐标上, 内宽, 24, 12);
                余手机字(操作字, 0, 坐标上 + 28, 内宽, 24, 12);
                正文.sizeDelta = new Vector2(0, 坐标上 + 56);
            };
            var 正文排版 = 正文.gameObject.AddComponent<天帝正文排版>(); 正文排版.更新 = 更新正文;
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 内宽 = 宽 - 64, 半宽 = (内宽 - 16) / 2;
                余手机字(标题, 32, 8, 内宽, 44, 22);
                天帝双端页面布局.固定(正文口, 32, 64, 内宽, 高 - 138); 更新正文();
                天帝双端页面布局.按键(继续键.transform as RectTransform, 32, 高 - 64, 半宽, 52);
                天帝双端页面布局.按键(返回键.transform as RectTransform, 48 + 半宽, 高 - 64, 半宽, 52);
            });
        }
    }
    void 显示山水失败()
    {
        关闭战斗暂停(); 清空(弹层); 触控跑步 = false; if (战斗摇杆 != null) 战斗摇杆.gameObject.SetActive(false);
        余遮(弹层, "战斗失败遮罩", .65f); var 框 = 余窗(弹层, "战斗失败面板", 490, 91, 620, 718, "松山弹窗");
        余字(框, "身陨此地", 48, 99, 524, 122, 57, true, TextAnchor.MiddleCenter);
        余字(框, "本局已拾取的道纹、通货与灵石\n均保留。", 70, 253, 480, 106, 25, 对齐: TextAnchor.MiddleCenter);
        余字(框, "回到主页调整构筑，\n下次进入时恢复生命与资源。", 70, 393, 480, 94, 23, 对齐: TextAnchor.MiddleCenter, 次要: true);
        余键(框, "返回主页", 168, 587, 284, 62, 游戏.返回主页, true, 大小: 28);
        if (天帝移动适配.启用)
        {
            var 标题 = Array.Find(框.GetComponentsInChildren<Text>(true), x => x != null && x.text == "身陨此地");
            var 保留 = Array.Find(框.GetComponentsInChildren<Text>(true), x => x != null && x.text.StartsWith("本局已拾取"));
            var 说明 = Array.Find(框.GetComponentsInChildren<Text>(true), x => x != null && x.text.StartsWith("回到主页"));
            var 返回 = 框.Find("返回主页") as RectTransform;
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                天帝双端页面布局.固定(标题?.rectTransform, 16, 14, 宽 - 32, 44);
                if (标题 != null) { 标题.fontSize = 30; 标题.alignment = TextAnchor.MiddleCenter; }
                天帝双端页面布局.固定(保留?.rectTransform, 24, 76, 宽 - 48, 64);
                if (保留 != null) { 保留.fontSize = 17; 保留.alignment = TextAnchor.MiddleCenter; }
                天帝双端页面布局.固定(说明?.rectTransform, 24, 148, 宽 - 48, 64);
                if (说明 != null) { 说明.fontSize = 16; 说明.alignment = TextAnchor.MiddleCenter; }
                天帝双端页面布局.按键(返回, 32, Mathf.Max(228, 高 - 64), 宽 - 64, 52);
            });
        }
        更新战斗目标();
    }
    void 刷新山水暂停详情(天帝战斗系统 战, 天帝主角属性 人)
    {
        战斗详细统计.text = "青岚原 · 地图等级 " + 游戏.当前地图等级 + "\n" + 战.刷新阶段
            + "\n剩余 / 总量 " + 战.剩余敌人数量 + " / " + 战.敌人.Count + "\n场上 " + 战.场上敌人数量;
        山水战况右字.text = "待刷新 " + 战.未生成敌人数量 + "\n生命 " + 人.当前血量.ToString("0.##") + " / " + 人.血量.ToString("0.##")
            + "\n灵力 " + 人.当前灵力.ToString("0.##") + " / " + 人.灵力.ToString("0.##") + "\n护盾 " + (人.当前灵气护盾 + 战.特性临时护盾).ToString("0.##");
        更新战斗位置(游戏.战斗场景.玩家位置, 游戏.战斗场景.地图.所在格(游戏.战斗场景.玩家位置));
        var 参数 = 战.当前普攻;
        战斗配置字.text = 天帝道纹.通路名称(参数.通路) + "\n参与通路数 " + 战.参与通路数
            + (参数.顺序计划 != null ? "\n功能按链路执行\n各段属性见画布" : "\n普通伤害 " + 参数.普通伤害.ToString("0.##") + "\n五行额外伤害 " + 参数.五行额外伤害.ToString("0.##"));
        山水攻击右字.text = 参数.顺序计划 != null ? "首发数量 " + 参数.数量 + "\n各出口独立攻击\n分段伤害见画布" : "单发伤害 " + 参数.伤害.ToString("0.##")
            + "\n数量 " + 参数.数量 + "\n分裂 " + 参数.分裂 + "\n连锁 " + 参数.连锁;
        掉落提示.text = "道纹 " + 战.掉落.拾取数 + " 枚   ·   通货 " + 战.通货掉落.拾取总量 + "   ·   灵石 +" + 战.灵石掉落.拾取总量;
    }
}
