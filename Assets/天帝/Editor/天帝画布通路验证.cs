#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 隔离模型、预览场景及存档目录；不切换当前场景或Play Mode。检查包含通路实战模拟。
[InitializeOnLoad]
public static class 天帝画布通路验证
{
    [Serializable] sealed class 报告 { public List<string> 通过 = new List<string>(), 失败 = new List<string>(), 错误 = new List<string>(); }
    static 报告 结果;
    static string 根 => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static 天帝画布通路验证() { EditorApplication.update += 检查请求; }
    static void 检查请求()
    {
        var 路径 = Path.Combine(根, "生成/验证/画布通路请求.txt");
        if (!File.Exists(路径)) { EditorApplication.update -= 检查请求; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= 检查请求; File.Delete(路径); 运行();
    }
    static void 检查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static object 私调(object 对象, string 名, params object[] 参数) => 对象.GetType().GetMethod(名, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(对象, 参数);
    static 天帝道纹 网(int 等级 = 10, 天赋种类 天赋 = 天赋种类.普通人)
    {
        var 网 = new 天帝道纹(52, 天帝天赋.获取((int)天赋)); 网.设置玩家等级(等级);
        var 存 = 网.导出存档(); 存.技能点 = 200; return 天帝道纹.读取存档(存);
    }
    static 道纹实例 加(天帝道纹 网, 道纹属性 属性, int 值, int 口, Vector2Int? 格)
    {
        var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(3));
        纹.词条.Clear(); 纹.词条.Add(new 道纹词条(属性, 值)); 纹.接口 = 口; 网.获得道纹(纹);
        if (格.HasValue) { 网.解锁格子(格.Value); if (!网.放置(纹, 格.Value)) throw new Exception("夹具放置失败"); } return 纹;
    }
    static 天帝道纹 双路(天赋种类 天赋 = 天赋种类.普通人)
    {
        var 网格 = 网(10, 天赋);
        加(网格, 道纹属性.力量, 4, 9, new Vector2Int(1, 0));
        加(网格, 道纹属性.数量, 2, 9, new Vector2Int(2, 0));
        加(网格, 道纹属性.分裂, 1, 9, new Vector2Int(3, 0));
        加(网格, 道纹属性.火, 3, 8, new Vector2Int(4, 0));
        加(网格, 道纹属性.连锁, 2, 36, new Vector2Int(1, -1));
        加(网格, 道纹属性.金, 5, 36, new Vector2Int(2, -2));
        加(网格, 道纹属性.攻速, 20, 4, new Vector2Int(3, -3)); return 网格;
    }
    [MenuItem("天帝/验证/画布操作与独立通路")]
    public static void 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        结果 = new 报告(); var 目录 = Path.Combine(根, "生成/验证/画布通路-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")); Directory.CreateDirectory(目录);
        try { 模型检查(目录); } catch (Exception e) { 结果.错误.Add(e.ToString()); }
        try { 战斗检查(); } catch (Exception e) { 结果.错误.Add(e.ToString()); }
        try { 界面检查(目录); } catch (Exception e) { 结果.错误.Add(e.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
    }
    static void 模型检查(string 目录)
    {
        var 图 = 双路();
        using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 图))
        {
            var a = 普攻参数.读取通路(图, 人, 0); var b = 普攻参数.读取通路(图, 人, 5);
            检查("第一路多颗分裂不泄漏连锁", a.数量 == 3 && a.分裂 == 1 && a.连锁 == 0);
            检查("第二路单颗连锁不泄漏数量分裂", b.数量 == 1 && b.分裂 == 0 && b.连锁 == 2);
            检查("基础力量全局生效且两路各有五行伤害", a.普通伤害 == 7 && b.普通伤害 == 7 && a.伤害 == 10 && b.伤害 == 12);
            var 裸装 = 天帝数值.计算主角(图.玩家等级, null, 图.天赋);
            检查("第二路攻速全局影响两路射击间隔", Mathf.Approximately(a.间隔, b.间隔) && a.间隔 < 1 / 裸装.攻速);
            检查("封印通路没有战斗参数", !普攻参数.读取通路(图, 人, 4).已激活);
        }
        var 新图 = 网(1); using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 新图))
            检查("真实裸装首口基础伤害1且只开一条", 新图.道纹.Count == 0 && 新图.开放通路数 == 1 && 普攻参数.读取(新图, 人).伤害 == 1);
        新图.设置玩家等级(50); 检查("50级按原规则开放六条", 新图.开放通路数 == 6);
        检查("新解封空接口不稀释已有攻击且裸装保留天然普攻", 新图.射击通路数 == 1 && 新图.通路参与射击(0) && !新图.通路参与射击(5));
        加(新图,道纹属性.连锁,1,4,new Vector2Int(1,-1));
        检查("仅配置第二路时直接用第二路射击", 新图.射击通路数 == 1 && !新图.通路参与射击(0) && 新图.通路参与射击(5));
        var 共 = 网(); 加(共, 道纹属性.力量, 2, 63, new Vector2Int(1,0)); 加(共, 道纹属性.智力, 3, 63, new Vector2Int(1,-1));
        var 共享 = 加(共, 道纹属性.数量, 2, 13, new Vector2Int(2,-1));
        检查("共享节点明确记录两个源接口", 共.通路掩码(共享) == 33);
        检查("共享词条各路计一次全网仍去重", 共.生效加成[(int)道纹属性.数量] == 2 && 共.弹槽加成[0][(int)道纹属性.数量] == 2 && 共.弹槽加成[5][(int)道纹属性.数量] == 2);
        检查("指定路径从对应源口出发", new 天帝道纹连接诊断(共).路径(共享,5)[1] == new Vector2Int(1,-1));
        var 环 = 网(1); foreach (var 格 in 天帝道纹.邻向) 加(环, 道纹属性.力量,1,63,格);
        检查("同圈环路不会无限累计", 环.生效数 == 6 && 环.生效加成[(int)道纹属性.力量] == 6 && 环.弹槽生效数[0] == 6);
        检查("接口多选同时满足方向", 天帝道纹操作筛选.接口匹配(9,9,false) && !天帝道纹操作筛选.接口匹配(9,3,false));
        检查("旋转筛选不会将相对形状不同当成可用", 天帝道纹操作筛选.接口匹配(9,36,true) && !天帝道纹操作筛选.接口匹配(9,3,true));
        检查("接口筛选不修改实例朝向", 图.道纹[0].接口 == 9);
        图.保存布局方案(0,"多颗与连锁"); string 前 = JsonUtility.ToJson(图.导出存档());
        var 坏 = 图.当前布局(); 坏[0].编号 = int.MaxValue;
        int 事件 = 0; 图.状态改变 += () => 事件++;
        检查("缺少实际道纹拒绝整个布局", !图.应用布局(坏,out var 缺因) && 缺因.Contains("缺少道纹") && 前 == JsonUtility.ToJson(图.导出存档()) && 事件 == 0);
        坏 = 图.当前布局(); 坏[0].格子 = new Vector2Int(20,20);
        检查("锁格方案拒绝且不扣点", !图.应用布局(坏,out _) && 前 == JsonUtility.ToJson(图.导出存档()));
        坏 = 图.当前布局(); 坏[1].格子 = 坏[0].格子;
        检查("重复位置拒绝且不部分应用", !图.应用布局(坏,out _) && 前 == JsonUtility.ToJson(图.导出存档()));
        坏 = 图.当前布局(); 坏[0].接口 = 63;
        检查("布局不能凭空增加接口", !图.应用布局(坏,out _) && 前 == JsonUtility.ToJson(图.导出存档()));
        var 原实例 = 图.道纹[0]; var 源 = 图.已放置[Vector2Int.zero]; int 原点 = 图.技能点;
        图.收回(图.道纹[0]); 事件 = 0;
        检查("方案一次性恢复全部位置", 图.载入布局方案(0,out _) && 事件 == 1 && 图.技能点 == 原点 && 图.生效数 == 7);
        检查("应用保留实际实例与源纹引用", ReferenceEquals(原实例,图.道纹[0]) && ReferenceEquals(源,图.已放置[Vector2Int.zero]));
        var 换 = 图.当前布局(); var 甲 = 换[0].格子; 换[0].格子 = 换[1].格子; 换[1].格子 = 甲;
        检查("布局能交换两个已占用的位置", 图.应用布局(换,out _) && 图.道纹[0].格子 == 换[0].格子);
        图.载入布局方案(0,out _);
        var 保存 = new 天帝存档数据 { 主角 = 天帝普攻.主角配置(), 画布 = 图.导出存档(), 通货 = new int[13], 地图等级 = 1 };
        var 档 = new 天帝存档(Path.Combine(目录,"隔离存档")); bool 已存 = 档.保存(保存); var 读 = 档.读取();
        检查("布局方案与正常存档一起保存读取", 已存 && 读 != null && 天帝道纹.读取存档(读.画布).布局方案[0].名称 == "多颗与连锁");
        var 旧 = 图.导出存档(); 旧.布局方案 = null; 检查("旧档缺方案字段正常继续", 天帝道纹.读取存档(旧).布局方案.All(x => !x.已保存));
        图.道纹[0].词条[0].数值 = 8; 图.重算(); 图.载入布局方案(0,out _);
        检查("方案不回滚洗练后的实际词条", 图.道纹[0].词条[0].数值 == 8);
    }
    static void 敌设置(战斗敌人 敌,string 名,object 值) => typeof(战斗敌人).GetProperty(名).SetValue(敌,值);
    static void 战斗检查()
    {
        foreach (var 天赋 in new[] { 天赋种类.普通人, 天赋种类.余响 })
        {
            var 图 = 双路(天赋); var 配 = 天帝普攻.主角配置(); 配.血量.基础值 = 100000;
            using (var 人 = new 天帝主角属性(配,图))
            {
                var 地 = new 天帝战斗地图(27,true); var 战 = new 天帝战斗系统(地,图,人,战斗难度.普通,null,100) { 射击前摇 = .2f };
                for (int i = 0; i < 8; i++) { var 敌 = 战.敌人[i]; 敌设置(敌,"已生成",true); 敌设置(敌,"位置",new Vector2(4 + i % 4, (i / 4) * 1.5f)); 敌设置(敌,"登场剩余秒",0f); }
                var 路序 = new List<int>(); bool 参数正确 = true;
                战.射击释放 += _ => { var p = 战.当前普攻; 路序.Add(p.通路); 参数正确 &= p.通路 == 0 ? p.数量 == 3 && p.分裂 == 1 && p.连锁 == 0 : p.通路 == 5 && p.数量 == 1 && p.分裂 == 0 && p.连锁 == 2; };
                for (int i = 0; i < 220; i++) 战.推进(Vector2.zero,.025f);
                string 名 = 天赋.ToString();
                检查(名 + "实战带前摇轮流发射独立通路", 参数正确 && 战.通路释放次数[0] > 0 && 战.通路释放次数[5] > 0 && Math.Abs(战.通路释放次数[0] - 战.通路释放次数[5]) <= 1);
                检查(名 + "未向封印通路发射", 战.通路释放次数.Skip(1).Take(4).All(x => x == 0));
                检查(名 + "实战实际发生分裂与连锁", 战.分裂生成数 > 0 && 战.连锁发生数 > 0);
                检查(名 + "全局射速没有按通路数翻倍", 战.普通释放次数 <= 11);
                if (天赋 == 天赋种类.余响) 检查("余响继承第五发通路且不推进轮转", 战.回响次数 > 0 && 路序.Count > 5 && 路序[4] == 路序[5]);
            }
        }
    }
    static void 界面检查(string 目录)
    {
        var 场 = EditorSceneManager.NewPreviewScene(); GameObject 根物 = null, 相机物 = null; RenderTexture 缓冲 = null;
        try
        {
            根物 = new GameObject("画布通路测试临时根",typeof(RectTransform),typeof(Canvas)); SceneManager.MoveGameObjectToScene(根物,场);
            var 根区 = (RectTransform)根物.transform; 根区.sizeDelta = new Vector2(1600,900); 根物.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var 页物 = new GameObject("道纹页面",typeof(RectTransform),typeof(天帝道纹界面)); 页物.transform.SetParent(根区,false);
            var 区 = (RectTransform)页物.transform; 区.anchorMin = Vector2.zero; 区.anchorMax = Vector2.one; 区.offsetMin = 区.offsetMax = Vector2.zero;
            var 图 = 双路(); var 孤 = 加(图,道纹属性.范围,1,9,new Vector2Int(-3,1)); var 候 = 加(图,道纹属性.范围,2,9,null); 图.解锁格子(new Vector2Int(5,0));
            var 字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
            var 页 = 页物.GetComponent<天帝道纹界面>(); 页.初始化(图,字体,() => {}); Canvas.ForceUpdateCanvases();
            图.保存布局方案(0,"齐射分裂 / 单发连锁");
            bool 聚焦 = true; foreach (var 格 in 图.所有解锁格) 聚焦 &= 页.画布.rectTransform.rect.Contains(天帝道纹.格位置(格) * 页.画布.缩放 + 页.画布.平移);
            检查("初次进入已解锁区域在视口内",聚焦);
            页.定位未接通(); 检查("定位未接通并显示具体原因", Vector2.Distance(天帝道纹.格位置(孤.格子.Value) * 页.画布.缩放 + 页.画布.平移,Vector2.zero) < .01f && 页.连接诊断说明.Contains("独立"));
            页.回到源点(); 检查("回源点恢复源纹中心",页.画布.平移 == Vector2.zero);
            页.设置接口筛选(8); 检查("候选区接口筛选结果正确",Enumerable.Range(0,页.筛选结果数).All(i => 页.候选显示项(i).有接口(3))); 页.设置接口筛选(0);
            检查("通路页签切换真实参数",页.选择预览通路(5) && 页.当前演示参数.数量 == 1 && 页.当前演示参数.连锁 == 2 && 页.当前演示参数.五行额外伤害 == 5);
            检查("封印通路不能选择", !页.选择预览通路(4)); 页.选择预览通路(0);
            var e = new PointerEventData(null) { button = PointerEventData.InputButton.Left, position = 页.候选屏幕位置(图.道纹.IndexOf(候)) };
            页.按下(候,false,e); 页.开始拖动(false,e); int 原口 = 候.接口; string 原档 = JsonUtility.ToJson(图.导出存档());
            检查("拖拽快捷旋转只改变影子",页.旋转拖动道纹() && 候.接口 == 原口 && 页.拖动接口 == 天帝道纹.顺时针接口(原口) && 原档 == JsonUtility.ToJson(图.导出存档()));
            页.取消拖动(); 检查("取消拖拽保留原接口与背包实例",候.接口 == 原口 && !候.格子.HasValue);
            页.按下(候,false,e); 页.开始拖动(false,e); 页.旋转拖动道纹(); e.position = 页.格屏幕位置(new Vector2Int(5,0)); 页.结束拖动(e);
            检查("放下才提交旋转和位置",候.格子 == new Vector2Int(5,0) && 候.接口 == 天帝道纹.顺时针接口(原口) && 页.可撤销);
            检查("撤销放置同步恢复朝向",页.撤销上一步() && !候.格子.HasValue && 候.接口 == 原口);
            int 原点 = 图.技能点; var 解格 = new Vector2Int(6,0); 私调(页,"记录画布操作",(Action)(() => 图.解锁格子(解格)));
            检查("撤销一次解锁归还对应技能点",页.撤销上一步() && !图.格已解锁(解格) && 图.技能点 == 原点);
            私调(页,"记录画布操作",(Action)(() => 图.旋转(图.道纹[0]))); 图.道纹[0].词条[0].数值++; 图.重算();
            检查("库存词条变化后旧撤销拒绝",!页.可撤销 && !页.撤销上一步());
            图.载入布局方案(0,out _);
            页.聚焦解锁区域();
            var 演示 = 页物.GetComponentInChildren<天帝道纹攻击演示>(); for (int i = 0; i < 12; i++) 演示.推进演示(.05f);
            相机物 = new GameObject("画布通路验证相机",typeof(Camera)); SceneManager.MoveGameObjectToScene(相机物,场);
            var 相机 = 相机物.GetComponent<Camera>(); 相机.enabled = false; 相机.orthographic = true; 相机.orthographicSize = 450; 相机.transform.position = new Vector3(0,0,-10); 相机.clearFlags = CameraClearFlags.SolidColor; 相机.backgroundColor = Color.black; 相机.cullingMask = 1 << 30; 相机.scene = 场;
            根物.GetComponent<Canvas>().worldCamera = 相机; 缓冲 = new RenderTexture(1600,900,24); 相机.targetTexture = 缓冲;
            拍(根物,相机,缓冲,Path.Combine(目录,"道纹画布_操作与通路.png"));
            页.打开布局方案(); 检查("方案窗口支持三个命名槽",页.筛选已打开 && 页物.GetComponentsInChildren<InputField>().Length == 3);
            拍(根物,相机,缓冲,Path.Combine(目录,"道纹画布_布局方案.png")); 页.关闭筛选();
            检查("关闭方案恢复画布",!页.筛选已打开);
            var 包物 = new GameObject("接口筛选背包",typeof(RectTransform),typeof(天帝道纹背包)); 包物.transform.SetParent(根区,false);
            var 包区 = (RectTransform)包物.transform; 包区.anchorMin = Vector2.zero; 包区.anchorMax = Vector2.one; 包区.offsetMin = 包区.offsetMax = Vector2.zero;
            var 包 = 包物.GetComponent<天帝道纹背包>(); 包.初始化(图,字体,null,_ => {},() => {}); 包.设置接口筛选(8);
            检查("改造背包也支持接口方向筛选",Enumerable.Range(0,Math.Min(32,包.筛选结果数)).All(i => 包.显示项(i).有接口(3)));
            UnityEngine.Object.DestroyImmediate(包物);
            foreach (var 文 in 页物.GetComponentsInChildren<Text>())
                if (文.transform.parent.name == "实时构筑预览") 检查("通路面板文字未截断:" + 文.text.Split('\n')[0],文.preferredHeight <= 文.rectTransform.rect.height + 2);
            var 大存 = 网(1).导出存档(); 大存.解锁格.Clear();
            for (int q = -50; q < 50; q++) for (int r = -50; r < 50; r++) 大存.解锁格.Add(new Vector2Int(q,r)); 大存.技能点 = 0;
            var 大图 = 天帝道纹.读取存档(大存);
            var 大物 = new GameObject("一万格总览验证",typeof(RectTransform),typeof(天帝道纹界面)); 大物.transform.SetParent(根区,false);
            var 大区 = (RectTransform)大物.transform; 大区.anchorMin = Vector2.zero; 大区.anchorMax = Vector2.one; 大区.offsetMin = 大区.offsetMax = Vector2.zero;
            var 大页 = 大物.GetComponent<天帝道纹界面>(); 大页.初始化(大图,字体,() => {}); Canvas.ForceUpdateCanvases(); 大页.画布.Rebuild(CanvasUpdate.PreRender);
            var 网格 = 大页.画布.canvasRenderer.GetMesh();
            检查("一万格总览保持UGUI顶点限制内",网格 != null && 网格.vertexCount > 0 && 网格.vertexCount < 65000 && 大页.画布.缩放 < .45f);
            bool 全可见 = true; foreach (var 格 in 大图.所有解锁格) 全可见 &= 大页.画布.rectTransform.rect.Contains(天帝道纹.格位置(格) * 大页.画布.缩放 + 大页.画布.平移);
            检查("一万格聚焦完整覆盖已解锁格",全可见);
            UnityEngine.Object.DestroyImmediate(大物);
        }
        finally
        {
            if (根物 != null) UnityEngine.Object.DestroyImmediate(根物); if (相机物 != null) UnityEngine.Object.DestroyImmediate(相机物);
            if (缓冲 != null) { 缓冲.Release(); UnityEngine.Object.DestroyImmediate(缓冲); } EditorSceneManager.ClosePreviewScene(场);
        }
    }
    static void 拍(GameObject 根物,Camera 相机,RenderTexture 缓冲,string 路径)
    {
        foreach (var t in 根物.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
        Canvas.ForceUpdateCanvases(); 相机.Render(); var 原 = RenderTexture.active; var 图 = new Texture2D(1600,900,TextureFormat.RGB24,false);
        try { RenderTexture.active = 缓冲; 图.ReadPixels(new Rect(0,0,1600,900),0,0); 图.Apply(); File.WriteAllBytes(路径,图.EncodeToPNG()); }
        finally { RenderTexture.active = 原; UnityEngine.Object.DestroyImmediate(图); }
    }
}
#endif
