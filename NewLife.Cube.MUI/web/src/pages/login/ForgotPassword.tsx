import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Card, CardContent, Typography, TextField, Button, Alert,
  RadioGroup, FormControlLabel, Radio, Stack,
} from '@mui/material';
import api from '@/api';
import { validateResetPassword, type ResetPasswordForm } from '@/utils/forgot-password';

export default function ForgotPassword() {
  const navigate = useNavigate();

  const [step, setStep] = useState<'input' | 'verify'>('input');
  const [form, setForm] = useState<ResetPasswordForm>({
    username: '',
    channel: 'Sms',
    code: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [errors, setErrors] = useState<Partial<Record<keyof ResetPasswordForm, string>>>({});
  const [error, setError] = useState('');
  const [sending, setSending] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const update = (patch: Partial<ResetPasswordForm>) => setForm((prev) => ({ ...prev, ...patch }));

  const sendCode = async () => {
    setSending(true);
    setError('');
    try {
      await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
      setStep('verify');
    } catch (err: any) {
      setError(err?.message ?? '发送失败，请稍后重试');
    } finally {
      setSending(false);
    }
  };

  const onSendCode = async (e: FormEvent) => {
    e.preventDefault();
    setErrors({});
    if (!form.username) { setErrors({ username: '请输入手机号或邮箱' }); return; }
    await sendCode();
  };

  const onConfirmReset = async (e: FormEvent) => {
    e.preventDefault();
    setErrors({});
    const message = validateResetPassword(form);
    if (message) {
      setErrors({ [message[0]]: message[1] });
      return;
    }

    setSubmitting(true);
    setError('');
    try {
      await api.user.resetPassword({
        username: form.username,
        code: form.code,
        newPassword: form.newPassword,
        confirmPassword: form.confirmPassword,
      });
      navigate('/login');
    } catch (err: any) {
      setError(err?.message ?? '重置失败，请重试');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'grey.100', p: 2 }}>
      <Card sx={{ width: 440 }}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" fontWeight={700} textAlign="center" mb={3}>重置密码</Typography>

          {step === 'input' ? (
            <form onSubmit={onSendCode}>
              <TextField
                fullWidth label="手机号或邮箱" margin="normal"
                value={form.username} onChange={(e) => update({ username: e.target.value })}
                error={!!errors.username} helperText={errors.username}
              />
              <RadioGroup row value={form.channel} onChange={(e) => update({ channel: e.target.value as 'Sms' | 'Mail' })}>
                <FormControlLabel value="Sms" control={<Radio />} label="短信" />
                <FormControlLabel value="Mail" control={<Radio />} label="邮箱" />
              </RadioGroup>
              {error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}
              <Button type="submit" fullWidth variant="contained" size="large" disabled={sending}>
                {sending ? '发送中...' : '发送验证码'}
              </Button>
            </form>
          ) : (
            <form onSubmit={onConfirmReset}>
              <Stack direction="row" spacing={1} alignItems="flex-start">
                <TextField
                  fullWidth label="验证码" margin="normal"
                  value={form.code} onChange={(e) => update({ code: e.target.value })}
                  error={!!errors.code} helperText={errors.code}
                />
                <Button sx={{ mt: 3, whiteSpace: 'nowrap' }} onClick={sendCode} disabled={sending}>重新发送</Button>
              </Stack>
              <TextField
                fullWidth label="新密码" type="password" margin="normal"
                value={form.newPassword} onChange={(e) => update({ newPassword: e.target.value })}
                error={!!errors.newPassword} helperText={errors.newPassword}
              />
              <TextField
                fullWidth label="确认密码" type="password" margin="normal"
                value={form.confirmPassword} onChange={(e) => update({ confirmPassword: e.target.value })}
                error={!!errors.confirmPassword} helperText={errors.confirmPassword}
              />
              {error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}
              <Button type="submit" fullWidth variant="contained" size="large" disabled={submitting}>
                {submitting ? '提交中...' : '确认重置'}
              </Button>
            </form>
          )}

          <Box textAlign="center" mt={2}>
            <Button size="small" onClick={() => (step === 'input' ? navigate('/login') : setStep('input'))}>
              {step === 'input' ? '返回登录' : '上一步'}
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
