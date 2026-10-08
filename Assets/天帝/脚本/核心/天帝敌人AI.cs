using System;
using System.Collections.Generic;
using UnityEngine;

// 本局集中驱动：感知5Hz、分离10Hz、每次推进最多4次BFS。使用缓存路线等待预算，避免同时重算。
public sealed class 天帝敌人AI
{
    readonly 天帝战斗地图 地图;
    readonly 天帝战斗寻路 寻路;
    readonly List<战斗敌人> 敌人;
    readonly Func<战斗敌人, double, bool, bool> 伤害玩家;
    readonly List<战斗光圈> 光圈;
    readonly bool 持续追击;
    int 预算, 轮转;
    Vector2 上次玩家;
    float 采样秒;
    bool 已采样;
    readonly Dictionary<Vector2Int, List<战斗敌人>> 空间桶 = new Dictionary<Vector2Int, List<战斗敌人>>();
    readonly Dictionary<int, List<战斗敌人>> 队员 = new Dictionary<int, List<战斗敌人>>();
    public int 本次分离候选数 { get; private set; }
    static Vector2Int 桶格(Vector2 位) => new Vector2Int(Mathf.FloorToInt(位.x / 2), Mathf.FloorToInt(位.y / 2));
    public 天帝敌人战术 战术 { get; set; }
    public Func<战斗敌人,Vector2,Vector2> 特性目标 { get; set; }
    public Func<战斗敌人,Vector2,float,Vector2> 特性阻挡 { get; set; }
    public Func<Vector2,Vector2,bool> 特性挡路 { get; set; }
    public int 本次寻路次数 { get; private set; }
    public 天帝敌人AI(天帝战斗地图 地图, 天帝战斗寻路 寻路, List<战斗敌人> 敌人,
        Func<float, bool> 伤害玩家, List<战斗光圈> 光圈, bool 持续追击 = false)
        : this(地图, 寻路, 敌人, (敌, 倍率, 范围) => 伤害玩家((float)(敌.攻击力 * 倍率)), 光圈, 持续追击) { }
    public 天帝敌人AI(天帝战斗地图 地图, 天帝战斗寻路 寻路, List<战斗敌人> 敌人,
        Func<战斗敌人, double, bool, bool> 伤害玩家, List<战斗光圈> 光圈, bool 持续追击 = false)
    {
        this.地图 = 地图; this.寻路 = 寻路; this.敌人 = 敌人; this.伤害玩家 = 伤害玩家; this.光圈 = 光圈;
        this.持续追击 = 持续追击;
        var 布点队号 = new Dictionary<战斗敌人布点, int>();
        for (int 队 = 0; 队 < 地图.小队.Count; 队++) foreach (var 点 in 地图.小队[队].成员) 布点队号[点] = 队;
        for (int i = 0; i < 敌人.Count; i++)
        {
            var 敌 = 敌人[i]; 敌.序号 = i; 敌.感知冷却 = i % 5 * .04f; 敌.分离冷却 = i % 4 * .025f;
            if (布点队号.TryGetValue(敌.布点, out int 队号))
            {
                敌.小队编号 = 队号;
                if (!队员.TryGetValue(队号, out var 列)) 队员.Add(队号, 列 = new List<战斗敌人>());
                列.Add(敌);
            }
        }
    }
    public void 开始帧() { 预算 = 4; 本次寻路次数 = 0; }
    public void 推进(Vector2 玩家, float 秒)
    {
        if (敌人.Count == 0) return;
        if (地图.生存大图 && 战术 != null)
        {
            if (!已采样) { 已采样 = true; 上次玩家 = 玩家; }
            采样秒 += 秒;
            if (采样秒 >= 天帝数值.取("map.arena.velocity_sample"))
            { 战术.玩家速度 = (玩家 - 上次玩家) / 采样秒; 上次玩家 = 玩家; 采样秒 = 0; }
        }
        本次分离候选数 = 0;
        foreach (var 桶 in 空间桶.Values) 桶.Clear();
        foreach (var 敌 in 敌人) if (敌.存活)
        {
            var 格 = 桶格(敌.位置);
            if (!空间桶.TryGetValue(格, out var 桶)) 空间桶.Add(格, 桶 = new List<战斗敌人>(4));
            桶.Add(敌);
        }
        战术?.开始敌人推进();
        int 起 = 轮转++ % 敌人.Count;
        for (int i = 0; i < 敌人.Count; i++)
        { var 敌 = 敌人[(起 + i) % 敌人.Count]; if (敌.存活) 一步(敌, 玩家, 秒); }
    }
    void 警戒(战斗敌人 敌, Vector2 玩家)
    {
        敌.最后目标 = 玩家; 敌.失视秒 = 0; 敌.搜索秒 = 0;
        if (敌.行动 != 敌人行动.蓄力 && 敌.行动 != 敌人行动.后摇) 敌.行动 = 敌人行动.追击;
        敌.再寻路 = 0;
    }
    public void 受击警戒(战斗敌人 敌, Vector2 玩家)
    {
        警戒(敌, 玩家); 敌.警戒禁用秒 = 0;
        呼叫小队(敌, 玩家);
    }
    void 呼叫小队(战斗敌人 呼叫者, Vector2 玩家)
    {
        if (呼叫者.小队编号 < 0) return;
        if (!队员.TryGetValue(呼叫者.小队编号, out var 列)) return;
        foreach (var 邻 in 列)
            if (邻 != 呼叫者 && 邻.存活 && 邻.小队编号 == 呼叫者.小队编号 &&
                邻.行动 == 敌人行动.待机 && 邻.警戒禁用秒 <= 0 && (邻.位置 - 呼叫者.位置).sqrMagnitude <= 144)
                警戒(邻, 玩家); // 同队12米内听到警报，接收者不再广播。
    }
    void 一步(战斗敌人 敌, Vector2 玩家, float 秒)
    {
        玩家=特性目标?.Invoke(敌,玩家)??玩家;
        if (敌.登场剩余秒 > 0) { 敌.登场剩余秒 = Mathf.Max(0, 敌.登场剩余秒 - 秒); return; }
        bool 王 = 敌.布点.级别 == 战斗敌人级别.王级;
        bool 房 = 地图.王房范围.Contains(地图.所在格(玩家));
        float 距 = Vector2.Distance(敌.位置, 玩家);
        敌.冷却 = Mathf.Max(0, 敌.冷却 - 秒); 敌.再寻路 -= 秒;
        敌.警戒禁用秒 = Mathf.Max(0, 敌.警戒禁用秒 - 秒); 敌.感知冷却 -= 秒;
        if (敌.感知冷却 <= 0)
        {
            敌.感知冷却 = .2f;
            float 范围 = 敌.行动 == 敌人行动.待机 ? (王 ? 18 : 10) : (王 ? 22 : 14);
            敌.看见玩家 = 持续追击 || (距 <= 范围 && (地图.固定图片布局 || 地图.横向区域 && !王 || 房 == 王) && 寻路.无遮挡(敌.位置, 玩家));
            if (敌.看见玩家 && 敌.行动 != 敌人行动.归巢 && 敌.警戒禁用秒 <= 0)
            {
                bool 刚发现 = 敌.行动 == 敌人行动.待机;
                敌.最后目标 = 玩家; 敌.失视秒 = 0;
                if (刚发现 || 敌.行动 == 敌人行动.搜索) 警戒(敌, 玩家);
                if (刚发现) 呼叫小队(敌, 玩家);
            }
        }
        if (敌.行动 == 敌人行动.待机) return;
        if (!地图.生存大图 && 地图.横向区域 && 敌.行动 != 敌人行动.归巢 &&
            (王 ? !房 : 房 || Vector2.Distance(敌.位置, 敌.布点.位置) > 天帝数值.取("map.long_region.camp_leash") || 距 > 天帝数值.取("map.long_region.player_leash"))) 归巢(敌);
        if (!持续追击 && !地图.固定图片布局 && 敌.行动 != 敌人行动.归巢 && ((王 && !房) || (!王 && (房 || (敌.位置 - 敌.布点.位置).sqrMagnitude > 784 || 距 > 26)))) 归巢(敌);
        if (敌.行动 == 敌人行动.归巢)
        {
            if ((敌.位置 - 敌.布点.位置).sqrMagnitude <= .04f)
            { 敌.位置 = 敌.布点.位置; 敌.行动 = 敌人行动.待机; 敌.路线.Clear(); 敌.警戒禁用秒 = 1.5f; 敌.看见玩家 = false; }
            else 移动(敌, 敌.布点.位置, 秒);
            return;
        }
        if (战术 != null) { 战术.推进敌(敌, 玩家, 秒, 移动); return; }
        if (敌.行动 == 敌人行动.蓄力)
        {
            敌.蓄力 -= 秒;
            if (敌.蓄力 <= 0)
            {
                // 突进沿蓄力时锁定的落点前进，地图移动负责挡墙，玩家仍能横移躲开。
                if (敌.本次突进)
                {
                    var 差 = 敌.攻击落点 - 敌.位置;
                    敌.位置 = 地图.移动(敌.位置, 差.normalized, Mathf.Min(差.magnitude, 敌.本次攻击范围));
                }
                Vector2 中心 = 敌.本次范围技 ? 敌.位置 : 敌.本次突进 ? 敌.位置 : 敌.攻击落点;
                float 半径 = 敌.本次范围技 ? 敌.本次攻击范围 : 敌.攻击范围;
                if ((中心 - 玩家).sqrMagnitude <= 半径 * 半径 && 寻路.无遮挡(敌.位置, 玩家)) 伤害玩家(敌, 敌.本次技能倍率, 敌.本次范围技);
                光圈.Add(new 战斗光圈 { 位置 = 中心, 半径 = 半径, 敌方 = true });
                敌.行动 = 敌人行动.后摇; 敌.后摇秒 = (float)天帝数值.敌参数(敌.布点.级别, "recovery");
                敌.已攻击次数++;
            }
            return;
        }
        if (敌.行动 == 敌人行动.后摇)
        { 敌.后摇秒 -= 秒; if (敌.后摇秒 <= 0) 敌.行动 = 敌人行动.追击; return; }
        if (!敌.看见玩家) 敌.失视秒 += 秒;
        if (敌.失视秒 >= 1.2f && 敌.行动 == 敌人行动.追击) { 敌.行动 = 敌人行动.搜索; 敌.搜索秒 = 0; }
        if (敌.行动 == 敌人行动.搜索)
        {
            if ((敌.位置 - 敌.最后目标).sqrMagnitude < 1) 敌.搜索秒 += 秒;
            if (敌.搜索秒 >= 3 || 敌.失视秒 >= 12) { 归巢(敌); return; }
            移动(敌, 敌.最后目标, 秒); return;
        }
        int 技能序 = 敌.布点.级别 == 战斗敌人级别.普通 ? 0 : 敌.已攻击次数 % 3;
        double 倍率 = 天帝数值.敌参数(敌.布点.级别, "skills." + 技能序);
        bool 特殊 = 倍率 > 1;
        float 攻击距离 = (特殊 ? (float)天帝数值.敌参数(敌.布点.级别, "skill_range") : 敌.攻击范围) - .1f;
        if (距 <= 攻击距离 && 寻路.无遮挡(敌.位置, 玩家))
        {
            if (敌.冷却 <= 0)
            {
                敌.行动 = 敌人行动.蓄力;
                敌.蓄力 = (float)天帝数值.敌参数(敌.布点.级别, 特殊 ? "skill_windup" : "windup");
                敌.冷却 = 敌.攻击间隔; // 含前后摇，按开始蓄力到下次开始蓄力计时。
                敌.本次技能倍率 = 倍率; 敌.本次攻击范围 = 攻击距离 + .1f;
                敌.本次范围技 = 天帝数值.敌参数(敌.布点.级别, "aoe." + 技能序) > 0;
                敌.本次突进 = 特殊 && !敌.本次范围技;
                敌.攻击落点 = 敌.本次范围技 ? 敌.位置 : 玩家;
            }
            return;
        }
        Vector2 终 = 敌.最后目标;
        // 接近后采用可站立的环形攻击位，防止所有敌人挤在玩家中心；远处仍走最短通路。
        if (敌.看见玩家 && 距 < 6)
        {
            float 角 = (敌.序号 % 8) * Mathf.PI / 4;
            Vector2 候选 = 玩家 + new Vector2(Mathf.Cos(角), Mathf.Sin(角)) * (攻击距离 - .35f);
            if (允许点(敌, 候选) && 寻路.无遮挡(玩家, 候选, .45f)) 终 = 候选;
        }
        移动(敌, 终, 秒);
    }
    void 归巢(战斗敌人 敌)
    { 敌.行动 = 敌人行动.归巢; 敌.蓄力 = 0; 敌.再寻路 = 0; 敌.路线.Clear(); 敌.看见玩家 = false; 敌.失视秒 = 0; }
    bool 允许点(战斗敌人 敌, Vector2 点)
    {
        if (!地图.可站立(点)) return false;
        if (持续追击 || 地图.固定图片布局) return true;
        return 地图.王房范围.Contains(地图.所在格(点)) == (敌.布点.级别 == 战斗敌人级别.王级);
    }
    bool 可直达(战斗敌人 敌, Vector2 点)
    {
        if (!允许点(敌, 点) || 特性挡路!=null && 特性挡路(敌.位置,点) || !寻路.无遮挡(敌.位置, 点, .45f)) return false;
        int 步 = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(敌.位置, 点) / 1));
        for (int i = 1; i < 步; i++) if (!允许点(敌, Vector2.Lerp(敌.位置, 点, (float)i / 步))) return false;
        return true;
    }
    void 移动(战斗敌人 敌, Vector2 终, float 秒)
    {
        if (!允许点(敌, 终)) { if (!持续追击) 归巢(敌); return; }
        Vector2 点 = 终;
        if (!可直达(敌, 终))
        {
            var 格 = 寻路.目标格(终);
            if (预算 > 0 && 敌.再寻路 <= 0 && (敌.路线索引 >= 敌.路线.Count || 敌.上个目标格 != 格 || 敌.卡住秒 > .6f))
            {
                bool 王 = 敌.布点.级别 == 战斗敌人级别.王级;
                预算--; 本次寻路次数++; 寻路.路径(敌.位置, 终, 敌.路线, !持续追击 && 王, !持续追击 && !王,特性挡路);
                敌.路线索引 = 0; 敌.上个目标格 = 格; 敌.再寻路 = .6f + 敌.序号 % 4 * .05f;
            }
            while (敌.路线索引 < 敌.路线.Count && (敌.路线[敌.路线索引] - 敌.位置).sqrMagnitude <= .04f) 敌.路线索引++;
            if (敌.路线索引 >= 敌.路线.Count) return;
            // 从缓存路线选择最远的可直达拐点，重寻路也不会先倒退到格中心。
            for (int i = 敌.路线.Count - 1; i > 敌.路线索引; i--)
                if (可直达(敌, 敌.路线[i])) { 敌.路线索引 = i; break; }
            点 = 敌.路线[敌.路线索引];
        }
        var 差 = 点 - 敌.位置; if (差.magnitude < .08f) return;
        敌.分离冷却 -= 秒;
        if (敌.分离冷却 <= 0)
        {
            敌.分离冷却 = .1f; 敌.分离方向 = Vector2.zero;
            var 格 = 桶格(敌.位置);
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            {
                if (!空间桶.TryGetValue(格 + new Vector2Int(x, y), out var 桶)) continue;
                foreach (var 邻 in 桶)
                {
                本次分离候选数++;
                if (邻 == 敌 || !邻.存活) continue;
                var 离 = 敌.位置 - 邻.位置; float 距 = 离.magnitude;
                if (距 >= .95f) continue;
                if (距 < .001f) 离 = 敌.序号 > 邻.序号 ? Vector2.right : Vector2.left;
                敌.分离方向 += 离.normalized * (.95f - 距);
                }
            }
            敌.分离方向 = Vector2.ClampMagnitude(敌.分离方向, .65f);
        }
        Vector2 向 = (差.normalized + 敌.分离方向).normalized;
        float 倍率 = 1;
        if (地图.生存大图)
        {
            int 角色 = 天帝敌种配置.角色(敌.物种);
            string 键 = 敌.物种 == 14 ? "boss" : 角色 == 1 ? "ranged" : 角色 == 2 ? "mobile" : 角色 >= 3 ? "support" : "melee";
            倍率 = (float)天帝数值.取("map.arena." + 键 + "_speed_factor");
        }
        float 步距=Mathf.Min(差.magnitude,(敌.束缚剩余秒>0?0:敌.战术移速*倍率*敌.特性移速倍率)*地图.地形移速(敌.位置)*秒);
        var 新 = 地图.移动(敌.位置, 向, 步距);
        // 障碍转角或拥挤时轮流试两侧，失败仍保持原位，不瞬移穿墙。
        if(敌.卡住秒>.35f&&(新-敌.位置).sqrMagnitude<.00001f&&步距>0)
        {
            var 侧=new Vector2(-向.y,向.x)*(敌.序号%2==0?1:-1);
            for(int i=0;i<2;i++){var 绕=地图.移动(敌.位置,(向*.35f+侧*(i==0?1:-1)).normalized,步距);if(允许点(敌,绕)&&(特性挡路==null||!特性挡路(敌.位置,绕))&&(绕-敌.位置).sqrMagnitude>.00001f){新=绕;break;}}
        }
        if(特性阻挡!=null)新=特性阻挡(敌,新,秒);
        if (!允许点(敌, 新)) 新 = 敌.位置;
        if ((新 - 敌.位置).sqrMagnitude < .00001f) { 敌.卡住秒 += 秒; if (敌.卡住秒 > .6f) 敌.再寻路 = 0; }
        else 敌.卡住秒 = Mathf.Max(0, 敌.卡住秒 - 秒);
        敌.位置 = 新;
    }
}
