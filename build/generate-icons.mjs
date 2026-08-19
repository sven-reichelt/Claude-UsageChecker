// Erzeugt die Symbole der Anwendung aus Code - reproduzierbar und ohne Abhaengigkeiten.
// Aufruf: node build/generate-icons.mjs
import { deflateSync } from 'node:zlib';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const OUT_DIR = join(dirname(fileURLToPath(import.meta.url)), '..', 'assets', 'icons');

/** Zustandsfarben des Infobereich-Symbols. */
const STATES = {
  'tray-normal':   { ring: [0xd9, 0x77, 0x57], core: [0xd9, 0x77, 0x57] }, // Claude-Orange
  'tray-warning':  { ring: [0xe0, 0xa0, 0x30], core: [0xe0, 0xa0, 0x30] },
  'tray-critical': { ring: [0xd0, 0x40, 0x40], core: [0xd0, 0x40, 0x40] },
  'tray-inactive': { ring: [0x88, 0x88, 0x88], core: [0x88, 0x88, 0x88] },
};

/** Zeichnet einen Ring mit Kernpunkt, kantengeglaettet ueber 4x-Supersampling. */
function renderIcon(size, { ring, core }) {
  const px = Buffer.alloc(size * size * 4);
  const c = (size - 1) / 2;
  const outer = size * 0.46;
  const inner = size * 0.30;
  const dot = size * 0.16;
  const S = 4; // Supersampling-Faktor

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      let hitRing = 0;
      let hitCore = 0;
      for (let sy = 0; sy < S; sy++) {
        for (let sx = 0; sx < S; sx++) {
          const dx = x + (sx + 0.5) / S - 0.5 - c;
          const dy = y + (sy + 0.5) / S - 0.5 - c;
          const d = Math.hypot(dx, dy);
          if (d <= outer && d >= inner) hitRing++;
          if (d <= dot) hitCore++;
        }
      }
      const total = S * S;
      const aRing = hitRing / total;
      const aCore = hitCore / total;
      const alpha = Math.min(1, aRing + aCore);
      const colour = aCore > aRing ? core : ring;
      const i = (y * size + x) * 4;
      px[i] = colour[0];
      px[i + 1] = colour[1];
      px[i + 2] = colour[2];
      px[i + 3] = Math.round(alpha * 255);
    }
  }
  return px;
}

function crc32(buf) {
  let c, table = [];
  for (let n = 0; n < 256; n++) {
    c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c >>> 0;
  }
  let crc = 0xffffffff;
  for (const b of buf) crc = table[(crc ^ b) & 0xff] ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

/** Kodiert RGBA-Rohdaten als PNG (Farbtyp 6, Filter 0). */
function encodePng(size, rgba) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;  // Bittiefe
  ihdr[9] = 6;  // RGBA
  const raw = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) {
    raw[y * (size * 4 + 1)] = 0; // Filter: None
    rgba.copy(raw, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  }
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

/** Packt PNGs als ICO (Windows akzeptiert eingebettete PNGs ab Vista). */
function encodeIco(entries) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2); // Typ: Symbol
  header.writeUInt16LE(entries.length, 4);

  let offset = 6 + entries.length * 16;
  const dir = [];
  for (const { size, png } of entries) {
    const e = Buffer.alloc(16);
    e[0] = size >= 256 ? 0 : size;
    e[1] = size >= 256 ? 0 : size;
    e.writeUInt16LE(1, 4);   // Farbebenen
    e.writeUInt16LE(32, 6);  // Bit pro Pixel
    e.writeUInt32BE(0, 8);
    e.writeUInt32LE(png.length, 8);
    e.writeUInt32LE(offset, 12);
    dir.push(e);
    offset += png.length;
  }
  return Buffer.concat([header, ...dir, ...entries.map(e => e.png)]);
}

mkdirSync(OUT_DIR, { recursive: true });

// Infobereich-Symbole: 32 px reicht Avalonia fuer alle Skalierungen.
for (const [name, colours] of Object.entries(STATES)) {
  const png = encodePng(32, renderIcon(32, colours));
  writeFileSync(join(OUT_DIR, `${name}.png`), png);
  console.log(`${name}.png (${png.length} Bytes)`);
}

// Anwendungssymbol fuer die Exe und das Detailfenster.
const appColours = STATES['tray-normal'];
const icoSizes = [16, 32, 48, 256];
const ico = encodeIco(icoSizes.map(size => ({ size, png: encodePng(size, renderIcon(size, appColours)) })));
writeFileSync(join(OUT_DIR, 'app.ico'), ico);
console.log(`app.ico (${ico.length} Bytes)`);

const appPng = encodePng(256, renderIcon(256, appColours));
writeFileSync(join(OUT_DIR, 'app.png'), appPng);
console.log(`app.png (${appPng.length} Bytes)`);
