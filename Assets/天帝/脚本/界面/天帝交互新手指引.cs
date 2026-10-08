using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum 修行引导步骤
{
    欢迎, 查看角色, 认识角色, 关闭角色, 打开宝盒, 获得道纹, 关闭宝盒,
    打开画布, 认识源纹, 解锁格子, 放置道纹, 接通道纹, 查看构筑,
    返回主页, 选择等级, 确认历练, 认识战斗, 移动避让, 历练说明
}

public partial class 天帝界面
{
    public 天帝交互新手指引 新手指引 { get; private set; }
    public bool 新手指引已打开 => 新手指引 != null && 新手指引.gameObject.activeSelf;
    public bool 新手指引冻结战斗 => 新手指引已打开 && 新手指引.冻结战斗;
    public void 跳过新手指引() => 新手指引?.结束();
    void 开始交互指引()
    {
        if (新手指引 != null) 删除界面对象(新手指引.gameObject);
        var 层 = 铺满((RectTransform)根.transform, "交互新手指引");
        // UGUI Dropdown展开层使用30000，教程须在其上方保留说明按钮与点击遮挡。
        var 画布 = 层.gameObject.AddComponent<Canvas>(); 画布.overrideSorting = true; 画布.sortingOrder = 31000;
        层.gameObject.AddComponent<GraphicRaycaster>();
        新手指引 = 层.gameObject.AddComponent<天帝交互新手指引>();
        新手指引.初始化(游戏, this, (RectTransform)根.transform);
    }
}

// 四周暗下，仅实际操作目标穿透射线；不用整页截图代替可交互UI。
[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝新手聚光遮罩 : MaskableGraphic, ICanvasRaycastFilter
{
    public readonly List<Rect> 聚光区域 = new List<Rect>(3);
    public bool 允许操作;
    readonly List<Rect> 剩余 = new List<Rect>(16), 临时 = new List<Rect>(16);
    void 片(VertexHelper 网, Rect 框, Color 色)
    {
        if (框.width <= 0 || 框.height <= 0) return;
        int 起 = 网.currentVertCount;
        网.AddVert(new Vector3(框.xMin, 框.yMin), 色, Vector2.zero); 网.AddVert(new Vector3(框.xMin, 框.yMax), 色, Vector2.zero);
        网.AddVert(new Vector3(框.xMax, 框.yMax), 色, Vector2.zero); 网.AddVert(new Vector3(框.xMax, 框.yMin), 色, Vector2.zero);
        网.AddTriangle(起, 起+1, 起+2); 网.AddTriangle(起, 起+2, 起+3);
    }
    protected override void OnPopulateMesh(VertexHelper 网)
    {
        网.Clear(); 剩余.Clear(); 剩余.Add(rectTransform.rect);
        foreach (var 洞 in 聚光区域)
        {
            临时.Clear();
            foreach (var 框 in 剩余)
            {
                if (!框.Overlaps(洞)) { 临时.Add(框); continue; }
                float 左=Mathf.Max(框.xMin,洞.xMin), 右=Mathf.Min(框.xMax,洞.xMax), 下=Mathf.Max(框.yMin,洞.yMin), 上=Mathf.Min(框.yMax,洞.yMax);
                临时.Add(Rect.MinMaxRect(框.xMin,框.yMin,左,框.yMax)); 临时.Add(Rect.MinMaxRect(右,框.yMin,框.xMax,框.yMax));
                临时.Add(Rect.MinMaxRect(左,框.yMin,右,下)); 临时.Add(Rect.MinMaxRect(左,上,右,框.yMax));
            }
            剩余.Clear(); 剩余.AddRange(临时);
        }
        foreach (var 框 in 剩余) 片(网,框,new Color(0,0,0,.72f));
        foreach (var 框 in 聚光区域)
        {
            var 金=new Color(.94f,.79f,.40f,.96f);
            片(网,new Rect(框.xMin-3,框.yMin-3,框.width+6,3),金); 片(网,new Rect(框.xMin-3,框.yMax,框.width+6,3),金);
            片(网,new Rect(框.xMin-3,框.yMin,3,框.height),金); 片(网,new Rect(框.xMax,框.yMin,3,框.height),金);
        }
    }
    public bool IsRaycastLocationValid(Vector2 屏幕, Camera 相机)
    {
        if (!允许操作) return true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,屏幕,相机,out var 点);
        foreach (var 框 in 聚光区域) if (框.Contains(点)) return false;
        return true;
    }
}

public sealed class 天帝交互新手指引 : MonoBehaviour
{
    天帝游戏 游戏;
    天帝界面 界面;
    RectTransform 根, 全画布, 卡;
    天帝新手聚光遮罩 遮罩;
    Text 标题, 正文, 操作字;
    Button 继续键;
    readonly List<RectTransform> 目标 = new List<RectTransform>(3);
    readonly Vector3[] 四角 = new Vector3[4];
    道纹实例 引导道纹;
    Vector2Int 引导格 = Vector2Int.right;
    Vector2 移动起点;
    string 上次正文;
    float 刷新等待;
    bool 可继续;
    public 修行引导步骤 当前步骤 { get; private set; }
    public bool 冻结战斗 => 当前步骤 != 修行引导步骤.移动避让;
    public string 当前说明 => 正文 != null ? 正文.text : "";
    public 天帝新手聚光遮罩 聚光遮罩 => 遮罩;
    public RectTransform 说明区域 => 卡;

    static RectTransform 区(Transform 父,string 名)
    {
        var r=new GameObject(名,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(父,false);return r;
    }
    static void 铺满(RectTransform r) { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero; }
    Text 字(Transform 父,string 名,int 大小,Color 色)
    {
        var r=区(父,名);var t=r.gameObject.AddComponent<Text>();t.font=游戏.默认字体;t.fontSize=大小;t.color=色;t.alignment=TextAnchor.UpperLeft;t.raycastTarget=false;return t;
    }
    Button 按键(string 名,string 文,Action 点击)
    {
        var r=区(卡,名);var 图=r.gameObject.AddComponent<Image>();
        天帝辅助页山水.轻纸(图);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=图;b.onClick.AddListener(()=>点击());
        var t=字(r,"按键文字",26,new Color(.10f,.23f,.19f));铺满(t.rectTransform);t.text=文;t.alignment=TextAnchor.MiddleCenter;
        天帝辅助页山水.按钮(b);return b;
    }
    public void 初始化(天帝游戏 主,天帝界面 UI,RectTransform 画布)
    {
        游戏=主;界面=UI;全画布=画布;根=(RectTransform)transform;
        var 黑=区(根,"黑色半透明聚光遮罩");铺满(黑);遮罩=黑.gameObject.AddComponent<天帝新手聚光遮罩>();遮罩.raycastTarget=true;
        卡=区(根,"引导说明纸笺");var 纸=卡.gameObject.AddComponent<Image>();天帝辅助页山水.纸(纸,"指引纸卷");纸.raycastTarget=true;
        标题=字(卡,"引导步骤标题",32,new Color(.12f,.24f,.19f));
        正文=字(卡,"引导详细说明",24,new Color(.16f,.23f,.19f));正文.lineSpacing=1.1f;
        继续键=按键("继续引导","下一步",继续);
        var 跳=按键("跳过引导","跳过引导",结束);
        操作字=继续键.GetComponentInChildren<Text>();跳.GetComponentInChildren<Text>().fontSize=24;
        if(游戏.道纹数据.已放置.TryGetValue(Vector2Int.right,out var 已有))引导道纹=已有;
        else 引导道纹=游戏.道纹数据?.道纹.Find(x=>!x.格子.HasValue)??游戏.道纹数据?.道纹.Find(x=>x.生效)??游戏.道纹数据?.道纹.Find(x=>true);
        if (引导道纹?.格子.HasValue==true) 引导格=引导道纹.格子.Value;
        转到(修行引导步骤.欢迎);
    }
    void LateUpdate() { 刷新等待-=Time.unscaledDeltaTime;if(刷新等待<=0){刷新等待=.08f;更新引导();} }
    RectTransform 找(string 名,string 父名=null)
    {
        foreach(var t in 全画布.GetComponentsInChildren<RectTransform>())
            if(t.name==名&& !t.IsChildOf(根) && (父名==null||t.parent?.name==父名))return t;
        return null;
    }
    void 聚光(string 名,string 父名=null) { var r=找(名,父名);if(r!=null)目标.Add(r); }
    void 转到(修行引导步骤 步)
    {
        当前步骤=步;目标.Clear();上次正文=null;
        switch(步)
        {
            case 修行引导步骤.查看角色:聚光("角色");break;
            case 修行引导步骤.认识角色:聚光("角色正文");break;
            case 修行引导步骤.关闭角色:聚光("关闭角色");break;
            case 修行引导步骤.打开宝盒:聚光("宝盒");break;
            case 修行引导步骤.获得道纹:聚光("抽取","属性宝盒");break;
            case 修行引导步骤.关闭宝盒:聚光("关闭","宝盒面板");break;
            case 修行引导步骤.打开画布:聚光("道纹");break;
            case 修行引导步骤.认识源纹:界面.道纹页?.回到源点();break;
            case 修行引导步骤.解锁格子:界面.道纹页?.定位格子(引导格);break;
            case 修行引导步骤.放置道纹:
                var 候选=界面.道纹页?.定位引导候选(引导道纹);if(候选!=null)目标.Add(候选);break;
            case 修行引导步骤.接通道纹:if(天帝移动适配.启用)聚光("旋转60°");break;
            case 修行引导步骤.查看构筑:聚光("实时构筑预览");if(目标.Count==0)聚光("战斗变化");break;
            case 修行引导步骤.返回主页:聚光("返回主页");break;
            case 修行引导步骤.选择等级:聚光("地图等级下拉");break;
            case 修行引导步骤.确认历练:聚光("开始历练");聚光("开始游戏");break;
            case 修行引导步骤.认识战斗:聚光("主角战斗状态");聚光("波次敌人信息");break;
            case 修行引导步骤.移动避让:
                移动起点=游戏.战斗场景?.玩家位置??Vector2.zero;if(天帝移动适配.启用)聚光("移动摇杆");break;
        }
        更新引导(false);
    }
    public void 继续()
    {
        if(!可继续||!gameObject.activeSelf)return;
        if(当前步骤==修行引导步骤.历练说明){结束();return;}
        if(当前步骤==修行引导步骤.获得道纹){转到(修行引导步骤.关闭宝盒);return;}
        if(当前步骤==修行引导步骤.选择等级){界面.关闭等级下拉();转到(修行引导步骤.确认历练);return;}
        if(当前步骤==修行引导步骤.解锁格子&& !游戏.道纹数据.格已解锁(引导格)) { 转到(修行引导步骤.查看构筑);return; }
        if(当前步骤==修行引导步骤.认识源纹&&引导道纹==null){转到(修行引导步骤.查看构筑);return;}
        if(当前步骤==修行引导步骤.认识源纹&&引导道纹.格子.HasValue&&!引导道纹.生效&&引导道纹.格子.Value!=Vector2Int.right){转到(修行引导步骤.查看构筑);return;}
        转到(当前步骤+1);
    }
    public void 结束()
    {
        if(!gameObject.activeSelf)return;
        if(界面.道纹页!=null)界面.道纹页.引导讲解中=false;
        gameObject.SetActive(false);界面.移动失去焦点();游戏.完成新手指引();
    }
    public void 更新引导() => 更新引导(true);
    void 更新引导(bool 检查操作)
    {
        if(游戏==null||游戏.道纹数据==null||!gameObject.activeSelf)return;
        if(游戏.阶段==游戏阶段.标题||游戏.阶段==游戏阶段.序章||游戏.阶段==游戏阶段.源道纹选择){gameObject.SetActive(false);return;}
        var 网=游戏.道纹数据;var 页=界面.道纹页;
        if(页!=null&&!页.引导讲解中){页.引导讲解中=true;页.指针离开();}
        if(检查操作)
        {
            switch(当前步骤)
            {
                case 修行引导步骤.查看角色:if(界面.角色已打开){转到(修行引导步骤.认识角色);return;}break;
                case 修行引导步骤.关闭角色:if(!界面.角色已打开){转到(修行引导步骤.打开宝盒);return;}break;
                case 修行引导步骤.打开宝盒:if(界面.宝盒已打开){转到(修行引导步骤.获得道纹);return;}break;
                case 修行引导步骤.获得道纹:
                    if(引导道纹==null&&网.道纹.Count>0){引导道纹=网.道纹[网.道纹.Count-1];转到(修行引导步骤.关闭宝盒);return;}break;
                case 修行引导步骤.关闭宝盒:if(!界面.宝盒已打开){转到(修行引导步骤.打开画布);return;}break;
                case 修行引导步骤.打开画布:if(游戏.阶段==游戏阶段.道纹){转到(修行引导步骤.认识源纹);return;}break;
                case 修行引导步骤.解锁格子:if(网.格已解锁(引导格)){转到(修行引导步骤.放置道纹);return;}break;
                case 修行引导步骤.放置道纹:if(引导道纹?.格子.HasValue==true){引导格=引导道纹.格子.Value;转到(修行引导步骤.接通道纹);return;}break;
                case 修行引导步骤.返回主页:if(游戏.阶段==游戏阶段.主页){转到(修行引导步骤.选择等级);return;}break;
                case 修行引导步骤.确认历练:
                    if(游戏.阶段==游戏阶段.战斗){转到(修行引导步骤.认识战斗);return;}
                    if(界面.确认已打开&&目标.Count>0&&目标[0]?.name!="确认进入"){目标.Clear();聚光("确认进入");}
                    else if(!界面.确认已打开&&游戏.阶段==游戏阶段.主页&&(目标.Count==0||目标[0]==null)){目标.Clear();聚光("开始历练");聚光("开始游戏");}
                    break;
                case 修行引导步骤.移动避让:
                    if(游戏.战斗场景?.战斗?.玩家死亡==true||(游戏.战斗场景!=null&&Vector2.Distance(移动起点,游戏.战斗场景.玩家位置)>=2)){转到(修行引导步骤.历练说明);return;}
                    break;
            }
        }
        可继续=false;遮罩.允许操作=true;
        string 名="",文="";
        switch(当前步骤)
        {
            case 修行引导步骤.欢迎:
                名="踏上修行之路";文="接下来跟着金色框操作，亲手完成第一次构筑与历练。\n黑色区域会暂时挡住其他按钮；每一步只讲当前要做的事。随时可跳过，主页“修行指引”可以重新开始。";可继续=true;遮罩.允许操作=false;break;
            case 修行引导步骤.查看角色:
                名="先认识自己";文="点击亮起的“角色”。\n这里可以查看等级、本命天赋，以及力量、速度、智力带来的生命、攻击、移动等属性。";break;
            case 修行引导步骤.认识角色:
                名="基础属性与成长";文="力量、速度、智力是构筑的基础；道纹接通后，增益才会算进角色。\n升级获得技能点，用来扩展道纹格子。生命、护盾不会因为换道纹或升级自动补满。";可继续=true;遮罩.允许操作=false;break;
            case 修行引导步骤.关闭角色:名="回到主页";文="点击角色页右上角的“关闭”。\n接下来认识灵石和道纹宝盒。";break;
            case 修行引导步骤.打开宝盒:名="准备第一枚道纹";文="点击“宝盒”。\n灵石主要用于抽取道纹。属性盒提供属性增益；功能盒改变攻击方式，未必直接增加伤害。";break;
            case 修行引导步骤.获得道纹:
                名="开一次属性宝盒";
                if(引导道纹!=null){文="你已经持有道纹，本次讲解会使用现有道纹，不必再消费。\n每次开属性盒花费"+天帝宝盒.价格(宝盒种类.属性)+"灵石，获得一枚随机属性道纹；开盒后自动进入藏匣。";可继续=true;遮罩.允许操作=false;}
                else if(游戏.宝盒数据.灵石<天帝宝盒.价格(宝盒种类.属性)){文="当前灵石不足，先去青岚原击败敌人收集灵石与道纹。\n点击下一步继续认识画布；不会补发资源，也不会强迫你购买。";可继续=true;遮罩.允许操作=false;}
                else 文="点击属性宝盒下的“抽取”。\n这次会真实消费"+天帝宝盒.价格(宝盒种类.属性)+"灵石，获得一枚随机道纹。先不必连续抽取，留一些灵石给后续构筑。";break;
            case 修行引导步骤.关闭宝盒:
                名="道纹已进入藏匣";文=(引导道纹!=null?"获得／持有：“"+引导道纹.名称+"”。\n":"暂时没有道纹，之后可以从战斗收集。\n")+"点击右上角“关闭”，回到主页打开道纹画布。";break;
            case 修行引导步骤.打开画布:名="打开道纹构筑";文="点击“道纹”。\n这里是真正发挥道纹效果的地方：先解锁格子，再放置道纹，把接口与源纹连起来。";break;
            case 修行引导步骤.认识源纹:
                名="源纹是构筑起点";文="金框中的中心六边形是本命天赋源纹，不能移动或卸下。\n初始只向右传导，所以第一枚道纹放在右侧格子，并让它的左接口对接。仅同圈或向外传导。";
                if(引导道纹==null)文+="\n暂时没有可练习的道纹，下一步先认识构筑预览。";
                else if(引导道纹.格子.HasValue&&!引导道纹.生效&&引导道纹.格子.Value!=Vector2Int.right)文="中心是不能移动或卸下的本命天赋源纹；接口对接后仅同圈或向外传导。\n现有道纹已放在其他位置且未接通，本次保留布局，先认识预览；之后可自由调整通路。";
                可继续=true;遮罩.允许操作=false;break;
            case 修行引导步骤.解锁格子:
                名="花1技能点解锁格子";文=网.技能点>0?"点击源纹右侧亮起的空格，真实花费1技能点。\n解锁只开放位置，不会自动放入道纹。每升一级获得1技能点；格子解锁后可反复更换道纹。":"技能点已用完，不能解锁新格子。\n先去历练升级，再回来扩展构筑。点击下一步继续认识预览。";可继续=网.技能点==0;break;
            case 修行引导步骤.放置道纹:
                名="把道纹放进亮起的格子";文=天帝移动适配.启用?"先点金框中的道纹卡，再点亮起的已解锁格子。\n手机“旋转60°”可调整待放朝向；候选旋转只改预览，真正放置时才提交。":"按住金框中的道纹卡，拖到亮起的已解锁格子后松开。\n拖动时按鼠标右键可旋转接口。先放进去，下一步检查是否接通。";break;
            case 修行引导步骤.接通道纹:
                名="让接口真正接通";
                if(引导道纹?.生效==true){文="已经接通！亮起的连线代表当前道纹生效。\n放在格子里还不够：相邻两枚道纹的接口需要互相对准，并满足传导方向。点击下一步查看实际增益。";可继续=true;遮罩.允许操作=false;}
                else 文=天帝移动适配.启用?"点击刚放入的道纹，再按“旋转60°”，直到左接口接上源纹的右口。\n旋转已放道纹会真实改变构筑；下方连接提示会说明接通或失败的原因。":"在刚放入的道纹上点击鼠标右键，每次旋转60°，直到左接口对准源纹的右口。\n连接成功后，此处会显示已接通，再点下一步。";break;
            case 修行引导步骤.查看构筑:
                名="看懂战斗变化";文="右侧预览读取当前真实构筑：伤害、数量、分裂、连锁，以及接口执行顺序。\n“加成来源”可追查每项增益。特性道纹接通后，还要满足自己的激活条件；未接通道纹不会提供加成。";可继续=true;遮罩.允许操作=false;break;
            case 修行引导步骤.返回主页:名="准备第一次历练";文="点击右上角“返回主页”。\n无需道纹也能战斗；构筑暂未完成时，仍可先去收集经验和资源。";break;
            case 修行引导步骤.选择等级:
                名="选择适合的地图等级";文="从一级青岚原开始最容易熟悉操作。\n打开亮起的等级框可以调整档位：等级越高，敌人与掉落道纹也越强。确认自己选的等级后点击下一步。";可继续=true;
                var 下拉=找("Dropdown List");if(下拉!=null&&!目标.Contains(下拉))目标.Add(下拉);break;
            case 修行引导步骤.确认历练:
                名=界面.确认已打开?"确认这一场挑战":"出发前往青岚原";
                文=界面.确认已打开?"确认框会显示地图等级、敌人数量与挑战信息。\n确认选择后点击亮起的“确认进入”；之后会讲解手动技能与移动避让。":"点击亮起的“开始历练”，查看这场挑战的确认信息。\n进入地图不会额外消耗灵石，战斗技能需要手动释放并消耗灵力。";
                if(游戏.阶段==游戏阶段.战斗加载){文="正在加载青岚原，请稍候……\n引导会在地图加载完成后继续。";遮罩.允许操作=false;}break;
            case 修行引导步骤.认识战斗:
                名="瞄准施法，主动避让";文=天帝移动适配.启用?"左侧摇杆移动，右侧拖动技能瞄准、松手释放；短按沿最近方向释放。\n底部球体显示生命、灵力与护盾，施法消耗灵力；闪避键沿瞄准方向移动，获得0.1秒无敌。阅读讲解时战斗暂停。":"WASD移动，鼠标瞄准，按1—6释放对应道纹；不按键不会攻击。\n底部球体显示生命、灵力与护盾，施法消耗灵力；空格朝鼠标方向闪避，获得0.1秒无敌。阅读讲解时战斗暂停。";可继续=true;遮罩.允许操作=false;break;
            case 修行引导步骤.移动避让:
                名="试着移动一小段";文=(天帝移动适配.启用?"拖动亮起的左下角摇杆，移动至少2米。":"按W/A/S/D或方向键，移动至少2米；Shift可以跑步。")+"\n这一步战斗正常进行。离开暖红预警范围，留意追来的敌人；瞄准后主动释放技能。";break;
            case 修行引导步骤.历练说明:
                名="开始自己的修行";文="击败敌人获得经验；道纹、通货和灵石会全图自动入库，不需要跑去捡。\n死亡或离场也保留已获得资源。暂停页可查看本局构筑与资源；返回主页后用新道纹和技能点继续扩展。";可继续=true;遮罩.允许操作=false;break;
        }
        标题.text=((int)当前步骤+1)+" / 19 · "+名;
        if(正文.text!=文)正文.text=文;
        继续键.interactable=可继续;操作字.text=可继续?(当前步骤==修行引导步骤.历练说明?"开始修行":"下一步"):"等待你的操作";
        重排();
    }
    Rect 边(RectTransform r)
    {
        r.GetWorldCorners(四角);var a=(Vector2)根.InverseTransformPoint(四角[0]);var b=(Vector2)根.InverseTransformPoint(四角[2]);
        return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
    }
    // 教程会限制其他操作，必须先把滚动列表中的实际按钮移入可见视口。
    void 显露目标(RectTransform r)
    {
        for(var 父=r.parent;父!=null&&父!=全画布;父=父.parent)
        {
            var 滚=父.GetComponent<ScrollRect>();
            if(滚==null||滚.content==null||滚.viewport==null||!r.IsChildOf(滚.content))continue;
            r.GetWorldCorners(四角);
            var 左下=(Vector2)滚.viewport.InverseTransformPoint(四角[0]);
            var 右上=(Vector2)滚.viewport.InverseTransformPoint(四角[2]);var 口=滚.viewport.rect;
            Vector2 差=Vector2.zero;
            if(滚.vertical&&右上.y-左下.y<=口.height-16)
                差.y=左下.y<口.yMin+8?口.yMin+8-左下.y:右上.y>口.yMax-8?口.yMax-8-右上.y:0;
            if(滚.horizontal&&右上.x-左下.x<=口.width-16)
                差.x=左下.x<口.xMin+8?口.xMin+8-左下.x:右上.x>口.xMax-8?口.xMax-8-右上.x:0;
            if(差.sqrMagnitude<.01f)continue;
            滚.StopMovement();
            滚.content.anchoredPosition+=(Vector2)滚.content.parent.InverseTransformVector(滚.viewport.TransformVector(差));
            Canvas.ForceUpdateCanvases();
        }
    }
    Rect 可见边(RectTransform r)
    {
        var 框=边(r);
        for(var 父=r.parent;父!=null&&父!=全画布;父=父.parent)
            if(父.GetComponent<RectMask2D>()!=null)框=交集(框,边((RectTransform)父));
        return 交集(框,根.rect);
    }
    static Rect 交集(Rect a,Rect b)
        => Rect.MinMaxRect(Mathf.Max(a.xMin,b.xMin),Mathf.Max(a.yMin,b.yMin),Mathf.Max(Mathf.Max(a.xMin,b.xMin),Mathf.Min(a.xMax,b.xMax)),Mathf.Max(Mathf.Max(a.yMin,b.yMin),Mathf.Min(a.yMax,b.yMax)));
    void 格聚光(Vector2Int 格)
    {
        var 图=界面.道纹页?.画布;if(图==null)return;
        var 中=图.格位置(格)*图.缩放+图.平移;float 半=天帝道纹.半径*图.缩放;
        var a=(Vector2)根.InverseTransformPoint(图.rectTransform.TransformPoint(中-Vector2.one*半));
        var b=(Vector2)根.InverseTransformPoint(图.rectTransform.TransformPoint(中+Vector2.one*半));
        遮罩.聚光区域.Add(Rect.MinMaxRect(a.x-5,a.y-5,b.x+5,b.y+5));
    }
    void 重排()
    {
        if(上次正文!=正文.text)Canvas.ForceUpdateCanvases();
        遮罩.聚光区域.Clear();
        foreach(var r in 目标)if(r!=null&&r.gameObject.activeInHierarchy)
        {
            显露目标(r);var 框=可见边(r);if(框.width>0&&框.height>0)遮罩.聚光区域.Add(框);
        }
        if(当前步骤==修行引导步骤.认识源纹)格聚光(Vector2Int.zero);
        if(当前步骤==修行引导步骤.解锁格子||当前步骤==修行引导步骤.放置道纹||当前步骤==修行引导步骤.接通道纹)格聚光(引导格);
        遮罩.SetVerticesDirty();
        var 安=全画布.Find("安全区") as RectTransform;var 范围=安!=null?边(安):根.rect;
        bool 手机=天帝移动适配.启用;float 宽=Mathf.Min(手机?740:620,范围.width-40);
        标题.fontSize=手机?42:25;正文.fontSize=手机?36:22;操作字.fontSize=手机?42:22;
        标题.fontStyle=正文.fontStyle=FontStyle.Normal;
        天帝双端页面布局.固定(标题.rectTransform,36,手机?100:94,宽-72,手机?90:56);
        float 正文顶=手机?200:162;
        天帝双端页面布局.固定(正文.rectTransform,32,正文顶,宽-64,100);
        float 按键高=手机?132:48;
        float 正文高=正文.preferredHeight+12;float 高=Mathf.Min(范围.height-32,正文高+正文顶+48+按键高);
        天帝双端页面布局.固定(正文.rectTransform,32,正文顶,宽-64,高-正文顶-48-按键高);
        var 继续=(RectTransform)继续键.transform;var 跳=卡.Find("跳过引导") as RectTransform;
        if(跳!=null)跳.GetComponentInChildren<Text>().fontSize=手机?42:24;
        天帝双端页面布局.固定(跳,28,高-22-按键高,(宽-72)*.42f,按键高);天帝双端页面布局.固定(继续,44+(宽-72)*.42f,高-22-按键高,(宽-72)*.58f,按键高);
        Rect 最佳=new Rect(范围.center.x-宽/2,范围.center.y-高/2,宽,高);float 最小=float.MaxValue;
        var 候选=new[]{new Rect(范围.xMin+16,范围.yMax-高-16,宽,高),new Rect(范围.xMax-宽-16,范围.yMax-高-16,宽,高),new Rect(范围.xMin+16,范围.yMin+16,宽,高),new Rect(范围.xMax-宽-16,范围.yMin+16,宽,高)};
        if(遮罩.聚光区域.Count>0)foreach(var c in 候选)
        {
            float 重叠=0;foreach(var 洞 in 遮罩.聚光区域)重叠+=Mathf.Max(0,Mathf.Min(c.xMax,洞.xMax)-Mathf.Max(c.xMin,洞.xMin))*Mathf.Max(0,Mathf.Min(c.yMax,洞.yMax)-Mathf.Max(c.yMin,洞.yMin));
            if(重叠<最小){最小=重叠;最佳=c;}
        }
        卡.anchorMin=卡.anchorMax=卡.pivot=new Vector2(.5f,.5f);卡.sizeDelta=new Vector2(宽,高);卡.anchoredPosition=最佳.center;
        天帝辅助页山水.按钮(继续键); if(跳!=null)天帝辅助页山水.按钮(跳.GetComponent<Button>());
        上次正文=正文.text;
    }
}
