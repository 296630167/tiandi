from pathlib import Path
import re,json
s=Path('Assets/天帝/脚本/核心/天帝序章.cs').read_text(encoding='utf-8-sig')
lines=re.findall(r'"([^"]+)"',s.split('string[] 叙述 = {')[1].split('};')[0])
print(json.dumps(lines,ensure_ascii=False))
