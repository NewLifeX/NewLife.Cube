import { describe, expect, it } from 'vitest';
import { parseColor, withAlpha } from './themeColor';

describe('parseColor / withAlpha', () => {
  it('parses hex and rgb', () => {
    expect(parseColor('#1D2129')).toEqual([29, 33, 41]);
    expect(parseColor('rgb(29, 33, 41)')).toEqual([29, 33, 41]);
  });

  it('adds alpha for canvas freeze-line gradients', () => {
    expect(withAlpha('#1D2129', 0.22)).toBe('rgba(29, 33, 41, 0.22)');
    expect(withAlpha('rgb(201, 205, 212)', 0)).toBe('rgba(201, 205, 212, 0)');
  });

  it('clamps alpha to 0..1', () => {
    expect(withAlpha('#000000', 2)).toBe('rgba(0, 0, 0, 1)');
    expect(withAlpha('#000000', -1)).toBe('rgba(0, 0, 0, 0)');
  });
});
