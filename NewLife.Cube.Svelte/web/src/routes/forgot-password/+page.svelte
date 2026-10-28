<!--
  找回密码（忘记密码） — 验证码重置

  两步流程，与后端 /Auth/* 约定一致：
  1. 输入手机号或邮箱 → POST /Auth/SendCode { action: 'reset' }
  2. 输入验证码 + 新密码 → POST /Auth/ResetPassword（新密码由 @newlifex/cube 自动 RSA 挑战加密）

  本皮肤未接入 i18n 基础设施（与其它页面一致使用中文文案），校验规则与 Vue 参考皮肤保持一致：
  账号必填、验证码必填、新密码不少于 6 位、两次输入一致。
-->
<script lang="ts">
  import { goto } from '$app/navigation';
  import { getApi } from '$lib/api';
  import { siteTitle } from '$lib/stores/app';

  /** 新密码最小长度，与后端弱密码门限一致 */
  const minLength = 6;

  let step = $state<'account' | 'verify'>('account');
  let username = $state('');
  let channel = $state<'Sms' | 'Mail'>('Sms');
  let code = $state('');
  let newPassword = $state('');
  let confirmPassword = $state('');
  let sending = $state(false);
  let submitting = $state(false);
  let countdown = $state(0);
  let error = $state('');

  let timer: ReturnType<typeof setInterval> | null = null;

  /** 按账号格式自动切换发码渠道（含 @ 视为邮箱） */
  function onUsernameInput() {
    channel = username.includes('@') ? 'Mail' : 'Sms';
  }

  function startCountdown(seconds = 60) {
    if (timer) clearInterval(timer);
    countdown = seconds;
    timer = setInterval(() => {
      countdown -= 1;
      if (countdown <= 0 && timer) {
        clearInterval(timer);
        timer = null;
      }
    }, 1000);
  }

  /** 发送验证码，成功才进入第二步 */
  async function handleSendCode() {
    if (sending || (step === 'verify' && countdown > 0)) return;

    error = '';
    const account = username.trim();
    if (!account) {
      error = '请输入手机号或邮箱';
      return;
    }

    sending = true;
    try {
      await getApi().user.sendCode({ channel, username: account, action: 'reset' });
      username = account;
      step = 'verify';
      startCountdown();
    } catch (e) {
      error = e instanceof Error ? e.message : '验证码发送失败，请稍后重试';
    } finally {
      sending = false;
    }
  }

  /** 提交重置：本地校验通过后调用 /Auth/ResetPassword */
  async function handleReset() {
    error = '';
    if (!code.trim()) {
      error = '请输入验证码';
      return;
    }
    if (!newPassword) {
      error = '请输入新密码';
      return;
    }
    if (newPassword.length < minLength) {
      error = `新密码不少于${minLength}位`;
      return;
    }
    if (newPassword !== confirmPassword) {
      error = '两次输入的密码不一致';
      return;
    }

    submitting = true;
    try {
      await getApi().user.resetPassword({
        username: username.trim(),
        code: code.trim(),
        newPassword,
        confirmPassword,
      });
      goto('/login');
    } catch (e) {
      error = e instanceof Error ? e.message : '重置密码失败，请稍后重试';
    } finally {
      submitting = false;
    }
  }
</script>

<div class="min-h-screen flex items-center justify-center" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%)">
  <div class="w-full max-w-md mx-4">
    <div class="card">
      <h1 class="text-2xl font-bold text-center mb-6">找回密码</h1>

      {#if error}
        <div class="mb-4 p-3 rounded-md bg-red-50 text-red-600 text-sm dark:bg-red-900/30 dark:text-red-400">{error}</div>
      {/if}

      {#if step === 'account'}
        <form onsubmit={(e) => { e.preventDefault(); handleSendCode(); }} class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)" for="forgot-username">手机号或邮箱</label>
            <input id="forgot-username" class="input" type="text" bind:value={username} oninput={onUsernameInput} placeholder="请输入手机号或邮箱" autocomplete="username" />
          </div>
          <div class="flex gap-2">
            <button type="button" class="btn btn-outline flex-1" class:btn-primary={channel === 'Sms'} onclick={() => (channel = 'Sms')}>短信</button>
            <button type="button" class="btn btn-outline flex-1" class:btn-primary={channel === 'Mail'} onclick={() => (channel = 'Mail')}>邮箱</button>
          </div>
          <button class="btn btn-primary w-full" type="submit" disabled={sending}>{sending ? '发送中...' : '发送验证码'}</button>
        </form>
      {:else}
        <form onsubmit={(e) => { e.preventDefault(); handleReset(); }} class="space-y-4">
          <div>
            <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)" for="forgot-code">验证码</label>
            <div class="flex gap-2">
              <input id="forgot-code" class="input flex-1" type="text" bind:value={code} maxlength="6" placeholder="请输入验证码" autocomplete="one-time-code" />
              <button type="button" class="btn btn-outline whitespace-nowrap" disabled={sending || countdown > 0} onclick={handleSendCode}>
                {countdown > 0 ? `${countdown}s` : '重新发送'}
              </button>
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)" for="forgot-new-password">新密码</label>
            <input id="forgot-new-password" class="input" type="password" bind:value={newPassword} placeholder="请输入新密码" autocomplete="new-password" />
          </div>
          <div>
            <label class="block text-sm font-medium mb-1" style="color: var(--text-secondary)" for="forgot-confirm-password">确认密码</label>
            <input id="forgot-confirm-password" class="input" type="password" bind:value={confirmPassword} placeholder="请再次输入新密码" autocomplete="new-password" />
          </div>
          <button class="btn btn-primary w-full" type="submit" disabled={submitting}>{submitting ? '提交中...' : '重置密码'}</button>
          <button type="button" class="btn btn-outline w-full" onclick={() => (step = 'account')}>上一步</button>
        </form>
      {/if}

      <a class="block text-center text-sm mt-4" style="color: var(--primary)" href="/login">返回登录</a>
      <p class="text-center text-xs mt-2" style="color: var(--text-secondary)">{$siteTitle}</p>
    </div>
  </div>
</div>
