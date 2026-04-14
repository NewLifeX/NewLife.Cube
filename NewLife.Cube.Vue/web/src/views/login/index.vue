<template>
	<div class="login-container flex">
		<div class="login-left">
			<div class="login-left-logo">
				<img :src="logoSrc" />
				<div class="login-left-logo-text">
					<span>{{ siteStore.loginConfig.displayName || getThemeConfig.globalViceTitle }}</span>
					<span class="login-left-logo-text-msg">{{ siteStore.loginConfig.loginTip || getThemeConfig.globalViceTitleMsg }}</span>
				</div>
			</div>
			<div class="login-left-img">
				<img :src="loginBgSrc" />
			</div>
			<img :src="loginBgWaves" class="login-left-waves" />
		</div>
		<div class="login-right flex">
			<div class="login-right-warp flex-margin">
				<span class="login-right-warp-one"></span>
				<span class="login-right-warp-two"></span>
				<div class="login-right-warp-mian">
					<div class="login-right-warp-main-title">{{ siteStore.loginConfig.displayName || getThemeConfig.globalTitle }} 欢迎您！</div>
					<div class="login-right-warp-main-form">
						<div v-if="!state.isScan">
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
							<!-- OAuth 第三方登录 -->
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
							<!-- 注册入口 -->
							<div v-if="siteStore.loginConfig.allowRegister" class="login-register-link mt10">
								<span class="font12">{{ $t('message.register.noAccount') }}</span>
								<el-link type="primary" :underline="false" @click="onGoRegister">{{ $t('message.register.linkText') }}</el-link>
							</div>
						</div>
						<Scan v-if="state.isScan" />
						<div class="login-content-main-sacn" @click="state.isScan = !state.isScan">
							<i class="iconfont" :class="state.isScan ? 'icon-diannao1' : 'icon-barcode-qr'"></i>
							<div class="login-content-main-sacn-delta"></div>
						</div>
					</div>
				</div>
			</div>
		</div>
		<!-- 底部版权信息 -->
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
import { useRouter } from 'vue-router';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useSiteInfo } from '/@/stores/siteInfo';
import { NextLoading } from '/@/utils/loading';
import logoMiniDefault from '/@/assets/logo-mini.png';
import loginMainDefault from '/@/assets/login-main.svg';
import loginBgWaves from '/@/assets/login-bg.svg';

// 引入组件
const Account = defineAsyncComponent(() => import('/@/views/login/component/account.vue'));
const Mobile = defineAsyncComponent(() => import('/@/views/login/component/mobile.vue'));
const Email = defineAsyncComponent(() => import('/@/views/login/component/email.vue'));
const Scan = defineAsyncComponent(() => import('/@/views/login/component/scan.vue'));

// 定义变量内容
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const siteStore = useSiteInfo();
const router = useRouter();
const state = reactive({
	tabsActiveName: 'account',
	isScan: false,
});

// 获取布局配置信息
const getThemeConfig = computed(() => {
	return themeConfig.value;
});

// 动态 Logo：优先使用后端配置，降级到内置静态资源
const logoSrc = computed(() => siteStore.siteInfo.loginLogo || logoMiniDefault);

// 动态背景图：优先使用后端配置，降级到内置静态资源
const loginBgSrc = computed(() => siteStore.siteInfo.loginBg || loginMainDefault);

// OAuth 第三方登录（全页跳转，由后端处理 OAuth 流程）
const onOAuthLogin = (name: string) => {
	const redirect = new URLSearchParams(window.location.search).get('redirect') || '/';
	window.location.href = `/Sso/Login/${name}?r=${encodeURIComponent(redirect)}`;
};

// 跳转到注册页
const onGoRegister = () => {
	router.push('/register');
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
	height: 100%;
	flex-direction: column;
	background: var(--el-color-white);
	position: relative;

	// 主内容区（左右布局）
	> .login-left,
	> .login-right {
		// 仅在屏幕足够宽时保持左右布局
	}
	display: flex;
	flex-direction: row;

	.login-left {
		flex: 1;
		position: relative;
		background-color: rgba(211, 239, 255, 1);
		margin-right: 100px;
		.login-left-logo {
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
			.login-left-logo-text {
				display: flex;
				flex-direction: column;
				span {
					margin-left: 10px;
					font-size: 28px;
					color: #26a59a;
				}
				.login-left-logo-text-msg {
					font-size: 12px;
					color: #32a99e;
				}
			}
		}
		.login-left-img {
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
		.login-left-waves {
			position: absolute;
			top: 0;
			right: -100px;
		}
	}
	.login-right {
		width: 700px;
		.login-right-warp {
			border: 1px solid var(--el-color-primary-light-3);
			border-radius: 3px;
			width: 500px;
			min-height: 500px;
			position: relative;
			overflow: hidden;
			background-color: var(--el-color-white);
			.login-right-warp-one,
			.login-right-warp-two {
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
			.login-right-warp-one {
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
			.login-right-warp-two {
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
			.login-right-warp-mian {
				display: flex;
				flex-direction: column;
				height: 100%;
				.login-right-warp-main-title {
					height: 130px;
					line-height: 130px;
					font-size: 27px;
					text-align: center;
					letter-spacing: 3px;
					animation: logoAnimation 0.3s ease;
					animation-delay: 0.3s;
					color: var(--el-text-color-primary);
				}
				.login-right-warp-main-form {
					flex: 1;
					padding: 0 50px 30px;
					.login-content-main-sacn {
						position: absolute;
						top: 0;
						right: 0;
						width: 50px;
						height: 50px;
						overflow: hidden;
						cursor: pointer;
						transition: all ease 0.3s;
						color: var(--el-color-primary);
						&-delta {
							position: absolute;
							width: 35px;
							height: 70px;
							z-index: 2;
							top: 2px;
							right: 21px;
							background: var(--el-color-white);
							transform: rotate(-45deg);
						}
						&:hover {
							opacity: 1;
							transition: all ease 0.3s;
							color: var(--el-color-primary) !important;
						}
						i {
							width: 47px;
							height: 50px;
							display: inline-block;
							font-size: 48px;
							position: absolute;
							right: 1px;
							top: 0px;
						}
					}
					// OAuth 第三方登录区
					.login-oauth-providers {
						margin-top: 8px;
						.login-oauth-list {
							display: flex;
							flex-wrap: wrap;
							justify-content: center;
							gap: 16px;
							margin-top: 8px;
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
							.login-oauth-logo {
								width: 40px;
								height: 40px;
								border-radius: 50%;
								object-fit: contain;
							}
							.login-oauth-fallback-icon {
								font-size: 36px;
								width: 40px;
								height: 40px;
								display: flex;
								align-items: center;
								justify-content: center;
							}
							.login-oauth-name {
								font-size: 12px;
								margin-top: 4px;
								max-width: 60px;
								text-align: center;
								overflow: hidden;
								text-overflow: ellipsis;
								white-space: nowrap;
							}
						}
					}
					// 注册入口
					.login-register-link {
						text-align: center;
						color: var(--el-text-color-placeholder);
					}
				}
			}
		}
	}

	// 底部版权
	.login-footer {
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
		.login-footer-copyright {
			:deep(a) {
				color: var(--el-text-color-placeholder);
				&:hover {
					color: var(--el-color-primary);
				}
			}
		}
		.login-footer-registration {
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


<style scoped lang="scss">
.login-container {
	height: 100%;
	background: var(--el-color-white);
	.login-left {
		flex: 1;
		position: relative;
		background-color: rgba(211, 239, 255, 1);
		margin-right: 100px;
		.login-left-logo {
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
			.login-left-logo-text {
				display: flex;
				flex-direction: column;
				span {
					margin-left: 10px;
					font-size: 28px;
					color: #26a59a;
				}
				.login-left-logo-text-msg {
					font-size: 12px;
					color: #32a99e;
				}
			}
		}
		.login-left-img {
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
		.login-left-waves {
			position: absolute;
			top: 0;
			right: -100px;
		}
	}
	.login-right {
		width: 700px;
		.login-right-warp {
			border: 1px solid var(--el-color-primary-light-3);
			border-radius: 3px;
			width: 500px;
			height: 500px;
			position: relative;
			overflow: hidden;
			background-color: var(--el-color-white);
			.login-right-warp-one,
			.login-right-warp-two {
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
			.login-right-warp-one {
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
			.login-right-warp-two {
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
			.login-right-warp-mian {
				display: flex;
				flex-direction: column;
				height: 100%;
				.login-right-warp-main-title {
					height: 130px;
					line-height: 130px;
					font-size: 27px;
					text-align: center;
					letter-spacing: 3px;
					animation: logoAnimation 0.3s ease;
					animation-delay: 0.3s;
					color: var(--el-text-color-primary);
				}
				.login-right-warp-main-form {
					flex: 1;
					padding: 0 50px 50px;
					.login-content-main-sacn {
						position: absolute;
						top: 0;
						right: 0;
						width: 50px;
						height: 50px;
						overflow: hidden;
						cursor: pointer;
						transition: all ease 0.3s;
						color: var(--el-color-primary);
						&-delta {
							position: absolute;
							width: 35px;
							height: 70px;
							z-index: 2;
							top: 2px;
							right: 21px;
							background: var(--el-color-white);
							transform: rotate(-45deg);
						}
						&:hover {
							opacity: 1;
							transition: all ease 0.3s;
							color: var(--el-color-primary) !important;
						}
						i {
							width: 47px;
							height: 50px;
							display: inline-block;
							font-size: 48px;
							position: absolute;
							right: 1px;
							top: 0px;
						}
					}
				}
			}
		}
	}
}
</style>
