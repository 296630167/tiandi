using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class 天帝界面
{
    RectTransform 主动HUD, 技能区, 生命球区, 灵力球区, 闪避区, 操作反馈区;
    readonly Button[] 技能键 = new Button[6];
    readonly Text[] 技能标签 = new Text[6], 技能状态字 = new Text[6], 技能按键字 = new Text[6];
    readonly CanvasGroup[] 技能透明 = new CanvasGroup[6];
    readonly Image[] 技能冷却遮罩 = new Image[6];
    Image 生命液, 灵力液, 护盾环, 闪避冷却轨道, 闪避冷却进度;
    Text 生命球字, 灵力球字, 护盾球字, 闪避状态字, 操作反馈字, 操作提示字;
    CanvasGroup 闪避透明;
    Button 闪避键;
    float 操作反馈秒, 拒绝音效冷却, 灵力闪色秒;
    int 当前闪色槽 = -1;
    static readonly Color 生命液色 = new Color(.72f, .23f, .18f), 灵力液色 = new Color(.12f, .55f, .64f);
    RectTransform HUD区(RectTransform 父, string 名, float x, float y, float w, float h)
    {
        var r = 区块(父, 名, x, y, w, h); 天帝响应布局.动态(r);
        天帝双端页面布局.固定(r, x, y, w, h); return r;
    }
    Text HUD字(RectTransform 父, string 名, string 文, float x, float y, float w, float h, int 字号, Color 色)
    {
        var t = 字(父, 文, x, y, w, h, 字号, 色); t.name = 名;
        // UGUI 的图标与按钮底图都在同一技能格内，文字始终置于最上层，避免被圆形 Image 盖住。
        t.transform.SetAsLastSibling();
        天帝响应布局.动态(t.rectTransform); 天帝双端页面布局.固定(t.rectTransform, x, y, w, h);
        t.color = 色; t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Normal;
        t.raycastTarget = false; t.resizeTextForBestFit = false; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
    }
    Image HUD图(RectTransform 父, string 名, Sprite 素材, float x, float y, float w, float h, Color 色)
    {
        var r = HUD区(父, 名, x, y, w, h); var i = r.gameObject.AddComponent<Image>();
        i.sprite = 素材; i.color = 色; i.raycastTarget = false; return i;
    }
    void 建立主动战斗HUD()
    {
        bool 手机 = 天帝移动适配.启用;
        主动HUD = 铺满(战斗界面层, "主动战斗HUD"); 天帝响应布局.动态(主动HUD);
        生命球区 = HUD区(主动HUD, "生命灵球", 0, 0, 172, 172);
        灵力球区 = HUD区(主动HUD, "灵力灵球", 0, 0, 172, 172);
        建立资源球(生命球区, false); 建立资源球(灵力球区, true);
        技能区 = HUD区(主动HUD, "六接口技能栏", 0, 0, 608, 114);
        var 轻纸 = Resources.Load<Sprite>("剪纸界面/轻纸框");
        var 圆底 = 手机 ? Resources.Load<Sprite>("山水战斗HUD/灵球液体") : null;
        for (int i = 0; i < 6; i++)
        {
            int 槽 = i; var r = HUD区(技能区, "技能" + (i + 1), 0, 0, 88, 128);
            var 底 = r.gameObject.AddComponent<Image>(); 底.sprite = 手机 ? null : 轻纸; 底.type = 手机 ? Image.Type.Simple : Image.Type.Sliced;
            底.color = 手机 ? Color.clear : Color.white; 底.raycastTarget = true;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = 底; b.transition = Selectable.Transition.None;
            技能键[i] = b; 技能透明[i] = r.gameObject.AddComponent<CanvasGroup>();
            if (手机)
            {
                // 圆形只占触控格的大部分，保留四周战场留白；触控格仍大于可见圆。
                HUD图(r, "圆形技能金边", 圆底, 5, 5, 42, 42, new Color(.82f, .73f, .46f));
                HUD图(r, "圆形技能青玉底", 圆底, 6.5f, 6.5f, 39, 39, new Color(.10f, .30f, .27f));
            }
            var 技能图标 = HUD图(r, "接口道纹图标", Resources.Load<Sprite>("山水首两页/导航_道纹"), 手机 ? 18 : 22, 10, 手机 ? 18 : 44, 手机 ? 18 : 44, Color.white);
            var 冷却片 = HUD图(r, "技能冷却径向进度", 手机 ? 圆底 : 技能图标.sprite, 手机 ? 6.5f : 22, 手机 ? 6.5f : 10, 手机 ? 39 : 44, 手机 ? 39 : 44, new Color(.02f, .07f, .07f, .78f));
            冷却片.type = Image.Type.Filled; 冷却片.fillMethod = Image.FillMethod.Radial360; 冷却片.fillOrigin = 2; 冷却片.fillClockwise = true;
            技能冷却遮罩[i] = 冷却片;
            技能标签[i] = HUD字(r, "接口技能名称", "灵力弹", 手机 ? 4 : 4, 手机 ? 28 : 59, 手机 ? 48 : 80, 手机 ? 16 : 32, 手机 ? 10 : 16, 手机 ? new Color(1, .97f, .84f) : 天帝剪纸界面皮肤.墨);
            技能状态字[i] = HUD字(r, "接口技能状态", "", 手机 ? 4 : 4, 手机 ? 42 : 92, 手机 ? 48 : 80, 手机 ? 12 : 28, 手机 ? 9 : 13, 手机 ? new Color(.83f, .91f, .80f) : 天帝道纹美术.次文);
            var 按键字 = HUD字(r, "接口按键", (i + 1).ToString(), 手机 ? 6 : 3, 手机 ? 4 : 0, 手机 ? 12 : 22, 手机 ? 15 : 30, 手机 ? 10 : 16, 手机 ? new Color(1, .97f, .84f) : 天帝剪纸界面皮肤.墨);
            技能按键字[i] = 按键字;
            if (手机)
            {
                // 触屏格内的技能名、冷却和序号按实际字体度量自适应，长名不裁切。
                foreach (var 文 in new[] { 技能标签[i], 技能状态字[i], 按键字 })
                {
                    文.resizeTextForBestFit = true; 文.resizeTextMinSize = 7; 文.resizeTextMaxSize = 文.fontSize;
                    var 字阴影 = 文.gameObject.AddComponent<Shadow>(); 字阴影.effectColor = new Color(.02f, .08f, .07f, .95f); 字阴影.effectDistance = new Vector2(1, -1);
                }
            }
            if (手机)
            {
                var 触控 = r.gameObject.AddComponent<天帝战斗触控技能>();
                触控.可以操作 = () => 游戏.战斗场景?.可以主动操作 == true;
                触控.瞄准 = 向 => 游戏.战斗场景.设置触控瞄准(向);
                触控.释放 = () => 游戏.战斗场景.释放技能(槽);
            }
            else b.onClick.AddListener(() => { if (Mouse.current != null) 游戏.战斗场景.设置屏幕瞄准(Mouse.current.position.ReadValue()); 游戏.战斗场景.释放技能(槽); });
        }
        闪避区 = HUD区(主动HUD, "闪避技能", 0, 0, 102, 58);
        闪避键 = 按钮(闪避区, "闪避", 0, 0, 102, 58, () => 游戏.战斗场景.闪避(), true);
        闪避键.transition = Selectable.Transition.None; 闪避透明 = 闪避区.gameObject.AddComponent<CanvasGroup>();
        闪避状态字 = 闪避键.GetComponentInChildren<Text>(); 闪避状态字.fontSize = 手机 ? 13 : 18;
        闪避冷却轨道 = HUD图((RectTransform)闪避键.transform, "闪避冷却轨道", null, 10, 49, 82, 3, new Color(.04f, .14f, .13f, .75f));
        天帝响应布局.比例(闪避冷却轨道.rectTransform, .10f, .84f, .80f, .06f);
        闪避冷却进度 = HUD图(闪避冷却轨道.rectTransform, "闪避冷却进度", null, 0, 0, 82, 3, new Color(.62f, .88f, .73f));
        闪避冷却进度.rectTransform.anchorMin = Vector2.zero; 闪避冷却进度.rectTransform.anchorMax = Vector2.one;
        闪避冷却进度.rectTransform.offsetMin = 闪避冷却进度.rectTransform.offsetMax = Vector2.zero;
        闪避冷却轨道.gameObject.SetActive(false);
        闪避状态字.transform.SetAsLastSibling();
        操作反馈区 = HUD区(主动HUD, "主动操作反馈", 0, 0, 380, 46);
        var 提示底 = 操作反馈区.gameObject.AddComponent<Image>(); 提示底.sprite = Resources.Load<Sprite>("山水首两页/墨绿按钮"); 提示底.type = Image.Type.Sliced; 提示底.raycastTarget = false;
        操作反馈字 = HUD字(操作反馈区, "技能失败提示", "", 16, 4, 348, 38, 手机 ? 13 : 22, new Color(1, .96f, .82f));
        操作反馈区.gameObject.SetActive(false);
        操作提示字 = HUD字(主动HUD, "主动战斗操作说明", 手机 ? "拖动技能瞄准 · 松手释放" : "1—6 释放道纹 · 鼠标瞄准 · 空格闪避", 0, 0, 600, 28, 手机 ? 11 : 16, new Color(.99f, .96f, .85f));
        var 阴影 = 操作提示字.gameObject.AddComponent<Shadow>(); 阴影.effectColor = new Color(.02f, .07f, .06f, .9f); 阴影.effectDistance = new Vector2(1, -1);
        操作提示字.gameObject.SetActive(!手机);
        操作反馈秒 = 灵力闪色秒 = 拒绝音效冷却 = 0; 当前闪色槽 = -1;
        更新主动战斗布局();
    }
    void 建立资源球(RectTransform 父, bool 灵)
    {
        var 液 = Resources.Load<Sprite>("山水战斗HUD/灵球液体"); var 框 = Resources.Load<Sprite>("山水战斗HUD/青玉灵球外框");
        HUD图(父, "空资源球", 液, 22, 22, 128, 128, new Color(.045f, .11f, .10f, .92f));
        var 填 = HUD图(父, "真实液位", 液, 22, 22, 128, 128, 灵 ? 灵力液色 : 生命液色);
        填.type = Image.Type.Filled; 填.fillMethod = Image.FillMethod.Vertical; 填.fillOrigin = 0; 填.fillAmount = 1;
        HUD图(父, "青玉灵球外框", 框, 0, 0, 172, 172, Color.white);
        if (!灵)
        {
            护盾环 = HUD图(父, "真实护盾环", 框, -3, -3, 178, 178, new Color(.48f, .88f, .92f, .9f));
            护盾环.type = Image.Type.Filled; 护盾环.fillMethod = Image.FillMethod.Radial360; 护盾环.fillOrigin = 2; 护盾环.fillClockwise = true;
        }
        var 数 = HUD字(父, "真实资源数值", "", 12, 61, 148, 36, 17, new Color(1, .99f, .92f));
        数.horizontalOverflow = HorizontalWrapMode.Overflow;
        var 阴影 = 数.gameObject.AddComponent<Shadow>(); 阴影.effectColor = new Color(.02f, .08f, .06f, 1); 阴影.effectDistance = new Vector2(1, -1);
        HUD字(父, "资源名称", 灵 ? "灵力" : "生命", 40, 105, 92, 36, 17, new Color(1, .97f, .84f));
        if (灵) { 灵力液 = 填; 灵力球字 = 数; }
        else { 生命液 = 填; 生命球字 = 数; 护盾球字 = HUD字(父, "真实护盾数值", "", -12, 153, 196, 25, 15, new Color(.76f, .98f, 1)); }
    }
    void 刷新主动战斗HUD(天帝战斗系统 战, 天帝主角属性 人)
    {
        if (主动HUD == null) return;
        生命液.fillAmount = 人.当前血量 / Mathf.Max(1, 人.血量); 灵力液.fillAmount = 人.当前灵力 / Mathf.Max(1, 人.灵力);
        生命球字.text = 战斗短数(人.当前血量) + " / " + 战斗短数(人.血量);
        灵力球字.text = 战斗短数(人.当前灵力) + " / " + 战斗短数(人.灵力);
        float 盾 = 人.当前灵气护盾 + 战.特性临时护盾;
        护盾环.gameObject.SetActive(盾 > 0); 护盾环.fillAmount = 盾 / Mathf.Max(1, 人.灵气护盾 + 战.特性临时护盾);
        护盾球字.text = "护盾 " + 战斗短数(盾);
        灵力液.color = 灵力闪色秒 > 0 ? Color.Lerp(灵力液色, new Color(1, .45f, .25f), .6f) : 灵力液色;
        for (int i = 0; i < 6; i++)
        {
            bool 有链路 = 战.技能有链路(i), 开 = 战.技能已解封(i);
            float 冷却 = 战.技能冷却剩余;
            bool 可操作非冷却 = 有链路 && 开 && !战.玩家死亡 && !战斗已暂停 && 人.当前灵力 >= 天帝战斗系统.技能灵力消耗;
            bool 可 = 可操作非冷却 && 冷却 <= .00001f;
            // 仅把实际接入初始道纹的技能链路放入战斗栏；隐藏槽位同时关闭按钮和所有被提到技能区顶层的文字。
            技能键[i].gameObject.SetActive(有链路);
            技能标签[i].gameObject.SetActive(有链路);
            技能状态字[i].gameObject.SetActive(有链路);
            技能按键字[i].gameObject.SetActive(有链路);
            技能透明[i].blocksRaycasts = 有链路;
            技能键[i].interactable = 可; 技能透明[i].alpha = 可操作非冷却 ? 1 : .5f;
            技能标签[i].text = 战.技能名称(i);
            bool 手机 = 天帝移动适配.启用;
            bool 正在冷却 = 有链路 && 开 && 冷却 > .00001f;
            技能冷却遮罩[i].gameObject.SetActive(正在冷却);
            技能冷却遮罩[i].fillAmount = 正在冷却 ? Mathf.Clamp01(冷却 / Mathf.Max(.001f, 战.技能冷却总时长)) : 0;
            技能状态字[i].text = !开 ? 天帝道纹.通路解封等级(天帝战斗系统.技能通路(i)) + (手机 ? "级" : "级解封") : 冷却 > .00001f ? 冷却.ToString("0.0") + (手机 ? "" : "秒") : (手机 ? "" : "灵力 ") + 天帝战斗系统.技能灵力消耗.ToString("0");
            技能状态字[i].color = 当前闪色槽 == i && 灵力闪色秒 > 0 ? new Color(.85f, .20f, .12f) : 手机 ? new Color(.83f, .91f, .80f) : 天帝道纹美术.次文;
            // 移动端文字为便于显示会挂到技能区顶层，单独同步 CanvasGroup alpha，禁用态保持整枚技能一致变灰。
            float 文字Alpha = 技能透明[i].alpha;
            var 标签色 = 技能标签[i].color; 标签色.a = 文字Alpha; 技能标签[i].color = 标签色;
            var 状态色 = 技能状态字[i].color; 状态色.a = 文字Alpha; 技能状态字[i].color = 状态色;
            var 按键色 = 技能按键字[i].color; 按键色.a = 文字Alpha; 技能按键字[i].color = 按键色;
        }
        float 闪冷却 = 战.闪避冷却剩余;
        bool 闪可操作非冷却 = !战.玩家死亡 && !战斗已暂停;
        bool 闪可 = 闪可操作非冷却 && 闪冷却 <= .00001f;
        闪避键.interactable = 闪可; 闪避透明.alpha = 闪可操作非冷却 ? 1 : .5f;
        闪避冷却轨道.gameObject.SetActive(闪冷却 > .00001f);
        float 闪进度 = Mathf.Clamp01(1 - 闪冷却 / Mathf.Max(.001f, 战.闪避冷却总时长));
        闪避冷却进度.rectTransform.anchorMax = new Vector2(闪进度, 1);
        闪避状态字.text = 闪冷却 > .00001f ? "闪避 " + 闪冷却.ToString("0.0") : 天帝移动适配.启用 ? "闪避" : "闪避 Space";
    }
    public void 显示战斗操作反馈(战斗操作结果 结果, int 槽, bool 闪)
    {
        if (操作反馈字 == null || 结果 == 战斗操作结果.无法操作) return;
        if (结果 == 战斗操作结果.成功) { if (闪) 显示操作提示("闪避 · 0.1秒无敌", .6f); return; }
        string 文 = 结果 == 战斗操作结果.灵力不足 ? "灵力不足，无法释放" : 结果 == 战斗操作结果.冷却中 ? "尚未就绪" : 结果 == 战斗操作结果.无技能链路 ? "该接口还没有技能链路" : 结果 == 战斗操作结果.未解封 ? "该接口尚未解封" : "请先选择释放方向";
        显示操作提示(文, 1.25f);
        if (结果 == 战斗操作结果.灵力不足) { 灵力闪色秒 = .45f; 当前闪色槽 = 槽; }
        if (拒绝音效冷却 <= 0) { 天帝声音.提示("UI04_拒绝"); 拒绝音效冷却 = .18f; }
    }
    void 显示操作提示(string 文, float 秒)
    { 操作反馈字.text = 文; 操作反馈秒 = 秒; 操作反馈区.gameObject.SetActive(true); }
    public void 推进战斗操作反馈(float 秒)
    {
        操作反馈秒 = Mathf.Max(0, 操作反馈秒 - 秒); 灵力闪色秒 = Mathf.Max(0, 灵力闪色秒 - 秒); 拒绝音效冷却 = Mathf.Max(0, 拒绝音效冷却 - 秒);
        if (操作反馈区 != null && 操作反馈秒 <= 0) 操作反馈区.gameObject.SetActive(false);
    }
    public void 取消战斗手势()
    {
        if (主动HUD != null) foreach (var 手势 in 主动HUD.GetComponentsInChildren<天帝战斗触控技能>(true)) 手势.取消();
        战斗摇杆?.重置输入();
    }
    void 清理主动战斗HUD()
    {
        主动HUD = null; 操作反馈字 = 操作提示字 = null; 操作反馈秒 = 灵力闪色秒 = 0;
        闪避冷却轨道 = 闪避冷却进度 = null;
        for (int i = 0; i < 6; i++) { 技能键[i] = null; 技能透明[i] = null; 技能冷却遮罩[i] = null; 技能标签[i] = 技能状态字[i] = 技能按键字[i] = null; }
    }
    void 更新主动战斗布局()
    {
        if (主动HUD == null) return;
        bool 手机 = 天帝移动适配.启用;
        float w = 战斗界面层.rect.width, h = 战斗界面层.rect.height;
        void 置(RectTransform r, float x, float y, float 宽, float 高) => 天帝双端页面布局.固定(r, x, y, 宽, 高);
        float 球 = 手机 ? 60 : 172;
        void 球位(RectTransform r, float x, float y)
        { 置(r, x, y, 172, 172); r.localScale = Vector3.one * (球 / 172); }
        球位(生命球区, 手机 ? 148 : w * .5f - 500, h - 球 - (手机 ? 12 : 28));
        球位(灵力球区, 手机 ? 222 : w * .5f + 328, h - 球 - (手机 ? 12 : 28));
        // 手机六枚圆形技能保持 56px 触控热区，同时向左扩展一格，给右侧通知和闪避留安全边距。
        置(技能区, 手机 ? w - 184 : w * .5f - 304, h - (手机 ? 128 : 152), 手机 ? 168 : 608, 手机 ? 116 : 128);
        for (int i = 0; i < 6; i++)
        {
            float x = 手机 ? i % 3 * 56 : i * 102, y = 手机 ? i / 3 * 56 : 0;
            var r = (RectTransform)技能键[i].transform; 置(r, x, y, 手机 ? 56 : 88, 手机 ? 56 : 128); r.localScale = Vector3.one;
            if (手机)
            {
                // 技能文字放到栏的顶层，避免圆形按钮的绘制/裁剪层盖住中文与序号。
                foreach (var 文 in new[] { 技能标签[i], 技能状态字[i], 技能按键字[i] })
                {
                    if (文.transform.parent != 技能区) 文.transform.SetParent(技能区, false);
                }
                天帝双端页面布局.固定(技能标签[i].rectTransform, x + 4, y + 28, 48, 16);
                天帝双端页面布局.固定(技能状态字[i].rectTransform, x + 4, y + 42, 48, 12);
                天帝双端页面布局.固定(技能按键字[i].rectTransform, x + 6, y + 4, 12, 15);
                技能标签[i].fontSize = 8; 技能状态字[i].fontSize = 7; 技能按键字[i].fontSize = 9;
                foreach (var 文 in new[] { 技能标签[i], 技能状态字[i], 技能按键字[i] })
                {
                    文.font = 游戏.默认字体; 文.color = Color.white; 文.fontStyle = FontStyle.Bold;
                    文.horizontalOverflow = HorizontalWrapMode.Overflow; 文.verticalOverflow = VerticalWrapMode.Overflow;
                    文.resizeTextForBestFit = false;
                    文.canvasRenderer.cull = false; 文.enabled = true;
                }
                技能标签[i].transform.SetAsLastSibling(); 技能状态字[i].transform.SetAsLastSibling(); 技能按键字[i].transform.SetAsLastSibling();
            }
        }
        置(闪避区, 手机 ? 342 : w * .5f + 516, h - (手机 ? 51 : 103), 手机 ? 54 : 102, 手机 ? 39 : 58);
        天帝响应布局.比例((RectTransform)闪避键.transform, 0, 0, 1, 1);
        置(操作反馈区, w * .5f - (手机 ? 104 : 190), h - (手机 ? 112 : 224), 手机 ? 208 : 380, 手机 ? 32 : 46);
        天帝响应布局.比例(操作反馈字.rectTransform, .045f, .08f, .91f, .84f);
        操作提示字.gameObject.SetActive(!手机);
        置(操作提示字.rectTransform, 手机 ? w - 174 : w * .5f - 304, h - (手机 ? 138 : 190), 手机 ? 168 : 608, 手机 ? 18 : 32);
        var 地图状态 = 战斗界面层.Find("主角战斗状态") as RectTransform;
        置(地图状态, 手机 ? 10 : 18, 手机 ? 8 : 18, 手机 ? 148 : 290, 手机 ? 44 : 76);
        foreach (var 文 in 地图状态.GetComponentsInChildren<Text>())
        { if (文 == 战斗血量) continue; 天帝响应布局.比例(文.rectTransform, .05f, 文 == 战斗经验字 ? .50f : .04f, .9f, .40f); if (手机) 文.fontSize = 文 == 战斗经验字 ? 10 : 11; }
        天帝响应布局.比例(战斗经验底, .05f, .44f, .9f, .035f);
        bool 满级 = 游戏.主角属性.等级 >= 天帝数值.玩家上限;
        战斗条进度(战斗经验条, 战斗经验底, 满级 ? 1 : (float)游戏.道纹数据.当前经验 / Mathf.Max(1, 游戏.道纹数据.升级所需经验));
        var 离开 = 战斗界面层.Find("离开入口"); if (离开 != null) 离开.gameObject.SetActive(false);
        var 跑步 = 战斗界面层.Find("跑步入口"); if (跑步 != null) 跑步.gameObject.SetActive(false);
        var 统计 = 战斗界面层.Find("波次敌人信息") as RectTransform;
        if (手机)
        {
            置(统计, w - 174, 8, 164, 44);
            if (战斗波次 != null)
            {
                天帝响应布局.比例(战斗波次.rectTransform, .05f, .06f, .90f, .88f);
                战斗波次.fontSize = 10;
                战斗波次.alignment = TextAnchor.MiddleLeft;
            }
        }
        var 暂停区 = 战斗界面层.Find("暂停入口") as RectTransform;
        if (手机) 置(暂停区, w - 70, 58, 60, 30);
        var 小图 = 战斗界面层.Find("战斗小地图底") as RectTransform;
        置(小图, 手机 ? 10 : 18, 手机 ? 58 : h - 138, 手机 ? 48 : 112, 手机 ? 48 : 112);
        if (手机 && 战斗小地图 != null) 天帝响应布局.比例(战斗小地图.rectTransform, .08f, .08f, .84f, .84f);
        var 目标 = 战斗界面层.Find("战斗目标") as RectTransform;
        置(目标, 手机 ? 10 : 18, 手机 ? 112 : h - 202, 手机 ? 160 : 274, 手机 ? 24 : 60);
        if (战斗目标 != null)
        {
            战斗目标.fontSize = 手机 ? 10 : 16; 战斗目标.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (手机) 天帝响应布局.比例(战斗目标.rectTransform, .06f, .08f, .88f, .84f);
        }
        // 紧凑拾取提示内部按四行堆叠；移动端放到技能栏左侧，保留完整行高并避免遮挡可点击技能。
        if (拾取列表 != null)
        {
            float 提示宽 = 手机 ? Mathf.Min(238, Mathf.Max(188, w - 230)) : 362;
            float 提示左 = 手机 ? Mathf.Max(10, Mathf.Min(w - 提示宽 - 10, w - 184 - 提示宽 - 8)) : w - 380;
            置(拾取列表.区域, 提示左, 手机 ? 120 : h - 440, 提示宽, 手机 ? 216 : 150);
        }
        if (手机 && 战斗摇杆 != null) 置((RectTransform)战斗摇杆.transform, 10, h - 90, 82, 82);
    }
}
