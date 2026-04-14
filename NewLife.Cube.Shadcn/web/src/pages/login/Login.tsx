import { useState, useEffect, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { LoginConfig, SiteInfo } from '@cube/api-core';

export default function Login() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { login } = useUserStore();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [smsUsername, setSmsUsername] = useState('');
  const [smsCode, setSmsCode] = useState('');
  const [mailUsername, setMailUsername] = useState('');
  const [mailCode, setMailCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [codeLoading, setCodeLoading] = useState(false);
  const [mailLoading, setMailLoading] = useState(false);
  const [smsCountdown, setSmsCountdown] = useState(0);
  const [mailCountdown, setMailCountdown] = useState(0);
  const [error, setError] = useState('');
  const [config, setConfig] = useState<LoginConfig | null>(null);
  const [siteInfo, setSiteInfo] = useState<SiteInfo | null>(null);
  const [activeTab, setActiveTab] = useState('password');

  useEffect(() => {
    Promise.all([api.user.getLoginConfig(), api.user.getSiteInfo()])
      .then(([configRes, siteRes]) => {
        setConfig(configRes.data);
        setSiteInfo(siteRes.data);
        if (configRes.data?.allowLogin === false) {
          if (configRes.data.enableSms) setActiveTab('sms');
          else if (configRes.data.enableMail) setActiveTab('email');
        }
      })
      .catch(() => {});
  }, []);

  const startCountdown = (type: 'sms' | 'mail') => {
    const setter = type === 'sms' ? setSmsCountdown : setMailCountdown;
    setter(60);
    const timer = setInterval(() => {
      setter((v) => {
        if (v <= 1) { clearInterval(timer); return 0; }
        return v - 1;
      });
    }, 1000);
  };

  const handleSendCode = async (channel: 'Sms' | 'Mail') => {
    const name = channel === 'Sms' ? smsUsername : mailUsername;
    if (!name) { setError(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址'); return; }
    setError('');
    try {
      await api.user.sendCode({ channel, username: name, action: 'login' });
      startCountdown(channel === 'Sms' ? 'sms' : 'mail');
    } catch (err: any) {
      setError(err?.message ?? '发送失败');
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!username || !password) { setError('请输入用户名和密码'); return; }
    setLoading(true);
    setError('');
    try {
      await login(username, password);
      navigate(params.get('redirect') ?? '/', { replace: true });
    } catch (err: any) {
      setError(err?.message ?? '登录失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCodeLogin = async (loginCategory: 1 | 2) => {
    const name = loginCategory === 1 ? smsUsername : mailUsername;
    const code = loginCategory === 1 ? smsCode : mailCode;
    const setLoad = loginCategory === 1 ? setCodeLoading : setMailLoading;
    if (!name) { setError(loginCategory === 1 ? '请输入手机号' : '请输入邮箱地址'); return; }
    if (!code) { setError('请输入验证码'); return; }
    setLoad(true);
    setError('');
    try {
      const res = await api.user.loginByCode({ username: name, password: code, loginCategory });
      if (res.data?.accessToken) api.tokenManager.setToken(res.data.accessToken);
      navigate(params.get('redirect') ?? '/', { replace: true });
    } catch (err: any) {
      setError(err?.message ?? '登录失败');
    } finally {
      setLoad(false);
    }
  };

  const logoSrc = siteInfo?.loginLogo || config?.logo || '';

  const hasTabs = (config?.allowLogin !== false ? 1 : 0) + (config?.enableSms ? 1 : 0) + (config?.enableMail ? 1 : 0) > 1;

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-indigo-500 to-purple-600 p-4">
      <div className="w-full max-w-sm rounded-lg border bg-card p-8 shadow-xl">
        {/* Logo + 标题 */}
        <div className="mb-6 text-center">
          {logoSrc && <img src={logoSrc} alt="logo" className="mx-auto mb-2 h-16 w-16 rounded-full object-contain" />}
          <h1 className="text-2xl font-bold text-card-foreground">{siteInfo?.displayName ?? config?.displayName ?? '魔方管理平台'}</h1>
          {config?.loginTip && <p className="mt-1 text-sm text-muted-foreground">{config.loginTip}</p>}
        </div>

        {error && (
          <div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            {error}
          </div>
        )}

        {hasTabs ? (
          <div>
            {/* Tab 导航栏 */}
            <div className="flex rounded-lg bg-muted p-1 mb-4">
              {config?.allowLogin !== false && (
                <button type="button"
                  className={`flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-all ${activeTab === 'password' ? 'bg-background shadow text-foreground' : 'text-muted-foreground hover:text-foreground'}`}
                  onClick={() => setActiveTab('password')}>密码登录</button>
              )}
              {config?.enableSms && (
                <button type="button"
                  className={`flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-all ${activeTab === 'sms' ? 'bg-background shadow text-foreground' : 'text-muted-foreground hover:text-foreground'}`}
                  onClick={() => setActiveTab('sms')}>手机验证码</button>
              )}
              {config?.enableMail && (
                <button type="button"
                  className={`flex-1 rounded-md px-3 py-1.5 text-sm font-medium transition-all ${activeTab === 'email' ? 'bg-background shadow text-foreground' : 'text-muted-foreground hover:text-foreground'}`}
                  onClick={() => setActiveTab('email')}>邮箱验证码</button>
              )}
            </div>

            {/* 密码登录 */}
            {activeTab === 'password' && config?.allowLogin !== false && (
              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="username">用户名</Label>
                  <Input id="username" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} placeholder="请输入用户名" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="password">密码</Label>
                  <Input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="请输入密码" />
                </div>
                <Button type="submit" className="w-full" disabled={loading}>{loading ? '登录中...' : '登 录'}</Button>
              </form>
            )}

            {/* 手机验证码 */}
            {activeTab === 'sms' && config?.enableSms && (
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label>手机号</Label>
                  <Input value={smsUsername} onChange={(e) => setSmsUsername(e.target.value)} placeholder="请输入手机号" />
                </div>
                <div className="space-y-2">
                  <Label>验证码</Label>
                  <div className="flex gap-2">
                    <Input value={smsCode} onChange={(e) => setSmsCode(e.target.value)} placeholder="请输入验证码" />
                    <Button type="button" variant="outline" disabled={smsCountdown > 0} onClick={() => handleSendCode('Sms')}>
                      {smsCountdown > 0 ? `${smsCountdown}s` : '获取验证码'}
                    </Button>
                  </div>
                </div>
                <Button className="w-full" disabled={codeLoading} onClick={() => handleCodeLogin(1)}>
                  {codeLoading ? '登录中...' : '登 录'}
                </Button>
              </div>
            )}

            {/* 邮箱验证码 */}
            {activeTab === 'email' && config?.enableMail && (
              <div className="space-y-4">
                <div className="space-y-2">
                  <Label>邮箱地址</Label>
                  <Input type="email" value={mailUsername} onChange={(e) => setMailUsername(e.target.value)} placeholder="请输入邮箱地址" />
                </div>
                <div className="space-y-2">
                  <Label>验证码</Label>
                  <div className="flex gap-2">
                    <Input value={mailCode} onChange={(e) => setMailCode(e.target.value)} placeholder="请输入验证码" />
                    <Button type="button" variant="outline" disabled={mailCountdown > 0} onClick={() => handleSendCode('Mail')}>
                      {mailCountdown > 0 ? `${mailCountdown}s` : '获取验证码'}
                    </Button>
                  </div>
                </div>
                <Button className="w-full" disabled={mailLoading} onClick={() => handleCodeLogin(2)}>
                  {mailLoading ? '登录中...' : '登 录'}
                </Button>
              </div>
            )}
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">用户名</Label>
              <Input id="username" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} placeholder="请输入用户名" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">密码</Label>
              <Input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="请输入密码" />
            </div>
            <Button type="submit" className="w-full" disabled={loading}>{loading ? '登录中...' : '登 录'}</Button>
          </form>
        )}

        {/* OAuth 第三方登录 */}
        {config?.providers?.length ? (
          <>
            <div className="relative my-4">
              <div className="absolute inset-0 flex items-center"><span className="w-full border-t" /></div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-card px-2 text-muted-foreground">其他登录方式</span>
              </div>
            </div>
            <div className="flex flex-wrap justify-center gap-3">
              {config.providers.map((p) => (
                <div key={p.name} className="flex flex-col items-center gap-1 cursor-pointer"
                  title={p.nickName ?? p.name}
                  onClick={() => { const redirect = new URLSearchParams(window.location.search).get('redirect') || '/'; window.location.href = `/Sso/Login/${p.name}?r=${encodeURIComponent(redirect)}`; }}
                >
                  {p.logo
                    ? <img src={p.logo} alt="" className="h-9 w-9 rounded-full object-contain" />
                    : <span className="flex h-9 w-9 items-center justify-center rounded-full bg-muted text-base font-semibold">
                        {(p.nickName ?? p.name).charAt(0).toUpperCase()}
                      </span>
                  }
                  <span className="max-w-[56px] overflow-hidden text-ellipsis whitespace-nowrap text-xs text-muted-foreground">
                    {p.nickName ?? p.name}
                  </span>
                </div>
              ))}
            </div>
          </>
        ) : null}

        {/* 注册入口 */}
        {config?.allowRegister && (
          <p className="mt-4 text-center text-sm text-muted-foreground">
            还没有账号？{' '}
            <button className="text-primary underline-offset-4 hover:underline" onClick={() => navigate('/register')}>立即注册</button>
          </p>
        )}

        {/* 忘记密码入口 */}
        <p className="mt-2 text-center text-sm">
          <button className="text-primary underline-offset-4 hover:underline" onClick={() => navigate('/forgot-password')}>忘记密码？</button>
        </p>

        {/* 版权信息 */}
        {(siteInfo?.copyright || siteInfo?.registration) && (
          <div className="mt-4 text-center text-xs text-muted-foreground">
            {siteInfo.copyright && <div dangerouslySetInnerHTML={{ __html: siteInfo.copyright }} />}
            {siteInfo.registration && (
              <a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer" className="hover:underline">
                {siteInfo.registration}
              </a>
            )}
          </div>
        )}
      </div>
    </div>
  );
}


export default function Login() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { login } = useUserStore();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [config, setConfig] = useState<LoginConfig | null>(null);

  useEffect(() => {
    api.user.getLoginConfig().then((res) => setConfig(res.data)).catch(() => {});
  }, []);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!username || !password) { setError('请输入用户名和密码'); return; }
    setLoading(true);
    setError('');
    try {
      await login(username, password);
      navigate(params.get('redirect') ?? '/', { replace: true });
    } catch (err: any) {
      setError(err?.message ?? '登录失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-indigo-500 to-purple-600 p-4">
      <div className="w-full max-w-sm rounded-lg border bg-card p-8 shadow-xl">
        <div className="mb-6 text-center">
          {config?.logo && <img src={config.logo} alt="logo" className="mx-auto mb-2 h-16 w-16 rounded-full" />}
          <h1 className="text-2xl font-bold text-card-foreground">{config?.displayName ?? '魔方管理平台'}</h1>
          {config?.loginTip && <p className="mt-1 text-sm text-muted-foreground">{config.loginTip}</p>}
        </div>

        {error && (
          <div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="username">用户名</Label>
            <Input id="username" autoFocus value={username} onChange={(e) => setUsername(e.target.value)} placeholder="请输入用户名" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="password">密码</Label>
            <Input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="请输入密码" />
          </div>
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? '登录中...' : '登 录'}
          </Button>
        </form>

        {config?.providers?.length ? (
          <>
            <div className="relative my-4">
              <div className="absolute inset-0 flex items-center"><span className="w-full border-t" /></div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-card px-2 text-muted-foreground">其他登录方式</span>
              </div>
            </div>
            <div className="flex flex-wrap justify-center gap-2">
              {config.providers.map((p) => (
                <Button key={p.name} variant="outline" size="sm"
                  onClick={() => { window.location.href = `/Sso/Login?provider=${encodeURIComponent(p.name)}`; }}
                >
                  {p.logo && <img src={p.logo} alt="" className="mr-1 h-4 w-4" />}
                  {p.nickName ?? p.name}
                </Button>
              ))}
            </div>
          </>
        ) : null}
      </div>
    </div>
  );
}
