import { computed, onMounted, reactive, ref, watch, type ComputedRef, type Ref } from 'vue';
import type { AutomationEntityOption, AutomationRecipientOption } from '@cube/api-core';
import cubeApi from '@/api';
import type { FieldMeta } from '@/core/types/field';
import type { ActionDraft } from '@/core/utils/automationGraph';
import {
  FILTER_OP_LABELS,
  FILTER_OPS_BY_KIND,
  draftToFilter,
  filterToDraftRows,
  newFilterDraftRow,
  opNeedsValue,
  resolveFieldFilterKind,
  type FilterDraftRow,
} from '@/core/utils/filterBuilder';
import type { ViewFilter, ViewFilterOp } from '@/core/utils/viewProfile';

export type FieldAssignRow = { name: string; value: string };
export type HeaderRow = { key: string; value: string };
export type RecipientKind = 'users' | 'roles' | 'departments';

function asNumArray(v: unknown): number[] {
  if (!Array.isArray(v)) return [];
  return v.map((x) => Number(x)).filter((n) => Number.isFinite(n) && n > 0);
}

function inferRecipientKind(to: Record<string, unknown>): RecipientKind {
  const k = String(to.kind ?? '').toLowerCase();
  if (k === 'users' || k === 'user') return 'users';
  if (k === 'roles' || k === 'role') return 'roles';
  if (k === 'departments' || k === 'department' || k === 'dept') return 'departments';
  const u = asNumArray(to.users).length;
  const r = asNumArray(to.roles).length;
  const d = asNumArray(to.departments).length;
  if (r && !u && !d) return 'roles';
  if (d && !u && !r) return 'departments';
  return 'users';
}

/** 动作卡片：各动作 data 双向编辑 */
export function useAutomationActionCard(
  action: ComputedRef<ActionDraft>,
  fields: Ref<FieldMeta[]> | ComputedRef<FieldMeta[]>,
) {
  const collapsed = ref(false);
  const data = computed(() => {
    if (!action.value.data) action.value.data = {};
    return action.value.data as Record<string, any>;
  });

  /** 通知：用户/角色/部门 三选一 */
  const recipientKind = computed({
    get(): RecipientKind {
      ensureTo();
      return inferRecipientKind(data.value.to as Record<string, unknown>);
    },
    set(kind: RecipientKind) {
      ensureTo();
      const to = data.value.to as Record<string, unknown>;
      to.kind = kind;
      to.users = kind === 'users' ? asNumArray(to.users) : [];
      to.roles = kind === 'roles' ? asNumArray(to.roles) : [];
      to.departments = kind === 'departments' ? asNumArray(to.departments) : [];
    },
  });

  const recipientIds = computed({
    get(): number[] {
      ensureTo();
      const to = data.value.to as Record<string, unknown>;
      const kind = inferRecipientKind(to);
      if (kind === 'roles') return asNumArray(to.roles);
      if (kind === 'departments') return asNumArray(to.departments);
      return asNumArray(to.users);
    },
    set(ids: number[]) {
      ensureTo();
      const to = data.value.to as Record<string, unknown>;
      const kind = inferRecipientKind(to);
      const clean = asNumArray(ids);
      to.kind = kind;
      to.users = kind === 'users' ? clean : [];
      to.roles = kind === 'roles' ? clean : [];
      to.departments = kind === 'departments' ? clean : [];
    },
  });

  function ensureTo() {
    if (!data.value.to || typeof data.value.to !== 'object') {
      data.value.to = { kind: 'users', users: [], roles: [], departments: [] };
    }
    const to = data.value.to as Record<string, unknown>;
    if (!to.kind) to.kind = inferRecipientKind(to);
  }

  const recipientOptions = ref<AutomationRecipientOption[]>([]);
  const recipientLoading = ref(false);
  let recipientSeq = 0;

  function normalizeRecipients(raw: unknown): AutomationRecipientOption[] {
    const arr = Array.isArray(raw) ? raw : [];
    return arr
      .map((r) => {
        const row = r as Record<string, unknown>;
        const id = Number(row.id ?? row.Id ?? 0);
        const name = String(row.name ?? row.Name ?? '');
        const displayName = String(
          row.displayName ?? row.DisplayName ?? (name || id),
        );
        return { id, name, displayName };
      })
      .filter((x) => Number.isFinite(x.id) && x.id > 0);
  }

  async function searchRecipientsViaEntity(keyword = '') {
    const pathMap = {
      users: '/Admin/User',
      roles: '/Admin/Role',
      departments: '/Admin/Department',
    } as const;
    const type = pathMap[recipientKind.value];
    const res = await cubeApi.page.getList(type, {
      pageIndex: 0,
      pageSize: 50,
      ...(keyword ? { q: keyword } : {}),
    });
    const rows = (res.data ?? []) as Record<string, unknown>[];
    return rows
      .map((row) => {
        const id = Number(row.id ?? row.Id ?? row.iD ?? 0);
        const name = String(row.name ?? row.Name ?? '');
        const displayName = String(
          row.displayName ??
            row.DisplayName ??
            row.fullName ??
            row.FullName ??
            (name || id),
        );
        return { id, name, displayName } as AutomationRecipientOption;
      })
      .filter((x) => Number.isFinite(x.id) && x.id > 0);
  }

  async function searchRecipients(keyword = '') {
    const kindMap = { users: 'user', roles: 'role', departments: 'department' } as const;
    const kind = kindMap[recipientKind.value];
    const seq = ++recipientSeq;
    recipientLoading.value = true;
    try {
      const tasks: Promise<AutomationRecipientOption[]>[] = [
        (async () => {
          try {
            const res = await cubeApi.automation.recipients({
              kind,
              key: keyword || undefined,
            });
            return normalizeRecipients(res.data);
          } catch {
            return [];
          }
        })(),
        (async () => {
          try {
            return await searchRecipientsViaEntity(keyword);
          } catch {
            return [];
          }
        })(),
      ];
      const [fromApi, fromEntity] = await Promise.all(tasks);
      if (seq !== recipientSeq) return;
      // 优先接口；空则回退实体列表；两侧都有时取更长的一份
      let list =
        fromApi.length >= fromEntity.length && fromApi.length > 0
          ? fromApi
          : fromEntity.length > 0
            ? fromEntity
            : fromApi;
      // 已选 id 若不在当前页，保留展示项避免空白标签
      const selected = recipientIds.value;
      if (selected.length) {
        const map = new Map(list.map((x) => [x.id, x]));
        for (const id of selected) {
          if (!map.has(id)) {
            map.set(id, { id, name: String(id), displayName: String(id) });
          }
        }
        list = [...map.values()];
      }
      recipientOptions.value = list;
    } finally {
      if (seq === recipientSeq) recipientLoading.value = false;
    }
  }

  function onRecipientIdsUpdate(v: unknown) {
    recipientIds.value = asNumArray(v);
  }

  let recipientTimer: ReturnType<typeof setTimeout> | null = null;
  function onRecipientSearch(keyword: string) {
    if (recipientTimer) clearTimeout(recipientTimer);
    recipientTimer = setTimeout(() => void searchRecipients(keyword), 300);
  }

  watch(recipientKind, () => {
    recipientOptions.value = [];
    void searchRecipients('');
  });

  // —— 字段赋值 ——
  const fieldRows = computed({
    get(): FieldAssignRow[] {
      const raw = data.value.fields;
      if (!Array.isArray(raw) || raw.length === 0) return [{ name: '', value: '' }];
      return raw.map((f: { name?: string; value?: string }) => ({
        name: String(f?.name ?? ''),
        value: String(f?.value ?? ''),
      }));
    },
    set(rows: FieldAssignRow[]) {
      data.value.fields = rows
        .filter((r) => (r.name || '').trim())
        .slice(0, 32)
        .map((r) => ({ name: r.name.trim(), value: String(r.value ?? '') }));
    },
  });

  function addFieldRow() {
    const rows = [...fieldRows.value];
    if (rows.length >= 32) return;
    rows.push({ name: '', value: '' });
    data.value.fields = rows;
  }

  function removeFieldRow(i: number) {
    const rows = [...fieldRows.value];
    rows.splice(i, 1);
    data.value.fields = rows.length ? rows : [{ name: '', value: '' }];
  }

  function patchFieldRow(i: number, patch: Partial<FieldAssignRow>) {
    const rows = [...fieldRows.value];
    rows[i] = { ...rows[i], ...patch };
    data.value.fields = rows;
  }

  // —— 实体下拉（查找=Update / 创建=Insert）——
  const updateEntities = ref<AutomationEntityOption[]>([]);
  const insertEntities = ref<AutomationEntityOption[]>([]);
  const findSearchFields = ref<FieldMeta[]>([]);
  const createWritableFields = ref<FieldMeta[]>([]);
  const findFieldsLoading = ref(false);

  async function loadEntityOptions() {
    try {
      const [u, i] = await Promise.all([
        cubeApi.automation.entities('update'),
        cubeApi.automation.entities('insert'),
      ]);
      updateEntities.value = u.data ?? [];
      insertEntities.value = i.data ?? [];
    } catch {
      updateEntities.value = [];
      insertEntities.value = [];
    }
  }

  async function loadMetaFields(typePath: string, kind: 'all' | 'search'): Promise<FieldMeta[]> {
    const tp = (typePath || '').replace(/^\/+/, '');
    if (!tp) return [];
    try {
      if (kind === 'search') {
        try {
          const page = await cubeApi.page.getPage(`/${tp}`);
          const search = (page.data as { search?: FieldMeta[] } | null)?.search;
          if (Array.isArray(search) && search.length) return search.filter((f) => f.name);
        } catch {
          /* fall through */
        }
      }
      const res = await cubeApi.automation.meta(tp, { kind });
      return (res.data ?? []).map((f) => ({
        name: f.name,
        displayName: f.displayName,
        typeName: f.typeName || 'String',
        primaryKey: f.primaryKey,
        readOnly: f.readOnly,
      }));
    } catch {
      return [];
    }
  }

  async function loadFindSearchFields(typePath: string) {
    findFieldsLoading.value = true;
    try {
      findSearchFields.value = await loadMetaFields(typePath, 'search');
    } finally {
      findFieldsLoading.value = false;
    }
  }

  async function loadCreateFields(typePath: string) {
    const all = await loadMetaFields(typePath, 'all');
    createWritableFields.value = all.filter((f) => f.name && !f.primaryKey && !f.readOnly);
  }

  watch(
    () => [action.value?.type, data.value.typePath] as const,
    ([type, typePath]) => {
      const tp = String(typePath || '');
      if (type === 'findRecords') void loadFindSearchFields(tp);
      if (type === 'createRecord') void loadCreateFields(tp);
    },
    { immediate: true },
  );

  // —— findRecords 筛选 ——
  const findFilterLogic = computed({
    get(): 'all' | 'any' {
      const f = data.value.filter as ViewFilter | undefined;
      return f?.logic === 'any' ? 'any' : 'all';
    },
    set(v: 'all' | 'any') {
      syncFindFilter(v, findFilterRows);
    },
  });

  const findFilterRows = reactive<FilterDraftRow[]>([]);

  function syncFindFilter(logic: 'all' | 'any', rows: FilterDraftRow[]) {
    const filter = draftToFilter(logic, rows);
    if (!filter.conditions.length) {
      delete data.value.filter;
      return;
    }
    data.value.filter = filter;
  }

  watch(
    () => action.value,
    (act) => {
      findFilterRows.splice(0, findFilterRows.length);
      if (act?.type !== 'findRecords') return;
      const f = (act.data?.filter as ViewFilter | undefined) ?? { logic: 'all', conditions: [] };
      findFilterRows.push(...filterToDraftRows(f));
    },
    { immediate: true },
  );

  watch(
    findFilterRows,
    () => {
      if (action.value?.type !== 'findRecords') return;
      syncFindFilter(findFilterLogic.value, findFilterRows);
    },
    { deep: true },
  );

  const filterFieldSource = computed(() => {
    if (action.value?.type === 'findRecords' && findSearchFields.value.length) {
      return findSearchFields.value;
    }
    return fields.value || [];
  });

  function opsOf(fieldName: string): ViewFilterOp[] {
    const f = filterFieldSource.value.find((x) => x.name === fieldName);
    if (!f) return [...FILTER_OPS_BY_KIND.string];
    return [...FILTER_OPS_BY_KIND[resolveFieldFilterKind(f)]];
  }

  function addFindFilterRow() {
    findFilterRows.push(newFilterDraftRow());
  }

  function removeFindFilterRow(i: number) {
    findFilterRows.splice(i, 1);
  }

  function onFindFilterField(row: FilterDraftRow) {
    const ops = opsOf(row.cond.field);
    if (!ops.includes(row.cond.op)) row.cond.op = ops[0];
    if (!opNeedsValue(row.cond.op)) row.cond.value = undefined;
  }

  function onFindTypePathChange(typePath: string) {
    data.value.typePath = typePath;
    findFilterRows.splice(0, findFilterRows.length);
    delete data.value.filter;
    void loadFindSearchFields(typePath);
  }

  // —— HTTP headers ——
  const headerRows = computed({
    get(): HeaderRow[] {
      const h = data.value.headers;
      if (!h || typeof h !== 'object' || Array.isArray(h)) return [{ key: '', value: '' }];
      const entries = Object.entries(h as Record<string, unknown>);
      if (!entries.length) return [{ key: '', value: '' }];
      return entries.map(([key, value]) => ({ key, value: String(value ?? '') }));
    },
    set(rows: HeaderRow[]) {
      const obj: Record<string, string> = {};
      for (const r of rows) {
        const k = (r.key || '').trim();
        if (!k) continue;
        obj[k] = String(r.value ?? '');
        if (Object.keys(obj).length >= 16) break;
      }
      data.value.headers = Object.keys(obj).length ? obj : undefined;
    },
  });

  function addHeaderRow() {
    const rows = [...headerRows.value];
    if (rows.length >= 16) return;
    rows.push({ key: '', value: '' });
    headerRows.value = rows;
  }

  function removeHeaderRow(i: number) {
    const rows = [...headerRows.value];
    rows.splice(i, 1);
    headerRows.value = rows.length ? rows : [{ key: '', value: '' }];
  }

  function patchHeaderRow(i: number, patch: Partial<HeaderRow>) {
    const rows = [...headerRows.value];
    rows[i] = { ...rows[i], ...patch };
    headerRows.value = rows;
  }

  const writableFields = computed(() => {
    if (action.value?.type === 'createRecord' && createWritableFields.value.length) {
      return createWritableFields.value;
    }
    return (fields.value || []).filter((f) => f.name && !f.primaryKey && !f.readOnly);
  });

  onMounted(() => {
    void loadEntityOptions();
    if (action.value?.type === 'notify') void searchRecipients('');
  });

  watch(
    () => action.value?.type,
    (t) => {
      if (t === 'notify') void searchRecipients('');
      collapsed.value = false;
    },
  );

  return {
    collapsed,
    data,
    recipientKind,
    recipientIds,
    recipientOptions,
    recipientLoading,
    onRecipientSearch,
    onRecipientIdsUpdate,
    searchRecipients,
    fieldRows,
    addFieldRow,
    removeFieldRow,
    patchFieldRow,
    updateEntities,
    insertEntities,
    findSearchFields,
    findFieldsLoading,
    filterFieldSource,
    findFilterLogic,
    findFilterRows,
    opsOf,
    addFindFilterRow,
    removeFindFilterRow,
    onFindFilterField,
    onFindTypePathChange,
    FILTER_OP_LABELS,
    opNeedsValue,
    headerRows,
    addHeaderRow,
    removeHeaderRow,
    patchHeaderRow,
    writableFields,
  };
}
