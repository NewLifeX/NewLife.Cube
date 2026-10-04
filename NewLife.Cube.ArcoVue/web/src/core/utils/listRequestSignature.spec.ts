import { describe, expect, it } from 'vitest';
import { listRequestSignature } from './listRequestSignature';

describe('listRequestSignature', () => {
  it('normalizes object key order but retains all row-set inputs', () => {
    expect(
      listRequestSignature({
        typePath: '/Admin/User',
        pageIndex: 0,
        viewFilter: '{"field":"Enable"}',
        search: { Q: 'admin', Enable: true },
      }),
    ).toBe(
      listRequestSignature({
        search: { Enable: true, Q: 'admin' },
        viewFilter: '{"field":"Enable"}',
        pageIndex: 0,
        typePath: '/Admin/User',
      }),
    );
  });

  it('changes when a filter, tenant, or page differs', () => {
    const source = {
      typePath: '/Admin/User',
      tenantCode: 'north',
      pageIndex: 0,
      pageSize: 20,
      viewFilter: '',
    };
    expect(listRequestSignature(source)).not.toBe(
      listRequestSignature({ ...source, tenantCode: 'south' }),
    );
    expect(listRequestSignature(source)).not.toBe(
      listRequestSignature({ ...source, pageIndex: 1 }),
    );
    expect(listRequestSignature(source)).not.toBe(
      listRequestSignature({ ...source, viewFilter: '{"logic":"all"}' }),
    );
  });
});
