#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class 天帝独立敌人导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/天帝/Resources/敌人独立美术/")) return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.spritePixelsPerUnit=100;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
        t.mipmapEnabled=false;t.maxTextureSize=1024;t.wrapMode=TextureWrapMode.Clamp;
        t.textureCompression=TextureImporterCompression.CompressedHQ;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);
        s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Custom;
        s.spritePivot=assetPath.Contains("BFX")?new Vector2(.5f,.5f):new Vector2(.5f,.1f);
        t.SetTextureSettings(s);
    }
}
#endif
