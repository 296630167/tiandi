using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 单独生成的美术部件；不包含概念整屏、假数值或交互截图。
public static class 天帝图三四山水素材
{
    static readonly Dictionary<string,Sprite> 缓存=new Dictionary<string,Sprite>();
    public static Sprite 获取(string 名)
    {
        if(!缓存.TryGetValue(名,out var 图)){图=Resources.Load<Sprite>("山水图三四/"+名);if(图!=null)缓存.Add(名,图);}
        return 图;
    }
    public static bool 已启用=>!天帝移动适配.启用&&获取("构筑背景")!=null&&获取("角色立绘")!=null;
    public static void 定(RectTransform 区,float x,float y,float w,float h)=>天帝双端页面布局.固定(区,x,y,w,h);
    public static void 应用(Image 图,string 名)
    {if(图==null)return;图.sprite=获取(名);图.overrideSprite=null;图.type=Image.Type.Simple;图.color=Color.white;}
    public static Image 图(RectTransform 父,string 名,string 素材,float x,float y,float w,float h)
    {
        var 区=new GameObject(名,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();区.SetParent(父,false);定(区,x,y,w,h);
        var 像=区.GetComponent<Image>();应用(像,素材);像.raycastTarget=false;return 像;
    }
    public static void 按钮(Button 键,bool 主=false)
    {
        if(键==null)return;
        天帝首两页山水素材.按钮(键,主?"朱红按钮":"暖纸按钮",主);
        var 像=键.targetGraphic as Image;
        if(像!=null){像.sprite=天帝剪纸界面皮肤.素材("轻纸框");像.overrideSprite=null;像.type=Image.Type.Sliced;像.pixelsPerUnitMultiplier=2;像.color=主?new Color(.78f,.88f,.76f):Color.white;}
        foreach(var 文 in 键.GetComponentsInChildren<Text>(true))
        {
            // 弹窗按钮提交比例布局后sizeDelta为零，文字随按钮伸展，不能据此固定成负宽/零高。
            var 区=文.rectTransform;天帝响应布局.动态(区);
            区.anchorMin=new Vector2(.09f,0);区.anchorMax=new Vector2(.91f,1);区.pivot=new Vector2(.5f,.5f);
            区.offsetMin=区.offsetMax=Vector2.zero;区.localScale=Vector3.one;
            文.color=天帝剪纸界面皮肤.墨;文.fontStyle=FontStyle.Normal;
            文.alignment=TextAnchor.MiddleCenter;文.fontSize=18;文.resizeTextForBestFit=true;文.resizeTextMinSize=14;文.resizeTextMaxSize=18;
        }
    }
}
