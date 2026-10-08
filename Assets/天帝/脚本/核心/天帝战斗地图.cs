using System;
using System.Collections.Generic;
using UnityEngine;

public enum 战斗地块 { 草地, 石径, 水域, 木桥, 林地, 王房, 平原 }
public enum 战斗难度 { 普通, 困难 }
public enum 战斗敌人级别 { 普通, 精英, 头目, 王级 }

public sealed class 战斗敌人布点
{
    public 战斗敌人级别 级别 { get; }
    public Vector2 位置 { get; }
    public 战斗敌人布点(战斗敌人级别 级别, Vector2 位置) { this.级别 = 级别; this.位置 = 位置; }
}
public sealed class 战斗敌人小队
{
    public Vector2Int 中心格 { get; }
    readonly List<战斗敌人布点> 成员数据 = new List<战斗敌人布点>();
    public IReadOnlyList<战斗敌人布点> 成员 => 成员数据;
    public 战斗敌人小队(Vector2Int 格) { 中心格 = 格; }
    internal void 添加(战斗敌人布点 点) => 成员数据.Add(点);
}

// 固定大图按图片轮廓生成通行位图；无大图时保留种子可重现的旧地图。布点不代表已生成实体。
public sealed partial class 天帝战斗地图
{
    public const int 宽 = 20, 高 = 20;
    public const int 普通数量 = 60, 精英数量 = 4, 头目数量 = 1, 王级数量 = 1, 小队数量 = 8;
    public const int 最低等级 = 天帝地图挑战.最低等级, 最高等级 = 天帝地图挑战.最高等级;
    public static int 敌人总量 => 区域参数("normal") + 区域参数("elite") + 区域参数("leader") + 区域参数("boss");
    // 未设计的经济值保持未配置，不能用0%或100%冒充确定结果。
    public static float? 道纹掉落概率(战斗难度 难度) => 天帝道纹掉落.基础概率(战斗敌人级别.普通, 难度);
    public static float? 通货掉落概率(战斗难度 难度) => 天帝通货掉落.基础概率(战斗敌人级别.普通, 难度);
    public static float? 经验倍率(战斗难度 难度) => null;
    public const float 格边长 = 4;
    public const string 场景路径 = "Assets/天帝/场景/青岚原.unity";
    public const string 名称 = "青岚原";
    public const string 说明 = "中央草甸出生，四周边缘不断涌入敌群；在开阔原野走位迎战，清剿兽潮后迎战狼王。";
    public bool 固定图片布局 { get; }
    public bool 横向区域 { get; }
    public bool 长卷布局 { get; }
    public bool 生存大图 { get; }
    public int 地块宽 { get; }
    public int 地块高 { get; }
    public float 半宽 => 地块宽 * 格边长 / 2;
    public float 半高 => 地块高 * 格边长 / 2;
    public int 区域宽 => Mathf.RoundToInt(半宽 * 2 / 区域格边长);
    public int 区域高 => Mathf.RoundToInt(半高 * 2 / 区域格边长);
    public bool 精细碰撞 => 固定图片布局 || 横向区域 || 生存大图;
    public const int 区域分辨率 = 160;
    public const float 区域格边长 = .5f;
    readonly bool[,] 通行区域;
    public int 种子 { get; }
    public int 地图等级 { get; }
    public Vector2Int 出生格 { get; private set; }
    public Vector2Int 王房中心格 { get; private set; }
    public Vector2Int 王房入口格 { get; private set; }
    public int 王房路线格数 { get; private set; }
    public float 王房路线距离 => 王房路线格数 * 格边长;
    public RectInt 王房范围 => 生存大图 ? new RectInt(0, 0, 地块宽, 地块高) : 横向区域
        ? new RectInt(区域参数("boss_start_column"), 区域参数("boss_min_row"), 地块宽 - 区域参数("boss_start_column") - 1, 区域参数("boss_rows"))
        : new RectInt(王房中心格.x - 2, 王房中心格.y - 2, 5, 5);
    readonly 战斗地块[,] 地形;
    readonly List<战斗敌人小队> 小队数据 = new List<战斗敌人小队>();
    readonly List<战斗敌人布点> 敌人数据 = new List<战斗敌人布点>();
    public IReadOnlyList<战斗敌人小队> 小队 => 小队数据;
    public IReadOnlyList<战斗敌人布点> 敌人 => 敌人数据;
    static readonly Vector2Int[] 四向 = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
    public float 半边长 => 半宽; // 兼容旧方形地图API；新逻辑分别使用半宽/半高。
    public Vector2 出生位置 => 生存大图 || 固定图片布局 && !横向区域 ? Vector2.zero : 格中心(出生格.x, 出生格.y);
    public Vector2 王位置 => 格中心(王房中心格.x, 王房中心格.y);
    public 天帝战斗地图(int 种子 = 0, bool 固定图片布局 = false, bool 横向区域 = false, Texture2D 长卷通行图 = null, int 地图等级 = 1, bool 生存大图 = false)
    {
        if (!天帝地图挑战.等级有效(地图等级)) throw new ArgumentOutOfRangeException(nameof(地图等级));
        this.地图等级 = 地图等级;
        this.生存大图 = 生存大图;
        this.种子 = 种子;
        this.横向区域 = 横向区域;
        长卷布局 = 横向区域 && 长卷通行图 != null;
        this.固定图片布局 = 固定图片布局 && !横向区域;
        地块宽 = 生存大图 ? Mathf.RoundToInt((float)天帝数值.取("map.arena.width") / 格边长) : 横向区域 ? Mathf.RoundToInt((float)天帝数值.取("map.long_region.width") / 格边长) : 宽;
        地块高 = 生存大图 ? Mathf.RoundToInt((float)天帝数值.取("map.arena.height") / 格边长) : 横向区域 ? Mathf.RoundToInt((float)天帝数值.取("map.long_region.height") / 格边长) : 高;
        地形 = new 战斗地块[地块宽, 地块高];
        var 随机 = new System.Random(种子);
        if (精细碰撞) 通行区域 = new bool[区域宽, 区域高];
        if (生存大图) { 生成生存大图(随机, 长卷通行图); return; }
        if (长卷布局) { 生成长卷区域(随机, 长卷通行图); return; }
        if (横向区域) { 生成横向区域(随机); return; }
        if (this.固定图片布局)
        {
            生成图片区域(); return;
        }
        // 出生点始终是第一项随机决策，失败重试只重做同一个出生点的布局。
        switch (随机.Next(8))
        {
            case 0: 出生格 = new Vector2Int(0, 0); break;
            case 1: 出生格 = new Vector2Int(19, 0); break;
            case 2: 出生格 = new Vector2Int(0, 19); break;
            case 3: 出生格 = new Vector2Int(19, 19); break;
            case 4: 出生格 = new Vector2Int(0, 随机.Next(1, 19)); break;
            case 5: 出生格 = new Vector2Int(19, 随机.Next(1, 19)); break;
            case 6: 出生格 = new Vector2Int(随机.Next(1, 19), 0); break;
            default: 出生格 = new Vector2Int(随机.Next(1, 19), 19); break;
        }
        for (int 尝试 = 0; 尝试 < 16; 尝试++) if (生成(随机)) return;
        throw new InvalidOperationException("无法生成满足出生、王房距离与小队布点条件的地图，种子=" + 种子);
    }
    // 顶视背景1536×1024的地面轮廓；点以图片左上角为原点，映射至既有80×80世界。
    // 同一位图负责圆形碰撞、寻路采样和红绿测试层，不能另建一套显示边界。
    static readonly Vector2[][] 图片通行轮廓 =
    {
        new[] {
            new Vector2(635,65), new Vector2(711,54), new Vector2(866,57), new Vector2(914,81),
            new Vector2(945,120), new Vector2(963,171), new Vector2(1001,193), new Vector2(1080,282),
            new Vector2(1178,311), new Vector2(1244,355), new Vector2(1275,385), new Vector2(1290,418),
            new Vector2(1374,451), new Vector2(1396,514), new Vector2(1380,567), new Vector2(1341,617),
            new Vector2(1242,620), new Vector2(1239,655), new Vector2(1160,703), new Vector2(1099,743),
            new Vector2(1072,766), new Vector2(1025,812), new Vector2(970,838), new Vector2(922,858),
            new Vector2(819,862), new Vector2(775,895), new Vector2(697,917), new Vector2(620,883),
            new Vector2(577,864), new Vector2(515,836), new Vector2(438,843), new Vector2(385,794),
            new Vector2(365,712), new Vector2(333,679), new Vector2(292,636), new Vector2(273,595),
            new Vector2(259,535), new Vector2(259,488), new Vector2(320,438), new Vector2(343,391),
            new Vector2(323,335), new Vector2(317,268), new Vector2(355,260), new Vector2(354,233),
            new Vector2(387,225), new Vector2(405,246), new Vector2(469,252), new Vector2(543,209),
            new Vector2(608,184), new Vector2(593,136), new Vector2(602,95)
        },
        // 桥面与桥另一端的石路，河岸不得以矩形整块放行。
        new[] {
            new Vector2(1005,781), new Vector2(1080,772), new Vector2(1105,797), new Vector2(1190,856),
            new Vector2(1220,869), new Vector2(1272,876), new Vector2(1328,900), new Vector2(1338,949),
            new Vector2(1305,964), new Vector2(1220,917), new Vector2(1172,897), new Vector2(1037,818)
        }
    };
    static readonly Vector2[][] 图片障碍轮廓 =
    {
        // 右侧空地边缘突出的两簇岩石。
        new[] { new Vector2(1011,224), new Vector2(1027,200), new Vector2(1046,216), new Vector2(1057,265), new Vector2(1036,276), new Vector2(1013,263) },
        new[] { new Vector2(1258,392), new Vector2(1284,377), new Vector2(1300,400), new Vector2(1340,428), new Vector2(1354,453), new Vector2(1305,457), new Vector2(1262,433) }
    };
    public static Vector2 图片位置(float x, float y) => new Vector2(x / 1536f * 80 - 40, 40 - y / 1024f * 80);
    static bool 在多边形(Vector2 点, Vector2[] 轮廓)
    {
        bool 内 = false;
        for (int i = 0, j = 轮廓.Length - 1; i < 轮廓.Length; j = i++)
        {
            var a = 轮廓[i]; var b = 轮廓[j];
            if ((a.y > 点.y) != (b.y > 点.y) && 点.x < (b.x - a.x) * (点.y - a.y) / (b.y - a.y) + a.x) 内 = !内;
        }
        return 内;
    }
    public Vector2 区域格中心(int x, int y) => new Vector2((x + .5f) * 区域格边长 - 半宽, (y + .5f) * 区域格边长 - 半高);
    public bool 区域格可通行(int x, int y) => x >= 0 && y >= 0 && x < 区域宽 && y < 区域高 && 通行区域 != null && 通行区域[x, y];
    static int 区域参数(string 键) => (int)天帝数值.取("map.long_region." + 键);
    public bool 区域内(int x, int y) => x >= 0 && x < 地块宽 && y >= 0 && y < 地块高;
    void 生成生存大图(System.Random 随机, Texture2D 图)
    {
        if (图 == null || !图.isReadable || 图.width != 区域宽 || 图.height != 区域高)
            throw new InvalidOperationException("生存大图通行图需可读且匹配0.5米采样。");
        var 像素 = 图.GetPixels32();
        for (int y=0;y<区域高;y++) for (int x=0;x<区域宽;x++) 通行区域[x,y] = 像素[y*区域宽+x].r >= 128;
        生成随机战场(随机);
        出生格 = 所在格(Vector2.zero); 王房中心格 = new Vector2Int(地块宽/2,地块高-2); 王房入口格=王房中心格;
        for (int y=0;y<地块高;y++) for (int x=0;x<地块宽;x++) 地形[x,y] = 可站立(格中心(x,y)) ? 战斗地块.草地 : 战斗地块.林地;
        if (!可站立(Vector2.zero,1.2f)) throw new InvalidOperationException("中央出生空地不可通行。");
        // 这里只预留整局单位名额；实际位置由边缘刷新器逐批选择，不在开场铺满。
        var 品质 = new List<战斗敌人级别>();
        for (int k=0;k<3;k++) for(int i=0;i<天帝数值.区域敌人数(地图等级,k);i++) 品质.Add((战斗敌人级别)k);
        for(int i=品质.Count-1;i>0;i--) {int j=随机.Next(i+1);var v=品质[i];品质[i]=品质[j];品质[j]=v;}
        foreach(var 类 in 品质) 敌人数据.Add(new 战斗敌人布点(类,Vector2.zero));
        敌人数据.Add(new 战斗敌人布点(战斗敌人级别.王级,Vector2.zero));
    }
    // 图片和通行图共享1000×200归一化坐标（与实际图片比例无关），不再按格铺装饰。
    public Vector2 长卷位置(float x, float y) => new Vector2(x / 1000 * 半宽 * 2 - 半宽, 半高 - y / 200 * 半高 * 2);
    void 生成长卷区域(System.Random 随机, Texture2D 图)
    {
        if (!图.isReadable || 图.width != 区域宽 || 图.height != 区域高) throw new InvalidOperationException("长卷通行图需可读，尺寸与0.5米采样一致。");
        var 像素 = 图.GetPixels32();
        for (int y = 0; y < 区域高; y++) for (int x = 0; x < 区域宽; x++) 通行区域[x, y] = 像素[y * 区域宽 + x].r >= 128;
        foreach (var 障碍 in 天帝长卷障碍数据.布点)
        {
            var 中 = 障碍.世界脚底(this); var 半径 = 障碍.阻挡半径;
            int 左 = Mathf.FloorToInt((中.x - 半径.x + 半宽) / 区域格边长), 右 = Mathf.CeilToInt((中.x + 半径.x + 半宽) / 区域格边长);
            int 下 = Mathf.FloorToInt((中.y - 半径.y + 半高) / 区域格边长), 上 = Mathf.CeilToInt((中.y + 半径.y + 半高) / 区域格边长);
            for (int y = Mathf.Max(0, 下); y < Mathf.Min(区域高, 上); y++) for (int x = Mathf.Max(0, 左); x < Mathf.Min(区域宽, 右); x++)
            {
                var 偏 = 区域格中心(x, y) - 中;
                if (偏.x * 偏.x / (半径.x * 半径.x) + 偏.y * 偏.y / (半径.y * 半径.y) <= 1) 通行区域[x, y] = false;
            }
        }
        出生格 = new Vector2Int(区域参数("spawn_column"), 区域参数("spawn_row"));
        王房中心格 = new Vector2Int(区域参数("boss_column"), 区域参数("boss_row"));
        王房入口格 = new Vector2Int(区域参数("boss_start_column"), 区域参数("boss_row"));
        for (int y = 0; y < 地块高; y++) for (int x = 0; x < 地块宽; x++)
            地形[x, y] = 可站立(格中心(x, y)) ? 王房范围.Contains(new Vector2Int(x, y)) ? 战斗地块.王房 : 战斗地块.草地 : 战斗地块.林地;
        if (!可站立(出生位置) || !可站立(王位置, 1.2f)) throw new InvalidOperationException("长卷出生或王区被遮挡。");
        var 路 = new 天帝战斗寻路(this); var 连通 = new List<Vector2>(); 路.取得连通节点(出生位置, 连通);
        // 先采样出生连通区，再按东西四段/上下两区均衡散布；数量增长不会把小队矩阵撑进河流。
        var 可达 = new HashSet<Vector2Int>(); foreach (var 点 in 连通) 可达.Add(路.目标格(点));
        var 分区 = new List<Vector2>[小队数量];
        for (int i = 0; i < 分区.Length; i++) 分区[i] = new List<Vector2>();
        float 间距 = (float)天帝数值.取("map.long_region.growth.scatter_spacing");
        float 安全 = (float)天帝数值.取("map.long_region.growth.spawn_safe_radius");
        float 西 = 出生位置.x + 安全, 东 = 王房范围.xMin * 格边长 - 半宽 - 2;
        float 偏x = (float)随机.NextDouble() * 间距, 偏y = (float)随机.NextDouble() * 间距;
        for (float y = -半高 + 2 + 偏y; y < 半高 - 2; y += 间距)
            for (float x = 西 + 偏x; x < 东; x += 间距)
            {
                var 点 = new Vector2(x, y);
                if (!可站立(点, .65f) || !可达.Contains(路.目标格(点)) ||
                    !路.无遮挡(点, new Vector2(Mathf.Floor(x + 半宽) + .5f - 半宽, Mathf.Floor(y + 半高) + .5f - 半高), .45f)) continue;
                int 区 = Mathf.Clamp(Mathf.FloorToInt((x - 西) / (东 - 西) * 4), 0, 3) * 2 + (y >= 0 ? 0 : 1);
                分区[区].Add(点);
            }
        int 普通总数 = 天帝数值.区域敌人数(地图等级, 0), 精英总数 = 天帝数值.区域敌人数(地图等级, 1), 头目总数 = 天帝数值.区域敌人数(地图等级, 2);
        int 总 = 普通总数 + 精英总数 + 头目总数, 容量 = 0;
        foreach (var 区 in 分区)
        {
            容量 += 区.Count;
            for (int j = 区.Count - 1; j > 0; j--) { int k = 随机.Next(j + 1); var 点 = 区[j]; 区[j] = 区[k]; 区[k] = 点; }
        }
        if (容量 < 总) throw new InvalidOperationException("长卷安全布怪容量不足：" + 容量 + "/" + 总);
        var 选中 = new List<Vector2>[小队数量]; var 索引 = new int[小队数量];
        for (int i = 0; i < 小队数量; i++) 选中[i] = new List<Vector2>();
        for (int n = 0, i = 0; n < 总; i = (i + 1) % 小队数量)
            if (索引[i] < 分区[i].Count) { 选中[i].Add(分区[i][索引[i]++]); n++; }
        // 品质标签独立洗牌，避免精英/头目全部堆在同一营地。
        var 品质 = new List<战斗敌人级别>(总);
        for (int n = 0; n < 总; n++) 品质.Add(n < 普通总数 ? 战斗敌人级别.普通 : n < 普通总数 + 精英总数 ? 战斗敌人级别.精英 : 战斗敌人级别.头目);
        for (int j = 品质.Count - 1; j > 0; j--) { int k = 随机.Next(j + 1); var 值 = 品质[j]; 品质[j] = 品质[k]; 品质[k] = 值; }
        int 品序 = 0;
        for (int i = 0; i < 小队数量; i++)
        {
            if (选中[i].Count == 0) continue;
            var 队 = new 战斗敌人小队(所在格(选中[i][0])); 小队数据.Add(队);
            foreach (var 位 in 选中[i]) { var 点 = new 战斗敌人布点(品质[品序++], 位); 队.添加(点); 敌人数据.Add(点); }
        }
        var 王路径 = new List<Vector2>();
        if (!路.路径(出生位置, 王位置, 王路径, false, false)) throw new InvalidOperationException("长卷BOSS不可达。");
        王房路线格数 = Mathf.CeilToInt(王路径.Count / 格边长);
        敌人数据.Add(new 战斗敌人布点(战斗敌人级别.王级, 王位置));
    }
    void 生成横向区域(System.Random 随机)
    {
        int 路 = 区域参数("road_row");
        出生格 = new Vector2Int(区域参数("spawn_column"), 路);
        王房中心格 = new Vector2Int(区域参数("boss_column"), 路);
        王房入口格 = new Vector2Int(区域参数("boss_start_column"), 路);
        // 平原—草原—林间原野—东部遗迹。树林成簇留空地，道路和桥梁保证东西连通。
        for (int y = 0; y < 地块高; y++) for (int x = 0; x < 地块宽; x++)
        {
            bool 林簇 = (x >= 42 && x <= 60 && (y < 14 || y > 26)) ||
                (x >= 72 && x < 85 && (y < 12 || y > 28)) ||
                (x >= 15 && x <= 25 && y > 29);
            地形[x, y] = x == 0 || y == 0 || x == 地块宽 - 1 || y == 地块高 - 1 ? 战斗地块.林地
                : 王房范围.Contains(new Vector2Int(x, y)) ? 战斗地块.王房
                : 林簇 && (x * 13 + y * 7) % 11 < 7 ? 战斗地块.林地
                : x < 28 || x > 70 ? 战斗地块.平原 : 战斗地块.草地;
        }
        for (int 河 = 0; 河 < 2; 河++)
        {
            int 河列 = 区域参数("river_columns." + 河), 副桥 = 区域参数("side_bridge_rows." + 河);
            for (int y = 1; y < 地块高 - 1; y++)
            {
                int 中 = 河列 + Mathf.RoundToInt(Mathf.Sin((y - 路) * .22f) * 2);
                if (Mathf.Abs(y - 路) <= 1) 中 = 河列;
                else if (Mathf.Abs(y - 副桥) <= 1) 中 = 河列 + Mathf.RoundToInt(Mathf.Sin((副桥 - 路) * .22f) * 2);
                for (int x = 中 - 1; x <= 中 + 1; x++) 地形[x, y] = 战斗地块.水域;
                if (Mathf.Abs(y - 路) <= 1 || Mathf.Abs(y - 副桥) <= 1)
                    for (int x = 河列 - 6; x <= 河列 + 6; x++) 地形[x, y] = 地形[x, y] == 战斗地块.水域 ? 战斗地块.木桥 : 战斗地块.石径;
            }
        }
        for (int x = 1; x < 地块宽 - 1; x++) for (int y = 路 - 1; y <= 路 + 1; y++)
            if (地形[x, y] != 战斗地块.木桥 && 地形[x, y] != 战斗地块.王房) 地形[x, y] = 战斗地块.石径;
        var 营地 = new List<Vector2Int>();
        for (int i = 0; i < 小队数量; i++)
        {
            int x = 区域参数("camp_start_column") + i * 区域参数("camp_stride") + 随机.Next(区域参数("camp_column_jitter"));
            int y = 随机.Next(区域参数("camp_min_row"), 区域参数("camp_max_row"));
            // 避开两条河的岸线；营地和接入道路不会覆盖水域或桥梁。
            foreach (int 河 in new[] { 区域参数("river_columns.0"), 区域参数("river_columns.1") })
                if (Mathf.Abs(x - 河) < 6) x = x < 河 ? 河 - 6 : 河 + 6;
            var 中 = new Vector2Int(x, y); 营地.Add(中);
            for (int yy = y - 2; yy <= y + 2; yy++) for (int xx = x - 2; xx <= x + 2; xx++) 地形[xx, yy] = 战斗地块.草地;
            for (int yy = Mathf.Min(y, 路); yy <= Mathf.Max(y, 路); yy++) for (int xx = x - 1; xx <= x + 1; xx++) 地形[xx, yy] = 战斗地块.石径;
        }
        for (int y = 0; y < 区域高; y++) for (int x = 0; x < 区域宽; x++)
            通行区域[x, y] = 可通行格(所在格(区域格中心(x, y)));
        float 间距 = (float)天帝数值.取("map.long_region.camp_spacing");
        for (int i = 0; i < 营地.Count; i++)
        {
            var 队 = new 战斗敌人小队(营地[i]); 小队数据.Add(队);
            int 普通数 = 普通数量 / 小队数量 + (i < 普通数量 % 小队数量 ? 1 : 0);
            bool 精英 = i == 1 || i == 3 || i == 5 || i == 6;
            for (int j = 0; j < 普通数 + (精英 || i == 7 ? 1 : 0); j++)
            {
                var 点 = new 战斗敌人布点(j < 普通数 ? 战斗敌人级别.普通 : 精英 ? 战斗敌人级别.精英 : 战斗敌人级别.头目,
                    格中心(营地[i].x, 营地[i].y) + new Vector2(j % 3 - 1, j / 3 - 1) * 间距);
                if (!可站立(点.位置)) throw new InvalidOperationException("横向区域营地落入障碍。");
                队.添加(点); 敌人数据.Add(点);
            }
        }
        敌人数据.Add(new 战斗敌人布点(战斗敌人级别.王级, 王位置));
        王房路线格数 = 路线距离(出生格, 王房入口格);
    }
    void 生成图片区域()
    {
        for (int y = 0; y < 区域分辨率; y++) for (int x = 0; x < 区域分辨率; x++)
        {
            var 中 = 区域格中心(x, y);
            var 像素 = new Vector2((中.x + 40) / 80 * 1536, (40 - 中.y) / 80 * 1024);
            bool 可走 = false;
            foreach (var 轮廓 in 图片通行轮廓) if (在多边形(像素, 轮廓)) { 可走 = true; break; }
            if (可走) foreach (var 障碍 in 图片障碍轮廓) if (在多边形(像素, 障碍)) { 可走 = false; break; }
            通行区域[x, y] = 可走;
        }
        for (int y = 0; y < 高; y++) for (int x = 0; x < 宽; x++)
        {
            var 中 = 格中心(x, y);
            地形[x, y] = 可站立(中, 0) ? (中.y > 22 ? 战斗地块.王房 : 战斗地块.草地) : (中.y < -25 ? 战斗地块.水域 : 战斗地块.林地);
        }
        出生格 = 所在格(Vector2.zero);
        if (!可站立(Vector2.zero)) throw new InvalidOperationException("地图中央不可站立。");
        王房中心格 = 所在格(图片位置(780, 128)); 王房入口格 = 所在格(图片位置(780, 230));
        王房路线格数 = 路线距离(出生格, 王房入口格);
        var 营地 = new[] { new Vector2(475,540), new Vector2(940,660), new Vector2(540,340), new Vector2(1070,455), new Vector2(660,470), new Vector2(810,325), new Vector2(940,280), new Vector2(740,220) };
        for (int i = 0; i < 营地.Length; i++)
        {
            var 中 = 图片位置(营地[i].x, 营地[i].y);
            var 队 = new 战斗敌人小队(所在格(中)); 小队数据.Add(队);
            int 普通数 = 普通数量 / 小队数量 + (i < 普通数量 % 小队数量 ? 1 : 0);
            bool 有精英 = i == 1 || i == 3 || i == 5 || i == 6;
            for (int j = 0; j < 普通数 + (有精英 || i == 7 ? 1 : 0); j++)
            {
                var 位置 = 中 + new Vector2((j % 3 - 1) * 1.6f, (j / 3 - 1) * 1.6f);
                if (!可站立(位置)) throw new InvalidOperationException("图片地图敌人布点落入阻挡：" + i + "/" + j);
                var 点 = new 战斗敌人布点(j < 普通数 ? 战斗敌人级别.普通 : 有精英 ? 战斗敌人级别.精英 : 战斗敌人级别.头目, 位置);
                队.添加(点); 敌人数据.Add(点);
            }
        }
        敌人数据.Add(new 战斗敌人布点(战斗敌人级别.王级, 王位置));
    }
    bool 生成(System.Random 随机)
    {
        小队数据.Clear(); 敌人数据.Clear();
        for (int y = 0; y < 高; y++) for (int x = 0; x < 宽; x++) 地形[x, y] = 战斗地块.林地;
        var 远端 = new[] { new Vector2Int(3, 3), new Vector2Int(16, 3), new Vector2Int(3, 16), new Vector2Int(16, 16) };
        王房中心格 = 远端[随机.Next(4)];
        foreach (var 格 in 远端) if ((格 - 出生格).sqrMagnitude > (王房中心格 - 出生格).sqrMagnitude) 王房中心格 = 格;
        var 朝内 = 王房中心格.x < 10 ? Vector2Int.right : Vector2Int.left;
        王房入口格 = 王房中心格 + 朝内 * 3;
        清空地块(出生格, 2, 战斗地块.草地);
        var 候选 = new List<Vector2Int>();
        foreach (int y in new[] { 3, 7, 12, 16 }) foreach (int x in new[] { 3, 7, 12, 16 })
        {
            var 格 = new Vector2Int(x, y);
            if ((格 - 出生格).sqrMagnitude >= 36 && !(Mathf.Abs(x - 王房中心格.x) <= 4 && Mathf.Abs(y - 王房中心格.y) <= 4)) 候选.Add(格);
        }
        if (候选.Count < 小队数量) return false;
        // 分散采样营地中心，敌人仍聚集在各营地，避免整张图均匀撒怪。
        var 营地 = new List<Vector2Int>(); 营地.Add(候选[随机.Next(候选.Count)]); 候选.Remove(营地[0]);
        while (营地.Count < 小队数量)
        {
            int 最佳 = -1, 距离 = -1;
            for (int i = 0; i < 候选.Count; i++)
            {
                int 最近 = int.MaxValue; foreach (var 已选 in 营地) 最近 = Math.Min(最近, (候选[i] - 已选).sqrMagnitude);
                if (最近 > 距离 || 最近 == 距离 && 随机.Next(2) == 0) { 距离 = 最近; 最佳 = i; }
            }
            营地.Add(候选[最佳]); 候选.RemoveAt(最佳);
        }
        营地.Sort((a, b) => (a - 出生格).sqrMagnitude.CompareTo((b - 出生格).sqrMagnitude));
        var 守门营地 = 营地[0];
        foreach (var 格 in 营地) if ((格 - 王房入口格).sqrMagnitude < (守门营地 - 王房入口格).sqrMagnitude) 守门营地 = 格;
        营地.Remove(守门营地); 营地.Add(守门营地);
        var 上个 = 出生格;
        foreach (var 格 in 营地)
        {
            清空地块(格, 1, 战斗地块.草地);
            if (!铺路(上个, 格, 随机)) return false;
            上个 = 格;
        }
        if (!铺路(上个, 王房入口格 + 朝内, 随机)) return false;
        // 王房外围始终是阻挡，仅在朝向地图内部的一格入口开口。
        清空地块(王房中心格, 2, 战斗地块.王房);
        地形[王房入口格.x, 王房入口格.y] = 战斗地块.石径;
        王房路线格数 = 路线距离(出生格, 王房入口格);
        if (Vector2.Distance(出生位置, 王位置) < 56 || 王房路线格数 < 20) return false;
        // 非通行地块中的少量水面，只作地貌；不会覆盖出生、道路、营地或王房。
        for (int i = 0; i < 24; i++)
        {
            int x = 随机.Next(1, 19), y = 随机.Next(1, 19);
            if (!王房保护(x, y) && 地形[x, y] == 战斗地块.林地) 地形[x, y] = 战斗地块.水域;
        }
        for (int i = 0; i < 营地.Count; i++)
        {
            var 队 = new 战斗敌人小队(营地[i]); 小队数据.Add(队);
            int 普通数 = 普通数量 / 小队数量 + (i < 普通数量 % 小队数量 ? 1 : 0); // 共60；4个精英随队，头目位于接近王房的最后一队。
            int 精英数 = i == 1 || i == 3 || i == 5 || i == 6 ? 1 : 0;
            for (int j = 0; j < 普通数 + 精英数 + (i == 7 ? 1 : 0); j++)
            {
                var 中 = 格中心(营地[i].x, 营地[i].y);
                var 偏移 = new Vector2((j % 3 - 1) * 2.5f, (j / 3 - 1) * 2.5f);
                var 点 = new 战斗敌人布点(j < 普通数 ? 战斗敌人级别.普通 : 精英数 > 0 ? 战斗敌人级别.精英 : 战斗敌人级别.头目, 中 + 偏移);
                if (!可站立(点.位置) || Vector2.Distance(出生位置, 点.位置) < 16) return false;
                队.添加(点); 敌人数据.Add(点);
            }
        }
        敌人数据.Add(new 战斗敌人布点(战斗敌人级别.王级, 王位置));
        return true;
    }
    bool 王房保护(int x, int y) => Mathf.Abs(x - 王房中心格.x) <= 3 && Mathf.Abs(y - 王房中心格.y) <= 3;
    void 清空地块(Vector2Int 中, int 半径, 战斗地块 类)
    {
        for (int y = 中.y - 半径; y <= 中.y + 半径; y++) for (int x = 中.x - 半径; x <= 中.x + 半径; x++)
            if (在范围(x, y) && (类 == 战斗地块.王房 || !王房保护(x, y))) 地形[x, y] = 类;
    }
    bool 铺路(Vector2Int 起, Vector2Int 终, System.Random 随机)
    {
        var 前 = new Dictionary<Vector2Int, Vector2Int>(); var 队 = new Queue<Vector2Int>(); 队.Enqueue(起); 前[起] = 起;
        int 偏移 = 随机.Next(4);
        while (队.Count > 0 && !前.ContainsKey(终))
        {
            var 格 = 队.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                var 新 = 格 + 四向[(i + 偏移) % 4];
                if (在范围(新.x, 新.y) && !王房保护(新.x, 新.y) && !前.ContainsKey(新)) { 前[新] = 格; 队.Enqueue(新); }
            }
        }
        if (!前.ContainsKey(终)) return false;
        for (var 格 = 终; ; 格 = 前[格])
        {
            地形[格.x, 格.y] = 战斗地块.石径;
            // 两格宽通路；保护圈内不扩宽，王房仍只有一个入口。
            foreach (var 向 in new[] { Vector2Int.right, Vector2Int.up, Vector2Int.one })
            {
                var 邻 = 格 + 向;
                if (在范围(邻.x, 邻.y) && !王房保护(邻.x, 邻.y)) 地形[邻.x, 邻.y] = 战斗地块.石径;
            }
            if (格 == 起) break;
        }
        return true;
    }
    public int 路线距离(Vector2Int 起, Vector2Int 终)
    {
        if (!可通行格(起) || !可通行格(终)) return -1;
        var 距离 = new Dictionary<Vector2Int, int>(); var 队 = new Queue<Vector2Int>(); 队.Enqueue(起); 距离[起] = 0;
        while (队.Count > 0)
        {
            var 格 = 队.Dequeue(); if (格 == 终) return 距离[格];
            foreach (var 向 in 四向) { var 新 = 格 + 向; if (可通行格(新) && !距离.ContainsKey(新)) { 距离[新] = 距离[格] + 1; 队.Enqueue(新); } }
        }
        return -1;
    }
    public bool 可通行格(Vector2Int 格) => 区域内(格.x, 格.y) && 地块(格.x, 格.y) != 战斗地块.林地 && 地块(格.x, 格.y) != 战斗地块.水域;
    public 战斗地块 地块(int x, int y)
    {
        if (!区域内(x, y)) return 战斗地块.林地;
        return 地形[x, y];
    }
    public static bool 在范围(int x, int y) => x >= 0 && x < 宽 && y >= 0 && y < 高;
    public Vector2 格中心(int x, int y) => new Vector2((x + 0.5f) * 格边长 - 半宽, (y + 0.5f) * 格边长 - 半高);
    public Vector2Int 所在格(Vector2 点) => new Vector2Int(Mathf.FloorToInt((点.x + 半宽) / 格边长), Mathf.FloorToInt((点.y + 半高) / 格边长));
    public bool 可站立(Vector2 点, float 半径 = 0.45f)
    {
        if (!有限(点.x) || !有限(点.y) || !有限(半径) || 半径 < 0) return false;
        if (点.x - 半径 < -半宽 || 点.y - 半径 < -半高 || 点.x + 半径 >= 半宽 || 点.y + 半径 >= 半高) return false;
        if (精细碰撞)
        {
            int 左 = Mathf.FloorToInt((点.x - 半径 + 半边长) / 区域格边长), 右 = Mathf.FloorToInt((点.x + 半径 + 半边长) / 区域格边长);
            int 下边 = Mathf.FloorToInt((点.y - 半径 + 半高) / 区域格边长), 上边 = Mathf.FloorToInt((点.y + 半径 + 半高) / 区域格边长);
            for (int y = 下边; y <= 上边; y++) for (int x = 左; x <= 右; x++)
            {
                if (区域格可通行(x, y)) continue;
                var 中 = 区域格中心(x, y);
                float dx = Mathf.Max(0, Mathf.Abs(点.x - 中.x) - 区域格边长 / 2), dy = Mathf.Max(0, Mathf.Abs(点.y - 中.y) - 区域格边长 / 2);
                if (dx * dx + dy * dy <= 半径 * 半径) return false;
            }
            return true;
        }
        var 下 = 所在格(点 - Vector2.one * 半径); var 上 = 所在格(点 + Vector2.one * 半径);
        for (int y = 下.y; y <= 上.y; y++) for (int x = 下.x; x <= 上.x; x++)
        {
            var 类 = 地块(x, y); if (类 != 战斗地块.水域 && 类 != 战斗地块.林地) continue;
            var 中 = 格中心(x, y);
            float dx = Mathf.Max(0, Mathf.Abs(点.x - 中.x) - 格边长 / 2), dy = Mathf.Max(0, Mathf.Abs(点.y - 中.y) - 格边长 / 2);
            if (dx * dx + dy * dy <= 半径 * 半径) return false;
        }
        return true;
    }
    public Vector2 移动(Vector2 当前位置, Vector2 方向, float 距离)
    {
        if (!可站立(当前位置) || !有限(方向.x) || !有限(方向.y) || !有限(距离) || 距离 <= 0) return 当前位置;
        var 位移 = Vector2.ClampMagnitude(方向, 1) * Mathf.Min(距离, 100);
        // 分步碰撞，低帧率和高速移动也不能穿过一格水域。
        int 步数 = Mathf.Max(1, Mathf.CeilToInt(位移.magnitude / 0.25f)); var 步 = 位移 / 步数;
        for (int i = 0; i < 步数; i++)
        {
            var 新 = 当前位置 + 步;
            if (可站立(新)) 当前位置 = 新;
            else
            {
                新 = 当前位置 + new Vector2(步.x, 0); if (可站立(新)) 当前位置 = 新;
                新 = 当前位置 + new Vector2(0, 步.y); if (可站立(新)) 当前位置 = 新;
            }
        }
        return 当前位置;
    }
    public Vector2 直线闪避(Vector2 起, Vector2 向, float 距)
    {
        if (!可站立(起) || !有限(向.x) || !有限(向.y) || !有限(距) || 距 <= 0) return 起;
        var 位移 = Vector2.ClampMagnitude(向, 1) * Mathf.Min(距, 100);
        int 数 = Mathf.Max(1, Mathf.CeilToInt(位移.magnitude / .1f)); var 步 = 位移 / 数;
        for (int i = 0; i < 数; i++) { if (!可站立(起 + 步)) break; 起 += 步; }
        return 起;
    }
    public Color 地块颜色(int x, int y)
    {
        switch (地块(x, y))
        {
            case 战斗地块.平原: return new Color(.52f, .59f, .36f);
            case 战斗地块.王房: return new Color(0.46f, 0.36f, 0.30f);
            case 战斗地块.石径: return new Color(0.57f, 0.55f, 0.43f);
            case 战斗地块.水域: return new Color(0.16f, 0.40f, 0.48f);
            case 战斗地块.木桥: return new Color(0.65f, 0.46f, 0.25f);
            case 战斗地块.林地: return new Color(0.12f, 0.25f, 0.22f);
            default: float 差 = ((x * 17 + y * 31) % 5) * 0.012f; return new Color(0.30f + 差, 0.43f + 差, 0.31f + 差);
        }
    }
    static bool 有限(float 值) => !float.IsNaN(值) && !float.IsInfinity(值);
}
