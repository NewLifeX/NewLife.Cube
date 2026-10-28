import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import type { LoginConfig } from '@newlifex/cube';
import { AuthLogic } from '@newlifex/cube';
import { ApiService } from '../../services/api.service';
import { UserService } from '../../services/user.service';
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
} from '../../utils/register';

type NoticeType = 'success' | 'info' | 'warning' | 'error';

interface Notice {
  type: NoticeType;
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

/** Angular 注册页：按后端 LoginConfig.register 能力渲染账号/手机/邮箱注册 */
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzCardModule,
    NzFormModule,
    NzInputModule,
    NzRadioModule,
    NzSpinModule,
  ],
  template: `
    <div style="display: flex; justify-content: center; align-items: center; min-height: 100vh; padding: 24px; box-sizing: border-box; background: linear-gradient(135deg, #1890ff 0%, #722ed1 100%);">
      <nz-card style="width: 460px; max-width: 100%; max-height: 92vh; overflow: auto;" [nzBordered]="false">
        <div style="text-align: center; font-size: 20px; font-weight: bold; margin-bottom: 24px;">注册账号</div>

        @if (loadError) {
          <nz-alert nzType="error" [nzMessage]="loadError" nzShowIcon style="margin-bottom: 16px;"></nz-alert>
          <button nz-button nzType="primary" nzBlock (click)="loadConfig()">重试</button>
        } @else if (configLoading) {
          <div style="text-align: center; padding: 24px 0;">
            <nz-spin nzSimple></nz-spin>
          </div>
        } @else if (!registerAvailable) {
          <nz-alert nzType="warning" nzMessage="系统未开放注册，请联系管理员。" nzShowIcon style="margin-bottom: 16px;"></nz-alert>
        } @else if (pending) {
          <nz-alert nzType="success" [nzMessage]="pending.message" nzShowIcon style="margin-bottom: 16px;"></nz-alert>
          @if (pending.targets.length) {
            <nz-alert nzType="info" [nzMessage]="'激活信息已发送至：' + pending.targets.join('、')" style="margin-bottom: 16px;"></nz-alert>
          } @else if (pendingChannelText) {
            <nz-alert nzType="info" [nzMessage]="'请查收' + pendingChannelText + '中的激活信息。'" style="margin-bottom: 16px;"></nz-alert>
          }
          <button nz-button nzType="primary" nzBlock nzSize="large" (click)="goLogin()">返回登录</button>
        } @else {
          @if (notice) {
            <nz-alert [nzType]="notice.type" [nzMessage]="notice.text" nzShowIcon style="margin-bottom: 16px;"></nz-alert>
          }

          @if (modes.length > 1) {
            <nz-radio-group [(ngModel)]="mode" nzButtonStyle="solid" style="display: block; text-align: center; margin-bottom: 20px;">
              @for (item of modes; track item) {
                <label nz-radio-button [nzValue]="item">{{ modeLabels[item] }}</label>
              }
            </nz-radio-group>
          } @else {
            <div style="text-align: center; color: rgba(0, 0, 0, 0.45); margin-bottom: 16px;">{{ modeLabels[mode] }}</div>
          }

          <form nz-form (ngSubmit)="handleSubmit()">
            <nz-form-item>
              <nz-form-label>{{ usernameLabel }}</nz-form-label>
              <nz-form-control>
                <input nz-input [(ngModel)]="form.username" name="username" placeholder="请输入用户名" />
                @if (errors.username) {
                  <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.username }}</div>
                }
              </nz-form-control>
            </nz-form-item>

            @if (showEmail) {
              <nz-form-item>
                <nz-form-label>{{ emailLabel }}</nz-form-label>
                <nz-form-control>
                  <input nz-input type="email" [(ngModel)]="form.email" name="email" placeholder="请输入邮箱" />
                  @if (errors.email) {
                    <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.email }}</div>
                  }
                </nz-form-control>
              </nz-form-item>
            }

            @if (showMobile) {
              <nz-form-item>
                <nz-form-label>{{ mobileLabel }}</nz-form-label>
                <nz-form-control>
                  <input nz-input [(ngModel)]="form.mobile" name="mobile" placeholder="请输入手机号" />
                  @if (errors.mobile) {
                    <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.mobile }}</div>
                  }
                </nz-form-control>
              </nz-form-item>
            }

            @if (mode !== 'password') {
              <nz-form-item>
                <nz-form-label>{{ codeLabel }}</nz-form-label>
                <nz-form-control>
                  <div style="display: flex; gap: 8px;">
                    <input nz-input [(ngModel)]="form.code" name="code" [placeholder]="'请输入' + codeLabel" />
                    <button nz-button type="button" [nzLoading]="sending" [disabled]="countdown > 0 || sending" (click)="handleSendCode()">
                      {{ countdown > 0 ? countdown + 's' : '发送验证码' }}
                    </button>
                  </div>
                  @if (errors.code) {
                    <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.code }}</div>
                  }
                </nz-form-control>
              </nz-form-item>
            }

            <nz-form-item>
              <nz-form-label>{{ passwordLabel }}</nz-form-label>
              <nz-form-control>
                <input nz-input type="password" [(ngModel)]="form.password" name="password" placeholder="请输入密码" />
                @if (errors.password) {
                  <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.password }}</div>
                }
              </nz-form-control>
            </nz-form-item>

            <nz-form-item>
              <nz-form-label>确认密码</nz-form-label>
              <nz-form-control>
                <input nz-input type="password" [(ngModel)]="form.confirmPassword" name="confirmPassword" placeholder="请再次输入密码" />
                @if (errors.confirmPassword) {
                  <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.confirmPassword }}</div>
                }
              </nz-form-control>
            </nz-form-item>

            @if (captchaRequired) {
              <nz-form-item>
                <nz-form-label>图片验证码</nz-form-label>
                <nz-form-control>
                  <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                    @if (captcha?.image) {
                      <img [src]="captcha?.image || ''" alt="图片验证码" style="height: 40px; width: 120px; object-fit: contain; border: 1px solid #d9d9d9; border-radius: 4px;" />
                    }
                    <button nz-button type="button" [nzLoading]="captchaLoading" (click)="refreshCaptcha()">刷新</button>
                  </div>
                  <input nz-input [(ngModel)]="form.captchaCode" name="captchaCode" placeholder="请输入图片验证码" />
                  @if (errors.captchaCode) {
                    <div style="color: #ff4d4f; font-size: 12px; margin-top: 4px;">{{ errors.captchaCode }}</div>
                  }
                </nz-form-control>
              </nz-form-item>
            }

            <button nz-button nzType="primary" nzBlock nzSize="large" [nzLoading]="submitting">
              {{ mode === 'password' ? '立即注册' : '注册并登录' }}
            </button>
          </form>
        }

        <div style="text-align: center; margin-top: 16px; color: rgba(0, 0, 0, 0.45);">
          已有账号？
          <button nz-button nzType="link" (click)="goLogin()">去登录</button>
        </div>
      </nz-card>
    </div>
  `,
})
export class RegisterComponent implements OnInit, OnDestroy {
  /** 登录/注册配置 */
  config: LoginConfig | null = null;
  /** 配置加载中 */
  configLoading = true;
  /** 配置加载错误 */
  loadError = '';
  /** 当前注册方式 */
  mode: RegisterMode = 'password';
  /** 注册表单 */
  form: RegisterFormInput = {
    username: '',
    email: '',
    mobile: '',
    password: '',
    confirmPassword: '',
    code: '',
    captchaCode: '',
  };
  /** 表单字段错误 */
  errors: RegisterFormErrors = {};
  /** 提交中 */
  submitting = false;
  /** 发送验证码中 */
  sending = false;
  /** 发码倒计时 */
  countdown = 0;
  /** 页面提示 */
  notice: Notice | null = null;
  /** 待激活结果 */
  pending: PendingResult | null = null;
  /** 图片验证码 */
  captcha: { captchaId: string; image: string } | null = null;
  /** 图片验证码加载中 */
  captchaLoading = false;
  /** 注册方式名称 */
  readonly modeLabels = modeLabels;

  /** 租户参数，透传 X-App-Id / X-Tenant */
  private tenant: { appId?: string; tenantCode?: string } = {};
  /** 共享认证逻辑 */
  private authLogic: AuthLogic;
  /** 发码倒计时定时器 */
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor(
    private api: ApiService,
    private userService: UserService,
    private router: Router,
    private route: ActivatedRoute,
    private message: NzMessageService,
  ) {
    this.authLogic = new AuthLogic(this.api.api, () => {});
  }

  /** 后端允许的注册方式 */
  get modes(): RegisterMode[] {
    return resolveRegisterModes(this.config?.register);
  }

  /** 是否开放注册 */
  get registerAvailable(): boolean {
    return this.modes.length > 0;
  }

  /** 注册提交是否需要图片验证码 */
  get registerCaptchaRequired(): boolean {
    return this.config?.register?.captcha === true;
  }

  /** 发送验证码是否需要图片验证码 */
  get sendCodeCaptchaRequired(): boolean {
    return this.config?.login?.sendCode === true;
  }

  /** 当前场景是否需要图片验证码 */
  get captchaRequired(): boolean {
    return this.registerCaptchaRequired || this.sendCodeCaptchaRequired;
  }

  /** 是否展示邮箱输入 */
  get showEmail(): boolean {
    return this.mode !== 'mobile' || this.config?.register?.requireMailVerify === true;
  }

  /** 是否展示手机输入 */
  get showMobile(): boolean {
    return this.mode !== 'mail' || this.config?.register?.requireMobileVerify === true;
  }

  /** 用户名标签 */
  get usernameLabel(): string {
    return this.mode === 'password' ? '用户名' : '用户名（可选）';
  }

  /** 邮箱标签 */
  get emailLabel(): string {
    return this.mode === 'mail' || this.config?.register?.requireMailVerify === true ? '邮箱 *' : '邮箱（可选）';
  }

  /** 手机标签 */
  get mobileLabel(): string {
    return this.mode === 'mobile' || this.config?.register?.requireMobileVerify === true ? '手机号 *' : '手机号（可选）';
  }

  /** 密码标签 */
  get passwordLabel(): string {
    return this.mode === 'password' ? '密码 *' : '密码（可选）';
  }

  /** 验证码标签 */
  get codeLabel(): string {
    return this.mode === 'mobile' ? '手机验证码' : '邮箱验证码';
  }

  /** 待激活渠道文案 */
  get pendingChannelText(): string {
    return this.pending?.channels.map((item) => channelLabels[item] ?? item).join('、') ?? '';
  }

  ngOnInit() {
    this.tenant = resolveRegisterTenant(this.route.snapshot.queryParams as Record<string, unknown>);
    void this.loadConfig();
  }

  ngOnDestroy() {
    this.stopCountdown();
  }

  /** 加载登录/注册配置，按租户参数对齐后端能力 */
  async loadConfig() {
    this.configLoading = true;
    this.loadError = '';
    try {
      const res = await this.api.user.getLoginConfig(this.tenant.tenantCode);
      this.config = res.data ?? null;
      if (this.modes.length > 0 && !this.modes.includes(this.mode)) this.mode = this.modes[0];
      if (this.captchaRequired) await this.refreshCaptcha();
    } catch (error) {
      this.loadError = getRegisterErrorMessage(error);
    } finally {
      this.configLoading = false;
    }
  }

  /** 刷新图片验证码 */
  async refreshCaptcha() {
    this.captchaLoading = true;
    try {
      const res = await this.api.user.getCaptcha();
      this.captcha = res.data ? { captchaId: res.data.captchaId, image: res.data.image } : null;
      this.form.captchaCode = '';
      this.clearError('captchaCode');
    } catch {
      this.captcha = null;
      this.notice = { type: 'error', text: '图片验证码加载失败，请点击刷新重试' };
    } finally {
      this.captchaLoading = false;
    }
  }

  /** 发送手机/邮箱注册验证码 */
  async handleSendCode() {
    if (this.sending || this.countdown > 0) return;

    const isSms = this.mode === 'mobile';
    const account = (isSms ? this.form.mobile : this.form.email).trim();
    if (!account) {
      this.errors = { ...this.errors, [isSms ? 'mobile' : 'email']: isSms ? '请输入手机号' : '请输入邮箱' };
      return;
    }
    if (isSms && !isValidMobile(account)) {
      this.errors = { ...this.errors, mobile: '手机号格式不正确' };
      return;
    }
    if (!isSms && !isValidEmail(account)) {
      this.errors = { ...this.errors, email: '邮箱格式不正确' };
      return;
    }
    if (this.sendCodeCaptchaRequired) {
      if (!this.captcha?.captchaId) {
        this.errors = { ...this.errors, captchaCode: '图片验证码加载失败，请刷新' };
        return;
      }
      if (!this.form.captchaCode.trim()) {
        this.errors = { ...this.errors, captchaCode: '请输入图片验证码' };
        return;
      }
    }

    this.sending = true;
    try {
      // 发码需要按 login.sendCode 透传图片验证码，AuthLogic/RegisterLogic 未覆盖该场景，
      // 因此这里直接调用 api-core 的 sendCode；注册提交统一走 AuthLogic.register。
      await this.api.user.sendCode({
        channel: isSms ? 'Sms' : 'Mail',
        username: account,
        action: 'register',
        ...(this.sendCodeCaptchaRequired && this.captcha
          ? { captchaId: this.captcha.captchaId, captchaCode: this.form.captchaCode.trim() }
          : {}),
      });
      this.notice = { type: 'success', text: '验证码已发送，请查收' };
      this.startCountdown();
      if (this.sendCodeCaptchaRequired) await this.refreshCaptcha();
    } catch (error) {
      this.notice = { type: 'error', text: getRegisterErrorMessage(error) };
      if (this.sendCodeCaptchaRequired) await this.refreshCaptcha();
    } finally {
      this.sending = false;
    }
  }

  /** 提交注册 */
  async handleSubmit() {
    if (this.submitting) return;

    const result = validateRegister(this.form, this.config, this.mode, {
      captchaRequired: this.registerCaptchaRequired,
      captchaId: this.registerCaptchaRequired ? this.captcha?.captchaId : undefined,
    });
    this.errors = result.errors;
    if (!result.valid) {
      this.notice = { type: 'warning', text: '请检查输入信息' };
      return;
    }

    this.submitting = true;
    this.notice = null;
    const payload = buildRegisterPayload(this.form, this.mode, {
      captchaId: this.registerCaptchaRequired ? this.captcha?.captchaId : undefined,
      captchaCode: this.registerCaptchaRequired ? this.form.captchaCode : undefined,
      appId: this.tenant.appId,
      tenantCode: this.tenant.tenantCode,
    });

    try {
      const res = await this.authLogic.register(payload);

      // 待激活：后端已发送激活邮件/短信，不返回 token
      if (res.data?.pendingActivation) {
        this.pending = {
          message: res.message || '注册成功，请先激活邮箱/手机后再登录',
          channels: res.data.channels ?? [],
          targets: res.data.targets ?? [],
        };
        return;
      }

      // 注册成功并已登录
      if (res.data?.accessToken) {
        this.message.success(res.message || '注册成功，正在进入系统...');
        await this.userService.fetchUserInfo().catch(() => undefined);
        await this.userService.fetchMenus().catch(() => undefined);
        await this.router.navigate(['/home']);
        return;
      }

      this.message.success(res.message || '注册成功，请登录');
      await this.goLogin();
    } catch (error) {
      this.notice = { type: 'error', text: getRegisterErrorMessage(error) };
      if (this.registerCaptchaRequired) await this.refreshCaptcha();
    } finally {
      this.submitting = false;
    }
  }

  /** 返回登录页，保留租户查询参数 */
  async goLogin() {
    const queryParams: Record<string, string> = {};
    if (this.tenant.appId) queryParams['appId'] = this.tenant.appId;
    if (this.tenant.tenantCode) queryParams['tenant'] = this.tenant.tenantCode;
    await this.router.navigate(['/login'], {
      queryParams: Object.keys(queryParams).length > 0 ? queryParams : undefined,
    });
  }

  /** 清除单个字段错误 */
  private clearError(field: keyof RegisterFormErrors) {
    if (this.errors[field]) this.errors = { ...this.errors, [field]: undefined };
  }

  /** 启动发码倒计时 */
  private startCountdown(seconds = 60) {
    this.stopCountdown();
    this.countdown = seconds;
    this.timer = setInterval(() => {
      this.countdown -= 1;
      if (this.countdown <= 0) this.stopCountdown();
    }, 1000);
  }

  /** 停止发码倒计时 */
  private stopCountdown() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
