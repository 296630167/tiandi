using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class 天帝道纹操作筛选
{
    public static bool 接口匹配(int 接口, int 必需接口, bool 允许旋转)
    {
        if (必需接口 == 0) return true;
        for (int i = 0; i < (允许旋转 ? 6 : 1); i++, 接口 = 天帝道纹.顺时针接口(接口))
            if ((接口 & 必需接口) == 必需接口) return true;
        return false;
    }
}

public sealed partial class 天帝道纹界面
{
    道纹实例 拖影纹;
    道纹存档数据 撤销前;
    string 撤销后标识;
    Button 撤销键, 拖转键, 定位键, 确认方案键, 取消方案键;
    Text 定位文字, 方案提示, 旋转筛选文字;
    RectTransform 方案层;
    readonly List<InputField> 方案名称输入 = new List<InputField>();
    readonly List<Text> 方案说明 = new List<Text>();
    readonly List<Button> 方案载入键 = new List<Button>();
    readonly List<Button> 方案保存键 = new List<Button>();
    readonly List<Image> 方案行 = new List<Image>();
    readonly List<Text> 接口筛选文字 = new List<Text>();
    int 待载入方案 = -1, 待覆盖方案 = -1, 上次定位编号;
    public int 当前接口筛选 { get; private set; }
    public bool 允许旋转筛选 { get; private set; }
    public bool 可撤销 => 撤销前 != null && 天帝道纹.快照标识(数据.导出存档()) == 撤销后标识;
    public int? 拖动接口 => 拖影纹 == null ? (int?)null : 拖影纹.接口;

    void 建操作工具()
    {
        文字(根, "定位", 412, 87, 42, 34, 15, TextAnchor.MiddleLeft).color = 天帝道纹美术.次文;
        按钮(根, "回到源点", 460, 87, 104, 34, 回到源点);
        按钮(根, "聚焦已解锁", 572, 87, 116, 34, 聚焦解锁区域);
        定位键 = 按钮(根, "定位未接通", 696, 87, 140, 34, () => 定位未接通()); 定位文字 = 定位键.GetComponentInChildren<Text>();
        var 分组线 = 区块(根, "工具分组线", 845, 90, 1, 28).gameObject.AddComponent<Image>(); 分组线.color = new Color(.30f,.48f,.44f,.4f); 分组线.raycastTarget = false;
        撤销键 = 按钮(根, "撤销上一步", 856, 87, 116, 34, () => 撤销上一步());
        按钮(根, "布局方案", 980, 87, 116, 34, 打开布局方案);
        拖转键 = 按钮(根, "旋转 · R", 980, 582, 116, 34, () => 旋转拖动道纹());
        if (天帝移动适配.启用) 拖转键.GetComponentInChildren<Text>().text = "旋转";
        拖转键.gameObject.SetActive(false);
        foreach (var 键 in new[] { 定位键, 撤销键, 拖转键 }) 键.GetComponentInChildren<Text>().fontSize = 17;
    }
    void 更新操作状态()
    {
        if (撤销键 == null) return;
        撤销键.interactable = !拖动中 && !平移中 && 可撤销;
        拖转键.interactable = 拖动中;
        拖转键.gameObject.SetActive(拖动中);
        int 未接 = 0; foreach (var 纹 in 数据.道纹) if (纹.格子.HasValue && !纹.生效) 未接++;
        定位键.interactable = 未接 > 0 && !拖动中;
        定位文字.text = 天帝移动适配.启用||山水构筑 ? "未接通 " + 未接 : "未接通 " + 未接 + " · 定位";
        foreach (var 键 in new[] { 定位键, 撤销键, 拖转键 }) 键.GetComponentInChildren<Text>().color = 键.interactable ? 文字色 : 天帝道纹美术.次文;
        if(山水构筑)foreach(var 键 in new[]{定位键,撤销键,拖转键})天帝图三四山水素材.按钮(键);
    }
    public void 聚焦解锁区域()
    {
        if (画布 == null) return; 取消拖动();
        Vector2 最小 = new Vector2(float.MaxValue, float.MaxValue), 最大 = new Vector2(float.MinValue, float.MinValue);
        foreach (var 格 in 数据.所有解锁格) { var 点 = 画布.格位置(格); 最小 = Vector2.Min(最小, 点); 最大 = Vector2.Max(最大, 点); }
        var 区 = 画布.rectTransform.rect; float 宽 = 区.width > 1 ? 区.width : 1070, 高 = 区.height > 1 ? 区.height : 535;
        if (画布.轻透画布) { 宽 = 悬浮画布尺寸.x; 高 = 悬浮画布尺寸.y; }
        var 尺寸 = 最大 - 最小 + Vector2.one * 天帝道纹.半径 * 画布.格间距 * 4;
        // 手机工作区只有208单位高，不能沿用PC的90单位留白而默认退成色块总览。
        float 横留白 = 天帝移动适配.启用 ? 32 : 100, 纵留白 = 天帝移动适配.启用 ? 24 : 90;
        画布.缩放 = Mathf.Clamp(Mathf.Min((宽 - 横留白) / 尺寸.x, (高 - 纵留白) / 尺寸.y), .08f, 1.25f);
        画布.平移 = -(最小 + 最大) * .5f * 画布.缩放 + (画布.轻透画布 ? 悬浮画布中心 : Vector2.zero); 限制平移(); 画布.SetVerticesDirty(); 更新标签();
    }
    public void 回到源点() { if (画布 == null) return; 取消拖动(); 定位格子(Vector2Int.zero); }
    public void 定位格子(Vector2Int 格)
    {
        画布.缩放 = 1.1f; 画布.平移 = -画布.格位置(格) * 画布.缩放 + (画布.轻透画布 ? 悬浮画布中心 : Vector2.zero); 限制平移(); 画布.SetVerticesDirty(); 更新标签();
    }
    public bool 定位未接通()
    {
        if (拖动中 || 筛选已打开) return false;
        道纹实例 第一 = null, 下一枚 = null;
        foreach (var 纹 in 数据.道纹)
        {
            if (!纹.格子.HasValue || 纹.生效) continue;
            if (第一 == null || 纹.编号 < 第一.编号) 第一 = 纹;
            if (纹.编号 > 上次定位编号 && (下一枚 == null || 纹.编号 < 下一枚.编号)) 下一枚 = 纹;
        }
        var 目标 = 下一枚 ?? 第一;
        if (目标 == null) { 提示字.text = "已放置的道纹全部接通"; return false; }
        指针离开(); 上次定位编号 = 目标.编号; 定位格子(目标.格子.Value);
        更新构筑预览(目标, null, false, false); 提示字.text = "已定位 #" + 目标.编号 + " · " + 连接诊断.说明(目标); return true;
    }
    void 建接口筛选(RectTransform 框)
    {
        文字(框, "接口方向 · 同时满足所选接口", 16, 356, 436, 28, 16, TextAnchor.MiddleLeft).color = 天帝道纹美术.次文;
        for (int d = 0; d < 6; d++)
        {
            int 方向 = d; var b = 按钮(框, 天帝道纹.方向名[d], 12 + d * 74, 394, 68, 38, () =>
            { 设置接口筛选(当前接口筛选 ^ (1 << 方向), 允许旋转筛选); 更新接口筛选文字(); });
            var 字 = b.GetComponentInChildren<Text>(); 字.fontSize = 16; 接口筛选文字.Add(字);
        }
        var 转 = 按钮(框, "按当前朝向匹配", 12, 454, 282, 40, () => { 设置接口筛选(当前接口筛选, !允许旋转筛选); 更新接口筛选文字(); });
        旋转筛选文字 = 转.GetComponentInChildren<Text>(); 旋转筛选文字.fontSize = 17;
        按钮(框, "清除接口", 308, 454, 148, 40, () => { 设置接口筛选(0, false); 更新接口筛选文字(); });
    }
    void 更新接口筛选文字()
    {
        for (int d = 0; d < 接口筛选文字.Count; d++)
        {
            bool 选 = (当前接口筛选 & (1 << d)) != 0; 接口筛选文字[d].text = (选 ? "✓" : "") + 天帝道纹.方向名[d];
            接口筛选文字[d].color = 选 ? 亮青 : 文字色;
            天帝道纹美术.选中(接口筛选文字[d].transform.parent.GetComponent<Image>(),选);
        }
        if (旋转筛选文字 != null) 旋转筛选文字.text = 允许旋转筛选 ? "✓ 旋转后也能匹配" : "仅匹配当前朝向";
    }
    public bool 设置接口筛选(int 接口, bool 可旋转 = false)
    {
        if (接口 < 0 || 接口 > 63 || 拖动中 || 平移中) return false;
        当前接口筛选 = 接口; 允许旋转筛选 = 可旋转; 候选页码 = 0; 刷新(); return true;
    }
    static 道纹实例 创建拖影(道纹实例 原)
    {
        var 新 = new 道纹实例 { 编号 = 原.编号, 品阶 = 原.品阶, 接口 = 原.接口, 分类 = 原.分类, 功能 = 原.功能, 入口方向 = 原.入口方向, 格子 = 原.格子, 生效 = 原.生效, 物品等级 = 原.物品等级 };
        foreach (var 词 in 原.词条) 新.词条.Add(词.副本()); return 新;
    }
    public bool 旋转拖动道纹()
    {
        if (拖影纹 == null || 筛选已打开) return false;
        拖影纹.顺时针旋转接口(); 拖影图.SetVerticesDirty(); 已有预览缓存 = false;
        更新拖动(指针位置); 提示字.text += " · 朝向已顺时针转60°（放下才生效）"; return true;
    }
    void 更新操作快捷键()
    {
        if (天帝移动适配.启用) return;
        var 键 = Keyboard.current; if (键 == null || 筛选已打开 || 数据 == null) return;
        var 选中 = EventSystem.current?.currentSelectedGameObject;
        if (选中 != null && 选中.GetComponent<InputField>() != null) return;
        if (拖动中 && 键.rKey.wasPressedThisFrame) 旋转拖动道纹();
        if (!拖动中 && (键.leftCtrlKey.isPressed || 键.rightCtrlKey.isPressed) && 键.zKey.wasPressedThisFrame) 撤销上一步();
    }
    void 记录画布操作(Action 操作, string 声 = "DW02_放置")
    {
        var 原接通 = new HashSet<int>(); foreach (var 纹 in 数据.道纹) if (纹.生效) 原接通.Add(纹.编号);
        var 前 = 数据.导出存档(); string 前标识 = 天帝道纹.快照标识(前); 操作();
        string 后 = 天帝道纹.快照标识(数据.导出存档());
        if (前标识 != 后)
        {
            撤销前 = 前; 撤销后标识 = 后; 天帝声音.提示(声);
            foreach (var 纹 in 数据.道纹) if (纹.生效 && !原接通.Contains(纹.编号)) { 天帝声音.提示("DW05_接通"); break; }
        }
        else 天帝声音.提示("UI04_拒绝");
        更新操作状态();
    }
    public bool 撤销上一步()
    {
        if (拖动中 || 平移中 || 筛选已打开 || 撤销前 == null) return false;
        var 前 = 撤销前; string 预期 = 撤销后标识; 撤销前 = null; 撤销后标识 = null;
        bool 成功 = 数据.恢复画布(前, 预期, out var 原因); 刷新();
        提示字.text = 成功 ? "已撤销上一步 · 解锁、位置和朝向恢复；词条与资源按当前状态保留" : 原因; return 成功;
    }
    public void 打开布局方案()
    {
        if (拖动中 || 平移中) return; 关闭筛选(); 待载入方案 = 待覆盖方案 = -1;
        if (方案层 == null) 建方案窗口();
        确认方案键.interactable = false;刷新方案窗口(); 方案提示.text = "选择一个已保存的方案，检查通过后在下方应用。未列入方案的道纹回到背包。";
        方案层.SetAsLastSibling(); 方案层.gameObject.SetActive(true);
    }
    void 建方案窗口()
    {
        方案层 = 区块(根, "布局方案层", 0, 0, 1600, 900);
        var 遮 = 底(方案层, "布局遮罩", 0, 0, 1600, 900, new Color(0, 0, 0, .75f)); 遮.GetComponent<Image>().raycastTarget = true;
        var 框 = 底(方案层, "布局方案面板", 300, 80, 1000, 740, new Color(.035f, .065f, .078f)); 框.GetComponent<Image>().raycastTarget = true;
        天帝辅助页山水.纸(框.GetComponent<Image>(),"弹窗纸框");
        var 标题=文字(框, "我的布局方案", 190, 76, 620, 70, 32, TextAnchor.MiddleCenter);标题.fontStyle=FontStyle.Normal;
        按钮(框, "关闭", 840, 82, 132, 48, 关闭筛选);
        文字(框, "保存三个布局 · 保留实际道纹的词条、品阶和接口形状", 40, 142, 920, 44, 18, TextAnchor.MiddleLeft);
        for (int i = 0; i < 天帝道纹.方案槽数; i++)
        {
            int 槽 = i; var 行 = 底(框, "方案槽-" + (i + 1), 28, 192 + i * 136, 944, 124, new Color(.07f, .12f, .14f));
            天帝辅助页山水.轻纸(行.GetComponent<Image>());
            方案行.Add(行.GetComponent<Image>());
            文字(行, "方案 " + (i + 1), 16, 10, 110, 40, 21, TextAnchor.MiddleLeft);
            var 输入区 = 底(行, "方案名称", 130, 12, 372, 38, new Color(.035f, .065f, .078f)); 输入区.GetComponent<Image>().raycastTarget = true;
            输入区.GetComponent<Image>().sprite=null;输入区.GetComponent<Image>().color=new Color(.93f,.92f,.82f,.7f);
            var 名字 = 文字(输入区, "", 12, 0, 348, 38, 19, TextAnchor.MiddleLeft);
            var 输入 = 输入区.gameObject.AddComponent<InputField>(); 输入.textComponent = 名字; 输入.targetGraphic = 输入区.GetComponent<Image>(); 输入.characterLimit = 20; 输入.lineType = InputField.LineType.SingleLine;
            方案名称输入.Add(输入);
            方案保存键.Add(按钮(行, "保存当前", 528, 10, 184, 42, () => 保存当前方案(槽)));
            方案载入键.Add(按钮(行, "检查 / 载入", 730, 10, 196, 42, () => 准备载入方案(槽)));
            var 描述 = 文字(行, "", 16, 68, 910, 52, 17, TextAnchor.UpperLeft); 描述.verticalOverflow = VerticalWrapMode.Truncate; 方案说明.Add(描述);
        }
        方案提示 = 文字(框, "", 40, 606, 920, 62, 18, TextAnchor.UpperLeft); 方案提示.verticalOverflow = VerticalWrapMode.Truncate;
        取消方案键 = 按钮(框, "取消方案选择", 28, 675, 190, 42, () =>
        { 待载入方案 = 待覆盖方案 = -1; 确认方案键.interactable = false; 刷新方案窗口(); 方案提示.text = "已取消 · 当前布局与已保存方案未改变。"; });
        确认方案键 = 按钮(框, "应用已检查的方案", 644, 675, 328, 42, () =>
        {
            if (待载入方案 < 0) return; bool 成功 = false; string 原因 = "";
            记录画布操作(() => 成功 = 数据.载入布局方案(待载入方案, out 原因), "UI02_确认");
            方案提示.text = 成功 ? "方案已载入 · 关闭后可查看连接；撤销上一步可恢复原布局。" : "未应用：" + 原因;
            待载入方案 = -1; 确认方案键.interactable = false; 刷新方案窗口();
        });
        foreach(var 键 in 框.GetComponentsInChildren<Button>(true))天帝辅助页山水.按钮(键,键==确认方案键);
        foreach(var 文 in 框.GetComponentsInChildren<Text>(true)){文.fontStyle=FontStyle.Normal;文.color=天帝剪纸界面皮肤.墨;}
        if(天帝移动适配.启用)
        {
            var 口=天帝响应布局.滚动列(框,"方案槽列表",28,192,944,404);
            var 内容=口.GetComponent<ScrollRect>().content;天帝响应布局.动态(内容);
            天帝双端页面布局.移动页(框,面板=>
            {
                天帝双端页面布局.页头(面板,"关闭");float 宽=面板.rect.width,高=面板.rect.height;
                var 概述=面板.GetComponentsInChildren<Text>().First(x=>x.text.StartsWith("保存三个布局"));
                天帝双端页面布局.固定(概述.rectTransform,12,天帝双端页面布局.页头高度,宽-24,26);概述.fontSize=14;
                天帝双端页面布局.固定(口,8,100,宽-16,高-208);
                天帝双端页面布局.固定(内容,0,0,宽-16,444);
                for(int i=0;i<方案行.Count;i++)
                {
                    var 行=方案行[i].rectTransform;float 行宽=宽-16;
                    天帝双端页面布局.固定(行,0,i*148,行宽,140);
                    var 标签=行.GetComponentsInChildren<Text>().First(x=>x.text=="方案 "+(i+1));
                    天帝双端页面布局.固定(标签.rectTransform,8,6,66,32);标签.fontSize=16;
                    天帝双端页面布局.固定((RectTransform)方案名称输入[i].transform,78,4,行宽-86,38);
                    天帝响应布局.比例(方案名称输入[i].textComponent.rectTransform,.03f,0,.94f,1);
                    天帝双端页面布局.按键((RectTransform)方案保存键[i].transform,8,48,(行宽-22)*.5f);
                    天帝双端页面布局.按键((RectTransform)方案载入键[i].transform,14+(行宽-22)*.5f,48,(行宽-22)*.5f);
                    天帝双端页面布局.固定(方案说明[i].rectTransform,8,98,行宽-16,36);方案说明[i].fontSize=14;
                }
                天帝双端页面布局.固定(方案提示.rectTransform,12,高-100,宽-24,44);方案提示.fontSize=14;
                天帝双端页面布局.按键((RectTransform)取消方案键.transform,12,高-48,(宽-30)*.35f);
                天帝双端页面布局.按键((RectTransform)确认方案键.transform,18+(宽-30)*.35f,高-48,(宽-30)*.65f);
            });
        }
    }
    void 刷新方案窗口(bool 更新名称 = true)
    {
        for (int i = 0; i < 方案名称输入.Count; i++)
        {
            var 方案 = 数据.布局方案[i]; if (更新名称) 方案名称输入[i].SetTextWithoutNotify(方案.已保存 ? 方案.名称 : "布局 " + (i + 1));
            天帝辅助页山水.轻纸(方案行[i], i == 待载入方案 || i == 待覆盖方案);
            bool 覆盖 = i == 待覆盖方案;
            天帝辅助页山水.按钮(方案保存键[i], true);
            方案保存键[i].GetComponentInChildren<Text>().text = 覆盖 ? "确认覆盖" : "保存当前";
            bool 有效 = 数据.检查布局方案(i, out var 原因); 方案载入键[i].interactable = 方案.已保存;
            天帝辅助页山水.按钮(方案载入键[i]);
            方案说明[i].text = 覆盖 ? "待覆盖「" + 方案.名称 + "」 · 确认后替换为当前画布" : !方案.已保存 ? "空槽 · 将当前画布保存到此处" : (i == 待载入方案 ? "等待应用 · " : "") + "摆放 " + 方案.摆放.Count + " 枚 · " + (有效 ? "可以载入" : "暂不可用：" + 原因);
            方案说明[i].color = !方案.已保存 || 有效 ? 文字色 : new Color(1, .56f, .45f);
        }
        取消方案键.interactable = 待载入方案 >= 0 || 待覆盖方案 >= 0;
        天帝界面美术.按钮(确认方案键, 确认方案键.interactable);
        var 应用字=确认方案键.GetComponentInChildren<Text>();
        应用字.text = 待载入方案 >= 0 ? "应用「" + 数据.布局方案[待载入方案].名称 + "」" : "选择方案后应用";
        应用字.resizeTextForBestFit=true;应用字.resizeTextMinSize=16;应用字.resizeTextMaxSize=19;
        foreach(var 键 in 方案层.GetComponentsInChildren<Button>(true))天帝辅助页山水.按钮(键,键==确认方案键||方案保存键.Contains(键));
    }
    void 保存当前方案(int 槽)
    {
        if (数据.布局方案[槽].已保存 && 待覆盖方案 != 槽) { 待覆盖方案 = 槽; 待载入方案 = -1; 确认方案键.interactable = false; 刷新方案窗口(false); 方案提示.text = "将覆盖「" + 数据.布局方案[槽].名称 + "」。点击该槽“确认覆盖”，或取消。"; return; }
        if (!数据.保存布局方案(槽, 方案名称输入[槽].text)) { 方案提示.text = "请填写1～20字的方案名称"; return; }
        待覆盖方案 = 待载入方案 = -1; 确认方案键.interactable = false; 刷新方案窗口(); 方案提示.text = "已保存当前布局 · 方案随正常进度存档保留。";
    }
    void 准备载入方案(int 槽)
    {
        待覆盖方案 = -1; bool 可用 = 数据.检查布局方案(槽, out var 原因); 待载入方案 = 可用 ? 槽 : -1;
        确认方案键.interactable = 可用;
        刷新方案窗口(false);
        方案提示.text = 可用 ? "将载入「" + 数据.布局方案[槽].名称 + "」：摆放 " + 数据.布局方案[槽].摆放.Count + " 枚实际道纹，替换当前布局并保留词条。点击下方应用。" : "方案未应用：" + 原因;
    }
}
