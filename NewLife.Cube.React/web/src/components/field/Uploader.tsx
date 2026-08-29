/**
 * 上传组件（图片 / 文件）
 *
 * 调用 UploadFile API（POST /{type}/UploadFile），返回 URL 写入表单值。
 * 图片模式带预览，文件模式带下载链接。
 */
import { useState } from 'react';
import { Button, Image, Input, Space, Upload, message } from 'antd';
import { DeleteOutlined, UploadOutlined } from '@ant-design/icons';
import { api } from '@/api';

export interface UploaderProps {
  value?: string;
  onChange?: (value: string | undefined) => void;
  /** 实体路径前缀（如 '/Admin/User'），UploadFile 需要 */
  type?: string;
  /** 主记录主键（0=新增） */
  recordId?: number | string;
  /** 图片模式 */
  image?: boolean;
  placeholder?: string;
  disabled?: boolean;
}

export default function Uploader({ value, onChange, type, recordId, image, placeholder, disabled }: UploaderProps) {
  const [uploading, setUploading] = useState(false);

  const handleUpload = async (file: File) => {
    if (!type) {
      message.warning('缺少上传路径配置');
      return false;
    }
    setUploading(true);
    try {
      const res = await api.page.uploadFile(type, file, { id: Number(recordId ?? 0) });
      const data = (res?.data ?? {}) as Record<string, unknown>;
      // 后端可能以 HTTP 200 + error/code 字段返回业务错误（如 User 头像仅限当前登录用户）
      if (data.error || (typeof data.code === 'number' && data.code !== 0)) {
        message.error(String(data.error ?? data.message ?? '上传失败'));
        return false;
      }
      // 兼容字段名差异：旧版返回 url，新版返回 filePath
      const url = String(data.url ?? data.filePath ?? '');
      if (!url) {
        message.error('上传失败：未返回文件地址');
        return false;
      }
      onChange?.(url);
      message.success('上传成功');
      return false;
    } catch {
      message.error('上传失败');
      return false;
    } finally {
      setUploading(false);
    }
  };

  if (image) {
    return (
      <Space direction="vertical" size={8}>
        {value ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Image src={value} width={80} height={80} style={{ objectFit: 'cover', borderRadius: 4 }} />
            <Button
              size="small"
              danger
              icon={<DeleteOutlined />}
              disabled={disabled}
              onClick={() => onChange?.(undefined)}
            />
          </div>
        ) : (
          <Upload
            accept="image/*"
            showUploadList={false}
            beforeUpload={handleUpload}
            disabled={disabled || uploading}
          >
            <Button icon={<UploadOutlined />} loading={uploading}>
              上传图片
            </Button>
          </Upload>
        )}
      </Space>
    );
  }

  // 文件模式：URL 展示 + 上传按钮
  return (
    <Space direction="vertical" size={8} style={{ width: '100%' }}>
      <Upload showUploadList={false} beforeUpload={handleUpload} disabled={disabled || uploading}>
        <Button icon={<UploadOutlined />} loading={uploading}>
          上传文件
        </Button>
      </Upload>
      {value && (
        <Space>
          <a href={value} target="_blank" rel="noreferrer">
            查看文件
          </a>
          <Button size="small" danger icon={<DeleteOutlined />} disabled={disabled} onClick={() => onChange?.(undefined)} />
        </Space>
      )}
      {placeholder && <Input value={value ?? ''} readOnly placeholder={placeholder} size="small" />}
    </Space>
  );
}
