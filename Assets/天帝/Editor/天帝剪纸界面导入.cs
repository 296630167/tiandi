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
        导入.spriteBorder = assetPath.EndsWith("纸框.png") ? new Vector4(24,24,24,24)
            : assetPath.EndsWith("按钮.png") ? new Vector4(32,16,32,16) : Vector4.zero;
        var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
        设置.spriteMeshType = SpriteMeshType.FullRect; 导入.SetTextureSettings(设置);
    }
}
#endif
