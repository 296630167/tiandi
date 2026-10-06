from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


root = Path(__file__).resolve().parent
images = sorted((root / "序章简单物件").glob("*_预览.png"))
thumb = 390
canvas = Image.new("RGB", (thumb * 4, (thumb + 26) * 4), "#e9e9e7")
draw = ImageDraw.Draw(canvas)
for index, path in enumerate(images):
    x = index % 4 * thumb
    y = index // 4 * (thumb + 26)
    with Image.open(path) as source:
        tile = source.convert("RGB").resize((thumb, thumb), Image.Resampling.LANCZOS)
    canvas.paste(tile, (x, y + 26))
    draw.text((x + 8, y + 5), path.stem.split("_")[0], fill="#222729")
canvas.save(root / "序章简单物件_总览.jpg", quality=90)
