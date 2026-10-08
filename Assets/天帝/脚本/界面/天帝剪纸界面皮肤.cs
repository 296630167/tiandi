using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 审批概念只提供纸材与插画，玩法文字和状态全部由原页面维护。
public static class 天帝剪纸界面皮肤
{
    static readonly Dictionary<string, Sprite> 缓存 = new Dictionary<string, Sprite>();
    public static Sprite 素材(string 名)
    {
        if (string.IsNullOrWhiteSpace(名)) return null;
        if (!缓存.TryGetValue(名, out var 图))
        { 图 = Resources.Load<Sprite>("剪纸界面/" + 名); if (图 != null) 缓存.Add(名,图); }
        return 图;
    }
    public static bool 已启用 => 素材("朱红纸框") != null;
    public static readonly Color 墨 = new Color32(22,55,45,255), 次墨 = new Color32(79,91,70,255);
    public static readonly Color 朱红 = new Color32(153,48,32,255);
    public static Sprite 获取(string 名)
    {
        if (!已启用) return null;
        switch (名)
        {
            case "页面背景": return 素材("构筑背景");
            case "一级面板": case "二级面板": case "属性面板": case "详情框": return 素材("朱红纸框");
            case "道纹卡槽": case "小信息框": return 素材("卡片纸框");
            case "画布内衬": return 素材("墨青画布");
            case "滚动轨": return 素材("素纸");
            case "确认按钮": return 素材("墨绿按钮");
            case "按钮": case "取消按钮": case "返回按钮": case "纸页签": return 素材("暖纸按钮");
            case "滚动滑块": return 素材("素纸");
            case "选中页签": case "选中按钮": return 素材("墨绿按钮");
            default: return null;
        }
    }
    public static void 装配(RectTransform 根, string 页, RectTransform 面板 = null)
    {
        if (根 == null || string.IsNullOrWhiteSpace(页) || !已启用) return;
        if (页 != "设置")
        {
            var 底 = new GameObject("剪纸" + 页 + "底图", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            底.SetParent(根,false); 底.SetAsFirstSibling();
            底.anchorMin = Vector2.zero; 底.anchorMax = Vector2.one; 底.offsetMin = 底.offsetMax = Vector2.zero;
            var 图 = 底.GetComponent<Image>();
            图.sprite = 页 == "改造" ? 天帝首两页山水素材.获取("改造背景") : 素材(页 + "背景");
            图.color = Color.white; 图.raycastTarget = true;
            foreach (Transform 子 in 根)
                if (子.name.Contains("遮罩") && 子.GetComponent<Image>() is Image 遮) 遮.color = Color.clear;
            if (面板 != null)
            {
                var 面板图 = 面板.GetComponent<Image>();
                if (面板图 != null) 面板图.color = Color.clear;
                if (!天帝移动适配.启用)
                {
                    var 大小 = 面板.sizeDelta;
                    if (大小.x > 0.01f && 大小.y > 0.01f)
                    {
                        float 比例 = Mathf.Min(1520 / 大小.x, 850 / 大小.y);
                        天帝双端页面布局.固定(面板,(1600-大小.x*比例)/2,(900-大小.y*比例)/2,大小.x,大小.y);
                        面板.localScale = Vector3.one * 比例;
                    }
                }
            }
        }
        else if (面板 != null)
        {
            var 面板图 = 面板.GetComponent<Image>();
            if (面板图 != null)
            {
                面板图.sprite = 素材("朱红纸框");
                面板图.pixelsPerUnitMultiplier = 1;
            }
        }
        if (页 != "设置" && !天帝移动适配.启用) 标题(面板 ?? 根,页);
        foreach (var 文 in 根.GetComponentsInChildren<Text>(true))
        {
            if (文.color.r > .65f && 文.color.g > .65f && 文.color.b > .55f
                && 文.GetComponentInParent<Selectable>() == null) 文.color = 墨;
        }
    }
    public static void 标题(RectTransform 父,string 页)
    {
        if (父 == null || string.IsNullOrWhiteSpace(页)) return;
        string 原文=页=="角色"?"角色":页=="构筑"?"道纹构筑":页=="宝盒"?"道纹宝盒":"道纹"+页;
        foreach(Transform 子 in 父)if(子.GetComponent<Text>() is Text 文&&文.text==原文)文.gameObject.SetActive(false);
        var 区=new GameObject("审批剪纸标题",typeof(RectTransform),typeof(Text)).GetComponent<RectTransform>();区.SetParent(父,false);
        float 高=页=="角色"?72:60;天帝双端页面布局.固定(区,24,8,320,高);
        var 字=区.GetComponent<Text>();字.font=天帝美术资源.当前?.主页标题字体 ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        字.text=原文;字.fontSize=页=="角色"?42:35;字.color=墨;字.alignment=TextAnchor.MiddleLeft;
        字.raycastTarget=false;字.horizontalOverflow=HorizontalWrapMode.Overflow;字.verticalOverflow=VerticalWrapMode.Truncate;
    }
}
