using UnityEngine;
using UnityEngine.UI;

public partial class 天帝界面
{
    bool 显示剪纸标题()
    {
        var 背景=天帝辅助页山水.素材("标题背景","剪纸界面/标题山水背景");if(背景==null)return false;
        换页();页面背景.sprite=背景;页面背景.color=Color.white;
        var 区=剪纸区(页面,"剪纸标题1920坐标",0,0,1920,1080);
        区.localScale=Vector3.one*(天帝移动适配.布局尺寸.x/1920f);
        var 标题=剪纸字(区,"游戏全名",天帝游戏.全名.Replace("到我为","到\n我为"),430,142,1060,292,76,天帝剪纸界面皮肤.墨);
        标题.font=游戏.美术!=null&&游戏.美术.主页标题字体!=null?游戏.美术.主页标题字体:游戏.默认字体;
        标题.fontStyle=FontStyle.Normal;
        标题.alignment=TextAnchor.MiddleCenter;标题.lineSpacing=.94f;
        标题.verticalOverflow=VerticalWrapMode.Overflow;
        void 键(string 名,float x,float y,float w,float h,System.Action 点击,bool 主=false)
        {
            var b=按钮(区,名,x,y,w,h,点击,主);
            b.GetComponentInChildren<Text>().fontSize=主?48:30;
            天帝辅助页山水.按钮(b,主);
            if(主)天帝辅助页山水.纸(b.targetGraphic as Image,"朱红按钮","剪纸界面/朱红按钮",false);
        }
        键(游戏.可继续游戏?"继续游戏":"开始游戏",684,516,552,130,()=>{if(游戏.可继续游戏)游戏.继续游戏();else 游戏.开始序章();},true);
        if(游戏.可继续游戏)键("新游戏",714,696,492,108,游戏.开始序章);
        键("设置",1640,32,222,76,显示设置);
        if(!天帝移动适配.启用)键("退出游戏",1620,946,246,72,游戏.退出游戏);
        var 存档=剪纸字(区,"存档提示",游戏.存档提示,430,828,1060,56,24,天帝剪纸界面皮肤.朱红);存档.alignment=TextAnchor.MiddleCenter;
        var 版本底=剪纸区(区,"版本纸签",16,970,300,76).gameObject.AddComponent<Image>();天帝辅助页山水.轻纸(版本底);版本底.raycastTarget=false;
        var 版本=剪纸字(区,"实际版本","版本 "+Application.version,32,978,268,60,24,天帝剪纸界面皮肤.次墨);
        版本.alignment=TextAnchor.MiddleCenter;
        return true;
    }
}
