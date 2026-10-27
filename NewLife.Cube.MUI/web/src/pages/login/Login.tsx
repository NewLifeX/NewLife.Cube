import { useState, useEffect, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Box, Card, CardContent, Typography, TextField, Button,
  Alert, Divider, Stack, Avatar,
} from '@mui/material';
import { useUserStore } from '@/stores/user';
import api from '@/api';
import type { LoginConfig, SiteConfig } from '@newlifex/api-core';

export default function Login() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { login } = useUserStore();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [config, setConfig] = useState<LoginConfig | null>(null);
  const [siteConfig, setSiteConfig] = useState<SiteConfig | null>(null);
  const registerEnabled = config?.register?.enabled === true;
  const oauthProviders = config?.oauth ?? [];
  const tenantCode = params.get('tenant') ?? params.get('tenantCode') ?? params.get('code') ?? undefined;

  useEffect(() => {
    api.user.getLoginConfig(tenantCode).then((res) => setConfig(res.data)).catch(() => {});
    // 品牌、Logo、登录提示与版权由匿名接口 /Cube/Site 提供，失败时回退内置默认
    api.site.getSite(tenantCode).then((res) => setSiteConfig(res.data)).catch(() => {});
  }, [tenantCode]);

  /** 进入注册页时保留租户等查询参数 */
  const goRegister = () => {
    const search = params.toString();
    navigate(search ? `/register?${search}` : '/register');
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

  return (
    <Box sx={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    }}>
      <Card sx={{ width: 400, p: 2 }}>
        <CardContent>
          <Box textAlign="center" mb={3}>
            {siteConfig?.logo && <Avatar src={siteConfig.logo} sx={{ width: 64, height: 64, mx: 'auto', mb: 1 }} />}
            <Typography variant="h5">{siteConfig?.name ?? '魔方管理平台'}</Typography>
            {siteConfig?.loginTip && <Typography variant="body2" color="text.secondary">{siteConfig.loginTip}</Typography>}
          </Box>

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

          <form onSubmit={handleSubmit}>
            <TextField
              label="用户名" fullWidth margin="normal"
              value={username} onChange={(e) => setUsername(e.target.value)}
              autoFocus
            />
            <TextField
              label="密码" type="password" fullWidth margin="normal"
              value={password} onChange={(e) => setPassword(e.target.value)}
            />
            <Button type="submit" variant="contained" fullWidth size="large" disabled={loading} sx={{ mt: 2 }}>
              {loading ? '登录中...' : '登 录'}
            </Button>
          </form>

          {oauthProviders.length ? (
            <>
              <Divider sx={{ my: 2 }}>其他登录方式</Divider>
              <Stack direction="row" spacing={1} justifyContent="center">
                {oauthProviders.map((p) => (
                  <Button key={p.name} variant="outlined" size="small"
                    startIcon={p.logo ? <Avatar src={p.logo} sx={{ width: 20, height: 20 }} /> : undefined}
                    onClick={() => { window.location.href = `/Sso/Login/${encodeURIComponent(p.name)}?source=front-end&redirect_uri=${encodeURIComponent(window.location.origin + '/')}`; }}
                  >
                    {p.nickName ?? p.name}
                  </Button>
                ))}
              </Stack>
            </>
          ) : null}

          <Box textAlign="center" mt={1}>
            <Button size="small" onClick={() => navigate('/forgot-password')}>忘记密码？</Button>
          </Box>

          {registerEnabled ? (
            <Box textAlign="center" mt={2}>
              <Typography variant="body2" color="text.secondary" component="span">
                还没有账号？
              </Typography>
              <Button size="small" onClick={goRegister}>立即注册</Button>
            </Box>
          ) : null}
        </CardContent>
      </Card>
    </Box>
  );
}
