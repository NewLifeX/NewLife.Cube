/**
 * 工作台部件管理页（/Cube/Widget）
 *
 * 对齐 MVC WidgetController + Index.cshtml：
 * - 分组显示顺序（逗号分隔组名，全局配置存 Parameter 表）
 * - 组内默认顺序（每组一行逗号分隔部件名）
 * - 部件列表表格（名称/标题/图标/宽度/分类/可见性/状态/启用禁用）
 *
 * 后端仅系统管理员可访问（CheckAdmin），接口：
 * - GET  /Cube/Widget                     → 部件列表 + 分组配置
 * - POST /Cube/Widget/Enable?name=&enable=  → 启用/禁用
 * - POST /Cube/Widget/SaveGroupOrder?groups= → 保存组顺序
 * - POST /Cube/Widget/SaveGroupItemOrder?group=&names= → 保存组内顺序
 */
import { useCallback, useEffect, useState } from 'react';
import { App, Button, Card, Input, Space, Spin, Table, Tag } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { api } from '@/api';

export interface WidgetPageProps {
  /** 实体路径前缀，如 '/Cube/Widget' */
  type: string;
}

export interface WidgetItem {
  name: string;
  title: string;
  icon?: string;
  cols?: number;
  category?: string;
  permission?: string;
  adminOnly?: boolean;
  sort?: number;
  enable: boolean;
}

/** 可见性文本（对齐 MVC Index.cshtml：仅管理员 / 所有用户 / 权限列表） */
export function visibleText(w: WidgetItem): string {
  if (w.adminOnly) return '仅管理员';
  return w.permission ? w.permission : '所有用户';
}

export default function WidgetPage({ type }: WidgetPageProps) {
  const { message } = App.useApp();
  const [rows, setRows] = useState<WidgetItem[]>([]);
  const [groupOrder, setGroupOrder] = useState('');
  const [groupItems, setGroupItems] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [acting, setActing] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.client.get(type);
      const data = res.data?.data ?? {};
      setRows(Array.isArray(data.widgets) ? data.widgets : []);
      setGroupOrder(Array.isArray(data.groupOrder) ? data.groupOrder.join(',') : String(data.groupOrder ?? ''));
      const items: Record<string, string> = {};
      for (const [g, names] of Object.entries(data.groupItems ?? {})) {
        items[g] = Array.isArray(names) ? (names as string[]).join(',') : String(names);
      }
      setGroupItems(items);
    } catch {
      message.error('加载部件列表失败');
    } finally {
      setLoading(false);
    }
  }, [type, message]);

  useEffect(() => {
    void load();
  }, [load]);

  /** 启用 / 禁用部件 */
  const handleToggle = async (name: string, enable: boolean) => {
    setActing(`${name}_${enable}`);
    try {
      await api.client.post(`${type}/Enable`, null, { params: { name, enable } });
      message.success(`${name} ${enable ? '已启用' : '已禁用'}`);
      void load();
    } catch {
      message.error('操作失败');
    } finally {
      setActing('');
    }
  };

  /** 保存组顺序 */
  const handleSaveGroupOrder = async () => {
    try {
      await api.client.post(`${type}/SaveGroupOrder`, null, { params: { groups: groupOrder } });
      message.success('组顺序已保存');
      void load();
    } catch {
      message.error('保存组顺序失败');
    }
  };

  /** 保存所有组内顺序（逐组提交，全部完成后再刷新） */
  const handleSaveGroupItems = async () => {
    setActing('__group_items__');
    try {
      const posts = Object.entries(groupItems).map(([g, names]) =>
        api.client.post(`${type}/SaveGroupItemOrder`, null, { params: { group: g, names } }),
      );
      await Promise.all(posts);
      message.success('组内顺序已保存');
      void load();
    } catch {
      message.error('保存组内顺序失败');
    } finally {
      setActing('');
    }
  };

  return (
    <Card
      size="small"
      extra={
        <Button icon={<ReloadOutlined />} onClick={() => void load()} loading={loading}>
          刷新
        </Button>
      }
    >
      <Spin spinning={loading}>
        <Space direction="vertical" style={{ width: '100%' }} size={16}>
          {/* 分组显示顺序（对齐 MVC Index.cshtml 面板） */}
          <Card size="small" type="inner" title="分组显示顺序" extra={<small>同组卡片默认聚合显示；未列出的组排到最后并按名称排序</small>}>
            <Space.Compact style={{ width: '100%' }}>
              <Input
                placeholder="系统,个人,通用"
                value={groupOrder}
                onChange={(e) => setGroupOrder(e.target.value)}
                style={{ fontFamily: 'monospace' }}
              />
              <Button type="primary" onClick={() => void handleSaveGroupOrder()} loading={acting === '__group_order__'}>
                保存
              </Button>
            </Space.Compact>
          </Card>

          {/* 组内默认顺序（对齐 MVC Index.cshtml 面板） */}
          {Object.keys(groupItems).length > 0 && (
            <Card size="small" type="inner" title="组内默认顺序" extra={<small>每组一行，逗号分隔部件名；新部件自动排到组内末尾</small>}>
              <Space direction="vertical" style={{ width: '100%' }} size={8}>
                {Object.entries(groupItems).map(([g, names]) => (
                  <div key={g}>
                    <div style={{ fontWeight: 600, marginBottom: 2 }}>{g}</div>
                    <Input
                      value={names}
                      onChange={(e) => setGroupItems((prev) => ({ ...prev, [g]: e.target.value }))}
                      style={{ fontFamily: 'monospace' }}
                    />
                  </div>
                ))}
                <Button type="primary" onClick={() => void handleSaveGroupItems()} loading={acting === '__group_items__'}>
                  保存组内顺序
                </Button>
              </Space>
            </Card>
          )}

          {/* 部件列表（对齐 MVC 表格列） */}
          <Table<WidgetItem>
            rowKey={(r) => r.name}
            dataSource={rows}
            size="medium"
            pagination={false}
            locale={{ emptyText: '暂无部件' }}
            columns={[
              { title: '名称', dataIndex: 'name', width: 160 },
              { title: '标题', dataIndex: 'title', width: 200 },
              {
                title: '图标',
                dataIndex: 'icon',
                width: 160,
                render: (v?: string) => (v ? <span style={{ fontFamily: 'monospace' }}>{v}</span> : '-'),
              },
              { title: '宽度', dataIndex: 'cols', width: 80, align: 'center', render: (v?: number) => v ?? 6 },
              {
                title: '分类',
                dataIndex: 'category',
                width: 120,
                render: (v?: string) => (v ? <Tag>{v}</Tag> : '-'),
              },
              { title: '可见性', key: 'visible', width: 140, render: (_, w) => visibleText(w) },
              {
                title: '状态',
                key: 'enable',
                width: 100,
                align: 'center',
                render: (_, w) => (w.enable ? '✅ 启用' : '⛔ 禁用'),
              },
              {
                title: '操作',
                key: '__ops',
                width: 100,
                align: 'center',
                render: (_, w) => (
                  <Button
                    size="small"
                    type={w.enable ? 'default' : 'primary'}
                    danger={w.enable}
                    loading={acting === `${w.name}_${!w.enable}`}
                    onClick={() => void handleToggle(w.name, !w.enable)}
                  >
                    {w.enable ? '禁用' : '启用'}
                  </Button>
                ),
              },
            ]}
          />
        </Space>
      </Spin>
    </Card>
  );
}
