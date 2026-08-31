/**
 * 表格列设置（齿轮按钮 + 弹层）
 *
 * 勾选显示/隐藏列、上下调整顺序、恢复默认，持久化到后端（Parameter 表 Page-React 分类，用户级）。
 * 渲染由后端 GetPage 干预：保存后重新加载页面元数据，GetPage 返回已按配置排序/过滤的字段。
 */
import { useEffect, useState } from 'react';
import { App, Button, Checkbox, Popover, Tooltip } from 'antd';
import { ArrowDownOutlined, ArrowUpOutlined, SettingOutlined } from '@ant-design/icons';
import { api } from '@/api';
import { toFieldMeta } from '@/types/field';
import type { FieldMapping } from '@newlifex/field-mapping';

export interface ColumnSettingProps {
  /** 页面路径，如 /Cube/Area */
  type: string;
  /** 全部可用字段（GetPage.allList，应用配置前） */
  allFields: FieldMapping[];
  /** 当前可见字段名列表（GetPage.list 中 visible 未隐藏的） */
  visibleFields: string[];
  /** 保存/恢复默认后回调（重新加载页面元数据） */
  onChanged: () => void;
}

/** 列配置持久化 kind（对应后端 PageService 的 Page-React 分类） */
export const COLUMN_CONFIG_KIND = 'React';

/** 过滤可见字段：visible !== false 的字段参与渲染（后端 GetPage 按用户配置标记隐藏） */
export function filterVisibleFields(fields: FieldMapping[]): FieldMapping[] {
  return fields.filter((f) => f.field.visible !== false);
}

export default function ColumnSetting({ type, allFields, visibleFields, onChanged }: ColumnSettingProps) {
  const { message } = App.useApp();
  const [open, setOpen] = useState(false);
  const [order, setOrder] = useState<string[]>([]);
  const [hidden, setHidden] = useState<string[]>([]);

  // 打开面板时初始化本地状态：顺序 = 全量字段顺序；隐藏 = 全量 - 当前可见
  useEffect(() => {
    if (open) {
      setOrder(allFields.map((f) => f.field.name));
      const vis = new Set(visibleFields);
      setHidden(allFields.map((f) => f.field.name).filter((n) => !vis.has(n)));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const list = order
    .map((name) => {
      const m = allFields.find((f) => f.field.name === name);
      return { name, meta: m ? toFieldMeta(m.field) : null };
    })
    .filter((x) => x.meta !== null);

  const toggle = (name: string) => {
    setHidden((prev) => (prev.includes(name) ? prev.filter((n) => n !== name) : [...prev, name]));
  };

  const move = (name: string, dir: -1 | 1) => {
    setOrder((prev) => {
      const i = prev.indexOf(name);
      const j = i + dir;
      if (i < 0 || j < 0 || j >= prev.length) return prev;
      const next = [...prev];
      [next[i], next[j]] = [next[j], next[i]];
      return next;
    });
  };

  const persist = async (payload: Record<string, unknown>) => {
    try {
      await api.config.savePageSetting(COLUMN_CONFIG_KIND, type, payload);
      message.success('列设置已保存');
      setOpen(false);
      onChanged();
    } catch {
      message.error('保存列设置失败');
    }
  };

  const handleSave = () => void persist({ listOrder: order, listHidden: hidden });
  const handleReset = () => void persist({ listOrder: [], listHidden: [] });

  const content = (
    <div style={{ width: 300 }}>
      <div style={{ marginBottom: 8, fontWeight: 600 }}>列设置</div>
      {list.length === 0 && <div style={{ color: '#999' }}>无可用字段</div>}
      {list.map(({ name, meta }, idx) => (
        <div key={name} style={{ display: 'flex', alignItems: 'center', gap: 4, padding: '2px 0' }}>
          <Checkbox
            checked={!hidden.includes(name)}
            onChange={() => toggle(name)}
            style={{ flex: 1, minWidth: 0 }}
          >
            <span
              style={{ display: 'inline-block', maxWidth: 150, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', verticalAlign: 'middle' }}
              title={meta!.displayName || name}
            >
              {meta!.displayName || name}
            </span>
          </Checkbox>
          <Tooltip title="上移">
            <Button type="text" size="small" icon={<ArrowUpOutlined />} disabled={idx === 0} onClick={() => move(name, -1)} />
          </Tooltip>
          <Tooltip title="下移">
            <Button
              type="text"
              size="small"
              icon={<ArrowDownOutlined />}
              disabled={idx === list.length - 1}
              onClick={() => move(name, 1)}
            />
          </Tooltip>
        </div>
      ))}
      <div style={{ marginTop: 8, display: 'flex', justifyContent: 'space-between' }}>
        <Button size="small" onClick={handleReset}>
          恢复默认
        </Button>
        <Button type="primary" size="small" onClick={handleSave}>
          保存
        </Button>
      </div>
    </div>
  );

  return (
    <Popover content={content} trigger="click" open={open} onOpenChange={setOpen} placement="bottomRight">
      <Tooltip title="列设置">
        <Button aria-label="列设置" type="text" icon={<SettingOutlined />} />
      </Tooltip>
    </Popover>
  );
}
