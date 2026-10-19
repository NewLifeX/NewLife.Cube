import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Divider,
  InputAdornment,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
  type AlertColor,
} from '@mui/material';
import { AuthLogic } from '@newlifex/auth-logic';
import type { LoginConfig } from '@newlifex/api-core';
import api from '@/api';
import { useUserStore } from '@/stores/user';
import {
  buildRegisterPayload,
  getRegisterErrorMessage,
  isValidEmail,
  isValidMobile,
  resolveRegisterModes,
  resolveRegisterTenant,
  validateRegister,
  type RegisterFormErrors,
  type RegisterFormInput,
  type RegisterMode,
} from '@/utils/register';

interface Notice {
  severity: AlertColor;
  text: string;
}

interface PendingResult {
  message: string;
  channels: string[];
  targets: string[];
}

const modeLabels: Record<RegisterMode, string> = {
  password: '账号注册',
  mobile: '手机注册',
  mail: '邮箱注册',
};

const channelLabels: Record<string, string> = {
  mail: '邮箱',
  sms: '手机短信',
};

const emptyForm: RegisterFormInput = {
  username: '',
  email: '',
  mobile: '',
  password: '',
  confirmPassword: '',
  code: '',
  captchaCode: '',
};

/** 从 URLSearchParams 提取查询对象，保留重复参数的数组形态 */
function toQuery(params: URLSearchParams): Record<string, unknown> {
  const query: Record<string, unknown> = {};
  for (const key of new Set(params.keys())) {
    const values = params.getAll(key);
    query[key] = values.length > 1 ? values : values[0];
  }
  return query;
}

/** MUI 注册页：按后端 LoginConfig.register 能力渲染账号/手机/邮箱注册 */
export default function Register() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const fetchUserInfo = useUserStore((state) => state.fetchUserInfo);
  const fetchMenus = useUserStore((state) => state.fetchMenus);

  const tenant = useMemo(() => resolveRegisterTenant(toQuery(params)), [params]);
  const authLogic = useMemo(() => new AuthLogic(api, () => {}), []);

  const [config, setConfig] = useState<LoginConfig | null>(null);
  const [configLoading, setConfigLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [mode, setMode] = useState<RegisterMode>('password');
  const [form, setForm] = useState<RegisterFormInput>(emptyForm);
  const [errors, setErrors] = useState<RegisterFormErrors>({});
  const [notice, setNotice] = useState<Notice | null>(null);
  const [pending, setPending] = useState<PendingResult | null>(null);
  const [captcha, setCaptcha] = useState<{ captchaId: string; image: string } | null>(null);
  const [captchaLoading, setCaptchaLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [sending, setSending] = useState(false);
  const [countdown, setCountdown] = useState(0);

  const modes = useMemo(() => resolveRegisterModes(config?.register), [config]);
  const registerAvailable = modes.length > 0;
  const registerCaptchaRequired = config?.register?.captcha === true;
  const sendCodeCaptchaRequired = config?.login?.sendCode === true;
  const captchaRequired = registerCaptchaRequired || sendCodeCaptchaRequired;
  const requireMailVerify = config?.register?.requireMailVerify === true;
  const requireMobileVerify = config?.register?.requireMobileVerify === true;
  const showEmail = mode !== 'mobile' || requireMailVerify;
  const showMobile = mode !== 'mail' || requireMobileVerify;
  const usernameLabel = mode === 'password' ? '用户名' : '用户名（可选）';
  const emailLabel = mode === 'mail' || requireMailVerify ? '邮箱 *' : '邮箱（可选）';
  const mobileLabel = mode === 'mobile' || requireMobileVerify ? '手机号 *' : '手机号（可选）';
  const passwordLabel = mode === 'password' ? '密码 *' : '密码（可选）';
  const codeLabel = mode === 'mobile' ? '手机验证码' : '邮箱验证码';
  const pendingChannelText = pending?.channels.map((channel) => channelLabels[channel] ?? channel).join('、') ?? '';

  /** 清除单个字段错误 */
  const clearError = useCallback((field: keyof RegisterFormErrors) => {
    setErrors((prev) => (prev[field] ? { ...prev, [field]: undefined } : prev));
  }, []);

  /** 刷新图片验证码 */
  const refreshCaptcha = useCallback(async () => {
    setCaptchaLoading(true);
    try {
      const res = await api.user.getCaptcha();
      setCaptcha(res.data ? { captchaId: res.data.captchaId, image: res.data.image } : null);
      setForm((prev) => ({ ...prev, captchaCode: '' }));
      setErrors((prev) => (prev.captchaCode ? { ...prev, captchaCode: undefined } : prev));
    } catch {
      setCaptcha(null);
      setNotice({ severity: 'error', text: '图片验证码加载失败，请点击刷新重试' });
    } finally {
      setCaptchaLoading(false);
    }
  }, []);

  /** 加载登录/注册配置，按租户参数对齐后端能力 */
  const loadConfig = useCallback(async () => {
    setConfigLoading(true);
    setLoadError('');
    try {
      const res = await api.user.getLoginConfig(tenant.tenantCode);
      setConfig(res.data ?? null);
      if (res.data?.register?.captcha === true || res.data?.login?.sendCode === true) {
        void refreshCaptcha();
      }
    } catch (error) {
      setLoadError(getRegisterErrorMessage(error));
    } finally {
      setConfigLoading(false);
    }
  }, [tenant.tenantCode, refreshCaptcha]);

  useEffect(() => {
    void loadConfig();
  }, [loadConfig]);

  // 配置加载后校正当前注册方式，避免停留在后端未开放的页签
  useEffect(() => {
    if (modes.length > 0 && !modes.includes(mode)) setMode(modes[0]);
  }, [modes, mode]);

  // 发码倒计时
  useEffect(() => {
    if (countdown <= 0) return;
    const timer = window.setTimeout(() => setCountdown((value) => Math.max(0, value - 1)), 1000);
    return () => window.clearTimeout(timer);
  }, [countdown]);

  /** 更新表单字段并清除对应错误 */
  function updateField(field: keyof RegisterFormInput, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
    clearError(field);
  }

  /** 返回登录页，保留租户等查询参数 */
  function goLogin() {
    const search = params.toString();
    navigate(search ? `/login?${search}` : '/login');
  }

  /** 发送手机/邮箱注册验证码 */
  async function handleSendCode() {
    if (sending || countdown > 0) return;

    const isSms = mode === 'mobile';
    const account = (isSms ? form.mobile : form.email).trim();
    if (!account) {
      setErrors((prev) => ({ ...prev, [isSms ? 'mobile' : 'email']: isSms ? '请输入手机号' : '请输入邮箱' }));
      return;
    }
    if (isSms && !isValidMobile(account)) {
      setErrors((prev) => ({ ...prev, mobile: '手机号格式不正确' }));
      return;
    }
    if (!isSms && !isValidEmail(account)) {
      setErrors((prev) => ({ ...prev, email: '邮箱格式不正确' }));
      return;
    }
    if (sendCodeCaptchaRequired) {
      if (!captcha?.captchaId) {
        setErrors((prev) => ({ ...prev, captchaCode: '图片验证码加载失败，请刷新' }));
        return;
      }
      if (!form.captchaCode.trim()) {
        setErrors((prev) => ({ ...prev, captchaCode: '请输入图片验证码' }));
        return;
      }
    }

    setSending(true);
    try {
      // 发码需要按 login.sendCode 透传图片验证码，AuthLogic/RegisterLogic 未覆盖该场景，
      // 因此这里直接调用 api-core 的 sendCode；注册提交统一走 AuthLogic.register。
      await api.user.sendCode({
        channel: isSms ? 'Sms' : 'Mail',
        username: account,
        action: 'register',
        ...(sendCodeCaptchaRequired && captcha
          ? { captchaId: captcha.captchaId, captchaCode: form.captchaCode.trim() }
          : {}),
      });
      setNotice({ severity: 'success', text: '验证码已发送，请查收' });
      setCountdown(60);
      if (sendCodeCaptchaRequired) void refreshCaptcha();
    } catch (error) {
      setNotice({ severity: 'error', text: getRegisterErrorMessage(error) });
      if (sendCodeCaptchaRequired) void refreshCaptcha();
    } finally {
      setSending(false);
    }
  }

  /** 提交注册 */
  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;

    const result = validateRegister(form, config, mode, {
      captchaRequired: registerCaptchaRequired,
      captchaId: registerCaptchaRequired ? captcha?.captchaId : undefined,
    });
    setErrors(result.errors);
    if (!result.valid) {
      setNotice({ severity: 'warning', text: '请检查输入信息' });
      return;
    }

    setSubmitting(true);
    setNotice(null);
    const payload = buildRegisterPayload(form, mode, {
      captchaId: registerCaptchaRequired ? captcha?.captchaId : undefined,
      captchaCode: registerCaptchaRequired ? form.captchaCode : undefined,
      appId: tenant.appId,
      tenantCode: tenant.tenantCode,
    });

    try {
      const res = await authLogic.register(payload);

      // 待激活：后端已发送激活邮件/短信，不返回 token
      if (res.data?.pendingActivation) {
        setPending({
          message: res.message || '注册成功，请先激活邮箱/手机后再登录',
          channels: res.data.channels ?? [],
          targets: res.data.targets ?? [],
        });
        return;
      }

      // 注册成功并已登录
      if (res.data?.accessToken) {
        setNotice({ severity: 'success', text: res.message || '注册成功，正在进入系统...' });
        await fetchUserInfo().catch(() => undefined);
        await fetchMenus().catch(() => undefined);
        navigate('/home', { replace: true });
        return;
      }

      setNotice({ severity: 'success', text: res.message || '注册成功，请登录' });
      goLogin();
    } catch (error) {
      setNotice({ severity: 'error', text: getRegisterErrorMessage(error) });
      if (registerCaptchaRequired) void refreshCaptcha();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 3,
        background: 'linear-gradient(135deg, #1976d2 0%, #64b5f6 100%)',
      }}
    >
      <Card sx={{ width: { xs: '100%', sm: 460 }, maxWidth: '100%', maxHeight: '92vh', overflow: 'auto' }}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" component="h1" align="center" fontWeight="bold" gutterBottom>
            注册账号
          </Typography>

          {loadError ? (
            <Alert
              severity="error"
              sx={{ mt: 2 }}
              action={
                <Button color="inherit" size="small" onClick={() => void loadConfig()}>
                  重试
                </Button>
              }
            >
              {loadError}
            </Alert>
          ) : configLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress size={28} />
            </Box>
          ) : !registerAvailable ? (
            <Alert severity="warning" sx={{ mt: 2 }}>
              系统未开放注册，请联系管理员。
            </Alert>
          ) : pending ? (
            <Stack spacing={2} sx={{ mt: 2 }}>
              <Alert severity="success">{pending.message}</Alert>
              {pending.targets.length > 0 ? (
                <Alert severity="info">激活信息已发送至：{pending.targets.join('、')}</Alert>
              ) : pendingChannelText ? (
                <Alert severity="info">请查收{pendingChannelText}中的激活信息。</Alert>
              ) : null}
              <Button variant="contained" size="large" onClick={goLogin}>
                返回登录
              </Button>
            </Stack>
          ) : (
            <>
              {notice ? (
                <Alert severity={notice.severity} sx={{ mt: 2 }} onClose={() => setNotice(null)}>
                  {notice.text}
                </Alert>
              ) : null}

              {modes.length > 1 ? (
                <Tabs
                  value={mode}
                  onChange={(_, value: RegisterMode) => {
                    setMode(value);
                    setErrors({});
                    setNotice(null);
                  }}
                  variant="fullWidth"
                  sx={{ mt: 2 }}
                >
                  {modes.map((item) => (
                    <Tab key={item} value={item} label={modeLabels[item]} />
                  ))}
                </Tabs>
              ) : (
                <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 2 }}>
                  {modeLabels[mode]}
                </Typography>
              )}

              <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 1 }}>
                <TextField
                  fullWidth
                  margin="dense"
                  label={usernameLabel}
                  value={form.username}
                  onChange={(event) => updateField('username', event.target.value)}
                  error={!!errors.username}
                  helperText={errors.username}
                  autoComplete="username"
                />

                {showEmail ? (
                  <TextField
                    fullWidth
                    margin="dense"
                    type="email"
                    label={emailLabel}
                    value={form.email}
                    onChange={(event) => updateField('email', event.target.value)}
                    error={!!errors.email}
                    helperText={errors.email}
                    autoComplete="email"
                  />
                ) : null}

                {showMobile ? (
                  <TextField
                    fullWidth
                    margin="dense"
                    label={mobileLabel}
                    value={form.mobile}
                    onChange={(event) => updateField('mobile', event.target.value)}
                    error={!!errors.mobile}
                    helperText={errors.mobile}
                    autoComplete="tel"
                  />
                ) : null}

                {mode !== 'password' ? (
                  <TextField
                    fullWidth
                    margin="dense"
                    label={codeLabel}
                    value={form.code}
                    onChange={(event) => updateField('code', event.target.value)}
                    error={!!errors.code}
                    helperText={errors.code}
                    autoComplete="one-time-code"
                    slotProps={{
                      input: {
                        endAdornment: (
                          <InputAdornment position="end">
                            <Button
                              size="small"
                              disabled={countdown > 0 || sending}
                              onClick={() => void handleSendCode()}
                            >
                              {countdown > 0 ? `${countdown}s` : '发送验证码'}
                            </Button>
                          </InputAdornment>
                        ),
                      },
                    }}
                  />
                ) : null}

                <TextField
                  fullWidth
                  margin="dense"
                  type="password"
                  label={passwordLabel}
                  value={form.password}
                  onChange={(event) => updateField('password', event.target.value)}
                  error={!!errors.password}
                  helperText={errors.password}
                  autoComplete="new-password"
                />

                <TextField
                  fullWidth
                  margin="dense"
                  type="password"
                  label="确认密码"
                  value={form.confirmPassword}
                  onChange={(event) => updateField('confirmPassword', event.target.value)}
                  error={!!errors.confirmPassword}
                  helperText={errors.confirmPassword}
                  autoComplete="new-password"
                />

                {captchaRequired ? (
                  <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 1, mb: 1 }}>
                    {captcha?.image ? (
                      <Box
                        component="img"
                        src={captcha.image}
                        alt="图片验证码"
                        sx={{
                          width: 120,
                          height: 40,
                          objectFit: 'contain',
                          borderRadius: 1,
                          border: '1px solid',
                          borderColor: 'divider',
                          bgcolor: 'background.paper',
                        }}
                      />
                    ) : null}
                    <Button size="small" onClick={() => void refreshCaptcha()} disabled={captchaLoading}>
                      {captchaLoading ? '加载中...' : '刷新'}
                    </Button>
                  </Stack>
                ) : null}

                {captchaRequired ? (
                  <TextField
                    fullWidth
                    margin="dense"
                    label="图片验证码"
                    value={form.captchaCode}
                    onChange={(event) => updateField('captchaCode', event.target.value)}
                    error={!!errors.captchaCode}
                    helperText={errors.captchaCode}
                  />
                ) : null}

                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  size="large"
                  disabled={submitting}
                  sx={{ mt: 2 }}
                >
                  {submitting ? '提交中...' : mode === 'password' ? '立即注册' : '注册并登录'}
                </Button>
              </Box>
            </>
          )}
        </CardContent>

        <Divider />
        <CardContent sx={{ textAlign: 'center', py: 1.5 }}>
          <Typography variant="body2" color="text.secondary" component="span">
            已有账号？
          </Typography>
          <Button size="small" onClick={goLogin}>
            去登录
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
}
