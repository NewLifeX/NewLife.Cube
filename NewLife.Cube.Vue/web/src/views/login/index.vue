<template>
	<div class="login-container">
		<div class="login-card-wrap">
			<el-card class="login-card" shadow="never">
				<div class="login-header">
					<img :src="logoSrc" class="login-logo" />
					<div class="login-title">{{ siteStore.loginConfig.displayName || getThemeConfig.globalTitle }}</div>
					<div v-if="siteStore.loginConfig.loginTip || getThemeConfig.globalViceTitleMsg" class="login-subtitle">
						{{ siteStore.loginConfig.loginTip || getThemeConfig.globalViceTitleMsg }}
					</div>
				</div>
				<div class="login-form-wrap">
					<el-tabs v-model="state.tabsActiveName">
						<el-tab-pane v-if="siteStore.loginConfig.allowLogin" :label="$t('message.label.one1')" name="account">
							<Account />
						</el-tab-pane>
						<el-tab-pane v-if="siteStore.loginConfig.enableSms" :label="$t('message.label.two2')" name="mobile">
							<Mobile />
						</el-tab-pane>
						<el-tab-pane v-if="siteStore.loginConfig.enableMail" :label="$t('message.label.three3')" name="email">
							<Email />
						</el-tab-pane>
					</el-tabs>

					<div v-if="siteStore.loginConfig.providers && siteStore.loginConfig.providers.length > 0" class="login-oauth-providers">
						<el-divider>{{ $t('message.oauth.dividerText') }}</el-divider>
						<div class="login-oauth-list">
							<a
								v-for="item in siteStore.loginConfig.providers"
								:key="item.name"
								class="login-oauth-item"
								:title="item.nickName || item.name"
								@click="onOAuthLogin(item.name)"
							>
								<img v-if="item.logo" :src="item.logo" class="login-oauth-logo" :alt="item.nickName || item.name" />
								<i v-else class="iconfont icon-third-party login-oauth-fallback-icon"></i>
								<span class="login-oauth-name">{{ item.nickName || item.name }}</span>
							</a>
						</div>
					</div>
				</div>
			</el-card>
		</div>

		<div v-if="siteStore.siteInfo.copyright || siteStore.siteInfo.registration" class="login-footer">
			<div v-if="siteStore.siteInfo.copyright" class="login-footer-copyright" v-html="siteStore.siteInfo.copyright"></div>
			<div v-if="siteStore.siteInfo.registration" class="login-footer-registration">
				<a href="https://www.beianx.cn/" target="_blank" rel="noopener noreferrer">{{ siteStore.siteInfo.registration }}</a>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="loginIndex">
import { defineAsyncComponent, onMounted, reactive, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useSiteInfo } from '/@/stores/siteInfo';
import { NextLoading } from '/@/utils/loading';
import logoMiniDefault from '/@/assets/logo-mini.png';

// 引入组件
const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));
const Mobile = defineAsyncComponent(() => import('/@/views/login/component/mobile.vue'));
const Email = defineAsyncComponent(() => import('/@/views/login/component/email.vue'));

// 定义变量内容
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const siteStore = useSiteInfo();
const state = reactive({
	tabsActiveName: 'account',
});

// 获取布局配置信息
const getThemeConfig = computed(() => {
	return themeConfig.value;
});

// 动态 Logo：优先使用后端配置，降级到内置静态资源
const logoSrc = computed(() => siteStore.siteInfo.loginLogo || logoMiniDefault);

// OAuth 第三方登录（全页跳转，由后端处理 OAuth 流程）
const onOAuthLogin = (name: string) => {
	const redirect = new URLSearchParams(window.location.search).get('redirect') || '/';
	window.location.href = `/Sso/Login/${name}?r=${encodeURIComponent(redirect)}`;
};

// 页面加载时
onMounted(async () => {
	NextLoading.done();
	// 并行加载站点信息和登录配置
	await Promise.all([siteStore.loadSiteInfo(), siteStore.loadLoginConfig()]);
	// 如果密码登录不可用，自动切换到第一个可用的登录方式
	if (!siteStore.loginConfig.allowLogin) {
		if (siteStore.loginConfig.enableSms)
			state.tabsActiveName = 'mobile';
		else if (siteStore.loginConfig.enableMail)
			state.tabsActiveName = 'email';
	}
});
</script>

<style scoped lang="scss">
.login-container {
	min-height: 100vh;
	display: flex;
	flex-direction: column;
	justify-content: center;
	align-items: center;
	padding: 32px 16px 20px;
	background: linear-gradient(135deg, #165dff 0%, #722ed1 100%);

	.login-card-wrap {
		width: 100%;
		display: flex;
		justify-content: center;
	}

	.login-card {
		width: 460px;
		max-width: 100%;
		border-radius: 12px;
		border: none;
		:deep(.el-card__body) {
			padding: 28px 28px 20px;
		}
	}

	.login-header {
		text-align: center;
		margin-bottom: 12px;
		.login-logo {
			width: 52px;
			height: 52px;
			object-fit: contain;
			margin-bottom: 10px;
		}
		.login-title {
			font-size: 22px;
			font-weight: 600;
			color: var(--el-text-color-primary);
		}
		.login-subtitle {
			margin-top: 4px;
			font-size: 13px;
			color: var(--el-text-color-secondary);
		}
	}

	.login-form-wrap {
		:deep(.el-tabs__header) {
			margin-bottom: 8px;
		}
		:deep(.el-tabs__item) {
			font-size: 14px;
		}
		:deep(.login-content-form .el-form-item) {
			margin-bottom: 14px;
		}
		:deep(.login-content-form .el-input__wrapper),
		:deep(.login-content-form .el-textarea__inner) {
			border-radius: 6px;
			transition: box-shadow .2s ease, border-color .2s ease;
		}
		:deep(.login-content-form .el-input.is-focus .el-input__wrapper),
		:deep(.login-content-form .el-textarea .el-textarea__inner:focus) {
			box-shadow: 0 0 0 1px var(--el-color-primary), 0 0 0 3px var(--el-color-primary-light-9);
		}
		:deep(.login-content-form .el-form-item__error) {
			line-height: 1.2;
			padding-top: 2px;
			position: relative;
			top: 1px;
		}
		:deep(.login-content-form .el-button) {
			border-radius: 8px;
			transition: all .2s ease;
		}
		:deep(.login-content-form .el-button--primary) {
			font-weight: 500;
		}
		:deep(.login-content-form .el-button:hover:not(.is-disabled)) {
			transform: translateY(-1px);
		}
		:deep(.login-content-form .el-button:focus-visible) {
			outline: none;
			box-shadow: 0 0 0 2px var(--el-color-primary-light-9);
		}
		:deep(.login-content-form .el-button.is-loading) {
			opacity: .9;
		}
		:deep(.login-content-form .el-button.is-disabled) {
			opacity: .65;
		}
		.login-oauth-providers {
			margin-top: 4px;
			:deep(.el-divider) {
				margin: 12px 0 10px;
			}
		}
		.login-oauth-list {
			display: flex;
			flex-wrap: wrap;
			justify-content: center;
			gap: 14px;
		}
		.login-oauth-item {
			display: flex;
			flex-direction: column;
			align-items: center;
			cursor: pointer;
			text-decoration: none;
			color: var(--el-text-color-regular);
			&:hover {
				color: var(--el-color-primary);
			}
		}
		.login-oauth-logo {
			width: 36px;
			height: 36px;
			border-radius: 50%;
			object-fit: contain;
		}
		.login-oauth-fallback-icon {
			font-size: 32px;
			width: 36px;
			height: 36px;
			display: flex;
			align-items: center;
			justify-content: center;
		}
		.login-oauth-name {
			font-size: 12px;
			margin-top: 3px;
			max-width: 80px;
			text-align: center;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}
	}

	.login-footer {
		margin-top: 16px;
		text-align: center;
		font-size: 12px;
		color: rgba(255, 255, 255, 0.75);
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 4px;
		.login-footer-copyright {
			:deep(a) {
				color: rgba(255, 255, 255, 0.78);
				&:hover {
					color: #ffffff;
				}
			}
		}
		.login-footer-registration {
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
	.login-container {
		padding: 16px 12px;
		.login-card :deep(.el-card__body) {
			padding: 18px 16px 16px;
		}
		.login-header {
			.login-title {
				font-size: 20px;
			}
		}
	}
}
</style>
