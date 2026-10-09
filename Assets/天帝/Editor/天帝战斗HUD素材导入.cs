#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝战斗HUD素材导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/天帝/Resources/山水战斗HUD/") || !assetPath.EndsWith(".png")) return;
        var i = (TextureImporter)assetImporter; i.textureType = TextureImporterType.Sprite;
        i.spriteImportMode = SpriteImportMode.Single; i.spritePixelsPerUnit = 100;
        i.alphaIsTransparency = true; i.mipmapEnabled = false; i.npotScale = TextureImporterNPOTScale.None;
        i.textureCompression = TextureImporterCompression.Uncompressed; i.filterMode = FilterMode.Bilinear;
        i.maxTextureSize = 512; i.wrapMode = TextureWrapMode.Clamp;
        设置平台无损(i, "Android"); 设置平台无损(i, "iPhone"); 设置平台无损(i, "Standalone");
        var s = new TextureImporterSettings(); i.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; i.SetTextureSettings(s);
    }
    static void 设置平台无损(TextureImporter i, string 平台)
    {
        var s = i.GetPlatformTextureSettings(平台); s.name = 平台; s.overridden = true;
        s.maxTextureSize = 512; s.textureCompression = TextureImporterCompression.Uncompressed;
        s.crunchedCompression = false; s.compressionQuality = 100; i.SetPlatformTextureSettings(s);
    }
}
#endif
