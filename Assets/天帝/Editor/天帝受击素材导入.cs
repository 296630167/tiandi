#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class 天帝受击素材导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/天帝/Resources/受击反馈/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.spritePixelsPerUnit=100;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.maxTextureSize=512;t.wrapMode=TextureWrapMode.Clamp;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Center;t.SetTextureSettings(s);
    }
}
#endif
