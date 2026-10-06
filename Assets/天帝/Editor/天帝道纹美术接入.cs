#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class 天帝道纹美术接入
{
    [Serializable] sealed class 条目 { public string name, id; public int width, height, border, borderX, borderY; public bool atlas; }
    [Serializable] sealed class 清单 { public 条目[] sprites; }
    const string 目录 = "Assets/天帝/美术/界面/敦煌彩绘/";
    [MenuItem("天帝/美术/接入道纹构筑界面美术")]
    public static void 导入菜单() => 导入();
    public static string 导入()
    {
        int 数 = 导入目录(目录);
        return "已接入" + 数 + "项敦煌彩绘美术与石庭主页，已保留旧素材与场景";
    }
    public static string 导入主页()
    {
        int 数 = 导入目录("Assets/天帝/美术/界面/D概念主页/");
        var 美术 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        美术.主页标题字体 = AssetDatabase.LoadAssetAtPath<Font>("Assets/天帝/美术/字体/SourceHanSerifSC-Bold.otf");
        if (美术.主页标题字体 == null) throw new InvalidOperationException("D主页粗宋体未导入");
        EditorUtility.SetDirty(美术); AssetDatabase.SaveAssets();
        return "已接入" + 数 + "项D概念主页专用素材与粗宋体，不修改场景和共用皮肤";
    }
    static int 导入目录(string 路径)
    {
        var 清 = JsonUtility.FromJson<清单>(File.ReadAllText(路径 + "导入清单.json"));
        var 美术 = AssetDatabase.LoadAssetAtPath<天帝美术资源>("Assets/天帝/美术/天帝美术资源.asset");
        if (美术 == null) throw new InvalidOperationException("已有美术资源资产缺失");
        foreach (var 项 in 清.sprites)
        {
            string 路 = 路径 + 项.name + ".png";
            AssetDatabase.ImportAsset(路);
            var 入 = (TextureImporter)AssetImporter.GetAtPath(路);
            入.textureType = 项.atlas ? TextureImporterType.Default : TextureImporterType.Sprite;
            入.spriteImportMode = SpriteImportMode.Single;
            var 精灵设置 = new TextureImporterSettings(); 入.ReadTextureSettings(精灵设置);
            精灵设置.spriteMeshType = SpriteMeshType.FullRect; 入.SetTextureSettings(精灵设置); 入.spritePixelsPerUnit = 100;
            入.spriteBorder = 项.borderX > 0 || 项.borderY > 0 ? new Vector4(项.borderX, 项.borderY, 项.borderX, 项.borderY) : Vector4.one * 项.border;
            入.alphaIsTransparency = true; 入.mipmapEnabled = false;
            入.sRGBTexture = true; 入.textureCompression = TextureImporterCompression.Uncompressed;
            入.wrapMode = TextureWrapMode.Clamp; 入.filterMode = FilterMode.Bilinear;
            入.maxTextureSize = 2048; 入.SaveAndReimport();
            if (项.atlas) 美术.道纹构筑图集 = AssetDatabase.LoadAssetAtPath<Texture2D>(路);
            else
            {
                var 精灵 = AssetDatabase.LoadAssetAtPath<Sprite>(路);
                if (精灵 == null) throw new InvalidOperationException("精灵导入失败：" + 路);
                string 编号 = string.IsNullOrEmpty(项.id) ? "DWUI_" + 项.name : 项.id;
                var 已有 = 美术.图片.FirstOrDefault(p => p.编号 == 编号);
                if (已有 != null) 已有.图片 = 精灵;
                else 美术.图片 = 美术.图片.Concat(new[] { new 天帝美术资源.图片条目 { 编号 = 编号, 图片 = 精灵 } }).ToArray();
            }
        }
        EditorUtility.SetDirty(美术); AssetDatabase.SaveAssets();
        return 清.sprites.Length;
    }
}
#endif
