using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 每枚拾取生成独立快照；等待队列不计停留时间，由游戏主循环每帧驱动一次。
public sealed class 天帝拾取提示 : IDisposable
{
    public const int 最大显示数 = 4;
    public const float 默认停留秒 = 4, 动画秒 = .25f, 入场间隔秒 = .15f;
    readonly float 宽, 高, 行距, 停靠点, 起始点;
    readonly bool 紧凑;
    readonly RectTransform 根;
    readonly Font 字体;
    readonly float 停留秒;
    readonly Queue<快照> 等待 = new Queue<快照>();
    readonly 条目[] 显示 = new 条目[最大显示数];
    float 下次入场;
    bool 已销毁;
    sealed class 快照 { public string 标题, 详情; }
    sealed class 条目 { public RectTransform 区; public CanvasGroup 组; public float 年龄; }
    public int 等待数 => 等待.Count;
    public int 显示数 { get { int n = 0; foreach (var 条 in 显示) if (条 != null) n++; return n; } }
    public RectTransform 区域 => 根;

    public 天帝拾取提示(RectTransform 父, Font 字体, float 停留秒 = 默认停留秒, bool 紧凑 = false, bool 右侧 = false)
    {
        this.字体 = 字体; this.紧凑 = 紧凑;
        宽 = 紧凑 ? 344 : 490; 高 = 紧凑 ? 68 : 62; 行距 = 紧凑 ? 74 : 68;
        停靠点 = 紧凑 ? 8 : 28; 起始点 = 右侧 ? 宽 + 16 : -宽;
        this.停留秒 = float.IsNaN(停留秒) || float.IsInfinity(停留秒) ? 默认停留秒 : Mathf.Max(.1f, 停留秒);
        根 = 创建区(父, "拾取提示列表", 0, 389, 紧凑 ? 420 : 530, 行距 * 最大显示数);
        根.gameObject.AddComponent<RectMask2D>();
    }
    static RectTransform 创建区(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var 区 = new GameObject(名, typeof(RectTransform)).GetComponent<RectTransform>();
        区.SetParent(父, false); 区.anchorMin = 区.anchorMax = 区.pivot = new Vector2(0, 1);
        区.anchoredPosition = new Vector2(x, -y); 区.sizeDelta = new Vector2(w, h); return 区;
    }
    void 文(RectTransform 父, string 内容, float y, float 高度, int 字号, Color 色)
    {
        float 内边=16;
        var 字 = 创建区(父, "文字", 内边, y, 宽 - 内边*2, 高度).gameObject.AddComponent<Text>();
        字.font = 字体; 字.text = 内容; 字.fontSize = 字号; 字.color = 色;
        字.alignment = TextAnchor.MiddleLeft; 字.raycastTarget = false;
        字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Truncate;
        if(天帝剪纸界面皮肤.已启用)
        {
            // 纸雕按钮两端花饰与上下边线不能占用文字区；按实际条目比例适配双端。
            bool 标题=y==0;
            // 两行各占一条清晰基线，避免标题和详情锚点重叠；扩大文字安全区但保留两端花饰。
            天帝响应布局.比例(字.rectTransform,.10f,标题?.06f:.52f,.80f,标题?.42f:.36f);
            字.fontSize=标题?18:14;
            字.resizeTextForBestFit=true;字.resizeTextMinSize=标题?12:10;字.resizeTextMaxSize=字.fontSize;
            return;
        }
        if (紧凑)
        {
            天帝响应布局.比例(字.rectTransform, 内边 / 宽, y / 高, (宽 - 内边*2) / 宽, 高度 / 高);
            天帝响应布局.字号(字);
        }
    }
    public void 加入(道纹实例 纹)
    {
        if (已销毁 || 纹 == null) return;
        int 口 = 0; for (int d = 0; d < 6; d++) if (纹.有接口(d)) 口++;
        string 简述 = "";
        for (int i = 0; i < Math.Min(2, 纹.词条.Count); i++)
            简述 += (i == 0 ? "" : "  ·  ") + 纹.词条[i].属性 + " " + 天帝道纹属性.数值文字(纹.词条[i].属性, 纹.词条[i].实际数值);
        if (纹.词条.Count > 2) 简述 += " 等" + 纹.词条.Count + "条";
        等待.Enqueue(new 快照 { 标题 = "获得 " + 天帝道纹美术.深底品阶文字(纹.品阶) + " · " + 纹.名称,
            详情 = 简述 + "   |   " + 口 + "接口" });
        尝试入场();
    }
    public void 加入(通货种类 种类, int 数量)
    {
        if (已销毁 || 数量 <= 0 || (int)种类 < 0 || (int)种类 >= 天帝通货.定义.Count ||
            种类 == 通货种类.通脉针 || 种类 == 通货种类.六通玉) return;
        string 说明 = 天帝通货.定义[(int)种类].说明;
        // 长说明只作用途摘要，完整规则在改造页展示。
        int 分隔 = 说明.IndexOf('；'); if (分隔 >= 0) 说明 = 说明.Substring(0, 分隔);
        if (种类 == 通货种类.问天石) 说明 = "普通道纹随机升阶";
        else if(种类==通货种类.易纹砂)说明="重抽所选词条";
        else if(种类==通货种类.重铸石)说明="重抽全部词条";
        else if(种类==通货种类.添蕴砂)说明="增加一条词条";
        等待.Enqueue(new 快照 { 标题 = "获得 <color=#FFD16B>" + 种类 + " ×" + 数量 + "</color>", 详情 = "用于改造  |  " + 说明 });
        尝试入场();
    }
    public void 加入灵石(int 数量)
    {
        if (已销毁 || 数量 <= 0) return;
        等待.Enqueue(new 快照 { 标题 = "获得 <color=#9DF3D5>灵石 ×" + 数量 + "</color>", 详情 = "自动入库  |  主要用于主页开启宝盒" });
        尝试入场();
    }
    void 尝试入场()
    {
        if (等待.Count == 0 || 下次入场 > .0001f) return;
        for (int i = 0; i < 显示.Length; i++)
        {
            if (显示[i] != null) continue;
            var 快 = 等待.Dequeue(); var 区 = 创建区(根, "拾取条目", 起始点, i * 行距, 宽, 高);
            var 底 = 区.gameObject.AddComponent<Image>(); 底.color = new Color(.025f, .07f, .07f, .88f); 底.raycastTarget = false;
            if (天帝剪纸界面皮肤.已启用) { 底.sprite = 天帝剪纸界面皮肤.素材("墨绿按钮"); 底.type = Image.Type.Simple; 底.color = new Color(1,1,1,.94f); }
            else if (天帝青绿皮肤.已启用) { 底.sprite = 天帝青绿皮肤.获取("QLUI_深青面板"); 底.type = Image.Type.Sliced; 底.pixelsPerUnitMultiplier = 2; 底.color = new Color(1,1,1,.94f); }
            var 组 = 区.gameObject.AddComponent<CanvasGroup>(); 组.alpha = 0; 组.interactable = 组.blocksRaycasts = false;
            文(区, 快.标题, 0, 紧凑 ? 30 : 32, 紧凑 ? 18 : 20, new Color(.94f, .94f, .89f));
            文(区, 快.详情, 紧凑 ? 30 : 32, 紧凑 ? 24 : 26, 紧凑 ? 15 : 17, new Color(.68f, .82f, .79f));
            显示[i] = new 条目 { 区 = 区, 组 = 组 }; 下次入场 = 入场间隔秒; return;
        }
    }
    public void 更新(float 秒)
    {
        if (已销毁 || 秒 < 0 || float.IsNaN(秒) || float.IsInfinity(秒)) return;
        // 分段处理长帧，避免排队中的下一条一入场便被扣掉整帧时间。
        while (秒 > 0)
        {
            float 步 = Mathf.Min(.05f, 秒); 秒 -= 步; 下次入场 = Mathf.Max(0, 下次入场 - 步);
            for (int i = 0; i < 显示.Length; i++)
            {
                var 条 = 显示[i]; if (条 == null) continue; 条.年龄 += 步;
                if (条.年龄 >= 动画秒 * 2 + 停留秒) { 删除(条.区.gameObject); 显示[i] = null; continue; }
                bool 退场 = 条.年龄 > 动画秒 + 停留秒;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(退场 ? (条.年龄 - 动画秒 - 停留秒) / 动画秒 : 条.年龄 / 动画秒));
                if (紧凑)
                {
                    float 行高 = 根.rect.height / 最大显示数;
                    float 横 = 退场 ? Mathf.Lerp(8, 根.rect.width + 16, t) : Mathf.Lerp(根.rect.width + 16, 8, t);
                    条.区.anchorMin = new Vector2(0, 1); 条.区.anchorMax = Vector2.one;
                    条.区.sizeDelta = new Vector2(-16, Mathf.Max(32, 行高 - 6));
                    条.区.anchoredPosition = new Vector2(横, -i * 行高);
                }
                else 条.区.anchoredPosition = new Vector2(退场 ? Mathf.Lerp(停靠点, 起始点, t) : Mathf.Lerp(起始点, 停靠点, t), -i * 行距);
                条.组.alpha = 退场 ? 1 - t : t;
            }
            尝试入场();
        }
    }
    static void 删除(GameObject 物)
    {
        if (物 == null) return; 物.SetActive(false);
        if (Application.isPlaying) UnityEngine.Object.Destroy(物); else UnityEngine.Object.DestroyImmediate(物);
    }
    public void Dispose()
    {
        if (已销毁) return; 已销毁 = true; 等待.Clear();
        Array.Clear(显示, 0, 显示.Length); if (根 != null) 删除(根.gameObject);
    }
}
