/**
 * 列表表单弹窗（命令式 add/edit Modal）
 *
 * 依据 FieldMapping 渲染 FieldControl，提交时 serializeSubmitModel 序列化。
 * 编辑模式自动加载详情（布尔串转布尔）。
 */
import { useEffect, useMemo, useState } from 'react';
import { App, Form, Modal, Row, Tabs } from 'antd';
import FormFieldItem from './FormFieldItem';
import { groupByCategory, hasCategory, serializeSubmitModel } from '@/utils/fieldControl';
import { toFieldMeta, type FieldMeta } from '@/types/field';
import type { FieldMapping } from '@newlifex/field-mapping';
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
  const { message } = App.useApp();
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

  // 分组：字段带 Category 才按分类 Tabs 分组，否则平铺单页（字段少的页面一屏展示）
  const groups = useMemo(() => {
    if (!hasCategory(metas)) return null;
    return groupByCategory(metas);
  }, [metas]);

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

  /** 渲染一组字段栅格（分组内 / 平铺共用，label|控件|description 同排） */
  const renderFields = (list: FieldMeta[]) => (
    <Row gutter={16}>
      {list.map((meta) => (
        <FormFieldItem key={meta.name} field={meta} apiPrefix={apiPrefix} recordId={recordId} />
      ))}
    </Row>
  );

  return (
    <Modal
      open={open}
      title={title}
      onOk={() => void handleOk()}
      onCancel={onCancel}
      confirmLoading={submitting}
      okText="保存"
      cancelText="取消"
      width={800}
      destroyOnHidden
      // 高度自适应：内容少时模态窗贴合内容，接近视口上限才出现滚动条；overflowX 兜底横向溢出
      styles={{ body: { maxHeight: 'calc(100vh - 240px)', overflowY: 'auto', overflowX: 'hidden' } }}
    >
      <Form form={form} layout="vertical" requiredMark={false} style={{ marginTop: 16 }}>
        {groups ? (
          <Tabs
            items={groups.map((g) => ({
              key: g.category,
              label: g.category,
              // forceRender：非激活分组表单项也注册进 Form，跨 Tab 必填校验生效
              forceRender: true,
              children: renderFields(g.fields),
            }))}
          />
        ) : (
          renderFields(metas)
        )}
      </Form>
    </Modal>
  );
}
