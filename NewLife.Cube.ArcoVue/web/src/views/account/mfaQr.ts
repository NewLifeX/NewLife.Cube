/**
 * MFA totpUri → 二维码 DataURL（浏览器本地生成，不依赖外网）。
 */
import QRCode from 'qrcode';

export async function buildTotpQrDataUrl(uri: string, size = 180): Promise<string> {
  if (!uri) return '';
  return QRCode.toDataURL(uri, {
    width: size,
    margin: 1,
    errorCorrectionLevel: 'M',
  });
}
