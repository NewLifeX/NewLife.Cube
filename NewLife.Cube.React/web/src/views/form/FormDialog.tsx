/**
 * 列表表单弹窗（命令式 add/edit Modal）
 *
 * 依据 FieldMapping 渲染 FieldControl，提交时 serializeSubmitModel 序列化。
 * 编辑模式自动加载详情（布尔串转布尔）。
 */
import { useEffect, useMemo, useState } from 'react';
import { Col, Form, Modal, Row, message } from 'antd';
import type { RuleObject } from 'antd/es/form';
import FieldControl from '@/components/field/FieldControl';
import { serializeSubmitModel, isFullWidthControl, resolveControl } from '@/utils/fieldControl';
import { toFieldMeta } from '@/types/field';
import type { FieldMapping } from '@cube/field-mapping';
import { useAiFillForm } from '@/hooks/useAiFillForm';

export interface FormDialogProps {
  open: boolean;
  title?: string;
  mode: 'add' | 'edit';
  fields: FieldMapping[];
  /** 编辑时的行数据（含主键） */
  row?: Record<string, unknown> | null;
  apiPrefix?: string;
  submitting?: boolean;
  onSubmit?: (data: Record<string, unknown>) => Promise<void>;
  onCancel?: () => void;
}

/** 详情布尔串转布尔 */
function normalizeDetail(data: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(data).map(([k, v]) => [k, v === 'true' ? true : v === 'false' ? false : v]),
  );
}

export default function FormDialog({
  open,
  title = '新增',
  mode,
  fields,
  row,
  apiPrefix,
  submitting,
  onSubmit,
  onCancel,
}: FormDialogProps) {
  const [form] = Form.useForm();
  // AI 填表：弹窗打开时监听 AiAssistant 派发的 cube:ai-fill-form 事件
  useAiFillForm(form, open);
  const [detail, setDetail] = useState<Record<string, unknown>>({});

  // 表单字段元数据（过滤主键与只读字段；编辑时主键已通过 setFieldsValue(row) 保留在表单中）
  const metas = useMemo(
    () =>
      fields
        .map((f) => toFieldMeta(f.field))
        .filter((f) => !f.primaryKey && !f.readOnly),
    [fields],
  );

  // 打开时初始化表单值
  useEffect(() => {
    if (!open) return;
    form.resetFields();
    setDetail({});
    if (mode === 'edit' && row) {
      const values = normalizeDetail(row);
      form.setFieldsValue(values);
      setDetail(values);
    }
  }, [open, mode, row, form]);

  const handleOk = async () => {
    try {
      const values = await form.validateFields();
      const model = serializeSubmitModel(values, metas);
      // 编辑模式：主键已通过 setFieldsValue(row) 保留在表单中，随 model 提交
      await onSubmit?.(model);
    } catch (err) {
      // 校验失败或提交失败
      if ((err as Error)?.message) message.error((err as Error).message);
    }
  };

  const recordId = mode === 'edit' && row ? (Object.values(row)[0] as number | string | undefined) : undefined;

  /** 构建字段校验规则 */
  const buildRules = (meta: ReturnType<typeof toFieldMeta>): RuleObject[] => {
    const rules: RuleObject[] = [];
    if (meta.required) {
      rules.push({ required: true, message: `请输入${meta.displayName || meta.name}` });
    }
    if (meta.itemType === 'mail') {
      rules.push({ type: 'email', message: '邮箱格式不正确' });
    }
    if (meta.itemType === 'mobile') {
      rules.push({ pattern: /^1\d{10}$/, message: '手机号格式不正确' });
    }
    return rules;
  };

  return (
    <Modal
      open={open}
      title={title}
      onOk={() => void handleOk()}
      onCancel={onCancel}
      confirmLoading={submitting}
      okText="保存"
      cancelText="取消"
      width={680}
      destroyOnClose
    >
      <Form form={form} layout="vertical" requiredMark={false} style={{ marginTop: 16 }}>
        <Row gutter={16}>
          {metas.map((meta) => {
            const control = resolveControl(meta);
            const span = isFullWidthControl(control) ? 24 : 12;
            return (
              <Col key={meta.name} span={span}>
                <Form.Item
                  name={meta.name}
                  label={meta.displayName || meta.name}
                  rules={buildRules(meta)}
                >
                  <FieldControl
                    field={meta}
                    apiPrefix={apiPrefix}
                    recordId={recordId}
                  />
                </Form.Item>
              </Col>
            );
          })}
        </Row>
      </Form>
    </Modal>
  );
}
