using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹图鉴
{
    readonly Dictionary<string,List<RectTransform>> 剪纸分类行 = new Dictionary<string,List<RectTransform>>();
    readonly List<Button> 剪纸图鉴卡 = new List<Button>();
    readonly List<天帝道纹绘图> 剪纸图鉴图 = new List<天帝道纹绘图>();
    readonly List<Text> 剪纸图鉴名 = new List<Text>();
    readonly List<Text> 剪纸道纹单字 = new List<Text>();
    string 剪纸当前分类;
    int 剪纸页;
    Text 剪纸页码, 剪纸详情文, 剪纸详情名;
    Text 剪纸详情单字;
    Text 山水详情类别;
    RectTransform 山水详情视口, 山水详情图区, 山水详情规则;
    天帝道纹绘图 剪纸详情图;
    RectTransform 剪纸详情目标;
    ScrollRect 剪纸详情滚动;
    Button 剪纸上页, 剪纸下页;

    void 建剪纸图鉴(RectTransform 框)
    {
        void 定(RectTransform r,float x,float y,float w,float h)=>天帝双端页面布局.固定(r,x,y,w,h);
        天帝响应布局.动态(框);
        定(框,0,0,1600,900);框.localScale=Vector3.one;
        天帝图录回收山水素材.纸(根.Find("剪纸图鉴底图")?.GetComponent<Image>(),"图鉴背景","图鉴背景",false);
        var 标题=框.Find("审批剪纸标题")?.GetComponent<Text>();
        if(标题!=null){定(标题.rectTransform,108,0,520,88);标题.fontSize=48;}
        定(框.Find("收录说明") as RectTransform,108,93,940,30);天帝图录回收山水素材.文字(框.Find("收录说明").GetComponent<Text>(),字体,20);
        定(框.Find("关闭图鉴") as RectTransform,1361,28,166,52);
        定(框.Find("查看共通规则") as RectTransform,918,29,198,40);
        定(框.Find("等级查询标签") as RectTransform,1170,28,156,32);天帝图录回收山水素材.文字(框.Find("等级查询标签").GetComponent<Text>(),字体,20);
        定(框.Find("物品等级输入") as RectTransform,1132,71,212,45);天帝首两页山水素材.轻纸(等级输入.targetGraphic as Image);
        定(等级输入.textComponent.rectTransform,12,0,188,45);天帝图录回收山水素材.文字(等级输入.textComponent,字体,22);
        框.Find("等级查询范围").gameObject.SetActive(false);
        for(int i=0;i<分类名.Length;i++)
        {
            var b=分类按钮[分类名[i]];定((RectTransform)b.transform,72+i*168,133,159,47);天帝图录回收山水素材.按钮(b);
            var 文=b.GetComponentInChildren<Text>();定(文.rectTransform,8,0,143,47);天帝图录回收山水素材.文字(文,字体,21);
        }
        天帝图录回收山水素材.按钮(框.Find("关闭图鉴").GetComponent<Button>());
        天帝图录回收山水素材.按钮(框.Find("查看共通规则").GetComponent<Button>(),false,true);
        string 分类 = null;
        foreach (RectTransform 行 in 内容)
        {
            if (行.name.StartsWith("分类标题-")) { 分类=行.name.Substring(5); 剪纸分类行[分类]=new List<RectTransform>(); }
            else if (行.name.StartsWith("图鉴行-") && 分类 != null) 剪纸分类行[分类].Add(行);
        }
        视口.gameObject.SetActive(false); 框.Find("图鉴滚动条")?.gameObject.SetActive(false);
        foreach (var 滑 in 框.GetComponentsInChildren<Scrollbar>()) 滑.gameObject.SetActive(false);
        var 卡区=底(框,"剪纸图鉴卡区",72,210,988,542,null,Color.clear);
        for(int i=0;i<9;i++)
        {
            int 槽=i;
            var b=按钮(卡区,"剪纸图鉴条目-"+i,"",i%3*334,i/3*184,320,168,()=>选择剪纸条目(槽));
            天帝图录回收山水素材.按钮(b);天帝图录回收山水素材.纸((Image)b.targetGraphic,"图录卡","轻纸框");
            剪纸图鉴卡.Add(b);
            var 图区=区((RectTransform)b.transform,"六边道纹",19,16,100,106);
            var 图=图区.gameObject.AddComponent<天帝道纹绘图>(); 图.单纹模式=true; 图.构筑美术=true; 图.单纹半径=42; 图.raycastTarget=false;
            剪纸图鉴图.Add(图);
            var 单字=字(图区,"核心汉字","",0,0,100,100,30,天帝剪纸界面皮肤.墨);单字.alignment=TextAnchor.MiddleCenter;
            天帝道纹单字.绑定(单字,图区);剪纸道纹单字.Add(单字);
            var 名=字((RectTransform)b.transform,"条目名称","",129,17,176,96,23,天帝剪纸界面皮肤.墨); 名.alignment=TextAnchor.MiddleLeft;天帝图录回收山水素材.文字(名,字体,23);
            剪纸图鉴名.Add(名);
            var 提示=字((RectTransform)b.transform,"查看提示","点击查看正式说明",26,130,268,30,18,天帝剪纸界面皮肤.次墨);天帝图录回收山水素材.文字(提示,字体,18);
        }
        剪纸上页=按钮(框,"剪纸图鉴上一页","‹",387,789,78,50,()=>{剪纸页--;刷新剪纸图鉴();选择剪纸条目(0);});天帝图录回收山水素材.按钮(剪纸上页);
        剪纸下页=按钮(框,"剪纸图鉴下一页","›",674,789,78,50,()=>{剪纸页++;刷新剪纸图鉴();选择剪纸条目(0);});天帝图录回收山水素材.按钮(剪纸下页);
        剪纸页码=字(框,"剪纸图鉴页码","",487,789,165,50,24,天帝剪纸界面皮肤.墨); 剪纸页码.alignment=TextAnchor.MiddleCenter;
        var 详情=底(框,"剪纸图鉴固定详情",1087,199,485,634,"详情框");天帝图录回收山水素材.纸(详情.GetComponent<Image>(),"详情长卷","朱红纸框");
        剪纸详情名=字(详情,"剪纸详情标题","",43,22,397,57,32,天帝剪纸界面皮肤.墨);天帝图录回收山水素材.文字(剪纸详情名,字体,32);
        山水详情类别=字(详情,"山水图鉴类别","",43,84,397,34,20,天帝剪纸界面皮肤.墨);天帝图录回收山水素材.文字(山水详情类别,字体,20);
        var 详情图区=区(详情,"选中道纹纸雕",172,138,140,140);山水详情图区=详情图区;
        剪纸详情图=详情图区.gameObject.AddComponent<天帝道纹绘图>(); 剪纸详情图.单纹模式=true; 剪纸详情图.构筑美术=true;剪纸详情图.单纹半径=59; 剪纸详情图.raycastTarget=false;
        剪纸详情单字=字(详情图区,"详情核心汉字","",0,0,140,140,43,天帝剪纸界面皮肤.墨);剪纸详情单字.alignment=TextAnchor.MiddleCenter;
        天帝道纹单字.绑定(剪纸详情单字,详情图区);
        var 口=底(详情,"正式说明视口",42,300,400,198,null,Color.clear);山水详情视口=口;口.gameObject.AddComponent<RectMask2D>();
        剪纸详情文=字(口,"完整正式说明","",0,0,400,600,22,天帝剪纸界面皮肤.墨); 剪纸详情文.alignment=TextAnchor.UpperLeft; 剪纸详情文.lineSpacing=1.12f;天帝图录回收山水素材.文字(剪纸详情文,字体,22);
        剪纸详情滚动=口.gameObject.AddComponent<ScrollRect>(); 剪纸详情滚动.viewport=口; 剪纸详情滚动.content=剪纸详情文.rectTransform;
        剪纸详情滚动.horizontal=false; 剪纸详情滚动.vertical=true; 剪纸详情滚动.movementType=ScrollRect.MovementType.Clamped; 剪纸详情滚动.scrollSensitivity=30;
        山水详情规则=区(详情,"图鉴通用规则",42,512,400,102);
        var 分隔=底(山水详情规则,"图鉴规则分隔",0,0,400,1,null,new Color(.28f,.43f,.37f,.6f));分隔.GetComponent<Image>().raycastTarget=false;
        var 规则键=按钮(山水详情规则,"图鉴通用规则入口","通用规则",0,13,168,37,展示剪纸共通规则);天帝图录回收山水素材.按钮(规则键,false,true);
        var 摘要=字(山水详情规则,"图鉴规则摘要","品阶决定词条容量；接口方向随机，可旋转。",0,57,400,44,18,天帝剪纸界面皮肤.次墨);天帝图录回收山水素材.文字(摘要,字体,18);
        var 底注=框.Find("图鉴底注").GetComponent<Text>();定(底注.rectTransform,75,852,1448,27);天帝图录回收山水素材.文字(底注,字体,16);
        显示剪纸分类("功能道纹");
    }
    bool 显示剪纸分类(string 分类)
    {
        if(!剪纸分类行.ContainsKey(分类))return false;
        剪纸当前分类=分类;剪纸页=0;刷新剪纸图鉴();选择剪纸条目(0);return true;
    }
    void 刷新剪纸图鉴()
    {
        var 列=剪纸分类行[剪纸当前分类]; int 总页=Mathf.Max(1,Mathf.CeilToInt(列.Count/9f)); 剪纸页=Mathf.Clamp(剪纸页,0,总页-1);
        for(int i=0;i<9;i++)
        {
            int n=剪纸页*9+i; 剪纸图鉴卡[i].gameObject.SetActive(n<列.Count);if(n>=列.Count)continue;
            var 行=列[n];var 原图=行.GetComponentInChildren<天帝道纹绘图>(true);
            剪纸图鉴图[i].单纹=原图?.单纹;剪纸图鉴图[i].SetVerticesDirty();
            剪纸道纹单字[i].text=天帝道纹美术.单字(原图?.单纹);
            剪纸图鉴名[i].text=行.Find("道纹名称").GetComponent<Text>().text;
            天帝图录回收山水素材.纸((Image)剪纸图鉴卡[i].targetGraphic,"图录卡","轻纸框");
            ((Image)剪纸图鉴卡[i].targetGraphic).color=行==剪纸详情目标?new Color(.78f,.88f,.76f):Color.white;
        }
        剪纸页码.text=(剪纸页+1)+" / "+总页; 剪纸上页.interactable=剪纸页>0;剪纸下页.interactable=剪纸页+1<总页;
        foreach(var 项 in 分类按钮)天帝图录回收山水素材.按钮(项.Value,项.Key==剪纸当前分类);
        天帝图录回收山水素材.禁用(剪纸上页);天帝图录回收山水素材.禁用(剪纸下页);
    }
    void 选择剪纸条目(int 槽)
    {
        var 列=剪纸分类行[剪纸当前分类];int n=剪纸页*9+槽;if(n<列.Count)展示剪纸详情(列[n]);刷新剪纸图鉴();
    }
    void 展示剪纸详情(RectTransform 行)
    {
        剪纸详情目标=行;剪纸详情名.text=行.Find("道纹名称").GetComponent<Text>().text;
        山水详情类别.text=行.Find("道纹类别").GetComponent<Text>().text;
        山水详情类别.gameObject.SetActive(true);山水详情图区.gameObject.SetActive(true);山水详情规则.gameObject.SetActive(true);
        天帝双端页面布局.固定(山水详情视口,42,300,400,198);
        剪纸详情图.单纹=行.GetComponentInChildren<天帝道纹绘图>(true)?.单纹;剪纸详情图.SetVerticesDirty();
        剪纸详情单字.text=天帝道纹美术.单字(剪纸详情图.单纹);
        剪纸详情文.text=string.Join("\n\n",行.GetComponentsInChildren<Text>(true).Where(x=>x.name!="道纹单字"&&x.name!="示意标签"&&x.name!="道纹名称"&&x.name!="道纹类别").Select(x=>x.text));
        排剪纸详情();
    }
    void 展示剪纸共通规则()
    {
        剪纸详情目标=null;剪纸详情名.text="品阶与连接规则";剪纸详情图.单纹=null;剪纸详情图.SetVerticesDirty();
        剪纸详情单字.text="";
        山水详情类别.gameObject.SetActive(false);山水详情图区.gameObject.SetActive(false);山水详情规则.gameObject.SetActive(false);
        天帝双端页面布局.固定(山水详情视口,42,101,400,498);
        剪纸详情文.text=string.Join("\n\n",new[]{"品阶容量说明","连接机制说明"}.SelectMany(n=>内容.Find(n).GetComponentsInChildren<Text>(true)).Select(t=>t.text));排剪纸详情();
    }
    void 排剪纸详情()
    {
        剪纸详情文.rectTransform.sizeDelta=new Vector2(400,Mathf.Max(山水详情视口.rect.height,剪纸详情文.preferredHeight+12));
        Canvas.ForceUpdateCanvases();剪纸详情滚动.verticalNormalizedPosition=1;
    }
}
