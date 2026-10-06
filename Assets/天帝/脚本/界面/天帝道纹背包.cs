using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class 天帝道纹背包 : MonoBehaviour
{
    int 每页数量 => 天帝移动适配.启用 ? 6 : 24;
    天帝道纹 数据;
    Font 字体;
    Action<道纹实例> 选择;
    Action 关闭;
    道纹实例 当前选择;
    RectTransform 根, 浮窗;
    Text 汇总字, 页码字, 空提示;
    天帝道纹详情卡 详情卡;
    Button 上页, 下页, 清筛键, 转键;
    readonly List<Dropdown> 筛选下拉 = new List<Dropdown>();
    readonly List<道纹实例> 结果 = new List<道纹实例>();
    readonly List<格显示> 格子 = new List<格显示>();
    int 品阶筛选, 分组筛选, 状态筛选, 排序, 接口筛选;
    bool 旋转接口筛选;
    public int 页码 { get; private set; }
    public int 总页数 => Mathf.Max(1,Mathf.CeilToInt(结果.Count / (float)每页数量));
    public int 筛选结果数 => 结果.Count;
    public bool 详情显示 => 浮窗 != null && 浮窗.gameObject.activeSelf;
    public 道纹实例 显示项(int 格序) => 格序 >= 0 && 格序 < 格子.Count && 格子[格序].键.gameObject.activeSelf ? 格子[格序].纹 : null;
    static readonly Color 卡色 = 天帝道纹美术.行底, 字色 = 天帝道纹美术.正文, 金色 = 天帝道纹美术.金墨;
    sealed class 格显示 { public Button 键; public 天帝道纹绘图 图; public Text 名, 品阶, 状态,图短名, 效果; public Outline 边; public 道纹实例 纹; public 天帝道纹锁定按钮 锁; }

    public void 初始化(天帝道纹 数据,Font 字体,道纹实例 当前选择,Action<道纹实例> 选择,Action 关闭)
    {
        this.数据 = 数据; this.字体 = 字体; this.当前选择 = 当前选择; this.选择 = 选择; this.关闭 = 关闭; 根 = (RectTransform)transform;
        var 遮 = 底(根,"背包遮罩",0,0,1600,900,new Color(0,0,0,.80f));
        var 遮键 = 遮.gameObject.AddComponent<Button>(); 遮键.targetGraphic = 遮.GetComponent<Image>(); 遮键.transition = Selectable.Transition.None; 遮键.onClick.AddListener(() => this.关闭());
        var 框 = 底(根,"道纹背包窗口",200,70,1200,760,new Color(.045f,.08f,.095f));
        if (天帝移动适配.启用) 天帝响应布局.比例(框, .02f, .02f, .96f, .96f);
        字(框,"选择道纹",24,14,500,44,28).fontStyle = FontStyle.Bold;
        键(框,"关闭背包","关闭",1060,16,116,40,() => this.关闭());
        var 品阶选项 = new List<string> { "全部品阶" }; for (int i = 7; i >= 0; i--) 品阶选项.Add(((道纹品阶)i).ToString());
        选项(框,"背包品阶筛选",24,76,品阶选项,v => { 品阶筛选 = v; 重筛(); });
        选项(框,"背包属性筛选",316,76,new List<string> { "全部类别","基础属性","普通属性","功能道纹","元素属性","分叉道纹","特性道纹","转化道纹" },v => { 分组筛选 = v; 重筛(); });
        选项(框,"背包状态筛选",608,76,new List<string> { "全部状态","未放置","已放置" },v => { 状态筛选 = v; 重筛(); });
        选项(框,"背包排序",900,76,new List<string> { "获取顺序","品阶：高到低","属性分组" },v => { 排序 = v; 重筛(); });
        if (!天帝移动适配.启用)
        {
            string[] 维度 = { "品阶", "道纹类别", "放置状态", "排列方式" };
            for (int i = 0; i < 4; i++) 字(框, 维度[i], 24 + i * 292, 55, 276, 20, 14).color = 天帝道纹美术.次文;
        }
        var 接口区=区(框,"高级接口筛选",24,128,568,42);
        选项(接口区,"背包接口筛选",0,0,new List<string> { "全部接口","需要右接口","需要右上接口","需要左上接口","需要左接口","需要左下接口","需要右下接口" },v => 设置接口筛选(v == 0 ? 0 : 1 << (v - 1),旋转接口筛选));
        转键 = 键(接口区,"背包旋转匹配","当前朝向匹配",292,0,276,42,() =>
        { 旋转接口筛选 = !旋转接口筛选; 转键.GetComponentInChildren<Text>().text = 旋转接口筛选 ? "✓ 旋转后也可匹配" : "当前朝向匹配"; 重筛(); });
        接口区.gameObject.SetActive(false);Button 高级=null;
        高级=键(框,"展开接口筛选","接口筛选",24,704,168,40,()=>{bool 开=!接口区.gameObject.activeSelf;接口区.gameObject.SetActive(开);高级.GetComponentInChildren<Text>().text=开?"收起接口筛选":"接口筛选";});
        汇总字 = 字(框,"",608,135,544,28,16);
        空提示 = 字(框,"",24,298,1152,74,22); 空提示.alignment = TextAnchor.MiddleCenter;
        清筛键 = 键(框, "清除背包筛选", "清除筛选", 470, 386, 260, 42, 清除筛选);
        for (int i = 0; i < 每页数量; i++)
        {
            int 序 = i;
            int 列数 = 天帝移动适配.启用 ? 3 : 6;
            var b = 键(框,"背包道纹-" + i,"",24 + i % 列数 * (1164f / 列数),176 + i / 列数 * (天帝移动适配.启用 ? 172 : 128),天帝移动适配.启用 ? 278 : 182,天帝移动适配.启用 ? 154 : 116,() => 选中(序));
            var r = (RectTransform)b.transform;
            var 图 = 区(r,"背包道纹图",8,8,48,48).gameObject.AddComponent<天帝道纹绘图>(); 图.单纹模式 = true; 图.单纹半径 = 22; 图.raycastTarget = false;
            var 格 = new 格显示 { 键 = b,图 = 图,名 = 字(r,"",62,10,78,28,18),品阶 = 字(r,"",62,39,112,22,15),效果 = 字(r,"",8,64,164,24,15),状态 = 字(r,"",8,91,164,22,13),边 = r.gameObject.AddComponent<Outline>(),锁=天帝道纹锁定按钮.创建(r,数据) };
            格.名.fontStyle = FontStyle.Bold; 格.效果.color = 天帝道纹美术.强调;
            格.效果.resizeTextForBestFit = true; 格.效果.resizeTextMinSize = 14; 格.效果.resizeTextMaxSize = 15;
            格.图短名=字(图.rectTransform,"",4,0,40,48,18);格.图短名.alignment=TextAnchor.MiddleCenter;格.图短名.fontStyle=FontStyle.Bold;
            格.品阶.alignment = 格.名.alignment = 格.状态.alignment = 格.效果.alignment = TextAnchor.MiddleCenter; 格.边.effectDistance = new Vector2(1,-1); 格.边.useGraphicAlpha = false;
            格.效果.gameObject.SetActive(!天帝移动适配.启用);
            var 色 = b.colors; 色.disabledColor = Color.white; b.colors = 色;
            var 输入 = r.gameObject.AddComponent<天帝背包悬停>(); 输入.页面 = this; 输入.格序 = i; 格子.Add(格);
        }
        上页 = 键(框,"背包上一页","‹",838,704,72,40,() => 切页(页码 - 1));
        页码字 = 字(框,"",920,708,168,32,17); 页码字.alignment = TextAnchor.MiddleCenter;
        下页 = 键(框,"背包下一页","›",1104,704,72,40,() => 切页(页码 + 1));
        浮窗 = 区(根,"背包道纹详情",0,0,天帝道纹详情卡.宽度,440);
        详情卡 = 浮窗.gameObject.AddComponent<天帝道纹详情卡>(); 详情卡.初始化(字体);
        if (天帝移动适配.启用)
        {
            var 列表口 = 天帝双端页面布局.滚动组(框, "背包卡片列表", 24, 176, 1152, 500);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                天帝双端页面布局.页头(面板, "关闭背包");
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                string[] 名 = { "背包品阶筛选", "背包属性筛选", "背包状态筛选", "背包排序" };
                for (int i = 0; i < 名.Length; i++) 天帝双端页面布局.区域(面板, 名[i], 8 + i * (宽 - 16) / 4, 天帝双端页面布局.页头高度, (宽 - 24) / 4, 44);
                天帝双端页面布局.固定(接口区, 8, 116, 宽 * .55f, 44);
                天帝双端页面布局.固定(汇总字.rectTransform, 宽 * .57f, 118, 宽 * .4f, 40);
                天帝双端页面布局.固定(列表口, 8, 164, 宽 - 16, 高 - 218);
                var 列表 = 列表口.GetComponent<ScrollRect>().content;
                int 列数 = 宽 < 680 ? 2 : 3;
                float 卡宽 = (宽 - 16 - (列数 - 1) * 6) / 列数, 卡高 = 130;
                列表.sizeDelta = new Vector2(0, Mathf.CeilToInt(格子.Count / (float)列数) * 136);
                for (int i = 0; i < 格子.Count; i++)
                {
                    var 格 = 格子[i]; var 卡 = (RectTransform)格.键.transform;
                    天帝双端页面布局.固定(卡, i % 列数 * (卡宽 + 6), i / 列数 * (卡高 + 6), 卡宽, 卡高);
                    天帝双端页面布局.固定(格.图.rectTransform, 4, 10, 44, 44);
                    天帝双端页面布局.固定(格.品阶.rectTransform, 52, 2, 卡宽 - 106, 44); 格.品阶.fontSize = 14;
                    天帝双端页面布局.固定(格.名.rectTransform, 52, 48, 卡宽 - 58, 56); 格.名.fontSize = 15;
                    天帝双端页面布局.固定(格.状态.rectTransform, 4, 卡高 - 24, 卡宽 - 8, 22); 格.状态.fontSize = 14;
                }
                天帝双端页面布局.按键((RectTransform)高级.transform, 8, 高 - 48, 128);
                天帝双端页面布局.按键((RectTransform)上页.transform, 宽 - 244, 高 - 48, 64);
                天帝双端页面布局.固定(页码字.rectTransform, 宽 - 174, 高 - 48, 98, 44);
                天帝双端页面布局.按键((RectTransform)下页.transform, 宽 - 70, 高 - 48, 64);
                天帝双端页面布局.固定(空提示.rectTransform, 8, 172, 宽 - 16, 60);
                天帝双端页面布局.按键((RectTransform)清筛键.transform, 宽 * .5f - 80, 236, 160);
            });
        }
        数据.状态改变+=刷新;刷新();
    }
    void 重筛() { 页码 = 0; 刷新(); }
    public void 清除筛选()
    {
        品阶筛选 = 分组筛选 = 状态筛选 = 排序 = 接口筛选 = 0; 旋转接口筛选 = false;
        foreach (var d in 筛选下拉) { d.SetValueWithoutNotify(0); d.RefreshShownValue(); }
        转键.GetComponentInChildren<Text>().text = "当前朝向匹配"; 重筛();
    }
    public void 更新选择(道纹实例 纹) { 当前选择 = 纹; 刷新(); }
    public void 刷新()
    {
        隐藏详情(); 结果.Clear();
        foreach (var 纹 in 数据.道纹)
        {
            if (纹.是源纹 || 纹.是天赋 || 品阶筛选 > 0 && (int)纹.品阶 != 8 - 品阶筛选) continue;
            if (!天帝道纹操作筛选.接口匹配(纹.接口,接口筛选,旋转接口筛选)) continue;
            if (状态筛选 == 1 && 纹.格子.HasValue || 状态筛选 == 2 && !纹.格子.HasValue) continue;
            if (分组筛选 == 5 ? 纹.分类 != 道纹分类.分叉 :
                分组筛选 == 6 ? 纹.分类 != 道纹分类.特性 : 分组筛选 == 7 ? 纹.分类 != 道纹分类.转化 :
                分组筛选 > 0 && !纹.词条.Exists(x => 天帝道纹属性.分组(x.属性) ==
                    (分组筛选 == 1 ? 道纹属性分组.基础 : 分组筛选 == 2 ? 道纹属性分组.普通 :
                     分组筛选 == 3 ? 道纹属性分组.形态 : 道纹属性分组.元素))) continue;
            结果.Add(纹);
        }
        if (排序 != 0) 结果.Sort((a,b) => { int c = 排序 == 1 ? b.品阶.CompareTo(a.品阶) : a.分类 != b.分类 ? a.分类.CompareTo(b.分类) : 天帝道纹属性.分组(a.属性).CompareTo(天帝道纹属性.分组(b.属性)); return c != 0 ? c : a.编号.CompareTo(b.编号); });
        页码 = Mathf.Clamp(页码,0,总页数 - 1);
        for (int i = 0; i < 格子.Count; i++)
        {
            var 格 = 格子[i]; int 索 = 页码 * 每页数量 + i; bool 有 = 索 < 结果.Count;
            格.键.gameObject.SetActive(有); 格.纹 = 有 ? 结果[索] : null;
            if (!有) continue;
            var 纹 = 格.纹; bool 已选 = ReferenceEquals(纹,当前选择);
            格.锁.设置(纹);
            格.键.interactable = !已选;
            天帝道纹美术.选中(格.键.GetComponent<Image>(),已选);
            格.边.effectColor = 已选 ? 金色 : 天帝道纹品阶.获取(纹.品阶).颜色 * .65f;
            格.图.单纹 = 纹; 格.图.候选暗 = 已选; 格.图.SetVerticesDirty();
            格.图短名.text=天帝美术资源.有道纹图标(纹)?"":纹.短名;
            格.品阶.text = 天帝道纹品阶.彩色品阶文字(纹.品阶);
            string 摘要 = 纹.分类 == 道纹分类.分叉 ? "连接与分流" : 纹.词条.Count == 0 ? "暂无词条" : 天帝道纹属性.词条名称(纹.词条[0].属性).Replace("属性额外伤害", "伤害") + " " + 天帝道纹属性.数值文字(纹.词条[0].属性, 纹.词条[0].实际数值);
            格.名.text = 纹.短名 + (天帝移动适配.启用 ? "\n" + 摘要 : ""); 格.效果.text = 摘要;
            格.状态.text = (纹.回收锁定?"回收已锁" : 已选 ? "当前选择" : 纹.格子.HasValue ? "已放置" : "未放置") + " · #" + 纹.编号;
            格.状态.color = 已选 ? 金色 : 天帝道纹美术.次文;
        }
        汇总字.text = "道纹 " + 结果.Count + " / " + 数据.道纹.Count + (接口筛选==0?"":" · 已按接口筛选");
        页码字.text = (页码 + 1) + " / " + 总页数; 上页.interactable = 页码 > 0; 下页.interactable = 页码 < 总页数 - 1;
        空提示.gameObject.SetActive(结果.Count == 0); 空提示.text = 数据.道纹.Count == 0 ? "背包暂无道纹" : "没有符合筛选条件的道纹";
        清筛键.gameObject.SetActive(结果.Count == 0 && 数据.道纹.Count > 0);
    }
    public bool 切页(int 页)
    { if (页 < 0 || 页 >= 总页数) return false; 页码 = 页; 刷新(); return true; }
    public bool 设置接口筛选(int 口,bool 允许旋转 = false)
    { if (口 < 0 || 口 > 63) return false; 接口筛选 = 口; 旋转接口筛选 = 允许旋转; 重筛(); return true; }
    void 选中(int 序)
    {
        var 纹 = 显示项(序);
        if (纹 == null || ReferenceEquals(纹,当前选择) || !数据.道纹.Contains(纹)) return;
        选择(纹);
    }
    public void 显示详情(int 序,Vector2 屏幕)
    {
        var 纹 = 显示项(序); if (纹 == null) { 隐藏详情(); return; }
        详情卡.设置(纹, 纹.回收锁定 ? "已锁定 · 不可回收" : ReferenceEquals(纹,当前选择) ? "当前选择" : null);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根,屏幕,null,out var 点);
        float x = 点.x + 18;
        if (x + 天帝道纹详情卡.宽度 > 根.rect.width - 8) x = 点.x - 天帝道纹详情卡.宽度 - 18;
        x = Mathf.Clamp(x, 8, 根.rect.width - 天帝道纹详情卡.宽度 - 8);
        float 上 = -点.y + 18;
        if (上 + 详情卡.高度 > 根.rect.height - 8) 上 = -点.y - 详情卡.高度 - 18;
        上 = Mathf.Clamp(上, 8, 根.rect.height - 详情卡.高度 - 8);
        浮窗.anchoredPosition = new Vector2(x,-上);
        浮窗.SetAsLastSibling(); 浮窗.gameObject.SetActive(true);
    }
    public void 隐藏详情() { if (浮窗 != null) 浮窗.gameObject.SetActive(false); }
    void OnApplicationFocus(bool 焦点) { if (!焦点) 隐藏详情(); }
    void OnDisable() => 隐藏详情();
    void OnDestroy(){if(数据!=null)数据.状态改变-=刷新;}
    RectTransform 区(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    RectTransform 底(RectTransform 父,string 名,float x,float y,float w,float h,Color 色)
    { var t = 区(父,名,x,y,w,h); var 图 = t.gameObject.AddComponent<Image>(); 图.color = 色; 图.raycastTarget = true;天帝界面美术.自动面板(图,名,w,h,色);return t; }
    Text 字(RectTransform 父,string 文,float x,float y,float w,float h,int 大小)
    { var t = 区(父,"文字",x,y,w,h).gameObject.AddComponent<Text>(); t.font = 字体; t.fontSize = 大小; t.text = 文; t.color = 字色; t.raycastTarget = false; t.verticalOverflow = VerticalWrapMode.Truncate; 天帝界面美术.文字(t); return t; }
    Button 键(RectTransform 父,string 名,string 文,float x,float y,float w,float h,Action 点击)
    {
        var t = 底(父,名,x,y,w,h,卡色); var b = t.gameObject.AddComponent<Button>(); b.targetGraphic = t.GetComponent<Image>(); b.onClick.AddListener(() => 点击());
        天帝界面美术.按钮(b);
        if (!string.IsNullOrEmpty(文)) 字(t,文,6,0,w - 12,h,18).alignment = TextAnchor.MiddleCenter; 天帝界面美术.按钮(b); return b;
    }
    void 选项(RectTransform 父,string 名,float x,float y,List<string> 项,Action<int> 改变)
    {
        var t = 底(父,名,x,y,276,42,卡色);天帝界面美术.选项(t.GetComponent<Image>(),false); var d = t.gameObject.AddComponent<Dropdown>(); d.targetGraphic = t.GetComponent<Image>();
        d.captionText = 字(t,"",12,6,232,30,18); d.captionText.fontStyle=FontStyle.Bold; d.captionText.alignment = TextAnchor.MiddleLeft; 字(t,"▾",246,4,24,34,18).alignment = TextAnchor.MiddleCenter;
        float 列高 = Mathf.Min(342,项.Count * 38);
        var 模板 = 底(t,"Template",0,42,276,列高,new Color(.035f,.065f,.075f));
        天帝响应布局.动态(模板);
        模板.anchorMin = Vector2.zero; 模板.anchorMax = new Vector2(1, 0); 模板.anchoredPosition = Vector2.zero; 模板.sizeDelta = new Vector2(0, 列高);
        var 菜单素材=底(模板,"菜单素材衬底",0,0,276,列高,Color.white);天帝界面美术.面板(菜单素材.GetComponent<Image>(),"二级面板",Color.white);菜单素材.GetComponent<Image>().raycastTarget=false;
        模板.pivot = new Vector2(0,1);
        var 视口 = 底(模板,"Viewport",0,0,276,列高,Color.clear); 视口.gameObject.AddComponent<RectMask2D>();
        var 内容 = 区(视口,"Content",0,0,276,38);
        var 项区 = 底(内容,"Item",0,0,276,38,卡色); var toggle = 项区.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = 项区.GetComponent<Image>();
        var 勾 = 底(项区,"选中",6,15,8,8,金色).GetComponent<Image>(); 勾.raycastTarget = false; toggle.graphic = 勾;
        d.itemText = 字(项区,"",24,3,242,32,17); d.itemText.alignment = TextAnchor.MiddleLeft;
        var scroll = 模板.gameObject.AddComponent<ScrollRect>(); scroll.viewport = 视口; scroll.content = 内容; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        天帝响应布局.比例(菜单素材, 0, 0, 1, 1); 天帝响应布局.比例(视口, 0, 0, 1, 1);
        内容.anchorMin = new Vector2(0, 1); 内容.anchorMax = Vector2.one; 内容.sizeDelta = new Vector2(0, 38);
        项区.anchorMin = new Vector2(0, 1); 项区.anchorMax = Vector2.one; 项区.sizeDelta = new Vector2(0, 38);
        d.itemText.rectTransform.anchorMin = Vector2.zero; d.itemText.rectTransform.anchorMax = Vector2.one;
        d.itemText.rectTransform.offsetMin = new Vector2(24, 3); d.itemText.rectTransform.offsetMax = new Vector2(-10, -3);
        d.template = 模板; 模板.gameObject.SetActive(false); d.AddOptions(项); d.onValueChanged.AddListener(v => 改变(v));
        筛选下拉.Add(d);
    }
}

public sealed class 天帝背包悬停 : MonoBehaviour,IPointerEnterHandler,IPointerMoveHandler,IPointerExitHandler,IPointerClickHandler
{
    public 天帝道纹背包 页面;
    public int 格序;
    public void OnPointerEnter(PointerEventData e) { if (!天帝移动适配.启用) 页面.显示详情(格序,e.position); }
    public void OnPointerMove(PointerEventData e) { if (!天帝移动适配.启用) 页面.显示详情(格序,e.position); }
    public void OnPointerExit(PointerEventData e) { if (!天帝移动适配.启用) 页面.隐藏详情(); }
    public void OnPointerClick(PointerEventData e)
    { if (天帝移动适配.启用 && e.button == PointerEventData.InputButton.Left && 页面 != null && 页面.isActiveAndEnabled) 页面.显示详情(格序,e.position); }
}
