import { describe, expect, it } from 'vitest';
import {
  addMentionEntry,
  buildMentionIds,
  insertMentionText,
  removeMentionEntry,
  MENTION_MAX,
  type MentionEntry,
} from './commentMention';

const entry = (id: number, name = `用户${id}`): MentionEntry => ({ id, name });

describe('buildMentionIds（OSC-260926c2b8 提交体）', () => {
  it('去重且保持顺序', () => {
    expect(buildMentionIds([entry(3), entry(1), entry(3), entry(2)])).toEqual([3, 1, 2]);
  });

  it('去掉自己', () => {
    expect(buildMentionIds([entry(1), entry(2), entry(3)], 2)).toEqual([1, 3]);
  });

  it('截断到最多 20 个', () => {
    const list = Array.from({ length: 25 }, (_, i) => entry(i + 1));
    const ids = buildMentionIds(list);
    expect(ids).toHaveLength(MENTION_MAX);
    expect(ids[0]).toBe(1);
    expect(ids[19]).toBe(20);
  });

  it('非法 id（<=0）跳过', () => {
    expect(buildMentionIds([entry(0), entry(-1), entry(5)])).toEqual([5]);
  });
});

describe('addMentionEntry', () => {
  it('未选且未满时加入', () => {
    const list = addMentionEntry([entry(1)], { id: 2, name: '张三' });
    expect(list).toEqual([entry(1), { id: 2, name: '张三' }]);
  });

  it('已选的不重复加入', () => {
    const base = [entry(1)];
    expect(addMentionEntry(base, { id: 1, name: '用户1' })).toEqual(base);
  });

  it('自己不会加入', () => {
    expect(addMentionEntry([], { id: 9, name: '我' }, 9)).toEqual([]);
  });

  it('满 20 后不再加入（第 21 人进不来）', () => {
    const full = Array.from({ length: MENTION_MAX }, (_, i) => entry(i + 1));
    expect(addMentionEntry(full, { id: 99, name: '第二十一人' })).toEqual(full);
  });

  it('不修改入参数组', () => {
    const base: MentionEntry[] = [];
    addMentionEntry(base, { id: 1, name: 'a' });
    expect(base).toEqual([]);
  });
});

describe('removeMentionEntry', () => {
  it('移除列表项并删除正文第一次出现的 @显示名', () => {
    const res = removeMentionEntry([entry(1, '张三'), entry(2, '李四')], 1, '你好 @张三 请看', '张三');
    expect(res.entries).toEqual([entry(2, '李四')]);
    expect(res.content).toBe('你好 请看');
  });

  it('正文没有 @显示名 时仅移除列表项', () => {
    const res = removeMentionEntry([entry(1, '张三')], 1, '纯文本', '张三');
    expect(res.entries).toEqual([]);
    expect(res.content).toBe('纯文本');
  });

  it('句尾 @显示名（无尾随空格）同样可删', () => {
    const res = removeMentionEntry([entry(1, '张三')], 1, '麻烦 @张三', '张三');
    expect(res.content).toBe('麻烦 ');
  });
});

describe('insertMentionText', () => {
  it('光标处插入 @显示名 并返回新光标位置', () => {
    const res = insertMentionText('你好世界', '张三', 2);
    expect(res.content).toBe('你好@张三 世界');
    expect(res.cursor).toBe(6);
  });

  it('光标缺失/越界时追加到末尾', () => {
    expect(insertMentionText('你好', '张三').content).toBe('你好@张三 ');
    expect(insertMentionText('你好', '张三', 99).content).toBe('你好@张三 ');
  });
});
