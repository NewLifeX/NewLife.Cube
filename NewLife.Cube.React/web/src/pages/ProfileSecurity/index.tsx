/**
 * 安全中心（对齐 Vue 皮肤 pages/ProfileSecurity.vue + MFA 增强）
 *
 * 1. 邮箱/手机验证状态（/Auth/Info 的 mailVerified/mobileVerified）
 * 2. 验证/更换联系方式（发码 /Auth/SendCode action=bind，提交 /Auth/VerifyContact）
 * 3. MFA 双因素认证：状态/绑定（二维码+密钥）/激活（备用码）/解绑
 */
import { useEffect, useState } from 'react';
import { Alert, App, Button, Card, Descriptions, Form, Input, Modal, Space, Tag } from 'antd';
import { api } from '@/api';
import { useUserStore } from '@/stores/user';

type Panel = 'mail' | 'sms' | null;

export default function ProfileSecurityPage() {
  const { message } = App.useApp();
  const [form] = Form.useForm();
  const userInfo = useUserStore((s) => s.userInfo);
  const [panel, setPanel] = useState<Panel>(null);
  const [sending, setSending] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  // MFA 状态
  const [mfaEnabled, setMfaEnabled] = useState(false);
  const [mfaAvailable, setMfaAvailable] = useState(false);
  const [mfaSetup, setMfaSetup] = useState<{ qrCodeUri: string; secret: string } | null>(null);
  const [backupCodes, setBackupCodes] = useState<string[] | null>(null);
  const [mfaCode, setMfaCode] = useState('');
  const [mfaBusy, setMfaBusy] = useState(false);
  const [disableOpen, setDisableOpen] = useState(false);
  const [disableCode, setDisableCode] = useState('');

  useEffect(() => {
    api.user
      .mfaStatus()
      .then((res) => {
        setMfaEnabled(!!res.data?.enabled);
        setMfaAvailable(!!res.data?.available);
      })
      .catch(() => {
        // 后端不支持 MFA 时静默
      });
  }, []);

  const handleSendVerifyCode = async () => {
    const values = await form.validateFields(['account']);
    setSending(true);
    try {
      await api.user.sendCode({
        channel: panel === 'mail' ? 'Mail' : 'Sms',
        username: values.account,
        action: 'bind',
      });
      message.success('验证码已发送');
    } catch (err) {
      message.error((err as Error)?.message || '发送失败');
    } finally {
      setSending(false);
    }
  };

  const handleVerify = async () => {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      const res = await api.user.verifyContact({
        channel: panel === 'mail' ? 'Mail' : 'Sms',
        account: values.account,
        code: values.code,
      });
      message.success('验证成功');
      setPanel(null);
      form.resetFields();
      // 刷新用户信息
      void useUserStore.getState().fetchUserInfo();
    } catch (err) {
      message.error((err as Error)?.message || '验证失败');
    } finally {
      setSubmitting(false);
    }
  };

  // ── MFA ─────────────────────────────────────────────
  const handleMfaSetup = async () => {
    setMfaBusy(true);
    try {
      const res = await api.user.mfaSetup();
      setMfaSetup(res.data);
    } catch (err) {
      message.error((err as Error)?.message || '初始化失败');
    } finally {
      setMfaBusy(false);
    }
  };

  const handleMfaActivate = async () => {
    if (!mfaCode) return;
    setMfaBusy(true);
    try {
      const res = await api.user.mfaActivate(mfaCode);
      setBackupCodes(res.data?.backupCodes ?? []);
      setMfaEnabled(true);
      setMfaSetup(null);
      message.success('MFA 已开启');
    } catch (err) {
      message.error((err as Error)?.message || '激活失败');
    } finally {
      setMfaBusy(false);
    }
  };

  const handleMfaDisable = async () => {
    if (!disableCode) return;
    setMfaBusy(true);
    try {
      await api.user.mfaDisable(disableCode);
      setMfaEnabled(false);
      setDisableOpen(false);
      setDisableCode('');
      message.success('MFA 已关闭');
    } catch (err) {
      message.error((err as Error)?.message || '关闭失败');
    } finally {
      setMfaBusy(false);
    }
  };

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      <Card title="联系方式验证" style={{ marginBottom: 16 }}>
        <Descriptions
          column={1}
          size="medium"
          items={[
            {
              key: 'mail',
              label: '邮箱',
              children: (
                <Space>
                  <span>{userInfo?.mail || '未绑定'}</span>
                  {userInfo?.mail && (
                    <Tag color={userInfo.mailVerified ? 'success' : 'warning'}>
                      {userInfo.mailVerified ? '已验证' : '未验证'}
                    </Tag>
                  )}
                  {userInfo?.mail && (
                    <Button size="small" onClick={() => setPanel(panel === 'mail' ? null : 'mail')}>
                      {userInfo.mailVerified ? '更换' : '验证'}
                    </Button>
                  )}
                </Space>
              ),
            },
            {
              key: 'mobile',
              label: '手机号',
              children: (
                <Space>
                  <span>{userInfo?.mobile || '未绑定'}</span>
                  {userInfo?.mobile && (
                    <Tag color={userInfo.mobileVerified ? 'success' : 'warning'}>
                      {userInfo.mobileVerified ? '已验证' : '未验证'}
                    </Tag>
                  )}
                  {userInfo?.mobile && (
                    <Button size="small" onClick={() => setPanel(panel === 'sms' ? null : 'sms')}>
                      {userInfo.mobileVerified ? '更换' : '验证'}
                    </Button>
                  )}
                </Space>
              ),
            },
          ]}
        />

        {panel && (
          <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
            <Form.Item
              name="account"
              label={panel === 'mail' ? '新邮箱' : '新手机号'}
              rules={[
                { required: true, message: '请输入' },
                panel === 'mail'
                  ? { type: 'email', message: '邮箱格式不正确' }
                  : { pattern: /^1\d{10}$/, message: '手机号格式不正确' },
              ]}
            >
              <Input placeholder={panel === 'mail' ? '请输入新邮箱' : '请输入新手机号'} />
            </Form.Item>
            <Form.Item name="code" label="验证码" rules={[{ required: true, message: '请输入验证码' }]}>
              <Space.Compact style={{ width: '100%' }}>
                <Input placeholder="验证码" maxLength={6} />
                <Button loading={sending} onClick={() => void handleSendVerifyCode()}>
                  发送验证码
                </Button>
              </Space.Compact>
            </Form.Item>
            <Button type="primary" loading={submitting} onClick={() => void handleVerify()}>
              验证
            </Button>
          </Form>
        )}
      </Card>

      <Card title="双重验证（MFA）">
        {!mfaAvailable ? (
          <Alert type="info" showIcon message="当前系统未开放 MFA 功能" />
        ) : (
          <Space orientation="vertical" style={{ width: '100%' }}>
            {mfaEnabled ? (
              <Alert
                type="success"
                showIcon
                message="MFA 已开启"
                description="登录时需要输入身份验证器动态验证码。"
                action={<Button danger size="small" onClick={() => setDisableOpen(true)}>关闭 MFA</Button>}
              />
            ) : (
              <Alert
                type="warning"
                showIcon
                message="MFA 未开启"
                description="开启后登录将需要身份验证器动态验证码，提升账号安全性。"
                action={
                  <Button type="primary" size="small" loading={mfaBusy} onClick={() => void handleMfaSetup()}>
                    开启 MFA
                  </Button>
                }
              />
            )}

            {mfaSetup && (
              <Card size="small" title="绑定 MFA">
                <Space orientation="vertical" style={{ width: '100%' }}>
                  <img
                    src={mfaSetup.qrCodeUri}
                    alt="MFA 二维码"
                    style={{ width: 160, height: 160, alignSelf: 'center' }}
                  />
                  <Alert
                    type="info"
                    showIcon
                    message="使用身份验证器 App 扫码，或手动输入密钥"
                    description={mfaSetup.secret}
                  />
                  <Space.Compact style={{ width: '100%' }}>
                    <Input
                      placeholder="输入 App 中显示的 6 位验证码"
                      maxLength={6}
                      value={mfaCode}
                      onChange={(e) => setMfaCode(e.target.value)}
                    />
                    <Button type="primary" loading={mfaBusy} onClick={() => void handleMfaActivate()}>
                      激活
                    </Button>
                  </Space.Compact>
                </Space>
              </Card>
            )}

            {backupCodes && (
              <Alert
                type="warning"
                showIcon
                message="请妥善保存备用验证码"
                description={
                  <div>
                    每个备用码仅可使用一次，用于无法使用验证器时恢复登录：
                    <pre style={{ background: '#f5f5f5', padding: 8, borderRadius: 4 }}>{backupCodes.join('\n')}</pre>
                  </div>
                }
                action={<Button size="small" onClick={() => setBackupCodes(null)}>我已保存</Button>}
              />
            )}
          </Space>
        )}
      </Card>

      <Modal
        title="关闭 MFA"
        open={disableOpen}
        onCancel={() => setDisableOpen(false)}
        onOk={() => void handleMfaDisable()}
        confirmLoading={mfaBusy}
        okText="关闭"
        okButtonProps={{ danger: true }}
      >
        <p>请输入当前身份验证器中的 6 位动态验证码以确认关闭：</p>
        <Input
          placeholder="6 位动态验证码"
          maxLength={6}
          value={disableCode}
          onChange={(e) => setDisableCode(e.target.value)}
        />
      </Modal>
    </div>
  );
}
