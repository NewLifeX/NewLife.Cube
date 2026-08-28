/**
 * 登录页（对齐 Vue 皮肤 pages/LoginPage.vue + LoginForm.vue）
 *
 * 功能：
 * 1. 加载 /Auth/LoginConfig，按能力开关渲染密码/短信/邮箱登录、注册入口、OAuth、版权
 * 2. 密码登录自动 RSA-OAEP Challenge 加密（@cube/auth-logic 内置）
 * 3. 图片验证码（login.captcha=true 时显示）
 * 4. MFA 二步验证（登录返回 mfa_required 时进入）
 * 5. 账号未激活时展示重发激活入口
 */
import { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Form, Input, Space, Spin, Tabs, message } from 'antd';
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { getConfig } from '@/configure';
import { useLoginConfig, useCaptcha } from '@/hooks/useLoginConfig';
import { parsePasswordRules } from '@/utils/passwordRules';
import RegisterPanel from './RegisterPanel';
import type { AuthCategory, LoginResult } from '@cube/api-core';

const useAuthStore = useUserStore;

/** 从登录响应消息中提取 MFA 令牌（消息形如 "mfa_required:xxx"） */
function extractMfaToken(message?: string): string {
  if (!message) return '';
  const idx = message.indexOf('mfa_required:');
  return idx >= 0 ? message.slice(idx + 'mfa_required:'.length).trim() : '';
}

export default function LoginPage() {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { config, loading, error: configError } = useLoginConfig();
  const { captcha, refresh } = useCaptcha();
  const { redirectKey } = getConfig().auth;

  const redirect = params.get(redirectKey) || params.get('redirect') || '/';
  const [activeTab, setActiveTab] = useState<'login' | 'register'>('login');
  const [mode, setMode] = useState<'password' | 'sms' | 'mail'>('password');
  const [submitting, setSubmitting] = useState(false);
  const [mfaToken, setMfaToken] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [mfaSubmitting, setMfaSubmitting] = useState(false);
  const [activation, setActivation] = useState<Partial<LoginResult> | null>(null);

  const loginCfg = config?.login;
  const registerCfg = config?.register;
  const security = config?.security;

  const showPassword = loginCfg?.password !== false;
  const showSms = !!loginCfg?.sms;
  const showMail = !!loginCfg?.mail;
  const showCaptcha = !!loginCfg?.captcha;
  const showRegister = !!registerCfg?.enabled;
  const oauthProviders = config?.oauth ?? [];
  const systemName = config?.name || getConfig().base.title;
  const logoSrc = config?.loginLogo || config?.logo || '';

  // 密码规则（security.passwordStrength 驱动）
  const passwordRules = useMemo(() => {
    const password = form.getFieldValue('password') as string | undefined;
    if (!password) return [];
    return parsePasswordRules(security?.passwordStrength).map((r) => ({ ...r, satisfied: r.test(password) }));
  }, [form, security?.passwordStrength]);

  // 自动跳转：密码禁用且仅一个 OAuth 时
  useEffect(() => {
    if (!config) return;
    if (showPassword === false && oauthProviders.length === 1) {
      handleOAuth(oauthProviders[0].name);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [config]);

  const buildOAuthUrl = (name: string, redirectUri: string) =>
    `/Sso/Login/${name}?source=front-end&redirect_uri=${encodeURIComponent(redirectUri)}`;

  const handleOAuth = (name: string) => {
    const redirectUri = `${window.location.origin}${redirect}`;
    window.location.href = buildOAuthUrl(name, redirectUri);
  };

  const afterLogin = () => {
    message.success('登录成功');
    navigate(redirect, { replace: true });
  };

  const handleLogin = async () => {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      const captchaId = showCaptcha ? captcha?.captchaId : undefined;
      const captchaCode = showCaptcha ? (values.captchaCode as string) : undefined;

      const res =
        mode === 'password'
          ? await useAuthStore.getState().login(values.username, values.password, captchaId, captchaCode)
          : await useAuthStore.getState().loginByCode(values.username, values.code, (mode === 'sms' ? 'mobile' : 'mail') as AuthCategory, captchaId, captchaCode);

      const data = res.data as LoginResult | undefined;

      // MFA 二步验证
      const mfa = extractMfaToken(res.message);
      if (mfa && !data?.accessToken) {
        setMfaToken(mfa);
        return;
      }

      // 账号未激活
      if (data?.pendingActivation || res.message?.includes('未激活')) {
        setActivation(data ?? { pendingActivation: true, channels: [], targets: [] });
        message.warning('账号尚未激活，请先激活');
        return;
      }
      if (data?.accessToken) {
        afterLogin();
      } else {
        message.error(res.message || '登录失败，请检查用户名和密码');
      }
    } catch (err) {
      if (showCaptcha) void refresh();
      message.error((err as Error)?.message || '网络错误，请稍后重试');
    } finally {
      setSubmitting(false);
    }
  };

  const handleMfaVerify = async () => {
    if (!mfaCode) return;
    setMfaSubmitting(true);
    try {
      const res = await api.user.mfaVerify({ mfaToken, code: mfaCode });
      if (res.data?.accessToken) {
        api.tokenManager.setToken(res.data.accessToken);
        afterLogin();
      } else {
        message.error(res.message || '验证码错误');
      }
    } catch (err) {
      message.error((err as Error)?.message || '验证失败');
    } finally {
      setMfaSubmitting(false);
    }
  };

  const handleResend = async (channel: string, account: string) => {
    try {
      const res = await api.user.sendActivateCode(channel === 'sms' ? 'Sms' : 'Mail', account);
      message.success(`已发送至 ${res.data?.target ?? ''}`);
    } catch (err) {
      message.error((err as Error)?.message || '发送失败');
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', height: '100%', alignItems: 'center', justifyContent: 'center' }}>
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
        background:
          config?.loginBackground || 'linear-gradient(135deg, #0f2027 0%, #203a43 50%, #2c5364 100%)',
        padding: 16,
      }}
    >
      <Card style={{ width: 420, borderRadius: 12, boxShadow: '0 8px 32px rgba(0,0,0,0.3)' }}>
        {/* 品牌区 */}
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          {logoSrc ? (
            <img src={logoSrc} alt="logo" style={{ height: 48, marginBottom: 8 }} />
          ) : (
            <div style={{ fontSize: 40, marginBottom: 8 }}>🧊</div>
          )}
          <h1 style={{ margin: 0, fontSize: 22 }}>{systemName}</h1>
          {config?.loginTip && <p style={{ color: '#888', marginTop: 4 }}>{config.loginTip}</p>}
        </div>

        {configError && <Alert type="error" message={configError} style={{ marginBottom: 16 }} />}

        {activation ? (
          <div>
            <Alert
              type="warning"
              showIcon
              message="账号未激活"
              description="请通过邮箱或手机验证激活账号后登录。"
              style={{ marginBottom: 16 }}
            />
            {(activation.targets ?? []).map((target, i) => {
              const channel = (activation.channels ?? [])[i] === 'sms' ? 'sms' : 'mail';
              return (
                <div key={i} style={{ marginBottom: 8, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span>{target}</span>
                  <Button size="small" onClick={() => void handleResend(channel, target)}>
                    重新发送
                  </Button>
                </div>
              );
            })}
            <Button block onClick={() => setActivation(null)} style={{ marginTop: 8 }}>
              返回登录
            </Button>
          </div>
        ) : mfaToken ? (
          <div>
            <Alert type="info" showIcon message="双重验证" description="请输入身份验证器应用中的 6 位动态验证码。" style={{ marginBottom: 16 }} />
            <Input
              size="large"
              placeholder="6 位动态验证码"
              maxLength={6}
              value={mfaCode}
              onChange={(e) => setMfaCode(e.target.value)}
              onPressEnter={() => void handleMfaVerify()}
              style={{ marginBottom: 12 }}
            />
            <Button type="primary" block size="large" loading={mfaSubmitting} onClick={() => void handleMfaVerify()}>
              验证
            </Button>
            <Button type="link" block onClick={() => setMfaToken('')} style={{ marginTop: 8 }}>
              返回登录
            </Button>
          </div>
        ) : (
          <Tabs
            activeKey={activeTab}
            onChange={(k) => setActiveTab(k as 'login' | 'register')}
            centered
            items={[
              {
                key: 'login',
                label: '登 录',
                children: (
                  <Form form={form} layout="vertical" requiredMark={false} initialValues={{ username: params.get('username') ?? '' }}>
                    {/* 登录方式 Tab */}
                    {(showSms || showMail) && (
                      <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
                        {showPassword && (
                          <Button size="small" type={mode === 'password' ? 'primary' : 'default'} onClick={() => setMode('password')}>
                            密码登录
                          </Button>
                        )}
                        {showSms && (
                          <Button size="small" type={mode === 'sms' ? 'primary' : 'default'} onClick={() => setMode('sms')}>
                            短信登录
                          </Button>
                        )}
                        {showMail && (
                          <Button size="small" type={mode === 'mail' ? 'primary' : 'default'} onClick={() => setMode('mail')}>
                            邮箱登录
                          </Button>
                        )}
                      </div>
                    )}

                    <Form.Item name="username" label={mode === 'password' ? '用户名' : mode === 'sms' ? '手机号' : '邮箱'} rules={[{ required: true, message: '请输入' }]}>
                      <Input size="large" prefix={<UserOutlined />} placeholder={mode === 'password' ? '用户名 / 邮箱 / 手机号' : mode === 'sms' ? '手机号' : '邮箱'} autoComplete="username" />
                    </Form.Item>

                    {mode === 'password' ? (
                      <Form.Item name="password" label="密码" rules={[{ required: true, message: '请输入密码' }]}>
                        <Input.Password
                          size="large"
                          prefix={<LockOutlined />}
                          placeholder="请输入密码"
                          autoComplete="current-password"
                          onChange={() => form.validateFields(['password']).catch(() => {})}
                        />
                      </Form.Item>
                    ) : (
                      <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
                        <Input size="large" placeholder="验证码" autoComplete="one-time-code" />
                      </Form.Item>
                    )}

                    {/* 密码强度提示 */}
                    {mode === 'password' && passwordRules.length > 0 && (
                      <div style={{ marginBottom: 12, fontSize: 12, color: '#888' }}>
                        {passwordRules.map((r, i) => (
                          <div key={i} style={{ color: r.satisfied ? '#52c41a' : '#bbb' }}>
                            {r.satisfied ? '✓' : '○'} {r.label}
                          </div>
                        ))}
                      </div>
                    )}

                    {/* 图形验证码 */}
                    {showCaptcha && (
                      <Form.Item name="captchaCode" label="图形验证码" rules={[{ required: true, message: '请输入图形验证码' }]}>
                        <Space.Compact style={{ width: '100%' }}>
                          <Input size="large" placeholder="图形验证码" />
                          {captcha?.image && (
                            <img
                              src={`data:image/svg+xml;utf8,${encodeURIComponent(captcha.image)}`}
                              onClick={refresh}
                              style={{ width: 110, height: 40, cursor: 'pointer', border: '1px solid #d9d9d9', borderRadius: 4 }}
                              alt="captcha"
                              title="点击刷新"
                            />
                          )}
                        </Space.Compact>
                      </Form.Item>
                    )}

                    <Button type="primary" block size="large" loading={submitting} onClick={() => void handleLogin()}>
                      登 录
                    </Button>

                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 12 }}>
                      {showRegister ? (
                        <Button type="link" size="small" onClick={() => setActiveTab('register')}>
                          注册账号
                        </Button>
                      ) : (
                        <span />
                      )}
                      <Button type="link" size="small" onClick={() => navigate('/forgot-password')}>
                        忘记密码
                      </Button>
                    </div>
                  </Form>
                ),
              },
              ...(showRegister
                ? [
                    {
                      key: 'register',
                      label: '注 册',
                      children: <RegisterPanel config={config!} onBack={() => setActiveTab('login')} />,
                    },
                  ]
                : []),
            ]}
          />
        )}

        {/* OAuth 第三方登录 */}
        {!mfaToken && !activation && oauthProviders.length > 0 && (
          <div style={{ marginTop: 16 }}>
            <div style={{ textAlign: 'center', color: '#aaa', fontSize: 12, marginBottom: 12 }}>———— 第三方登录 ————</div>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 12 }}>
              {oauthProviders.map((p) => (
                <Button key={p.name} onClick={() => handleOAuth(p.name)}>
                  {p.logo ? <img src={p.logo} alt={p.name} style={{ width: 18, height: 18, marginRight: 6 }} /> : null}
                  {p.nickName || p.name}
                </Button>
              ))}
            </div>
          </div>
        )}

        {config?.copyright && (
          <div style={{ textAlign: 'center', color: '#aaa', fontSize: 12, marginTop: 16 }} dangerouslySetInnerHTML={{ __html: config.copyright }} />
        )}
      </Card>
    </div>
  );
}
