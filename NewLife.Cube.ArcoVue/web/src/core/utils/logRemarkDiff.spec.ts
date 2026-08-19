import { describe, expect, it } from 'vitest';
import { parseRemarkDiff } from './logRemarkDiff';

const fields = [
  { name: 'Name', displayName: '名称' },
  { name: 'DisplayName', displayName: '显示名' },
  { name: 'Enable', displayName: '启用' },
  { name: 'Sort', displayName: '排序' },
  { name: 'Remark', displayName: '备注' },
  { name: 'Password', displayName: '口令' },
];

describe('parseRemarkDiff (OSC-260819e483 P4)', () => {
  it('标量 Update：解析出多个字段新旧值（长名优先）', () => {
    const r = parseRemarkDiff(
      'ID=12,Name=张三 -> 李四,Enable=1 -> 0,Sort=1 -> 2',
      fields,
    );
    expect(r).not.toBeNull();
    const diffs = r!;
    expect(diffs.length).toBe(3);
    // 主键 ID=12 无箭头不进入
    expect(diffs.map((d) => d.field)).toEqual(['Name', 'Enable', 'Sort']);
    expect(diffs[0]).toEqual({ field: 'Name', displayName: '名称', oldValue: '张三', newValue: '李四' });
    expect(diffs[1]).toEqual({ field: 'Enable', displayName: '启用', oldValue: '1', newValue: '0' });
  });

  it('长名优先：DisplayName 不被 Name 误吞', () => {
    const r = parseRemarkDiff('DisplayName=旧 -> 新,Name=a -> b', fields);
    expect(r).not.toBeNull();
    const diffs = r!;
    expect(diffs.map((d) => d.field)).toEqual(['DisplayName', 'Name']);
    expect(diffs[0].displayName).toBe('显示名');
  });

  it('Insert 快照（无箭头）→ null（回落原文）', () => {
    expect(parseRemarkDiff('ID=12,Name=张三', fields)).toBeNull();
    expect(parseRemarkDiff('', fields)).toBeNull();
    expect(parseRemarkDiff(null as unknown as string, fields)).toBeNull();
  });

  it('口令列：两侧已清空，仍解析出但值均为空（不展示明文）', () => {
    const r = parseRemarkDiff('Password= -> ', fields);
    expect(r).not.toBeNull();
    expect(r![0].field).toBe('Password');
    expect(r![0].oldValue).toBe('');
    expect(r![0].newValue).toBe('');
  });

  it('自动化 JSON / 非 Update 文法：不整段 JSON.parse，无箭头则回落', () => {
    // 自动化 JSON 由 historyRemark 处理（action=automation），此处不解析 JSON
    const r = parseRemarkDiff('{"type":"start","ok":true}', fields);
    expect(r).toBeNull();
    // 无字段锚点的散文
    expect(parseRemarkDiff('未知内容没有等号', fields)).toBeNull();
  });

  it('逗号在值内：值被逗号截断错乱 → 整体回退 null（历史 Tab 显示原文）', () => {
    const r = parseRemarkDiff('Name=含,逗号 -> 新,Enable=1 -> 0', fields);
    // Name 段 old 含逗号 → 解析错乱，整体回退
    expect(r).toBeNull();
  });
});
