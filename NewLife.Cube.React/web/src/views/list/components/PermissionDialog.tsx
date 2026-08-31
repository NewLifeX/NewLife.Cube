/**
 * 角色权限配置弹窗（对齐 MVC 角色编辑页的「授权」树形表格）
 *
 * 后端 /Admin/Role/PermissionTree 返回菜单树（每项含可用权限位 flags 与当前角色已授权位 granted），
 * 前端树形 Table 逐行勾选 查看/添加/修改/删除（对齐 MVC 授权表格），并支持快捷 只读/全部/无权限。
 * 保存时收集为 "id#flag,id#flag" 权限字符串（与后端 Role.LoadPermission 解析格式一致）提交保存。
 */
import { useEffect, useMemo, useState } from 'react';
import { App, Checkbox, Modal, Space, Table, Typography } from 'antd';
import { api } from '@/api';

/** 权限树节点（与后端 PermissionTree 返回结构对应） */
export interface PermissionNode {
  id: number;
  name: string;
  displayName: string;
  /** 该菜单可用权限位（PermissionFlags：1 查看/2 添加/4 修改/8 删除） */
  flags?: Array<{ value: number; name: string }>;
  /** 当前角色已授权位 */
  granted?: number;
  children?: PermissionNode[];
}

interface PermissionDialogProps {
  open: boolean;
  roleId?: number;
  roleName?: string;
  onClose: () => void;
  onSaved?: () => void;
}

/** 权限位列（对齐 MVC：查看/添加/修改/删除） */
const FLAG_COLS = [
  { flag: 1, key: 'detail', label: '查看' },
  { flag: 2, key: 'insert', label: '添加' },
  { flag: 4, key: 'update', label: '修改' },
  { flag: 8, key: 'delete', label: '删除' },
];

export default function PermissionDialog({ open, roleId, roleName, onClose, onSaved }: PermissionDialogProps) {
  const { message } = App.useApp();
  const [tree, setTree] = useState<PermissionNode[]>([]);
  const [grantedMap, setGrantedMap] = useState<Record<number, number>>({});
  /** 展开键（受控）。defaultExpandAllRows 对异步数据无效，数据加载后收集全部含子节点的行 */
  const [expandedKeys, setExpandedKeys] = useState<React.Key[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  // 打开时加载权限树
  useEffect(() => {
    if (!open || !roleId) return;
    setLoading(true);
    api.client
      .get('/Admin/Role/PermissionTree', { params: { roleId } })
      .then((res) => {
        // api.client 为原始 axios（不解包），后端返回 { code, data: { role, tree } }
        const body = res.data as { data?: { tree?: PermissionNode[] } };
        const data = body?.data?.tree ?? [];
        setTree(data);
        // 扁平化收集已授权位 + 所有含子节点的展开键
        const map: Record<number, number> = {};
        const keys: React.Key[] = [];
        const walk = (nodes: PermissionNode[]) => {
          for (const n of nodes) {
            if (n.granted) map[n.id] = n.granted;
            if (n.children?.length) {
              keys.push(n.id);
              walk(n.children);
            }
          }
        };
        walk(data);
        setGrantedMap(map);
        setExpandedKeys(keys);
      })
      .catch(() => message.error('加载权限树失败'))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, roleId]);

  /** 勾选/取消单个权限位 */
  const toggle = (id: number, flag: number) => {
    setGrantedMap((prev) => {
      const cur = prev[id] ?? 0;
      const next = cur & flag ? cur & ~flag : cur | flag;
      const copy = { ...prev };
      if (next) copy[id] = next;
      else delete copy[id];
      return copy;
    });
  };

  /** 快捷设置：只读=查看(1)，全部=该菜单可用权限位并集，无权限=0 */
  const setQuick = (id: number, flags: number) => {
    setGrantedMap((prev) => {
      const copy = { ...prev };
      if (flags) copy[id] = flags;
      else delete copy[id];
      return copy;
    });
  };

  const allFlags = (row: PermissionNode) => row.flags?.reduce((acc, f) => acc | f.value, 0) ?? 0;

  const handleSave = async () => {
    if (!roleId) return;
    setSaving(true);
    try {
      const parts = Object.entries(grantedMap)
        .filter(([, f]) => f)
        .map(([id, f]) => `${id}#${f}`)
        .sort((a, b) => parseInt(a, 10) - parseInt(b, 10));
      await api.client.post('/Admin/Role/SavePermission', { roleId, permission: parts.join(',') });
      message.success('权限保存成功');
      onSaved?.();
      onClose();
    } catch {
      message.error('权限保存失败');
    } finally {
      setSaving(false);
    }
  };

  const columns = useMemo(() => {
    const renderFlag = (flag: number, label: string) => ({
      title: label,
      key: `f${flag}`,
      width: 60,
      align: 'center' as const,
      render: (_: unknown, row: PermissionNode) => {
        // 无该权限位的菜单显示占位，不可勾选
        if (!row.flags?.some((f) => f.value === flag)) return <span className="cube-perm-na">-</span>;
        return <Checkbox checked={((grantedMap[row.id] ?? 0) & flag) === flag} onChange={() => toggle(row.id, flag)} />;
      },
    });

    return [
      {
        title: '菜单',
        key: 'menu',
        render: (_: unknown, row: PermissionNode) => (
          <Space size={8}>
            <Typography.Text strong>{row.displayName || row.name}</Typography.Text>
            {row.displayName && <Typography.Text type="secondary">{row.name}</Typography.Text>}
          </Space>
        ),
      },
      ...FLAG_COLS.map((c) => renderFlag(c.flag, c.label)),
      {
        title: '快捷',
        key: 'quick',
        width: 150,
        render: (_: unknown, row: PermissionNode) => (
          <Space size={6}>
            <a onClick={() => setQuick(row.id, 1)}>只读</a>
            <a onClick={() => setQuick(row.id, allFlags(row))}>全部</a>
            <a onClick={() => setQuick(row.id, 0)}>无权限</a>
          </Space>
        ),
      },
    ];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [grantedMap, tree]);

  return (
    <Modal
      open={open}
      title={`角色权限${roleName ? ` - ${roleName}` : ''}`}
      width={760}
      onCancel={onClose}
      onOk={handleSave}
      confirmLoading={saving}
      okText="保存"
      cancelText="取消"
      destroyOnClose
    >
      <Table<PermissionNode>
        rowKey="id"
        columns={columns}
        dataSource={tree}
        loading={loading}
        size="small"
        pagination={false}
        expandable={{ expandedRowKeys: expandedKeys, onExpandedRowsChange: (keys) => setExpandedKeys(keys as React.Key[]) }}
      />
    </Modal>
  );
}
