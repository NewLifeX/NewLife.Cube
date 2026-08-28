/**
 * 账号激活页（对齐 Vue 皮肤 pages/ActivatePage.vue）
 *
 * 两种激活方式：
 * 1. 邮箱链接直达：/activate?token=xxx&account=xxx → GET /Auth/Activate
 * 2. 验证码激活：邮箱/手机号 + 验证码（发码 POST /Auth/SendCode action=activate）→ POST /Auth/Activate
 */
import { useEffect, useState } from 'react';
import { Alert, Button, Card, Form, Input, Radio, Space, message } from 'antd';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '@/api';

export default function ActivatePage() {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [done, setDone] = useState(false);
  const [linkError, setLinkError] = useState('');
  const [sending, setSending] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [channel, setChannel] = useState<'mail' | 'sms'>('mail');

  // 链接直达激活
  useEffect(() => {
    const token = params.get('token');
    const account = params.get('account');
    if (token && account) {
      api.user
        .activateByLink(token, account)
        .then(() => setDone(true))
        .catch((err: Error) => setLinkError(err.message || '激活链接无效或已过期'));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSendCode = async () => {
    const values = await form.validateFields(['account']);
    setSending(true);
    try {
      await api.user.sendCode({
        channel: channel === 'mail' ? 'Mail' : 'Sms',
        username: values.account,
        action: 'activate',
      });
      message.success('验证码已发送');
    } catch (err) {
      message.error((err as Error)?.message || '发送失败');
    } finally {
      setSending(false);
    }
  };

  const handleSubmit = async () => {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      const res = await api.user.activateByCode({
        channel: channel === 'mail' ? 'Mail' : 'Sms',
        account: values.account,
        code: values.code,
      });
      if (res.data?.activated) {
        message.success('激活成功');
        setDone(true);
      } else {
        message.warning('激活未完成，请检查验证码');
      }
    } catch (err) {
      message.error((err as Error)?.message || '激活失败');
    } finally {
      setSubmitting(false);
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
          <div style={{ fontSize: 40 }}>🔑</div>
          <h1 style={{ margin: 0, fontSize: 22 }}>账号激活</h1>
          <p style={{ color: '#888' }}>激活邮箱/手机后即可正常登录</p>
        </div>

        {done ? (
          <div style={{ textAlign: 'center' }}>
            <Alert type="success" showIcon message="激活成功" description="您的账号已激活，现在可以登录了" style={{ marginBottom: 16 }} />
            <Button type="primary" block onClick={() => navigate('/login')}>
              前往登录
            </Button>
          </div>
        ) : (
          <Form form={form} layout="vertical" requiredMark={false}>
            {linkError && (
              <Alert type="warning" showIcon message={linkError} description="您也可以在下方输入验证码完成激活" style={{ marginBottom: 16 }} />
            )}

            <Form.Item label="激活渠道">
              <Radio.Group value={channel} onChange={(e) => setChannel(e.target.value)}>
                <Radio.Button value="mail">邮箱激活</Radio.Button>
                <Radio.Button value="sms">手机激活</Radio.Button>
              </Radio.Group>
            </Form.Item>

            <Form.Item
              name="account"
              label={channel === 'mail' ? '邮箱' : '手机号'}
              rules={[
                { required: true, message: `请输入${channel === 'mail' ? '邮箱' : '手机号'}` },
                channel === 'mail'
                  ? { type: 'email', message: '邮箱格式不正确' }
                  : { pattern: /^1\d{10}$/, message: '手机号格式不正确' },
              ]}
            >
              <Input placeholder={channel === 'mail' ? '请输入邮箱' : '请输入手机号'} size="large" />
            </Form.Item>

            <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
              <Space.Compact style={{ width: '100%' }}>
                <Input placeholder="验证码" size="large" maxLength={6} />
                <Button size="large" loading={sending} onClick={() => void handleSendCode()}>
                  发送验证码
                </Button>
              </Space.Compact>
            </Form.Item>

            <Button type="primary" block size="large" loading={submitting} onClick={() => void handleSubmit()}>
              激 活
            </Button>
            <Button type="link" block onClick={() => navigate('/login')} style={{ marginTop: 8 }}>
              返回登录
            </Button>
          </Form>
        )}
      </Card>
    </div>
  );
}
