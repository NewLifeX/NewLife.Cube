<template>
	<div class="forgot-container flex">
		<div class="forgot-left">
			<div class="forgot-left-logo">
				<img :src="logoSrc" />
				<div class="forgot-left-logo-text">
					<span>{{ siteStore.loginConfig.displayName }}</span>
				</div>
			</div>
		</div>
		<div class="forgot-right flex">
			<div class="forgot-right-warp flex-margin">
				<span class="forgot-right-warp-one"></span>
				<span class="forgot-right-warp-two"></span>
				<div class="forgot-right-warp-main">
					<div class="forgot-right-warp-main-title">重置密码</div>
					<!-- 步骤一：输入账号并发送验证码 -->
					<el-form v-if="state.step === 'input'" size="large" class="forgot-content-form" ref="step1FormRef" :model="state.form" :rules="step1Rules">
						<el-form-item prop="username" class="forgot-animation1">
							<el-input
								text
								placeholder="请输入手机号或邮箱"
								v-model="state.form.username"
								clearable
								autocomplete="off"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-User /></el-icon>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item prop="channel" class="forgot-animation2">
							<el-radio-group v-model="state.form.channel" class="forgot-channel-group">
								<el-radio-button value="Sms">短信验证码</el-radio-button>
								<el-radio-button value="Mail">邮箱验证码</el-radio-button>
							</el-radio-group>
						</el-form-item>
						<el-form-item class="forgot-animation3">
							<el-button
								type="primary"
								class="forgot-content-submit"
								round
								:loading="state.sending"
								@click="onSendCode"
							>
								发送验证码
							</el-button>
						</el-form-item>
						<el-form-item class="forgot-animation4">
							<div class="forgot-back-link">
								<el-link type="primary" :underline="false" @click="router.push('/login')">返回登录</el-link>
							</div>
						</el-form-item>
					</el-form>

					<!-- 步骤二：输入验证码和新密码 -->
					<el-form v-else size="large" class="forgot-content-form" ref="step2FormRef" :model="state.form" :rules="step2Rules">
						<el-form-item prop="code" class="forgot-animation1">
							<el-input
								text
								placeholder="请输入验证码"
								v-model="state.form.code"
								clearable
								autocomplete="off"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Key /></el-icon>
								</template>
								<template #append>
									<el-button
										:disabled="state.countdown > 0"
										:loading="state.sending"
										@click="onResend"
									>
										{{ state.countdown > 0 ? `${state.countdown}s` : '重新发送' }}
									</el-button>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item prop="newPassword" class="forgot-animation2">
							<el-input
								:type="state.showPwd ? 'text' : 'password'"
								placeholder="请输入新密码"
								v-model="state.form.newPassword"
								autocomplete="new-password"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Unlock /></el-icon>
								</template>
								<template #suffix>
									<i
										class="iconfont el-input__icon login-content-password"
										:class="state.showPwd ? 'icon-yincangmima' : 'icon-xianshimima'"
										@click="state.showPwd = !state.showPwd"
									/>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item prop="confirmPassword" class="forgot-animation3">
							<el-input
								:type="state.showPwd2 ? 'text' : 'password'"
								placeholder="请再次输入新密码"
								v-model="state.form.confirmPassword"
								autocomplete="new-password"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Unlock /></el-icon>
								</template>
								<template #suffix>
									<i
										class="iconfont el-input__icon login-content-password"
										:class="state.showPwd2 ? 'icon-yincangmima' : 'icon-xianshimima'"
										@click="state.showPwd2 = !state.showPwd2"
									/>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item class="forgot-animation4">
							<el-button
								type="primary"
								class="forgot-content-submit"
								round
								:loading="state.submitting"
								@click="onConfirmReset"
							>
								确认重置
							</el-button>
						</el-form-item>
						<el-form-item class="forgot-animation5">
							<div class="forgot-back-link">
								<el-link type="default" :underline="false" @click="state.step = 'input'">上一步</el-link>
								<el-divider direction="vertical" />
								<el-link type="primary" :underline="false" @click="router.push('/login')">返回登录</el-link>
							</div>
						</el-form-item>
					</el-form>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="forgotPassword">
import { reactive, ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { useSiteInfo } from '/@/stores/siteInfo';
import { useUserApi } from '/@/api/user';
import logoMiniDefault from '/@/assets/logo-mini.png';

const router = useRouter();
const siteStore = useSiteInfo();
const userApi = useUserApi();

const step1FormRef = ref<FormInstance>();
const step2FormRef = ref<FormInstance>();

const state = reactive({
	step: 'input' as 'input' | 'verify',
	sending: false,
	submitting: false,
	countdown: 0,
	showPwd: false,
	showPwd2: false,
	form: {
		username: '',
		channel: 'Sms' as 'Sms' | 'Mail',
		code: '',
		newPassword: '',
		confirmPassword: '',
	},
});

let _countdownTimer: ReturnType<typeof setInterval> | null = null;

const logoSrc = computed(() => siteStore.siteInfo.loginLogo || logoMiniDefault);

const step1Rules: FormRules = {
	username: [{ required: true, message: '请输入手机号或邮箱', trigger: 'blur' }],
};

const step2Rules: FormRules = {
	code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
	newPassword: [
		{ required: true, message: '请输入新密码', trigger: 'blur' },
		{ min: 6, message: '密码长度不少于6位', trigger: 'blur' },
	],
	confirmPassword: [
		{ required: true, message: '请再次输入密码', trigger: 'blur' },
		{
			validator: (_rule, value, callback) => {
				if (value !== state.form.newPassword) {
					callback(new Error('两次密码不一致'));
				} else {
					callback();
				}
			},
			trigger: 'blur',
		},
	],
};

const startCountdown = (seconds = 60) => {
	if (_countdownTimer) clearInterval(_countdownTimer);
	state.countdown = seconds;
	_countdownTimer = setInterval(() => {
		state.countdown -= 1;
		if (state.countdown <= 0) {
			clearInterval(_countdownTimer!);
			_countdownTimer = null;
		}
	}, 1000);
};

const onSendCode = async () => {
	const valid = await step1FormRef.value?.validate().catch(() => false);
	if (!valid) return;
	state.sending = true;
	try {
		await userApi.sendCode({
			channel: state.form.channel,
			username: state.form.username,
			action: 'reset',
		});
		state.step = 'verify';
		startCountdown();
		ElMessage.success('验证码已发送');
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
		ElMessage.error(msg);
	} finally {
		state.sending = false;
	}
};

const onResend = async () => {
	if (state.countdown > 0) return;
	state.sending = true;
	try {
		await userApi.sendCode({
			channel: state.form.channel,
			username: state.form.username,
			action: 'reset',
		});
		startCountdown();
		ElMessage.success('验证码已重新发送');
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
		ElMessage.error(msg);
	} finally {
		state.sending = false;
	}
};

const onConfirmReset = async () => {
	const valid = await step2FormRef.value?.validate().catch(() => false);
	if (!valid) return;
	state.submitting = true;
	try {
		await userApi.resetPassword({
			username: state.form.username,
			code: state.form.code,
			newPassword: state.form.newPassword,
			confirmPassword: state.form.confirmPassword,
		});
		ElMessage.success('密码重置成功，请重新登录');
		router.push('/login');
	} catch (e: unknown) {
		const msg = e instanceof Error ? e.message : '重置失败，请重试';
		ElMessage.error(msg);
	} finally {
		state.submitting = false;
	}
};
</script>

<style scoped lang="scss">
.forgot-container {
	width: 100%;
	height: 100vh;
	overflow: hidden;
	background: var(--el-bg-color-page, #f0f2f5);

	.forgot-left {
		flex: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		background: linear-gradient(135deg, #26a59a 0%, #2c6e49 100%);
		.forgot-left-logo {
			display: flex;
			flex-direction: column;
			align-items: center;
			color: #fff;
			img {
				width: 80px;
				height: 80px;
				margin-bottom: 16px;
			}
			.forgot-left-logo-text span {
				font-size: 24px;
			}
		}
	}

	.forgot-right {
		width: 420px;
		background: var(--el-bg-color, #fff);
		.forgot-right-warp {
			width: 320px;
			.forgot-right-warp-one,
			.forgot-right-warp-two {
				position: absolute;
				width: 8px;
				height: 8px;
				border-radius: 50%;
				background: var(--el-color-primary);
				opacity: 0.3;
			}
			.forgot-right-warp-two {
				top: 10px;
				left: 10px;
			}
			.forgot-right-warp-main {
				.forgot-right-warp-main-title {
					font-size: 22px;
					font-weight: 700;
					margin-bottom: 24px;
					color: var(--el-text-color-primary);
				}
				.forgot-content-form {
					.forgot-content-submit {
						width: 100%;
					}
					.forgot-channel-group {
						width: 100%;
						display: flex;
						.el-radio-button {
							flex: 1;
							:deep(.el-radio-button__inner) {
								width: 100%;
							}
						}
					}
					.forgot-back-link {
						display: flex;
						align-items: center;
						gap: 8px;
					}
				}
			}
		}
	}
}

@media (max-width: 768px) {
	.forgot-container .forgot-left {
		display: none;
	}
	.forgot-container .forgot-right {
		width: 100%;
	}
}
</style>
