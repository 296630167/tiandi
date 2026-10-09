using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹回收界面 : MonoBehaviour
{
    const int 最大卡数=9;
    int 每页=>天帝移动适配.启用?6:最大卡数;
    天帝道纹 数据;天帝宝盒 钱;Font 字体;Action 关闭;
    readonly HashSet<道纹实例> 选择=new HashSet<道纹实例>();
    readonly List<道纹实例> 列表=new List<道纹实例>();
    readonly Button[] 卡=new Button[最大卡数];
    readonly Toggle[] 回收勾选=new Toggle[最大卡数];
    readonly 天帝道纹锁定按钮[] 锁按钮=new 天帝道纹锁定按钮[最大卡数];
    readonly Text[] 卡名=new Text[最大卡数],卡价=new Text[最大卡数],卡状态=new Text[最大卡数];
    readonly Text[] 图短名=new Text[最大卡数];
    readonly 天帝道纹绘图[] 图标=new 天帝道纹绘图[最大卡数];
    Text 余额,汇总,总价,提示,页码字,空库存;
    readonly Dropdown[] 筛选下拉=new Dropdown[3];
    RectTransform 详情区,确认层;
    RectTransform 余额条;
    天帝道纹详情卡 详情;
    Button 卖出,上页,下页;
    int 页,筛阶=-1,筛类型,筛状态;
    道纹实例 焦点;
    List<道纹实例> 待售;
    int 预期价;
    public int 选中数量=>选择.Count;
    public int 总回收灵石{get;private set;}
    public bool 确认已打开=>确认层!=null;
    public string 最近结果=>提示?.text??"";
    public 道纹实例 显示项(int 槽)=>槽>=0&&槽<每页&&页*每页+槽<列表.Count?列表[页*每页+槽]:null;
    public void 初始化(天帝道纹 网,天帝宝盒 盒,Font 字,Action 关)
    {
        数据=网;钱=盒;字体=字;关闭=关;var 根=(RectTransform)transform;
        var 遮=图(根,"回收遮罩",0,0,1600,900,null,new Color(0,0,0,.82f));遮.raycastTarget=true;
        var 框=图(根,"道纹回收窗口",72,34,1456,832,"一级面板").rectTransform;
        字文(框,"道纹回收",32,18,620,58,34);
        余额条=图(框,"回收灵石纸签",960,22,296,56,"确认按钮").rectTransform;
        余额=字文(余额条,"",16,0,264,56,22);余额.name="回收灵石余额";余额.alignment=TextAnchor.MiddleCenter;
        键(框,"关闭回收","关闭",1276,22,148,56,关闭);
        字文(框,"处理闲置道纹，换回少量灵石。锁定、已放置与布局方案使用的道纹受到保护。",32,82,1300,34,19,天帝道纹美术.次文);
        var 品阶项=new List<string>{"全部品阶"};for(int i=0;i<8;i++)品阶项.Add(((道纹品阶)i).ToString());
        筛选下拉[0]=条件下拉(框,"回收品阶筛选",32,131,品阶项,v=>{筛阶=v-1;页=0;刷新();});
        筛选下拉[1]=条件下拉(框,"回收类型筛选",240,131,new List<string>{"全部类型","属性道纹","分叉道纹","功能道纹","特性道纹","转化道纹"},v=>{筛类型=v;页=0;刷新();});
        筛选下拉[2]=条件下拉(框,"回收状态筛选",448,131,new List<string>{"全部状态","未锁定","已锁定","可回收"},v=>{筛状态=v;页=0;刷新();});
        var 批选边=图(框,"批选分组边",654,135,1,30,null,天帝道纹美术.次文);批选边.raycastTarget=false;
        键(框,"一键选中","一键选中",676,131,134,38,一键选中);
        键(框,"回收范围设置","范围设置",822,131,134,38,打开范围);
        键(框,"清空回收选择","清空选择",968,131,134,38,()=>{选择.Clear();刷新();});
        for(int i=0;i<每页;i++)
        {
            int 槽=i;var b=键(框,"回收道纹-"+i,"",32+i%3*314,192+i/3*155,298,139,()=>查看(槽));卡[i]=b;b.transition=Selectable.Transition.None;
            var r=(RectTransform)b.transform;
            var 区=区块(r,"回收图标",8,13,72,78);图标[i]=区.gameObject.AddComponent<天帝道纹绘图>();图标[i].单纹模式=true;图标[i].单纹半径=30;图标[i].raycastTarget=false;
            图标[i].数据=数据;
            图短名[i]=字文(区,"",6,0,60,78,22);图短名[i].alignment=TextAnchor.MiddleCenter;图短名[i].fontStyle=FontStyle.Bold;
            天帝道纹单字.绑定(图短名[i], 区);
            卡名[i]=字文(r,"",89,10,160,58,19);
            卡价[i]=字文(r,"",89,72,190,30,18,new Color(.91f,.80f,.52f));
            卡状态[i]=字文(r,"",48,107,239,28,16,天帝道纹美术.次文);
            var 勾选区=图(r,"回收复选框-"+i,8,101,38,36,null,Color.clear);勾选区.raycastTarget=true;
            var 复选框=勾选区.gameObject.AddComponent<Toggle>();回收勾选[i]=复选框;
            var 方框=图(勾选区.rectTransform,"复选框底",4,4,28,28,"小信息框");方框.raycastTarget=false;
            var 边=方框.gameObject.AddComponent<Outline>();边.effectColor=天帝道纹美术.强调;边.effectDistance=new Vector2(1,-1);边.useGraphicAlpha=false;
            var 勾=字文(方框.rectTransform,"✓",0,-4,28,36,24,天帝道纹美术.正文);勾.alignment=TextAnchor.MiddleCenter;勾.font=字体;勾.fontSize=24;勾.fontStyle=FontStyle.Bold;
            复选框.targetGraphic=方框;复选框.graphic=勾;复选框.toggleTransition=Toggle.ToggleTransition.None;
            var 色=复选框.colors;色.normalColor=色.selectedColor=Color.white;色.highlightedColor=new Color(.91f,.97f,.92f);色.pressedColor=new Color(.78f,.89f,.84f);色.disabledColor=new Color(.64f,.71f,.67f);复选框.colors=色;
            复选框.onValueChanged.AddListener(勾上=>设置勾选(槽,勾上));
            锁按钮[i]=天帝道纹锁定按钮.创建(r,数据);
        }
        详情区=区块(框,"回收道纹详情",992,191,432,540);
        var 实际区=区块(详情区,"回收详情卡",0,0,460,550);实际区.localScale=Vector3.one*(432f/460);
        详情=实际区.gameObject.AddComponent<天帝道纹详情卡>();详情.初始化(字体);
        详情.设置数据(数据);
        空库存=字文(框,"",96,344,788,104,23,天帝道纹美术.次文);空库存.alignment=TextAnchor.MiddleCenter;
        字文(框,"点击卡片查看详情，勾选复选框加入回收。",32,671,690,32,18,天帝道纹美术.次文);
        页码字=字文(框,"",780,668,100,38,18);页码字.alignment=TextAnchor.MiddleCenter;
        上页=键(框,"回收上一页","‹",714,668,52,38,()=>{页--;刷新();});
        下页=键(框,"回收下一页","›",886,668,52,38,()=>{页++;刷新();});
        var 底=图(框,"回收结算面板",32,726,1392,82,天帝青绿皮肤.已启用?"小信息框":"二级面板").rectTransform;
        汇总=字文(底,"",20,6,240,36,21);
        总价=字文(底,"",294,4,790,40,26);总价.fontStyle=FontStyle.Bold;
        提示=字文(底,"先勾选不再需要的道纹。回收确认后无法撤销。",20,43,950,28,17,天帝道纹美术.次文);
        卖出=键(底,"预览回收","一键回收",1110,15,258,54,打开确认,true);
        天帝剪纸界面皮肤.装配(根,"回收",框);
        if (天帝剪纸界面皮肤.已启用) 余额.color=天帝道纹美术.浅字;
        装配山水回收(根,框,底,实际区);
        布局手机(框,底,实际区);
        数据.状态改变+=刷新;钱.余额改变+=刷新;刷新();
    }
    void 布局手机(RectTransform 框,RectTransform 结算,RectTransform 详情卡)
    {
        if(!天帝移动适配.启用)return;
        var 列表口=天帝双端页面布局.滚动组(框,"回收卡片列表",32,192,940,465);
        详情.填满父区域=true;
        天帝双端页面布局.移动页(框,面板=>
        {
            float 宽=面板.rect.width,高=面板.rect.height,左宽=宽*.62f;
            天帝双端页面布局.页头(面板,"关闭回收");
            天帝双端页面布局.固定(余额条,164,4,宽-332,56);
            天帝响应布局.比例(余额.rectTransform,.06f,0,.88f,1);余额.fontSize=15;
            float 条件宽=(宽-28)/3;
            for(int i=0;i<3;i++)天帝双端页面布局.固定((RectTransform)筛选下拉[i].transform,8+i*(条件宽+6),天帝双端页面布局.页头高度,条件宽,44);
            string[] 批选名={"一键选中","回收范围设置","清空回收选择"};
            for(int i=0;i<3;i++)天帝双端页面布局.按键(面板.Find(批选名[i]) as RectTransform,8+i*(条件宽+6),118,条件宽);
            天帝双端页面布局.固定(列表口,8,168,左宽-16,高-282);
            var 内容=列表口.GetComponent<ScrollRect>().content;内容.sizeDelta=new Vector2(0,每页*66);
            for(int i=0;i<每页;i++)
            {
                float 卡宽=左宽-16;
                var r=(RectTransform)卡[i].transform;天帝双端页面布局.固定(r,0,i*66,卡宽,60);
                天帝双端页面布局.固定(图标[i].rectTransform,52,6,40,44);
                天帝双端页面布局.固定(卡名[i].rectTransform,100,4,卡宽-152,52);卡名[i].fontSize=14;
                卡价[i].gameObject.SetActive(false);卡状态[i].gameObject.SetActive(false);
                天帝双端页面布局.固定((RectTransform)回收勾选[i].transform,4,8,44,44);
            }
            天帝双端页面布局.固定(详情区,左宽,168,宽-左宽-8,高-246);
            天帝响应布局.比例(详情卡,0,0,1,1);
            天帝双端页面布局.按键((RectTransform)上页.transform,8,高-108,64);
            天帝双端页面布局.固定(页码字.rectTransform,78,高-108,左宽-156,44);
            天帝双端页面布局.按键((RectTransform)下页.transform,左宽-72,高-108,64);
            天帝双端页面布局.固定(结算,8,高-60,宽-16,54);
            天帝双端页面布局.固定(汇总.rectTransform,8,0,110,25);汇总.fontSize=14;
            天帝双端页面布局.固定(总价.rectTransform,120,0,宽-300,30);总价.fontSize=18;
            天帝双端页面布局.固定(提示.rectTransform,8,28,宽-190,26);提示.fontSize=14;
            天帝双端页面布局.按键((RectTransform)卖出.transform,宽-180,5,148);
            foreach(Transform 子 in 面板)if(子.GetComponent<Text>() is Text 文 && (文.text.StartsWith("处理闲置")||文.text.StartsWith("点击卡片")||文.text=="品阶筛选"))文.gameObject.SetActive(false);
        });
    }
    void 查看(int 槽)
    {
        var 纹=显示项(槽);if(纹==null||操作框已打开)return;焦点=纹;
        刷新();
    }
    void 设置勾选(int 槽,bool 勾上)
    {
        var 纹=显示项(槽);if(纹==null||操作框已打开)return;焦点=纹;
        if(数据.可回收(纹,out _)){if(勾上)选择.Add(纹);else 选择.Remove(纹);}刷新();
    }
    public void 刷新()
    {
        if(数据==null)return;选择.RemoveWhere(x=>!数据.可回收(x,out _));
        列表.Clear();列表.AddRange(数据.道纹.Where(x=>(筛阶<0||(int)x.品阶==筛阶)&&
            (筛类型==0||筛类型==1&&x.分类==道纹分类.属性||筛类型==2&&x.分类==道纹分类.分叉||筛类型==3&&x.是功能道纹||筛类型==4&&x.分类==道纹分类.特性||筛类型==5&&x.分类==道纹分类.转化)&&
            (筛状态==0||筛状态==1&&!x.回收锁定||筛状态==2&&x.回收锁定||筛状态==3&&数据.可回收(x,out _))));
        // 锁定只改变保护状态，获取顺序与卡片位置保持不变。
        列表.Sort((a,b)=>a.编号.CompareTo(b.编号));
        int 总=Mathf.Max(1,Mathf.CeilToInt(列表.Count/(float)每页));页=Mathf.Clamp(页,0,总-1);
        for(int i=0;i<每页;i++)
        {
            var 纹=显示项(i);卡[i].gameObject.SetActive(纹!=null);if(纹==null)continue;
            bool 可=数据.可回收(纹,out string 原因),选=选择.Contains(纹);
            图标[i].单纹=纹;图标[i].SetVerticesDirty();
            图短名[i].text=天帝道纹美术.单字(纹);
            天帝界面美术.选项((Image)卡[i].targetGraphic,选,!可);
            回收勾选[i].SetIsOnWithoutNotify(选);回收勾选[i].interactable=可&&!操作框已打开;
            锁按钮[i].设置(纹);锁按钮[i].GetComponent<Button>().interactable=!操作框已打开;
            卡名[i].text=(纹.分类==道纹分类.分叉?"分叉":纹.短名)+(天帝移动适配.启用?" · ":"\n")+天帝道纹品阶.彩色品阶文字(纹.品阶);
            卡价[i].text=天帝数值.道纹回收价(纹)+(天帝移动适配.启用?"\n灵石":" 灵石");
            卡状态[i].text=可?"回收 · #"+纹.编号:原因;
            卡状态[i].color=天帝道纹美术.次文;
        }
        余额.text="持有灵石  "+钱.灵石显示;页码字.text=(页+1)+" / "+总;上页.interactable=页>0;下页.interactable=页+1<总;
        空库存.gameObject.SetActive(列表.Count==0);空库存.text=数据.道纹.Count==0?"背包暂无道纹\n战斗拾取或开启宝盒后，可在这里回收闲置道纹。":"没有符合筛选条件的道纹\n调整顶部条件，查看其他道纹。";
        钱.预览回收(选择.ToList(),out int 金额,out _);总回收灵石=金额;
        汇总.text="已勾选  "+选择.Count+" 枚";总价.text="总回收灵石  "+金额;
        卖出.interactable=选择.Count>0&&!操作框已打开;
        if(焦点==null||!列表.Contains(焦点))焦点=列表.FirstOrDefault();
        if(焦点!=null){详情.设置(焦点,数据.可回收(焦点,out string 原因)?"回收价 "+天帝数值.道纹回收价(焦点)+" 灵石":原因);详情.gameObject.SetActive(true);}
        else 详情.gameObject.SetActive(false);
        刷新山水回收();
    }
    void 打开确认()
    {
        if(操作框已打开)return;待售=选择.OrderBy(x=>x.编号).ToList();
        if(!钱.预览回收(待售,out 预期价,out string 原因)){提示.text=原因;return;}
        foreach(var b in GetComponentsInChildren<Selectable>())b.interactable=false;
        var 根=(RectTransform)transform;确认层=区块(根,"回收确认层",0,0,1600,900);
        var 遮=图(确认层,"回收确认遮罩",0,0,1600,900,null,new Color(0,0,0,.70f));遮.raycastTarget=true;
        var 框=图(确认层,"回收确认面板",370,224,860,452,"一级面板").rectTransform;
        字文(框,"确认回收",40,24,780,58,31).name="回收确认标题";
        字文(框,"确认回收这 "+待售.Count+" 枚道纹？",40,86,780,48,28).name="回收确认数量";
        字文(框,"可获得 "+预期价+" 灵石",40,136,780,48,26).name="回收确认收益";
        var 品阶=待售.GroupBy(x=>x.品阶).OrderBy(x=>x.Key).Select(x=>x.Key+" ×"+x.Count());
        字文(框,string.Join("  ·  ",品阶),40,192,780,52,21).name="回收确认品阶";
        字文(框,"只处理本次勾选的道纹。确认后从背包移除，无法撤销。",40,262,780,48,20,天帝道纹美术.次文).name="回收不可撤销";
        键(框,"取消回收","取消",94,350,280,60,关闭确认);
        键(框,"确认回收","确认回收",486,350,280,60,确认,true);
        if(山水回收)装配山水确认(框);
        if(天帝移动适配.启用)
        {
            var 口=天帝双端页面布局.滚动组(框,"回收确认正文",40,24,780,300);
            var 重排=天帝双端页面布局.重排正文(口.GetComponent<ScrollRect>().content);
            天帝双端页面布局.移动页(框,面板=>
            {
                float 宽=面板.rect.width,高=面板.rect.height;
                天帝双端页面布局.固定(口,12,12,宽-24,高-72);重排();
                天帝双端页面布局.按键(面板.Find("取消回收") as RectTransform,12,高-52,(宽-30)/2);
                天帝双端页面布局.按键(面板.Find("确认回收") as RectTransform,18+(宽-30)/2,高-52,(宽-30)/2);
            });
        }
        刷新();
    }
    void 确认()
    {
        if(!确认已打开||待售==null)return;
        bool 成功=钱.回收道纹(待售,预期价,out string 结果);提示.text=结果;
        天帝声音.提示(成功?"YS05_回收完成":"UI04_拒绝");
        关闭确认();刷新();
    }
    public void 关闭确认()
    {if(确认层==null)return;确认层.gameObject.SetActive(false);if(Application.isPlaying)Destroy(确认层.gameObject);else DestroyImmediate(确认层.gameObject);确认层=null;待售=null;foreach(var b in GetComponentsInChildren<Selectable>())b.interactable=true;刷新();}
    void OnDestroy(){if(数据!=null)数据.状态改变-=刷新;if(钱!=null)钱.余额改变-=刷新;}
    RectTransform 区块(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    Image 图(RectTransform 父,string 名,float x,float y,float w,float h,string 素材,Color? 色=null)
    {var i=区块(父,名,x,y,w,h).gameObject.AddComponent<Image>();i.color=色??Color.white;i.raycastTarget=false;if(素材!=null)天帝界面美术.面板(i,素材,Color.white);return i;}
    Text 字文(RectTransform 父,string 文,float x,float y,float w,float h,int 大小,Color? 色=null)
    {var t=区块(父,"文字",x,y,w,h).gameObject.AddComponent<Text>();t.font=字体;t.text=文;t.fontSize=大小;t.color=天帝道纹美术.纸面文字(色??天帝道纹美术.正文);t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Truncate;天帝界面美术.文字(t);return t;}
    Button 键(RectTransform 父,string 名,string 文,float x,float y,float w,float h,Action 点击,bool 主=false)
    {var i=图(父,名,x,y,w,h,"按钮");i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;if(!string.IsNullOrEmpty(文))字文(i.rectTransform,文,8,0,w-16,h,21).alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>点击());天帝界面美术.按钮(b,主);return b;}
}
