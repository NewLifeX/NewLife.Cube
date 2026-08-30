/**
 * 登录页（对齐 Vue 皮肤 pages/LoginPage.vue + LoginForm.vue）
 *
 * 功能：
 * 1. 加载 /Auth/LoginConfig，按能力开关渲染密码/短信/邮箱登录、注册入口、OAuth、版权
 * 2. 密码登录自动 RSA-OAEP Challenge 加密（@newlifex/auth-logic 内置）
 * 3. 图片验证码（login.captcha=true 时显示）
 * 4. MFA 二步验证（登录返回 mfa_required 时进入）
 * 5. 账号未激活时展示重发激活入口
 */
import { useEffect, useMemo, useRef, useState } from 'react';
import { Alert, App, Button, Checkbox, Form, Input, Segmented, Spin, Tabs } from 'antd';
import type { InputRef } from 'antd';
import { AppstoreOutlined, LockOutlined, SafetyOutlined, ThunderboltOutlined, UserOutlined } from '@ant-design/icons';
import { useNavigate, useSearchParams } from 'react-router-dom';
import AuthLayout from '@/components/auth/AuthLayout';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { getConfig } from '@/configure';
import { useLoginConfig, useCaptcha } from '@/hooks/useLoginConfig';
import { parsePasswordRules } from '@/utils/passwordRules';
import RegisterPanel from './RegisterPanel';
import type { AuthCategory, LoginResult } from '@newlifex/api-core';

const useAuthStore = useUserStore;

/** 从登录响应消息中提取 MFA 令牌（消息形如 "mfa_required:xxx"） */
function extractMfaToken(message?: string): string {
  if (!message) return '';
  const idx = message.indexOf('mfa_required:');
  return idx >= 0 ? message.slice(idx + 'mfa_required:'.length).trim() : '';
}

export default function LoginPage() {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const passwordRef = useRef<InputRef>(null);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { config, loading, error: configError } = useLoginConfig();
  // 配置异步加载：enabled 由 false→true 时 useCaptcha 自动触发首次拉取
  const { captcha, refresh } = useCaptcha(!!config?.login?.captcha);
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
          ? await useAuthStore.getState().login(values.username, values.password, captchaId, captchaCode, !!values.remember)
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
      <div className="cube-fullscreen-center">
        <Spin size="large" />
      </div>
    );
  }

  return (
    <AuthLayout
      title="登 录"
      description="登录后即可访问管理后台"
      brandTitle={systemName}
      brandSubtitle={config?.loginTip || '魔方快速开发平台'}
      highlights={[
        { icon: <SafetyOutlined />, title: '统一认证入口', description: '密码 / 短信 / 邮箱 / 第三方，安全策略内置' },
        { icon: <AppstoreOutlined />, title: '权限菜单驱动', description: '登录后按角色自动生成管理导航' },
        { icon: <ThunderboltOutlined />, title: '字段元数据驱动', description: '列表与表单由后端字段配置动态渲染' },
      ]}
      footer={config?.copyright ? <span dangerouslySetInnerHTML={{ __html: config.copyright }} /> : undefined}
    >
      {configError && <Alert type="error" message={configError} />}

      {activation ? (
        <div className="cube-auth-status-list">
          <Alert
            type="warning"
            showIcon
            message="账号未激活"
            description="请通过邮箱或手机验证激活账号后登录。"
          />
          {(activation.targets ?? []).map((target, i) => {
            const channel = (activation.channels ?? [])[i] === 'sms' ? 'sms' : 'mail';
            return (
              <div key={i} className="cube-auth-status-item">
                <span>{target}</span>
                <Button size="small" onClick={() => void handleResend(channel, target)}>
                  重新发送
                </Button>
              </div>
            );
          })}
          <Button block onClick={() => setActivation(null)}>
            返回登录
          </Button>
        </div>
      ) : mfaToken ? (
        <div style={{ display: 'grid', gap: 16 }}>
          <Alert type="info" showIcon message="双重验证" description="请输入身份验证器应用中的 6 位动态验证码。" />
          <Input
            size="large"
            placeholder="6 位动态验证码"
            maxLength={6}
            value={mfaCode}
            onChange={(e) => setMfaCode(e.target.value)}
            onPressEnter={() => void handleMfaVerify()}
          />
          <Button type="primary" block size="large" loading={mfaSubmitting} onClick={() => void handleMfaVerify()}>
            验证
          </Button>
          <Button type="link" block onClick={() => setMfaToken('')}>
            返回登录
          </Button>
        </div>
      ) : (
        <Tabs
          className="cube-auth-tabs"
          activeKey={activeTab}
          onChange={(k) => setActiveTab(k as 'login' | 'register')}
          centered
          items={[
            {
              key: 'login',
              label: '登 录',
              children: (
                <Form form={form} layout="vertical" requiredMark={false} initialValues={{ username: params.get('username') ?? '' }} onFinish={() => void handleLogin()}>
                  {/* 登录方式切换 */}
                  {(showSms || showMail) && (
                    <Segmented
                      className="cube-auth-segment"
                      value={mode}
                      onChange={(v) => setMode(v as 'password' | 'sms' | 'mail')}
                      options={[
                        ...(showPassword ? [{ label: '密码登录', value: 'password' as const }] : []),
                        ...(showSms ? [{ label: '短信登录', value: 'sms' as const }] : []),
                        ...(showMail ? [{ label: '邮箱登录', value: 'mail' as const }] : []),
                      ]}
                    />
                  )}

                  <Form.Item name="username" label={mode === 'password' ? '用户名' : mode === 'sms' ? '手机号' : '邮箱'} rules={[{ required: true, message: '请输入' }]}>
                    <Input
                      size="large"
                      prefix={<UserOutlined />}
                      placeholder={mode === 'password' ? '用户名 / 邮箱 / 手机号' : mode === 'sms' ? '手机号' : '邮箱'}
                      autoComplete="username"
                      onPressEnter={(e) => {
                        // 用户名框 Enter 不提交（密码为空），焦点跳密码框；Tab 走浏览器原生顺序
                        e.preventDefault();
                        passwordRef.current?.focus();
                      }}
                    />
                  </Form.Item>

                  {mode === 'password' ? (
                    <Form.Item name="password" label="密码" rules={[{ required: true, message: '请输入密码' }]}>
                      <Input.Password
                        ref={passwordRef}
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
                    <div className="cube-auth-password-rules">
                      {passwordRules.map((r, i) => (
                        <div key={i} className={r.satisfied ? 'cube-auth-password-rule-ok' : ''}>
                          {r.satisfied ? '✓' : '○'} {r.label}
                        </div>
                      ))}
                    </div>
                  )}

                  {/* 图形验证码 */}
                  {showCaptcha && (
                    <Form.Item name="captchaCode" label="图形验证码" rules={[{ required: true, message: '请输入图形验证码' }]}>
                      <div className="cube-auth-captcha">
                        <Input size="large" placeholder="图形验证码" />
                        {captcha?.image && (
                          <img
                            src={`data:image/svg+xml;utf8,${encodeURIComponent(captcha.image)}`}
                            onClick={refresh}
                            className="cube-auth-captcha-image"
                            alt="captcha"
                            title="点击刷新"
                          />
                        )}
                      </div>
                    </Form.Item>
                  )}

                  {/* 保存密码（记住登录状态）：后端按 remember 把令牌有效期延长到 365 天，重开系统免登录 */}
                  {mode === 'password' && (
                    <Form.Item name="remember" valuePropName="checked" className="cube-auth-remember">
                      <Checkbox>保存密码</Checkbox>
                    </Form.Item>
                  )}

                  <Button type="primary" block size="large" htmlType="submit" loading={submitting}>
                    登 录
                  </Button>

                  <div className="cube-auth-inline-actions">
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
        <div className="cube-auth-provider-section">
          <div className="cube-auth-provider-title">
            <span>第三方登录</span>
          </div>
          <div className="cube-auth-provider-grid">
            {oauthProviders.map((p) => (
              <Button key={p.name} className="cube-auth-provider-button" onClick={() => handleOAuth(p.name)}>
                {p.logo ? <img src={p.logo} alt={p.name} /> : null}
                {p.nickName || p.name}
              </Button>
            ))}
          </div>
        </div>
      )}
    </AuthLayout>
  );
}
