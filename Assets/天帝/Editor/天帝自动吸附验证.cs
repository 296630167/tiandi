#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 隔离模型与预览场景，不读取、写入玩家真实存档或修改当前场景。
public static class 天帝自动吸附验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 设置(object 物, string 名, object 值) => 物.GetType().GetProperty(名).SetValue(物, 值);
    static bool 结算(object 落, 战斗敌人 敌) => (bool)落.GetType().GetMethod("敌人死亡", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(落, new object[] { 敌 });
    static 战斗敌人 死敌(Vector2 点)
    {
        var 敌 = new 战斗敌人(new 战斗敌人布点(战斗敌人级别.王级, 点), 战斗难度.普通);
        设置(敌, "已生成", true); 设置(敌, "血量", 0f); return 敌;
    }
    public static string 运行()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请在编辑模式运行自动吸附验证。");
        结果 = new 报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/自动吸附-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录);
        try { 模型与表现(目录); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true)); return 目录;
    }
    static void 模型与表现(string 目录)
    {
        var 地图 = new 天帝战斗地图(42, true); var 寻路 = new 天帝战斗寻路(地图);
        Vector2 远处 = Vector2.zero; bool 找到 = false;
        for (int y = 0; y < 20 && !找到; y++) for (int x = 0; x < 20 && !找到; x++)
        {
            var 点 = 地图.格中心(x, y);
            if (地图.可站立(点) && 点.sqrMagnitude > 400 && !寻路.无遮挡(Vector2.zero, 点)) { 远处 = 点; 找到 = true; }
        }
        检查("夹具在20米外且被地形遮挡", 找到);
        if (!找到) throw new InvalidOperationException("未找到远处隔墙夹具。");
        var 网 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); var 钱 = new 天帝通货(网, 42);
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
        {
            var 战 = new 天帝战斗系统(地图, 网, 人, 战斗难度.普通, 钱);
            int 道纹提示 = 0, 通货提示 = 0;
            战.掉落.获得道纹 += _ => { 道纹提示++; 战.掉落.自动拾取(); };
            战.通货掉落.获得通货 += (_, n) => { 通货提示++; 战.通货掉落.自动拾取(); };
            var 敌 = 战.敌人.Single(e => e.布点.级别 == 战斗敌人级别.王级);
            设置(敌, "已生成", true); 设置(敌, "位置", 远处);
            检查("真实伤害流程击杀远处BOSS", 战.伤害敌人(敌, 100000));
            var 纹 = 战.掉落.地面.Single(); var 货 = 战.通货掉落.地面.Single();
            检查("道纹无需移动即入库且仍播放吸附", 纹.已拾取 && 网.道纹.Contains(纹.道纹) && !纹.演出结束);
            检查("通货无需移动即入库且仍播放吸附", 货.已拾取 && 钱.数量(货.种类) == 货.数量 && !货.演出结束);
            检查("逐条提示及重入回调各通知一次", 道纹提示 == 1 && 通货提示 == 1);
            检查("重复死亡或拾取不复制奖励", !战.伤害敌人(敌, 1) && !结算(战.掉落, 敌) && !结算(战.通货掉落, 敌) &&
                战.掉落.拾取附近(new Vector2(1000, 1000), true) == 0 && 战.通货掉落.拾取附近(new Vector2(1000, 1000), true) == 0 && 道纹提示 == 1 && 通货提示 == 1);
            var 档 = new 天帝存档(Path.Combine(目录, "隔离存档"));
            检查("飞行尚未完成即可保存离场奖励", 档.保存(new 天帝存档数据 { 序章已完成 = true, 主角 = 人.导出配置(), 画布 = 网.导出存档(), 通货 = 钱.导出库存() }));
            var 回读 = 档.读取(); var 回网 = 天帝道纹.读取存档(回读.画布); var 回钱 = new 天帝通货(回网, 1); 回钱.读取库存(回读.通货);
            检查("再次读取保留道纹和通货", 回网.道纹.Count == 1 && 回钱.数量(货.种类) == 货.数量);

            var 预览 = EditorSceneManager.NewPreviewScene(); GameObject 根 = null; 天帝美术资源 素材 = null;
            try
            {
                素材 = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset"));
                素材.主角移动动画 = null; 根 = new GameObject("自动吸附隔离预览"); SceneManager.MoveGameObjectToScene(根, 预览);
                var 美术 = new 天帝战斗美术(根.transform, 素材, 地图, 战, false);
                美术.更新(战, Vector2.zero, 0);
                var 图 = 根.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("地面道纹") || s.name.StartsWith("地面通货")).ToArray();
                检查("真实美术创建道纹壳属性图与通货", 美术.可用 && 图.Length == 3 && 图.All(s => s.enabled));
                战.伤害玩家(100000); 战.推进(Vector2.zero, .2f); 美术.更新(战, Vector2.zero, 0);
                检查("玩家死亡后吸附继续且奖励保留", 战.玩家死亡 && !纹.演出结束 && 纹.显示位置 != 纹.位置 && 网.道纹.Count == 1 && 钱.数量(货.种类) == 货.数量);
                检查("精灵跟随吸附位置而非原掉落地点", 图.All(s => (new Vector2(s.transform.position.x, s.transform.position.z) - (s.name.StartsWith("地面通货") ? 货.显示位置 : 纹.显示位置)).sqrMagnitude < .0001f));
                var 比例 = 图.Select(s => s.transform.localScale).ToArray(); 美术.更新(战, Vector2.zero, 0);
                检查("重复绘制不累乘缩小", 图.Select(s => s.transform.localScale).SequenceEqual(比例));
                var 新位置 = new Vector2(.5f, 0); 战.推进(新位置, .2f); 美术.更新(战, 新位置, 0);
                检查("飞行持续接近移动后的玩家", (纹.显示位置 - 新位置).sqrMagnitude < (远处 - 新位置).sqrMagnitude && 图[0].transform.localScale.x < 比例[0].x);
                战.推进(新位置, .25f); 美术.更新(战, 新位置, 0);
                检查("最终到达玩家并隐藏全部掉落图", 纹.演出结束 && 货.演出结束 && 纹.显示位置 == 新位置 && 货.显示位置 == 新位置 && 图.All(s => !s.enabled));
            }
            finally
            {
                if (根 != null)
                {
                    var 材质 = 根.GetComponentsInChildren<SpriteRenderer>().Select(s => s.sharedMaterial).Where(m => m != null).Distinct().ToArray();
                    UnityEngine.Object.DestroyImmediate(根); foreach (var 项 in 材质) UnityEngine.Object.DestroyImmediate(项);
                }
                if (素材 != null) UnityEngine.Object.DestroyImmediate(素材); EditorSceneManager.ClosePreviewScene(预览);
            }
        }
        var 满钱 = new 天帝通货(网, 1, int.MaxValue); var 落 = new 天帝通货掉落(地图, 网, 满钱, 战斗难度.普通);
        结算(落, 死敌(远处)); var 等待 = 落.地面.Single();
        检查("库存拒收时保留掉落不吞物品", !等待.已拾取 && 等待.吸附 == null && 落.拾取总量 == 0);
        满钱.读取库存(new int[13]); 检查("库存恢复可用后自动重试成功", 落.自动拾取() == 1 && 等待.已拾取 && 满钱.数量(等待.种类) == 等待.数量);
        var 无库存 = new 天帝通货掉落(地图, 网, null, 战斗难度.普通); 结算(无库存, 死敌(远处));
        检查("无通货库存时安全保留掉落", 无库存.自动拾取() == 0 && !无库存.地面.Single().已拾取);
    }
}
#endif
