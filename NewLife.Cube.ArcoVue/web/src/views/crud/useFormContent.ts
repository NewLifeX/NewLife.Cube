import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField, isFullWidthControl, isTenantField, resolveControl } from '@/core/utils/fieldControl';
import { isIamPermissionFullWidth } from '@/core/utils/rolePermission';
import { isSystemRoleFlagLocked, isSystemRoleNameLocked } from '@/core/utils/iamGuards';
import { applyFormLayout, groupFieldsByCategory } from '@/core/utils/fieldGroups';
import type { FormLayout } from '@/core/utils/viewProfile';
import { isFieldRequired } from '@/core/utils/submitPayload';
import { fieldFormatRules } from '@/core/utils/validation';
import { useTenantStore } from '@/stores/tenant';

/** FormContent 组件 props 类型（与 FormContent.vue defineProps 泛型逐字一致） */
interface FormContentProps {
  fields: FieldMeta[];
  model: Record<string, unknown>;
  typePath: string;
  readonly?: boolean;
  /** 新增时隐藏主键/只读；编辑默认隐藏主键「编号」（对齐 Cube.Vue） */
  mode?: 'add' | 'edit' | 'detail';
  /** 后端字段级错误（FieldErrors），映射到对应 a-form-item（OSC-0009） */
  fieldErrors?: { field: string; message: string }[];
  /** 受限表单布局（OSC-0013）：字段排序/显隐/Category 折叠；null 表示元数据原序 */
  layout?: FormLayout | null;
}

/** FormContent 组件全部业务 TS：字段分组可见性、校验规则与 FieldErrors 映射（自 FormContent.vue script setup 原样搬移） */
export function useFormContent(props: FormContentProps) {
  const formRef = ref();
  const tenantStore = useTenantStore();

  const visibleFields = computed(() => {
    // 新增/编辑均隐藏审计字段（创建/更新用户、IP、时间），由后端自动维护
    let list = props.fields.filter((f) => !isAuditField(f));
    // 多租户关闭：隐藏 TenantId 等，避免表单/详情出现租户控件
    if (!tenantStore.enableTenant) {
      list = list.filter((f) => !isTenantField(f));
    }
    if (props.mode === 'add') {
      return list.filter((f) => !f.primaryKey && !f.readOnly);
    }
    // 编辑默认不显示主键「编号」（系统自动生成）；详情仍展示便于核对
    if (props.mode === 'edit') {
      return list.filter((f) => !f.primaryKey);
    }
    return list;
  });

  /** 应用受限布局：hidden 过滤 + order 排序 + Category 折叠（OSC-0013） */
  const appliedGroups = computed(() =>
    applyFormLayout(groupFieldsByCategory(visibleFields.value), props.layout),
  );
  const visibleGroups = computed(() => appliedGroups.value.groups);
  const collapsed = computed(() => appliedGroups.value.collapsed);
  const collapsedSet = computed(() => new Set(collapsed.value));

  function fieldRequired(field: FieldMeta): boolean {
    if (isTenantField(field) && !tenantStore.enableTenant) return false;
    return isFieldRequired(field);
  }

  function rulesFor(field: FieldMeta) {
    const rules = [...fieldFormatRules(field)];
    if (fieldRequired(field)) {
      rules.unshift({ required: true, message: `${field.displayName || field.name}不可以为空！` });
    }
    return rules.length ? rules : undefined;
  }

  /** 后端 FieldErrors 的 field 名（PascalCase）与表单字段名做大小写不敏感匹配后写入 Arco Form 字段错误 */
  function applyFieldErrors() {
    const errs = props.fieldErrors;
    if (!errs?.length || !formRef.value) return;
    const names = new Set(visibleFields.value.map((f) => f.name));
    const fields: Record<string, { status: 'error'; message: string }> = {};
    for (const e of errs) {
      const key = names.has(e.field)
        ? e.field
        : visibleFields.value.find((f) => f.name.toLowerCase() === e.field.toLowerCase())?.name;
      if (key && !fields[key]) fields[key] = { status: 'error', message: e.message };
    }
    if (Object.keys(fields).length) formRef.value.setFields(fields);
  }

  watch(() => props.fieldErrors, applyFieldErrors);

  async function validate() {
    return formRef.value?.validate();
  }

  function clearValidate() {
    formRef.value?.clearValidate();
  }

  function fieldDisabled(field: FieldMeta): boolean {
    if (props.readonly || !!field.readOnly || !!field.primaryKey) return true;
    if (isSystemRoleNameLocked(props.typePath, props.model, field.name)) return true;
    if (isSystemRoleFlagLocked(props.typePath, props.model, field.name, props.mode)) return true;
    return false;
  }

  function fieldFullWidth(field: FieldMeta): boolean {
    if (isIamPermissionFullWidth(props.typePath, field)) return true;
    return isFullWidthControl(resolveControl(field));
  }

  return {
    formRef,
    visibleGroups,
    collapsedSet,
    isFullWidthControl,
    resolveControl,
    fieldDisabled,
    fieldFullWidth,
    isFieldRequired: fieldRequired,
    rulesFor,
    validate,
    clearValidate,
  };
}
