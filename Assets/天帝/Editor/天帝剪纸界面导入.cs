#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝剪纸界面导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/天帝/Resources/剪纸界面/")) return;
        var 导入 = (TextureImporter)assetImporter;
        导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
        导入.alphaIsTransparency = true; 导入.mipmapEnabled = false; 导入.maxTextureSize = 2048;
        导入.textureCompression = TextureImporterCompression.Uncompressed;
        导入.filterMode = FilterMode.Bilinear;
        设置移动平台无损(导入, "Android");
        设置移动平台无损(导入, "iPhone");
        设置移动平台无损(导入, "Standalone");
        导入.spriteBorder = assetPath.EndsWith("纸框.png") ? new Vector4(24,24,24,24)
            : assetPath.EndsWith("按钮.png") ? new Vector4(32,16,32,16) : Vector4.zero;
        var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
        设置.spriteMeshType = SpriteMeshType.FullRect; 导入.SetTextureSettings(设置);
    }

    // UI 纸材包含细线和中文装饰，移动平台压缩会在缩放后形成明显色边和糊边。
    static void 设置移动平台无损(TextureImporter 导入, string 平台)
    {
        var 设置 = 导入.GetPlatformTextureSettings(平台);
        设置.name = 平台;
        设置.overridden = true;
        设置.maxTextureSize = 2048;
        设置.textureCompression = TextureImporterCompression.Uncompressed;
        设置.crunchedCompression = false;
        设置.compressionQuality = 100;
        导入.SetPlatformTextureSettings(设置);
    }
}
#endif
