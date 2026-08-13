import { computed, ref, watch } from 'vue';
import type { FieldMeta } from '@/core/types/field';
import { isAuditField, isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
import { applyFormLayout, groupFieldsByCategory } from '@/core/utils/fieldGroups';
import type { FormLayout } from '@/core/utils/viewProfile';
import { isFieldRequired } from '@/core/utils/submitPayload';
import { fieldFormatRules } from '@/core/utils/validation';

/** FormContent 组件 props 类型（与 FormContent.vue defineProps 泛型逐字一致） */
interface FormContentProps {
  fields: FieldMeta[];
  model: Record<string, unknown>;
  typePath: string;
  readonly?: boolean;
  /** 新增时隐藏主键/只读（对齐 Cube.Vue） */
  mode?: 'add' | 'edit' | 'detail';
  /** 后端字段级错误（FieldErrors），映射到对应 a-form-item（OSC-0009） */
  fieldErrors?: { field: string; message: string }[];
  /** 受限表单布局（OSC-0013）：字段排序/显隐/Category 折叠；null 表示元数据原序 */
  layout?: FormLayout | null;
}

/** FormContent 组件全部业务 TS：字段分组可见性、校验规则与 FieldErrors 映射（自 FormContent.vue script setup 原样搬移） */
export function useFormContent(props: FormContentProps) {
  const formRef = ref();

  const visibleFields = computed(() => {
    // 新增/编辑均隐藏审计字段（创建/更新用户、IP、时间），由后端自动维护
    const withoutAudit = props.fields.filter((f) => !isAuditField(f));
    if (props.mode === 'add') {
      return withoutAudit.filter((f) => !f.primaryKey && !f.readOnly);
    }
    return withoutAudit;
  });

  /** 应用受限布局：hidden 过滤 + order 排序 + Category 折叠（OSC-0013） */
  const appliedGroups = computed(() =>
    applyFormLayout(groupFieldsByCategory(visibleFields.value), props.layout),
  );
  const visibleGroups = computed(() => appliedGroups.value.groups);
  const collapsed = computed(() => appliedGroups.value.collapsed);
  const collapsedSet = computed(() => new Set(collapsed.value));

  function rulesFor(field: FieldMeta) {
    const rules = [...fieldFormatRules(field)];
    if (isFieldRequired(field)) {
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

  return {
    formRef,
    visibleGroups,
    collapsedSet,
    isFullWidthControl,
    resolveControl,
    isFieldRequired,
    rulesFor,
    validate,
    clearValidate,
  };
}
