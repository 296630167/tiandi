#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class 天帝第三轮寻路验证
{
    static readonly BindingFlags 私有实例 = BindingFlags.Instance | BindingFlags.NonPublic;
    static 天帝真实数值验证.报告 结果;
    static void 检查(string 名称, bool 通过) => (通过 ? 结果.通过 : 结果.失败).Add(名称);
    static void 设置(object 目标, string 名称, object 值)
        => 目标.GetType().GetField(名称, 私有实例).SetValue(目标, 值);
    static float 寻路等待(战斗敌人 敌)
        => (float)typeof(战斗敌人).GetField("再寻路", 私有实例).GetValue(敌);
    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证隔离寻路模型。");
        结果 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/第三轮寻路-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        try { 拥挤绕墙(); }
        catch (Exception 异常) { 结果.错误.Add(异常.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        Debug.Log("第三轮寻路验证：" + 目录 + "；通过 " + 结果.通过.Count + "，失败 " + 结果.失败.Count + "，错误 " + 结果.错误.Count);
        return 目录;
    }
    static void 拥挤绕墙()
    {
        var 图 = new 天帝战斗地图(42);
        var 寻 = new 天帝战斗寻路(图);
        var 路 = new List<Vector2>();
        Vector2 起 = Vector2.zero, 终 = Vector2.zero;
        bool 找到 = false;
        for (int y = 1; y < 18 && !找到; y++) for (int x = 1; x < 18 && !找到; x++)
        {
            var 格 = new Vector2Int(x, y);
            if (!图.可通行格(格) || 图.王房范围.Contains(格)) continue;
            foreach (var 偏 in new[] { new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(-2, 1) })
            {
                var 另格 = 格 + 偏;
                if (!图.可通行格(另格) || 图.王房范围.Contains(另格)) continue;
                起 = 图.格中心(x, y); 终 = 图.格中心(另格.x, 另格.y);
                if (!寻.无遮挡(起, 终) && 寻.路径(起, 终, 路, false, true) && 路.Count <= 6)
                { 找到 = true; break; }
            }
        }
        检查("寻路帧预算-夹具为真实隔墙可绕通路", 找到);
        if (!找到) return;
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(图, 网, 人, 战斗难度.普通) { 自动攻击启用 = false };
            var 场景物 = new GameObject("第三轮寻路隔离场景");
            场景物.SetActive(false);
            var 场景 = 场景物.AddComponent<天帝战斗场景>();
            typeof(天帝战斗场景).GetProperty("战斗").SetValue(场景, 战);
            typeof(天帝战斗场景).GetProperty("玩家位置").SetValue(场景, 终);
            try
            {
                var 列 = (List<战斗敌人>)战.敌人;
                列.Clear();
                for (int 序 = 0; 序 < 20; 序++)
                {
                    var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.普通, 起), 战斗难度.普通);
                    typeof(战斗敌人).GetProperty("已生成").SetValue(敌, true);
                    typeof(战斗敌人).GetProperty("行动").SetValue(敌, 敌人行动.追击);
                    设置(敌, "最后目标", 终); 设置(敌, "序号", 序);
                    列.Add(敌);
                }
                var 推进模型 = typeof(天帝战斗场景).GetMethod("推进战斗模型", 私有实例);
                void 低帧率一帧() => 推进模型.Invoke(场景, new object[] { .25f });
                低帧率一帧();
                int 已请求 = 列.Count(敌 => 寻路等待(敌) > 0);
                检查("寻路帧预算-250毫秒十子步整帧仅四次请求", 战.本次寻路次数 == 4 && 已请求 == 4);
                检查("寻路帧预算-超额敌人保留等待且全部位置合法", 已请求 < 列.Count && 列.All(敌 => 图.可站立(敌.位置)));
                低帧率一帧();
                检查("寻路帧预算-下一帧重新领取四次并服务等待敌人", 战.本次寻路次数 == 4 && 列.Count(敌 => 寻路等待(敌) > 0) == 8);
                战.推进(终, .025f);
                检查("寻路帧预算-独立推进兼容原来的四次额度", 战.本次寻路次数 == 4 && 列.Count(敌 => 寻路等待(敌) > 0) == 12);
                战.推进(终, 0);
                检查("寻路帧预算-零时间不重置已完成帧统计", 战.本次寻路次数 == 4);
                double 时钟 = (double)typeof(天帝战斗系统).GetField("战斗时钟", 私有实例).GetValue(战);
                检查("寻路帧预算-共享额度不减少十子步战斗时间", Math.Abs(时钟 - .525) < .000001);
            }
            finally
            {
                typeof(天帝战斗场景).GetProperty("战斗").SetValue(场景, null);
                UnityEngine.Object.DestroyImmediate(场景物);
                战.清理特性战斗(); 战.战术.清理();
            }
        }
    }
}
#endif
