/** 玻璃样式生成器单测（OSC-261004d7f4） */
import { describe, expect, it } from 'vitest';
import { glassPanelStyle } from './glassStyle';

describe('glassPanelStyle', () => {
  it('默认：白底 62% + 14px 模糊', () => {
    const s = glassPanelStyle(null, { supports: true });
    expect(s.backgroundColor).toBe('rgba(255, 255, 255, 0.62)');
    expect(s.backdropFilter).toBe('blur(14px) saturate(160%)');
    expect(s.WebkitBackdropFilter).toBe(s.backdropFilter);
  });

  it('暗色默认底色', () => {
    const s = glassPanelStyle(null, { dark: true, supports: true });
    expect(s.backgroundColor).toBe('rgba(32, 34, 40, 0.62)');
  });

  it('自定义 chrome：颜色/不透明度/模糊', () => {
    const s = glassPanelStyle(
      { bgPreset: 'custom', bgColor: '#000000', bgOpacity: 30, bgBlur: 50 },
      { supports: true },
    );
    expect(s.backgroundColor).toBe('rgba(0, 0, 0, 0.3)');
    expect(s.backdropFilter).toBe('blur(10px) saturate(160%)');
  });

  it('transparent 与不支持 backdrop-filter 时降级', () => {
    const t = glassPanelStyle({ bgPreset: 'custom', bgColor: 'transparent' }, { supports: true });
    expect(t.backgroundColor).toBe('transparent');
    const s = glassPanelStyle(null, { supports: false });
    expect(s.backdropFilter).toBeUndefined();
    expect(s.backgroundColor).toBe('rgba(255, 255, 255, 0.62)');
  });
});
