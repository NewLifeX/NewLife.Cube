/**
 * 忘记密码页（对齐 Vue 皮肤忘记密码流程）
 *
 * 流程：输入邮箱/手机号 → 发送验证码 → 输入验证码 + 新密码 → 重置 → 跳登录。
 * 使用 @cube/auth-logic/zustand 的 ForgotPassword store。
 */
import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Input, Radio, Space, message } from 'antd';
import { useNavigate } from 'react-router-dom';
import { createZustandForgotPasswordStore } from '@cube/auth-logic/zustand';
import { api } from '@/api';

const useForgotStore = createZustandForgotPasswordStore(api);

export default function ForgotPasswordPage() {
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
      <Card style={{ width: 420, borderRadius: 12 }}>
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <div style={{ fontSize: 40 }}>🔒</div>
          <h1 style={{ margin: 0, fontSize: 22 }}>找回密码</h1>
          <p style={{ color: '#888' }}>通过邮箱或手机验证码重置密码</p>
        </div>

        <Form form={form} layout="vertical" requiredMark={false}>
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
              <Button type="primary" block size="large" loading={sending} onClick={() => void handleSendCode()}>
                {countdown > 0 ? `${countdown}s 后重发` : '发送验证码'}
              </Button>
            </>
          )}

          {step === 'reset' && (
            <>
              <Alert type="info" showIcon message={`验证码已发送至 ${account}`} style={{ marginBottom: 16 }} />
              <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
                <Space.Compact style={{ width: '100%' }}>
                  <Input size="large" placeholder="验证码" maxLength={6} />
                  <Button
                    size="large"
                    disabled={countdown > 0 || sending}
                    onClick={() => void useForgotStore.getState().resendCode(account, channel)}
                  >
                    {countdown > 0 ? `${countdown}s` : '重发'}
                  </Button>
                </Space.Compact>
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
              <Button type="primary" block size="large" loading={submitting} onClick={() => void handleReset()}>
                重置密码
              </Button>
            </>
          )}

          <Button type="link" block onClick={() => navigate('/login')} style={{ marginTop: 8 }}>
            返回登录
          </Button>
        </Form>
      </Card>
    </div>
  );
}
