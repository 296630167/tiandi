using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹回收界面
{
    public void 设置显示条件(int 品阶=-1,int 类型=0,int 状态=0)
    {
        if(操作框已打开)return;
        筛阶=Mathf.Clamp(品阶,-1,7);筛类型=Mathf.Clamp(类型,0,5);筛状态=Mathf.Clamp(状态,0,3);页=0;
        int[] 值={筛阶+1,筛类型,筛状态};
        for(int i=0;i<3;i++){筛选下拉[i].SetValueWithoutNotify(值[i]);筛选下拉[i].RefreshShownValue();}
        // 改变显示条件不保留隐藏的旧勾选，避免批量回收超出眼前条件。
        选择.Clear();刷新();
    }
    Dropdown 条件下拉(RectTransform 父,string 名,float x,float y,List<string> 项,Action<int> 改变,bool 清空勾选=true)
    {
        // 紧凑条件控件使用清晰的纸面边框，花饰按钮会压住44行高中的文字。
        var r=图(父,名,x,y,198,44,"小信息框").rectTransform;r.GetComponent<Image>().raycastTarget=true;
        var d=r.gameObject.AddComponent<Dropdown>();d.targetGraphic=r.GetComponent<Image>();
        d.captionText=字文(r,"",16,0,144,44,18);d.captionText.alignment=TextAnchor.MiddleLeft;
        天帝响应布局.比例(d.captionText.rectTransform,.08f,0,.72f,1);
        var 箭头=字文(r,"▾",168,0,22,44,18);箭头.alignment=TextAnchor.MiddleCenter;天帝响应布局.比例(箭头.rectTransform,.86f,0,.1f,1);
        float 高=Mathf.Min(天帝移动适配.启用?176:264,项.Count*44);
        var 模板=图(r,"Template",0,44,198,高,null,new Color(.96f,.95f,.85f,1)).rectTransform;模板.GetComponent<Image>().raycastTarget=true;
        var 边框=模板.gameObject.AddComponent<Outline>();边框.effectColor=new Color(.28f,.43f,.37f,1);边框.effectDistance=new Vector2(1,-1);
        天帝响应布局.动态(模板);模板.anchorMin=Vector2.zero;模板.anchorMax=new Vector2(1,0);模板.pivot=new Vector2(0,1);模板.anchoredPosition=Vector2.zero;模板.sizeDelta=new Vector2(0,高);
        var 口=图(模板,"Viewport",0,0,198,高,null,Color.clear).rectTransform;口.GetComponent<Image>().raycastTarget=true;
        天帝响应布局.比例(口,0,0,1,1);口.gameObject.AddComponent<RectMask2D>();
        var 内容=区块(口,"Content",0,0,198,44);天帝响应布局.动态(内容);
        内容.anchorMin=new Vector2(0,1);内容.anchorMax=Vector2.one;内容.sizeDelta=new Vector2(0,44);
        var 行=图(内容,"Item",0,0,198,44,null,new Color(.96f,.95f,.85f,1)).rectTransform;行.GetComponent<Image>().raycastTarget=true;
        行.anchorMin=new Vector2(0,1);行.anchorMax=Vector2.one;行.sizeDelta=new Vector2(0,44);
        var 勾=行.gameObject.AddComponent<Toggle>();勾.targetGraphic=行.GetComponent<Image>();
        var 色=勾.colors;色.normalColor=色.selectedColor=Color.white;色.highlightedColor=new Color(.85f,.94f,.87f);色.pressedColor=new Color(.74f,.87f,.78f);勾.colors=色;
        var 标=字文(行,"✓",4,0,20,44,16);勾.graphic=标;
        d.itemText=字文(行,"",28,0,162,44,16);d.itemText.alignment=TextAnchor.MiddleLeft;
        天帝响应布局.比例(d.itemText.rectTransform,.14f,0,.82f,1);
        var 滚=模板.gameObject.AddComponent<ScrollRect>();滚.viewport=口;滚.content=内容;滚.horizontal=false;滚.movementType=ScrollRect.MovementType.Clamped;滚.scrollSensitivity=32;
        d.template=模板;模板.gameObject.SetActive(false);d.AddOptions(项);d.onValueChanged.AddListener(v=>{if(清空勾选)选择.Clear();改变(v);});return d;
    }
}
