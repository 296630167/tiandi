using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class 特性激活结果
{
    public 道纹实例 道纹;
    public int 通路;
    public bool 激活, 同名覆盖;
    public readonly double[] 下游 = new double[天帝道纹属性.数量];
    public readonly List<int> 来源 = new List<int>();
    public string 条件说明;
}
public sealed class 特性连接视图
{
    public readonly List<特性激活结果> 特性 = new List<特性激活结果>();
    public readonly Dictionary<int,List<道纹词条>> 虚拟词条 = new Dictionary<int,List<道纹词条>>();
    public IEnumerable<特性激活结果> 生效 => 特性.Where(x=>x.激活&&!x.同名覆盖);
}
public sealed partial class 天帝道纹
{
    public 特性连接视图 特性视图 { get; private set; } = new 特性连接视图();
    public IEnumerable<道纹词条> 读取有效词条(道纹实例 r) => 特性视图.虚拟词条.TryGetValue(r.编号,out var v)?v:r.词条;
}
public static class 天帝特性道纹
{
    public static double 取(string k) => 天帝数值.取("trait_runes."+k);
    public static double 品阶值(道纹实例 r,string k) => 取("grades."+(int)r.品阶+"."+k);
    public static double 数值(int id,string k) => 取("traits."+(id-1)+"."+k);
    public static string 文本(int id,string k) => 天帝数值配置.取文本("trait_runes.traits."+(id-1)+"."+k);
    public static string 名称(道纹分类 kind,int id) => kind==道纹分类.转化?
        天帝数值配置.取文本("trait_runes.converters."+(id-1)+".名称"):文本(id,"名称");
    public static bool 定义有效(道纹分类 kind,int id,int ports) =>
        (kind==道纹分类.特性 && id>=1&&id<=38 || kind==道纹分类.转化&&id>=1&&id<=8) &&
        Enumerable.Range(0,3).Any(i=>ports==((1<<i)|(1<<(i+3))));
    public static 道纹实例 创建(int number,道纹分类 kind,int id,道纹品阶 grade,int level,int dir)
    {
        int ports=(1<<dir)|(1<<((dir+3)%6));
        if(number<=0 || !定义有效(kind,id,ports)||grade<道纹品阶.普通||grade>道纹品阶.传说||level<1||level>100)throw new ArgumentException("特性定义无效");
        return new 道纹实例 { 编号=number,分类=kind,特性编号=id,品阶=grade,物品等级=level,接口=ports,
            介绍=kind==道纹分类.特性?文本(id,"说明"):"范围内属性词条按预算转化；原词条保留，卸下恢复。不同目标转化冲突时保留原属性。" };
    }
    static 道纹属性 属性(string name)=>(道纹属性)Enum.Parse(typeof(道纹属性),name);
    public static double 门槛(道纹实例 r,int index)
    {
        string root="traits."+(r.特性编号-1)+".条件."+index;
        var a=属性(天帝数值配置.取文本("trait_runes."+root+".0"));
        string unit=天帝数值配置.取文本("trait_runes."+root+".2");
        double value=取(root+".1")*品阶值(r,"门槛")*(unit=="g"?天帝数值.成长(r.物品等级):1);
        return a==道纹属性.弧度?Math.Ceiling(value/30-1e-9)*30:天帝道纹属性.是功能(a)?Math.Ceiling(value-1e-9):Math.Ceiling(value*100-1e-9)/100;
    }
    public static double 中值(道纹属性 a,int level)
    {
        if(天帝道纹属性.是功能(a))return a==道纹属性.弧度?30:1;
        天帝数值.词条定点范围(a,level,out int lo,out int hi);return (lo+hi)/200d;
    }
    static HashSet<int> 下游(天帝道纹 g,道纹实例 r)
    {
        var seen=new HashSet<Vector2Int>{Vector2Int.zero,r.格子.Value};var ids=new HashSet<int>();var q=new Queue<Vector2Int>();q.Enqueue(r.格子.Value);
        while(q.Count>0)
        {
            var p=q.Dequeue();var cur=g.已放置[p];
            for(int d=0;d<6;d++)
            {
                var next=p+天帝道纹.邻向[d];
                if(!天帝道纹.可传导(p,next)||!cur.有接口(d)||!g.已放置.TryGetValue(next,out var nr)||!nr.有接口((d+3)%6)||!seen.Add(next))continue;
                ids.Add(nr.编号);q.Enqueue(next);
            }
        }
        return ids;
    }
    public static void 功能贡献(double[] sum,道纹功能 f)
    {
        if(f==道纹功能.齐射||天帝顺序道纹.多弹功能(f))sum[(int)道纹属性.数量]+=f==道纹功能.齐射?1:天帝顺序道纹.多弹数量(f)-1;
        if(f==道纹功能.分裂)sum[(int)道纹属性.分裂]++;
        if(f==道纹功能.连锁)sum[(int)道纹属性.连锁]++;
        if(f==道纹功能.回旋)sum[(int)道纹属性.弧度]+=天帝顺序道纹.扩展数值("curve_degrees");
        if(f==道纹功能.波动)sum[(int)道纹属性.弧度]+=30;
        double radius=f==道纹功能.增大?取("rules.root_shape_counts.4"):0;
        string key=f==道纹功能.爆破?"blast_radius":f==道纹功能.震荡?"pulse_radius":f==道纹功能.拖尾?"trail_radius":f==道纹功能.牵引?"pull_radius":f==道纹功能.烙印?"mark_radius":f==道纹功能.陨落?"fall_radius":null;
        if(key!=null)radius=天帝顺序道纹.扩展数值(key);
        key=f==道纹功能.刃波?"blade_half_width":f==道纹功能.旋刃?"orbit_radius":f==道纹功能.灵鞭?"whip_range":f==道纹功能.灵网?"net_half_width":f==道纹功能.地雷?"mine_blast_radius":null;
        if(key!=null)radius=天帝顺序道纹.形态数值(key);
        sum[(int)道纹属性.范围]+=radius;
    }
    static int 距离(Vector2Int a,Vector2Int b){var d=a-b;return Math.Max(Math.Abs(d.x),Math.Max(Math.Abs(d.y),Math.Abs(d.x+d.y)));}
    static readonly int[] 顺序={0,5,4,3,2,1};
    public static 特性连接视图 计算(天帝道纹 g)
    {
        var result=new 特性连接视图();var claims=new Dictionary<int,List<道纹实例>>();
        foreach(var r in g.道纹.Where(x=>x.是特性道纹))r.特性状态=r.生效?"已接通 · 条件未满足":"未接通";
        // 转化实例只选择第一条接通的源通路；所有转换均读取原始收藏快照。
        foreach(var c in g.道纹.Where(x=>x.分类==道纹分类.转化&&x.生效))
        {
            int route=顺序.First(s=>g.弹槽道纹[s].Contains(c.编号));int count=0;
            foreach(var r in g.道纹.Where(x=>x.分类==道纹分类.属性&&x.格子.HasValue&&g.弹槽道纹[route].Contains(x.编号)&&距离(x.格子.Value,c.格子.Value)<=品阶值(c,"半径")))
            {if(!claims.TryGetValue(r.编号,out var list))claims[r.编号]=list=new List<道纹实例>();list.Add(c);count++;}
            c.特性状态="已接通 · 通路"+(route+1)+" · 半径"+品阶值(c,"半径")+"格 · 候选"+count+"枚";
        }
        foreach(var kv in claims)
        {
            var targets=kv.Value.Select(c=>天帝数值配置.取文本("trait_runes.converters."+(c.特性编号-1)+".目标")).Distinct().ToArray();
            if(targets.Length!=1){foreach(var c in kv.Value)c.特性状态+="\n转化目标冲突：#"+kv.Key+"保持原词条";continue;}
            var winner=kv.Value.OrderByDescending(c=>品阶值(c,"转换")).ThenBy(c=>c.编号).First();var r=g.道纹.First(x=>x.编号==kv.Key);var target=属性(targets[0]);
            var list=new List<道纹词条>();foreach(var a in r.词条)
            { double v=a.实际数值/中值(a.属性,r.物品等级)*品阶值(winner,"转换")*中值(target,r.物品等级);list.Add(道纹词条.从定点(target,(int)Math.Round(v*100,MidpointRounding.AwayFromZero))); }
            result.虚拟词条[r.编号]=list;
        }
        foreach(var r in g.道纹.Where(x=>x.分类==道纹分类.特性))
        {
            var reach=r.生效?下游(g,r):new HashSet<int>();特性激活结果 chosen=null;
            foreach(int route in 顺序.Where(s=>g.弹槽道纹[s].Contains(r.编号)))
            {
                var test=new 特性激活结果{道纹=r,通路=route,激活=true};var text=new StringBuilder();
                foreach(var nr in g.道纹.Where(x=>reach.Contains(x.编号)&&g.弹槽道纹[route].Contains(x.编号)))
                {
                    test.来源.Add(nr.编号);
                    if(nr.是顺序功能){功能贡献(test.下游,nr.功能);continue;}
                    if(nr.分类!=道纹分类.属性)continue;
                    foreach(var a in result.虚拟词条.TryGetValue(nr.编号,out var v)?v:nr.词条)test.下游[(int)a.属性]+=a.实际数值;
                }
                for(int i=0;i<数值(r.特性编号,"condition_count");i++)
                {var a=属性(文本(r.特性编号,"条件."+i+".0"));double need=门槛(r,i),have=test.下游[(int)a];bool ok=have+1e-9>=need;test.激活&=ok;text.Append(a).Append(" ").Append(have.ToString("0.##")).Append(" / ").Append(need.ToString("0.##")).Append(ok?" ✓":" 未满足").Append('\n');}
                test.条件说明=text.ToString();chosen??=test;if(test.激活){chosen=test;break;}
            }
            if(chosen==null)
            {
                var missing=new StringBuilder();
                for(int i=0;i<数值(r.特性编号,"condition_count");i++)missing.Append(文本(r.特性编号,"条件."+i+".0")).Append(" 0 / ").Append(门槛(r,i).ToString("0.##")).Append(" 未满足\n");
                chosen=new 特性激活结果{道纹=r,通路=-1,条件说明=missing.ToString()};
            }
            result.特性.Add(chosen);
            r.特性状态=(chosen.激活?"已激活 · 通路"+(chosen.通路+1):r.生效?"已接通 · 条件不足":"未接通")+"\n物品"+r.物品等级+"级 · "+r.品阶+"条件\n"+chosen.条件说明+
                "效果×"+品阶值(r,"效果").ToString("0.##")+" · 持续×"+品阶值(r,"持续").ToString("0.##")+" · 周期×"+品阶值(r,"周期").ToString("0.##");
        }
        foreach(var group in result.特性.Where(x=>x.激活).GroupBy(x=>x.道纹.特性编号))
        {var winner=group.OrderByDescending(x=>x.道纹.品阶).ThenBy(x=>x.道纹.编号).First();foreach(var x in group)if(x!=winner){x.同名覆盖=true;x.道纹.特性状态+="同名由#"+winner.道纹.编号+"生效，本枚不重复施放";}}
        return result;
    }
    public static void 应用转换加成(天帝道纹 g,特性连接视图 view)
    {
        foreach(var kv in view.虚拟词条)
        {
            var r=g.道纹.First(x=>x.编号==kv.Key);var delta=new double[天帝道纹属性.数量];
            foreach(var a in r.词条)delta[(int)a.属性]-=a.实际数值;
            foreach(var a in kv.Value)delta[(int)a.属性]+=a.实际数值;
            for(int i=0;i<delta.Length;i++) {g.生效加成[i]+=delta[i];for(int route=0;route<6;route++)if(g.弹槽道纹[route].Contains(r.编号))g.弹槽加成[route][i]+=delta[i];}
        }
    }
}
