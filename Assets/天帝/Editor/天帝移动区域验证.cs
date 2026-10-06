#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class 天帝移动区域验证
{
    [Serializable] sealed class 报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>();
        public string 错误;
    }
    public static string 运行()
    {
        var 结果 = new 报告();
        void 检查(string 名, bool 值) => (值 ? 结果.通过 : 结果.失败).Add(名);
        string 目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/移动区域-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(目录);
        try
        {
            var 图 = new 天帝战斗地图(42, true); var 寻 = new 天帝战斗寻路(图); var 路 = new List<Vector2>();
            检查("玩家出生点有效", 图.可站立(图.出生位置));
            检查("敌人总量保留66", 图.敌人.Count == 66);
            foreach (var 点 in 图.敌人)
            {
                检查(点.级别 + "布点有效" + 点.位置, 图.可站立(点.位置));
                bool 有路 = 寻.路径(图.出生位置, 点.位置, 路, true, true), 合法 = 有路;
                var 前 = 图.出生位置;
                foreach (var 步 in 路) { 合法 &= 寻.无遮挡(前, 步, .45f); 前 = 步; }
                检查("布点连接出生点且路径不穿墙" + 点.位置, 合法);
            }
            foreach (var 点 in new[] { new Vector2(760,520), new Vector2(780,128), new Vector2(1120,830), new Vector2(1280,920) })
                检查("空地/石台/桥面/对岸通行" + 点, 图.可站立(天帝战斗地图.图片位置(点.x, 点.y)));
            foreach (var 点 in new[] { new Vector2(1100,930), new Vector2(200,200), new Vector2(1400,800), new Vector2(1300,420) })
                检查("河流/树林/岩壁/岩石阻挡" + 点, !图.可站立(天帝战斗地图.图片位置(点.x, 点.y), 0));
            var 对岸 = 天帝战斗地图.图片位置(1280,920);
            bool 跨桥 = 寻.路径(图.出生位置, 对岸, 路, false, false); var 前点 = 图.出生位置;
            foreach (var 点 in 路) { 跨桥 &= 寻.无遮挡(前点, 点, .45f); 前点 = 点; }
            检查("跨桥到对岸全路径有角色宽度余量", 跨桥);
            var 岩左 = 天帝战斗地图.图片位置(1210,390); var 岩右 = 天帝战斗地图.图片位置(1360,490);
            检查("岩石确实截断直线", !寻.无遮挡(岩左, 岩右, .45f));
            检查("绕岩路径存在", 寻.路径(岩左, 岩右, 路, false, false));
            bool 移动正确 = true; var 移动点 = 岩左;
            foreach (var 点 in 路)
            {
                for (int i = 0; i < 1000 && Vector2.Distance(移动点, 点) > .01f; i++)
                { 移动点 = 图.移动(移动点, (点 - 移动点).normalized, Mathf.Min(.2f, Vector2.Distance(移动点, 点))); 移动正确 &= 图.可站立(移动点); }
            }
            检查("沿绕障路径移动成功且始终未穿墙", 移动正确 && Vector2.Distance(移动点, 岩右) < .02f);
            var 高速终 = 图.移动(图.出生位置, Vector2.down, 100);
            检查("高速移动无法越过河流", 图.可站立(高速终) && 高速终.y > -32);
            检查("无效输入保留原位置", 图.移动(图.出生位置, new Vector2(float.NaN, 0), 2) == 图.出生位置);
            var 队 = new List<战斗敌人>();
            var 王 = new 战斗敌人(图.敌人[图.敌人.Count - 1], 战斗难度.普通);
            typeof(战斗敌人).GetProperty("已生成").SetValue(王, true); 队.Add(王);
            var AI = new 天帝敌人AI(图, 寻, 队, _ => false, new List<战斗光圈>());
            var 玩家 = 图.王位置 + Vector2.down * 12; var 王起 = 王.位置;
            bool 预算有效 = true;
            for (int i = 0; i < 30; i++) { AI.开始帧(); AI.推进(玩家, .1f); 预算有效 &= AI.本次寻路次数 <= 4 && 图.可站立(王.位置); }
            检查("固定大图BOSS可以越过旧王房边界追击", Vector2.Distance(王.位置, 王起) > 5);
            检查("AI移动共用碰撞且寻路遵守预算", 预算有效);
            bool 位图一致 = true;
            for (int y = 0; y < 天帝战斗地图.区域分辨率; y++) for (int x = 0; x < 天帝战斗地图.区域分辨率; x++)
                位图一致 &= 图.区域格可通行(x, y) == 图.可站立(图.区域格中心(x, y), 0);
            检查("25600个红绿像素与碰撞判断一致", 位图一致);
            var 预览场景 = EditorSceneManager.NewPreviewScene();
            try
            {
                var 物 = new GameObject("区域默认设置检查") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(物, 预览场景);
                var 组件 = 物.AddComponent<天帝战斗场景>();
                检查("默认隐藏填色并显示发光边界", !组件.显示移动区域 && 组件.显示边界线);
            }
            finally { EditorSceneManager.ClosePreviewScene(预览场景); }
            导出对照(图, Path.Combine(目录, "青岚原_通行区域对照.png"));
        }
        catch (Exception ex) { 结果.错误 = ex.ToString(); }
        File.WriteAllText(Path.Combine(目录, "report.json"), JsonUtility.ToJson(结果, true));
        return "通过=" + 结果.通过.Count + "，失败=" + 结果.失败.Count + "，错误=" + (string.IsNullOrEmpty(结果.错误) ? 0 : 1) + "；" + 目录;
    }
    static void 导出对照(天帝战斗地图 图, string 路径)
    {
        var 图像 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            图像.LoadImage(File.ReadAllBytes(Path.Combine(天帝构建工具.项目根, "Assets/天帝/美术/生成素材/青岚原_大地图.png")));
            var 像素 = 图像.GetPixels();
            for (int y = 0; y < 图像.height; y++) for (int x = 0; x < 图像.width; x++)
            {
                int 格x = x * 天帝战斗地图.区域分辨率 / 图像.width, 格y = y * 天帝战斗地图.区域分辨率 / 图像.height;
                var 色 = 图.区域格可通行(格x, 格y) ? new Color(40/255f,220/255f,90/255f) : new Color(240/255f,45/255f,45/255f);
                像素[y * 图像.width + x] = Color.Lerp(像素[y * 图像.width + x], 色, .28f);
            }
            图像.SetPixels(像素); 图像.Apply(); File.WriteAllBytes(路径, 图像.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(图像); }
    }
}
#endif
