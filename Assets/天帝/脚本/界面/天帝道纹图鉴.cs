using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 图鉴只读：展示定义与正式随机区间，不创建玩家物品、不写存档。
public sealed partial class 天帝道纹图鉴 : MonoBehaviour
{
    const float 列表宽 = 1320, 文字左 = 148, 文字宽 = 1144;
    static readonly string[] 分类名 = { "天赋道纹", "基础属性", "普通属性", "功能道纹", "五行元素", "分叉道纹", "特性道纹", "转化道纹" };
    readonly Dictionary<string, float> 分类位置 = new Dictionary<string, float>();
    readonly Dictionary<string, Button> 分类按钮 = new Dictionary<string, Button>();
    readonly List<范围显示项> 范围列表 = new List<范围显示项>();
    sealed class 范围显示项 { public 道纹属性 属性; public Text 文字; }
    Font 字体;
    RectTransform 根, 内容, 视口, 浮窗;
    Text 浮窗字;
    ScrollRect 滚动;
    InputField 等级输入;
    float 共通规则位置;
    public int 展示数量 { get; private set; }
    public int 查询物品等级 { get; private set; } = 1;

    public void 初始化(Font 默认字体, Action 关闭)
    {
        // 公共初始化可重复调用，先清理旧视图与查询状态。
        分类位置.Clear(); 分类按钮.Clear(); 范围列表.Clear(); 特性查询.Clear();
        剪纸分类行.Clear(); 剪纸图鉴卡.Clear(); 剪纸图鉴图.Clear(); 剪纸图鉴名.Clear(); 剪纸道纹单字.Clear();
        剪纸当前分类 = null; 剪纸页 = 0; 剪纸详情目标 = null;
        查询物品等级 = 1;
        for(int i=transform.childCount-1;i>=0;i--)
        {
            var 旧=transform.GetChild(i).gameObject;旧.SetActive(false);
            if(Application.isPlaying)Destroy(旧);else DestroyImmediate(旧);
        }
        字体 = 默认字体 ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); 根 = (RectTransform)transform;
        var 遮罩 = 底(根, "图鉴遮罩", 0, 0, 1600, 900, null, new Color(0, 0, 0, .82f));
        var 遮罩按钮 = 遮罩.gameObject.AddComponent<Button>();
        遮罩按钮.transition = Selectable.Transition.None; 遮罩按钮.onClick.AddListener(() => 关闭?.Invoke());
        var 框 = 底(根, "道纹图鉴面板", 84, 32, 1432, 836, "一级面板");
        字(框, "图鉴标题", "道纹图鉴", 34, 14, 600, 52, 32, 天帝道纹美术.正文);
        按钮(框, "关闭图鉴", "关闭", 1248, 22, 148, 56, 关闭);
        展示数量 = 天帝天赋.全部.Count + 天帝道纹属性.非功能属性.Length + 天帝顺序道纹.功能数量 + 47;
        字(框, "收录说明", "天赋 " + 天帝天赋.全部.Count + " · 属性 " + 天帝道纹属性.非功能属性.Length + " · 功能 " + 天帝顺序道纹.功能数量 + " · 分叉 1 · 特性 38 · 转化 8", 34, 70, 940, 28, 18, 天帝道纹美术.次文);
        按钮(框, "查看共通规则", "品阶与连接规则", 1000, 22, 226, 30, () => 定位位置(共通规则位置));
        for (int i = 0; i < 分类名.Length; i++)
        {
            string 名 = 分类名[i];
            分类按钮[名] = 按钮(框, "图鉴分类-" + 名, 名, 34 + i * 172, 108, 162, 38, () => 定位分类(名));
        }
        字(框, "等级查询标签", "物品等级", 1014, 76, 102, 26, 18, 天帝道纹美术.次文);
        var 输入底 = 底(框, "物品等级输入", 1118, 70, 82, 38, "小信息框");
        if(天帝剪纸界面皮肤.已启用)天帝界面美术.选项(输入底.GetComponent<Image>(),false);
        等级输入 = 输入底.gameObject.AddComponent<InputField>(); 等级输入.targetGraphic = 输入底.GetComponent<Image>();
        等级输入.textComponent = 字(输入底, "等级查询文字", "1", 8, 3, 66, 32, 21, 天帝道纹美术.正文);
        等级输入.textComponent.alignment = TextAnchor.MiddleCenter;
        等级输入.contentType = InputField.ContentType.IntegerNumber; 等级输入.characterLimit = 3; 等级输入.SetTextWithoutNotify("1");
        等级输入.onEndEdit.AddListener(文 => 设置查询物品等级(int.TryParse(文, out int 值) ? 值 : 查询物品等级));
        字(框, "等级查询范围", "1–" + 天帝数值.玩家上限, 1212, 76, 132, 26, 18, 天帝道纹美术.次文);

        视口 = 底(框, "道纹列表视口", 34, 160, 列表宽, 626, null, Color.clear); 视口.gameObject.AddComponent<RectMask2D>();
        内容 = 区(视口, "道纹逐行列表", 0, 0, 列表宽, 626);
        滚动 = 视口.gameObject.AddComponent<ScrollRect>(); 滚动.viewport = 视口; 滚动.content = 内容;
        滚动.horizontal = false; 滚动.vertical = true; 滚动.movementType = ScrollRect.MovementType.Clamped; 滚动.scrollSensitivity = 48;
        var 轨 = 底(框, "图鉴滚动条", 1372, 160, 16, 626, "滚动轨");
        var 滑区 = 区(轨, "滑动区域", 0, 0, 16, 626);
        滑区.anchorMin = Vector2.zero; 滑区.anchorMax = Vector2.one; 滑区.offsetMin = 滑区.offsetMax = Vector2.zero;
        var 滑块 = 底(滑区, "图鉴滚动滑块", 0, 0, 16, 80, "滚动滑块");
        滑块.anchorMin = Vector2.zero; 滑块.anchorMax = Vector2.one; 滑块.pivot = new Vector2(.5f, .5f); 滑块.offsetMin = 滑块.offsetMax = Vector2.zero;
        var 条 = 轨.gameObject.AddComponent<Scrollbar>(); 条.targetGraphic = 滑块.GetComponent<Image>(); 条.handleRect = 滑块; 条.direction = Scrollbar.Direction.BottomToTop;
        滚动.verticalScrollbar = 条; 滚动.onValueChanged.AddListener(_ => 更新分类高亮());

        float y = 0;
        添加标题("天赋道纹", "序章从随机五枚中选定一枚；固定中心、不可旋转或替换。右接口初始开放，其余接口每10级解封一个（10至50级）。", ref y);
        foreach (var 天赋 in 天帝天赋.全部) 添加天赋(天赋, ref y);
        添加属性分组("基础属性", "属性道纹 · 力量、速度、智力，全局生效并影响衍生属性。", 天帝道纹属性.基础属性, ref y);
        添加属性分组("普通属性", "属性道纹 · 直接强化生存与行动能力，全局生效。", 天帝道纹属性.普通属性, ref y);
        添加标题("功能道纹", 天帝顺序道纹.功能数量 + "种固定稀有、单功能；齐射/分裂3口、连锁2口，其余随机1～2口。方向随机，接口对应连接，每条链路每枚仅生效一次。", ref y);
        for (int i = 1; i <= 天帝顺序道纹.功能数量; i++) 添加顺序功能((道纹功能)i, ref y);
        添加属性分组("五行元素", "属性道纹 · 新生成的五行道纹固定单词条，所有品阶均为1条，仅作用于接入的通路。", 天帝道纹属性.五行属性, ref y);
        添加标题("分叉道纹", "连接专用道纹 · 传导与分流，不提供属性词条。", ref y); 添加分叉(ref y);
        添加标题("特性道纹", "38种条件机制，八品阶、固定相对两口，仅战斗掉落，不可改造；条件读取转换后的下游属性与功能节点，原攻击链路照常执行。",ref y);
        for(int id=1;id<=38;id++)添加特性(道纹分类.特性,id,ref y);
        添加标题("转化道纹", "8种预算转化，八品阶、固定相对两口；半径1～3格、效率80%～100%，只转换第一条源通路内接通属性，同节点多目标冲突保留原词条。",ref y);
        for(int id=1;id<=8;id++)添加特性(道纹分类.转化,id,ref y);
        共通规则位置 = y;
        var 容量框 = 底(内容, "品阶容量说明", 0, y, 列表宽, 178, "二级面板");
        字(容量框, "共通规则标题", "属性道纹 · 品阶与接口", 26, 12, 1240, 40, 23, 天帝道纹美术.强调);
        字(容量框, "容量规则摘要", "品阶决定词条容量；五行道纹固定单条。属性词条可以重复，数值相加。", 26, 54, 1266, 26, 18, 天帝道纹美术.次文);
        for (int i = 0; i < 8; i++)
        {
            var 阶 = (道纹品阶)i; var 定 = 天帝道纹品阶.获取(阶);
            string 数量 = 定.最少词条 == 定.最多词条 ? 定.最少词条.ToString() : 定.最少词条 + "–" + 定.最多词条;
            字(容量框, "共通容量-" + 阶, 阶 + "  " + 数量 + "条", 26 + i % 4 * 318, 85 + i / 4 * 28, 302, 28, 18, 天帝道纹美术.正文);
        }
        字(容量框, "共通接口概率", "接口：1口 " + 天帝数值.取("rune.property_port_weights.0") + "% / 2口 " + 天帝数值.取("rune.property_port_weights.1") + "% · 朝向随机、可旋转；数量与品阶独立，改造保留接口。", 26, 146, 1266, 26, 17, 天帝道纹美术.次文);
        y += 190;
        var 规则 = 底(内容, "连接机制说明", 0, y, 列表宽, 200, "二级面板");
        字(规则, "连接规则标题", "连接与计算规则", 26, 12, 1240, 40, 23, 天帝道纹美术.强调);
        var 规则字 = 正文(规则, "连接规则正文", "接口必须相互对接并连通源道纹；线路只能向同圈或外圈传导，未接通的道纹不生效。\n基础与普通属性全局去重；形态与五行按通路分别计算，每枚道纹每路只计一次，跨路共享以白点标记。\n自动选择最近敌人，从已接通接口中随机释放一路，共用全局攻击间隔；增加通路不会直接倍增攻速。顺序弹继承祖先历史，兄弟独立；特性同次覆盖共享命中历史。\n特性/转化已进入8级以上战斗独立掉落池；冰、雷、时间、空间仍不掉落。", 26, 58, 1266, 18, 天帝道纹美术.次文);
        规则.sizeDelta = new Vector2(列表宽, 规则字.rectTransform.sizeDelta.y + 78);
        y += 规则.sizeDelta.y + 8; 内容.sizeDelta = new Vector2(列表宽, Mathf.Max(626, y));
        字(框, "图鉴底注", "单条区间由物品等级决定，品阶控制容量；重复词条相加。查询不改变已拥有道纹。", 34, 799, 1320, 24, 16, 天帝道纹美术.次文);
        创建浮窗(); 更新分类高亮();
        天帝剪纸界面皮肤.装配(根,"图鉴",框);
        if (天帝剪纸界面皮肤.已启用 && !天帝移动适配.启用) 建剪纸图鉴(框);
        if (天帝移动适配.启用)
        {
            var 分类列 = new List<RectTransform>();
            foreach (var 名 in 分类名) if (分类按钮.TryGetValue(名, out var 分类键) && 分类键 != null) 分类列.Add((RectTransform)分类键.transform);
            var 共通按钮 = 框.Find("查看共通规则") as RectTransform; if (共通按钮 != null) 分类列.Add(共通按钮);
            var 分类口 = 天帝双端页面布局.横列(框, "图鉴分类视口", 分类列.ToArray(), 120);
            框.Find("收录说明")?.gameObject.SetActive(false);
            框.Find("图鉴底注")?.gameObject.SetActive(false);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                float 宽 = 面板.rect.width, 高 = 面板.rect.height;
                天帝双端页面布局.页头(面板, "关闭图鉴");
                天帝双端页面布局.固定(分类口, 8, 天帝双端页面布局.页头高度, 宽 - 240, 44);
                天帝双端页面布局.区域(面板, "等级查询标签", 宽 - 226, 天帝双端页面布局.页头高度, 80, 44);
                天帝双端页面布局.区域(面板, "物品等级输入", 宽 - 144, 天帝双端页面布局.页头高度, 70, 44);
                if (等级输入 != null && 等级输入.textComponent != null) 天帝响应布局.比例(等级输入.textComponent.rectTransform, .05f, 0, .9f, 1);
                天帝双端页面布局.区域(面板, "等级查询范围", 宽 - 68, 天帝双端页面布局.页头高度, 60, 44);
                天帝双端页面布局.固定(视口, 8, 118, 宽 - 40, 高 - 126);
                天帝双端页面布局.固定(轨, 宽 - 26, 118, 18, 高 - 126);
                排版移动正文();
            });
        }
    }

    // 手机正文按实际宽度重新计算行高，滚动只发生在列表内。
    void 排版移动正文()
    {
        float 宽 = 视口.rect.width, y = 0;
        foreach (RectTransform 行 in 内容)
        {
            if (行.name.StartsWith("分类标题-")) 分类位置[行.name.Substring(5)] = y;
            if (行.name == "品阶容量说明") 共通规则位置 = y;
            var 单文 = 行.GetComponent<Text>();
            if (单文 != null)
            {
                单文.fontSize = 行.name.StartsWith("分类标题-") ? 20 : 16;
                天帝双端页面布局.固定(行, 8, y, 宽 - 16, 1000);
                float 高 = Mathf.Ceil(单文.preferredHeight) + 8;
                天帝双端页面布局.固定(行, 8, y, 宽 - 16, 高); y += 高 + 8; continue;
            }
            bool 有图 = 行.Find("道纹图标") != null;
            float 下 = 有图 ? 74 : 8;
            if (有图)
            {
                天帝双端页面布局.区域(行, "道纹图标", 8, 8, 58, 58);
                行.Find("示意标签").gameObject.SetActive(false);
                var 图 = 行.Find("道纹图标").GetComponent<天帝道纹绘图>(); 图.单纹半径 = 25;
            }
            foreach (Transform 子 in 行)
            {
                var 文 = 子.GetComponent<Text>(); if (文 == null || !子.gameObject.activeSelf) continue;
                bool 名称 = 子.name == "道纹名称", 类别 = 子.name == "道纹类别";
                文.fontSize = 名称 ? 20 : 16; 文.alignment = TextAnchor.UpperLeft;
                float 左 = 名称 || 类别 ? 76 : 12, 上 = 名称 ? 8 : 类别 ? 40 : 下;
                天帝双端页面布局.固定(文.rectTransform, 左, 上, 宽 - 左 - 12, 1000);
                float 高 = Mathf.Ceil(文.preferredHeight) + 6;
                天帝双端页面布局.固定(文.rectTransform, 左, 上, 宽 - 左 - 12, 高);
                if (!名称 && !类别) 下 += 高 + 6;
            }
            天帝双端页面布局.固定(行, 0, y, 宽, 下 + 8); y += 下 + 20;
        }
        天帝双端页面布局.固定(内容, 0, 0, 宽, Mathf.Max(视口.rect.height, y));
        更新分类高亮();
    }

    void 添加标题(string 分类, string 说明, ref float y)
    {
        分类位置[分类] = y;
        字(内容, "分类标题-" + 分类, 分类, 8, y + 2, 1300, 42, 25, 天帝道纹美术.强调);
        var 说明字 = 正文(内容, "分类说明-" + 分类, 说明, 8, y + 48, 1292, 18, 天帝道纹美术.次文);
        y += 说明字.rectTransform.sizeDelta.y + 64;
    }
    void 添加天赋(天赋定义 天赋, ref float y)
    {
        var 行 = 行框("天赋-" + 天赋.名称, y); 画图标(行, 天帝道纹.创建天赋源纹(天赋));
        字(行, "道纹名称", 天赋.名称 + "天赋道纹", 文字左, 14, 780, 36, 24, 天帝道纹美术.正文);
        字(行, "道纹类别", "天赋 / 中心源纹", 980, 20, 312, 26, 17, 天帝道纹美术.次文).alignment = TextAnchor.MiddleRight;
        var 说明 = 正文(行, "天赋介绍", 天赋.效果 + "\n" + 天赋.说明 + "\n构筑倾向：" + 天赋.倾向, 文字左, 56, 文字宽, 19, 天帝道纹美术.正文);
        完成行(行, Mathf.Max(156, 说明.rectTransform.sizeDelta.y + 76), ref y);
    }
    void 添加属性分组(string 分类, string 说明, 道纹属性[] 属性们, ref float y)
    {
        添加标题(分类, 说明, ref y);
        foreach (var 属性 in 属性们)
        {
            var 行 = 行框("属性-" + 属性, y);
            bool 功能 = 天帝道纹属性.是功能(属性);
            var 示意 = new 道纹实例 { 编号 = (int)属性 + 1, 分类 = 功能 ? 道纹分类.功能 : 道纹分类.属性, 品阶 = 功能 ? 道纹品阶.稀有 : 道纹品阶.普通, 接口 = 1 };
            示意.词条.Add(new 道纹词条(属性, 1)); 画图标(行, 示意);
            字(行, "道纹名称", 属性 + "道纹", 文字左, 14, 780, 36, 24, 天帝道纹美术.正文);
            字(行, "道纹类别", 功能 ? "功能 / 固定稀有 / 单词条" : "属性 / " + 分类, 980, 20, 312, 26, 17, 天帝道纹美术.次文).alignment = TextAnchor.MiddleRight;
            var 说明字 = 正文(行, "属性机制", 类型说明(属性), 文字左, 56, 文字宽, 19, 天帝道纹美术.正文);
            float 下 = 说明字.rectTransform.sizeDelta.y + 66;
            var 范围字 = 正文(行, "词条数值范围", 范围说明(属性, 查询物品等级), 文字左, 下, 文字宽, 20, 天帝道纹美术.强调);
            范围列表.Add(new 范围显示项 { 属性 = 属性, 文字 = 范围字 }); 下 += 范围字.rectTransform.sizeDelta.y + 8;
            完成行(行, Mathf.Max(158, 下 + 16), ref y);
        }
    }
    void 添加分叉(ref float y)
    {
        var 行 = 行框("连接-分叉", y); 画图标(行, new 道纹实例 { 编号 = 1, 分类 = 道纹分类.分叉, 品阶 = 道纹品阶.普通, 接口 = 21 });
        字(行, "道纹名称", "分叉道纹", 文字左, 14, 780, 36, 24, 天帝道纹美术.正文);
        字(行, "道纹类别", "连接 / 无属性", 980, 20, 312, 26, 17, 天帝道纹美术.次文).alignment = TextAnchor.MiddleRight;
        string 概率 = "接口概率："; for (int i = 0; i < 4; i++) 概率 += (i + 3) + "口 " + 天帝数值.取("rune.branch_port_weights." + i) + "%" + (i < 3 ? " / " : "");
        var 说明 = 正文(行, "分叉介绍", "不提供任何属性词条，只把有效连接分流到更多方向；仍需接口对接并连通源纹。\n" + 概率 + "；方向随机。\n仅普通品阶，不可改造；掉落后接口固定，可旋转、装备与卸下。", 文字左, 56, 文字宽, 19, 天帝道纹美术.正文);
        完成行(行, Mathf.Max(156, 说明.rectTransform.sizeDelta.y + 76), ref y);
    }
    void 添加顺序功能(道纹功能 功能, ref float y)
    {
        var 行 = 行框("功能-" + 功能, y);
        var 纹 = new 道纹实例 { 编号 = 100 + (int)功能, 分类 = 道纹分类.功能, 品阶 = 道纹品阶.稀有,
            功能 = 功能, 入口方向 = 3, 接口 = 天帝顺序道纹.固定接口(功能, 3) };
        纹.词条.Add(new 道纹词条(天帝顺序道纹.兼容属性(功能), 1)); 画图标(行, 纹);
        字(行, "道纹名称", 功能 + "道纹", 文字左, 14, 780, 36, 24, 天帝道纹美术.正文);
        字(行, "道纹类别", "顺序功能 / 稀有 / 单条", 980, 20, 312, 26, 17, 天帝道纹美术.次文).alignment = TextAnchor.MiddleRight;
        var 文 = 正文(行, "功能机制", 天帝顺序道纹.功能摘要(功能) + "。\n上游攻击属性继承，下游属性只强化其出口对应的投掷物；同一路的多个功能依次执行。"+(天帝顺序道纹.攻击形态(功能)?"形态空出口结束，不额外射普通弹。":"空出口保留基础弹。")+"不同投掷物可分别命中同一敌人，连锁不回跳。", 文字左, 56, 文字宽, 19, 天帝道纹美术.正文);
        完成行(行, Mathf.Max(158, 文.rectTransform.sizeDelta.y + 76), ref y);
    }
    RectTransform 行框(string 名, float y) => 底(内容, "图鉴行-" + 名, 0, y, 列表宽, 200, "二级面板");
    void 添加特性(道纹分类 kind,int id,ref float y)
    {
        var r=天帝特性道纹.创建(1,kind,id,道纹品阶.普通,查询物品等级,0);var row=行框(kind+"-"+id,y);画图标(row,r);
        字(row,"道纹名称",r.名称,文字左,14,780,36,24,天帝道纹美术.正文);
        字(row,"道纹类别",kind+" / 八品阶 / 不可改造",980,20,312,26,17,天帝道纹美术.次文).alignment=TextAnchor.MiddleRight;
        var text=正文(row,"特性机制",特性说明(r),文字左,56,文字宽,19,天帝道纹美术.正文);
        特性查询.Add((r,text));完成行(row,Mathf.Max(158,text.rectTransform.sizeDelta.y+76),ref y);
    }
    readonly List<(道纹实例 纹,Text 文)> 特性查询=new List<(道纹实例,Text)>();
    string 特性说明(道纹实例 纹)
    {
        if(纹.分类==道纹分类.转化)
            return 纹.介绍+"\n普通品阶：半径 "+天帝特性道纹.品阶值(纹,"半径")+" 格，转化效率 "+(天帝特性道纹.品阶值(纹,"转换")*100).ToString("0.##")+"%。\n仅转换第一条接通源通路内的范围属性；目标冲突保留原词条，同目标取最高效率。";
        string 条件="";for(int i=0;i<天帝特性道纹.数值(纹.特性编号,"condition_count");i++)条件+=天帝特性道纹.文本(纹.特性编号,"条件."+i+".0")+"≥"+天帝特性道纹.门槛(纹,i).ToString("0.##")+"；";
        return 纹.介绍+"\n普通品阶条件（物品"+查询物品等级+"级）："+条件+"\n固定相对两口 · 同名最高激活品阶生效 · 下游功能不被接管";
    }
    void 完成行(RectTransform 行, float 高, ref float y) { 行.sizeDelta = new Vector2(列表宽, 高); y += 高 + 12; }
    void 画图标(RectTransform 行, 道纹实例 示意)
    {
        var 图区 = 区(行, "道纹图标", 20, 18, 108, 108); var 图 = 图区.gameObject.AddComponent<天帝道纹绘图>();
        图.单纹模式 = true; 图.单纹 = 示意; 图.构筑美术 = true; 图.单纹半径 = 46; 图.raycastTarget = false;
        var 标识=字(图区,"道纹单字",天帝道纹美术.单字(示意),0,0,108,108,32,天帝道纹美术.正文);
        标识.alignment=TextAnchor.MiddleCenter;标识.raycastTarget=false;
        天帝道纹单字.绑定(标识, 图区);
        标识.resizeTextForBestFit=true;标识.resizeTextMinSize=18;标识.resizeTextMaxSize=32;
        字(行, "示意标签", "外观示意", 20, 130, 108, 24, 15, 天帝道纹美术.次文).alignment = TextAnchor.MiddleCenter;
    }
    public void 设置查询物品等级(int 等级)
    {
        查询物品等级 = Mathf.Clamp(等级, 1, 天帝数值.玩家上限); if (等级输入 != null) 等级输入.SetTextWithoutNotify(查询物品等级.ToString());
        foreach (var 项 in 范围列表) if (项 != null && 项.文字 != null) 项.文字.text = 范围说明(项.属性, 查询物品等级);
        foreach(var item in 特性查询)
        {
            if(item.文 == null || item.纹 == null) continue;
            item.纹.物品等级=查询物品等级;item.文.text=特性说明(item.纹);
        }
        if (天帝移动适配.启用 && 视口 != null && 视口.rect.width < 列表宽) 排版移动正文();
        if (剪纸详情目标 != null) 展示剪纸详情(剪纸详情目标);
    }
    public bool 定位分类(string 分类)
    {
        if (剪纸分类行.Count > 0) return 显示剪纸分类(分类);
        if (滚动 == null || !分类位置.TryGetValue(分类, out float 位置)) return false;
        return 定位位置(位置);
    }
    bool 定位位置(float 位置)
    {
        if (剪纸分类行.Count > 0) { 展示剪纸共通规则(); return true; }
        隐藏详情();
        Canvas.ForceUpdateCanvases(); 滚动.StopMovement();
        滚动.verticalNormalizedPosition = 1 - Mathf.Clamp01(位置 / Mathf.Max(1, 内容.rect.height - 视口.rect.height)); 更新分类高亮(); return true;
    }
    void 更新分类高亮()
    {
        if (剪纸分类行.Count > 0) return;
        if (滚动 == null || 内容 == null) return;
        float 顶 = Mathf.Max(0, 内容.anchoredPosition.y) + 2; string 当前 = 分类名[0];
        foreach (var 名 in 分类名) if (分类位置.TryGetValue(名, out float 位置) && 顶 >= 位置) 当前 = 名;
        if (滚动.verticalNormalizedPosition <= .001f) 当前 = 分类名[分类名.Length - 1];
        foreach (var 项 in 分类按钮)
        {
            var 图 = 项.Value?.targetGraphic as Image;
            if (图 != null) 天帝道纹美术.选中(图, 项.Key == 当前);
        }
    }
    public static string 范围说明(道纹属性 属性, int 物品等级)
    {
        物品等级 = Mathf.Clamp(物品等级, 1, 天帝数值.玩家上限); 天帝数值.词条定点范围(属性, 物品等级, out int 下, out int 上);
        double 尺度 = 天帝数值.取("rune.storage_scale"); string 单位 = 属性 == 道纹属性.攻速 || 属性 == 道纹属性.移速 ? "%" : 属性 == 道纹属性.弧度 ? "°/秒" : "";
        string 范围 = "+" + (下 / 尺度).ToString("0.##") + 单位; if (上 != 下) 范围 += " ～ +" + (上 / 尺度).ToString("0.##") + 单位;
        return "物品等级 " + 物品等级 + " · 单条「" + 天帝道纹属性.词条名称(属性) + "」 " + 范围 +
            (天帝道纹属性.是功能(属性) ? "  /  固定稀有 · 1条功能词条" : "  /  所有品阶使用同一区间");
    }
    static string 数(string 路径) => 天帝数值.取(路径).ToString("0.##");
    static string 百分比(string 路径) => (天帝数值.取(路径) * 100).ToString("0.##") + "%";
    static string 类型说明(道纹属性 属性)
    {
        switch (属性)
        {
            case 道纹属性.力量: return "提升全局力量，增加物理攻击、最大血量与防御。每点有效力量增益提供 " + 数("player.attack_per_strength_bonus") + " 点物理攻击；道纹增益超过软上限后收益递减。";
            case 道纹属性.速度: return "提升全局速度，影响物理攻击、攻速、暴击率、移动/跑步与闪避。每点有效速度增益提供 " + 数("player.attack_per_speed_bonus") + " 点物理攻击，其他收益受各自软上限限制。";
            case 道纹属性.智力: return "提升全局智力，影响物理攻击、灵力、灵气护盾、暴击倍率、抗性与急速。每点有效智力增益提供 " + 数("player.attack_per_intelligence_bonus") + " 点物理攻击。";
            case 道纹属性.血量: return "提高最大血量，全局生效；道纹正增益超过软上限后收益递减。更换道纹不补满当前血量。";
            case 道纹属性.灵力: return "提高最大灵力，全局生效，并通过灵力影响弹体飞行速度；增益受软上限限制，更换道纹不自动回复。";
            case 道纹属性.防御: return "提高防御，全局生效；降低物理部分的伤害，元素部分由抗性减伤。防御不是固定减伤百分比，实际效果与攻击者等级有关。";
            case 道纹属性.护盾: return "提高灵气护盾上限，全局生效；受击优先消耗护盾。正增益受软上限限制，增加上限不立即回复当前护盾。";
            case 道纹属性.攻速: return "按百分比提高攻击速度，全局生效，缩短所有通路共用的射击间隔；增益递减，最终最高 " + 数("player.aps_max") + " 次/秒。单条区间不随物品等级提高。";
            case 道纹属性.移速: return "按百分比提高移动和跑步速度，全局生效；增益递减，移动最高 " + 数("player.move_max") + " 米/秒、跑步最高 " + 数("player.run_max") + " 米/秒。单条区间不随物品等级提高。";
            case 道纹属性.数量: return "本通路每次正常射击增加灵力弹数量，含基础与天赋在内最多 " + 数("shape.quantity_max") + " 枚；各根弹分别按分裂词条生成子弹。词条固定+1，不随物品等级或品阶变动。";
            case 道纹属性.分裂: return "本通路命中后产生分裂弹，最多 " + 数("shape.split_max") + " 枚；每枚伤害为原释放的 " + 百分比("shape.split_factor") + "，分裂弹不再次分裂。词条固定+1。";
            case 道纹属性.连锁: return "本通路命中后跳向其他目标，最多 " + 数("shape.chain_max") + " 次；每次连锁的伤害倍率乘 " + 百分比("shape.chain_factor") + "，同次释放不重复命中同一敌人。词条固定+1。";
            case 道纹属性.弧度: return "旧存档保留词条，当前不生效。子弹发射后沿固定方向飞行，敌人移动可以躲开。";
            case 道纹属性.范围: return "本通路每点增加 " + 数("shape.radius_per_point") + " 米命中溅射半径，最终最高 " + 数("shape.radius_max") + " 米；最多波及 " + 数("shape.splash_targets_max") + " 个次级目标，伤害为原释放的 " + 百分比("shape.splash_factor") + "。词条固定+1。";
            default: return "增加本通路的「" + 天帝道纹属性.词条名称(属性) + "」，与该通路其他五行词条累加并受软上限限制。元素伤害按抗性减伤，物理部分单独按防御减伤；当前不附带元素控制或独有特效。";
        }
    }
    void 创建浮窗()
    {
        浮窗 = 底(根, "道纹悬停详情", 0, 0, 440, 240, "详情框"); var 透传 = 浮窗.gameObject.AddComponent<CanvasGroup>(); 透传.blocksRaycasts = false; 透传.interactable = false;
        天帝响应布局.动态(浮窗);
        浮窗字 = 字(浮窗, "悬停正文", "", 18, 16, 404, 208, 18, 天帝道纹美术.正文); 浮窗字.alignment = TextAnchor.UpperLeft; 浮窗字.lineSpacing = 1.12f; 浮窗.gameObject.SetActive(false);
    }
    // 保留原悬停组件入口；主要规则已内联，不需要悬停才能阅读。
    public void 显示详情(道纹属性 属性, bool 分叉, Vector2 屏幕)
    {
        if (浮窗 == null || 浮窗字 == null || 根 == null) return;
        浮窗字.text = 分叉 ? "分叉道纹\n无属性词条；掉落时获得3–6个固定接口，可以旋转，不能改造。" : 属性 + "道纹\n" + 类型说明(属性) + "\n" + 范围说明(属性, 查询物品等级);
        float 宽 = Mathf.Min(440, Mathf.Max(120, 根.rect.width - 24));
        浮窗字.rectTransform.sizeDelta = new Vector2(宽 - 36, 740);
        float 高 = Mathf.Min(浮窗字.preferredHeight + 36, 根.rect.height - 24);
        浮窗.sizeDelta = new Vector2(宽, 高); 浮窗字.rectTransform.sizeDelta = new Vector2(宽 - 36, 高 - 36);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根, 屏幕, null, out var 点);
        float x = 点.x - 根.rect.xMin + 20, y = 根.rect.yMax - 点.y + 20;
        if (x + 宽 > 根.rect.width - 12) x -= 宽 + 40;
        if (y + 高 > 根.rect.height - 12) y -= 高 + 40;
        浮窗.anchoredPosition = new Vector2(Mathf.Clamp(x, 12, Mathf.Max(12, 根.rect.width - 宽 - 12)), -Mathf.Clamp(y, 12, Mathf.Max(12, 根.rect.height - 高 - 12))); 浮窗.SetAsLastSibling(); 浮窗.gameObject.SetActive(true);
    }
    public void 隐藏详情() { if (浮窗 != null) 浮窗.gameObject.SetActive(false); }
    void OnApplicationFocus(bool 焦点) { if (!焦点) 隐藏详情(); }
    void OnDisable() => 隐藏详情();
    RectTransform 区(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    RectTransform 底(RectTransform 父, string 名, float x, float y, float w, float h, string 资源, Color? 色 = null)
    {
        var t = 区(父, 名, x, y, w, h); var 图 = t.gameObject.AddComponent<Image>(); 图.color = 色 ?? new Color(.08f, .14f, .15f);
        if (资源 != null) 天帝道纹美术.应用(图, 资源); 图.raycastTarget = true; return t;
    }
    Text 字(RectTransform 父, string 名, string 文, float x, float y, float w, float h, int 大小, Color 色)
    {
        var t = 区(父, 名, x, y, w, h).gameObject.AddComponent<Text>(); t.font = 字体; t.text = 文; t.fontSize = 大小; t.color = 天帝道纹美术.纸面文字(色);
        t.alignment = TextAnchor.MiddleLeft; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; 天帝界面美术.文字(t,名); return t;
    }
    Text 正文(RectTransform 父, string 名, string 文, float x, float y, float w, int 大小, Color 色)
    {
        var t = 字(父, 名, 文, x, y, w, 200, 大小, 色); t.alignment = TextAnchor.UpperLeft; t.lineSpacing = 1.15f; t.rectTransform.sizeDelta = new Vector2(w, Mathf.Ceil(t.preferredHeight) + 6); return t;
    }
    Button 按钮(RectTransform 父, string 名, string 文, float x, float y, float w, float h, Action 点击)
    {
        var t = 底(父, 名, x, y, w, h, "按钮"); if (t == null) return null; var 图 = t.GetComponent<Image>(); var 键 = t.gameObject.AddComponent<Button>(); 键.targetGraphic = 图; 天帝道纹美术.设置按钮(键);
        键.onClick.AddListener(() => 点击?.Invoke()); 字(t, "按钮文字", 文, 6, 0, w-12, h, 19, 天帝道纹美术.正文).alignment = TextAnchor.MiddleCenter; 天帝道纹美术.设置按钮(键); return 键;
    }
}

public sealed class 天帝图鉴悬停 : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    public 天帝道纹图鉴 页面;
    public 道纹属性 属性;
    public bool 分叉;
    public void OnPointerEnter(PointerEventData e) => 页面.显示详情(属性, 分叉, e.position);
    public void OnPointerMove(PointerEventData e) => 页面.显示详情(属性, 分叉, e.position);
    public void OnPointerExit(PointerEventData e) => 页面.隐藏详情();
}
