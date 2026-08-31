/**
 * 个人中心（/profile）
 *
 * 对齐 MVC 用户中心（/Admin/User/Info）：
 * - 用户卡：大头像 + 昵称 + 角色 + 在线状态
 * - 基础信息（可编辑）：头像上传 + 昵称/性别/生日/邮箱/手机，保存走 POST /Admin/User/Info
 * - 第三方授权：GET /Admin/User/Binds 展示绑定列表，/Sso/Bind|UnBind 绑定/解绑
 * - 账号操作：安全中心 / 退出登录
 */
import { useEffect, useState } from 'react';
import {
  App,
  Avatar,
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  Select,
  Space,
  Tag,
  Upload,
} from 'antd';
import {
  DisconnectOutlined,
  LinkOutlined,
  LogoutOutlined,
  SafetyOutlined,
  UploadOutlined,
  UserOutlined,
} from '@ant-design/icons';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { api } from '@/api';
import { useUserStore, logoutAndRedirect } from '@/stores/user';
import { useDashboard } from '@/hooks/useDashboard';
import type { BindsResult, UserInfo } from '@newlifex/api-core';

/** 性别枚举（对齐 XCode SexKinds：0 未知 / 1 男 / 2 女） */
const SEX_OPTIONS = [
  { value: 0, label: '未知' },
  { value: 1, label: '男' },
  { value: 2, label: '女' },
];

/** 第三方平台显示名兜底 */
function platformName(item: { name?: string; nickName?: string }): string {
  return item.nickName || item.name || '第三方';
}

export default function ProfilePage() {
  const { message, modal } = App.useApp();
  const navigate = useNavigate();
  const userInfo = useUserStore((s) => s.userInfo);
  const { data: wb } = useDashboard();
  const [form] = Form.useForm();

  // 当前用户详细资料（GET /Admin/User/Info，含验证状态/生日）
  const [profile, setProfile] = useState<UserInfo | null>(null);
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);

  // 第三方授权
  const [binds, setBinds] = useState<BindsResult | null>(null);
  const [unbinding, setUnbinding] = useState<string | null>(null);

  // 用户卡数据：工作台接口优先，降级 userInfo / profile
  const user = wb?.user;
  const wbProfile = wb?.profile;
  const displayName = user?.displayName || userInfo?.displayName || userInfo?.name || '用户';
  const userName = user?.name || userInfo?.name || '—';
  const roles = user?.roles?.join(' / ') || userInfo?.roleName || '用户';
  const online = wbProfile?.online;
  const avatar = profile?.avatar || userInfo?.avatar;

  // 加载资料 + 第三方绑定
  useEffect(() => {
    api.user
      .profile()
      .then((res) => {
        setProfile(res.data ?? null);
        form.setFieldsValue({
          displayName: res.data?.displayName,
          sex: res.data?.sex,
          birthday: res.data?.birthday ? dayjs(res.data.birthday) : undefined,
          mail: res.data?.mail,
          mobile: res.data?.mobile,
        });
      })
      .catch(() => {});
    api.user
      .binds()
      .then((res) => setBinds(res.data ?? null))
      .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /** 保存基础信息：POST /Admin/User/Info */
  const handleSave = async () => {
    const values = await form.validateFields();
    setSaving(true);
    try {
      // 后端模型校验要求携带 名称(name)/更新者(updateUser) 字段，一并提交
      const userName = profile?.name || userInfo?.name || '';
      const data: Record<string, unknown> = {
        id: profile?.id ?? userInfo?.id,
        name: userName,
        displayName: values.displayName,
        updateUser: values.displayName || userName,
      };
      if (values.sex !== undefined) data.sex = values.sex;
      if (values.birthday) data.birthday = values.birthday.format('YYYY-MM-DDTHH:mm:ss');
      if (values.mail) data.mail = values.mail;
      if (values.mobile) data.mobile = values.mobile;
      if (avatar) data.avatar = avatar;

      await api.user.updateProfile(data);
      message.success('保存成功');
      // 刷新本地用户信息（顶栏昵称/头像即时更新）
      void useUserStore.getState().fetchUserInfo();
      void api.user
        .profile()
        .then((res) => setProfile(res.data ?? null))
        .catch(() => {});
    } catch (err) {
      // 字段级/业务错误已由全局拦截提示，这里兜底
      message.error((err as Error)?.message || '保存失败');
    } finally {
      setSaving(false);
    }
  };

  /** 头像上传：POST /Admin/User/UploadFile → filePath 回填 avatar，随保存提交 */
  const handleAvatarUpload = async (file: File) => {
    setUploading(true);
    try {
      const res = await api.page.uploadFile('/Admin/User', file, { id: profile?.id ?? userInfo?.id });
      const filePath = (res.data as { filePath?: string } | undefined)?.filePath;
      if (filePath) {
        setProfile((p) => (p ? { ...p, avatar: filePath } : p));
        message.success('头像已上传，点击保存生效');
      } else {
        message.error('上传失败：未返回文件路径');
      }
    } catch (err) {
      message.error((err as Error)?.message || '上传失败');
    } finally {
      setUploading(false);
    }
    return false; // 阻止自动上传
  };

  /** 解绑第三方 */
  const handleUnbind = (item: { name?: string; nickName?: string }) => {
    const name = item.name || '';
    const label = platformName(item);
    modal.confirm({
      title: '取消绑定',
      content: `确定取消绑定「${label}」吗？取消后需重新授权才能登录。`,
      okText: '取消绑定',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        setUnbinding(name);
        try {
          await api.user.unbind(name);
          message.success('已取消绑定');
          const res = await api.user.binds();
          setBinds(res.data ?? null);
        } catch (err) {
          message.error((err as Error)?.message || '解绑失败');
        } finally {
          setUnbinding(null);
        }
      },
    });
  };

  /** 绑定第三方：新窗口走 /Sso/Bind/{name} OAuth 授权流，返回后刷新列表 */
  const handleBind = (item: { name?: string; nickName?: string }) => {
    const name = item.name || '';
    window.open(`${window.location.origin}/Sso/Bind/${name}`, '_blank', 'noopener');
  };

  const connects = binds?.connects ?? [];
  const platforms = binds?.oAuthItems ?? [];

  return (
    <div style={{ maxWidth: 720, margin: '0 auto' }}>
      {/* 用户卡：大头像 + 昵称 + 角色 + 在线状态 + 登录统计 */}
      <Card style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
          <Avatar size={72} src={avatar} icon={<UserOutlined />} />
          <div style={{ minWidth: 0 }}>
            <Space size={8}>
              <span style={{ fontSize: 20, fontWeight: 600 }}>{displayName}</span>
              {online !== undefined && (
                <Tag color={online ? 'success' : 'default'}>{online ? '在线' : '离线'}</Tag>
              )}
            </Space>
            <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 8 }}>
              <Tag color="blue">{roles}</Tag>
              <span style={{ color: 'var(--cube-text-muted)' }}>{userName}</span>
            </div>
          </div>
        </div>
        {/* 登录统计（对齐 MVC 用户卡：登录次数/最近登录/登录IP/注册时间） */}
        <div
          style={{
            marginTop: 16,
            paddingTop: 14,
            borderTop: '1px solid var(--cube-border-soft)',
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))',
            gap: 12,
          }}
        >
          {[
            { label: '登录次数', value: profile?.logins ?? userInfo?.logins },
            { label: '最近登录', value: profile?.lastLogin ?? userInfo?.lastLogin },
            { label: '登录 IP', value: profile?.lastLoginIP ?? userInfo?.lastLoginIP },
            { label: '注册时间', value: profile?.registerTime },
          ].map((item) => (
            <div key={item.label}>
              <div style={{ color: 'var(--cube-text-muted)', fontSize: 12 }}>{item.label}</div>
              <div style={{ marginTop: 2, fontSize: 14, fontWeight: 600, color: 'var(--cube-text)' }}>
                {item.value ? String(item.value) : '—'}
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* 基础信息：可编辑（对齐 MVC Info 编辑表单） */}
      <Card title="基础信息" style={{ marginBottom: 16 }}>
        <Form form={form} layout="vertical" style={{ maxWidth: 520 }} onFinish={handleSave}>
          {/* 头像上传 */}
          <Form.Item label="头像">
            <Space size={16} align="center">
              <Avatar size={56} src={avatar} icon={<UserOutlined />} />
              <Upload
                accept="image/*"
                showUploadList={false}
                beforeUpload={(file) => {
                  void handleAvatarUpload(file as unknown as File);
                  return false;
                }}
              >
                <Button icon={<UploadOutlined />} loading={uploading}>
                  上传头像
                </Button>
              </Upload>
            </Space>
          </Form.Item>

          <Form.Item label="昵称" name="displayName" rules={[{ required: true, message: '请输入昵称' }]}>
            <Input maxLength={20} placeholder="请输入昵称" />
          </Form.Item>

          <Form.Item label="性别" name="sex">
            <Select options={SEX_OPTIONS} placeholder="请选择性别" />
          </Form.Item>

          <Form.Item label="生日" name="birthday">
            <DatePicker style={{ width: '100%' }} placeholder="请选择生日" />
          </Form.Item>

          <Form.Item
            label="邮箱"
            name="mail"
            rules={[{ type: 'email', message: '邮箱格式不正确' }]}
            extra={profile?.mailVerified ? <Tag color="success">已验证</Tag> : <Tag color="warning">未验证</Tag>}
          >
            <Input placeholder="请输入邮箱" />
          </Form.Item>

          <Form.Item
            label="手机"
            name="mobile"
            extra={profile?.mobileVerified ? <Tag color="success">已验证</Tag> : <Tag color="warning">未验证</Tag>}
          >
            <Input placeholder="请输入手机号" />
          </Form.Item>

          <Button type="primary" htmlType="submit" loading={saving}>
            保存
          </Button>
        </Form>
      </Card>

      {/* 第三方授权（对齐 MVC Binds 页） */}
      <Card title="第三方授权" style={{ marginBottom: 16 }}>
        {platforms.length === 0 ? (
          <div style={{ color: 'var(--cube-text-muted)' }}>未配置第三方登录平台</div>
        ) : (
          <div style={{ display: 'grid', gap: 12 }}>
            {platforms.map((item) => {
              const name = item.name || '';
              const uc = connects.find((c) => c.provider?.toLowerCase() === name.toLowerCase() && c.enable);
              const label = platformName(item);
              return (
                <div
                  key={name}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 12,
                    padding: '10px 12px',
                    borderRadius: 8,
                    border: '1px solid var(--cube-border-soft)',
                    background: 'var(--cube-surface-muted)',
                  }}
                >
                  <Avatar size={32} src={item.logo} icon={<LinkOutlined />} />
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 600 }}>{label}</div>
                    {uc ? (
                      <div style={{ color: 'var(--cube-text-muted)', fontSize: 12 }}>
                        {uc.nickName || uc.provider}
                        {uc.createTime ? ` · 绑定于 ${String(uc.createTime).slice(0, 10)}` : ''}
                      </div>
                    ) : (
                      <div style={{ color: 'var(--cube-text-muted)', fontSize: 12 }}>未绑定</div>
                    )}
                  </div>
                  {uc ? (
                    <Button
                      size="small"
                      danger
                      icon={<DisconnectOutlined />}
                      loading={unbinding === name}
                      onClick={() => handleUnbind(item)}
                    >
                      取消绑定
                    </Button>
                  ) : (
                    <Button size="small" type="primary" icon={<LinkOutlined />} onClick={() => handleBind(item)}>
                      绑定
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        )}
        <div style={{ marginTop: 12, color: 'var(--cube-text-muted)', fontSize: 12 }}>
          绑定第三方平台后，可直接使用第三方账号一键登录；取消绑定后需重新授权。
        </div>
      </Card>

      {/* 账号操作：安全中心 / 退出登录 */}
      <Card title="账号操作">
        <Space wrap>
          <Button type="primary" icon={<SafetyOutlined />} onClick={() => navigate('/profile/security')}>
            安全中心
          </Button>
          <Button danger icon={<LogoutOutlined />} onClick={() => void logoutAndRedirect()}>
            退出登录
          </Button>
        </Space>
      </Card>
    </div>
  );
}
