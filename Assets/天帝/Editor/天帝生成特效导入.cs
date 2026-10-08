#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝生成特效导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(assetPath!="Assets/天帝/Resources/战斗特效/战斗特效图集.png")return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaSource=TextureImporterAlphaSource.FromInput;
        t.alphaIsTransparency=true;t.mipmapEnabled=false;t.isReadable=false;t.npotScale=TextureImporterNPOTScale.None;
        t.maxTextureSize=2048;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
        t.textureCompression=TextureImporterCompression.Uncompressed;
    }
}
#endif
