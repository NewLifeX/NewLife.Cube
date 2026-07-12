import { describe, it, expect, vi } from 'vitest';
import { resolveAssetUrl } from '@newlifex/cube-vue/core/utils/url';

// 用可变变量驱动 mock，便于覆盖不同 baseUrl 形态
const state = vi.hoisted(() => ({ baseUrl: 'http://localhost:5000' }));
vi.mock('@newlifex/cube-vue/core/configure', () => ({
  getConfig: () => ({ request: { baseUrl: state.baseUrl } }),
}));

describe('resolveAssetUrl（资源地址拼接 baseUrl）', () => {
  it('空值返回空串', () => {
    expect(resolveAssetUrl('')).toBe('');
    expect(resolveAssetUrl(null)).toBe('');
    expect(resolveAssetUrl(undefined)).toBe('');
  });

  it('绝对地址（http/https/协议相对/data/blob）原样返回', () => {
    expect(resolveAssetUrl('https://x.com/a.png')).toBe('https://x.com/a.png');
    expect(resolveAssetUrl('http://x.com/a.png')).toBe('http://x.com/a.png');
    expect(resolveAssetUrl('//x.com/a.png')).toBe('//x.com/a.png');
    expect(resolveAssetUrl('data:image/png;base64,abc')).toBe('data:image/png;base64,abc');
    expect(resolveAssetUrl('blob:http://x/uuid')).toBe('blob:http://x/uuid');
  });

  it('以「/」开头的相对路径拼接 baseUrl', () => {
    expect(resolveAssetUrl('/cube/image?id=1.png')).toBe(
      'http://localhost:5000/cube/image?id=1.png',
    );
  });

  it('不带「/」的相对地址拼接 baseUrl + 斜杠', () => {
    expect(resolveAssetUrl('cube/image.png')).toBe('http://localhost:5000/cube/image.png');
  });

  it('baseUrl 带尾斜杠时归一，不出现双斜杠', () => {
    state.baseUrl = 'http://localhost:5000/';
    expect(resolveAssetUrl('/cube/image.png')).toBe('http://localhost:5000/cube/image.png');
    state.baseUrl = 'http://localhost:5000';
  });

  it('baseUrl 为空时返回原始路径（前后端同域回退）', () => {
    state.baseUrl = '';
    expect(resolveAssetUrl('/cube/image.png')).toBe('/cube/image.png');
    state.baseUrl = 'http://localhost:5000';
  });
});
