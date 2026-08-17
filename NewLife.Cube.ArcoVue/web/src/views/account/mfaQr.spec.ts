import { beforeEach, describe, expect, it, vi } from 'vitest';
import QRCode from 'qrcode';
import { buildTotpQrDataUrl } from './mfaQr';

vi.mock('qrcode', () => ({
  default: { toDataURL: vi.fn() },
}));

/** toDataURL 存在 callback 重载（返回 void），断言到无 callback 签名以正确 mock */
const toDataURL = QRCode.toDataURL as (text: string, options?: Record<string, unknown>) => Promise<string>;
const toDataURLMock = vi.mocked(toDataURL);

describe('buildTotpQrDataUrl', () => {
  beforeEach(() => {
    toDataURLMock.mockReset();
  });

  it('生成二维码 DataURL', async () => {
    toDataURLMock.mockResolvedValue('data:image/png;base64,xxx');
    const url = await buildTotpQrDataUrl('otpauth://totp/Cube:admin?secret=ABCD');
    expect(url).toBe('data:image/png;base64,xxx');
    expect(toDataURLMock).toHaveBeenCalledWith(
      'otpauth://totp/Cube:admin?secret=ABCD',
      expect.objectContaining({ width: 180 }),
    );
  });

  it('空 URI 返回空字符串，不调用二维码库', async () => {
    const url = await buildTotpQrDataUrl('');
    expect(url).toBe('');
    expect(toDataURLMock).not.toHaveBeenCalled();
  });
});
