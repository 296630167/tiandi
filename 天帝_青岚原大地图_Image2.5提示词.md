# 青岚原固定战场大地图

用途：Unity 纯2D俯视战斗的固定场景背景图。图片只负责视觉，碰撞、道路、刷新点和BOSS判定仍由Unity的20×20逻辑网格控制。

## 正向提示词

Use case: stylized-concept. Asset type: a large 2D fixed arena background for a top-down action roguelite game. Generate a polished premium commercial game environment illustration for Qinglan Plain, a cultivation-world wilderness arena. High top-down view, broad jade-green mountain valley, winding pale stone paths, a small clear stream with one simple wooden bridge, scattered ancient trees and rounded rocks, distant blue-green mountains, a darker circular ritual stone clearing near the far edge for a boss arena. Keep the exact center of the image open, readable and low detail for player and enemy sprites. Design the whole image as one continuous fixed battlefield background, no separate tiles, no map UI.

Modern Chinese fantasy game art, refined hand-painted 2D background, realistic material separation with a lightly stylized illustrated finish, soft warm sunlight and cool shadows, atmospheric depth, restrained bloom, muted jade green, blue-green, warm stone and small desaturated gold accents. Wide landscape composition, 3:2 ratio, no text, no characters, no monsters, no weapons, no interface, no grid lines, no logos, no watermark. Avoid photorealism, anime characters, isometric buildings, excessive fog, noisy center, huge visual effects, extreme saturation, overexposed highlights and repeated patterns.

## 规格

- 推荐输出：1536×1024 或 2048×1365 PNG。
- 中央约40%区域保持低细节，方便叠加主角、敌人、弹体和血条。
- 四周提供树、岩石、溪流和远山层次，保证镜头移动时画面不空。
- 不要生成任何文字和地图标识，所有HUD由Unity绘制。
- 输出后放入 `Assets/天帝/美术/生成素材/青岚原_大地图.png`，再绑定到 `青岚原` 场景入口的“地图大图”字段。
