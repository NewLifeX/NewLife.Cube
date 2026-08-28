/**
 * 注册面板（对齐 Vue 皮肤 pages/RegisterForm.vue）
 *
 * 能力开关驱动：password / sms / mail 三方式 + OAuth 预填。
 * 使用 @cube/auth-logic/zustand 的注册 store。
 */
import { useEffect, useState } from 'react';
import { Button, Form, Input, Space, message } from 'antd';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { createZustandRegisterStore } from '@cube/auth-logic/zustand';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';
import { useMenuStore } from '@/stores/menu';
import { getConfig } from '@/configure';
import { useCaptcha } from '@/hooks/useLoginConfig';
import type { LoginConfig } from '@cube/api-core';

const useRegisterStore = createZustandRegisterStore(api);

export interface RegisterPanelProps {
  config: LoginConfig;
  onBack: () => void;
}

export default function RegisterPanel({ config, onBack }: RegisterPanelProps) {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const register = config.register;
  const { captcha, refresh } = useCaptcha();
  const [category, setCategory] = useState<'password' | 'mobile' | 'mail'>('password');
  const { redirectKey } = getConfig().auth;
  const redirect = params.get(redirectKey) || params.get('redirect') || '/';

  const sending = useRegisterStore((s) => s.sending);
  const submitting = useRegisterStore((s) => s.submitting);
  const countdown = useRegisterStore((s) => s.countdown);
  const error = useRegisterStore((s) => s.error);

  // OAuth 预填
  useEffect(() => {
    const token = params.get('oauthToken');
    if (token) {
      useRegisterStore
        .getState()
        .loadOAuthPendingInfo(token)
        .then((info) => {
          if (!info) return;
          form.setFieldsValue({
            username: info.username,
            email: info.email,
            mobile: info.mobile,
          });
          if (info.email) setCategory('mail');
          else if (info.mobile) setCategory('mobile');
        });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (error) message.error(error);
  }, [error]);

  const canPassword = register?.password !== false;
  const canSms = !!register?.sms;
  const canMail = !!register?.mail;

  const sendCode = async (channel: 'mobile' | 'mail') => {
    const values = await form.validateFields([channel === 'mobile' ? 'mobile' : 'email']);
    const account = channel === 'mobile' ? values.mobile : values.email;
    const ok =
      channel === 'mobile'
        ? await useRegisterStore.getState().sendSmsCode(account)
        : await useRegisterStore.getState().sendMailCode(account);
    if (ok) message.success('验证码已发送');
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    const payload = { ...values, confirmPassword: undefined };
    let ok = false;
    if (category === 'mobile') ok = await useRegisterStore.getState().registerByPhone(payload);
    else if (category === 'mail') ok = await useRegisterStore.getState().registerByEmail(payload);
    else ok = await useRegisterStore.getState().registerByPassword(payload);
    if (ok) {
      // 注册成功：写 token（AuthLogic.register 已写）或提示激活
      message.success('注册成功');
      // 若已自动登录，跳转
      if (api.tokenManager.getToken()) {
        useUserStore.getState().fetchUserInfo().catch(() => {});
        useUserStore.getState().fetchMenus().then((menus) => useMenuStore.getState().setFlatMenus(menus)).catch(() => {});
        navigate(redirect);
      } else {
        onBack();
      }
    }
  };

  return (
    <Form form={form} layout="vertical" requiredMark={false} style={{ maxWidth: 380, margin: '0 auto' }}>
      {/* 注册方式切换 */}
      {(canPassword || canSms || canMail) && (
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          {canPassword && (
            <Button size="small" type={category === 'password' ? 'primary' : 'default'} onClick={() => setCategory('password')}>
              账号密码
            </Button>
          )}
          {canSms && (
            <Button size="small" type={category === 'mobile' ? 'primary' : 'default'} onClick={() => setCategory('mobile')}>
              手机注册
            </Button>
          )}
          {canMail && (
            <Button size="small" type={category === 'mail' ? 'primary' : 'default'} onClick={() => setCategory('mail')}>
              邮箱注册
            </Button>
          )}
        </div>
      )}

      {category === 'password' && (
        <>
          <Form.Item name="username" label="用户名" rules={[{ required: true, message: '请输入用户名' }, { min: 2, message: '用户名至少 2 个字符' }]}>
            <Input placeholder="请输入用户名" autoComplete="username" />
          </Form.Item>
          {canMail && (
            <Form.Item name="email" label="邮箱" rules={[{ type: 'email', message: '邮箱格式不正确' }]}>
              <Input placeholder="请输入邮箱" />
            </Form.Item>
          )}
          {canSms && (
            <Form.Item name="mobile" label="手机号" rules={[{ pattern: /^1\d{10}$/, message: '手机号格式不正确' }]}>
              <Input placeholder="请输入手机号" />
            </Form.Item>
          )}
        </>
      )}
      {category === 'mobile' && (
        <Form.Item name="mobile" label="手机号" rules={[{ required: true, pattern: /^1\d{10}$/, message: '请输入正确手机号' }]}>
          <Input placeholder="请输入手机号" />
        </Form.Item>
      )}
      {category === 'mail' && (
        <Form.Item name="email" label="邮箱" rules={[{ required: true, type: 'email', message: '请输入正确邮箱' }]}>
          <Input placeholder="请输入邮箱" />
        </Form.Item>
      )}

      {(category === 'mobile' || category === 'mail') && (
        <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
          <Space.Compact style={{ width: '100%' }}>
            <Input placeholder="验证码" />
            <Button
              disabled={countdown > 0 || sending}
              onClick={() => void sendCode(category)}
            >
              {countdown > 0 ? `${countdown}s` : '获取验证码'}
            </Button>
          </Space.Compact>
        </Form.Item>
      )}

      <Form.Item name="password" label="密码" rules={[{ required: true, message: '请输入密码' }, { min: 6, message: '密码至少 6 位' }]}>
        <Input.Password placeholder="请输入密码" autoComplete="new-password" />
      </Form.Item>
      <Form.Item name="confirmPassword" label="确认密码" dependencies={['password']} rules={[
        { required: true, message: '请确认密码' },
        ({ getFieldValue }) => ({
          validator(_, value) {
            if (!value || getFieldValue('password') === value) return Promise.resolve();
            return Promise.reject(new Error('两次输入的密码不一致'));
          },
        }),
      ]}>
        <Input.Password placeholder="请再次输入密码" autoComplete="new-password" />
      </Form.Item>

      {register?.captcha && (
        <Form.Item name="captchaCode" label="验证码" rules={[{ required: true, message: '请输入图形验证码' }]}>
          <Space.Compact style={{ width: '100%' }}>
            <Input placeholder="图形验证码" />
            {captcha?.image && (
              <img
                src={`data:image/svg+xml;utf8,${encodeURIComponent(captcha.image)}`}
                onClick={refresh}
                style={{ width: 100, height: 32, cursor: 'pointer', border: '1px solid #d9d9d9', borderRadius: 4 }}
                alt="captcha"
                title="点击刷新"
              />
            )}
          </Space.Compact>
        </Form.Item>
      )}

      <Button type="primary" block size="large" loading={submitting} onClick={() => void handleSubmit()} style={{ marginTop: 8 }}>
        注 册
      </Button>
      <div style={{ textAlign: 'center', marginTop: 12 }}>
        <Button type="link" size="small" onClick={onBack}>
          已有账号？去登录
        </Button>
      </div>
    </Form>
  );
}
