using System;
using System.Collections.Generic;

public enum 宝盒种类 { 属性, 功能, 分叉 }

// 宝盒只负责消耗灵石并向现有道纹背包发放实例。
public sealed class 天帝宝盒
{
    public static int 开局灵石 => (int)天帝数值.取("economy.starting_stones");
    static readonly int[][] 品阶权重 =
    {
        天帝数值.整数表("economy.box_grade_weights.0", 8),
        天帝数值.整数表("economy.box_grade_weights.1", 8)
    };
    static readonly int[] 分叉接口权重 = 天帝数值.整数表("economy.box_branch_weights", 4);
    readonly 天帝道纹 道纹;
    readonly Random 随机;
    bool 回收中;
    int 有限灵石;
    public bool 无限灵石 { get; private set; }
    public int 灵石 => 无限灵石 ? int.MaxValue : 有限灵石;
    public string 灵石显示 => 无限灵石 ? "无限" : 灵石.ToString();
    public void 启用无限灵石()
    { if (无限灵石) return; 无限灵石 = true; 余额改变?.Invoke(); }
    public event Action 余额改变;
    public bool 预览回收(IReadOnlyList<道纹实例> 批次,out int 金额,out string 提示)
    {
        金额=0;
        if(批次==null||批次.Count==0){提示="请先选择要回收的道纹";return false;}
        long 合计=0;var 唯一=new HashSet<道纹实例>();
        foreach(var 纹 in 批次)
        {
            if(!唯一.Add(纹)){提示="重复选择了同一道纹";return false;}
            if(!道纹.可回收(纹,out 提示))return false;
            合计+=天帝数值.道纹回收价(纹);
        }
        if(合计>int.MaxValue||!无限灵石&&灵石>int.MaxValue-合计){提示="灵石余额已达上限，未回收任何道纹";return false;}
        金额=(int)合计;提示="";return true;
    }
    public bool 回收道纹(IReadOnlyList<道纹实例> 批次,int 预期金额,out string 提示)
    {
        if(回收中){提示="正在回收，请稍后";return false;}
        if(!预览回收(批次,out int 金额,out 提示))return false;
        if(金额!=预期金额){提示="道纹状态或价格已变化，请重新预览";return false;}
        回收中=true;
        try
        {
            if(!道纹.移除回收批次(批次,out 提示))return false;
            if (!无限灵石) 有限灵石+=金额;
            道纹.发布回收改变();余额改变?.Invoke();
            提示="已回收 "+批次.Count+" 枚道纹，获得 "+金额+" 灵石";return true;
        }
        finally{回收中=false;}
    }

    public 天帝宝盒(天帝道纹 道纹, int 种子, int 初始灵石, bool 无限 = false)
    {
        this.道纹 = 道纹 ?? throw new ArgumentNullException(nameof(道纹));
        if (初始灵石 < 0) throw new ArgumentOutOfRangeException(nameof(初始灵石));
        随机 = new Random(种子);
        有限灵石 = 初始灵石; 无限灵石 = 无限;
    }

    public static int 价格(宝盒种类 种类)
    {
        return (int)天帝数值.取("economy.box_prices." + (int)种类);
    }

    public static int[] 品阶概率(宝盒种类 种类)
    {
        if (种类 != 宝盒种类.属性 && 种类 != 宝盒种类.功能) throw new ArgumentOutOfRangeException(nameof(种类));
        return (int[])品阶权重[(int)种类].Clone();
    }

    public static int[] 分叉概率() => (int[])分叉接口权重.Clone();

    public bool 获得灵石(int 数量)
    {
        if (数量 <= 0 || !无限灵石 && 灵石 > int.MaxValue - 数量) return false;
        if (!无限灵石) 有限灵石 += 数量;
        余额改变?.Invoke(); return true;
    }

    public static int 击败奖励(战斗敌人级别 级别)
    {
        return (int)天帝数值.取("economy.kill_stones." + (int)级别);
    }

    public static 道纹品阶 抽品阶(宝盒种类 种类, int 点)
    {
        if (点 < 0 || 点 >= 10000 || (种类 != 宝盒种类.属性 && 种类 != 宝盒种类.功能)) throw new ArgumentOutOfRangeException();
        int 累计 = 0;
        var 权重 = 品阶权重[(int)种类];
        for (int i = 0; i < 权重.Length; i++)
        {
            累计 += 权重[i];
            if (点 < 累计) return (道纹品阶)i;
        }
        throw new InvalidOperationException("宝盒品阶概率总和必须为10000");
    }

    public static int 抽分叉接口数(int 点)
    {
        if (点 < 0 || 点 >= 100) throw new ArgumentOutOfRangeException(nameof(点));
        int 累计 = 0;
        for (int i = 0; i < 分叉接口权重.Length; i++)
        {
            累计 += 分叉接口权重[i];
            if (点 < 累计) return i + 3;
        }
        throw new InvalidOperationException("分叉接口概率总和必须为100");
    }

    public bool 抽取(宝盒种类 种类, out 道纹实例 结果, out string 提示)
    {
        结果 = null;
        if (种类 != 宝盒种类.属性 && 种类 != 宝盒种类.功能 && 种类 != 宝盒种类.分叉) { 提示 = "未知宝盒"; return false; }
        int 费用 = 价格(种类);
        if (!无限灵石 && 灵石 < 费用) { 提示 = "灵石不足，还差 " + (费用 - 灵石) + " 灵石"; return false; }

        道纹实例 候选;
        if (种类 == 宝盒种类.分叉)
        {
            候选 = new 道纹实例 { 编号 = 1, 分类 = 道纹分类.分叉, 品阶 = 道纹品阶.普通,
                接口 = 天帝道纹生成.随机方向(随机, 抽分叉接口数(随机.Next(100))), 介绍 = "固定接口的分流道纹；无属性词条", 物品等级 = 道纹.玩家等级 };
        }
        else
        {
            int 属性点 = 随机.Next(天帝道纹属性.基础属性.Length + 天帝道纹属性.普通属性.Length + 天帝道纹属性.五行属性.Length);
            var 分组 = 种类 == 宝盒种类.功能 ? 道纹属性分组.形态 :
                属性点 < 天帝道纹属性.基础属性.Length ? 道纹属性分组.基础 :
                属性点 < 天帝道纹属性.基础属性.Length + 天帝道纹属性.普通属性.Length ? 道纹属性分组.普通 : 道纹属性分组.元素;
            候选 = 天帝道纹生成.创建(1, 道纹分类.属性, 抽品阶(种类, 随机.Next(10000)), 随机, 分组, 道纹.玩家等级);
        }
        if (!道纹.获得道纹(候选)) { 提示 = "道纹背包暂时无法接收，未扣灵石"; return false; }
        if (!无限灵石) 有限灵石 -= 费用;
        余额改变?.Invoke();
        结果 = 候选;
        提示 = "获得 " + 候选.名称;
        return true;
    }
}
