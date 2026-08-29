/**
 * 通用配置页（魔方设置等 ConfigController<T> 页面）
 *
 * 魔方后台的配置控制器（如 /Admin/Cube 魔方设置、/Admin/Sys 系统设置、/Admin/XCode XCode设置）
 * 与实体控制器不同，后端只暴露：
 * - GET  `{type}`             → 返回配置对象（单对象，非列表）
 * - PUT  `{type}`             → 保存配置对象
 * - GET  `{type}/GetFields`   → 返回配置字段元数据（按 [Category] 分组）
 *
 * 本组件通用渲染任意配置控制器页面：按分类 Tabs 分组渲染字段表单，保存时 PUT 提交。
 */
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Form, Row, Spin, Tabs, message } from 'antd';
import type { DataField } from '@cube/api-core';
import FieldControl from '@/components/field/FieldControl';
import { resolveControl, isFullWidthControl, serializeSubmitModel } from '@/utils/fieldControl';
import { toFieldMeta, type FieldMeta } from '@/types/field';
import { api } from '@/api';

export interface ConfigPageProps {
  /** 实体路径前缀，如 '/Admin/Cube' */
  type: string;
  /** 页面标题 */
  title?: string;
}

/** 布尔串转布尔（后端可能返回 'true'/'false' 字符串） */
function normalizeValue(v: unknown): unknown {
  if (v === 'true') return true;
  if (v === 'false') return false;
  return v;
}

/** 按分类分组字段（未分类归为「常规设置」） */
function groupByCategory(fields: FieldMeta[]): { category: string; fields: FieldMeta[] }[] {
  const map = new Map<string, FieldMeta[]>();
  for (const f of fields) {
    const cat = f.category || '常规设置';
    if (!map.has(cat)) map.set(cat, []);
    map.get(cat)!.push(f);
  }
  return [...map.entries()].map(([category, list]) => ({ category, fields: list }));
}

export default function ConfigPage({ type, title }: ConfigPageProps) {
  const [form] = Form.useForm();
  const [fields, setFields] = useState<FieldMeta[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // 加载字段元数据 + 当前配置值
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    Promise.all([
      // ConfigController.GetFields 返回全部成员（kind 参数被忽略）
      api.page.getFields(type, 4).catch(() => null),
      // GET {type} 返回配置对象（非列表）
      api.client.get(type).catch(() => null),
    ])
      .then(([fieldsRes, objRes]) => {
        if (cancelled) return;
        const fieldList = (fieldsRes?.data ?? []) as DataField[];
        if (!fieldList.length) {
          setError(`无法获取配置字段，请确认 ${type} 为配置控制器（ConfigController）`);
          return;
        }
        // 过滤只读字段（主键/只读不参与编辑）
        const metas = fieldList
          .map((f) => toFieldMeta(f))
          .filter((f) => !f.primaryKey && !f.readOnly);
        setFields(metas);

        // 回填当前值（布尔串转布尔）
        const values = (objRes?.data?.data ?? objRes?.data ?? {}) as Record<string, unknown>;
        const normalized = Object.fromEntries(
          Object.entries(values).map(([k, v]) => [k, normalizeValue(v)]),
        );
        form.setFieldsValue(normalized);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [type]);

  const groups = useMemo(() => groupByCategory(fields), [fields]);

  const handleSave = useCallback(async () => {
    try {
      const values = await form.validateFields();
      const model = serializeSubmitModel(values, fields);
      setSaving(true);
      await api.client.put(type, model);
      message.success('保存成功');
    } catch (err) {
      if ((err as Error)?.message && !(err as Error)?.message.includes('validateFields')) {
        message.error((err as Error).message);
      }
    } finally {
      setSaving(false);
    }
  }, [form, fields, type]);

  if (error) {
    return (
      <Card title={title || '配置'} size="small">
        <div style={{ color: 'var(--cube-danger)', padding: 24, textAlign: 'center' }}>{error}</div>
      </Card>
    );
  }

  return (
    <Card
      title={title || '配置'}
      size="small"
      extra={
        <Button type="primary" loading={saving} onClick={() => void handleSave()}>
          保存设置
        </Button>
      }
    >
      <Spin spinning={loading}>
        <Form form={form} layout="vertical" requiredMark={false}>
          <Tabs
            items={groups.map((g) => ({
              key: g.category,
              label: g.category,
              children: (
                <Row gutter={16}>
                  {g.fields.map((meta) => {
                    const control = resolveControl(meta);
                    const span = isFullWidthControl(control) ? 24 : 12;
                    return (
                      <Col key={meta.name} span={span}>
                        <Form.Item
                          name={meta.name}
                          label={meta.displayName || meta.name}
                          extra={meta.description}
                          rules={meta.required ? [{ required: true, message: `请输入${meta.displayName || meta.name}` }] : []}
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
          <div style={{ marginTop: 8 }}>
            <Button type="primary" loading={saving} onClick={() => void handleSave()}>
              保存设置
            </Button>
          </div>
        </Form>
      </Spin>
    </Card>
  );
}
