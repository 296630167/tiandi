#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 组合在独立网络中穷举；实战只推进一次释放的弹体，禁止宿主自动保存和敌人AI介入。
public static class 天帝顺序道纹验证
{
    [Serializable] sealed class 报告
    {
        public int 线性组合, 朝向组合, 分支组合, 实战组合;
        public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>();
    }
    static 报告 r;
    static void 查(string 名, bool 对) => (对 ? r.通过 : r.失败).Add(名);
    static void 设(object o, string n, object v) => o.GetType().GetProperty(n).SetValue(o, v);
    static object 字段(object o, string n) => o.GetType().GetField(n, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    static object 调(object o, string n, params object[] v) => o.GetType().GetMethod(n, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, v);
    static 天帝道纹 新网(天赋种类 天 = 天赋种类.普通人)
    { var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天)); 网.设置玩家等级(100); return 网; }
    static 道纹实例 功(天帝道纹 网, 道纹功能 f, Vector2Int p, int inlet = 3)
    {
        var x = 天帝道纹生成.创建(网.道纹.Count + 1, 道纹分类.功能, 道纹品阶.稀有, new System.Random(21));
        设(x, "功能", f); 设(x, "入口方向", inlet); x.接口 = 天帝顺序道纹.固定接口(f, inlet);
        x.词条.Clear(); x.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(f), 1)); 放(网, x, p); return x;
    }
    static 道纹实例 属性(天帝道纹 网, Vector2Int p, int 口, 道纹属性 a, double 值)
    {
        var x = new 道纹实例 { 编号 = 网.道纹.Count + 1, 接口 = 口 };
        设(x, "分类", 道纹分类.属性); 设(x, "物品等级", 100);
        x.词条.Add(道纹词条.从定点(a, (long)Math.Round(值 * 100))); 放(网, x, p); return x;
    }
    static void 放(天帝道纹 网, 道纹实例 x, Vector2Int p)
    { if (!网.获得道纹(x) || !网.解锁格子(p) || !网.放置(x, p)) throw new Exception("隔离网络放置失败 " + p); }
    static 天帝道纹 线(道纹功能[] fs)
    { var 网 = 新网(); for (int i = 0; i < fs.Length; i++) 功(网, fs[i], new Vector2Int(i + 1, 0)); return 网; }
    static 道纹执行计划 编译(天帝道纹 网, int 路 = 0)
    { using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网)) return 天帝顺序道纹.编译(网, 人, 路); }
    static int 根(道纹功能[] fs, int i = 0) => i == fs.Length || fs[i] != 道纹功能.齐射 ? 1 : 1 + 根(fs, i + 1);
    static Vector2Int 转(Vector2Int p) => new Vector2Int(-p.y, p.x + p.y);
    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式运行。");
        天帝数值同步检查.校验(); r = new 报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/顺序道纹-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        Application.LogCallback 日志 = (文, 栈, 类) => { if (类 == LogType.Error || 类 == LogType.Exception || 类 == LogType.Assert) r.错误.Add(文 + 栈); };
        Application.logMessageReceived += 日志;
        try
        {
            for (int len = 1; len <= 4; len++) for (int code = 0; code < Math.Pow(3, len); code++)
            {
                var fs = new 道纹功能[len]; int n = code;
                for (int i = 0; i < len; i++, n /= 3) fs[i] = (道纹功能)(n % 3 + 1);
                string 名 = string.Join("→", fs); var 网 = 线(fs); var p = 编译(网); r.线性组合++;
                var 主线 = p.起点; bool 顺序 = true;
                foreach (var f in fs) { 顺序 &= 主线.功能 == f; 主线 = 主线.后续[0]; }
                查("链路顺序-" + 名, 顺序 && 主线.功能 == 道纹功能.旧版 && p.功能数 == len && p.提示.Count == 0);
                查("首发数量-" + 名, p.根弹数 == 根(fs));
                for (int d = 0; d < 6; d++)
                {
                    var 布 = 网.当前布局();
                    foreach (var 项 in 布)
                    {
                        for (int j = 0; j < d; j++) 项.格子 = 转(项.格子);
                        for (int j = 0; j < d; j++) 项.接口 = ((项.接口 << 1) | (项.接口 >> 5)) & 63;
                    }
                    var 回 = 天帝道纹.读取存档(网.导出存档());
                    foreach (var 项 in 布) if (!回.格已解锁(项.格子)) 回.解锁格子(项.格子);
                    bool 放对 = 回.应用布局(布, out _); var cp = 编译(回, d); r.朝向组合++;
                    查("旋转与方向-" + 名 + "@" + d, 放对 && cp.功能数 == len && cp.根弹数 == p.根弹数 && cp.提示.Count == 0);
                }
                实战(网, 名, false); r.实战组合++;
            }
            for (int a = 1; a <= 3; a++) for (int b = 1; b <= 3; b++) for (int c = 1; c <= 3; c++)
            {
                var 网 = 新网(); 功(网, (道纹功能)a, new Vector2Int(1, 0)); 功(网, (道纹功能)b, new Vector2Int(2, 0));
                if (a != 3) 功(网, (道纹功能)c, new Vector2Int(1, 1), 4);
                var p = 编译(网); r.分支组合++;
                查("左右分支-" + a + b + c, p.提示.Count == 0 && p.起点.后续[0].功能 == (道纹功能)b &&
                    (a == 3 || p.起点.后续[1].功能 == (道纹功能)c));
                实战(网, "分支" + a + b + c, false); r.实战组合++;
            }
            扩展功能(); 任意接口(); 闭环去重(); 直线弹道(); 自动连锁验证(); 属性隔离(); 方向与兼容(目录); 预算(); 演示(); 纸面与天赋();
        }
        catch (Exception ex) { r.错误.Add(ex.ToString()); }
        finally { Application.logMessageReceived -= 日志; File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(r, true)); }
        return 目录;
    }
    static void 扩展功能()
    {
        for (int a = 1; a <= 8; a++) for (int b = 1; b <= 8; b++) for (int c = 0; c <= 8; c++)
        {
            var fs = c == 0 ? new[] { (道纹功能)a, (道纹功能)b } : new[] { (道纹功能)a, (道纹功能)b, (道纹功能)c };
            var 网 = 线(fs); var p = 编译(网); string 名 = string.Join("→", fs);
            查("八功能两两三重顺序编译-" + 名, p.功能数 == fs.Length && p.提示.Count == 0 && p.各段.All(s => s.来源.Count == s.来源.Distinct().Count()));
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var 参数 = 普攻参数.读取(网, 人); var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通);
                靶场(战, 地, false); 调(战, "发射", 参数);
                var 活 = (List<战斗灵矢>)字段(战, "灵矢数据"); var 待 = (List<战斗灵矢>)字段(战, "待加灵矢"); var 释放 = 活[0].释放;
                bool 历史正确 = true;
                for (int k = 0; k < 1000 && 活.Count > 0; k++)
                {
                    for (int i = 活.Count - 1; i >= 0; i--)
                    {
                        var 矢 = 活[i]; var 前 = new HashSet<int>((HashSet<int>)字段(矢, "独立命中"));
                        bool 留 = (bool)调(战, "灵矢一步", 矢, .025f);
                        历史正确 &= 前.All(((HashSet<int>)字段(矢, "独立命中")).Contains);
                        if (!留) 活.RemoveAt(i);
                    }
                    调(战, "处理连锁攻击"); 活.AddRange(待); 待.Clear();
                }
                查("八功能组合实战有限收敛-" + 名, 活.Count == 0 && (int)字段(释放, "顺序生成数") <= 32 && 历史正确);
                查("八功能纸面结果有限-" + 名, !double.IsNaN(天帝顺序道纹.伤害预算(p, 8, 1)) && 天帝顺序道纹.伤害预算(p, 8, 1) > 0);
            }
        }
        foreach (var f in new[] { 道纹功能.增大, 道纹功能.缩小, 道纹功能.加速, 道纹功能.减速 })
        {
            var 网 = 线(new[] { f }); var p = 编译(网); var 弹段 = 天帝顺序道纹.展开齐射(p.起点).Single();
            double 倍 = f == 道纹功能.增大 || f == 道纹功能.加速 ? 1.5 : 2d / 3;
            查("单修饰倍率-" + f, Math.Abs((f == 道纹功能.增大 || f == 道纹功能.缩小 ? 弹段.参数.体型倍率 : 弹段.参数.弹速倍率) - 倍) < .00001 && p.根弹数 == 1);
            网.道纹[0].接口 = 8; 网.重算(); var 单口 = 编译(网);
            查("单口修饰作为末端仍生效-" + f, 单口.根弹数 == 1 && 天帝顺序道纹.展开齐射(单口.起点).Single().参数.体型倍率 == 弹段.参数.体型倍率 &&
                天帝顺序道纹.展开齐射(单口.起点).Single().参数.弹速倍率 == 弹段.参数.弹速倍率);
            var 极 = 编译(线(Enumerable.Repeat(f, 12).ToArray())); var 末 = 天帝顺序道纹.展开齐射(极.起点).Single().参数;
            查("重复修饰限幅-" + f, 末.体型倍率 >= .399f && 末.体型倍率 <= 3 && 末.弹速倍率 >= .25f && 末.弹速倍率 <= 3);
        }
        var 成对 = 编译(线(new[] { 道纹功能.增大, 道纹功能.缩小, 道纹功能.加速, 道纹功能.减速 }));
        var 成对末 = 天帝顺序道纹.展开齐射(成对.起点).Single().参数;
        查("反向修饰成对抵消", Math.Abs(成对末.体型倍率 - 1) < .00001 && Math.Abs(成对末.弹速倍率 - 1) < .00001);
        var 支 = 新网(); 功(支, 道纹功能.增大, new Vector2Int(1, 0)); 功(支, 道纹功能.齐射, new Vector2Int(2, 0));
        功(支, 道纹功能.缩小, new Vector2Int(3, 0)); 功(支, 道纹功能.加速, new Vector2Int(2, 1), 4);
        var 支计 = 编译(支); var 弹组 = 天帝顺序道纹.展开齐射(支计.起点).ToArray();
        查("修饰共同上游继承且兄弟分支不串值", 弹组.Length == 2 && 支计.提示.Count == 0 &&
            Math.Abs(弹组[0].参数.体型倍率 - 1) < .00001 && 弹组[0].参数.弹速倍率 == 1 &&
            弹组[1].参数.体型倍率 == 1.5f && 弹组[1].参数.弹速倍率 == 1.5f);
        foreach (var 后 in new[] { 道纹功能.齐射, 道纹功能.分裂, 道纹功能.连锁, 道纹功能.增大, 道纹功能.缩小, 道纹功能.加速, 道纹功能.减速, 道纹功能.穿透 })
        {
            var 计 = 编译(线(new[] { 道纹功能.穿透, 后 }));
            double 预期 = 后 == 道纹功能.齐射 || 后 == 道纹功能.穿透 ? 4 : 后 == 道纹功能.分裂 ? 3 : 后 == 道纹功能.连锁 ? 2.8 : 2;
            查("穿透纸面伤害独立对照-" + 后, Math.Abs(天帝顺序道纹.伤害预算(计, 8, 1) / 计.起点.参数.伤害 - 预期) < .00001);
        }
        foreach (bool 连续 in new[] { false, true })
        {
            var 网 = 线(连续 ? new[] { 道纹功能.穿透, 道纹功能.穿透 } : new[] { 道纹功能.加速, 道纹功能.穿透, 道纹功能.分裂 });
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 靶场(战, 地, true);
                var 敌 = (List<战斗敌人>)字段(战, "敌人数据");
                foreach (float x in new[] { 4f, 6f, 7.5f })
                { var e = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 地.出生位置 + Vector2.right * x), 战斗难度.普通, 100); 设(e, "已生成", true); 敌.Add(e); }
                调(战, "发射", 普攻参数.读取(网, 人)); var 矢 = 战.灵矢[0];
                bool 留 = (bool)调(战, "灵矢一步", 矢, 8f / 矢.参数.弹速);
                查("同帧高速穿透不漏目标-" + 连续, !留 && ((HashSet<int>)字段(矢, "独立命中")).Count == (连续 ? 4 : 2) &&
                    敌.Count(e => e.血量 < e.最大血量) == (连续 ? 4 : 2) && 战.顺序分裂次数 == (连续 ? 0 : 1));
            }
        }
        foreach (var 后 in new[] { 道纹功能.齐射, 道纹功能.分裂, 道纹功能.连锁, 道纹功能.增大, 道纹功能.缩小, 道纹功能.加速, 道纹功能.减速, 道纹功能.穿透 })
            穿透定向(后);
        foreach (var f in new[] { 道纹功能.增大, 道纹功能.缩小 })
        {
            var 网 = 线(new[] { f }); using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 靶场(战, 地, true);
                调(战, "发射", 普攻参数.读取(网, 人)); var 矢 = 战.灵矢[0]; 设(战.敌人[0], "位置", 战.敌人[0].位置 + Vector2.up * .83f);
                for (int i = 0; i < 400; i++) if (!(bool)调(战, "灵矢一步", 矢, .025f)) break;
                查("体型改变实际命中半径-" + f, (战.敌人[0].血量 < 战.敌人[0].最大血量) == (f == 道纹功能.增大));
            }
        }
        var 孤 = 线(new[] { 道纹功能.穿透, 道纹功能.分裂 });
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 孤))
        {
            var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 孤, 人, 战斗难度.普通); 靶场(战, 地, true);
            调(战, "发射", 普攻参数.读取(孤, 人)); var 矢 = 战.灵矢[0]; bool 留 = true;
            for (int i = 0; i < 1000 && 留; i++) 留 = (bool)调(战, "灵矢一步", 矢, .025f);
            查("只有第一目标飞行耗尽不触发后续", !留 && 战.敌人[0].血量 < 战.敌人[0].最大血量 && 战.顺序分裂次数 == 0 &&
                ((List<战斗灵矢>)字段(战, "待加灵矢")).Count == 0);
        }
    }
    static void 穿透定向(道纹功能 后)
    {
        var 网 = 线(new[] { 道纹功能.穿透, 后 });
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var p = 普攻参数.读取(网, 人); var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通);
            靶场(战, 地, true); var 敌 = (List<战斗敌人>)字段(战, "敌人数据"); var 点 = 地.出生位置;
            foreach (float x in new[] { 4f, 6f, 7.5f })
            { var e = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 点 + Vector2.right * x), 战斗难度.普通, 100); 设(e, "已生成", true); 敌.Add(e); }
            调(战, "发射", p); var 矢 = 战.灵矢[0]; var 待 = (List<战斗灵矢>)字段(战, "待加灵矢"); bool 留 = true;
            for (int k = 0; k < 200 && 敌[0].血量 == 敌[0].最大血量; k++) 留 = (bool)调(战, "灵矢一步", 矢, .01f);
            查("穿透第一次命中不执行后续-" + 后, 留 && 矢.执行段.功能 == 道纹功能.穿透 && 敌[1].血量 == 敌[1].最大血量 && 待.Count == 0 && 战.顺序分裂次数 == 0 && 战.顺序连锁次数 == 0);
            for (int k = 0; k < 200 && 敌[1].血量 == 敌[1].最大血量; k++) 留 = (bool)调(战, "灵矢一步", 矢, .01f);
            查("穿透第二命中触发后续且不重复伤害-" + 后, 敌[1].血量 < 敌[1].最大血量 && ((HashSet<int>)字段(矢, "独立命中")).Count >= 2 &&
                (后 == 道纹功能.穿透 ? 留 && 矢.执行段.功能 == 后 : 后 == 道纹功能.齐射 ? 待.Count == 2 : 后 == 道纹功能.分裂 ? 战.顺序分裂次数 == 1 : 后 == 道纹功能.连锁 ? 战.顺序连锁次数 == 1 : !留));
        }
    }
    static void 自动连锁验证()
    {
        foreach (int 模式 in new[] { 0, 1, 2, 3, 4 }) foreach (bool 范围内 in new[] { true, false })
        {
            var 网 = 新网(模式 == 1 ? 天赋种类.续雷 : 天赋种类.普通人);
            if (模式 == 0) 属性(网, new Vector2Int(1, 0), 9, 道纹属性.连锁, 1);
            if (模式 >= 2) { 功(网, 道纹功能.连锁, new Vector2Int(1, 0)); if (模式 > 2) 功(网, (道纹功能)(模式 - 2), new Vector2Int(2, 0)); }
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var p = 普攻参数.读取(网, 人); var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通);
                靶场(战, 地, true); var 敌 = (List<战斗敌人>)字段(战, "敌人数据");
                Vector2 命中点 = 敌[0].位置;
                Vector2 邻点 = 命中点 + Vector2.right * (范围内 ? 2 : (float)天帝数值.取("shape.chain_range") + .1f);
                var 邻 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 邻点), 战斗难度.普通, 100); 设(邻, "已生成", true); 敌.Add(邻);
                float 前血 = 邻.血量; 调(战, "发射", p); var 矢 = 战.灵矢[0];
                for (int i = 0; i < 400 && 敌[0].血量 == 敌[0].最大血量; i++)
                {
                    bool 留 = (bool)调(战, "灵矢一步", 矢, .025f);
                    if (!留) ((List<战斗灵矢>)字段(战, "灵矢数据")).Remove(矢);
                }
                if (范围内)
                {
                    // 目标选择之后移入一个拦路敌人，直接连锁仍必须命中选定目标。
                    var 挡 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 命中点 + Vector2.right), 战斗难度.普通, 100);
                    设(挡, "已生成", true); 敌.Add(挡);
                    设(邻, "位置", 邻.位置 + Vector2.up * .1f);
                    调(战, "处理连锁攻击");
                    查("连锁自动命中并显示连接-" + 模式, 邻.血量 < 前血 && 战.电弧.Count > 0 && 战.电弧[0].跳链 &&
                        战.电弧[0].起点 == 命中点 && 战.电弧[0].终点 == 邻.位置);
                    查("连锁不经过飞行也不被拦路敌人截获-" + 模式, 挡.血量 == 挡.最大血量 &&
                        !战.灵矢.Any(v => v.自动连锁) && !((List<战斗灵矢>)字段(战, "待加灵矢")).Any(v => v.自动连锁));
                }
                else { 调(战, "处理连锁攻击"); 查("范围外不自动连锁-" + 模式, 邻.血量 == 前血 && 战.电弧.Count == 0 && 战.连锁发生数 == 0); }
                查("自动连锁排除已命中目标-" + 模式 + 范围内, 战.连锁发生数 <= 1);
            }
        }
    }
    static void 直线弹道()
    {
        for (int f = 0; f <= 3; f++) foreach (bool 躲开 in new[] { false, true })
        {
            var 网 = 新网(); if (f > 0) 功(网, (道纹功能)f, new Vector2Int(1, 0));
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var p = 普攻参数.读取(网, 人); var 地 = new 天帝战斗地图(42, true);
                var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 靶场(战, 地, true);
                var 敌 = 战.敌人[0]; int 命中 = 0; 战.伤害反馈 += (_, __, ___) => 命中++;
                查("正式弹道转向参数为零-" + f, p.转向角度 == 0 && (p.顺序计划 == null || p.顺序计划.各段.All(s => s.参数.转向角度 == 0)));
                查("自动选择最近敌人-" + f, (int)调(战, "找目标", 地.出生位置, 普攻参数.索敌距离, null) == 0);
                调(战, "发射", p); var 首发 = 战.灵矢.ToArray(); var 向 = 首发.Select(v => v.方向).ToArray();
                if (躲开) 设(敌, "位置", 敌.位置 + Vector2.up * 3);
                bool 固定 = true;
                for (int i = 0; i < 首发.Length; i++)
                {
                    // 即便旧调用传来非零弧度，运行时也不得跟随目标转弯。
                    首发[i].参数.转向角度 = 180;
                    for (int j = 0; j < 400; j++)
                    {
                        bool 留 = (bool)调(战, "灵矢一步", 首发[i], .025f); 固定 &= 首发[i].方向 == 向[i]; if (!留) break;
                    }
                }
                查("发射后方向固定-" + f + 躲开, 固定);
                查("敌人离开原弹道可躲开-" + f + 躲开, 躲开 ? 命中 == 0 : 命中 > 0);
            }
        }
    }
    static void 闭环去重()
    {
        for (int 圈 = 1; 圈 <= 4; 圈++) for (int 模式 = 0; 模式 < 4; 模式++)
        {
            var 网 = 新网(); var 环 = new List<Vector2Int>(); var 格 = new Vector2Int(圈, 0);
            foreach (int d in new[] { 2, 3, 4, 5, 0, 1 }) for (int j = 0; j < 圈; j++) { 环.Add(格); 格 += 天帝道纹.邻向[d]; }
            for (int q = 1; q < 圈; q++) 属性(网, new Vector2Int(q, 0), 9, 道纹属性.火, 1);
            int 属性数 = 圈 - 1;
            for (int i = 0; i < 环.Count; i++)
            {
                int 口 = 0;
                foreach (int 邻 in new[] { (i + 环.Count - 1) % 环.Count, (i + 1) % 环.Count })
                    口 |= 1 << Array.IndexOf(天帝道纹.邻向, 环[邻] - 环[i]);
                if (i == 0) 口 |= 1 << 3;
                if (模式 == 0 || 模式 == 1 && i > 0) { 属性(网, 环[i], 口, 道纹属性.火, 1); 属性数++; }
                else
                {
                    var f = i == 0 ? (模式 == 3 ? 道纹功能.分裂 : 道纹功能.齐射) : (道纹功能)(i % 3 + 1);
                    if (f != 道纹功能.连锁 && i != 0)
                        for (int d = 0; d < 6; d++) if (天帝道纹.格权重(环[i] + 天帝道纹.邻向[d]) > 圈) { 口 |= 1 << d; break; }
                    var x = 功(网, f, 环[i]); x.接口 = 口; 网.重算();
                }
            }
            var p = 编译(网); string 名 = "圈" + 圈 + "/模式" + 模式;
            查("整圈闭合全网属性不重复-" + 名, 网.生效数 == 圈 - 1 + 环.Count && 网.生效加成[(int)道纹属性.火] == 属性数 &&
                网.弹槽加成.Where((_, d) => 网.弹槽生效数[d] > 0).All(a => a[(int)道纹属性.火] == 属性数));
            查("闭环每段属性来源唯一-" + 名, p.各段.All(s => s.来源.Count == s.来源.Distinct().Count() && s.参数.五行来源.火 == s.来源.Count));
            bool 唯一 = true;
            void 检查路径(道纹执行段 s, HashSet<int> 已触发)
            {
                var 历史 = new HashSet<int>(已触发);
                if (s.功能 != 道纹功能.旧版) 唯一 &= 历史.Add(s.节点编号);
                foreach (var 下 in s.后续) 检查路径(下, 历史);
            }
            检查路径(p.起点, new HashSet<int>());
            查("跨功能绕圈不重触发起点-" + 名, 唯一 && p.段数 <= 环.Count * 4 + 1 &&
                p.各段.Where(s => s.功能 != 道纹功能.旧版).All(s => s.节点编号 != 网.道纹[圈 - 1].编号 || ReferenceEquals(s, p.起点)));
            string 前 = p.详情(); 网.重算();
            查("闭环反复重算不积累-" + 名, 网.生效加成[(int)道纹属性.火] == 属性数 && 编译(网).详情() == 前);
            if (模式 > 0) 实战(网, "闭环" + 名, false, false);
        }
    }
    static void 任意接口()
    {
        // 全部20种三口/15种两口形状，逐个接口作为真实来路，并检查所有旋转。
        foreach (var f in new[] { 道纹功能.齐射, 道纹功能.分裂, 道纹功能.连锁 })
        for (int mask = 1; mask < 64; mask++)
        {
            if (Enumerable.Range(0, 6).Count(d => (mask & (1 << d)) != 0) != (f == 道纹功能.连锁 ? 2 : 3)) continue;
            for (int inlet = 0; inlet < 6; inlet++) if ((mask & (1 << inlet)) != 0)
            for (int rot = 0; rot < 6; rot++)
            {
                int m = mask; for (int i = 0; i < rot; i++) m = ((m << 1) | (m >> 5)) & 63;
                int 来 = (inlet + rot) % 6, 路 = (来 + 3) % 6;
                var 网 = 新网(); var 格 = 天帝道纹.邻向[路]; var 纹 = 功(网, f, 格);
                纹.接口 = m; 设(纹, "入口方向", (来 + 1) % 6); 网.重算();
                var 下游 = new Dictionary<int, 道纹实例>();
                for (int d = 0; d < 6; d++) if (d != 来 && (m & (1 << d)) != 0)
                    下游[d] = 属性(网, 格 + 天帝道纹.邻向[d], 1 << ((d + 3) % 6), 道纹属性.火, d + 1);
                var p = 编译(网, 路); string 名 = f + "/" + mask + "/" + inlet + "@" + rot;
                查("任意口接入与来路-" + 名, 纹.生效 && p.功能数 == 1 && p.起点.功能 == f && p.起点.实际来路 == 来 &&
                    p.起点.后续.Count == 下游.Count && p.提示.Count == 0 && p.根弹数 == (f == 道纹功能.齐射 ? 2 : 1));
                查("任意形状出口属性隔离-" + 名, 下游.All(kv => p.起点.后续.Count(s => s.来源.Contains(kv.Value.编号) &&
                    s.参数.五行来源.火 == kv.Key + 1 && s.来源.Count == 1) == 1));
                foreach (var 下 in 下游.Values) 下.接口 = 1 << ((Enumerable.Range(0, 6).Single(下.有接口) + 1) % 6);
                网.重算(); var 断 = 编译(网, 路);
                查("接口不对应不接属性-" + 名, 断.功能数 == 1 && 断.起点.后续.All(s => s.来源.Count == 0 && s.参数.五行来源.火 == 0));
            }
        }
        var 双 = 新网(); var 共 = 功(双, 道纹功能.齐射, new Vector2Int(1, 0));
        共.接口 = (1 << 2) | (1 << 3) | (1 << 4); 双.重算();
        属性(双, new Vector2Int(0, 1), (1 << 4) | (1 << 5), 道纹属性.力量, 2);
        查("同道纹按不同源路确定不同来路", 编译(双, 0).起点.实际来路 == 3 && 编译(双, 1).起点.实际来路 == 2);
    }
    static void 属性隔离()
    {
        foreach (var f in new[] { 道纹功能.齐射, 道纹功能.分裂, 道纹功能.连锁 })
        {
            var 网 = 新网(); var 上 = 属性(网, new Vector2Int(1, 0), 9, 道纹属性.力量, 10);
            功(网, f, new Vector2Int(2, 0)); var 左 = 属性(网, new Vector2Int(3, 0), 8, 道纹属性.火, 7);
            if (f != 道纹功能.连锁) 属性(网, new Vector2Int(2, 1), 16, 道纹属性.水, 19);
            var p = 编译(网); var x = p.起点.后续[0];
            查("上游继承与出口独立-" + f, p.起点.来源.SequenceEqual(new[] { 上.编号 }) && x.来源.Contains(上.编号) && x.来源.Contains(左.编号) &&
                x.参数.五行来源.火 == 7 && x.参数.五行来源.水 == 0 &&
                (f == 道纹功能.连锁 || p.起点.后续[1].参数.五行来源.水 == 19 && p.起点.后续[1].参数.五行来源.火 == 0));
            using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var 正式 = 天帝数值.计算主角(100, new double[天帝道纹属性.数量], 网.天赋);
                查("下游攻击不会反哺命中前伤害-" + f, x.参数.普通伤害 == p.起点.参数.普通伤害 && p.起点.参数.五行额外伤害 == 0 && x.参数.五行额外伤害 > 0);
            }
        }
        var 支 = 新网(); 功(支, 道纹功能.齐射, new Vector2Int(1, 0));
        属性(支, new Vector2Int(2, 0), 11, 道纹属性.力量, 10);
        属性(支, new Vector2Int(1, 1), 49, 道纹属性.智力, 20);
        var 共享 = 属性(支, new Vector2Int(2, 1), 24, 道纹属性.火, 5); var 计划 = 编译(支);
        查("共享下游每颗弹去重且两颗均继承", 计划.起点.后续.All(s => s.来源.Count(x => x == 共享.编号) == 1 && s.参数.五行来源.火 == 5));
        查("左右基础攻击独立", 计划.起点.后续[0].参数.普通伤害 != 计划.起点.后续[1].参数.普通伤害);
        查("暴伤不会串到兄弟分支", 计划.起点.后续[0].参数.暴击倍率 < 计划.起点.后续[1].参数.暴击倍率);
        var 快 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.穿越者)); 功(快, 道纹功能.齐射, new Vector2Int(1, 0));
        属性(快, new Vector2Int(2, 0), 8, 道纹属性.速度, 200);
        属性(快, new Vector2Int(1, 1), 16, 道纹属性.智力, 200); var 快计 = 编译(快);
        查("未触顶时弹速按本段灵力计算", 快计.起点.后续[0].参数.弹速 < 快计.起点.后续[1].参数.弹速);
        查("暴击率按投掷物分段", 快计.起点.后续[0].参数.暴击率 > 快计.起点.后续[1].参数.暴击率);
        var rel = new 战斗释放记录(); 设(rel, "暴击采样", (快计.起点.后续[0].参数.暴击率 + 快计.起点.后续[1].参数.暴击率) / 2d);
        var v0 = new 战斗灵矢 { 执行段 = 快计.起点.后续[0], 参数 = 快计.起点.后续[0].参数 };
        var v1 = new 战斗灵矢 { 执行段 = 快计.起点.后续[1], 参数 = 快计.起点.后续[1].参数 };
        设(v0, "释放", rel); 设(v1, "释放", rel);
        查("相同随机样本按本段属性判定暴击", v0.实际暴击倍率 > 1 && v1.实际暴击倍率 == 1);
    }
    static void 方向与兼容(string 目录)
    {
        var 网 = 线(new[] { 道纹功能.连锁 }); var x = 网.道纹[0]; int 口 = x.接口;
        网.保存布局方案(0, "正向"); for (int i = 0; i < 3; i++) 网.旋转(x);
        查("连锁180度同接口保持生效", x.接口 == 口 && x.生效 && 编译(网).起点.实际来路 == 3);
        var 沙 = new 天帝道纹连接诊断(网).预览(x, x.格子, false, 口, 3);
        查("拖动预览按物理接口且不改原实例", 沙 != null && 沙.道纹[0].接口 == 口 && 沙.道纹[0].生效 && x.接口 == 口);
        查("布局恢复连锁接口", 网.载入布局方案(0, out _) && x.接口 == 口 && x.生效);
        var 前 = 网.导出存档(); 网.旋转(x);
        var 方法 = typeof(天帝道纹).GetMethod("恢复画布", BindingFlags.Instance | BindingFlags.NonPublic);
        var 标识 = typeof(天帝道纹).GetMethod("快照标识", BindingFlags.Static | BindingFlags.NonPublic);
        var args = new object[] { 前, 标识.Invoke(null, new object[] { 网.导出存档() }), "" };
        查("撤销恢复物理端口", (bool)方法.Invoke(网, args) && x.接口 == 口 && x.生效);
        var 存 = new 天帝存档(Path.Combine(目录, "隔离存档"));
        查("正式存档读回功能方向", 存.保存(new 天帝存档数据 { 主角 = 天帝普攻.主角配置(), 画布 = 网.导出存档(), 通货 = new 天帝通货(网, 42).导出库存() }) &&
            天帝道纹.读取存档(存.读取().画布).道纹[0].入口方向 == 3);
        foreach (int 错 in new[] { -1, 6, 100 })
        {
            var d = 网.导出存档(); d.道纹[0].入口方向 = 错;
            var 回 = 天帝道纹.读取存档(d);
            查("旧入口字段不限制连接" + 错, 回.道纹[0].生效 && 编译(回).起点.实际来路 == 3);
        }
        foreach (int 错 in new[] { 0, 1, 7, 64 })
        {
            var d = 网.导出存档(); d.道纹[0].接口 = 错; bool 拒 = false;
            try { 天帝道纹.读取存档(d); } catch (InvalidDataException) { 拒 = true; }
            查("拒绝错误物理接口数量或掩码" + 错, 拒);
        }
        var 旧 = 新网(); var 老 = 属性(旧, new Vector2Int(1, 0), 9, 道纹属性.数量, 1);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 旧)) 查("旧混合形态保留累计", 普攻参数.读取(旧, 人).数量 == 2 && 普攻参数.读取(旧, 人).顺序计划 == null);
        功(旧, 道纹功能.连锁, new Vector2Int(2, 0)); 老.词条.Add(new 道纹词条(道纹属性.分裂, 1)); 旧.重算(); 实战(旧, "旧形态混入新链路", false);
        var 歧 = 新网(); 属性(歧, new Vector2Int(1, 0), 11, 道纹属性.力量, 1);
        功(歧, 道纹功能.分裂, new Vector2Int(2, 0)); 功(歧, 道纹功能.连锁, new Vector2Int(1, 1), 4);
        var pp = 编译(歧); 查("普通分叉多个下一功能明确提示", pp.功能数 == 0 && pp.提示.Count > 0 && pp.根弹数 == 1);
        var 断 = 线(new[] { 道纹功能.齐射, 道纹功能.分裂 }); 断.收回(断.道纹[1]);
        查("空出口断链仍保留两颗基础弹", 编译(断).根弹数 == 2 && 编译(断).功能数 == 1);
        实战(线(new[] { 道纹功能.齐射 }), "齐射同目标", true);
    }
    static void 预算()
    {
        var fs = Enumerable.Repeat(道纹功能.连锁, 25).ToArray(); var 网 = 线(fs); var p = 编译(网);
        查("功能深度16截断并提示", p.功能数 == 16 && p.提示.Count > 0);
        var 压 = 新网(); 设(压, "技能点", 300); // 隔离预算边界输入，不写真实进度。
        for (int q = 1; q <= 18; q++) for (int y = 0; y <= 12; y++)
            if (q + y <= 25) 功(压, 道纹功能.齐射, new Vector2Int(q, y), y == 0 ? 3 : 4);
        var cp = 编译(压);
        查("复杂重汇合图编译不超过256段", cp.段数 <= 256 && cp.提示.Count > 0);
        var 大 = 线(Enumerable.Repeat(道纹功能.齐射, 16).ToArray());
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 大))
        {
            var 参数 = 普攻参数.读取(大, 人);
            // 只向独立计划扩张压力分支，生产网络和正式数值不变。
            道纹执行段 树(int 深)
            {
                var s = new 道纹执行段 { 参数 = 参数.顺序计划.起点.参数, 功能 = 深 > 0 ? 道纹功能.齐射 : 道纹功能.旧版 };
                if (深 > 0) { s.后续.Add(树(深 - 1)); s.后续.Add(树(深 - 1)); } return s;
            }
            参数.顺序计划.起点 = 树(7);
            var 地 = new 天帝战斗地图(42, true); var 战 = new 天帝战斗系统(地, 大, 人, 战斗难度.普通);
            靶场(战, 地, true); 调(战, "发射", 参数);
            查("单次释放32弹体硬上限", 战.灵矢.Count == 32 && 战.顺序预算截断次数 > 0);
        }
    }
    static void 靶场(天帝战斗系统 战, 天帝战斗地图 地, bool 单)
    {
        var 敌 = (List<战斗敌人>)字段(战, "敌人数据"); 敌.Clear();
        for (int i = 0; i < (单 ? 1 : 20); i++)
        {
            var p = 地.出生位置 + new Vector2(2 + i % 5 * 1.25f, (i / 5 - 1.5f) * 1.1f);
            if (单) p = 地.出生位置 + new Vector2(2, 0);
            if (!地.可站立(p)) continue;
            var e = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, p), 战斗难度.普通, 100);
            设(e, "已生成", true); 敌.Add(e);
        }
        if (敌.Count == 0) throw new Exception("隔离靶场无合法目标");
    }
    static void 实战(天帝道纹 网, string 名, bool 单, bool 检查完整触发 = true)
    {
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 参数 = 普攻参数.读取(网, 人); var 地 = new 天帝战斗地图(42, true);
            var 战 = new 天帝战斗系统(地, 网, 人, 战斗难度.普通); 靶场(战, 地, 单);
            int 命中 = 0; 战.伤害反馈 += (p, dmg, crit) => 命中++;
            bool 发 = (bool)调(战, "发射", 参数);
            查("实战首发一致-" + 名, 发 && 战.灵矢.Count == 参数.顺序计划.根弹数);
            var 释放 = 战.灵矢[0].释放;
            var 活 = (List<战斗灵矢>)字段(战, "灵矢数据"); var 待 = (List<战斗灵矢>)字段(战, "待加灵矢");
            int 初始 = 活.Count; bool 平行 = 活.All(v => Vector2.Dot(v.方向, 活[0].方向) > .999f), 衍生对 = true, 历史对 = true;
            var 触发段 = new HashSet<道纹执行段>();
            HashSet<道纹执行段> 下一弹段(道纹执行段 s)
            {
                var 组 = new HashSet<道纹执行段>();
                void 展(道纹执行段 a) { if (a.功能 == 道纹功能.齐射) foreach (var b in a.后续) 展(b); else 组.Add(a); }
                foreach (var a in s.后续) 展(a); return 组;
            }
            for (int k = 0; k < 400 && 活.Count > 0; k++)
            {
                for (int i = 活.Count - 1; i >= 0; i--)
                {
                    var v = 活[i]; int 前 = 待.Count, 命前 = 命中;
                    bool 留 = (bool)调(战, "灵矢一步", v, .025f);
                    if (命中 > 命前) 触发段.Add(v.执行段);
                    var hs = (HashSet<int>)字段(v, "独立命中");
                    for (int j = 前; j < 待.Count; j++)
                    {
                        var ch = 待[j]; var childHs = (HashSet<int>)字段(ch, "独立命中");
                        历史对 &= !ReferenceEquals(hs, childHs) && hs.All(childHs.Contains) && ReferenceEquals(ch.释放, v.释放);
                        if (v.参数.分裂 == 0)
                        {
                            double factor = v.执行段.功能 == 道纹功能.分裂 ? .5 : .8;
                            衍生对 &= 下一弹段(v.执行段).Contains(ch.执行段) && Math.Abs(ch.形态倍率 - v.形态倍率 * factor) < .00001 &&
                                Math.Abs(ch.伤害 - ch.参数.伤害 * ch.形态倍率) < .0001;
                        }
                    }
                    if (!留) 活.RemoveAt(i);
                }
                活.AddRange(待); 待.Clear();
            }
            int 总 = (int)字段(释放, "顺序生成数");
            查("实战命中收敛与预算-" + 名, 命中 > 0 && 活.Count == 0 && 总 <= 32 && 总 >= 初始);
            查("实战齐射保持平行-" + 名, 平行);
            查("命中触发严格进入对应出口并继承倍率-" + 名, 衍生对);
            查("衍生弹复制祖先历史且暴击共享-" + 名, 历史对);
            if (检查完整触发 && !单 && 网.道纹.All(v => !v.词条.Any(t => t.属性 == 道纹属性.分裂) || v.是顺序功能))
                查("实战到达所有延迟功能-" + 名, 参数.顺序计划.各段.Where(s => s.功能 == 道纹功能.分裂 || s.功能 == 道纹功能.连锁).All(触发段.Contains));
            if (单) 查("齐射两弹均可击中唯一目标", 命中 == 2 && 总 == 2);
        }
    }
    static void 纸面与天赋()
    {
        double[,] 八 = { { 3, 3, 2.8 }, { 2.5, 2.5, 2.4 }, { 2.6, 2.6, 2.44 } };
        for (int a = 1; a <= 3; a++) for (int b = 1; b <= 3; b++)
        {
            var 网 = 线(new[] { (道纹功能)a, (道纹功能)b }); var p = 编译(网);
            查("纸面八目标独立对照-" + a + b, Math.Abs(天帝顺序道纹.伤害预算(p, 8, 1) / p.起点.参数.伤害 - 八[a - 1, b - 1]) < .00001);
            int 单 = a == 1 ? b == 1 ? 3 : 2 : 1;
            查("纸面单体允许齐射同目标-" + a + b, Math.Abs(天帝顺序道纹.伤害预算(p, 1, 1) / p.起点.参数.伤害 - 单) < .00001);
        }
        foreach (var t in 天帝天赋.全部) foreach (var f in new[] { 道纹功能.齐射, 道纹功能.分裂, 道纹功能.连锁 })
        {
            var 网 = 新网(t.种类); 功(网, f, new Vector2Int(1, 0)); var p = 编译(网);
            int 根 = (f == 道纹功能.齐射 ? 2 : 1) * (t.种类 == 天赋种类.双生矢 ? 2 : 1);
            查("十三天赋首发-" + t.名称 + f, p.根弹数 == 根);
            查("续雷仅分支末段完成额外连锁-" + t.名称 + f, p.各段.Where(s => s.功能 == 道纹功能.旧版).All(s => s.参数.连锁 == (t.种类 == 天赋种类.续雷 ? 1 : 0)));
            实战(网, t.名称 + f, false);
        }
    }
    static void 演示()
    {
        foreach (var fs in new[] { new[] { 道纹功能.齐射, 道纹功能.分裂 }, new[] { 道纹功能.分裂, 道纹功能.齐射 }, new[] { 道纹功能.连锁, 道纹功能.分裂 } })
        {
            var 网 = 线(fs); using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
            {
                var go = new GameObject("隔离形态演示", typeof(RectTransform));
                try
                {
                    var ui = go.AddComponent<天帝道纹攻击演示>(); ui.设置参数(普攻参数.读取(网, 人));
                    for (int i = 0; i < 500; i++) ui.推进演示(.025f);
                    查("画布演示顺序功能-" + string.Join("→", fs), ui.演示命中次数 > 0 &&
                        (fs.Contains(道纹功能.分裂) ? ui.演示分裂次数 > 0 : true) && (fs.Contains(道纹功能.连锁) ? ui.演示连锁次数 > 0 : true));
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
        }
    }
}
#endif
