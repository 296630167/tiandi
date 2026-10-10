using UnityEngine;
using UnityEngine.UI;

public partial class 天帝界面
{
    RectTransform 战斗界面层, 战斗暂停层;
    Text 战斗经验字, 战斗详细统计;
    Image 战斗经验条;
    RectTransform 战斗生命底,战斗经验底;
    float 下次战斗文本刷新;
    public bool 战斗已暂停 { get; private set; }

    RectTransform 战斗角落(string 名, Vector2 锚点, Vector2 偏移, Vector2 尺寸)
    {
        var 区 = 区块(战斗界面层, 名, 0, 0, 尺寸.x, 尺寸.y);
        区.anchorMin = 区.anchorMax = 区.pivot = 锚点; 区.anchoredPosition = 偏移; return 区;
    }
    Text 战斗文字(RectTransform 父, string 内容, float x, float y, float 宽, float 高, int 字号, Color 色)
    {
        var 文 = 字(父, 内容, x, y, 宽, 高, 字号, 色);
        文.alignment = TextAnchor.MiddleLeft; 文.horizontalOverflow = HorizontalWrapMode.Overflow;
        文.verticalOverflow = VerticalWrapMode.Overflow; return 文;
    }
    // 战斗层压低装饰对比，让角色、敌人和技能成为视线中心；页面仍沿用同一套青绿/宣纸素材。
    void 战斗面板(Image 图)
    {
        if (图 == null) return;
        if (天帝剪纸界面皮肤.已启用)
        {
            图.sprite = 天帝剪纸界面皮肤.素材("墨青画布");
            图.type = Image.Type.Sliced; 图.pixelsPerUnitMultiplier = 2;
            图.color = new Color(1, 1, 1, .90f);
        }
        else if (天帝青绿皮肤.已启用)
        {
            图.sprite = 天帝青绿皮肤.获取("QLUI_深青面板");
            图.type = Image.Type.Sliced; 图.pixelsPerUnitMultiplier = 2;
            图.color = new Color(1, 1, 1, .90f);
        }
        else
        {
            图.sprite = null; 图.type = Image.Type.Simple;
            图.color = new Color(.025f, .08f, .075f, .86f);
        }
        图.raycastTarget = false;
    }
    void 建立紧凑战斗界面()
    {
        下次战斗文本刷新=0;
        // HUD与页面共用固定16:9画面，保持本平台的逻辑字号和触控尺寸。
        战斗界面层 = 铺满(安全区, "战斗HUD");
        天帝响应布局.动态(战斗界面层);
        战斗界面层.anchorMin = 战斗界面层.anchorMax = 战斗界面层.pivot = new Vector2(.5f, .5f);
        战斗界面层.anchoredPosition = Vector2.zero; 战斗界面层.sizeDelta = 天帝移动适配.布局尺寸;
        战斗界面层.localScale = Vector3.one * 安全区布局比例(天帝移动适配.布局尺寸);
        战斗界面层.SetSiblingIndex(设计区.GetSiblingIndex());
        var 状态 = 战斗角落("主角战斗状态", new Vector2(0, 1), new Vector2(18, -18), new Vector2(330, 108));
        var 底 = 状态.gameObject.AddComponent<Image>(); 战斗面板(底);
        战斗文字(状态, "青岚原 · 地图 Lv." + 游戏.当前地图等级, 14, 4, 302, 25, 17, new Color(.74f, .87f, .80f));
        战斗血量 = 战斗文字(状态, "", 14, 29, 302, 25, 18, 纸);
        战斗生命底=图(状态, "生命底", 14, 55, 302, 7, new Color(.12f, .08f, .07f, .8f)).rectTransform;
        战斗血条 = 图(状态, "生命填充", 14, 55, 302, 7, new Color(.73f, .31f, .27f));
        战斗经验底=图(状态, "经验底", 14, 69, 302, 3, new Color(.05f, .17f, .16f)).rectTransform;
        战斗经验条 = 图(状态, "经验填充", 14, 69, 0, 3, new Color(.45f, .83f, .70f));
        战斗经验字 = 战斗文字(状态, "", 14, 74, 302, 30, 15, new Color(.66f, .81f, .76f));
        // 真实资源交给底部球体HUD；顶部只保留地图与经验。
        战斗血量.gameObject.SetActive(false); 战斗生命底.gameObject.SetActive(false); 战斗血条.gameObject.SetActive(false);

        var 统计 = 战斗角落("波次敌人信息", Vector2.one, new Vector2(-18, -18), new Vector2(374, 114));
        var 统计底 = 统计.gameObject.AddComponent<Image>(); 战斗面板(统计底);
        战斗波次 = 战斗文字(统计, "", 14, 5, 346, 104, 16, 纸);
        战斗波次.alignment = TextAnchor.UpperLeft;
        var 暂停区 = 战斗角落("暂停入口", Vector2.one, new Vector2(-18, -140), new Vector2(94, 36));
        var 暂停键 = 按钮(暂停区, 天帝移动适配.启用 ? "暂停" : "暂停 Esc", 0, 0, 94, 36, 切换战斗暂停);
        暂停键.targetGraphic.color = Color.white;
        var 暂停字 = 暂停键 != null ? 暂停键.GetComponentInChildren<Text>() : null;
        if (暂停字 != null) { 暂停字.color = 天帝道纹美术.正文; 暂停字.fontSize = 16; }

        bool 手机 = 使用移动控件(天帝移动适配.启用);
        var 小图框 = 战斗角落("战斗小地图底", Vector2.zero, new Vector2(18, 手机 ? 190 : 18), new Vector2(112, 112));
        var 小图底 = 小图框.gameObject.AddComponent<Image>(); 天帝界面美术.面板(小图底,"二级面板",new Color(1,1,1,.95f)); 小图底.raycastTarget = false;
        战斗小地图 = 地图预览(小图框, 6, 6, 100, 100);
        战斗小地图.当前地图 = 游戏.战斗场景.地图; 战斗小地图.SetVerticesDirty();
        var 目标框 = 战斗角落("战斗目标", Vector2.zero, new Vector2(18, 82), new Vector2(300, 52));
        var 目标底 = 目标框.gameObject.AddComponent<Image>(); 战斗面板(目标底);
        战斗锁定信息 = 战斗文字(目标框, "未锁定目标", 10, 2, 280, 20, 14, new Color(.80f, .92f, .82f));
        战斗锁定信息.name = "自动锁定信息";
        战斗目标 = 战斗文字(目标框, "", 10, 23, 280, 25, 15, 天帝道纹美术.浅字);
        战斗目标.color = 天帝界面主题.浅字;
        战斗目标.horizontalOverflow = HorizontalWrapMode.Wrap;

        拾取列表 = new 天帝拾取提示(战斗界面层, 游戏.默认字体, 游戏.拾取提示停留秒, true, true);
        var 通知区 = 拾取列表.区域;
        通知区.anchorMin = 通知区.anchorMax = 通知区.pivot = new Vector2(1, 0);
        通知区.anchoredPosition = new Vector2(-18, 80);
        拾取事件源 = 游戏.战斗场景.战斗.掉落; 拾取事件源.获得道纹 += 拾取列表.加入;
        通货事件源 = 游戏.战斗场景.战斗.通货掉落; 通货事件源.获得通货 += 拾取列表.加入;
        灵石事件源 = 游戏.战斗场景.战斗.灵石掉落; 灵石事件源.获得灵石 += 拾取列表.加入灵石;

        var 离开区 = 战斗角落("离开入口", new Vector2(1, 0), new Vector2(-18, 18), new Vector2(142, 44));
        战斗离开 = 按钮(离开区, "返回主页", 0, 0, 142, 44, 游戏.返回主页);
        var 离开字 = 战斗离开 != null ? 战斗离开.GetComponentInChildren<Text>() : null;
        if (离开字 != null) 离开字.fontSize = 19;
        if (手机 && 游戏.战斗场景?.战斗?.纯AI模式 != true)
        {
            var 摇杆区 = 战斗角落("移动摇杆", Vector2.zero, new Vector2(18, 18), new Vector2(154, 154));
            战斗摇杆 = 摇杆区.gameObject.AddComponent<天帝移动摇杆>();
            var 跑步区 = 战斗角落("跑步入口", new Vector2(1, 0), new Vector2(-174, 18), new Vector2(106, 48));
            var 跑 = 按钮(跑步区, "跑步：关", 0, 0, 106, 48, () => 触控跑步 = !触控跑步);
            移动跑步文字 = 跑 != null ? 跑.GetComponentInChildren<Text>() : null; var 标签 = 移动跑步文字;
            if (标签 != null) 标签.fontSize = 18;
            if (跑 != null) 跑.onClick.AddListener(() => { if (标签 != null) 标签.text = 触控跑步 ? "跑步：开" : "跑步：关"; });
        }
        建立主动战斗HUD(); 更新移动战斗布局();
    }
    static string 战斗短数(float 值) => Mathf.Abs(值) >= 10000 ? (值 / 10000).ToString("0.##") + "万" : 值.ToString("0.#");
    static void 战斗条进度(Image 图,RectTransform 底,float 值)
    {
        if(图==null||底==null)return;
        var r=图.rectTransform;var min=底.anchorMin;var max=new Vector2(Mathf.Lerp(min.x,底.anchorMax.x,Mathf.Clamp01(值)),底.anchorMax.y);
        if(r.anchorMin==min&&r.anchorMax==max&&r.offsetMin==Vector2.zero&&r.offsetMax==Vector2.zero)return;
        r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
    }
    void 刷新紧凑战斗状态(天帝战斗系统 战, 天帝主角属性 人)
    {
        战斗条进度(战斗血条,战斗生命底,人.当前血量/Mathf.Max(1,人.血量));
        刷新主动战斗HUD(战, 人);
        更新战斗锁定信息(战);
        if(Time.unscaledTime<下次战斗文本刷新&&!战斗已暂停)return;
        下次战斗文本刷新=Time.unscaledTime+天帝战斗润色.取("hud_interval");
        if (战斗血量 != null) 战斗血量.text = "Lv." + 人.等级 + "  生命 " + 战斗短数(人.当前血量) + " / " + 战斗短数(人.血量) + "  盾 " + 战斗短数(人.当前灵气护盾 + 战.特性临时护盾);
        bool 满级 = 人.等级 >= 天帝数值.玩家上限;
        if (战斗经验字 != null) 战斗经验字.text = 满级 ? "经验 · 已满级" : "经验 " + 游戏.道纹数据.当前经验 + " / " + 游戏.道纹数据.升级所需经验;
        战斗条进度(战斗经验条,战斗经验底,满级?1:(float)游戏.道纹数据.当前经验/Mathf.Max(1,游戏.道纹数据.升级所需经验));
        string 分类(战斗敌人级别 类) => 战.分类剩余(类) + "<color=#A9C2B4>(场" + 战.分类在场(类) + ")</color>";
        if (战斗波次 != null)
        {
            if (天帝移动适配.启用)
                战斗波次.text = "剩余 " + 战.剩余敌人数量 + " · 场 " + 战.场上敌人数量 + " · 待 " + 战.未生成敌人数量 + "\n普 " + 战.分类剩余(战斗敌人级别.普通) + "  精 " + 战.分类剩余(战斗敌人级别.精英) + "  头 " + 战.分类剩余(战斗敌人级别.头目) + "  王 " + 战.分类剩余(战斗敌人级别.王级);
            else
                战斗波次.text = "<size=24>剩余 " + 战.剩余敌人数量 + " / " + 战.敌人.Count + "</size>  <color=#A9C2B4>场上 " + 战.场上敌人数量 + " · 待刷 " + 战.未生成敌人数量 + "</color>\n"
                    + "普通 " + 分类(战斗敌人级别.普通) + "    精英 " + 分类(战斗敌人级别.精英) + "\n"
                    + "头目 " + 分类(战斗敌人级别.头目) + "    BOSS " + 分类(战斗敌人级别.王级);
        }
        if (战斗已暂停) 刷新战斗暂停详情(战, 人);
    }
    public void 切换战斗暂停()
    {
        if (游戏.阶段 != 游戏阶段.战斗 || 游戏.战斗场景?.战斗 == null) return;
        if (战斗已暂停) { 关闭战斗暂停(); return; }
        if (游戏.战斗场景.战斗.玩家死亡) return;
        取消战斗手势();
        if (天帝剪纸界面皮肤.已启用) { 显示山水暂停(); return; }
        战斗已暂停 = true; 触控跑步 = false;
        if (战斗摇杆 != null) 战斗摇杆.gameObject.SetActive(false);
        战斗暂停层 = 铺满(弹层, "战斗暂停层");
        // HUD分平台生成，暂停详情与其它页面共用百分比锚点。
        var 遮 = 铺满(战斗界面层, "暂停遮罩").gameObject.AddComponent<Image>();
        遮.color = new Color(.01f, .025f, .025f, .68f); 遮.raycastTarget = true;
        遮.transform.SetAsLastSibling();
        var 详情 = 图(战斗暂停层, "暂停详情", 340, 118, 920, 664, new Color(.025f, .075f, .068f, .98f)).rectTransform;
        if (天帝移动适配.启用) 天帝响应布局.比例(详情, .045f, .025f, .91f, .95f);
        字(详情, "战斗暂停", 24, 18, 872, 52, 32, 纸);
        var 左=图(详情,"战况面板",28,92,416,262,Color.white).rectTransform;
        var 右=图(详情,"攻击面板",476,92,416,262,Color.white).rectTransform;
        字(左,"当前战况",20,12,376,36,22,天帝道纹美术.强调).alignment=TextAnchor.MiddleLeft;
        字(右,"当前攻击",20,12,376,36,22,天帝道纹美术.强调).alignment=TextAnchor.MiddleLeft;
        战斗详细统计 = 战斗文字(左, "", 20, 60, 376, 190, 18, 纸); 战斗详细统计.alignment = TextAnchor.UpperLeft;
        战斗配置字 = 战斗文字(右, "", 20, 60, 376, 190, 20, 纸); 战斗配置字.alignment = TextAnchor.UpperLeft;
        var 收益=图(详情,"战斗收获面板",28,374,864,104,Color.white).rectTransform;
        字(收益,"本局收获",20,10,200,34,22,天帝道纹美术.强调).alignment=TextAnchor.MiddleLeft;
        掉落提示 = 战斗文字(收益, "", 20, 49, 824, 42, 20, 纸);
        战斗坐标 = 战斗文字(详情, "", 28, 495, 864, 34, 16, 天帝道纹美术.次文);
        字(详情, "自动观战 · 道纹构筑展示 · Esc暂停", 28, 539, 864, 32, 17, 天帝道纹美术.次文);
        var 继续键 = 按钮(详情, "继续战斗", 80, 590, 340, 54, 关闭战斗暂停, true);
        var 继续字 = 继续键 != null ? 继续键.GetComponentInChildren<Text>() : null; if (继续字 != null) 继续字.fontSize = 23;
        var 返回键 = 按钮(详情, "返回主页", 500, 590, 340, 54, 游戏.返回主页);
        var 返回字 = 返回键 != null ? 返回键.GetComponentInChildren<Text>() : null; if (返回字 != null) 返回字.fontSize = 23;
        刷新战斗暂停详情(游戏.战斗场景.战斗, 游戏.主角属性);
        if (天帝移动适配.启用)
        {
            var 正文口 = 天帝双端页面布局.滚动组(详情, "暂停正文", 28, 92, 864, 482);
            var 正文 = 正文口.GetComponent<ScrollRect>().content;
            // 先保留设计位置，按可读字号重排；信息只在正文内部滚动，操作固定在底部。
            foreach (var 文 in 正文.GetComponentsInChildren<Text>()) 文.fontSize = 14;
            var 重排 = 天帝双端页面布局.重排正文(正文);
            天帝双端页面布局.移动页(详情, 面板 =>
            {
                float 宽 = 面板.rect.width;
                float 半宽 = (宽 - 36) / 2;
                天帝双端页面布局.固定(正文口, 12, 50, 宽 - 24, 面板.rect.height - 116); 重排();
                foreach (Transform 子 in 面板)
                    if (子.GetComponent<Text>() is Text 文)
                    {
                        文.fontSize = 22; 天帝双端页面布局.固定(文.rectTransform, 12, 4, 宽 - 24, 40);
                    }
                天帝双端页面布局.按键(面板.Find("继续战斗") as RectTransform, 12, 面板.rect.height - 56, 半宽, 48);
                天帝双端页面布局.按键(面板.Find("返回主页") as RectTransform, 24 + 半宽, 面板.rect.height - 56, 半宽, 48);
            });
        }
    }
    void 刷新战斗暂停详情(天帝战斗系统 战, 天帝主角属性 人)
    {
        if (战斗详细统计 == null) return;
        if (山水战况右字 != null) { 刷新山水暂停详情(战, 人); return; }
        战斗详细统计.text = "青岚原 · 地图等级 " + 游戏.当前地图等级 + "\n" + 战.刷新阶段
            + "\n剩余 " + 战.剩余敌人数量 + " / " + 战.敌人.Count + " · 场上 " + 战.场上敌人数量 + "\n待刷新 " + 战.未生成敌人数量
            + "\n生命 " + 人.当前血量.ToString("0.##") + " / " + 人.血量.ToString("0.##") + "\n护盾 " + (人.当前灵气护盾 + 战.特性临时护盾).ToString("0.##")
            + "\n本局经验 " + 战.本局经验 + " · 升级 " + 战.本局升级次数 + "次";
        更新战斗位置(游戏.战斗场景.玩家位置, 游戏.战斗场景.地图.所在格(游戏.战斗场景.玩家位置));
        var 参数 = 战.当前普攻;
        战斗配置字.text = 参数.顺序计划 != null ? 天帝道纹.通路名称(参数.通路) + " · " + 战.开放通路数 + "路自动施法\n首发 " + 参数.数量 + " 颗 · 各出口独立攻击\n功能按链路顺序执行\n各段属性与伤害请查看道纹画布" : 天帝道纹.通路名称(参数.通路) + " · " + 战.开放通路数 + "路自动施法\n普通 " + 参数.普通伤害.ToString("0.##") + " + 五行 " + 参数.五行额外伤害.ToString("0.##")
            + "\n当前单发伤害 " + 参数.伤害.ToString("0.##") + "\n数量 " + 参数.数量 + " · 分裂 " + 参数.分裂 + " · 连锁 " + 参数.连锁;
        掉落提示.text = "道纹 " + 战.掉落.拾取数 + " 枚   ·   通货 " + 战.通货掉落.拾取总量 + "   ·   灵石 +" + 战.灵石掉落.拾取总量;
    }
    public void 关闭战斗暂停()
    {
        战斗已暂停 = false;
        if (移动跑步文字 != null) 移动跑步文字.text = 触控跑步 ? "跑步：开" : "跑步：关";
        更新主动战斗布局();
        if (战斗暂停层 != null) { 战斗暂停层.gameObject.SetActive(false); 删除界面对象(战斗暂停层.gameObject); 战斗暂停层 = null; }
        if (战斗界面层 != null)
        {
            var 遮 = 战斗界面层.Find("暂停遮罩");
            if (遮 != null) { 遮.gameObject.SetActive(false); 删除界面对象(遮.gameObject); }
        }
        战斗详细统计 = 战斗坐标 = 战斗配置字 = 掉落提示 = null;
        山水战况右字 = 山水攻击右字 = null;
        if (战斗摇杆 != null && 游戏.战斗场景?.战斗?.玩家死亡 != true) 战斗摇杆.gameObject.SetActive(true);
    }
    Text 移动跑步文字;
    void 更新移动战斗布局()
    {
        if (战斗界面层 == null) return;
        bool 手机 = 天帝移动适配.启用;
        float 宽 = 战斗界面层.rect.width, 高 = 战斗界面层.rect.height;
        if (宽 <= 0 || 高 <= 0) return;
        void 区(string 名, float x, float y, float w, float h)
        { var r = 战斗界面层.Find(名) as RectTransform; if (r != null) 天帝响应布局.比例(r, x, y, w, h); }
        float 顶高 = 手机 ? Mathf.Clamp(52f / 高, .13f, .16f) : Mathf.Min(.28f, Mathf.Max(.125f, 110f / 高));
        区("主角战斗状态", .015f, .02f, 手机 ? .25f : .225f, 顶高);
        区("波次敌人信息", 手机 ? .72f : .735f, .02f, 手机 ? .265f : .25f, 顶高);
        var 状态=战斗界面层.Find("主角战斗状态");
        foreach(Transform 子 in 状态)
        {
            if(子.GetComponent<Text>() is Text 文)
            {
                float y=文==战斗血量 ? .28f : 文==战斗经验字 ? .73f : .045f;
                天帝响应布局.比例(文.rectTransform,.045f,y,.91f,.24f);
                文.fontSize=手机?(文==战斗血量?12:11):(文==战斗血量?18:文==战斗经验字?15:17);
                文.color=天帝道纹美术.正文;
            }
            else if(子.GetComponent<Image>()!=null)
                天帝响应布局.比例((RectTransform)子,.045f,子.name.StartsWith("生命") ? .57f : .70f,.91f,子.name.StartsWith("生命") ? .065f : .025f);
        }
        if(战斗波次!=null)
        {天帝响应布局.比例(战斗波次.rectTransform,.045f,.08f,.91f,.84f);战斗波次.fontSize=手机?10:16;战斗波次.color=天帝道纹美术.正文;战斗波次.horizontalOverflow=HorizontalWrapMode.Overflow;}
        float 按高 = Mathf.Max(手机 ? 44 : 36, 高 * (手机 ? .11f : .045f)) / 高;
        float 暂停宽 = Mathf.Max(手机 ? 76 : 94, 宽 * .08f) / 宽;
        区("暂停入口", .985f - 暂停宽, .03f + 顶高, 暂停宽, 按高);
        float 离开宽 = Mathf.Max(手机 ? 104 : 142, 宽 * .115f) / 宽;
        float 跑宽 = Mathf.Max(88, 宽 * .10f) / 宽;
        区("离开入口", .985f - 离开宽 - (手机 ? 跑宽 + .015f : 0), .98f - 按高, 离开宽, 按高);
        if (手机) 区("跑步入口", .985f - 跑宽, .98f - 按高, 跑宽, 按高);
        float 摇边 = Mathf.Min(160, Mathf.Max(120, 高 * .29f));
        if (手机) 区("移动摇杆", .015f, .98f - 摇边 / 高, 摇边 / 宽, 摇边 / 高);
        float 图边 = 手机 ? Mathf.Clamp(高 * .15f, 56, 88) : Mathf.Clamp(高 * .15f, 100, 140);
        float 图宽 = 游戏.战斗场景?.地图.横向区域 == true ? 图边 * 2.5f : 图边;
        float 图高 = 游戏.战斗场景?.地图.横向区域 == true ? 图宽 * 游戏.战斗场景.地图.半高 / 游戏.战斗场景.地图.半宽 : 图边;
        float 图顶 = 手机 ? .02f + 顶高 + .025f : .98f - 图高 / 高;
        区("战斗小地图底", .015f, 图顶, 图宽 / 宽, 图高 / 高);
        if (战斗小地图 != null) 天帝响应布局.比例(战斗小地图.rectTransform, .025f, .06f, .95f, .88f);
        // 导航目标与小地图共用左边界，PC放上方，手机放下方并避开底部摇杆。
        float 目标高 = 手机 ? 32 : 64, 间距 = 手机 ? 6 : 12;
        float 目标宽 = Mathf.Max(图宽, 手机 ? 180 : 300);
        区("战斗目标", 手机 ? .015f : .015f+(图宽+间距)/宽, 手机 ? 图顶 + 图高 / 高 + 间距 / 高 : .98f-目标高/高, 目标宽 / 宽, 目标高 / 高);
        if (战斗目标 != null)
        {
            天帝响应布局.比例(战斗目标.rectTransform, .045f, 手机 ? .48f : .40f, .91f, 手机 ? .46f : .54f);
            战斗目标.fontSize = 手机 ? 9 : 15;
            战斗目标.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        if (战斗锁定信息 != null)
        {
            天帝响应布局.比例(战斗锁定信息.rectTransform, .045f, .04f, .91f, 手机 ? .40f : .34f);
            战斗锁定信息.fontSize = 手机 ? 9 : 14;
            战斗锁定信息.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        // 拾取通知需要保留完整的两行文案；移动端给出更宽的右侧安全区，避免字体被压缩成一团。
        区("拾取提示列表", 手机 ? .53f : .70f, 手机 ? .39f : .56f, 手机 ? .45f : .285f, 手机 ? .36f : .34f);
        foreach (string 名 in new[] { "暂停入口", "跑步入口", "离开入口" })
        {
            var r = 战斗界面层.Find(名); var 键 = r == null ? null : r.GetComponentInChildren<Button>(true);
            if (键 != null) 天帝响应布局.比例((RectTransform)键.transform, 0, 0, 1, 1);
        }
        if (移动跑步文字 != null) 移动跑步文字.text = 触控跑步 ? "跑步：开" : "跑步：关";
        更新主动战斗布局();
    }
    void 清理战斗界面()
    {
        取消战斗手势(); 清理主动战斗HUD(); 关闭战斗暂停(); 战斗经验字 = null; 战斗经验条 = null;
        if (战斗界面层 == null) return;
        战斗界面层.gameObject.SetActive(false); 删除界面对象(战斗界面层.gameObject); 战斗界面层 = null;
    }
}
