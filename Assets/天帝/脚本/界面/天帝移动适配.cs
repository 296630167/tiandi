using UnityEngine;

// 平台判断集中在这里；触屏电脑继续使用PC操作。验证覆盖只在Editor编译。
public static class 天帝移动适配
{
    public static readonly Vector2Int 固定分辨率 = new Vector2Int(1920, 1080);
    // 两套交互保持各自的字号与触控尺寸，整体映射到同一个1080p画面。
    public static Vector2 布局尺寸 => 启用 ? new Vector2(640, 360) : new Vector2(1600, 900);
#if UNITY_EDITOR
    public static bool? 验证移动平台;
    public static float? 验证密度;
    public static Vector2Int? 验证屏幕尺寸;
    public static Rect? 验证安全区;
#endif
    public static Vector2Int 屏幕尺寸
    {
        get
        {
#if UNITY_EDITOR
            if (验证屏幕尺寸.HasValue) return 验证屏幕尺寸.Value;
#endif
            return new Vector2Int(Screen.width, Screen.height);
        }
    }
    public static Rect 安全区
    {
        get
        {
#if UNITY_EDITOR
            if (验证安全区.HasValue) return 验证安全区.Value;
#endif
            return Screen.safeArea;
        }
    }
    public static bool 启用
    {
        get
        {
#if UNITY_EDITOR
            if (验证移动平台.HasValue) return 验证移动平台.Value;
#endif
            return Application.isMobilePlatform;
        }
    }
    public static float 像素密度
    {
        get
        {
            float 密度;
#if UNITY_EDITOR
            if (验证密度.HasValue) 密度 = Mathf.Max(.5f, 验证密度.Value);
            else
#endif
            // 部分Android机型报告0或错误DPI；按短边约420逻辑像素回退。
            密度 = Screen.dpi >= 100 && Screen.dpi <= 640 ? Screen.dpi / 160f : Mathf.Max(1, Mathf.Min(屏幕尺寸.x, 屏幕尺寸.y) / 420f);
            // 高密度的小屏幕仍需容纳分区与44像素触控行，安全区内至少560×360逻辑像素。
            var 区 = 有效安全区(安全区, 屏幕尺寸);
            return Mathf.Max(.5f, Mathf.Min(密度, 区.width / 560f, 区.height / 360f));
        }
    }
    public static Rect 有效安全区(Rect 区, Vector2Int 尺寸)
    {
        float 左 = Mathf.Clamp(区.xMin, 0, 尺寸.x), 下 = Mathf.Clamp(区.yMin, 0, 尺寸.y);
        float 右 = Mathf.Clamp(区.xMax, 左, 尺寸.x), 上 = Mathf.Clamp(区.yMax, 下, 尺寸.y);
        return 右 - 左 > 1 && 上 - 下 > 1 ? Rect.MinMaxRect(左, 下, 右, 上) : new Rect(0, 0, 尺寸.x, 尺寸.y);
    }
    public static Rect 横屏视口(Rect 区)
    {
        float 比例 = Mathf.Min(区.width / 固定分辨率.x, 区.height / 固定分辨率.y);
        var 大小 = (Vector2)固定分辨率 * 比例;
        return new Rect(区.center - 大小 * .5f, 大小);
    }
    public static float 显示比例(Rect 区) => Mathf.Max(.01f, Mathf.Min(区.width / 固定分辨率.x, 区.height / 固定分辨率.y));
    // 启动画面前限制方向，直接从战斗场景启动也不会进入竖屏。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void 设置横屏()
    {
        if (!Application.isEditor)
            Screen.SetResolution(固定分辨率.x, 固定分辨率.y, 启用 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        if (!启用) return;
        Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}
