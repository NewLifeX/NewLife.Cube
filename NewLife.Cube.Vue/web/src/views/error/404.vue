<template>
	<div class="error layout-padding">
		<div class="layout-padding-auto layout-padding-view">
			<div class="error-flex">
				<div class="left">
					<div class="left-item">
						<div class="left-item-animation left-item-num">404</div>
						<div class="left-item-animation left-item-title">{{ $t('message.notFound.foundTitle') }}</div>
						<div class="left-item-animation left-item-msg">{{ $t('message.notFound.foundMsg') }}</div>
						<div class="left-item-animation left-item-btn">
							<el-button type="primary" size="default" round @click="onGoHome">{{ $t('message.notFound.foundBtn') }}</el-button>
						</div>
					</div>
				</div>
				<div class="right">
					<img
						src="https://img-blog.csdnimg.cn/9eb1d85a417f4ed1ba7107f149ce3da1.png?x-oss-process=image/watermark,type_d3F5LXplbmhlaQ,shadow_50,text_Q1NETiBAbHl0LXRvcA==,size_16,color_FFFFFF,t_70,g_se,x_16"
					/>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="notFound">
import { useRouter } from 'vue-router';

// 定义变量内容
const router = useRouter();

// 返回首页
const onGoHome = () => {
	router.push('/');
};
</script>

<style scoped lang="scss">
.error {
	height: 100%;
	.error-flex {
		margin: auto;
		display: flex;
		height: 350px;
		width: 900px;
		.left {
			flex: 1;
			height: 100%;
			align-items: center;
			display: flex;
			.left-item {
				.left-item-animation {
					opacity: 0;
					animation-name: error-num;
					animation-duration: 0.5s;
					animation-fill-mode: forwards;
				}
				.left-item-num {
					color: var(--el-color-primary);
					font-size: 90px;
					font-weight: 800;
					letter-spacing: -3px;
					line-height: 1;
				}
				.left-item-title {
					font-size: 22px;
					font-weight: 600;
					color: var(--el-text-color-primary);
					margin: 15px 0 5px 0;
					animation-delay: 0.1s;
				}
				.left-item-msg {
					color: var(--el-text-color-secondary);
					font-size: 13px;
					line-height: 1.5;
					margin-bottom: 30px;
					animation-delay: 0.2s;
				}
				.left-item-btn {
					animation-delay: 0.2s;
					:deep(.el-button) {
						transition: all .2s ease;
						&:hover:not(.is-disabled) {
							transform: translateY(-2px);
							box-shadow: 0 4px 14px rgba(0, 0, 0, 0.15);
						}
					}
				}
			}
		}
		.right {
			flex: 1;
			opacity: 0;
			animation-name: error-img;
			animation-duration: 2s;
			animation-fill-mode: forwards;
			border-radius: 12px;
			overflow: hidden;
			img {
				width: 100%;
				height: 100%;
				object-fit: cover;
			}
		}
	}
}
</style>
