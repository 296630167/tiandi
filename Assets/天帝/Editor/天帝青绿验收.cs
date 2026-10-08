#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class 天帝青绿验收
{
    public static string 图片目录;
    [Serializable] sealed class 验收结果 { public List<string> 通过, 失败, 错误; }
    public static void 运行()
    {
        图片目录 = Path.Combine(天帝构建工具.项目根, "生成/验证/青绿UI截图"); Directory.CreateDirectory(图片目录);
        var dir = 天帝双端页面验证.运行(); Debug.Log("青绿UI验收报告: " + dir);
        File.WriteAllText(Path.Combine(天帝构建工具.项目根, "生成/验证/青绿UI报告路径.txt"), dir);
        var 结果 = JsonUtility.FromJson<验收结果>(File.ReadAllText(Path.Combine(dir, "report.json")));
        if (结果.失败.Count > 0 || 结果.错误.Count > 0)
            throw new Exception("青绿UI验收未通过：失败 " + 结果.失败.Count + "，错误 " + 结果.错误.Count + "。报告：" + dir);
        Debug.Log("青绿UI验收通过：" + 结果.通过.Count + " 项，0失败、0错误。");
    }
    public static void 拍摄(GameObject host, string 名, int 宽, int 高)
    {
        if (string.IsNullOrEmpty(图片目录)) return;
        var rt = new RenderTexture(宽, 高, 24); rt.Create();
        var obj = new GameObject("隔离UI拍摄", typeof(Camera)); obj.hideFlags = HideFlags.HideAndDontSave;
        var cam = obj.GetComponent<Camera>(); cam.orthographic = true;
        cam.orthographicSize = ((RectTransform)host.transform).rect.height / 2;
        cam.aspect = (float)宽 / 高; cam.transform.position = host.transform.position + new Vector3(0,0,-20);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.targetTexture = rt;
        var canvas = host.GetComponent<Canvas>(); var oldCam = canvas.worldCamera; canvas.worldCamera = cam;
        var group = host.GetComponent<CanvasGroup>(); float alpha = group.alpha; group.alpha = 1;
        var old = RenderTexture.active; Texture2D tex = null;
        try
        {
            Canvas.ForceUpdateCanvases(); cam.Render(); RenderTexture.active = rt;
            tex = new Texture2D(宽, 高, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0,0,宽,高),0,0); tex.Apply();
            File.WriteAllBytes(Path.Combine(图片目录, 名 + ".png"), tex.EncodeToPNG());
        }
        finally
        {
            group.alpha = alpha; canvas.worldCamera = oldCam; RenderTexture.active = old;
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(obj); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
