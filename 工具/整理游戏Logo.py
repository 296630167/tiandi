from pathlib import Path
import argparse
import json
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '生成/宣传素材/游戏LOGO重制-20261004'


def main(source):
    OUT.mkdir(parents=True, exist_ok=True)
    image = Image.open(source).convert('RGBA')
    alpha = image.getchannel('A')
    if alpha.getextrema() != (0, 255):
        raise ValueError('Expected a logo with transparent and opaque pixels')
    image.save(OUT / '生成原图_1280x720.png', optimize=True)
    bounds = alpha.getbbox()
    cropped = image.crop(bounds)
    margin = 16
    width = 1280 - margin * 2
    height = round(cropped.height * width / cropped.width)
    cropped = cropped.resize((width, height), Image.Resampling.LANCZOS)
    final = Image.new('RGBA', (1280, height + margin * 2))
    final.alpha_composite(cropped, (margin, margin))
    output = OUT / '01_游戏LOGO_透明PNG_上传用.png'
    final.save(output, optimize=True)
    if output.stat().st_size >= 4_000_000:
        raise ValueError('PNG exceeds the 4 MB limit')
    assert final.width >= 1280 or final.height >= 720
    assert all(final.getpixel(point)[3] == 0 for point in (
        (0, 0), (final.width - 1, 0),
        (0, final.height - 1), (final.width - 1, final.height - 1),
    ))
    report = {
        'file': str(output), 'format': 'PNG', 'mode': final.mode,
        'size': list(final.size), 'bytes': output.stat().st_size,
        'alpha_range': list(final.getchannel('A').getextrema()),
        'source_content_bounds': list(bounds), 'safe_margin_px': margin,
        'minimum_dimension_passed': True, 'under_4_MB': True,
    }
    (OUT / '规格校验.json').write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8'
    )
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', required=True)
    args = parser.parse_args()
    main(Path(args.source))
