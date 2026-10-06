using UnityEngine;
using UnityEngine.UI;

// 共用的道纹悬停详情，信息按装备名称、接口、词条和附加信息分层。
public sealed class 天帝道纹详情卡 : MonoBehaviour
{
    public const float 宽度 = 460;
    static readonly Color 背景 = new Color(.035f, .065f, .072f, .97f);
    static readonly Color 正文 = 天帝道纹美术.正文;
    static readonly Color 次文 = 天帝道纹美术.次文;
    static readonly Color 暗文 = new Color(.45f, .49f, .45f);
    static readonly Color 分隔色 = new Color(.74f, .79f, .72f);

    Font 字体;
    RectTransform 根;
    RectTransform 触屏外框;
    float 完整高度;
    readonly System.Collections.Generic.List<(RectTransform 区, float x, float 宽, float 父宽)> 触屏行 = new System.Collections.Generic.List<(RectTransform, float, float, float)>();
    Outline 边框;
    Image 顶线;
    天帝道纹绘图 图标;
    Text 名称, 品阶, 类型, 接口数量, 词条标题, 特殊内容, 介绍内容, 状态, 图标短名;
    readonly Image[] 词条底 = new Image[6];
    readonly Text[] 词条名 = new Text[6], 词条值 = new Text[6];
    RectTransform 特殊区, 介绍区, 状态区, 解封区;
    Text 解封说明;
    public float 高度 { get; private set; }
    public 道纹实例 当前道纹 { get; private set; }
    int 上次签名;
    string 上次状态;
    bool 构筑皮肤;

    public void 初始化(Font 默认字体, bool 使用构筑美术 = false)
    {
        字体 = 默认字体; 根 = (RectTransform)transform; 构筑皮肤 = 天帝道纹美术.已接入;
        天帝响应布局.动态(根);
        根.anchorMin = 根.anchorMax = 根.pivot = new Vector2(0, 1);
        if (天帝移动适配.启用)
        {
            触屏外框 = 根;
            根 = new GameObject("详情滚动正文", typeof(RectTransform)).GetComponent<RectTransform>(); 根.SetParent(触屏外框, false);
            根.anchorMin = 根.anchorMax = 根.pivot = new Vector2(0, 1); 根.sizeDelta = new Vector2(宽度, 440);
            var 命中 = 触屏外框.gameObject.AddComponent<Image>(); 命中.color = 背景; 命中.raycastTarget = true;
            触屏外框.gameObject.AddComponent<RectMask2D>();
            var 滚 = 触屏外框.gameObject.AddComponent<ScrollRect>(); 滚.viewport = 触屏外框; 滚.content = 根;
            滚.horizontal = false; 滚.vertical = true; 滚.movementType = ScrollRect.MovementType.Clamped; 滚.scrollSensitivity = 32;
        }
        var 底 = 根.gameObject.AddComponent<Image>(); 底.color = 背景; 底.raycastTarget = false;
        边框 = gameObject.AddComponent<Outline>(); 边框.effectDistance = new Vector2(2, -2); 边框.useGraphicAlpha = false;
        if (构筑皮肤) { 天帝道纹美术.应用(底, "详情框"); 边框.enabled = false; }
        var 透传 = gameObject.AddComponent<CanvasGroup>(); 透传.blocksRaycasts = 透传.interactable = 天帝移动适配.启用;
        顶线 = 图(根, "品阶顶线", 0, 0, 宽度, 5, Color.white);
        var 图区 = 区(根, "道纹图标", 18, 17, 74, 70);
        图标 = 图区.gameObject.AddComponent<天帝道纹绘图>();
        图标.单纹模式 = true; 图标.单纹半径 = 29; 图标.raycastTarget = false;
        图标.构筑美术 = 构筑皮肤;
        图标短名=字(图区,"图标短名",7,8,60,54,22,正文);图标短名.alignment=TextAnchor.MiddleCenter;图标短名.fontStyle=FontStyle.Bold;
        名称 = 字(根, "道纹名称", 106, 16, 330, 38, 27, 正文);
        名称.resizeTextForBestFit = true; 名称.resizeTextMinSize = 20; 名称.resizeTextMaxSize = 24;
        品阶 = 字(根, "品阶", 106, 57, 184, 28, 19, 正文);
        类型 = 字(根, "类型", 302, 58, 134, 26, 16, 次文); 类型.alignment = TextAnchor.MiddleRight;
        图(根, "名称分隔", 18, 96, 424, 1, 分隔色);

        接口数量 = 字(根, "接口数量", 20, 106, 420, 28, 18, 次文);
        图(根, "接口分隔", 18, 143, 424, 1, 分隔色);
        词条标题 = 字(根, "道纹词条", 20, 155, 420, 26, 17, 次文);
        for (int i = 0; i < 6; i++)
        {
            词条底[i] = 图(根, "词条位-" + (i + 1), 20, 185 + i * 28, 420, 27,
                i % 2 == 0 ? 天帝道纹美术.行底 : new Color(.98f, .96f, .87f));
            var 行 = 词条底[i].rectTransform;
            字(行, (i + 1).ToString("00"), 9, 0, 42, 27, 14, 暗文);
            词条名[i] = 字(行, "", 56, 0, 354, 27, 17, 正文);
            词条值[i] = 字(行, "", 328, 0, 82, 27, 17, 正文); 词条值[i].alignment = TextAnchor.MiddleRight;
        }
        特殊区 = 区(根, "特殊效果", 20, 0, 420, 55);
        字(特殊区, "特殊效果", 0, 0, 160, 22, 16, 次文);
        特殊内容 = 字(特殊区, "", 0, 23, 420, 31, 18, 正文);
        介绍区 = 区(根, "道纹介绍", 20, 0, 420, 50);
        字(介绍区, "道纹介绍", 0, 0, 160, 22, 16, 次文);
        介绍内容 = 字(介绍区, "", 0, 22, 420, 28, 16, 次文);
        状态区 = 区(根, "连接状态", 20, 0, 420, 35);
        状态 = 字(状态区, "", 0, 0, 420, 35, 16, 正文);
        解封区 = 区(根, "源纹解封条件", 20, 0, 420, 125);
        解封说明 = 字(解封区, "", 0, 0, 420, 125, 15, 次文);
        解封说明.alignment = TextAnchor.UpperLeft;
        gameObject.SetActive(false);
    }

    public void 设置(道纹实例 纹, string 状态覆盖 = null)
    {
        if (纹 == null) return;
        int 签名 = 纹.接口;
        unchecked
        {
            签名 = 签名 * 31 + (int)纹.品阶;
            签名 = 签名 * 31 + 纹.等级;
            签名 = 签名 * 31 + 纹.物品等级;
            签名 = 签名 * 31 + (纹.格子.HasValue ? 纹.格子.Value.GetHashCode() : 0);
            签名 = 签名 * 31 + (纹.生效 ? 1 : 0);
            签名 = 签名 * 31 + (纹.特殊效果?.GetHashCode() ?? 0);
            签名 = 签名 * 31 + (纹.介绍?.GetHashCode() ?? 0);
            签名 = 签名 * 31 + (纹.特性状态?.GetHashCode() ?? 0);
            foreach (var 词 in 纹.词条) 签名 = (签名 * 31 + (int)词.属性) * 31 + 词.实际数值.GetHashCode();
        }
        if (当前道纹 == 纹 && 上次签名 == 签名 && 上次状态 == 状态覆盖) return;
        当前道纹 = 纹; 上次签名 = 签名; 上次状态 = 状态覆盖;
        Color 品阶色 = 纹.是天赋 ? 天帝道纹绘图.源色 : 天帝道纹品阶.边颜色(纹.品阶, 0);
        顶线.color = 构筑皮肤 ? new Color(品阶色.r,品阶色.g,品阶色.b,.5f) : 品阶色; 边框.effectColor = 品阶色 * .7f;
        名称.text = 纹.名称; 名称.color = 构筑皮肤 ? 正文 : 品阶色;
        品阶.text = (纹.是天赋 ? "源道纹" : 天帝道纹品阶.彩色品阶文字(纹.品阶)) +
            (构筑皮肤 ? "  ·  " + (纹.是源纹 ? "Lv." + 纹.等级 : "物品" + 纹.物品等级 + "级") : "");
        类型.text = 纹.是特性道纹 ? 纹.分类 + " / 不可改造" : 纹.是天赋 ? "天赋起点" : 纹.分类 == 道纹分类.分叉 ? "分叉 / 连接" : 纹.是顺序功能 ? "功能 / 顺序执行" : 纹.是功能道纹 ? "旧版功能" : "属性 / 构筑";
        图标.单纹 = 纹; 图标.SetVerticesDirty();
        图标短名.text=天帝美术资源.有道纹图标(纹)?"":纹.短名;
        int 口数 = 0;
        for (int d = 0; d < 6; d++) if (纹.有接口(d)) 口数++;
        接口数量.text = "接口  " + 口数 + " / 6";

        int 行数 = 纹.可改造词条 && !纹.是天赋 ? 纹.词条上限 : 1;
        行数 = Mathf.Clamp(行数, 1, 6);
        词条标题.text = 纹.是特性道纹 ? "固定机制 · 仅战斗掉落" : 纹.是天赋 ? "天赋道纹" : 纹.分类 == 道纹分类.分叉 ? "连接构件" : (纹.是功能道纹 ? "功能词条  " : "属性词条  ") + 纹.词条.Count + " / " + 纹.词条上限;
        for (int i = 0; i < 6; i++)
        {
            词条底[i].gameObject.SetActive(i < 行数);
            if (i >= 行数) continue;
            if (纹.是天赋)
            { 词条名[i].text = 纹.天赋?.倾向 ?? "源纹起点"; 词条名[i].color = 正文; 词条值[i].text = ""; }
            else if (纹.是特性道纹)
            {词条名[i].text=纹.短名+" · 固定相对两口";词条名[i].color=正文;词条值[i].text="";}
            else if (纹.分类 == 道纹分类.分叉)
            { 词条名[i].text = "仅负责连接与分流"; 词条名[i].color = 次文; 词条值[i].text = 口数 + "接口"; 词条值[i].color = 天帝道纹美术.纸面文字(品阶色); }
            else if (i < 纹.词条.Count)
            {
                var 词 = 纹.词条[i];
                var 分组 = 天帝道纹属性.分组(词.属性);
                词条名[i].text = 纹.是顺序功能 ? "功能 · " + 纹.功能 + " · 按链路执行" : 天帝道纹属性.分组名称(分组) + " · " + 天帝道纹属性.词条名称(词.属性) + " " + 天帝道纹属性.数值文字(词.属性, 词.实际数值);
                词条名[i].color = 分组 == 道纹属性分组.基础 ? 天帝道纹美术.成功色 :
                    分组 == 道纹属性分组.形态 ? 天帝道纹美术.强调 :
                    分组 == 道纹属性分组.普通 ? 正文 : 天帝道纹美术.金墨;
                词条值[i].text = "";
            }
            else
            { 词条名[i].text = "空词条位"; 词条名[i].color = 暗文; 词条值[i].text = "可改造"; 词条值[i].color = 暗文; }
        }

        float 词条底部 = 185 + 行数 * 28;
        特殊内容.text = 纹.是天赋 ? 纹.天赋?.效果 ?? "无" : string.IsNullOrEmpty(纹.特殊效果) ? "无" : 纹.特殊效果;
        bool 有特殊 = 特殊内容.text != "无"; 特殊区.gameObject.SetActive(有特殊);
        特殊内容.color = 特殊内容.text == "无" ? 暗文 : 天帝道纹美术.金墨;
        float 特殊高 = 有特殊 ? Mathf.Clamp(特殊内容.preferredHeight, 30, 110) : 0;
        特殊内容.rectTransform.sizeDelta = new Vector2(420, 特殊高);
        特殊区.sizeDelta = new Vector2(420, 特殊高 + 23);
        特殊区.anchoredPosition = new Vector2(20, -(词条底部 + 15));
        string 说明 = 纹.是天赋 ? 纹.天赋?.说明 : 纹.是顺序功能 ? 天帝顺序道纹.功能摘要(纹.功能) : 纹.分类 == 道纹分类.分叉 ? "接口数量固定；可旋转朝向，不可改造。" : 纹.介绍;
        介绍内容.text = string.IsNullOrEmpty(说明) || 说明 == "待补充" ? "暂无介绍" : 说明;
        bool 有介绍 = 介绍内容.text != "暂无介绍"; 介绍区.gameObject.SetActive(有介绍);
        float 说明高 = 有介绍 ? Mathf.Clamp(介绍内容.preferredHeight, 28, 110) : 0;
        介绍内容.rectTransform.sizeDelta = new Vector2(420, 说明高);
        介绍区.sizeDelta = new Vector2(420, 说明高 + 22);
        float 介绍顶 = 词条底部 + 15 + (有特殊 ? 特殊高 + 34 : 0);
        介绍区.anchoredPosition = new Vector2(20, -介绍顶);
        float 状态顶 = 介绍顶 + (有介绍 ? 说明高 + 34 : 0);
        状态区.anchoredPosition = new Vector2(20, -状态顶);
        状态.text = 纹.是特性道纹 ? 纹.特性状态 : 状态覆盖 ?? (纹.是源纹 ? "固定起点 · 不可移动、旋转或卸载" : !纹.格子.HasValue ? "未放置 · 等待接入源纹" :
            纹.生效 ? "已接入源纹 · 属性生效" : "未接入源纹 · 暂不生效");
        float 状态高 = Mathf.Max(35,状态.preferredHeight);
        状态区.sizeDelta = 状态.rectTransform.sizeDelta = new Vector2(420, 状态高);
        状态.color = 纹.生效 || 状态覆盖 != null ? 天帝道纹美术.成功色 : 次文;
        解封区.gameObject.SetActive(纹.是源纹);
        高度 = 状态顶 + 状态高 + 14;
        if (纹.是源纹)
        {
            解封区.anchoredPosition = new Vector2(20, -(状态顶 + 状态高 + 7));
            string 文 = "其余五个接口 · 每10级解封一个";
            for (int i = 1; i < 6; i++)
            {
                int d = (6 - i) % 6;
                文 += "\n" + 天帝道纹.方向名[d] + "  " + (i * 10) + "级  " + (纹.有接口(d) ? "已解封" : "封印中");
            }
            解封说明.text = 文;
            float 条件高 = Mathf.Max(125, 解封说明.preferredHeight);
            解封区.sizeDelta = 解封说明.rectTransform.sizeDelta = new Vector2(420, 条件高);
            高度 = 状态顶 + 状态高 + 7 + 条件高 + 14;
        }
        根.sizeDelta = new Vector2(宽度, 高度);
        完整高度 = 高度; 更新触屏高度();
    }
    void LateUpdate() => 更新触屏高度();
    void 更新触屏高度()
    {
        if (触屏外框 == null || !(触屏外框.parent is RectTransform 父)) return;
        高度 = Mathf.Min(完整高度, Mathf.Max(100, 父.rect.height * .72f));
        触屏外框.sizeDelta = new Vector2(Mathf.Min(宽度, Mathf.Max(160, 父.rect.width)), 高度);
        根.anchorMin = new Vector2(0, 1); 根.anchorMax = Vector2.one; 根.sizeDelta = new Vector2(0, 完整高度);
        foreach (var 行 in 触屏行)
        {
            var r = 行.区; float y = r.anchoredPosition.y;
            r.anchorMin = new Vector2(行.x / 行.父宽, 1); r.anchorMax = new Vector2((行.x + 行.宽) / 行.父宽, 1);
            r.sizeDelta = new Vector2(0, r.sizeDelta.y); r.anchoredPosition = new Vector2(0, y);
        }
        品阶.fontSize = 类型.fontSize = 14;
        品阶.rectTransform.sizeDelta = new Vector2(0, 44); 类型.rectTransform.sizeDelta = new Vector2(0, 44);
    }

    RectTransform 区(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var t = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        t.SetParent(父, false); t.anchorMin = t.anchorMax = t.pivot = new Vector2(0, 1);
        t.anchoredPosition = new Vector2(x, -y); t.sizeDelta = new Vector2(w, h);
        if (天帝移动适配.启用) 触屏行.Add((t, x, w, Mathf.Max(1, 父.rect.width)));
        return t;
    }
    Image 图(RectTransform 父, string 名, float x, float y, float w, float h, Color 色)
    {
        var image = 区(父, 名, x, y, w, h).gameObject.AddComponent<Image>();
        if (构筑皮肤 && 名.Contains("分隔")) { 天帝道纹美术.应用(image, "分割线"); image.rectTransform.sizeDelta = new Vector2(w, 5); return image; }
        if (构筑皮肤 && 名.StartsWith("词条位")) { 天帝道纹美术.应用(image, "小信息框"); image.color = new Color(1,1,1,.60f); return image; }
        image.color = 色; image.raycastTarget = false; return image;
    }
    Text 字(RectTransform 父, string 名, float x, float y, float w, float h, int 大小, Color 色)
    {
        var text = 区(父, 名, x, y, w, h).gameObject.AddComponent<Text>();
        text.font = 字体; text.fontSize = 大小; text.color = 色; text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft; text.verticalOverflow = VerticalWrapMode.Truncate;
        天帝界面美术.文字(text,名);
        天帝响应布局.字号(text);
        return text;
    }
}
