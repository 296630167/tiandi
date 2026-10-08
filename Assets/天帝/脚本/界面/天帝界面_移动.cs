using UnityEngine;

public partial class 天帝界面
{
    public void 移动失去焦点()
    {
        if (!天帝移动适配.启用) return;
        触控跑步 = false; 战斗摇杆?.重置输入(); 道纹页?.取消触屏选择();
        if (游戏.阶段 == 游戏阶段.战斗 && !战斗已暂停 && 游戏.战斗场景?.战斗?.玩家死亡 == false) 切换战斗暂停();
    }
    void 布局主页()
    {
        if (天帝青绿皮肤.已启用) return;
        if (!天帝移动适配.启用) return;
        void 区(string 名, float x, float y, float w, float h)
        { var r = 页面.Find(名) as RectTransform; if (r != null) 天帝响应布局.比例(r, x, y, w, h); }
        区("主页导航留白", 0, 0, .24f, 1);
        string[] 导航 = { "角色", "道纹", "道纹改造", "道纹图鉴", "宝盒", "道纹回收", "作弊码" };
        for (int i = 0; i < 导航.Length; i++)
        {
            区(导航[i], .012f, .025f + i * .137f, .205f, .127f);
            var 键 = 页面.Find(导航[i]); if (键 == null) continue;
            var 文 = 键.GetComponentInChildren<UnityEngine.UI.Text>(); if (文 != null) 天帝响应布局.比例(文.rectTransform, .34f, .05f, .62f, .90f);
            var 图 = 键.Find("导航图标-" + 导航[i]) as RectTransform; if (图 != null) 天帝响应布局.比例(图, .12f, .22f, .18f, .56f);
        }
        区("主角立绘", .23f, .06f, .36f, .73f);
        区("主角落地阴影", .32f, .72f, .19f, .035f);
        区("主页灵石纸面", .62f, .015f, .20f, .105f);
        区("设置", .825f, .015f, .16f, .105f);
        区("主页常驻选图", .615f, .15f, .375f, .835f);
        var 驱动 = 页面.GetComponent<天帝移动排版>() ?? 页面.gameObject.AddComponent<天帝移动排版>();
        驱动.排版 = 面板 =>
        {
            foreach (string 名 in 导航)
            { var 文 = 面板.Find(名)?.GetComponentInChildren<UnityEngine.UI.Text>(); if (文 != null) 文.fontSize = 16; }
            var 地图区 = 面板.Find("主页常驻选图") as RectTransform;
            float 宽 = 地图区.rect.width, 卡宽 = (宽 - 8) / 3;
            for (int i = 0; i < 3; i++)
            {
                var 卡 = 地图区.Find("地图卡" + (i + 1)) as RectTransform;
                if (卡 == null) continue;
                天帝双端页面布局.固定(卡, i * (卡宽 + 4), 0, 卡宽, 卡宽 + 36);
                foreach (var 文 in 卡.GetComponentsInChildren<UnityEngine.UI.Text>())
                {
                    文.fontSize = 14;
                    if (文.text == "尚未开放") { 文.gameObject.SetActive(false); continue; }
                    天帝双端页面布局.固定(文.rectTransform, 2, 文.text == "封印" ? 20 : 卡宽 - 4, 卡宽 - 4, 40);
                }
            }
            var 信息 = 地图区.Find("地图信息面板") as RectTransform;
            if (信息 == null) return;
            天帝双端页面布局.固定(信息, 0, 卡宽 + 38, 宽, 地图区.rect.height - 卡宽 - 38);
            float y = 4;
            foreach (Transform 子 in 信息)
            {
                var 文 = 子.GetComponent<UnityEngine.UI.Text>(); if (文 == null || 文 == 主页地图等级字) continue;
                if (文.text == "战斗与掉落详情将在进入前展示") { 文.gameObject.SetActive(false); continue; }
                文.fontSize = 文.text == "青岚原" ? 22 : 14;
                if (文.text == "地图等级") { 天帝双端页面布局.固定(文.rectTransform, 12, 信息.rect.height - 98, 74, 44); continue; }
                天帝双端页面布局.固定(文.rectTransform, 12, y, 宽 - 24, 100);
                float 高 = Mathf.Ceil(文.preferredHeight) + 4; 天帝双端页面布局.固定(文.rectTransform, 12, y, 宽 - 24, 高); y += 高 + 4;
            }
            信息.Find("内容分隔")?.gameObject.SetActive(false);
            if (地图等级下拉 != null)
            {
                天帝双端页面布局.固定((RectTransform)地图等级下拉.transform, 90, 信息.rect.height - 98, 宽 - 102, 44);
                if (地图等级下拉.captionText != null) 天帝响应布局.比例(地图等级下拉.captionText.rectTransform, .04f, 0, .78f, 1);
                foreach (Transform 子 in 地图等级下拉.transform)
                    if (子.GetComponent<UnityEngine.UI.Text>() is UnityEngine.UI.Text 文 && 文 != 地图等级下拉.captionText)
                    { 天帝双端页面布局.固定(文.rectTransform, 宽 - 128, 0, 20, 44); 文.fontSize = 14; }
            }
            var 开始 = 信息.Find("开始游戏") as RectTransform;
            if (开始 != null) 天帝双端页面布局.按键(开始, 12, 信息.rect.height - 48, 宽 - 24);
        };
    }
}
