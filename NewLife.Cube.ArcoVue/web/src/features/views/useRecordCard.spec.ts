import { describe, expect, it } from 'vitest';
import {
  estimateOpsBtnWidth,
  fitOpsLinkInlineCount,
} from './useRecordCard';
import type { OpsCustomLink } from '@/core/utils/opsAction';

const links = (labels: string[]): OpsCustomLink[] =>
  labels.map((label, i) => ({
    name: `L${i}`,
    label,
    url: `/x/${i}`,
  }));

describe('fitOpsLinkInlineCount', () => {
  it('宽度不足时自定义链接全部进更多（直出 0）', () => {
    const crud = ['详情', '编辑', '删除'];
    const n = fitOpsLinkInlineCount({
      availableWidth: 200,
      crudLabels: crud,
      links: links(['链接', '令牌', '挂载']),
    });
    expect(n).toBe(0);
  });

  it('宽度充足时可直出部分链接', () => {
    const crud = ['详情', '编辑'];
    const ls = links(['链', '牌']);
    // 详情+编辑 ≈ 56*2，再加两条短链与更多应能放下若干
    const wide = fitOpsLinkInlineCount({
      availableWidth: 480,
      crudLabels: crud,
      links: ls,
    });
    expect(wide).toBe(2);
  });

  it('无自定义链接时返回 0', () => {
    expect(
      fitOpsLinkInlineCount({
        availableWidth: 400,
        crudLabels: ['详情'],
        links: [],
      }),
    ).toBe(0);
  });

  it('estimateOpsBtnWidth 中文宽于英文', () => {
    expect(estimateOpsBtnWidth('详情')).toBeGreaterThan(estimateOpsBtnWidth('AB'));
  });
});
