using UnityEngine;
using UnityEngine.UI;
public sealed partial class 天帝道纹界面
{
    bool 山水构筑;
    void 装配山水构筑()
    {
        if(!天帝图三四山水素材.已启用)return;
        山水构筑=true;
        void 定(string 名,float x,float y,float w,float h){var r=天帝双端页面布局.子区(根,名);if(r!=null)天帝图三四山水素材.定(r,x,y,w,h);}
        天帝图三四山水素材.应用(根.Find("道纹背景").GetComponent<Image>(),"构筑背景");
        foreach(Transform 子 in 根)if(子.GetComponent<Text>() is Text 文&&文.text=="道纹构筑")子.gameObject.SetActive(false);
        天帝图三四山水素材.图(根,"山水构筑标题","构筑标题",28,0,300,76).preserveAspect=true;
        天帝图三四山水素材.定(成长字.rectTransform,350,16,970,44);成长字.fontSize=20;
        定("返回主页",1370,16,200,48);
        string[] 工具={"回到源点","聚焦已解锁","定位未接通","撤销上一步","布局方案","操作说明"};
        for(int i=0;i<6;i++)定(工具[i],330+i*149,78,140,46);
        根.Find("撤销上一步").GetComponentInChildren<Text>().text="撤销上一步";
        var 视口=(RectTransform)画布.transform.parent;天帝图三四山水素材.定(视口,0,140,1600,650);
        视口.Find("墨青纹理工作区")?.gameObject.SetActive(false);画布.山水工作区=true;
        var 匣=天帝图三四山水素材.图(根,"山水藏匣框","藏匣框",20,134,314,670);匣.transform.SetSiblingIndex(2);
        天帝图三四山水素材.定(状态字.rectTransform,46,128,264,48);状态字.fontSize=24;状态字.alignment=TextAnchor.MiddleCenter;
        var 匣签=天帝图三四山水素材.图(根,"山水藏匣标题签","",44,126,268,52);匣签.sprite=天帝首两页山水素材.获取("墨绿按钮");匣签.transform.SetSiblingIndex(状态字.transform.GetSiblingIndex());
        var 汇总纸=天帝图三四山水素材.图(根,"山水藏匣汇总衬纸","",44,184,268,36);
        汇总纸.sprite=天帝剪纸界面皮肤.素材("素纸");汇总纸.transform.SetSiblingIndex(汇总字.transform.GetSiblingIndex());
        天帝图三四山水素材.定(汇总字.rectTransform,50,186,256,32);汇总字.font=字体;汇总字.fontSize=16;汇总字.alignment=TextAnchor.MiddleCenter;汇总字.resizeTextForBestFit=false;
        定("筛选 / 排序",44,224,266,42);
        天帝图三四山水素材.定(候选区,42,278,270,460);
        天帝图三四山水素材.定(空列表提示.rectTransform,18,18,234,142);空列表提示.font=字体;空列表提示.fontSize=18;空列表提示.resizeTextForBestFit=false;空列表提示.alignment=TextAnchor.MiddleCenter;
        天帝图三四山水素材.定(悬浮候选滚动.content,0,0,270,每页数量*76);
        for(int i=0;i<候选卡.Count;i++)排山水候选卡(i,i%每页数量);
        定("上一页",42,746,84,40);定("下一页",228,746,84,40);
        天帝图三四山水素材.定(页码字.rectTransform,126,746,102,40);
        天帝图三四山水素材.定(提示字.rectTransform,350,850,854,34);提示字.fontSize=16;
        var 操作纸=天帝图三四山水素材.图(根,"山水操作提示衬纸","",342,844,878,46);操作纸.sprite=天帝剪纸界面皮肤.素材("素纸");操作纸.color=new Color(1,1,1,.65f);操作纸.transform.SetSiblingIndex(提示字.transform.GetSiblingIndex());
        定("链路状态图例",348,764,578,32);定("旋转 · R",630,810,190,40);
        var 预览=单弹字.transform.parent as RectTransform;
        var 预览口=预览.parent as RectTransform;天帝图三四山水素材.定(预览口,1230,118,350,744);
        var 预览框=天帝图三四山水素材.图(根,"山水战斗变化框","变化框",1230,118,350,744);预览框.transform.SetSiblingIndex(2);
        预览.GetComponent<Image>().color=Color.clear;天帝图三四山水素材.定(预览,0,0,350,744);
        var 预览净纸=天帝图三四山水素材.图(预览,"山水预览净纸","",20,60,310,676);预览净纸.sprite=天帝剪纸界面皮肤.素材("素纸");预览净纸.color=Color.white;预览净纸.transform.SetAsFirstSibling();
        var 标题=预览.GetComponentInChildren<Text>();天帝图三四山水素材.定(标题.rectTransform,26,8,298,48);标题.alignment=TextAnchor.MiddleCenter;标题.fontSize=25;
        var 变化签=天帝图三四山水素材.图(预览,"山水变化标题签","",24,6,302,52);变化签.sprite=天帝首两页山水素材.获取("墨绿按钮");变化签.transform.SetAsFirstSibling();
        for(int i=0;i<6;i++){int d=(6-i)%6;天帝图三四山水素材.定((RectTransform)通路按钮[d].transform,26+i%3*102,100+i/3*44,94,36);}
        void 文位(Text 文,float y,float h,int 字号){天帝图三四山水素材.定(文.rectTransform,26,y,298,h);文.fontSize=字号;文.fontStyle=FontStyle.Normal;}
        Text 小标题(string 名,string 内容,float y){var 文=文字(预览,内容,26,y,298,28,18,TextAnchor.MiddleLeft);文.name=名;天帝图三四山水素材.定(文.rectTransform,26,y,298,28);文.font=字体;文.fontStyle=FontStyle.Normal;文.color=天帝剪纸界面皮肤.墨;return 文;}
        小标题("山水通路小标题","通路选择",64);小标题("山水演示小标题","攻击演示",364);小标题("山水比较小标题","当前与预览",534);
        文位(单弹字,204,46,26);文位(射击字,258,30,17);文位(形态字,302,50,18);
        var 演示=预览.Find("攻击形态演示") as RectTransform;天帝图三四山水素材.定(演示,26,402,298,108);
        foreach(Transform 子 in 预览)if(子.GetComponent<Text>() is Text 文&&文.text.StartsWith("顺序链路："))子.gameObject.SetActive(false);
        天帝图三四山水素材.定(变化底.rectTransform,22,568,306,68);变化底.color=new Color(.40f,.60f,.48f,.10f);变化底.sprite=null;
        文位(对比字,574,62,17);文位(连接字,644,32,16);
        var 来源=预览.Find("加成来源") as RectTransform;天帝图三四山水素材.定(来源,26,690,298,36);
        刷新山水构筑();
    }
    void 排山水候选卡(int i,int 行)
    {
        if(!山水构筑)return;
        var 卡=(RectTransform)候选卡[i].transform;天帝图三四山水素材.定(卡,0,行*76,270,74);
        var 底=卡.GetComponent<Image>();底.sprite=天帝剪纸界面皮肤.素材("素纸");底.type=Image.Type.Simple;底.color=new Color(1,1,1,.18f);
        天帝图三四山水素材.定(候选图[i].rectTransform,4,13,60,55);候选图[i].单纹半径=25;
        天帝图三四山水素材.定(候选品阶[i].rectTransform,72,4,168,24);候选品阶[i].fontSize=18;候选品阶[i].font=字体;候选品阶[i].fontStyle=FontStyle.Normal;
        天帝图三四山水素材.定(候选说明[i].rectTransform,72,25,190,25);候选说明[i].fontSize=15;候选说明[i].alignment=TextAnchor.MiddleLeft;
        天帝图三四山水素材.定(候选状态[i].rectTransform,72,51,190,20);候选状态[i].fontSize=14;
        候选状态图[i].gameObject.SetActive(false);
        var 选框=卡.Find("道纹选中框") as RectTransform;if(选框!=null)天帝图三四山水素材.定(选框,2,10,66,62);
        var 锁=(RectTransform)候选锁[i].transform;锁.anchorMin=锁.anchorMax=锁.pivot=Vector2.one;锁.anchoredPosition=new Vector2(-2,-2);锁.sizeDelta=Vector2.one*30;
    }
    void 刷新山水构筑()
    {
        if(!山水构筑)return;
        foreach(var 键 in 根.GetComponentsInChildren<Button>(true))
        {if(键.name=="关闭筛选遮罩")continue;if(键.name=="道纹回收锁定"){天帝首两页山水素材.按钮透明度(键);continue;}天帝图三四山水素材.按钮(键);}
        for(int d=0;d<6;d++)
        {
            天帝图三四山水素材.按钮(通路按钮[d],d==当前预览通路);
            var 文=通路按钮[d].GetComponentInChildren<Text>();文.fontSize=14;文.resizeTextMaxSize=15;
        }
        foreach(var 文 in new[]{单弹字,射击字,形态字,对比字,连接字})文.color=天帝剪纸界面皮肤.墨;
        状态字.color=new Color32(246,230,192,255);
        foreach(var 文 in new[]{成长字,汇总字,空列表提示,单弹字,射击字,形态字,对比字,连接字,提示字}){文.font=字体;文.resizeTextForBestFit=false;文.color=天帝剪纸界面皮肤.墨;}
        if(数据.道纹.Count==0)空列表提示.text="藏匣尚空\n回主页开启属性宝盒\n或去青岚原收集道纹";
        页码字.text=(候选页码+1)+" / "+候选总页数;
        foreach(Transform 子 in 单弹字.transform.parent)if(子.GetComponent<Text>() is Text 文&&文.text=="战斗变化")文.color=状态字.color;
        for(int i=0;i<候选卡.Count;i++)
        {
            var 纹=数据.道纹[i];候选品阶[i].text=纹.名称;
            候选状态[i].text=天帝道纹品阶.彩色品阶文字(纹.品阶)+(纹.格子.HasValue?" · 已放置":"");
        }
        foreach(var 文 in 根.GetComponentsInChildren<Button>(true))foreach(var 字 in 文.GetComponentsInChildren<Text>(true))字.font=字体;
        变化底.sprite=null;变化底.color=new Color(.40f,.60f,.48f,.10f);
    }
}
