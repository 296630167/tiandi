using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class 道纹存档实例
{
    public int 编号;
    public 道纹分类 分类;
    public 道纹品阶 品阶;
    public int 接口;
    public int 物品等级;
    public 道纹功能 功能;
    public int 特性编号;
    public int 入口方向;
    public List<道纹词条> 词条 = new List<道纹词条>();
    public string 特殊效果, 介绍;
    public bool 已放置;
    // 旧存档缺省false，沿用原有保护规则。
    public bool 回收锁定;
    public Vector2Int 格子;
}

[Serializable]
public sealed class 道纹存档数据
{
    // 旧档缺省0表示100×100；新档记录尺寸，避免越界退款在重读时重复发生。
    public int 画布边长;
    public int 天赋编号 = -1;
    public int 玩家等级, 技能点;
    public int 当前经验, 迁移前等级;
    public List<Vector2Int> 解锁格 = new List<Vector2Int>();
    public List<道纹存档实例> 道纹 = new List<道纹存档实例>();
    // 可选字段，旧存档缺省为空；方案只记录实例编号、位置与朝向。
    public List<道纹布局方案> 布局方案 = new List<道纹布局方案>();
}

[Serializable]
public sealed class 天帝存档数据
{
    public int 版本 = 天帝存档.当前版本;
    public string 数值版本 = 天帝数值配置.版本;
    public string 保存时间;
    public bool 序章已完成;
    // 仅新创建的角色设true；旧存档缺省false，不打断老玩家。
    public bool 新手指引待完成;
    public 主角属性配置 主角;
    public 道纹存档数据 画布;
    public int[] 通货;
    public int 灵石;
    public bool 无限灵石;
    public bool 无限通货;
    public int 地图编号;
    public int 地图等级 = 1;
    public 战斗难度 难度;
}

// 只保存角色成长和主页状态；读取后回到主页，不恢复战场中的敌人与掉落。
public sealed class 天帝存档
{
    public const int 当前版本 = 3;
    public string 路径 { get; }
    public string 备份路径 => 路径 + ".bak";
    public bool 有文件 => File.Exists(路径) || File.Exists(备份路径);
#if UNITY_EDITOR
    public static string 验证目录;
#endif
    public string 提示 { get; private set; } = "";
    public 天帝存档(string 目录 = null)
    {
#if UNITY_EDITOR
        目录 = 目录 ?? 验证目录;
#endif
        路径 = Path.Combine(目录 ?? Application.persistentDataPath, "天帝进度.json");
    }
    static bool 有限非负(float 值) => !float.IsNaN(值) && !float.IsInfinity(值) && 值 >= 0;
    static bool 配置有效(主角衍生属性配置 值) => 值 != null && 有限非负(值.基础值) &&
        有限非负(值.力量系数) && 有限非负(值.速度系数) && 有限非负(值.智力系数);
    public static void 校验(天帝存档数据 数据)
    {
        if (数据 == null || 数据.版本 < 1 || 数据.版本 > 当前版本) throw new InvalidDataException("存档版本不受支持");
        if (数据.灵石 < 0) throw new InvalidDataException("灵石余额无效");
        var 人 = 数据.主角;
        if (人 == null || string.IsNullOrWhiteSpace(人.名字) || !有限非负(人.力量) || !有限非负(人.速度) || !有限非负(人.智力) ||
            !配置有效(人.血量) || !配置有效(人.灵力) || !配置有效(人.防御) || !配置有效(人.灵气护盾) ||
            !配置有效(人.移动速度) || !配置有效(人.跑步速度) || !配置有效(人.攻击力)) throw new InvalidDataException("角色数据不完整");
        if (数据.地图编号 != 0 || (数据.难度 != 战斗难度.普通 && 数据.难度 != 战斗难度.困难)) throw new InvalidDataException("地图配置不受支持");
        if (!天帝地图挑战.等级有效(数据.地图等级)) throw new InvalidDataException("地图等级超出1至100级");
        var 网 = 天帝道纹.读取存档(数据.画布);
        var 钱 = new 天帝通货(网, 1); 钱.读取库存(数据.通货, 数据.无限通货);
    }
    bool 尝试读取(string 文件, out 天帝存档数据 数据, out string 原因)
    {
        数据 = null; 原因 = "";
        try
        {
            if (!File.Exists(文件)) return false;
            if (new FileInfo(文件).Length > 16 * 1024 * 1024) throw new InvalidDataException("存档文件过大");
            var 值 = JsonUtility.FromJson<天帝存档数据>(File.ReadAllText(文件, Encoding.UTF8));
            // 旧存档没有地图等级字段；缺省0迁移为第一档，不改变角色或库存。
            if (值 != null && 值.地图等级 == 0) 值.地图等级 = 1;
            迁移(值);
            校验(值); 数据 = 值; return true;
        }
        catch (Exception 异常) when (异常 is IOException || 异常 is InvalidDataException || 异常 is UnauthorizedAccessException || 异常 is ArgumentException || 异常 is InvalidOperationException)
        { 原因 = 异常.Message; return false; }
    }
    public static void 迁移(天帝存档数据 数据)
    {
        if (数据 == null || 数据.版本 < 1 || 数据.版本 > 当前版本) throw new InvalidDataException("存档版本不受支持");
        // 旧版涌现只保存无限灵石；读取时补齐通货，原存档仍由后续正常保存更新。
        if (数据.无限灵石) 数据.无限通货 = true;
        if (数据.版本 < 3)
        {
            string 姓名 = 数据.主角?.名字;
            数据.主角 = 天帝普攻.主角配置();
            if (!string.IsNullOrWhiteSpace(姓名)) 数据.主角.名字 = 姓名;
            if (数据.画布 != null)
            {
                数据.画布.当前经验 = 0;
                if (数据.画布.玩家等级 > 天帝数值.玩家上限) { 数据.画布.迁移前等级 = 数据.画布.玩家等级; 数据.画布.玩家等级 = 天帝数值.玩家上限; }
                if (数据.画布.道纹 != null) foreach (var 纹 in 数据.画布.道纹)
                {
                    if (纹 == null) continue;
                    if (纹.物品等级 == 0) 纹.物品等级 = 1;
                    if (纹.词条 != null) foreach (var 词 in 纹.词条) 词?.迁移精度();
                }
            }
        }
        数据.版本 = 当前版本; 数据.数值版本 = 天帝数值配置.版本;
    }
    public 天帝存档数据 读取()
    {
        提示 = "";
        if (尝试读取(路径, out var 数据, out var 主因)) return 数据;
        // 不用旧备份回滚未来版本的存档，避免升级后误覆盖新格式。
        if (主因 == "存档版本不受支持") { 提示 = "存档来自其他版本，暂时无法继续。原文件已保留。"; return null; }
        if (尝试读取(备份路径, out 数据, out var 备因))
        { 提示 = "已读取上一份有效备份。"; return 数据; }
        if (主因.Length > 0 || 备因.Length > 0) 提示 = "存档无法读取，原文件已保留；可选择新游戏。";
        return null;
    }
    public bool 保存(天帝存档数据 数据)
    {
        string 临时 = 路径 + ".tmp";
        try
        {
            校验(数据); 数据.版本 = 当前版本; 数据.保存时间 = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(Path.GetDirectoryName(路径));
            using (var 流 = new FileStream(临时, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var 写入 = new StreamWriter(流, new UTF8Encoding(false)))
            { 写入.Write(JsonUtility.ToJson(数据, true)); 写入.Flush(); 流.Flush(true); }
            if (!File.Exists(路径)) File.Move(临时, 路径);
            else
            {
                bool 原有效 = 尝试读取(路径, out _, out _);
                try { File.Replace(临时, 路径, 原有效 ? 备份路径 : null); }
                catch (Exception 异常) when (异常 is PlatformNotSupportedException || 异常 is IOException)
                {
                    // 不支持原子替换的平台仍保留有效备份，下一次启动可恢复。
                    if (原有效) File.Copy(路径, 备份路径, true);
                    File.Copy(临时, 路径, true); File.Delete(临时);
                }
            }
            提示 = ""; return true;
        }
        catch (Exception 异常) when (异常 is IOException || 异常 is InvalidDataException || 异常 is UnauthorizedAccessException || 异常 is ArgumentException || 异常 is InvalidOperationException)
        { 提示 = "进度保存失败，请检查磁盘空间或目录权限。"; Debug.LogWarning(提示 + " " + 异常.Message); return false; }
    }
}
