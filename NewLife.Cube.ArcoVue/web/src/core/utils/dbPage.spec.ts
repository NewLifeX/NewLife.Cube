import { describe, expect, it } from 'vitest';
import { Auth } from '@newlifex/page-utils';
import { getDbActionPermissions, splitDbDescription } from './dbPage';

describe('数据库页纯函数', () => {
  it('只有 Update 权限时不显示查看动作但显示压缩', () => {
    expect(getDbActionPermissions({ [String(Auth.EDIT)]: '编辑' })).toEqual({
      canInspect: false,
      canCompact: true,
    });
  });

  it('splitDbDescription 以首个句号拆分名称与备注', () => {
    expect(splitDbDescription('AccessRule', '访问规则。控制系统访问的安全访问规则，放行或拦截或限流')).toEqual({
      name: '访问规则',
      remark: '控制系统访问的安全访问规则，放行或拦截或限流',
    });
    expect(splitDbDescription('User', '用户')).toEqual({ name: '用户', remark: '' });
    expect(splitDbDescription('EntityViewProfile', undefined)).toEqual({
      name: 'EntityViewProfile',
      remark: '',
    });
    expect(splitDbDescription('X', 'A。B。C')).toEqual({ name: 'A', remark: 'B。C' });
  });
});
