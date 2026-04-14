<template>
	<el-form size="large" class="login-content-form">
		<el-form-item class="login-animation1">
			<el-input text :placeholder="$t('message.email.placeholder1')" v-model="state.ruleForm.username" clearable autocomplete="off">
				<template #prefix>
					<el-icon class="el-input__icon"><ele-Message /></el-icon>
				</template>
			</el-input>
		</el-form-item>
		<el-form-item class="login-animation2">
			<el-col :span="15">
				<el-input text maxlength="6" :placeholder="$t('message.email.placeholder2')" v-model="state.ruleForm.code" clearable autocomplete="off">
					<template #prefix>
						<el-icon class="el-input__icon"><ele-Position /></el-icon>
					</template>
				</el-input>
			</el-col>
			<el-col :span="1"></el-col>
			<el-col :span="8">
				<el-button v-waves class="login-content-code" :disabled="state.countdown > 0" @click="onSendCode">
					{{ state.countdown > 0 ? $t('message.email.codeCountdown').replace('{s}', String(state.countdown)) : $t('message.email.codeText') }}
				</el-button>
			</el-col>
		</el-form-item>
		<el-form-item class="login-animation3">
			<el-button round type="primary" v-waves class="login-content-submit" @click="onSignIn" :loading="state.loading">
				<span>{{ $t('message.email.btnText') }}</span>
			</el-button>
		</el-form-item>
	</el-form>
</template>

<script setup lang="ts" name="loginEmail">
import { reactive } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useUserApi } from '/@/api/user';
import { Session } from '/@/utils/storage';
import { initFrontEndControlRoutes } from '/@/router/frontEnd';
import { initBackEndControlRoutes } from '/@/router/backEnd';

const router = useRouter();
const route = useRoute();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const userApi = useUserApi();

const state = reactive({
	ruleForm: {
		username: '',
		code: '',
	},
	loading: false,
	countdown: 0,
});

let countdownTimer: ReturnType<typeof setInterval> | null = null;

// 发送邮箱验证码
const onSendCode = async () => {
	if (!state.ruleForm.username) {
		ElMessage.warning('请输入邮箱地址');
		return;
	}
	try {
		await userApi.sendCode({ channel: 'Mail', username: state.ruleForm.username, action: 'login' });
		ElMessage.success('验证码已发送至您的邮箱');
		state.countdown = 60;
		countdownTimer = setInterval(() => {
			state.countdown--;
			if (state.countdown <= 0 && countdownTimer) {
				clearInterval(countdownTimer);
				countdownTimer = null;
			}
		}, 1000);
	} catch (err: any) {
		ElMessage.error(err?.message || '发送失败，请稍后重试');
	}
};

// 邮箱验证码登录
const onSignIn = async () => {
	if (!state.ruleForm.username) {
		ElMessage.warning('请输入邮箱地址');
		return;
	}
	if (!state.ruleForm.code) {
		ElMessage.warning('请输入验证码');
		return;
	}
	state.loading = true;
	try {
		const res = await userApi.loginByCode({
			username: state.ruleForm.username,
			password: state.ruleForm.code,
			loginCategory: 2,
		});
		Session.set('token', res.data.token);
		if (!themeConfig.value.isRequestRoutes) {
			const isNoPower = await initFrontEndControlRoutes();
			signInSuccess(isNoPower);
		} else {
			const isNoPower = await initBackEndControlRoutes();
			signInSuccess(isNoPower);
		}
	} catch (err: any) {
		ElMessage.error(err?.message || '登录失败');
	} finally {
		state.loading = false;
	}
};

const signInSuccess = (isNoPower: boolean | undefined) => {
	if (isNoPower) {
		ElMessage.warning('暂无权限，请联系管理员');
		return;
	}
	const returnUrl = (route.query?.redirect as string) || '/';
	router.push(returnUrl);
	ElMessage.success('登录成功');
};
</script>

<style scoped lang="scss">
.login-content-form {
	margin-top: 20px;
	@for $i from 1 through 3 {
		.login-animation#{$i} {
			opacity: 0;
			animation-name: error-num;
			animation-duration: 0.5s;
			animation-fill-mode: forwards;
			animation-delay: calc($i/10) + s;
		}
	}
	.login-content-code {
		width: 100%;
		padding: 0;
	}
	.login-content-submit {
		width: 100%;
		letter-spacing: 2px;
		font-weight: 300;
		margin-top: 15px;
	}
}
</style>
