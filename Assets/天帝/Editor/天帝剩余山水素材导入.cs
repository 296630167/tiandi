#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝剩余山水素材导入 : AssetPostprocessor
{
    [InitializeOnLoadMethod]
    static void 补齐导入()
    {
        EditorApplication.delayCall += () =>
        {
            const string 目录 = "Assets/天帝/Resources/山水剩余界面";
            if (!System.IO.Directory.Exists(目录)) return;
            foreach (var p in System.IO.Directory.GetFiles(目录, "*.png"))
            {
                var 路径 = p.Replace('\\', '/');
                var t = AssetImporter.GetAtPath(路径) as TextureImporter;
                if (t == null) continue;
                if (t.textureType != TextureImporterType.Sprite || t.mipmapEnabled ||
                    t.textureCompression != TextureImporterCompression.Uncompressed ||
                    t.spriteImportMode != SpriteImportMode.Single || t.spriteBorder != 预期边框(路径) ||
                    t.maxTextureSize != 2048 || t.wrapMode != TextureWrapMode.Clamp || t.filterMode != FilterMode.Bilinear)
                    AssetDatabase.ImportAsset(路径, ImportAssetOptions.ForceUpdate);
            }
        };
    }
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/天帝/Resources/山水剩余界面/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
        t.alphaIsTransparency = true; t.mipmapEnabled = false; t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear; t.textureCompression = TextureImporterCompression.Uncompressed;
        t.maxTextureSize = 2048; t.spritePixelsPerUnit = 100;
        设置移动平台无损(t, "Android"); 设置移动平台无损(t, "iPhone"); 设置移动平台无损(t, "Standalone");
        t.spriteBorder = 预期边框(assetPath);
        var s = new TextureImporterSettings(); t.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; t.SetTextureSettings(s);
    }
    static void 设置移动平台无损(TextureImporter t, string 平台)
    {
        var s = t.GetPlatformTextureSettings(平台); s.name = 平台; s.overridden = true;
        s.maxTextureSize = 2048; s.textureCompression = TextureImporterCompression.Uncompressed;
        s.crunchedCompression = false; s.compressionQuality = 100; t.SetPlatformTextureSettings(s);
    }
    static Vector4 预期边框(string 路径)
    {
        var 名 = System.IO.Path.GetFileNameWithoutExtension(路径);
        if (名.EndsWith("背景")) return Vector4.zero;
        var 边 = new Vector4(170, 190, 140, 140);
        if (名 == "红叶弹窗" || 名 == "图录卡" || 名 == "回收卡" || 名 == "背包卡片" || 名 == "长卷详情")
            边 = new Vector4(280, 135, 170, 90);
        if (名 == "详情长卷" || 名 == "竖卷详情" || 名 == "天赋卡片") 边 = new Vector4(150, 220, 150, 140);
        return 边;
    }
}
#endif
