#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 仅由用户明确要求后手动执行，不参与新开局、掉落、自动保存或打包。
public static class 天帝试玩道纹发放
{
    [Serializable] sealed class 权威参数 { public 功能参数 rune; public 等级参数 levels; }
    [Serializable] sealed class 功能参数 { public int function_grade, function_affix_count; }
    [Serializable] sealed class 等级参数 { public int player_max; }
    [Serializable] sealed class 回执
    {
        public string 存档路径, 备份目录, 修改前指纹, 修改后指纹;
        public int 原道纹数, 当前道纹数, 物品等级;
        public int[] 齐射, 分裂, 连锁;
    }
    static string 指纹(string 路径)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(路径))).Replace("-", "").ToLowerInvariant();
    }
    public static string 各二十()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式发放，避免覆盖正在游玩的进度。");
        if (天帝存档.验证目录 != null) throw new InvalidOperationException("当前启用了隔离存档，不能向真实进度发放。");
        // 此操作只发库存、不运行战斗或导出其它模块；核对所用规则，保留并行数值工作的全局Play/构建保护。
        var 内容 = File.ReadAllText(Path.Combine(天帝构建工具.项目根, "游戏数值配置.md"), Encoding.UTF8);
        var 块 = Regex.Match(内容, @"<!-- 数值配置开始 -->\s*```json\s*(.*?)\s*```\s*<!-- 数值配置结束 -->", RegexOptions.Singleline);
        if (!块.Success) throw new InvalidDataException("缺少权威数值配置。");
        var 参数 = JsonUtility.FromJson<权威参数>(块.Groups[1].Value);
        if (参数?.rune == null || 参数.levels == null || 参数.rune.function_grade != 3 || 参数.rune.function_affix_count != 1 ||
            参数.rune.function_grade != 天帝数值.取("rune.function_grade") || 参数.rune.function_affix_count != 天帝数值.取("rune.function_affix_count") ||
            参数.levels.player_max != 天帝数值.玩家上限) throw new InvalidOperationException("本次发放使用的品阶、词条数或等级规则尚未同步，未发放。");
        string 目录 = Path.Combine(天帝构建工具.项目根, "output/顺序功能道纹/试玩发放-20261006");
        string 回执路径 = Path.Combine(目录, "发放回执.json");
        if (File.Exists(回执路径)) return "本轮已发放，回执：" + 回执路径;
        var 存 = new 天帝存档(); var 数据 = 存.读取();
        if (数据 == null || !File.Exists(存.路径)) throw new InvalidOperationException("没有可用的当前存档，未发放。");
        天帝存档.校验(数据); var 网 = 天帝道纹.读取存档(数据.画布);
        var 回 = new 回执 { 存档路径 = 存.路径, 备份目录 = 目录, 原道纹数 = 网.道纹.Count, 物品等级 = 网.玩家等级, 修改前指纹 = 指纹(存.路径) };
        Directory.CreateDirectory(目录);
        File.Copy(存.路径, Path.Combine(目录, "天帝进度-发放前.json"), false);
        if (File.Exists(存.备份路径)) File.Copy(存.备份路径, Path.Combine(目录, "天帝进度-原备份.json"), false);
        var 随机 = new System.Random(20261006);
        int[] 增加(道纹功能 功能)
        {
            var 编号 = new int[20];
            for (int i = 0; i < 20; i++)
            {
                道纹实例 纹;
                do { 纹 = 天帝道纹生成.创建(1, 道纹分类.功能, 道纹品阶.稀有, 随机, 物品等级: 网.玩家等级); } while (纹.功能 != 功能);
                if (!网.获得道纹(纹)) throw new InvalidOperationException("库存拒绝发放，未提交存档。");
                编号[i] = 纹.编号;
            }
            return 编号;
        }
        回.齐射 = 增加(道纹功能.齐射); 回.分裂 = 增加(道纹功能.分裂); 回.连锁 = 增加(道纹功能.连锁);
        // 只追加这60枚的序列化条目，旧道纹、布局、技能点和资源沿用原数据。
        var 新画布 = 网.导出存档();
        数据.画布.道纹.AddRange(新画布.道纹.Where(x => 回.齐射.Contains(x.编号) || 回.分裂.Contains(x.编号) || 回.连锁.Contains(x.编号)));
        天帝存档.校验(数据);
        if (指纹(存.路径) != 回.修改前指纹) throw new InvalidOperationException("发放期间进度被其它进程更新，未提交存档。");
        if (!存.保存(数据)) throw new IOException(存.提示);
        var 读回 = 存.读取(); var 验证网 = 天帝道纹.读取存档(读回.画布);
        回.当前道纹数 = 验证网.道纹.Count;
        bool 组有效(int[] ids, 道纹功能 f) => ids.All(id => 验证网.道纹.Any(x => x.编号 == id && x.功能 == f &&
            x.品阶 == 道纹品阶.稀有 && x.词条.Count == 1 && !x.格子.HasValue && 天帝顺序道纹.定义有效(x.功能, x.接口)));
        if (回.当前道纹数 != 回.原道纹数 + 60 || !组有效(回.齐射, 道纹功能.齐射) || !组有效(回.分裂, 道纹功能.分裂) || !组有效(回.连锁, 道纹功能.连锁))
            throw new InvalidDataException("发放后回读不符合60枚要求，请检查备份。");
        回.修改后指纹 = 指纹(存.路径);
        File.WriteAllText(回执路径, JsonUtility.ToJson(回, true), new UTF8Encoding(false));
        return "已发放齐射/分裂/连锁各20枚，共60枚；库存 " + 回.原道纹数 + "→" + 回.当前道纹数 + "。回执：" + 回执路径;
    }
}
#endif
