#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 在独立PreviewScene中检查，不操作正在编辑的场景、Play Mode或玩家存档。
[InitializeOnLoad]
public static class 天帝道纹画布预览验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 根 => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static 天帝道纹画布预览验证() { EditorApplication.update += 检查请求; }
    static void 检查请求()
    {
        var 请求 = Path.Combine(根, "生成/验证/道纹画布诊断请求.txt");
        if (!File.Exists(请求)) { EditorApplication.update -= 检查请求; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= 检查请求; File.Delete(请求); 运行();
    }
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static 天帝道纹 网(int 等级 = 1)
    {
        var 原 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人)); 原.设置玩家等级(等级);
        var 存 = 原.导出存档(); 存.技能点 = 100; return 天帝道纹.读取存档(存);
    }
    static 道纹实例 加(天帝道纹 网, 道纹属性 属性, int 数值, int 接口, Vector2Int? 格 = null)
    {
        var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(42), 道纹属性分组.基础);
        纹.词条.Clear(); 纹.词条.Add(new 道纹词条(属性, 数值)); 纹.接口 = 接口; 网.获得道纹(纹);
        if (格.HasValue) { 网.解锁格子(格.Value); if (!网.放置(纹, 格.Value)) throw new Exception("夹具放置失败"); }
        return 纹;
    }
    [MenuItem("天帝/验证/道纹连接与构筑预览")]
    public static void 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        结果 = new 报告(); var 目录 = Path.Combine(根, "生成/验证/道纹连接预览-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        try { 模型检查(); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        try { 界面检查(目录); } catch (Exception ex) { 结果.错误.Add(ex.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
    }
    static void 模型检查()
    {
        var 图 = 网(); var a = 加(图, 道纹属性.力量, 4, 9, new Vector2Int(1, 0));
        var b = 加(图, 道纹属性.数量, 2, 9, new Vector2Int(2, 0));
        var 诊 = new 天帝道纹连接诊断(图);
        检查("有效通路从源纹逐格到目标", 诊.路径(b).SequenceEqual(new[] { Vector2Int.zero, new Vector2Int(1, 0), new Vector2Int(2, 0) }));
        string 原档 = JsonUtility.ToJson(图.导出存档()); int 事件数 = 0; 图.状态改变 += () => 事件数++;
        var 转后 = 诊.预览(a, null, true);
        检查("旋转预览能预测自身及下游断开", 转后.生效数 == 0 && 图.生效数 == 2);
        var 拿后 = 诊.预览(a, null, false);
        检查("回收预览只移除副本节点并使下游失效", !拿后.道纹[0].格子.HasValue && !拿后.道纹[1].生效);
        图.解锁格子(new Vector2Int(3, 0)); var c = 加(图, 道纹属性.连锁, 2, 9);
        原档 = JsonUtility.ToJson(图.导出存档()); 事件数 = 0;
        var 放后 = new 天帝道纹连接诊断(图).预览(c, new Vector2Int(3, 0), false);
        检查("放置预览接通新节点", 放后.生效数 == 3 && !c.格子.HasValue);
        检查("预览不触发原网事件且存档技能点接口不变", 事件数 == 0 && 原档 == JsonUtility.ToJson(图.导出存档()));
        检查("占用格及锁定格拒绝预览", 诊.预览(c, new Vector2Int(1, 0), false) == null && 诊.预览(c, new Vector2Int(4, 0), false) == null);
        var 真 = 天帝道纹连接诊断.副本(图); 真.放置(真.道纹.Find(x => x.编号 == c.编号), new Vector2Int(3, 0));
        检查("预览汇总与实际操作一致", 真.生效加成.SequenceEqual(放后.生效加成));
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 放后))
        using (var 实际人 = new 天帝主角属性(天帝普攻.主角配置(), 真))
            检查("预览读取真实普攻伤害与形态", Mathf.Abs(普攻参数.读取(放后, 人).伤害 - 普攻参数.读取(真, 实际人).伤害) < .0001f && 普攻参数.读取(放后, 人).数量 == 3 && 普攻参数.读取(放后, 人).连锁 == 2);
        var 错口网 = 网(); var 错口 = 加(错口网, 道纹属性.力量, 1, 1, new Vector2Int(1, 0));
        检查("未对接能指出本纹缺失方向", new 天帝道纹连接诊断(错口网).说明(错口).Contains("左口未启用"));
        var 封印 = 加(错口网, 道纹属性.智力, 1, 16, new Vector2Int(0, 1));
        检查("源纹封印显示对应解封等级", new 天帝道纹连接诊断(错口网).说明(封印).Contains("50级"));
        var 孤 = 加(错口网, 道纹属性.智力, 1, 9, new Vector2Int(5, 0));
        检查("无邻纹显示独立原因", new 天帝道纹连接诊断(错口网).说明(孤).Contains("独立道纹"));
        var 邻 = 加(错口网, 道纹属性.智力, 1, 9, new Vector2Int(6, 0));
        检查("互接分支未连源单独解释", new 天帝道纹连接诊断(错口网).说明(邻).Contains("分支未接源纹"));
        var 返 = 网(); 加(返, 道纹属性.力量, 1, 63, new Vector2Int(1, 0)); 加(返, 道纹属性.力量, 1, 63, new Vector2Int(2, -1));
        var 内 = 加(返, 道纹属性.力量, 1, 1, new Vector2Int(1, -1));
        检查("物理连通但向内返回会解释圈数阻断", !内.生效 && new 天帝道纹连接诊断(返).说明(内).Contains("第2圈返回第1圈"));
        bool 一致 = true, 合法 = true;
        for (int 种 = 0; 种 < 10; 种++)
        {
            var 随 = new System.Random(种); var 网格 = 网(50);
            for (int q = -3; q <= 3; q++) for (int r = -3; r <= 3; r++) if (q != 0 || r != 0) 加(网格, 道纹属性.力量, 1, 随.Next(1, 64), new Vector2Int(q, r));
            var 结果诊断 = new 天帝道纹连接诊断(网格);
            foreach (var 纹 in 网格.道纹)
            {
                var 路 = 结果诊断.路径(纹); 一致 &= (路.Count > 0) == 纹.生效;
                for (int i = 1; i < 路.Count; i++)
                {
                    var 向 = 路[i] - 路[i - 1]; int d = Array.IndexOf(天帝道纹.邻向, 向);
                    合法 &= d >= 0 && 网格.已放置[路[i - 1]].有接口(d) && 网格.已放置[路[i]].有接口((d + 3) % 6) && 天帝道纹.可传导(路[i - 1], 路[i]);
                }
            }
        }
        检查("10张随机网络的诊断与实际生效一致", 一致); 检查("全部高亮路线遵守接口对接与圈传导", 合法);
    }
    static void 调(天帝道纹界面 页, 道纹实例 纹, Vector2Int? 格, bool 转, bool 收)
        => typeof(天帝道纹界面).GetMethod("更新构筑预览", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(页, new object[] { 纹, 格, 转, 收 });
    static void 界面检查(string 目录)
    {
        Scene 场 = EditorSceneManager.NewPreviewScene(); GameObject 根物 = null, 相机物 = null; RenderTexture 缓冲 = null;
        try
        {
            根物 = new GameObject("道纹预览验证临时画布", typeof(RectTransform), typeof(Canvas)); SceneManager.MoveGameObjectToScene(根物, 场);
            var 根区 = (RectTransform)根物.transform; 根区.sizeDelta = new Vector2(1600, 900); 根物.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var 页物 = new GameObject("道纹页面", typeof(RectTransform), typeof(天帝道纹界面)); 页物.transform.SetParent(根区, false);
            var 页区 = (RectTransform)页物.transform; 页区.anchorMin = Vector2.zero; 页区.anchorMax = Vector2.one; 页区.offsetMin = 页区.offsetMax = Vector2.zero;
            var 数据 = 网(); var a = 加(数据, 道纹属性.力量, 4, 9, new Vector2Int(1, 0)); 加(数据, 道纹属性.数量, 2, 9, new Vector2Int(2, 0));
            加(数据, 道纹属性.连锁, 2, 9, new Vector2Int(3, 0)); 加(数据, 道纹属性.分裂, 1, 9, new Vector2Int(4, 0));
            var 候 = 加(数据, 道纹属性.范围, 2, 9); 数据.解锁格子(new Vector2Int(5, 0));
            var 字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
            var 页 = 页物.GetComponent<天帝道纹界面>(); 页.初始化(数据, 字体, () => { }); Canvas.ForceUpdateCanvases();
            using (var 实际人 = new 天帝主角属性(天帝普攻.主角配置(), 数据))
                检查("新面板显示当前实战参数", Mathf.Abs(页.当前演示参数.伤害 - 普攻参数.读取(数据, 实际人).伤害) < .0001f && 页.当前演示参数.数量 == 3 && 页.当前演示参数.分裂 == 1 && 页.当前演示参数.连锁 == 2);
            var 演 = 页物.GetComponentInChildren<天帝道纹攻击演示>();
            for (int i = 0; i < 45; i++) 演.推进演示(.05f);
            检查("攻击演示确实发生命中分裂及连锁", 演.演示命中次数 > 0 && 演.演示分裂次数 > 0 && 演.演示连锁次数 > 0);
            调(页, a, null, true, false);
            检查("右键旋转前显示断链与伤害变化", 页.构筑预览说明.Contains("将暗掉 4 枚") && 页.当前演示参数.伤害 == 1 && 页.画布.高亮路径.Count == 2 && 页.画布.预览暗掉.Count == 4);
            调(页, 候, new Vector2Int(5, 0), false, false);
            检查("拖放预览标出新生效格并改变范围", 页.构筑预览说明.Contains("将亮起 1 枚") && 页.当前演示参数.溅射半径 == 1 && 页.画布.预览亮起.Contains(new Vector2Int(5, 0)));
            检查("拖放预览高亮将接通的完整通路", 页.画布.高亮路径.Count == 6 && 页.画布.高亮路径.Last() == new Vector2Int(5, 0));
            相机物 = new GameObject("道纹预览验证相机", typeof(Camera)); SceneManager.MoveGameObjectToScene(相机物, 场);
            var 相机 = 相机物.GetComponent<Camera>(); 相机.enabled = false; 相机.orthographic = true; 相机.orthographicSize = 450; 相机.transform.position = new Vector3(0, 0, -10); 相机.clearFlags = CameraClearFlags.SolidColor; 相机.backgroundColor = Color.black; 相机.cullingMask = 1 << 30; 相机.scene = 场;
            foreach (var t in 根物.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            根物.GetComponent<Canvas>().worldCamera = 相机;
            缓冲 = new RenderTexture(1600, 900, 24); 相机.targetTexture = 缓冲;
            for (int i = 0; i < 7; i++) 演.推进演示(.05f);
            拍(相机, 缓冲, Path.Combine(目录, "道纹画布_拖放预览.png"));
            页物.GetComponentsInChildren<Button>().Single(x => x.name == "加成来源").onClick.Invoke(); Canvas.ForceUpdateCanvases();
            foreach (var t in 根物.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            检查("来源面板可打开并列出实际有效道纹", 页.筛选已打开 && 页物.GetComponentsInChildren<Text>().Any(x => x.text.Contains("力量道纹 #1")));
            拍(相机, 缓冲, Path.Combine(目录, "道纹画布_加成来源.png")); 页.关闭筛选();
            检查("关闭来源面板恢复画布", !页.筛选已打开);
            var 卡物 = new GameObject("详情检查", typeof(RectTransform), typeof(天帝道纹详情卡)); 卡物.transform.SetParent(根区, false);
            var 卡 = 卡物.GetComponent<天帝道纹详情卡>(); 卡.初始化(字体); 卡.设置(a, "传导受阻 · 此路线需从第2圈返回第1圈；只允许同圈或向外。请调整接口连接路线。");
            var 状态 = 卡物.GetComponentsInChildren<Text>().Single(x => x.transform.parent.name == "连接状态");
            检查("较长诊断详情有动态高度", 状态.rectTransform.rect.height >= 状态.preferredHeight);
            检查("界面测试未消耗道纹或技能点", 数据.生效数 == 4 && !候.格子.HasValue);
        }
        finally
        {
            if (根物 != null) UnityEngine.Object.DestroyImmediate(根物);
            if (相机物 != null) UnityEngine.Object.DestroyImmediate(相机物);
            if (缓冲 != null) { 缓冲.Release(); UnityEngine.Object.DestroyImmediate(缓冲); }
            EditorSceneManager.ClosePreviewScene(场);
        }
    }
    static void 拍(Camera 相机, RenderTexture 缓冲, string 文件)
    {
        Canvas.ForceUpdateCanvases(); 相机.Render(); var 原 = RenderTexture.active; var 图 = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try { RenderTexture.active = 缓冲; 图.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); 图.Apply(); File.WriteAllBytes(文件, 图.EncodeToPNG()); }
        finally { RenderTexture.active = 原; UnityEngine.Object.DestroyImmediate(图); }
    }
}
#endif
