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
						<div class="flex justify-between w-full ml-10">
							<div>
								<el-button size="default" type="primary" @click="onSearch">查询 </el-button>
								<el-button size="default" type="info" class="ml10" @click="onReset"> 重置 </el-button>
							</div>
							<div class="ml-2.5">
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
		<div v-else class="flex justify-end w-full mb-2">
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
	.table-form {
		flex: 1;
		.table-form-btn-toggle {
			white-space: nowrap;
			user-select: none;
			display: flex;
			align-items: center;
			color: var(--el-color-primary);
		}
	}
}
</style>
