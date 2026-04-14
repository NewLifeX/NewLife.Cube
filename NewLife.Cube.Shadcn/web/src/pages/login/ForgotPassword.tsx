import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import api from '@/api';

export default function ForgotPassword() {
  const navigate = useNavigate();

  const [step, setStep] = useState<'input' | 'verify'>('input');
  const [username, setUsername] = useState('');
  const [channel, setChannel] = useState<'Sms' | 'Mail'>('Sms');
  const [code, setCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [sending, setSending] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [countdown, setCountdown] = useState(0);
  const [error, setError] = useState('');

  let _timer: ReturnType<typeof setInterval> | null = null;

  const startCountdown = (seconds = 60) => {
    if (_timer) clearInterval(_timer);
    setCountdown(seconds);
    _timer = setInterval(() => {
      setCountdown((v) => {
        if (v <= 1) { clearInterval(_timer!); _timer = null; return 0; }
        return v - 1;
      });
    }, 1000);
  };

  const onSendCode = async () => {
    if (!username) { setError('请输入手机号或邮箱'); return; }
    setSending(true);
    setError('');
    try {
      await api.user.sendCode({ channel, username, action: 'reset' });
      setStep('verify');
      startCountdown();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : '发送失败，请稍后重试');
    } finally {
      setSending(false);
    }
  };

  const onResend = async () => {
    if (countdown > 0) return;
    setSending(true);
    setError('');
    try {
      await api.user.sendCode({ channel, username, action: 'reset' });
      startCountdown();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : '发送失败');
    } finally {
      setSending(false);
    }
  };

  const onConfirmReset = async () => {
    if (!code) { setError('请输入验证码'); return; }
    if (!newPassword) { setError('请输入新密码'); return; }
    if (newPassword !== confirmPassword) { setError('两次密码不一致'); return; }
    setSubmitting(true);
    setError('');
    try {
      await api.user.resetPassword({ username, code, newPassword, confirmPassword });
      navigate('/login');
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : '重置失败，请重试');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-indigo-500 to-purple-600 p-4">
      <div className="w-full max-w-sm rounded-lg border bg-card p-8 shadow-xl">
        <h1 className="mb-6 text-center text-2xl font-bold text-card-foreground">重置密码</h1>

        {error && (
          <div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
        )}

        {step === 'input' ? (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">手机号或邮箱</Label>
              <Input id="username" value={username} onChange={(e) => setUsername(e.target.value)} placeholder="请输入手机号或邮箱" />
            </div>
            <div className="space-y-2">
              <Label>验证渠道</Label>
              <div className="flex gap-2">
                {(['Sms', 'Mail'] as const).map((c) => (
                  <button
                    key={c}
                    type="button"
                    className={`flex-1 rounded-md border px-3 py-2 text-sm transition-all ${channel === c ? 'border-primary bg-primary text-primary-foreground' : 'border-input bg-background text-foreground hover:bg-accent'}`}
                    onClick={() => setChannel(c)}
                  >
                    {c === 'Sms' ? '短信' : '邮箱'}
                  </button>
                ))}
              </div>
            </div>
            <Button className="w-full" onClick={onSendCode} disabled={sending}>{sending ? '发送中...' : '发送验证码'}</Button>
            <p className="text-center text-sm">
              <button className="text-primary underline-offset-4 hover:underline" onClick={() => navigate('/login')}>返回登录</button>
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="code">验证码</Label>
              <div className="flex gap-2">
                <Input id="code" value={code} onChange={(e) => setCode(e.target.value)} placeholder="请输入验证码" className="flex-1" />
                <Button variant="outline" onClick={onResend} disabled={countdown > 0 || sending} className="shrink-0">
                  {countdown > 0 ? `${countdown}s` : '重新发送'}
                </Button>
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="newPwd">新密码</Label>
              <Input id="newPwd" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="请输入新密码（≥6位）" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirmPwd">确认密码</Label>
              <Input id="confirmPwd" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} placeholder="请再次输入新密码" />
            </div>
            <Button className="w-full" onClick={onConfirmReset} disabled={submitting}>{submitting ? '重置中...' : '确认重置'}</Button>
            <p className="text-center text-sm space-x-4">
              <button className="text-muted-foreground underline-offset-4 hover:underline" onClick={() => setStep('input')}>上一步</button>
              <button className="text-primary underline-offset-4 hover:underline" onClick={() => navigate('/login')}>返回登录</button>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
