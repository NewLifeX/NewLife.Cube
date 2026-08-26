/** 解析 SSE 一行：须 `data: ` 前缀；JSON 失败返回 null */
export function parseSseDataLine(line: string): unknown | null {
  const t = line.trim();
  if (!t.startsWith('data: ')) return null;
  try {
    return JSON.parse(t.slice(6));
  } catch {
    return null;
  }
}

/** 从缓冲切出完整 SSE 事件；最后一行未完则留在 rest */
export function takeSseEvents(buffer: string): { rest: string; events: Record<string, unknown>[] } {
  const lines = buffer.split('\n');
  const rest = lines.pop() ?? '';
  const events: Record<string, unknown>[] = [];
  for (const line of lines) {
    const json = parseSseDataLine(line);
    if (json && typeof json === 'object' && !Array.isArray(json)) {
      events.push(json as Record<string, unknown>);
    }
  }
  return { rest, events };
}

/** 对话结束事件：须在此结束读取，否则代理可能不关连接，输入框会一直 disabled */
export function isAiSseDone(json: Record<string, unknown> | null | undefined): boolean {
  return json?.type === 'done';
}
