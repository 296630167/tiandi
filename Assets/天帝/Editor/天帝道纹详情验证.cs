#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝道纹详情验证
{
    [MenuItem("天帝/验证道纹详情")]
    static void 菜单验证() => Debug.Log(运行());

    public static string 运行()
    {
        void 检查(bool 成立, string 名称)
        { if (!成立) throw new InvalidOperationException("道纹详情验证失败：" + 名称); }

        var 根 = new GameObject("道纹详情验证", typeof(RectTransform));
        try
        {
            var 卡 = 根.AddComponent<天帝道纹详情卡>();
            卡.初始化(AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf"));
            var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.稀有, new System.Random(7));
            纹.词条.Clear(); 纹.词条.Add(new 道纹词条(道纹属性.力量, 3));
            卡.设置(纹);
            var 文 = 根.GetComponentsInChildren<Text>(true).Select(x => x.text).ToArray();
            检查(卡.当前道纹 == 纹 && 卡.高度 > 350 && 卡.高度 < 780, "属性卡片尺寸");
            检查(文.Contains("属性词条  1 / 3") && 文.Contains("基础属性 · 力量 +3") && 文.Contains("空词条位") && 文.Contains("可改造"), "品阶容量与空词条位");
            int 口数 = Enumerable.Range(0, 6).Count(纹.有接口);
            检查(文.Contains("接口  " + 口数 + " / 6") &&
                !根.GetComponentsInChildren<RectTransform>(true).Any(x => x.name.StartsWith("接口-")), "接口只显示数量");
            纹.词条.Clear(); 纹.词条.Add(new 道纹词条(道纹属性.火, 3)); 卡.设置(纹);
            文 = 根.GetComponentsInChildren<Text>(true).Select(x => x.text).ToArray();
            检查(文.Contains("属性词条  1 / 1") && 文.Contains("火属性额外伤害 +3") &&
                !文.Contains("空词条位"), "五行单词条详情");
            var 源 = 天帝道纹.创建天赋源纹(天帝天赋.获取((int)天赋种类.普通人));
            卡.设置(源);
            文 = 根.GetComponentsInChildren<Text>(true).Select(x => x.text).ToArray();
            检查(卡.高度 <= 820 && 文.Any(x => x.Contains("每10级解封一个") && x.Contains("50级")), "源纹解封条件与高度");
            return "道纹详情验证通过：品阶、词条、接口数量、源纹解封与尺寸";
        }
        finally { UnityEngine.Object.DestroyImmediate(根); }
    }
}
#endif
