using System;

// 仅从玩家主动提交的主页入口启用，不改变默认开局或正式成长公式。
public static class 天帝作弊码
{
    public static bool 兑换(string 代码, 天帝道纹 道纹, 天帝宝盒 宝盒, 天帝通货 通货, out string 提示)
    {
        if (string.IsNullOrWhiteSpace(代码)) { 提示 = "请输入作弊码。"; return false; }
        if (!string.Equals(代码.Trim(), "涌现", StringComparison.Ordinal)) { 提示 = "作弊码无效，请重新输入。"; return false; }
        if (道纹 == null || 宝盒 == null || 通货 == null) { 提示 = "角色尚未准备完成。"; return false; }
        bool 已启用 = 宝盒.无限灵石 && 通货.无限通货 && 道纹.玩家等级 == 天帝数值.玩家上限;
        if (!道纹.设置玩家等级(天帝数值.玩家上限)) { 提示 = "无法提升角色等级。"; return false; }
        宝盒.启用无限灵石();
        通货.启用无限通货();
        提示 = 已启用 ? "已启用：无限灵石 · 无限通货 · 满级" : "涌现！\n无限灵石 · 所有通货无限\n角色达到 " + 天帝数值.玩家上限 + " 级。";
        return true;
    }
}
