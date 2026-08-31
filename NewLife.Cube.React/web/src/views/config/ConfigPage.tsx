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
 *
 * 字段布局（对齐 MVC _Form_Item）：每行一个配置项——左侧标签 + 中间控件 + 右侧 Description，
 * 保证配置说明始终可见。
 */
import { useCallback, useEffect, useMemo, useState } from 'react';
import { App, Button, Card, Form, Spin, Tabs } from 'antd';
import type { DataField } from '@newlifex/api-core';
import FieldControl from '@/components/field/FieldControl';
import { groupByCategory, isFullWidthControl, resolveControl, serializeSubmitModel } from '@/utils/fieldControl';
import { toFieldMeta, type FieldMeta } from '@/types/field';
import { getValueByKey } from '@/utils/url';
import { api } from '@/api';
import { useAiFillForm } from '@/hooks/useAiFillForm';

export interface ConfigPageProps {
  /** 实体路径前缀，如 '/Admin/Cube' */
  type: string;
}

/** 布尔串转布尔（后端可能返回 'true'/'false' 字符串） */
function normalizeValue(v: unknown): unknown {
  if (v === 'true') return true;
  if (v === 'false') return false;
  return v;
}

export default function ConfigPage({ type }: ConfigPageProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  // AI 填表：监听 AiAssistant 派发的 cube:ai-fill-form 事件（配置页也是表单页）
  useAiFillForm(form);
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

        // 回填当前值（布尔串转布尔；配置对象键与字段名大小写可能不一致，按字段名大小写不敏感取值）
        const values = (objRes?.data?.data ?? objRes?.data ?? {}) as Record<string, unknown>;
        const normalized: Record<string, unknown> = {};
        for (const f of metas) {
          const v = getValueByKey(values, f.name);
          if (v !== undefined) normalized[f.name] = normalizeValue(v);
        }
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
      // React 皮肤设置：保存后刷新页面，让全局配置（表单风格/导航排开等）立即生效
      if (type.toLowerCase().endsWith('/react')) {
        setTimeout(() => window.location.reload(), 600);
      }
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
      <Card size="small">
        <div style={{ color: 'var(--cube-danger)', padding: 24, textAlign: 'center' }}>{error}</div>
      </Card>
    );
  }

  return (
    // 页面名由顶栏面包屑/多标签承担，Card 不再重复标题
    <Card
      size="small"
      extra={
        <Button type="primary" loading={saving} onClick={() => void handleSave()}>
          保存设置
        </Button>
      }
    >
      <Spin spinning={loading}>
        <Form form={form} layout="horizontal" requiredMark={false}>
          <Tabs
            items={groups.map((g) => ({
              key: g.category,
              label: g.category,
              children: (
                <div className="cube-config-fields">
                  {g.fields.map((meta) => {
                    // 每行一个配置项：标签 + 控件 + 右侧 Description
                    const full = isFullWidthControl(resolveControl(meta));
                    return (
                      <div className="cube-config-row" key={meta.name}>
                        <Form.Item
                          name={meta.name}
                          label={meta.displayName || meta.name}
                          labelCol={{ flex: '0 0 200px' }}
                          // 控件列固定 340px 不收缩（0 0），描述列吸收剩余空间 → 各条配置描述左对齐，
                          // 文本框/数字框等控件均占满该列，视觉等宽（对齐 MVC _Form_Item 栅格）
                          wrapperCol={{ flex: full ? '1 1 auto' : '0 0 340px' }}
                          rules={meta.required ? [{ required: true, message: `请输入${meta.displayName || meta.name}` }] : []}
                          // 非整行控件固定基础宽度（标签 200 + 控件 340，对齐 MVC _Form_Item 栅格比例），
                          // 保证「标签 | 控件 | 描述」同行；整行控件占满中间，由描述列吸收剩余空间
                          style={{ flex: full ? '1 1 auto' : '0 0 548px', minWidth: 0, marginBottom: 0 }}
                        >
                          <FieldControl field={meta} apiPrefix={type} />
                        </Form.Item>
                        {meta.description && (
                          <div className="cube-config-desc" title={meta.description}>
                            {meta.description}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
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
