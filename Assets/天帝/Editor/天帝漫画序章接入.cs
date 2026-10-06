#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class 天帝漫画序章接入
{
    public static string 导入()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
        var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (资源 == null) throw new InvalidOperationException("找不到现有美术资源，不创建替代资源。");
        var 项目 = new List<天帝美术资源.图片条目>(资源.图片);
        for (int i = 0; i < 9; i++)
        {
            string 编号 = i < 8 ? "CM" + (i + 1).ToString("00") : "CB01";
            string 路径 = "Assets/天帝/美术/漫画序章_v1/" + 编号 + ".png";
            AssetDatabase.ImportAsset(路径, ImportAssetOptions.ForceSynchronousImport);
            var 导入 = AssetImporter.GetAtPath(路径) as TextureImporter;
            if (导入 == null) throw new InvalidOperationException("缺少漫画素材：" + 路径);
            导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
            导入.mipmapEnabled = false; 导入.alphaIsTransparency = false; 导入.npotScale = TextureImporterNPOTScale.None;
            导入.maxTextureSize = 2048; 导入.textureCompression = TextureImporterCompression.Uncompressed;
            导入.filterMode = FilterMode.Bilinear; 导入.wrapMode = TextureWrapMode.Clamp; 导入.isReadable = false;
            导入.SaveAndReimport();
            var 图片 = AssetDatabase.LoadAssetAtPath<Sprite>(路径);
            if (图片 == null) throw new InvalidOperationException("漫画未导入为Sprite：" + 路径);
            项目.RemoveAll(项 => 项.编号 == 编号);
            项目.Add(new 天帝美术资源.图片条目 { 编号 = 编号, 图片 = 图片 });
        }
        资源.图片 = 项目.ToArray(); EditorUtility.SetDirty(资源); AssetDatabase.SaveAssetIfDirty(资源);
        return "已导入并绑定8页漫画与1张天赋衔接背景；未修改场景。";
    }
}
#endif
