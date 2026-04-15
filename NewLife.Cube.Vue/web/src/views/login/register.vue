<template>
	<div class="register-container">
		<div class="register-card-wrap">
			<el-card class="register-card" shadow="never">
				<div class="register-header">
					<img :src="logoSrc" class="register-logo" />
					<div class="register-title">{{ siteStore.loginConfig.displayName || '注册账号' }}</div>
				</div>

				<div class="register-main">
					<el-tabs v-if="!state.oauthMode" v-model="state.registerTab">
						<el-tab-pane label="账号注册" name="password" />
						<el-tab-pane v-if="enableSmsRegister" label="手机注册" name="phone" />
						<el-tab-pane v-if="enableMailRegister" label="邮箱注册" name="email" />
					</el-tabs>

					<div v-if="state.oauthMode" class="register-oauth-tip">
						<el-alert
							title="第三方账号首次登录，请补充密码完成本地账号创建"
							type="info"
							:closable="false"
						/>
					</div>

					<el-form size="large" class="register-content-form" ref="formRef" :model="state.ruleForm" :rules="rules">
						<el-form-item v-if="state.registerTab === 'password' || state.oauthMode" prop="username">
							<el-input
								text
								placeholder="请输入用户名"
								v-model="state.ruleForm.username"
								clearable
								autocomplete="off"
								:readonly="state.oauthMode"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-User /></el-icon>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item v-if="state.registerTab === 'password' || state.registerTab === 'email' || state.oauthMode" prop="email">
							<el-input
								text
								:placeholder="state.registerTab === 'email' ? '请输入邮箱地址（用于接收验证码）' : '请输入邮箱地址'"
								v-model="state.ruleForm.email"
								clearable
								autocomplete="off"
								:readonly="state.oauthMode && !!state.ruleForm.email"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Message /></el-icon>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item v-if="state.registerTab === 'phone'" prop="mobile">
							<el-input
								text
								placeholder="请输入手机号"
								v-model="state.ruleForm.mobile"
								clearable
								autocomplete="off"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Phone /></el-icon>
								</template>
								<template #append>
									<el-button :disabled="state.countdown > 0" :loading="state.sending" @click="onSendCode('Sms')">
										{{ state.countdown > 0 ? `${state.countdown}s` : '发送验证码' }}
									</el-button>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item v-if="state.registerTab === 'email'" prop="emailCodeTarget">
							<el-input
								text
								placeholder="请输入邮箱"
								v-model="state.ruleForm.emailCodeTarget"
								clearable
								autocomplete="off"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Message /></el-icon>
								</template>
								<template #append>
									<el-button :disabled="state.countdown > 0" :loading="state.sending" @click="onSendCode('Mail')">
										{{ state.countdown > 0 ? `${state.countdown}s` : '发送验证码' }}
									</el-button>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item v-if="state.registerTab === 'phone' || state.registerTab === 'email'" prop="code">
							<el-input
								text
								placeholder="请输入验证码"
								v-model="state.ruleForm.code"
								clearable
								autocomplete="off"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Key /></el-icon>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item prop="password">
							<el-input
								:type="state.isShowPassword ? 'text' : 'password'"
								placeholder="请输入密码"
								v-model="state.ruleForm.password"
								autocomplete="new-password"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Unlock /></el-icon>
								</template>
								<template #suffix>
									<i
										class="iconfont el-input__icon login-content-password"
										:class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'"
										@click="state.isShowPassword = !state.isShowPassword"
									></i>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item prop="password2">
							<el-input
								:type="state.isShowPassword2 ? 'text' : 'password'"
								placeholder="请再次输入密码"
								v-model="state.ruleForm.password2"
								autocomplete="new-password"
							>
								<template #prefix>
									<el-icon class="el-input__icon"><ele-Unlock /></el-icon>
								</template>
								<template #suffix>
									<i
										class="iconfont el-input__icon login-content-password"
										:class="state.isShowPassword2 ? 'icon-yincangmima' : 'icon-xianshimima'"
										@click="state.isShowPassword2 = !state.isShowPassword2"
									></i>
								</template>
							</el-input>
						</el-form-item>
						<el-form-item>
							<el-button round type="primary" v-waves class="register-content-submit" @click="onRegister" :loading="state.loading">
								<span>{{ state.oauthMode ? '完成绑定并登录' : '立即注册' }}</span>
							</el-button>
						</el-form-item>
						<div class="register-go-login">
							<span class="font12">已有账号？</span>
							<el-link type="primary" :underline="false" @click="router.push('/login')">立即登录</el-link>
						</div>
					</el-form>
				</div>
			</el-card>
		</div>

		<div v-if="siteStore.siteInfo.copyright || siteStore.siteInfo.registration" class="register-footer">
			<div v-if="siteStore.siteInfo.copyright" class="register-footer-copyright" v-html="siteStore.siteInfo.copyright"></div>
			<div v-if="siteStore.siteInfo.registration" class="register-footer-registration">
				<a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ siteStore.siteInfo.registration }}</a>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="loginRegister">
import { reactive, ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { useSiteInfo } from '/@/stores/siteInfo';
import { useUserApi } from '/@/api/user';
import { NextLoading } from '/@/utils/loading';
import { Session } from '/@/utils/storage';
import { RegisterCategory } from '/@/model/api/login';
import logoMiniDefault from '/@/assets/logo-mini.png';

const router = useRouter();
const route = useRoute();
const siteStore = useSiteInfo();
const userApi = useUserApi();
const formRef = ref<FormInstance>();

const state = reactive({
	registerTab: 'password' as 'password' | 'phone' | 'email',
	oauthMode: false,
	ruleForm: {
		username: '',
		email: '',
		emailCodeTarget: '',
		mobile: '',
		code: '',
		oauthToken: '',
		password: '',
		password2: '',
	},
	isShowPassword: false,
	isShowPassword2: false,
	sending: false,
	countdown: 0,
	loading: false,
});

let _countdownTimer: ReturnType<typeof setInterval> | null = null;

const logoSrc = computed(() => siteStore.siteInfo.loginLogo || logoMiniDefault);
const enableSmsRegister = computed(() => !!(siteStore.loginConfig.enableSmsRegister ?? siteStore.loginConfig.enableSms));
const enableMailRegister = computed(() => !!(siteStore.loginConfig.enableMailRegister ?? siteStore.loginConfig.enableMail));

const rules: FormRules = {
	username: [
		{ required: true, message: '请输入用户名', trigger: 'blur' },
		{ min: 3, max: 32, message: '用户名长度为 3-32 个字符', trigger: 'blur' },
	],
	email: [
		{ required: false, message: '请输入邮箱地址', trigger: 'blur' },
		{ type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' },
	],
	emailCodeTarget: [
		{ required: false, message: '请输入邮箱地址', trigger: 'blur' },
		{ type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' },
	],
	mobile: [{ required: false, message: '请输入手机号', trigger: 'blur' }],
	code: [{ required: false, message: '请输入验证码', trigger: 'blur' }],
	password: [
		{ required: true, message: '请输入密码', trigger: 'blur' },
		{ min: 6, message: '密码长度不少于 6 个字符', trigger: 'blur' },
	],
	password2: [
		{ required: true, message: '请再次输入密码', trigger: 'blur' },
		{
			validator: (_rule, value, callback) => {
				if (value !== state.ruleForm.password)
					callback(new Error('两次输入的密码不一致'));
				else
					callback();
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

const onSendCode = async (channel: 'Sms' | 'Mail') => {
	if (state.countdown > 0) return;
	const username = channel === 'Sms' ? state.ruleForm.mobile : state.ruleForm.emailCodeTarget;
	if (!username) {
		ElMessage.warning(channel === 'Sms' ? '请输入手机号' : '请输入邮箱');
		return;
	}
	state.sending = true;
	try {
		await userApi.sendCode({
			channel,
			username,
			action: 'register',
		});
		startCountdown();
		ElMessage.success('验证码已发送');
	} catch (err: any) {
		ElMessage.error(err?.message || '发送失败，请稍后重试');
	} finally {
		state.sending = false;
	}
};

const onRegister = async () => {
	if (state.registerTab === 'phone') {
		if (!state.ruleForm.mobile) return ElMessage.warning('请输入手机号');
		if (!state.ruleForm.code) return ElMessage.warning('请输入验证码');
	}
	if (state.registerTab === 'email') {
		if (!state.ruleForm.emailCodeTarget) return ElMessage.warning('请输入邮箱地址');
		if (!state.ruleForm.code) return ElMessage.warning('请输入验证码');
	}

	const valid = await formRef.value?.validate().catch(() => false);
	if (!valid) return;

	if (state.ruleForm.password !== state.ruleForm.password2) {
		ElMessage.warning('两次输入的密码不一致');
		return;
	}

	state.loading = true;
	try {
		const registerPayload = state.oauthMode
			? {
				registerCategory: RegisterCategory.OAuthBind,
				oauthToken: state.ruleForm.oauthToken,
				username: state.ruleForm.username,
				email: state.ruleForm.email,
				password: state.ruleForm.password,
				confirmPassword: state.ruleForm.password2,
			}
			: state.registerTab === 'phone'
				? {
					registerCategory: RegisterCategory.Phone,
					mobile: state.ruleForm.mobile,
					username: state.ruleForm.username || state.ruleForm.mobile,
					email: state.ruleForm.email,
					code: state.ruleForm.code,
					password: state.ruleForm.password,
					confirmPassword: state.ruleForm.password2,
				}
				: state.registerTab === 'email'
					? {
						registerCategory: RegisterCategory.Email,
						email: state.ruleForm.emailCodeTarget,
						username: state.ruleForm.username || state.ruleForm.emailCodeTarget,
						code: state.ruleForm.code,
						password: state.ruleForm.password,
						confirmPassword: state.ruleForm.password2,
					}
					: {
						registerCategory: RegisterCategory.Password,
						username: state.ruleForm.username,
						email: state.ruleForm.email,
						password: state.ruleForm.password,
						confirmPassword: state.ruleForm.password2,
					};

		const res = await userApi.register(registerPayload);
		const token = res?.data?.accessToken || res?.data?.token;
		if (token) {
			Session.set('token', token);
			ElMessage.success('注册成功，已自动登录');
			router.push('/');
			return;
		}

		ElMessage.success('注册成功，请登录');
		router.push('/login');
	} catch (err: any) {
		ElMessage.error(err?.message || '注册失败，请稍后重试');
	} finally {
		state.loading = false;
	}
};

onMounted(async () => {
	NextLoading.done();
	await Promise.all([siteStore.loadSiteInfo(), siteStore.loadLoginConfig()]);

	const oauthToken = (route.query.oauthToken as string) || '';
	if (!oauthToken) return;

	state.oauthMode = true;
	state.ruleForm.oauthToken = oauthToken;
	try {
		const rs = await userApi.getOAuthPendingInfo(oauthToken);
		if (rs?.data) {
			state.ruleForm.username = rs.data.username || state.ruleForm.username;
			state.ruleForm.email = rs.data.email || state.ruleForm.email;
			state.ruleForm.mobile = rs.data.mobile || state.ruleForm.mobile;
		}
	} catch (err: any) {
		ElMessage.warning(err?.message || 'OAuth 预填信息已过期，请重新发起第三方登录');
	}
});
</script>

<style scoped lang="scss">
.register-container {
	min-height: 100vh;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 32px 16px 20px;
	background: linear-gradient(135deg, #165dff 0%, #722ed1 100%);

	.register-card-wrap {
		width: 100%;
		display: flex;
		justify-content: center;
	}

	.register-card {
		width: 500px;
		max-width: 100%;
		border-radius: 12px;
		border: none;
		:deep(.el-card__body) {
			padding: 28px 28px 22px;
		}
	}

	.register-header {
		text-align: center;
		margin-bottom: 12px;
		.register-logo {
			width: 52px;
			height: 52px;
			object-fit: contain;
			margin-bottom: 10px;
		}
		.register-title {
			font-size: 22px;
			font-weight: 600;
			color: var(--el-text-color-primary);
		}
	}

	.register-main {
		:deep(.el-tabs__header) {
			margin-bottom: 8px;
		}
	}

	.register-content-form {
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
		.register-content-submit {
			width: 100%;
			font-weight: 500;
		}
		.register-go-login {
			text-align: center;
			margin-top: 12px;
			color: var(--el-text-color-placeholder);
		}
	}

	.register-footer {
		margin-top: 16px;
		text-align: center;
		font-size: 12px;
		color: rgba(255, 255, 255, 0.75);
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 4px;
		.register-footer-copyright {
			:deep(a) {
				color: rgba(255, 255, 255, 0.78);
				&:hover {
					color: #ffffff;
				}
			}
		}
		.register-footer-registration {
			a {
				color: rgba(255, 255, 255, 0.78);
				text-decoration: none;
				&:hover {
					color: #ffffff;
					text-decoration: underline;
				}
			}
		}
	}
}

@media (max-width: 576px) {
	.register-container {
		padding: 16px 12px;
		.register-card :deep(.el-card__body) {
			padding: 18px 16px 16px;
		}
		.register-header .register-title {
			font-size: 20px;
		}
	}
}
</style>
