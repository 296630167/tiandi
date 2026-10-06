#if UNITY_EDITOR
using System;
using System.Collections.Generic;

// 多词条/连通回归使用独立的旧演示夹具，不进入Player，也不代表正式开局发放。
public static class 天帝道纹夹具
{
    public static void 实战包(天帝道纹 数据, int 点数 = 20)
    {
        // 仅Editor验证/录制显式调用，不能进入正式Player或默认开局。
        if (数据.道纹.Count != 0 || 数据.已解锁格数 != 1) throw new InvalidOperationException("实战夹具必须注入未配置的真实开局");
        typeof(天帝道纹).GetProperty("技能点").SetValue(数据, 点数);
        int[] 数值 = { 5, 5, 5, 50, 20, 1, 1, 1, 15, 1 };
        string[] 说明 = {
            "力量提升攻击力、血量与防御", "速度提升移动、跑步和普攻频率", "智力提升灵矢攻击力与灵力上限",
            "直接增加血量上限；插拔不回复", "增加灵力上限并提高灵矢弹速；普攻不消耗灵力",
            "灵矢首次命中产生分裂子矢；子矢不再分裂或连锁", "每次发射增加一枚灵矢，最多六枚",
            "灵矢命中后寻找下一个目标，最多四次，伤害逐次乘0.8", "灵矢每秒转向角度增加15度，追踪当前目标，最多180度",
            "命中溅射半径增加0.5米，最多4米，溅射伤害50%且不触发衍生攻击"
        };
        for (int a = 0; a < 10; a++) for (int n = 0; n < 10; n++)
        {
            var 纹 = new 道纹实例 { 编号 = 数据.道纹.Count + 1, 接口 = 63, 介绍 = "开局测试赠送：" + 说明[a] + "。六向接口方便试接，非正常掉落。" };
            设置(纹, "分类", 道纹分类.属性); 设置(纹, "品阶", 道纹品阶.普通);
            纹.词条.Add(new 道纹词条((道纹属性)a, 数值[a])); 数据.道纹.Add(纹);
        }
    }
    static void 设置(道纹实例 纹, string 属性, object 值) => typeof(道纹实例).GetProperty(属性).SetValue(纹, 值);
    public static 天帝道纹 创建(int 种子, int 源纹编号 = 0) => 填充(new 天帝道纹(种子, 源纹编号), 种子, 0);
    public static 天帝道纹 创建(int 种子, 天赋定义 天赋) => 填充(new 天帝道纹(种子, 天赋), 种子, 天帝天赋效果.开局技能点(天赋));
    public static 天帝道纹 填充(天帝道纹 数据, int 种子, int 夹具点数)
    {
        数据.道纹.Clear();
        typeof(天帝道纹).GetProperty("技能点").SetValue(数据, 夹具点数);
        var 随机 = new System.Random(种子);
        var 属性序列 = new List<道纹属性>(); for (int i = 0; i < 10; i++) 属性序列.Add((道纹属性)i);
        for (int i = 属性序列.Count - 1; i > 0; i--) { int j = 随机.Next(i + 1); var 临 = 属性序列[i]; 属性序列[i] = 属性序列[j]; 属性序列[j] = 临; }
        var 品阶序列 = new List<道纹品阶>(); for (int i = 0; i < 8; i++) 品阶序列.Add((道纹品阶)i);
        while (品阶序列.Count < 11) 品阶序列.Add((道纹品阶)随机.Next(8));
        for (int i = 品阶序列.Count - 1; i > 0; i--) { int j = 随机.Next(i + 1); var 临 = 品阶序列[i]; 品阶序列[i] = 品阶序列[j]; 品阶序列[j] = 临; }
        for (int i = 0; i <= 属性序列.Count; i++)
        {
            var 阶 = 品阶序列[i]; var 规则 = 天帝道纹品阶.获取(阶);
            int 掩码 = 天帝道纹生成.随机接口(随机);
            var 纹 = new 道纹实例 { 编号 = i + 1, 接口 = 掩码 }; 设置(纹, "品阶", 阶);
            if (i == 属性序列.Count) { 纹.属性 = 道纹属性.雷; 纹.数值 = 1; }
            else
            {
                var 属性 = 属性序列[i]; bool 功能 = (int)属性 >= 5;
                设置(纹, "分类", 道纹分类.属性); 纹.属性 = 属性; 纹.数值 = 功能 ? 1 : 随机.Next(1, 4);
                int 词数 = 随机.Next(规则.最少词条, 规则.最多词条 + 1);
                for (int j = 1; j < 词数; j++) 纹.词条.Add(new 道纹词条((道纹属性)随机.Next(功能 ? 5 : 0, 功能 ? 10 : 5), 功能 ? 1 : 随机.Next(1, 4)));
            }
            数据.道纹.Add(纹);
        }
        数据.重算(); return 数据;
    }
}
#endif
