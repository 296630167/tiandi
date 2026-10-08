using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝角色界面
{
    bool 山水角色册;
    void 装配山水角色册(RectTransform 框)
    {
        if(!天帝图三四山水素材.已启用)return;
        山水角色册=true;框.localScale=Vector3.one;天帝图三四山水素材.定(框,0,0,1600,900);
        var 背景=transform.Find("剪纸角色底图")?.GetComponent<Image>();天帝图三四山水素材.应用(背景,"角色背景");
        foreach(string 名 in new[]{"审批剪纸标题","角色资料衬纸","角色正文衬纸","天赋说明衬纸","分隔线","竖线","底线"})框.Find(名)?.gameObject.SetActive(false);
        void 定(string 名,float x,float y,float w,float h){var 区=框.Find(名) as RectTransform;if(区!=null)天帝图三四山水素材.定(区,x,y,w,h);}
        var 纸框=天帝图三四山水素材.图(框,"山水角色身份框","身份框",28,112,420,686);纸框.transform.SetAsFirstSibling();
        天帝图三四山水素材.图(框,"山水角色标题","角色标题",38,8,304,100).preserveAspect=true;
        定("关闭角色",1344,24,212,58);
        定("角色立绘",48,140,196,498);天帝图三四山水素材.应用(框.Find("角色立绘").GetComponent<Image>(),"角色立绘");
        定("角色名字",250,146,178,52);姓名.fontSize=30;
        定("角色等级",250,204,178,120);状态.fontSize=20;状态.alignment=TextAnchor.UpperLeft;
        定("天赋标题",250,338,178,34);定("天赋名称",250,382,178,46);
        定("天赋效果",250,446,172,132);框.Find("天赋效果").GetComponent<Text>().fontSize=21;
        定("天赋说明",60,690,340,90);框.Find("天赋说明").GetComponent<Text>().fontSize=18;
        var 身份净纸=天帝图三四山水素材.图(框,"山水天赋说明净纸","",48,682,378,102);身份净纸.sprite=天帝剪纸界面皮肤.素材("素纸");身份净纸.color=new Color(1,1,1,.88f);身份净纸.transform.SetSiblingIndex(框.Find("天赋说明").GetSiblingIndex());
        for(int i=0;i<4;i++)天帝图三四山水素材.定((RectTransform)页签[i].transform,478+i*268,112,256,56);
        天帝图三四山水素材.定(正文,486,196,1058,524);
        天帝图三四山水素材.定(评语.rectTransform,486,758,1048,56);评语.fontSize=22;
        var 提示框=天帝图三四山水素材.图(框,"山水属性说明占位框","属性说明框",1010,626,534,196);
        var 提示净纸=天帝图三四山水素材.图(提示框.rectTransform,"说明净纸","",20,16,494,164);提示净纸.sprite=天帝剪纸界面皮肤.素材("素纸");提示净纸.color=new Color(1,1,1,.9f);
        字文(提示框.rectTransform,"属性说明提示","属性说明\n悬停属性查看说明，点击查看完整公式。",44,40,446,104,20,次墨);
        天帝图三四山水素材.图(提示框.rectTransform,"属性说明信息图标","图标-信息",24,145,22,22).preserveAspect=true;
        foreach(var 键 in 框.GetComponentsInChildren<Button>())天帝图三四山水素材.按钮(键);
        foreach(var 键 in 框.GetComponentsInChildren<Button>())foreach(var 文 in 键.GetComponentsInChildren<Text>())文.font=字体;
    }
    void 装配山水角色正文(int 页)
    {
        if(!山水角色册)return;
        for(int i=0;i<4;i++)天帝图三四山水素材.按钮(页签[i],i==页);
        foreach(Transform 子 in 正文)if(子.name=="重点属性衬底")子.gameObject.SetActive(false);
        foreach(var 文 in 正文.GetComponentsInChildren<Text>())if(文.name.StartsWith("数值-")){文.color=墨;文.fontStyle=FontStyle.Bold;文.resizeTextMaxSize=24;文.fontSize=24;}
        if(页==0)
        {
            foreach(Transform 子 in 正文)
            {
                var r=子 as RectTransform;if(r==null)continue;float x=r.anchoredPosition.x,y=-r.anchoredPosition.y;
                bool 右=x>=480;float 左=右?560:0,宽=右?498:440;
                if(子.name.StartsWith("标签-"))天帝图三四山水素材.定(r,左,y,宽*.42f,42);
                else if(子.name.StartsWith("数值-"))天帝图三四山水素材.定(r,左+宽*.42f,y,宽*.58f,42);
                else if(子.name.StartsWith("属性说明入口-"))天帝图三四山水素材.定(r,左,y,宽,47);
                else if(子.name=="行线")天帝图三四山水素材.定(r,左,y,宽,1);
                else if(子.name=="基础属性标题")天帝图三四山水素材.定(r,0,0,440,48);
                else if(子.name=="衍生属性标题")天帝图三四山水素材.定(r,560,0,498,48);
            }
        }
        // 按真实属性行的位置添加图标，并扩大标签留白；不改变任何绑定值。
        foreach(var 文 in 正文.GetComponentsInChildren<Text>())
        {
            if(!文.name.StartsWith("标签-"))continue;
            string 名=文.name.Substring(3);var 素材=天帝图三四山水素材.获取("图标-"+名);if(素材==null)continue;
            var r=文.rectTransform;float x=r.anchoredPosition.x,y=-r.anchoredPosition.y;
            var 标=天帝图三四山水素材.图(正文,"山水属性图标-"+名,"图标-"+名,x,y+10,30,32);标.preserveAspect=true;
            天帝图三四山水素材.定(r,x+48,y,r.sizeDelta.x-48,42);
            文.color=墨;文.font=字体;文.fontSize=22;文.fontStyle=FontStyle.Normal;文.resizeTextForBestFit=false;文.alignment=TextAnchor.MiddleLeft;
            var 数=正文.Find("数值-"+名)?.GetComponent<Text>();
            if(数!=null){数.font=字体;数.fontSize=22;数.fontStyle=FontStyle.Bold;数.resizeTextForBestFit=false;数.alignment=TextAnchor.MiddleRight;}
        }
        var 概况=正文.Find("画布概况") as RectTransform;if(概况!=null)天帝图三四山水素材.定(概况,0,424,500,78);
        if(页==0)foreach(float x in new[]{382f,1000f})天帝图三四山水素材.图(正文,"属性栏花枝","图标-花枝",x,10,42,22).preserveAspect=true;
    }
    void 装配山水属性说明()
    {
        if(!山水角色册||属性浮窗==null)return;
        天帝图三四山水素材.应用(属性浮窗.GetComponent<Image>(),"属性说明框");
        var 净纸=属性浮窗.Find("山水公式净纸") as RectTransform;
        if(净纸==null){var 像=天帝图三四山水素材.图(属性浮窗,"山水公式净纸","",20,14,500,190);像.sprite=天帝剪纸界面皮肤.素材("素纸");像.color=new Color(1,1,1,.93f);净纸=像.rectTransform;净纸.SetAsFirstSibling();}
        属性说明标题.font=字体;属性说明标题.fontStyle=FontStyle.Bold;
        天帝图三四山水素材.定(属性说明标题.rectTransform,28,12,属性浮窗.sizeDelta.x-200,52);
        if(详细说明已打开)天帝图三四山水素材.应用(属性浮窗.GetComponent<Image>(),"身份框");
        天帝图三四山水素材.按钮(说明关闭键);
        if(!详细说明已打开)
        {
            天帝图三四山水素材.定(属性浮窗,1006,624,542,210);
            天帝图三四山水素材.定(属性说明标题.rectTransform,28,12,486,52);属性说明标题.fontSize=24;
            if(触屏说明视口!=null){触屏说明视口.offsetMin=new Vector2(28,10);触屏说明视口.offsetMax=new Vector2(-28,-64);}
            天帝图三四山水素材.定(属性说明正文.rectTransform,0,0,486,136);属性说明正文.fontSize=17;
            属性说明标题.gameObject.SetActive(true);属性说明正文.verticalOverflow=VerticalWrapMode.Truncate;
        }
        else if(触屏说明视口!=null)
        {触屏说明视口.offsetMin=new Vector2(32,28);触屏说明视口.offsetMax=new Vector2(-32,-72);}
        天帝图三四山水素材.定(净纸,18,14,属性浮窗.sizeDelta.x-36,属性浮窗.sizeDelta.y-28);
    }
}
