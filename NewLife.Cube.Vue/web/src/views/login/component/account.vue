<template>
	<el-form size="large" class="login-content-form">
		<el-form-item>
			<el-input text :placeholder="$t('message.account.accountPlaceholder1')" v-model="state.ruleForm.userName" clearable autocomplete="off">
				<template #prefix>
					<el-icon class="el-input__icon"><ele-User /></el-icon>
				</template>
			</el-input>
		</el-form-item>
		<el-form-item>
			<el-input
				:type="state.isShowPassword ? 'text' : 'password'"
				:placeholder="$t('message.account.accountPlaceholder2')"
				v-model="state.ruleForm.password"
				autocomplete="off"
			>
				<template #prefix>
					<el-icon class="el-input__icon"><ele-Unlock /></el-icon>
				</template>
				<template #suffix>
					<i
						class="iconfont el-input__icon login-content-password"
						:class="state.isShowPassword ? 'icon-yincangmima' : 'icon-xianshimima'"
						@click="state.isShowPassword = !state.isShowPassword"
					>
					</i>
				</template>
			</el-input>
		</el-form-item>
		<el-form-item>
			<el-button type="primary" class="login-content-submit" round v-waves @click="onSignIn" :loading="state.loading.signIn">
				<span>{{ $t('message.account.accountBtnText') }}</span>
			</el-button>
		</el-form-item>
		<el-form-item>
			<div class="login-action-links">
				<el-link class="login-action-links-left" type="primary" :underline="false" @click="router.push('/forgot-password')">{{ $t('message.account.forgotPassword') }}</el-link>
				<div v-if="siteStore.loginConfig.allowRegister" class="login-action-links-right">
					<span class="font12 login-action-links-text">{{ $t('message.register.noAccount') }}</span>
					<el-link type="primary" :underline="false" @click="router.push('/register')">{{ $t('message.register.linkText') }}</el-link>
				</div>
			</div>
		</el-form-item>
	</el-form>
</template>

<script setup lang="ts" name="loginAccount">
import { reactive, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import Cookies from 'js-cookie';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useSiteInfo } from '/@/stores/siteInfo';
import { initFrontEndControlRoutes } from '/@/router/frontEnd';
import { initBackEndControlRoutes } from '/@/router/backEnd';
import { Session } from '/@/utils/storage';
import { formatAxis } from '/@/utils/formatTime';
import { NextLoading } from '/@/utils/loading';
import { useUserApi } from '/@/api/user';

// 定义变量内容
const { t } = useI18n();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const siteStore = useSiteInfo();
const route = useRoute();
const router = useRouter();
const userApi = useUserApi();
const state = reactive({
	isShowPassword: false,
	ruleForm: {
		userName: '',
		password: '',
	},
	loading: {
		signIn: false,
	},
});

// 时间获取
const currentTime = computed(() => {
	return formatAxis(new Date());
});
// 登录
const onSignIn = async () => {
	state.loading.signIn = true;
	// 存储 token 到浏览器缓存
	// 模拟数据，对接接口时，记得删除多余代码及对应依赖的引入。用于 `/src/stores/userInfo.ts` 中不同用户登录判断（模拟数据）
	userApi.signIn(state.ruleForm).then(async res => {
		// console.log(res.data.token)
		Session.set('token', res.data.token);
		Cookies.set('userName', state.ruleForm.userName);
		if (!themeConfig.value.isRequestRoutes) {
			// 前端控制路由，2、请注意执行顺序
			const isNoPower = await initFrontEndControlRoutes();
			signInSuccess(isNoPower);
		} else {
			// 模拟后端控制路由，isRequestRoutes 为 true，则开启后端控制路由
			// 添加完动态路由，再进行 router 跳转，否则可能报错 No match found for location with path "/"
			const isNoPower = await initBackEndControlRoutes();
			// 执行完 initBackEndControlRoutes，再执行 signInSuccess
			signInSuccess(isNoPower);
		}
	}).catch(() => {
		state.loading.signIn = false;
	})
};
// 登录成功后的跳转
const signInSuccess = (isNoPower: boolean | undefined) => {
	if (isNoPower) {
		ElMessage.warning('抱歉，您没有登录权限');
		Session.clear();
	} else {
		// 初始化登录成功时间问候语
		let currentTimeInfo = currentTime.value;
		// 登录成功，跳到转首页
		// 如果是复制粘贴的路径，非首页/登录页，那么登录成功后重定向到对应的路径中
		if (route.query?.redirect) {
			router.push({
				path: <string>route.query?.redirect,
				query: Object.keys(<string>route.query?.params).length > 0 ? JSON.parse(<string>route.query?.params) : '',
			});
		} else {
			router.push('/');
		}
		// 登录成功提示
		const signInText = t('message.signInText');
		ElMessage.success(`${currentTimeInfo}，${signInText}`);
		// 添加 loading，防止第一次进入界面时出现短暂空白
		NextLoading.start();
	}
	state.loading.signIn = false;
};
</script>

<style scoped lang="scss">
.login-content-form {
	margin-top: 20px;
	.login-content-password {
		display: inline-block;
		width: 20px;
		cursor: pointer;
		&:hover {
			color: #909399;
		}
	}
	.login-content-submit {
		width: 100%;
	}
	.login-action-links {
		width: 100%;
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 12px;
		&-left {
			flex-shrink: 0;
		}
		&-right {
			display: flex;
			align-items: center;
			justify-content: flex-end;
			gap: 4px;
			min-width: 0;
		}
		&-text {
			color: var(--el-text-color-placeholder);
			white-space: nowrap;
		}
	}
}
</style>
