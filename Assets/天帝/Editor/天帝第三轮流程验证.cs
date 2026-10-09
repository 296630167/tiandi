#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 独立模型和隐藏UI，不运行游戏Awake，也不读取或写入玩家进度。
public static class 天帝第三轮流程验证
{
    static 天帝真实数值验证.报告 结果;
    static readonly BindingFlags 私有实例 = BindingFlags.Instance | BindingFlags.NonPublic;
    static void 检查(string 名, bool 成功) => (成功 ? 结果.通过 : 结果.失败).Add(名);
    static void 设(object 目标, string 名, object 值) => 目标.GetType().GetProperty(名).SetValue(目标, 值);
    static void 调用(object 目标, string 名) => 目标.GetType().GetMethod(名, 私有实例).Invoke(目标, null);

    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证隔离流程。");
        结果 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/第三轮流程-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(目录);
        var 原移动 = 天帝移动适配.验证移动平台;
        var 原美术 = 天帝美术资源.当前;
        try
        {
            指引边界(); 宝盒快照();
            typeof(天帝美术资源).GetProperty("当前").SetValue(null, AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset"));
            foreach (bool 手机 in new[] { false, true }) 页面流程(手机);
        }
        catch (Exception 异常) { 结果.错误.Add(异常.ToString()); }
        finally { 天帝移动适配.验证移动平台 = 原移动; typeof(天帝美术资源).GetProperty("当前").SetValue(null, 原美术); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        Debug.Log("第三轮流程验证：" + 目录 + "；通过 " + 结果.通过.Count + "，失败 " + 结果.失败.Count + "，错误 " + 结果.错误.Count);
        return 目录;
    }

    static 天帝道纹 新网() => new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
    static 道纹实例 属性纹(天帝道纹 网, int 种子)
    {
        var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(种子), 道纹属性分组.基础);
        纹.接口 = 9; 网.获得道纹(纹); return 纹;
    }
    static void 指引边界()
    {
        var 网 = 新网(); var 首 = 属性纹(网, 42); var 盒 = new 天帝宝盒(网, 42, 500);
        检查("指引-首次构筑仍引导解锁右格", 天帝修行指引.下一步(网, 盒, 1).Contains("解锁源纹右侧格"));
        检查("指引-独立夹具解锁放置右格", 网.解锁格子(Vector2Int.right) && 网.放置(首, Vector2Int.right) && 首.生效);
        网.设置玩家等级(2); 属性纹(网, 43);
            string 下一步 = 天帝修行指引.下一步(网, 盒, 1);
        检查("指引-占用右格后建议可执行的相邻扩展", 下一步.Contains("已解锁格旁边") && !下一步.Contains("解锁源纹右侧格"));
        检查("指引-建议相邻扩展时确有可解锁格", 网.所有解锁格.Any(格 => 天帝道纹.邻向.Any(方向 => 网.可解锁格子(格 + 方向))));
        网.解锁格子(new Vector2Int(2, 0));
        属性纹(网, 44); // 保持待放状态，触发“已有空格”分支。
        检查("指引-已有空格优先使用空格", 天帝修行指引.下一步(网, 盒, 1).Contains("已解锁空格"));
    }

    static void 宝盒快照()
    {
        var 快照方法 = typeof(天帝界面).GetMethod("道纹快照", BindingFlags.Static | BindingFlags.NonPublic);
        for (int 编号 = 1; 编号 <= 天帝顺序道纹.功能数量; 编号++)
        {
            var 原 = 天帝道纹生成.创建(1, 道纹分类.功能, 道纹品阶.稀有, new System.Random(编号 + 42));
            设(原, "功能", (道纹功能)编号);
            设(原, "入口方向", 编号 % 6);
            原.接口 = 天帝顺序道纹.固定接口(原.功能, 原.入口方向);
            原.词条.Clear(); 原.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(原.功能), 1));
            原.介绍 = 天帝顺序道纹.功能摘要(原.功能);
            var 副 = (道纹实例)快照方法.Invoke(null, new object[] { 原 });
            检查("宝盒快照-" + 原.功能 + "名称与功能身份一致", 副.名称 == 原.名称 && 副.是顺序功能 && 副.功能 == 原.功能 && 副.入口方向 == 原.入口方向);
            检查("宝盒快照-" + 原.功能 + "完整详情保留机制", 副.候选说明 == 原.候选说明 && 副.详情文字() == 原.详情文字());
            double 原值 = 原.词条[0].实际数值;
            副.词条.Clear(); 副.接口 = 1;
            检查("宝盒快照-" + 原.功能 + "日志独立于背包实例", 原.词条.Count == 1 && 原.词条[0].实际数值 == 原值 && 原.接口 != 副.接口);
        }
        foreach (var 分类 in new[] { 道纹分类.特性, 道纹分类.转化 })
        {
            var 原 = 天帝特性道纹.创建(1, 分类, 1, 道纹品阶.稀有, 10, 0);
            var 副 = (道纹实例)快照方法.Invoke(null, new object[] { 原 });
            检查("宝盒快照-" + 分类 + "编号与名称保留", 副.特性编号 == 原.特性编号 && 副.名称 == 原.名称);
        }
    }

    static void 页面流程(bool 手机)
    {
        天帝移动适配.验证移动平台 = 手机;
        string 前 = 手机 ? "Android返回" : "PC返回共用处理";
        GameObject 假游戏 = null, 根 = null;
        天帝主角属性 人 = null;
        try
        {
            var 网 = 新网(); 属性纹(网, 42); 属性纹(网, 43);
            var 盒 = new 天帝宝盒(网, 42, 500); var 钱 = new 天帝通货(网, 42);
            人 = new 天帝主角属性(天帝普攻.主角配置(), 网);
            假游戏 = new GameObject("第三轮独立流程游戏"); 假游戏.SetActive(false); 假游戏.hideFlags = HideFlags.HideAndDontSave;
            var 游戏 = 假游戏.AddComponent<天帝游戏>();
            游戏.默认字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            游戏.美术 = 天帝美术资源.当前;
            设(游戏, "阶段", 游戏阶段.主页); 设(游戏, "道纹数据", 网); 设(游戏, "宝盒数据", 盒); 设(游戏, "通货数据", 钱); 设(游戏, "主角属性", 人);
            var 界面 = new 天帝界面(游戏); 设(游戏, "界面", 界面);
            根 = (GameObject)typeof(天帝界面).GetField("根", 私有实例).GetValue(界面);
            根.transform.SetParent(null, false); 根.hideFlags = HideFlags.HideAndDontSave;
            根.GetComponent<CanvasScaler>().enabled = false; 根.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var 隐藏 = 根.AddComponent<CanvasGroup>(); 隐藏.alpha = 0; 隐藏.blocksRaycasts = false;
            界面.显示回收(); var 回收 = 界面.回收页; 回收.一键选中();
            int 原选择 = 回收.选中数量; var 原范围 = 回收.当前批选范围;
            回收.打开范围();
            检查(前 + "-范围设置打开且保留选择", 回收.范围已打开 && 原选择 == 2);
            var 类型 = 回收.GetComponentsInChildren<Button>(true).FirstOrDefault(键 => 键.name == "批选道纹类型" && 键.gameObject.activeSelf);
            if (类型 != null) 类型.onClick.Invoke();
            else 回收.GetComponentsInChildren<Dropdown>(true).First(下拉 => 下拉.name == "批选类型下拉").value = 2;
            void 返回() { if (手机) 游戏.触屏返回(); else 调用(游戏, "返回回收上一层"); }
            返回();
            检查(前 + "-第一次只关闭范围小窗", 界面.回收已打开 && ReferenceEquals(回收, 界面.回收页) && !回收.范围已打开);
            检查(前 + "-取消范围草稿不改已应用条件", 回收.当前批选范围.类型 == 原范围.类型 && 回收.当前批选范围.位置 == 原范围.位置 && 回收.当前批选范围.品阶掩码 == 原范围.品阶掩码);
            检查(前 + "-取消范围保留原选择", 回收.选中数量 == 原选择);
            检查(前 + "-范围关闭后重复关闭无操作", !回收.关闭范围());
            调用(回收, "打开确认"); 返回();
            检查(前 + "-确认回收仍按层返回", 界面.回收已打开 && !回收.确认已打开 && 回收.选中数量 == 原选择);
            返回(); 检查(前 + "-没有嵌套弹窗时关闭回收页", !界面.回收已打开 && 界面.回收页 == null);

            var 解封网 = 新网(); var 方向 = new List<string>(); int 原接口 = 1;
            for (int 级 = 10; 级 <= 50; 级 += 10)
            {
                解封网.设置玩家等级(级); int 现接口 = 解封网.已放置[Vector2Int.zero].接口;
                int 新口 = Enumerable.Range(0, 6).Single(序 => ((现接口 ^ 原接口) & 1 << 序) != 0);
                方向.Add(天帝道纹.方向名[新口]); 原接口 = 现接口;
            }
            设(游戏, "天赋池", new 天帝天赋池(42));
            界面.显示源道纹选择(false);
            var 文案 = 界面.源道纹页.GetComponentsInChildren<Text>(true).Select(文 => 文.text).ToArray();
            string 旧顺序 = "依次解封" + string.Join("、", 方向);
            string 新顺序 = "解锁顺序：" + string.Join("→", 方向);
            检查((手机 ? "手机" : "PC") + "-天赋页解封顺序与实际接口一致", 文案.Any(文 => 文.Contains(旧顺序) || 文.Contains(新顺序)));
        }
        finally
        {
            if (根 != null) UnityEngine.Object.DestroyImmediate(根);
            if (假游戏 != null) UnityEngine.Object.DestroyImmediate(假游戏);
            人?.Dispose();
        }
    }
}
#endif
