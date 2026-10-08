using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 全游戏共用的青绿岩彩语义皮肤，旧资源编号与品阶颜色保留兼容。
public static class 天帝道纹美术
{
    static Texture2D 瓷图;
    static bool 已查瓷图;
    public static Texture2D 白瓷图集
    {
        get { if (!已查瓷图) { 瓷图=Resources.Load<Texture2D>("白瓷道纹/构筑道纹图集"); 已查瓷图=true; } return 瓷图; }
    }
    public static readonly Color 正文 = new Color(.09f, .24f, .27f);
    public static readonly Color 次文 = new Color(.27f, .37f, .37f);
    public static readonly Color 纸墨 = 正文;
    public static readonly Color 纸次墨 = 次文;
    public static readonly Color 强调 = new Color(.14f, .38f, .39f);
    public static readonly Color 金墨 = new Color(.53f, .33f, .13f);
    public static readonly Color 浅字 = new Color(.95f, .91f, .78f);
    public static readonly Color 纸色 = new Color(.95f, .91f, .80f);
    public static readonly Color 行底 = new Color(.93f, .94f, .85f);
    public static readonly Color 画布石青 = new Color(.16f, .29f, .32f);
    public static readonly Color 成功色 = new Color(.16f, .41f, .30f);
    public static readonly Color 警示色 = new Color(.64f, .27f, .17f);
    // 品阶文字在通知深底上使用独立亮色，浅纸面继续沿用原品阶配色。
    static readonly string[] 深底品阶色 = { "#FFF7DC", "#8DE5B2", "#8FC7FF", "#E5B1FF", "#FFCE8E", "#FFB1AB", "#FFE68E", "#BFE9FF" };
    public static string 深底品阶文字(道纹品阶 阶) => "<color=" + 深底品阶色[Mathf.Clamp((int)阶, 0, 7)] + ">" + 阶 + "</color>";
    public static Sprite 获取(string 名) => 天帝剪纸界面皮肤.获取(名) ?? 天帝青绿皮肤.获取("DWUI_" + 名) ?? 天帝美术资源.当前?.获取("DWUI_" + 名);
    public static bool 已接入 => 获取("页面背景") != null;
    public static bool 彩绘皮肤 => 获取("主页落地阴影") != null;
    // 中央只显示一个字；完整名称仍由卡片说明与详情显示。
    public static string 单字(道纹实例 纹)
    {
        if(纹==null)return "";
        string 名=纹.短名;
        return string.IsNullOrEmpty(名)?"":名.Substring(0,1);
    }
    // 保留原有语义色相，将旧暗底用的浅字转成亮纸上的深色。
    public static Color 纸面文字(Color 原色)
    {
        if (!彩绘皮肤) return 原色;
        Color.RGBToHSV(原色, out float h, out float s, out float v);
        if (v < .48f) return 原色;
        Color 色 = s < .18f ? (v < .78f ? 次文 : 正文) : Color.HSVToRGB(h, Mathf.Max(.62f, s), Mathf.Min(.48f, v));
        色.a = 原色.a; return 色;
    }
    public static void 应用(Image 图, string 名, bool 拉伸 = true)
    {
        var 资源 = 获取(名); if (资源 == null) return;
        图.sprite = 资源; 图.type = 拉伸 && !(天帝剪纸界面皮肤.已启用&&名.Contains("按钮")) ? Image.Type.Sliced : Image.Type.Simple;
        图.color = Color.white; 图.raycastTarget = false;
        图.pixelsPerUnitMultiplier = 天帝剪纸界面皮肤.已启用 ? 1 : 彩绘皮肤 ? 2 : 1;
    }
    public static void 选中(Image 图, bool 是)
    {
        天帝界面美术.选项(图, 是);
    }
    public static void 设置按钮(Button 键)
    {
        天帝界面美术.按钮(键);
    }
    public static string 面板名(string 名)
    {
        if (名 == "道纹背景") return "页面背景";
        if (名 == "实时构筑预览") return "属性面板";
        if (名 == "画布主面板") return "一级面板";
        if (名.StartsWith("候选道纹-")) return "道纹卡槽";
        if (名.Contains("遮罩")) return null;
        if (名.Contains("演示") || 名 == "画布视口") return "画布内衬";
        if (名.Contains("方案槽") || 名.Contains("滚动") || 名.Contains("名称")) return "二级面板";
        return "详情框";
    }
}

// 卡片仅在鼠标/拖拽焦点时强调，图案不截获原有拖放输入。
public sealed class 天帝道纹卡片美术 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image 选框;
    public void OnPointerEnter(PointerEventData e) { if (选框 != null) 选框.enabled = true; }
    public void OnPointerExit(PointerEventData e) { if (选框 != null) 选框.enabled = false; }
    void OnDisable() { if (选框 != null) 选框.enabled = false; }
}
