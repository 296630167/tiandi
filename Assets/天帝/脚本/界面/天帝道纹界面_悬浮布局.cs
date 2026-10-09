using UnityEngine;
using UnityEngine.UI;

public sealed partial class 天帝道纹界面
{
    ScrollRect 悬浮候选滚动;
    int 悬浮上次页 = -1;
    Vector2 悬浮画布中心 => 天帝移动适配.启用 ? new Vector2(-42, 0) : 山水构筑 ? new Vector2(-19, 0) : new Vector2(-74, 0);
    Vector2 悬浮画布尺寸 => 天帝移动适配.启用 ? new Vector2(330, 208) : 山水构筑 ? new Vector2(878, 650) : new Vector2(1016, 752);
    // B方案：网格铺开，边缘工具只占自己的窄区，中间没有大纸板。
    void 建悬浮布局()
    {
        bool 手机 = 天帝移动适配.启用;
        float 宽 = 手机 ? 640 : 1600, 高 = 手机 ? 360 : 900;
        void 定(RectTransform r, float x, float y, float w, float h) => 天帝双端页面布局.固定(r, x, y, w, h);
        void 区(string 名, float x, float y, float w, float h)
        { var r = 天帝双端页面布局.子区(根, 名); if (r != null) 定(r, x, y, w, h); }
        void 键(string 名, float x, float y, float w, float h, string 标签 = null)
        {
            var r = 天帝双端页面布局.子区(根, 名); if (r == null) return;
            定(r, x, y, w, h);
            if (标签 != null) { var 文 = r.GetComponentInChildren<Text>(); if (文 != null) 文.text = 标签; }
            foreach (var 文 in r.GetComponentsInChildren<Text>()) 文.fontSize = 手机 ? 14 : 18;
            if (名 != "返回主页")
            {
            var 图 = r.GetComponent<Image>(); if (图 != null) { 图.sprite = 天帝剪纸界面皮肤.素材("墨绿按钮") ?? 天帝青绿皮肤.获取("QLUI_精修深青按钮");
            图.type = 天帝剪纸界面皮肤.已启用?Image.Type.Simple:Image.Type.Sliced; 图.pixelsPerUnitMultiplier = 天帝剪纸界面皮肤.已启用 ? 1 : 3; 图.color = Color.white; 图.raycastTarget = true; }
                foreach (var 文 in r.GetComponentsInChildren<Text>()) 文.color = 天帝道纹美术.浅字;
            }
            // 旧图标在窄按钮中不再占位，保留完整标签和点击逻辑。
            foreach (Transform 子 in r) if (子.GetComponent<Image>() != null) 子.gameObject.SetActive(false);
            r.GetComponent<天帝按钮文字区域>()?.更新();
        }
        区("道纹背景", 0, 0, 宽, 高);
        var 背景 = 根.Find("道纹背景")?.GetComponent<Image>();
        if (背景 != null) { 天帝道纹美术.应用(背景, "画布内衬"); 背景.color = new Color(.08f, .22f, .24f); }
        if (天帝剪纸界面皮肤.已启用 && 背景 != null) { 背景.sprite=天帝剪纸界面皮肤.素材("构筑背景"); 背景.type=Image.Type.Simple; 背景.color=Color.white; }
        foreach (string 名 in new[] { "页面标题衬纸", "画布主面板", "藏匣标题衬纸", "藏匣分割线", "工具分组线" })
            根.Find(名)?.gameObject.SetActive(false);
        foreach (Transform 子 in 根)
        {
            if (!(子.GetComponent<Text>() is Text 文)) continue;
            if (文.text == "道纹构筑")
            { 定(文.rectTransform, 手机 ? 12 : 32, 4, 手机 ? 132 : 250, 手机 ? 36 : 50); 文.fontSize = 手机 ? 22 : 32; }
            else if (文.text == "定位") 文.gameObject.SetActive(false);
        }
        定(成长字.rectTransform, 手机 ? 148 : 300, 4, 手机 ? 362 : 1032, 手机 ? 36 : 50); 成长字.fontSize = 手机 ? 14 : 18;
        键("返回主页", 宽 - (手机 ? 116 : 208), 4, 手机 ? 108 : 180, 手机 ? 40 : 46);
        string[] 工具 = { "回到源点", "聚焦已解锁", "定位未接通", "撤销上一步", "布局方案", "操作说明" };
        if (手机)
        {
            for (int i = 0; i < 5; i++) 键(工具[i], 116 + i * 103, 46, 99, 44, new[] { "源点", "聚焦", null, "撤销", "方案" }[i]);
            键("操作说明", 8, 46, 100, 44, "筛选 / 排序");
            // 筛选沿用独立按钮，操作说明保留在右栏滚动内容内。
            根.Find("操作说明")?.gameObject.SetActive(false);
        }
        else
            for (int i = 0; i < 工具.Length; i++) 键(工具[i], 300 + i * 176, 60, 166, 44, i == 3 ? "撤销" : null);
        定(汇总字.rectTransform, 24, 60, 260, 40); 汇总字.fontSize = 16; 汇总字.gameObject.SetActive(!手机);

        if (画布 == null || 画布.transform.parent == null) return;
        var 视口 = (RectTransform)画布.transform.parent;
        画布.轻透画布 = true;
        定(视口, 8, 手机 ? 96 : 110, 宽 - 16, 手机 ? 208 : 752);
        var 底图 = 视口.GetComponent<Image>(); if (底图 != null) { 底图.sprite = null; 底图.color = 天帝剪纸界面皮肤.已启用 ? Color.clear : new Color(.055f, .18f, .20f, .84f); }
        if (天帝剪纸界面皮肤.已启用)
        {
            var 内衬=区块(视口,"墨青纹理工作区",手机?108:212,0,手机?318:988,手机?168:694);
            定(内衬,手机?108:212,0,手机?318:988,手机?168:694);
            var 纸=内衬.gameObject.AddComponent<Image>();纸.sprite=天帝剪纸界面皮肤.素材("墨青画布");纸.raycastTarget=false;
            内衬.SetAsFirstSibling();
        }
        视口.SetSiblingIndex(1); // 所有边缘控件在网格之上；格标签始终跟随画布。
        定(状态字.rectTransform, 20, 110, 196, 36); 状态字.text = "道纹藏匣"; 状态字.fontSize = 22; 状态字.gameObject.SetActive(!手机);
        键("筛选 / 排序", 手机 ? 8 : 20, 手机 ? 46 : 150, 手机 ? 100 : 192, 44);
        定(候选区, 手机 ? 8 : 20, 手机 ? 98 : 204, 手机 ? 100 : 192, 手机 ? 166 : 596);
        if (候选区 == null) return;
        var 口图 = 候选区.GetComponent<Image>() ?? 候选区.gameObject.AddComponent<Image>(); 口图.color = Color.clear; 口图.raycastTarget = true;
        候选区.gameObject.AddComponent<RectMask2D>();
        var 内容 = 区块(候选区, "悬浮藏匣内容", 0, 0, 手机 ? 100 : 192, 每页数量 * (手机 ? 110 : 120));
        天帝响应布局.动态(内容);
        foreach (var 卡 in 候选卡) if (卡 != null) 卡.transform.SetParent(内容, false);
        悬浮候选滚动 = 候选区.gameObject.AddComponent<ScrollRect>(); 悬浮候选滚动.viewport = 候选区; 悬浮候选滚动.content = 内容;
        悬浮候选滚动.horizontal = false; 悬浮候选滚动.vertical = true; 悬浮候选滚动.movementType = ScrollRect.MovementType.Clamped;
        悬浮候选滚动.scrollSensitivity = 36;
        // 图鉴和详情仍保留全部词条，藏匣缩略卡只承担选择与拖动。
        if (空列表提示 != null) { 空列表提示.transform.SetParent(候选区, false); 定(空列表提示.rectTransform, 4, 12, 手机 ? 92 : 184, 手机 ? 130 : 140); 空列表提示.fontSize = 14; }
        for (int i = 0; i < 候选卡.Count; i++) 排悬浮候选卡(i, i % 每页数量);
        键("上一页", 手机 ? 8 : 20, 手机 ? 270 : 812, 手机 ? 46 : 90, 44, "‹");
        键("下一页", 手机 ? 62 : 122, 手机 ? 270 : 812, 手机 ? 46 : 90, 44, "›");
        定(页码字.rectTransform, 手机 ? 8 : 20, 手机 ? 316 : 858, 手机 ? 100 : 192, 32); 页码字.fontSize = 14; 页码字.alignment = TextAnchor.MiddleCenter;
        定(提示字.rectTransform, 手机 ? 116 : 250, 手机 ? 266 : 862, 手机 ? 318 : 1330, 手机 ? 40 : 34); 提示字.fontSize = 手机 ? 14 : 16;
        var 图例 = 文字(根, "<color=#235641>━ 已激活</color>   <color=#9A3529>━ 未激活</color>   <color=#59675D>━ 空位</color>",
            手机 ? 116 : 260, 手机 ? 242 : 816, 手机 ? 318 : 600, 手机 ? 22 : 32, 手机 ? 12 : 16, TextAnchor.MiddleCenter);
        图例.name = "链路状态图例"; 图例.supportRichText = true;
        定(图例.rectTransform, 手机 ? 116 : 260, 手机 ? 242 : 816, 手机 ? 318 : 600, 手机 ? 22 : 32);

        var 预览 = 根.Find("实时构筑预览") as RectTransform; if (预览 == null) return;
        var 预览口 = 天帝响应布局.滚动正文(预览);
        定(预览口, 手机 ? 448 : 1208, 手机 ? 98 : 118, 手机 ? 184 : 376, 手机 ? 202 : 730);
        float 信息宽 = 手机 ? 184 : 376;
        float 内边 = 手机 ? 10 : 18, 正文宽 = 信息宽 - 内边 * 2;
        定(预览, 0, 0, 信息宽, 手机 ? 650 : 680);
        var 预览底 = 预览.GetComponent<Image>(); if (预览底 != null) { 天帝道纹美术.应用(预览底, 天帝剪纸界面皮肤.已启用 ? "详情框" : "画布内衬"); 预览底.color = 天帝剪纸界面皮肤.已启用 ? Color.white : new Color(.09f, .24f, .26f, .9f); }
        var 来源键 = 预览.Find("加成来源") as RectTransform;
        定(来源键, 手机 ? 98 : 242, 8, 手机 ? 76 : 116, 44);
        var 来源图 = 来源键?.GetComponent<Image>(); if (来源图 != null) { 天帝道纹美术.应用(来源图, "确认按钮"); 来源图.raycastTarget = true; }
        var 来源字 = 来源键?.GetComponentInChildren<Text>(); if (来源字 != null) 来源字.color = 天帝道纹美术.浅字;
        var 标题 = 预览.GetComponentInChildren<Text>(); if (标题 != null) { 定(标题.rectTransform, 内边, 8, 手机 ? 84 : 200, 44); 标题.fontSize = 手机 ? 18 : 24; }
        if (手机)
        {
            var 行口 = 区块(预览, "通路横滑视口", 内边, 60, 正文宽, 44);
            天帝双端页面布局.固定(行口, 内边, 60, 正文宽, 44);
            var 行图 = 行口.gameObject.AddComponent<Image>(); 行图.color = Color.clear; 行口.gameObject.AddComponent<RectMask2D>();
            var 行 = 区块(行口, "通路横滑内容", 0, 0, 6 * 100, 44); 天帝响应布局.动态(行);
            var 滚 = 行口.gameObject.AddComponent<ScrollRect>(); 滚.viewport = 行口; 滚.content = 行; 滚.horizontal = true; 滚.vertical = false;
            滚.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < 6; i++)
            { int d = (6 - i) % 6; if (通路按钮[d] != null) { 通路按钮[d].transform.SetParent(行, false); 定((RectTransform)通路按钮[d].transform, i * 100, 0, 96, 44); } }
        }
        else
            for (int i = 0; i < 6; i++)
            { int d = (6 - i) % 6; if (通路按钮[d] != null) { 定((RectTransform)通路按钮[d].transform, 内边 + i % 3 * 116, 62 + i / 3 * 54, 108, 46); var 文=通路按钮[d].GetComponentInChildren<Text>();if (文 != null) 文.fontSize=16; } }
        float y = 手机 ? 112 : 178;
        if (单弹字 != null) { 定(单弹字.rectTransform, 内边, y, 正文宽, 50); 单弹字.fontSize = 22; }
        if (射击字 != null) { 定(射击字.rectTransform, 内边, y + 56, 正文宽, 手机 ? 64 : 42); 射击字.fontSize = 14; }
        if (形态字 != null) { 定(形态字.rectTransform, 内边, y + (手机 ? 126 : 104), 正文宽, 手机 ? 76 : 62); 形态字.fontSize = 手机 ? 14 : 16; }
        var 演示 = 预览.Find("攻击形态演示") as RectTransform;
        定(演示, 内边, y + (手机 ? 210 : 178), 正文宽, 手机 ? 80 : 100);
        天帝响应布局.比例((RectTransform)攻击演示.transform, 0, 0, 1, 1);
        float 下 = y + (手机 ? 300 : 288);
        foreach (Transform 子 in 预览)
            if (子.GetComponent<Text>() is Text 文 && 文.text.StartsWith("顺序链路："))
            { 定(文.rectTransform, 内边, 下, 正文宽, 42); 文.fontSize = 14; }
        if (变化底 != null) { 定(变化底.rectTransform, 内边, 下 + 44, 正文宽, 手机 ? 110 : 96); 变化底.color = new Color(.30f, .54f, .48f, .6f); }
        if (对比字 != null) { 定(对比字.rectTransform, 内边, 下 + 48, 正文宽, 手机 ? 110 : 94); 对比字.fontSize = 14; }
        if (连接字 != null) { 定(连接字.rectTransform, 内边, 下 + (手机 ? 164 : 148), 正文宽, 86); 连接字.fontSize = 14; }
        if (手机)
        {
            var 说明 = 根.Find("操作说明") as RectTransform; if (说明 == null) 说明 = 区块(根,"操作说明",0,0,100,44); 说明.SetParent(预览, false); 说明.gameObject.SetActive(true);
            定(说明, 8, 下 + 236, 信息宽 - 16, 44); var 说明字 = 说明.GetComponentInChildren<Text>(); if (说明字 != null) 说明字.text = "操作说明";
            图例.transform.SetParent(预览, false);
            定(图例.rectTransform, 8, 下 + 284, 信息宽 - 16, 44);
            图例.text = "<color=#235641>━ 已激活</color>  <color=#9A3529>━ 未激活</color>\n<color=#59675D>━ 空位</color>";
            定(预览, 0, 0, 信息宽, 下 + 336);
            触屏旋转键 = 按钮(根, "旋转60°", 116, 308, 100, 44, () => 触屏旋转());
            触屏卸下键 = 按钮(根, "卸下道纹", 220, 308, 100, 44, () => 触屏卸下());
            触屏取消键 = 按钮(根, "取消选择", 324, 308, 100, 44, 取消触屏选择);
            按钮(根, "缩小 −", 428, 308, 100, 44, () => 触屏缩放(.8f));
            按钮(根, "放大 +", 532, 308, 100, 44, () => 触屏缩放(1.25f));
            foreach (string 名 in new[] { "旋转60°", "卸下道纹", "取消选择", "缩小 −", "放大 +" })
                键(名, ((RectTransform)根.Find(名)).anchoredPosition.x, 308, 100, 44);
            更新触屏操作();
        }
        else 定(预览, 0, 0, 信息宽, 下 + 244);
        键("旋转 · R", 手机 ? 330 : 1010, 手机 ? 226 : 816, 手机 ? 100 : 180, 44);
        更新悬浮配色();
        if (天帝剪纸界面皮肤.已启用 && !手机)
        {
            天帝剪纸界面皮肤.标题(根,"构筑");
            定(成长字.rectTransform,350,4,962,50);
        }
    }

    void 排悬浮候选卡(int 索引, int 槽)
    {
        if(山水构筑){排山水候选卡(索引,槽);return;}
        bool 手机 = 天帝移动适配.启用; float 宽 = 手机 ? 100 : 192, 高 = 手机 ? 104 : 114;
        var 卡 = (RectTransform)候选卡[索引].transform;
        天帝双端页面布局.固定(卡, 0, 槽 * (高 + 6), 宽, 高);
        var 底图 = 卡.GetComponent<Image>(); 天帝道纹美术.应用(底图, "小信息框"); 底图.color = 天帝剪纸界面皮肤.已启用 ? Color.white : new Color(.07f, .22f, .24f, .94f); 底图.raycastTarget = true;
        void 定(RectTransform r, float x, float y, float w, float h) => 天帝双端页面布局.固定(r, x, y, w, h);
        定(候选品阶[索引].rectTransform, 6, 2, 宽 - (手机 ? 48 : 42), 24); 候选品阶[索引].fontSize = 14;
        定(候选图[索引].rectTransform, 手机 ? 0 : 6, 手机 ? 26 : 30, 手机 ? 52 : 64, 手机 ? 46 : 58); 候选图[索引].单纹半径 = 手机 ? 20 : 26;
        候选短名[索引].fontSize = 手机 ? 20 : 24;
        var 框 = 卡.Find("道纹选中框") as RectTransform; 定(框, 手机 ? 0 : 6, 手机 ? 26 : 30, 手机 ? 52 : 64, 手机 ? 46 : 58);
        候选说明[索引].gameObject.SetActive(!手机);
        定(候选说明[索引].rectTransform, 74, 30, 宽 - 80, 64); 候选说明[索引].fontSize = 14; 候选说明[索引].alignment = TextAnchor.MiddleLeft;
        定(候选状态[索引].rectTransform, 手机 ? 6 : 28, 手机 ? 74 : 88, 宽 - (手机 ? 12 : 34), 手机 ? 28 : 24); 候选状态[索引].fontSize = 14;
        候选状态图[索引].gameObject.SetActive(!手机); 定(候选状态图[索引].rectTransform, 6, 92, 16, 16);
        // 挂锁仍是独立按钮，保留手机44单位触摸区，避开缩略符号。
        var 锁 = (RectTransform)候选锁[索引].transform;
        锁.anchorMin = 锁.anchorMax = 锁.pivot = Vector2.one; 锁.anchoredPosition = new Vector2(-2, -2);
        锁.sizeDelta = Vector2.one * (手机 ? 44 : 32);
    }

    void 更新悬浮配色()
    {
        Color 正文色 = 天帝剪纸界面皮肤.已启用 ? 天帝剪纸界面皮肤.墨 : 天帝道纹美术.浅字;
        foreach (Transform 子 in 根)
            if (子.GetComponent<Text>() is Text 文) 文.color = 正文色;
        foreach (var 文 in new[] { 单弹字, 射击字, 形态字, 对比字, 连接字 }) if (文 != null) 文.color = 正文色;
        var 预览 = 单弹字 != null ? 单弹字.transform.parent : null;
        if (预览 != null) foreach (Transform 子 in 预览)
            if (子.GetComponent<Text>() is Text 文) 文.color = 正文色;
        for (int i = 0; i < 候选卡.Count; i++)
        {
            if (数据 == null || 数据.道纹 == null || i >= 数据.道纹.Count || 数据.道纹[i] == null) continue;
            if (i < 候选短名.Count && 候选短名[i] != null) 候选短名[i].color = 天帝道纹美术.正文;
            if (i < 候选说明.Count && 候选说明[i] != null) 候选说明[i].color = 正文色;
            if (i < 候选状态.Count && 候选状态[i] != null) 候选状态[i].color = 正文色;
            if (i < 候选品阶.Count && 候选品阶[i] != null) 候选品阶[i].text = 天帝剪纸界面皮肤.已启用 ? 天帝道纹品阶.彩色品阶文字(数据.道纹[i].品阶) : 天帝道纹美术.深底品阶文字(数据.道纹[i].品阶);
            if (天帝移动适配.启用)
            {
                var 纹 = 数据.道纹[i];
                if (i < 候选状态.Count && 候选状态[i] != null) 候选状态[i].text = 纹.格子.HasValue ? "已放置" : 纹.是特性道纹 ? "固定机制" : "词条 " + (纹.词条?.Count ?? 0) + "/" + 纹.词条上限;
            }
        }
        if (悬浮候选滚动 != null && 悬浮上次页 != 候选页码)
        {
            悬浮上次页 = 候选页码;
            // 翻页会先切换卡片 active 状态；重建内容尺寸后再归顶，避免桌面端短列表保留旧滚动偏移。
            悬浮候选滚动.StopMovement();
            if (悬浮候选滚动.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(悬浮候选滚动.content);
            Canvas.ForceUpdateCanvases();
            悬浮候选滚动.verticalNormalizedPosition = 1;
        }
        if (页码字 != null) 页码字.text = (候选页码 + 1) + " / " + 候选总页数 + " · " + 筛选结果数 + "枚";
        if (空列表提示 != null) 空列表提示.color = 正文色;
        foreach (var 键 in new[] { 定位键, 撤销键, 拖转键 })
            if (键 != null) { var 文=键.GetComponentInChildren<Text>();if (文 != null) 文.color = 键.interactable ? 天帝道纹美术.浅字 : new Color(.66f, .71f, .63f); }
    }
}
