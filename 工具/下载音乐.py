from pathlib import Path
import json,requests
root=Path(__file__).resolve().parents[1]
data=json.loads((root/'output/音频接入/完整生成记录.json').read_text(encoding='utf-8'))['music']
target=Path('C:/Users/123/Documents/taptap製造_一滴水的故事/assets/audio/music')
target.mkdir(parents=True,exist_ok=True)
for item in data:
    music=item['music']; p=target/(music['title']+'.mp3')
    if p.exists() and p.stat().st_size>10000: continue
    try:
        r=requests.get(music['audioUrl'],timeout=60); r.raise_for_status()
        assert len(r.content)>10000 and not r.content[:1]==b'<', 'invalid audio'
        p.write_bytes(r.content)
        print(music['title'],len(r.content))
    except Exception as e: print(music['title'],str(e))
