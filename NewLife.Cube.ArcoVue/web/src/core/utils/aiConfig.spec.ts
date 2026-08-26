import { describe, expect, it } from 'vitest';
import { DEFAULT_AI_CONFIG, parseAiConfig } from './aiConfig';

describe('parseAiConfig', () => {
  it('FastJson CamelCase：aISwitch 嵌在 Axios+ApiResponse 里', () => {
    const raw = {
      status: 200,
      data: {
        code: 0,
        data: { aISwitch: true, aIPrimaryColor: '#111', aISecondaryColor: '#222' },
      },
    };
    expect(parseAiConfig(raw)).toEqual({
      enabled: true,
      primary: '#111',
      secondary: '#222',
    });
  });

  it('System.Text.Json CamelCase：aiSwitch', () => {
    expect(parseAiConfig({ data: { aiSwitch: true, aiPrimaryColor: '#abc' } })).toEqual({
      enabled: true,
      primary: '#abc',
      secondary: DEFAULT_AI_CONFIG.secondary,
    });
  });

  it('PascalCase AISwitch', () => {
    expect(parseAiConfig({ AISwitch: true, AIPrimaryColor: '#2ecc71', AISecondaryColor: '#1e8e3e' })).toEqual({
      enabled: true,
      primary: '#2ecc71',
      secondary: '#1e8e3e',
    });
  });

  it('开关关闭 / 缺失', () => {
    expect(parseAiConfig({ aISwitch: false }).enabled).toBe(false);
    expect(parseAiConfig({}).enabled).toBe(false);
    expect(parseAiConfig(null).enabled).toBe(false);
  });
});
