using System.Collections.Generic;
using UnityEngine;

public static class 天帝实力评语
{
    public const string 初始评语 = "你是个普通人，只能勉强射出一丝灵力";
    static 普攻参数 最强通路(天帝道纹 网, 天帝主角属性 人)
    {
        普攻参数 最强 = null;
        for (int d = 0; d < 6; d++) if (网.通路参与射击(d))
        {
            var p = 普攻参数.读取通路(网, 人, d);
            if (最强 == null || p.伤害 * p.数量 > 最强.伤害 * 最强.数量) 最强 = p;
        }
        return 最强 ?? 普攻参数.读取(网, 人);
    }
    public static string 读取(天帝道纹 网, 天帝主角属性 人)
    {
        if (网 == null || 人 == null) return 初始评语;
        var p = 最强通路(网, 人);
        if (p.顺序计划 != null) return "你道纹相承，灵力已能沿链路依次变化";
        var 加 = 网.生效加成;
        bool 基础增强 = false;
        for (int i = 0; i < 5; i++) 基础增强 |= 加[i] > 0;
        bool 有形态 = p.数量 > 1 || p.分裂 > 0 || p.连锁 > 0 || p.转向角度 > 0 || p.溅射半径 > 0;
        if (p.伤害 <= 1 && !基础增强 && !有形态 && 人.灵气护盾 <= 0) return 初始评语;
        // This is a description of build potential, not a promise of kills or a new cultivation level.
        float 输出 = p.伤害 * p.数量 / Mathf.Max(.15f,p.间隔);
        if (输出 >= 60 && 有形态) return "你道纹成势，举手间已有修士气象";
        if (p.连锁 > 0 && p.分裂 > 0) return "你道纹相济，灵力分化后还能相引追敌";
        if (p.数量 > 1) return "你灵力成束，已能齐射多股灵力";
        if (p.连锁 > 0) return "你灵力相引，已能在敌群之间连锁";
        if (p.分裂 > 0) return "你灵力分化，一击之后还能分出余势";
        if (p.溅射半径 > 0) return "你灵力外放，攻击已能波及周围";
        if (输出 >= 12) return "你灵力渐盛，出手已有几分修士气象";
        if (p.伤害 > 1) return "你初窥灵力，已能凝聚灵力伤敌";
        if (加[(int)道纹属性.速度] > 0) return "你身法渐轻，已能游走驱使灵力";
        return "你道纹护身，已有几分立足之力";
    }
    public static string 说明(天帝道纹 网, 天帝主角属性 人)
    {
        if (网 == null || 人 == null) return "灵力伤害 1";
        var p = 最强通路(网,人);
        if (p.顺序计划 != null) return "有效道纹 " + 网.生效数 + " · " + 网.射击通路数 + "路轮转 · 首发 " + p.数量 + "颗 · 出口独立 · 顺序执行";
        var 形态 = new List<string>();
        if (p.数量 > 1) 形态.Add("齐射" + p.数量);
        if (p.分裂 > 0) 形态.Add("分裂" + p.分裂);
        if (p.连锁 > 0) 形态.Add("连锁" + p.连锁);
        if (p.溅射半径 > 0) 形态.Add("范围" + p.溅射半径.ToString("0.#") + "m");
        return "有效道纹 " + 网.生效数 + "  ·  " + 网.射击通路数 + " 路轮转  ·  " + 天帝道纹.通路名称(p.通路) + "伤害 " + p.伤害.ToString("0.##") + (形态.Count > 0 ? "  ·  " + string.Join(" / ",形态) : "  ·  单股灵力");
    }
}
