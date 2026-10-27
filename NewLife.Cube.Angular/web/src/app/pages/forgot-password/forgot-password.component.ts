import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { ApiService } from '../../services/api.service';
import { validateResetPassword, type ResetPasswordForm } from '../../utils/forgot-password';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule, FormsModule, NzAlertModule, NzButtonModule, NzCardModule,
    NzFormModule, NzInputModule, NzRadioModule,
  ],
  template: `
    <div class="forgot-wrap">
      <nz-card nzTitle="重置密码" [nzBordered]="false" class="forgot-card">
        <form nz-form *ngIf="step === 'input'" (ngSubmit)="onSendCode()">
          <nz-form-item>
            <nz-form-label nzRequired>手机号或邮箱</nz-form-label>
            <nz-form-control [nzErrorTip]="errors.username">
              <input nz-input name="username" [(ngModel)]="form.username" placeholder="请输入手机号或邮箱" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label>验证渠道</nz-form-label>
            <nz-form-control>
              <nz-radio-group [(ngModel)]="form.channel" name="channel">
                <label nz-radio nzValue="Sms">短信</label>
                <label nz-radio nzValue="Mail">邮箱</label>
              </nz-radio-group>
            </nz-form-control>
          </nz-form-item>
          <nz-alert *ngIf="error" nzType="error" [nzMessage]="error" nzShowIcon class="mb12"></nz-alert>
          <button nz-button nzType="primary" nzBlock [nzLoading]="sending">发送验证码</button>
        </form>

        <form nz-form *ngIf="step === 'verify'" (ngSubmit)="onConfirmReset()">
          <nz-form-item>
            <nz-form-label nzRequired>验证码</nz-form-label>
            <nz-form-control [nzErrorTip]="errors.code">
              <input nz-input name="code" [(ngModel)]="form.code" placeholder="请输入验证码" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>新密码</nz-form-label>
            <nz-form-control [nzErrorTip]="errors.newPassword">
              <input nz-input type="password" name="newPassword" [(ngModel)]="form.newPassword" placeholder="请输入新密码" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>确认密码</nz-form-label>
            <nz-form-control [nzErrorTip]="errors.confirmPassword">
              <input nz-input type="password" name="confirmPassword" [(ngModel)]="form.confirmPassword" placeholder="请再次输入新密码" />
            </nz-form-control>
          </nz-form-item>
          <nz-alert *ngIf="error" nzType="error" [nzMessage]="error" nzShowIcon class="mb12"></nz-alert>
          <button nz-button nzType="primary" nzBlock [nzLoading]="submitting">确认重置</button>
        </form>

        <div class="forgot-back">
          <button nz-button nzType="link" type="button" (click)="onBack()">
            {{ step === 'input' ? '返回登录' : '上一步' }}
          </button>
        </div>
      </nz-card>
    </div>
  `,
  styles: [`
    .forgot-wrap { display: flex; align-items: center; justify-content: center; min-height: 100vh; background: #f5f5f5; }
    .forgot-card { width: 440px; }
    .forgot-back { text-align: center; margin-top: 12px; }
    .mb12 { margin-bottom: 12px; }
  `],
})
export class ForgotPasswordComponent {
  step: 'input' | 'verify' = 'input';
  sending = false;
  submitting = false;
  error = '';
  errors: Partial<Record<keyof ResetPasswordForm, string>> = {};

  form: ResetPasswordForm = {
    username: '',
    channel: 'Sms',
    code: '',
    newPassword: '',
    confirmPassword: '',
  };

  constructor(
    private api: ApiService,
    private message: NzMessageService,
    private router: Router,
  ) {}

  onBack() {
    if (this.step === 'input') {
      this.router.navigate(['/login']);
      return;
    }
    this.step = 'input';
  }

  private clearErrors() {
    this.errors = {};
    this.error = '';
  }

  private async sendCode() {
    this.sending = true;
    try {
      await this.api.user.sendCode({ channel: this.form.channel, username: this.form.username, action: 'reset' });
      this.step = 'verify';
      this.message.success('验证码已发送');
    } catch (e: any) {
      this.error = e?.message ?? '发送失败，请稍后重试';
    } finally {
      this.sending = false;
    }
  }

  async onSendCode() {
    this.clearErrors();
    if (!this.form.username) {
      this.errors = { username: '请输入手机号或邮箱' };
      return;
    }
    await this.sendCode();
  }

  async onConfirmReset() {
    this.clearErrors();
    const failed = validateResetPassword(this.form);
    if (failed) {
      this.errors = { [failed[0]]: failed[1] };
      return;
    }

    this.submitting = true;
    try {
      await this.api.user.resetPassword({
        username: this.form.username,
        code: this.form.code,
        newPassword: this.form.newPassword,
        confirmPassword: this.form.confirmPassword,
      });
      this.message.success('密码重置成功，请重新登录');
      this.router.navigate(['/login']);
    } catch (e: any) {
      this.error = e?.message ?? '重置失败，请重试';
    } finally {
      this.submitting = false;
    }
  }
}
