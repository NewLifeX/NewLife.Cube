/**
 * DefaultObject 业务 TS（OSC-2608139feb 魔方设置优化）。
 *
 * 通用 ObjectController 配置中心：
 * - GET {type} + GetFields + enrich + FieldInput + PUT
 * - 左列表右配置：菜单树自动发现 Object 配置页（会话级探测缓存），统一入口
 * - Category 作为当前对象（如魔方设置）的子菜单管理；右侧按分组不折叠、流式 6/12
 * - description 经 form-item tooltip 展示
 * 权限不足/GET 失败 → 表单 disabled、保存隐藏、a-alert 说明。
 */
import { computed, onMounted, reactive, ref } from 'vue';
import { Message } from '@arco-design/web-vue';
import { FieldKind, type MenuItem } from '@cube/api-core';
import cubeApi from '@/api';
import { useUserStore } from '@/stores/user';
import { resolveCrudFlags } from '@/core/utils/permissions';
import { toFieldMetas } from '@/core/utils/fieldNormalize';
import { serializeSubmitModel } from '@/core/utils/fieldControl';
import { groupFieldsByCategory, mergeObjectModel } from '@/core/utils/objectForm';
import { collectObjectCandidates, type ObjectPageRef } from '@/core/utils/objectPages';
import { detectPageKind, type PageKind } from '@/core/utils/pageKind';
import {
  enrichFieldsWithEnumDataSource,
  enrichFieldsWithLookup,
} from '@/core/utils/lov-api';
import { formatApiError } from '@/core/utils/apiError';
import type { FieldMeta } from '@/core/types/field';

/** DefaultObject 组件 props 类型（与 DefaultObject.vue defineProps 泛型逐字一致） */
interface DefaultObjectProps {
  type: string;
  authId?: number;
}

/** 从 request 响应解包 data（兼容已解包/未解包两种形态） */
function unwrapData(res: unknown): unknown {
  if (res && typeof res === 'object' && !Array.isArray(res) && 'data' in res) {
    return (res as { data?: unknown }).data;
  }
  return res;
}

/** Object 页探测结果会话级缓存：同 type 只探测一次 */
const objectKindCache = new Map<string, boolean>();

/** 菜单名（displayName 优先）；未命中回落 type 末段 */
function menuNameOf(menus: MenuItem[], type: string): string {
  const seg = type.replace(/^\/+/, '').split('/').pop() ?? '';
  const key = type.replace(/^\/+/, '').toLowerCase();
  const walk = (items: MenuItem[]): string | null => {
    for (const m of items ?? []) {
      if ((m.url ?? '').replace(/^\/+/, '').toLowerCase() === key) {
        return m.displayName || m.name || seg;
      }
      if (m.children?.length) {
        const hit = walk(m.children);
        if (hit) return hit;
      }
    }
    return null;
  };
  return walk(menus) ?? seg;
}

/** DefaultObject 组件全部业务 TS：加载/切换/分组折叠/对象发现/保存（薄 SFC 宿主） */
export function useDefaultObject(props: DefaultObjectProps) {
  const userStore = useUserStore();

  /** 当前配置对象类型（左侧切换后加载对应对象） */
  const currentType = ref(props.type);
  /** 可配置对象列表：至少含当前页；自动注入菜单发现的 Object 页 */
  const objectPages = ref<ObjectPageRef[]>([
    { type: props.type, name: menuNameOf(userStore.menus, props.type) },
  ]);
  const pagesLoading = ref(false);

  const loading = ref(true);
  const loadError = ref('');
  const fields = ref<FieldMeta[]>([]);
  const model = reactive<Record<string, unknown>>({});
  const form = reactive<Record<string, unknown>>({});
  const saving = ref(false);

  /** Category 当前选中分组（多分组时作为子菜单项管理；空表示未初始化/单分组） */
  const activeCategory = ref('');
  /** 左菜单展开的子菜单 key（受控）：当前对象多分组时自动展开，允许手动收起 */
  const openKeys = ref<string[]>([]);

  const flags = computed(() =>
    resolveCrudFlags(userStore.getMenuPermission(currentType.value), null),
  );
  const canUpdate = computed(() => flags.value.canEdit);

  const groups = computed(() => groupFieldsByCategory(fields.value));

  /** 分组名列表（子菜单项） */
  const categories = computed(() => groups.value.map((g) => g.category));
  /** 多分组时当前对象在左菜单呈现为子菜单 */
  const isMultiCategory = computed(() => categories.value.length > 1);
  /** 右侧展示的分组：当前选中分组（多分组时）或唯一分组 */
  const visibleGroups = computed(() => {
    const gs = groups.value;
    if (!gs.length) return [];
    const hit = gs.find((g) => g.category === activeCategory.value) ?? gs[0];
    return [hit];
  });
  /** 左菜单选中 key：多分组为「对象#分组」，否则为对象路径 */
  const activeKey = computed(() =>
    isMultiCategory.value
      ? `${currentType.value}#${activeCategory.value || categories.value[0]}`
      : currentType.value,
  );

  /** 展开当前对象的 Category 子菜单（幂等） */
  function ensureOpen() {
    if (!isMultiCategory.value) return;
    const key = `grp-${currentType.value}`;
    if (!openKeys.value.includes(key)) openKeys.value = [...openKeys.value, key];
  }

  /** 菜单名作页头标题；无菜单回落 type 末段 */
  const title = computed(() => menuNameOf(userStore.menus, currentType.value));

  function resetFormFrom(obj: Record<string, unknown>) {
    Object.keys(form).forEach((k) => delete form[k]);
    Object.assign(form, obj);
  }

  async function load() {
    const type = currentType.value;
    loading.value = true;
    loadError.value = '';
    try {
      const res = await cubeApi.page.getObject(type);
      const body = unwrapData(res);
      if (!body || typeof body !== 'object' || Array.isArray(body)) {
        throw new Error('对象响应无效');
      }
      const obj = body as Record<string, unknown>;
      Object.keys(model).forEach((k) => delete model[k]);
      Object.assign(model, obj);
      resetFormFrom(obj);

      const fres = await cubeApi.page.getFields(type, FieldKind.List);
      const flist = unwrapData(fres);
      const metas = toFieldMetas((Array.isArray(flist) ? flist : []) as never);
      await enrichFieldsWithEnumDataSource(metas);
      await enrichFieldsWithLookup(metas);
      fields.value = metas;
      // 分组就绪后默认选中第一个分组（多分组子菜单）；无分组回落空串
      activeCategory.value = groups.value[0]?.category ?? '';
      // 配置项菜单自动全部展开
      ensureOpen();
    } catch (err) {
      loadError.value = formatApiError(err, '加载失败');
      fields.value = [];
    } finally {
      loading.value = false;
    }
  }

  /** 探测单个 type 的页面种类（与 DynamicPage 探测真值表一致） */
  async function probeKind(type: string): Promise<PageKind> {
    return detectPageKind(type, {
      getPage: (t) => cubeApi.page.getPage(t),
      getObjectProbe: async (t) => {
        const fields = await cubeApi.page.getFields(t, FieldKind.List);
        const body = await cubeApi.page.getList(t, { pageIndex: 0, pageSize: 1 });
        return { fields, body };
      },
    });
  }

  /**
   * 自动发现 Object 配置页并注入左侧列表：菜单树两层 URL 候选逐个探测，
   * 结果会话级缓存；探测失败忽略（不阻断当前页）。
   */
  async function discoverObjectPages() {
    if (pagesLoading.value) return;
    pagesLoading.value = true;
    try {
      const candidates = collectObjectCandidates(userStore.menus, props.type);
      const found: ObjectPageRef[] = [
        { type: props.type, name: menuNameOf(userStore.menus, props.type) },
      ];
      for (const c of candidates) {
        const cached = objectKindCache.get(c.type);
        if (cached === false) continue;
        if (cached === true) {
          found.push(c);
          continue;
        }
        try {
          const kind = await probeKind(c.type);
          if (kind === 'object') {
            objectKindCache.set(c.type, true);
            found.push(c);
          } else {
            objectKindCache.set(c.type, false);
          }
        } catch {
          /* 探测失败忽略 */
        }
      }
      // 稳定顺序：按名称排序（中文环境 localeCompare）
      objectPages.value = [...found].sort((a, b) => a.name.localeCompare(b.name, 'zh'));
    } finally {
      pagesLoading.value = false;
    }
  }

  /** 左侧切换配置页 */
  function onSelectPage(type: string) {
    if (type === currentType.value) return;
    currentType.value = type;
    activeCategory.value = '';
    void load();
  }

  /** 当前对象的分组子菜单切换（不重新请求，字段已全量加载） */
  function onSelectCategory(category: string) {
    if (categories.value.includes(category)) activeCategory.value = category;
  }

  async function save() {
    const type = currentType.value;
    saving.value = true;
    try {
      const normalized = serializeSubmitModel({ ...form }, fields.value);
      const merged = mergeObjectModel({ ...model }, normalized);
      const res = await cubeApi.page.update(type, merged);
      // 用返回对象刷新 model
      const body = unwrapData(res);
      if (body && typeof body === 'object' && !Array.isArray(body)) {
        const obj = body as Record<string, unknown>;
        Object.assign(model, obj);
        resetFormFrom(obj);
      }
      Message.success('保存成功');
    } catch (err) {
      Message.error(formatApiError(err, '保存失败'));
    } finally {
      saving.value = false;
    }
  }

  onMounted(() => {
    void load();
    // 后台自动发现其它 Object 配置页，不阻塞当前页渲染
    void discoverObjectPages();
  });

  return {
    currentType,
    objectPages,
    pagesLoading,
    loading,
    loadError,
    fields,
    groups,
    categories,
    isMultiCategory,
    visibleGroups,
    activeKey,
    model,
    form,
    canUpdate,
    saving,
    title,
    openKeys,
    onSelectPage,
    onSelectCategory,
    save,
    load,
  };
}
