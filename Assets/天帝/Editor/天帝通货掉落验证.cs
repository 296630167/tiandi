#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class 天帝通货掉落验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 设置(object 物, string 名, object 值) => 物.GetType().GetProperty(名).SetValue(物, 值);
    static bool 结算(天帝通货掉落 落, 战斗敌人 敌) => (bool)typeof(天帝通货掉落).GetMethod("敌人死亡", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(落, new object[] { 敌 });
    static 战斗敌人 死敌(战斗敌人级别 级, Vector2 点)
    { var 敌 = new 战斗敌人(new 战斗敌人布点(级, 点), 战斗难度.普通); 设置(敌, "血量", 0f); return 敌; }
    public static string 运行()
    {
        结果 = new 报告();
        try { 模型(); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        var 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/通货掉落-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        File.WriteAllText(Path.Combine(目录, "currency-loot.json"), JsonUtility.ToJson(结果, true)); return 目录;
    }
    static void 模型()
    {
        int[] 实际 = new int[13]; for (int n = 0; n < 10000; n++) 实际[(int)天帝通货掉落.种类结果(n)]++;
        检查("十一种通货有来源、旧接口通货权重为零", 天帝通货掉落.种类权重.Sum() == 10000 &&
            实际[(int)通货种类.通脉针] == 0 && 实际[(int)通货种类.六通玉] == 0 &&
            天帝通货.可用种类.All(x => 实际[(int)x] > 0) && 实际.SequenceEqual(天帝通货掉落.种类权重));
        foreach (int n in new[] { -1, 10000 }) { bool 拒绝 = false; try { 天帝通货掉落.种类结果(n); } catch (ArgumentOutOfRangeException) { 拒绝 = true; } 检查("非法权重点拒绝" + n, 拒绝); }
        var 图 = new 天帝战斗地图(42); var 普 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 幸 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.天命之子)); var 货 = new 天帝通货(普, 42);
        检查("真实开局无通货与额外候选", Enum.GetValues(typeof(通货种类)).Cast<通货种类>().All(t => 货.数量(t) == 0) && 普.道纹.Count == 0 && 普.技能点 == 1);
        foreach (战斗难度 难 in Enum.GetValues(typeof(战斗难度)))
        {
            foreach (战斗敌人级别 级 in Enum.GetValues(typeof(战斗敌人级别)))
            {
                var 落 = new 天帝通货掉落(图, 普, 货, 难); var 幸落 = new 天帝通货掉落(图, 幸, 货, 难);
                for (int i = 0; i < 2000; i++) { 结算(落, 死敌(级, 图.出生位置)); 结算(幸落, 死敌(级, 图.出生位置)); }
                float 基 = 天帝通货掉落.基础概率(级, 难), 幸运 = Mathf.Min(1, 基 * 1.5f);
                检查(难 + " " + 级 + "实际掉率接近配置", Mathf.Abs(落.地面.Count / 2000f - 基) < .04f && Mathf.Abs(幸落.地面.Count / 2000f - 幸运) < .04f);
                检查(难 + " " + 级 + "只掉一堆且数量按级别", 落.地面.Count <= 2000 && 落.地面.All(d => d.数量 == 天帝通货掉落.堆叠数量(级, 难)));
            }
        }
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 普))
        {
            var 战 = new 天帝战斗系统(图, 普, 人, 战斗难度.普通, 货);
            var 王 = 战.敌人.Single(e => e.布点.级别 == 战斗敌人级别.王级);
            检查("存活和空敌人拒绝结算", !结算(战.通货掉落, 王) && !结算(战.通货掉落, null));
            战.伤害敌人(王, 100000); var 物 = 战.通货掉落.地面.Single();
            检查("真实王死亡同时掉道纹与四个通货", 战.掉落.地面.Count == 1 && 物.数量 == 4 && !物.已拾取 && 货.数量(物.种类) == 0);
            检查("重复死亡不再掉落", !战.伤害敌人(王, 1) && !结算(战.通货掉落, 王) && 战.通货掉落.地面.Count == 1);
            int 通知 = 0, 通知量 = 0; 战.通货掉落.获得通货 += (t, n) => { 通知++; 通知量 += n; };
            检查("远处死亡和非法位置拒绝拾取", 战.通货掉落.拾取附近(图.出生位置, false) == 0 && 战.通货掉落.拾取附近(物.位置, true) == 0 && 战.通货掉落.拾取附近(new Vector2(1000, 1000), false) == 0);
            检查("附近详情准确且远处隐藏", 战.通货掉落.附近详情(物.位置) == 物 && 战.通货掉落.附近详情(图.出生位置) == null);
            检查("拾取入库同时产生一次逐条事件", 战.通货掉落.拾取附近(物.位置, false) == 1 && 货.数量(物.种类) == 4 && 通知 == 1 && 通知量 == 4 && 战.通货掉落.拾取数 == 1 && 战.通货掉落.拾取总量 == 4);
            检查("重复拾取与详情不重复", 战.通货掉落.拾取附近(物.位置, false) == 0 && 通知 == 1 && 战.通货掉落.附近详情(物.位置) == null);
            var 再 = new 天帝战斗系统(图, 普, 人, 战斗难度.普通, 货);
            检查("重入地面空且已拾取库存保留", 再.通货掉落.地面.Count == 0 && 货.数量(物.种类) == 4);
            // 独立随机流不会改变同种子道纹结果。
            var 原 = new 天帝道纹掉落(图, 普, 战斗难度.普通);
            typeof(天帝道纹掉落).GetMethod("敌人死亡", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(原, new object[] { 王 });
            var r = 原.地面.Single().道纹; var s = 战.掉落.地面.Single().道纹;
            检查("新增通货不改变原道纹品阶接口词条", r.品阶 == s.品阶 && r.接口 == s.接口 && r.分类 == s.分类 && r.词条.Select(t => t.属性 + ":" + t.数值).SequenceEqual(s.词条.Select(t => t.属性 + ":" + t.数值)));
        }
        // 隔墙与入库溢出：失败时保留原地，可稍后再拾取。
        var 落2 = new 天帝通货掉落(图, 普, 货, 战斗难度.普通); 结算(落2, 死敌(战斗敌人级别.头目, 图.出生位置)); var 满 = 落2.地面.Single();
        货.获得(满.种类, int.MaxValue - 货.数量(满.种类));
        检查("库存溢出拒绝且地面保留", 落2.拾取附近(满.位置, false) == 0 && !满.已拾取 && 落2.拾取总量 == 0);
        var 寻 = new 天帝战斗寻路(图); bool 找到 = false;
        for (int y = 1; y < 20 && !找到; y++) for (int x = 1; x < 20 && !找到; x++)
        {
            Vector2 角 = 图.格中心(x, y) - Vector2.one * 2;
            foreach (var 偏 in new[] { new Vector2(.55f, .55f), new Vector2(.55f, -.55f) })
            {
                var a = 角 + 偏; var b = 角 - 偏;
                if (!图.可站立(a) || !图.可站立(b) || 寻.无遮挡(a, b)) continue;
                找到 = true; var 测货 = new 天帝通货(普, 1); var 隔 = new 天帝通货掉落(图, 普, 测货, 战斗难度.普通);
                结算(隔, 死敌(战斗敌人级别.头目, b)); 设置(隔.地面.Single(), "位置", b);
                检查("半径内隔墙不能拾取", Vector2.Distance(a, b) < 1.8f && 隔.拾取附近(a, false) == 0);
                检查("绕到同侧可拾取", 隔.拾取附近(b, false) == 1); break;
            }
        }
        检查("隔墙夹具存在", 找到);
    }
}
#endif
