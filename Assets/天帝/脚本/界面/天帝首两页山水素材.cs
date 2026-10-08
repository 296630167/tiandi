using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 专用于获批的首两页；资源均为无字独立生成素材，数值与状态仍由真实组件驱动。
public static class 天帝首两页山水素材
{
    static readonly Dictionary<string,Sprite> 缓存=new Dictionary<string,Sprite>();
    public static Sprite 获取(string 名)
    {
        if(!缓存.TryGetValue(名,out var 图)){图=Resources.Load<Sprite>("山水首两页/"+名);if(图!=null)缓存.Add(名,图);}
        return 图;
    }
    public static bool 已启用=>获取("主页背景")!=null&&获取("目标面板")!=null;
    public static void 应用(Image 图,string 名)
    {
        if(图==null)return;var 素材=获取(名);if(素材==null)return;
        图.sprite=素材;图.overrideSprite=null;图.type=Image.Type.Simple;图.color=Color.white;
    }
    public static void 按钮(Button 键,string 名,bool 浅字=false)
    {
        应用(键.targetGraphic as Image,名);
        var 区域=键.GetComponent<天帝按钮文字区域>();if(区域!=null)区域.enabled=false;
        var 色=键.colors;色.normalColor=色.selectedColor=Color.white;
        色.disabledColor=Color.white;
        色.highlightedColor=new Color(1.05f,1.05f,1.02f);色.pressedColor=new Color(.86f,.88f,.82f);键.colors=色;
        foreach(var 文 in 键.GetComponentsInChildren<Text>())文.color=浅字?new Color32(246,230,192,255):天帝剪纸界面皮肤.墨;
        按钮透明度(键);
    }
    public static void 按钮透明度(Button 键)
    {
        var 组=键.GetComponent<CanvasGroup>();if(组==null)组=键.gameObject.AddComponent<CanvasGroup>();
        组.alpha=键.interactable?1f:.5f;
    }
    // 小控件复用独立九宫格纸框，不改变子级图标和文字的布局。
    public static void 轻纸(Image 图,bool 选中=false)
    {
        if(图==null)return;
        var 素材=Resources.Load<Sprite>("剪纸界面/轻纸框");if(素材==null)return;
        图.sprite=素材;图.overrideSprite=null;图.type=Image.Type.Sliced;图.pixelsPerUnitMultiplier=2;
        图.color=选中?new Color(.78f,.88f,.76f):Color.white;
    }
    public static void 轻按钮(Button 键,bool 选中=false)
    {
        轻纸(键.targetGraphic as Image,选中);
        var 区域=键.GetComponent<天帝按钮文字区域>();if(区域!=null)区域.enabled=false;
        键.transition=Selectable.Transition.ColorTint;
        var 色=键.colors;色.normalColor=色.selectedColor=色.disabledColor=Color.white;
        色.highlightedColor=new Color(.90f,.96f,.88f);色.pressedColor=new Color(.78f,.88f,.76f);键.colors=色;
        按钮透明度(键);
    }
}
