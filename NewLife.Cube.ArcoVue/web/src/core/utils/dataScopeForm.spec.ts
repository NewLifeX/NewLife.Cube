import { describe, expect, it } from 'vitest';
import {
  CUSTOM_DATA_SCOPE,
  isCustomDataScope,
  shouldShowDataDepartmentIds,
  shouldShowDataScopeField,
} from './dataScopeForm';

describe('isCustomDataScope', () => {
  it('accepts numeric, string and named enum values', () => {
    expect(isCustomDataScope(CUSTOM_DATA_SCOPE)).toBe(true);
    expect(isCustomDataScope('4')).toBe(true);
    expect(isCustomDataScope('自定义')).toBe(true);
    expect(isCustomDataScope('custom')).toBe(true);
    expect(isCustomDataScope('Custom')).toBe(true);
  });

  it('rejects other scopes and empty values', () => {
    expect(isCustomDataScope(0)).toBe(false);
    expect(isCustomDataScope(1)).toBe(false);
    expect(isCustomDataScope(2)).toBe(false);
    expect(isCustomDataScope(3)).toBe(false);
    expect(isCustomDataScope('3')).toBe(false);
    expect(isCustomDataScope('本部门')).toBe(false);
    expect(isCustomDataScope(null)).toBe(false);
    expect(isCustomDataScope(undefined)).toBe(false);
    expect(isCustomDataScope('')).toBe(false);
  });
});

describe('shouldShowDataDepartmentIds', () => {
  it('follows dataScope in camelCase or PascalCase', () => {
    expect(shouldShowDataDepartmentIds({ dataScope: 4 })).toBe(true);
    expect(shouldShowDataDepartmentIds({ DataScope: '自定义' })).toBe(true);
    expect(shouldShowDataDepartmentIds({ dataScope: 2 })).toBe(false);
    expect(shouldShowDataDepartmentIds({})).toBe(false);
    expect(shouldShowDataDepartmentIds(null)).toBe(false);
  });
});

describe('shouldShowDataScopeField', () => {
  it('hides DataDepartmentIds on role form unless scope is custom', () => {
    expect(
      shouldShowDataScopeField({ typePath: '/Admin/Role', model: { dataScope: 2 }, fieldName: 'DataDepartmentIds' }),
    ).toBe(false);
    expect(
      shouldShowDataScopeField({ typePath: '/Admin/Role', model: { dataScope: 4 }, fieldName: 'DataDepartmentIds' }),
    ).toBe(true);
  });

  it('keeps other fields and other modules visible', () => {
    expect(
      shouldShowDataScopeField({ typePath: '/Admin/Role', model: { dataScope: 2 }, fieldName: 'Name' }),
    ).toBe(true);
    expect(
      shouldShowDataScopeField({ typePath: '/Admin/User', model: { dataScope: 2 }, fieldName: 'DataDepartmentIds' }),
    ).toBe(true);
  });
});
