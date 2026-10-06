using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class 天帝战斗系统
{
    public int 顺序齐射次数 { get; private set; }
    public int 顺序分裂次数 { get; private set; }
    public int 顺序连锁次数 { get; private set; }
    public int 顺序预算截断次数 { get; private set; }
    void 发射执行段(道纹执行段 段, Vector2 位置, Vector2 向, int 目标, 战斗释放记录 释放, double 倍率,
        HashSet<int> 历史, float 横偏移, List<战斗灵矢> 列表, bool 衍生, int 重复 = 1, bool 自动连锁 = false)
    {
        if (段 == null) return;
        int 剩余预算 = (int)天帝数值.取("rune.ordered_functions.projectiles_per_release") - 释放.顺序生成数;
        if (剩余预算 <= 0) { 顺序预算截断次数++; return; }
        var 首发 = new List<天帝顺序道纹.射击规格>(Math.Min(32, 剩余预算));
        for (int n = 0; n < 重复; n++)
        {
            foreach (var 规格 in 天帝顺序道纹.展开射击(段, () => 顺序齐射次数++, () => 顺序预算截断次数++))
                for (int j = 0; j < 规格.段.参数.数量; j++)
                {
                    if (首发.Count >= 剩余预算) { 顺序预算截断次数++; goto 完成展开; }
                    首发.Add(规格);
                }
        }
        完成展开:
        for (int i = 0; i < 首发.Count; i++)
        {
            var 规格 = 首发[i]; var 弹段 = 规格.段; var 弹向 = 转向(向, 规格.角度);
            Vector2 起 = 自动连锁 ? 位置 : 位置 + new Vector2(-向.y, 向.x) * (横偏移 + (规格.并排 || 弹段.参数.数量 > 1 ? (i - (首发.Count - 1) * .5f) * (float)天帝数值.取("rune.ordered_functions.parallel_offset") : 0));
            // 并排偏移不允许把出生点推入障碍；退回本次有效发射点。
            if (!地图.可站立(起) || !寻路.无遮挡(位置, 起)) 起 = 位置;
            var 矢 = new 战斗灵矢 { 位置 = 起, 方向 = 弹向, 目标 = 目标, 参数 = 弹段.参数, 执行段 = 弹段,
                独立命中 = new HashSet<int>(历史), 释放 = 释放, 形态倍率 = 倍率 * 规格.倍率, 伤害 = (float)(弹段.参数.伤害 * 倍率 * 规格.倍率),
                剩余距离 = 衍生 ? (float)天帝数值.取("shape.chain_range") : 普攻参数.飞行距离,
                剩余连锁 = 弹段.参数.连锁, 自动连锁 = 自动连锁 };
            初始化扩展功能(矢); 列表.Add(矢);
            矢.根普攻弹 = !衍生 && 释放.根普攻;
            释放.顺序生成数++;
        }
    }
    bool 顺序命中(战斗灵矢 矢, int 命中, Vector2 点)
    {
        var 段 = 矢.执行段;
        if (矢.子矢) return false;
        if (段.功能 >= 道纹功能.折返 && !天帝顺序道纹.即时功能(段.功能)) return 扩展命中(矢, 命中, 点);
        if (段.功能 == 道纹功能.穿透)
        {
            if (矢.剩余穿透 < 0) 矢.剩余穿透 = (int)天帝数值.取("rune.ordered_functions.pierce_count");
            if (矢.剩余穿透-- > 0) return true;
            if (段.后续.Count == 0) return false;
            var 下 = 段.后续[0];
            while (天帝顺序道纹.即时功能(下.功能) && 下.功能 != 道纹功能.齐射 && !天帝顺序道纹.多弹功能(下.功能))
            { if (下.后续.Count == 0) return false; 下 = 下.后续[0]; }
            if (下.功能 >= 道纹功能.折返) return 扩展续接(矢, 命中, 点, false);
            if (下.功能 == 道纹功能.齐射 || 天帝顺序道纹.多弹功能(下.功能))
            {
                发射执行段(下, 点, 矢.方向, -1, 矢.释放, 矢.形态倍率, 矢.命中过, 0, 待加灵矢, true);
                return false;
            }
            矢.执行段 = 下; 矢.参数 = 下.参数; 矢.剩余连锁 = 下.参数.连锁;
            矢.伤害 = (float)(下.参数.伤害 * 矢.形态倍率); 矢.已分裂 = false; 矢.剩余穿透 = -1;
            // 连续穿透在此开启下一阶段；其他命中型功能从第二次命中位置触发，不重复结算伤害。
            if (下.功能 == 道纹功能.穿透) return true;
            return 顺序命中(矢, 命中, 点);
        }
        if (!矢.已分裂)
        {
            矢.已分裂 = true;
            if (矢.参数.溅射半径 > 0)
            {
                光圈数据.Add(new 战斗光圈 { 位置 = 点, 半径 = 矢.参数.溅射半径 }); int 次级 = 0;
                for (int i = 0; i < 敌人数据.Count && 次级 < 天帝数值.取("shape.splash_targets_max"); i++)
                {
                    var 邻 = 敌人数据[i];
                    if (!邻.存活 || 矢.命中过.Contains(i) || Vector2.Distance(邻.位置, 点) > 矢.参数.溅射半径 || !寻路.无遮挡(点, 邻.位置)) continue;
                    矢.命中过.Add(i); 命中伤害(邻, 矢, 矢.形态倍率 * 天帝数值.取("shape.splash_factor")); 次级++; 溅射命中数++;
                }
            }
            // 旧词条存档兼容：子弹继承已执行的历史，不重复执行当前顺序功能。
            for (int i = 0; i < 矢.参数.分裂; i++)
            {
                int 下 = 找目标(点, (float)天帝数值.取("shape.split_child_life"), 矢.命中过);
                if (下 < 0) continue;
                var 末段 = new 道纹执行段 { 参数 = 矢.参数 };
                int 前 = 矢.释放.顺序生成数;
                发射执行段(末段, 点, (敌人数据[下].位置 - 点).normalized, 下, 矢.释放,
                    矢.形态倍率 * 天帝数值.取("shape.split_factor"), 矢.命中过, 0, 待加灵矢, true);
                for (int j = 待加灵矢.Count - (矢.释放.顺序生成数 - 前); j < 待加灵矢.Count; j++) 待加灵矢[j].子矢 = true;
                分裂生成数 += 矢.释放.顺序生成数 - 前;
            }
        }
        if (段.功能 == 道纹功能.分裂)
        {
            顺序分裂次数++; var 选过 = new HashSet<int>(矢.命中过);
            for (int i = 0; i < 段.后续.Count; i++)
            {
                int 下 = 找目标(点, (float)天帝数值.取("shape.split_child_life"), 选过);
                if (下 >= 0) 选过.Add(下);
                Vector2 向 = 下 >= 0 ? (敌人数据[下].位置 - 点).normalized : 转向(矢.方向, (i - .5f) * 35);
                int 前 = 矢.释放.顺序生成数;
                发射执行段(段.后续[i], 点, 向, 下, 矢.释放, 矢.形态倍率 * 天帝数值.取("rune.ordered_functions.split_factor"),
                    矢.命中过, 0, 待加灵矢, true);
                分裂生成数 += 矢.释放.顺序生成数 - 前;
            }
            return false;
        }
        if (段.功能 == 道纹功能.连锁)
        {
            int 下 = 找目标(点, (float)天帝数值.取("shape.chain_range"), 矢.命中过);
            if (下 < 0 || 段.后续.Count == 0) return false;
            int 前 = 矢.释放.顺序生成数;
            发射执行段(段.后续[0], 点, (敌人数据[下].位置 - 点).normalized, 下, 矢.释放,
                矢.形态倍率 * 天帝数值.取("rune.ordered_functions.chain_factor"), 矢.命中过, 0, 待加灵矢, true, 自动连锁: true);
            if (矢.释放.顺序生成数 > 前) { 顺序连锁次数++; 连锁发生数++; }
            return false;
        }
        if (矢.剩余连锁 > 0)
        {
            int 下 = 找目标(点, (float)天帝数值.取("shape.chain_range"), 矢.命中过);
            if (下 >= 0)
            {
                矢.剩余连锁--; 矢.形态倍率 *= 天帝数值.取("shape.chain_factor"); 矢.目标 = 下;
                矢.位置 = 点; 矢.方向 = (敌人数据[下].位置 - 点).normalized; 矢.伤害 = (float)(矢.参数.伤害 * 矢.形态倍率);
                矢.自动连锁 = true; 连锁发生数++; return true;
            }
        }
        return false;
    }
}
