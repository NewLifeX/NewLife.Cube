import { describe, expect, it } from 'vitest';
import {
  buildOpsParts,
  buildOpsPartsWithLinks,
  isOpsLinkKey,
  opsLinkKey,
  parseOpsLinkKey,
  type OpsCustomLink,
} from './opsAction';

const links = (n: number): OpsCustomLink[] =>
  Array.from({ length: n }, (_, i) => ({
    name: `L${i + 1}`,
    label: `链接${i + 1}`,
    url: `/x/${i + 1}?id={Id}`,
  }));

describe('buildOpsPartsWithLinks', () => {
  it('顺序：CRUD → 自定义直出≤2 → 自动化≤3 → more', () => {
    const { parts, overflowLinks } = buildOpsPartsWithLinks({
      canViewDetail: true,
      canEdit: true,
      canDelete: true,
      opsLinks: links(4),
      automationButtons: [
        { id: 1, name: 'A1' },
        { id: 2, name: 'A2' },
      ],
    });
    expect(parts).toEqual([
      'detail',
      'edit',
      'delete',
      'link:L1',
      'link:L2',
      'auto:1',
      'auto:2',
      'more',
    ]);
    expect(overflowLinks.map((l) => l.name)).toEqual(['L3', 'L4']);
  });

  it('无溢出时不追加 more', () => {
    const { parts, overflowLinks } = buildOpsPartsWithLinks({
      canViewDetail: true,
      opsLinks: links(1),
    });
    expect(parts).toEqual(['detail', 'link:L1']);
    expect(overflowLinks).toEqual([]);
  });

  it('buildOpsParts 兼容无链接', () => {
    expect(
      buildOpsParts({ canViewDetail: true, canEdit: true, canDelete: false }),
    ).toEqual(['detail', 'edit']);
  });

  it('link key 解析', () => {
    expect(opsLinkKey('Log')).toBe('link:Log');
    expect(isOpsLinkKey('link:Log')).toBe(true);
    expect(parseOpsLinkKey('link:Log')).toBe('Log');
    expect(parseOpsLinkKey('detail')).toBeNull();
  });
});
