using System;
using UnityEngine;
using UnityEngine.UI;

public partial class 天帝界面
{
    RectTransform 青绿区(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var r = 区块(父, 名, x, y, w, h);
        天帝双端页面布局.固定(r, x, y, w, h); return r;
    }
    Image 青绿图(RectTransform 父, string 名, string 素材, float x, float y, float w, float h, bool 切片 = true)
    {
        var r = 青绿区(父, 名, x, y, w, h); var i = r.gameObject.AddComponent<Image>();
        i.sprite = 天帝青绿皮肤.获取("QLUI_" + 素材); i.color = Color.white;
        i.type = 切片 ? Image.Type.Sliced : Image.Type.Simple; i.pixelsPerUnitMultiplier = 素材.StartsWith("精修") ? 3 : 2; i.raycastTarget = false;
        return i;
    }
    Text 青绿字(RectTransform 父, string 内容, float x, float y, float w, float h, int 大小, bool 浅 = false)
    {
        int 初始字号 = 天帝移动适配.启用 ? Mathf.RoundToInt(大小 / .72f) : 大小;
        var t = 字(父, 内容, x, y, w, h, 初始字号, 天帝道纹美术.正文);
        t.font = 游戏.默认字体; t.fontStyle = FontStyle.Normal;
        天帝双端页面布局.固定(t.rectTransform, x, y, w, h);
        t.color = 浅 ? 天帝道纹美术.浅字 : 天帝道纹美术.正文;
        t.alignment = TextAnchor.MiddleLeft; return t;
    }
    Button 青绿键(RectTransform 父, string 名, float x, float y, float w, float h, Action 点击, bool 主 = false)
    {
        var r = 青绿区(父, 名, x, y, w, h);
        var 图 = r.gameObject.AddComponent<Image>(); 图.sprite = 天帝青绿皮肤.获取("QLUI_" + (主 ? "精修浅纸按钮" : "精修深青按钮"));
        图.type = Image.Type.Sliced; 图.pixelsPerUnitMultiplier = 3; 图.raycastTarget = true;
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = 图; b.onClick.AddListener(() => 点击?.Invoke()); 天帝按钮声音.绑定(b);
        var 文 = 青绿字(r, 名, 0, 0, w, h, 天帝移动适配.启用 ? 19 : 28, !主);
        文.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体; 文.alignment = TextAnchor.MiddleCenter;
        天帝按钮文字区域.绑定(b);
        if (天帝移动适配.启用) r.gameObject.AddComponent<天帝触控热区>();
        return b;
    }
    void 显示青绿主页()
    {
        换页(); bool 移 = 天帝移动适配.启用;
        float 宽 = 移 ? 640 : 1600, 高 = 移 ? 360 : 900;
        var 标题 = 青绿字(页面, "天帝", 移 ? 14 : 44, 移 ? 0 : 4, 移 ? 112 : 260, 移 ? 46 : 88, 移 ? 30 : 64, true);
        标题.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
        var 灵石 = 青绿图(页面, "主页灵石纸面", "精修深青按钮", 移 ? 344 : 1110, 移 ? 4 : 24, 移 ? 180 : 252, 移 ? 38 : 48);
        青绿图(灵石.rectTransform, "灵石图标", "图标灵石", 移 ? 10 : 18, 6, 移 ? 25 : 40, 移 ? 25 : 42, false).preserveAspect = true;
        主页灵石字 = 青绿字(灵石.rectTransform, "灵石  " + (游戏.宝盒数据?.灵石显示 ?? "0"), 移 ? 40 : 64, 0, 移 ? 128 : 176, 移 ? 38 : 48, 移 ? 14 : 24, true);
        主页灵石字.resizeTextForBestFit = true; 主页灵石字.resizeTextMinSize = 14; 主页灵石字.resizeTextMaxSize = 移 ? 14 : 24;
        青绿键(页面, "设置", 移 ? 536 : 1384, 移 ? 3 : 24, 移 ? 92 : 180, 移 ? 40 : 48, 显示设置);

        // 立绘的轴心和脚底位置固定在画面中线；待机只换帧，不漂移中心。
        var 阴影 = 天帝道纹美术.获取("主页落地阴影");
        if (阴影 != null)
        {
            var r = 青绿区(页面, "主角落地阴影", 宽 * .5f - (移 ? 44 : 110), 高 * .80f - 6, 移 ? 88 : 220, 移 ? 14 : 35);
            var i = r.gameObject.AddComponent<Image>(); i.sprite = 阴影; i.raycastTarget = false;
        }
        var 人 = 青绿区(页面, "主角立绘", 宽 * .5f - (移 ? 80 : 215), 移 ? 54 : 70, 移 ? 160 : 430, 移 ? 246 : 706);
        var 像 = 人.gameObject.AddComponent<Image>(); 像.sprite = 游戏.主角立绘 ?? 游戏.美术?.获取("CH01"); 像.preserveAspect = true; 像.raycastTarget = false;
        主页人物 = 人;
        if (游戏.美术?.主角移动动画 != null && 游戏.美术.主角移动动画.十组完整)
        {
            主页人物图 = 像; 像.sprite = 游戏.美术.主角移动动画.正面待机(0);
            人.sizeDelta = Vector2.one * (移 ? 310 : 780); 人.pivot = new Vector2(.5f, 48f / 512);
            人.anchoredPosition = new Vector2(宽 * .5f, -高 * .80f);
        }
        var 左 = 青绿图(页面, "历练面板", "精修深青面板", 移 ? 10 : 40, 移 ? 48 : 100, 移 ? 205 : 536, 移 ? 252 : 660).rectTransform;
        var 历练 = 青绿字(左, "历练", 移 ? 20 : 30, 移 ? 0 : 16, 移 ? 172 : 476, 移 ? 30 : 64, 移 ? 20 : 42, true);
        历练.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
        // 小屏保留全部信息，说明在卡片区内滚动；等级和开始按钮始终可见。
        var 口 = 青绿区(左, "历练说明视口", 移 ? 12 : 30, 移 ? 32 : 96, 移 ? 181 : 476, 移 ? 112 : 352);
        var 口图 = 口.gameObject.AddComponent<Image>(); 口图.color = Color.clear; 口图.raycastTarget = true; 口.gameObject.AddComponent<RectMask2D>();
        float 内容高 = 移 ? 194 : 352;
        var 内容 = 青绿区(口, "历练说明内容", 0, 0, 移 ? 181 : 476, 内容高);
        var 滚 = 口.gameObject.AddComponent<ScrollRect>(); 滚.content = 内容; 滚.viewport = 口; 滚.horizontal = false; 滚.vertical = 移;
        滚.movementType = ScrollRect.MovementType.Clamped; 滚.scrollSensitivity = 28;
        float 卡宽 = 移 ? 57 : 148, 间 = 移 ? 5 : 16, 卡高 = 移 ? 66 : 178;
        for (int n = 0; n < 3; n++)
        {
            bool 开 = n == 0;
            var 卡 = 青绿区(内容, "地图卡" + (n + 1), n * (卡宽 + 间), 0, 卡宽, 卡高);
            var 图像 = 开 ? (天帝青绿皮肤.获取("QLUI_精修青岚原卡面") ?? 游戏.美术?.获取("青岚原")) : 游戏.美术?.获取(n == 1 ? "BG03" : "MAP01");
            var 画 = 青绿区(卡, "地图裁切", 2, 2, 卡宽 - 4, 卡高 - 4); 画.gameObject.AddComponent<RectMask2D>();
            var 画图 = 图(画, "地图原画", 0, 0, 卡宽 - 4, 卡高 - 4, 开 ? Color.white : new Color(.45f, .56f, .53f), 图像);
            天帝双端页面布局.固定(画图.rectTransform, 0, 0, 卡宽 - 4, 卡高 - 4);
            if (图像 != null) { var 比 = 画图.gameObject.AddComponent<AspectRatioFitter>(); 比.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; 比.aspectRatio = 图像.rect.width / 图像.rect.height; }
            if (!开)
            {
                var 遮 = 青绿区(画, "封印遮罩", 0, 0, 卡宽 - 4, 卡高 - 4); var 遮图 = 遮.gameObject.AddComponent<Image>(); 遮图.color = new Color(.03f, .13f, .15f, .75f); 遮图.raycastTarget = false;
                var 封 = 青绿字(画, "封印", 0, 移 ? 4 : 42, 卡宽 - 4, 移 ? 30 : 46, 移 ? 14 : 26, true); 封.alignment = TextAnchor.MiddleCenter;
                if (!移) { var 未 = 青绿字(画, "尚未开放", 0, 91, 卡宽 - 4, 32, 18, true); 未.alignment = TextAnchor.MiddleCenter; }
            }
            var 名底 = 青绿区(卡, "地图名称安静区", 2, 移 ? 40 : 132, 卡宽 - 4, 移 ? 24 : 44).gameObject.AddComponent<Image>();
            名底.color = new Color(.025f,.15f,.17f,.93f); 名底.raycastTarget = false;
            var 名 = 青绿字(卡, 开 ? "青岚原" : n == 1 ? (移 ? "秘境" : "未知秘境") : (移 ? "禁地" : "未知禁地"), 0, 移 ? 40 : 132, 卡宽, 移 ? 26 : 44, 移 ? 14 : 24, true); 名.alignment = TextAnchor.MiddleCenter;
            名.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
            var 框 = 青绿图(卡, "地图卡框", "精修地图卡框", 0, 0, 卡宽, 卡高);
            // 边框只覆盖外缘；九宫格四边不能吞掉地图与名称。
            框.fillCenter = false; 框.pixelsPerUnitMultiplier = 移 ? 16 : 8;
            if (开)
            {
                var 键 = 卡.gameObject.AddComponent<Button>(); 键.targetGraphic = 框; 框.raycastTarget = true; 天帝按钮声音.绑定(键);
                键.onClick.AddListener(() => 游戏.选择战斗地图(0));
                青绿图(画, "地图已选图标", "图标勾选", 卡宽 - (移 ? 23 : 38), 3, 移 ? 18 : 30, 移 ? 18 : 30, false);
                if (移) 卡.gameObject.AddComponent<天帝触控热区>();
            }
        }
        青绿图(内容, "地图说明分隔", "分割线", 0, 移 ? 70 : 186, 移 ? 181 : 476, 1, false);
        var 地名 = 青绿字(内容, "青岚原", 0, 移 ? 74 : 194, 移 ? 181 : 476, 移 ? 28 : 54, 移 ? 20 : 40, true);
        地名.gameObject.name = "当前地图名称"; 地名.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
        青绿字(内容, "击退狼群，挑战青岚狼王。", 0, 移 ? 108 : 255, 移 ? 181 : 476, 移 ? 38 : 40, 移 ? 14 : 25, true);
        青绿字(内容, "战斗与掉落详情将在进入前展示", 0, 移 ? 151 : 307, 移 ? 181 : 476, 移 ? 40 : 36, 移 ? 14 : 20, true);
        // 参考图顺序：地图说明、等级选择、掉落提示、浅纸主操作。
        if (移) 天帝双端页面布局.固定(口, 12, 32, 181, 100);
        青绿图(左, "等级分隔", "分割线", 移 ? 12 : 30, 移 ? 135 : 452, 移 ? 181 : 476, 1, false);
        青绿字(左, "地图等级", 移 ? 12 : 30, 移 ? 136 : 462, 移 ? 62 : 160, 移 ? 44 : 56, 移 ? 14 : 26, true);
        地图等级下拉 = 创建等级下拉(左, 移 ? 79 : 210, 移 ? 136 : 462, 移 ? 113 : 296, 移 ? 44 : 56);
        天帝双端页面布局.固定((RectTransform)地图等级下拉.transform, 移 ? 79 : 210, 移 ? 136 : 462, 移 ? 113 : 296, 移 ? 44 : 56);
        var 下拉底 = 地图等级下拉.GetComponent<Image>();
        下拉底.sprite = 天帝青绿皮肤.获取("QLUI_精修深青按钮"); 下拉底.pixelsPerUnitMultiplier = 3;
        地图等级下拉.captionText.fontSize = 移 ? 18 : 27;
        foreach (Transform 子 in 地图等级下拉.transform)
            if (子.GetComponent<Text>() is Text 文) 文.color = 天帝道纹美术.浅字;
        地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1); 地图等级下拉.RefreshShownValue();
        地图等级下拉.onValueChanged.AddListener(i => { if (!游戏.选择地图等级(i + 1)) { 地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1); 地图等级下拉.RefreshShownValue(); } });
        主页地图等级字 = 青绿字(左, 移 ? "掉落等级随地图等级提升" : "掉落物品等级随地图等级提升", 移 ? 12 : 30, 移 ? 180 : 526, 移 ? 181 : 476, 移 ? 24 : 36, 移 ? 14 : 20, true);
        var 开始 = 青绿键(左, "开始游戏", 移 ? 12 : 30, 移 ? 204 : 582, 移 ? 181 : 476, 移 ? 44 : 66, 请求进入地图, true);
        var 开始文 = 开始 != null ? 开始.GetComponentInChildren<Text>() : null;
        if (开始文 != null) 开始文.fontSize = 移 ? 20 : 36;
        if (移) 青绿字(左, "↕", 178, 107, 16, 24, 14, true);

        青绿图(页面, "底部渐隐导航", "底部渐隐", 0, 移 ? 294 : 708, 宽, 移 ? 66 : 192, false);
        string[] 导航名 = { "角色", "道纹", "道纹改造", "道纹图鉴", "宝盒", "道纹回收", "作弊码" };
        Action[] 点击 = { 显示角色, 游戏.打开道纹, 游戏.打开道纹改造, 显示图鉴, 显示宝盒, 显示回收, 显示作弊码 };
        float 导航宽 = 移 ? 88 : 170, 总宽 = 导航宽 * 7;
        for (int i = 0; i < 7; i++)
        {
            var r = 青绿区(页面, 导航名[i], (宽 - 总宽) * .5f + i * 导航宽, 移 ? 310 : 796, 导航宽, 移 ? 48 : 100);
            var bg = r.gameObject.AddComponent<Image>(); bg.color = Color.clear; bg.raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = bg; 天帝按钮声音.绑定(b); b.onClick.AddListener(点击[i].Invoke);
            var icon = 青绿图(r, "底栏图标-" + 导航名[i], "图标" + 导航名[i], (导航宽 - (移 ? 24 : 58)) / 2, 0, 移 ? 24 : 58, 移 ? 24 : 58, false); icon.preserveAspect = true;
            var label = 青绿字(r, 导航名[i], 2, 移 ? 26 : 62, 导航宽 - 4, 移 ? 22 : 34, 移 ? 14 : 23, true); label.alignment = TextAnchor.MiddleCenter;
            label.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
            if (移) r.gameObject.AddComponent<天帝触控热区>();
        }
        根.GetComponent<天帝响应布局>().提交();
        foreach (var s in 页面.GetComponentsInChildren<天帝按钮文字区域>()) s.更新();
    }
}
