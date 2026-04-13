import { useState, useEffect, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { LoginConfig } from '@cube/api-core';

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
