<template>
	<div class="table-search-container">
		<Form ref="formRef" v-model="formData" :config="config" class="table-form" :handleVisible="false" v-if="search.length">
			<template #form-after>
				<el-col class="!flex-1 !max-w-none">
					<el-form-item class="table-form-btn" :label-width="search.length <= 1 ? '10px' : '100px'">
						<template #label v-if="search.length > 1">
							<div class="table-form-btn-toggle ml10" @click="state.isToggle = !state.isToggle">
								<span>{{ state.isToggle ? '收起筛选' : '展开筛选' }}</span>
								<SvgIcon :name="state.isToggle ? 'ele-ArrowUp' : 'ele-ArrowDown'" />
							</div>
						</template>
						<div class="table-form-actions">
							<div class="table-form-actions-left">
								<el-button size="default" type="primary" @click="onSearch">查询 </el-button>
								<el-button size="default" type="info" class="ml10" @click="onReset"> 重置 </el-button>
							</div>
							<div class="table-form-actions-right">
								<slot name="handle-after"></slot>
							</div>
						</div>
					</el-form-item>
				</el-col>
			</template>
			<template v-for="item in config.filter(item => item.slot)" :key="item.prop" #[`${item.slot!}`]="data">
				<slot :name="item.slot" :model="data.model" :prop="data.prop"></slot>
			</template>
		</Form>
		<div v-else class="table-search-empty">
			<slot name="handle-after"></slot>
		</div>
	</div>
</template>

<script setup lang="ts" name="makeTableDemoSearch">
import { reactive, ref, onMounted, computed } from 'vue';
import Form from '/@/components/form/index.vue'
import { ColumnConfig } from '../form/model/form';
import { isObjTrue } from '/@/utils/other';
interface Props {
	search: ColumnConfig[];
	modelValue: EmptyObjectType;
}
interface Emits {
	(e: 'search', val: EmptyObjectType): void;
	(e: 'update:modelValue', val: EmptyObjectType): void;
}
// 定义父组件传过来的值
const props = withDefaults(defineProps<Props>(), {
	search: () => [],
});
// 定义子组件向父组件传值/事件
const emits = defineEmits<Emits>();

const formData = computed({
	get () {
		return props.modelValue
	},
	set (val) {
		emits('update:modelValue', val)
	}
})
const config = computed(() => {
	return props.search.filter(item => 
		(typeof item.if === 'function' ? item.if(formData.value) : isObjTrue(item.if)) &&
		(typeof item.show === 'function' ? item.show(formData.value) : isObjTrue(item.show))
	).map((item, index) => ({
		...item,
		show: index === 0 || state.isToggle
	}))
})

// 定义变量内容
const formRef = ref<InstanceType<typeof Form>>();
const state = reactive({
	// form: {},
	isToggle: false,
});

// 查询
const onSearch = () => {
	if (!formRef.value) return;
	emits('search', formData.value);
};
// 重置
const onReset = () => {
	if (!formRef.value) return;
	formRef.value?.formEl?.resetFields();
	emits('search', formData.value);
};
// 初始化 form 字段，取自父组件 search.prop
const initFormField = () => {
	if (props.search.length <= 0) return false;
	props.search.forEach((v) => (formData.value[v.prop.toString()] = ''));
};
// 页面加载时
onMounted(() => {
	initFormField();
});
</script>

<style scoped lang="scss">
.table-search-container {
	display: flex;
	width: 100%;
	padding: 12px 12px 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background: var(--el-fill-color-extra-light);
	transition: box-shadow .2s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
	&:hover,
	&:focus-within {
		box-shadow: 0 3px 10px rgba(0, 0, 0, 0.06);
	}
	.table-form {
		flex: 1;
		:deep(.el-form) {
			margin-bottom: 0;
		}
		.table-form-actions {
			display: flex;
			justify-content: space-between;
			align-items: center;
			width: 100%;
			gap: 10px;
			&-left,
			&-right {
				display: flex;
				align-items: center;
				flex-wrap: wrap;
				gap: 10px;
			}
			:deep(.el-button) {
				border-radius: 6px;
				transition: all .2s ease;
				&:hover:not(.is-disabled):not(.is-loading) {
					transform: translateY(-1px);
					box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
				}
				&:focus-visible {
					outline: 2px solid var(--el-color-primary);
					outline-offset: 2px;
				}
			}
		}
		.table-form-btn-toggle {
			white-space: nowrap;
			user-select: none;
			display: flex;
			align-items: center;
			color: var(--el-color-primary);
			cursor: pointer;
			transition: opacity .2s ease;
			&:hover {
				opacity: 0.8;
			}
		}
	}
	.table-search-empty {
		width: 100%;
		display: flex;
		justify-content: flex-end;
		padding-bottom: 8px;
		gap: 10px;
	}
}

@media screen and (max-width: 992px) {
	.table-search-container {
		padding-bottom: 8px;
		.table-form {
			.table-form-actions {
				flex-direction: column;
				align-items: flex-start;
				&-right {
					width: 100%;
					justify-content: flex-start;
				}
			}
		}
		.table-search-empty {
			justify-content: flex-start;
			flex-wrap: wrap;
		}
	}
}

@media screen and (max-width: 576px) {
	.table-search-container {
		padding: 10px 10px 6px;
	}
}
</style>
