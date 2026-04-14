import React, { useState } from 'react';
import { history } from '@umijs/max';
import { Alert, Button, Form, Input, message, Radio, Steps } from 'antd';
import { sendCode, resetPassword } from '@/services/ant-design-pro/api';
import styles from './index.less';

const { Step } = Steps;

const ForgotPassword: React.FC = () => {
  const [step, setStep] = useState<0 | 1>(0);
  const [channel, setChannel] = useState<'Sms' | 'Mail'>('Sms');
  const [username, setUsername] = useState('');
  const [sending, setSending] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [error, setError] = useState('');
  const [form] = Form.useForm();

  let _timer: ReturnType<typeof setInterval> | null = null;

  const startCountdown = (seconds = 60) => {
    if (_timer) clearInterval(_timer);
    setCountdown(seconds);
    _timer = setInterval(() => {
      setCountdown((v) => {
        if (v <= 1) { clearInterval(_timer!); _timer = null; return 0; }
        return v - 1;
      });
    }, 1000);
  };

  const onSendCode = async () => {
    if (!username) { setError('请输入手机号或邮箱'); return; }
    setSending(true);
    setError('');
    try {
      await sendCode({ channel, username, action: 'reset' });
      message.success('验证码已发送');
      setStep(1);
      startCountdown();
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
      setError(msg);
    } finally {
      setSending(false);
    }
  };

  const onResend = async () => {
    if (countdown > 0) return;
    setSending(true);
    setError('');
    try {
      await sendCode({ channel, username, action: 'reset' });
      message.success('验证码已重新发送');
      startCountdown();
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '发送失败';
      setError(msg);
    } finally {
      setSending(false);
    }
  };

  const onConfirmReset = async (values: { code: string; newPassword: string; confirmPassword: string }) => {
    if (values.newPassword !== values.confirmPassword) {
      setError('两次密码不一致');
      return;
    }
    setSubmitting(true);
    setError('');
    try {
      await resetPassword({ username, code: values.code, newPassword: values.newPassword, confirmPassword: values.confirmPassword });
      message.success('密码重置成功，请重新登录');
      history.push('/user/login');
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : '重置失败，请重试';
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.card}>
        <h2 className={styles.title}>重置密码</h2>
        <Steps current={step} style={{ marginBottom: 32 }}>
          <Step title="发送验证码" />
          <Step title="设置新密码" />
        </Steps>

        {error && <Alert type="error" message={error} style={{ marginBottom: 16 }} />}

        {step === 0 ? (
          <Form layout="vertical">
            <Form.Item label="手机号或邮箱" required>
              <Input
                placeholder="请输入手机号或邮箱"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                size="large"
              />
            </Form.Item>
            <Form.Item label="验证渠道">
              <Radio.Group value={channel} onChange={(e) => setChannel(e.target.value)}>
                <Radio.Button value="Sms">短信</Radio.Button>
                <Radio.Button value="Mail">邮箱</Radio.Button>
              </Radio.Group>
            </Form.Item>
            <Form.Item>
              <Button type="primary" block size="large" loading={sending} onClick={onSendCode}>
                发送验证码
              </Button>
            </Form.Item>
            <Form.Item>
              <a onClick={() => history.push('/user/login')}>返回登录</a>
            </Form.Item>
          </Form>
        ) : (
          <Form form={form} layout="vertical" onFinish={onConfirmReset}>
            <Form.Item label="验证码" name="code" rules={[{ required: true, message: '请输入验证码' }]}>
              <Input
                placeholder="请输入验证码"
                size="large"
                addonAfter={
                  <Button type="link" disabled={countdown > 0} loading={sending} onClick={onResend} style={{ padding: 0 }}>
                    {countdown > 0 ? `${countdown}s` : '重新发送'}
                  </Button>
                }
              />
            </Form.Item>
            <Form.Item label="新密码" name="newPassword" rules={[{ required: true, message: '请输入新密码' }, { min: 6, message: '密码不少于6位' }]}>
              <Input.Password placeholder="请输入新密码" size="large" />
            </Form.Item>
            <Form.Item label="确认密码" name="confirmPassword" rules={[{ required: true, message: '请再次输入密码' }]}>
              <Input.Password placeholder="请再次输入新密码" size="large" />
            </Form.Item>
            <Form.Item>
              <Button type="primary" block size="large" loading={submitting} htmlType="submit">
                确认重置
              </Button>
            </Form.Item>
            <Form.Item>
              <a onClick={() => setStep(0)}>上一步</a>
              <a style={{ marginLeft: 16 }} onClick={() => history.push('/user/login')}>返回登录</a>
            </Form.Item>
          </Form>
        )}
      </div>
    </div>
  );
};

export default ForgotPassword;
