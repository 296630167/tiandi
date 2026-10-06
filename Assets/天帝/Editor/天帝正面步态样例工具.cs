#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 仅接入经过2D骨架烘焙的正面样例。其他方向复用现有资源；试玩写入隔离存档。
public static class 天帝正面步态样例工具
{
    const string 目录 = "Assets/天帝/美术/角色/主角正面骨骼样例";
    const string 配置路径 = 目录 + "/主角正面步态样例.asset";
    const string 原配置路径 = "Assets/天帝/美术/角色/主角八方向/主角八方向帧动画.asset";
    const string 资源路径 = "Assets/天帝/美术/天帝美术资源.asset";
    const string 主场景 = "Assets/天帝/场景/天帝.unity";
    static string 试玩目录, 原存档目录;
    static EnterPlayModeOptions 原选项;
    static bool 原选项启用, 正在等待, 已进入战斗;
    static double 截止;

    [MenuItem("天帝/美术/接入正面2D骨骼步态样例")]
    public static void 接入菜单() => Debug.Log(接入());
    public static string 接入()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止运行后更新动画资源。");
        string[] 文件 = Enumerable.Range(1,30).Select(i=>目录+"/主角_移动_南_"+i.ToString("00")+".png")
            .Concat(new[]{目录+"/主角_南_待机.png"}).ToArray();
        foreach (var 路 in 文件) if (!File.Exists(路)) throw new FileNotFoundException("正面样例素材不完整。",路);
        var 原配置 = AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(原配置路径);
        var 资源 = AssetDatabase.LoadAssetAtPath<天帝美术资源>(资源路径);
        if (原配置 == null || !原配置.完整 || 资源 == null) throw new InvalidOperationException("已有八方向配置或美术资源缺失。");
        AssetDatabase.Refresh();
        foreach(var 路 in 文件)
        {
            var 导入 = (TextureImporter)AssetImporter.GetAtPath(路);
            导入.textureType=TextureImporterType.Sprite; 导入.spriteImportMode=SpriteImportMode.Single;
            导入.alphaIsTransparency=true; 导入.mipmapEnabled=false; 导入.isReadable=false;
            导入.sRGBTexture=true; 导入.wrapMode=TextureWrapMode.Clamp; 导入.filterMode=FilterMode.Bilinear;
            导入.spritePixelsPerUnit=100; 导入.maxTextureSize=512;
            导入.textureCompression=TextureImporterCompression.Uncompressed;
            var 设置=new TextureImporterSettings(); 导入.ReadTextureSettings(设置);
            设置.spriteMeshType=SpriteMeshType.FullRect; 设置.spriteAlignment=(int)SpriteAlignment.Custom;
            设置.spritePivot=new Vector2(.5f,48f/512); 导入.SetTextureSettings(设置);
            导入.SaveAndReimport();
        }
        var 配置=AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(配置路径);
        if(配置==null){配置=UnityEngine.Object.Instantiate(原配置);配置.name="主角正面步态样例";AssetDatabase.CreateAsset(配置,配置路径);}
        else EditorUtility.CopySerialized(原配置,配置);
        var 南=配置.获取(角色朝向.南);
        南.待机=AssetDatabase.LoadAssetAtPath<Sprite>(目录+"/主角_南_待机.png");
        南.移动帧=Enumerable.Range(1,30).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(目录+"/主角_移动_南_"+i.ToString("00")+".png")).ToArray();
        南.移动帧率=25; // 30帧/1.2秒；其余方向仍沿用各自的原始节奏。
        验证配置(配置,原配置);
        资源.主角移动动画=配置;EditorUtility.SetDirty(配置);EditorUtility.SetDirty(资源);AssetDatabase.SaveAssets();
        return "正面2D骨骼烘焙样例已绑定：30帧、1.2秒、512共同画布与固定脚下轴心；其余方向沿用原八方向素材。";
    }

    static void 验证配置(天帝角色帧动画 配置,天帝角色帧动画 原配置)
    {
        if(!配置.完整)throw new InvalidOperationException("样例配置缺少方向或精灵。");
        var 南=配置.获取(角色朝向.南);
        if(南.移动帧.Length!=30||Mathf.Abs(南.移动帧.Length/南.移动帧率-1.2f)>.001f)throw new InvalidOperationException("样例帧数或周期不符。");
        foreach(var 帧 in 南.移动帧.Concat(new[]{南.待机}))
            if(帧.rect.size!=new Vector2(512,512)||Vector2.Distance(帧.pivot,new Vector2(256,48))>.01f)
                throw new InvalidOperationException("样例尺寸或脚下轴心不一致："+帧.name);
        for(int i=1;i<8;i++)
        {
            var 项=配置.获取((角色朝向)i);var 原项=原配置.获取((角色朝向)i);
            if(项.待机!=原项.待机||!项.移动帧.SequenceEqual(原项.移动帧))throw new InvalidOperationException("样例意外修改其他方向。");
        }
        var 播放=new 天帝八方向播放(配置);var 已见=new HashSet<Sprite>();
        for(int i=0;i<30;i++)已见.Add(播放.推进(Vector2.down*.24f,.04f,true));
        if(已见.Count!=30||已见.Any(p=>!南.移动帧.Contains(p)))throw new InvalidOperationException("正面循环没有播放全部30帧。");
        if(播放.推进(Vector2.zero,.1f,true)!=南.待机)throw new InvalidOperationException("零位移仍在踩步。");
        播放.推进(Vector2.right*.6f,.1f,true);
        if(!配置.获取(角色朝向.东).移动帧.Contains(播放.当前精灵))throw new InvalidOperationException("转向没有回到对应方向动画。");
        var 停下=播放.推进(Vector2.zero,.1f,true);
        if(播放.推进(Vector2.down,.1f,false)!=停下)throw new InvalidOperationException("死亡仍在播放移动。");
    }

    [MenuItem("天帝/美术/在战斗中查看正面步态样例")]
    public static void 查看菜单()=>Debug.Log(查看());
    public static string 查看()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("编辑器已在运行，保持当前试玩。");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("当前有未保存场景，不能切换试玩场景。");
        if(EditorSceneManager.GetActiveScene().path!=主场景)EditorSceneManager.OpenScene(主场景);
        var 资源=AssetDatabase.LoadAssetAtPath<天帝美术资源>(资源路径);
        if(资源?.主角移动动画!=AssetDatabase.LoadAssetAtPath<天帝角色帧动画>(配置路径))throw new InvalidOperationException("正面样例尚未绑定。");
        试玩目录=Path.Combine(天帝构建工具.项目根,"生成/验证/正面步态试玩-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        原存档目录=天帝存档.验证目录;
        var 存档=new 天帝存档().读取();
        if(存档==null)
        {
            var 网=new 天帝道纹(42,天帝天赋.获取((int)天赋种类.普通人));
            存档=new 天帝存档数据{序章已完成=true,主角=天帝普攻.主角配置(),画布=网.导出存档(),通货=new 天帝通货(网,42).导出库存()};
        }
        if(!new 天帝存档(Path.Combine(试玩目录,"隔离存档")).保存(存档))throw new InvalidOperationException("试玩隔离存档创建失败。");
        天帝存档.验证目录=Path.Combine(试玩目录,"隔离存档");
        原选项启用=EditorSettings.enterPlayModeOptionsEnabled;原选项=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        已进入战斗=false;正在等待=true;截止=EditorApplication.timeSinceStartup+45;
        EditorApplication.update+=等待场景;EditorApplication.playModeStateChanged+=退出清理;
        EditorApplication.isPlaying=true;
        return "正在进入青岚原；试玩采用隔离存档。按S/下方向键查看正面走路，Shift加速，松开停止。";
    }
    static void 等待场景()
    {
        if(!正在等待)return;
        if(EditorApplication.timeSinceStartup>截止){Debug.LogError("正面步态试玩进入战斗超时。");EditorApplication.isPlaying=false;清理();return;}
        if(!EditorApplication.isPlaying)return;
        var 游戏=UnityEngine.Object.FindAnyObjectByType<天帝游戏>();if(游戏==null)return;
        try
        {
            if(!已进入战斗)
            {
                if(!游戏.继续游戏()||!游戏.进入战斗())throw new InvalidOperationException("无法从隔离存档进入战斗。");
                已进入战斗=true;return;
            }
            if(游戏.阶段!=游戏阶段.战斗||游戏.战斗场景?.美术==null)return;
            var 场=游戏.战斗场景;
            var 像=场.GetComponentsInChildren<SpriteRenderer>().Single(p=>p.name=="程序员主角");
            var 南=游戏.美术.主角移动动画.获取(角色朝向.南);
            if(!场.美术.可用||像.sprite!=南.待机)throw new InvalidOperationException("真实战场没有使用样例正面待机。");
            var 起点=场.玩家位置;
            场.移动一步(Vector2.down,false,.04f);场.美术.更新(场.战斗,场.玩家位置,.04f);
            if(!南.移动帧.Contains(像.sprite)||像.flipX)throw new InvalidOperationException("真实战场没有播放样例移动帧。");
            if(Vector2.Distance(new Vector2(像.transform.position.x,像.transform.position.z),场.玩家位置)>.001f)
                throw new InvalidOperationException("主角根位置没有对齐实际碰撞位置。");
            float 画布高度=像.sprite.bounds.size.y*像.transform.localScale.y;
            if(Mathf.Abs(画布高度-游戏.美术.主角移动动画.展示高度)>.001f)throw new InvalidOperationException("样例分辨率改变了主角显示比例。");
            场.美术.更新(场.战斗,场.玩家位置,.1f);
            if(像.sprite!=南.待机)throw new InvalidOperationException("战场零位移没有停止样例。");
            // 只拍一张真实战斗相机图验证接入；相机、场景和存档不另行保存。
            var 相机=场.俯视相机;var 原目标=相机.targetTexture;var 原活动=RenderTexture.active;
            var 缓冲=RenderTexture.GetTemporary(1280,720,24);var 图=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{相机.targetTexture=缓冲;相机.Render();RenderTexture.active=缓冲;图.ReadPixels(new Rect(0,0,1280,720),0,0);图.Apply();File.WriteAllBytes(Path.Combine(试玩目录,"战斗正面样例.png"),图.EncodeToPNG());}
            finally{相机.targetTexture=原目标;RenderTexture.active=原活动;RenderTexture.ReleaseTemporary(缓冲);UnityEngine.Object.DestroyImmediate(图);}
            File.WriteAllText(Path.Combine(试玩目录,"接入检查.txt"),"正面30帧循环、独立周期、零位移停止、其他方向、死亡冻结及真实战场的精灵绑定、根位置与画布比例检查通过。\n按S查看正面样例；正式存档未参与试玩写入。");
            正在等待=false;EditorApplication.update-=等待场景;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.isPlaying=false;清理();}
    }
    static void 退出清理(PlayModeStateChange 状态){if(状态==PlayModeStateChange.EnteredEditMode)清理();}
    static void 清理()
    {
        正在等待=false;EditorApplication.update-=等待场景;EditorApplication.playModeStateChanged-=退出清理;
        天帝存档.验证目录=原存档目录;EditorSettings.enterPlayModeOptionsEnabled=原选项启用;EditorSettings.enterPlayModeOptions=原选项;
    }
}
#endif
