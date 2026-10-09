using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝角色界面 : MonoBehaviour
{
    天帝主角属性 人;
    天帝道纹 网;
    Font 字体;
    RectTransform 正文;
    Text 姓名, 状态, 评语;
    readonly List<Action> 更新项 = new List<Action>();
    readonly Button[] 页签 = new Button[4];
    readonly string[] 页名 = { "角色属性", "战斗进阶", "技能与形态", "道纹分布" };
    readonly Color 墨 = new Color(.13f, .23f, .22f), 次墨 = new Color(.35f, .43f, .39f);
    readonly Color 青 = new Color(.28f, .49f, .43f), 纸 = new Color(.95f, .95f, .90f);
    bool 已订阅;

    public void 初始化(天帝主角属性 主角, 天帝道纹 数据, Font 字, Action 关闭)
    {
        人 = 主角; 网 = 数据; 字体 = 字;
        var 根 = (RectTransform)transform;
        var 遮 = 图(根, "角色遮罩", 0, 0, 1600, 900, new Color(.02f, .05f, .045f, .72f)); 遮.raycastTarget = true;
        var 框 = 图(根, "角色详情面板", 120, 60, 1360, 780, 纸).rectTransform; 框.GetComponent<Image>().raycastTarget = true;
        字文(框, "角色", "角色", 32, 18, 500, 52, 32, 墨);
        按钮(框, "关闭角色", "关闭", 1180, 18, 148, 56, 关闭);
        图(框, "分隔线", 32, 86, 1296, 1, new Color(.73f, .80f, .74f));
        姓名 = 字文(框, "角色名字", "", 32, 108, 284, 58, 28, 墨);
        姓名.resizeTextForBestFit = true; 姓名.resizeTextMinSize = 16; 姓名.resizeTextMaxSize = 28;
        状态 = 字文(框, "角色等级", "", 32, 162, 284, 58, 19, 次墨);
        var 立绘 = 图(框, "角色立绘", 54, 216, 240, 252, Color.white, 天帝剪纸界面皮肤.素材("角色纸雕") ?? 天帝美术资源.当前?.获取("CH01")); 立绘.preserveAspect = true;
        字文(框, "天赋标题", "本命天赋", 32, 484, 284, 32, 19, 次墨);
        var 天赋 = 网.天赋;
        字文(框, "天赋名称", 天赋?.名称 ?? "未选择", 32, 522, 284, 42, 28, 青);
        字文(框, "天赋效果", 天赋?.效果 ?? "无", 32, 574, 284, 66, 22, 墨);
        字文(框, "天赋说明", 天赋?.说明 ?? "", 32, 644, 284, 120, 19, 次墨);
        图(框, "竖线", 344, 108, 1, 640, new Color(.73f, .80f, .74f));
        for (int i = 0; i < 页签.Length; i++)
        {
            int 页 = i;
            页签[i] = 按钮(框, "页签-" + 页名[i], 页名[i], 376 + i * 242, 108, 226, 52, () => 显示页(页));
        }
        正文 = 区(框, "角色正文", 376, 184, 952, 502);
        图(框, "底线", 376, 700, 952, 1, new Color(.73f, .80f, .74f));
        评语 = 字文(框, "实力评语", "", 376, 714, 952, 42, 22, 墨);
        if (天帝移动适配.启用)
        {
            天帝响应布局.比例(框, .025f, .025f, .95f, .95f);
            var 正文口 = 天帝响应布局.滚动正文(正文);
            var 身份口 = 天帝响应布局.滚动列(框, "角色身份信息", 32, 108, 284, 656);
            if(天帝剪纸界面皮肤.已启用)
            {
                foreach(var 口 in new[]{正文口,身份口})
                {
                    var 衬=口.GetComponent<Image>()??口.gameObject.AddComponent<Image>();
                    衬.sprite=天帝剪纸界面皮肤.素材("素纸");衬.type=Image.Type.Sliced;衬.color=new Color(1,1,1,.96f);
                }
            }
            var 重排身份 = 天帝双端页面布局.重排正文(身份口.GetComponent<ScrollRect>().content);
            var 页签列 = new RectTransform[页签.Length]; for (int i = 0; i < 页签.Length; i++) 页签列[i] = (RectTransform)页签[i].transform;
            var 页签口 = 天帝双端页面布局.横列(框, "角色页签视口", 页签列, 126);
            天帝双端页面布局.移动页(框, 面板 =>
            {
                天帝双端页面布局.页头(面板, "关闭角色");
                float 宽 = 面板.rect.width, 高 = 面板.rect.height, 左宽 = Mathf.Clamp(宽 * .25f, 150, 220);
                天帝双端页面布局.固定(身份口, 8, 天帝双端页面布局.页头高度, 左宽 - 12, 高 - 76);
                天帝双端页面布局.固定(页签口, 左宽 + 8, 天帝双端页面布局.页头高度, 宽 - 左宽 - 16, 44);
                天帝双端页面布局.固定(正文口, 左宽 + 8, 118, 宽 - 左宽 - 16, 高 - 164);
                天帝双端页面布局.固定(评语.rectTransform, 左宽 + 8, 高 - 42, 宽 - 左宽 - 16, 34);
                天帝双端页面布局.区域(面板, "竖线", 左宽, 天帝双端页面布局.页头高度, 1, 高 - 76);
                重排身份(); 正文.GetComponent<天帝正文排版>()?.更新?.Invoke();
            });
        }
        if (天帝剪纸界面皮肤.已启用 && !天帝移动适配.启用)
        {
            void 衬纸(string 名,float x,float y,float w,float h)
            {
                var 衬=图(框,名,x,y,w,h,Color.white,天帝剪纸界面皮肤.素材("卡片纸框"));
                衬.type=Image.Type.Sliced;衬.pixelsPerUnitMultiplier=2;衬.raycastTarget=false;衬.transform.SetAsFirstSibling();
            }
            衬纸("角色资料衬纸",166,122,178,414);
            衬纸("角色正文衬纸",360,174,984,592);
            衬纸("天赋说明衬纸",24,632,302,130);
            天帝双端页面布局.固定(立绘.rectTransform,8,122,156,510);
            天帝双端页面布局.固定(姓名.rectTransform,174,126,160,54);
            天帝双端页面布局.固定(状态.rectTransform,174,184,160,88);
            天帝双端页面布局.区域(框,"天赋标题",174,292,160,34);
            天帝双端页面布局.区域(框,"天赋名称",174,334,160,44);
            天帝双端页面布局.区域(框,"天赋效果",174,392,160,122);
            天帝双端页面布局.区域(框,"天赋说明",32,636,284,112);
        }
        天帝剪纸界面皮肤.装配(根,"角色",框);
        装配山水角色册(框);
        显示页(0); 订阅();
    }

    void 订阅()
    {
        if (已订阅 || 人 == null) return;
        人.属性改变 += 刷新; 已订阅 = true;
    }
    void OnEnable() { 订阅(); if (人 != null) 刷新(); }
    void OnDisable() { 隐藏属性说明(); if (已订阅) 人.属性改变 -= 刷新; 已订阅 = false; }
    void OnDestroy() { OnDisable(); }
    public void 刷新()
    {
        if (this == null) { if (人 != null) 人.属性改变 -= 刷新; 已订阅 = false; return; }
        if (人 == null || 网 == null || 姓名 == null || 状态 == null || 评语 == null) return;
        姓名.text = 人.名字;
        状态.text = "等级 " + 人.等级 + (天帝剪纸界面皮肤.已启用&&!天帝移动适配.启用?"\n技能点 ":"  ·  技能点 ") + 网.技能点 + "\n" + (人.等级 >= 天帝数值.玩家上限 ? "等级已满" : "经验 " + 网.当前经验 + " / " + 网.升级所需经验);
        评语.text = 天帝实力评语.读取(网, 人);
        foreach (var 更新 in 更新项) 更新();
        刷新属性说明();
    }
    void 显示页(int 页)
    {
        隐藏属性说明();
        更新项.Clear();
        for (int i = 正文.childCount - 1; i >= 0; i--)
        {
            var 子 = 正文.GetChild(i).gameObject; 子.SetActive(false);
            if (Application.isPlaying) Destroy(子); else DestroyImmediate(子);
        }
        for (int i = 0; i < 页签.Length; i++)
        {
            天帝道纹美术.选中(页签[i].GetComponent<Image>(),i==页);
            页签[i].GetComponentInChildren<Text>().color = i == 页 ? 天帝道纹美术.正文 : 天帝道纹美术.次文;
        }
        if (页 == 0) 属性页(); else if (页 == 1) 进阶页(); else if (页 == 2) 技能页(); else 道纹页();
        刷新();
        if (天帝移动适配.启用) 天帝双端页面布局.重排正文(正文)();
        装配山水角色正文(页);
    }
    static string 数(double 值) => 值.ToString("0.##");
    void 绑定(Text 文, Func<string> 取值) { 更新项.Add(() => 文.text = 取值()); }
    void 行(string 名, float x, float y, Func<string> 值, float 宽 = 440)
    {
        bool 重点 = 名 == "攻击力" || 名 == "血量" || 名 == "单体 DPS" || 名 == "八目标 DPS";
        if (重点) 图(正文, "重点属性衬底", x - 4, y - 2, 宽 + 8, 47, 天帝道纹美术.行底);
        字文(正文, "标签-" + 名, 名, x, y, 宽 * .42f, 42, 重点 ? 22 : 20, 重点 ? 墨 : 次墨);
        var 文 = 字文(正文, "数值-" + 名, "", x + 宽 * .42f, y, 宽 * .58f, 42, 重点 ? 26 : 21, 重点 ? 天帝道纹美术.强调 : 墨);
        文.fontStyle = 重点 ? FontStyle.Bold : FontStyle.Normal;
        文.resizeTextForBestFit=true;文.resizeTextMinSize=天帝移动适配.启用?14:16;文.resizeTextMaxSize=重点?26:21;
        文.alignment = TextAnchor.MiddleRight; 绑定(文, 值);
        图(正文, "行线", x, y + 46, 宽, 1, new Color(.83f, .87f, .81f));
        绑定属性说明(名, x, y, 宽, 47);
    }
    void 属性页()
    {
        字文(正文, "基础属性标题", "基础属性", 0, 0, 440, 42, 26, 墨);
        行("力量", 0, 58, () => 数(人.力量));
        行("速度", 0, 118, () => 数(人.速度));
        行("智力", 0, 178, () => 数(人.智力));
        行("攻击力", 0, 266, () => 数(人.攻击力));
        行("防御", 0, 326, () => 数(人.防御));
        字文(正文, "衍生属性标题", "资源与移动", 504, 0, 448, 42, 26, 墨);
        行("血量", 504, 58, () => 数(人.当前血量) + " / " + 数(人.最大血量));
        行("灵力", 504, 118, () => 数(人.当前灵力) + " / " + 数(人.最大灵力));
        行("灵气护盾", 504, 178, () => 数(人.当前灵气护盾) + " / " + 数(人.最大灵气护盾));
        行("移动速度", 504, 266, () => 数(人.移动速度) + " 米/秒");
        行("跑步速度", 504, 326, () => 数(人.跑步速度) + " 米/秒");
        var 概况 = 字文(正文, "画布概况", "", 0, 424, 952, 66, 20, 次墨);
        绑定(概况, () => "画布已放置 " + (网.已放置.Count - 1) + " 枚 · 生效 " + 网.生效数 + " 枚\n暴击、抗性与伤害估算见「战斗进阶」。");
    }
    void 进阶页()
    {
        字文(正文,"输出标题","攻击与暴击",0,0,440,42,26,墨);
        行("攻击速度",0,58,()=>数(人.攻击速度)+" 次/秒");
        行("暴击率",0,118,()=>数(人.暴击率*100)+"%");
        行("暴击伤害",0,178,()=>数(人.暴击倍率*100)+"%");
        行("技能急速",0,238,()=>数(人.技能急速));
        字文(正文,"防御标题","防御与生存",504,0,448,42,26,墨);
        行("防御",504,58,()=>数(人.防御));
        行("抗性",504,118,()=>数(人.抗性*100)+"%");
        行("闪避率",504,178,()=>数(人.闪避率*100)+"%");
        行("纸面战力",504,238,()=>数(人.综合战斗力));
        字文(正文,"伤害估算标题","满命中伤害估算",0,322,952,38,24,墨);
        行("单体 DPS",0,374,()=>数(人.单体期望DPS));
        行("八目标 DPS",504,374,()=>数(人.八目标期望DPS));
        字文(正文,"估算说明","估算用于比较构筑，实战伤害还受命中与敌人减伤影响。",0,448,952,46,19,次墨);
    }
    void 技能页()
    {
        字文(正文, "技能名称", "道纹技能 · 自动索敌", 0, 0, 420, 42, 27, 墨);
        var 参数 = 字文(正文, "技能参数", "", 0, 56, 952, 70, 22, 墨);
        绑定(参数, () => { var p = 普攻参数.读取(网, 人); return p.顺序计划 != null ? "第一通路按顺序执行 · 首发 " + p.数量 + " 枚 · " + 数(p.间隔) + " 秒/次\n各弹伤害与后续形态独立计算，详见道纹画布的加成来源。" : "第一通路伤害 " + 数(p.伤害) + "（普通 " + 数(p.普通伤害) + " + 五行 " + 数(p.五行额外伤害) + "）   ·   攻击间隔 " + 数(p.间隔) + " 秒\n已解封 " + 网.开放通路数 + " / 参与射击 " + 网.射击通路数 + " 路 · 各路形态与伤害见道纹画布。"; });
        字文(正文, "形态列", "形态", 0, 142, 180, 34, 19, 次墨);
        字文(正文, "道纹列", "第一通路", 180, 142, 138, 34, 19, 次墨);
        字文(正文, "实际列", "实际效果", 352, 142, 600, 34, 19, 次墨);
        var 属性 = new[] { 道纹属性.数量, 道纹属性.分裂, 道纹属性.连锁, 道纹属性.弧度, 道纹属性.范围 };
        for (int i = 0; i < 属性.Length; i++)
        {
            var a = 属性[i]; float y = 186 + i * 50;
            字文(正文, "形态-" + a, a.ToString(), 0, y, 138, 34, 22, 墨);
            var 加成 = 字文(正文, "形态加成-" + a, "", 180, y, 138, 34, 22, 次墨); 绑定(加成, () => "+" + 网.弹槽加成[0][(int)a]);
            var 实际 = 字文(正文, "形态效果-" + a, "", 352, y, 348, 34, 22, 墨);
            图(正文, "形态轨道-" + a, 716, y + 13, 236, 8, new Color(.80f, .86f, .79f));
            var 条 = 图(正文, "形态进度-" + a, 716, y + 13, 0, 8, 青);
            更新项.Add(() =>
            {
                var p = 普攻参数.读取(网, 人); float 比例;
                if (p.顺序计划 != null && a != 道纹属性.数量)
                {
                    实际.text = "按分段链路执行"; 天帝响应布局.进度(条.rectTransform, 0, 236); return;
                }
                switch (a)
                {
                    case 道纹属性.数量: 实际.text = p.数量 + " 枚/次"; 比例 = p.数量 / 6f; break;
                    case 道纹属性.分裂: 实际.text = p.分裂 > 0 ? p.分裂 + " 枚衍生弹" : "未启用分裂"; 比例 = p.分裂 / 3f; break;
                    case 道纹属性.连锁: 实际.text = p.连锁 + " 次跳转"; 比例 = p.连锁 / 4f; break;
                    case 道纹属性.弧度: 实际.text = "旧词条 · 不生效"; 比例 = 0; break;
                    default: 实际.text = 数(p.溅射半径) + " 米溅射半径"; 比例 = p.溅射半径 / 4f; break;
                }
                天帝响应布局.进度(条.rectTransform, 比例, 236);
            });
            绑定属性说明(a.ToString(), 0, y, 952, 42);
        }
        var 被动 = 字文(正文, "技能被动", "", 0, 458, 952, 38, 20, 次墨);
        绑定(被动, () => 网.天赋?.种类 == 天赋种类.余响 ? "余响：每第5次正常射击，0.2秒后追加一次回响。" : "天然普攻 · 自动索敌");
    }
    void 道纹页()
    {
        字文(正文, "基础标题", "基础属性", 0, 0, 448, 42, 25, 墨);
        字文(正文, "普通标题", "普通属性", 0, 174, 448, 42, 25, 墨);
        字文(正文, "形态标题", "攻击形态（全网去重总览）", 504, 0, 448, 42, 23, 墨);
        字文(正文, "元素标题", "五行伤害（全网去重总览）", 504, 220, 448, 42, 23, 墨);
        void 汇总行(道纹属性 a, float x, float y)
        {
            字文(正文, "汇总标签-" + a, a.ToString(), x, y, 290, 32, 21, 次墨);
            var 文 = 字文(正文, "汇总-" + a, "", x + 306, y, 134, 32, 22, 墨);
            文.alignment = TextAnchor.MiddleRight;
            更新项.Add(() => { double 值 = 网.生效加成[(int)a]; 文.text = 值 == 0 ? "—" : 天帝道纹属性.数值文字(a, 值); 文.color = 值 == 0 ? 次墨 : 天帝道纹美术.强调; 文.fontStyle = 值 == 0 ? FontStyle.Normal : FontStyle.Bold; });
            绑定属性说明("道纹：" + a, x, y, 440, 32);
        }
        for (int i = 0; i < 天帝道纹属性.基础属性.Length; i++) 汇总行(天帝道纹属性.基础属性[i], 0, 56 + i * 35);
        for (int i = 0; i < 天帝道纹属性.普通属性.Length; i++) 汇总行(天帝道纹属性.普通属性[i], 0, 215 + i * 35);
        for (int i = 0; i < 天帝道纹属性.形态属性.Length; i++) 汇总行(天帝道纹属性.形态属性[i], 504, 56 + i * 35);
        for (int i = 0; i < 天帝道纹属性.五行属性.Length; i++) 汇总行(天帝道纹属性.五行属性[i], 504, 261 + i * 35);
        var 接口 = 字文(正文, "源纹接口", "", 0, 432, 952, 80, 17, 次墨);
        绑定(接口, () =>
        {
            var 源 = 网.已放置[Vector2Int.zero]; var 开 = new List<string>(); var 封 = new List<string>();
            for (int i = 0; i < 6; i++) if (源.有接口(i)) 开.Add(天帝道纹.方向名[i]);
            for (int i = 1; i <= 5; i++) { int d = (6 - i) % 6; if (!源.有接口(d)) 封.Add(天帝道纹.方向名[d] + " " + i * 10 + "级"); }
            return (网.生效数 == 0 ? "尚无有效道纹加成 · 在画布接通源纹后生效。" : "已生效 " + 网.生效数 + " 枚道纹 · — 表示该项无加成。") + "\n源纹已启用：" + string.Join("、", 开) + "\n封印接口：" + (封.Count == 0 ? "已全部解封" : string.Join(" · ", 封));
        });
    }
    RectTransform 区(RectTransform 父, string 名, float x, float y, float w, float h)
    { return 天帝响应布局.创建(父, 名, x, y, w, h); }
    Image 图(RectTransform 父, string 名, float x, float y, float w, float h, Color 色, Sprite 素材 = null)
    {
        var 像 = 区(父, 名, x, y, w, h).gameObject.AddComponent<Image>(); 像.color = 色; 像.sprite = 素材; 像.raycastTarget = false;
        if(素材==null)天帝界面美术.自动面板(像,名,w,h,色,名=="角色详情面板");return 像;
    }
    Text 字文(RectTransform 父, string 名, string 内容, float x, float y, float w, float h, int 大小, Color 色)
    {
        var 文 = 区(父, 名, x, y, w, h).gameObject.AddComponent<Text>(); 文.font = 字体; 文.text = 内容; 文.fontSize = 大小;
        文.color = 色; 文.alignment = TextAnchor.MiddleLeft; 文.raycastTarget = false; 文.supportRichText = false;
        文.horizontalOverflow = HorizontalWrapMode.Wrap; 文.verticalOverflow = VerticalWrapMode.Truncate; 天帝界面美术.文字(文,名); return 文;
    }
    Button 按钮(RectTransform 父, string 名, string 内容, float x, float y, float w, float h, Action 点击)
    {
        var 像 = 图(父, 名, x, y, w, h, new Color(.86f, .90f, .84f)); 像.raycastTarget = true;
        var 键 = 像.gameObject.AddComponent<Button>(); 键.targetGraphic = 像;
        字文(像.rectTransform, "按钮文字", 内容, 8, 0, w - 16, h, 23, 墨).alignment = TextAnchor.MiddleCenter;
        键.onClick.AddListener(() => 点击?.Invoke());天帝界面美术.按钮(键);return 键;
    }
}
