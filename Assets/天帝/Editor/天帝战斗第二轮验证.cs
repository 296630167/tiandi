#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 第二轮战斗导演的隔离模型验收。不切换场景、不写正式存档，只检查等级档位、事件生命周期和链路演出状态。
public static class 天帝战斗第二轮验证
{
    static 天帝真实数值验证.报告 报告;
    static void 检查(string 名称, bool 通过) => (通过 ? 报告.通过 : 报告.失败).Add(名称);

    [MenuItem("天帝/验证/第二轮战斗内容")]
    public static void 菜单运行() => Debug.Log("第二轮战斗内容验证：" + 运行());

    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式运行第二轮战斗内容验证。");
        报告 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/战斗第二轮-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录);
        try
        {
            等级档位();
            事件生命周期();
            链路演出();
            碰撞边界();
        }
        catch (Exception 异常) { 报告.错误.Add(异常.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(报告, true));
        return 目录;
    }

    static void 等级档位()
    {
        int[] 等级 = { 1, 40, 80 };
        int[] 总量 = 等级.Select(l => Enumerable.Range(0, 3).Sum(q => 天帝数值.区域敌人数(l, q))).ToArray();
        int[] 精英 = 等级.Select(l => 天帝数值.区域敌人数(l, 1)).ToArray();
        int[] 头目 = 等级.Select(l => 天帝数值.区域敌人数(l, 2)).ToArray();
        检查("低/中/高地图总敌人数量递增", 总量[0] < 总量[1] && 总量[1] < 总量[2]);
        检查("中/高地图精英数量递增", 精英[0] <= 精英[1] && 精英[1] < 精英[2]);
        检查("中/高地图头目数量递增", 头目[0] <= 头目[1] && 头目[1] < 头目[2]);
        double lowActive = 天帝数值.大图成长(1, "active"), highActive = 天帝数值.大图成长(80, "active");
        double lowBatch = 天帝数值.大图成长(1, "batch"), highBatch = 天帝数值.大图成长(80, "batch");
        double lowInterval = 天帝数值.大图成长(1, "interval"), highInterval = 天帝数值.大图成长(80, "interval");
        检查("高等级场上容量与批次增加", highActive > lowActive && highBatch > lowBatch);
        检查("高等级增援间隔缩短", highInterval < lowInterval);
        检查("BOSS阶段按等级解锁", 天帝敌种配置.形态数(1) == 1 && 天帝敌种配置.形态数(40) == 3 && 天帝敌种配置.形态数(80) == 5);
        检查("高等级物种池扩大", 解锁物种数(1) < 解锁物种数(40) && 解锁物种数(40) < 解锁物种数(80));
    }

    static int 解锁物种数(int 等级)
    {
        int 数 = 0;
        for (int i = 0; i < 22; i++) if (天帝敌种配置.已解锁(i, 等级)) 数++;
        return 数;
    }

    static void 事件生命周期()
    {
        foreach (int 等级 in new[] { 1, 40, 80 })
        {
            var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                // 事件验收必须走正式生存大图构造，覆盖真实地图等级的容量、批次与混合敌群。
                var 通行 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/天帝/美术/生成素材/青岚原_四面兽潮通行.png");
                if (通行 == null) throw new InvalidOperationException("正式生存通行图未导入。");
                var 地 = new 天帝战斗地图(42, false, false, 通行, 等级, true);
                var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通, null, 等级);
                try
                {
                    // 生存波次由正式场景推进；事件本身只需要一个已生成目标作为观战前提。
                    战.设置演示靶(new[] { Vector2.right * 5f });
                    var 导演 = 战.导演;
                    var 类型 = new HashSet<战斗危险类型>();
                    bool 见预警 = false, 见生效 = false, 见结束 = false;
                    for (int i = 0; i < 600; i++)
                    {
                        导演.推进(.25f);
                        var 当前 = 导演.当前事件;
                        if (当前 != null)
                        {
                            类型.Add(当前.类型);
                            if (!当前.已生效) 见预警 = true;
                            if (当前.已生效) 见生效 = true;
                        }
                        见结束 |= 导演.事件结束反馈剩余秒 > 0;
                    }
                    检查(等级 + "级事件先预警后生效", 见预警 && 见生效);
                    检查(等级 + "级事件拥有结束反馈", 见结束);
                    检查(等级 + "级同屏只保留一个主要事件", 导演.事件同时峰值 <= 1 && 导演.事件次数 - 导演.事件结束次数 <= 1);
                    if (等级 == 1) 检查("1级只出现火焰裂隙", 类型.SetEquals(new[] { 战斗危险类型.火焰裂隙 }));
                    if (等级 == 40) 检查("40级事件覆盖火冰雷毒", new[] { 战斗危险类型.火焰裂隙, 战斗危险类型.冰霜潮汐, 战斗危险类型.雷击标记, 战斗危险类型.毒雾扩散 }.All(类型.Contains));
                    if (等级 == 80) 检查("80级事件加入风暴与范围收缩", 类型.Contains(战斗危险类型.能量风暴) && 类型.Contains(战斗危险类型.范围收缩));
                }
                finally { 战.清理特性战斗(); 战.战术.清理(); }
            }
        }
    }

    static void 链路演出()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 地 = new 天帝战斗地图(42, true);
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通);
            try
            {
                战.设置演示靶(new[] { Vector2.right * 3.5f });
                战.纯AI模式 = true;
                战.自动攻击启用 = true;
                bool 见槽位 = false, 见共鸣 = false;
                for (int i = 0; i < 360; i++)
                {
                    战.推进(地.出生位置, .025f);
                    见槽位 |= 战.当前链路演出槽 >= 0 && 战.链路演出进度 > .001f;
                    见共鸣 |= 战.完整链路共鸣剩余秒 > .001f;
                }
                检查("纯AI链路实际释放", 战.普通释放次数 > 0);
                检查("链路释放槽位可追踪", 见槽位);
                检查("完整链路共鸣状态可追踪", 见共鸣 || 战.参与通路数 <= 1);
            }
            finally { 战.清理特性战斗(); 战.战术.清理(); }
        }
    }

    static void 碰撞边界()
    {
        var 地 = new 天帝战斗地图(42, true);
        Vector2 点 = 地.出生位置;
        Vector2[] 方向 = { Vector2.right, Vector2.left, Vector2.up, Vector2.down, new Vector2(1, 1).normalized };
        bool 全部可站立 = true;
        for (int i = 0; i < 240; i++)
        {
            点 = 地.移动(点, 方向[i % 方向.Length], .15f);
            全部可站立 &= 地.可站立(点, .45f);
        }
        检查("自动移动边界始终可站立", 全部可站立);
    }
}
#endif
