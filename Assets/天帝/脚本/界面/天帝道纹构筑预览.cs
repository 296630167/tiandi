using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹界面
{
    天帝道纹连接诊断 连接诊断;
    天帝道纹攻击演示 攻击演示;
    Text 单弹字, 射击字, 形态字, 对比字, 连接字;
    Image 变化底;
    天帝主角属性 真实主角;
    天帝道纹 沙盘;
    道纹实例 上次预览纹;
    Vector2Int? 上次预览格;
    bool 上次预览旋转, 上次预览收回, 已有预览缓存;
    int? 上次预览接口;
    int? 上次预览入口;
    public int 当前预览通路 { get; private set; }
    readonly Button[] 通路按钮 = new Button[6];
    RectTransform 来源层;
    RectTransform 来源卡内容;
    Text 来源正文;
    public 普攻参数 当前演示参数 { get; private set; }
    string 山水完整预览说明, 山水完整诊断说明;
    public string 构筑预览说明 => 山水构筑 ? 山水完整预览说明 ?? "" : 对比字 != null ? 对比字.text : "";
    public string 连接诊断说明 => 山水构筑 ? 山水完整诊断说明 ?? "" : 连接字 != null ? 连接字.text : "";
    static readonly Color 亮青 = 天帝道纹美术.强调;

    void 建构筑预览()
    {
        真实主角 = GetComponentInParent<天帝游戏>()?.主角属性;
        var 框 = 底(根, "实时构筑预览", 1140, 78, 432, 564, Color.white);
        var 预览标题=文字(框, "战斗变化", 22, 12, 270, 40, 24, TextAnchor.MiddleLeft);预览标题.color = 天帝道纹美术.纸墨;
        按钮(框, "加成来源", 302, 16, 108, 34, 显示加成来源);
        for (int i = 0; i < 6; i++)
        {
            int d = (6 - i) % 6;
            var 键 = 按钮(框, "通路" + (i + 1), 22 + i % 3 * 132, 62 + i / 3 * 40, 124, 34, () => 选择预览通路(d));
            键.GetComponentInChildren<Text>().fontSize = 16; 通路按钮[d] = 键;
        }
        单弹字 = 文字(框, "", 22, 151, 388, 52, 31, TextAnchor.MiddleLeft); 单弹字.color = 天帝道纹美术.纸墨;
        射击字 = 文字(框, "", 22, 208, 388, 26, 15, TextAnchor.MiddleLeft); 射击字.color = 天帝道纹美术.纸次墨;
        形态字 = 文字(框, "", 22, 248, 388, 55, 18, TextAnchor.UpperLeft); 形态字.color = 天帝道纹美术.纸墨;
        var 演示区 = 底(框, "攻击形态演示", 22, 316, 388, 92, new Color(.018f, .036f, .045f));
        演示区.gameObject.AddComponent<RectMask2D>();
        var 画 = 区块(演示区, "形态演示绘制", 0, 0, 388, 92);
        攻击演示 = 画.gameObject.AddComponent<天帝道纹攻击演示>(); 攻击演示.raycastTarget = false;
        文字(框, "顺序链路：攻击分段 · 生存/射速全网去重", 22, 413, 388, 20, 13, TextAnchor.MiddleLeft).color = 天帝道纹美术.纸次墨;
        变化底 = 区块(框, "数值变化提示", 16, 439, 400, 74).gameObject.AddComponent<Image>(); 天帝道纹美术.应用(变化底, "数值增加提示"); 变化底.enabled = false;
        对比字 = 文字(框, "", 22, 445, 388, 66, 15, TextAnchor.UpperLeft); 对比字.color = 天帝道纹美术.纸墨;
        连接字 = 文字(框, "", 22, 511, 388, 48, 13, TextAnchor.UpperLeft); 连接字.color = 天帝道纹美术.纸次墨;
        对比字.verticalOverflow = 连接字.verticalOverflow = VerticalWrapMode.Truncate;
    }
    void 重建构筑预览()
    {
        if (攻击演示 == null || 数据 == null) return;
        连接诊断 = new 天帝道纹连接诊断(数据); 已有预览缓存 = false; 沙盘 = null;
        更新通路按钮();
        更新构筑预览(null, null, false, false);
    }
    public bool 选择预览通路(int 通路)
    {
        if (!数据.通路开放(通路) || 拖动中 || 平移中 || 筛选已打开) return false;
        当前预览通路 = 通路; 重建构筑预览(); return true;
    }
    void 更新通路按钮()
    {
        画布.强调通路 = 当前预览通路;
        for (int d = 0; d < 6; d++)
        {
            var 键 = 通路按钮[d]; if (键 == null) continue;
            bool 开 = 数据.通路开放(d); 键.interactable = 开;
            天帝界面美术.选项(键.GetComponent<Image>(), d == 当前预览通路, !开);
            var 字 = 键.GetComponentInChildren<Text>(); 字.color = 开 ? 天帝道纹美术.纸墨 : new Color(.35f,.42f,.39f);
            字.text = (((6 - d) % 6) + 1) + " · " + 天帝道纹.方向名[d] + (开 ? "" : " " + 天帝道纹.通路解封等级(d) + "级");
            if(山水构筑){天帝图三四山水素材.按钮(键,d==当前预览通路);字.fontSize=14;字.resizeTextMaxSize=15;}
        }
    }
    普攻参数 读取演示参数(天帝道纹 网)
    {
        // 与实战使用同一个参数读取入口，包括天赋和当前生命的影响。
        if (网 == 数据 && 真实主角 != null) return 普攻参数.读取通路(网, 真实主角, 当前预览通路);
        using (var 人 = new 天帝主角属性(真实主角?.导出配置() ?? 天帝普攻.主角配置(), 网))
        {
            if (真实主角 != null) 人.设置当前资源(真实主角.当前血量, 真实主角.当前灵力, 真实主角.当前灵气护盾);
            return 普攻参数.读取通路(网, 人, 当前预览通路);
        }
    }
    static string 变化(float 前, float 后, string 格式 = "0.##")
    {
        float 差 = 后 - 前;
        return Mathf.Abs(差) < .0001f ? "" : " <color=" + (差 > 0 ? "#28654F>+" : "#934839>") + 差.ToString(格式) + "</color>";
    }
    void 更新构筑预览(道纹实例 纹, Vector2Int? 放置格, bool 旋转, bool 收回)
        => 更新待放构筑预览(纹, 放置格, 旋转, 收回, null);
    void 更新待放构筑预览(道纹实例 纹, Vector2Int? 放置格, bool 旋转, bool 收回, int? 待放接口)
    {
        if (攻击演示 == null || 连接诊断 == null) return;
        int? 接口覆盖 = 待放接口 ?? (拖纹 == 纹 ? 拖动接口 : null);
        int? 入口覆盖 = 拖纹 == 纹 ? 拖影纹?.入口方向 : null;
        if (已有预览缓存 && 上次预览纹 == 纹 && 上次预览格 == 放置格 && 上次预览旋转 == 旋转 && 上次预览收回 == 收回 && 上次预览接口 == 接口覆盖 && 上次预览入口 == 入口覆盖) return;
        上次预览纹 = 纹; 上次预览格 = 放置格; 上次预览旋转 = 旋转; 上次预览收回 = 收回; 已有预览缓存 = true;
        上次预览接口 = 接口覆盖;
        上次预览入口 = 入口覆盖;
        沙盘 = (旋转 || 收回 || 放置格.HasValue) ? 连接诊断.预览(纹, 放置格, 旋转, 接口覆盖, 入口覆盖) : null;
        var 当前 = 读取演示参数(数据); var 后 = 沙盘 == null ? 当前 : 读取演示参数(沙盘);
        当前演示参数 = 后; 攻击演示.设置参数(后,沙盘??数据);
        string 身份 = 沙盘 == null ? "当前单弹" : 旋转 ? "旋转后预览" : 收回 ? "卸下后预览" : "放置后预览";
        单弹字.text = 后.顺序计划 != null ? "<size=16>顺序链路 · 首发 </size><size=32>" + 后.顺序计划.根弹数 + "</size><size=16> 颗</size>" : "<size=16>" + 身份 + "  </size><size=32>" + 后.伤害.ToString("0.##") + "</size>" + (沙盘 != null ? 变化(当前.伤害, 后.伤害) : "");
        变化底.enabled = 沙盘 != null;
        if (沙盘 != null)
        {
            bool 减 = 后.伤害 < 当前.伤害 || 后.数量 < 当前.数量 || 后.分裂 < 当前.分裂 || 后.连锁 < 当前.连锁;
            天帝道纹美术.应用(变化底, 减 ? "数值减少提示" : "数值增加提示"); 变化底.color = new Color(1,1,1,.72f);
            if(山水构筑){变化底.sprite=null;变化底.color=new Color(.40f,.60f,.48f,.12f);}
        }
        射击字.text = 后.顺序计划 != null ? "每颗属性独立 · 加成来源查看各段 · " + 后.间隔.ToString("0.00") + "秒/次" : "普通 " + 后.普通伤害.ToString("0.##") + " ＋ 五行 " + 后.五行额外伤害.ToString("0.##") + "  ·  " + 后.间隔.ToString("0.00") + "秒 / 次";
        形态字.text = 后.顺序计划 != null ? 后.顺序计划.摘要 : "数量 " + 后.数量 + "   分裂 " + 后.分裂 + "   连锁 " + 后.连锁 + "\n直线飞行   范围 " + 后.溅射半径.ToString("0.#") + "米";
        画布.高亮路径.Clear(); 画布.预览亮起.Clear(); 画布.预览暗掉.Clear();
        画布.高亮路径.AddRange(连接诊断.路径(纹, 当前预览通路));
        int 亮起 = 0, 暗掉 = 0;
        if (沙盘 != null)
        {
            foreach (var 原 in 数据.道纹)
            {
                var 新 = 沙盘.道纹.Find(x => x.编号 == 原.编号);
                if (新.生效 && !原.生效) { 亮起++; if (新.格子.HasValue) 画布.预览亮起.Add(新.格子.Value); }
                if (!新.生效 && 原.生效) { 暗掉++; if (原.格子.HasValue) 画布.预览暗掉.Add(原.格子.Value); }
            }
            var 新纹 = 沙盘.道纹.Find(x => x.编号 == 纹.编号);
            var 预览诊断 = new 天帝道纹连接诊断(沙盘);
            连接字.text = 归属说明(沙盘, 新纹, 预览诊断);
            if (放置格.HasValue)
            {
                画布.高亮路径.Clear(); 画布.高亮路径.AddRange(预览诊断.路径(新纹, 当前预览通路));
            }
            var 变 = new StringBuilder(旋转 ? "旋转后（尚未执行）" : 收回 ? "收回背包后（尚未执行）" : "放置后（尚未执行）");
            变.Append("\n将亮起 ").Append(亮起).Append(" 枚 · 将暗掉 ").Append(暗掉).Append(" 枚");
            int 项 = 0;
            for (int i = 0; i < 数据.生效加成.Length; i++)
            {
                bool 本路 = 天帝道纹属性.分组((道纹属性)i) == 道纹属性分组.形态 || 天帝道纹属性.分组((道纹属性)i) == 道纹属性分组.元素;
                double 差 = 本路 ? 沙盘.弹槽加成[当前预览通路][i] - 数据.弹槽加成[当前预览通路][i] : 沙盘.生效加成[i] - 数据.生效加成[i]; if (差 == 0) continue;
                if (项 == 0) 变.Append("\n");
                if (项 < 4) 变.Append((道纹属性)i).Append(差 > 0 ? " +" : " ").Append(差.ToString("0.##")).Append("  "); 项++;
            }
            if (项 == 0) 变.Append("\n有效加成不变"); else if (项 > 4) 变.Append("等").Append(项).Append("项");
            对比字.text = 变.ToString();
        }
        else
        {
            连接字.text = 归属说明(数据, 纹, 连接诊断);
            var 字 = new StringBuilder(天帝道纹.通路名称(当前预览通路)).Append(" · 接通 ").Append(数据.弹槽生效数[当前预览通路]).Append(" 枚");
            字.Append("\n原始词条：力量+").Append(数据.生效加成[(int)道纹属性.力量].ToString("0.##")).Append("  智力+").Append(数据.生效加成[(int)道纹属性.智力].ToString("0.##")).Append("  速度+").Append(数据.生效加成[(int)道纹属性.速度].ToString("0.##"));
            字.Append(数据.通路开放(当前预览通路) ? "\n就绪通路随机自动施法 · 余响继承通路与方向" : "\n接口尚未解封"); 对比字.text = 字.ToString();
        }
        if(山水构筑)
        {
            // 计算与完整诊断保持原文；主栏只展示当前决策所需摘要，规则与各段来源在详情入口中查看。
            山水完整预览说明=对比字.text;山水完整诊断说明=连接字.text;
            单弹字.text=后.顺序计划!=null?"<size=17>"+(沙盘==null?"首发数量":身份)+"  </size><size=30>"+后.顺序计划.根弹数+"</size><size=17> 颗</size>":"<size=17>"+身份+"  </size><size=30>"+后.伤害.ToString("0.##")+"</size>"+(沙盘!=null?变化(当前.伤害,后.伤害):"");
            射击字.text="释放间隔  "+后.间隔.ToString("0.00")+" 秒 / 次";
            形态字.text=后.顺序计划!=null?后.顺序计划.摘要.Split('\n')[0]:"数量 "+后.数量+"   分裂 "+后.分裂+"   连锁 "+后.连锁+"\n范围 "+后.溅射半径.ToString("0.#")+" 米";
            if(后.顺序计划!=null){int 分隔=形态字.text.IndexOf('·');if(分隔>=0)形态字.text=形态字.text.Substring(分隔+1).Trim();}
            对比字.text=沙盘==null?"本通路已接通 "+数据.弹槽生效数[当前预览通路]+" 枚\n"+(数据.通路开放(当前预览通路)?"就绪通路随机自动施法":"接口尚未解封"):(旋转?"旋转后（尚未执行）":收回?"收回后（尚未执行）":"放置后（尚未执行）")+"\n将亮起 "+亮起+" 枚 · 将暗掉 "+暗掉+" 枚";
            连接字.text=纹==null?"拖动道纹，预览放置后的变化":山水完整诊断说明.Split('\n')[0];
            if(纹!=null&&连接字.text.Length>18){int 分隔=连接字.text.IndexOf('·');连接字.text=(分隔<0?"道纹状态":连接字.text.Substring(0,分隔).Trim())+" · 悬停查看详情";}
        }
        画布.SetVerticesDirty();
    }
    string 归属说明(天帝道纹 网, 道纹实例 纹, 天帝道纹连接诊断 诊断)
    {
        if (纹 == null || !纹.生效 || 纹.是源纹) return 诊断.说明(纹);
        if (纹.是特性道纹)
        {
            string 状态 = 纹.特性状态 ?? "已接通 · 请查看条件";
            int 换行 = 状态.IndexOf('\n');
            return (换行 < 0 ? 状态 : 状态.Substring(0, 换行)) + "\n点选道纹看实际条件；加成来源可看完整链路。";
        }
        int 归属 = 网.通路掩码(纹); var 路 = new List<string>();
        for (int i = 0; i < 6; i++) { int d = (6 - i) % 6; if ((归属 & (1 << d)) != 0) 路.Add((i + 1).ToString()); }
        return "已接通 · 通路 " + string.Join(" / ", 路) + (路.Count > 1 ? " 共享（白点）" : "") + ((归属 & (1 << 当前预览通路)) == 0 ? "\n未归属当前所选通路。" : "");
    }
    void 清理连接预览()
    {
        已有预览缓存 = false;
        if (画布 != null) { 画布.高亮路径.Clear(); 画布.预览亮起.Clear(); 画布.预览暗掉.Clear(); 画布.SetVerticesDirty(); }
        if (攻击演示 != null && 连接诊断 != null) 更新构筑预览(null, null, false, false);
    }
    void 显示加成来源()
    {
        if (拖动中 || 平移中) return;
        指针离开();
        if (来源层 == null)
        {
            来源层 = 区块(根, "加成来源层", 0, 0, 1600, 900);
            var 遮 = 底(来源层, "来源遮罩", 0, 0, 1600, 900, new Color(0, 0, 0, .65f)); 遮.GetComponent<Image>().raycastTarget = true;
            var 框 = 底(来源层, "来源面板", 350, 80, 900, 740, new Color(.035f, .065f, .078f));
            天帝辅助页山水.纸(框.GetComponent<Image>(),"弹窗纸框");
            var 标题=文字(框, "有效加成来源", 190, 76, 520, 70, 32, TextAnchor.MiddleCenter);标题.fontStyle=FontStyle.Normal;
            var 关闭=按钮(框, "关闭", 740, 82, 130, 48, 关闭筛选);天帝辅助页山水.按钮(关闭);
            文字(框, "顺序链路：各弹攻击独立 · 生存/射速全网去重 · 白点表示共享节点", 38, 142, 822, 46, 17, TextAnchor.MiddleLeft);
            var 视 = 底(框, "来源滚动视口", 25, 194, 850, 510, new Color(.025f, .046f, .055f)); 视.GetComponent<Image>().raycastTarget = true; 视.gameObject.AddComponent<RectMask2D>();
            var 滚 = 视.gameObject.AddComponent<ScrollRect>(); 滚.horizontal = false; 滚.vertical = true; 滚.movementType = ScrollRect.MovementType.Clamped; 滚.viewport = 视;
            来源正文 = 文字(视, "", 14, 10, 796, 570, 20, TextAnchor.UpperLeft); 滚.content = 来源正文.rectTransform;
            来源正文.fontStyle=FontStyle.Normal;来源正文.color=天帝剪纸界面皮肤.墨;来源正文.lineSpacing=1.16f;
            来源正文.rectTransform.anchorMin = 来源正文.rectTransform.anchorMax = 来源正文.rectTransform.pivot = new Vector2(0, 1);
            if(!天帝移动适配.启用)
            {
                来源卡内容=区块(视,"来源分组卡片列表",0,0,802,510);天帝响应布局.动态(来源卡内容);
                来源正文.rectTransform.SetParent(来源卡内容,false);滚.content=来源卡内容;
            }
            var 轨 = 底(框, "来源滚动轨", 856, 198, 14, 502, Color.white); 天帝道纹美术.应用(轨.GetComponent<Image>(), "滚动轨");
            var 柄 = 底(轨, "滚动滑块", 0, 0, 14, 80, Color.white); 天帝道纹美术.应用(柄.GetComponent<Image>(), "滚动滑块");
            var 条 = 轨.gameObject.AddComponent<Scrollbar>(); 条.handleRect = 柄; 条.targetGraphic = 柄.GetComponent<Image>(); 条.direction = Scrollbar.Direction.BottomToTop;
            滚.verticalScrollbar = 条; 滚.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            if(山水构筑)
            {
                视.GetComponent<Image>().sprite=null;视.GetComponent<Image>().color=new Color(1,1,1,.18f);
                天帝双端页面布局.固定(轨,856,198,10,502);
                var 滑区=new GameObject("来源滑动区域",typeof(RectTransform)).GetComponent<RectTransform>();滑区.SetParent(轨,false);
                滑区.anchorMin=Vector2.zero;滑区.anchorMax=Vector2.one;滑区.offsetMin=滑区.offsetMax=Vector2.zero;
                柄.SetParent(滑区,false);天帝响应布局.动态(柄);柄.anchorMin=Vector2.zero;柄.anchorMax=Vector2.one;柄.offsetMin=柄.offsetMax=Vector2.zero;
                天帝响应布局.动态(来源正文.rectTransform);
            }
            if (天帝移动适配.启用)
                天帝双端页面布局.移动页(框, 面板 =>
                {
                    天帝双端页面布局.页头(面板, "关闭");
                    float 宽 = 面板.rect.width;
                    foreach (Transform 子 in 面板)
                        if (子.GetComponent<Text>() is Text 文 && 文.text != "有效加成来源")
                        {
                            文.fontSize = 14; 天帝双端页面布局.固定(文.rectTransform, 12, 66, 宽 - 24, 60);
                            天帝双端页面布局.固定(文.rectTransform, 12, 66, 宽 - 24, Mathf.Ceil(文.preferredHeight) + 4);
                        }
                    天帝双端页面布局.固定(视, 12, 118, 宽 - 30, 面板.rect.height - 130);
                    来源正文.fontSize = 16;
                    天帝双端页面布局.固定(来源正文.rectTransform, 8, 4, 宽 - 54, Mathf.Max(570, 来源正文.preferredHeight + 24));
                    天帝双端页面布局.固定(轨, 宽 - 18, 122, 10, 面板.rect.height - 138);
                });
        }
        var 文 = new StringBuilder();
        var 网 = 沙盘 ?? 数据;
        var 参数 = 读取演示参数(网);
        if (参数.顺序计划 != null) 文.Append(参数.顺序计划.详情()).Append("\n\n以下为全网属性总览，攻击以各投掷物分段数值为准。\n");
        foreach (var 属性 in 天帝道纹属性.当前属性)
        {
            bool 本路 = 天帝道纹属性.分组(属性) == 道纹属性分组.形态 || 天帝道纹属性.分组(属性) == 道纹属性分组.元素;
            double 值 = 本路 ? 网.弹槽加成[当前预览通路][(int)属性] : 网.生效加成[(int)属性]; if (值 <= 0) continue;
            文.Append("<b><color=#215463>").Append(本路 ? 天帝道纹.通路名称(当前预览通路) : "全局").Append(" · ").Append(天帝道纹属性.词条名称(属性)).Append("  ").Append(天帝道纹属性.数值文字(属性, 值)).Append("</color></b>\n");
            foreach (var 项 in 网.已放置)
            {
                var 纹 = 项.Value; if (纹.是顺序功能 || !纹.生效 || 本路 && !网.弹槽道纹[当前预览通路].Contains(纹.编号)) continue;
                double 数 = 0; foreach (var 词 in 网.读取有效词条(纹)) if (词.属性 == 属性) 数 += Math.Max(0, 词.实际数值);
                if (数 == 0) continue;
                文.Append("  ").Append(纹.名称).Append(" #").Append(纹.编号).Append("  +").Append(数.ToString("0.##")).Append("  · 第").Append(天帝道纹.格权重(项.Key)).Append("圈\n");
            }
            文.Append("\n");
        }
        foreach(var t in 网.特性视图.特性)
            文.Append(t.道纹.名称).Append(" #").Append(t.道纹.编号).Append("\n").Append(t.道纹.特性状态).Append("\n\n");
        来源正文.text = 文.Length == 0 ? "暂无有效属性加成。\n连通道纹后，这里会列出每项加成来自哪些道纹。" : 文.ToString();
        var 正文区 = 来源正文.rectTransform;
        if(来源卡内容!=null)
        {
            建来源分组卡(网);
            var 滚=来源卡内容.parent.GetComponent<ScrollRect>();
            滚.verticalNormalizedPosition=1;
            滚.verticalScrollbar.gameObject.SetActive(来源卡内容.rect.height>((RectTransform)来源卡内容.parent).rect.height);
        }
        else if(山水构筑)
        {
            float 视高=((RectTransform)正文区.parent).rect.height;
            天帝双端页面布局.固定(正文区,14,12,796,Mathf.Max(视高-24,来源正文.preferredHeight+24));
            var 滚=正文区.parent.GetComponent<ScrollRect>();滚.verticalScrollbar.gameObject.SetActive(来源正文.preferredHeight+24>视高);
        }
        else if (正文区.GetComponent<天帝比例矩形>()?.待提交 == false)
            正文区.sizeDelta = new Vector2(0, Mathf.Max(570, 来源正文.preferredHeight + 24));
        else 正文区.sizeDelta = new Vector2(796, Mathf.Max(570, 来源正文.preferredHeight + 24));
        来源层.SetAsLastSibling(); 来源层.gameObject.SetActive(true);
    }
    void 建来源分组卡(天帝道纹 网)
    {
        for(int i=来源卡内容.childCount-1;i>=0;i--)
        {
            var 子=来源卡内容.GetChild(i);
            if(子!=来源正文.transform)
            {
                子.gameObject.SetActive(false);
                if(Application.isPlaying)Destroy(子.gameObject);else DestroyImmediate(子.gameObject);
            }
        }
        float y=12;
        Text 正文字(RectTransform 父,string 文,float x,float 顶,float w,float h,int 字号)
        {
            var t=文字(父,文,x,顶,w,h,字号,TextAnchor.MiddleLeft);
            天帝双端页面布局.固定(t.rectTransform,x,顶,w,h);t.font=字体;t.fontStyle=FontStyle.Normal;
            t.resizeTextForBestFit=false;t.color=天帝剪纸界面皮肤.墨;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        foreach(var 属性 in 天帝道纹属性.当前属性)
        {
            bool 本路=天帝道纹属性.分组(属性)==道纹属性分组.形态||天帝道纹属性.分组(属性)==道纹属性分组.元素;
            double 值=本路?网.弹槽加成[当前预览通路][(int)属性]:网.生效加成[(int)属性];if(值<=0)continue;
            var 标签=底(来源卡内容,"来源分组-"+属性,12,y,774,46,Color.white);
            天帝双端页面布局.固定(标签,12,y,774,46);天帝辅助页山水.纸(标签.GetComponent<Image>(),"墨绿按钮","剪纸界面/墨绿按钮",false);
            var 标签文=正文字(标签,(本路?天帝道纹.通路名称(当前预览通路):"全局")+" · "+天帝道纹属性.词条名称(属性)+" "+天帝道纹属性.数值文字(属性,值),62,0,650,46,21);标签文.color=new Color(.99f,.97f,.88f);
            y+=56;
            foreach(var 项 in 网.已放置)
            {
                var 纹=项.Value;if(纹.是顺序功能||!纹.生效||本路&&!网.弹槽道纹[当前预览通路].Contains(纹.编号))continue;
                double 数=0;foreach(var 词 in 网.读取有效词条(纹))if(词.属性==属性)数+=Math.Max(0,词.实际数值);if(数<=0)continue;
                var 卡=底(来源卡内容,"来源道纹卡-"+纹.编号+"-"+属性,12,y,774,128,Color.white);
                天帝双端页面布局.固定(卡,12,y,774,128);天帝辅助页山水.轻纸(卡.GetComponent<Image>());
                var 图区=区块(卡,"来源道纹图标",20,26,70,70);天帝双端页面布局.固定(图区,20,26,70,70);
                var 图=图区.gameObject.AddComponent<天帝道纹绘图>();图.单纹模式=true;图.单纹半径=30;图.单纹=纹;图.数据=网;图.raycastTarget=false;图.构筑美术=true;
                var 短字=正文字(图区,天帝道纹美术.单字(纹),0,0,70,70,24);短字.alignment=TextAnchor.MiddleCenter;天帝道纹单字.绑定(短字,图区);
                正文字(卡,纹.名称+" #"+纹.编号,112,12,636,38,22);
                int 口=0;for(int d=0;d<6;d++)if(纹.有接口(d))口++;
                正文字(卡,纹.品阶+" · 接口 "+口+"/6 · 第"+天帝道纹.格权重(项.Key)+"圈",112,52,636,32,18);
                正文字(卡,"来源词条："+天帝道纹属性.词条名称(属性)+" "+天帝道纹属性.数值文字(属性,数),112,88,636,32,19);
                y+=140;
            }
            y+=12;
        }
        if(y>12)
        {
            正文字(来源卡内容,"完整链路与机制说明",24,y,750,42,21);y+=54;
        }
        天帝双端页面布局.固定(来源正文.rectTransform,24,y,750,600);来源正文.font=字体;来源正文.fontStyle=FontStyle.Normal;
        float 高=Mathf.Max(100,来源正文.preferredHeight+24);天帝双端页面布局.固定(来源正文.rectTransform,24,y,750,高);
        天帝双端页面布局.固定(来源卡内容,0,0,802,Mathf.Max(510,y+高+16));
    }
}

// 仅在画布演示射击形态，不进入战场、不获得经验或物品；所有数量和轨迹参数取自实战。
[RequireComponent(typeof(CanvasRenderer))]
public sealed class 天帝道纹攻击演示 : MaskableGraphic
{
    sealed class 弹
    {
        public Vector2 点, 向; public float 距; public int 链, 目标; public bool 子, 已衍生, 自动连锁;
        public int 剩余穿透 = -1;
        public HashSet<int> 命中过 = new HashSet<int>();
        public 道纹执行段 执行段; public 普攻参数 参数; public 演示释放 释放;
    }
    sealed class 演示释放 { public int 数量; }
    sealed class 环 { public Vector2 点; public float 秒, 半径; }
    sealed class 链线 { public Vector2 起, 终; public float 秒 = .18f; }
    readonly List<弹> 弹体 = new List<弹>(96), 待加 = new List<弹>(24);
    readonly List<环> 命中圈 = new List<环>(32);
    readonly List<链线> 连线 = new List<链线>(32);
    readonly Vector2[] 靶 = { new Vector2(6, 0), new Vector2(8.5f, 2), new Vector2(10.8f, -1.2f), new Vector2(12.6f, 2.2f), new Vector2(13.4f, -2.3f) };
    readonly float[] 闪白 = new float[5];
    普攻参数 参数;
    天帝战斗系统 扩展演示;
    天帝主角属性 演示人;
    Vector2 演示原点;
    float 冷却;
    public int 演示命中次数 { get; private set; }
    public int 演示连锁次数 { get; private set; }
    public int 演示分裂次数 { get; private set; }
    public void 设置参数(普攻参数 新,天帝道纹 模型=null)
    {
        参数 = 新; 弹体.Clear(); 待加.Clear(); 命中圈.Clear(); 连线.Clear(); Array.Clear(闪白, 0, 闪白.Length); 冷却 = 0;
        演示命中次数 = 演示连锁次数 = 演示分裂次数 = 0; SetVerticesDirty();
        演示人?.Dispose(); 演示人 = null; 扩展演示 = null;
        if (新.顺序计划 != null && 新.顺序计划.各段.Exists(s => s.功能 > 道纹功能.穿透) || 模型!=null && 模型.特性视图.特性.Count>0)
        {
            var 网 = 模型!=null?天帝道纹.读取存档(模型.导出存档()):new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
            演示人 = new 天帝主角属性(天帝普攻.主角配置(), 网);
            var 地 = new 天帝战斗地图(42, true); 演示原点 = 地.出生位置;
            扩展演示 = new 天帝战斗系统(地, 网, 演示人, 战斗难度.普通); 扩展演示.设置演示靶(靶);
            扩展演示.伤害反馈 += (点, 伤, 玩家) => { 演示命中次数++; for (int i = 0; i < 靶.Length; i++) if (Vector2.Distance(点 - 演示原点, 靶[i]) < 1) 闪白[i] = .12f; };
        }
    }
    protected override void OnDestroy() { 演示人?.Dispose(); base.OnDestroy(); }
    void Update() => 推进演示(Time.unscaledDeltaTime);
    public void 推进演示(float 时长)
    {
        if (参数 == null || !参数.已激活 || 时长 <= 0 || float.IsNaN(时长) || float.IsInfinity(时长)) return;
        float 秒 = Mathf.Min(时长, .05f); 冷却 -= 秒;
        if (扩展演示 != null)
        {
            if (冷却 <= 0) { 冷却 = 参数.间隔; 扩展演示.发射演示(参数); }
            扩展演示.推进演示弹体(秒); 扩展演示.推进特性演示(秒); 演示连锁次数 = 扩展演示.顺序连锁次数; 演示分裂次数 = 扩展演示.顺序分裂次数;
            for (int i = 0; i < 闪白.Length; i++) 闪白[i] = Mathf.Max(0, 闪白[i] - 秒);
            SetVerticesDirty(); return;
        }
        if (冷却 <= 0)
        {
            冷却 = 参数.间隔;
            var 本次命中 = new HashSet<int>(); var 已分配 = new HashSet<int>();
            if (参数.顺序计划 != null)
            {
                var 释放 = new 演示释放();
                发射段(参数.顺序计划.起点, Vector2.zero, 靶[0].normalized, 0, 本次命中, 释放, 0, 弹体, false, 参数.顺序计划.起始链路数);
            }
            else for (int i = 0; i < 参数.数量 && 弹体.Count < 96; i++)
            {
                int 目标 = 最近靶(Vector2.zero, 已分配); if (目标 < 0) 目标 = 0; 已分配.Add(目标);
                弹体.Add(new 弹 { 点 = Vector2.zero, 向 = 靶[目标].normalized, 目标 = 目标, 距 = 普攻参数.飞行距离, 链 = 参数.连锁, 命中过 = 本次命中 });
            }
        }
        for (int i = 0; i < 闪白.Length; i++) 闪白[i] = Mathf.Max(0, 闪白[i] - 秒);
        for (int i = 命中圈.Count - 1; i >= 0; i--) { 命中圈[i].秒 -= 秒; if (命中圈[i].秒 <= 0) 命中圈.RemoveAt(i); }
        for (int i = 连线.Count - 1; i >= 0; i--) { 连线[i].秒 -= 秒; if (连线[i].秒 <= 0) 连线.RemoveAt(i); }
        for (int i = 弹体.Count - 1; i >= 0; i--) if (!推进弹(弹体[i], 秒)) 弹体.RemoveAt(i);
        bool 保留(弹 矢) { while (矢.自动连锁) if (!推进弹(矢, 0)) return false; return true; }
        for (int i = 弹体.Count - 1; i >= 0; i--) if (弹体[i].自动连锁 && !保留(弹体[i])) 弹体.RemoveAt(i);
        for (int i = 0; i < 待加.Count;) if (待加[i].自动连锁 && !保留(待加[i])) 待加.RemoveAt(i); else i++;
        foreach (var 子 in 待加) if (弹体.Count < 96) 弹体.Add(子); 待加.Clear(); SetVerticesDirty();
    }
    bool 推进弹(弹 矢, float 秒)
    {
        var 参数 = 矢.参数 ?? this.参数;
        bool 穿透阶段 = 矢.执行段?.功能 == 道纹功能.穿透; float 剩余秒 = 0;
        Vector2 旧 = 矢.点; int 命中 = -1;
        if (矢.自动连锁)
        {
            矢.自动连锁 = false;
            if (矢.目标 < 0 || 矢.目标 >= 靶.Length || 矢.命中过.Contains(矢.目标)) return false;
            命中 = 矢.目标;
            if (连线.Count >= 32) 连线.RemoveAt(0);
            连线.Add(new 链线 { 起 = 旧, 终 = 靶[命中] });
        }
        else
        {
            float 距 = Mathf.Min(矢.距, 参数.弹速 * 秒); 矢.点 += 矢.向 * 距; 矢.距 -= 距;
            float 最早 = float.MaxValue;
            for (int i = 0; i < 靶.Length; i++)
            {
                if (矢.命中过.Contains(i)) continue;
                var 段 = 矢.点 - 旧; float t = 段.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector2.Dot(靶[i] - 旧, 段) / 段.sqrMagnitude) : 0;
                float 半径 = .6f + 参数.弹体半径;
                if ((旧 + 段 * t - 靶[i]).sqrMagnitude < 半径 * 半径 && t < 最早) { 命中 = i; 最早 = t; }
            }
            if (命中 < 0) return 矢.距 > 0;
            if (穿透阶段) { 矢.距 += 距 * (1 - 最早); 剩余秒 = Mathf.Max(0, 秒 - 距 * 最早 / Mathf.Max(.001f, 参数.弹速)); }
        }
        矢.点 = 靶[命中]; 矢.命中过.Add(命中); 闪白[命中] = .12f;
        演示命中次数++;
        if (命中圈.Count < 32) 命中圈.Add(new 环 { 点 = 矢.点, 半径 = Mathf.Max(.4f, 参数.溅射半径), 秒 = .22f });
        if (矢.执行段 != null)
        {
            bool 留 = 命中段(矢);
            return 留 && 穿透阶段 && !矢.自动连锁 && 剩余秒 > 0 ? 推进弹(矢, 剩余秒) : 留;
        }
        if (!矢.子 && !矢.已衍生)
        {
            矢.已衍生 = true;
            if (参数.溅射半径 > 0)
            {
                int 数量 = 0;
                for (int 索引 = 0; 索引 < 靶.Length && 数量 < 天帝数值.取("shape.splash_targets_max"); 索引++)
                    if (!矢.命中过.Contains(索引) && Vector2.Distance(靶[索引], 矢.点) <= 参数.溅射半径) { 矢.命中过.Add(索引); 闪白[索引] = .12f; 数量++; }
            }
            for (int i = 0; i < 参数.分裂; i++)
            {
                int 子目标 = 最近靶(矢.点, 矢.命中过);
                var 子 = new 弹 { 点 = 矢.点, 向 = 子目标 < 0 ? 转向(矢.向, (i - (参数.分裂 - 1) * .5f) * 35) : (靶[子目标] - 矢.点).normalized,
                    距 = (float)天帝数值.取("shape.split_child_life"), 子 = true, 目标 = 子目标, 命中过 = 矢.命中过 };
                待加.Add(子); 演示分裂次数++;
            }
        }
        if (矢.链 > 0)
        {
            int 下个 = 最近靶(矢.点, 矢.命中过);
            if (下个 >= 0 && Vector2.Distance(靶[下个], 矢.点) <= 天帝数值.取("shape.chain_range"))
            { 矢.链--; 矢.目标 = 下个; 矢.自动连锁 = true; 演示连锁次数++; return true; }
        }
        return false;
    }
    int 最近靶(Vector2 点, HashSet<int> 排除)
    { int 号 = -1; float 最佳 = float.MaxValue; for (int i = 0; i < 靶.Length; i++) { float d = (靶[i] - 点).sqrMagnitude; if (!排除.Contains(i) && d < 最佳) { 号 = i; 最佳 = d; } } return 号; }
    void 发射段(道纹执行段 段, Vector2 点, Vector2 向, int 目标, HashSet<int> 历史, 演示释放 释放, float 偏, List<弹> 列表, bool 子, int 重复 = 1, bool 自动连锁 = false)
    {
        float 间距 = (float)天帝数值.取("rune.ordered_functions.parallel_offset");
        int 剩余 = (int)天帝数值.取("rune.ordered_functions.projectiles_per_release") - 释放.数量;
        var 首发 = new List<道纹执行段>();
        for (int n = 0; n < 重复; n++)
        {
            foreach (var 弹段 in 天帝顺序道纹.展开齐射(段))
                for (int j = 0; j < 弹段.参数.数量; j++)
                { if (首发.Count >= 剩余) goto 完成展开; 首发.Add(弹段); }
        }
        完成展开:
        for (int i = 0; i < 首发.Count; i++)
        {
            var 弹段 = 首发[i];
            释放.数量++;
            列表.Add(new 弹 { 点 = 自动连锁 ? 点 : 点 + new Vector2(-向.y, 向.x) * (偏 + (i - (首发.Count - 1) * .5f) * 间距), 向 = 向,
                目标 = 目标, 距 = 子 ? (float)天帝数值.取("shape.chain_range") : 普攻参数.飞行距离,
                链 = 弹段.参数.连锁, 执行段 = 弹段, 参数 = 弹段.参数, 释放 = 释放, 命中过 = new HashSet<int>(历史), 自动连锁 = 自动连锁 });
        }
    }
    bool 命中段(弹 矢)
    {
        if (矢.子) return false;
        var 参数 = 矢.参数; var 段 = 矢.执行段;
        if (段.功能 == 道纹功能.穿透)
        {
            if (矢.剩余穿透 < 0) 矢.剩余穿透 = (int)天帝数值.取("rune.ordered_functions.pierce_count");
            if (矢.剩余穿透-- > 0) return true;
            if (段.后续.Count == 0) return false;
            var 下 = 段.后续[0];
            while (天帝顺序道纹.即时功能(下.功能) && 下.功能 != 道纹功能.齐射)
            { if (下.后续.Count == 0) return false; 下 = 下.后续[0]; }
            if (下.功能 == 道纹功能.齐射)
            { 发射段(下, 矢.点, 矢.向, -1, 矢.命中过, 矢.释放, 0, 待加, true); return false; }
            矢.执行段 = 下; 矢.参数 = 下.参数; 矢.链 = 下.参数.连锁; 矢.已衍生 = false; 矢.剩余穿透 = -1;
            return 下.功能 == 道纹功能.穿透 || 命中段(矢);
        }
        if (!矢.已衍生)
        {
            矢.已衍生 = true;
            if (参数.溅射半径 > 0)
            {
                int 数 = 0;
                for (int i = 0; i < 靶.Length && 数 < 天帝数值.取("shape.splash_targets_max"); i++)
                    if (!矢.命中过.Contains(i) && Vector2.Distance(靶[i], 矢.点) <= 参数.溅射半径) { 矢.命中过.Add(i); 闪白[i] = .12f; 数++; }
            }
            for (int i = 0; i < 参数.分裂; i++)
            {
                int 下 = 最近靶(矢.点, 矢.命中过);
                if (下 < 0 || Vector2.Distance(靶[下], 矢.点) > 天帝数值.取("shape.split_child_life")) continue;
                int 前 = 待加.Count;
                发射段(new 道纹执行段 { 参数 = 参数 }, 矢.点, (靶[下] - 矢.点).normalized, 下, 矢.命中过, 矢.释放, 0, 待加, true);
                for (int j = 前; j < 待加.Count; j++) 待加[j].子 = true;
                演示分裂次数 += 待加.Count - 前;
            }
        }
        if (段.功能 == 道纹功能.分裂)
        {
            var 选过 = new HashSet<int>(矢.命中过);
            for (int i = 0; i < 段.后续.Count; i++)
            {
                int 下 = 最近靶(矢.点, 选过);
                if (下 >= 0 && Vector2.Distance(靶[下], 矢.点) > 天帝数值.取("shape.split_child_life")) 下 = -1;
                if (下 >= 0) 选过.Add(下);
                var 向 = 下 < 0 ? 转向(矢.向, (i - .5f) * 35) : (靶[下] - 矢.点).normalized;
                int 前 = 待加.Count; 发射段(段.后续[i], 矢.点, 向, 下, 矢.命中过, 矢.释放, 0, 待加, true);
                演示分裂次数 += 待加.Count - 前;
            }
            return false;
        }
        if (段.功能 == 道纹功能.连锁)
        {
            int 下 = 最近靶(矢.点, 矢.命中过);
            if (下 >= 0 && Vector2.Distance(靶[下], 矢.点) <= 天帝数值.取("shape.chain_range"))
            {
                int 前 = 待加.Count; 发射段(段.后续[0], 矢.点, (靶[下] - 矢.点).normalized, 下, 矢.命中过, 矢.释放, 0, 待加, true, 自动连锁: true);
                if (待加.Count > 前) 演示连锁次数++;
            }
            return false;
        }
        if (矢.链 > 0)
        {
            int 下 = 最近靶(矢.点, 矢.命中过);
            if (下 >= 0 && Vector2.Distance(靶[下], 矢.点) <= 天帝数值.取("shape.chain_range"))
            { 矢.链--; 矢.目标 = 下; 矢.自动连锁 = true; 演示连锁次数++; return true; }
        }
        return false;
    }
    static Vector2 转向(Vector2 向, float 度)
    { float a = 度 * Mathf.Deg2Rad; return new Vector2(向.x * Mathf.Cos(a) - 向.y * Mathf.Sin(a), 向.x * Mathf.Sin(a) + 向.y * Mathf.Cos(a)); }
    protected override void OnPopulateMesh(VertexHelper 网)
    {
        网.Clear(); var 区 = rectTransform.rect; float 比例 = (区.width - 44) / 14.5f;
        Vector2 像(Vector2 点) => new Vector2(区.xMin + 22 + 点.x * 比例, 区.center.y + 点.y * 比例);
        if (扩展演示 != null)
        {
            foreach (var 敌 in 扩展演示.敌人) 圆(网, 像(敌.位置 - 演示原点), 8, 敌.束缚剩余秒 > 0 ? new Color(.55f,.5f,1) : new Color(.35f,.52f,.57f));
            foreach (var 矢 in 扩展演示.灵矢)
            {
                if (天帝顺序道纹.攻击形态(矢.执行段?.功能 ?? 道纹功能.旧版)) continue;
                var 点 = 矢.位置 - 演示原点; float 缩 = 矢.参数.体型倍率;
                线(网, 像(点 - 矢.方向 * .42f * 缩), 像(点), 3 * 缩, new Color(.42f,.94f,.81f)); 圆(网, 像(点), 3 * 缩, Color.white);
                if (矢.等待秒 > 0) 画演示环(网, 像(点), 功能等待半径(矢) * 比例, new Color(1,.8f,.35f));
            }
            foreach (var e in 扩展演示.功能地面效果) 画演示环(网, 像(e.位置 - 演示原点), e.半径 * 比例, new Color(.4f,.8f,.6f,.6f));
            foreach(var e in 扩展演示.特性效果列表)
            {if(e.护体)天帝攻击形态绘制.画护体(e,(a,b,w,c)=>线(网,像(a-演示原点),像(b-演示原点),Mathf.Max(1,w*比例),c));else 画演示环(网,像(e.位置-演示原点),(e.召唤||e.诱饵?.5f:e.半径)*比例,new Color(.55f,.8f,.5f,.8f));}
            foreach (var e in 扩展演示.攻击形态效果) 天帝攻击形态绘制.画(e,
                (a,b,w,c)=>线(网,像(a-演示原点),像(b-演示原点),Mathf.Max(1,w*比例),c),
                (p,r,c)=>画演示环(网,像(p-演示原点),r*比例,c));
            foreach (var e in 扩展演示.光圈) 画演示环(网, 像(e.位置 - 演示原点), e.半径 * 比例, new Color(.4f,.9f,.73f));
            foreach (var e in 扩展演示.电弧) 线(网, 像(e.起点 - 演示原点), 像(e.终点 - 演示原点), 2, new Color(.42f,.8f,1));
            return;
        }
        圆(网, 像(Vector2.zero), 7, new Color(1, .83f, .4f));
        for (int i = 0; i < 靶.Length; i++) 圆(网, 像(靶[i]), 8, 闪白[i] > 0 ? Color.white : new Color(.35f, .52f, .57f));
        foreach (var 矢 in 弹体)
        {
            float 缩 = (矢.参数 ?? 参数).体型倍率;
            线(网, 像(矢.点 - 矢.向 * .42f * 缩), 像(矢.点), (矢.子 ? 2 : 3) * 缩, new Color(.42f, .94f, .81f));
            圆(网, 像(矢.点), 3 * 缩, Color.white);
        }
        foreach (var 链 in 连线) 线(网, 像(链.起), 像(链.终), 2, new Color(.42f, .8f, 1, 链.秒 / .18f));
        foreach (var 圈 in 命中圈)
        {
            var 中 = 像(圈.点); float 半 = 圈.半径 * 比例;
            for (int i = 0; i < 24; i++)
            { float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12; 线(网, 中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半, 中 + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 半, 1, new Color(.4f, .9f, .73f, 圈.秒 / .22f)); }
        }
    }
    static void 圆(VertexHelper 网, Vector2 中, float 半, Color 色)
    {
        画演示实圆(网, 中, 半, 色);
    }
    static float 功能等待半径(战斗灵矢 矢) => 天帝顺序道纹.扩展数值(矢.执行段.功能 == 道纹功能.陨落 ? "fall_radius" : 矢.执行段.功能 == 道纹功能.烙印 ? "mark_radius" : 矢.执行段.功能 == 道纹功能.震荡 ? "pulse_radius" : "trail_radius");
    static void 画演示环(VertexHelper 网, Vector2 中, float 半, Color 色)
    { for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12; 线(网, 中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半, 中 + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 半, 1.5f, 色); } }
    static void 画演示实圆(VertexHelper 网, Vector2 中, float 半, Color 色)
    {
        for (int i = 0; i < 12; i++)
        { float a = i * Mathf.PI / 6, b = (i + 1) * Mathf.PI / 6; int n = 网.currentVertCount; 网.AddVert(中, 色, Vector2.zero); 网.AddVert(中 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 半, 色, Vector2.zero); 网.AddVert(中 + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 半, 色, Vector2.zero); 网.AddTriangle(n, n + 1, n + 2); }
    }
    static void 线(VertexHelper 网, Vector2 a, Vector2 b, float 宽, Color 色)
    {
        var 横 = new Vector2(-(b - a).y, (b - a).x).normalized * 宽 * .5f; int n = 网.currentVertCount;
        网.AddVert(a + 横, 色, Vector2.zero); 网.AddVert(b + 横, 色, Vector2.zero); 网.AddVert(b - 横, 色, Vector2.zero); 网.AddVert(a - 横, 色, Vector2.zero); 网.AddTriangle(n, n + 1, n + 2); 网.AddTriangle(n, n + 2, n + 3);
    }
}
