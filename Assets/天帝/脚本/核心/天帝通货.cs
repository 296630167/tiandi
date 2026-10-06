using System;
using System.Collections.Generic;

public enum 通货种类
{
    启灵石, 点玄石, 紫蕴石, 无瑕玉, 凝华石, 蕴玄石, 琢天玉,
    通脉针, 六通玉, 添蕴砂, 易纹砂, 重铸石, 问天石
}

public sealed class 通货定义
{
    public readonly 通货种类 种类;
    public string 名称 => 种类.ToString();
    public readonly string 说明;
    public readonly 道纹品阶? 来源, 目标;
    public 通货定义(通货种类 种类, string 说明, 道纹品阶? 来源 = null, 道纹品阶? 目标 = null)
    { this.种类 = 种类; this.说明 = 说明; this.来源 = 来源; this.目标 = 目标; }
}

// 改造与库存；跨启动持久化由天帝存档统一处理。
public sealed class 天帝通货
{
    // 两个旧索引留给既有存档，游戏中不再掉落、显示或使用。
    public static readonly IReadOnlyList<通货种类> 可用种类 = Array.AsReadOnly(new[]
    {
        通货种类.启灵石, 通货种类.点玄石, 通货种类.紫蕴石, 通货种类.无瑕玉,
        通货种类.凝华石, 通货种类.蕴玄石, 通货种类.琢天玉,
        通货种类.添蕴砂, 通货种类.易纹砂, 通货种类.重铸石, 通货种类.问天石
    });
    public static readonly IReadOnlyList<通货定义> 定义 = Array.AsReadOnly(new[]
    {
        new 通货定义(通货种类.启灵石, "普通 → 优秀", 道纹品阶.普通, 道纹品阶.优秀),
        new 通货定义(通货种类.点玄石, "普通 → 杰出", 道纹品阶.普通, 道纹品阶.杰出),
        new 通货定义(通货种类.紫蕴石, "普通 → 稀有", 道纹品阶.普通, 道纹品阶.稀有),
        new 通货定义(通货种类.无瑕玉, "普通 → 完美", 道纹品阶.普通, 道纹品阶.完美),
        new 通货定义(通货种类.凝华石, "优秀 → 杰出", 道纹品阶.优秀, 道纹品阶.杰出),
        new 通货定义(通货种类.蕴玄石, "杰出 → 稀有", 道纹品阶.杰出, 道纹品阶.稀有),
        new 通货定义(通货种类.琢天玉, "稀有 → 完美", 道纹品阶.稀有, 道纹品阶.完美),
        new 通货定义(通货种类.通脉针, "旧版保留，不再使用"),
        new 通货定义(通货种类.六通玉, "旧版保留，不再使用"),
        new 通货定义(通货种类.添蕴砂, "未达品阶词条上限时，随机增加一条词条"),
        new 通货定义(通货种类.易纹砂, "重抽所选一条的属性与数值；可能抽到原结果"),
        new 通货定义(通货种类.重铸石, "重抽全部词条的属性与数值；保留条数和接口"),
        new 通货定义(通货种类.问天石, "普通赌阶：优秀50%、杰出25%、稀有12%、完美7%、超凡4%、史诗2%", 道纹品阶.普通)
    });
    readonly int[] 库存 = new int[13];
    readonly System.Random 随机;
    readonly 天帝道纹 数据;
    public event Action 数量改变;
    public bool 无限通货 { get; private set; }
    public void 启用无限通货()
    { if (无限通货) return; 无限通货 = true; 数量改变?.Invoke(); }
    public 天帝通货(天帝道纹 数据, int 种子, int 初始数量 = 0)
    {
        this.数据 = 数据 ?? throw new ArgumentNullException(nameof(数据)); 随机 = new System.Random(种子);
        if (初始数量 < 0) throw new ArgumentOutOfRangeException(nameof(初始数量));
        for (int i = 0; i < 库存.Length; i++) 库存[i] = 初始数量;
    }
    static bool 有效种类(通货种类 种类) => (int)种类 >= 0 && (int)种类 < 定义.Count &&
        种类 != 通货种类.通脉针 && 种类 != 通货种类.六通玉;
    public int 数量(通货种类 种类) => 有效种类(种类) ? (无限通货 ? int.MaxValue : 库存[(int)种类]) : 0;
    public string 数量显示(通货种类 种类) => 无限通货 && 有效种类(种类) ? "无限" : 数量(种类).ToString();
    public int[] 导出库存() => (int[])库存.Clone();
    public void 读取库存(int[] 数量, bool 无限 = false)
    {
        if (数量 == null || 数量.Length != 库存.Length || Array.Exists(数量, x => x < 0))
            throw new System.IO.InvalidDataException("通货库存无效");
        Array.Copy(数量, 库存, 库存.Length); 无限通货 = 无限; 数量改变?.Invoke();
    }
    public bool 获得(通货种类 种类, int 数量)
    {
        if (!有效种类(种类) || 数量 <= 0 || !无限通货 && 库存[(int)种类] > int.MaxValue - 数量) return false;
        if (!无限通货) 库存[(int)种类] += 数量;
        数量改变?.Invoke(); return true;
    }
    public bool 可使用(通货种类 种类, 道纹实例 纹, int 词条索引, out string 原因)
    {
        原因 = "";
        if (!有效种类(种类)) 原因 = "未知通货";
        else if (纹 == null) 原因 = "请选择目标道纹";
        else if (纹.是源纹 || 纹.是天赋) 原因 = "起点与天赋道纹不能改造";
        else if (!数据.道纹.Contains(纹)) 原因 = "此道纹不属于当前画布";
        else if (!纹.可改造词条) 原因 = "只接受属性或功能道纹改造";
        else if (纹.是功能道纹 && 种类 != 通货种类.易纹砂 && 种类 != 通货种类.重铸石) 原因 = "功能道纹固定稀有、单词条；仅可重抽功能";
        else if (数量(种类) < 1) 原因 = "此通货数量不足";
        else if (定义[(int)种类].来源.HasValue && 纹.品阶 != 定义[(int)种类].来源.Value) 原因 = "需要" + 定义[(int)种类].来源.Value + "品阶的道纹";
        else if (种类 == 通货种类.添蕴砂 && 纹.词条.Count >= 纹.词条上限) 原因 = "已达到当前品阶的词条上限";
        else if (种类 == 通货种类.易纹砂 && (词条索引 < 0 || 词条索引 >= 纹.词条.Count)) 原因 = "请选择要洗练的词条";
        else if (种类 == 通货种类.重铸石 && 纹.词条.Count == 0) 原因 = "没有可以洗练的词条";
        return 原因.Length == 0;
    }
    道纹词条 抽词条(道纹实例 纹) => 纹.是功能道纹 ? 天帝道纹属性.抽词条(随机, 道纹属性分组.形态, 纹.物品等级) :
        纹.是五行道纹 ? 天帝道纹属性.抽词条(随机, 道纹属性分组.元素, 纹.物品等级) : 天帝道纹属性.抽非五行词条(随机, 纹.物品等级);
    // 独立的赌阶抽样，不使用天命之子的掉落品阶倍率。
    public static 道纹品阶 赌阶结果(int 百分位)
    {
        if (百分位 < 0 || 百分位 >= 100) throw new ArgumentOutOfRangeException(nameof(百分位));
        int 累计 = 0;
        for (int 序 = 0; 序 < 6; 序++) { 累计 += (int)天帝数值.取("loot.gamble_grade_weights." + 序); if (百分位 < 累计) return (道纹品阶)(序 + 1); }
        throw new InvalidOperationException("赌阶权重不完整");
    }
    public bool 使用(通货种类 种类, 道纹实例 纹, int 词条索引, out string 结果)
    {
        if (!可使用(种类, 纹, 词条索引, out 结果)) return false;
        // 先准备结果，检查失败绝不扣库存或修改实例。
        var 新词条 = new List<道纹词条>();
        foreach (var 词 in 纹.词条) 新词条.Add(词.副本());
        var 阶 = 纹.品阶;
        var 定 = 定义[(int)种类];
        道纹功能 新功能 = 纹.功能; int 新入口 = 纹.入口方向, 新接口 = 纹.接口;
        if (纹.是功能道纹)
        {
            新功能 = (道纹功能)随机.Next(1, 天帝顺序道纹.功能数量 + 1);
            新接口 = 天帝顺序道纹.调整接口数量(纹.接口, 新功能, 随机);
            新词条.Clear(); 新词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(新功能), 1));
        }
        else if (定.目标.HasValue || 种类 == 通货种类.问天石)
        {
            阶 = 定.目标 ?? 赌阶结果(随机.Next(100));
            var 规则 = 天帝道纹品阶.获取(阶);
            while (!纹.是五行道纹 && 新词条.Count < 规则.最少词条) 新词条.Add(抽词条(纹));
        }
        else if (种类 == 通货种类.添蕴砂) 新词条.Add(抽词条(纹));
        else if (种类 == 通货种类.易纹砂) 新词条[词条索引] = 抽词条(纹);
        else if (种类 == 通货种类.重铸石)
            for (int i = 0; i < 新词条.Count; i++) 新词条[i] = 抽词条(纹);
        if (!无限通货) 库存[(int)种类]--;
        纹.品阶 = 阶; 纹.词条.Clear(); 纹.词条.AddRange(新词条);
        纹.功能 = 新功能; 纹.入口方向 = 新入口; 纹.接口 = 新接口;
        if (纹.是顺序功能) 纹.介绍 = 天帝顺序道纹.功能摘要(新功能) + "；接口对应即可连接。";
        结果 = 定.名称 + "使用成功 · " + 纹.名称 + " · " + 阶 + " · " + 新词条.Count + "条词条";
        数据.重算(); 数量改变?.Invoke(); return true;
    }
}
