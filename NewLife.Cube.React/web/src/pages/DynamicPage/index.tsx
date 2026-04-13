import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ActionType, ProColumns } from '@ant-design/pro-components';
import {
  ModalForm,
  PageContainer,
  ProForm,
  ProFormText,
  ProFormDigit,
  ProFormSwitch,
  ProFormDateTimePicker,
  ProFormSelect,
  ProFormTextArea,
  ProTable,
} from '@ant-design/pro-components';
import { Button, Descriptions, Drawer, Dropdown, message, Modal, Popconfirm, Space, Upload } from 'antd';
import {
  PlusOutlined,
  DeleteOutlined,
  DownloadOutlined,
  UploadOutlined,
  EyeOutlined,
  EditOutlined,
} from '@ant-design/icons';
import { useLocation } from '@umijs/max';
import * as echarts from 'echarts';
import {
  ColumnKind,
  getFields,
  queryList,
  getDetail,
  addItem,
  updateItem,
  deleteItem,
  deleteSelect,
  importFile,
  getExportUrl,
  getChartData,
} from '@/services/cube/page';
import IconStatus from '@/components/IconStatus';

/** 将后端字段类型映射为 ProTable valueType */
const mapValueType = (dataType: string): string => {
  switch (dataType) {
    case 'Boolean': return 'switch';
    case 'DateTime': return 'dateTime';
    case 'Int32':
    case 'Int64':
    case 'Double':
    case 'Decimal': return 'digit';
    default: return 'text';
  }
};

/** 将 CubeColumn 转为 ProColumns */
const buildColumns = (
  fields: CubeColumn[],
  onView: (row: any) => void,
  onEdit: (row: any) => void,
  onDel: (id: any) => void,
): ProColumns<Record<string, any>>[] => {
  const cols: ProColumns<Record<string, any>>[] = fields
    .sort((a, b) => a.sort - b.sort)
    .map((f) => {
      const col: ProColumns<Record<string, any>> = {
        dataIndex: toCamelCase(f.name),
        title: f.displayName || f.name,
        width: parseInt(f.width) || undefined,
        hideInTable: !f.showInList,
        hideInSearch: !f.showInSearch,
        sorter: f.showInList,
        ellipsis: true,
        valueType: mapValueType(f.dataType) as any,
      };

      // Boolean 字段特殊渲染
      if (f.dataType === 'Boolean') {
        col.render = (_, row) => <IconStatus status={row[toCamelCase(f.name)]} />;
        col.valueEnum = new Map([
          [true, { text: '是' }],
          [false, { text: '否' }],
        ]);
      }

      // 超链接字段
      if (f.cellUrl) {
        col.render = (_, row) => {
          const url = resolveUrl(f.cellUrl, row);
          return <a href={url}>{row[toCamelCase(f.name)]}</a>;
        };
      }

      // 主键字段隐藏搜索
      if (f.primaryKey) {
        col.hideInSearch = true;
      }

      return col;
    });

  // 操作列
  cols.push({
    title: '操作',
    dataIndex: 'option',
    valueType: 'option',
    width: 160,
    fixed: 'right',
    render: (_, row) => (
      <Space>
        <a key="view" onClick={() => onView(row)}>
          <EyeOutlined /> 查看
        </a>
        <a key="edit" onClick={() => onEdit(row)}>
          <EditOutlined /> 编辑
        </a>
        <Popconfirm key="del" title="确定删除吗？" onConfirm={() => onDel(row.id)}>
          <a style={{ color: '#ff4d4f' }}>
            <DeleteOutlined /> 删除
          </a>
        </Popconfirm>
      </Space>
    ),
  });

  return cols;
};

/** 将 CubeColumn 转为表单字段组件 */
const buildFormFields = (fields: CubeColumn[]) => {
  return fields
    .sort((a, b) => a.sort - b.sort)
    .map((f) => {
      const name = toCamelCase(f.name);
      const label = f.displayName || f.name;
      const rules = f.nullable === false ? [{ required: true, message: `请输入${label}` }] : undefined;

      switch (f.dataType) {
        case 'Boolean':
          return <ProFormSwitch key={name} name={name} label={label} />;
        case 'Int32':
        case 'Int64':
        case 'Double':
        case 'Decimal':
          return <ProFormDigit key={name} name={name} label={label} rules={rules} />;
        case 'DateTime':
          return <ProFormDateTimePicker key={name} name={name} label={label} rules={rules} />;
        default:
          if (f.length > 200) {
            return <ProFormTextArea key={name} name={name} label={label} rules={rules} />;
          }
          return <ProFormText key={name} name={name} label={label} rules={rules} />;
      }
    });
};

/** 驼峰转换 */
const toCamelCase = (s: string) => s.charAt(0).toLowerCase() + s.slice(1);

/** 解析 URL 模板变量 */
const resolveUrl = (url: string, row: Record<string, any>) =>
  url.replace(/\{(\w+)\}/g, (_, key) => {
    const val = row[key] ?? row[toCamelCase(key)];
    return val !== undefined ? encodeURIComponent(String(val)) : '';
  });

/** 导出格式 */
const exportFormats = [
  { key: 'Excel', label: '导出 Excel' },
  { key: 'Csv', label: '导出 CSV' },
  { key: 'Json', label: '导出 JSON' },
  { key: 'Xml', label: '导出 XML' },
  { key: 'ExcelTemplate', label: '导出模板' },
];

const DynamicPage: React.FC = () => {
  const location = useLocation();
  // 从路径推断实体类型，如 /admin/user -> /Admin/User
  const type = useMemo(() => location.pathname, [location.pathname]);

  const actionRef = useRef<ActionType>();
  const chartRef = useRef<HTMLDivElement>(null);

  const [listFields, setListFields] = useState<CubeColumn[]>([]);
  const [addFields, setAddFields] = useState<CubeColumn[]>([]);
  const [editFields, setEditFields] = useState<CubeColumn[]>([]);
  const [detailFields, setDetailFields] = useState<CubeColumn[]>([]);

  const [addOpen, setAddOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [detailOpen, setDetailOpen] = useState(false);
  const [selectedRows, setSelectedRows] = useState<Record<string, any>[]>([]);
  const [currentRow, setCurrentRow] = useState<Record<string, any>>({});
  const [chartOptions, setChartOptions] = useState<any[]>([]);

  // 加载字段元数据
  useEffect(() => {
    const loadFields = async () => {
      try {
        const [listRes, addRes, editRes, detailRes] = await Promise.all([
          getFields(type, ColumnKind.LIST),
          getFields(type, ColumnKind.ADD),
          getFields(type, ColumnKind.EDIT),
          getFields(type, ColumnKind.DETAIL),
        ]);
        setListFields(listRes.data || []);
        setAddFields(addRes.data || []);
        setEditFields(editRes.data || []);
        setDetailFields(detailRes.data || []);
      } catch {
        // 字段加载失败
      }
    };
    loadFields();
  }, [type]);

  // 加载图表
  useEffect(() => {
    const loadChart = async () => {
      try {
        const res = await getChartData(type);
        if (Array.isArray(res.data) && res.data.length > 0) {
          setChartOptions(res.data);
        }
      } catch {
        // 无图表数据
      }
    };
    loadChart();
  }, [type]);

  // 渲染图表
  useEffect(() => {
    if (chartRef.current && chartOptions.length > 0) {
      const instance = echarts.init(chartRef.current);
      instance.setOption(chartOptions[0]);
      const onResize = () => instance.resize();
      window.addEventListener('resize', onResize);
      return () => {
        window.removeEventListener('resize', onResize);
        instance.dispose();
      };
    }
    return undefined;
  }, [chartOptions]);

  // 查看
  const handleView = useCallback(async (row: Record<string, any>) => {
    try {
      const res = await getDetail(type, row.id);
      setCurrentRow(res.data || row);
    } catch {
      setCurrentRow(row);
    }
    setDetailOpen(true);
  }, [type]);

  // 编辑
  const handleEdit = useCallback(async (row: Record<string, any>) => {
    try {
      const res = await getDetail(type, row.id);
      setCurrentRow(res.data || row);
    } catch {
      setCurrentRow(row);
    }
    setEditOpen(true);
  }, [type]);

  // 删除
  const handleDel = useCallback(async (id: any) => {
    try {
      await deleteItem(type, id);
      message.success('删除成功');
      actionRef.current?.reload();
    } catch {
      // 错误由拦截器处理
    }
  }, [type]);

  // 批量删除
  const handleBatchDel = useCallback(async () => {
    const keys = selectedRows.map((r) => String(r.id)).filter(Boolean);
    if (keys.length === 0) return;
    try {
      await deleteSelect(type, keys);
      message.success('批量删除成功');
      setSelectedRows([]);
      actionRef.current?.reload();
    } catch {
      // 错误由拦截器处理
    }
  }, [type, selectedRows]);

  // 导出
  const handleExport = useCallback((format: string) => {
    window.open(getExportUrl(type, format), '_blank');
  }, [type]);

  // 导入
  const handleImport = useCallback(async (file: File) => {
    try {
      const res = await importFile(type, file);
      message.success(res.message || '导入成功');
      actionRef.current?.reload();
    } catch {
      // 错误由拦截器处理
    }
    return false; // 阻止 antd Upload 默认行为
  }, [type]);

  // 添加提交
  const handleAddSubmit = useCallback(async (values: Record<string, any>) => {
    try {
      await addItem(type, values);
      message.success('添加成功');
      actionRef.current?.reload();
      return true;
    } catch {
      return false;
    }
  }, [type]);

  // 编辑提交
  const handleEditSubmit = useCallback(async (values: Record<string, any>) => {
    try {
      await updateItem(type, { ...currentRow, ...values });
      message.success('修改成功');
      actionRef.current?.reload();
      return true;
    } catch {
      return false;
    }
  }, [type, currentRow]);

  // 构建列配置
  const columns = useMemo(
    () => buildColumns(listFields, handleView, handleEdit, handleDel),
    [listFields, handleView, handleEdit, handleDel],
  );

  return (
    <PageContainer>
      {/* 图表区域 */}
      {chartOptions.length > 0 && (
        <div
          ref={chartRef}
          style={{ width: '100%', height: 400, marginBottom: 16, background: '#fff', borderRadius: 4 }}
        />
      )}

      {/* 数据表格 */}
      <ProTable<Record<string, any>>
        actionRef={actionRef}
        rowKey="id"
        search={{ labelWidth: 120 }}
        request={(params, sort) => queryList(type, params, sort)}
        columns={columns}
        scroll={{ x: 1200 }}
        rowSelection={{
          onChange: (_, rows) => setSelectedRows(rows),
        }}
        toolBarRender={() => [
          <Button key="add" type="primary" onClick={() => setAddOpen(true)}>
            <PlusOutlined /> 添加
          </Button>,
          selectedRows.length > 0 && (
            <Popconfirm key="batchDel" title={`确定删除选中的 ${selectedRows.length} 项吗？`} onConfirm={handleBatchDel}>
              <Button danger>
                <DeleteOutlined /> 删除选中({selectedRows.length})
              </Button>
            </Popconfirm>
          ),
          <Dropdown
            key="export"
            menu={{
              items: exportFormats.map((f) => ({ key: f.key, label: f.label })),
              onClick: ({ key }) => handleExport(key),
            }}
          >
            <Button>
              <DownloadOutlined /> 导出
            </Button>
          </Dropdown>,
          <Upload key="import" showUploadList={false} accept=".xlsx,.xls,.csv,.json,.zip" beforeUpload={handleImport}>
            <Button>
              <UploadOutlined /> 导入
            </Button>
          </Upload>,
        ]}
      />

      {/* 添加弹窗 */}
      <ModalForm
        title="添加"
        open={addOpen}
        onOpenChange={setAddOpen}
        onFinish={handleAddSubmit}
        modalProps={{ destroyOnClose: true }}
      >
        {buildFormFields(addFields)}
      </ModalForm>

      {/* 编辑弹窗 */}
      <ModalForm
        title="编辑"
        open={editOpen}
        onOpenChange={setEditOpen}
        onFinish={handleEditSubmit}
        initialValues={currentRow}
        modalProps={{ destroyOnClose: true }}
      >
        {buildFormFields(editFields)}
      </ModalForm>

      {/* 详情抽屉 */}
      <Drawer title="查看详情" width={600} open={detailOpen} onClose={() => setDetailOpen(false)}>
        <Descriptions column={1} bordered>
          {detailFields
            .sort((a, b) => a.sort - b.sort)
            .map((f) => (
              <Descriptions.Item key={f.name} label={f.displayName || f.name}>
                {f.dataType === 'Boolean' ? (
                  <IconStatus status={currentRow[toCamelCase(f.name)]} />
                ) : (
                  String(currentRow[toCamelCase(f.name)] ?? '')
                )}
              </Descriptions.Item>
            ))}
        </Descriptions>
      </Drawer>
    </PageContainer>
  );
};

export default DynamicPage;
