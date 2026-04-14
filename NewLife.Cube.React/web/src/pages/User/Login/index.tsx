import Footer from '@/components/Footer';
import { login, getLoginConfig, getSiteInfo, sendCode, loginByCode } from '@/services/ant-design-pro/api';
import {
  LockOutlined,
  MailOutlined,
  MobileOutlined,
  UserOutlined,
} from '@ant-design/icons';
import {
  LoginForm,
  ProFormCaptcha,
  ProFormText,
} from '@ant-design/pro-components';
import { useEmotionCss } from '@ant-design/use-emotion-css';
import { history, useModel, Helmet } from '@umijs/max';
import { Alert, message, Tabs, Avatar, Space } from 'antd';
import React, { useState, useEffect, useRef } from 'react';
import { flushSync } from 'react-dom';
import { useLocation, useNavigate } from '@umijs/max';
import { setToken } from '@/utils/token';

const LoginMessage: React.FC<{ content: string }> = ({ content }) => (
  <Alert style={{ marginBottom: 24 }} message={content} type="error" showIcon />
);

const Login: React.FC = () => {
  const [userLoginState, setUserLoginState] = useState<API.LoginResult>({});
  const [type, setType] = useState<string>('account');
  const [loginConfig, setLoginConfig] = useState<API.LoginConfig>({
    allowLogin: true,
    allowRegister: false,
    enableSms: false,
    enableMail: false,
    providers: [],
  });
  const [siteInfo, setSiteInfo] = useState<API.SiteInfo>({});
  const { initialState, setInitialState } = useModel('@@initialState');
  const navigate = useNavigate();
  const location = useLocation();
  const token = (location as any).hash?.replace('#token=', '') || '';

  const containerClassName = useEmotionCss(() => ({
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'auto',
    backgroundImage: "url('https://mdn.alipayobjects.com/yuyan_qk0oxh/afts/img/V-_oS6r-i7wAAAAAAAAAAAAAFl94AQBr')",
    backgroundSize: '100% 100%',
  }));

  const fetchUserInfo = async () => {
    const userInfo = await initialState?.fetchUserInfo?.();
    if (userInfo) {
      flushSync(() => {
        setInitialState((s) => ({ ...s, currentUser: userInfo }));
      });
    }
  };

  useEffect(() => {
    Promise.all([getLoginConfig(), getSiteInfo()])
      .then(([configRes, siteRes]) => {
        if (configRes?.data) {
          setLoginConfig(configRes.data);
          if (!configRes.data.allowLogin) {
            if (configRes.data.enableSms) setType('mobile');
            else if (configRes.data.enableMail) setType('email');
          }
        }
        if (siteRes?.data) setSiteInfo(siteRes.data);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!token) return;
    const fetch = async () => {
      setToken(token);
      const hide = message.loading('正在登录...');
      await fetchUserInfo();
      hide();
      const urlParams = new URL(window.location.href).searchParams;
      history.push(urlParams.get('redirect') || '/');
    };
    fetch();
  }, [token]);

  const handleSubmit = async (values: API.LoginParams) => {
    try {
      const msg = await login({ ...values, type });
      if (msg.status === 'ok') {
        message.success('登录成功！');
        await fetchUserInfo();
        const urlParams = new URL(window.location.href).searchParams;
        history.push(urlParams.get('redirect') || '/');
        return;
      }
      setUserLoginState(msg);
    } catch {
      message.error('登录失败，请重试！');
    }
  };

  const { status, type: loginType } = userLoginState;

  const tabItems: { key: string; label: string }[] = [];
  if (loginConfig.allowLogin !== false) tabItems.push({ key: 'account', label: '账户密码登录' });
  if (loginConfig.enableSms) tabItems.push({ key: 'mobile', label: '手机号登录' });
  if (loginConfig.enableMail) tabItems.push({ key: 'email', label: '邮箱验证码' });
  if (tabItems.length === 0) tabItems.push({ key: 'account', label: '账户密码登录' });

  const logoUrl = siteInfo.loginLogo || loginConfig.logo || '';
  const displayName = siteInfo.displayName || loginConfig.displayName || 'NewLife Cube';

  const oauthActions = loginConfig.providers?.length
    ? [
        <span key="loginWith">其他登录方式</span>,
        <Space key="icons" size={8}>
          {loginConfig.providers.map((provider) => (
            <div key={provider.name} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', cursor: 'pointer' }}
              onClick={() => { window.location.href = `/Sso/Login/${provider.name}`; }}
              title={provider.nickName || provider.name}
            >
              <Avatar
                src={provider.logo}
                size={36}
                alt={provider.nickName || provider.name}
              >
                {!provider.logo ? (provider.nickName || provider.name).charAt(0).toUpperCase() : undefined}
              </Avatar>
              <span style={{ fontSize: 11, marginTop: 2, maxWidth: 56, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {provider.nickName || provider.name}
              </span>
            </div>
          ))}
        </Space>,
      ]
    : undefined;

  return (
    <div className={containerClassName}>
      <Helmet>
        <title>登录 - {displayName}</title>
      </Helmet>
      <div style={{ flex: '1', padding: '32px 0' }}>
        <LoginForm
          contentStyle={{ minWidth: 280, maxWidth: '75vw' }}
          logo={logoUrl ? <img alt="logo" src={logoUrl} /> : undefined}
          title={displayName}
          subTitle={loginConfig.loginTip || ''}
          initialValues={{ autoLogin: true }}
          actions={oauthActions}
          onFinish={async (values) => { await handleSubmit(values as API.LoginParams); }}
        >
          {tabItems.length > 1 && (
            <Tabs activeKey={type} onChange={setType} centered items={tabItems} />
          )}

          {/* 密码登录 */}
          {status === 'error' && loginType === 'account' && <LoginMessage content="账户或密码错误" />}
          {type === 'account' && (
            <>
              <ProFormText name="username" fieldProps={{ size: 'large', prefix: <UserOutlined /> }} placeholder="用户名"
                rules={[{ required: true, message: '请输入用户名!' }]} />
              <ProFormText.Password name="password" fieldProps={{ size: 'large', prefix: <LockOutlined /> }} placeholder="密码"
                rules={[{ required: true, message: '请输入密码！' }]} />
            </>
          )}

          {/* 手机验证码登录 */}
          {status === 'error' && loginType === 'mobile' && <LoginMessage content="验证码错误" />}
          {type === 'mobile' && (
            <>
              <ProFormText fieldProps={{ size: 'large', prefix: <MobileOutlined /> }} name="mobile" placeholder="手机号"
                rules={[{ required: true, message: '请输入手机号！' }, { pattern: /^1\d{10}$/, message: '手机号格式错误！' }]} />
              <ProFormCaptcha
                fieldProps={{ size: 'large', prefix: <LockOutlined /> }}
                captchaProps={{ size: 'large' }}
                placeholder="请输入验证码"
                captchaTextRender={(timing, count) => timing ? `${count} 秒后重新获取` : '获取验证码'}
                name="captcha"
                rules={[{ required: true, message: '请输入验证码！' }]}
                onGetCaptcha={async (mobile) => {
                  await sendCode({ channel: 'Sms', username: mobile, action: 'login' });
                  message.success('验证码发送成功！');
                }}
              />
            </>
          )}

          {/* 邮箱验证码登录 */}
          {type === 'email' && (
            <>
              <ProFormText fieldProps={{ size: 'large', prefix: <MailOutlined /> }} name="email" placeholder="邮箱地址"
                rules={[{ required: true, message: '请输入邮箱！' }, { type: 'email', message: '邮箱格式错误！' }]} />
              <ProFormCaptcha
                fieldProps={{ size: 'large', prefix: <LockOutlined /> }}
                captchaProps={{ size: 'large' }}
                placeholder="请输入验证码"
                captchaTextRender={(timing, count) => timing ? `${count} 秒后重新获取` : '获取验证码'}
                name="captcha"
                rules={[{ required: true, message: '请输入验证码！' }]}
                onGetCaptcha={async (email) => {
                  await sendCode({ channel: 'Mail', username: email, action: 'login' });
                  message.success('验证码已发送至您的邮箱！');
                }}
              />
            </>
          )}

          <div style={{ marginBottom: 24 }}>
            {loginConfig.allowRegister && (
              <a style={{ float: 'right' }} onClick={() => navigate('/user/register')}>注册账号</a>
            )}
          </div>
        </LoginForm>

        {/* 版权信息 */}
        {(siteInfo.copyright || siteInfo.registration) && (
          <div style={{ textAlign: 'center', fontSize: 12, color: '#bbb', marginTop: 8 }}>
            {siteInfo.copyright && <div dangerouslySetInnerHTML={{ __html: siteInfo.copyright }} />}
            {siteInfo.registration && (
              <div>
                <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer" style={{ color: '#bbb' }}>
                  {siteInfo.registration}
                </a>
              </div>
            )}
          </div>
        )}
      </div>
      <Footer />
    </div>
  );
};

export default Login;


const LoginMessage: React.FC<{
  content: string;
}> = ({ content }) => {
  return (
    <Alert
      style={{
        marginBottom: 24,
      }}
      message={content}
      type="error"
      showIcon
    />
  );
};

const Login: React.FC = () => {
  const [userLoginState, setUserLoginState] = useState<API.LoginResult>({});
  const [type, setType] = useState<string>('account');
  const [loginConfig, setLoginConfig] = useState<API.LoginConfig>({
    allowLogin: true,
    allowRegister: true,
    enableSms: false,
    enableMail: false,
    providers: [],
  });
  const { initialState, setInitialState } = useModel('@@initialState');
  let location = useLocation();
  let token = location.hash.replace('#token=', '');

  const containerClassName = useEmotionCss(() => {
    return {
      display: 'flex',
      flexDirection: 'column',
      height: '100vh',
      overflow: 'auto',
      backgroundImage:
        "url('https://mdn.alipayobjects.com/yuyan_qk0oxh/afts/img/V-_oS6r-i7wAAAAAAAAAAAAAFl94AQBr')",
      backgroundSize: '100% 100%',
    };
  });

  const fetchUserInfo = async () => {
    const userInfo = await initialState?.fetchUserInfo?.();
    if (userInfo) {
      flushSync(() => {
        setInitialState((s) => ({
          ...s,
          currentUser: userInfo,
        }));
      });
    }
  };

  useEffect(() => {
    // 加载登录配置
    getLoginConfig()
      .then((res) => {
        if (res?.data) {
          setLoginConfig(res.data);
          // 如果不允许密码登录但允许短信登录，默认切到手机 Tab
          if (!res.data.allowLogin && res.data.enableSms) {
            setType('mobile');
          }
        }
      })
      .catch(() => {
        // 加载失败使用默认值
      });
  }, []);

  useEffect(() => {
    const fetch = async () => {
      // 存储 token
      setToken(token);
      const hide = message.loading('正在登录...');
      await fetchUserInfo();
      hide();
      const urlParams = new URL(window.location.href).searchParams;
      history.push(urlParams.get('redirect') || '/');
    };
    if (token) {
      fetch();
    }
  }, [token]);

  const handleSubmit = async (values: API.LoginParams) => {
    try {
      // 登录
      const msg = await login({ ...values, type });
      if (msg.status === 'ok') {
        message.success('登录成功！');
        await fetchUserInfo();
        const urlParams = new URL(window.location.href).searchParams;
        history.push(urlParams.get('redirect') || '/');
        return;
      }
      console.log(msg);
      // 如果失败去设置用户错误信息
      setUserLoginState(msg);
    } catch (error) {
      console.log(error);
      message.error('登录失败，请重试！');
    }
  };
  const { status, type: loginType } = userLoginState;

  // 构建 Tab 列表
  const tabItems: { key: string; label: string }[] = [];
  if (loginConfig.allowLogin !== false) {
    tabItems.push({ key: 'account', label: '账户密码登录' });
  }
  if (loginConfig.enableSms) {
    tabItems.push({ key: 'mobile', label: '手机号登录' });
  }
  // 如果都没有，至少显示账号登录
  if (tabItems.length === 0) {
    tabItems.push({ key: 'account', label: '账户密码登录' });
  }

  // 构建 OAuth 提供商图标
  const oauthActions =
    loginConfig.providers && loginConfig.providers.length > 0
      ? [
          <span key="loginWith">其他登录方式</span>,
          <Space key="icons" size={8}>
            {loginConfig.providers.map((provider) => (
              <Avatar
                key={provider.name}
                src={provider.logo || 'https://newlifex.com/logo.png'}
                style={{ cursor: 'pointer' }}
                alt={provider.nickName || provider.name}
                onClick={() => {
                  const redirectUri = encodeURIComponent(window.location.href);
                  const url =
                    provider.loginUrl ||
                    `${API_URL}/Sso/Login?name=${provider.name}&source=front-end&redirect_uri=${redirectUri}`;
                  window.location.href = url;
                }}
              />
            ))}
          </Space>,
        ]
      : undefined;

  const displayName = loginConfig.displayName || 'NewLife Cube';
  const logoUrl = loginConfig.logo || 'https://newlifex.com/logo.png';

  return (
    <div className={containerClassName}>
      <Helmet>
        <title>登录 - {displayName}</title>
      </Helmet>
      <div
        style={{
          flex: '1',
          padding: '32px 0',
        }}
      >
        <LoginForm
          contentStyle={{
            minWidth: 280,
            maxWidth: '75vw',
          }}
          logo={<img alt="logo" src={logoUrl} />}
          title={displayName}
          subTitle={loginConfig.loginTip || ''}
          initialValues={{
            autoLogin: true,
          }}
          actions={oauthActions}
          onFinish={async (values) => {
            await handleSubmit(values as API.LoginParams);
          }}
        >
          {tabItems.length > 1 ? (
            <Tabs activeKey={type} onChange={setType} centered items={tabItems} />
          ) : null}

          {status === 'error' && loginType === 'account' && (
            <LoginMessage content="账户或密码错误" />
          )}
          {type === 'account' && (
            <>
              <ProFormText
                name="username"
                fieldProps={{
                  size: 'large',
                  prefix: <UserOutlined />,
                }}
                placeholder="用户名"
                rules={[
                  {
                    required: true,
                    message: '请输入用户名!',
                  },
                ]}
              />
              <ProFormText.Password
                name="password"
                fieldProps={{
                  size: 'large',
                  prefix: <LockOutlined />,
                }}
                placeholder="密码"
                rules={[
                  {
                    required: true,
                    message: '请输入密码！',
                  },
                ]}
              />
            </>
          )}

          {status === 'error' && loginType === 'mobile' && <LoginMessage content="验证码错误" />}
          {type === 'mobile' && (
            <>
              <ProFormText
                fieldProps={{
                  size: 'large',
                  prefix: <MobileOutlined />,
                }}
                name="mobile"
                placeholder="手机号"
                rules={[
                  {
                    required: true,
                    message: '请输入手机号！',
                  },
                  {
                    pattern: /^1\d{10}$/,
                    message: '手机号格式错误！',
                  },
                ]}
              />
              <ProFormCaptcha
                fieldProps={{
                  size: 'large',
                  prefix: <LockOutlined />,
                }}
                captchaProps={{
                  size: 'large',
                }}
                placeholder="请输入验证码"
                captchaTextRender={(timing, count) => {
                  if (timing) {
                    return `${count} 秒后重新获取`;
                  }
                  return '获取验证码';
                }}
                name="captcha"
                rules={[
                  {
                    required: true,
                    message: '请输入验证码！',
                  },
                ]}
                onGetCaptcha={async () => {
                  message.success('验证码发送成功');
                }}
              />
            </>
          )}
          <div
            style={{
              marginBottom: 24,
            }}
          >
            <ProFormCheckbox noStyle name="autoLogin">
              自动登录
            </ProFormCheckbox>
            {loginConfig.allowRegister && (
              <a style={{ float: 'right' }}>注册账号</a>
            )}
          </div>
        </LoginForm>
      </div>
      <Footer />
    </div>
  );
};

export default Login;
