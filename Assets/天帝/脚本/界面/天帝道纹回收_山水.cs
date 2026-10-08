using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 图录与回收按获批山水图的真实控件布局装配，图片只承担无字纸材与环境。
internal static class 天帝图录回收山水素材
{
    public static Sprite 获取(string 名, string 备用)
    {
        var 图 = Resources.Load<Sprite>("山水剩余界面/" + 名);
        return 图 != null ? 图 : 天帝剪纸界面皮肤.素材(备用);
    }
    public static void 纸(Image 图, string 名, string 备用, bool 九宫格 = true)
    {
        if (图 == null) return;
        图.sprite = 获取(名, 备用); 图.overrideSprite = null; 图.color = Color.white;
        图.type = 九宫格 ? Image.Type.Sliced : Image.Type.Simple;
        图.pixelsPerUnitMultiplier = 2;
    }
    public static void 按钮(Button 键, bool 选中 = false, bool 主操作 = false)
    {
        if (主操作) 天帝首两页山水素材.按钮(键, "墨绿按钮", true);
        else 天帝首两页山水素材.轻按钮(键, 选中);
        foreach (var 字 in 键.GetComponentsInChildren<Text>())
        {
            字.fontStyle = FontStyle.Normal;
            字.color = 主操作 ? 天帝道纹美术.浅字 : 天帝剪纸界面皮肤.墨;
        }
    }
    public static void 禁用(Selectable 控件)
    {
        if (控件 == null) return;
        var 色 = 控件.colors; 色.disabledColor = Color.white; 控件.colors = 色;
        var 组 = 控件.GetComponent<CanvasGroup>();
        if (组 == null) 组 = 控件.gameObject.AddComponent<CanvasGroup>();
        组.alpha = 控件.interactable ? 1 : .5f;
    }
    public static void 文字(Text 字, Font 字体, int 大小)
    {
        if (字 == null) return;
        字.font = 字体; 字.fontSize = 大小; 字.fontStyle = FontStyle.Normal;
        字.resizeTextForBestFit = false; 字.color = 天帝剪纸界面皮肤.墨;
        字.horizontalOverflow = HorizontalWrapMode.Wrap; 字.verticalOverflow = VerticalWrapMode.Truncate;
        foreach (var 边 in 字.GetComponents<Outline>()) 边.enabled = false;
    }
}

public sealed partial class 天帝道纹回收界面
{
    bool 山水回收;
    RectTransform 山水详情正文;
    void 装配山水回收(RectTransform 根, RectTransform 框, RectTransform 结算, RectTransform 详情卡)
    {
        if (!天帝剪纸界面皮肤.已启用 || 天帝移动适配.启用) return;
        山水回收 = true;
        天帝响应布局.动态(框);
        void 定(RectTransform r, float x, float y, float w, float h) => 天帝双端页面布局.固定(r, x, y, w, h);
        void 名(string n, float x, float y, float w, float h) => 定(框.Find(n) as RectTransform, x, y, w, h);
        var 背景 = 根.Find("剪纸回收底图")?.GetComponent<Image>();
        天帝图录回收山水素材.纸(背景, "回收背景", "回收背景", false);
        定(框, 0, 0, 1600, 900); 框.localScale = Vector3.one;
        var 标题 = 框.Find("审批剪纸标题")?.GetComponent<Text>();
        定(标题.rectTransform, 84, 0, 540, 88); 标题.fontSize = 48;
        foreach (Transform 子 in 框)
        {
            if (子.GetComponent<Text>() is Text 文 && 文.text.StartsWith("处理闲置"))
            { 定(文.rectTransform, 84, 93, 1090, 36); 天帝图录回收山水素材.文字(文, 字体, 19); }
            if (子.GetComponent<Text>() is Text 注 && 注.text.StartsWith("点击卡片")) 注.gameObject.SetActive(false);
        }
        定(余额条, 1076, 28, 248, 48); 定(余额.rectTransform, 10, 0, 228, 48);
        名("关闭回收", 1360, 28, 166, 48);
        for (int i = 0; i < 3; i++)
        {
            定((RectTransform)筛选下拉[i].transform, 84 + i * 193, 139, 177, 44);
            天帝首两页山水素材.轻纸(筛选下拉[i].targetGraphic as Image);
        }
        名("批选分组边", 664, 142, 1, 36);
        名("一键选中", 686, 139, 112, 44); 名("回收范围设置", 810, 139, 112, 44); 名("清空回收选择", 934, 139, 112, 44);
        for (int i = 0; i < 每页; i++)
        {
            var r = (RectTransform)卡[i].transform; 定(r, 84 + i % 3 * 326, 201 + i / 3 * 170, 308, 154);
            天帝图录回收山水素材.纸((Image)卡[i].targetGraphic, "回收卡", "轻纸框");
            定(图标[i].rectTransform, 17, 14, 72, 77); 图标[i].单纹半径 = 31; 图标[i].构筑美术 = true;
            定(图短名[i].rectTransform, 4, 0, 64, 77); 图短名[i].fontSize = 25;
            定(卡名[i].rectTransform, 100, 10, 176, 60); 天帝图录回收山水素材.文字(卡名[i], 字体, 21);
            定(卡价[i].rectTransform, 100, 76, 179, 29); 天帝图录回收山水素材.文字(卡价[i], 字体, 19);
            定(卡状态[i].rectTransform, 55, 115, 239, 28); 天帝图录回收山水素材.文字(卡状态[i], 字体, 16);
            定((RectTransform)回收勾选[i].transform, 13, 111, 36, 36);
        }
        // 固定长卷只承载纸材，动态详情用真实共用详情卡重排并独立滚动。
        定(详情区, 1082, 156, 446, 668);
        var 纸 = 详情区.gameObject.AddComponent<Image>(); 纸.raycastTarget = false;
        天帝图录回收山水素材.纸(纸, "详情长卷", "朱红纸框");
        var 口 = 图(详情区, "山水回收详情视口", 28, 24, 390, 620, null, Color.clear).rectTransform;
        口.gameObject.AddComponent<RectMask2D>();
        详情卡.SetParent(口, false); 定(详情卡, 0, 0, 390, 620); 详情卡.localScale = Vector3.one; 山水详情正文 = 详情卡;
        var 滚 = 口.gameObject.AddComponent<ScrollRect>(); 滚.viewport = 口; 滚.content = 详情卡;
        滚.horizontal = false; 滚.vertical = true; 滚.movementType = ScrollRect.MovementType.Clamped; 滚.scrollSensitivity = 32;
        定(空库存.rectTransform, 132, 386, 850, 140);
        定(页码字.rectTransform, 824, 714, 118, 32);
        定((RectTransform)上页.transform, 780, 714, 40, 32); 定((RectTransform)下页.transform, 944, 714, 40, 32);
        定(结算, 84, 751, 962, 81); 结算.GetComponent<Image>().color = Color.clear;
        定(汇总.rectTransform, 8, 0, 235, 38); 定(总价.rectTransform, 258, 0, 433, 38);
        天帝图录回收山水素材.文字(汇总, 字体, 25); 天帝图录回收山水素材.文字(总价, 字体, 25);
        定(提示.rectTransform, 8, 46, 680, 28); 天帝图录回收山水素材.文字(提示, 字体, 17);
        定((RectTransform)卖出.transform, 690, 0, 260, 64);
        foreach (var b in 框.GetComponentsInChildren<Button>())
            if (!b.name.StartsWith("回收道纹-") && b.GetComponent<天帝道纹锁定按钮>() == null) 天帝图录回收山水素材.按钮(b, false, b == 卖出);
    }
    void 刷新山水回收()
    {
        if (!山水回收) return;
        for (int i = 0; i < 每页; i++)
        {
            var 纹 = 显示项(i); if (纹 == null) continue;
            天帝图录回收山水素材.纸((Image)卡[i].targetGraphic, "回收卡", "轻纸框");
            ((Image)卡[i].targetGraphic).color = 选择.Contains(纹) ? new Color(.78f, .88f, .76f) : Color.white;
            卡名[i].text = 纹.名称 + "\n" + 纹.品阶 + " · " + (纹.是功能道纹 ? "功能" : 纹.分类.ToString());
            卡价[i].text = "回收：" + 天帝数值.道纹回收价(纹) + " 灵石";
            卡价[i].color = 卡状态[i].color = 天帝剪纸界面皮肤.墨;
            卡状态[i].text = 数据.可回收(纹, out string 原因) ? "回收 · 未放置" : 原因;
        }
        if (焦点 != null) { 详情.山水窄栏布局(390); foreach (var 文 in 山水详情正文.GetComponentsInChildren<Text>()) 文.fontStyle = FontStyle.Normal; }
        foreach (var 控件 in GetComponentsInChildren<Selectable>()) 天帝图录回收山水素材.禁用(控件);
    }
    void 装配山水确认(RectTransform 框)
    {
        void 定(RectTransform r,float x,float y,float w,float h)=>天帝双端页面布局.固定(r,x,y,w,h);
        天帝响应布局.动态(框);
        定(框,410,58,780,784);
        天帝图录回收山水素材.纸(框.GetComponent<Image>(),"确认纸卷","弹窗纸框");
        var 标题=框.Find("回收确认标题").GetComponent<Text>();
        定(标题.rectTransform,72,90,616,72);标题.font=天帝美术资源.当前?.主页标题字体??字体;标题.fontSize=36;标题.alignment=TextAnchor.MiddleCenter;
        var 数量=框.Find("回收确认数量").GetComponent<Text>();定(数量.rectTransform,72,173,616,40);天帝图录回收山水素材.文字(数量,字体,24);
        var 收益=框.Find("回收确认收益").GetComponent<Text>();定(收益.rectTransform,72,218,616,40);天帝图录回收山水素材.文字(收益,字体,24);
        框.Find("回收确认品阶").gameObject.SetActive(false);
        var 摘要=图(框,"道纹列表摘要纸面",58,272,664,282,"小信息框").rectTransform;
        定(摘要,58,272,664,282);
        天帝首两页山水素材.轻纸(摘要.GetComponent<Image>());
        var 摘要标题=字文(摘要,"道纹列表摘要",22,12,620,35,24);摘要标题.name="回收摘要标题";天帝图录回收山水素材.文字(摘要标题,字体,23);
        var 口=图(摘要,"回收摘要视口",20,58,624,202,null,Color.clear).rectTransform;口.GetComponent<Image>().raycastTarget=true;口.gameObject.AddComponent<RectMask2D>();
        定(口,20,58,624,202);
        var 行组=待售.GroupBy(x=>x.品阶).OrderBy(x=>x.Key).ToList();
        var 内容=区块(口,"本次勾选品阶摘要",0,0,624,Mathf.Max(202,行组.Count*65));
        for(int i=0;i<行组.Count;i++)
        {
            var 行=图(内容,"回收摘要-"+行组[i].Key,0,i*65,624,57,"小信息框").rectTransform;天帝首两页山水素材.轻纸(行.GetComponent<Image>());
            var 阶=字文(行,行组[i].Key.ToString(),24,0,240,57,23);阶.color=天帝剪纸界面皮肤.墨;
            字文(行,行组[i].Count()+" 枚",408,0,186,57,23).alignment=TextAnchor.MiddleRight;
        }
        var 滚=口.gameObject.AddComponent<ScrollRect>();滚.viewport=口;滚.content=内容;滚.horizontal=false;滚.vertical=true;滚.movementType=ScrollRect.MovementType.Clamped;滚.scrollSensitivity=32;
        var 提醒=框.Find("回收不可撤销").GetComponent<Text>();定(提醒.rectTransform,72,580,616,80);天帝图录回收山水素材.文字(提醒,字体,21);提醒.color=天帝剪纸界面皮肤.朱红;
        var 取消=框.Find("取消回收").GetComponent<Button>();定((RectTransform)取消.transform,70,685,282,63);天帝图录回收山水素材.按钮(取消);
        var 确认键=框.Find("确认回收").GetComponent<Button>();定((RectTransform)确认键.transform,424,685,282,63);天帝首两页山水素材.按钮(确认键,"朱红按钮",true);
        var 关闭键=键(框,"关闭回收确认","×",690,20,56,56,关闭确认);天帝图录回收山水素材.按钮(关闭键);定((RectTransform)关闭键.transform,690,20,56,56);
        var 关闭文字=关闭键.GetComponentInChildren<Text>();关闭文字.text="×";关闭文字.font=字体;关闭文字.fontSize=32;关闭文字.alignment=TextAnchor.MiddleCenter;定(关闭文字.rectTransform,0,0,56,56);
    }
}
