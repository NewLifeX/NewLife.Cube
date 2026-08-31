/**
 * 数据库管理页（/Admin/Db）
 *
 * 数据库控制器（DbController）是特殊控制器，非实体 CRUD，后端只暴露：
 * - GET  /api/Admin/Db                → 数据库连接列表（Name/Type/ConnStr/Version/Dynamic/Backups）
 * - POST /api/Admin/Db/Backup?name=    → 备份数据库
 * - POST /api/Admin/Db/BackupAndCompress?name= → 备份并压缩
 * - GET  /api/Admin/Db/Download?name=  → 下载数据库架构 XML
 */
import { useCallback, useEffect, useState } from 'react';
import { App, Button, Card, Popconfirm, Space, Spin, Table, Tag, Tooltip } from 'antd';
import { CloudDownloadOutlined, ReloadOutlined } from '@ant-design/icons';
import { api } from '@/api';

export interface DbPageProps {
  /** 实体路径前缀，如 '/Admin/Db' */
  type: string;
}

interface DbItem {
  name: string;
  type: string;
  connStr?: string;
  version?: string;
  dynamic?: boolean;
  backups?: number;
}

/** 连接字符串中需隐藏的密码键（对齐 MVC ProtectedKey.Names=["password","pass","pwd"]） */
const CONN_SECRET_NAMES = ['password', 'pass', 'pwd'];

/**
 * 隐藏连接字符串中的密码（对齐 MVC ProtectedKey.Hide：password/pass/pwd 键的值替换为 {***}）。
 *
 * @param connStr 连接字符串（如 `Data Source=..;Password=123;...`）
 * @returns 隐藏密码后的连接字符串
 */
export function hideConnSecret(connStr: string): string {
  return connStr
    .split(';')
    .map((pair) => {
      const eq = pair.indexOf('=');
      if (eq <= 0) return pair;
      const key = pair.slice(0, eq).trim().toLowerCase();
      if (CONN_SECRET_NAMES.includes(key)) return `${pair.slice(0, eq)}={***}`;
      return pair;
    })
    .join(';');
}

/** 数据库类型转标签色（常见类型配色，未知走默认） */
function typeColor(type: string): string {
  const t = String(type || '').toLowerCase();
  if (t.includes('sqlite')) return 'green';
  if (t.includes('mysql') || t.includes('mariadb')) return 'blue';
  if (t.includes('sqlserver') || t.includes('mssql')) return 'red';
  if (t.includes('postgre') || t.includes('pgsql')) return 'cyan';
  if (t.includes('oracle')) return 'orange';
  if (t.includes('mongodb')) return 'purple';
  return 'default';
}

export default function DbPage({ type }: DbPageProps) {
  const { message } = App.useApp();
  const [rows, setRows] = useState<DbItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [acting, setActing] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.client.get(type);
      const data = (res.data?.data ?? []) as DbItem[];
      setRows(Array.isArray(data) ? data : []);
    } catch {
      message.error('加载数据库列表失败');
    } finally {
      setLoading(false);
    }
  }, [type]);

  useEffect(() => {
    void load();
  }, [load]);

  /** 备份 / 备份并压缩 */
  const handleBackup = async (name: string, compress: boolean) => {
    const key = `${name}_${compress ? 'compress' : 'backup'}`;
    setActing(key);
    try {
      await api.client.post(`${type}/BackupAndCompress`, null, { params: { name } });
      message.success(`数据库 ${name} 备份成功`);
      void load();
    } catch {
      message.error(`数据库 ${name} 备份失败`);
    } finally {
      setActing('');
    }
  };

  /** 下载架构 XML */
  const handleDownload = async (name: string) => {
    try {
      const res = await api.client.get(`${type}/Download`, { params: { name }, responseType: 'blob' });
      const blob = res.data as Blob;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${name}.xml`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      message.error('下载失败');
    }
  };

  return (
    // 页面名由顶栏面包屑/多标签承担，Card 不再重复标题
    <Card
      size="small"
      extra={
        <Button icon={<ReloadOutlined />} onClick={() => void load()} loading={loading}>
          刷新
        </Button>
      }
    >
      <Spin spinning={loading}>
        <Table<DbItem>
          rowKey={(r) => r.name}
          dataSource={rows}
          size="medium"
          pagination={false}
          locale={{ emptyText: '暂无数据库连接' }}
          columns={[
            { title: '连接名', dataIndex: 'name', width: 180 },
            {
              title: '数据库类型',
              dataIndex: 'type',
              width: 160,
              render: (v: string) => <Tag color={typeColor(v)}>{v}</Tag>,
            },
            { title: '版本', dataIndex: 'version', width: 200, render: (v?: string) => v || '-' },
            {
              title: '备份数',
              dataIndex: 'backups',
              width: 90,
              render: (v?: number) => (v ?? 0) > 0 ? v : 0,
            },
            {
              title: '连接字符串',
              dataIndex: 'connStr',
              width: 420,
              ellipsis: true,
              // 对齐 MVC Index.cshtml：连接串长省略，悬浮 title 显示完整（隐藏密码后的值，避免泄露）
              render: (v?: string) => {
                if (!v) return '-';
                const hidden = hideConnSecret(v);
                return (
                  <Tooltip title={hidden} placement="topLeft">
                    <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{hidden}</span>
                  </Tooltip>
                );
              },
            },
            {
              title: '操作',
              key: '__ops',
              width: 280,
              render: (_, row) => (
                <Space size={4}>
                  <Button
                    size="small"
                    type="primary"
                    loading={acting === `${row.name}_backup`}
                    disabled={!!acting}
                    onClick={() => void handleBackup(row.name, false)}
                  >
                    备份
                  </Button>
                  <Popconfirm
                    title="备份并压缩数据库？"
                    description="将生成 zip 压缩备份文件，耗时较长"
                    onConfirm={() => void handleBackup(row.name, true)}
                  >
                    <Button
                      size="small"
                      loading={acting === `${row.name}_compress`}
                      disabled={!!acting}
                    >
                      备份并压缩
                    </Button>
                  </Popconfirm>
                  <Button size="small" icon={<CloudDownloadOutlined />} onClick={() => void handleDownload(row.name)}>
                    下载架构
                  </Button>
                </Space>
              ),
            },
          ]}
        />
      </Spin>
    </Card>
  );
}
