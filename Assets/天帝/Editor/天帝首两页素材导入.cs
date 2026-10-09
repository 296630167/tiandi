#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class 天帝首两页素材导入:AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/天帝/Resources/山水首两页/"))return;
        var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
        t.alphaIsTransparency=true;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
        t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.spritePixelsPerUnit=100;
        设置移动平台无损(t, "Android"); 设置移动平台无损(t, "iPhone"); 设置移动平台无损(t, "Standalone");
        var 设置=new TextureImporterSettings();t.ReadTextureSettings(设置);设置.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(设置);
        if(assetPath.EndsWith("材料面板.png")||assetPath.EndsWith("目标面板.png")||assetPath.EndsWith("主页信息卷.png")||assetPath.EndsWith("结果框.png"))
        {
            var 原=System.IO.File.ReadAllBytes(assetPath);var 图=new Texture2D(2,2);图.LoadImage(原);
            t.spriteBorder=new Vector4(图.width*.28f,图.height*.28f,图.width*.32f,图.height*.24f);Object.DestroyImmediate(图);
        }
    }
    static void 设置移动平台无损(TextureImporter t, string 平台)
    {
        var s = t.GetPlatformTextureSettings(平台); s.name = 平台; s.overridden = true;
        s.maxTextureSize = 2048; s.textureCompression = TextureImporterCompression.Uncompressed;
        s.crunchedCompression = false; s.compressionQuality = 100; t.SetPlatformTextureSettings(s);
    }
}
#endif
