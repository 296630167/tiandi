#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static partial class 天帝剩余概念验收
{
    static GameObject 弹根() => (GameObject)typeof(天帝界面).GetField("根", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(游戏.界面);
    static T[] 弹组件<T>() where T : Component => 弹根().GetComponentsInChildren<T>();
    static Button 弹键(string 名, string 父 = null)
        => 弹组件<Button>().First(b => b.name == 名 && (父 == null || b.transform.parent.name == 父));
    static Image 弹图(string 名) => 弹组件<Image>().First(i => i.name == 名);
    static void 弹字容纳(string 前缀, params string[] 名称)
    {
        Canvas.ForceUpdateCanvases();
        foreach (string 名 in 名称)
        {
            var 字 = 弹组件<Text>().First(t => t.name == 名);
            检查(前缀 + "真实文字完整容纳-" + 名, 字.preferredHeight <= 字.rectTransform.rect.height + .5f);
        }
    }
    static void 弹点(string 名, string 父)
    {
        var 键 = 弹键(名, 父); Canvas.ForceUpdateCanvases();
        var 画布 = 键.GetComponentInParent<Canvas>();
        var 中心 = ((RectTransform)键.transform).TransformPoint(((RectTransform)键.transform).rect.center);
        var 指针 = new PointerEventData(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(画布.renderMode == RenderMode.ScreenSpaceOverlay ? null : 画布.worldCamera, 中心), button = PointerEventData.InputButton.Left };
        var 命中 = new List<RaycastResult>(); EventSystem.current.RaycastAll(指针, 命中);
        bool 可达 = 命中.Count > 0 && (命中[0].gameObject == 键.gameObject || 命中[0].gameObject.transform.IsChildOf(键.transform));
        检查("实际射线可点 " + 父 + "/" + 名, 键.interactable && 可达);
        if (!键.interactable || !可达) throw new Exception("按钮未通过真实射线：" + 父 + "/" + 名);
        ExecuteEvents.Execute(键.gameObject, 指针, ExecuteEvents.pointerClickHandler);
    }
    static IEnumerator 弹窗验证()
    {
        检查("弹窗验收从真实主页开始", 游戏.阶段 == 游戏阶段.主页);
        点("宝盒"); yield return null;
        检查("06宝盒打开真实三种宝盒卡", 游戏.界面.宝盒已打开 && new[] { "属性宝盒", "功能宝盒", "分叉宝盒" }.All(n => 弹组件<Image>().Any(i => i.name == n)));
        var 宝盒底 = Resources.Load<Sprite>("山水剩余界面/宝盒背景");
        var 红叶 = Resources.Load<Sprite>("山水剩余界面/红叶弹窗");
        检查("06宝盒使用新独立背景非旧截图或fallback", 宝盒底 != null && 弹图("宝盒山水背景").sprite == 宝盒底);
        检查("06三盒独立插画真实接入", 弹组件<Image>().Count(i => i.name == "宝盒独立插画" && i.sprite != null) == 3);
        检查("06三卡使用新独立红叶纸窗", 红叶 != null && new[] { "属性宝盒", "功能宝盒", "分叉宝盒" }.All(n => 弹图(n).sprite == 红叶));
        yield return 拍("06_宝盒_三种真实卡片");
        int 余额前 = 游戏.宝盒数据.灵石, 库存前 = 游戏.道纹数据.道纹.Count;
        for (int i = 0; i < 3; i++)
        {
            点("宝盒概率-" + i); yield return null;
            检查("06真实概率独立弹层-" + i, 游戏.界面.宝盒概率已打开 && 弹图("概率面板").sprite == 红叶);
            string 断言 = i == 2 ? 天帝宝盒.分叉概率()[0] + "%" : (天帝宝盒.品阶概率((宝盒种类)i).First(x => x > 0) / 100f).ToString("0.##") + "%";
            检查("06概率来自正式配置-" + i, 弹组件<Text>().Any(t => t.text.Contains(断言)));
            弹字容纳("06概率-" + i, "概率品阶列", "概率百分列");
            yield return 拍("06_概率_" + new[] { "属性", "功能", "分叉" }[i]);
            点("返回宝盒"); yield return null;
            检查("06关闭概率不消费不发道纹-" + i, !游戏.界面.宝盒概率已打开 && 游戏.宝盒数据.灵石 == 余额前 && 游戏.道纹数据.道纹.Count == 库存前);
        }
        检查("06属性宝盒验收余额足够", 余额前 >= 天帝宝盒.价格(宝盒种类.属性));
        弹点("抽取", "属性宝盒"); yield return null;
        检查("06真实抽取扣费并入库一枚", 游戏.宝盒数据.灵石 == 余额前 - 天帝宝盒.价格(宝盒种类.属性) && 游戏.道纹数据.道纹.Count == 库存前 + 1);
        检查("06真实抽取结果和悬停详情接通", 弹组件<Text>().Any(t => t.name == "宝盒结果标题" && t.text.Contains("本次获得")) && 弹组件<天帝宝盒日志悬停>().Length > 0);
        foreach (var 键 in 弹组件<Button>().Where(b => b.name == "抽取"))
            检查("06禁用只透明度保留按钮素材-" + 键.transform.parent.name,
                键.GetComponent<CanvasGroup>().alpha == (键.interactable ? 1f : .5f) && (键.targetGraphic as Image).color == Color.white && 键.colors.disabledColor == Color.white);
        yield return 拍("06_宝盒_真实抽取后");
        int 抽后余额 = 游戏.宝盒数据.灵石, 抽后库存 = 游戏.道纹数据.道纹.Count;
        点("关闭"); yield return null;
        检查("06关闭宝盒不追加抽取", !游戏.界面.宝盒已打开 && 游戏.宝盒数据.灵石 == 抽后余额 && 游戏.道纹数据.道纹.Count == 抽后库存);

        点("设置"); yield return null;
        检查("10设置使用批准独立红叶窗", 游戏.界面.设置已打开 && 弹图("设置面板").sprite == 红叶);
        var 滑条 = 弹组件<Slider>(); 检查("10四个真实音量滑条齐全", 滑条.Length == 4);
        float[] 原值 = { 游戏.音量, 游戏.音乐音量, 游戏.音效音量, 游戏.剧情音量 };
        string[] 滑名 = { "总音量", "音乐", "音效", "剧情声音" };
        Func<int, float> 模型值 = i => i == 0 ? 游戏.音量 : i == 1 ? 游戏.音乐音量 : i == 2 ? 游戏.音效音量 : 游戏.剧情音量;
        for (int i = 0; i < 滑名.Length; i++)
        { var 滑 = 滑条.First(s => s.name == 滑名[i]); 滑.value = .37f; 检查("10音量实时驱动模型-" + 滑名[i], Mathf.Abs(模型值(i) - .37f) < .001f); 滑.value = 原值[i]; }
        var 重看 = 弹键("重看序章"); 检查("10未解锁重看只半透明", 重看.interactable || 重看.GetComponent<CanvasGroup>().alpha == .5f);
        yield return 拍("10_设置_真实音量和字幕"); 点("关闭"); yield return null;
        检查("10关闭设置并恢复真实值", !游戏.界面.设置已打开 && 原值.Select((v, i) => Mathf.Abs(v - 模型值(i)) < .001f).All(x => x));

        点("作弊码"); yield return null;
        检查("10作弊码独立输入与反馈窗", 游戏.界面.作弊码已打开 && 弹图("作弊码面板").sprite == 红叶 && 弹组件<InputField>().Length == 1);
        var 输入 = 弹组件<InputField>().Single(); 输入.text = "不是有效作弊码";
        int 作弊前余额 = 游戏.宝盒数据.灵石; var 作弊前货 = 游戏.通货数据.导出库存();
        bool 原无限石 = 游戏.宝盒数据.无限灵石, 原无限货 = 游戏.通货数据.无限通货;
        点("启用"); yield return null;
        检查("10无效码显示实际失败反馈", 弹组件<Text>().Any(t => t.name == "作弊码反馈" && !string.IsNullOrEmpty(t.text) && t.text != "启用后在此显示实际结果。"));
        检查("10无效码无余额库存或无限模式变化", 游戏.宝盒数据.灵石 == 作弊前余额 && 游戏.通货数据.导出库存().SequenceEqual(作弊前货) && 游戏.宝盒数据.无限灵石 == 原无限石 && 游戏.通货数据.无限通货 == 原无限货);
        yield return 拍("10_作弊码_无效反馈"); 点("关闭"); yield return null;
        检查("10关闭作弊返回主页", !游戏.界面.作弊码已打开 && 游戏.阶段 == 游戏阶段.主页);

        游戏.界面.显示新游戏确认(); yield return null;
        var 绿框 = Resources.Load<Sprite>("山水剩余界面/绿边确认窗");
        检查("11新游戏确认独立绿边纸窗", 绿框 != null && 游戏.界面.确认已打开 && 弹图("确认面板").sprite == 绿框);
        yield return 拍("11_开始新游戏_取消前"); 点("取消"); yield return null;
        检查("11取消新游戏保留当前模型", !游戏.界面.确认已打开 && 游戏.阶段 == 游戏阶段.主页 && 游戏.道纹数据.道纹.Count == 抽后库存);
        点("开始历练"); yield return null;
        检查("11入图确认真实规则与敌人类别", 游戏.界面.确认已打开 && 弹图("确认面板").sprite == 绿框 && 弹组件<Image>().Count(i => i.name.StartsWith("敌人类别-")) == 4);
        yield return 拍("11_青岚原_确认前"); 点("取消"); yield return null;
        检查("11取消入图不加载战斗", 游戏.阶段 == 游戏阶段.主页 && 游戏.战斗场景 == null && !游戏.界面.确认已打开);

        点("开始历练"); yield return null; 点("确认进入");
        for (int i = 0; i < 900 && 游戏.阶段 == 游戏阶段.战斗加载; i++) yield return null;
        检查("13通过真实确认进入实际青岚原场景", 游戏.阶段 == 游戏阶段.战斗 && 游戏.战斗场景?.战斗 != null);
        if (游戏.阶段 != 游戏阶段.战斗 || 游戏.战斗场景?.战斗 == null) throw new Exception("无法加载真实青岚原场景，不能验证战斗弹窗");
        // 战斗HUD在LateUpdate提交锚点，再等待一帧后用最终矩形做实际射线。
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null;
        游戏.界面.更新适配(); Canvas.ForceUpdateCanvases(); yield return null;
        点(天帝移动适配.启用 ? "暂停" : "暂停 Esc"); yield return null;
        var 松窗 = Resources.Load<Sprite>("山水剩余界面/松山弹窗");
        检查("13暂停使用新独立松山纸窗", 松窗 != null && 游戏.界面.战斗已暂停 && 弹图("暂停详情").sprite == 松窗);
        float 血前 = 游戏.主角属性.当前血量; int 经前 = 游戏.战斗场景.战斗.本局经验;
        游戏.战斗场景.战斗一步(.5f);
        检查("13暂停冻结真实战斗推进", 游戏.主角属性.当前血量 == 血前 && 游戏.战斗场景.战斗.本局经验 == 经前);
        检查("13暂停战况攻击收获均为可编辑真实文字", new[] { "当前战况", "当前攻击", "本局收获" }.All(n => 弹组件<Text>().Any(t => t.text == n)) && 弹组件<Text>().Any(t => t.text.Contains("地图等级 " + 游戏.当前地图等级)));
        弹字容纳("13暂停", "暂停攻击左列", "暂停攻击右列");
        yield return 拍("13_战斗暂停_真实场景"); 点("继续战斗"); yield return null;
        检查("13继续战斗关闭实际暂停层", !游戏.界面.战斗已暂停 && !弹组件<Image>().Any(i => i.name == "暂停详情"));
        int 失败前库存 = 游戏.道纹数据.道纹.Count, 失败前石 = 游戏.宝盒数据.灵石; var 失败前货 = 游戏.通货数据.导出库存();
        游戏.战斗场景.战斗.伤害玩家(1000000f); 游戏.战斗场景.战斗一步(.01f); yield return null;
        检查("13真实死亡触发失败纸窗", 游戏.战斗场景.战斗.玩家死亡 && 弹图("战斗失败面板").sprite == 松窗);
        yield return 拍("13_身陨此地_真实场景"); 弹点("返回主页", "战斗失败面板");
        for (int i = 0; i < 900 && 游戏.阶段 == 游戏阶段.战斗加载; i++) yield return null;
        检查("13失败离场回到主页且保留实际资源", 游戏.阶段 == 游戏阶段.主页 && 游戏.战斗场景 == null && 游戏.道纹数据.道纹.Count == 失败前库存 && 游戏.宝盒数据.灵石 == 失败前石 && 游戏.通货数据.导出库存().SequenceEqual(失败前货));
        检查("所有弹窗退出后无残留模态状态", !游戏.界面.宝盒已打开 && !游戏.界面.宝盒概率已打开 && !游戏.界面.确认已打开 && !游戏.界面.设置已打开 && !游戏.界面.作弊码已打开 && !游戏.界面.战斗已暂停);
    }
}
#endif
