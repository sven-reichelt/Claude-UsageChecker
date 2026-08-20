// Draws the icons of the application from code - reproducible and without
// dependencies. Run: node build/generate-icons.mjs
import { deflateSync } from 'node:zlib';
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const OUT_DIR = join(dirname(fileURLToPath(import.meta.url)), '..', 'assets', 'icons');

const WHITE = [0xff, 0xff, 0xff];

/**
 * The four states of the tray icon.
 *
 * The colour carries the meaning, the badge confirms it: colour alone is lost
 * on anyone who cannot tell amber from red, and a taskbar sixteen pixels high
 * is no place to rely on hue. Signed out stays plain grey - there is nothing to
 * report yet, and a badge would claim otherwise.
 */
const STATES = {
  'tray-normal':   { ring: [0xd9, 0x77, 0x57], badge: { glyph: 'check', fill: [0x1f, 0x9d, 0x55] } },
  'tray-warning':  { ring: [0xe0, 0xa0, 0x30], badge: { glyph: 'question', fill: [0xa8, 0x63, 0x00] } },
  'tray-critical': { ring: [0xe5, 0x39, 0x35], badge: { glyph: 'bang',     fill: [0xa4, 0x14, 0x14] } },
  'tray-inactive': { ring: [0x88, 0x88, 0x88], badge: null },
};

/** Distance from a point to a line segment. */
function distanceToSegment(px, py, x1, y1, x2, y2) {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const lengthSquared = dx * dx + dy * dy;
  const t = lengthSquared === 0 ? 0 : Math.max(0, Math.min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSquared));

  return Math.hypot(px - (x1 + t * dx), py - (y1 + t * dy));
}

/**
 * Whether a point lies inside the glyph of the badge.
 *
 * Coordinates are relative to the centre of the badge and to its radius, so the
 * shapes hold at every size. Only one glyph per state: at sixteen pixels the
 * badge is barely seven across, and two characters beside each other are a
 * smear rather than a reading.
 */
function insideGlyph(dx, dy, radius, badge) {
  const stroke = radius * 0.21;

  if (badge.glyph === 'check') {
    const onShort = distanceToSegment(dx, dy, -radius * 0.42, radius * 0.02, -radius * 0.10, radius * 0.36);
    const onLong = distanceToSegment(dx, dy, -radius * 0.10, radius * 0.36, radius * 0.46, -radius * 0.34);

    return Math.min(onShort, onLong) <= stroke;
  }

  if (badge.glyph === 'question') {
    // The hook: a ring open towards the lower left, so the eye reads a question
    // mark rather than a circle.
    const hookY = dy + radius * 0.20;
    const onHook = Math.abs(Math.hypot(dx, hookY) - radius * 0.30) <= stroke
      && !(hookY > 0 && dx < radius * 0.06);
    // The neck runs from the lower end of the hook into the middle.
    const onNeck = distanceToSegment(dx, dy, radius * 0.30, radius * 0.10, 0, radius * 0.26) <= stroke;
    const onDot = Math.hypot(dx, dy - radius * 0.52) <= stroke * 1.15;

    return onHook || onNeck || onDot;
  }

  // The exclamation mark: a bar with a dot beneath it.
  const onBar = Math.abs(dx) <= stroke && dy >= -radius * 0.50 && dy <= radius * 0.22;
  const onBang = Math.hypot(dx, dy - radius * 0.50) <= stroke * 1.15;

  return onBar || onBang;
}

/**
 * Draws the ring with its core dot and, where the state calls for one, the
 * badge - antialiased through 4x supersampling.
 */
function renderIcon(size, { ring, badge }) {
  const px = Buffer.alloc(size * size * 4);
  const c = (size - 1) / 2;
  const outer = size * 0.46;
  const inner = size * 0.30;
  const dot = size * 0.16;
  const S = 4;

  // The badge sits in the lower right corner, where a notification badge is
  // expected. Its white outline separates it both from the ring and from a
  // taskbar of any colour.
  const badgeCx = size * 0.72;
  const badgeCy = size * 0.72;
  const badgeR = size * 0.25;
  const outlineR = badgeR * 1.26;

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      let hitRing = 0, hitCore = 0, hitBadge = 0, hitOutline = 0, hitGlyph = 0;

      for (let sy = 0; sy < S; sy++) {
        for (let sx = 0; sx < S; sx++) {
          const px2 = x + (sx + 0.5) / S - 0.5;
          const py2 = y + (sy + 0.5) / S - 0.5;
          const d = Math.hypot(px2 - c, py2 - c);

          if (d <= outer && d >= inner) hitRing++;
          if (d <= dot) hitCore++;

          if (badge) {
            const bdx = px2 - badgeCx;
            const bdy = py2 - badgeCy;
            const bd = Math.hypot(bdx, bdy);

            if (bd <= outlineR) hitOutline++;
            if (bd <= badgeR) {
              hitBadge++;
              if (insideGlyph(bdx, bdy, badgeR, badge)) hitGlyph++;
            }
          }
        }
      }

      const total = S * S;
      let colour = ring;
      let alpha = Math.min(1, hitRing / total + hitCore / total);

      // Painted from the back forwards: logo, outline, badge, glyph.
      if (hitOutline > 0) {
        const a = hitOutline / total;
        colour = WHITE;
        alpha = Math.max(alpha, a);
      }
      if (hitBadge > 0) {
        const a = hitBadge / total;
        if (a >= 0.5) colour = badge.fill;
        alpha = Math.max(alpha, a);
      }
      if (hitGlyph > 0 && hitGlyph / total >= 0.4) {
        colour = WHITE;
        alpha = 1;
      }

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

/** Encodes raw RGBA as PNG (colour type 6, filter 0). */
function encodePng(size, rgba) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8;  // Bit depth
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

/** Packs PNGs as an ICO (Windows accepts embedded PNGs from Vista onwards). */
function encodeIco(entries) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2); // Type: icon
  header.writeUInt16LE(entries.length, 4);

  let offset = 6 + entries.length * 16;
  const dir = [];
  for (const { size, png } of entries) {
    const e = Buffer.alloc(16);
    e[0] = size >= 256 ? 0 : size;
    e[1] = size >= 256 ? 0 : size;
    e.writeUInt16LE(1, 4);   // Colour planes
    e.writeUInt16LE(32, 6);  // Bits per pixel
    e.writeUInt32BE(0, 8);
    e.writeUInt32LE(png.length, 8);
    e.writeUInt32LE(offset, 12);
    dir.push(e);
    offset += png.length;
  }
  return Buffer.concat([header, ...dir, ...entries.map(e => e.png)]);
}

mkdirSync(OUT_DIR, { recursive: true });

// Tray icons: 32 px is enough for Avalonia at every scaling.
for (const [name, state] of Object.entries(STATES)) {
  const png = encodePng(32, renderIcon(32, state));
  writeFileSync(join(OUT_DIR, `${name}.png`), png);
  console.log(`${name}.png (${png.length} bytes)`);
}

// The application icon for the executable and the details window. Without a
// badge: the badges report a state, and an icon in the taskbar or in Explorer
// reports nothing - a permanent green tick there would be a claim about a
// program that is not even running.
const appIcon = { ring: STATES['tray-normal'].ring, badge: null };
const icoSizes = [16, 32, 48, 256];
const ico = encodeIco(icoSizes.map(size => ({ size, png: encodePng(size, renderIcon(size, appIcon)) })));
writeFileSync(join(OUT_DIR, 'app.ico'), ico);
console.log(`app.ico (${ico.length} bytes)`);

const appPng = encodePng(256, renderIcon(256, appIcon));
writeFileSync(join(OUT_DIR, 'app.png'), appPng);
console.log(`app.png (${appPng.length} bytes)`);
