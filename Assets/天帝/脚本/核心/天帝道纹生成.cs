using System;
using System.Collections.Generic;

// 常规实例的生成规则；敌人掉落与开发夹具共用，接口独立于品阶。
public static class 天帝道纹生成
{
    public const int 接口权重总和 = 100;
    public static IReadOnlyList<int> 接口权重 { get; } = Array.AsReadOnly(new[] { (int)天帝数值.取("rune.property_port_weights.0"), (int)天帝数值.取("rune.property_port_weights.1") });

    public static int 接口数量结果(int 权重点)
    {
        if (权重点 < 0 || 权重点 >= 接口权重总和) throw new ArgumentOutOfRangeException(nameof(权重点));
        int 累计 = 0;
        for (int i = 0; i < 接口权重.Count; i++)
        { 累计 += 接口权重[i]; if (权重点 < 累计) return i + 1; }
        throw new InvalidOperationException("接口权重与总和不一致");
    }
    public static int 随机接口(System.Random 随机)
    {
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        int 数量 = 接口数量结果(随机.Next(接口权重总和));
        return 随机方向(随机, 数量);
    }
    public static int 随机分叉接口(System.Random 随机)
    {
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        int 点 = 随机.Next(100);
        int 累计 = 0;
        for (int 序 = 0; 序 < 4; 序++) { 累计 += (int)天帝数值.取("rune.branch_port_weights." + 序); if (点 < 累计) return 随机方向(随机, 序 + 3); }
        throw new InvalidOperationException("分叉接口权重不完整");
    }
    public static int 随机方向(System.Random 随机, int 数量)
    {
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        if (数量 < 1 || 数量 > 6) throw new ArgumentOutOfRangeException(nameof(数量));
        int[] 方向 = { 0, 1, 2, 3, 4, 5 };
        // 无放回抽取：给定接口数时，所有方向组合等概率。
        int 掩码 = 0;
        for (int i = 0; i < 数量; i++)
        {
            int j = 随机.Next(i, 6), 临 = 方向[i]; 方向[i] = 方向[j]; 方向[j] = 临;
            掩码 |= 1 << 方向[i];
        }
        return 掩码;
    }
    public static 道纹实例 创建(int 编号, 道纹分类 分类, 道纹品阶 品阶, System.Random 随机, 道纹属性分组? 限定分组 = null, int 物品等级 = 1)
    {
        if (编号 < 1) throw new ArgumentOutOfRangeException(nameof(编号));
        if (分类 != 道纹分类.属性 && 分类 != 道纹分类.分叉 && 分类 != 道纹分类.功能 && 分类 != 道纹分类.特性 && 分类 != 道纹分类.转化)
            throw new ArgumentOutOfRangeException(nameof(分类));
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        if (限定分组.HasValue && (限定分组 < 道纹属性分组.基础 || 限定分组 > 道纹属性分组.普通))
            throw new ArgumentOutOfRangeException(nameof(限定分组));
        物品等级 = (int)天帝数值.夹(物品等级, 1, 天帝数值.玩家上限);
        if(分类==道纹分类.特性 || 分类==道纹分类.转化)
            return 天帝特性道纹.创建(编号,分类,随机.Next(1,分类==道纹分类.特性?39:9),品阶,物品等级,随机.Next(6));
        if (分类 == 道纹分类.分叉)
            return new 道纹实例 { 编号 = 编号, 分类 = 分类, 品阶 = 道纹品阶.普通, 物品等级 = 物品等级,
                接口 = 随机分叉接口(随机), 介绍 = "固定接口的分流道纹；无属性词条" };
        var 分组 = 分类 == 道纹分类.功能 ? 道纹属性分组.形态 :
            限定分组 ?? 天帝道纹属性.分组(天帝道纹属性.当前属性[随机.Next(天帝道纹属性.当前词条数量)]);
        if (分组 == 道纹属性分组.形态)
        {
            var 机制 = (道纹功能)随机.Next(1, 天帝顺序道纹.功能数量 + 1);
            var 功能 = new 道纹实例 { 编号 = 编号, 分类 = 道纹分类.功能,
                品阶 = (道纹品阶)天帝数值.取("rune.function_grade"), 物品等级 = 物品等级,
                功能 = 机制, 接口 = 随机方向(随机, 随机.Next(天帝顺序道纹.最少接口数(机制), 天帝顺序道纹.接口数(机制) + 1)),
                介绍 = 天帝顺序道纹.功能摘要(机制) + "；接口对应即可连接。" };
            功能.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(机制), 1));
            return 功能;
        }
        var 规则 = 天帝道纹品阶.获取(品阶);
        // 接口先独立抽取；品阶只控制统一属性词条的容量。
        var 纹 = new 道纹实例 { 编号 = 编号, 分类 = 分类, 品阶 = 品阶, 接口 = 随机接口(随机), 物品等级 = 物品等级 };
        if (分组 == 道纹属性分组.元素)
        {
            纹.词条.Add(天帝道纹属性.抽词条(随机, 分组, 物品等级));
            return 纹;
        }
        int 词数 = 随机.Next(规则.最少词条, 规则.最多词条 + 1);
        for (int i = 0; i < 词数; i++)
            纹.词条.Add(限定分组.HasValue ? 天帝道纹属性.抽词条(随机, 分组, 物品等级) : 天帝道纹属性.抽非五行词条(随机, 物品等级));
        return 纹;
    }
}
