#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class 天帝图三四素材导入:AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/天帝/Resources/山水图三四/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.alphaIsTransparency=true;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
        t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.spritePixelsPerUnit=100;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(s);
    }
}
#endif
