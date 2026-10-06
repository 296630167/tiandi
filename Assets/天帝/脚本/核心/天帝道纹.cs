using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum 道纹属性 { 力量, 速度, 智力, 血量, 灵力, 分裂, 数量, 连锁, 弧度, 范围, 金, 木, 水, 火, 土, 冰, 雷, 时间, 空间, 防御, 护盾, 攻速, 移速 }
public enum 道纹分类 { 属性, 天赋, 分叉, 功能, 特性, 转化 }
public enum 道纹功能 { 旧版, 齐射, 分裂, 连锁, 增大, 缩小, 加速, 减速, 穿透,
    扇射, 环射, 十字, 背射, 折返, 回旋, 波动, 弹墙, 跃迁, 延时, 停驻, 蓄势, 爆破, 震荡, 拖尾, 击退, 牵引, 束缚, 烙印, 陨落,
    光束, 刃波, 地刺, 剑雨, 旋刃, 灵鞭, 飞轮, 游龙, 灵网, 地雷 }
public enum 道纹属性分组 { 基础, 形态, 元素, 普通 }

public static class 天帝道纹属性
{
    public static readonly int 数量 = Enum.GetValues(typeof(道纹属性)).Length;
    public static readonly 道纹属性[] 基础属性 = { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力 };
    public static readonly 道纹属性[] 普通属性 = { 道纹属性.血量, 道纹属性.灵力, 道纹属性.防御, 道纹属性.护盾, 道纹属性.攻速, 道纹属性.移速 };
    public static readonly 道纹属性[] 形态属性 = { 道纹属性.分裂, 道纹属性.数量, 道纹属性.连锁, 道纹属性.弧度, 道纹属性.范围 };
    public static readonly 道纹属性[] 五行属性 = { 道纹属性.金, 道纹属性.木, 道纹属性.水, 道纹属性.火, 道纹属性.土 };
    public static readonly 道纹属性[] 当前属性 = { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力,
        道纹属性.血量, 道纹属性.灵力, 道纹属性.防御, 道纹属性.护盾, 道纹属性.攻速, 道纹属性.移速,
        道纹属性.分裂, 道纹属性.数量, 道纹属性.连锁, 道纹属性.弧度, 道纹属性.范围,
        道纹属性.金, 道纹属性.木, 道纹属性.水, 道纹属性.火, 道纹属性.土 };
    public static readonly 道纹属性[] 非元素属性 = { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力,
        道纹属性.血量, 道纹属性.灵力, 道纹属性.防御, 道纹属性.护盾, 道纹属性.攻速, 道纹属性.移速 };
    public static readonly 道纹属性[] 非功能属性 = { 道纹属性.力量, 道纹属性.速度, 道纹属性.智力,
        道纹属性.血量, 道纹属性.灵力, 道纹属性.防御, 道纹属性.护盾, 道纹属性.攻速, 道纹属性.移速,
        道纹属性.金, 道纹属性.木, 道纹属性.水, 道纹属性.火, 道纹属性.土 };
    public const int 当前词条数量 = 19;
    public static 道纹属性分组 分组(道纹属性 属性)
    {
        if (Array.IndexOf(基础属性, 属性) >= 0) return 道纹属性分组.基础;
        if (Array.IndexOf(普通属性, 属性) >= 0) return 道纹属性分组.普通;
        if (Array.IndexOf(形态属性, 属性) >= 0) return 道纹属性分组.形态;
        if (Array.IndexOf(五行属性, 属性) >= 0) return 道纹属性分组.元素;
        // 冰、雷、时间、空间等旧版本属性只允许存档兼容，详情仍归入元素类。
        return 道纹属性分组.元素;
    }
    public static bool 是五行(道纹属性 属性) => Array.IndexOf(五行属性, 属性) >= 0;
    public static bool 是功能(道纹属性 属性) => Array.IndexOf(形态属性, 属性) >= 0;
    public static string 分组名称(道纹属性分组 分组)
    {
        switch (分组)
        {
            case 道纹属性分组.基础: return "基础属性";
            case 道纹属性分组.普通: return "普通属性";
            case 道纹属性分组.形态: return "功能道纹";
            case 道纹属性分组.元素: return "元素属性";
            default: return "未知分组";
        }
    }
    public static string 分组名称(道纹属性 属性) => 分组名称(分组(属性));
    public static string 词条名称(道纹属性 属性) => 是五行(属性) ? 属性 + "属性额外伤害" : 属性.ToString();
    public static string 数值文字(道纹属性 属性, double 数值) => "+" + 数值.ToString("0.##") + (属性 == 道纹属性.攻速 || 属性 == 道纹属性.移速 ? "%" : "");
    public static IReadOnlyList<道纹属性> 分组属性(道纹属性分组 分组)
    {
        switch (分组)
        {
            case 道纹属性分组.基础: return 基础属性;
            case 道纹属性分组.普通: return 普通属性;
            case 道纹属性分组.形态: return 形态属性;
            case 道纹属性分组.元素: return 五行属性;
            default: throw new ArgumentOutOfRangeException(nameof(分组));
        }
    }
    public static 道纹词条 抽词条(System.Random 随机, 道纹属性分组? 限定分组 = null, int 物品等级 = 1)
    {
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        var 池 = 限定分组.HasValue ? 分组属性(限定分组.Value) : 当前属性;
        var 属性 = 池[随机.Next(池.Count)];
        return 天帝数值.抽取词条(属性, 物品等级, 随机);
    }
    public static 道纹词条 抽非五行词条(System.Random 随机, int 物品等级 = 1)
    {
        if (随机 == null) throw new ArgumentNullException(nameof(随机));
        var 属性 = 非元素属性[随机.Next(非元素属性.Length)];
        return 天帝数值.抽取词条(属性, 物品等级, 随机);
    }
}

public sealed class 道纹实例
{
    public 天赋定义 天赋 { get; internal set; }
    public bool 是天赋 => 天赋 != null && 分类 == 道纹分类.天赋;
    public int 编号;
    public readonly List<道纹词条> 词条 = new List<道纹词条>();
    // 兼容已有主词条读取入口；只映射首条，其余词条独立保留。
    public 道纹属性 属性 { get => 词条.Count > 0 ? 词条[0].属性 : 道纹属性.力量; set { 确保首条(); 词条[0].属性 = value; } }
    public int 数值 { get => (int)实际数值; set { 确保首条(); 词条[0].数值 = value; 词条[0].定点格式 = false; } }
    void 确保首条() { if (词条.Count == 0) 词条.Add(new 道纹词条(道纹属性.力量, 0)); }
    public 道纹品阶 品阶 { get; internal set; }
    public bool 是五行道纹 => 分类 == 道纹分类.属性 && 词条.Count == 1 && 天帝道纹属性.是五行(词条[0].属性);
    public bool 是功能道纹 => 分类 == 道纹分类.功能;
    public 道纹功能 功能 { get; internal set; }
    public int 特性编号 { get; internal set; }
    public bool 是特性道纹 => 分类 == 道纹分类.特性 || 分类 == 道纹分类.转化;
    public string 特性状态 { get; internal set; } = "";
    // 仅保留旧存档字段，接口不再预设入口方向。
    public int 入口方向 { get; internal set; }
    public bool 是顺序功能 => 是功能道纹 && 功能 != 道纹功能.旧版;
    public bool 允许接入(int 方向) => 有接口(方向);
    public bool 允许传出(int 方向) => 有接口(方向);
    public void 顺时针旋转接口()
    { 接口 = 天帝道纹.顺时针接口(接口); }
    public bool 可改造词条 => 分类 == 道纹分类.属性 || 是功能道纹;
    public int 词条上限 => 是功能道纹 ? (int)天帝数值.取("rune.function_affix_count") : 分类 != 道纹分类.属性 ? 0 : 是五行道纹 ? 1 : 天帝道纹品阶.获取(品阶).最多词条;
    public int 接口;
    public string 特殊效果 = "无";
    public string 介绍 = "待补充";
    public 道纹分类 分类 { get; internal set; }
    public string 类型 => 分类.ToString();
    public int 等级 { get; internal set; } = 1;
    public int 物品等级 { get; internal set; } = 1;
    public double 实际数值 => 词条.Count > 0 ? 词条[0].实际数值 : 0;
    public Vector2Int? 格子;
    public bool 生效;
    public bool 回收锁定 { get; internal set; }
    public bool 是源纹 => 编号 == 0;
    public string 名称 => 是特性道纹 ? 天帝特性道纹.名称(分类,特性编号) + "道纹" : 是天赋 ? 天赋.名称 + "天赋道纹" : 是顺序功能 ? 功能 + "道纹" : 分类 == 道纹分类.分叉 ? "分叉道纹" : 属性 + (是源纹 ? "源道纹" : "道纹");
    public string 短名 => 是特性道纹 ? 天帝特性道纹.名称(分类,特性编号) : 是天赋 ? 天赋.名称.Substring(0, 1) : 是顺序功能 ? 功能.ToString() : 分类 == 道纹分类.分叉 ? "岔" : 属性.ToString();
    public string 候选说明 =>
        是特性道纹 ? 分类 + " · " + 短名 + " · 不可改造" : 是顺序功能 ? 接口数量 + "接口 · " + 功能 : 分类 == 道纹分类.分叉 ? "连接分流 · 无属性" : 天帝道纹属性.词条名称(属性) + " " + 天帝道纹属性.数值文字(属性, 实际数值) + (词条.Count > 1 ? " 等" + 词条.Count + "条" : "");
    public int 接口数量 { get { int n = 0; for (int d = 0; d < 6; d++) if (有接口(d)) n++; return n; } }
    public bool 有接口(int 方向) => (接口 & (1 << 方向)) != 0;
    public bool 接口封印(int 方向) => 是源纹 && !有接口(方向);
    public string 详情文字()
    {
        var 字 = new StringBuilder(名称);
        if (是天赋) 字.Append("\n天赋效果：").Append(天赋.效果).Append("\n").Append(天赋.说明).Append("\n本局唯一，开局选定后不可替换");
        else if(是特性道纹)字.Append("\n").Append(介绍).Append("\n").Append(特性状态).Append("\n固定相对两口 · 仅战斗掉落 · 不可改造\n形态条件读取下游功能，原链路继续执行");
        else if (是顺序功能) 字.Append("\n功能词条：").Append(功能).Append("\n").Append(天帝顺序道纹.功能摘要(功能))
            .Append("\n接口对应即可连接；来路由实际链路决定。");
        else if (分类 == 道纹分类.分叉) 字.Append("\n仅传导、分叉，不提供属性词条；接口数量在掉落时固定，可旋转朝向。");
        else
        {
            字.Append(是功能道纹 ? "\n功能词条：" : "\n属性词条：");
            for (int i = 0; i < 词条上限; i++)
            {
                if (i > 0) 字.Append("\n");
                字.Append((i + 1).ToString("00")).Append("  ");
                if (i < 词条.Count) 字.Append("[").Append(天帝道纹属性.分组名称(词条[i].属性)).Append("] ")
                    .Append(天帝道纹属性.词条名称(词条[i].属性)).Append(" ").Append(天帝道纹属性.数值文字(词条[i].属性, 词条[i].实际数值));
                else 字.Append("<color=#708082>空词条位 · 可改造</color>");
            }
        }
        if (!是天赋) 字.Append("\n品阶：").Append(天帝道纹品阶.彩色品阶文字(品阶));
        if (!是源纹) 字.Append("\n物品等级：").Append(物品等级);
        if (!是天赋 && 可改造词条) 字.Append("\n词条：").Append(词条.Count).Append(" / ").Append(词条上限);
        if (是功能道纹) 字.Append(是顺序功能 ? "\n固定稀有、单功能；接口数符合新功能范围则保方向，否则只增减所需接口。" : "\n旧版功能保留；重抽可转为当前" + 天帝顺序道纹.功能数量 + "种功能。");
        字.Append("\n特殊效果：").Append(特殊效果).Append("\n道纹类型：").Append(类型).Append("\n道纹介绍：").Append(是顺序功能 ? 天帝顺序道纹.功能摘要(功能) : 介绍);
        if (词条.Exists(x => 天帝道纹属性.是五行(x.属性))) 字.Append("\n五行额外伤害：仅计入连接到的攻击通路");
        var 启用 = new List<string>();
        for (int d = 0; d < 6; d++) if (有接口(d)) 启用.Add(天帝道纹.方向名[d]);
        字.Append("\n启用接口：").Append(string.Join("、", 启用));
        字.Append("\n").Append(是源纹 ? "固定起点，不可移动、旋转或卸载" : (!格子.HasValue ? "待放置" :
            (生效 ? "已接源纹 · 生效" : "未接源纹 · 未生效")));
        if (是源纹)
        {
            字.Append("\n源道纹等级：").Append(等级).Append("\n接口解锁条件（每10级一个）");
            for (int i = 1; i < 6; i++)
            {
                int d = (6 - i) % 6;
                字.Append("\n").Append(天帝道纹.方向名[d]).Append("：达到").Append(i * 10).Append("级 · ").Append(有接口(d) ? "已解锁" : "封印");
            }
        }
        return 字.ToString();
    }
}

public sealed partial class 天帝道纹
{
    public const int 边长 = 100;
    public const float 半径 = 40;
    public static readonly Vector2Int[] 邻向 = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) };
    public static readonly string[] 方向名 = { "右", "右上", "左上", "左", "左下", "右下" };
    public static int 格权重(Vector2Int 格) => Math.Max(Math.Abs(格.x), Math.Max(Math.Abs(格.y), Math.Abs(格.x + 格.y)));
    public static bool 可传导(Vector2Int 起, Vector2Int 终) => 格权重(终) >= 格权重(起);
    public readonly List<道纹实例> 道纹 = new List<道纹实例>();
    public readonly Dictionary<Vector2Int, 道纹实例> 已放置 = new Dictionary<Vector2Int, 道纹实例>();
    public readonly double[] 生效加成 = new double[天帝道纹属性.数量];
    public readonly double[][] 弹槽加成 = new double[6][];
    public readonly int[] 弹槽生效数 = new int[6];
    public readonly HashSet<int>[] 弹槽道纹 = new HashSet<int>[6];
    public int 修订号 { get; private set; }
    public IEnumerable<Vector2Int> 所有解锁格 => 解锁格;
    public 天赋定义 天赋 => 已放置[Vector2Int.zero].天赋;
    public int 生效数 { get; private set; }
    public int 玩家等级 { get; private set; } = 1;
    public int 技能点 { get; private set; } = 天帝普攻.开局技能点;
    public int 当前经验 { get; private set; }
    public int 升级所需经验 => 天帝数值.升级经验(玩家等级);
    public int 迁移前等级 { get; private set; }
    readonly HashSet<Vector2Int> 解锁格 = new HashSet<Vector2Int> { Vector2Int.zero };
    public int 已解锁格数 => 解锁格.Count;
    public event Action 状态改变;
    public bool 格已解锁(Vector2Int 格) => 解锁格.Contains(格);
    public 道纹存档数据 导出存档()
    {
        var 数据 = new 道纹存档数据 { 天赋编号 = 天赋?.编号 ?? -1, 玩家等级 = 玩家等级, 技能点 = 技能点, 当前经验 = 当前经验, 迁移前等级 = 迁移前等级 };
        数据.解锁格.AddRange(解锁格);
        数据.解锁格.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        foreach (var 纹 in 道纹)
        {
            var 值 = new 道纹存档实例 { 编号 = 纹.编号, 分类 = 纹.分类, 品阶 = 纹.品阶, 接口 = 纹.接口,
                特殊效果 = 纹.特殊效果, 介绍 = 纹.介绍, 物品等级 = 纹.物品等级, 回收锁定 = 纹.回收锁定, 功能 = 纹.功能, 特性编号 = 纹.特性编号, 入口方向 = 纹.入口方向, 已放置 = 纹.格子.HasValue, 格子 = 纹.格子 ?? Vector2Int.zero };
            foreach (var 词 in 纹.词条) { var 副本 = 词.副本(); 副本.迁移精度(); 值.词条.Add(副本); }
            数据.道纹.Add(值);
        }
        foreach (var 方案 in 布局方案) 数据.布局方案.Add(方案.副本());
        return 数据;
    }
    public static 天帝道纹 读取存档(道纹存档数据 数据)
    {
        if (数据 == null || 天帝天赋.获取(数据.天赋编号) == null || 数据.玩家等级 < 1 || 数据.技能点 < 0 ||
            数据.解锁格 == null || 数据.解锁格.Count < 1 || 数据.解锁格.Count > 边长 * 边长 || 数据.道纹 == null)
            throw new System.IO.InvalidDataException("画布数据不完整");
        var 网 = new 天帝道纹(Environment.TickCount, 天帝天赋.获取(数据.天赋编号));
        if (数据.当前经验 < 0) throw new System.IO.InvalidDataException("经验不能为负");
        网.设置玩家等级(Math.Min(数据.玩家等级, 天帝数值.玩家上限)); 网.技能点 = 数据.技能点; 网.解锁格.Clear();
        网.迁移前等级 = 数据.玩家等级 > 天帝数值.玩家上限 ? 数据.玩家等级 : 数据.迁移前等级;
        网.当前经验 = 网.玩家等级 >= 天帝数值.玩家上限 ? 0 : 数据.当前经验;
        if (网.玩家等级 < 天帝数值.玩家上限 && 网.当前经验 >= 网.升级所需经验) throw new System.IO.InvalidDataException("未结算的经验无效");
        foreach (var 格 in 数据.解锁格)
            if (!在范围(格) || !网.解锁格.Add(格)) throw new System.IO.InvalidDataException("解锁格无效或重复");
        if (!网.格已解锁(Vector2Int.zero)) throw new System.IO.InvalidDataException("缺少中心格");
        var 编号 = new HashSet<int>();
        foreach (var 值 in 数据.道纹)
        {
            if (值 == null || 值.编号 <= 0 || !编号.Add(值.编号) || 值.接口 < 1 || 值.接口 > 63 ||
                值.品阶 < 道纹品阶.普通 || 值.品阶 > 道纹品阶.传说 || 值.词条 == null)
                throw new System.IO.InvalidDataException("道纹数据无效");
            var 阶 = 天帝道纹品阶.获取(值.品阶);
            if (值.分类 != 道纹分类.属性 && 值.分类 != 道纹分类.分叉 && 值.分类 != 道纹分类.功能 && 值.分类 != 道纹分类.特性 && 值.分类 != 道纹分类.转化) throw new System.IO.InvalidDataException("道纹分类无效");
            int 口数 = 0; for (int d = 0; d < 6; d++) if ((值.接口 & (1 << d)) != 0) 口数++;
            if (值.功能 != 道纹功能.旧版 && (值.分类 != 道纹分类.功能 || !天帝顺序道纹.定义有效(值.功能, 值.接口) ||
                值.词条.Count != 1 || 值.词条[0] == null || 值.词条[0].属性 != 天帝顺序道纹.兼容属性(值.功能)))
                throw new System.IO.InvalidDataException("顺序功能接口数量或机制无效");
            if (值.分类 == 道纹分类.特性 || 值.分类 == 道纹分类.转化)
            { if (!天帝特性道纹.定义有效(值.分类,值.特性编号,值.接口) || 值.词条.Count!=0 || 值.功能!=道纹功能.旧版) throw new System.IO.InvalidDataException("特性编号或接口无效"); }
            else if (值.分类 == 道纹分类.功能 ? 值.品阶 != (道纹品阶)天帝数值.取("rune.function_grade") ||
                值.词条.Count != (int)天帝数值.取("rune.function_affix_count") || 值.词条[0] == null || !天帝道纹属性.是功能(值.词条[0].属性) ||
                口数 < 天帝数值.取("rune.function_port_range.0") || 口数 > 天帝数值.取("rune.function_port_range.1") :
                值.分类 == 道纹分类.分叉 ? 值.品阶 != 道纹品阶.普通 || 值.词条.Count != 0 || 口数 < 3 || 口数 > 6 :
                (值.词条.Count < 阶.最少词条 && !(值.词条.Count == 1 && 值.词条[0] != null && 天帝道纹属性.是五行(值.词条[0].属性))) || 值.词条.Count > 阶.最多词条)
                throw new System.IO.InvalidDataException("道纹词条或接口数量无效");
            var 纹 = new 道纹实例 { 编号 = 值.编号, 品阶 = 值.品阶, 接口 = 值.接口,
                特殊效果 = 值.特殊效果 ?? "无", 介绍 = 值.介绍 ?? "待补充", 分类 = 值.分类, 回收锁定 = 值.回收锁定, 功能 = 值.功能, 特性编号 = 值.特性编号, 入口方向 = 值.入口方向, 物品等级 = 值.物品等级 == 0 ? 1 : 值.物品等级 };
            if (纹.物品等级 < 1 || 纹.物品等级 > 天帝数值.玩家上限) throw new System.IO.InvalidDataException("物品等级无效");
            foreach (var 词 in 值.词条)
            {
                if (词 == null || (int)词.属性 < 0 || (int)词.属性 >= 天帝道纹属性.数量 || 词.实际数值 < 0)
                    throw new System.IO.InvalidDataException("道纹词条无效");
                var 副本 = 词.副本(); 副本.迁移精度(); 纹.词条.Add(副本);
            }
            if (值.已放置)
            {
                if (!网.格已解锁(值.格子) || 值.格子 == Vector2Int.zero || 网.已放置.ContainsKey(值.格子))
                    throw new System.IO.InvalidDataException("道纹放置位置无效");
                纹.格子 = 值.格子; 网.已放置.Add(值.格子, 纹);
            }
            网.道纹.Add(纹);
        }
        网.读取布局方案(数据.布局方案);
        网.重算(); return 网;
    }
    public bool 解锁格子(Vector2Int 格)
    {
        if (!在范围(格) || 格已解锁(格) || 技能点 < 1) return false;
        解锁格.Add(格); 技能点--; 状态改变?.Invoke(); return true;
    }

    public static 道纹实例 创建源纹(int 编号)
    {
        if (编号 < 0 || 编号 > 2) throw new ArgumentOutOfRangeException(nameof(编号));
        return new 道纹实例 { 编号 = 0, 属性 = (道纹属性)编号, 数值 = 10, 接口 = 1, 格子 = Vector2Int.zero, 生效 = true };
    }
    public static 道纹实例 创建天赋源纹(天赋定义 天赋)
    {
        if (天赋 == null) throw new ArgumentNullException(nameof(天赋));
        return new 道纹实例 { 编号 = 0, 分类 = 道纹分类.天赋, 天赋 = 天赋, 接口 = 1, 格子 = Vector2Int.zero, 生效 = true,
            特殊效果 = "无", 介绍 = 天赋.倾向 };
    }
    public 天帝道纹(int 种子, 天赋定义 天赋) : this(种子)
    {
        已放置[Vector2Int.zero] = 创建天赋源纹(天赋);
        技能点 = 天帝普攻.开局技能点 + 天帝天赋效果.开局技能点(天赋); 重算();
    }
    public 天帝道纹(int 种子, int 源纹编号 = 0)
    {
        for (int d = 0; d < 弹槽加成.Length; d++) { 弹槽加成[d] = new double[天帝道纹属性.数量]; 弹槽道纹[d] = new HashSet<int>(); }
        for (int i = 0; i < 方案槽数; i++) 布局方案.Add(new 道纹布局方案());
        var 源 = 创建源纹(源纹编号);
        已放置.Add(Vector2Int.zero, 源);
        重算();
    }
    public bool 设置源纹等级(int 等级)
        => 设置玩家等级(等级);
    public bool 获得道纹(道纹实例 纹)
    {
        if (纹 == null || 纹.是源纹 || 纹.是天赋 ||
            (纹.分类 != 道纹分类.属性 && 纹.分类 != 道纹分类.分叉 && 纹.分类 != 道纹分类.功能 && !纹.是特性道纹) || 纹.格子.HasValue || 道纹.Contains(纹)) return false;
        int 最大 = 0;
        foreach (var 原 in 道纹) 最大 = Math.Max(最大, 原.编号);
        if (最大 == int.MaxValue) return false;
        纹.编号 = 最大 + 1; 纹.生效 = false; 道纹.Add(纹); 重算(); return true;
    }
    public bool 设置玩家等级(int 等级)
    {
        // 等级只递增；重复同步不发点，防止回设后重复领取。
        if (等级 < 玩家等级 || 等级 > 天帝数值.玩家上限) return false;
        if (等级 == 玩家等级) return true;
        技能点 += 等级 - 玩家等级; 玩家等级 = 等级;
        if (玩家等级 >= 天帝数值.玩家上限) 当前经验 = 0;
        var 源 = 已放置[Vector2Int.zero]; 源.等级 = 等级; 源.接口 = 1;
        for (int i = 1; i < 6; i++) if (等级 >= i * 天帝数值.取("progression.port_unlock_stride")) 源.接口 |= 1 << ((6 - i) % 6);
        重算(); return true;
    }
    public int 获得经验(int 数量)
    {
        if (数量 <= 0 || 玩家等级 >= 天帝数值.玩家上限) return 0;
        long 余额 = (long)当前经验 + 数量; int 原级 = 玩家等级, 新级 = 玩家等级;
        while (新级 < 天帝数值.玩家上限 && 余额 >= 天帝数值.升级经验(新级)) { 余额 -= 天帝数值.升级经验(新级); 新级++; }
        当前经验 = 新级 >= 天帝数值.玩家上限 ? 0 : (int)余额;
        if (新级 != 原级) 设置玩家等级(新级); else 状态改变?.Invoke();
        return 新级 - 原级;
    }
    public static bool 在范围(Vector2Int 格) => 格.x >= -50 && 格.x < 50 && 格.y >= -50 && 格.y < 50;
    public static Vector2 格位置(Vector2Int 格) => new Vector2(Mathf.Sqrt(3) * 半径 * (格.x + 格.y * 0.5f), 1.5f * 半径 * 格.y);
    public static Vector2Int 位置格(Vector2 点)
    {
        float r = 点.y / (1.5f * 半径), q = 点.x / (Mathf.Sqrt(3) * 半径) - r * 0.5f;
        float s = -q - r; int x = Mathf.RoundToInt(q), y = Mathf.RoundToInt(r), z = Mathf.RoundToInt(s);
        float dx = Mathf.Abs(x - q), dy = Mathf.Abs(y - r), dz = Mathf.Abs(z - s);
        if (dx > dy && dx > dz) x = -y - z; else if (dy > dz) y = -x - z;
        return new Vector2Int(x, y);
    }
    public bool 可放置(道纹实例 纹, Vector2Int 格)
    {
        return 纹 != null && !纹.是源纹 && 道纹.Contains(纹) && 在范围(格) && 格已解锁(格) && 格 != Vector2Int.zero && (!已放置.TryGetValue(格, out var 原) || 原 == 纹);
    }
    public bool 放置(道纹实例 纹, Vector2Int 格)
    {
        if (!可放置(纹, 格)) return false;
        if (纹.格子.HasValue) 已放置.Remove(纹.格子.Value);
        纹.格子 = 格; 已放置[格] = 纹; 重算(); return true;
    }
    public void 收回(道纹实例 纹)
    {
        if (纹 == null || 纹.是源纹 || !道纹.Contains(纹)) return;
        if (纹.格子.HasValue) 已放置.Remove(纹.格子.Value);
        纹.格子 = null; 重算();
    }
    public bool 旋转(道纹实例 纹)
    {
        if (纹 == null || 纹.是源纹 || !道纹.Contains(纹) || !纹.格子.HasValue ||
            !已放置.TryGetValue(纹.格子.Value, out var 当前) || 当前 != 纹) return false;
        // 方向索引沿逆时针排列，顺时针一格将每个接口移到前一方向。
        纹.顺时针旋转接口();
        重算(); return true;
    }
    public void 重算()
    {
        Array.Clear(生效加成, 0, 生效加成.Length); 生效数 = 0;
        var 源 = 已放置[Vector2Int.zero]; 源.生效 = true;
        if (!源.是天赋) 生效加成[(int)源.属性] = 源.实际数值;
        foreach (var 纹 in 道纹) 纹.生效 = false;
        var 全网 = new HashSet<Vector2Int>();
        for (int 槽 = 0; 槽 < 6; 槽++)
        {
            var 加成 = 弹槽加成[槽]; Array.Clear(加成, 0, 加成.Length); 弹槽生效数[槽] = 0; 弹槽道纹[槽].Clear();
            if (!源.有接口(槽)) continue;
            if (!源.是天赋) 加成[(int)源.属性] = 源.实际数值;
            // 每条源通路只访问一次；全网再去重一次，绕同圈或跨通路汇合都不能反复累加。
            var 队列 = new Queue<Vector2Int>(); var 已访问 = new HashSet<Vector2Int> { Vector2Int.zero };
            队列.Enqueue(Vector2Int.zero);
            while (队列.Count > 0)
            {
                var 格 = 队列.Dequeue(); var 当前 = 已放置[格];
                for (int d = 0; d < 6; d++)
                {
                    var 邻格 = 格 + 邻向[d];
                    if (格 == Vector2Int.zero && d != 槽 || !可传导(格, 邻格) || !当前.允许传出(d) ||
                        !已放置.TryGetValue(邻格, out var 邻纹) || !邻纹.允许接入((d + 3) % 6) || !已访问.Add(邻格)) continue;
                    邻纹.生效 = true; 弹槽生效数[槽]++; 弹槽道纹[槽].Add(邻纹.编号);
                    bool 首次入网 = 全网.Add(邻格);
                    if (首次入网) 生效数++;
                    if (邻纹.是顺序功能) { 队列.Enqueue(邻格); continue; }
                    foreach (var 词条 in 邻纹.词条)
                    {
                        int 索引 = (int)词条.属性;
                        if (索引 < 0 || 索引 >= 生效加成.Length) continue;
                        double 值 = 天帝数值.正值(词条.实际数值);
                        加成[索引] += 值;
                        if (首次入网) 生效加成[索引] += 值;
                    }
                    队列.Enqueue(邻格);
                }
            }
        }
        特性视图 = 天帝特性道纹.计算(this);
        if (特性视图.虚拟词条.Count > 0) 天帝特性道纹.应用转换加成(this, 特性视图);
        修订号++; 状态改变?.Invoke();
    }
}
