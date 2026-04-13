<template>
	<div class="layout-footer pb15">
		<div class="layout-footer-warp">
			<div v-if="siteStore.siteInfo.copyright">{{ siteStore.siteInfo.copyright }}</div>
			<div v-else>{{ siteStore.siteInfo.displayName }}</div>
			<div v-if="siteStore.siteInfo.registration" class="mt5">{{ siteStore.siteInfo.registration }}</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="layoutFooter">
import { useSiteInfo } from '/@/stores/siteInfo';

const siteStore = useSiteInfo();
</script>

<style scoped lang="scss">
.layout-footer {
	width: 100%;
	display: flex;
	&-warp {
		margin: auto;
		color: var(--el-text-color-secondary);
		text-align: center;
		animation: error-num 0.3s ease;
	}
}
</style>
