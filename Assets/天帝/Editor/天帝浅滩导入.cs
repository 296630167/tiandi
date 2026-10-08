#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class 天帝浅滩导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(assetPath!="Assets/天帝/Resources/随机战场/浅滩.png")return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.spritePixelsPerUnit=100;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
        t.mipmapEnabled=false;t.maxTextureSize=1024;t.wrapMode=TextureWrapMode.Clamp;
        t.textureCompression=TextureImporterCompression.CompressedHQ;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Center;t.SetTextureSettings(s);
    }
}
#endif
