import { describe, expect, it } from 'vitest';
import { isAiSseDone, parseSseDataLine, takeSseEvents } from './aiSse';

describe('parseSseDataLine', () => {
  it('解析 data: JSON', () => {
    expect(parseSseDataLine('data: {"type":"text","content":"hi"}')).toEqual({
      type: 'text',
      content: 'hi',
    });
  });

  it('多行前缀 trim', () => {
    expect(parseSseDataLine('  data: {"type":"error"}  ')).toEqual({ type: 'error' });
  });

  it('残缺 JSON 跳过', () => {
    expect(parseSseDataLine('data: {not json')).toBeNull();
  });

  it('无 data: 前缀忽略', () => {
    expect(parseSseDataLine('event: ping')).toBeNull();
    expect(parseSseDataLine('')).toBeNull();
  });
});

describe('takeSseEvents', () => {
  it('切出完整行，尾巴留 rest', () => {
    const { rest, events } = takeSseEvents('data: {"type":"text","content":"a"}\n\ndata: {"type":"do');
    expect(events).toEqual([{ type: 'text', content: 'a' }]);
    expect(rest).toBe('data: {"type":"do');
  });
});

describe('isAiSseDone', () => {
  it('type=done 结束流', () => {
    expect(isAiSseDone({ type: 'done' })).toBe(true);
    expect(isAiSseDone({ type: 'text' })).toBe(false);
    expect(isAiSseDone(null)).toBe(false);
  });
});
