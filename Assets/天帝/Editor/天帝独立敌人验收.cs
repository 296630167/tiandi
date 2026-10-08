#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class 天帝独立敌人验收
{
    public static void 运行()
    {
        var seen=new HashSet<Sprite>();int checks=0;
        var art=ScriptableObject.CreateInstance<天帝美术资源>();
        try
        {
            for(int i=15;i<=21;i++)
            {
                string code="BTV"+i;
                var s=art.获取(code);var fx=art.获取("BFX"+i);
                if(s==null||fx==null||!seen.Add(s))throw new Exception("独立素材缺失或重复："+code);checks+=3;
                if(art.获取(code)!=s||天帝敌种配置.立绘颜色(i)!=Color.white)throw new Exception("缓存或白色基色错误");checks+=2;
                var t=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s));
                if(t.spriteImportMode!=SpriteImportMode.Single||t.mipmapEnabled||!t.alphaIsTransparency||Mathf.Abs(t.spritePivot.y-.1f)>.01f)throw new Exception("导入参数错误");checks++;
                if(s.bounds.size.x<=0||s.bounds.size.y<=0||fx.bounds.size.x<=0)throw new Exception("素材尺寸错误");checks++;
            }
            string dir=Path.Combine(Application.dataPath,"../output/敌人独立美术");Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir,"Unity验收.txt"),"独立原画7种，独立点缀7种；资源加载/去重/缓存/白色基色/导入/尺寸检查="+checks+"；失败=0\n");
        }
        finally{UnityEngine.Object.DestroyImmediate(art);}
    }
}
#endif
