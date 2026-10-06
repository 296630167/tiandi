using System;
using UnityEngine;

public enum 道纹品阶 { 普通, 优秀, 杰出, 稀有, 完美, 超凡, 史诗, 传说 }

public readonly struct 道纹品阶约定
{
    public readonly int 最少词条, 最多词条;
    public readonly Color 颜色;
    public 道纹品阶约定(int 少词, int 多词, Color 色)
    { 最少词条 = 少词; 最多词条 = 多词; 颜色 = 色; }
}

public static class 天帝道纹品阶
{
    static readonly 道纹品阶约定[] 规则 =
    {
        new 道纹品阶约定(1, 1, Color.white),
        new 道纹品阶约定(1, 1, new Color(0.25f, 0.90f, 0.35f)),
        new 道纹品阶约定(1, 2, new Color(0.25f, 0.60f, 1)),
        new 道纹品阶约定(1, 3, new Color(0.72f, 0.40f, 0.95f)),
        new 道纹品阶约定(2, 4, new Color(1, 0.55f, 0.16f)),
        new 道纹品阶约定(2, 5, new Color(1, 0.26f, 0.26f)),
        new 道纹品阶约定(3, 6, new Color(1, 0.83f, 0.18f)),
        new 道纹品阶约定(5, 6, Color.white)
    };
    public static 道纹品阶约定 获取(道纹品阶 品阶)
    {
        int i = (int)品阶;
        if (i < 0 || i >= 规则.Length) throw new ArgumentOutOfRangeException(nameof(品阶));
        return new 道纹品阶约定((int)天帝数值.取("rune.grade_counts." + i + ".0"), (int)天帝数值.取("rune.grade_counts." + i + ".1"), 规则[i].颜色);
    }
    public static Color 边颜色(道纹品阶 品阶, int 边)
        => 品阶 == 道纹品阶.传说 ? Color.HSVToRGB(Mathf.Repeat(边 / 6f, 1), 0.75f, 1) : 获取(品阶).颜色;
    public static string 彩色品阶文字(道纹品阶 品阶)
    {
        if (品阶 == 道纹品阶.传说) return 天帝道纹美术.彩绘皮肤 ? "<color=#A52D61>传</color><color=#25668F>说</color>" : "<color=#FF6B8B>传</color><color=#66DEFF>说</color>";
        return "<color=#" + ColorUtility.ToHtmlStringRGB(天帝道纹美术.纸面文字(获取(品阶).颜色)) + ">" + 品阶 + "</color>";
    }
}

[Serializable]
public sealed class 道纹词条
{
    public 道纹属性 属性;
    public int 数值;
    // 数值仅保留旧JSON兼容。新词条按百分之一存储，复制、计算和展示读取实际数值。
    public long 定点数值;
    public bool 定点格式;
    public double 实际数值 => 定点格式 ? 定点数值 / 天帝数值.取("rune.storage_scale") : 数值;
    public 道纹词条(道纹属性 属性, int 数值) { this.属性 = 属性; this.数值 = 数值; }
    public static 道纹词条 从定点(道纹属性 属性, long 数值) => new 道纹词条(属性, 0) { 定点格式 = true, 定点数值 = 数值 };
    public 道纹词条 副本() => 定点格式 ? 从定点(属性, 定点数值) : new 道纹词条(属性, 数值);
    public void 迁移精度() { if (定点格式) return; 定点数值 = (long)数值 * (int)天帝数值.取("rune.storage_scale"); 定点格式 = true; }
}
