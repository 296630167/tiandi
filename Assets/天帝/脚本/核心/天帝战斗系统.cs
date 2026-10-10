using System;
using System.Collections.Generic;
using UnityEngine;

public enum 敌人行动 { 待机, 追击, 搜索, 蓄力, 后摇, 归巢, 死亡 }
public sealed class 战斗敌人
{
    public 战斗敌人布点 布点 { get; }
    public Vector2 位置 { get; internal set; }
    public float 血量 { get; internal set; }
    public float 最大血量 { get; private set; }
    public int 物种 { get; internal set; }
    public int 地图档位 { get; }
    public int 形态 => 物种 == 14 ? 天帝敌种配置.形态(地图档位, 血量 / 最大血量) : 1;
    public string 名称 => 天帝敌种配置.名称(物种) + (物种 == 14 ? "·" + 天帝敌种配置.形态名(形态) : "");
    public float 护盾量 { get; internal set; }
    public float 累计恢复, 累计授盾, 盾剩余, 盾禁用, 最深伤损;
    public float 跳跃高度 { get; internal set; }
    internal int 技能编号, 显示形态 = 1;
    internal float 技能等待, 退步秒, 停步秒, 形态提示秒;
    internal float 突袭冷却, 蓄力总秒;
    internal float 战术决策秒;
    internal Vector2 战术站位;
    internal 战斗敌人 掩护队友;
    internal Vector2 锁定方向;
    internal readonly List<战斗敌人> 辅助目标 = new List<战斗敌人>(2);
    internal void 设置物种(int 种)
    { 物种 = 种; 最大血量 = (float)天帝数值配置.敌人(地图档位, (int)布点.级别, 0) * 天帝敌种配置.物种(种, "hp_factor"); 血量 = 最大血量; }
    public float 攻击力 { get; }
    public float 防御 { get; }
    public float 速度 { get; }
    public float 抗性 { get; }
    public float 攻击间隔 => (float)(二阶段 ? 天帝数值.敌参数(布点.级别, "phase_period") : 天帝数值.敌参数(布点.级别, "period"));
    public float 实际移速 => 速度 + (二阶段 ? (float)天帝数值.敌参数(布点.级别, "phase_speed_gain") : 0);
    public float 战术周期倍率 => (float)天帝数值.敌战术成长(地图档位, 物种 == 14 ? "boss_period_end" : "ordinary_period_end");
    public float 战术移速 => 实际移速 * (float)天帝数值.敌战术成长(地图档位, 物种 == 14 ? "boss_move_end" : "move_end");
    public float 束缚剩余秒 { get; internal set; }
    internal float 特性减速,特性减速秒,特性弱化,特性弱化秒,特性易伤,特性易伤秒,特性控制窗口,特性控制累计;
    public float 特性移速倍率 => 特性减速秒>0?1-特性减速:1;
    public float 特性伤害倍率 => 特性弱化秒>0?1-特性弱化:1;
    public bool 二阶段 => 布点.级别 == 战斗敌人级别.王级 && 血量 / 最大血量 <= 天帝数值.敌参数(布点.级别, "phase_hp");
    public int 等级 { get; }
    public int 登场波次 { get; internal set; }
    public float 登场剩余秒 { get; internal set; }
    public bool 已生成 { get; internal set; }
    public bool 存活 => 已生成 && 血量 > 0;
    public bool 显示血条 => 存活 && (血量 < 最大血量 || 护盾量 > 0 || 布点.级别 == 战斗敌人级别.王级);
    public 敌人行动 行动 { get; internal set; }
    public float 死亡秒 { get; internal set; }
    public float 闪白秒 { get; internal set; }
    internal readonly List<Vector2> 路线 = new List<Vector2>(32);
    internal int 路线索引;
    internal float 再寻路, 冷却, 蓄力;
    internal Vector2Int 上个目标格 = new Vector2Int(-100, -100);
    internal Vector2 攻击落点;
    public float 攻击范围 => (float)天帝数值.敌参数(布点.级别, "range");
    public float 本次攻击范围 { get; internal set; }
    public double 本次技能倍率 { get; internal set; } = 1;
    public bool 本次范围技 { get; internal set; }
    public bool 本次突进 { get; internal set; }
    public int 已攻击次数 { get; internal set; }
    internal int 小队编号 = -1, 序号;
    internal float 感知冷却, 失视秒, 搜索秒, 后摇秒, 警戒禁用秒, 卡住秒, 分离冷却;
    internal bool 看见玩家;
    internal Vector2 最后目标, 分离方向;
    public 战斗敌人(战斗敌人布点 点, 战斗难度 难度, int 地图等级 = 1)
    {
        布点 = 点; 位置 = 点.位置;
        int i = (int)点.级别;
        等级 = 天帝地图挑战.敌人等级(地图等级, 点.级别);
        int 档 = Mathf.Clamp(地图等级, 1, (int)天帝数值.取("levels.map_max"));
        地图档位 = 档;
        最大血量 = (float)天帝数值配置.敌人(档, i, 0); 血量 = 最大血量;
        攻击力 = (float)天帝数值配置.敌人(档, i, 1);
        防御 = (float)天帝数值配置.敌人(档, i, 2); 抗性 = (float)天帝数值配置.敌人(档, i, 3);
        速度 = (float)天帝数值配置.敌人(档, i, 4); 本次攻击范围 = 攻击范围;
    }
}
public sealed class 战斗释放记录
{
    public long 编号 { get; internal set; }
    public double 暴击倍率 { get; internal set; }
    public double 暴击采样 { get; internal set; }
    internal readonly HashSet<int> 命中目标 = new HashSet<int>();
    internal readonly HashSet<int> 根目标 = new HashSet<int>();
    internal int 顺序生成数;
    internal int 扩展效果数;
    internal bool 根普攻=true;
    internal bool 手动瞄准;
}
public sealed class 战斗灵矢
{
    public Vector2 位置, 方向;
    public float 伤害, 剩余距离;
    public int 目标, 剩余连锁;
    public bool 子矢, 已分裂;
    internal bool 根普攻弹;
    // 连锁是已选定的直接攻击，不经过飞行与碰撞；待处理列表避免递归扩张。
    public bool 自动连锁;
    public int 剩余穿透 = -1;
    public float 等待秒, 等待总秒, 已飞距离, 波动相位, 回旋累计角, 拖尾余距;
    public int 扩展阶段, 标记敌人 = -1;
    public Vector2 发射点;
    internal List<Vector2> 折返轨迹;
    internal int 回程节点;
    internal float 形态计时;
    internal int 形态步数;
    internal Vector2 形态锚点, 形态上一中心;
    internal HashSet<int> 形态进入历史;
    internal Dictionary<int, float> 形态命中时刻;
    public 普攻参数 参数;
    public 战斗释放记录 释放 { get; internal set; }
    public double 形态倍率 { get; internal set; } = 1;
    public 道纹执行段 执行段;
    public double 实际暴击倍率 => 执行段 == null ? 释放.暴击倍率 : 释放.暴击采样 < 参数.暴击率 ? 参数.暴击倍率 : 1;
    internal HashSet<int> 独立命中;
    internal int 根目标 = -1;
    internal HashSet<int> 命中过 => 独立命中 ?? 释放.命中目标;
}
public sealed class 战斗光圈
{
    public Vector2 位置;
    public float 半径, 剩余秒 = .18f;
    public bool 敌方;
}
// 纯演出数据，不参与命中或随机掉落。数量和生命周期都有上限。
public sealed class 战斗电弧
{
    public Vector2 起点, 终点;
    public float 剩余秒, 寿命;
    public int 种子;
    public bool 跳链;
}

// 同一场景统一驱动，敌人没有各自Update；寻路缓存只在目标换格或间隔到期时重建。
public sealed partial class 天帝战斗系统
{
    public IReadOnlyList<战斗敌人> 敌人 => 敌人数据;
    public IReadOnlyList<战斗灵矢> 灵矢 => 灵矢数据;
    public IReadOnlyList<战斗光圈> 光圈 => 光圈数据;
    public IReadOnlyList<战斗电弧> 电弧 => 电弧数据;
    public int 击败数 { get; private set; }
    public int 普通释放次数 { get; private set; }
    public int 分裂生成数 { get; private set; }
    public int 连锁发生数 { get; private set; }
    public int 溅射命中数 { get; private set; }
    public int 回响次数 { get; private set; }
    public int 本局经验 { get; private set; }
    public int 本局升级次数 { get; private set; }
    public int 暴击释放次数 { get; private set; }
    public int 当前通路 { get; private set; }
    public readonly int[] 通路释放次数 = new int[6];
    public int 开放通路数 => 道纹.开放通路数;
    public int 参与通路数 => 道纹.射击通路数;
    public int 当前波次 { get; private set; } = 1;
    public int 场上敌人数量 { get { int n = 0; foreach (var 敌 in 敌人数据) if (敌.存活) n++; return n; } }
    public int 未生成敌人数量 { get { int n = 0; foreach (var 敌 in 敌人数据) if (!敌.已生成) n++; return n; } }
    public bool BOSS已出现 { get; private set; }
    public int 地图等级 { get; }
    public int 生存场上上限 => Mathf.RoundToInt((float)天帝数值.大图成长(地图等级, "active"));
    public int 生存批次数量 => Mathf.RoundToInt((float)天帝数值.大图成长(地图等级, "batch"));
    public float 生存刷新间隔 => (float)天帝数值.大图成长(地图等级, "interval");
    public int 剩余敌人数量 => 场上敌人数量 + 未生成敌人数量;
    public int 分类剩余(战斗敌人级别 类) { int 数 = 0; foreach (var 敌 in 敌人数据) if (敌.布点.级别 == 类 && (!敌.已生成 || 敌.存活)) 数++; return 数; }
    public int 分类在场(战斗敌人级别 类) { int 数 = 0; foreach (var 敌 in 敌人数据) if (敌.布点.级别 == 类 && 敌.存活) 数++; return 数; }
    public string 刷新阶段 => 玩家死亡 ? "挑战结束" : 地图.生存大图 ? BOSS已出现 ? "狼王来袭 · 四面兽潮" : "第"+当前波次+"批 · "+增援节奏 : 地图.横向区域
        ? 地图.王房范围.Contains(地图.所在格(玩家)) ? "东端BOSS区域" : "向东探索 · 沿途营地"
        : BOSS已出现 ? 敌人数据[敌人数据.Count - 1].名称 : 本波精英阶段 ? (当前波次 == 5 ? "头目压阵" : "精英来袭") : "第 " + 当前波次 + " / 5 波 · 小怪群";
    public float 敌人损伤比例 => 总敌人最大生命 <= 0 ? 0 : Mathf.Clamp01(累计敌人损伤 / 总敌人最大生命);
    public bool 玩家死亡 => 主角.当前血量 <= 0;
    public 普攻参数 当前普攻 => 读取通路参数(当前通路);
    // HUD 与战斗演出共用同一套最近目标判定，避免识别环和实际索敌对象不一致。
    public 战斗敌人 最近目标
    {
        get
        {
            int 索引 = 找目标(玩家, (float)天帝数值.取("player.range_max"), null);
            return 索引 >= 0 ? 敌人数据[索引] : null;
        }
    }
    public 天帝道纹掉落 掉落 { get; }
    public 天帝通货掉落 通货掉落 { get; }
    public 天帝灵石掉落 灵石掉落 { get; }
    public int 本次寻路次数 => 敌人AI.本次寻路次数;
    public event Action<战斗敌人> 敌人死亡;
    public event Action<Vector2, float, bool> 伤害反馈;
    public event Action<Vector2, 战斗伤害明细, bool> 伤害分项反馈;
    public event Action<战斗受击反馈> 受击表现反馈;
    public event Action<Vector2> 准备射击;
    public event Action<Vector2> 射击释放;
    public float 射击前摇 { get; set; } = (float)天帝数值.取("player.attack_windup");
    readonly List<战斗敌人> 敌人数据 = new List<战斗敌人>();
    readonly List<战斗灵矢> 灵矢数据 = new List<战斗灵矢>(128);
    readonly List<战斗灵矢> 待加灵矢 = new List<战斗灵矢>();
    readonly List<战斗光圈> 光圈数据 = new List<战斗光圈>(32);
    readonly List<战斗电弧> 电弧数据 = new List<战斗电弧>(256);
    int 电弧序号;
    void 留电弧(Vector2 起, Vector2 终, bool 连 = false)
    {
        if (电弧数据.Count >= 256) 电弧数据.RemoveAt(0);
        float 寿命 = 连 ? .18f : .10f;
        电弧数据.Add(new 战斗电弧 { 起点 = 起, 终点 = 终, 跳链 = 连, 寿命 = 寿命, 剩余秒 = 寿命, 种子 = ++电弧序号 });
    }
    readonly 天帝战斗地图 地图;
    readonly 天帝战斗寻路 寻路;
    readonly 天帝敌人刷新 刷新选点;
    readonly 天帝道纹 道纹;
    readonly 天帝主角属性 主角;
    readonly 天帝余响计数 余响;
    readonly 天帝敌人AI 敌人AI;
    public 天帝敌人战术 战术 { get; }
    readonly System.Random 战斗随机;
    long 释放序号;
    Vector2 玩家;
    float 发射冷却, 回响剩余 = -1;
    float 前摇剩余 = -1;
    int 前摇通路;
    普攻参数 回响参数;
    readonly 普攻参数[] 通路参数缓存 = new 普攻参数[6];
    int 参数修订 = -1;
    float 参数攻击力, 参数速度, 参数攻速, 参数灵力, 参数当前血量, 参数最大血量;
    float 波次等待;
    float 刷新重试剩余;
    int 生存待刷名额, 生存队列索引;
    int 生存增援轮;
    public string 增援节奏 => 生存待刷名额>0?"增援入场":生存增援轮>0&&生存增援轮%(int)天帝数值.取("map.arena.pacing.breath_every")==0?"短暂喘息":"兽潮集结";
    bool 本波精英阶段;
    float 累计敌人损伤, 总敌人最大生命;
    public 天帝战斗系统(天帝战斗地图 地图, 天帝道纹 道纹, 天帝主角属性 主角, 战斗难度 难度, 天帝通货 通货 = null, int 地图等级 = 1, Func<Rect> 读取视野 = null, 天帝宝盒 宝盒 = null)
    {
        this.地图 = 地图; this.道纹 = 道纹; this.主角 = 主角; 寻路 = new 天帝战斗寻路(地图);
        if (!天帝地图挑战.等级有效(地图等级)) throw new ArgumentOutOfRangeException(nameof(地图等级));
        this.地图等级 = 地图等级;
        余响 = new 天帝余响计数(道纹.天赋);
        战斗随机 = new System.Random(unchecked(地图.种子 ^ 0x278DA5));
        foreach (var 点 in 地图.敌人) 敌人数据.Add(new 战斗敌人(点, 难度, 地图等级));
        var 队列 = 地图.横向区域 || 地图.生存大图 ? 天帝敌种配置.区域队列(地图等级, 地图.种子, 地图.敌人.Count) : 天帝敌种配置.普通队列(地图等级, 地图.种子); int 普 = 0, 精 = 0, 头 = 0;
        foreach (var 敌 in 敌人数据)
        {
            int 种;
            if (敌.布点.级别 == 战斗敌人级别.普通) 种 = 队列[普++ % 队列.Length];
            else if (敌.布点.级别 == 战斗敌人级别.王级) 种 = 14;
            else if (敌.布点.级别 == 战斗敌人级别.头目) 种 = 天帝敌种配置.已解锁(13, 地图等级) && (头++ + 地图.种子) % 2 != 0 ? 13 : 12;
            else { 种 = 地图.横向区域 || 地图.生存大图 ? 队列[精++ % 队列.Length] : new[] { 8, 9, 3, 10 }[精++ % 4]; if (!天帝敌种配置.已解锁(种, 地图等级)) 种 = 0; }
            敌.设置物种(种);
        }
        foreach (var 敌 in 敌人数据) if (敌.布点.级别 != 战斗敌人级别.王级) 总敌人最大生命 += 敌.最大血量;
        玩家 = 地图.出生位置;
        if (!地图.横向区域 && (地图.生存大图 || 地图.固定图片布局 || 读取视野 != null)) 刷新选点 = new 天帝敌人刷新(地图, 寻路, 敌人数据, 读取视野);
        敌人AI = new 天帝敌人AI(地图, 寻路, 敌人数据, (敌, 倍率, 范围) => 伤害玩家(敌, 倍率, 范围), 光圈数据, 刷新选点 != null);
        战术 = new 天帝敌人战术(地图, 寻路, 敌人数据, 包 => 结算玩家受伤(包, false),
            (包, 可闪避) => 结算玩家受伤(包, 可闪避), 总敌人最大生命, 读取视野);
        敌人AI.战术 = 战术;
        敌人AI.特性目标 = 特性诱饵目标; 敌人AI.特性阻挡 = 特性土垒阻挡;
        敌人AI.特性挡路=特性墙挡路;
        战术.特性目标=特性诱饵目标;战术.特性受击=特性目标受击;战术.特性免疫=()=>特性免控;
        掉落 = new 天帝道纹掉落(地图, 道纹, 难度);
        通货掉落 = new 天帝通货掉落(地图, 道纹, 通货, 难度);
        灵石掉落 = new 天帝灵石掉落(宝盒, 地图.种子, 道纹.天赋);
        if (地图.横向区域)
        {
            foreach (var 敌 in 敌人数据) { 敌.已生成 = true; 敌.行动 = 敌人行动.待机; 敌.登场波次 = 敌.小队编号 + 1; }
            BOSS已出现 = true;
        }
        else if (地图.生存大图) 波次等待 = (float)天帝数值.取("map.arena.first_delay");
        else 初始化波次();
        当前通路 = 下一开放通路();
    }
    public 普攻参数 读取通路参数(int 通路)
    {
        if (通路 < 0 || 通路 >= 6) throw new ArgumentOutOfRangeException(nameof(通路));
        if (参数修订 != 道纹.修订号 || 参数攻击力 != 主角.攻击力 || 参数速度 != 主角.速度 || 参数攻速 != 主角.攻击速度 || 参数灵力 != 主角.灵力 || 参数当前血量 != 主角.当前血量 || 参数最大血量 != 主角.血量)
        {
            参数修订 = 道纹.修订号; 参数攻击力 = 主角.攻击力; 参数速度 = 主角.速度; 参数攻速 = 主角.攻击速度; 参数灵力 = 主角.灵力; 参数当前血量 = 主角.当前血量; 参数最大血量 = 主角.血量;
            Array.Clear(通路参数缓存, 0, 通路参数缓存.Length);
        }
        return 通路参数缓存[通路] ?? (通路参数缓存[通路] = 普攻参数.读取通路(道纹, 主角, 通路));
    }
    int 下一开放通路()
    {
        for (int i = 0; i < 6; i++) { int d = 技能通路(i); if (道纹.通路开放(d)) return d; }
        return 0;
    }
    void 初始化波次()
    {
        当前波次 = 1; 本波精英阶段 = false; 波次等待 = 0; 生成下一组小怪();
    }
    bool 生成(战斗敌人 敌, bool 生存预算已检查 = false)
    {
        if (敌.已生成) return true;
        if (!地图.生存大图 && !战术.可投放(敌)) return false;
        int 上限 = 地图.生存大图 ? 生存场上上限 : 天帝地图挑战.场上上限;
        if (!生存预算已检查 && 场上敌人数量 >= 上限 + (敌.布点.级别 == 战斗敌人级别.王级 ? 1 : 0)) return false;
        if (刷新选点 != null)
        {
            if (!刷新选点.尝试选点(敌.布点.级别, 玩家, out var 点)) return false;
            敌.位置 = 点;
        }
        敌.已生成 = true; 敌.死亡秒 = 0; if (敌.登场波次 == 0) 敌.登场波次 = 当前波次;
        敌.登场剩余秒 = (float)天帝数值.取("map.spawn_warning");
        敌.行动 = 刷新选点 != null ? 敌人行动.追击 : 敌人行动.待机; 敌.最后目标 = 玩家;
        if (地图.生存大图) 敌.突袭冷却 = (float)天帝数值.取("map.arena.charge_period") * (float)(.25 + (敌.序号 % 8) / 8.0);
        return true;
    }
    int 选择本批敌人(int 在场)
    {
        int 末=敌人数据.Count-1,scan=(int)天帝数值.取("map.arena.pacing.candidate_scan");
        int 角色=(生存增援轮-1)%3==0?0:(生存增援轮-1)%3==1?1:2;
        bool 辅助=false;foreach(var e in 敌人数据)if(e.存活&&天帝敌种配置.角色(e.物种)>=3&&e.物种!=14){辅助=true;break;}
        if(在场>=4&&!辅助)角色=3;
        int fallback=-1;
        for(int i=生存队列索引;i<末&&i<生存队列索引+scan;i++)
        {
            if(敌人数据[i].已生成)continue;if(fallback<0)fallback=i;
            int r=天帝敌种配置.角色(敌人数据[i].物种);
            if(r==角色||角色==3&&r==4)return i;
        }
        return fallback;
    }
    void 推进生存(float 秒)
    {
        波次等待 -= 秒; 刷新重试剩余 -= 秒;
        if (波次等待 <= 0 && 生存待刷名额 == 0 && 生存队列索引 < 敌人数据.Count - 1)
        {
            生存增援轮++;当前波次=生存增援轮;
            生存待刷名额 = 生存批次数量;
            波次等待 = 生存刷新间隔+(生存增援轮%(int)天帝数值.取("map.arena.pacing.breath_every")==0?(float)天帝数值.取("map.arena.pacing.breath_seconds"):0);
        }
        if(生存待刷名额==0&&场上敌人数量<=生存场上上限*天帝数值.取("map.arena.pacing.low_active_fraction"))
            波次等待=Mathf.Min(波次等待,(float)天帝数值.取("map.arena.pacing.low_active_wait"));
        if (刷新重试剩余 > 0) return;
        刷新重试剩余 = (float)天帝数值.取("map.arena.retry_interval");
        int 在场 = 场上敌人数量;
        if (!BOSS已出现 && 敌人损伤比例 >= 天帝数值.取("map.boss_trigger") && 在场 < 生存场上上限 + 1)
        { BOSS已出现 = 生成(敌人数据[敌人数据.Count - 1], true); if (BOSS已出现) 在场++; }
        int 容量 = 生存场上上限 + (BOSS已出现 && 敌人数据[敌人数据.Count - 1].存活 ? 1 : 0);
        while (生存待刷名额 > 0 && 生存队列索引 < 敌人数据.Count - 1 && 在场 < 容量)
        {
            int index=选择本批敌人(在场);if(index<0||!生成(敌人数据[index], true))break;
            生存待刷名额--; 在场++;
            while(生存队列索引<敌人数据.Count-1&&敌人数据[生存队列索引].已生成)生存队列索引++;
        }
        if (生存队列索引 >= 敌人数据.Count - 1) 生存待刷名额 = 0;
    }
    void 生成下一组小怪()
    {
        int 数量 = 0;
        int 上限 = 天帝地图挑战.小怪数量(当前波次);
        foreach (var 敌 in 敌人数据)
        {
            if (敌.已生成 || 敌.登场波次 != 0 || 敌.布点.级别 != 战斗敌人级别.普通) continue;
            敌.登场波次 = 当前波次; // 即使选点失败也保留本波名额，不丢怪、不提前跳波。
            生成(敌); 数量++; if (数量 >= 上限) break;
        }
        本波精英阶段 = false;
    }
    void 推进波次(float 秒)
    {
        if (玩家死亡 || 地图.横向区域) return;
        if (地图.生存大图) { 推进生存(秒); return; }
        刷新重试剩余 -= 秒;
        bool 可重试 = 刷新重试剩余 <= 0;
        if (可重试)
        {
            刷新重试剩余 = (float)天帝数值.取("map.spawn_retry_interval");
            foreach (var 敌 in 敌人数据)
            {
                if (场上敌人数量 >= 天帝地图挑战.场上上限) break;
                if (!敌.已生成 && 敌.登场波次 > 0 && 敌.登场波次 <= 当前波次 && 敌.布点.级别 == 战斗敌人级别.普通) 生成(敌);
            }
        }
        // 只统计非BOSS的实际扣血，过量伤害不推进进度；BOSS不占自己的召唤门槛。
        if (可重试 && !BOSS已出现 && 敌人损伤比例 >= 天帝数值.取("map.boss_trigger"))
        {
            foreach (var 敌 in 敌人数据) if (!敌.已生成 && 敌.布点.级别 == 战斗敌人级别.王级) { BOSS已出现 = 生成(敌); break; }
        }
        foreach (var 敌 in 敌人数据)
            if (!敌.已生成 && 敌.登场波次 == 当前波次 && 敌.布点.级别 == 战斗敌人级别.普通) { 波次等待 = 0; return; }
        波次等待 += 秒;
        int 本波普通 = 0; bool 强敌存活 = false;
        foreach (var 敌 in 敌人数据)
        {
            if (!敌.存活 || 敌.登场波次 != 当前波次 || 敌.布点.级别 == 战斗敌人级别.王级) continue;
            if (敌.布点.级别 == 战斗敌人级别.普通) 本波普通++; else 强敌存活 = true;
        }
        if (!本波精英阶段)
        {
            if (波次等待 < 1.2f || (本波普通 > 天帝地图挑战.小怪数量(当前波次) / 2 && 波次等待 < 天帝数值.取("map.strong_wait"))) return;
            if (!可重试 || 场上敌人数量 >= 天帝地图挑战.场上上限) return;
            var 类 = 当前波次 < 5 ? 战斗敌人级别.精英 : 战斗敌人级别.头目;
            foreach (var 敌 in 敌人数据)
                if (!敌.已生成 && 敌.布点.级别 == 类)
                { if (生成(敌)) { 本波精英阶段 = true; 波次等待 = 0; } break; }
            return;
        }
        if (当前波次 >= 天帝地图挑战.波数 || 波次等待 < 天帝数值.取("map.advance_wait")) return;
        if ((强敌存活 || 本波普通 > 2) && 波次等待 < 天帝数值.取("map.slow_intervals." + (当前波次 - 1))) return;
        if (场上敌人数量 + 天帝地图挑战.小怪数量(当前波次 + 1) > 天帝地图挑战.场上上限) return;
        当前波次++; 波次等待 = 0; 生成下一组小怪();
    }
    public void 推进(Vector2 玩家位置, float 秒)
    {
        if (float.IsNaN(秒) || float.IsInfinity(秒) || 秒 <= 0 || !地图.可站立(玩家位置)) return;
        开始战斗帧();
        推进细步(玩家位置, 秒);
    }
    internal void 开始战斗帧() => 敌人AI.开始帧();
    // 场景先开启帧预算，再逐步同步闪避位置；一个渲染帧内的子步共享寻路额度和统计。
    internal void 推进细步(Vector2 玩家位置, float 秒)
    {
        if (float.IsNaN(秒) || float.IsInfinity(秒) || 秒 <= 0 || !地图.可站立(玩家位置)) return;
        玩家 = 玩家位置;
        推进波次(Mathf.Min(秒, .25f));
        // 有界细步长：暂停恢复不累计攻击，低帧率弹体也不穿墙。
        float 剩余 = Mathf.Min(秒, .25f);
        while (剩余 > .00001f) { float 步 = Mathf.Min(.025f, 剩余); 一步(步); 剩余 -= 步; }
        掉落.推进吸附(玩家, Mathf.Min(秒, .25f));
        通货掉落.推进吸附(玩家, Mathf.Min(秒, .25f));
        灵石掉落.推进吸附(玩家, Mathf.Min(秒, .25f));
    }
    void 一步(float 秒)
    {
        for (int i = 电弧数据.Count - 1; i >= 0; i--) { 电弧数据[i].剩余秒 -= 秒; if (电弧数据[i].剩余秒 <= 0) 电弧数据.RemoveAt(i); }
        for (int i = 光圈数据.Count - 1; i >= 0; i--) { 光圈数据[i].剩余秒 -= 秒; if (光圈数据[i].剩余秒 <= 0) 光圈数据.RemoveAt(i); }
        foreach (var 敌 in 敌人数据)
        { 敌.闪白秒 = Mathf.Max(0, 敌.闪白秒 - 秒); 敌.束缚剩余秒 = Mathf.Max(0, 敌.束缚剩余秒 - 秒); if (!敌.存活) 敌.死亡秒 += 秒; }
        if (玩家死亡) { 灵矢数据.Clear(); 待加灵矢.Clear(); 清理扩展功能(); 战术.清理(); 回响剩余 = 前摇剩余 = -1; return; }
        战斗时钟 += 秒; // 伤害以本细步结束时刻结算，0.100秒边界不延长无敌窗口。
        推进扩展地面(秒);
        推进形态演出(秒);
        推进特性战斗(秒);
        战术.推进效果(玩家, 秒);
        敌人AI.推进(玩家, 秒);
        if (玩家死亡) return;
        推进主动计时(秒);
        int 原数量 = 灵矢数据.Count;
        for (int i = 原数量 - 1; i >= 0; i--) if (!灵矢一步(灵矢数据[i], 秒)) { var 矢 = 灵矢数据[i]; 矢.释放.根目标.Remove(矢.根目标); 灵矢数据.RemoveAt(i); }
        处理连锁攻击();
        // 192只限制绘制，逻辑弹体有固定飞行距离；视觉预算不能吞掉根弹或衍生伤害。
        灵矢数据.AddRange(待加灵矢);
        待加灵矢.Clear();
    }
    void 处理连锁攻击()
    {
        // 同一更新内结算全部直接连锁，不花飞行时间；沿用命中历史、功能深度与生成预算。
        bool 保留(战斗灵矢 矢)
        { while (矢.自动连锁) if (!灵矢一步(矢, 0)) return false; return true; }
        for (int i = 灵矢数据.Count - 1; i >= 0; i--)
            if (灵矢数据[i].自动连锁 && !保留(灵矢数据[i])) 灵矢数据.RemoveAt(i);
        for (int i = 0; i < 待加灵矢.Count;)
            if (待加灵矢[i].自动连锁 && !保留(待加灵矢[i])) 待加灵矢.RemoveAt(i); else i++;
    }
    bool 发射(普攻参数 参数)=>发射根(参数,true);
    bool 发射根(普攻参数 参数,bool root, Vector2? 主动方向 = null)
    {
        if (参数 == null || !参数.已激活) return false;
        int 目标 = 主动方向.HasValue ? -1 : 找目标(玩家, 普攻参数.索敌距离, null);
        if (!主动方向.HasValue && 目标 < 0) return false;
        Vector2 向 = 主动方向 ?? (敌人数据[目标].位置 - 玩家).normalized;
        double 采样 = 战斗随机.NextDouble(); bool 暴击 = 采样 < 参数.暴击率;
        var 释放 = new 战斗释放记录 { 编号 = ++释放序号, 暴击倍率 = 暴击 ? 参数.暴击倍率 : 1, 暴击采样 = 采样,根普攻=root, 手动瞄准=主动方向.HasValue };
        if (暴击) 暴击释放次数++;
        if (参数.顺序计划 != null)
        {
            int 根数 = 天帝天赋效果.射击数量(道纹.天赋, 1, true);
            int 前 = 释放.顺序生成数;
            发射执行段(参数.顺序计划.起点, 玩家, 向, 目标, 释放, 1, new HashSet<int>(), 0, 灵矢数据, false, 根数);
            当前通路 = 参数.通路; 射击释放?.Invoke(向);
            return 释放.顺序生成数 > 前;
        }
        var 已分配 = new HashSet<int>();
        for (int i = 0; i < 参数.数量; i++)
        {
            int 独立目标 = 主动方向.HasValue ? -1 : 找目标(玩家, 普攻参数.索敌距离, 已分配);
            if (独立目标 >= 0) { 目标 = 独立目标; 已分配.Add(目标); 释放.根目标.Add(目标); }
            Vector2 瞄准 = 主动方向 ?? (敌人数据[目标].位置 - 玩家).normalized;
            float 角 = 独立目标 >= 0 ? 0 : (i - (参数.数量 - 1) * .5f) * (float)天帝数值.取("shape.root_spread_degrees");
            灵矢数据.Add(new 战斗灵矢 { 位置 = 玩家, 方向 = 转向(瞄准, 角), 目标 = 目标, 根目标 = 独立目标, 伤害 = 参数.伤害, 释放 = 释放,
                剩余距离 = 普攻参数.飞行距离, 剩余连锁 = 参数.连锁, 参数 = 参数, 根普攻弹 = root });
        }
        当前通路 = 参数.通路; 射击释放?.Invoke(向);
        return true;
    }
    int 找目标(Vector2 起, float 范围, HashSet<int> 排除)
    {
        int 最佳 = -1; float 最近 = 范围 * 范围;
        for (int i = 0; i < 敌人数据.Count; i++)
        {
            var 敌 = 敌人数据[i]; float 距 = (敌.位置 - 起).sqrMagnitude;
            if (!敌.存活 || 距 > 最近 || (排除 != null && 排除.Contains(i)) || !寻路.无遮挡(起, 敌.位置)) continue;
            最近 = 距; 最佳 = i;
        }
        return 最佳;
    }
    bool 灵矢一步(战斗灵矢 矢, float 秒)
    {
        if (天帝顺序道纹.攻击形态(矢.执行段?.功能 ?? 道纹功能.旧版)) return 推进攻击形态(矢, 秒);
        if (推进扩展等待(矢, 秒, out bool 等待保留)) return 等待保留;
        // 只在发射/命中后产生新阶段时瞄准；飞行中方向固定，移动敌人可以躲开。
        bool 穿透阶段 = 矢.执行段?.功能 == 道纹功能.穿透 || 矢.执行段?.功能 == 道纹功能.折返 || 矢.执行段?.功能 == 道纹功能.弹墙;
        float 剩余秒 = 0;
        int 命中 = -1;
        if (矢.自动连锁)
        {
            矢.自动连锁 = false;
            if (矢.目标 < 0 || 矢.目标 >= 敌人数据.Count || !敌人数据[矢.目标].存活 || 矢.命中过.Contains(矢.目标)) return false;
            命中 = 矢.目标; 留电弧(矢.位置, 敌人数据[命中].位置, true); 矢.位置 = 敌人数据[命中].位置;
        }
        else
        {
            float 距 = Mathf.Min(矢.参数.弹速 * 秒, 矢.剩余距离);
            Vector2 旧 = 矢.位置, 新 = 扩展飞行位置(矢, 距, 秒);
            if (!寻路.无遮挡(矢.位置, 新)) return 扩展撞墙(矢, 旧, 新);
            float 最早 = float.MaxValue;
            for (int i = 0; i < 敌人数据.Count; i++)
            {
                var 敌 = 敌人数据[i]; if (!敌.存活 || 矢.命中过.Contains(i) || (矢.执行段 == null && 矢.释放.根目标.Contains(i) && (矢.子矢 || 矢.已分裂 || i != 矢.根目标))) continue;
                Vector2 线 = 新 - 矢.位置;
                float t = 线.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(敌.位置 - 矢.位置, 线) / 线.sqrMagnitude) : 0;
                float 半径 = (敌.布点.级别 == 战斗敌人级别.王级 ? 1.25f : .6f) + 矢.参数.弹体半径;
                if ((敌.位置 - Vector2.Lerp(矢.位置, 新, t)).sqrMagnitude <= 半径 * 半径 && t < 最早) { 命中 = i; 最早 = t; }
            }
            矢.位置 = 新; 矢.剩余距离 -= 距;
            if (命中 < 0) { 登记扩展飞行(矢, 旧); return 矢.剩余距离 > 0 || 扩展飞行结束(矢); }
            if (穿透阶段)
            {
                float 实际距离 = 距 * 最早;
                矢.位置 = Vector2.Lerp(旧, 新, 最早); 矢.剩余距离 += 距 - 实际距离;
                剩余秒 = Mathf.Max(0, 秒 - 实际距离 / Mathf.Max(.001f, 矢.参数.弹速));
            }
            登记扩展飞行(矢, 旧);
        }
        var 被击 = 敌人数据[命中]; Vector2 命中点 = 被击.位置;
        矢.释放.根目标.Remove(矢.根目标); 矢.根目标 = -1;
        矢.命中过.Add(命中); 命中伤害(被击, 矢, 矢.形态倍率);
        if (矢.执行段 != null)
        {
            bool 继续 = 顺序命中(矢, 命中, 命中点);
            // 扫描本帧剩余路程，保证高弹速连续穿透不会跳过同帧的第二名敌人。
            return 继续 && 穿透阶段 && !矢.自动连锁 && 剩余秒 > 0 ? 灵矢一步(矢, 剩余秒) : 继续;
        }
        if (矢.子矢) return false;
        if (!矢.已分裂)
        {
            矢.已分裂 = true;
            if (矢.参数.溅射半径 > 0)
            {
                光圈数据.Add(new 战斗光圈 { 位置 = 命中点, 半径 = 矢.参数.溅射半径 });
                int 次级 = 0;
                for (int 索引 = 0; 索引 < 敌人数据.Count && 次级 < 天帝数值.取("shape.splash_targets_max"); 索引++)
                {
                    var 邻 = 敌人数据[索引];
                    if (!邻.存活 || 矢.命中过.Contains(索引) || 矢.释放.根目标.Contains(索引) || Vector2.Distance(邻.位置, 命中点) > 矢.参数.溅射半径 || !寻路.无遮挡(命中点, 邻.位置)) continue;
                    矢.命中过.Add(索引); 命中伤害(邻, 矢, 天帝数值.取("shape.splash_factor")); 次级++; 溅射命中数++;
                }
            }
            int 数 = 矢.参数.分裂;
            var 子目标 = new HashSet<int>(矢.命中过);
            子目标.UnionWith(矢.释放.根目标);
            for (int n = 0; n < 数; n++)
            {
                int 下个 = 找目标(命中点, (float)天帝数值.取("shape.split_child_life"), 子目标);
                if (下个 >= 0) 子目标.Add(下个);
                var 子 = new 战斗灵矢 { 位置 = 命中点, 方向 = 转向(矢.方向, (n - (数 - 1) * .5f) * 35),
                    伤害 = 矢.参数.伤害 * (float)天帝数值.取("shape.split_factor"), 子矢 = true, 参数 = 矢.参数, 目标 = 下个,
                    剩余距离 = (float)天帝数值.取("shape.split_child_life"), 释放 = 矢.释放, 形态倍率 = 天帝数值.取("shape.split_factor") };
                if (下个 >= 0) 子.方向 = (敌人数据[下个].位置 - 命中点).normalized;
                待加灵矢.Add(子); 分裂生成数++;
            }
        }
        if (矢.剩余连锁 > 0)
        {
            var 排除 = new HashSet<int>(矢.命中过); 排除.UnionWith(矢.释放.根目标);
            int 下个 = 找目标(命中点, (float)天帝数值.取("shape.chain_range"), 排除);
            if (下个 >= 0)
            {
                矢.剩余连锁--; 矢.形态倍率 *= 天帝数值.取("shape.chain_factor"); 矢.伤害 = 矢.参数.伤害 * (float)矢.形态倍率; 矢.目标 = 下个; 矢.位置 = 命中点;
                矢.方向 = (敌人数据[下个].位置 - 命中点).normalized; 矢.自动连锁 = true; 连锁发生数++; return true;
            }
        }
        return false;
    }
    public bool 伤害敌人(战斗敌人 敌, float 原伤害)
        => 伤害敌人(敌: 敌, 包: new 战斗伤害包(原伤害, 0, 主角.等级));
    void 命中伤害(战斗敌人 敌, 战斗灵矢 矢, double 形态)
    {
        伤害敌人(敌, new 战斗伤害包(矢.参数.普通伤害, 矢.参数.五行额外伤害, 矢.参数.等级, 天帝数值.取("player.skill_multiplier"), 形态 * 扩展蓄势倍率(矢), 矢.参数.天赋倍率, 矢.实际暴击倍率, 矢.参数.五行来源), 矢.方向);
        if(矢.根普攻弹 && !矢.子矢)特性根命中(敌,矢.参数.通路);
    }
    public bool 伤害敌人(战斗敌人 敌, 战斗伤害包 包, Vector2? 入射方向 = null)
    {
        if (敌 == null || !敌人数据.Contains(敌) || !敌.存活 || 玩家死亡) return false;
        float 伤 = (float)天帝数值.结算伤害(包, 敌.等级, 敌.防御, 敌.抗性);
        if(敌.特性易伤秒>0)伤*=1+敌.特性易伤;
        if (伤 <= 0) return false;
        敌人AI.受击警戒(敌, 玩家);
        战术.受伤(敌);
        float 伤前血 = 敌.血量;
        float 盾伤 = Mathf.Min(伤, 敌.护盾量); 敌.护盾量 -= 盾伤;
        if (盾伤 > 0 && 敌.护盾量 <= 0) 敌.盾禁用 = 天帝敌种配置.取("support.shield_lockout");
        敌.血量 = Mathf.Max(0, 敌.血量 - (伤 - 盾伤));
        if (敌.布点.级别 != 战斗敌人级别.王级)
        { float 深 = Mathf.Max(敌.最深伤损, 敌.最大血量 - 敌.血量); 累计敌人损伤 += 深 - 敌.最深伤损; 敌.最深伤损 = 深; }
        敌.闪白秒 = 天帝受击表现.取("flash_seconds"); 伤害反馈?.Invoke(敌.位置, 伤, false);
        伤害分项反馈?.Invoke(敌.位置, 天帝数值.拆分伤害(包, 伤, 敌.防御, 敌.抗性), false);
        受击表现反馈?.Invoke(new 战斗受击反馈(敌,敌.位置,入射方向 ?? 敌.位置-玩家,
            天帝数值.拆分伤害(包,伤,敌.防御,敌.抗性),伤前血-敌.血量,盾伤,false,包.暴击倍率>1,
            盾伤>0&&敌.护盾量<=0,!演示模式&&敌.血量<=0));
        if (演示模式) { if (敌.血量 <= 0) 敌.血量 = 敌.最大血量; return true; }
        if (!敌.存活)
        {
            敌.行动 = 敌人行动.死亡; 敌.路线.Clear(); 击败数++;
            int 经验 = 天帝数值.击杀经验(主角.等级, 敌.等级, 敌.布点.级别);
            本局经验 += 经验; 本局升级次数 += 道纹.获得经验(经验);
            主角.阶段恢复(天帝数值.敌参数(敌.布点.级别, "kill_heal"));
            掉落.敌人死亡(敌); 通货掉落.敌人死亡(敌); 灵石掉落.敌人死亡(敌); 敌人死亡?.Invoke(敌);
        }
        return true;
    }
    public bool 伤害玩家(float 原伤害)
        => 结算玩家受伤(new 战斗伤害包(原伤害, 0, 主角.等级), false);
    public bool 伤害玩家(战斗伤害包 包) => 结算玩家受伤(包, false);
    bool 伤害玩家(战斗敌人 敌, double 倍率, bool 范围技)
        => 特性目标受击(new 战斗伤害包(敌.攻击力, 0, 敌.等级, 倍率,1,1,1,default,敌),特性诱饵目标(敌,玩家), !范围技);
    bool 结算玩家受伤(战斗伤害包 包, bool 可闪避)
    {
        if (玩家死亡 || 闪避无敌中) return false;
        float 伤 = (float)天帝数值.结算伤害(包, 主角.等级, 主角.防御, 主角.抗性);
        if(包.来源!=null)伤*=包.来源.特性伤害倍率;
        if (伤 <= 0 || (可闪避 && 战斗随机.NextDouble() < 主角.闪避率)) return false;
        float 伤前血=主角.当前血量,伤前盾=主角.当前灵气护盾+特性临时护盾;
        伤=特性受伤前(伤,可闪避,包.来源);
        float 盾伤 = Mathf.Min(主角.当前灵气护盾, 伤);
        主角.设置当前资源(主角.当前血量 - (伤 - 盾伤), 主角.当前灵力, 主角.当前灵气护盾 - 盾伤);
        特性受伤后(Mathf.Max(0,伤前血-主角.当前血量),Mathf.Max(0,伤前盾-主角.当前灵气护盾-特性临时护盾),伤前盾>0 && 主角.当前灵气护盾+特性临时护盾<=0);
        伤害反馈?.Invoke(玩家, 伤, true);
        伤害分项反馈?.Invoke(玩家, 天帝数值.拆分伤害(包, 伤, 主角.防御, 主角.抗性), true);
        float 血损=Mathf.Max(0,伤前血-主角.当前血量),盾损=Mathf.Max(0,伤前盾-主角.当前灵气护盾-特性临时护盾);
        if(血损+盾损>0)受击表现反馈?.Invoke(new 战斗受击反馈(null,玩家,包.来源!=null?玩家-包.来源.位置:Vector2.up,
            天帝数值.拆分伤害(包,血损+盾损,主角.防御,主角.抗性),血损,盾损,true,包.暴击倍率>1,
            伤前盾>0&&主角.当前灵气护盾+特性临时护盾<=0,玩家死亡));
        return true;
    }
    static bool 有效伤害(float 伤) => 伤 > 0 && !float.IsNaN(伤) && !float.IsInfinity(伤);
    static Vector2 转向(Vector2 向, float 度)
    { float a = 度 * Mathf.Deg2Rad; return new Vector2(向.x * Mathf.Cos(a) - 向.y * Mathf.Sin(a), 向.x * Mathf.Sin(a) + 向.y * Mathf.Cos(a)); }
}

