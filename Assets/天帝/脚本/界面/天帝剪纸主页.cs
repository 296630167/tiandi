using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 独立山水背景、人物、纸框和双态导航；数值由正式模型提供。
public partial class 天帝界面
{
    RectTransform 剪纸主页根;
    readonly Dictionary<string, 天帝剪纸主页数值> 剪纸数值 = new Dictionary<string, 天帝剪纸主页数值>();
    float 剪纸刷新等待;
    Text 剪纸修行提示;

    RectTransform 剪纸区(RectTransform 父, string 名, float 左, float 上, float 宽, float 高)
    {
        var 区 = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        区.SetParent(父, false);
        天帝双端页面布局.固定(区, 左, 上, 宽, 高);
        return 区;
    }
    Image 剪纸图(RectTransform 父, string 名, float 左, float 上, float 宽, float 高)
    {
        var 区 = 剪纸区(父, 名, 左, 上, 宽, 高);
        var 像 = 区.gameObject.AddComponent<Image>();
        像.sprite = 天帝首两页山水素材.获取(名); 像.raycastTarget = false;
        像.type = Image.Type.Simple; 像.color = Color.white;
        return 像;
    }
    Button 剪纸键(RectTransform 父, string 名, string 素材, float 左, float 上, float 宽, float 高, Action 点击)
    {
        var 像 = 剪纸图(父, 素材, 左, 上, 宽, 高); 像.name = 名; 像.raycastTarget = true;
        var 键 = 像.gameObject.AddComponent<Button>(); 键.targetGraphic = 像;
        var 色 = 键.colors;
        色.normalColor = 色.highlightedColor = Color.white;
        色.pressedColor = new Color(.84f, .84f, .80f, 1);
        // 弹窗开启时保留底图亮度，由弹层统一遮罩；不对固定文字再做灰化。
        色.disabledColor = Color.white; 色.fadeDuration = .08f; 键.colors = 色;
        键.onClick.AddListener(() => { 关闭等级下拉(); 点击(); });
        天帝按钮声音.绑定(键);
        return 键;
    }
    Text 剪纸字(RectTransform 父, string 名, string 内容, float 左, float 上, float 宽, float 高, int 字号, Color 色)
    {
        var 文 = 剪纸区(父, 名, 左, 上, 宽, 高).gameObject.AddComponent<Text>();
        文.font = 游戏.默认字体;
        文.text = 内容; 文.fontSize = 字号; 文.color = 色; 文.raycastTarget = false;
        文.alignment = TextAnchor.MiddleLeft; 文.supportRichText = false;
        文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Truncate;
        return 文;
    }
    void 剪纸数字(string 名, float 左, float 上, float 宽, float 高, int 字号, bool 浅 = false, bool 靠右 = true, bool 居中 = false)
    {
        var 区 = 剪纸区(剪纸主页根, "数值" + 名, 左, 上, 宽, 高);
        var 文区 = 剪纸区(区, 名 + "实时值", 0, 0, 宽, 高);
        var 文 = 文区.gameObject.AddComponent<Text>();
        文.font = 游戏.默认字体;
        文.fontStyle = FontStyle.Normal; 文.fontSize = 字号; 文.raycastTarget = false;
        文.color = 浅 ? new Color32(246, 230, 192, 255) : new Color32(18, 55, 47, 255);
        文.alignment = 居中 ? TextAnchor.MiddleCenter : 靠右 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        文.horizontalOverflow = HorizontalWrapMode.Overflow; 文.verticalOverflow = VerticalWrapMode.Overflow;
        文.supportRichText = false;
        剪纸数值.Add(名, new 天帝剪纸主页数值(文, 宽, 字号, 靠右, 名 == "灵石" ? 92 : 名 == "地图等级" ? 宽 : 浅 ? 52 : 220));
    }
    void 剪纸导航(RectTransform 父, string 名, int 序, Action 点击)
    {
        var 像 = 剪纸图(父, "暖纸按钮", 25, 237 + 101 * 序, 382, 89);
        像.name = 名; 像.raycastTarget = true;
        var 键 = 像.gameObject.AddComponent<天帝剪纸导航按钮>(); 键.targetGraphic = 像;
        键.轻纸样式 = true;
        天帝首两页山水素材.轻按钮(键);
        键.标签 = 剪纸字((RectTransform)键.transform, "导航文字", 名, 136, 0, 213, 89, 29, new Color32(18,55,47,255));
        var 图标 = 剪纸图((RectTransform)键.transform, "导航_" + 名, 49, 10, 70, 68);
        图标.preserveAspect = true;
        键.图标=图标;
        键.刷新文字();
        键.onClick.AddListener(() => { 关闭等级下拉(); 点击(); });
        天帝按钮声音.绑定(键);
    }
    void 显示剪纸主页()
    {
        if (页面 == null) return;
        换页();
        if (页面背景 != null) 页面背景.enabled = false;
        剪纸主页根 = 剪纸区(页面, "剪纸主页1920原画坐标", 0, 0, 1920, 1080);
        剪纸主页根.localScale = Vector3.one * (天帝移动适配.布局尺寸.x / 1920f);
        剪纸图(剪纸主页根, "主页背景", 0, 0, 1920, 1080);
        var 人物=剪纸图(剪纸主页根,"主页人物",785,137,322,809);人物.preserveAspect=true;
        var 标题=剪纸图(剪纸主页根,"天帝题字",36,16,364,204);标题.name="天帝标题";标题.preserveAspect=true;
        主页纸面("资源纸面",1410,20,300,96,true);
        剪纸字(剪纸主页根,"灵石标签","灵石",1470,34,108,58,29,new Color32(246,230,192,255));
        var 卷=剪纸图(剪纸主页根,"主页信息卷",1252,122,630,893);卷.type=Image.Type.Sliced;卷.pixelsPerUnitMultiplier=2.5f;
        string[] 属性={"等级","生命","攻击","灵力"};
        for(int i=0;i<属性.Length;i++)
        {
            var 图标=剪纸图(剪纸主页根,"属性_"+属性[i],1324,203+i*57,44,44);图标.preserveAspect=true;
            剪纸字(剪纸主页根,"属性标签"+属性[i],属性[i],1396,203+i*57,180,44,29,天帝剪纸界面皮肤.墨);
        }
        var 地图题=剪纸字(剪纸主页根,"历练地图标题","选择历练地图",1302,432,508,52,29,天帝剪纸界面皮肤.墨);地图题.alignment=TextAnchor.MiddleCenter;
        string[] 名称 = { "角色", "道纹", "道纹改造", "道纹回收", "道纹图鉴", "宝盒", "作弊码" };
        Action[] 点击 = { 显示角色, 游戏.打开道纹, 游戏.打开道纹改造, 显示回收, 显示图鉴, 显示宝盒, 显示作弊码 };
        for (int 序 = 0; 序 < 名称.Length; 序++)
            剪纸导航(剪纸主页根, 名称[序], 序, 点击[序]);
        var 设置=按钮(剪纸主页根,"设置",1714,29,174,78,显示设置);
        var 设置文 = 设置 != null ? 设置.GetComponentInChildren<Text>() : null;
        if (设置文 != null) 设置文.fontSize=30;
        var 指引=按钮(剪纸主页根,"修行指引",1110,29,270,78,显示修行指引);
        var 指引文 = 指引 != null ? 指引.GetComponentInChildren<Text>() : null;
        if (指引文 != null) 指引文.fontSize=30;
        天帝首两页山水素材.轻按钮(设置);天帝首两页山水素材.轻按钮(指引);
        foreach(var 键 in new[]{设置,指引})
        {
            if (键 == null) continue;
            var 文=键.GetComponentInChildren<Text>();
            if (文 == null) continue;
            文.resizeTextForBestFit=false;文.font=游戏.默认字体;文.fontStyle=FontStyle.Normal;文.fontSize=26;文.color=天帝剪纸界面皮肤.墨;
            天帝双端页面布局.固定(文.rectTransform,20,0,((RectTransform)键.transform).rect.width-40,78);文.alignment=TextAnchor.MiddleCenter;
        }
        var 青岚=剪纸键(剪纸主页根, "青岚原", "地图_青岚原", 1302, 492, 153, 222, () =>
        {
            if (游戏.选择战斗地图(0)) 显示剪纸地图详情();
        });
        // 未开放的地图只保留图画和锁定标记，不创建可选择按钮。
        剪纸图(剪纸主页根, "地图_迷雾山脉", 1484, 492, 151, 222);
        剪纸图(剪纸主页根, "地图_幽冥谷", 1660, 492, 150, 222);
        string[] 地图名={"青岚原","迷雾山脉","幽冥谷"};
        for(int i=0;i<3;i++)
        {
            var 名=剪纸字(剪纸主页根,"地图名称"+i,地图名[i],1302+i*179,672,151,40,26,new Color32(246,230,192,255));名.alignment=TextAnchor.MiddleCenter;
            var 标记图=剪纸图(剪纸主页根,i==0?"地图选中":"地图锁定",i==0?1413:1537+(i-1)*179,i==0?495:574,i==0?36:42,i==0?36:43);标记图.preserveAspect=true;
            if(i>0){var 标记=剪纸字(剪纸主页根,"地图状态"+i,"锁定",1302+i*179,616,151,31,22,new Color32(246,230,192,255));标记.alignment=TextAnchor.MiddleCenter;}
        }
        var 开始=剪纸键(剪纸主页根, "开始历练", "墨绿按钮", 1376, 832, 360, 77, 请求进入地图);
        var 开始字=剪纸字((RectTransform)开始.transform,"开始文字","开始历练",32,0,296,77,36,new Color32(246,230,192,255));开始字.alignment=TextAnchor.MiddleCenter;开始字.font=游戏.美术?.主页标题字体??游戏.默认字体;
        var 提示 = 剪纸字(剪纸主页根, "地图详情提示", "", 1302, 939, 508, 36, 18, new Color32(69,91,76,255));
        提示.alignment = TextAnchor.MiddleCenter;
        剪纸修行提示 = 提示;
        建立剪纸等级下拉();
        // 数值行按同一基线扩展到 54px，给默认字体的真实字面高度留出上下安全边距。
        // 行中心保持与左侧标签一致，避免长数值在高分辨率和移动端缩放时被裁切。
        剪纸数字("灵石", 1584, 37, 82, 52, 32, true, false);
        剪纸数字("等级", 1714, 198, 50, 54, 32);
        剪纸数字("生命", 1692, 255, 72, 54, 32);
        剪纸数字("攻击", 1714, 312, 50, 54, 32);
        剪纸数字("灵力", 1714, 369, 50, 54, 32);
        剪纸数字("地图等级", 1544, 744, 192, 57, 30, false, false, true);
        刷新剪纸数值();
    }
    void 显示剪纸地图详情()
    {
        string 正文 = "目标：击败青岚狼王，保留本局拾取。\n\n" + 天帝地图挑战.进入说明(游戏.当前地图等级)
            + "\n\n掉落物品等级随地图等级提升。";
        显示确认("青岚原 · 地图详情", 正文, "知道了", () => { }, 660);
        foreach (var 文 in 弹层.GetComponentsInChildren<Text>())
            if (文.text == 正文)
            {
                文.fontSize = 天帝移动适配.启用 ? 16 : 18;
                文.alignment = TextAnchor.UpperLeft; 文.lineSpacing = 1.05f;
            }
    }
    void 建立剪纸等级下拉()
    {
        var 等级标签=剪纸字(剪纸主页根,"地图等级标签","地图等级",1376,744,152,57,26,天帝剪纸界面皮肤.墨);等级标签.alignment=TextAnchor.MiddleCenter;
        地图等级下拉 = 创建等级下拉(剪纸主页根, 1544, 744, 192, 57);
        if (地图等级下拉 == null) return;
        天帝双端页面布局.固定((RectTransform)地图等级下拉.transform, 1544, 744, 192, 57);
        var 底 = (Image)地图等级下拉.targetGraphic;
        天帝首两页山水素材.轻纸(底);
        var 色 = 地图等级下拉.colors;
        色.normalColor = 色.disabledColor = Color.white;
        色.highlightedColor = new Color(1.12f,1.12f,1.05f); 色.pressedColor = new Color(.84f,.84f,.80f);
        地图等级下拉.colors = 色;
        foreach (Transform 子 in 地图等级下拉.transform)
            if (子.GetComponent<Text>() is Text 文) 文.enabled = false;
        var 按钮文字区域 = 地图等级下拉.GetComponent<天帝按钮文字区域>();
        if (按钮文字区域 != null) 按钮文字区域.enabled = false;
        var 箭头 = 剪纸字((RectTransform)地图等级下拉.transform, "等级展开箭头", "▾", 154, 0, 30, 57, 26, 天帝剪纸界面皮肤.墨);
        箭头.alignment = TextAnchor.MiddleCenter;
        var 菜单 = 地图等级下拉.template;
        if (菜单 == null) return;
        菜单.anchorMin = 菜单.anchorMax = new Vector2(1,1); 菜单.pivot = new Vector2(1,0);
        菜单.anchoredPosition = new Vector2(0,8); 菜单.sizeDelta = new Vector2(360,474);
        var 纸框 = 菜单.GetComponent<Image>(); 天帝首两页山水素材.轻纸(纸框);
        var 旧皮肤 = 菜单.Find("等级菜单素材"); if (旧皮肤 != null) 旧皮肤.gameObject.SetActive(false);
        剪纸字(菜单,"等级菜单标题","选择地图等级",30,20,280,48,30,new Color32(18,55,47,255));
        剪纸字(菜单,"等级掉落提示","掉落等级随地图等级提升",30,64,290,34,22,new Color32(69,91,76,255));
        var 口 = (RectTransform)菜单.Find("Viewport");
        if (口 == null) return;
        口.offsetMin = new Vector2(22,22); 口.offsetMax = new Vector2(-32,-108);
        var 项 = (RectTransform)口.Find("Content/Item");
        if (项 == null) return;
        var 项图 = 项.GetComponent<Image>(); if (项图 != null) 项图.color = new Color(1,1,1,.18f);
        var 项勾选 = 项.GetComponent<Toggle>();
        if (项勾选 != null)
        {
            var 项色 = 项勾选.colors;
            项色.normalColor = Color.white; 项色.highlightedColor = new Color(.76f,.87f,.80f); 项勾选.colors = 项色;
        }
        var 勾 = 项.Find("选中高亮")?.GetComponent<Image>();
        if (勾 != null) 勾.color = new Color(.70f,.18f,.13f,.19f);
        if (地图等级下拉.itemText != null)
        {
            地图等级下拉.itemText.font = 游戏.默认字体;
            地图等级下拉.itemText.color = new Color32(18,55,47,255); 地图等级下拉.itemText.fontSize = 30;
            地图等级下拉.itemText.alignment = TextAnchor.MiddleLeft;
            地图等级下拉.itemText.rectTransform.offsetMin = new Vector2(24,0);
        }
        var 滑 = (RectTransform)菜单.Find("Scrollbar");
        if (滑 == null) return;
        滑.offsetMin = new Vector2(-24,24); 滑.offsetMax = new Vector2(-16,-110);
        var 滑图 = 滑.GetComponent<Image>(); if (滑图 != null) 滑图.color = new Color(.18f,.27f,.23f,.16f);
        var 滑柄图 = 滑.Find("Handle")?.GetComponent<Image>(); if (滑柄图 != null) 滑柄图.color = new Color32(156,62,45,255);
        地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1); 地图等级下拉.RefreshShownValue();
        地图等级下拉.onValueChanged.AddListener(序 =>
        {
            if (!游戏.选择地图等级(序 + 1)) 地图等级下拉.SetValueWithoutNotify(游戏.当前地图等级 - 1);
            刷新剪纸数值();
        });
    }
    void 主页纸面(string 名,float x,float y,float w,float h,bool 深=false)
    {
        if (剪纸主页根 == null) return;
        var 像=剪纸区(剪纸主页根,名,x,y,w,h).gameObject.AddComponent<Image>();
        像.sprite=天帝首两页山水素材.获取(深?"墨绿按钮":"材料面板");像.type=Image.Type.Simple;
        像.pixelsPerUnitMultiplier=深?1:2;像.raycastTarget=false;
    }
    void 刷新剪纸数值()
    {
        if (剪纸主页根 == null) return;
        var 人 = 游戏.主角属性; var 盒 = 游戏.宝盒数据;
        if (剪纸数值.TryGetValue("灵石", out var 灵石)) 灵石.显示(盒?.灵石显示 ?? "0");
        if (剪纸数值.TryGetValue("等级", out var 等级)) 等级.显示((人?.等级 ?? 游戏.道纹数据?.玩家等级 ?? 1).ToString());
        if (剪纸数值.TryGetValue("生命", out var 生命)) 生命.显示(剪纸数值文(人?.血量 ?? 0));
        if (剪纸数值.TryGetValue("攻击", out var 攻击)) 攻击.显示(剪纸数值文(人?.攻击力 ?? 0));
        if (剪纸数值.TryGetValue("灵力", out var 灵力)) 灵力.显示(剪纸数值文(人?.灵力 ?? 0));
        if (剪纸数值.TryGetValue("地图等级", out var 地图等级)) 地图等级.显示(游戏.当前地图等级.ToString());
        if (剪纸修行提示 != null) 剪纸修行提示.text = 天帝修行指引.下一步(游戏.道纹数据, 盒, 游戏.当前地图等级);
    }
    static string 剪纸数值文(float 值) => 值.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    void 更新剪纸主页()
    {
        if (剪纸主页根 == null) return;
        var 比例 = Vector3.one * (天帝移动适配.布局尺寸.x / 1920f);
        if (剪纸主页根.localScale != 比例) 剪纸主页根.localScale = 比例;
        剪纸刷新等待 -= Time.unscaledDeltaTime;
        if (剪纸刷新等待 > 0) return;
        剪纸刷新等待 = .1f; 刷新剪纸数值();
    }
    void 清理剪纸主页() { 剪纸主页根 = null; 剪纸修行提示 = null; 剪纸数值.Clear(); 剪纸刷新等待 = 0; }
}

// 实时Text保留真实语义和值。
sealed class 天帝剪纸主页数值
{
    readonly Text 文字;
    readonly float 原宽, 最大宽;
    readonly int 字号;
    readonly bool 靠右;
    string 上次;
    public 天帝剪纸主页数值(Text 文, float 宽, int 大小, bool 右, float 上限)
    { 文字 = 文; 原宽 = 宽; 字号 = 大小; 靠右 = 右; 最大宽 = 上限; }
    public void 显示(string 值)
    {
        if (文字 == null) return;
        if (上次 == 值) return;
        上次 = 值; 文字.text = 值;
        文字.fontSize = 字号;
        float 宽 = Mathf.Min(最大宽, Mathf.Max(原宽, 文字.preferredWidth + 4));
        var 区 = 文字.rectTransform;
        区.anchoredPosition = new Vector2(靠右 ? 原宽 - 宽 : 0, 0); 区.sizeDelta = new Vector2(宽, 区.sizeDelta.y);
        // 只有超过资源栏预留宽度的余额缩字，常规值固定原字号。
        文字.resizeTextForBestFit = 文字.preferredWidth > 最大宽;
        文字.resizeTextMaxSize = 字号; 文字.resizeTextMinSize = 14;
        文字.horizontalOverflow = HorizontalWrapMode.Wrap;
    }
}
