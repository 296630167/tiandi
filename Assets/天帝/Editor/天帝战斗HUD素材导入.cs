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
        var s = new TextureImporterSettings(); i.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; i.SetTextureSettings(s);
    }
}
#endif
