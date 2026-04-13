import { useState, useEffect, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Box, Card, CardContent, Typography, TextField, Button,
  Alert, Divider, Stack, Avatar,
} from '@mui/material';
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
    <Box sx={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    }}>
      <Card sx={{ width: 400, p: 2 }}>
        <CardContent>
          <Box textAlign="center" mb={3}>
            {config?.logo && <Avatar src={config.logo} sx={{ width: 64, height: 64, mx: 'auto', mb: 1 }} />}
            <Typography variant="h5">{config?.displayName ?? '魔方管理平台'}</Typography>
            {config?.loginTip && <Typography variant="body2" color="text.secondary">{config.loginTip}</Typography>}
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

          {config?.providers?.length ? (
            <>
              <Divider sx={{ my: 2 }}>其他登录方式</Divider>
              <Stack direction="row" spacing={1} justifyContent="center">
                {config.providers.map((p) => (
                  <Button key={p.name} variant="outlined" size="small"
                    startIcon={p.logo ? <Avatar src={p.logo} sx={{ width: 20, height: 20 }} /> : undefined}
                    onClick={() => { window.location.href = `/Sso/Login?provider=${encodeURIComponent(p.name)}`; }}
                  >
                    {p.nickName ?? p.name}
                  </Button>
                ))}
              </Stack>
            </>
          ) : null}
        </CardContent>
      </Card>
    </Box>
  );
}
