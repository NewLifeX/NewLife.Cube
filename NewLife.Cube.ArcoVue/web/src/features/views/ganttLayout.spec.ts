import { describe, expect, it } from 'vitest';
import type { GanttMapping } from '@/core/utils/viewMapping';
import {
  ganttMappingSignature,
  ganttRecordsSignature,
  nearlySame,
} from './ganttLayout';

const mapping: GanttMapping = {
  kind: 'gantt',
  titleField: 'Name',
  plannedStartField: 'Start',
  plannedEndField: 'End',
};

describe('nearlySame', () => {
  it('ignores 1px jitter and treats 2px as change', () => {
    expect(nearlySame(380, 381)).toBe(true);
    expect(nearlySame(520, 522)).toBe(false);
  });
});

describe('ganttMappingSignature', () => {
  it('ignores tableWidth so width persist does not remount', () => {
    const a = ganttMappingSignature({ ...mapping, tableWidth: 380 });
    const b = ganttMappingSignature({ ...mapping, tableWidth: 420 });
    expect(a).toBe(b);
  });

  it('changes when planned fields change', () => {
    expect(ganttMappingSignature(mapping)).not.toBe(
      ganttMappingSignature({ ...mapping, plannedStartField: 'PlanStart' }),
    );
  });
});

describe('ganttRecordsSignature', () => {
  it('same content different array identity is equal', () => {
    const row = { Id: 1, Name: 'A', Start: '2026-01-01', End: '2026-01-02' };
    const a = ganttRecordsSignature([row], mapping, 'Id');
    const b = ganttRecordsSignature([{ ...row }], mapping, 'Id');
    expect(a).toBe(b);
  });

  it('changes when a date value changes', () => {
    const a = ganttRecordsSignature(
      [{ Id: 1, Name: 'A', Start: '2026-01-01', End: '2026-01-02' }],
      mapping,
      'Id',
    );
    const b = ganttRecordsSignature(
      [{ Id: 1, Name: 'A', Start: '2026-02-01', End: '2026-01-02' }],
      mapping,
      'Id',
    );
    expect(a).not.toBe(b);
  });
});
