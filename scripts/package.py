#!/usr/bin/env python3
"""Package only our two assemblies; never bundle game or other mods' DLLs."""
from pathlib import Path
import hashlib, json, struct, zipfile, zlib
root = Path(__file__).resolve().parents[1]
out = root / 'artifacts'
out.mkdir(exist_ok=True)
# Original pixel icon, 256x256 RGBA PNG. No third-party artwork.
def icon():
    rows = []
    for y in range(256):
        row = bytearray()
        for x in range(256):
            color = (20, 28, 34, 255)
            if 56 <= x < 200 and 40 <= y < 210: color = (99, 161, 95, 255)
            if 73 <= y < 103 and (78 <= x < 108 or 148 <= x < 178): color = (20, 28, 34, 255)
            if 144 <= y < 163 and 89 <= x < 170: color = (20, 28, 34, 255)
            if y >= 212 and abs(x-128) < (255-y)*2: color = (224, 190, 99, 255)
            row.extend(color)
        rows.append(b'\0' + row)
    def chunk(kind, data):
        return struct.pack('!I', len(data))+kind+data+struct.pack('!I', zlib.crc32(kind+data)&0xffffffff)
    return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('!2I5B',256,256,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(rows)))+chunk(b'IEND',b'')
manifest = json.loads((root/'thunderstore/manifest.json').read_text())
assemblies = {
    'BepInEx/plugins/PEAKZombieDirector/PEAKZombieDirector.dll': root/'src/PeakZombieDirector.Plugin/bin/Release/netstandard2.1/PEAKZombieDirector.dll',
    'BepInEx/plugins/PEAKZombieDirector/PeakZombieDirector.Core.dll': root/'src/PeakZombieDirector.Core/bin/Release/netstandard2.1/PeakZombieDirector.Core.dll'
}
for path in assemblies.values():
    if not path.is_file(): raise SystemExit(f'Build first; missing {path}')
archive = out / f"PEAK_Zombie_Director-{manifest['version_number']}.zip"
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
    z.writestr('icon.png', icon())
    for filename in ['README.md','LICENSE']: z.write(root/filename,filename)
    z.write(root/'thunderstore/manifest.json','manifest.json')
    for name,path in assemblies.items(): z.write(path,name)
(out/'SHA256SUMS').write_text(hashlib.sha256(archive.read_bytes()).hexdigest()+'  '+archive.name+'\n')
print(archive)
