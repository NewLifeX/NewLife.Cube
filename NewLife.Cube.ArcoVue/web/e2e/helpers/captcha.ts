import { PNG } from 'pngjs';

/**
 * Cube 图片验证码识别（E2E 专用）。
 *
 * 依据 `NewLife.Cube/Services/DrawingCaptchaService.cs`：
 * - 图片 160×60，内嵌 8×8 位图字体按 Scale=2 放大（字符 16×16）；
 * - 字符水平位置固定（整行居中，startX=(160-L*16)/2），仅垂直方向 ±4 抖动；
 * - 背景亮灰（228+），字符深色（≤120），另有噪点与干扰线。
 *
 * 策略：已知字体模板 + 固定槽位 + 垂直滑窗匹配，连续"暗度"打分抗噪。
 */

/** 8×8 位图字体（与 DrawingCaptchaService._font 完全一致）：0-9、+、-、=、空格 */
const FONT: number[][] = [
  [0x3c, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3c, 0x00], // 0
  [0x18, 0x38, 0x18, 0x18, 0x18, 0x18, 0x7e, 0x00], // 1
  [0x3c, 0x66, 0x06, 0x0c, 0x18, 0x30, 0x7e, 0x00], // 2
  [0x3c, 0x66, 0x06, 0x1e, 0x06, 0x66, 0x3c, 0x00], // 3
  [0x30, 0x38, 0x3c, 0x36, 0x7e, 0x30, 0x30, 0x00], // 4
  [0x7e, 0x60, 0x7c, 0x06, 0x06, 0x66, 0x3c, 0x00], // 5
  [0x3c, 0x60, 0x7c, 0x66, 0x66, 0x66, 0x3c, 0x00], // 6
  [0x7e, 0x06, 0x0c, 0x18, 0x30, 0x30, 0x30, 0x00], // 7
  [0x3c, 0x66, 0x66, 0x3c, 0x66, 0x66, 0x3c, 0x00], // 8
  [0x3c, 0x66, 0x66, 0x3e, 0x06, 0x0c, 0x38, 0x00], // 9
  [0x00, 0x18, 0x18, 0x7e, 0x18, 0x18, 0x00, 0x00], // +
  [0x00, 0x00, 0x00, 0x7e, 0x00, 0x00, 0x00, 0x00], // -
  [0x00, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x00, 0x00], // =
  [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00], // 空格
];
const CHARS = '0123456789+-= ';

/** 16×16 模板掩码（Scale=2 放大；1=笔画） */
const MASKS: Uint8Array[] = FONT.map((rows) => {
  const mask = new Uint8Array(16 * 16);
  rows.forEach((rowByte, ry) => {
    for (let c = 0; c < 8; c++) {
      if ((rowByte & (0x80 >> c)) === 0) continue;
      for (let sy = 0; sy < 2; sy++) {
        for (let sx = 0; sx < 2; sx++) {
          mask[(ry * 2 + sy) * 16 + c * 2 + sx] = 1;
        }
      }
    }
  });
  return mask;
});

/**
 * 识别验证码并返回算术答案。
 * @param pngBuffer 验证码 PNG 二进制
 * @returns 算式的计算结果字符串；无法识别返回 null
 */
export function solveCaptcha(pngBuffer: Buffer): string | null {
  const img = PNG.sync.read(pngBuffer);
  const { width: W, height: H, data } = img;
  if (W !== 160 || H !== 60) return null;

  // 连续暗度：min 通道越暗得分越高（字符≤120 得高分；背景 228+ 得 0）
  const dark = new Float32Array(W * H);
  for (let i = 0; i < W * H; i++) {
    const m = Math.min(data[i * 4], data[i * 4 + 1], data[i * 4 + 2]);
    dark[i] = Math.min(1, Math.max(0, (200 - m) / 160));
  }

  const baseY = (H - 16) >> 1; // 22

  /** 按文本长度 L 逐槽位识别，返回文本与平均得分 */
  const recognize = (L: number) => {
    const startX = (W - L * 16) >> 1;
    let total = 0;
    let text = '';
    for (let i = 0; i < L; i++) {
      let bestCh = ' ';
      let bestScore = -1;
      for (let dy = -5; dy <= 5; dy++) {
        const cy = baseY + dy;
        for (let t = 0; t < MASKS.length; t++) {
          const mask = MASKS[t];
          let onCount = 0;
          let onHit = 0;
          let offHit = 0;
          for (let y = 0; y < 16; y++) {
            const py = cy + y;
            if (py < 0 || py >= H) continue;
            for (let x = 0; x < 16; x++) {
              const px = startX + i * 16 + x;
              if (px < 0 || px >= W) continue;
              const v = dark[py * W + px];
              if (mask[y * 16 + x]) {
                onCount++;
                onHit += v;
              } else {
                offHit += 1 - v;
              }
            }
          }
          const score =
            onCount > 0
              ? (onHit / onCount) * 0.8 + (offHit / (256 - onCount)) * 0.2
              : (offHit / 256) * 0.6;
          if (score > bestScore) {
            bestScore = score;
            bestCh = CHARS[t];
          }
        }
      }
      total += bestScore;
      text += bestCh;
    }
    return { text, avg: total / L };
  };

  // 算式 "a + b =" 长度 7（个位）或 8（十位）；试两种长度，取平均分高者
  const c7 = recognize(7);
  const c8 = recognize(8);
  const text = c7.avg >= c8.avg ? c7.text : c8.text;

  const m = /^(\d{1,2}) ?([+-]) ?(\d) ?=$/.exec(text);
  if (!m) return null;
  const a = parseInt(m[1], 10);
  const b = parseInt(m[3], 10);
  return String(m[2] === '+' ? a + b : a - b);
}

/**
 * 从 data URL 识别验证码。
 * @param dataUrl `data:image/png;base64,...` 或纯 base64 字符串
 * @returns 算式的计算结果字符串；无法识别返回 null
 */
export function solveCaptchaDataUrl(dataUrl: string): string | null {
  const idx = dataUrl.indexOf(',');
  const b64 = idx >= 0 ? dataUrl.slice(idx + 1) : dataUrl;
  try {
    return solveCaptcha(Buffer.from(b64, 'base64'));
  } catch {
    return null;
  }
}
