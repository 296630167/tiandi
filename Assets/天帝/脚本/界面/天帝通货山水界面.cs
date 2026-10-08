using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝通货界面
{
    bool 山水已装配;
    Image 空目标图示;
    void 山水框(RectTransform 区,string 名)
    {
        var 图=区.GetComponent<Image>();天帝首两页山水素材.应用(图,名);图.type=Image.Type.Sliced;图.pixelsPerUnitMultiplier=4;
    }
    void 山水位置(RectTransform 区,float x,float y,float w,float h)=>天帝双端页面布局.固定(区,x,y,w,h);
    void 山水文字(Text 文,float x,float y,float w,float h,int 号,TextAnchor 对齐=TextAnchor.MiddleLeft)
    {
        山水位置(文.rectTransform,x,y,w,h);文.fontSize=号;文.resizeTextForBestFit=false;
        文.font=字体;文.fontStyle=FontStyle.Normal;
        文.alignment=对齐;文.color=天帝剪纸界面皮肤.墨;文.horizontalOverflow=HorizontalWrapMode.Wrap;
    }
    void 装配首两页山水()
    {
        if(!天帝首两页山水素材.已启用)return;
        山水已装配=true;
        var 背景=transform.Find("剪纸改造底图");if(背景!=null)天帝首两页山水素材.应用(背景.GetComponent<Image>(),"改造背景");
        RectTransform 找(string 名)
        {
            foreach(var 区 in 面板.GetComponentsInChildren<RectTransform>(true))
                if(区.name==名)return 区;
            return null;
        }
        var 左=找("通货工具面板");
        var 右=找("道纹详情");
        var 结果=结果标题.transform.parent as RectTransform;
        if(左==null||右==null||结果==null)
        {
            Debug.LogWarning("首两页山水装配跳过缺失的改造页面区域："+(左==null?"通货工具面板 ":"")+(右==null?"道纹详情 ":"")+(结果==null?"结果面板":""));
            山水已装配=false;return;
        }
        山水框(左,"材料面板");山水框(右,"目标面板");山水框(结果,"结果框");
        var 关闭键=面板.Find("关闭通货").GetComponent<Button>();天帝首两页山水素材.按钮(关闭键,"暖纸按钮");
        foreach(var 键 in 通货键)
        {
            var 卡=(RectTransform)键.transform;
            var 原=卡.Find("图标").GetComponent<RawImage>();原.enabled=false;原.name="原通货图标";
            var 图区=new GameObject("图标",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();图区.SetParent(卡,false);
            山水位置(图区,10,10,34,34);var 图=图区.GetComponent<Image>();
            var 名=键.name.Substring("通货-".Length);天帝首两页山水素材.应用(图,"材料_"+名);图.preserveAspect=true;图.raycastTarget=false;
        }
        // 装饰托盘位于绘图背后；有真实道纹时仍由原绘图与单字提供内容。
        var 托区=new GameObject("独立目标纸托",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        托区.SetParent(目标图.transform.parent,false);托区.SetAsFirstSibling();山水位置(托区,2,2,84,76);
        天帝首两页山水素材.应用(托区.GetComponent<Image>(),"六边纸托");托区.GetComponent<Image>().raycastTarget=false;
        var 示意=new GameObject("空目标示意",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();示意.SetParent(目标图.transform.parent,false);
        山水位置(示意,2,2,84,76);空目标图示=示意.GetComponent<Image>();天帝首两页山水素材.应用(空目标图示,"导航_道纹");空目标图示.raycastTarget=false;
        if(天帝移动适配.启用)return;
        天帝首两页山水素材.轻按钮(关闭键);
        天帝首两页山水素材.轻纸(结果.GetComponent<Image>());
        山水位置(面板,0,0,1600,900);面板.GetComponent<Image>().color=Color.clear;
        山水位置(左,70,122,720,723);山水位置(右,810,84,720,761);
        山水位置((RectTransform)关闭键.transform,1306,34,188,69);
        山水文字(关闭键.GetComponentInChildren<Text>(),24,0,140,69,24,TextAnchor.MiddleCenter);
        var 题区=new GameObject("独立改造题字",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
        题区.SetParent(面板,false);山水位置(题区,109,22,284,67);
        天帝首两页山水素材.应用(题区.GetComponent<Image>(),"改造题字");题区.GetComponent<Image>().raycastTarget=false;
        foreach(var 文 in 面板.GetComponentsInChildren<Text>(true))
        {
            if(文.transform.parent!=面板)continue;
            if(文.text=="道纹改造")
            {
                文.gameObject.SetActive(false);
            }
            else if(文.text.StartsWith("先选目标道纹"))山水文字(文,112,92,850,33,22);
            else if(文.text=="2 · 选择材料")山水文字(文,110,150,380,43,25);
            else if(文.text=="1 · 选择目标道纹")山水文字(文,846,121,580,40,25);
            else if(文.text=="词条")山水文字(文,847,306,300,30,21);
            else if(文.text=="3 · 下一次改造")山水文字(文,847,607,500,36,24);
            if(文.text=="2 · 选择材料"||文.text=="1 · 选择目标道纹")
            {
                文.fontStyle=FontStyle.Normal;
            }
            文.transform.SetAsLastSibling();
        }
        foreach(Transform 子 in 左)
        {
            if(!(子.GetComponent<Text>() is Text 文))continue;
            if(文.text=="通货"||文.text=="升阶与赌阶")文.gameObject.SetActive(false);
            else if(文.text.StartsWith("每次1个"))山水文字(文,310,35,369,28,18,TextAnchor.MiddleRight);
            else if(文.text=="词条改造")山水文字(文,40,376,350,33,22);
        }
        for(int i=0;i<通货键.Length;i++)
        {
            var 种=天帝通货.可用种类[i];int 槽=Array.IndexOf(材料顺序,种);int 行=槽<8?槽/2:槽==10?5:4;int 列=槽==10?0:槽%2;
            var 卡=(RectTransform)通货键[i].transform;山水位置(卡,36+列*342,76+行*77+(槽>=8?43:0),321,68);
            山水位置(卡.Find("图标") as RectTransform,10,7,56,55);
            山水文字(名称字[i],82,9,130,28,22);
            名称字[i].verticalOverflow=VerticalWrapMode.Overflow;
            山水文字(数量字[i],213,8,66,32,26,TextAnchor.MiddleRight);
            数量字[i].fontStyle=FontStyle.Bold;数量字[i].verticalOverflow=VerticalWrapMode.Overflow;
            数量字[i].resizeTextForBestFit=true;数量字[i].resizeTextMaxSize=26;数量字[i].resizeTextMinSize=16;
            var 数量描边=数量字[i].gameObject.AddComponent<Outline>();数量描边.effectColor=new Color32(247,239,218,220);数量描边.effectDistance=new Vector2(.8f,-.8f);
            foreach(var 文 in 卡.GetComponentsInChildren<Text>())if(文!=名称字[i]&&文!=数量字[i])
            {
                山水文字(文,82,37,220,20,16);文.verticalOverflow=VerticalWrapMode.Overflow;
            }
        }
        var 分隔=左.Find("材料说明分隔") as RectTransform;山水位置(分隔,40,574,640,1);
        分隔.GetComponent<Image>().sprite=null;分隔.GetComponent<Image>().color=new Color(.28f,.36f,.26f,.35f);
        山水文字(说明名,40,585,630,41,24);山水文字(说明字,40,630,610,70,17);
        山水位置((RectTransform)目标按钮.transform,846,165,650,128);
        山水文字(目标提示,237,20,390,46,25);山水位置(目标图.rectTransform,123,24,84,76);
        山水位置(托区,118,20,94,84);山水位置(示意,125,25,82,76);
        山水文字(目标字,1083,185,265,46,25);山水文字(分类字,1083,239,402,30,18);
        山水文字(更换提示,1350,190,137,38,19);
        var 原分隔=右.Find("分隔");if(原分隔!=null)原分隔.gameObject.SetActive(false);
        山水文字(容量字,1190,306,295,30,19,TextAnchor.MiddleRight);
        山水文字(技能字,860,360,600,58,24,TextAnchor.MiddleCenter);
        for(int i=0;i<词条键.Length;i++)
        {
            山水位置((RectTransform)词条键[i].transform,847,336+i*27,640,26);
            山水文字(词条名称[i],14,0,612,26,17);
        }
        山水文字(接口标题,847,502,640,30,20);
        for(int d=0;d<6;d++)
        {
            山水位置(接口底[d].rectTransform,847+d*109,536,103,35);
            山水文字(接口字[d],0,0,103,35,20,TextAnchor.MiddleCenter);
        }
        山水位置(洗练区,847,574,640,28);
        山水文字(词条字,50,0,535,28,18);
        foreach(var 键 in 洗练区.GetComponentsInChildren<Button>())
            山水位置((RectTransform)键.transform,键.name=="上一词条"?0:602,0,38,28);
        山水位置((RectTransform)使用键.transform,848,654,353,65);
        山水文字(使用键.GetComponentInChildren<Text>(),45,0,263,65,22,TextAnchor.MiddleCenter);
        山水文字(原因字,1206,654,285,65,20);
        山水位置(结果,841,734,661,87);山水文字(结果标题,24,4,615,32,21);山水文字(结果字,24,38,613,39,18);
    }
    void 刷新山水改造状态()
    {
        if(!山水已装配)return;
        if(空目标图示!=null)空目标图示.enabled=当前目标==null;
        for(int i=0;i<通货键.Length;i++)
        {
            var 键=通货键[i];bool 可用=键.interactable;
            bool 选中=可用&&天帝通货.可用种类[i]==当前通货;
            if(天帝移动适配.启用)天帝首两页山水素材.应用(键.GetComponent<Image>(),选中?"材料选中":"材料默认");
            else 天帝首两页山水素材.轻按钮(键,选中);
            var 色=键.colors;色.disabledColor=Color.white;键.colors=色;
            天帝首两页山水素材.按钮透明度(键);
            通货标记[i].enabled=false;
            foreach(var 文 in 键.GetComponentsInChildren<Text>())文.color=天帝剪纸界面皮肤.墨;
            数量字[i].color=new Color32(13,45,37,255);
            var 图标=键.transform.Find("图标").GetComponent<Image>();图标.color=Color.white;
        }
        if(天帝移动适配.启用)天帝首两页山水素材.按钮(目标按钮,"目标选择框");
        else 天帝首两页山水素材.轻按钮(目标按钮);
        天帝首两页山水素材.按钮(使用键,"墨绿按钮",true);
        if(!天帝移动适配.启用)
        {
            var 文=使用键.GetComponentInChildren<Text>();山水文字(文,45,0,263,65,22,TextAnchor.MiddleCenter);
            文.color=new Color32(246,230,192,255);
        }
        // 不可点击时仅降低整颗按钮的CanvasGroup透明度。
        for(int d=0;d<6;d++)
        {
            if(天帝移动适配.启用)天帝首两页山水素材.应用(接口底[d],"接口纸签");
            else 天帝首两页山水素材.轻纸(接口底[d]);
            接口底[d].color=当前目标!=null&&当前目标.有接口(d)?new Color(.78f,.88f,.78f):Color.white;
        }
        for(int i=0;i<词条键.Length;i++)if(词条键[i].gameObject.activeSelf)
        {
            var 键=词条键[i];
            if(天帝移动适配.启用)天帝首两页山水素材.应用(键.GetComponent<Image>(),词条标记[i].enabled?"材料选中":"材料默认");
            else
            {
                天帝首两页山水素材.轻按钮(键,词条标记[i].enabled);
                山水文字(词条名称[i],14,0,612,26,17);
            }
            天帝首两页山水素材.按钮透明度(键);
            词条名称[i].color=天帝剪纸界面皮肤.墨;
        }
    }
}
