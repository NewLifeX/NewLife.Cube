/**
 * 忘记密码页（对齐 Vue 皮肤忘记密码流程）
 *
 * 流程：输入邮箱/手机号 → 发送验证码 → 输入验证码 + 新密码 → 重置 → 跳登录。
 * 使用 @newlifex/auth-logic/zustand 的 ForgotPassword store。
 */
import { useEffect, useState } from 'react';
import { Alert, App, Button, Form, Input, Radio } from 'antd';
import { LockOutlined, MailOutlined, MobileOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import AuthLayout from '@/components/auth/AuthLayout';
import { createZustandForgotPasswordStore } from '@newlifex/auth-logic/zustand';
import { api } from '@/api';

const useForgotStore = createZustandForgotPasswordStore(api);

export default function ForgotPasswordPage() {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const [channel, setChannel] = useState<'mail' | 'sms'>('mail');
  const [step, setStep] = useState<'input' | 'reset'>('input');
  const [account, setAccount] = useState('');

  const sending = useForgotStore((s) => s.sending);
  const submitting = useForgotStore((s) => s.submitting);
  const countdown = useForgotStore((s) => s.countdown);
  const error = useForgotStore((s) => s.error);

  useEffect(() => {
    if (error) message.error(error);
  }, [error]);

  const handleSendCode = async () => {
    const values = await form.validateFields(['account']);
    setAccount(values.account);
    const ok = await useForgotStore.getState().sendCode(values.account, channel);
    if (ok) {
      setStep('reset');
      message.success('验证码已发送');
    }
  };

  const handleReset = async () => {
    const values = await form.validateFields();
    const ok = await useForgotStore
      .getState()
      .confirmReset({
        username: account,
        code: values.code,
        newPassword: values.newPassword,
        confirmPassword: values.confirmPassword,
      });
    if (ok) {
      message.success('密码重置成功，请重新登录');
      navigate('/login');
    }
  };

  return (
    <AuthLayout
      brandTitle="魔方系统"
      brandSubtitle="通过邮箱或手机验证码重置密码"
      title="找回密码"
      description="验证身份后设置新密码"
      highlights={[
        { icon: <MailOutlined />, title: '邮箱找回', description: '发送邮件验证码验证身份' },
        { icon: <MobileOutlined />, title: '手机找回', description: '发送短信验证码验证身份' },
        { icon: <LockOutlined />, title: '安全重置', description: '重置后使用新密码重新登录' },
      ]}
      footer="©2002-2026 NewLife"
    >
      <Form form={form} layout="vertical" requiredMark={false} onFinish={() => void (step === 'input' ? handleSendCode() : handleReset())}>
        {step === 'input' && (
          <>
            <Form.Item label="找回渠道">
              <Radio.Group value={channel} onChange={(e) => setChannel(e.target.value)}>
                <Radio.Button value="mail">邮箱</Radio.Button>
                <Radio.Button value="sms">手机</Radio.Button>
              </Radio.Group>
            </Form.Item>
            <Form.Item
              name="account"
              label={channel === 'mail' ? '邮箱' : '手机号'}
              rules={[
                { required: true, message: '请输入' },
                channel === 'mail'
                  ? { type: 'email', message: '邮箱格式不正确' }
                  : { pattern: /^1\d{10}$/, message: '手机号格式不正确' },
              ]}
            >
              <Input size="large" placeholder={channel === 'mail' ? '请输入邮箱' : '请输入手机号'} />
            </Form.Item>
            <Button type="primary" block size="large" htmlType="submit" loading={sending}>
              {countdown > 0 ? `${countdown}s 后重发` : '发送验证码'}
            </Button>
          </>
        )}

        {step === 'reset' && (
          <>
            <Alert type="info" showIcon message={`验证码已发送至 ${account}`} style={{ marginBottom: 16 }} />
            <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
              <div className="cube-auth-captcha">
                <Input size="large" placeholder="验证码" maxLength={6} />
                <Button
                  size="large"
                  disabled={countdown > 0 || sending}
                  onClick={() => void useForgotStore.getState().resendCode(account, channel)}
                >
                  {countdown > 0 ? `${countdown}s` : '重发'}
                </Button>
              </div>
            </Form.Item>
            <Form.Item name="newPassword" label="新密码" rules={[{ required: true, message: '请输入新密码' }, { min: 6, message: '密码至少 6 位' }]}>
              <Input.Password size="large" placeholder="请输入新密码" autoComplete="new-password" />
            </Form.Item>
            <Form.Item
              name="confirmPassword"
              label="确认密码"
              dependencies={['newPassword']}
              rules={[
                { required: true, message: '请确认密码' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (!value || getFieldValue('newPassword') === value) return Promise.resolve();
                    return Promise.reject(new Error('两次输入的密码不一致'));
                  },
                }),
              ]}
            >
              <Input.Password size="large" placeholder="请再次输入新密码" autoComplete="new-password" />
            </Form.Item>
            <Button type="primary" block size="large" htmlType="submit" loading={submitting}>
              重置密码
            </Button>
          </>
        )}

        <div className="cube-auth-form-footer-link">
          <Button type="link" onClick={() => navigate('/login')}>
            返回登录
          </Button>
        </div>
      </Form>
    </AuthLayout>
  );
}
