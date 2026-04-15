<template>
	<div class="forgot-container">
		<div class="forgot-card-wrap">
			<el-card class="forgot-card" shadow="never">
				<div class="forgot-header">
					<img :src="logoSrc" class="forgot-logo" />
					<div class="forgot-title">{{ siteStore.loginConfig.displayName || '重置密码' }}</div>
					<div class="forgot-subtitle">通过验证码重置账号密码</div>
				</div>

				<el-form v-if="state.step === 'input'" size="large" class="forgot-content-form" ref="step1FormRef" :model="state.form" :rules="step1Rules">
						<el-form-item prop="username">
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
						<el-form-item prop="channel">
							<el-radio-group v-model="state.form.channel" class="forgot-channel-group">
								<el-radio-button value="Sms">短信验证码</el-radio-button>
								<el-radio-button value="Mail">邮箱验证码</el-radio-button>
							</el-radio-group>
						</el-form-item>
						<el-form-item>
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
						<el-form-item>
							<div class="forgot-back-link">
								<el-link type="primary" :underline="false" @click="router.push('/login')">返回登录</el-link>
							</div>
						</el-form-item>
					</el-form>

					<el-form v-else size="large" class="forgot-content-form" ref="step2FormRef" :model="state.form" :rules="step2Rules">
						<el-form-item prop="code">
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
						<el-form-item prop="newPassword">
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
						<el-form-item prop="confirmPassword">
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
						<el-form-item>
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
						<el-form-item>
							<div class="forgot-back-link">
								<el-link type="default" :underline="false" @click="state.step = 'input'">上一步</el-link>
								<el-divider direction="vertical" />
								<el-link type="primary" :underline="false" @click="router.push('/login')">返回登录</el-link>
							</div>
						</el-form-item>
					</el-form>
			</el-card>
		</div>
	</div>
</template>

<script setup lang="ts" name="forgotPassword">
import { reactive, ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { useSiteInfo } from '/@/stores/siteInfo';
import { useUserApi } from '/@/api/user';
import { NextLoading } from '/@/utils/loading';
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

onMounted(async () => {
	NextLoading.done();
	await Promise.all([siteStore.loadSiteInfo(), siteStore.loadLoginConfig()]);
});
</script>

<style scoped lang="scss">
.forgot-container {
	min-height: 100vh;
	display: flex;
	justify-content: center;
	align-items: center;
	padding: 32px 16px;
	background: linear-gradient(135deg, #165dff 0%, #722ed1 100%);

	.forgot-card-wrap {
		width: 100%;
		display: flex;
		justify-content: center;
	}

	.forgot-card {
		width: 440px;
		max-width: 100%;
		border-radius: 12px;
		border: none;
		:deep(.el-card__body) {
			padding: 28px 24px 20px;
		}
	}

	.forgot-header {
		text-align: center;
		margin-bottom: 16px;
		.forgot-logo {
			width: 52px;
			height: 52px;
			object-fit: contain;
			margin-bottom: 10px;
		}
		.forgot-title {
			font-size: 22px;
			font-weight: 600;
			color: var(--el-text-color-primary);
		}
		.forgot-subtitle {
			margin-top: 4px;
			font-size: 13px;
			color: var(--el-text-color-secondary);
		}
	}

	.forgot-content-form {
		:deep(.el-form-item) {
			margin-bottom: 14px;
		}
		:deep(.el-input__wrapper),
		:deep(.el-textarea__inner) {
			border-radius: 6px;
			transition: box-shadow .2s ease, border-color .2s ease;
		}
		:deep(.el-input.is-focus .el-input__wrapper),
		:deep(.el-textarea .el-textarea__inner:focus) {
			box-shadow: 0 0 0 1px var(--el-color-primary), 0 0 0 3px var(--el-color-primary-light-9);
		}
		:deep(.el-form-item__error) {
			line-height: 1.2;
			padding-top: 2px;
			position: relative;
			top: 1px;
		}
		.forgot-content-submit {
			width: 100%;
			font-weight: 500;
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

@media (max-width: 576px) {
	.forgot-container {
		padding: 16px 12px;
		.forgot-card :deep(.el-card__body) {
			padding: 18px 16px 16px;
		}
		.forgot-header .forgot-title {
			font-size: 20px;
		}
	}
}
</style>
