#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public static class 天帝战斗帧动画导入
{
    const string 资源目录 = "Assets/天帝/Resources/角色帧动画";
    [Serializable] public sealed class 清单
    {
        public string schema, character, review_status, source_policy, source_map, source_map_sha256;
        public int[] canvas;
        public float[] pivot;
        public float pixels_per_unit, display_height, reference_speed;
        public 动作记录[] clips;
    }
    [Serializable] public sealed class 动作记录
    {
        public string direction, action;
        public bool loop;
        public int release_frame;
        public float[] durations;
        public 验收记录 review;
        public 帧记录[] frames;
    }
    [Serializable] public sealed class 验收记录
    {
        public bool identity_pass, phase_pass, edges_pass, playback_pass;
    }
    [Serializable] public sealed class 帧记录
    {
        public string path, sha256, source;
    }
    sealed class 预检动作
    {
        public 动作记录 原记录;
        public 角色朝向 朝向;
        public 战斗帧动作 动作;
        public string[] 文件;
    }

    [MenuItem("天帝/美术/导入已验收战斗帧动画")]
    public static void 导入菜单()
    {
        string 路径 = EditorUtility.OpenFilePanel("选择已验收 release-manifest.json", "", "json");
        if (!string.IsNullOrEmpty(路径)) Debug.Log(导入已验收(路径));
    }

    public static string 核对交付清单(string 清单路径)
    {
        string 路径 = Path.GetFullPath(清单路径);
        var 数据 = 读取清单(路径, out _);
        var 动作 = 预检清单(数据, Path.GetDirectoryName(路径));
        return 数据.character + " 的 " + 动作.Count + " 组动作交付文件与验收指纹一致；尚未导入。";
    }

    public static string 导入已验收(string 清单路径)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止运行，再导入动画。");
        string 绝对清单 = Path.GetFullPath(清单路径);
        string 目录 = Path.GetDirectoryName(绝对清单);
        var 数据 = 读取清单(绝对清单, out var 清单指纹);
        var 预检 = 预检清单(数据, 目录);
        string 目标 = 资源目录 + "/" + 数据.character + "/release-" + 清单指纹.Substring(0, 12);
        Directory.CreateDirectory(目标);

        // 所有文件与验收指纹先检查，确认后才复制、导入并更新独立配置。
        foreach (var 动作 in 预检) for (int i = 0; i < 动作.文件.Length; i++)
            复制核对文件(动作.文件[i], 帧路径(目标, 动作, i), 动作.原记录.frames[i].sha256);
        复制核对文件(绝对清单, 目标 + "/release-manifest.json", 清单指纹);
        复制核对文件(内路径(目录, 数据.source_map), 目标 + "/source-map.json", 数据.source_map_sha256);
        AssetDatabase.Refresh();
        var 方向们 = new 天帝战斗帧动画.方向动作[预检.Count];
        for (int a = 0; a < 预检.Count; a++)
        {
            var 动作 = 预检[a];
            var 项 = new 天帝战斗帧动画.方向动作 { 朝向 = 动作.朝向, 动作 = 动作.动作,
                每帧秒 = (float[])动作.原记录.durations.Clone(), 循环 = 动作.原记录.loop,
                释放帧 = 动作.原记录.release_frame, 帧 = new Sprite[动作.文件.Length] };
            方向们[a] = 项;
            for (int i = 0; i < 项.帧.Length; i++)
            {
                string 路径 = 帧路径(目标, 动作, i);
                var 导入 = (TextureImporter)AssetImporter.GetAtPath(路径);
                导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
                导入.alphaIsTransparency = true; 导入.mipmapEnabled = false; 导入.isReadable = false;
                导入.sRGBTexture = true; 导入.wrapMode = TextureWrapMode.Clamp; 导入.filterMode = FilterMode.Bilinear;
                导入.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(数据.canvas[0], 数据.canvas[1]));
                导入.textureCompression = TextureImporterCompression.Uncompressed;
                导入.spritePixelsPerUnit = 数据.pixels_per_unit;
                var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
                设置.spriteMeshType = SpriteMeshType.FullRect; 设置.spriteAlignment = (int)SpriteAlignment.Custom;
                设置.spritePivot = new Vector2(数据.pivot[0] / 数据.canvas[0], 数据.pivot[1] / 数据.canvas[1]);
                导入.SetTextureSettings(设置);
                foreach (string 平台 in new[] { "Standalone", "Android", "iPhone" }) 导入.ClearPlatformTextureSettings(平台);
                导入.SaveAndReimport(); 项.帧[i] = AssetDatabase.LoadAssetAtPath<Sprite>(路径);
                if (项.帧[i] == null) throw new InvalidOperationException("Unity 未能导入帧：" + 路径);
            }
        }
        var 候选 = ScriptableObject.CreateInstance<天帝战斗帧动画>();
        候选.编号 = 数据.character; 候选.已验收 = true; 候选.动作 = 方向们;
        候选.共同画布 = new Vector2Int(数据.canvas[0], 数据.canvas[1]);
        候选.共同根点 = new Vector2(数据.pivot[0], 数据.pivot[1]); 候选.每米像素 = 数据.pixels_per_unit;
        候选.展示高度 = 数据.display_height; 候选.参考移速 = 数据.reference_speed;
        候选.验收清单指纹 = 清单指纹; 候选.来源清单指纹 = 数据.source_map_sha256;
        if (!候选.可用) { UnityEngine.Object.DestroyImmediate(候选); throw new InvalidOperationException("导入后的画布、轴心或动作配置不一致，保持旧配置。"); }
        string 配置路径 = 资源目录 + "/" + 数据.character + ".asset";
        var 配置 = AssetDatabase.LoadAssetAtPath<天帝战斗帧动画>(配置路径);
        if (配置 == null) AssetDatabase.CreateAsset(候选, 配置路径);
        else { EditorUtility.CopySerialized(候选, 配置); EditorUtility.SetDirty(配置); UnityEngine.Object.DestroyImmediate(候选); }
        AssetDatabase.SaveAssets();
        return "已导入 " + 数据.character + " 的 " + 预检.Count + " 组已验收动作；缺失方向仍回退原素材。";
    }

    static List<预检动作> 预检清单(清单 数据, string 目录)
    {
        if (数据 == null || 数据.schema != "tiandi.sprite-release.v1" || 数据.review_status != "accepted")
            throw new InvalidDataException("只允许导入 tiandi.sprite-release.v1 且 review_status=accepted 的交付清单。");
        if (数据.source_policy != "mixed_production" && 数据.source_policy != "pure_ai_frames" && 数据.source_policy != "video_derived")
            throw new InvalidDataException("未知的素材来源政策。");
        if (string.IsNullOrEmpty(数据.character) || !System.Text.RegularExpressions.Regex.IsMatch(数据.character, "^(HERO|BTB[0-9]{2}|BTN[0-9]{2})$"))
            throw new InvalidDataException("角色编号必须为 HERO、BTBxx 或 BTNxx。");
        if (数据.canvas == null || 数据.canvas.Length != 2 || 数据.canvas[0] <= 0 || 数据.canvas[1] <= 0 || 数据.canvas[0] > 8192 || 数据.canvas[1] > 8192 ||
            数据.pivot == null || 数据.pivot.Length != 2 || !有限非负(数据.pivot[0]) || !有限非负(数据.pivot[1]) ||
            数据.pivot[0] > 数据.canvas[0] || 数据.pivot[1] > 数据.canvas[1] || !正数(数据.pixels_per_unit) || !正数(数据.display_height) || !正数(数据.reference_speed))
            throw new InvalidDataException("画布、左下原点 pivot、PPU 或展示比例无效。");
        string 来源 = 内路径(目录, 数据.source_map);
        if (!指纹相同(指纹(来源), 数据.source_map_sha256)) throw new InvalidDataException("来源清单指纹不匹配。");
        if (数据.clips == null || 数据.clips.Length == 0) throw new InvalidDataException("没有已验收动作。");
        var 结果 = new List<预检动作>(); var 唯一动作 = new HashSet<string>();
        foreach (var 动作 in 数据.clips)
        {
            if (动作 == null || 动作.review == null || !动作.review.identity_pass || !动作.review.phase_pass || !动作.review.edges_pass || !动作.review.playback_pass)
                throw new InvalidDataException("动作尚未通过身份、相位、全帧边缘与播放验收。");
            var 朝向 = 读方向(动作.direction); var 类型 = 读动作(动作.action);
            if (!唯一动作.Add(朝向 + "/" + 类型)) throw new InvalidDataException("重复方向动作。");
            if (动作.frames == null || 动作.frames.Length == 0 || 动作.durations == null || 动作.frames.Length != 动作.durations.Length ||
                类型 == 战斗帧动作.攻击 && (动作.loop || 动作.release_frame < 1 || 动作.release_frame >= 动作.frames.Length))
                throw new InvalidDataException("帧长数组或攻击释放帧无效。");
            float 总秒 = 0;
            foreach (float 秒 in 动作.durations) 总秒 += 秒;
            if (!正数(总秒)) throw new InvalidDataException("动作总时长无效或溢出。");
            var 项 = new 预检动作 { 原记录 = 动作, 朝向 = 朝向, 动作 = 类型, 文件 = new string[动作.frames.Length] };
            var 已见 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < 动作.frames.Length; i++)
            {
                var 帧 = 动作.frames[i];
                if (帧 == null || !正数(动作.durations[i]) || !允许来源(数据.source_policy, 帧.source))
                    throw new InvalidDataException("帧长或帧来源与制作政策不一致。");
                string 文件 = 内路径(目录, 帧.path);
                if (!文件.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("交付帧必须为 PNG。");
                string 哈希 = 指纹(文件);
                if (!指纹相同(哈希, 帧.sha256) || !已见.Add(哈希)) throw new InvalidDataException("帧指纹不匹配或重复姿态被重复计帧：" + 文件);
                var 图 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!ImageConversion.LoadImage(图, File.ReadAllBytes(文件), false) || 图.width != 数据.canvas[0] || 图.height != 数据.canvas[1])
                        throw new InvalidDataException("PNG 与共同画布尺寸不一致：" + 文件);
                    bool 有透明 = false, 有主体 = false;
                    foreach (var 像素 in 图.GetPixels32()) { 有透明 |= 像素.a == 0; 有主体 |= 像素.a > 0; }
                    if (!有透明 || !有主体) throw new InvalidDataException("帧无透明空间或无可见主体：" + 文件);
                }
                finally { UnityEngine.Object.DestroyImmediate(图); }
                项.文件[i] = 文件;
            }
            结果.Add(项);
        }
        return 结果;
    }
    static string 帧路径(string 目录, 预检动作 动作, int 帧) => 目录 + "/" + 动作.原记录.action + "_" + 动作.原记录.direction + "_" + 帧.ToString("000") + ".png";
    static string 内路径(string 目录, string 相对)
    {
        if (string.IsNullOrEmpty(相对) || Path.IsPathRooted(相对)) throw new InvalidDataException("清单路径必须相对交付目录。");
        string 根 = Path.GetFullPath(目录).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string 路径 = Path.GetFullPath(Path.Combine(根, 相对));
        if (!路径.StartsWith(根, StringComparison.OrdinalIgnoreCase) || !File.Exists(路径)) throw new FileNotFoundException("交付文件缺失或越出交付目录。", 相对);
        string 检查路径 = 路径;
        while (检查路径 != null && 检查路径.StartsWith(根.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            if ((File.GetAttributes(检查路径) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("交付路径包含链接或联接，拒绝越过目录边界：" + 相对);
            if (检查路径.Equals(根.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) break;
            检查路径 = Path.GetDirectoryName(检查路径);
        }
        return 路径;
    }
    static 清单 读取清单(string 文件, out string 哈希)
    {
        byte[] 内容 = File.ReadAllBytes(文件);
        using (var 算法 = SHA256.Create()) 哈希 = BitConverter.ToString(算法.ComputeHash(内容)).Replace("-", "").ToLowerInvariant();
        using (var 流 = new MemoryStream(内容)) using (var 读取 = new StreamReader(流)) return JsonUtility.FromJson<清单>(读取.ReadToEnd());
    }
    static string 指纹(string 文件)
    { using (var 算法 = SHA256.Create()) using (var 流 = File.OpenRead(文件)) return BitConverter.ToString(算法.ComputeHash(流)).Replace("-", "").ToLowerInvariant(); }
    static void 复制核对文件(string 源, string 目标, string 预期指纹)
    {
        byte[] 内容 = File.ReadAllBytes(源);
        string 实际;
        using (var 算法 = SHA256.Create()) 实际 = BitConverter.ToString(算法.ComputeHash(内容)).Replace("-", "").ToLowerInvariant();
        if (!指纹相同(实际, 预期指纹)) throw new InvalidDataException("预检后源文件已变化，保持旧动画配置：" + 源);
        if (File.Exists(目标))
        {
            if (!指纹相同(指纹(目标), 预期指纹)) throw new InvalidDataException("交付版本目录存在内容不同的文件，拒绝覆盖：" + 目标);
            return;
        }
        using (var 流 = new FileStream(目标, FileMode.CreateNew, FileAccess.Write, FileShare.None)) 流.Write(内容, 0, 内容.Length);
    }
    static bool 指纹相同(string 实际, string 记录) => !string.IsNullOrEmpty(记录) && 实际.Equals(记录, StringComparison.OrdinalIgnoreCase);
    static bool 正数(float 值) => 值 > 0 && !float.IsNaN(值) && !float.IsInfinity(值);
    static bool 有限非负(float 值) => 值 >= 0 && !float.IsNaN(值) && !float.IsInfinity(值);
    static bool 允许来源(string 政策, string 来源)
    {
        switch (政策)
        {
            case "pure_ai_frames": return 来源 == "ai_key_pose";
            case "mixed_production": return 来源 == "ai_key_pose" || 来源 == "cleaned_redrawn" || 来源 == "redrawn" || 来源 == "rig_rendered";
            case "video_derived": return 来源 == "video_frame";
            default: return false;
        }
    }
    static 角色朝向 读方向(string 值)
    {
        switch (值)
        {
            case "south": return 角色朝向.南; case "southeast": return 角色朝向.东南;
            case "east": return 角色朝向.东; case "northeast": return 角色朝向.东北;
            case "north": return 角色朝向.北; case "northwest": return 角色朝向.西北;
            case "west": return 角色朝向.西; case "southwest": return 角色朝向.西南;
            default: throw new InvalidDataException("未知方向：" + 值);
        }
    }
    static 战斗帧动作 读动作(string 值)
    {
        switch (值)
        {
            case "idle": return 战斗帧动作.待机; case "move": return 战斗帧动作.移动;
            case "run": return 战斗帧动作.奔跑; case "attack": return 战斗帧动作.攻击;
            default: throw new InvalidDataException("未知动作：" + 值);
        }
    }

    [Serializable] sealed class 验证报告
    {
        public List<string> 通过 = new List<string>(), 失败 = new List<string>();
        public string 错误;
    }
    public static void 批处理验证()
    {
        var 报告 = new 验证报告(); var 临时对象 = new List<UnityEngine.Object>();
        void 检查(string 名, bool 对) => (对 ? 报告.通过 : 报告.失败).Add(名);
        try
        {
            var 贴图 = new Texture2D(16, 16); 临时对象.Add(贴图);
            var 配置 = ScriptableObject.CreateInstance<天帝战斗帧动画>(); 临时对象.Add(配置);
            配置.编号 = "HERO"; 配置.已验收 = true; 配置.共同画布 = new Vector2Int(16, 16);
            配置.共同根点 = new Vector2(8, 2); 配置.动作 = new 天帝战斗帧动画.方向动作[32];
            for (int d = 0; d < 8; d++) for (int a = 0; a < 4; a++)
            {
                var 项 = new 天帝战斗帧动画.方向动作 { 朝向 = (角色朝向)d, 动作 = (战斗帧动作)a,
                    循环 = a != 3, 释放帧 = 2, 每帧秒 = new[] { .1f, .2f, .15f, .25f }, 帧 = new Sprite[4] };
                配置.动作[d * 4 + a] = 项;
                for (int f = 0; f < 4; f++)
                { 项.帧[f] = Sprite.Create(贴图, new Rect(0, 0, 16, 16), new Vector2(.5f, .125f), 100, 0, SpriteMeshType.FullRect); 临时对象.Add(项.帧[f]); }
            }
            检查("固定画布和脚下根点配置可用", 配置.可用);
            检查("展示缩放只由共同画布与PPU计算", Mathf.Abs(配置.固定缩放 - 21.875f) < .0001f);
            for (int d = 0; d < 8; d++)
            {
                float 角 = d * Mathf.PI / 4; var 向 = new Vector2(Mathf.Sin(角), -Mathf.Cos(角));
                var 播放 = new 天帝战斗帧播放(配置); var 项 = 配置.获取((角色朝向)d, 战斗帧动作.移动);
                bool 成功 = 播放.尝试推进(战斗帧动作.移动, 向, .05f, out var 图);
                检查("八方向选择 " + (角色朝向)d, 成功 && 播放.朝向 == (角色朝向)d && 图 == 项.帧[0]);
            }
            var 时序 = new 天帝战斗帧播放(配置); var 南移动 = 配置.获取(角色朝向.南, 战斗帧动作.移动);
            时序.尝试推进(战斗帧动作.移动, Vector2.down, .05f, out var 当前);
            检查("长短帧第一帧", 当前 == 南移动.帧[0]);
            时序.尝试推进(战斗帧动作.移动, Vector2.down, .11f, out 当前);
            检查("长短帧第二帧", 当前 == 南移动.帧[1]);
            时序.尝试推进(战斗帧动作.移动, Vector2.down, .25f, out 当前);
            检查("长短帧第三帧", 当前 == 南移动.帧[2]);
            时序.尝试推进(战斗帧动作.移动, Vector2.right, 0, out 当前);
            检查("转向保留循环相位且不镜像", 当前 == 配置.获取(角色朝向.东, 战斗帧动作.移动).帧[2]);
            时序.尝试推进(战斗帧动作.移动, Vector2.right, .31f, out 当前);
            检查("循环接缝保留时间余量", 当前 == 配置.获取(角色朝向.东, 战斗帧动作.移动).帧[0]);
            var 暂停前 = 当前; 时序.尝试推进(战斗帧动作.移动, Vector2.right, 0, out 当前);
            检查("零战斗秒冻结播放", 当前 == 暂停前);
            时序.尝试推进(战斗帧动作.奔跑, Vector2.right, .05f, out 当前);
            检查("奔跑使用独立原画数组", 当前 == 配置.获取(角色朝向.东, 战斗帧动作.奔跑).帧[0]);
            var 南攻击 = 配置.获取(角色朝向.南, 战斗帧动作.攻击);
            时序.尝试攻击(Vector2.down, 1, false, out 当前);
            检查("未释放时停在释放帧前", 当前 == 南攻击.帧[1]);
            时序.尝试攻击(Vector2.down, 0, true, out 当前);
            检查("真实释放事件对应释放帧", 当前 == 南攻击.帧[2]);
            时序.尝试攻击(Vector2.down, 1, true, out 当前);
            检查("后摇对应回收末帧", 当前 == 南攻击.帧[3]);
            检查("无效时间回退", !时序.尝试推进(战斗帧动作.移动, Vector2.down, float.NaN, out 当前) && !时序.使用新帧);
            var 原动作 = 配置.动作; 配置.动作 = new[] { 南移动 };
            var 不完整 = new 天帝战斗帧播放(配置);
            检查("未制作方向返回旧素材入口", !不完整.尝试推进(战斗帧动作.移动, Vector2.up, .1f, out 当前));
            检查("未制作动作返回旧素材入口", !不完整.尝试推进(战斗帧动作.攻击, Vector2.down, .1f, out 当前));
            配置.动作 = 原动作; 配置.已验收 = false;
            检查("未验收配置拒绝运行", !配置.可用); 配置.已验收 = true;
            配置.共同根点 = new Vector2(8, 3); 检查("轴心漂移拒绝运行", !配置.可用); 配置.共同根点 = new Vector2(8, 2);
            方向们异常测试(配置, 检查);
            交付文件验证(检查);
            来源政策验证(检查);
            真实表现层验证(配置, 临时对象, 检查);
        }
        catch (Exception 异常) { 报告.错误 = 异常.ToString(); }
        finally { foreach (var 项 in 临时对象) if (项 != null) UnityEngine.Object.DestroyImmediate(项); }
        string 目录 = Path.GetFullPath(Path.Combine(Application.dataPath, "../生成/验证")); Directory.CreateDirectory(目录);
        string 路径 = Path.Combine(目录, "战斗帧动画契约-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
        File.WriteAllText(路径, JsonUtility.ToJson(报告, true));
        Debug.Log("帧动画契约：通过=" + 报告.通过.Count + "，失败=" + 报告.失败.Count + "；" + 路径);
        if (报告.失败.Count > 0 || !string.IsNullOrEmpty(报告.错误)) throw new InvalidOperationException("帧动画契约验证失败，请检查 " + 路径);
    }

    static void 方向们异常测试(天帝战斗帧动画 配置, Action<string, bool> 检查)
    {
        var 动作 = 配置.动作[0]; var 帧长 = 动作.每帧秒;
        动作.每帧秒 = new[] { float.MaxValue, float.MaxValue, .1f, .1f };
        检查("总时长溢出拒绝播放", !配置.可用);
        动作.每帧秒 = new[] { .1f, 0, .1f, .1f }; 检查("零时长帧拒绝播放", !配置.可用);
        动作.每帧秒 = 帧长;
        var 原 = 配置.动作; 配置.动作 = new[] { 原[0], 原[0] };
        检查("重复方向动作拒绝播放", !配置.可用); 配置.动作 = 原;
        var 播放 = new 天帝战斗帧播放(配置);
        检查("无穷速度拒绝播放", !播放.尝试推进(战斗帧动作.移动, Vector2.down, .1f, out _, float.PositiveInfinity));
        检查("有限输入乘积溢出拒绝播放", !播放.尝试推进(战斗帧动作.移动, Vector2.down, float.MaxValue, out _, 2));
        检查("无效方向拒绝播放", !播放.尝试攻击(new Vector2(float.NaN, 0), .1f, false, out _));
    }

    static void 真实表现层验证(天帝战斗帧动画 配置, List<UnityEngine.Object> 临时对象, Action<string, bool> 检查)
    {
        var 原资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (原资源 == null) throw new InvalidOperationException("真实表现层验证缺少原美术资源。");
        var 资源 = UnityEngine.Object.Instantiate(原资源); 临时对象.Add(资源);
        var 父 = new GameObject("帧动画契约临时根"); 临时对象.Add(父);
        var 道纹 = new 天帝道纹(42, 天帝天赋.获取((int)天赋种类.普通人));
        using (var 主角 = new 天帝主角属性(天帝普攻.主角配置(), 道纹))
        {
            var 地图 = new 天帝战斗地图(42, true);
            var 战斗 = new 天帝战斗系统(地图, 道纹, 主角, 战斗难度.普通);
            using (var 美术 = new 天帝战斗美术(父.transform, 资源, 地图, 战斗, false))
            {
                if (!美术.可用) throw new InvalidOperationException("真实表现层构造失败。");
                // 验证配置仅存在内存，不保存为 accepted 资产，也不调用候选素材导入。
                写私有字段(美术, "主角方向配置", 配置);
                写私有字段(美术, "主角方向播放", new 天帝战斗帧播放(配置));
                var 玩家图 = 读私有字段(美术, "玩家图");
                var 像 = (SpriteRenderer)玩家图.GetType().GetField("像").GetValue(玩家图);
                var 点 = 地图.出生位置;
                美术.更新(战斗, 点, .05f);
                检查("主角真实表现层读取内存待机帧", Array.IndexOf(配置.获取(角色朝向.南, 战斗帧动作.待机).帧, 像.sprite) >= 0);
                美术.移动反馈(Vector2.left, false); 点 += Vector2.left * .3f; 美术.更新(战斗, 点, .05f);
                检查("主角真实表现层西向无镜像与非均匀缩放", !像.flipX && Vector3.Distance(像.transform.localScale, new Vector3(配置.固定缩放, 配置.固定缩放, 1)) < .0001f);
                var 原动作 = 配置.动作;
                配置.动作 = Array.FindAll(原动作, a => a.朝向 != 角色朝向.北);
                点 += Vector2.up * .3f; 美术.更新(战斗, 点, .05f);
                var 旧北 = 资源.主角移动动画?.获取(角色朝向.北);
                检查("主角缺北方向回退现有八方向素材", 旧北 != null && Array.IndexOf(旧北.移动帧, 像.sprite) >= 0);
                配置.动作 = 原动作;
                var 准备 = (Action<Vector2>)读私有字段(战斗, "准备射击");
                var 释放 = (Action<Vector2>)读私有字段(战斗, "射击释放");
                准备.Invoke(Vector2.down); 美术.更新(战斗, 点, .05f);
                var 攻击 = 配置.获取(角色朝向.南, 战斗帧动作.攻击);
                检查("主角真实事件前摇不提前显示释放帧", Array.IndexOf(攻击.帧, 像.sprite) >= 0 && Array.IndexOf(攻击.帧, 像.sprite) < 攻击.释放帧);
                释放.Invoke(Vector2.down); 美术.更新(战斗, 点, .25f);
                检查("主角真实释放大dt仍绘制释放帧", 像.sprite == 攻击.帧[攻击.释放帧]);
                var 释放图 = 像.sprite; 美术.更新(战斗, 点, 0);
                检查("主角释放后零dt暂停保持帧", 像.sprite == 释放图);
                美术.更新(战斗, 点, .25f);
                检查("主角回收结束返回同方向待机", Array.IndexOf(配置.获取(角色朝向.南, 战斗帧动作.待机).帧, 像.sprite) >= 0);

                var 敌 = 战斗.敌人[0];
                写属性(敌, "已生成", true); 写属性(敌, "血量", 敌.最大血量);
                写属性(敌, "行动", 敌人行动.待机);
                var 敌图们 = (System.Collections.IList)读私有字段(美术, "敌图"); var 单位 = 敌图们[0];
                string 编号 = (string)单位.GetType().GetField("动作编号").GetValue(单位);
                var 敌动作 = (天帝敌人动作)读私有字段(美术, "敌动作");
                var 配置表 = (Dictionary<string, 天帝战斗帧动画>)读私有字段(敌动作, "方向配置"); 配置表[编号] = 配置;
                var 敌像 = (SpriteRenderer)单位.GetType().GetField("像").GetValue(单位);
                美术.更新(战斗, 点, .05f);
                写属性(敌, "位置", 敌.位置 + Vector2.left * .1f); 美术.更新(战斗, 点, .05f);
                检查("敌人真实表现层西向独立帧无flip", Array.IndexOf(配置.获取(角色朝向.西, 战斗帧动作.移动).帧, 敌像.sprite) >= 0 && !敌像.flipX);
                检查("敌人真实表现层无旧挤压与gait bob", Vector3.Distance(敌像.transform.localScale, new Vector3(配置.固定缩放, 配置.固定缩放, 1)) < .0001f && Mathf.Abs(敌像.transform.position.z - 敌.位置.y) < .0001f);
                写属性(敌, "行动", 敌人行动.蓄力); 写私有字段(敌, "锁定方向", Vector2.down);
                写私有字段(敌, "蓄力总秒", .2f); 写私有字段(敌, "蓄力", .1f); 美术.更新(战斗, 点, .05f);
                检查("敌人真实表现层前摇保持共同比例", Array.IndexOf(攻击.帧, 敌像.sprite) < 攻击.释放帧 && Vector3.Distance(敌像.transform.localScale, new Vector3(配置.固定缩放, 配置.固定缩放, 1)) < .0001f);
                写属性(敌, "行动", 敌人行动.后摇); 写私有字段(敌, "后摇秒", .3f); 美术.更新(战斗, 点, .05f);
                检查("敌人蓄力被打断不伪装释放", Array.IndexOf(攻击.帧, 敌像.sprite) < 0);
                写属性(敌, "行动", 敌人行动.蓄力); 美术.更新(战斗, 点, .05f);
                写属性(敌, "已攻击次数", 敌.已攻击次数 + 1); 写属性(敌, "行动", 敌人行动.后摇);
                美术.更新(战斗, 点, .05f);
                检查("敌人真实攻击计数释放对应释放帧", 敌像.sprite == 攻击.帧[攻击.释放帧]);
                var 敌暂停帧 = 敌像.sprite; 美术.更新(战斗, 点, 0);
                检查("敌人释放后零dt冻结", 敌像.sprite == 敌暂停帧);
            }
        }
    }
    static object 读私有字段(object 目标, string 名) => 目标.GetType().GetField(名, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(目标);
    static void 写私有字段(object 目标, string 名, object 值) => 目标.GetType().GetField(名, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(目标, 值);
    static void 写属性(object 目标, string 名, object 值) => 目标.GetType().GetProperty(名).SetValue(目标, 值);
    static void 交付文件验证(Action<string, bool> 检查)
    {
        string 目录 = Path.Combine(Path.GetTempPath(), "tiandi-animation-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(目录);
        string 源 = Path.Combine(目录, "source.json"), 目标 = Path.Combine(目录, "copy.json");
        try
        {
            File.WriteAllText(源, "original"); string 哈希 = 指纹(源);
            检查("相对路径限定交付目录", 内路径(目录, "source.json") == 源);
            try { 内路径(目录, "../source.json"); 检查("越出交付目录拒绝", false); }
            catch (FileNotFoundException) { 检查("越出交付目录拒绝", true); }
            try { 内路径(目录, 源); 检查("绝对素材路径拒绝", false); }
            catch (InvalidDataException) { 检查("绝对素材路径拒绝", true); }
            复制核对文件(源, 目标, 哈希); 检查("复制文件与预检SHA256一致", 指纹(目标) == 哈希);
            复制核对文件(源, 目标, 哈希); 检查("同一交付版本重跑保持文件", 指纹(目标) == 哈希);
            File.WriteAllText(源, "changed");
            try { 复制核对文件(源, 目标, 哈希); 检查("预检后源文件变化拒绝", false); }
            catch (InvalidDataException) { 检查("预检后源文件变化拒绝", 指纹(目标) == 哈希); }
            File.WriteAllText(源, "original"); File.WriteAllText(目标, "damaged");
            try { 复制核对文件(源, 目标, 哈希); 检查("已有交付文件异常拒绝覆盖", false); }
            catch (InvalidDataException) { 检查("已有交付文件异常拒绝覆盖", File.ReadAllText(目标) == "damaged"); }
        }
        finally
        {
            foreach (string 文件 in new[] { 源, 目标 }) if (File.Exists(文件)) File.Delete(文件);
            Directory.Delete(目录);
        }
    }

    static void 来源政策验证(Action<string, bool> 检查)
    {
        string 目录 = Path.Combine(Path.GetTempPath(), "tiandi-animation-source-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(目录);
        string 来源路径 = Path.Combine(目录, "source-map.json"), 帧路径 = Path.Combine(目录, "frame.png");
        var 贴图 = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        try
        {
            var 像素 = new Color32[256]; 像素[8 * 16 + 8] = new Color32(255, 255, 255, 255);
            贴图.SetPixels32(像素); 贴图.Apply();
            File.WriteAllBytes(帧路径, 贴图.EncodeToPNG()); File.WriteAllText(来源路径, "{}");
            var 验收 = new 验收记录 { identity_pass = true, phase_pass = true, edges_pass = true, playback_pass = true };
            var 帧 = new 帧记录 { path = "frame.png", sha256 = 指纹(帧路径), source = "video_frame" };
            var 数据 = new 清单 { schema = "tiandi.sprite-release.v1", character = "BTB01", review_status = "accepted",
                source_policy = "video_derived", source_map = "source-map.json", source_map_sha256 = 指纹(来源路径),
                canvas = new[] { 16, 16 }, pivot = new[] { 8f, 2f }, pixels_per_unit = 100, display_height = 1, reference_speed = 1,
                clips = new[] { new 动作记录 { direction = "southeast", action = "idle", loop = true, release_frame = -1,
                    durations = new[] { .1f }, review = 验收, frames = new[] { 帧 } } } };
            bool 通过预检()
            {
                try { return 预检清单(数据, 目录).Count == 1; }
                catch (InvalidDataException) { return false; }
            }
            string[] 来源们 = { "ai_key_pose", "cleaned_redrawn", "redrawn", "rig_rendered", "video_frame", "unknown" };
            foreach (string 政策 in new[] { "pure_ai_frames", "mixed_production", "video_derived" })
                foreach (string 来源 in 来源们)
                {
                    数据.source_policy = 政策; 帧.source = 来源;
                    bool 预期 = 政策 == "pure_ai_frames" ? 来源 == "ai_key_pose" :
                        政策 == "video_derived" ? 来源 == "video_frame" :
                        来源 == "ai_key_pose" || 来源 == "cleaned_redrawn" || 来源 == "redrawn" || 来源 == "rig_rendered";
                    检查("来源组合 " + 政策 + "/" + 来源 + (预期 ? "通过" : "拒绝"), 通过预检() == 预期);
                }
            数据.source_policy = "unknown"; 帧.source = "video_frame";
            检查("未知来源政策拒绝", !通过预检()); 数据.source_policy = "video_derived";
            数据.review_status = "pending"; 检查("待验清单拒绝导入", !通过预检()); 数据.review_status = "accepted";
            验收.identity_pass = false; 检查("视频身份门未通过拒绝", !通过预检()); 验收.identity_pass = true;
            验收.phase_pass = false; 检查("视频相位门未通过拒绝", !通过预检()); 验收.phase_pass = true;
            验收.edges_pass = false; 检查("视频边缘门未通过拒绝", !通过预检()); 验收.edges_pass = true;
            验收.playback_pass = false; 检查("视频播放门未通过拒绝", !通过预检()); 验收.playback_pass = true;
            string 帧指纹 = 帧.sha256; 帧.sha256 = new string('0', 64);
            检查("视频帧SHA256不匹配拒绝", !通过预检()); 帧.sha256 = 帧指纹;
            string 来源指纹 = 数据.source_map_sha256; 数据.source_map_sha256 = new string('0', 64);
            检查("视频来源清单SHA256不匹配拒绝", !通过预检()); 数据.source_map_sha256 = 来源指纹;
            数据.canvas[0] = 17; 检查("视频帧共同画布尺寸不匹配拒绝", !通过预检()); 数据.canvas[0] = 16;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(贴图);
            foreach (string 文件 in new[] { 来源路径, 帧路径 }) if (File.Exists(文件)) File.Delete(文件);
            Directory.Delete(目录);
        }
    }
}
#endif
