from pathlib import Path
from PIL import Image, ImageEnhance


root = Path(__file__).resolve().parent
project = root.parent.parent
reference = project / "output" / "imagegen" / "prologue" / "P01_reference.png"
output = project / "Assets" / "天帝" / "美术" / "序章3D" / "P01_书桌" / "P01_浅灰木纹.png"

with Image.open(reference) as image:
    # The four points follow the inside edge of the tabletop in the approved reference.
    tabletop = image.convert("RGB").transform(
        (1024, 512), Image.Transform.QUAD,
        (455, 245, 100, 354, 818, 522, 1135, 416),
        resample=Image.Resampling.BICUBIC,
    )
    tabletop = tabletop.crop((0, 0, 970, 512)).resize((1024, 512), Image.Resampling.BICUBIC)
    tabletop = ImageEnhance.Contrast(tabletop).enhance(1.12)
    tabletop = ImageEnhance.Brightness(tabletop).enhance(0.82)
    tabletop.save(output)
