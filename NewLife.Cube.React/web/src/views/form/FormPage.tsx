/**
 * 通用表单页（对齐 Vue 皮肤 core/views/form.vue）
 *
 * 通过 URL 判定模式：
 * - `?id=123` → 编辑模式（加载详情）
 * - 无 id → 新增模式
 * 提交 POST/PUT 后返回上一页。
 */
import { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Form, Row, Space, Spin, Tabs, message } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import FieldControl from '@/components/field/FieldControl';
import { groupByCategory, hasCategory, isFullWidthControl, resolveControl, serializeSubmitModel } from '@/utils/fieldControl';
import { routeToApiPrefix, getValueByKey } from '@/utils/url';
import { toFieldMeta, type FieldMeta } from '@/types/field';
import { usePageStore } from '@/hooks/usePageStore';
import { useAiFillForm } from '@/hooks/useAiFillForm';

export interface FormPageProps {
  title?: string;
}

export default function FormPage({ title }: FormPageProps) {
  const [form] = Form.useForm();
  // AI 填表：监听 AiAssistant 派发的 cube:ai-fill-form 事件
  useAiFillForm(form);
  const location = useLocation();
  const navigate = useNavigate();
  const [params] = useSearchParams();

  // 路径去掉 /Edit /Add 等后缀后作为 API 前缀
  const path = location.pathname.replace(/\/+(edit|add|new|detail)$/i, '');
  const type = routeToApiPrefix(path);
  const store = usePageStore(type);

  const isEdit = params.get('id') != null;
  const addFields = store((s) => s.addFields);
  const editFields = store((s) => s.editFields);
  const formLoading = store((s) => s.formLoading);

  const [detailLoading, setDetailLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const fields = isEdit && editFields.length ? editFields : addFields;
  // 过滤主键与只读字段（编辑时主键已随详情 setFieldsValue 保留）
  const metas = useMemo(
    () => fields.map((f) => toFieldMeta(f.field)).filter((f) => !f.primaryKey && !f.readOnly),
    [fields],
  );

  // 分组：字段带 Category 才按分类 Tabs 分组，否则平铺单页（字段少的页面一屏展示）
  const groups = useMemo(() => {
    if (!hasCategory(metas)) return null;
    return groupByCategory(metas);
  }, [metas]);

  // 加载字段 + 详情
  useEffect(() => {
    let cancelled = false;
    store
      .getState()
      .loadFields()
      .then(async () => {
        if (cancelled) return;
        if (isEdit) {
          const id = params.get('id')!;
          setDetailLoading(true);
          try {
            const detail = await store.getState().getDetail<Record<string, unknown>>(id);
            if (!cancelled) {
              // 布尔串转布尔
              const normalized = Object.fromEntries(
                Object.entries(detail).map(([k, v]) => [k, v === 'true' ? true : v === 'false' ? false : v]),
              );
              form.setFieldsValue(normalized);
            }
          } catch {
            message.error('加载详情失败');
          } finally {
            if (!cancelled) setDetailLoading(false);
          }
        }
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [type, isEdit, params.get('id')]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const model = serializeSubmitModel(values, metas);
      setSubmitting(true);
      if (isEdit) {
        await store.getState().update(model);
      } else {
        await store.getState().add(model);
      }
      message.success(isEdit ? '更新成功' : '新增成功');
      navigate(-1);
    } catch (err) {
      if ((err as Error)?.message && !(err as Error)?.message.includes('validateFields')) {
        message.error((err as Error).message);
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card
      className="cube-entity-card"
      title={
        <Space>
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)} />
          <span>{title || (isEdit ? '编辑' : '新增')}</span>
        </Space>
      }
      size="small"
    >
      <Spin spinning={detailLoading || formLoading}>
        <Form form={form} layout="vertical" requiredMark={false} style={{ maxWidth: 900 }}>
          {groups ? (
            <Tabs
              items={groups.map((g) => ({
                key: g.category,
                label: g.category,
                // forceRender：非激活分组表单项也注册进 Form，跨 Tab 必填校验生效
                forceRender: true,
                children: (
                  <Row gutter={16} className="cube-form-grid">
                    {g.fields.map((meta: FieldMeta) => {
                      const control = resolveControl(meta);
                      const span = isFullWidthControl(control) ? 24 : 12;
                      return (
                        <Col key={meta.name} span={span}>
                          <Form.Item
                            name={meta.name}
                            label={meta.displayName || meta.name}
                            rules={[
                              ...(meta.required ? [{ required: true, message: `请输入${meta.displayName || meta.name}` }] : []),
                              ...(meta.itemType === 'mail' ? [{ type: 'email' as const, message: '邮箱格式不正确' }] : []),
                              ...(meta.itemType === 'mobile' ? [{ pattern: /^1\d{10}$/, message: '手机号格式不正确' }] : []),
                            ]}
                          >
                            <FieldControl field={meta} apiPrefix={type} />
                          </Form.Item>
                        </Col>
                      );
                    })}
                  </Row>
                ),
              }))}
            />
          ) : (
            <Row gutter={16} className="cube-form-grid">
              {metas.map((meta) => {
                const control = resolveControl(meta);
                const span = isFullWidthControl(control) ? 24 : 12;
                return (
                  <Col key={meta.name} span={span}>
                    <Form.Item
                      name={meta.name}
                      label={meta.displayName || meta.name}
                      rules={[
                        ...(meta.required ? [{ required: true, message: `请输入${meta.displayName || meta.name}` }] : []),
                        ...(meta.itemType === 'mail' ? [{ type: 'email' as const, message: '邮箱格式不正确' }] : []),
                        ...(meta.itemType === 'mobile' ? [{ pattern: /^1\d{10}$/, message: '手机号格式不正确' }] : []),
                      ]}
                    >
                      <FieldControl field={meta} apiPrefix={type} />
                    </Form.Item>
                  </Col>
                );
              })}
            </Row>
          )}
          <div className="cube-form-actions">
            <Button type="primary" loading={submitting} onClick={() => void handleSubmit()}>
              保存
            </Button>
            <Button onClick={() => navigate(-1)}>返回</Button>
          </div>
        </Form>
      </Spin>
    </Card>
  );
}
