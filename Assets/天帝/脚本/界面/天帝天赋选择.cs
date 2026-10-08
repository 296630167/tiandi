using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class 天帝天赋选择 : MonoBehaviour
{
    public bool 可选择 { get; private set; }
    public int 选中槽位 { get; private set; } = -1;
    readonly RectTransform[] 石纹 = new RectTransform[5];
    readonly CanvasGroup[] 石透明 = new CanvasGroup[5], 文透明 = new CanvasGroup[5];
    readonly Button[] 卡按钮 = new Button[5];
    readonly Image[] 卡背景 = new Image[5];
    readonly Outline[] 卡选中边 = new Outline[5];
    readonly Text[] 名字 = new Text[5], 效果 = new Text[5], 倾向 = new Text[5], 标记 = new Text[5], 纹字 = new Text[5];
    readonly 天帝源道纹绘图[] 图纹 = new 天帝源道纹绘图[5];
    readonly RectTransform[] 衔接卡 = new RectTransform[5];
    readonly CanvasGroup[] 衔接透明 = new CanvasGroup[5];
    readonly 天帝源道纹绘图[] 衔接图纹 = new 天帝源道纹绘图[5];
    readonly Image[] 选中线 = new Image[5];
    RectTransform 浮窗;
    RectTransform 浮窗卡区;
    天帝道纹详情卡 浮窗卡;
    RectTransform 详情遮罩;
    public bool 详情已打开 => 详情遮罩 != null && 详情遮罩.gameObject.activeSelf;
    Text 提示, 次数;
    Text 固定详情标题, 固定详情效果, 固定详情说明, 固定详情倾向;
    Button 刷新按钮, 确认按钮;
    天帝游戏 游戏;
    bool 衔接序章;
    bool 山水天赋;
    int 悬停槽位 = -1;
    static readonly Color 金 = 天帝道纹美术.金墨, 浅 = 天帝道纹美术.正文;

    public void 初始化(天帝游戏 游戏, bool 衔接序章 = false)
    {
        this.游戏 = 游戏; this.衔接序章 = 衔接序章; var 根 = (RectTransform)transform;
        if (天帝剪纸界面皮肤.已启用) { 初始化山水天赋(根); return; }
        if (衔接序章) { 初始化序章选择(根); return; }
        图(根, "夜空", 0, 0, 1600, 900, new Color(0.035f, 0.065f, 0.08f));
        for (int i = 0; i < 7; i++) 图(根, "天光", 0, 175 + i * 75, 1600, 75, new Color(0.065f + i * 0.008f, 0.12f + i * 0.008f, 0.14f + i * 0.006f));
        for (int i = 0; i < 17; i++)
        {
            var 星 = 图(根, "星点", 63 + (i * 137) % 1470, 65 + (i * 83) % 160, i % 3 + 2, i % 3 + 2, new Color(0.8f, 0.8f, 0.65f, 0.22f));
            星.localRotation = Quaternion.Euler(0, 0, 45);
        }
        var 云海 = 游戏.美术 != null ? 游戏.美术.获取("CB01") : null;
        if (云海 != null)
        {
            var 背景 = 图(根, "天赋云海插画", 0, 0, 1600, 900, Color.white).GetComponent<Image>();
            背景.sprite = 云海;
        }
        if (天帝道纹美术.彩绘皮肤) 图(根, "天赋标题衬底", 90, 28, 1420, 145, Color.white);
        字(根, "序章 · 天赋初醒", 150, 39, 1300, 38, 20, 金);
        字(根, "选择你的源道纹", 150, 72, 1300, 58, 36, 浅);
        字(根, "比较天赋效果，选择本局的构筑起点", 150, 130, 1300, 32, 19, 天帝道纹美术.次文);
        按钮(根, "返回标题", "返回", 34, 30, 105, 50, 游戏.返回标题, false);
        for (int i = 0; i < 5; i++)
        {
            int 槽 = i; float x = 106 + i * 282;
            var 卡 = 图(根, "源道纹-" + i, x, 210, 260, 415, new Color(0.10f, 0.18f, 0.20f));
            卡背景[i] = 卡.GetComponent<Image>(); 卡背景[i].raycastTarget = true;
            建选中边(卡, i);
            var 键 = 卡.gameObject.AddComponent<Button>(); 天帝按钮声音.绑定(键); 键.targetGraphic = 卡背景[i]; 键.interactable = false; 卡按钮[i] = 键;
            var 色 = 键.colors; 色.highlightedColor = new Color(1.2f, 1.2f, 1.08f); 色.pressedColor = new Color(0.8f, 0.85f, 0.8f); 色.disabledColor = Color.white; 键.colors = 色;
            键.onClick.AddListener(() => 选中(槽));
            var 悬停 = 卡.gameObject.AddComponent<天帝源道纹悬停>(); 悬停.页面 = this; 悬停.编号 = i;
            图(卡, "上沿", 0, 0, 260, 2, 金);
            石纹[i] = 区(卡, "石质天赋道纹", 80, 18, 100, 100);
            图纹[i] = 石纹[i].gameObject.AddComponent<天帝源道纹绘图>(); 图纹[i].封印 = false; 图纹[i].天赋样式 = true; 图纹[i].raycastTarget = false;
            石透明[i] = 石纹[i].gameObject.AddComponent<CanvasGroup>(); 石透明[i].alpha = 0;
            纹字[i] = 字(石纹[i], "", 25, 54, 100, 50, 31, 金);
            var 文 = 区(卡, "天赋说明", 0, 126, 260, 276); 文透明[i] = 文.gameObject.AddComponent<CanvasGroup>(); 文透明[i].alpha = 0;
            名字[i] = 字(文, "", 10, 0, 240, 42, 29, 金);
            效果[i] = 字(文, "", 18, 52, 224, 88, 24, 浅); 效果[i].fontStyle = FontStyle.Bold;
            倾向[i] = 字(文, "", 10, 156, 240, 34, 18, 天帝道纹美术.次文);
            var 状态 = 图(文, "选择状态", 24, 220, 212, 39, new Color(0.25f, 0.32f, 0.29f));
            标记[i] = 字(状态, "查看并选择", 0, 0, 212, 39, 19, 浅);
        }
        if (天帝道纹美术.彩绘皮肤) 图(根, "天赋操作衬底", 90, 682, 1420, 195, Color.white);
        提示 = 字(根, "天赋道纹正在降临……", 120, 691, 1360, 43, 23, 金);
        次数 = 字(根, "", 120, 738, 1360, 30, 19, new Color(0.59f, 0.68f, 0.65f));
        刷新按钮 = 按钮(根, "刷新天赋", "刷新天赋", 530, 792, 250, 60, () => 游戏.刷新天赋(), false);
        确认按钮 = 按钮(根, "确认天赋", "确认天赋", 820, 792, 250, 60, 确认选中, true);
        刷新按钮.interactable = 确认按钮.interactable = false;
        浮窗 = 图(根, "天赋详情浮窗", 0, 0, 460, 420, new Color(0, 0, 0, 0.9f));
        浮窗卡区 = 区(浮窗, "悬停分层详情", 0, 0, 460, 620);
        浮窗卡 = 浮窗卡区.gameObject.AddComponent<天帝道纹详情卡>();
        浮窗卡.初始化(游戏.默认字体);
        浮窗.gameObject.SetActive(false);
        刷新候选(); 布局移动天赋(根); StartCoroutine(降临());
    }
    void 初始化山水天赋(RectTransform 根)
    {
        山水天赋 = 衔接序章 = true;
        var 背景 = 图(根, "天赋山水背景", 0, 0, 1600, 900, Color.white).GetComponent<Image>();
        天帝辅助页山水.纸(背景, "天赋背景", "剪纸界面/标题山水背景", false);
        var 标题 = 字(根, "选择你的开局天赋", 250, 26, 1100, 80, 54, 天帝剪纸界面皮肤.墨);
        标题.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
        提示 = 字(根, "五条可能的人生，先选一个起点。", 280, 128, 1040, 40, 23, 天帝剪纸界面皮肤.墨);
        次数 = 字(根, "", 580, 166, 440, 24, 16, 天帝剪纸界面皮肤.次墨);
        次数.gameObject.SetActive(false);
        for (int i = 0; i < 5; i++)
        {
            int 槽 = i;
            var 卡 = 图(根, "源道纹-" + i, 190 + i * 248, 196, 222, 332, Color.white);
            衔接卡[i] = 卡; 卡背景[i] = 卡.GetComponent<Image>(); 卡背景[i].raycastTarget = true;
            天帝辅助页山水.纸(卡背景[i], "天赋卡片", "剪纸界面/卡片纸框", false);
            衔接透明[i] = 卡.gameObject.AddComponent<CanvasGroup>(); 衔接透明[i].alpha = 0;
            建选中边(卡, i);
            var 键 = 卡.gameObject.AddComponent<Button>(); 天帝按钮声音.绑定(键);
            键.targetGraphic = 卡背景[i]; 键.transition = Selectable.Transition.None; 卡按钮[i] = 键;
            键.onClick.AddListener(() => 选中(槽)); 键.interactable = false;
            var 悬停 = 卡.gameObject.AddComponent<天帝源道纹悬停>(); 悬停.页面 = this; 悬停.编号 = i;
            var 图标区 = 区(卡, "源道纹图标", 43, 22, 136, 136);
            衔接图纹[i] = 图标区.gameObject.AddComponent<天帝源道纹绘图>();
            衔接图纹[i].天赋样式 = true; 衔接图纹[i].封印 = false; 衔接图纹[i].raycastTarget = false;
            var 名底 = 图(卡, "天赋名称纸签", 32, 182, 158, 42, Color.white).GetComponent<Image>();
            天帝辅助页山水.纸(名底, "墨绿按钮", "剪纸界面/墨绿按钮", false);
            名字[i] = 字(卡, "", 36, 182, 150, 42, 28, new Color(.99f,.97f,.88f));
            效果[i] = 字(卡, "", 18, 230, 186, 86, 18, 天帝剪纸界面皮肤.墨);
            效果[i].font=游戏.默认字体;效果[i].fontStyle=FontStyle.Normal;效果[i].lineSpacing=1;
            效果[i].horizontalOverflow=HorizontalWrapMode.Wrap;
            效果[i].verticalOverflow = VerticalWrapMode.Truncate;
            倾向[i] = 字(卡, "", 20, 294, 182, 20, 14, 天帝剪纸界面皮肤.次墨);
            倾向[i].gameObject.SetActive(false);
            标记[i] = 字(卡, "", 155, 8, 62, 32, 16, 天帝剪纸界面皮肤.朱红);
            选中线[i] = 图(卡, "选中标记", 24, 316, 174, 2, 天帝剪纸界面皮肤.朱红).GetComponent<Image>();
            选中线[i].gameObject.SetActive(false);
        }
        var 详情区 = 图(根, "天赋详情区", 142, 548, 1316, 176, Color.white);
        天帝辅助页山水.纸(详情区.GetComponent<Image>(), "长卷详情", "山水首两页/结果框", false);
        固定详情标题 = 字(详情区, "", 46, 35, 242, 82, 41, 天帝剪纸界面皮肤.墨);
        固定详情标题.font = 游戏.美术?.主页标题字体 ?? 游戏.默认字体;
        固定详情效果 = 字(详情区, "", 326, 30, 586, 43, 23, 天帝剪纸界面皮肤.墨);
        固定详情说明 = 字(详情区, "", 326, 76, 586, 70, 19, 天帝剪纸界面皮肤.墨);
        固定详情效果.alignment = 固定详情说明.alignment = TextAnchor.MiddleLeft;
        var 规则 = 字(详情区, "源纹固定中心 · 确认后本局不可更换", 952, 25, 320, 52, 18, 天帝剪纸界面皮肤.墨);
        规则.alignment = TextAnchor.MiddleLeft;
        固定详情倾向 = 字(详情区, "", 952, 80, 320, 74, 16, 天帝剪纸界面皮肤.次墨);
        固定详情倾向.alignment = TextAnchor.MiddleLeft;
        刷新按钮 = 按钮(根, "刷新天赋", "刷新天赋", 284, 752, 300, 80, () => 游戏.刷新天赋(), false);
        确认按钮 = 按钮(根, "确认天赋", "确认选择", 616, 744, 368, 88, 确认选中, true);
        var 返回 = 按钮(根, "返回标题", "返回标题", 1016, 752, 300, 80, 游戏.返回标题, false);
        天帝辅助页山水.按钮(刷新按钮); 天帝辅助页山水.按钮(确认按钮, true); 天帝辅助页山水.按钮(返回);
        foreach (var 键 in new[] { 刷新按钮, 确认按钮, 返回 }) 键.GetComponentInChildren<Text>().fontSize = 28;
        可选择 = false; 刷新按钮.interactable = false; 刷新候选();
        if (天帝移动适配.启用)
        {
            var 卡列 = new System.Collections.Generic.List<RectTransform>(衔接卡);
            天帝响应布局.横向卡列(根, 卡列, .025f, .22f, .95f, .43f);
            天帝响应布局.比例(详情区, .025f, .67f, .95f, .18f);
            天帝响应布局.比例((RectTransform)刷新按钮.transform, .14f, .87f, .22f, .11f);
            天帝响应布局.比例((RectTransform)确认按钮.transform, .38f, .87f, .26f, .11f);
            天帝响应布局.比例((RectTransform)返回.transform, .66f, .87f, .22f, .11f);
        }
        StartCoroutine(山水降临());
    }
    IEnumerator 山水降临()
    {
        float 秒 = 0;
        while (秒 < .8f)
        {
            秒 += Time.unscaledDeltaTime;
            for (int i = 0; i < 5; i++) 衔接透明[i].alpha = Mathf.Clamp01((秒 - i * .08f) / .4f);
            yield return null;
        }
        可选择 = true; foreach (var 键 in 卡按钮) 键.interactable = true;
        foreach (var 组 in 衔接透明) 组.alpha = 1;
        刷新按钮.interactable = true; 更新山水按键();
    }
    void 更新山水按键()
    {
        if (!山水天赋) return;
        天帝辅助页山水.按钮(刷新按钮); 天帝辅助页山水.按钮(确认按钮, true);
        提示.text="五条可能的人生 · 已刷新 "+游戏.天赋池.刷新次数+" 次 · 选择一枚作为起点";
        for (int i = 0; i < 5; i++)
        {
            名字[i].color = new Color(.99f,.97f,.88f);
            标记[i].text = i == 选中槽位 ? "已选" : "";
        }
    }
    void 初始化序章选择(RectTransform 根)
    {
        var 背景 = 图(根, "漫画终页山路", 0, 0, 1600, 900, Color.white).GetComponent<Image>();
        背景.sprite = 游戏.美术 != null ? 游戏.美术.获取("CB01") : null;
        图(根, "场景压暗", 0, 0, 1600, 900, new Color(.01f, .035f, .04f, .25f));
        图(根, "标题衬底", 0, 0, 1600, 145, new Color(.02f, .055f, .065f, .78f));
        字(根, "序章终页 · 第一笔由你落下", 90, 20, 1420, 32, 20, 金);
        字(根, "选择你的开局天赋", 90, 49, 1420, 54, 35, 浅);
        提示 = 字(根, "五枚源道纹停在面前。它们正在等待你的回应……", 90, 107, 1420, 30, 19, 浅);
        var 详情区 = 图(根, "天赋详情区", 0, 640, 1600, 260, new Color(.025f, .06f, .07f, .94f));
        固定详情标题 = 字(详情区, "", 310, 10, 970, 46, 28, 金);
        固定详情效果 = 字(详情区, "", 310, 62, 970, 48, 25, 浅); 固定详情效果.fontStyle = FontStyle.Bold;
        固定详情说明 = 字(详情区, "", 310, 126, 970, 62, 18, 天帝道纹美术.次文);
        固定详情倾向 = 字(详情区, "", 310, 201, 970, 28, 16, 天帝道纹美术.次文);
        固定详情标题.alignment = 固定详情效果.alignment = 固定详情说明.alignment = 固定详情倾向.alignment = TextAnchor.MiddleLeft;
        按钮(详情区, "返回标题", "返回", 42, 189, 118, 54, 游戏.返回标题, false);
        刷新按钮 = 按钮(详情区, "刷新天赋", "刷新天赋", 176, 189, 118, 54, () => 游戏.刷新天赋(), false);
        刷新按钮.GetComponentInChildren<Text>().fontSize = 19;
        确认按钮 = 按钮(详情区, "确认天赋", "确认选择", 1324, 178, 226, 64, 确认选中, true);
        for (int i = 0; i < 5; i++)
        {
            int 槽 = i;
            var 卡 = 图(根, "源道纹-" + i, 800, 370, 260, 390, 普通卡色()); 衔接卡[i] = 卡;
            衔接透明[i] = 卡.gameObject.AddComponent<CanvasGroup>(); 衔接透明[i].alpha = 0;
            卡背景[i] = 卡.GetComponent<Image>(); 卡背景[i].raycastTarget = true;
            建选中边(卡, i);
            var 键 = 卡.gameObject.AddComponent<Button>(); 天帝按钮声音.绑定(键); 键.targetGraphic = 卡背景[i]; 键.transition = Selectable.Transition.None;
            键.onClick.AddListener(() => 选中(槽)); 卡按钮[i] = 键;
            var 悬停 = 卡.gameObject.AddComponent<天帝源道纹悬停>(); 悬停.页面 = this; 悬停.编号 = i;
            var 图标区 = 区(卡, "源道纹图标", 80, 24, 100, 100);
            衔接图纹[i] = 图标区.gameObject.AddComponent<天帝源道纹绘图>(); 衔接图纹[i].天赋样式 = true; 衔接图纹[i].封印 = false; 衔接图纹[i].raycastTarget = false;
            图(卡, "漫画卡上沿", 0, 0, 260, 3, 金);
            字(卡, (i + 1).ToString("00"), 14, 12, 36, 26, 17, 金);
            名字[i] = 字(卡, "", 10, 134, 240, 38, 25, 浅);
            效果[i] = 字(卡, "", 18, 184, 224, 96, 24, 浅); 效果[i].fontStyle = FontStyle.Bold;
            倾向[i] = 字(卡, "", 10, 287, 240, 34, 17, 天帝道纹美术.次文);
            标记[i] = 字(卡, "点击查看并选择", 10, 351, 240, 28, 17, 金);
            选中线[i] = 图(卡, "选中标记", 10, 380, 240, 4, 天帝道纹美术.强调).GetComponent<Image>();
            选中线[i].gameObject.SetActive(false);
        }
        可选择 = false; 刷新候选();
        foreach (var 键 in 卡按钮) 键.interactable = false;
        刷新按钮.interactable = false;
        布局移动天赋(根); StartCoroutine(序章源纹降临());
    }
    IEnumerator 序章源纹降临()
    {
        float 秒 = 0;
        while (秒 < 2.4f)
        {
            秒 += Time.unscaledDeltaTime;
            for (int i = 0; i < 5; i++)
            {
                float 延迟 = i * .16f;
                float t = Mathf.Clamp01((秒 - 延迟) / 1.35f);
                float 缓动 = 1 - Mathf.Pow(1 - t, 3);
                var 卡 = 衔接卡[i];
                if (卡 == null) continue;
                if (!天帝移动适配.启用) 天帝响应布局.设计位置(卡, Vector2.Lerp(new Vector2(800 + (i - 2) * 24, -80 - i * 12), new Vector2(80 + i * 300, -200), 缓动));
                卡.localScale = Vector3.one * Mathf.Lerp(.18f, 1f, 缓动);
                卡.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp((i - 2) * 18, 0, 缓动));
                衔接透明[i].alpha = Mathf.Clamp01(t * 2.6f);
            }
            yield return null;
        }
        for (int i = 0; i < 5; i++)
        {
            if (!天帝移动适配.启用) 天帝响应布局.设计位置(衔接卡[i], new Vector2(80 + i * 300, -200));
            衔接卡[i].localScale = Vector3.one;
            衔接卡[i].localRotation = Quaternion.identity;
            衔接透明[i].alpha = 1;
        }
        可选择 = true;
        foreach (var 键 in 卡按钮) 键.interactable = true;
        刷新按钮.interactable = true;
        提示.text = "五条可能的人生，先选一个起点。查看下方详情，确认后启程。";
    }
    Color 普通卡色() => 天帝道纹美术.已接入 ? new Color(1,1,1,衔接序章 ? .90f : 1f) : new Color(.055f, .12f, .135f, 衔接序章 ? .90f : 1);
    Color 选中卡色() => 天帝道纹美术.已接入 ? new Color(.80f,.94f,.88f,1f) : new Color(.20f, .28f, .23f, .98f);
    void 建选中边(RectTransform 卡, int i)
    {
        卡选中边[i] = 卡.gameObject.AddComponent<Outline>(); 卡选中边[i].effectColor = 天帝道纹美术.强调;
        卡选中边[i].effectDistance = new Vector2(2, -2); 卡选中边[i].useGraphicAlpha = false; 卡选中边[i].enabled = false;
    }
    public void 刷新候选()
    {
        if (游戏.天赋池 == null) return;
        选中槽位 = 悬停槽位 = -1; 隐藏详情(); 关闭选中详情();
        for (int i = 0; i < 5; i++)
        {
            var 天赋 = 游戏.天赋池.候选[i];
            名字[i].text = 天赋.名称;
            天帝界面美术.标题(名字[i]);
            if (衔接序章) { 名字[i].color = 浅; 选中线[i].gameObject.SetActive(false); 效果[i].text = 天赋.效果; 倾向[i].text = 天赋.倾向; 标记[i].text = "点击查看并选择"; }
            else { 效果[i].text = 天赋.效果; 倾向[i].text = 天赋.倾向; 标记[i].text = "查看并选择"; }
            if (纹字[i] != null) 纹字[i].text = 天帝美术资源.已接入 ? "" : 天赋.名称.Substring(0, 1);
            if (图纹[i] != null) { 图纹[i].编号 = 天赋.编号; 图纹[i].SetVerticesDirty(); }
            if (衔接图纹[i] != null) { 衔接图纹[i].编号 = 天赋.编号; 衔接图纹[i].SetVerticesDirty(); }
            卡背景[i].color = 普通卡色();
            卡选中边[i].enabled = false;
        }
        确认按钮.interactable = false;
        确认按钮.GetComponentInChildren<Text>().text = "先选择天赋";
        if (山水天赋) 次数.text = "已刷新 " + 游戏.天赋池.刷新次数 + " 次";
        if (衔接序章) { 更新固定详情(); if (可选择) 提示.text = "五条可能的人生 · 已刷新 " + 游戏.天赋池.刷新次数 + " 次 · 选择一枚作为起点"; }
        else
        {
            次数.text = "天赋池 " + 天帝天赋.全部.Count + " 种  ·  已刷新 " + 游戏.天赋池.刷新次数 + " 次";
            if (可选择) 提示.text = "选择一枚天赋，或刷新寻找想玩的构筑。";
        }
        更新山水按键();
    }
    public void 选中(int 槽)
    {
        if (!可选择 || 槽 < 0 || 槽 >= 5 || 游戏.天赋池.已选天赋 != null) return;
        选中槽位 = 槽;
        for (int i = 0; i < 5; i++)
        {
            卡背景[i].color = i == 槽 ? 选中卡色() : 普通卡色();
            卡选中边[i].enabled = i == 槽;
            if (衔接序章) { 选中线[i].gameObject.SetActive(i == 槽); 名字[i].color = i == 槽 ? 金 : 浅; 标记[i].text = i == 槽 ? "已选中 · 等待确认" : "点击查看并选择"; }
            else 标记[i].text = i == 槽 ? "已选中 · 下方确认" : "查看并选择";
        }
        if (衔接序章) 更新固定详情();
        else 提示.text = "已选「" + 游戏.天赋池.候选[槽].名称 + "」 · 确认后本局不可更换";
        确认按钮.interactable = true;
        确认按钮.GetComponentInChildren<Text>().text = "确认「" + 游戏.天赋池.候选[槽].名称 + "」";
        if (!衔接序章) 显示选中详情();
        更新山水按键();
    }
    void 显示选中详情()
    {
        隐藏详情(); 关闭选中详情();
        var 根 = (RectTransform)transform;
        详情遮罩 = 图(根, "源道纹详情遮罩", 0, 0, 1600, 900, new Color(.01f, .025f, .035f, .78f));
        详情遮罩.GetComponent<Image>().raycastTarget = true;
        var 框 = 图(详情遮罩, "源道纹详情面板", 440, 155, 720, 590, new Color(.035f, .075f, .078f));
        var 天赋 = 游戏.天赋池.候选[选中槽位];
        var 图标 = 区(框, "详情天赋图标", 38, 24, 90, 90).gameObject.AddComponent<天帝源道纹绘图>();
        图标.编号 = 天赋.编号; 图标.天赋样式 = true; 图标.封印 = false; 图标.raycastTarget = false;
        字(框, 天赋.名称, 146, 24, 392, 58, 34, 浅).alignment = TextAnchor.MiddleLeft;
        字(框, "构筑倾向 · " + 天赋.倾向, 146, 85, 520, 32, 18, 天帝道纹美术.次文).alignment = TextAnchor.MiddleLeft;
        按钮(框, "关闭源道纹详情", "关闭", 560, 24, 122, 42, 关闭选中详情, false);
        var 效果框 = 图(框, "天赋效果面板", 38, 145, 644, 118, Color.white);
        字(效果框, "天赋效果", 20, 10, 604, 30, 18, 天帝道纹美术.次文).alignment = TextAnchor.MiddleLeft;
        var 效果正文 = 字(效果框, 天赋.效果, 20, 44, 604, 64, 26, 浅); 效果正文.fontStyle = FontStyle.Bold; 效果正文.alignment = TextAnchor.MiddleLeft;
        字(框, 天赋.说明, 38, 286, 644, 62, 19, 天帝道纹美术.次文).alignment = TextAnchor.MiddleLeft;
        字(框, "源纹固定中心 · 确认后本局不可更换", 38, 358, 466, 32, 17, 天帝道纹美术.次文).alignment = TextAnchor.MiddleLeft;
        var 解封 = 字(框, "初始开放右接口。10/20/30/40/50级依次解封右上、左上、左、左下、右下接口。", 38, 408, 644, 72, 17, 天帝道纹美术.次文); 解封.gameObject.SetActive(false);
        按钮(框, "天赋解封规则", "解封规则", 538, 358, 144, 34, () => 解封.gameObject.SetActive(!解封.gameObject.activeSelf), false);
        var 返回键=按钮(框, "返回选择", "返回选择", 74, 506, 260, 58, 关闭选中详情, false);
        var 确认键=按钮(框, "确认源道纹", "确认「" + 天赋.名称 + "」", 386, 506, 260, 58, 确认选中, true);
        if(天帝移动适配.启用)
        {
            var 口=天帝响应布局.滚动列(框,"天赋效果说明",38,145,644,335);
            var 内容=口.GetComponent<ScrollRect>().content;
            var 重排=天帝双端页面布局.重排正文(内容);
            天帝双端页面布局.移动页(框,面板=>
            {
                float 宽=面板.rect.width,高=面板.rect.height;
                天帝双端页面布局.按键(面板.Find("关闭源道纹详情") as RectTransform,宽-112,4,100);
                天帝双端页面布局.固定(图标.rectTransform,12,8,56,56);
                foreach(Transform 子 in 面板)
                {
                    var 文=子.GetComponent<Text>();if(文==null)continue;
                    bool 名称=文.text==天赋.名称;
                    天帝双端页面布局.固定(文.rectTransform,80,名称?4:38,宽-208,名称?32:28);文.fontSize=名称?22:14;
                }
                天帝双端页面布局.固定(口,12,76,宽-24,高-136);
                重排();
                天帝双端页面布局.按键((RectTransform)返回键.transform,12,高-48,(宽-30)*.5f);
                天帝双端页面布局.按键((RectTransform)确认键.transform,18+(宽-30)*.5f,高-48,(宽-30)*.5f);
            });
        }
    }
    public void 关闭选中详情()
    {
        if (详情遮罩 == null) return;
        详情遮罩.gameObject.SetActive(false); if (Application.isPlaying) Destroy(详情遮罩.gameObject); else DestroyImmediate(详情遮罩.gameObject); 详情遮罩 = null;
    }
    void 确认选中()
    {
        if (!可选择 || 选中槽位 < 0) return;
        游戏.选择天赋(游戏.天赋池.候选[选中槽位].编号, 游戏.天赋池.轮次);
    }
    IEnumerator 降临()
    {
        float 秒 = 0;
        while (秒 < 2.3f)
        {
            秒 += Time.unscaledDeltaTime;
            for (int i = 0; i < 5; i++)
            {
                float t = Mathf.Clamp01((秒 - i * 0.15f) / 1.5f), 缓动 = 1 - Mathf.Pow(1 - t, 3);
                if (!天帝移动适配.启用) 天帝响应布局.设计位置(石纹[i], Vector2.Lerp(new Vector2(680 - i * 180, 500 + i * 28), new Vector2(80, -18), 缓动));
                石纹[i].localScale = Vector3.one * Mathf.Lerp(0.13f, 1, 缓动);
                石纹[i].localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(22 - i * 9, 0, 缓动));
                石透明[i].alpha = Mathf.Clamp01(t * 3); 文透明[i].alpha = Mathf.Clamp01((t - 0.7f) / 0.3f);
            }
            yield return null;
        }
        可选择 = true; foreach (var 键 in 卡按钮) 键.interactable = true;
        刷新按钮.interactable = true; 提示.text = "选择一枚天赋，或刷新寻找想玩的构筑。";
    }
    public void 显示详情(int 槽, Vector2 屏幕)
    {
        if (!可选择 || 详情已打开 || 槽 < 0 || 槽 >= 5 || 游戏.天赋池 == null) return;
        if (衔接序章)
        {
            if (悬停槽位 != 槽) { 悬停槽位 = 槽; 更新固定详情(); }
            return;
        }
        if (浮窗 == null) return;
        浮窗卡.设置(天帝道纹.创建天赋源纹(游戏.天赋池.候选[槽]));
        浮窗卡区.gameObject.SetActive(true);
        float 高 = Mathf.Clamp(浮窗卡.高度, 360, 760);
        浮窗.sizeDelta = new Vector2(460, 高);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, 屏幕, null, out var 点);
        浮窗.anchoredPosition = new Vector2(Mathf.Clamp(点.x + 18, 8, 1132), Mathf.Clamp(点.y + 高 + 20, 高 - 892, -8));
        浮窗.SetAsLastSibling(); 浮窗.gameObject.SetActive(true);
    }
    public void 隐藏详情()
    {
        if (浮窗 != null) 浮窗.gameObject.SetActive(false);
        if (!衔接序章) return;
        悬停槽位 = -1;
        更新固定详情();
    }
    void 更新固定详情()
    {
        if (!衔接序章 || 固定详情标题 == null || 游戏?.天赋池 == null) return;
        int 槽 = 悬停槽位 >= 0 ? 悬停槽位 : 选中槽位;
        var 天赋 = 槽 >= 0 ? 游戏.天赋池.候选[槽] : null;
        固定详情标题.text = 天赋 == null ? "选择一枚源道纹" : 天赋.名称 + (槽 == 选中槽位 ? "  ·  已选定" : "  ·  预览");
        固定详情效果.text = 天赋 == null ? "" : 天赋.效果;
        固定详情说明.text = 天赋 == null ? "天赋决定本局的源道纹起点。可以刷新候选，五枚均可选择；确认之后，再把自己的路一步步连接起来。" : 天赋.说明;
        固定详情倾向.text = 天赋 == null ? "" : "构筑倾向  " + 天赋.倾向;
        if (山水天赋)
        {
            固定详情标题.text = 天赋 == null ? "选择源纹" : 天赋.名称;
            固定详情倾向.text = "初始开放右接口。10/20/30/40/50级\n依次解封右上、左上、左、左下、右下。";
        }
    }
    RectTransform 区(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    void 布局移动天赋(RectTransform 根)
    {
        if (!天帝移动适配.启用) return;
        var 列 = new System.Collections.Generic.List<RectTransform>();
        foreach (var 背景 in 卡背景) 列.Add(背景.rectTransform);
        var 卡口 = 天帝响应布局.横向卡列(根, 列, .025f, .24f, .95f, 衔接序章 ? .51f : .47f);
        if (!衔接序章)
        {
            天帝响应布局.比例(提示.rectTransform, .04f, .73f, .92f, .08f);
            天帝响应布局.比例(次数.rectTransform, .04f, .81f, .92f, .055f);
            天帝响应布局.比例(刷新按钮.GetComponent<RectTransform>(), .22f, .875f, .26f, .11f);
            天帝响应布局.比例(确认按钮.GetComponent<RectTransform>(), .52f, .875f, .26f, .11f);
            var 说明口 = new RectTransform[5]; var 重排 = new System.Action[5];
            for (int i = 0; i < 5; i++)
            {
                var 文 = (RectTransform)文透明[i].transform;
                说明口[i] = 天帝响应布局.滚动正文(文); 重排[i] = 天帝双端页面布局.重排正文(文);
            }
            var 布局 = 根.gameObject.AddComponent<天帝移动排版>();
            布局.排版 = 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                foreach (Transform 子 in 面板)
                    if (子.GetComponent<Text>() is Text 文 && 文 != 提示 && 文 != 次数)
                    {
                        bool 标题 = 文.text == "选择你的源道纹";
                        文.gameObject.SetActive(标题);
                        if (标题) { 天帝双端页面布局.固定(文.rectTransform, 124, 4, 宽 - 248, 40); 文.fontSize = 24; }
                    }
                天帝双端页面布局.按键(面板.Find("返回标题") as RectTransform, 12, 4, 100);
                天帝双端页面布局.固定(卡口, 12, 54, 宽 - 24, 高 - 168);
                for (int i = 0; i < 5; i++)
                {
                    天帝双端页面布局.固定(石纹[i], 82, 4, 56, 56);
                    天帝双端页面布局.固定(说明口[i], 4, 66, 212, 高 - 238); 重排[i]();
                }
                天帝双端页面布局.固定(提示.rectTransform, 12, 高 - 108, 宽 - 24, 30); 提示.fontSize = 16;
                天帝双端页面布局.固定(次数.rectTransform, 12, 高 - 78, 宽 - 24, 24); 次数.fontSize = 14;
                天帝双端页面布局.按键((RectTransform)刷新按钮.transform, 宽 * .5f - 154, 高 - 48, 148);
                天帝双端页面布局.按键((RectTransform)确认按钮.transform, 宽 * .5f + 6, 高 - 48, 148);
            };
        }
    }
    RectTransform 图(RectTransform 父, string 名, float x, float y, float w, float h, Color 色)
    { var 区 = this.区(父, 名, x, y, w, h); var 像 = 区.gameObject.AddComponent<Image>(); 像.color = 色; 像.raycastTarget = false;天帝界面美术.自动面板(像,名,w,h,色);return 区; }
    Text 字(RectTransform 父, string 文, float x, float y, float w, float h, int 大小, Color 色)
    {
        var 字 = 区(父, "文字", x, y, w, h).gameObject.AddComponent<Text>(); 字.font = 游戏.默认字体; 字.text = 文;
        字.fontSize = 大小; 字.color = 天帝道纹美术.纸面文字(色); 字.alignment = TextAnchor.MiddleCenter; 字.raycastTarget = false;
        字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Overflow; 天帝界面美术.文字(字); return 字;
    }
    Button 按钮(RectTransform 父, string 名, string 文, float x, float y, float w, float h, UnityEngine.Events.UnityAction 点击, bool 主按钮)
    {
        var 区 = 图(父, 名, x, y, w, h, 主按钮 ? new Color(0.60f, 0.43f, 0.22f) : new Color(0.17f, 0.25f, 0.25f));
        var 像 = 区.GetComponent<Image>(); 像.raycastTarget = true;
        var 键 = 区.gameObject.AddComponent<Button>(); 键.targetGraphic = 像; 键.onClick.AddListener(点击);
        字(区, 文, 0, 0, w, h, 23, 浅);天帝界面美术.按钮(键,主按钮);return 键;
    }
    protected void OnApplicationFocus(bool 焦点) { if (!焦点) 隐藏详情(); }
    protected void OnDisable() { 可选择 = false; 隐藏详情(); 关闭选中详情(); StopAllCoroutines(); }
}

public sealed class 天帝源道纹悬停 : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    public 天帝天赋选择 页面;
    public int 编号;
    public void OnPointerEnter(PointerEventData e) { if (!天帝移动适配.启用) 页面.显示详情(编号, e.position); }
    public void OnPointerMove(PointerEventData e) { if (!天帝移动适配.启用) 页面.显示详情(编号, e.position); }
    public void OnPointerExit(PointerEventData e) { if (!天帝移动适配.启用) 页面.隐藏详情(); }
}
