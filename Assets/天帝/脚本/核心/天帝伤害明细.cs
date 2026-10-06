using System;
using System.Text;

public enum 伤害来源 { 普通, 金, 木, 水, 火, 土, 属性 }

// 来源权重随释放保留；不对五种元素分别应用软上限或最低伤害。
public readonly struct 五行伤害分量
{
    public readonly double 金, 木, 水, 火, 土;
    public double 总和 => 金 + 木 + 水 + 火 + 土;
    public 五行伤害分量(double 金, double 木, double 水, double 火, double 土)
    { this.金 = 天帝数值.正值(金); this.木 = 天帝数值.正值(木); this.水 = 天帝数值.正值(水); this.火 = 天帝数值.正值(火); this.土 = 天帝数值.正值(土); }
    public double 读取(int 索引) => 索引 == 0 ? 金 : 索引 == 1 ? 木 : 索引 == 2 ? 水 : 索引 == 3 ? 火 : 索引 == 4 ? 土 : 0;
}

public readonly struct 战斗伤害明细
{
    public readonly double 普通, 金, 木, 水, 火, 土, 属性;
    public double 合计 => 普通 + 金 + 木 + 水 + 火 + 土 + 属性;
    public 战斗伤害明细(double 普通, double 金 = 0, double 木 = 0, double 水 = 0, double 火 = 0, double 土 = 0, double 属性 = 0)
    { this.普通 = 普通; this.金 = 金; this.木 = 木; this.水 = 水; this.火 = 火; this.土 = 土; this.属性 = 属性; }
    public double 读取(伤害来源 来源)
    {
        switch (来源)
        {
            case 伤害来源.普通: return 普通; case 伤害来源.金: return 金; case 伤害来源.木: return 木;
            case 伤害来源.水: return 水; case 伤害来源.火: return 火; case 伤害来源.土: return 土;
            default: return 属性;
        }
    }
}

public static class 天帝伤害显示
{
    static readonly string[] 颜色 = { "F4F0E5", "FFD15C", "65D887", "65BEFF", "FF6254", "D9AC78", "C9ADF0" };
    public static string 色值(伤害来源 来源) => 颜色[(int)来源];
    public static string 文本(战斗伤害明细 明细, StringBuilder 缓冲)
    {
        缓冲.Clear();
        for (int i = 0; i < 颜色.Length; i++)
        {
            var 来源 = (伤害来源)i; double 值 = 明细.读取(来源);
            if (!天帝数值.有限(值) || 值 <= 0) continue;
            if (缓冲.Length > 0) 缓冲.Append('\n');
            // 极小分量也显示非零有效位；不能把有伤害的来源写成0。
            string 数字 = 值 < .01 ? 值.ToString("G3") : 值 >= 1000000 ? 值.ToString("G4") : 值.ToString("0.##");
            缓冲.Append("<color=#").Append(颜色[i]).Append('>').Append(来源).Append('-').Append(数字).Append("</color>");
        }
        return 缓冲.ToString();
    }
}
