/**
 * 注册页（独立 /register 路由）
 *
 * 复用登录页的 RegisterPanel（完整注册逻辑：密码/手机/邮箱三方式 + OAuth 预填 + 验证码），
 * 以独立页面形式承载，onBack 返回登录页。
 */
import { Alert, Spin } from 'antd';
import { AppstoreOutlined, SafetyOutlined, ThunderboltOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import AuthLayout from '@/components/auth/AuthLayout';
import RegisterPanel from '@/pages/Login/RegisterPanel';
import { useLoginConfig } from '@/hooks/useLoginConfig';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { config, loading, error } = useLoginConfig();

  if (loading || !config) {
    return (
      <div className="cube-fullscreen-center">
        <Spin size="large" />
      </div>
    );
  }

  return (
    <AuthLayout
      brandTitle={config?.name || '魔方系统'}
      brandSubtitle={config?.loginTip}
      title="注册账号"
      description="创建账号，开启管理后台之旅"
      highlights={[
        { icon: <SafetyOutlined />, title: '安全策略内置', description: '密码强度、验证码、激活机制开箱即用' },
        { icon: <AppstoreOutlined />, title: '多种注册方式', description: '账号密码 / 手机 / 邮箱按能力开关展示' },
        { icon: <ThunderboltOutlined />, title: '登录即达', description: '注册成功自动进入系统，无需重复登录' },
      ]}
      footer={config?.copyright ? <span dangerouslySetInnerHTML={{ __html: config.copyright }} /> : undefined}
    >
      {error && <Alert type="error" message={error} />}
      <RegisterPanel config={config} onBack={() => navigate('/login')} />
    </AuthLayout>
  );
}
