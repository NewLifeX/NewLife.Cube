import { useMemo, useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import api from '@/api';
import { RegisterCategory } from '@cube/api-core';

export default function Register() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const oauthToken = params.get('oauthToken') || '';

  const [config, setConfig] = useState<any>(null);
  const [tab, setTab] = useState<'password' | 'phone' | 'email'>('password');
  const [sending, setSending] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [form, setForm] = useState({
    username: '',
    email: '',
    mobile: '',
    emailCodeTarget: '',
    code: '',
    password: '',
    confirmPassword: '',
  });

  const oauthMode = !!oauthToken;
  const enableSmsRegister = useMemo(() => !!(config?.enableSmsRegister ?? config?.enableSms), [config]);
  const enableMailRegister = useMemo(() => !!(config?.enableMailRegister ?? config?.enableMail), [config]);

  useEffect(() => {
    api.user.getLoginConfig().then((res) => setConfig(res.data)).catch(() => {});
  }, []);

  useEffect(() => {
    if (!oauthToken) return;
    api.user.getOAuthPendingInfo(oauthToken).then((res) => {
      setForm((f) => ({
        ...f,
        username: res.data?.username || f.username,
        email: res.data?.email || f.email,
        mobile: res.data?.mobile || f.mobile,
      }));
    }).catch(() => setError('OAuth预填信息已过期，请重新发起登录'));
  }, [oauthToken]);

  useEffect(() => {
    if (countdown <= 0) return;
    const t = setInterval(() => setCountdown((v) => (v <= 1 ? 0 : v - 1)), 1000);
    return () => clearInterval(t);
  }, [countdown]);

  const sendCode = async (channel: 'Sms' | 'Mail') => {
    const username = channel === 'Sms' ? form.mobile : form.emailCodeTarget;
    if (!username) return setError(channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址');
    setSending(true);
    setError('');
    try {
      await api.user.sendCode({ channel, username, action: 'register' });
      setCountdown(60);
    } catch (err: any) {
      setError(err?.message ?? '发送失败');
    } finally {
      setSending(false);
    }
  };

  const submit = async () => {
    if (!form.password || !form.confirmPassword) return setError('请输入密码和确认密码');
    if (form.password !== form.confirmPassword) return setError('两次密码不一致');
    if (tab === 'phone' && (!form.mobile || !form.code)) return setError('请填写手机号和验证码');
    if (tab === 'email' && (!form.emailCodeTarget || !form.code)) return setError('请填写邮箱和验证码');

    const payload = oauthMode
      ? { registerCategory: RegisterCategory.OAuthBind, oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
      : tab === 'phone'
        ? { registerCategory: RegisterCategory.Phone, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
        : tab === 'email'
          ? { registerCategory: RegisterCategory.Email, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
          : { registerCategory: RegisterCategory.Password, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };

    setLoading(true);
    setError('');
    try {
      const res = await api.user.register(payload as any);
      const token = res.data?.accessToken || (res.data as any)?.token;
      if (token) {
        api.tokenManager.setToken(token);
        navigate('/', { replace: true });
        return;
      }
      navigate('/login', { replace: true });
    } catch (err: any) {
      setError(err?.message ?? '注册失败');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-indigo-500 to-purple-600 p-4">
      <div className="w-full max-w-sm rounded-lg border bg-card p-8 shadow-xl">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-card-foreground">注册账号</h1>
        </div>

        {error && <div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

        {!oauthMode && (
          <div className="mb-4 flex rounded-lg bg-muted p-1">
            <button type="button" className={`flex-1 rounded-md px-3 py-1.5 text-sm ${tab==='password'?'bg-background shadow':''}`} onClick={() => setTab('password')}>账号</button>
            {enableSmsRegister && <button type="button" className={`flex-1 rounded-md px-3 py-1.5 text-sm ${tab==='phone'?'bg-background shadow':''}`} onClick={() => setTab('phone')}>手机</button>}
            {enableMailRegister && <button type="button" className={`flex-1 rounded-md px-3 py-1.5 text-sm ${tab==='email'?'bg-background shadow':''}`} onClick={() => setTab('email')}>邮箱</button>}
          </div>
        )}

        <div className="space-y-4">
          {(tab==='password' || oauthMode) && (
            <div className="space-y-2">
              <Label>用户名</Label>
              <Input value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} readOnly={oauthMode} />
            </div>
          )}

          {(tab==='password' || tab==='email' || oauthMode) && (
            <div className="space-y-2">
              <Label>邮箱</Label>
              <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
          )}

          {tab==='phone' && (
            <div className="space-y-2">
              <Label>手机号</Label>
              <div className="flex gap-2">
                <Input value={form.mobile} onChange={(e) => setForm({ ...form, mobile: e.target.value })} />
                <Button variant="outline" disabled={countdown>0 || sending} onClick={() => sendCode('Sms')}>{countdown>0?`${countdown}s`:'发码'}</Button>
              </div>
            </div>
          )}

          {tab==='email' && (
            <div className="space-y-2">
              <Label>邮箱地址</Label>
              <div className="flex gap-2">
                <Input type="email" value={form.emailCodeTarget} onChange={(e) => setForm({ ...form, emailCodeTarget: e.target.value })} />
                <Button variant="outline" disabled={countdown>0 || sending} onClick={() => sendCode('Mail')}>{countdown>0?`${countdown}s`:'发码'}</Button>
              </div>
            </div>
          )}

          {(tab==='phone' || tab==='email') && (
            <div className="space-y-2">
              <Label>验证码</Label>
              <Input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
            </div>
          )}

          <div className="space-y-2">
            <Label>密码</Label>
            <Input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          </div>

          <div className="space-y-2">
            <Label>确认密码</Label>
            <Input type="password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} />
          </div>

          <Button className="w-full" disabled={loading} onClick={submit}>{loading ? '提交中...' : (oauthMode ? '完成绑定并登录' : '立即注册')}</Button>
        </div>

        <p className="mt-4 text-center text-sm text-muted-foreground">已有账号？ <button className="text-primary underline-offset-4 hover:underline" onClick={() => navigate('/login')}>去登录</button></p>
      </div>
    </div>
  );
}
