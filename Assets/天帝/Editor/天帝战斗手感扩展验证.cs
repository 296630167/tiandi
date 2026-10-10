#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 只使用隔离战斗模型，验证目标粘滞、主动预警和自动技能节奏，不修改存档。
public static class 天帝战斗手感扩展验证
{
    static readonly BindingFlags 私有 = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static 天帝真实数值验证.报告 报告;
    static void 检查(string 名称, bool 通过) => (通过 ? 报告.通过 : 报告.失败).Add(名称);
    static void 写(object 对象, string 名称, object 值)
    {
        var 属性 = 对象.GetType().GetProperty(名称, 私有);
        if (属性 != null) { 属性.SetValue(对象, 值); return; }
        对象.GetType().GetField(名称, 私有)?.SetValue(对象, 值);
    }

    [MenuItem("天帝/验证/战斗手感扩展")]
    public static void 菜单运行() => Debug.Log("战斗手感扩展验证：" + 运行());

    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证隔离战斗模型。");
        报告 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/战斗手感扩展-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        try { 目标粘滞(); 主动预警(); 自动轮换(); 命中停顿上限(); }
        catch (Exception 异常) { 报告.错误.Add(异常.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(报告, true));
        return 目录;
    }

    static 天帝战斗系统 新战斗(天帝道纹 网, out 天帝主角属性 人, out 天帝战斗地图 地)
    {
        地 = new 天帝战斗地图(42, true);
        人 = new 天帝主角属性(天帝普攻.主角配置(), 网);
        return new 天帝战斗系统(地, 网, 人, 战斗难度.普通);
    }

    static void 目标粘滞()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(50);
        var 战 = 新战斗(网, out var 人, out var 地);
        try
        {
            战.设置演示靶(new[] { Vector2.right * 6, Vector2.right * 7 });
            var 第一 = 战.最近目标;
            写(战.敌人[0], "位置", 地.出生位置 + Vector2.right * 7);
            写(战.敌人[1], "位置", 地.出生位置 + Vector2.right * 5);
            bool 保持 = ReferenceEquals(第一, 战.最近目标);
            // 直接推进模型时钟，避免演示靶的AI移动干扰本项对“粘滞过期”的断言。
            double 粘滞截止 = (double)战.GetType().GetField("索敌粘滞结束", 私有).GetValue(战);
            写(战, "战斗时钟", 粘滞截止 + .01d);
            bool 切换 = ReferenceEquals(战.敌人[1], 战.最近目标);
            检查("目标粘滞窗口内保持锁定", 保持);
            var 时钟 = (double)战.GetType().GetField("战斗时钟", 私有).GetValue(战);
            var 粘滞目标 = (int)战.GetType().GetField("索敌粘滞目标", 私有).GetValue(战);
            var 粘滞结束 = (double)战.GetType().GetField("索敌粘滞结束", 私有).GetValue(战);
            检查("目标粘滞过期后切换到更近敌人 d0=" + Vector2.Distance(地.出生位置, 战.敌人[0].位置).ToString("0.00")
                + " d1=" + Vector2.Distance(地.出生位置, 战.敌人[1].位置).ToString("0.00")
                + " now=" + 时钟.ToString("0.00") + " target=" + 粘滞目标 + " end=" + 粘滞结束.ToString("0.00"), 切换);
        }
        finally { 战.清理特性战斗(); 战.战术.清理(); }
    }

    static void 主动预警()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.余响)); 网.设置玩家等级(50);
        var 战 = 新战斗(网, out var 人, out var 地);
        try
        {
            战.设置演示靶(Array.Empty<Vector2>());
            var 结果 = 战.尝试释放技能(0, Vector2.up, 地.出生位置);
            bool 建立 = 结果 == 战斗操作结果.成功 && 战.技能预警剩余 > 0 && 战.技能预警槽 == 0
                && Vector2.Dot(战.技能预警方向, Vector2.up) > .999f;
            战.推进(地.出生位置, .25f);
            检查("技能释放建立方向与槽位预警", 建立);
            检查("技能预警在前摇结束后清除", 战.技能预警剩余 <= .001f);
        }
        finally { 战.清理特性战斗(); 战.战术.清理(); }
    }

    static void 自动轮换()
    {
        var 构筑 = typeof(天帝画布通路验证).GetMethod("双路", BindingFlags.Static | BindingFlags.NonPublic);
        if (构筑 == null) throw new MissingMethodException("双路通路夹具不存在");
        var 网 = (天帝道纹)构筑.Invoke(null, new object[] { 天赋种类.普通人 });
        var 战 = 新战斗(网, out var 人, out var 地);
        try
        {
            战.设置演示靶(new[] { Vector2.right * 6, Vector2.right * 7 });
            写(战, "演示模式", false);
            foreach (var 敌 in 战.敌人) 写(敌, "登场剩余秒", 100f);
            // 关闭兼容自动攻击开关，只打开正式场景使用的纯 AI 模式，验证默认战斗路径本身。
            战.纯AI模式 = true;
            战.自动攻击启用 = false;
            var 通路 = new List<int>();
            战.射击释放 += _ => 通路.Add(战.当前普攻.通路);
            for (int i = 0; i < 480; i++) 战.推进(地.出生位置, .025f);
            bool 多链路 = 通路.Distinct().Count() > 1;
            bool 无即时重复 = 多链路 && 通路.Zip(通路.Skip(1), (前, 后) => 前 != 后).All(x => x);
            检查("多链路自动释放实际轮换", 多链路);
            检查("自动技能节奏避免即时重复", 无即时重复);
        }
        finally { 战.清理特性战斗(); 战.战术.清理(); }
    }

    static void 命中停顿上限()
    {
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); 网.设置玩家等级(50);
        var 战 = 新战斗(网, out var 人, out var 地);
        try
        {
            战.请求命中停顿(.5f);
            检查("重击确认帧有界且不超过0.06秒", 战.命中停顿剩余秒 > .059f && 战.命中停顿剩余秒 <= .0601f);
            战.推进(地.出生位置, .08f);
            检查("命中确认帧按战斗时钟自然结束", 战.命中停顿剩余秒 <= .001f);
        }
        finally { 战.清理特性战斗(); 战.战术.清理(); }
    }
}
#endif
