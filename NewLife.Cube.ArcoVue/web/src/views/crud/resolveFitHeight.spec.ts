import { describe, expect, it } from 'vitest';
import { fitContentHeight, resolveFitHeight } from './listContext';

describe('resolveFitHeight', () => {
  it('内容 200、视口 800 时宿主为 200', () => {
    expect(resolveFitHeight(200, 800)).toBe(200);
  });

  it('内容远高于视口时宿主等于视口，且不低于 240', () => {
    expect(resolveFitHeight(5000, 800)).toBe(800);
    expect(resolveFitHeight(5000, 100)).toBe(240);
  });

  it('按行估算的内容高度下限为 240', () => {
    expect(fitContentHeight(0)).toBe(240);
    expect(resolveFitHeight(fitContentHeight(0), 800)).toBe(240);
  });
});