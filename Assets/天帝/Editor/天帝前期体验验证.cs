#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public static class 天帝前期体验验证
{
    static readonly BindingFlags 隐 = BindingFlags.Instance | BindingFlags.NonPublic;
    static 天帝真实数值验证.报告 结果;
    static void 查(string 名, bool 对) => (对 ? 结果.通过 : 结果.失败).Add(名);
    static void 设(object 对象, string 名, object 值) => 对象.GetType().GetProperty(名).SetValue(对象, 值);
    public static string 运行()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("前期体验验证只在独立模型运行，请勿在Play期间调用。");
        结果 = new 天帝真实数值验证.报告();
        string 目录 = Path.Combine(Directory.GetCurrentDirectory(), "生成/验证/前期体验优化"); Directory.CreateDirectory(目录);
        try { 天帝数值同步检查.校验(); 诊断(); 界面(); }
        catch (Exception 错) { 结果.错误.Add(错.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        try
        {
            天帝移动适配验证.运行();
            天帝道纹画布预览验证.运行();
            天帝全面战斗验证.运行();
        }
        catch (Exception 错) { 结果.错误.Add(错.ToString()); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        if (Application.isBatchMode) EditorApplication.Exit(结果.错误.Count + 结果.失败.Count == 0 ? 0 : 1);
        return 目录;
    }
    static void 诊断()
    {
        var 网 = new 天帝道纹(101, 天帝天赋.获取((int)天赋种类.普通人));
        var 存 = 网.导出存档(); 存.技能点 = 100; 网 = 天帝道纹.读取存档(存); // 仅边界夹具。
        道纹实例 加(int 接口, Vector2Int 格)
        {
            var 纹 = 天帝道纹生成.创建(1, 道纹分类.属性, 道纹品阶.普通, new System.Random(42), 道纹属性分组.基础);
            纹.接口 = 接口; 网.获得道纹(纹); 网.解锁格子(格); 网.放置(纹, 格); return 纹;
        }
        var 上游 = 加(13, Vector2Int.right); var 目标 = 加(1, new Vector2Int(0, 1));
        var 诊断 = new 天帝道纹连接诊断(网);
        查("先遇封印源口仍优先提示可用邻纹缺口", 诊断.说明(目标).Contains("本纹的右下口"));
        查("未接通提示不依赖鼠标右键", !诊断.说明(目标).Contains("右键"));
        string 原 = JsonUtility.ToJson(网.导出存档()); var 影 = 诊断.预览(目标, null, true);
        查("旋转沙盘能接上第二来路且不改原档", 影.道纹.Find(x => x.编号 == 目标.编号).生效 && JsonUtility.ToJson(网.导出存档()) == 原);
        网.旋转(目标); 查("旋转实际执行与沙盘连接一致", 目标.生效);
        var 特性 = 天帝特性道纹.创建(1000, 道纹分类.特性, 1, 道纹品阶.普通, 8, 0);
        网.收回(上游); 网.获得道纹(特性); 网.放置(特性, Vector2Int.right);
        查("已接通特性明确保留条件不足状态", 特性.生效 && new 天帝道纹连接诊断(网).说明(特性).Contains("条件"));
        查("投影遮挡忽略不同Y平面", 天帝战斗辨识.平面相交(new Bounds(Vector3.zero, Vector3.one), new Bounds(new Vector3(0, 20, 0), Vector3.one)));
        查("分离物体不会误判遮挡", !天帝战斗辨识.平面相交(new Bounds(Vector3.zero, Vector3.one), new Bounds(new Vector3(4, 0, 4), Vector3.one)));
    }
    static void 界面()
    {
        var 美术 = AssetDatabase.FindAssets("t:天帝美术资源").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<天帝美术资源>).First();
        var 字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSansSC-Regular.otf");
        var 原手机 = 天帝移动适配.验证移动平台; var 原屏 = 天帝移动适配.验证屏幕尺寸; var 原安 = 天帝移动适配.验证安全区;
        GameObject 游戏物 = null, 画布物 = null;
        try
        {
            foreach (bool 手机 in new[] { false, true })
            {
                天帝移动适配.验证移动平台 = 手机;
                var 网 = new 天帝道纹(101, 天帝天赋.获取((int)天赋种类.普通人));
                var 盒 = new 天帝宝盒(网, 101, 天帝宝盒.开局灵石);
                using (var 人 = new 天帝主角属性(天帝普攻.主角配置(), 网))
                {
                    游戏物 = new GameObject("前期体验独立模型"); 游戏物.SetActive(false); var 游戏 = 游戏物.AddComponent<天帝游戏>();
                    游戏.美术 = 美术; 游戏.默认字体 = 字体;
                    设(游戏, "阶段", 游戏阶段.主页); 设(游戏, "道纹数据", 网); 设(游戏, "主角属性", 人); 设(游戏, "宝盒数据", 盒); 设(游戏, "通货数据", new 天帝通货(网, 101));
                    var 界面 = new 天帝界面(游戏); 设(游戏, "界面", 界面);
                    画布物 = (GameObject)typeof(天帝界面).GetField("根", 隐).GetValue(界面); 画布物.transform.SetParent(null, false);
                    画布物.hideFlags = HideFlags.HideAndDontSave; 画布物.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace; 画布物.GetComponent<CanvasScaler>().enabled = false;
                    foreach (var 屏 in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2340, 1080) })
                    {
                        string 前 = (手机 ? "手机" : "PC") + 屏;
                        天帝移动适配.验证屏幕尺寸 = 屏; 天帝移动适配.验证安全区 = new Rect(40, 16, 屏.x - 80, 屏.y - 32);
                        ((RectTransform)画布物.transform).sizeDelta = 屏;
                        界面.显示主页(); 排();
                        查(前 + "空档建议属性盒而非强制购买", 天帝修行指引.下一步(网, 盒, 1).Contains("属性盒"));
                        int 次数 = 界面.适配排版次数; for (int i = 0; i < 100; i++) 界面.更新适配();
                        查(前 + "静止界面100帧不重复全树排版", 界面.适配排版次数 == 次数);
                        var 按钮 = 画布物.GetComponentsInChildren<Button>().First(x => x.name == "修行指引"); 按钮.onClick.Invoke(); 排();
                        查(前 + "修行指引可开并呈现分步聚光", 界面.新手指引已打开 && 界面.新手指引.当前步骤 == 修行引导步骤.欢迎 && 界面.新手指引.当前说明.Contains("金色框"));
                        查(前 + "指引正文不超出文本区", 画布物.GetComponentsInChildren<Text>().Where(x => x.name == "引导详细说明").All(x => x.preferredHeight <= x.rectTransform.rect.height + 1));
                        界面.跳过新手指引(); 排(); 查(前 + "关闭指引恢复主页按钮", 按钮.interactable && !界面.新手指引已打开);
                        界面.显示宝盒(); 排(); 查(前 + "新弹层触发适配刷新", 界面.适配排版次数 > 次数);
                        查(前 + "宝盒功能风险解释可见", 画布物.GetComponentsInChildren<Text>().Any(x => x.text.Contains("未必增加伤害")));
                        界面.关闭宝盒(); 界面.显示道纹(网); 排();
                        查(前 + "空背包保留宝盒和战斗两种路线", 画布物.GetComponentsInChildren<Text>().Any(x => x.text.Contains("可回主页开属性宝盒")));
                        void 排()
                        { for (int i = 0; i < 3; i++) { 界面.更新适配(); Canvas.ForceUpdateCanvases(); foreach (var 布 in 画布物.GetComponentsInChildren<天帝正文排版>()) 布.更新?.Invoke(); } }
                    }
                    string 档 = JsonUtility.ToJson(网.导出存档()); int 初钱 = (int)盒.灵石;
                    查((手机 ? "手机" : "PC") + "查看引导不发资源不耗点", 网.技能点 == 1 && 网.道纹.Count == 0 && 初钱 == 500 && JsonUtility.ToJson(网.导出存档()) == 档);
                    if (手机)
                    {
                        界面.显示主页();
                        盒.抽取(宝盒种类.属性, out var 纹, out _);
                        界面.显示道纹(网); 界面.更新适配(); Canvas.ForceUpdateCanvases();
                        var 页 = 界面.道纹页;
                        页.触屏选择(纹, 页.格屏幕位置(Vector2Int.zero)); int 原口 = 纹.接口;
                        页.触屏旋转(); 查("候选手机旋转只改待放接口", 纹.接口 == 原口);
                        for (int 转 = 0; 转 < 6 && (页.触屏待放接口.Value & 8) == 0; 转++) 页.触屏旋转();
                        页.画布.缩放 = 1.1f; var 格 = Vector2Int.right; Canvas.ForceUpdateCanvases();
                        var 事件 = new PointerEventData(EventSystem.current) { position = 页.格屏幕位置(格), pointerId = 500, button = PointerEventData.InputButton.Left };
                        var 点格 = typeof(天帝道纹界面).GetMethod("触屏点画布", 隐); 点格.Invoke(页, new object[] { 事件 });
                        查("手机首点锁格仅消费正式一点且展示沙盘", 网.技能点 == 0 && !纹.格子.HasValue && 页.构筑预览说明.Contains("尚未执行"));
                        点格.Invoke(页, new object[] { 事件 });
                        查("手机次点放置显示真实接通结果", 纹.生效 && 页.连接诊断说明.Contains("已接通") && 页.触屏操作说明.Contains("已生效"));
                        查("手机放置不会创建额外道纹", 网.道纹.Count == 1 && 盒.灵石 == 400);
                        页.触屏卸下(); 查("卸下保留道纹不退技能点", !纹.格子.HasValue && 网.技能点 == 0);
                    }
                    UnityEngine.Object.DestroyImmediate(画布物); 画布物 = null; UnityEngine.Object.DestroyImmediate(游戏物); 游戏物 = null;
                }
            }
        }
        finally
        {
            if (画布物 != null) UnityEngine.Object.DestroyImmediate(画布物); if (游戏物 != null) UnityEngine.Object.DestroyImmediate(游戏物);
            天帝移动适配.验证移动平台 = 原手机; 天帝移动适配.验证屏幕尺寸 = 原屏; 天帝移动适配.验证安全区 = 原安;
        }
    }
}
#endif
