import { describe, expect, it } from 'vitest';
import {
  buildEnumSaveBody,
  buildListSaveBody,
  canSaveConfig,
  emptyListConfig,
  ensureLovCodePrefix,
  filterLovRows,
  isSameAppListUrl,
  lovPermFlags,
  lovRowOf,
  lovTypeLabel,
  validateLovCode,
} from './lovAdmin';
import { Auth } from '@newlifex/page-utils';

describe('lovPermFlags', () => {
  it('权限空对象 → 三个按钮都允许', () => {
    const f = lovPermFlags({});
    expect(f.canView).toBe(true);
    expect(f.canAdd).toBe(true);
    expect(f.canEdit).toBe(true);
    expect(f.canDelete).toBe(true);
  });

  it('仅有 Detail → 新增/编辑/删除均为否', () => {
    const f = lovPermFlags({ [String(Auth.VIEW)]: '查看' });
    expect(f.canView).toBe(true);
    expect(f.canAdd).toBe(false);
    expect(f.canEdit).toBe(false);
    expect(f.canDelete).toBe(false);
  });

  it('有键且无 Detail → 不可看', () => {
    const f = lovPermFlags({ [String(Auth.ADD)]: '新增' });
    expect(f.canView).toBe(false);
  });
});

describe('buildEnumSaveBody', () => {
  it('ENUM 配置剔除空 value 行，保留非空字符串', () => {
    const body = buildEnumSaveBody('Enum.Status', [
      { value: '', label: '空', sort: 0 },
      { value: '0', label: '零', sort: 1 },
      { value: 'false', label: '假', sort: 2 },
    ]);
    expect(body.enumItems).toEqual([
      { value: '0', label: '零', sort: 1, enabled: true },
      { value: 'false', label: '假', sort: 2, enabled: true },
    ]);
  });
});

describe('buildListSaveBody', () => {
  it('LIST body 含完整 listConfig 与列数组', () => {
    const body = buildListSaveBody('List.Users', {
      listConfig: {
        ...emptyListConfig(),
        requestUrl: 'https://api.example.com/users',
        method: 'POST',
        pageable: true,
        proxyRequest: true,
      },
      tableColumns: [
        {
          field: 'id',
          title: '编号',
          width: 80,
          align: 'left',
          sortable: false,
          refLovCode: '',
          formatType: '',
          sort: 0,
        },
      ],
    });
    expect(body.listConfig.requestUrl).toBe('https://api.example.com/users');
    expect(body.listConfig.method).toBe('POST');
    expect(body.listConfig.pageable).toBe(true);
    expect(body.listConfig.proxyRequest).toBe(true);
    expect(body.tableColumns).toHaveLength(1);
    expect(body.searchFields).toEqual([]);
  });

  it('同源 / 开头地址强制 proxyRequest=false', () => {
    const body = buildListSaveBody('List.Users', {
      listConfig: { ...emptyListConfig(), requestUrl: '/api/Admin/User', proxyRequest: true },
    });
    expect(isSameAppListUrl('/api/Admin/User')).toBe(true);
    expect(body.listConfig.proxyRequest).toBe(false);
  });
});

describe('canSaveConfig / lovTypeLabel', () => {
  it('类型 AUTO → canSaveConfig=false；文案友好', () => {
    expect(canSaveConfig('AUTO')).toBe(false);
    expect(canSaveConfig('ENUM')).toBe(true);
    expect(canSaveConfig('LIST')).toBe(true);
    expect(lovTypeLabel('ENUM')).toBe('枚举');
    expect(lovTypeLabel('LIST')).toBe('自定义列表');
  });
});

describe('filterLovRows / lovRowOf / validate', () => {
  it('关键字过滤编码与名称', () => {
    const rows = [
      {
        id: 'Enum.A',
        lovCode: 'Enum.A',
        name: '状态',
        type: 'ENUM',
        valueField: '',
        labelField: '',
        enabled: true,
        remark: '',
      },
      {
        id: 'List.B',
        lovCode: 'List.B',
        name: '用户',
        type: 'LIST',
        valueField: '',
        labelField: '',
        enabled: true,
        remark: '',
      },
    ];
    expect(filterLovRows(rows, '状态')).toHaveLength(1);
    expect(filterLovRows(rows, 'list.b')).toHaveLength(1);
  });

  it('行映射与编码校验', () => {
    expect(lovRowOf({ LovCode: 'Enum.X', Name: 'X', Type: 'ENUM' })?.lovCode).toBe('Enum.X');
    expect(validateLovCode('')).toBeTruthy();
    expect(validateLovCode('Enum.Ok_1')).toBeNull();
    expect(ensureLovCodePrefix('Status', 'ENUM')).toBe('Enum.Status');
    expect(ensureLovCodePrefix('Enum.Status', 'ENUM')).toBe('Enum.Status');
  });
});
