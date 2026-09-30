export interface FieldPatchErrorItem {
  id?: string;
  message?: string;
  Id?: string;
  Message?: string;
}

export interface FieldPatchResultLike {
  ok?: number;
  fail?: number;
  errors?: FieldPatchErrorItem[];
  Ok?: number;
  Fail?: number;
  Errors?: FieldPatchErrorItem[];
}

export interface FieldPatchRead {
  ok: number;
  fail: number;
  errors: { id?: string; message?: string }[];
}

function asRecord(res: unknown): Record<string, unknown> | undefined {
  if (!res || typeof res !== 'object') return undefined;
  const obj = res as Record<string, unknown>;
  const inner = obj.data ?? obj.Data;
  if (inner && typeof inner === 'object') return inner as Record<string, unknown>;
  return obj;
}

/** 读取 BatchUpdateFields / PatchFields 结果，兼容 camelCase / PascalCase 与多一层 data */
export function readFieldPatchResult(res: unknown): FieldPatchRead {
  const d = (asRecord(res) ?? {}) as FieldPatchResultLike;
  const rawErrors = d.errors ?? d.Errors ?? [];
  const errors = rawErrors.map((e) => ({
    id: e.id ?? e.Id,
    message: e.message ?? e.Message,
  }));
  return {
    ok: Number(d.ok ?? d.Ok ?? 0) || 0,
    fail: Number(d.fail ?? d.Fail ?? 0) || 0,
    errors,
  };
}
