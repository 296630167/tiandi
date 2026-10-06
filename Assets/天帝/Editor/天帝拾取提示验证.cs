#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝拾取提示验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    public static string 运行()
    {
        var 报 = new 报告();
        Action<string, bool> 检查 = (名, 对) => (对 ? 报.通过 : 报.失败).Add(名);
        var 父 = new GameObject("通知验证", typeof(RectTransform)); 天帝拾取提示 列 = null;
        try
        {
            var 字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
            列 = new 天帝拾取提示((RectTransform)父.transform, 字体);
            var 随机 = new System.Random(9);
            for (int i = 0; i < 9; i++)
            {
                var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, 随机);
                纹.词条.Clear(); 纹.词条.Add(new 道纹词条(道纹属性.力量, 100 + i));
                列.加入(纹); 纹.词条[0].数值 = 999; // 排队内容不得随后续改造变化。
            }
            var 首 = 父.GetComponentsInChildren<CanvasGroup>().Single();
            检查("连续九枚逐条入队无覆盖", 列.显示数 == 1 && 列.等待数 == 8);
            检查("入场从左侧透明开始且不拦截输入", ((RectTransform)首.transform).anchoredPosition.x < 0 && 首.alpha == 0 && !首.blocksRaycasts && !首.interactable);
            列.更新(.125f); 检查("滑入中可观测位置与透明度", 首.alpha > 0 && 首.alpha < 1 && ((RectTransform)首.transform).anchoredPosition.x < 28);
            列.更新(.375f); 检查("最多四条可见其余五条排队", 列.显示数 == 4 && 列.等待数 == 5);
            检查("每枚提示保留拾取瞬间快照", 父.GetComponentsInChildren<Text>().Where(t => t.text.Contains("接口")).All(t => !t.text.Contains("999")));
            检查("中文品阶名称与属性行高度足够无截断", 父.GetComponentsInChildren<Text>().All(t => t.preferredHeight <= t.rectTransform.rect.height + .01f));
            检查("滑入后停留位置与不透明度稳定", 首.alpha == 1 && ((RectTransform)首.transform).anchoredPosition.x == 28);
            列.更新(3.7f); 检查("四秒完整停留不因后续拾取提前退场", 首 != null && 首.alpha == 1 && 列.显示数 == 4 && 列.等待数 == 5);
            列.更新(.15f); 检查("停留完成向左淡出", 首 != null && 首.alpha > 0 && 首.alpha < 1 && ((RectTransform)首.transform).anchoredPosition.x < 28);
            列.更新(.16f); 检查("退场真正销毁且下一枚开始独立计时", 首 == null && 列.等待数 == 4 && 列.显示数 == 4);
            var 已见 = new HashSet<int> { 100, 101, 102, 103, 104 };
            for (int i = 0; i < 210; i++)
            {
                列.更新(.05f);
                foreach (var t in 父.GetComponentsInChildren<Text>()) for (int n = 100; n < 109; n++) if (t.text.Contains("+" + n)) 已见.Add(n);
            }
            检查("所有排队条目均实际显示后清空并销毁", 已见.Count == 9 && 列.等待数 == 0 && 列.显示数 == 0 && 父.GetComponentsInChildren<CanvasGroup>().Length == 0);
            列.加入(天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, 随机)); 列.Dispose(); 列.Dispose();
            列.加入(天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, 随机)); 列.更新(1);
            检查("换页销毁幂等并拒绝残留事件入队", 父.transform.childCount == 0 && 列.显示数 == 0 && 列.等待数 == 0);
            列 = new 天帝拾取提示((RectTransform)父.transform, 字体, .1f); 列.加入(天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, 随机)); 列.更新(.61f);
            检查("自定义停留N秒生效", 列.显示数 == 0);
            foreach (通货种类 t in 天帝通货.可用种类) 列.加入(t, 6);
            var 已见货 = new HashSet<通货种类>(); bool 无截断 = true;
            for (int i = 0; i < 110; i++)
            {
                列.更新(.05f);
                foreach (var t in 父.GetComponentsInChildren<Text>())
                {
                    无截断 &= t.preferredHeight <= t.rectTransform.rect.height + .01f;
                    foreach (通货种类 种 in 天帝通货.可用种类) if (t.text.Contains(种 + " ×6")) 已见货.Add(种);
                }
            }
            检查("十一种通货逐条展示名字与数量", 已见货.Count == 11 && 列.显示数 == 0 && 列.等待数 == 0);
            检查("最长通货用途摘要无截断", 无截断);
        }
        catch (Exception ex) { 报.错误.Add(ex.ToString()); }
        finally { 列?.Dispose(); UnityEngine.Object.DestroyImmediate(父); }
        var 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/拾取提示-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        File.WriteAllText(Path.Combine(目录, "pickup-toast.json"), JsonUtility.ToJson(报, true)); return 目录;
    }
}
#endif
