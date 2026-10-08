#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public sealed class 天帝润色素材导入 : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool motion=assetPath.StartsWith("Assets/天帝/Resources/敌人动作/");
        bool atlas=assetPath.StartsWith("Assets/天帝/Resources/独立攻击形态/");
        if(!motion&&!atlas)return;
        var t=(TextureImporter)assetImporter;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;
        t.textureCompression=TextureImporterCompression.CompressedHQ;t.maxTextureSize=motion?512:2048;
        if(!motion)return;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
        var s=new TextureImporterSettings();t.ReadTextureSettings(s);s.spriteMeshType=SpriteMeshType.FullRect;s.spriteAlignment=(int)SpriteAlignment.Custom;s.spritePivot=new Vector2(.5f,.12f);t.SetTextureSettings(s);
    }
    void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/天帝/Resources/战斗润色音效/"))return;
        var t=(AudioImporter)assetImporter;t.forceToMono=true;
        var s=t.defaultSampleSettings;s.preloadAudioData=true;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.ADPCM;s.sampleRateSetting=AudioSampleRateSetting.OptimizeSampleRate;t.defaultSampleSettings=s;
    }
}
#endif
