#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝扩展道纹验证
{
    [Serializable] sealed class 报告 { public int 两两组合, 三重组合; public List<string> 通过=new List<string>(), 失败=new List<string>(), 错误=new List<string>(); }
    static 报告 r;
    static void 查(string 名,bool 对) { (对?r.通过:r.失败).Add(名); }
    static object 字段(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void 设(object o,string n,object v)=>o.GetType().GetProperty(n).SetValue(o,v);
    static object 调(object o,string n,params object[] v)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,v);
    static 天帝道纹 网(params 道纹功能[] fs)
    {
        var g=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人)); g.设置玩家等级(100);
        for(int i=0;i<fs.Length;i++)
        {
            var x=天帝道纹生成.创建(i+1,道纹分类.功能,道纹品阶.稀有,new System.Random(i+42));
            设(x,"功能",fs[i]); x.接口=天帝顺序道纹.固定接口(fs[i],3); x.词条.Clear(); x.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(fs[i]),1));
            var p=new Vector2Int(i+1,0); if(!g.获得道纹(x)||!g.解锁格子(p)||!g.放置(x,p))throw new Exception("隔离放置失败");
        }
        return g;
    }
    sealed class 夹具:IDisposable
    {
        public 天帝道纹 网格; public 天帝主角属性 人; public 天帝战斗地图 地; public 天帝战斗系统 战; public 普攻参数 参数;
        public List<战斗灵矢> 活,待; public int 命中; public float 首次伤害; public 战斗灵矢 首;
        public 夹具(params 道纹功能[] fs)
        {
            网格=网(fs); 人=new 天帝主角属性(天帝普攻.主角配置(),网格); 参数=普攻参数.读取(网格,人);
            地=new 天帝战斗地图(42,true); 战=new 天帝战斗系统(地,网格,人,战斗难度.普通);
            战.设置演示靶(new[]{new Vector2(2,0),new Vector2(4,0),new Vector2(6,0),new Vector2(8,0),new Vector2(4,2),new Vector2(4,-2)});
            战.伤害反馈+=(p,d,b)=>{if(命中==0)首次伤害=d;命中++;}; 战.发射演示(参数);
            活=(List<战斗灵矢>)字段(战,"灵矢数据"); 待=(List<战斗灵矢>)字段(战,"待加灵矢"); 首=活[0];
        }
        public void 步(float dt=.025f)=>战.推进演示弹体(dt);
        public void 到首次命中(){ for(int i=0;i<500&&命中==0;i++)步(); }
        public void 收敛(){ for(int i=0;i<1600&&(活.Count>0||待.Count>0||战.功能地面效果.Count>0);i++)步(); }
        public void Dispose()=>人.Dispose();
    }
    public static string 运行()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("请在编辑态验证");
        天帝数值同步检查.校验(); r=new 报告(); string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/扩展道纹-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(dir);
        Application.LogCallback 日志=(文,栈,类)=>{if(类==LogType.Error||类==LogType.Exception||类==LogType.Assert)r.错误.Add(文+栈);}; Application.logMessageReceived+=日志;
        try
        {
            foreach(var f in Enumerable.Range(9,天帝顺序道纹.功能数量-8).Select(i=>(道纹功能)i))
            {
                var g=网(f); g.道纹[0].接口=8; g.重算();
                using(var p=new 天帝主角属性(天帝普攻.主角配置(),g))
                { var plan=天帝顺序道纹.编译(g,p,0); 查("单口末端有效-"+f,plan.功能数==1&&plan.根弹数>0&&plan.提示.Count==0); }
                var back=天帝道纹.读取存档(g.导出存档()); 查("新增功能单口存档无重抽-"+f,back.道纹[0].功能==f&&back.道纹[0].接口==8);
            }
            for(int a=1;a<=天帝顺序道纹.功能数量;a++)for(int b=1;b<=天帝顺序道纹.功能数量;b++)
            {
                r.两两组合++;
                using(var t=new 夹具((道纹功能)a,(道纹功能)b))
                {
                    var plan=t.参数.顺序计划; var 释放=t.首.释放; bool 历史=true;
                    for(int k=0;k<1600&&(t.活.Count>0||t.待.Count>0||t.战.功能地面效果.Count>0);k++)
                    { var prior=t.活.ToDictionary(v=>v,v=>new HashSet<int>((HashSet<int>)字段(v,"独立命中"))); t.步(); 历史&=prior.All(kv=>kv.Value.All(((HashSet<int>)字段(kv.Key,"独立命中")).Contains)); }
                    查("38功能两两实战有限收敛-"+a+"/"+b,t.活.Count==0&&t.待.Count==0&&t.战.功能地面效果.Count==0&&历史&&(int)字段(释放,"顺序生成数")<=32);
                    double d=天帝顺序道纹.伤害预算(plan,8,1); 查("两两纸面及编译有限-"+a+"/"+b,plan.功能数==2&&plan.提示.All(s=>s.Contains("预算"))&&d>0&&!double.IsInfinity(d)&&!double.IsNaN(d));
                }
            }
            // 每种三重排列编译一次；实战覆盖所有两两，并补充关键三重顺序和时机。
            for(int a=1;a<=天帝顺序道纹.功能数量;a++)for(int b=1;b<=天帝顺序道纹.功能数量;b++)for(int c=1;c<=天帝顺序道纹.功能数量;c++)
            {
                r.三重组合++; var g=网((道纹功能)a,(道纹功能)b,(道纹功能)c);
                using(var p=new 天帝主角属性(天帝普攻.主角配置(),g))
                {
                    var plan=天帝顺序道纹.编译(g,p,0); double d=天帝顺序道纹.伤害预算(plan,8,1);
                    查("38功能三重编译顺序与有限预算-"+a+"/"+b+"/"+c,plan.功能数==3&&plan.提示.All(s=>s.Contains("预算"))&&plan.根弹数>0&&plan.根弹数<=32&&d>0&&!double.IsNaN(d)&&!double.IsInfinity(d));
                }
            }
            定向(); 控制与范围边界(); 数值对照(); 演示与符号();
        }
        catch(Exception e){r.错误.Add(e.ToString());}
        finally{Application.logMessageReceived-=日志; File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));}
        return dir;
    }
    static void 定向()
    {
        using(var t=new 夹具(道纹功能.延时,道纹功能.分裂))
        { t.步(.39f); 查("延时未到不生成后续不伤害",t.活.Count==1&&t.首.执行段.功能==道纹功能.延时&&t.命中==0); t.步(.02f); 查("延时到期才生成后续",t.活.Any(v=>v.执行段.功能==道纹功能.分裂)&&t.命中==0); }
        using(var t=new 夹具(道纹功能.停驻,道纹功能.分裂))
        { t.到首次命中(); var 点=t.首.位置; t.步(.59f); 查("停驻期间冻结位置不执行后续",t.首.位置==点&&t.首.等待秒>0&&t.战.顺序分裂次数==0); t.步(.02f); 查("停驻到期恢复后续飞行",t.活.Any(v=>v.执行段.功能==道纹功能.分裂)&&t.战.顺序分裂次数==0); }
        using(var t=new 夹具(道纹功能.震荡,道纹功能.分裂))
        { t.到首次命中(); int n=t.命中; t.步(.49f); 查("震荡未到不脉冲不分裂",t.命中==n&&t.战.顺序分裂次数==0); t.步(.02f); 查("震荡到期范围伤害后才分裂",t.命中>n&&t.战.顺序分裂次数==1); }
        using(var t=new 夹具(道纹功能.烙印))
        { t.到首次命中(); var e=t.战.敌人[0]; 设(e,"位置",e.位置+Vector2.up*4); t.步(.2f); 查("烙印跟随目标位置",t.首.位置==e.位置&&t.首.等待秒>0); int n=t.命中; t.步(.81f); 查("烙印在移动后落点引爆",t.命中>n&&t.战.光圈.Any(x=>x.位置==e.位置)); }
        using(var t=new 夹具(道纹功能.陨落))
        { var 点=t.首.位置; 设(t.战.敌人[0],"位置",点+Vector2.up*4); t.步(.49f); 查("陨落预警固定落点",t.首.位置==点&&t.命中==0&&t.首.等待秒>0); t.步(.02f); 查("陨落目标移开可躲",t.命中==0&&t.活.Count==0); }
        using(var t=new 夹具(道纹功能.陨落)) { t.步(.51f); 查("陨落静止目标正常受伤",t.命中>0); }
        using(var t=new 夹具(道纹功能.爆破))
        { 设(t.战.敌人[1],"位置",t.地.出生位置+new Vector2(3,0)); t.到首次命中(); 查("爆破排除主目标并命中范围内邻敌",t.命中==2&&((HashSet<int>)字段(t.首,"独立命中")).SetEquals(new[]{0,1})); }
        using(var t=new 夹具(道纹功能.击退)) { var e=t.战.敌人[0]; var 点=e.位置; t.到首次命中(); 查("击退改变实际位置",Vector2.Distance(点,e.位置)>1.4f&&t.地.可站立(e.位置)); }
        using(var t=new 夹具(道纹功能.牵引)) { var e=t.战.敌人[1]; var 点=e.位置; t.到首次命中(); 查("牵引邻敌接近命中点",e.位置.x<点.x-1.4f&&t.地.可站立(e.位置)); }
        using(var t=new 夹具(道纹功能.束缚)) { t.到首次命中(); 查("束缚实际计时",t.战.敌人[0].束缚剩余秒>.7f); t.步(.81f); 查("束缚到期解除",t.战.敌人[0].束缚剩余秒==0); }
        using(var t=new 夹具(道纹功能.跃迁)) { t.步(); 查("跃迁跳过沿途目标不命中",t.命中==0&&t.活.Count>0&&Vector2.Distance(t.活[0].位置,t.地.出生位置)>3.9f); }
        using(var t=new 夹具(道纹功能.折返,道纹功能.分裂))
        { var 起=t.首.发射点; for(int i=0;i<1200&&t.首.扩展阶段!=2;i++)t.步(); 查("折返去程不提前分裂",t.首.扩展阶段==2&&t.战.顺序分裂次数==0); for(int i=0;i<1200&&t.首.执行段.功能==道纹功能.折返;i++)t.步(); 查("折返回到发射点再续接",Vector2.Distance(t.首.位置,起)<.15f&&t.首.执行段.功能==道纹功能.分裂&&t.战.扩展功能触发次数>0); }
        using(var t=new 夹具(道纹功能.弹墙,道纹功能.分裂))
        {
            Vector2 向=Vector2.zero, 起=t.首.位置;
            for(int i=0;i<32&&向==Vector2.zero;i++)
            { var v=new Vector2(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16)); for(float d=1;d<40;d+=.25f)if(!t.地.可站立(t.首.位置+v*d,0)){向=v;起=t.首.位置+v*Mathf.Max(0,d-1);break;} }
            t.首.方向=向; t.首.位置=起; var 敌=(List<战斗敌人>)字段(t.战,"敌人数据"); 敌.Clear();
            for(int i=0;i<1200&&t.战.扩展功能触发次数==0;i++)t.步();
            查("弹墙一次反弹后才续接且不穿墙",向!=Vector2.zero&&t.战.扩展功能触发次数==1&&t.地.可站立(t.首.位置,0)&&t.首.方向.x*向.x+t.首.方向.y*向.y<0&&t.战.顺序分裂次数==0);
        }
        using(var t=new 夹具(道纹功能.拖尾)) { t.到首次命中(); 查("拖尾生成真实区域",t.战.功能地面效果.Count>0); t.收敛(); 查("拖尾伤害区有限寿命",t.战.功能地面效果.Count==0); }
        using(var t=new 夹具(道纹功能.回旋)) { var 向=t.首.方向; t.步(.1f); 查("回旋固定弯曲不索敌",t.首.方向!=向&&t.首.回旋累计角>0); }
        using(var t=new 夹具(道纹功能.波动)) { t.步(.1f); 查("波动实际横向轨迹",Math.Abs(t.首.位置.y-t.首.发射点.y)>.1f); }
        using(var t=new 夹具(Enumerable.Repeat(道纹功能.环射,12).ToArray()))
        { 查("深层环射根数与纸面计算不溢出",t.活.Count==32&&t.参数.顺序计划.根弹数==32&&天帝顺序道纹.伤害预算(t.参数.顺序计划,8,1)>0); t.收敛(); 查("深层环射预算有限",t.活.Count==0); }
    }
    static void 数值对照()
    {
        原数值对照();
        foreach(var f in new[]{道纹功能.扇射,道纹功能.环射,道纹功能.十字,道纹功能.背射})using(var t=new 夹具(f))
        {
            int n=天帝顺序道纹.多弹数量(f); var 向=t.活.Select(v=>v.方向).ToArray();
            查("多弹共用触发点并按固定方向展开-"+f,t.活.Count==n&&t.活.All(v=>v.位置==t.地.出生位置)&&向.Distinct().Count()==n);
            t.步(.05f); 查("多弹飞行不自动转向-"+f,t.活.All(v=>向.Contains(v.方向)));
        }
        using(var t=new 夹具(道纹功能.蓄势))
        {
            t.到首次命中(); var p=t.首.参数; var e=t.战.敌人[0];
            double 倍=Math.Min(天帝顺序道纹.扩展数值("charge_max"),1+t.首.已飞距离*天帝顺序道纹.扩展数值("charge_per_meter"));
            var 包=new 战斗伤害包(p.普通伤害,p.五行额外伤害,p.等级,天帝数值.取("player.skill_multiplier"),倍,p.天赋倍率,1,p.五行来源);
            查("蓄势实际伤害包含飞行距离倍率",Math.Abs(t.首次伤害-天帝数值.结算伤害(包,e.等级,e.防御,e.抗性))<.0001&&倍>1);
        }
        foreach(var f in new[]{道纹功能.回旋,道纹功能.波动,道纹功能.蓄势,道纹功能.拖尾})using(var t=new 夹具(Enumerable.Repeat(f,12).ToArray()))
            查("重复运动节点按顺序叠加有上限-"+f,天帝顺序道纹.运动层(t.首.参数.运动功能,f)==3);
    }
    static void 控制与范围边界()
    {
        using(var t=new 夹具(道纹功能.束缚))
        {
            t.到首次命中(); var e=t.战.敌人[0]; var 点=e.位置; var ai=字段(t.战,"敌人AI");
            调(ai,"移动",e,点+Vector2.up*2,.2f); 查("束缚期间AI移动实际为零",e.位置==点);
            t.步(.81f); 调(ai,"移动",e,点+Vector2.up*2,.2f); 查("束缚解除后AI恢复移动",e.位置!=点);
        }
        using(var t=new 夹具(道纹功能.拖尾))
        {
            var 敌=(List<战斗敌人>)字段(t.战,"敌人数据"); 敌.Clear();
            t.步(1.05f/t.首.参数.弹速); var zone=t.战.功能地面效果.Single();
            var e=new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通,zone.位置),战斗难度.普通,100); 设(e,"已生成",true); 敌.Add(e); t.活.Clear();
            t.步(.29f); 查("拖尾首跳前不伤害",t.命中==0); t.步(.01f); 查("拖尾首跳按0.3秒触发",t.命中==1);
            t.步(.3f); t.步(.3f); t.步(.3f); 查("拖尾完整1.2秒严格四跳并销毁",t.命中==4&&t.战.功能地面效果.Count==0);
        }
        foreach(var f in new[]{道纹功能.震荡,道纹功能.烙印})using(var t=new 夹具(f,道纹功能.齐射,道纹功能.分裂))
        { t.到首次命中(); t.收敛(); 查("关键三重延迟功能逐段触发-"+f,t.战.顺序齐射次数>0&&t.活.Count==0); }
        using(var t=new 夹具(道纹功能.延时,道纹功能.延时,道纹功能.分裂))
        { t.步(.41f); 查("连续延时第一段结束不跳过第二段",t.首.等待秒>.39f&&t.战.顺序分裂次数==0); t.步(.41f); 查("连续延时第二段结束才放行",t.首.执行段.功能==道纹功能.分裂&&t.首.等待秒==0); }
        foreach(var 类 in new[]{战斗敌人级别.精英,战斗敌人级别.王级})using(var t=new 夹具(道纹功能.束缚))
        {
            var list=(List<战斗敌人>)字段(t.战,"敌人数据"); list.Clear(); var e=new 战斗敌人(new 战斗敌人布点(类,t.地.出生位置+Vector2.right*2),战斗难度.普通,100);设(e,"已生成",true);list.Add(e);
            t.到首次命中(); 查("强敌与BOSS控制减弱-"+类,Math.Abs(e.束缚剩余秒-.8f*(类==战斗敌人级别.王级?.2f:.5f))<.00001);
        }
        using(var t=new 夹具(道纹功能.环射,道纹功能.环射,道纹功能.拖尾))
        {
            ((List<战斗敌人>)字段(t.战,"敌人数据")).Clear(); var 释放=t.首.释放;
            for(int i=0;i<20;i++)t.步();
            查("拖尾单次释放与场上数量有界",(int)字段(释放,"扩展效果数")<=64&&t.战.功能地面效果.Count<=128&&t.战.功能地面效果.Count>0);
            for(int i=0;i<5;i++) { t.战.发射演示(t.参数); for(int k=0;k<10;k++)t.步(); }
            查("连续释放不会突破全场区域预算",t.战.功能地面效果.Count<=128);
            t.收敛(); 查("多弹拖尾释放全部有限清理",t.战.功能地面效果.Count==0&&t.活.Count==0);
        }
    }
    static void 原数值对照()
    {
        double[] expected={1.95,2,1.8,1.6,8,1,1,1,1,1,2,1.48,4.5,5.8,1.6,1,1,1,5.8,8};
        for(int i=0;i<20;i++) using(var t=new 夹具((道纹功能)(i+9)))
        { double d=天帝顺序道纹.伤害预算(t.参数.顺序计划,8,1)/t.参数.顺序计划.起点.参数.伤害; 查("单功能纸面独立对照-"+(i+9),Math.Abs(d-expected[i])<.0001); }
    }
    static void 演示与符号()
    {
        for(int i=9;i<=28;i++)
        {
            var g=网((道纹功能)i); using(var p=new 天帝主角属性(天帝普攻.主角配置(),g))
            {
                var go=new GameObject("隔离新功能演示",typeof(RectTransform));
                try
                {
                    var ui=go.AddComponent<天帝道纹攻击演示>(); ui.设置参数(普攻参数.读取(g,p)); for(int k=0;k<200;k++)ui.推进演示(.025f);
                    查("新功能演示复用实战且不获资源-"+i,字段(ui,"扩展演示")!=null&&((天帝战斗系统)字段(ui,"扩展演示")).击败数==0);
                    var 图物=new GameObject("隔离道纹符号",typeof(RectTransform)); 图物.transform.SetParent(go.transform,false);
                    var 图=图物.AddComponent<天帝道纹绘图>(); 图.单纹模式=true; 图.单纹=g.道纹[0]; 图.rectTransform.sizeDelta=new Vector2(100,100);
                    using(var vh=new VertexHelper()) { typeof(天帝道纹绘图).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(图,new object[]{vh}); 查("新功能识别符号非空-"+i,vh.currentVertCount>50); }
                }
                finally{UnityEngine.Object.DestroyImmediate(go);}
            }
        }
    }
}
#endif
