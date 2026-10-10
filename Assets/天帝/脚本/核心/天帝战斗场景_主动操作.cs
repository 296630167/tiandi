using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class 天帝战斗场景
{
    public Vector2 瞄准方向 { get; private set; } = Vector2.right;
    float 闪避剩余;
    Vector2 闪避方向;
    bool 输入已冻结;
    float 自动卡住秒;
    int 自动绕行符号 = 1;
    // 纯 AI 移动状态需要有记忆，避免目标、环绕方向和绕行方向在相邻帧之间抖动。
    战斗敌人 自动移动目标;
    bool 自动环绕中;
    int 自动环绕符号 = 1;
    float 自动绕行锁定剩余;
    Vector2 自动绕行方向;
    Vector2 自动平滑方向;
    Vector2 自动平滑方向速度;
    float 自动目标切换冷却;
    static bool 有效方向(Vector2 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y)
        && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && v.sqrMagnitude > .000001f;
    public bool 可以主动操作 => 游戏 != null && 游戏.阶段 == 游戏阶段.战斗 && 战斗 != null
        && !战斗.纯AI模式 && !战斗.玩家死亡 && !游戏.界面.战斗已暂停 && !游戏.界面.新手指引冻结战斗;
    public bool 设置屏幕瞄准(Vector2 屏幕)
    {
        if (俯视相机 == null) return false;
        var 射线 = 俯视相机.ScreenPointToRay(屏幕);
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(射线, out float 距)) return false;
        var 点 = 射线.GetPoint(距); var 向 = new Vector2(点.x, 点.z) - 玩家位置;
        if (向.sqrMagnitude < .000001f) return false;
        瞄准方向 = 向.normalized; return true;
    }
    public void 设置触控瞄准(Vector2 向)
    { if (有效方向(向)) 瞄准方向 = 向.normalized; }
    public 战斗操作结果 释放技能(int 槽)
    {
        var 结果 = 可以主动操作 ? 战斗.尝试释放技能(槽, 瞄准方向, 玩家位置) : 战斗操作结果.无法操作;
        if (可以主动操作) 游戏.界面.显示战斗操作反馈(结果, 槽, false);
        游戏?.界面?.更新战斗状态(); return 结果;
    }
    void 自动战斗操作反馈(战斗操作结果 结果, int 槽, bool 闪)
    {
        if (战斗?.纯AI模式 == true) 游戏?.界面?.显示自动战斗反馈(结果, 槽, 闪);
        else if (可以主动操作) 游戏?.界面?.显示战斗操作反馈(结果, 槽, 闪);
    }
    public 战斗操作结果 闪避()
    {
        if (!可以主动操作) return 战斗操作结果.无法操作;
        var 结果 = 战斗.尝试闪避();
        if (结果 == 战斗操作结果.成功)
        {
            // 闪避沿最后一次确认的瞄准方向，避免左摇杆残留输入把闪避带向另一侧。
            // 移动端技能拖拽与桌面鼠标都会更新同一个瞄准方向。
            闪避方向 = 有效方向(瞄准方向) ? 瞄准方向.normalized : Vector2.right;
            闪避剩余 = (float)天帝数值.取("player.dodge_duration"); 平滑输入 = Vector2.zero;
            天帝声音.提示("润色_跑步");
        }
        游戏.界面.显示战斗操作反馈(结果, -1, true); return 结果;
    }
    void 读取主动操作()
    {
        if (天帝移动适配.启用 || !可以主动操作 || 战斗?.纯AI模式 == true) return;
        // 技能按钮位于战场画布上，鼠标悬停或点击按钮时不能把按钮坐标当成战场瞄准点。
        // 保留最后一次有效战场方向，数字键与点击技能都沿同一方向释放。
        if (Mouse.current != null && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            设置屏幕瞄准(Mouse.current.position.ReadValue());
        var 选 = EventSystem.current?.currentSelectedGameObject;
        if (选 != null && 选.GetComponentInParent<InputField>() != null) return;
        var 键 = Keyboard.current; if (键 == null) return;
        // 使用明确的键位引用，避免不同键盘布局/输入设备在连续枚举值上的差异。
        // 同时接受数字区和小键盘 1-6，WASD 按住时仍可在同一帧触发技能。
        for (int i = 0; i < 6; i++) if (数字技能键按下(键, i)) 释放技能(i);
        if (键.spaceKey.wasPressedThisFrame) 闪避();
    }

    // 纯观战模式的移动决策：以最近存活敌人为目标，进入技能距离后保持小幅环绕，
    // 同时读取敌方蓄力落点，优先离开预警区域。地图移动函数负责墙体和不可行走区域裁剪。
    void 推进自动战斗移动(float 秒)
    {
        if (战斗 == null || 战斗.玩家死亡 || 地图 == null || 秒 <= 0) return;
        自动目标切换冷却 = Mathf.Max(0, 自动目标切换冷却 - 秒);
        自动绕行锁定剩余 = Mathf.Max(0, 自动绕行锁定剩余 - 秒);

        // 保留当前目标，只有当前目标失效或新目标明显更近时才切换，避免敌群交叠时锁敌左右跳。
        战斗敌人 目标 = 自动移动目标;
        float 当前目标距离 = 目标 != null && 目标.存活 ? Vector2.Distance(玩家位置, 目标.位置) : float.MaxValue;
        战斗敌人 最近目标 = null; float 最近距离 = float.MaxValue;
        foreach (var 敌 in 战斗.敌人)
        {
            if (敌 == null || !敌.存活) continue;
            float 距 = Vector2.Distance(玩家位置, 敌.位置);
            if (距 < 最近距离) { 最近距离 = 距; 最近目标 = 敌; }
        }
        bool 当前目标仍有效 = 目标 != null && 目标.存活;
        bool 新目标明显更近 = 最近目标 != null && 最近目标 != 目标
            && (当前目标距离 == float.MaxValue || 最近距离 + 1.35f < 当前目标距离);
        if (!当前目标仍有效 || (新目标明显更近 && 自动目标切换冷却 <= 0))
        {
            if (最近目标 != null)
            {
                目标 = 自动移动目标 = 最近目标;
                自动目标切换冷却 = .28f;
                自动环绕中 = false;
                自动环绕符号 = 目标.序号 % 2 == 0 ? 1 : -1;
            }
            else
            {
                目标 = 自动移动目标 = null;
                自动环绕中 = false;
            }
        }
        最近距离 = 目标 != null ? Vector2.Distance(玩家位置, 目标.位置) : float.MaxValue;

        Vector2 方向 = Vector2.zero;
        bool 需要闪避 = false;
        Vector2 危险方向 = Vector2.zero;
        foreach (var 敌 in 战斗.敌人)
        {
            if (敌 == null || !敌.存活 || 敌.行动 != 敌人行动.蓄力 || 敌.蓄力 <= 0) continue;
            Vector2 落点 = 敌.本次范围技 ? 敌.位置 : 敌.攻击落点;
            float 半径 = Mathf.Max(.75f, 敌.本次攻击范围);
            float 距离危险 = Vector2.Distance(玩家位置, 落点);
            if (距离危险 <= 半径 + .65f)
            {
                需要闪避 = true;
                Vector2 远离 = 玩家位置 - 落点;
                if (远离.sqrMagnitude > .001f) 危险方向 += 远离.normalized * Mathf.Clamp01((半径 + .65f - 距离危险) / (半径 + .65f));
            }
        }
        // 持续地面区和延迟二段攻击不是“蓄力”状态，但同样会在短时间内造成伤害。
        // 读取战术层的真实效果，避免角色站在已经生成的危险区内继续环绕。
        var 敌术 = 战斗.战术 == null ? null : 战斗.战术.敌术;
        if (敌术 != null)
            foreach (var 区 in 敌术)
            {
                if (区 == null || 区.延迟 <= 0 && !区.地面) continue;
                Vector2 落点 = 区.终点;
                float 半径 = Mathf.Max(.55f, 区.半径);
                float 距离危险 = Vector2.Distance(玩家位置, 落点);
                if (距离危险 <= 半径 + .65f)
                {
                    需要闪避 = true;
                    Vector2 远离 = 玩家位置 - 落点;
                    if (远离.sqrMagnitude > .001f) 危险方向 += 远离.normalized * Mathf.Clamp01((半径 + .65f - 距离危险) / (半径 + .65f));
                }
            }

        // 战斗导演的地图事件只在生效后驱动闪避，预警阶段仍保留观察时间。
        var 场景事件 = 战斗.当前危险事件;
        if (场景事件 != null)
        {
            float 距离事件 = Vector2.Distance(玩家位置, 场景事件.中心);
            float 安全边距 = 场景事件.已生效 ? .72f : 1.05f;
            bool 在危险内 = 战斗.导演.危险包含(玩家位置);
            bool 预警逼近 = 场景事件.类型 == 战斗危险类型.范围收缩
                ? 距离事件 > 场景事件.有效半径 - 安全边距
                : 距离事件 < 场景事件.半径 + 安全边距;
            if (在危险内 || (!场景事件.已生效 && 预警逼近))
            {
                需要闪避 = true;
                危险方向 += 战斗.导演.危险逃离方向(玩家位置) * (场景事件.已生效 ? 1.35f : .7f);
            }
        }

        if (需要闪避)
        {
            方向 = 安全移动方向(危险方向, 目标);
            if (战斗.闪避冷却剩余 <= .0001f && 方向.sqrMagnitude > .001f)
            {
                闪避方向 = 方向.normalized;
                var 结果 = 战斗.尝试闪避();
                if (结果 == 战斗操作结果.成功)
                {
                    闪避剩余 = (float)天帝数值.取("player.dodge_duration");
                    平滑输入 = Vector2.zero;
                    游戏?.界面?.显示自动战斗反馈(结果, -1, true);
                    天帝声音.提示("润色_跑步");
                }
            }
        }
        else if (目标 != null)
        {
            float 进入技能距离 = Mathf.Max(3.5f, (float)天帝数值.取("player.range_max") * .72f);
            float 退出技能距离 = 进入技能距离 + 1.15f;
            // 进入和退出使用两个阈值，避免目标在技能距离边缘时反复切换接近/环绕。
            if (自动环绕中)
            {
                if (最近距离 > 退出技能距离) 自动环绕中 = false;
            }
            else if (最近距离 <= 进入技能距离) 自动环绕中 = true;

            if (!自动环绕中)
            {
                方向 = (目标.位置 - 玩家位置).normalized;
            }
            else
            {
                Vector2 径向 = (玩家位置 - 目标.位置).normalized;
                if (径向.sqrMagnitude < .001f) 径向 = Vector2.right;
                Vector2 切向 = new Vector2(-径向.y, 径向.x);
                // 环绕方向在目标生命周期内保持稳定；目标更换时才重新选边，避免每帧反转。
                方向 = 切向 * 自动环绕符号;
            }
        }
        else
        {
            // 没有已生成目标时回到战斗中心，避免角色在边缘等待下一批敌人。
            Vector2 中心 = -玩家位置;
            方向 = 中心.sqrMagnitude > 4f ? 中心.normalized : new Vector2(Mathf.Cos(Time.time), Mathf.Sin(Time.time)) * .22f;
        }
        bool 紧急移动 = 需要闪避;
        if (自动绕行锁定剩余 > 0 && !紧急移动 && 自动绕行方向.sqrMagnitude > .001f)
            方向 = 自动绕行方向;

        if (方向.sqrMagnitude > .001f)
        {
            Vector2 期望方向 = 方向.normalized;
            float 转向秒 = 紧急移动 ? .055f : .14f;
            自动平滑方向 = Vector2.SmoothDamp(自动平滑方向, 期望方向, ref 自动平滑方向速度, 转向秒, 5.5f, 秒);
            if (自动平滑方向.sqrMagnitude > .0001f) 方向 = 自动平滑方向.normalized;
            Vector2 开始 = 玩家位置;
            移动一步(方向.normalized, false, 秒);
            if ((玩家位置 - 开始).sqrMagnitude > .00001f) 自动卡住秒 = Mathf.Max(0, 自动卡住秒 - 秒 * 2f);
            else
            {
                自动卡住秒 += 秒;
                if (自动卡住秒 >= .24f && 自动绕行锁定剩余 <= 0)
                {
                    // 地图.移动会做局部滑移；若仍被拐角挡住，换两侧切线，避免长期贴墙站桩。
                    Vector2 基础 = 方向.normalized;
                    Vector2 切向 = new Vector2(-基础.y, 基础.x) * 自动绕行符号;
                    Vector2[] 候选 = { 切向, -切向, (基础 + 切向).normalized, (-基础 + 切向).normalized };
                    for (int i = 0; i < 候选.Length; i++)
                    {
                        if (候选[i].sqrMagnitude < .001f || !地图.可站立(玩家位置 + 候选[i] * .55f, .45f)) continue;
                        Vector2 绕行前 = 玩家位置;
                        移动一步(候选[i], false, 秒);
                        if ((玩家位置 - 绕行前).sqrMagnitude > .00001f)
                        {
                            // 锁住本次成功的绕行方向一小段时间，避免下一帧又被目标方向抢回去。
                            自动绕行方向 = 候选[i].normalized;
                            自动绕行锁定剩余 = .42f;
                            自动卡住秒 = 0; 自动绕行符号 = -自动绕行符号; break;
                        }
                    }
                    if (自动绕行锁定剩余 <= 0) 自动卡住秒 = Mathf.Min(自动卡住秒, .16f);
                }
            }
        }
        else
        {
            // 没有有效移动方向时逐步收回平滑状态，防止下一次出现目标时继承旧方向。
            自动平滑方向 = Vector2.SmoothDamp(自动平滑方向, Vector2.zero, ref 自动平滑方向速度, .12f, 5.5f, 秒);
        }
    }

    Vector2 安全移动方向(Vector2 危险, 战斗敌人 目标)
    {
        Vector2 基础 = 危险.sqrMagnitude > .001f ? 危险.normalized : (目标 != null ? (玩家位置 - 目标.位置).normalized : Vector2.right);
        if (基础.sqrMagnitude < .001f) 基础 = Vector2.right;
        Vector2[] 候选 = { 基础, new Vector2(-基础.y, 基础.x), new Vector2(基础.y, -基础.x), -基础 };
        for (int i = 0; i < 候选.Length; i++)
            if (地图.可站立(玩家位置 + 候选[i] * 1.2f, .45f)) return 候选[i];
        return 基础;
    }
    static bool 数字技能键按下(Keyboard 键, int 槽)
    {
        switch (槽)
        {
            case 0: return 键.digit1Key.wasPressedThisFrame || 键.numpad1Key.wasPressedThisFrame;
            case 1: return 键.digit2Key.wasPressedThisFrame || 键.numpad2Key.wasPressedThisFrame;
            case 2: return 键.digit3Key.wasPressedThisFrame || 键.numpad3Key.wasPressedThisFrame;
            case 3: return 键.digit4Key.wasPressedThisFrame || 键.numpad4Key.wasPressedThisFrame;
            case 4: return 键.digit5Key.wasPressedThisFrame || 键.numpad5Key.wasPressedThisFrame;
            case 5: return 键.digit6Key.wasPressedThisFrame || 键.numpad6Key.wasPressedThisFrame;
            default: return false;
        }
    }
    void 推进闪避位移(float 秒)
    {
        if (战斗.玩家死亡) { 闪避剩余 = 0; return; }
        if (闪避剩余 <= 0) return;
        float 步 = Mathf.Min(秒, 闪避剩余); 闪避剩余 = Mathf.Max(0, 闪避剩余 - 步);
        float 闪避时长 = Mathf.Max(.001f, (float)天帝数值.取("player.dodge_duration"));
        玩家位置 = 地图.直线闪避(玩家位置, 闪避方向,
            步 * (float)天帝数值.取("player.dodge_distance") / 闪避时长);
        主角.position = new Vector3(玩家位置.x, 0, 玩家位置.y);
        美术?.移动反馈(闪避方向, true);
        游戏.界面.更新战斗位置(玩家位置, 地图.所在格(玩家位置));
    }
}
