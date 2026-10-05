/**
 * 地图工具栏业务逻辑（OSC-261004d7f4 后续增强）：
 * 1. 查询后聚焦：关键字/预定义/自定义查询后，结果就绪时在地图视图定位到首个有坐标的行（保持缩放）；
 *    结果 >2 条时显示查询结果卡片（分类图标/颜色 + 标题字段）。
 * 2. 地图「添加记录」轻量弹层：仅必填项 + 位置信息（支持地图点击拾取坐标）。
 *
 * 独立 composable：SFC 构薄门禁（sfcThin）要求 .vue 不内嵌 watch / 业务生命周期。
 */
import { computed, nextTick, ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import type { FieldMeta } from '@/core/types/field';
import { detectCoordinateFields } from '@/core/utils/mapCoord';
import { isFieldRequired } from '@/core/utils/submitPayload';
import type { MapMapping } from '@/core/utils/viewMapping';
import type { ViewFilter, ViewKind } from '@/core/utils/viewProfile';

/** 只读 ref 结构（兼容 Ref 与 ComputedRef 传参） */
type ReadRef<T> = { readonly value: T };

/** 地图视图组件暴露句柄（与 MapView defineExpose 保持一致） */
export interface MapViewHandle {
  ready: boolean;
  zoomIn: () => void;
  zoomOut: () => void;
  zoom: number;
  satellite: boolean;
  toggleSatellite: () => boolean;
  focusRow: (row: Record<string, unknown>) => boolean;
  focusFirstLocated: (rows: readonly Record<string, unknown>[]) => string;
  setResultsVisible: (v: boolean) => void;
  clearPickMarker: () => void;
  getCenterData: () => { lng: number; lat: number } | null;
}

interface MapToolsDeps {
  /** 当前列表数据（查询结果首批） */
  tableData: ReadRef<Record<string, unknown>[]>;
  /** 当前视图种类（仅 map 生效） */
  activeViewKind: ReadRef<ViewKind>;
  /** 后端总行数（结果卡片显隐阈值 2） */
  total: () => number;
  mapViewRef: ReadRef<MapViewHandle | null>;
  activeMapMapping: ReadRef<MapMapping | null>;
  /** 列表字段（坐标自动检测同源） */
  listFields: ReadRef<FieldMeta[]>;
  /** 新建表单字段（弹层必填项来源） */
  addFields: ReadRef<FieldMeta[]>;
  /** 轻量创建提交（useListCrud.createRowQuick） */
  createRowQuick: (model: Record<string, unknown>) => Promise<boolean>;
  /** 工具栏关键字查询 */
  handleSearch: () => void;
  /** 应用预定义查询 */
  handleApplyQuery: (id: string) => void;
  /** 应用自定义查询（筛选构建器） */
  onFilterApply: (filter: ViewFilter) => void;
}

export function useMapTools(deps: MapToolsDeps) {
  const { tableData, activeViewKind, total, mapViewRef, activeMapMapping, listFields, addFields, createRowQuick, handleSearch, handleApplyQuery, onFilterApply } = deps;

  // —— 查询后聚焦与结果卡片 ——
  /** 查询入口包装：标记待定位；结果就绪后在地图视图聚焦（保持当前缩放） */
  const mapFocusAfterQuery = ref(false);
  function onQuerySearch() {
    mapFocusAfterQuery.value = true;
    handleSearch();
  }
  function onQueryApply(id: string) {
    mapFocusAfterQuery.value = true;
    handleApplyQuery(id);
  }
  /** 自定义查询应用：同样纳入地图查询聚焦/结果卡片链路 */
  function onMapFilterApply(filter: ViewFilter) {
    mapFocusAfterQuery.value = true;
    onFilterApply(filter);
  }
  watch(
    () => tableData.value,
    (rows) => {
      if (!mapFocusAfterQuery.value) return;
      mapFocusAfterQuery.value = false;
      if (activeViewKind.value !== 'map') return;
      const first = rows[0];
      if (first) {
        // 结果 >2 条：显示查询结果卡片（分类图标/颜色+标题字段）并定位首个有坐标的行；≤2 条仅定位不显示卡片
        const t = total();
        void nextTick(() => {
          mapViewRef.value?.setResultsVisible?.(t > 2);
          mapViewRef.value?.focusFirstLocated?.(rows);
        });
      } else {
        mapViewRef.value?.setResultsVisible?.(false);
      }
    },
  );

  // —— 地图「添加记录」轻量弹层 ——
  const mapAddVisible = ref(false);
  const mapAddSaving = ref(false);
  const mapAddForm = ref<Record<string, unknown>>({});
  /** 位置信息单输入框文本（统一「经度,纬度」展示；提交时解析拆回坐标字段） */
  const mapAddCoordText = ref('');
  /** 坐标字段检测（与地图管线同源：mapping 配置优先，否则按字段自动识别） */
  const mapAddDetection = computed(() => {
    const m = activeMapMapping.value;
    if (m?.coordMode === 'latlng' && m.lngField && m.latField) {
      return { mode: 'latlng' as const, lngField: m.lngField, latField: m.latField };
    }
    if (m?.coordMode === 'merged' && m.coordField) {
      return { mode: 'merged' as const, coordField: m.coordField };
    }
    return detectCoordinateFields([...listFields.value]);
  });
  /** 是否显示位置信息区（检测到坐标字段才显示） */
  const mapAddCoordEnabled = computed(() => mapAddDetection.value != null);
  /** 弹层必填字段（add 分区；剔除坐标字段——坐标归入位置区） */
  const mapAddFields = computed(() => {
    const det = mapAddDetection.value;
    const coordNames = new Set(
      det ? (det.mode === 'latlng' ? [det.lngField, det.latField] : [det.coordField]) : [],
    );
    return addFields.value.filter((f) => isFieldRequired(f) && !coordNames.has(f.name));
  });
  function onMapAddOpen() {
    mapAddForm.value = {};
    // 预填位置 = 当前地图中心（数据坐标系；统一「经度,纬度」展示）
    mapAddCoordText.value = '';
    const c = mapViewRef.value?.getCenterData?.();
    if (c && mapAddDetection.value) mapAddCoordText.value = `${c.lng},${c.lat}`;
    mapAddVisible.value = true;
  }
  /** 地图拾取回填（数据坐标系；写入位置文本供用户确认后提交） */
  function onMapPick(lng: number, lat: number) {
    mapAddCoordText.value = `${lng},${lat}`;
  }
  function isEmptyValue(v: unknown): boolean {
    return v === undefined || v === null || v === '';
  }
  /** 解析位置文本「经度,纬度」；空格返回 null；非「两数且在范围内」返回 undefined */
  function parseCoordText(text: string): { lng: number; lat: number } | null | undefined {
    const t = text.trim();
    if (!t) return null;
    const parts = t.split(/[,，\s]+/).filter(Boolean).map(Number);
    if (parts.length !== 2) return undefined;
    const [a, b] = parts;
    if (!Number.isFinite(a) || !Number.isFinite(b) || Math.abs(a) > 180 || Math.abs(b) > 90) {
      return undefined;
    }
    return { lng: a, lat: b };
  }
  async function onMapAddSubmit() {
    const missing = mapAddFields.value.filter((f) => isEmptyValue(mapAddForm.value[f.name]));
    if (missing.length) {
      Message.warning(`请填写：${missing.map((f) => f.displayName || f.name).join('、')}`);
      return;
    }
    // 位置必填：未选择/无效不允许保存；合法则按坐标字段形态拆分写入
    const det = mapAddDetection.value;
    const coordWrites: Record<string, unknown> = {};
    if (det) {
      const parsed = parseCoordText(mapAddCoordText.value);
      if (parsed === undefined) {
        Message.warning('位置（经纬度）无效：应为「经度,纬度」，如 114.5,23.5');
        return;
      }
      if (parsed === null) {
        Message.warning('请选择位置：在地图上点击选取，或输入「经度,纬度」');
        return;
      }
      if (det.mode === 'latlng') {
        coordWrites[det.lngField] = parsed.lng;
        coordWrites[det.latField] = parsed.lat;
      } else {
        // 合并坐标字段：按 mapping.coordOrder 拼写（缺省 lnglat）
        coordWrites[det.coordField] =
          activeMapMapping.value?.coordOrder === 'latlng'
            ? `${parsed.lat},${parsed.lng}`
            : `${parsed.lng},${parsed.lat}`;
      }
    }
    mapAddSaving.value = true;
    const ok = await createRowQuick({ ...mapAddForm.value, ...coordWrites });
    mapAddSaving.value = false;
    if (ok) mapAddVisible.value = false;
  }
  /** 弹层关闭（取消/保存后）：清除地图拾取临时标记 */
  watch(
    () => mapAddVisible.value,
    (v) => {
      if (!v) mapViewRef.value?.clearPickMarker?.();
    },
  );

  return {
    onQuerySearch,
    onQueryApply,
    onMapFilterApply,
    mapAddVisible,
    mapAddSaving,
    mapAddForm,
    mapAddCoordText,
    mapAddFields,
    mapAddCoordEnabled,
    onMapAddOpen,
    onMapPick,
    onMapAddSubmit,
  };
}
