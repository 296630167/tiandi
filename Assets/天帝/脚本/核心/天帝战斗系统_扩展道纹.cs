using System;
using System.Collections.Generic;
using UnityEngine;

// 所有延迟、轨迹和区域效果由同一战斗时钟推进，暂停时不另起协程。
public sealed partial class 天帝战斗系统
{
    public sealed class 道纹地面效果
    {
        public Vector2 位置;
        public float 半径, 剩余秒, 下次伤害;
        internal 战斗灵矢 来源;
    }
    readonly List<道纹地面效果> 道纹地面 = new List<道纹地面效果>(128);
    public IReadOnlyList<道纹地面效果> 功能地面效果 => 道纹地面;
    public int 扩展功能触发次数 { get; private set; }
    bool 演示模式;
    static float 扩(string 键) => 天帝顺序道纹.扩展数值(键);
    static int 运动层(战斗灵矢 矢, 道纹功能 f) => 天帝顺序道纹.运动层(矢.参数.运动功能, f);
    static bool 有运动(战斗灵矢 矢, 道纹功能 f) => 运动层(矢, f) > 0;
    static double 扩展蓄势倍率(战斗灵矢 矢) => Math.Min(扩("charge_max"), 1 + 矢.已飞距离 * 扩("charge_per_meter") * 运动层(矢, 道纹功能.蓄势));
    void 初始化扩展功能(战斗灵矢 矢)
    {
        矢.发射点 = 矢.位置;
        var f = 矢.执行段.功能;
        if (天帝顺序道纹.攻击形态(f)) 初始化攻击形态(矢);
        if (f == 道纹功能.折返) 矢.折返轨迹 = new List<Vector2>(128) { 矢.位置 };
        if (f == 道纹功能.延时) 设置功能等待(矢, 扩("delay_seconds"));
        if (f == 道纹功能.陨落)
        {
            int 目标 = 矢.目标 >= 0 ? 矢.目标 : 矢.释放.手动瞄准 ? -1 : 找目标(矢.位置, 普攻参数.索敌距离, 矢.命中过);
            if (目标 >= 0) 矢.位置 = 敌人数据[目标].位置;
            else 矢.位置 += 矢.方向 * Mathf.Min(扩("hop_distance"), 矢.剩余距离);
            矢.标记敌人 = 目标; 矢.自动连锁 = false; 设置功能等待(矢, 扩("fall_seconds"));
        }
        if (f == 道纹功能.跃迁) 矢.扩展阶段 = -1;
    }
    static void 设置功能等待(战斗灵矢 矢, float 秒)
    { 矢.等待总秒 = 矢.等待秒 = 秒; 矢.扩展阶段 = 1; 矢.自动连锁 = false; }
    bool 推进扩展等待(战斗灵矢 矢, float 秒, out bool 保留)
    {
        保留 = false; var f = 矢.执行段?.功能 ?? 道纹功能.旧版;
        if (f == 道纹功能.跃迁 && 矢.扩展阶段 == -1)
        {
            float 距 = Mathf.Min(扩("hop_distance"), 矢.剩余距离); Vector2 落 = 矢.位置;
            for (float d = 距; d > 0; d -= .1f) if (地图.可站立(矢.位置 + 矢.方向 * d)) { 落 = 矢.位置 + 矢.方向 * d; break; }
            留电弧(矢.位置, 落); 矢.位置 = 落; 扩展功能触发次数++; 保留 = 扩展续接(矢, -1, 落, true); return true;
        }
        if (矢.等待秒 <= 0) return false;
        if (f == 道纹功能.烙印 && 矢.标记敌人 >= 0 && 矢.标记敌人 < 敌人数据.Count && 敌人数据[矢.标记敌人].存活)
            矢.位置 = 敌人数据[矢.标记敌人].位置;
        矢.等待秒 = Mathf.Max(0, 矢.等待秒 - 秒);
        if (矢.等待秒 > 0) { 保留 = true; return true; }
        扩展功能触发次数++;
        if (f == 道纹功能.震荡) 扩展范围伤害(矢, 矢.位置, 扩("pulse_radius"), 扩("pulse_factor"), false);
        if (f == 道纹功能.烙印) 扩展范围伤害(矢, 矢.位置, 扩("mark_radius"), 扩("mark_factor"), false);
        if (f == 道纹功能.陨落) 扩展范围伤害(矢, 矢.位置, 扩("fall_radius"), 扩("fall_factor"), true);
        保留 = 扩展续接(矢, 矢.标记敌人, 矢.位置, f == 道纹功能.延时 || f == 道纹功能.停驻);
        return true;
    }
    Vector2 扩展飞行位置(战斗灵矢 矢, float 距, float 秒)
    {
        if (矢.执行段?.功能 == 道纹功能.折返 && 矢.扩展阶段 == 2)
        {
            Vector2 点 = 矢.位置;
            while (距 > .00001f && 矢.回程节点 >= 0)
            {
                var 差 = 矢.折返轨迹[矢.回程节点] - 点; float 长 = 差.magnitude;
                if (长 > .00001f) 矢.方向 = 差 / 长;
                if (长 <= 距) { 点 += 差; 距 -= 长; 矢.回程节点--; }
                else { 点 += 差.normalized * 距; 距 = 0; }
            }
            return 点;
        }
        if (有运动(矢, 道纹功能.回旋))
        {
            float 角 = Mathf.Min(扩("curve_degrees") * 秒 * 运动层(矢, 道纹功能.回旋), Mathf.Max(0, 扩("curve_max") - 矢.回旋累计角));
            矢.方向 = 转向(矢.方向, 角); 矢.回旋累计角 += 角;
        }
        Vector2 新 = 矢.位置 + 矢.方向 * 距;
        if (有运动(矢, 道纹功能.波动))
        {
            float 相 = 矢.波动相位 + 距 * Mathf.PI * 2 / 扩("wave_wavelength");
            新 += new Vector2(-矢.方向.y, 矢.方向.x) * ((Mathf.Sin(相) - Mathf.Sin(矢.波动相位)) * 扩("wave_amplitude") * 运动层(矢, 道纹功能.波动));
            矢.波动相位 = 相;
        }
        return 新;
    }
    void 登记扩展飞行(战斗灵矢 矢, Vector2 旧)
    {
        float 距 = Vector2.Distance(旧, 矢.位置); 矢.已飞距离 += 距;
        if (矢.折返轨迹 != null && 矢.扩展阶段 != 2 && 距 > .001f && 矢.折返轨迹.Count < 1024) 矢.折返轨迹.Add(矢.位置);
        if (!有运动(矢, 道纹功能.拖尾) || 距 <= 0) return;
        float 剩 = 扩("trail_spacing") - 矢.拖尾余距;
        while (剩 <= 距)
        {
            if (道纹地面.Count >= 扩("effects_alive") || 矢.释放.扩展效果数 >= 扩("effects_per_release")) break;
            var 位 = Vector2.Lerp(旧, 矢.位置, 剩 / 距);
            if (地图.可站立(位))
            {
                道纹地面.Add(new 道纹地面效果 { 位置 = 位, 半径 = 扩("trail_radius"), 剩余秒 = 扩("trail_seconds"), 下次伤害 = 扩("trail_tick"), 来源 = 扩展快照(矢) });
                矢.释放.扩展效果数++;
            }
            剩 += 扩("trail_spacing");
        }
        矢.拖尾余距 = (矢.拖尾余距 + 距) % 扩("trail_spacing");
    }
    bool 扩展飞行结束(战斗灵矢 矢)
    {
        if (矢.执行段?.功能 != 道纹功能.折返) return false;
        if (矢.扩展阶段 != 2)
        {
            矢.扩展阶段 = 2; 矢.方向 = -矢.方向; 矢.回程节点 = 矢.折返轨迹.Count - 2;
            float 距 = 0; for (int i = 1; i < 矢.折返轨迹.Count; i++) 距 += Vector2.Distance(矢.折返轨迹[i - 1], 矢.折返轨迹[i]);
            矢.剩余距离 = 距; return 距 > .001f;
        }
        扩展功能触发次数++; return 扩展续接(矢, -1, 矢.位置, true);
    }
    bool 扩展撞墙(战斗灵矢 矢, Vector2 旧, Vector2 新)
    {
        if (矢.执行段?.功能 != 道纹功能.弹墙)
        {
            if (矢.执行段?.功能 == 道纹功能.折返 && 矢.扩展阶段 != 2) { 矢.剩余距离 = 0; return 扩展飞行结束(矢); }
            return false;
        }
        Vector2 差 = 新 - 旧; float 低 = 0, 高 = 1;
        for (int i = 0; i < 12; i++) { float 中 = (低 + 高) * .5f; if (寻路.无遮挡(旧, 旧 + 差 * 中)) 低 = 中; else 高 = 中; }
        Vector2 点 = 旧 + 差 * Mathf.Max(0, 低 - .005f);
        bool 横挡 = !地图.可站立(点 + new Vector2(矢.方向.x, 0) * .15f), 竖挡 = !地图.可站立(点 + new Vector2(0, 矢.方向.y) * .15f);
        矢.方向 = 横挡 && !竖挡 ? new Vector2(-矢.方向.x, 矢.方向.y) : 竖挡 && !横挡 ? new Vector2(矢.方向.x, -矢.方向.y) : -矢.方向;
        矢.位置 = 点; 登记扩展飞行(矢, 旧); 扩展功能触发次数++;
        return 扩展续接(矢, -1, 点, true);
    }
    bool 扩展命中(战斗灵矢 矢, int 命中, Vector2 点)
    {
        var f = 矢.执行段.功能;
        if (f == 道纹功能.折返 || f == 道纹功能.弹墙) return true;
        矢.位置 = 点; 矢.标记敌人 = 命中;
        if (f == 道纹功能.停驻 || f == 道纹功能.震荡 || f == 道纹功能.烙印)
        { 设置功能等待(矢, 扩(f == 道纹功能.停驻 ? "stay_seconds" : f == 道纹功能.震荡 ? "pulse_seconds" : "mark_seconds")); return true; }
        扩展功能触发次数++;
        if (f == 道纹功能.爆破) 扩展范围伤害(矢, 点, 扩("blast_radius"), 扩("blast_factor"), true);
        if (f == 道纹功能.击退 && 命中 >= 0) 功能位移(敌人数据[命中], 矢.方向, 扩("push_distance"));
        if (f == 道纹功能.牵引)
            foreach (var 敌 in 敌人数据) if (敌.存活 && Vector2.Distance(敌.位置, 点) <= 扩("pull_radius"))
                功能位移(敌, 点 - 敌.位置, Mathf.Min(扩("pull_distance"), Vector2.Distance(敌.位置, 点)));
        if (f == 道纹功能.束缚 && 命中 >= 0 && 敌人数据[命中].存活)
            敌人数据[命中].束缚剩余秒 = Mathf.Max(敌人数据[命中].束缚剩余秒, 扩("bind_seconds") * 控制系数(敌人数据[命中]));
        return 扩展续接(矢, 命中, 点, false);
    }
    static float 控制系数(战斗敌人 敌) => 敌.布点.级别 == 战斗敌人级别.王级 ? 扩("boss_control_factor") : 敌.布点.级别 != 战斗敌人级别.普通 ? 扩("strong_control_factor") : 1;
    void 功能位移(战斗敌人 敌, Vector2 向, float 距)
    {
        if (!敌.存活 || 向.sqrMagnitude < .00001f) return;
        距 *= 控制系数(敌); 向.Normalize();
        while (距 > .0001f) { float 步 = Mathf.Min(.08f, 距); var 新 = 敌.位置 + 向 * 步; if (!地图.可站立(新) || !寻路.无遮挡(敌.位置, 新)) break; 敌.位置 = 新; 距 -= 步; }
        敌.路线.Clear(); 敌.再寻路 = 0;
        战术.取消功能位移(敌);
    }
    int 扩展范围伤害(战斗灵矢 矢, Vector2 点, float 半径, float 倍率, bool 排除历史)
    {
        int 数 = 0; 光圈数据.Add(new 战斗光圈 { 位置 = 点, 半径 = 半径 });
        for (int i = 0; i < 敌人数据.Count && 数 < 扩("hits_per_area"); i++)
        {
            var 敌 = 敌人数据[i];
            if (!敌.存活 || 排除历史 && 矢.命中过.Contains(i) || Vector2.Distance(敌.位置, 点) > 半径 || !寻路.无遮挡(点, 敌.位置)) continue;
            命中伤害(敌, 矢, 矢.形态倍率 * 倍率); 矢.命中过.Add(i); 数++;
        }
        return 数;
    }
    bool 扩展续接(战斗灵矢 矢, int 命中, Vector2 点, bool 重新飞行)
    {
        矢.根普攻弹 = false;
        if (矢.执行段.后续.Count == 0) return false;
        var 下 = 矢.执行段.后续[0];
        while (天帝顺序道纹.即时功能(下.功能) && 下.功能 != 道纹功能.齐射 && !天帝顺序道纹.多弹功能(下.功能))
        { if (下.后续.Count == 0) return false; 下 = 下.后续[0]; }
        bool 新弹 = 重新飞行 || 下.功能 == 道纹功能.齐射 || 天帝顺序道纹.多弹功能(下.功能) ||
            下.功能 == 道纹功能.延时 || 下.功能 == 道纹功能.陨落 || 下.功能 == 道纹功能.跃迁 || 下.功能 == 道纹功能.折返 || 下.功能 == 道纹功能.弹墙 || 天帝顺序道纹.攻击形态(下.功能);
        if (新弹)
        {
            if (下.功能 == 道纹功能.齐射 || 天帝顺序道纹.多弹功能(下.功能))
            { 发射执行段(下, 点, 矢.方向, -1, 矢.释放, 矢.形态倍率, 矢.命中过, 0, 待加灵矢, true); return false; }
            // 单弹阶段切换继续使用同一弹体，不把延时/反弹当成额外生成32颗的预算。
            矢.执行段 = 下; 矢.参数 = 下.参数; 矢.伤害 = (float)(下.参数.伤害 * 矢.形态倍率);
            矢.位置 = 点; 矢.剩余距离 = (float)天帝数值.取("shape.chain_range"); 矢.剩余连锁 = 下.参数.连锁;
            矢.剩余穿透 = -1; 矢.已分裂 = 矢.自动连锁 = false; 矢.扩展阶段 = 0; 矢.等待秒 = 矢.等待总秒 = 0;
            矢.回旋累计角 = 矢.波动相位 = 矢.拖尾余距 = 0; 矢.折返轨迹 = null; 初始化扩展功能(矢); return true;
        }
        if (下.功能 == 道纹功能.旧版) return false;
        矢.执行段 = 下; 矢.参数 = 下.参数; 矢.剩余穿透 = -1; 矢.已分裂 = false; 矢.等待秒 = 矢.等待总秒 = 0;
        矢.伤害 = (float)(下.参数.伤害 * 矢.形态倍率); 矢.剩余连锁 = 下.参数.连锁;
        return 下.功能 == 道纹功能.穿透 || 顺序命中(矢, 命中, 点);
    }
    static 战斗灵矢 扩展快照(战斗灵矢 矢) => new 战斗灵矢 { 位置 = 矢.位置, 方向 = 矢.方向, 参数 = 矢.参数, 释放 = 矢.释放,
        执行段 = 矢.执行段, 形态倍率 = 矢.形态倍率, 已飞距离 = 矢.已飞距离, 独立命中 = new HashSet<int>(矢.命中过) };
    void 推进扩展地面(float 秒)
    {
        for (int i = 道纹地面.Count - 1; i >= 0; i--)
        {
            var e = 道纹地面[i]; float 有效秒 = Mathf.Min(秒, e.剩余秒); e.剩余秒 -= 秒; e.下次伤害 -= 有效秒;
            while (e.下次伤害 <= .00001f) { 扩展范围伤害(e.来源, e.位置, e.半径, 扩("trail_factor") * 运动层(e.来源, 道纹功能.拖尾), false); e.下次伤害 += 扩("trail_tick"); }
            if (e.剩余秒 <= .00001f) 道纹地面.RemoveAt(i);
        }
    }
    void 清理扩展功能() { 道纹地面.Clear(); 形态演出.Clear(); 清理特性战斗(); foreach (var 敌 in 敌人数据) 敌.束缚剩余秒 = 0; }
    // 画布只推进隔离弹体，不驱动AI、开局、刷新、存档或奖励。
    public void 发射演示(普攻参数 参数)
    { if(参数.顺序计划==null){发射(参数);return;} if (灵矢数据.Count < 96) 发射执行段(参数.顺序计划.起点, 地图.出生位置, Vector2.right, 0, new 战斗释放记录 { 暴击采样 = 1, 暴击倍率 = 1 }, 1, new HashSet<int>(), 0, 灵矢数据, false, 参数.顺序计划.起始链路数); }
    public void 推进演示弹体(float 秒)
    {
        foreach (var 敌 in 敌人数据) 敌.束缚剩余秒 = Mathf.Max(0, 敌.束缚剩余秒 - 秒);
        推进扩展地面(秒);
        推进形态演出(秒);
        for (int i = 光圈数据.Count - 1; i >= 0; i--) { 光圈数据[i].剩余秒 -= 秒; if (光圈数据[i].剩余秒 <= 0) 光圈数据.RemoveAt(i); }
        for (int i = 电弧数据.Count - 1; i >= 0; i--) { 电弧数据[i].剩余秒 -= 秒; if (电弧数据[i].剩余秒 <= 0) 电弧数据.RemoveAt(i); }
        for (int i = 灵矢数据.Count - 1; i >= 0; i--) if (!灵矢一步(灵矢数据[i], 秒)) 灵矢数据.RemoveAt(i);
        处理连锁攻击(); 灵矢数据.AddRange(待加灵矢); 待加灵矢.Clear();
    }
    public void 设置演示靶(Vector2[] 点)
    {
        演示模式 = true;
        敌人数据.Clear();
        foreach (var p in 点) 敌人数据.Add(new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 地图.出生位置 + p), 战斗难度.普通, 100) { 已生成 = true });
    }
}
