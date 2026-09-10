// Read PE resources as bytes only. Never loads or executes the source DLL.
import fs from 'node:fs';
import path from 'node:path';
const file = fs.readFileSync('references/cards.dll');
const u16 = n => file.readUInt16LE(n), u32 = n => file.readUInt32LE(n);
if (file.toString('ascii', 0, 2) !== 'MZ') throw new Error('Not a PE resource file');
const pe = u32(60), optional = pe + 24, sections = optional + u16(pe + 20);
if (file.toString('ascii', pe, pe + 4) !== 'PE\0\0') throw new Error('Invalid PE');
function offset(rva) {
  for (let i = 0; i < u16(pe + 6); i++) {
    const s = sections + i * 40, start = u32(s + 12);
    if (rva >= start && rva < start + Math.max(u32(s + 8), u32(s + 16))) return u32(s + 20) + rva - start;
  }
  throw new Error('Invalid resource RVA');
}
const resources = offset(u32(optional + (u16(optional) === 0x20b ? 112 : 96) + 16));
const out = 'src/Assets/Classic';
fs.mkdirSync(out, { recursive: true });
const manifest = [];
function walk(relative, ids = []) {
  const base = resources + relative;
  for (let i = 0; i < u16(base + 12) + u16(base + 14); i++) {
    const e = base + 16 + i * 8, id = u32(e), child = u32(e + 4), keys = [...ids, id];
    if (child & 0x80000000) walk(child & 0x7fffffff, keys);
    else if (keys[0] === 2) {
      const entry = resources + child, data = file.subarray(offset(u32(entry)), offset(u32(entry)) + u32(entry + 4));
      const headerSize = data.readUInt32LE(0), core = headerSize === 12;
      const bpp = data.readUInt16LE(core ? 10 : 14), compression = core ? 0 : data.readUInt32LE(16);
      if (![12,40,108,124].includes(headerSize)) throw new Error('Unsupported DIB header');
      const used = core ? 0 : data.readUInt32LE(32);
      const palette = bpp <= 8 ? (used > 0 && used <= 256 ? used : 2 ** bpp) * (core ? 3 : 4) : 0;
      const mask = headerSize === 40 && compression === 3 ? 12 : 0;
      const header = Buffer.alloc(14); header.write('BM'); header.writeUInt32LE(14 + data.length, 2); header.writeUInt32LE(14 + headerSize + palette + mask, 10);
      const name = `${keys[1]}.bmp`;
      fs.writeFileSync(path.join(out, name), Buffer.concat([header, data]));
      manifest.push({ id: keys[1], width: core ? data.readUInt16LE(4) : data.readInt32LE(4), height: core ? data.readUInt16LE(6) : data.readInt32LE(8), bpp, bytes: data.length });
    }
  }
}
walk(0);
fs.writeFileSync('references/card-resources.json', JSON.stringify(manifest, null, 2));
console.log(JSON.stringify(manifest));
