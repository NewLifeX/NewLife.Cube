/** AI 浮窗附件：条数/体积上限、文本抽取判定、拼进 AiChat.message（不改后端协议） */

export const AI_ATTACH_MAX_COUNT = 5;
export const AI_ATTACH_MAX_BYTES = 5 * 1024 * 1024;
export const AI_ATTACH_TEXT_MAX = 8000;

export type AiAttachSkipReason = 'count' | 'size' | 'empty';

export interface AiAttachMeta {
  name: string;
  size: number;
  type: string;
  text?: string;
}

const TEXT_EXT =
  /\.(txt|md|csv|json|xml|log|yml|yaml|ts|tsx|js|jsx|mjs|cjs|cs|vue|css|scss|html|htm|sql|ini|conf)$/i;

export function isTextAttachment(file: { type: string; name: string }): boolean {
  const t = (file.type || '').toLowerCase();
  if (t.startsWith('text/')) return true;
  if (t === 'application/json' || t === 'application/xml' || t === 'application/javascript') return true;
  return TEXT_EXT.test(file.name || '');
}

export function formatByteSize(n: number): string {
  if (!Number.isFinite(n) || n < 0) return '0 B';
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(n < 10 * 1024 ? 1 : 0)} KB`;
  return `${(n / (1024 * 1024)).toFixed(n < 10 * 1024 * 1024 ? 1 : 0)} MB`;
}

export function truncateAiAttachText(raw: string, max = AI_ATTACH_TEXT_MAX): string {
  if (raw.length <= max) return raw;
  return `${raw.slice(0, max)}\n…(已截断)`;
}

export function pickAiAttachments(
  currentCount: number,
  files: File[],
  opts?: { maxCount?: number; maxBytes?: number },
): { accepted: File[]; skipped: Array<{ name: string; reason: AiAttachSkipReason }> } {
  const maxCount = opts?.maxCount ?? AI_ATTACH_MAX_COUNT;
  const maxBytes = opts?.maxBytes ?? AI_ATTACH_MAX_BYTES;
  const accepted: File[] = [];
  const skipped: Array<{ name: string; reason: AiAttachSkipReason }> = [];
  let count = Math.max(0, currentCount);
  for (const file of files) {
    const name = file.name || '未命名';
    if (!file.size) {
      skipped.push({ name, reason: 'empty' });
      continue;
    }
    if (file.size > maxBytes) {
      skipped.push({ name, reason: 'size' });
      continue;
    }
    if (count >= maxCount) {
      skipped.push({ name, reason: 'count' });
      continue;
    }
    accepted.push(file);
    count += 1;
  }
  return { accepted, skipped };
}

export function attachSkipMessage(
  skipped: Array<{ name: string; reason: AiAttachSkipReason }>,
  opts?: { maxCount?: number; maxBytes?: number },
): string | null {
  if (!skipped.length) return null;
  const maxCount = opts?.maxCount ?? AI_ATTACH_MAX_COUNT;
  const maxBytes = opts?.maxBytes ?? AI_ATTACH_MAX_BYTES;
  const reasons = new Set(skipped.map((s) => s.reason));
  const parts: string[] = [];
  if (reasons.has('count')) parts.push(`最多 ${maxCount} 个附件`);
  if (reasons.has('size')) parts.push(`单个不超过 ${formatByteSize(maxBytes)}`);
  if (reasons.has('empty')) parts.push('不能上传空文件');
  return parts.join('；');
}

export function displayAiUserText(text: string, files: Array<{ name: string }>): string {
  const t = (text || '').trim();
  const names = files.map((f) => f.name).filter(Boolean);
  if (!names.length) return t;
  const line = `附件：${names.join('、')}`;
  return t ? `${t}\n${line}` : line;
}

export function formatAiAttachmentBlock(files: AiAttachMeta[]): string {
  if (!files.length) return '';
  const parts = files.map((f, i) => {
    const head = `${i + 1}. ${f.name}（${formatByteSize(f.size)}）`;
    if (f.text) return `${head}\n${f.text}`;
    return `${head}\n（二进制或非文本，仅提供文件名与大小）`;
  });
  return `\n\n[附件]\n${parts.join('\n\n')}`;
}

export function buildAiChatMessage(text: string, files: AiAttachMeta[]): string {
  const t = (text || '').trim();
  const block = formatAiAttachmentBlock(files);
  if (t) return t + block;
  return block.trim() || '';
}
