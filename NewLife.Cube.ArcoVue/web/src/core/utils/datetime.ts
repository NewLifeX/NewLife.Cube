/** 格式化日期时间：YYYY-MM-DD HH:mm:ss；无效/空输入返回 '-' */
export function formatDateTime(v: unknown): string {
  if (v == null || v === '') return '-';
  const d = typeof v === 'string' || typeof v === 'number' ? new Date(v) : (v as Date);
  if (Number.isNaN(d.getTime())) return '-';
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}
