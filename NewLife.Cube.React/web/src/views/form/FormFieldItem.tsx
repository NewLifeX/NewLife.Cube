/**
 * 表单表单项（label | 控件 | description 同排三栏，对齐 MVC _Form_Item）
 *
 * 供 FormDialog / FormPage / ConfigPage 共用：
 * - Form.Item 直接包裹 FieldControl，保证 value/onChange 注入与表单状态联动；
 * - 标签与描述列由 CSS Grid 同排布局（见 styles/entity.css `.cube-form-inline-*`）；
 * - 描述列仅当裁剪后非空时渲染，单行省略、悬浮显示完整；
 * - 必填/邮箱/手机校验规则内聚于此，视图无需重复构建。
 */
import { Col, Form } from 'antd';
import type { RuleObject } from 'antd/es/form';
import FieldControl from '@/components/field/FieldControl';
import { isFullWidthControl, resolveControl, resolveDescription } from '@/utils/fieldControl';
import type { FieldMeta } from '@/types/field';

export interface FormFieldItemProps {
  /** 字段元数据 */
  field: FieldMeta;
  /** 实体路径前缀（上传需要，如 '/Admin/User'） */
  apiPrefix?: string;
  /** 主记录主键（上传需要，0=新增） */
  recordId?: number | string;
}

/** 构建字段校验规则（必填 + 邮箱/手机格式） */
function buildRules(meta: FieldMeta): RuleObject[] {
  const rules: RuleObject[] = [];
  const label = meta.displayName || meta.name;
  if (meta.required) {
    rules.push({ required: true, message: `请输入${label}` });
  }
  if (meta.itemType === 'mail') {
    rules.push({ type: 'email', message: '邮箱格式不正确' });
  }
  if (meta.itemType === 'mobile') {
    rules.push({ pattern: /^1\d{10}$/, message: '手机号格式不正确' });
  }
  return rules;
}

export default function FormFieldItem({ field, apiPrefix, recordId }: FormFieldItemProps) {
  const control = resolveControl(field);
  const fullWidth = isFullWidthControl(control);
  const displayName = field.displayName || field.name;
  const desc = resolveDescription(field);

  const cls = [
    'cube-form-inline-cell',
    desc ? 'cube-form-inline-cell--desc' : '',
    fullWidth ? 'cube-form-inline-cell--top' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <Col span={fullWidth ? 24 : 12}>
      <div className={cls}>
        <label className="cube-form-inline-label" title={displayName}>
          {displayName}
        </label>
        <Form.Item name={field.name} rules={buildRules(field)} className="cube-form-inline-control">
          <FieldControl field={field} apiPrefix={apiPrefix} recordId={recordId} />
        </Form.Item>
        {desc && (
          <div className="cube-form-inline-desc" title={desc}>
            {desc}
          </div>
        )}
      </div>
    </Col>
  );
}
