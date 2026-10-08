#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static partial class 天帝剩余概念验收
{
    static IEnumerator 首四页自检()
    {
        游戏.界面.跳过新手指引();
        检查("01正式主页入口", 游戏.阶段 == 游戏阶段.主页);
        var 导航 = 游戏.GetComponentsInChildren<天帝剪纸导航按钮>();
        var 轻纸 = Resources.Load<Sprite>("剪纸界面/轻纸框");
        检查("01七导航保留清爽轻纸与原图标", 导航.Length == 7 && 导航.All(x => x.GetComponent<Image>().sprite == 轻纸));
        var 等级 = 游戏.GetComponentsInChildren<Text>().First(x => x.name == "地图等级实时值");
        var 开始 = 游戏.GetComponentsInChildren<Button>().First(x => x.name == "开始历练");
        检查("01等级与开始文案均居中", 等级.alignment == TextAnchor.MiddleCenter && 开始.GetComponentInChildren<Text>().alignment == TextAnchor.MiddleCenter);
        yield return 拍("01_主页清爽自检");
        点("道纹改造"); yield return null; yield return new WaitForEndOfFrame();
        var 改造 = 游戏.界面.改造页;
        检查("02主页实际打开改造", 游戏.阶段 == 游戏阶段.道纹改造 && 改造 != null);
        var 材料 = 改造.GetComponentsInChildren<Button>().Where(x => x.name.StartsWith("通货-")).ToArray();
        检查("02十一材料仍用轻纸", 材料.Length == 11 && 材料.All(x => ((Image)x.targetGraphic).sprite == 轻纸));
        检查("02禁用只整卡半透明", 材料.Where(x => !x.interactable).All(x => x.GetComponent<CanvasGroup>().alpha == .5f && x.colors.disabledColor == Color.white));
        检查("02选择道纹正确文案可见", 改造.GetComponentsInChildren<Text>().Any(x => x.text == "点击这里，选择道纹" && x.cachedTextGenerator.vertexCount > 0));
        yield return 拍("02_道纹改造清爽自检"); 点("关闭通货"); yield return null; yield return new WaitForEndOfFrame();
        点("道纹"); yield return null;
        检查("03主页真实打开构筑", 游戏.阶段 == 游戏阶段.道纹 && 游戏.界面.道纹页 != null);
        检查("03构筑保留独立山水背景", 游戏.界面.道纹页.GetComponentsInChildren<Image>().Any(x => x.sprite != null && x.sprite.name == "构筑背景"));
        检查("03构筑保留真实六边网格输入", 游戏.界面.道纹页.画布.GetComponent<天帝道纹输入>() != null);
        yield return 拍("03_道纹构筑清爽自检"); 点("返回主页"); yield return null; yield return new WaitForEndOfFrame();
        点("角色"); yield return null; yield return new WaitForEndOfFrame();
        var 角色 = 游戏.界面.角色页;
        检查("04真实角色页与四页签", 游戏.界面.角色已打开 && 角色.GetComponentsInChildren<Button>().Count(x => x.name.StartsWith("页签-")) == 4);
        foreach (var 名 in new[] { "角色属性", "战斗进阶", "技能与形态", "道纹分布" })
        {
            点("页签-" + 名); yield return null; yield return new WaitForEndOfFrame();
            检查("04真实页签正文存在-" + 名, 角色.GetComponentsInChildren<Text>().Any(x => !string.IsNullOrEmpty(x.text) && x.cachedTextGenerator.vertexCount > 0));
            yield return 拍("04_角色清爽自检_" + 名);
        }
        点("关闭角色"); yield return null;
        检查("首四页自检正常返回主页", 游戏.阶段 == 游戏阶段.主页 && !游戏.界面.角色已打开);
    }
}
#endif
