import { describe, expect, it } from 'vitest';
import {
  AI_ATTACH_MAX_COUNT,
  attachSkipMessage,
  buildAiChatMessage,
  displayAiUserText,
  formatAiAttachmentBlock,
  formatByteSize,
  isTextAttachment,
  pickAiAttachments,
  truncateAiAttachText,
} from './aiAttach';

function file(name: string, size: number, type = 'text/plain'): File {
  const buf = new Uint8Array(size);
  return new File([buf], name, { type });
}

describe('isTextAttachment', () => {
  it('按 MIME 与扩展名识别文本', () => {
    expect(isTextAttachment({ type: 'text/plain', name: 'a.bin' })).toBe(true);
    expect(isTextAttachment({ type: 'application/json', name: 'a' })).toBe(true);
    expect(isTextAttachment({ type: '', name: 'note.md' })).toBe(true);
    expect(isTextAttachment({ type: 'image/png', name: 'a.png' })).toBe(false);
  });
});

describe('formatByteSize / truncateAiAttachText', () => {
  it('格式化体积', () => {
    expect(formatByteSize(0)).toBe('0 B');
    expect(formatByteSize(512)).toBe('512 B');
    expect(formatByteSize(2048)).toBe('2.0 KB');
  });

  it('超长文本截断', () => {
    expect(truncateAiAttachText('abc', 10)).toBe('abc');
    expect(truncateAiAttachText('abcdefghij', 4)).toBe('abcd\n…(已截断)');
  });
});

describe('pickAiAttachments', () => {
  it('跳过空文件、超大文件，并截到上限', () => {
    const { accepted, skipped } = pickAiAttachments(4, [
      file('empty.txt', 0),
      file('big.bin', 6 * 1024 * 1024, 'application/octet-stream'),
      file('ok.txt', 10),
      file('late.txt', 10),
    ]);
    expect(accepted.map((f) => f.name)).toEqual(['ok.txt']);
    expect(skipped.map((s) => s.reason)).toEqual(['empty', 'size', 'count']);
  });
});

describe('attachSkipMessage / display / build', () => {
  it('合并跳过原因', () => {
    expect(
      attachSkipMessage([
        { name: 'a', reason: 'count' },
        { name: 'b', reason: 'size' },
      ]),
    ).toContain(`最多 ${AI_ATTACH_MAX_COUNT} 个附件`);
    expect(attachSkipMessage([])).toBeNull();
  });

  it('气泡展示不含正文内容', () => {
    expect(displayAiUserText('你好', [{ name: 'a.txt' }])).toBe('你好\n附件：a.txt');
    expect(displayAiUserText('', [{ name: 'a.txt' }, { name: 'b.png' }])).toBe('附件：a.txt、b.png');
  });

  it('AiChat message 附上文本块', () => {
    const block = formatAiAttachmentBlock([
      { name: 'a.txt', size: 4, type: 'text/plain', text: 'hi' },
      { name: 'b.png', size: 2048, type: 'image/png' },
    ]);
    expect(block).toContain('[附件]');
    expect(block).toContain('hi');
    expect(block).toContain('二进制或非文本');
    expect(buildAiChatMessage('问一下', [{ name: 'a.txt', size: 1, type: 'text/plain', text: 'x' }])).toMatch(
      /^问一下\n\n\[附件\]/,
    );
    expect(buildAiChatMessage('  ', [])).toBe('');
  });
});
