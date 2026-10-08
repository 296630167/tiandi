#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝青绿素材导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/天帝/Resources/青绿界面/")) return;
        var t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
        t.spritePixelsPerUnit = 100; t.alphaIsTransparency = true; t.mipmapEnabled = false;
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        t.textureCompression = TextureImporterCompression.Uncompressed; t.maxTextureSize = 2048;
        bool 面 = assetPath.Contains("面板") || assetPath.Contains("内衬") || assetPath.Contains("卡框");
        bool 键 = assetPath.Contains("按钮") || assetPath.Contains("页签") || assetPath.Contains("输入框");
        t.spriteBorder = 面 ? new Vector4(28,28,28,28) : 键 ? new Vector4(64,20,64,20) : Vector4.zero;
        if (assetPath.Contains("/精修"))
            t.spriteBorder = 面 ? new Vector4(96,96,96,96) : 键 ? new Vector4(80,42,80,42) : Vector4.zero;
        var s = new TextureImporterSettings(); t.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; t.SetTextureSettings(s);
    }
}
#endif
