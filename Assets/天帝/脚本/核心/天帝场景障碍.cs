using UnityEngine;

// 静态障碍：脚底阻挡在导航构建前写入地图，供角色、敌人和灵力弹共用。
public sealed class 天帝场景障碍 : MonoBehaviour
{
    public 长卷障碍种类 种类;
    public Vector2 脚底;
    public Vector2 阻挡半径;
    public void 初始化(战斗场景障碍布点 数据, 天帝战斗地图 地图)
    { 种类 = 数据.种类; 脚底 = 数据.世界脚底(地图); 阻挡半径 = 数据.阻挡半径; }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, .55f, .15f, .8f);
        for (int i = 0; i < 32; i++)
        {
            float 起 = i * Mathf.PI / 16, 终 = (i + 1) * Mathf.PI / 16;
            Gizmos.DrawLine(new Vector3(脚底.x + Mathf.Cos(起) * 阻挡半径.x, .05f, 脚底.y + Mathf.Sin(起) * 阻挡半径.y),
                new Vector3(脚底.x + Mathf.Cos(终) * 阻挡半径.x, .05f, 脚底.y + Mathf.Sin(终) * 阻挡半径.y));
        }
    }
}
