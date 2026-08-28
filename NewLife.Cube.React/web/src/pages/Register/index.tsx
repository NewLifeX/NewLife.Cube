/**
 * 注册页（独立 /register 路由）
 *
 * 复用登录页的 RegisterPanel（完整注册逻辑：密码/手机/邮箱三方式 + OAuth 预填 + 验证码），
 * 以独立页面形式承载，onBack 返回登录页。
 */
import { Alert, Card, Spin } from 'antd';
import { useNavigate } from 'react-router-dom';
import RegisterPanel from '@/pages/Login/RegisterPanel';
import { useLoginConfig } from '@/hooks/useLoginConfig';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { config, loading, error } = useLoginConfig();

  if (loading || !config) {
    return (
      <div style={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <Spin size="large" />
      </div>
    );
  }

  return (
    <div
      style={{
        height: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #0f2027 0%, #203a43 50%, #2c5364 100%)',
        padding: 16,
      }}
    >
      <Card style={{ width: 460, borderRadius: 12 }}>
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <div style={{ fontSize: 40 }}>📝</div>
          <h2 style={{ margin: '8px 0 0' }}>注册账号</h2>
        </div>
        {error && <Alert type="error" message={error} style={{ marginBottom: 16 }} />}
        <RegisterPanel config={config} onBack={() => navigate('/login')} />
      </Card>
    </div>
  );
}
