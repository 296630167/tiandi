#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// 只创建隔离的正式数据，覆盖屏外选点、延迟补刷和完整波次，不改玩家存档。
public static class 天帝战斗视野验证
{
    public static string 运行()
    {
        天帝数值同步检查.校验();
        var 结果 = new 天帝真实数值验证.报告();
        void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
        var 地图 = new 天帝战斗地图(42, true); var 路 = new 天帝战斗寻路(地图);
        var 路线 = new List<Vector2>(); var 空敌 = new List<战斗敌人>();
        Vector2? 首王点 = null;
        foreach (var 玩家 in new[] { Vector2.zero, new Vector2(-23, 0), new Vector2(25, 0), new Vector2(0, 32), new Vector2(0, -28) })
        {
            检查("玩家边界输入可站立 " + 玩家, 地图.可站立(玩家));
            foreach (float 宽高比 in new[] { 16f / 9, 21f / 9 })
            {
                float 半宽 = 14 * 宽高比;
                var 中 = new Vector2(Mathf.Clamp(玩家.x, -40 + 半宽, 40 - 半宽), Mathf.Clamp(玩家.y, -26, 26));
                var 视野 = new Rect(中.x - 半宽, 中.y - 14, 半宽 * 2, 28);
                var 刷新 = new 天帝敌人刷新(地图, 路, 空敌, () => 视野);
                foreach (var 类 in new[] { 战斗敌人级别.普通, 战斗敌人级别.精英, 战斗敌人级别.头目 })
                {
                    bool 找到 = 刷新.尝试选点(类, 玩家, out var 点); string 名 = 玩家 + "/" + 宽高比 + "/" + 类;
                    检查("屏外选点成功 " + 名, 找到);
                    float 边 = (float)天帝数值.取("map.spawn_offscreen_margin");
                    检查("出生含精灵余量在屏外 " + 名, 找到 && !Rect.MinMaxRect(视野.xMin - 边, 视野.yMin - 边, 视野.xMax + 边, 视野.yMax + 边).Contains(点));
                    检查("出生点可通行并能走到玩家 " + 名, 找到 && 地图.可站立(点, .65f) && 路.路径(点, 玩家, 路线, false, false));
                }
                bool 有王 = 刷新.尝试选点(战斗敌人级别.王级, 玩家, out var 王点);
                检查("BOSS固定在北端且可追击 " + 玩家 + "/" + 宽高比, 有王 && 王点.y >= 30 && 地图.可站立(王点, 1.2f) && 路.路径(王点, 玩家, 路线, false, false) && (!首王点.HasValue || 王点 == 首王点.Value));
                首王点 = 王点;
            }
        }
        Rect 当前视野 = new Rect(-100, -100, 200, 200);
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地图, 网, 人, 战斗难度.普通, new 天帝通货(网, 42), 1, () => 当前视野);
            检查("全图可见不硬刷到屏内", 战.场上敌人数量 == 0 && 战.未生成敌人数量 == 66);
            for (int i = 0; i < 60; i++) 战.推进(Vector2.zero, .25f);
            检查("无安全点保留首波不漏怪不跳波", 战.当前波次 == 1 && 战.场上敌人数量 == 0 && 战.未生成敌人数量 == 66 && 战.敌人.Count(敌 => 敌.登场波次 == 1) == 8);
            当前视野 = new Rect(-14 * 16f / 9, -14, 28 * 16f / 9, 28);
            for (int i = 0; i < 2; i++) 战.推进(Vector2.zero, .25f);
            检查("恢复视野补齐预留的八只小怪", 战.分类在场(战斗敌人级别.普通) == 8 && 战.未生成敌人数量 == 58);
            var 首波 = 战.敌人.Where(敌 => 敌.存活).ToArray(); float 距前 = 首波.Sum(敌 => 敌.位置.magnitude);
            for (int i = 0; i < 16; i++) 战.推进(Vector2.zero, .1f);
            检查("远处怪物主动向玩家移动", 首波.Sum(敌 => 敌.位置.magnitude) < 距前 - 3);
            检查("首波间距且每只均有可走路线", 首波.All(敌 => 路.路径(敌.位置, Vector2.zero, 路线, false, false)));
            当前视野 = new Rect(-100, -100, 200, 200);
            foreach (var 敌 in 首波) 战.伤害敌人(敌, 敌.最大血量 * 10);
            for (int i = 0; i < 20; i++) 战.推进(Vector2.zero, .25f);
            检查("强敌无屏外点也不丢失或跳波", 战.当前波次 == 1 && 战.分类在场(战斗敌人级别.精英) == 0 && 战.分类剩余(战斗敌人级别.精英) == 4);
            当前视野 = new Rect(-14 * 16f / 9, -14, 28 * 16f / 9, 28);
            for (int i = 0; i < 2; i++) 战.推进(Vector2.zero, .25f);
            检查("恢复后精英正常登场", 战.分类在场(战斗敌人级别.精英) == 1);
            bool 超限 = false;
            for (int i = 0; i < 400 && !战.BOSS已出现; i++)
            {
                foreach (var 敌 in 战.敌人) if (敌.存活 && 敌.布点.级别 != 战斗敌人级别.王级) 战.伤害敌人(敌, 敌.最大血量 * 10);
                战.推进(Vector2.zero, .25f); 超限 |= 战.场上敌人数量 > 天帝地图挑战.场上上限 + (战.BOSS已出现 ? 1 : 0);
            }
            var 王 = 战.敌人.Single(敌 => 敌.布点.级别 == 战斗敌人级别.王级);
            检查("实际80%损伤触发北端BOSS", 战.BOSS已出现 && 王.存活 && 王.位置.y >= 30 && 战.敌人损伤比例 >= .8f);
            检查("波次补刷不超场上上限", !超限);
            检查("剩余统计始终含待刷新与场上", 战.剩余敌人数量 == 战.未生成敌人数量 + 战.场上敌人数量 && 战.击败数 + 战.剩余敌人数量 == 66);
            float 王距 = 王.位置.magnitude;
            for (int i = 0; i < 16; i++) 战.推进(Vector2.zero, .1f);
            检查("BOSS从北端主动追向玩家", 王.位置.magnitude < 王距 - 1);
            战.伤害玩家((float)天帝数值.取("damage.technical_hit_max")); int 未生成 = 战.未生成敌人数量;
            for (int i = 0; i < 20; i++) 战.推进(Vector2.zero, .25f);
            检查("玩家死亡立即停止补刷", 战.玩家死亡 && 战.未生成敌人数量 == 未生成);
        }
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/战斗视野-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        return 目录;
    }
}
#endif
