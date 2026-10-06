using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class 天帝道纹界面
{
    const string 触屏默认提示 = "点选道纹，再点已解锁格放置 · 拖空白移动画布 · 双指缩放 · 空格点一次解锁";
    道纹实例 触屏选择纹, 触屏方向纹;
    readonly Dictionary<int, Vector2> 画布触点 = new Dictionary<int, Vector2>();
    readonly HashSet<int> 手势指针 = new HashSet<int>();
    int 触屏操作指针 = int.MinValue;
    Button 触屏旋转键, 触屏卸下键, 触屏取消键;
    public 道纹实例 触屏选中道纹 => 触屏选择纹;
    public int? 触屏待放接口 => 触屏方向纹?.接口;

    void 建触屏操作()
    {
        if (!天帝移动适配.启用) return;
        var 视口 = (RectTransform)画布.transform.parent;
        视口.sizeDelta = new Vector2(1068, 420);
        触屏旋转键 = 按钮(根, "旋转60°", 48, 572, 180, 58, () => 触屏旋转());
        触屏卸下键 = 按钮(根, "卸下道纹", 238, 572, 180, 58, () => 触屏卸下());
        触屏取消键 = 按钮(根, "取消选择", 428, 572, 180, 58, 取消触屏选择);
        按钮(根, "缩小 −", 618, 572, 180, 58, () => 触屏缩放(.8f));
        按钮(根, "放大 +", 808, 572, 180, 58, () => 触屏缩放(1.25f));
        状态字.text = "道纹藏匣  /  点选后点格子放置";
        void 布(string 名, float x, float y, float w, float h)
        { var r = 根.Find(名) as RectTransform; if (r != null) 天帝响应布局.比例(r, x, y, w, h); }
        布("页面标题衬纸", .015f, .01f, .78f, .09f);
        布("返回主页", .81f, .012f, .175f, .105f);
        布("画布主面板", .015f, .125f, .67f, .45f);
        布("画布视口", .025f, .24f, .65f, .325f);
        布("实时构筑预览", .70f, .125f, .285f, .56f);
        var 预览 = 根.Find("实时构筑预览") as RectTransform;
        System.Action 重排预览 = null;
        RectTransform 预览口 = null;
        if (预览 != null)
        {
            // 用比例定位视口，内部保留可读行高。
            预览.anchorMin = 预览.anchorMax = new Vector2(0, 1); 预览.anchoredPosition = new Vector2(1140, -78); 预览.sizeDelta = new Vector2(432, 564);
            var 口 = 天帝响应布局.滚动正文(预览); 天帝响应布局.比例(口, .70f, .125f, .285f, .56f);
            预览口 = 口; 重排预览 = 天帝双端页面布局.重排正文(预览);
        }
        string[] 工具 = { "回到源点", "聚焦已解锁", "定位未接通", "撤销上一步", "布局方案" };
        for (int i = 0; i < 工具.Length; i++) 布(工具[i], .027f + i * .13f, .13f, .122f, .10f);
        string[] 操作 = { "旋转60°", "卸下道纹", "取消选择", "缩小 −", "放大 +" };
        for (int i = 0; i < 操作.Length; i++) 布(操作[i], .027f + i * .13f, .585f, .122f, .105f);
        布("藏匣标题衬纸", .015f, .72f, .97f, .105f);
        布("筛选 / 排序", .30f, .72f, .16f, .105f);
        布("上一页", .73f, .72f, .12f, .105f); 布("下一页", .865f, .72f, .12f, .105f);
        天帝响应布局.比例(候选区, .02f, .84f, .96f, .145f);
        天帝响应布局.比例(提示字.rectTransform, .027f, .69f, .95f, .045f);
        天帝响应布局.比例(状态字.rectTransform, .025f, .73f, .27f, .07f);
        天帝响应布局.比例(页码字.rectTransform, .47f, .73f, .245f, .07f);
        汇总字.gameObject.SetActive(false);
        var 分割 = 根.Find("藏匣分割线"); if (分割 != null) 分割.gameObject.SetActive(false);
        for (int i = 0; i < 候选卡.Count; i++)
        {
            var 卡 = (RectTransform)候选卡[i].transform;
            天帝响应布局.比例(候选图[i].rectTransform, .025f, .13f, .22f, .75f);
            天帝响应布局.比例(候选短名[i].rectTransform, .025f, .13f, .22f, .75f);
            天帝响应布局.比例(候选品阶[i].rectTransform, .28f, .04f, .69f, .44f);
            天帝响应布局.比例(候选状态[i].rectTransform, .32f, .53f, .64f, .40f);
            天帝响应布局.比例(候选状态图[i].rectTransform, .27f, .64f, .045f, .20f);
            var 选框 = 卡.Find("道纹选中框") as RectTransform; if (选框 != null) 天帝响应布局.比例(选框, .025f, .08f, .22f, .82f);
            候选说明[i].gameObject.SetActive(false);
        }
        var 布局 = 根.gameObject.AddComponent<天帝移动排版>();
        布局.排版 = 面板 =>
        {
            float 宽 = 面板.rect.width, 高 = 面板.rect.height, 左宽 = 宽 * .67f;
            天帝双端页面布局.按键(面板.Find("返回主页") as RectTransform, 宽 - 112, 4, 100);
            foreach (Transform 子 in 面板)
                if (子.GetComponent<Text>() is Text 文)
                {
                    if (文.text == "道纹构筑") { 天帝双端页面布局.固定(文.rectTransform, 12, 4, 136, 40); 文.fontSize = 20; }
                    if (文.text == "定位") 文.gameObject.SetActive(false);
                }
            天帝双端页面布局.固定(成长字.rectTransform, 154, 4, 宽 - 274, 40); 成长字.fontSize = 14;
            天帝双端页面布局.区域(面板, "画布主面板", 8, 50, 左宽 - 12, 高 - 204);
            天帝双端页面布局.固定(视口, 12, 100, 左宽 - 20, 高 - 258);
            float 工具宽 = (左宽 - 24) / 5;
            for (int i = 0; i < 工具.Length; i++)
            {
                var 键 = 面板.Find(工具[i]) as RectTransform;
                天帝双端页面布局.按键(键, 12 + i * (工具宽 + 2), 52, 工具宽 - 2);
                var 文 = 键.GetComponentInChildren<Text>(); 文.fontSize = 14;
                if (i != 2) 文.text = new[] { "源点", "聚焦", "", "撤销", "方案" }[i];
            }
            for (int i = 0; i < 操作.Length; i++) 天帝双端页面布局.按键(面板.Find(操作[i]) as RectTransform, 12 + i * (工具宽 + 2), 高 - 152, 工具宽 - 2);
            if (预览口 != null) { 天帝双端页面布局.固定(预览口, 左宽 + 4, 50, 宽 - 左宽 - 12, 高 - 156); 重排预览?.Invoke(); }
            天帝双端页面布局.固定(提示字.rectTransform, 12, 高 - 104, 宽 - 24, 40); 提示字.fontSize = 14;
            面板.Find("操作说明").gameObject.SetActive(false); 状态字.gameObject.SetActive(false);
            天帝双端页面布局.按键(天帝双端页面布局.子区(面板, "筛选 / 排序"), 12, 高 - 60, 118);
            天帝双端页面布局.固定(页码字.rectTransform, 136, 高 - 60, 左宽 - 280, 44);
            天帝双端页面布局.按键((RectTransform)上页按钮.transform, 左宽 - 138, 高 - 60, 62);
            天帝双端页面布局.按键((RectTransform)下页按钮.transform, 左宽 - 70, 高 - 60, 62);
            // 候选区独立横滑，四枚道纹保持触控卡片高度。
            天帝双端页面布局.固定(候选区, 左宽 + 4, 高 - 100, 宽 - 左宽 - 12, 96);
            for (int i = 0; i < 候选卡.Count; i++)
            {
                var 卡 = (RectTransform)候选卡[i].transform;
                天帝双端页面布局.固定(卡, i % 每页数量 * 160, 0, 154, 90);
                天帝双端页面布局.固定(候选短名[i].rectTransform, 4, 40, 44, 44); 候选短名[i].fontSize = 16;
                天帝双端页面布局.固定(候选图[i].rectTransform, 4, 40, 44, 44); 候选图[i].单纹半径 = 20;
                天帝双端页面布局.固定(候选品阶[i].rectTransform, 4, 2, 98, 24); 候选品阶[i].fontSize = 14;
                天帝双端页面布局.固定(候选状态[i].rectTransform, 52, 48, 98, 40); 候选状态[i].fontSize = 14;
            }
        };
        // 为卡片建立可滑动内容，拖候选卡仍沿用道纹选择交互。
        var 卡内容 = 区块(候选区, "移动候选列表", 0, 0, 每页数量 * 160, 96);
        天帝响应布局.动态(卡内容);
        foreach (var 卡 in 候选卡) 卡.transform.SetParent(卡内容, false);
        候选区.gameObject.AddComponent<Image>().color = Color.clear; 候选区.gameObject.AddComponent<RectMask2D>();
        var 滚 = 候选区.gameObject.AddComponent<ScrollRect>(); 滚.viewport = 候选区; 滚.content = 卡内容;
        滚.horizontal = true; 滚.vertical = false; 滚.movementType = ScrollRect.MovementType.Clamped;
        更新触屏操作();
    }
    void 更新触屏操作()
    {
        if (触屏旋转键 == null) return;
        if (触屏选择纹 != null && !数据.道纹.Contains(触屏选择纹))
        { 触屏选择纹 = 触屏方向纹 = null; 提示字.text = 触屏默认提示; }
        触屏旋转键.interactable = 触屏选择纹 != null && !拖动中 && !筛选已打开;
        触屏卸下键.interactable = 触屏旋转键.interactable && 触屏选择纹.格子.HasValue;
        触屏取消键.interactable = 触屏选择纹 != null || 浮窗显示;
    }
    public bool 触屏选择(道纹实例 纹, Vector2 位置)
    {
        if (数据 == null || 纹 == null || 拖动中 || 筛选已打开) return false;
        if (!纹.是源纹 && !数据.道纹.Contains(纹)) return false;
        触屏选择纹 = 纹.是源纹 ? null : 纹;
        触屏方向纹 = 触屏选择纹 == null ? null : 创建拖影(纹);
        显示属性(纹, 位置);
        提示字.text = 纹.是源纹 ? "中心天赋不可移动、旋转或卸下" : "已选 " + 纹.名称 + (纹.格子.HasValue ? " · 可旋转、卸下或点空格移动" : " · 点已解锁格放置；旋转只在放置时提交");
        更新触屏操作(); return true;
    }
    public void 取消触屏选择()
    {
        触屏选择纹 = 触屏方向纹 = null;
        有指针 = false; 取消拖动(); 更新触屏操作();
    }
    public bool 触屏旋转()
    {
        if (触屏选择纹 == null || 拖动中 || 筛选已打开) return false;
        if (触屏选择纹.格子.HasValue)
        { bool 对 = false; 记录画布操作(() => 对 = 数据.旋转(触屏选择纹), "DW03_旋转"); 触屏方向纹 = 创建拖影(触屏选择纹); return 对; }
        触屏方向纹.顺时针旋转接口();
        提示字.text = "待放朝向已转60° · 放置后生效"; return true;
    }
    public bool 触屏卸下()
    {
        if (触屏选择纹 == null || !触屏选择纹.格子.HasValue || 拖动中 || 筛选已打开) return false;
        bool 对 = false; 记录画布操作(() => { 数据.收回(触屏选择纹); 对 = !触屏选择纹.格子.HasValue; }, "DW04_卸下");
        if (对) 取消触屏选择(); return 对;
    }
    bool 触屏点画布(PointerEventData e)
    {
        if (!画布命中(e.position, out var 格)) return true;
        if (提示解锁点不足(格)) return true;
        if (画布.缩放 < .45f) { 定位格子(格); 提示字.text = "已放大此处，再点格子编辑"; return true; }
        if (数据.已放置.TryGetValue(格, out var 已放)) { 触屏选择(已放, e.position); return true; }
        if (触屏选择纹 == null) return false; // 沿用正式的花费技能点解锁逻辑。
        if (!数据.格已解锁(格))
        {
            bool 成功 = false; 记录画布操作(() => 成功 = 数据.解锁格子(格), "DW01_解锁");
            提示字.text = 成功 ? "已花1点解锁 · 再点此格放置所选道纹" : "此格不能解锁 · 请检查范围与剩余技能点"; return true;
        }
        if (!数据.可放置(触屏选择纹, 格)) { 提示字.text = "此格不能放置 · 选择仍保留"; return true; }
        var 纹 = 触屏选择纹;
        var 布局 = 数据.当前布局(); 布局.RemoveAll(x => x.编号 == 纹.编号);
        布局.Add(new 道纹布局项 { 编号 = 纹.编号, 格子 = 格, 接口 = 触屏方向纹.接口, 入口方向 = 触屏方向纹.是顺序功能 ? 触屏方向纹.入口方向 : -1 });
        bool 放好 = false; 记录画布操作(() => 放好 = 数据.应用布局(布局, out _));
        if (放好) { 触屏方向纹 = 创建拖影(纹); 显示属性(纹, e.position); 提示字.text = "已放置 · 接口双向连到源纹才会生效"; }
        更新触屏操作(); return true;
    }
    public void 触屏缩放(float 倍率)
    {
        if (画布 == null || 拖动中 || 筛选已打开 || !天帝数值.有限(倍率) || 倍率 <= 0) return;
        var 中 = RectTransformUtility.WorldToScreenPoint(null, 画布.rectTransform.TransformPoint(画布.rectTransform.rect.center));
        应用触屏手势(中, 中, 倍率);
    }
    void 应用触屏手势(Vector2 旧中, Vector2 新中, float 倍率)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, 旧中, null, out var 前);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(画布.rectTransform, 新中, null, out var 现);
        float 原 = 画布.缩放, 新 = Mathf.Clamp(原 * 倍率, .08f, 1.65f);
        画布.平移 = 现 - (前 - 画布.平移) * (新 / 原); 画布.缩放 = 新;
        限制平移(); 画布.SetVerticesDirty(); 更新标签();
    }
    public bool 触屏按下(bool 在画布, PointerEventData e)
    {
        手势指针.Remove(e.pointerId);
        if (在画布)
        {
            画布触点[e.pointerId] = e.position;
            if (画布触点.Count > 1)
            { foreach (int id in 画布触点.Keys) 手势指针.Add(id); 取消拖动(); 触屏操作指针 = int.MinValue; return false; }
        }
        if (触屏操作指针 != int.MinValue && 触屏操作指针 != e.pointerId) return false;
        触屏操作指针 = e.pointerId; return true;
    }
    public bool 触屏手势移动(bool 在画布, PointerEventData e)
    {
        if (!在画布 || !画布触点.ContainsKey(e.pointerId)) return false;
        Vector2 a = Vector2.zero, b = Vector2.zero; int n = 0;
        foreach (var 点 in 画布触点.Values) { if (n == 0) a = 点; else if (n == 1) b = 点; n++; }
        var 旧中 = (a + b) * .5f; float 旧距 = Vector2.Distance(a, b);
        画布触点[e.pointerId] = e.position;
        if (n != 2 || !手势指针.Contains(e.pointerId)) return 手势指针.Contains(e.pointerId);
        n = 0; foreach (var 点 in 画布触点.Values) { if (n++ == 0) a = 点; else b = 点; }
        if (旧距 > 1) 应用触屏手势(旧中, (a + b) * .5f, Vector2.Distance(a, b) / 旧距);
        return true;
    }
    public bool 触屏指针可操作(PointerEventData e) => !手势指针.Contains(e.pointerId) && 触屏操作指针 == e.pointerId;
    public bool 触屏点击可操作(PointerEventData e) => !手势指针.Contains(e.pointerId);
    public void 触屏抬起(PointerEventData e)
    { 画布触点.Remove(e.pointerId); if (触屏操作指针 == e.pointerId) 触屏操作指针 = int.MinValue; }
    void 清理触屏手势() { 画布触点.Clear(); 手势指针.Clear(); 触屏操作指针 = int.MinValue; }
}
