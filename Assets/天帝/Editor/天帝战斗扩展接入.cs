#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class 天帝战斗扩展接入
{
    public static string 导入()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式。");
        var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (资源 == null) throw new InvalidOperationException("缺少现有美术资源。");
        var 项目 = new List<天帝美术资源.图片条目>(资源.图片);
        foreach (string 类 in new[] { "N", "E", "B" })
        for (int i = 1; i <= (类 == "N" ? 12 : 类 == "E" ? 2 : 5); i++)
        {
            string 编号 = "BT" + 类 + i.ToString("00"), 路径 = "Assets/天帝/美术/战斗扩展_v1/" + 编号 + ".png";
            AssetDatabase.ImportAsset(路径, ImportAssetOptions.ForceSynchronousImport);
            var 导入 = AssetImporter.GetAtPath(路径) as TextureImporter;
            if (导入 == null) throw new InvalidOperationException("缺少立绘 " + 路径);
            导入.textureType = TextureImporterType.Sprite; 导入.spriteImportMode = SpriteImportMode.Single;
            导入.spritePixelsPerUnit = 100; 导入.spritePivot = new Vector2(.5f, .0625f);
            var 设置 = new TextureImporterSettings(); 导入.ReadTextureSettings(设置); 设置.spriteAlignment = 9; 设置.spritePivot = new Vector2(.5f, .0625f); 设置.spriteMeshType = SpriteMeshType.FullRect; 导入.SetTextureSettings(设置);
            导入.alphaIsTransparency = true; 导入.mipmapEnabled = false; 导入.npotScale = TextureImporterNPOTScale.None;
            导入.maxTextureSize = 1024; 导入.textureCompression = TextureImporterCompression.Compressed;
            导入.filterMode = FilterMode.Bilinear; 导入.wrapMode = TextureWrapMode.Clamp; 导入.isReadable = false;
            导入.SaveAndReimport(); var 图 = AssetDatabase.LoadAssetAtPath<Sprite>(路径);
            if (图 == null) throw new InvalidOperationException("导入失败 " + 编号);
            项目.RemoveAll(a => a.编号 == 编号); 项目.Add(new 天帝美术资源.图片条目 { 编号 = 编号, 图片 = 图 });
        }
        资源.图片 = 项目.ToArray(); EditorUtility.SetDirty(资源); AssetDatabase.SaveAssetIfDirty(资源);
        return "19张敌人与狼王形态立绘已绑定现有美术资源；场景未重建。";
    }
}
#endif
