#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class 天帝AI掉落验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 字段(object 物, string 名, object 值) => 物.GetType().GetField(名, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(物, 值);
    static void 位置(战斗敌人 敌, Vector2 点) => typeof(战斗敌人).GetProperty("位置").SetValue(敌, 点);
    static 战斗敌人 加敌(天帝战斗系统 战, 战斗敌人级别 级, Vector2 点)
    { var 敌 = new 战斗敌人(new 战斗敌人布点(级, 点), 战斗难度.普通); ((List<战斗敌人>)战.敌人).Add(敌); return 敌; }
    static void 推(天帝战斗系统 战, Vector2 点, int 次)
    { for (int i = 0; i < 次; i++) { 战.推进(点, .1f); if (战.本次寻路次数 > 4) throw new Exception("寻路预算超限"); } }
    public static string 运行()
    {
        结果 = new 报告(); string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/AI掉落-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        try { 模型(); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        File.WriteAllText(Path.Combine(目录, "ai-loot.json"), JsonUtility.ToJson(结果, true)); return 目录;
    }
    static void 模型()
    {
        var 图 = new 天帝战斗地图(42); var 寻 = new 天帝战斗寻路(图); var 路 = new List<Vector2>();
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(图, 网, 人, 战斗难度.普通); var 列 = (List<战斗敌人>)战.敌人;
            Vector2 a = Vector2.zero, b = Vector2.zero; bool 找到 = false;
            for (int y = 1; y < 18 && !找到; y++) for (int x = 1; x < 18 && !找到; x++)
            {
                var 格 = new Vector2Int(x, y); if (!图.可通行格(格) || 图.王房范围.Contains(格)) continue;
                foreach (var 偏 in new[] { new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(-2, 1) })
                {
                    var 终 = 格 + 偏; if (!图.可通行格(终) || 图.王房范围.Contains(终)) continue;
                    a = 图.格中心(x, y); b = 图.格中心(终.x, 终.y);
                    if (!寻.无遮挡(a, b) && 寻.路径(a, b, 路, false, true) && 路.Count <= 6) { 找到 = true; break; }
                }
            }
            检查("找到有墙可绕的短路径夹具", 找到);
            列.Clear(); var 敌 = 加敌(战, 战斗敌人级别.普通, a);
            推(战, b, 6); 检查("隔墙靠近不凭空发现玩家", 敌.行动 == 敌人行动.待机 && 敌.位置 == a);
            战.伤害敌人(敌, 1); bool 合法 = true;
            for (int i = 0; i < 100; i++) { 战.推进(b, .1f); 合法 &= 图.可站立(敌.位置) && !图.王房范围.Contains(图.所在格(敌.位置)); }
            检查("受击得知位置后绕障且不穿墙不进王房", 合法 && Vector2.Distance(敌.位置, b) < 1.5f && 人.当前血量 < 人.血量);
            检查("寻路请求遵守每推进4次预算", 战.本次寻路次数 <= 4);
            float 敌血 = 敌.血量; 推(战, 图.出生位置, 260);
            检查("远离脱战归巢且保留受伤血量", 敌.行动 == 敌人行动.待机 && Vector2.Distance(敌.位置, a) < .21f && 敌.血量 == 敌血);

            // 营地九格通行区域，用于检验小队响应、锁定落点及可躲攻击。
            var c = 图.格中心(图.小队[0].中心格.x, 图.小队[0].中心格.y);
            列.Clear(); var 甲 = 加敌(战, 战斗敌人级别.普通, c); var 乙 = 加敌(战, 战斗敌人级别.普通, c + Vector2.right * 2);
            var 丙 = 加敌(战, 战斗敌人级别.普通, c + Vector2.up * 2);
            字段(甲, "小队编号", 0); 字段(乙, "小队编号", 0); 字段(丙, "小队编号", 1);
            战.伤害敌人(甲, 1);
            检查("受击只唤醒12米内同队且不全图连锁", 乙.行动 == 敌人行动.追击 && 丙.行动 == 敌人行动.待机);
            列.Clear(); var 攻 = 加敌(战, 战斗敌人级别.普通, c); Vector2 近 = c + Vector2.right;
            人.设置当前资源(人.血量, 人.灵力, 人.灵气护盾); 推(战, 近, 2);
            检查("近身攻击先进入可见蓄力", 攻.行动 == 敌人行动.蓄力);
            var 锁点 = (Vector2)typeof(战斗敌人).GetField("攻击落点", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(攻);
            float 血 = 人.当前血量; 推(战, c + Vector2.left * 3, 4);
            检查("蓄力锁定旧落点玩家躲开不受伤", 人.当前血量 == 血 && 锁点 == 近 && 攻.行动 == 敌人行动.后摇);
            推(战, c + Vector2.left * 3, 4); 检查("后摇结束才恢复追击", 攻.行动 != 敌人行动.后摇);
            列.Clear(); var 失踪 = 加敌(战, 战斗敌人级别.普通, a); 战.推进(b, .1f); 战.伤害敌人(失踪, 1);
            字段(失踪, "再寻路", 100f); // 模拟暂无可用缓存路线，保持隔墙状态验证记忆到搜索的转换。
            推(战, b, 14); 检查("丢失视线且暂无法寻路时进入搜索", 失踪.行动 == 敌人行动.搜索);
            列.Clear(); var 王 = 加敌(战, 战斗敌人级别.王级, 图.王位置);
            推(战, 图.王位置 + Vector2.right * 4, 3); 检查("进入王房触发王级警戒", 王.行动 != 敌人行动.待机);
            推(战, 图.出生位置, 80); 检查("王不跨房追击且回到出生点", 图.王房范围.Contains(图.所在格(王.位置)) && 王.行动 == 敌人行动.待机);
            列.Clear(); for (int i = 0; i < 20; i++) { var 群 = 加敌(战, 战斗敌人级别.普通, a); 字段(群, "序号", i); }
            战.推进(b, .1f); foreach (var 群 in 列) 战.伤害敌人(群, 1);
            bool 限额 = true; for (int i = 0; i < 30; i++) { 战.推进(b, .1f); 限额 &= 战.本次寻路次数 <= 4; }
            检查("20敌人同时绕障仍有预算且位置合法", 限额 && 列.All(e => 图.可站立(e.位置)) && 列.Any(e => Vector2.Distance(e.位置, a) > 2));
            战.伤害玩家(100000); var 停位 = 列[0].位置; 推(战, b, 4);
            检查("玩家死亡停止AI移动攻击和拾取", 列[0].位置 == 停位 && 战.掉落.拾取附近(b, true) == 0);
        }
        掉落模型(图);
    }
    static void 掉落模型(天帝战斗地图 图)
    {
        foreach (var 难 in new[] { 战斗难度.普通, 战斗难度.困难 })
        {
            int[] 次 = new int[8]; for (int n = 0; n < 10000; n++) 次[(int)天帝道纹掉落.品阶结果(难, n)]++;
            检查(难 + "八阶抽样区间无遗漏总和10000", 次.SequenceEqual(难 == 战斗难度.普通 ? 天帝道纹掉落.普通品阶权重 : 天帝道纹掉落.困难品阶权重));
            检查(难 + "普通掉落UI和实际概率同源", 天帝战斗地图.道纹掉落概率(难) == 天帝道纹掉落.基础概率(战斗敌人级别.普通, 难));
        }
        检查("通货已配置且经验仍未配置", 天帝战斗地图.通货掉落概率(战斗难度.普通) == .20f && !天帝战斗地图.经验倍率(战斗难度.普通).HasValue);
        var 普 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        var 幸 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.天命之子));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 普)) using (var 幸人 = new 天帝主角属性(天帝普攻.主角配置(), 幸))
        {
            var 战 = new 天帝战斗系统(图, 普, 人, 战斗难度.普通); var 幸战 = new 天帝战斗系统(图, 幸, 幸人, 战斗难度.普通);
            var c = 图.格中心(图.小队[0].中心格.x, 图.小队[0].中心格.y);
            ((List<战斗敌人>)战.敌人).Clear(); ((List<战斗敌人>)幸战.敌人).Clear();
            for (int i = 0; i < 2000; i++) { 战.伤害敌人(加敌(战, 战斗敌人级别.普通, c), 10000); 幸战.伤害敌人(加敌(幸战, 战斗敌人级别.普通, c), 10000); }
            检查("普通真实死亡抽样25%原型范围", 战.掉落.地面.Count > 420 && 战.掉落.地面.Count < 580);
            检查("天命真实掉落37.5%且只出一枚", 幸战.掉落.地面.Count > 650 && 幸战.掉落.地面.Count < 850);
            检查("拾取前候选不会提前增加", 普.道纹.Count == 0 && 幸.道纹.Count == 0);
            检查("地面只掉固定接口属性或分叉道纹", 战.掉落.地面.All(d => !d.道纹.格子.HasValue && !d.道纹.生效 &&
                (d.道纹.分类 == 道纹分类.属性 ? (d.道纹.是五行道纹 ? d.道纹.词条.Count == 1 :
                    d.道纹.词条.Count >= 天帝道纹品阶.获取(d.道纹.品阶).最少词条 && d.道纹.词条.Count <= d.道纹.词条上限) &&
                    Enumerable.Range(0, 6).Count(d.道纹.有接口) >= 1 && Enumerable.Range(0, 6).Count(d.道纹.有接口) <= 2 :
                 d.道纹.分类 == 道纹分类.分叉 && d.道纹.词条.Count == 0 && Enumerable.Range(0, 6).Count(d.道纹.有接口) >= 3)));
            var 头 = 加敌(战, 战斗敌人级别.头目, 图.出生位置); int 前 = 战.掉落.地面.Count;
            检查("头目必掉一枚", 战.伤害敌人(头, 10000) && 战.掉落.地面.Count == 前 + 1);
            检查("重复死亡和外来敌人不再生成掉落", !战.伤害敌人(头, 1) && !战.伤害敌人(new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, c), 战斗难度.普通), 10000) && 战.掉落.地面.Count == 前 + 1);
            检查("死亡不允许拾取", 战.掉落.拾取附近(图.出生位置, true) == 0);
            检查("超范围不隔空获得道纹", 战.掉落.拾取附近(图.出生位置 + Vector2.right * 3, false) == 0);
            int 点 = 普.技能点; double[] 加成 = (double[])普.生效加成.Clone(); int 通知 = 0; 普.状态改变 += () => 通知++;
            检查("靠近自动拾取转移至候选", 战.掉落.拾取附近(图.出生位置, false) == 1 && 普.道纹.Count == 1 && 战.掉落.拾取数 == 1);
            检查("拾取不自动装备不生效不改变技能点", 普.道纹.Last().编号 == 1 && !普.道纹.Last().格子.HasValue && !普.道纹.Last().生效 && 普.技能点 == 点 && 加成.SequenceEqual(普.生效加成) && 通知 == 1);
            检查("同一地面实例只能拾取一次", 战.掉落.拾取附近(图.出生位置, false) == 0 && !普.获得道纹(普.道纹.Last()) && 普.道纹.Count == 1);
            var 王 = 加敌(战, 战斗敌人级别.王级, 图.王位置); 前 = 战.掉落.地面.Count; 战.伤害敌人(王, 10000);
            检查("王级必掉一枚地面道纹", 战.掉落.地面.Count == 前 + 1 && 战.掉落.附近详情(图.王位置 + Vector2.right * 3) != null);
            战.掉落.拾取附近(图.王位置, false); 检查("不同掉落分配不同库存编号", 普.道纹.Count == 2 && 普.道纹.Select(d => d.编号).Distinct().Count() == 2);
            var 再入 = new 天帝战斗系统(图, 普, 人, 战斗难度.普通);
            检查("新战斗地面清空已拾取候选保留不重发", 再入.掉落.地面.Count == 0 && 普.道纹.Count == 2 && ReferenceEquals(战.掉落.最近拾取, 普.道纹.Last()));
            var 新纹 = 普.道纹.FirstOrDefault(x => x.分类 == 道纹分类.属性); var 货 = new 天帝通货(普, 7, 3);
            检查("拾取样本包含可洗练属性道纹", 新纹 != null);
            if (新纹 == null) return;
            检查("掉落道纹可直接通过现有通货洗练", 货.使用(通货种类.易纹砂, 新纹, 0, out var 描述) && 货.数量(通货种类.易纹砂) == 2 && !string.IsNullOrEmpty(描述));
            int 原口 = 新纹.接口; 新纹.接口 |= 8; 普.解锁格子(new Vector2Int(1, 0));
            检查("掉落道纹可以放置连接源纹并激活", 普.放置(新纹, new Vector2Int(1, 0)) && 新纹.生效);
            普.收回(新纹); 新纹.接口 = 原口;
            var 寻 = new 天帝战斗寻路(图); Vector2 墙左 = Vector2.zero, 墙右 = Vector2.zero; bool 隔墙 = false;
            for (int y = 1; y < 20 && !隔墙; y++) for (int x = 1; x < 20 && !隔墙; x++)
            {
                Vector2 边角 = 图.格中心(x, y) - Vector2.one * 2;
                foreach (var 对角 in new[] { new Vector2(.55f, .55f), new Vector2(.55f, -.55f) })
                {
                    墙左 = 边角 + 对角; 墙右 = 边角 - 对角;
                    if (图.可站立(墙左) && 图.可站立(墙右) && !寻.无遮挡(墙左, 墙右)) { 隔墙 = true; break; }
                }
            }
            检查("找到拾取半径内但隔墙的转角夹具", 隔墙);
            if (隔墙)
            {
                var 墙敌 = 加敌(再入, 战斗敌人级别.头目, 墙右); 再入.伤害敌人(墙敌, 10000);
                检查("1.8米内也不能隔墙吸取", Vector2.Distance(墙左, 墙右) < 天帝道纹掉落.拾取半径 && 再入.掉落.拾取附近(墙左, false) == 0);
                检查("绕到掉落一侧才可拾取", 再入.掉落.拾取附近(墙右, false) == 1);
            }
            var 平均 = 战.掉落.地面.Average(d => (int)d.道纹.品阶); var 幸均 = 幸战.掉落.地面.Average(d => (int)d.道纹.品阶);
            检查("实际天命品阶双抽取高提升样本均值", 幸均 > 平均 + .15);
            检查("普通道纹以单口为主且分叉独立", 战.掉落.地面.Count(d => d.道纹.分类 == 道纹分类.属性 && Enumerable.Range(0, 6).Count(d.道纹.有接口) == 1) > 战.掉落.地面.Count * .58 &&
                幸战.掉落.地面.Count(d => d.道纹.分类 == 道纹分类.属性 && Enumerable.Range(0, 6).Count(d.道纹.有接口) == 1) > 幸战.掉落.地面.Count * .58);
        }
    }
}
#endif
