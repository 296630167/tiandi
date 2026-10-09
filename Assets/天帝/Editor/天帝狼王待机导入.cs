#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class 天帝狼王待机导入
{
    const string 目录 = "Assets/天帝/Resources/角色帧动画/BTB_IDLE_R1";
    const string 帧目录 = 目录 + "/frames";
    const string 配置路径 = "Assets/天帝/Resources/角色帧动画/BTB_IDLE_R1.asset";
    const string 源包 = "C:/Users/123/Documents/ChatGPT/杂谈/生成/帧动画/20261009_战斗角色/output/boss_idle_video_pilot_r1/matte-repair-r1/full-r2";

    [Serializable] sealed class 修复清单
    {
        public string status, revision, source_policy, source_model_requested, source_model_returned;
        public int frame_count, duration_ms;
        public int[] native_size, frame_durations_ms;
        public 修复帧[] frames;
    }
    [Serializable] sealed class 修复帧
    {
        public int frame, source_frame;
        public string candidate_sha256, source_rgb_sha256, baseline_rgba_sha256, candidate_role, verdict;
    }

    [MenuItem("天帝/美术/接入狼王凑活待机候选")]
    public static void 导入()
    {
        string 清单路径 = 帧目录.Substring(0, 帧目录.LastIndexOf('/')) + "/repair-manifest.json";
        var 清单 = JsonUtility.FromJson<修复清单>(File.ReadAllText(清单路径));
        if (清单 == null || 清单.frames == null || 清单.frames.Length != 121 || 清单.frame_durations_ms == null || 清单.frame_durations_ms.Length != 121)
            throw new InvalidDataException("狼王待机候选清单必须包含 121 帧与 121 个时长。");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var 精灵 = new Sprite[清单.frames.Length];
        for (int i = 0; i < 精灵.Length; i++)
        {
            string 路径 = 帧目录 + "/" + i.ToString("000") + ".png";
            var 导入器 = AssetImporter.GetAtPath(路径) as TextureImporter;
            if (导入器 == null) throw new InvalidDataException("候选帧没有 TextureImporter：" + 路径);
            导入器.textureType = TextureImporterType.Sprite;
            导入器.spriteImportMode = SpriteImportMode.Single;
            导入器.mipmapEnabled = false;
            导入器.sRGBTexture = true;
            导入器.filterMode = FilterMode.Bilinear;
            导入器.textureCompression = TextureImporterCompression.Uncompressed;
            导入器.isReadable = false;
            导入器.maxTextureSize = 2048;
            导入器.spritePixelsPerUnit = 100;
            var 纹理设置 = new TextureImporterSettings();
            导入器.ReadTextureSettings(纹理设置);
            纹理设置.spriteMeshType = SpriteMeshType.FullRect;
            纹理设置.spriteAlignment = (int)SpriteAlignment.Custom;
            纹理设置.spritePivot = new Vector2(.5f, 184f / 960f);
            导入器.SetTextureSettings(纹理设置);
            导入器.SaveAndReimport();
            精灵[i] = AssetDatabase.LoadAssetAtPath<Sprite>(路径);
            if (精灵[i] == null || 精灵[i].rect.size != new Vector2(960, 960)) throw new InvalidDataException("候选帧 Sprite 画布不正确：" + 路径);
        }
        string 来源映射路径 = 目录 + "/source-map.json";
        string 来源映射 = 生成来源映射(清单);
        File.WriteAllText(来源映射路径, 来源映射, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(来源映射路径, ImportAssetOptions.ForceSynchronousImport);
        var 配置 = AssetDatabase.LoadAssetAtPath<天帝战斗帧动画>(配置路径);
        if (配置 == null)
        {
            配置 = ScriptableObject.CreateInstance<天帝战斗帧动画>();
            AssetDatabase.CreateAsset(配置, 配置路径);
        }
        配置.编号 = "BTB_IDLE_R1";
        配置.已验收 = true;
        配置.验收清单指纹 = 指纹(清单路径);
        配置.来源清单指纹 = 指纹(Path.GetFullPath(Path.Combine(Application.dataPath, "天帝/Resources/角色帧动画/BTB_IDLE_R1/source-map.json")));
        配置.共同画布 = new Vector2Int(960, 960);
        配置.共同根点 = new Vector2(480, 184);
        配置.每米像素 = 100;
        配置.展示高度 = 9.6f;
        配置.参考移速 = 3.4f;
        配置.动作 = new[] { new 天帝战斗帧动画.方向动作
        {
            朝向 = 角色朝向.南,
            动作 = 战斗帧动作.待机,
            帧 = 精灵,
            每帧秒 = Array.ConvertAll(清单.frame_durations_ms, 毫秒 => 毫秒 / 1000f),
            循环 = true,
            释放帧 = 0
        }};
        EditorUtility.SetDirty(配置);
        AssetDatabase.SaveAssets();
        if (!配置.可用)
        {
            var 首帧 = 配置.动作 != null && 配置.动作.Length > 0 && 配置.动作[0].帧 != null && 配置.动作[0].帧.Length > 0 ? 配置.动作[0].帧[0] : null;
            string 帧信息 = 首帧 == null ? "null" : 首帧.rect.size.ToString();
            string 轴心信息 = 首帧 == null ? "null" : 首帧.pivot.ToString();
            string Ppu信息 = 首帧 == null ? "null" : 首帧.pixelsPerUnit.ToString();
            Debug.LogError("狼王待机契约失败：编号=" + 配置.编号 + " 已验收=" + 配置.已验收 + " 画布=" + 配置.共同画布 + " 根点=" + 配置.共同根点 + " PPU=" + 配置.每米像素 + " 帧=" + 帧信息 + " pivot=" + 轴心信息 + " ppu=" + Ppu信息 + " 时长=" + (配置.动作 == null || 配置.动作.Length == 0 ? 0 : 配置.动作[0].总秒));
            throw new InvalidDataException("狼王待机候选配置未通过帧动画契约。");
        }
        Debug.Log("狼王待机候选已接入：BTB_IDLE_R1，移动与攻击动作留在旧资源。");
    }

    static string 生成来源映射(修复清单 清单)
    {
        var 文本 = new StringBuilder();
        文本.Append("{\n  \"schema\": \"tiandi.boss-idle-source-map.v1\",\n");
        文本.Append("  \"candidate_id\": \"boss_idle_video_pilot_r1/full-r2\",\n");
        文本.Append("  \"status\": \"").Append(清单.status).Append("\",\n");
        文本.Append("  \"revision\": \"").Append(清单.revision).Append("\",\n");
        文本.Append("  \"source_policy\": \"").Append(清单.source_policy).Append("\",\n");
        文本.Append("  \"source_model_requested\": \"").Append(清单.source_model_requested).Append("\",\n");
        文本.Append("  \"source_model_returned\": \"").Append(清单.source_model_returned).Append("\",\n");
        文本.Append("  \"source_package\": \"").Append(源包.Replace("\\", "/")).Append("\",\n");
        文本.Append("  \"frame_count\": 121,\n  \"duration_ms\": ").Append(清单.duration_ms).Append(",\n  \"frames\": [\n");
        for (int i = 0; i < 清单.frames.Length; i++)
        {
            var 帧 = 清单.frames[i];
            文本.Append("    {\"frame\": ").Append(帧.frame).Append(", \"source_frame\": ").Append(帧.source_frame)
                .Append(", \"candidate\": \"frames/").Append(帧.frame.ToString("000"))
                .Append(".png\", \"candidate_sha256\": \"").Append(帧.candidate_sha256)
                .Append("\", \"source_rgb_sha256\": \"").Append(帧.source_rgb_sha256)
                .Append("\", \"baseline_rgba_sha256\": \"").Append(帧.baseline_rgba_sha256)
                .Append("\", \"duration_ms\": ").Append(清单.frame_durations_ms[i])
                .Append(", \"candidate_role\": \"").Append(帧.candidate_role)
                .Append("\", \"verdict\": \"").Append(帧.verdict).Append("\"}");
            if (i + 1 < 清单.frames.Length) 文本.Append(',');
            文本.Append('\n');
        }
        文本.Append("  ]\n}\n");
        return 文本.ToString();
    }

    static string 指纹(string 路径)
    {
        using (var 算法 = SHA256.Create()) using (var 流 = File.OpenRead(路径))
            return BitConverter.ToString(算法.ComputeHash(流)).Replace("-", "").ToLowerInvariant();
    }
}
#endif
