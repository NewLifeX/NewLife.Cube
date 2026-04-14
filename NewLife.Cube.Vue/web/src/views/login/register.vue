<template>
	<div class="register-container flex">
		<div class="register-left">
			<div class="register-left-logo">
				<img :src="logoSrc" />
				<div class="register-left-logo-text">
					<span>{{ siteStore.loginConfig.displayName }}</span>
				</div>
			</div>
			<div class="register-left-img">
				<img :src="loginBackgroundSrc" />
			</div>
			<img :src="loginBackgroundWaves" class="register-left-waves" />
		</div>
		<div class="register-right flex">
			<div class="register-right-warp flex-margin">
				<span class="register-right-warp-one"></span>
				<span class="register-right-warp-two"></span>
				<div class="register-right-warp-mian">
					<div class="register-right-warp-main-title">注册账号</div>
					<div class="register-right-warp-main-form">
						<el-form size="large" class="register-content-form" ref="formRef" :model="state.ruleForm" :rules="rules">
							<el-form-item prop="username" class="register-animation1">
								<el-input
									text
									placeholder="请输入用户名"
									v-model="state.ruleForm.username"
									clearable
									autocomplete="off"
								>
									<template #prefix>
										<el-icon class="el-input__icon"><ele-User /></el-icon>
									</template>
								</el-input>
							</el-form-item>
							<el-form-item prop="email" class="register-animation2">
								<el-input
									text
									placeholder="请输入邮箱地址"
									v-model="state.ruleForm.email"
									clearable
									autocomplete="off"
								>
									<template #prefix>
										<el-icon class="el-input__icon"><ele-Message /></el-icon>
									</template>
								</el-input>
							</el-form-item>
							<el-form-item prop="password" class="register-animation3">
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
							<el-form-item prop="password2" class="register-animation4">
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
							<el-form-item class="register-animation5">
								<el-button round type="primary" v-waves class="register-content-submit" @click="onRegister" :loading="state.loading">
									<span>立即注册</span>
								</el-button>
							</el-form-item>
							<div class="register-go-login register-animation6">
								<span class="font12">已有账号？</span>
								<el-link type="primary" :underline="false" @click="router.push('/login')">立即登录</el-link>
							</div>
						</el-form>
					</div>
				</div>
			</div>
		</div>
		<!-- 底部版权信息 -->
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
import { useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { useSiteInfo } from '/@/stores/siteInfo';
import { useUserApi } from '/@/api/user';
import { NextLoading } from '/@/utils/loading';
import logoMiniDefault from '/@/assets/logo-mini.png';
import loginMainDefault from '/@/assets/login-main.svg';
import loginBackgroundWaves from '/@/assets/login-bg.svg';

const router = useRouter();
const siteStore = useSiteInfo();
const userApi = useUserApi();
const formRef = ref<FormInstance>();

const state = reactive({
	ruleForm: {
		username: '',
		email: '',
		password: '',
		password2: '',
	},
	isShowPassword: false,
	isShowPassword2: false,
	loading: false,
});

const logoSrc = computed(() => siteStore.siteInfo.loginLogo || logoMiniDefault);
const loginBackgroundSrc = computed(() => siteStore.siteInfo.loginBackground || loginMainDefault);

const rules: FormRules = {
	username: [
		{ required: true, message: '请输入用户名', trigger: 'blur' },
		{ min: 3, max: 32, message: '用户名长度为 3-32 个字符', trigger: 'blur' },
	],
	email: [
		{ required: true, message: '请输入邮箱地址', trigger: 'blur' },
		{ type: 'email', message: '请输入有效的邮箱地址', trigger: 'blur' },
	],
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

const onRegister = async () => {
	const valid = await formRef.value?.validate().catch(() => false);
	if (!valid) return;
	state.loading = true;
	try {
		await userApi.register({
			username: state.ruleForm.username,
			email: state.ruleForm.email,
			password: state.ruleForm.password,
			password2: state.ruleForm.password2,
		});
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
});
</script>

<style scoped lang="scss">
.register-container {
	height: 100%;
	display: flex;
	flex-direction: row;
	background: var(--el-color-white);
	position: relative;

	.register-left {
		flex: 1;
		position: relative;
		background-color: rgba(211, 239, 255, 1);
		margin-right: 100px;
		.register-left-logo {
			display: flex;
			align-items: center;
			position: absolute;
			top: 50px;
			left: 80px;
			z-index: 1;
			animation: logoAnimation 0.3s ease;
			img {
				width: 52px;
				height: 52px;
			}
			.register-left-logo-text {
				span {
					margin-left: 10px;
					font-size: 28px;
					color: #26a59a;
				}
			}
		}
		.register-left-img {
			position: absolute;
			top: 50%;
			left: 50%;
			transform: translate(-50%, -50%);
			width: 100%;
			height: 52%;
			img {
				width: 100%;
				height: 100%;
				animation: error-num 0.6s ease;
			}
		}
		.register-left-waves {
			position: absolute;
			top: 0;
			right: -100px;
		}
	}

	.register-right {
		width: 700px;
		.register-right-warp {
			border: 1px solid var(--el-color-primary-light-3);
			border-radius: 3px;
			width: 500px;
			min-height: 500px;
			position: relative;
			overflow: hidden;
			background-color: var(--el-color-white);
			.register-right-warp-one,
			.register-right-warp-two {
				position: absolute;
				display: block;
				width: inherit;
				height: inherit;
				&::before,
				&::after {
					content: '';
					position: absolute;
					z-index: 1;
				}
			}
			.register-right-warp-one {
				&::before {
					filter: hue-rotate(0deg);
					top: 0px;
					left: 0;
					width: 100%;
					height: 3px;
					background: linear-gradient(90deg, transparent, var(--el-color-primary));
					animation: loginLeft 3s linear infinite;
				}
				&::after {
					filter: hue-rotate(60deg);
					top: -100%;
					right: 2px;
					width: 3px;
					height: 100%;
					background: linear-gradient(180deg, transparent, var(--el-color-primary));
					animation: loginTop 3s linear infinite;
					animation-delay: 0.7s;
				}
			}
			.register-right-warp-two {
				&::before {
					filter: hue-rotate(120deg);
					bottom: 2px;
					right: -100%;
					width: 100%;
					height: 3px;
					background: linear-gradient(270deg, transparent, var(--el-color-primary));
					animation: loginRight 3s linear infinite;
					animation-delay: 1.4s;
				}
				&::after {
					filter: hue-rotate(300deg);
					bottom: -100%;
					left: 0px;
					width: 3px;
					height: 100%;
					background: linear-gradient(360deg, transparent, var(--el-color-primary));
					animation: loginBottom 3s linear infinite;
					animation-delay: 2.1s;
				}
			}
			.register-right-warp-mian {
				display: flex;
				flex-direction: column;
				height: 100%;
				.register-right-warp-main-title {
					height: 100px;
					line-height: 100px;
					font-size: 27px;
					text-align: center;
					letter-spacing: 3px;
					animation: logoAnimation 0.3s ease;
					color: var(--el-text-color-primary);
				}
				.register-right-warp-main-form {
					flex: 1;
					padding: 0 50px 30px;
					.register-content-form {
						@for $i from 1 through 6 {
							.register-animation#{$i} {
								opacity: 0;
								animation-name: error-num;
								animation-duration: 0.5s;
								animation-fill-mode: forwards;
								animation-delay: calc($i/10) + s;
							}
						}
						.register-content-submit {
							width: 100%;
							letter-spacing: 2px;
							font-weight: 300;
							margin-top: 10px;
						}
						.register-go-login {
							text-align: center;
							margin-top: 12px;
							color: var(--el-text-color-placeholder);
						}
					}
				}
			}
		}
	}

	.register-footer {
		position: absolute;
		bottom: 16px;
		left: 0;
		right: 0;
		text-align: center;
		font-size: 12px;
		color: var(--el-text-color-placeholder);
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 4px;
		.register-footer-copyright {
			:deep(a) {
				color: var(--el-text-color-placeholder);
				&:hover {
					color: var(--el-color-primary);
				}
			}
		}
		.register-footer-registration {
			a {
				color: var(--el-text-color-placeholder);
				text-decoration: none;
				&:hover {
					color: var(--el-color-primary);
					text-decoration: underline;
				}
			}
		}
	}
}
</style>
