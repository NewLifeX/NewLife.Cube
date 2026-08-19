import { computed } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import type { SavedQuery } from '@/core/utils/viewProfile';
import { useMasterTimeRange } from '@/core/composables/useMasterTimeRange';

/** SearchDrawer 组件 props 类型（与 SearchDrawer.vue defineProps 泛型逐字一致） */
interface SearchDrawerProps {
  /** 抽屉可见性 */
  visible: boolean;
  /** search 分区字段（GetPage Search 列表顺序） */
  fields: FieldMeta[];
  /** 搜索表单对象（父组件 reactive，直接读写其属性） */
  model: Record<string, unknown>;
  /** 主时间字段名（OSC-0016）；无 MasterTime 时不渲染主时间范围 */
  masterTimeName?: string | null;
  /** 主时间字段显示名（OSC-0016） */
  masterTimeDisplayName?: string | null;
  /** 关键字 Q 是否启用（OSC-0016）；false 时不渲染关键字框 */
  enableKey?: boolean;
  /** 预定义查询列表（OSC-0016） */
  queries: SavedQuery[];
  /** 当前应用的预定义查询 id（会话内存） */
  activeQueryId: string | null;
  /** 当前参数与 activeQuery 是否不一致（条目 ✓ 标记控制） */
  paramsDirty: boolean;
  /** 当前参数是否可保存（非空） */
  canSave: boolean;
}

/** SearchDrawer 组件全部业务 TS：查询条件列表与主时间范围映射（自 SearchDrawer.vue script setup 原样搬移） */
export function useSearchDrawer(props: SearchDrawerProps) {
  /** 其余查询条件：主时间字段不重复渲染（单独特殊控件），其余按 GetPage Search 顺序 */
  const fieldItems = computed(() =>
    props.fields.filter((f) => f.name !== props.masterTimeName),
  );

  // 主时间范围双向映射（与历史 InsightPanel 共用同一实现，避免两份漂移）
  const { masterTimeRange, onMasterTimeChange } = useMasterTimeRange(props.model);

  return {
    fieldItems,
    masterTimeRange,
    onMasterTimeChange,
  };
}
