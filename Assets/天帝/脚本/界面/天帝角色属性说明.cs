using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed partial class 天帝角色界面
{
    RectTransform 属性浮窗;
    Text 属性说明标题, 属性说明正文;
    string 当前说明属性;
    Vector2 说明指针;
    Camera 说明相机;
    bool 详细说明已打开;
    Button 说明关闭键;
    RectTransform 触屏说明视口;

    void 绑定属性说明(string 名, float x, float y, float 宽, float 高)
    {
        var 命中 = 图(正文, "属性说明入口-" + 名, x, y, 宽, 高, Color.clear);
        命中.raycastTarget = true;
        var 事件 = 命中.gameObject.AddComponent<天帝角色属性悬停>();
        事件.页面 = this; 事件.属性 = 名;
    }
    void 创建属性浮窗()
    {
        if (属性浮窗 != null) return;
        属性浮窗 = 图((RectTransform)transform, "角色属性说明浮窗", 0, 0, 620, 300, 纸).rectTransform;
        天帝响应布局.动态(属性浮窗);
        天帝界面美术.面板(属性浮窗.GetComponent<Image>(), "属性面板", Color.white);
        var 组 = 属性浮窗.gameObject.AddComponent<CanvasGroup>(); 组.blocksRaycasts = false; 组.interactable = false;
        属性说明标题 = 字文(属性浮窗, "属性说明标题", "", 22, 16, 576, 38, 25, 墨);
        属性说明正文 = 字文(属性浮窗, "属性说明正文", "", 22, 62, 576, 680, 18, 墨);
        属性说明正文.supportRichText = true;
        属性说明正文.lineSpacing = 1.12f;
        说明关闭键 = 按钮(属性浮窗, "关闭属性公式", "关闭", 450, 8, 148, 56, () => 隐藏属性说明());
        说明关闭键.gameObject.SetActive(false);
        if (天帝移动适配.启用)
        {
            触屏说明视口 = new GameObject("属性说明滚动视口", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
            触屏说明视口.SetParent(属性浮窗, false); 触屏说明视口.anchorMin = Vector2.zero; 触屏说明视口.anchorMax = Vector2.one;
            触屏说明视口.offsetMin = new Vector2(22, 14); 触屏说明视口.offsetMax = new Vector2(-22, -72);
            触屏说明视口.GetComponent<Image>().color = Color.clear;
            属性说明正文.rectTransform.SetParent(触屏说明视口, false); 属性说明正文.rectTransform.anchoredPosition = Vector2.zero;
            var s = 触屏说明视口.GetComponent<ScrollRect>(); s.viewport = 触屏说明视口; s.content = 属性说明正文.rectTransform;
            s.horizontal = false; s.vertical = true; s.movementType = ScrollRect.MovementType.Clamped;
        }
        属性浮窗.gameObject.SetActive(false);
    }
    public void 显示属性说明(string 名, Vector2 位置, Camera 相机 = null)
    {
        if (!isActiveAndEnabled || 人 == null || 网 == null || 详细说明已打开) return;
        创建属性浮窗(); 当前说明属性 = 名; 说明指针 = 位置; 说明相机 = 相机;
        属性浮窗.gameObject.SetActive(true); 属性浮窗.SetAsLastSibling(); 刷新属性说明();
    }
    void 刷新属性说明()
    {
        if (属性浮窗 == null || !属性浮窗.gameObject.activeSelf || 当前说明属性 == null) return;
        属性说明标题.text = 当前说明属性.Replace("道纹：", "道纹加成 · ");
        属性说明正文.text = 详细说明已打开 ? 天帝角色属性公式.读取(当前说明属性, 人, 网) : 天帝角色属性公式.简介(当前说明属性, 人, 网) + (天帝移动适配.启用 ? "\n\n再次点此属性查看公式" : "\n\n点击属性查看完整公式");
        var 根 = (RectTransform)transform;
        float 宽 = Mathf.Min(详细说明已打开 ? 620 : 460, 根.rect.width - 24);
        属性说明标题.rectTransform.sizeDelta = new Vector2(宽 - (详细说明已打开 || 天帝移动适配.启用 ? 194 : 44), 38);
        说明关闭键.gameObject.SetActive(详细说明已打开 || 天帝移动适配.启用);
        天帝双端页面布局.按键(说明关闭键.GetComponent<RectTransform>(), 宽 - 170, 8, 148, 56);
        属性浮窗.GetComponent<CanvasGroup>().blocksRaycasts = 详细说明已打开 || 天帝移动适配.启用;
        属性浮窗.GetComponent<CanvasGroup>().interactable = 详细说明已打开 || 天帝移动适配.启用;
        属性说明正文.rectTransform.sizeDelta = new Vector2(宽 - 44, 680);
        float 正文高 = 属性说明正文.preferredHeight;
        属性说明正文.rectTransform.sizeDelta = new Vector2(宽 - 44, 正文高 + 4);
        float 高 = 天帝移动适配.启用 ? Mathf.Min(正文高 + 88, 根.rect.height - 24) : 正文高 + 88;
        属性浮窗.sizeDelta = new Vector2(宽, 高);
        if (触屏说明视口 != null) 属性说明正文.rectTransform.anchoredPosition = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(根, 说明指针, 说明相机, out var 点);
        float x = 点.x - 根.rect.xMin + 18, y = 根.rect.yMax - 点.y + 18;
        if (x + 宽 > 根.rect.width - 12) x = 点.x - 根.rect.xMin - 宽 - 18;
        if (y + 高 > 根.rect.height - 12) y = 根.rect.yMax - 点.y - 高 - 18;
        x = Mathf.Clamp(x, 12, Mathf.Max(12, 根.rect.width - 宽 - 12));
        y = Mathf.Clamp(y, 12, Mathf.Max(12, 根.rect.height - 高 - 12));
        属性浮窗.anchoredPosition = new Vector2(x, -y);
    }
    public void 隐藏属性说明(string 名 = null)
    {
        if (名 != null && (名 != 当前说明属性 || 详细说明已打开)) return;
        当前说明属性 = null; 详细说明已打开 = false;
        if (属性浮窗 != null) 属性浮窗.gameObject.SetActive(false);
    }
    public void 打开详细属性说明(string 名, Vector2 位置, Camera 相机 = null)
    {
        if (!isActiveAndEnabled || 人 == null || 网 == null) return;
        详细说明已打开 = false; 显示属性说明(名, 位置, 相机);
        详细说明已打开 = true; 刷新属性说明();
    }
    public void 触屏属性说明(string 名, Vector2 位置, Camera 相机 = null)
    {
        if (当前说明属性 == 名 && !详细说明已打开) 打开详细属性说明(名, 位置, 相机);
        else { 隐藏属性说明(); 显示属性说明(名, 位置, 相机); }
    }
}

public sealed class 天帝角色属性悬停 : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
{
    public 天帝角色界面 页面;
    public string 属性;
    public void OnPointerEnter(PointerEventData e) { if (!天帝移动适配.启用) 页面?.显示属性说明(属性, e.position, e.enterEventCamera); }
    public void OnPointerMove(PointerEventData e) { if (!天帝移动适配.启用) 页面?.显示属性说明(属性, e.position, e.enterEventCamera); }
    public void OnPointerExit(PointerEventData e) { if (!天帝移动适配.启用) 页面?.隐藏属性说明(属性); }
    public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) { if (天帝移动适配.启用) 页面?.触屏属性说明(属性, e.position, e.pressEventCamera); else 页面?.打开详细属性说明(属性, e.position, e.pressEventCamera); } }
    void OnDisable() => 页面?.隐藏属性说明(属性);
}

// 显示参数直接读取正式配置，不改变属性或战斗计算。
public static class 天帝角色属性公式
{
    static string 数(double v) => v.ToString("0.##");
    static double 取(string k) => 天帝数值.取(k);
    static string 系(string k) => 取(k).ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
    public static string 简介(string 名, 天帝主角属性 人, 天帝道纹 网)
    {
        if(人==null||网==null)return "角色数据尚未准备完成。";
        bool 汇总=名.StartsWith("道纹：");名=名.Replace("道纹：","");
        if(名=="护盾")名="灵气护盾";if(名=="攻速")名="攻击速度";if(名=="移速")名="移动速度";
        string 简单;
        switch(名)
        {
            case "力量": 简单="提高攻击、血量和防御。\n每点力量：攻击 +"+系("player.attack_per_strength_bonus")+"，血量 +"+系("player.hp_per_strength")+"，基础防御 +"+系("player.armor_per_strength_bonus")+"。\n当前力量 "+数(人.力量);break;
            case "速度": 简单="提高攻击、攻速、暴击、移速和闪避。\n当前每秒攻击 "+数(人.攻击速度)+" 次，移动 "+数(人.移动速度)+" 米。\n当前速度 "+数(人.速度);break;
            case "智力": 简单="提高攻击、灵力、护盾、暴击伤害和抗性。\n每点智力：攻击 +"+系("player.attack_per_intelligence_bonus")+"，基础灵力 +"+系("player.mp_per_intelligence")+"，基础护盾 +"+系("player.shield_per_intelligence_bonus")+"。\n当前智力 "+数(人.智力);break;
            case "攻击力":简单="灵力弹的普通伤害，五行伤害另外叠加。\n来自力量、速度和智力的提升。\n当前攻击 "+数(人.攻击力);break;
            case "血量":简单="生命归零时角色死亡。\n当前 "+数(人.当前血量)+" / "+数(人.血量)+"。\n增加上限不会自动补满。";break;
            case "灵力":简单="灵力越高，灵力弹飞得越快。\n当前天然普攻不消耗灵力。\n当前 "+数(人.当前灵力)+" / "+数(人.灵力);break;
            case "灵气护盾":简单="先替血量承受伤害。\n当前 "+数(人.当前灵气护盾)+" / "+数(人.灵气护盾)+"。\n增加上限不会自动补满。";break;
            case "防御":简单="减少普通伤害，不减少五行伤害。\n面对同级、无穿透攻击，减伤 "+数((1-天帝数值.防御留存(人.防御,人.等级))*100)+"%。\n当前防御 "+数(人.防御);break;
            case "移动速度":简单="普通移动时每秒走过的距离。\n当前每秒 "+数(人.移动速度)+" 米。\n不改变攻击速度或弹速。";break;
            case "跑步速度":简单="跑步时的移动速度。\n由普通移速乘 "+系("player.run_factor")+"，最多每秒 "+系("player.run_max")+" 米。\n当前每秒 "+数(人.跑步速度)+" 米。";break;
            case "攻击速度":简单="每秒正常射击的次数。\n当前每秒 "+数(人.攻击速度)+" 次，约每 "+数(1/人.攻击速度)+" 秒一发。\n各条通路共用这个射击节奏。";break;
            case "暴击率":简单="一发攻击触发暴击的机会。\n当前 "+数(人.暴击率*100)+"%，同次释放共享结果。";break;
            case "暴击伤害":简单="暴击时造成的伤害倍数。\n当前 "+数(人.暴击倍率)+" 倍，普通命中为1倍。";break;
            case "抗性":简单="减少五行伤害，不减少普通伤害。\n没有抗性穿透时，减少 "+数(人.抗性*100)+"% 的五行伤害。";break;
            case "闪避率":简单="有机会躲过非范围攻击。\n当前闪避机会 "+数(人.闪避率*100)+"%。\n范围技能无法闪避。";break;
            case "技能急速":简单="用于独立技能的冷却缩减。\n当前自动灵力弹不受它影响。\n当前急速 "+数(人.技能急速);break;
            case "单体 DPS":简单="估算一秒对一个目标造成的伤害。\n假设全部命中、敌人没有减伤。\n当前约 "+数(人.单体期望DPS)+" 伤害/秒。";break;
            case "八目标 DPS":简单="估算一秒对八个目标造成的总伤害。\n假设全部命中、敌人没有减伤。\n当前约 "+数(人.八目标期望DPS)+" 伤害/秒。";break;
            case "纸面战力":简单="综合输出、生存和移动的构筑评分。\n用于比较构筑，不等于实战伤害。\n当前评分 "+数(人.综合战斗力);break;
            default:
                var 攻=普攻参数.读取(网,人);
                if (攻.顺序计划 != null && (名 == "数量" || 名 == "分裂" || 名 == "连锁" || 名 == "弧度" || 名 == "范围"))
                {
                    简单 = "第一通路按顺序执行，首发 " + 攻.数量 + " 颗。\n各出口独立计算攻击属性与功能。\n道纹画布→加成来源可查看每段。"; break;
                }
                switch(名)
                {
                    case "数量":简单="一次射击发出的灵力弹数量。\n第一通路每次 "+攻.数量+" 枚。";break;
                    case "分裂":简单="命中后分出衍生灵力弹。\n第一通路分裂 "+攻.分裂+" 枚，单枚伤害为原伤的 "+数(取("shape.split_factor")*100)+"%。";break;
                    case "连锁":简单="命中后跳转到附近的其他敌人。\n第一通路最多跳转 "+攻.连锁+" 次，每次保留上次的 "+数(取("shape.chain_factor")*100)+"% 伤害。";break;
                    case "弧度":简单="旧存档保留词条，当前不生效。\n子弹发射时瞄准，飞行中不转向。";break;
                    case "范围":简单="灵力弹命中后造成范围溅射。\n第一通路溅射半径 "+数(攻.溅射半径)+" 米。";break;
                    default:简单=名+"属性额外伤害，受敌人抗性减伤。\n只增加所属通路的伤害。";break;
                }
                break;
        }
        if(汇总)
        {
            string 原名=名=="灵气护盾"?"护盾":名=="攻击速度"?"攻速":名=="移动速度"?"移速":名;
            if(System.Enum.TryParse(原名,out 道纹属性 属性))
                return "接通道纹提供的原始加成："+天帝道纹属性.数值文字(属性,网.生效加成[(int)属性])+"。\n"+(天帝道纹属性.分组(属性)==道纹属性分组.形态||天帝道纹属性.分组(属性)==道纹属性分组.元素?"实际效果按各条通路分别计算。\n全网总量不代表单次攻击效果。":"会随等级、其他属性及天赋换算。\n最终数值见角色属性页。");
        }
        return 简单;
    }
    public static string 读取(string 名, 天帝主角属性 人, 天帝道纹 网)
    {
        if (人 == null || 网 == null) return "角色数据尚未准备完成。";
        bool 汇总 = 名.StartsWith("道纹："); 名 = 名.Replace("道纹：", "");
        if (名 == "护盾") 名 = "灵气护盾";
        if (名 == "攻速") 名 = "攻击速度";
        if (名 == "移速") 名 = "移动速度";
        double g = 天帝数值.成长(人.等级), 初始 = 取("player.initial_attribute");
        double 加(道纹属性 a) => 天帝数值.正值(网.生效加成[(int)a]);
        bool 是(天赋种类 a) => 网.天赋?.种类 == a;
        string 标准 = "L=等级，G=1+" + 系("growth.linear") + "×(L−1)+" + 系("growth.quadratic") + "×(L−1)²。当前 L=" + 人.等级 + "，G=" + 数(g) + "。\nΔ力/速/智=当前属性−" + 数(初始) + "。A为接通道纹的原始加成，同一实例全网去重。";
        string 软 = "F(A,K)：A≤K时为A；超过时为 K+(A−K)/(1+(A−K)/K)。只压缩道纹增益，不压缩等级成长。";
        string 作用 = "", 公式 = "", 当前 = "";
        string 速度关系(string 基, string 增, string 拐) => 系(基) + "+" + 系(增) + "×Δ速/(" + 系(拐) + "×G+Δ速)";
        string 智力关系(string 增, string 拐) => 系(增) + "×Δ智/(" + 系(拐) + "+Δ智)";
        switch (名)
        {
            case "力量":
            case "速度":
            case "智力":
                string k = 名 == "力量" ? "strength" : 名 == "速度" ? "speed" : "intelligence";
                var a = 名 == "力量" ? 道纹属性.力量 : 名 == "速度" ? 道纹属性.速度 : 道纹属性.智力;
                double 倍 = 名 == "力量" && 是(天赋种类.万钧) ? 取("talents.strength_multiplier") : 1;
                double 裸 = 初始 + 取("growth." + k) * (g - 1), 原 = 加(a), K = 取("soft." + k + "_g") * g;
                double 最终 = 名 == "力量" ? 人.力量 : 名 == "速度" ? 人.速度 : 人.智力;
                公式 = 名 + "=" + 数(初始) + "+" + 系("growth." + k) + "×(G−1)+F(A" + (倍 != 1 ? "×" + 数(倍) : "") + "," + 系("soft." + k + "_g") + "×G)";
                当前 = "等级基础 " + 数(裸) + " + 道纹有效增益 " + 数(天帝数值.软上限(原 * 倍, K)) + " = " + 数(最终) + "\n原始A=" + 数(原) + "，K=" + 数(K) + (倍 != 1 ? "，万钧倍率=" + 数(倍) : "");
                if (名 == "力量")
                {
                    作用 = "增加攻击、最大血量与防御。";
                    公式 += "\n每增加1点力量：攻击 +" + 系("player.attack_per_strength_bonus") + "，最大血量 +" + 系("player.hp_per_strength") + "，基础防御 +" + 系("player.armor_per_strength_bonus") + "（铁骨再乘天赋倍率）。";
                }
                else if (名 == "速度")
                {
                    作用 = "增加攻击、攻击速度、暴击率、移动速度与闪避率。跑步速度由移动速度推导。";
                    公式 += "\n每增加1点速度：攻击 +" + 系("player.attack_per_speed_bonus") + "。\n攻速基础=" + 系("player.aps_base") + "×(1+" + 系("player.aps_speed_gain") + "×Δ速/(" + 系("player.aps_speed_k_g") + "×G+Δ速))\n暴击率基础=" + 速度关系("player.crit_base", "player.crit_speed_gain", "player.crit_speed_k_g") + "\n移速基础=" + 速度关系("player.move_base", "player.move_speed_gain", "player.move_speed_k_g") + "\n闪避率=" + 系("player.evasion_speed_gain") + "×Δ速/(" + 系("player.evasion_speed_k_g") + "×G+Δ速)\n各项另受对应上限、道纹和天赋影响，悬停对应属性可查看。";
                }
                else
                {
                    作用 = "增加攻击、灵力、护盾、暴击伤害、抗性与技能急速。灵力还影响弹速。";
                    公式 += "\n每增加1点智力：攻击 +" + 系("player.attack_per_intelligence_bonus") + "，基础灵力 +" + 系("player.mp_per_intelligence") + "，基础护盾 +" + 系("player.shield_per_intelligence_bonus") + "。\n暴击倍率基础=" + 系("player.crit_damage_base") + "+" + 智力关系("player.crit_int_gain", "player.crit_int_k") + "\n抗性=" + 智力关系("player.resist_int_gain", "player.resist_int_k") + "\n急速=" + 智力关系("player.haste_int_gain", "player.haste_int_k") + "\n各项另受对应上限、道纹和天赋影响，悬停对应属性可查看。";
                }
                break;
            case "攻击力":
                作用 = "普通灵力弹的普通伤害部分。每条通路的五行伤害单独叠加，再乘天赋倍率；实战还受敌人减伤、等级差和暴击影响。";
                公式 = "攻击=" + 系("player.attack_base") + "+" + 系("player.attack_per_strength_bonus") + "×Δ力+" + 系("player.attack_per_speed_bonus") + "×Δ速+" + 系("player.attack_per_intelligence_bonus") + "×Δ智\n通路释放伤害=(攻击+本路五行有效伤害)×天赋倍率";
                当前 = 数(人.攻击力) + "\nΔ力=" + 数(人.力量 - 初始) + "，Δ速=" + 数(人.速度 - 初始) + "，Δ智=" + 数(人.智力 - 初始); break;
            case "血量":
                作用 = "可承受伤害的生命资源，归零死亡。此处显示当前值/上限；更换道纹或升级不会自动补满。";
                公式 = "最大血量=" + 系("player.hp_base") + "+" + 系("player.hp_level_g") + "×(G−1)+" + 系("player.hp_per_strength") + "×力量+F(A血量," + 系("soft.hp_g") + "×G)";
                当前 = 数(人.当前血量) + " / " + 数(人.血量) + "\nA血量=" + 数(加(道纹属性.血量)) + "，力量=" + 数(人.力量); break;
            case "灵力":
                作用 = "灵力资源上限。当前天然普攻不消耗灵力，灵力上限越高，灵力弹飞行越快。";
                公式 = "最大灵力=(" + 系("player.mp_base") + "+" + 系("player.mp_per_intelligence") + "×智力+F(A灵力," + 系("soft.mp_g") + "×G))×灵海倍率\n弹速=min(" + 系("player.projectile_max") + "," + 系("player.projectile_base") + "+" + 系("player.projectile_mp") + "×最大灵力) 米/秒";
                当前 = 数(人.当前灵力) + " / " + 数(人.灵力) + "\nA灵力=" + 数(加(道纹属性.灵力)) + "，灵海倍率=" + 数(是(天赋种类.灵海) ? 取("talents.mp_multiplier") : 1); break;
            case "灵气护盾":
                作用 = "先于血量承受伤害的护盾资源。显示当前值/上限，增加上限不会自动补满。";
                公式 = "最大护盾=" + 系("player.shield_per_intelligence_bonus") + "×Δ智+F(A护盾," + 系("soft.shield_g") + "×G)+凝光加成\n凝光加成：具有凝光时为 智力×" + 系("talents.shield_per_intelligence") + "，否则为0";
                当前 = 数(人.当前灵气护盾) + " / " + 数(人.灵气护盾) + "\nA护盾=" + 数(加(道纹属性.护盾)) + "，凝光加成=" + 数(是(天赋种类.凝光) ? 人.智力 * 取("talents.shield_per_intelligence") : 0); break;
            case "防御":
                作用 = "减少普通伤害部分，不减少五行伤害。同样防御面对更高等级攻击者时减伤较低。";
                公式 = "防御=(" + 系("player.armor_base") + "+" + 系("player.armor_per_strength_bonus") + "×Δ力+F(A防御," + 系("soft.armor_g") + "×G))×铁骨倍率\n无穿透时：普通伤害留存=max(" + 系("damage.armor_retention_min") + ",K/(K+防御))\nK=" + 系("damage.armor_k_g") + "×G(攻击者等级)，减伤率=1−留存";
                当前 = 数(人.防御) + "\nA防御=" + 数(加(道纹属性.防御)) + "，铁骨倍率=" + 数(是(天赋种类.铁骨) ? 取("talents.armor_multiplier") : 1) + "\n对同级无穿透攻击减伤 " + 数((1 - 天帝数值.防御留存(人.防御, 人.等级)) * 100) + "%"; break;
            case "攻击速度":
                作用 = "每秒正常射击次数。所有参与射击通路共用间隔轮流释放，通路数量不会再次倍增射速。";
                公式 = "攻速=min(" + 系("player.aps_max") + "," + 系("player.aps_base") + "×(1+" + 系("player.aps_speed_gain") + "×Δ速/(" + 系("player.aps_speed_k_g") + "×G+Δ速))×(1+F(A攻速," + 系("soft.attack_speed_percent") + ")/100))\n攻击间隔=1/攻速";
                当前 = 数(人.攻击速度) + " 次/秒，间隔 " + 数(1 / 人.攻击速度) + " 秒\nA攻速=" + 数(加(道纹属性.攻速)) + "（百分比点）"; break;
            case "移动速度":
                作用 = "普通移动的每秒距离，影响走位，不直接改变射速或弹体速度。";
                公式 = "移速=min(" + 系("player.move_max") + ",(" + 速度关系("player.move_base", "player.move_speed_gain", "player.move_speed_k_g") + ")×(1+F(A移速," + 系("soft.move_percent") + ")/100)×逐风倍率)";
                当前 = 数(人.移动速度) + " 米/秒\nA移速=" + 数(加(道纹属性.移速)) + "（百分比点），逐风倍率=" + 数(是(天赋种类.逐风) ? 取("talents.move_multiplier") : 1); break;
            case "跑步速度":
                作用 = "跑步状态每秒移动距离，由最终移动速度推导，具有独立上限。";
                公式 = "跑速=min(" + 系("player.run_max") + "," + 系("player.run_factor") + "×最终移动速度)";
                当前 = "min(" + 系("player.run_max") + "," + 系("player.run_factor") + "×" + 数(人.移动速度) + ")=" + 数(人.跑步速度) + " 米/秒"; break;
            case "暴击率":
                作用 = "一次正常释放发生暴击的概率，同次释放的根弹与衍生弹共享暴击结果。显示值按百分比呈现。";
                公式 = "概率=限制在0至" + 系("player.crit_max") + "之间(" + 速度关系("player.crit_base", "player.crit_speed_gain", "player.crit_speed_k_g") + ")\n期望暴击倍率=1+暴击概率×(暴击倍率−1)";
                当前 = 数(人.暴击率 * 100) + "%"; break;
            case "暴击伤害":
                作用 = "发生暴击时的伤害倍率，例如150%代表1.5倍，普通命中仍为1倍。";
                公式 = "暴击倍率=限制在1至" + 系("player.crit_damage_max") + "之间(" + 系("player.crit_damage_base") + "+" + 智力关系("player.crit_int_gain", "player.crit_int_k") + ")";
                当前 = 数(人.暴击倍率 * 100) + "%（" + 数(人.暴击倍率) + "倍）"; break;
            case "抗性":
                作用 = "减少五行伤害部分，不减少普通伤害。敌人有抗性穿透时会降低有效抗性。";
                公式 = "抗性=限制在0至" + 系("player.resist_max") + "之间(" + 智力关系("player.resist_int_gain", "player.resist_int_k") + ")\n五行伤害留存=1−限制在0至" + 系("player.resist_max") + "之间(抗性−抗性穿透)";
                当前 = 数(人.抗性 * 100) + "%"; break;
            case "技能急速":
                作用 = "由智力产生的技能冷却缩减参数。天然自动灵力弹使用攻击速度，不受此数值加速；当前主角尚无独立冷却技能。";
                公式 = "急速=限制在0至" + 系("player.haste_max") + "之间(" + 智力关系("player.haste_int_gain", "player.haste_int_k") + ")";
                当前 = 数(人.技能急速); break;
            case "闪避率":
                作用 = "闪避可闪避的非范围攻击的概率，由速度推导。范围技能不可闪避；直接伤害接口也不进行闪避判定。";
                公式 = "闪避概率=限制在0至" + 系("player.evasion_max") + "之间(" + 系("player.evasion_speed_gain") + "×Δ速/(" + 系("player.evasion_speed_k_g") + "×G+Δ速))";
                当前 = 数(人.闪避率 * 100) + "%"; break;
            case "单体 DPS":
            case "八目标 DPS":
                作用 = "满命中且未计算敌人减伤的纸面输出，用于比较构筑。各参与通路轮转，取平均，不将每路输出直接相加。";
                公式 = "DPS=参与通路平均(本路释放伤害×攻速×[1+暴击率×(暴击倍率−1)]×命中权重)×余响系数\n目标预算=" + (名 == "单体 DPS" ? "1" : 系("shape.targets_test")) + "；依次分配根弹、连锁、分裂、溅射，权重为1、" + 系("shape.chain_factor") + "的跳数次方、" + 系("shape.split_factor") + "、" + 系("shape.splash_factor") + "。\n余响系数：余响天赋为1+1/" + 系("talents.echo_every") + "，否则为1。";
                当前 = 数(名 == "单体 DPS" ? 人.单体期望DPS : 人.八目标期望DPS) + "，参与射击通路 " + 网.射击通路数 + " 路"; break;
            case "纸面战力":
                作用 = "综合输出、生存和走位的构筑评分，不是实际伤害。相对于一级裸装的基准评分。";
                公式 = "输出=单体DPS^" + 系("power.single_weight") + "×八目标DPS^" + 系("power.crowd_weight") + "\n有效生命=(最大血量+最大护盾)/同级无穿透普通伤害留存\n战力=" + 系("power.base_score") + "×(输出/一级输出)^" + 系("power.output_weight") + "×(有效生命/一级有效生命)^" + 系("power.ehp_weight") + "×(移速/" + 系("player.move_base") + ")^" + 系("power.move_weight") + "\n一级输出=基础攻击×基础攻速×期望暴击倍率；一级有效生命=一级最大血量/一级普通伤害留存。";
                当前 = 数(人.综合战斗力); break;
            default:
                if (System.Enum.TryParse(名, out 道纹属性 属性))
                {
                    if (属性 >= 道纹属性.金 && 属性 <= 道纹属性.土)
                    {
                        作用 = 名 + "属性额外伤害归属各自通路，共用五行抗性减伤。全网汇总不能当作任意一发的额外伤害。";
                        公式 = "本路五行有效伤害=F(本路金木水火土原始伤害之和," + 系("soft.element_g") + "×G)\n本路" + 名 + "分量按其占五行原始总和的比例分配；命中再乘(1−敌人有效抗性)。";
                        当前 = "全网去重原始加成 " + 数(加(属性));
                    }
                    else
                    {
                        var 攻 = 普攻参数.读取(网, 人);
                        作用 = "形态按各自通路计算；此处形态效果展示第一通路。全网去重总览不等于一发的形态。";
                        double A = 网.弹槽加成[0][(int)属性];
                        switch(属性)
                        {
                            case 道纹属性.数量: 公式="每次枚数=min("+系("shape.quantity_max")+",1+限制在0至"+系("shape.quantity_max")+"的本路A+双生加成)\n双生加成="+系("talents.quantity_bonus")+"（无双生为0）";当前=攻.数量+" 枚/次";break;
                            case 道纹属性.分裂: 公式="分裂枚数=限制在0至"+系("shape.split_max")+"的本路A\n每枚衍生弹伤害倍率="+系("shape.split_factor");当前=攻.分裂+" 枚";break;
                            case 道纹属性.连锁: 公式="连锁次数=min("+系("shape.chain_max")+",限制在0至"+系("shape.chain_max")+"的本路A+续雷加成)\n续雷加成="+系("talents.chain_bonus")+"（无续雷为0）；第n次倍率="+系("shape.chain_factor")+"^n";当前=攻.连锁+" 次";break;
                            case 道纹属性.弧度: 公式="转向速度=限制在0至"+系("shape.arc_max")+"的本路A，单位度/秒";当前=数(攻.转向角度)+" 度/秒";break;
                            case 道纹属性.范围: 公式="溅射半径=min("+系("shape.radius_max")+",本路A×"+系("shape.radius_per_point")+"×广域倍率)\n广域倍率="+系("talents.radius_multiplier")+"（无广域为1）；溅射倍率="+系("shape.splash_factor");当前=数(攻.溅射半径)+" 米";break;
                            default: return "此属性仅供旧存档兼容，不进入现行词条池。";
                        }
                        当前 += "，第一通路A=" + 数(A);
                    }
                }
                else return "暂无此属性的说明。";
                break;
        }
        string 前缀 = "";
        if (汇总)
        {
            string 原名 = 名 == "灵气护盾" ? "护盾" : 名 == "攻击速度" ? "攻速" : 名 == "移动速度" ? "移速" : 名;
            if (System.Enum.TryParse(原名, out 道纹属性 属性)) 前缀 = "道纹全网原始加成 " + 数(加(属性)) + "；这里只统计接通的实例，未接通不生效。\n\n";
        }
        bool 用软 = 公式.Contains("F(");
        return 前缀 + "<b>当前结果</b>\n<b>" + 当前 + "</b>\n\n作用\n" + 作用 + "\n\n计算公式\n" + 公式 + "\n\n" + 标准 + (用软 ? "\n" + 软 : "");
    }
}
