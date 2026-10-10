#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 全部测试使用隔离模型与演示敌人，不启动宿主或读写玩家档案。
public static class 天帝特性与形态验证
{
    [Serializable] sealed class 报告 {public List<string> 通过=new List<string>(),失败=new List<string>(),错误=new List<string>();}
    static 报告 r;
    static void 查(string name,bool ok)=>(ok?r.通过:r.失败).Add(name);
    static object 字段(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);
    static void 写(object o,string name,object v)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(o,v);
    static void 设(object o,string name,object v)=>o.GetType().GetProperty(name).SetValue(o,v);
    static object 调(object o,string name,params object[] args)
    {
        var m = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault(x => x.Name == name && x.GetParameters().Length == args.Length);
        if (m == null) throw new MissingMethodException(o.GetType().Name, name + "/" + args.Length);
        return m.Invoke(o, args);
    }
    static void 放(天帝道纹 g,道纹实例 rune,Vector2Int p)
    {
        bool got = g.获得道纹(rune); var unlock = got ? g.尝试解锁格子(p) : 道纹解锁结果.已解锁;
        bool placed = got && unlock == 道纹解锁结果.成功 && g.放置(rune,p);
        if (!placed) throw new Exception("隔离夹具放置失败 " + p + " · 获得=" + got + " · 解锁=" + unlock + " · 技能点=" + g.技能点 + " · 已解锁=" + g.格已解锁(p));
    }
    static void 放(天帝道纹 g,道纹实例 rune,int x,int y)=>放(g,rune,new Vector2Int(x,y));
    static 道纹实例 功(int id,道纹功能 f)
    {var a=天帝道纹生成.创建(id,道纹分类.功能,道纹品阶.稀有,new System.Random(id));设(a,"功能",f);a.接口=天帝顺序道纹.固定接口(f,3);a.词条.Clear();a.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(f),1));return a;}
    static 天帝道纹 新()=>new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
    static 天帝道纹 特网(int id,道纹品阶 grade=道纹品阶.普通)
    {
        var g=新();g.设置玩家等级(100);放(g,天帝特性道纹.创建(1,道纹分类.特性,id,grade,1,0),1,0);
        // 夹具需要覆盖 15 个属性分支、15 个叶子和 5 个功能，全部放在合法范围内。
        // 属性分支向下，功能链从最外层向上延伸，保持真实接口与向外传导规则。
        var 分叉位 = new List<Vector2Int>();
        for (int q = 2; q <= 15; q++) 分叉位.Add(new Vector2Int(q, 0));
        分叉位.Add(new Vector2Int(15, 1));
        var 叶位 = new List<Vector2Int>();
        for (int q = 2; q <= 15; q++) 叶位.Add(new Vector2Int(q, -1));
        叶位.Add(new Vector2Int(15, -1));
        var 功能位 = new[] { new Vector2Int(14, 1), new Vector2Int(14, 2), new Vector2Int(14, 3), new Vector2Int(14, 4), new Vector2Int(14, 5) };
        int x = 2, i = 0;
        foreach(var a in 天帝道纹属性.非功能属性)
        {
            var b=天帝道纹生成.创建(x,道纹分类.分叉,道纹品阶.普通,new System.Random(x));b.接口 = i < 14 ? 27 : 31;放(g,b,分叉位[i]);
            var leaf=天帝道纹生成.创建(100+x,道纹分类.属性,道纹品阶.普通,new System.Random(x),道纹属性分组.基础);leaf.接口 = i < 14 ? 2 : 32;leaf.词条.Clear();leaf.词条.Add(道纹词条.从定点(a,1000000));放(g,leaf,叶位[i]);x++;i++;
        }
        i = 0;
        foreach(var f in new[]{道纹功能.齐射,道纹功能.分裂,道纹功能.连锁,道纹功能.回旋,道纹功能.爆破})
        {
            int 位 = i++;
            var fun = 功(200+x,f); fun.接口 = 天帝顺序道纹.固定接口(f, 4);放(g,fun,功能位[位]);x++;
        }
        return g;
    }
    sealed class 战夹:IDisposable
    {
        public 天帝道纹 网;public 天帝主角属性 人;public 天帝战斗系统 战;public object 状态;
        public int 命中;public float 总伤,原移速;
        public 战夹(int id)
        {
            网=特网(id);人=new 天帝主角属性(天帝普攻.主角配置(),网);原移速=人.移动速度;战=new 天帝战斗系统(new 天帝战斗地图(42,true),网,人,战斗难度.普通);
            战.设置演示靶(new[]{new Vector2(2,0),new Vector2(3,1),new Vector2(3,-1),new Vector2(4,0),new Vector2(5,2)});
            战.伤害反馈+=(p,d,isPlayer)=>{if(!isPlayer){命中++;总伤+=d;}};
            战.推进特性演示(.01f);状态=((IDictionary)字段(战,"特性运行表"))[1];
        }
        public void 步(float seconds){for(float t=0;t<seconds-.00001f;t+=.025f)战.推进特性演示(Mathf.Min(.025f,seconds-t));}
        public bool 施放(){写(状态,"冷却",0f);return (bool)调(战,"施放特性",状态);}
        public void Dispose(){战.清理特性战斗();人.Dispose();}
    }
    public static string 运行()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("请在编辑态验证");天帝数值同步检查.校验();r=new 报告();
        string dir=Path.Combine(天帝构建工具.项目根,"生成/验证/特性与形态-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)r.错误.Add(m+s);};Application.logMessageReceived+=log;
        try{条件();转化();战斗();特性边界();形态();掉落();}
        catch(Exception e){r.错误.Add(e.ToString());}
        finally{Application.logMessageReceived-=log;File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));}
        return dir;
    }
    static void 条件()
    {
        for(int id=1;id<=38;id++)
        {
            var g=特网(id);var a=g.特性视图.特性.Single();查("T"+id+"下游功能激活且不吞攻击",a.激活&&!a.同名覆盖&&g.弹槽道纹[0].Count>=24);
            using(var p=new 天帝主角属性(天帝普攻.主角配置(),g))查("T"+id+"原五功能顺序不变",天帝顺序道纹.编译(g,p,0).功能数==5);
            for(int i=0;i<天帝特性道纹.数值(id,"condition_count");i++)
            {
                var attr=(道纹属性)Enum.Parse(typeof(道纹属性),天帝特性道纹.文本(id,"条件."+i+".0"));if(天帝道纹属性.是功能(attr))continue;
                var leaf=g.道纹.Single(x=>x.分类==道纹分类.属性&&x.属性==attr);double need=天帝特性道纹.门槛(g.道纹[0],i);
                leaf.词条[0]=道纹词条.从定点(attr,(int)Math.Round(need*100)-1);g.重算();查("T"+id+"条件"+attr+"少0.01关闭",!g.特性视图.特性.Single().激活);
                leaf.词条[0]=道纹词条.从定点(attr,(int)Math.Round(need*100));g.重算();查("T"+id+"条件"+attr+"刚好激活",g.特性视图.特性.Single().激活);
                leaf.词条[0]=道纹词条.从定点(attr,1000000);g.重算();
            }
            g.收回(g.道纹[0]);查("T"+id+"卸下关闭",!g.特性视图.特性.Single().激活);
        }
        foreach(var kind in new[]{道纹分类.特性,道纹分类.转化})for(int id=1;id<=(kind==道纹分类.特性?38:8);id++)for(int grade=0;grade<8;grade++)for(int dir=0;dir<3;dir++)
        {
            var g=新();g.设置玩家等级(100);var rune=天帝特性道纹.创建(1,kind,id,(道纹品阶)grade,100,dir);设(rune,"回收锁定",true);放(g,rune,1,0);
            var loaded=天帝道纹.读取存档(g.导出存档()).道纹.Single();查(kind+" "+id+"阶"+grade+"方向"+dir+"存档",loaded.接口==rune.接口&&loaded.特性编号==id&&loaded.回收锁定&&loaded.词条.Count==0&&loaded.物品等级==100);
            var currency=new 天帝通货(g,42,10);查(kind+" "+id+"阶"+grade+"方向"+dir+"全部通货拒绝",天帝通货.可用种类.All(c=>!currency.可使用(c,rune,0,out _)&&!currency.使用(c,rune,0,out _)));
        }
    }
    static void 转化()
    {
        for(int id=1;id<=8;id++)
        {
            var g=新();g.设置玩家等级(100);var c=天帝特性道纹.创建(1,道纹分类.转化,id,道纹品阶.稀有,30,0);放(g,c,1,0);
            var p=天帝道纹生成.创建(2,道纹分类.属性,道纹品阶.普通,new System.Random(42),道纹属性分组.普通,30);p.接口=9;p.词条.Clear();p.词条.Add(道纹词条.从定点(道纹属性.血量,5306));放(g,p,2,0);
            var target=(道纹属性)Enum.Parse(typeof(道纹属性),天帝数值配置.取文本("trait_runes.converters."+(id-1)+".目标"));var v=g.读取有效词条(p).Single();
            double expected=53.06/天帝特性道纹.中值(道纹属性.血量,30)*.9*天帝特性道纹.中值(target,30);
            查("转化"+id+"按正式中值而非复制数值",v.属性==target&&Math.Abs(v.实际数值-Math.Round(expected,2,MidpointRounding.AwayFromZero))<.000001&&p.词条[0].实际数值==53.06);
            g.收回(c);查("转化"+id+"卸下恢复原词条",g.读取有效词条(p).Single().属性==道纹属性.血量&&g.读取有效词条(p).Single().实际数值==53.06);
        }
        var net=新();net.设置玩家等级(100);放(net,天帝特性道纹.创建(1,道纹分类.转化,1,道纹品阶.稀有,1,0),1,0);放(net,天帝特性道纹.创建(2,道纹分类.转化,2,道纹品阶.稀有,1,0),2,0);
        var leaf=天帝道纹生成.创建(3,道纹分类.属性,道纹品阶.普通,new System.Random(42),道纹属性分组.基础);leaf.接口=9;放(net,leaf,3,0);查("不同转化目标冲突保留原属性",!net.特性视图.虚拟词条.ContainsKey(3));
    }
    static void 战斗()
    {
        for(int id=1;id<=38;id++)using(var t=new 战夹(id))
        {
            t.人.设置当前资源(t.人.最大血量*.5f,t.人.最大灵力*.5f,0);t.步(1.05f);int before=t.战.特性施放次数;
            switch(id)
            {
                case 12:写(t.状态,"冷却",0f);t.步(.85f);调(t.战,"特性根命中",t.战.敌人[0],0);break;
                case 13:double expected=Math.Min(天帝特性道纹.取("limits.walk_max"),天帝数值.计算主角(100,t.网.生效加成,t.网.天赋).移速*1.2);查("疾行主页及战场同样生效",Math.Abs(t.人.移动速度-t.原移速)<.001f&&Math.Abs(t.原移速-expected)<.001f);break;
                case 15:查("灵算技能急速生效",t.人.技能急速>=.1f);break;
                case 17:写(t.状态,"冷却",0f);调(t.战,"特性受伤后",t.人.最大血量*.11f,0f,false);break;
                case 18:float hp=t.人.当前血量;t.步(6f);查("归元未受伤恢复",t.人.当前血量>hp);break;
                case 21:t.步(.9f);查("砺甲站稳减伤",(float)调(t.战,"特性受伤前",100f,true,t.战.敌人[0])<100);break;
                case 22:写(t.状态,"冷却",0f);调(t.战,"特性受伤后",100f,0f,false);t.步(.025f);break;
                case 24:写(t.状态,"冷却",0f);调(t.战,"特性受伤后",1f,1f,true);break;
                case 26:写(t.状态,"冷却",0f);for(int n=0;n<8;n++)调(t.战,"特性根释放",0,(Vector2?)null);break;
                default:查("T"+id+"明确施放成功",t.施放());break;
            }
            t.步(.4f);
            if(id!=13&&id!=15&&id!=18&&id!=21)查("T"+id+"事件或定时触发一次",t.战.特性施放次数>before);
            if(new[]{1,3,4,5,7,8,9,11,17,20,22,29,30,31,34,35}.Contains(id)){t.步(7f);查("T"+id+"真实命中伤害",t.命中>0&&t.总伤>0);}
            if(id==32)查("种实首次接触只恢复而不凭空加伤",t.人.当前血量>t.人.最大血量*.5f&&t.总伤==0);
            if(id==14){var e=t.战.特性效果列表.First(x=>x.诱饵);float hp=t.人.当前血量;调(t.战,"特性目标受击",new 战斗伤害包(100,0,100),e.位置,true);查("诱饵受击不伤玩家",t.人.当前血量==hp);}
            if(id==23||id==26){查("临时盾实际授予",t.战.特性临时护盾>0);t.网.收回(t.网.道纹[0]);t.步(.025f);查("卸下清除所属临时盾",t.战.特性临时护盾==0);}
            t.战.清理特性战斗();查("T"+id+"离场清理效果盾",t.战.特性效果列表.Count==0&&t.战.特性临时护盾==0);
        }
        using(var t=new 战夹(3)){t.施放();var e=t.战.特性效果列表.ToArray();查("数量增加覆盖方向共享同次历史",e.Length==2&&e[0].方向!=e[1].方向&&ReferenceEquals(字段(e[0],"命中"),字段(e[1],"命中")));}
        using(var t=new 战夹(4)){t.施放();查("召唤数量分配但总伤预算不复制",t.战.特性效果列表.Count==2&&Math.Abs(t.战.特性效果列表.Sum(e=>(double)字段(e,"伤害"))-3*天帝数值.成长(100))<.001);}
        using(var t=new 战夹(6)){t.人.设置当前资源(t.人.最大血量*.5f,t.人.最大灵力*.5f,0);t.步(1);float hp=t.人.当前血量;t.施放();t.步(2);查("多水珠恢复总量3%不翻倍",Math.Abs(t.人.当前血量-hp-t.人.最大血量*.03f)<.1f);}
        using(var t=new 战夹(38)){t.施放();var e=t.战.敌人[0];t.步(.025f);设(e,"位置",e.位置+Vector2.up*8);t.步(.025f);int n=t.命中;设(e,"位置",e.位置-Vector2.up*8);t.步(.025f);查("围猎首次越界命中不重复",n>0&&t.命中==n);}
    }
    static void 特性边界()
    {
        using(var t=new 战夹(7))
        {
            t.施放();查("灼印发射时未附着也未伤害",t.命中==0&&t.战.特性效果列表.All(e=>!(bool)字段(e,"灼印附着")&&e.位置==字段玩家(t.战)));
            foreach(var e in t.战.敌人)设(e,"位置",e.位置+Vector2.up*10);
            t.步(2);查("灼印直线弹可躲开且不凭空附着",t.命中==0&&!t.战.特性效果列表.Any(e=>(bool)字段(e,"灼印附着")));
        }
        using(var t=new 战夹(7))
        {
            t.施放();t.步(.3f);var burn=t.战.特性效果列表.FirstOrDefault(e=>(bool)字段(e,"灼印附着"));查("灼印真实碰撞后才附着",burn!=null&&t.命中==0);
            if(burn!=null){调(t.战,"附着灼印",burn,(int)字段(burn,"目标"));查("同目标灼烧刷新不叠层",t.战.特性效果列表.Count(e=>(bool)字段(e,"灼印附着")&&(int)字段(e,"目标")== (int)字段(burn,"目标"))==1);}
            t.步(3.2f);查("灼印附着后有持续伤害",t.总伤>0);
        }
        using(var t=new 战夹(35))
        {
            t.施放();var wheel=t.战.特性效果列表.First();float start=(float)字段(wheel,"回转开始");t.步(start-.03f);var dir=wheel.方向;t.步(.1f);
            float arc=Vector2.Angle(dir,wheel.方向);查("回旋转向受弧度限速而非瞬时反向",arc>0&&arc<=60*.125f);
            写(wheel,"已过",(float)字段(wheel,"总时长"));查("回旋曲线终点回到释放位置",Vector2.Distance((Vector2)调(t.战,"回旋位置",wheel),wheel.起点)<.001f);写(wheel,"已过",start+.07f);
            t.步(8);查("回旋有限寿命结束",!t.战.特性效果列表.Contains(wheel));
        }
        using(var t=new 战夹(36))
        {
            t.施放();t.步(.025f);var armor=t.战.特性效果列表.Where(e=>e.护体).ToArray();查("旋甲数量拆分总120度覆盖",armor.Length==2&&Math.Abs(armor.Sum(e=>e.覆盖角度)-120)<.001&&Vector2.Angle(armor[0].方向,armor[1].方向)>179);
            if(armor.Length>0){var enemy=t.战.敌人[0];设(enemy,"位置",armor[0].位置+new Vector2(-armor[0].方向.y,armor[0].方向.x)*2);查("旋甲覆盖间隙不减伤",(float)调(t.战,"特性受伤前",100f,true,enemy)==100);设(enemy,"位置",armor[0].位置+armor[0].方向*2);float first=(float)调(t.战,"特性受伤前",100f,true,enemy),second=(float)调(t.战,"特性受伤前",100f,true,enemy);查("多片旋甲共用一次减伤",first<100&&second==100);}
        }
        using(var t=new 战夹(23))
        {
            var list=(IList)字段(t.战,"特性效果");for(int i=0;i<2;i++){var ally=new 天帝战斗系统.特性战斗效果{召唤=true,位置=字段玩家(t.战),血量=100,剩余秒=5};list.Add(ally);}
            写(t.战,"护盾额度",10f);调(t.战,"授范围盾",t.状态,100000f,5f);
            float shields=t.战.特性临时护盾+t.战.特性效果列表.Sum(e=>(float)字段(e,"召唤盾"));查("玩家与召唤共同扣减护盾额度",shields<=10.001f&&Math.Abs((float)字段(t.战,"护盾额度"))<.001);
            调(t.战,"授范围盾",t.状态,100000f,5f);查("额度耗尽不额外生成盟友盾",Math.Abs(shields-t.战.特性临时护盾-t.战.特性效果列表.Sum(e=>(float)字段(e,"召唤盾")))<.001);
            写(t.战,"护盾额度",t.人.最大血量*2);调(t.战,"授盾",t.人.最大血量*2,5f,1);调(t.战,"授范围盾",t.状态,100000f,5f);查("主角和召唤共同受临时盾总容量限制",t.战.特性临时护盾+t.战.特性效果列表.Sum(e=>(float)字段(e,"召唤盾"))<=t.人.最大血量*(float)天帝特性道纹.取("limits.shield_capacity")+.01f);
        }
    }
    static Vector2 字段玩家(天帝战斗系统 b)=>(Vector2)字段(b,"玩家");
    static void 形态()
    {
        foreach(var f in Enumerable.Range(29,10).Select(x=>(道纹功能)x))
        {
            var g=新();g.设置玩家等级(100);放(g,功(1,f),1,0);
            using(var p=new 天帝主角属性(天帝普攻.主角配置(),g))
            {
                var b=new 天帝战斗系统(new 天帝战斗地图(42,true),g,p,战斗难度.普通);b.设置演示靶(new[]{new Vector2(f==道纹功能.地雷?.5f:2,0),new Vector2(4,0),new Vector2(2,2),new Vector2(2,-2)});int hits=0;b.伤害反馈+=(a,d,c)=>hits++;
                b.发射演示(普攻参数.读取(g,p));for(int n=0;n<400;n++)b.推进演示弹体(.025f);
                查(f+"独立几何有命中且末端不补普通弹",hits>0&&b.灵矢.Count==0&&b.扩展功能触发次数==1);
                b.清理特性战斗();
            }
        }
    }
    static void 掉落()
    {
        var map=new 天帝战斗地图(42,true);var g=新();var drop=new 天帝道纹掉落(map,g,战斗难度.普通);int low=0;
        for(int i=0;i<100;i++){var e=new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通,map.出生位置),战斗难度.普通,7);调(drop,"生成特性掉落",e);low=g.道纹.Count;}
        查("低于8级无特性掉落",low==0);
        int traits=0,convert=0;
        for(int i=0;i<4000;i++){int before=g.道纹.Count;var e=new 战斗敌人(new 战斗敌人布点(战斗敌人级别.王级,map.出生位置),战斗难度.普通,100);调(drop,"生成特性掉落",e);if(g.道纹.Count>before){var a=g.道纹.Last();if(a.分类==道纹分类.特性)traits++;else convert++;}}
        查("独立35%掉落85/15分流",traits+convert>1200&&traits+convert<1600&&convert/(float)(traits+convert)>.12f&&convert/(float)(traits+convert)<.18f);
        查("38特性与8转化掉落覆盖",g.道纹.Where(x=>x.分类==道纹分类.特性).Select(x=>x.特性编号).Distinct().Count()==38&&g.道纹.Where(x=>x.分类==道纹分类.转化).Select(x=>x.特性编号).Distinct().Count()==8);
    }
}
#endif
