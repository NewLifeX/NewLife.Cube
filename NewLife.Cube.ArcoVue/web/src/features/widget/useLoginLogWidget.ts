import { computed } from 'vue';
import type { WidgetCardProps } from './context';

function asRows(raw: unknown, keys: [string, string][]): Record<string, string>[] {
  if (!Array.isArray(raw)) return [];
  return raw.map((it) => {
    const o = (it && typeof it === 'object' ? it : {}) as Record<string, unknown>;
    const row: Record<string, string> = {};
    for (const [camel, pascal] of keys) {
      const v = o[camel] ?? o[pascal];
      row[camel] = v instanceof Date ? v.toLocaleString() : String(v ?? '');
    }
    return row;
  });
}

export function useLoginLogWidget(props: WidgetCardProps) {
  const logins = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    return asRows(r.logins ?? r.Logins, [
      ['createTime', 'CreateTime'],
      ['userName', 'UserName'],
      ['action', 'Action'],
      ['createIP', 'CreateIP'],
    ]);
  });
  const onlines = computed(() => {
    const r = (props.result || {}) as Record<string, unknown>;
    return asRows(r.onlines ?? r.Onlines, [
      ['name', 'Name'],
      ['createTime', 'CreateTime'],
      ['oAuthProvider', 'OAuthProvider'],
    ]);
  });
  return { logins, onlines };
}
