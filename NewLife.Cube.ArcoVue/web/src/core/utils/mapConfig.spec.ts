/** parseMapConfig 单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import { DEFAULT_MAP_CONFIG, parseMapConfig } from './mapConfig';

describe('parseMapConfig', () => {
  it('Axios 包裹（data）与 camelCase 键', () => {
    const r = parseMapConfig({ data: { provider: 'amap', key: 'k1', scriptUrl: 'https://x' } });
    expect(r).toEqual({ provider: 'amap', key: 'k1', scriptUrl: 'https://x' });
  });

  it('PascalCase 键（MapProvider/MapKey/MapScriptUrl）且大小写不敏感', () => {
    const r = parseMapConfig({ MapProvider: 'BAIDU', MapKey: ' k2 ' });
    expect(r).toEqual({ provider: 'baidu', key: 'k2', scriptUrl: '' });
  });

  it('tencent 有效；未知服务商视为未配置', () => {
    expect(parseMapConfig({ provider: 'tencent', key: 'k' }).provider).toBe('tencent');
    expect(parseMapConfig({ provider: 'gaode', key: 'k' })).toEqual(DEFAULT_MAP_CONFIG);
  });

  it('缺 key 或空对象 → 未配置（保留 scriptUrl）', () => {
    expect(parseMapConfig({ provider: 'amap' })).toEqual(DEFAULT_MAP_CONFIG);
    expect(parseMapConfig({})).toEqual(DEFAULT_MAP_CONFIG);
    expect(parseMapConfig(null)).toEqual(DEFAULT_MAP_CONFIG);
    expect(parseMapConfig({ provider: 'amap', key: '', scriptUrl: 'https://y' })).toEqual({
      provider: null,
      key: '',
      scriptUrl: 'https://y',
    });
  });
});
