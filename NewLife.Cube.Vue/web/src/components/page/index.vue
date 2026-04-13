<template>
	<div class="table-demo-container layout-padding">
		<div class="table-demo-padding layout-padding-view layout-padding-auto h-full">
			<Table
				v-if="wrapper !== 'div'"
				class="table-demo"
				ref="tableRef"
				v-bind="config"
				v-model:columns="columns"
				v-model:search-data="searchForm"
				:authId="authId"
				:data="data"
				:search="search"
				:param="param"
				:pager-visible="pagerVisible"
				@del="onTableDelRow"
				@add="onTableAdd"
				@edit="onTableEdit"
				@search="onSearch"
				@sortHeader="onSortHeader">
				<template v-for="item in search.filter(v => v.slot)" :key="item.prop.toString()" #[`${item.slot!}`]="data">
					<slot :name="item.slot" :model="data.model" :prop="data.prop"></slot>
				</template>
				<template v-for="item in columns.filter(v => v.slot)" :key="item.prop" #[`${item.slot!}`]="data">
					<slot :name="item.slot" :scope="data.scope"></slot>
				</template>
				<template #table-top>
					<slot name="table-top"></slot>
				</template>
				<template #table-handle-before="{ scope }">
					<slot name="table-handle-before" :scope="scope"></slot>
				</template>
				<template #table-handle-after="{ scope }">
					<slot name="table-handle-after" :scope="scope"></slot>
				</template>
			</Table>
			<Edit
				ref="editEl"
				v-model:visible="editVisible"
				:type="type"
				:wrapper="wrapper"
				:config="isUpdate ? editConfig : addConfig"
				:key="type"
				v-model="editForm"
				:isUpdate="isUpdate"
				@submit-success="getTableData"
				@add-before="$attrs.onAddBefore"
				@add-after="$attrs.onAddAfter"
				@edit-before="$attrs.onEditBefore"
				@edit-after="$attrs.onEditAfter">
				<template v-for="item in slots" :key="item.prop.toString()" #[`${item.slot!}`]="data">
					<slot v-if="(isUpdate && item.in?.indexOf(ColumnKind.EDIT) !== -1) || (!isUpdate && item.in?.indexOf(ColumnKind.ADD) !== -1)" :name="item.slot" :model="data.model" :prop="data.prop"></slot>
				</template>
			</Edit>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, defineAsyncComponent, inject, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { ColumnKind, usePageApi } from '../../api/page';
import Edit from './edit.vue';
import { TableColumn } from '../table/type';
import { EditWrapper } from './model';
import useGetColumnsForm from './hook/useGetColumnsForm';
import { ColumnConfig } from '../form/model/form';
import providePageKey from './provide/key';

interface Props {
	type?: string;
	authId?: number;
	searchData?: EmptyObjectType;
	editWrapper?: EditWrapper;
	// tableConfig?: TableConfigType;
}
interface Emits {
	(e: 'update:searchData', val: EmptyObjectType): void;
}
const props = withDefaults(defineProps<Props>(), {
	searchData: () => ({})
});
const emits = defineEmits<Emits>();
const pageApi = usePageApi()
const editVisible = ref(false);
// 引入组件
const Table = defineAsyncComponent(() => import('/@/components/table/index.vue'));
const wrapper = ref<EditWrapper>(props.editWrapper || 'drawer');

const providePage = inject(providePageKey)
const tableConfig = providePage?.pageProps.tableConfig || {}

// 初始化配置项数据与表单数据
const { searchForm, editForm, editConfig, addConfig, search, columns } = useGetColumnsForm(props, emits, providePage)
const { onAddClick, onEditClick, onDelBefore, onDelAfter } = providePage?.pageProps || {};

const slots = computed(() => {
	let values: Array<ColumnConfig & { in?: ColumnKind[] }> = editConfig.value.filter(item => item.slot).map(item => ({ ...item, in: [ColumnKind.EDIT] }))
	addConfig.value.forEach(item => {
		if (item.slot) {
			let i = values.findIndex(val => val.prop === item.prop)
			if (i !== -1) {
				values[i].in?.push(ColumnKind.ADD)
			} else {
				values.push({...item, in: [ColumnKind.ADD]})
			}
		}
	})
	return values
})

// 定义变量内容
const tableRef = ref<RefType>();
const isUpdate = ref(false);
const pagerVisible = ref(false);
const data = ref<EmptyObjectType[]>([]);
const editEl = ref<InstanceType<typeof Edit>>();
const config = ref<TableConfigType>({
	total: 0, // 列表总数
	loading: true, // loading 加载
	isBorder: false, // 是否显示表格边框
	isSerialNo: false, // 是否显示表格序号
	isSelection: true, // 是否显示表格多选
	isOperate: true, // 是否显示表格操作栏
	...providePage?.pageProps.tableConfig
})
const param = ref({
	pageIndex: 1,
	pageSize: 20,
	sort: '',
	desc: false
})

// 初始化列表数据
const getTableData = async () => {
	config.value.loading = true;
	data.value = [];
	let res;
	try {
		const requestProps = tableConfig?.requestProps;
		if (tableConfig?.api) {
			res = await tableConfig?.api({ ...requestProps, ...param.value, ...searchForm.value })
		} else {
			let url = tableConfig?.url || props.type!
			res = await pageApi.getTableData(url, { ...requestProps, ...param.value, ...searchForm.value })
		}
		if (Array.isArray(res.data)) {
			data.value = res.data || []
		} else if (typeof res.data === 'object') {
			wrapper.value = 'div'
			editForm.value = res.data
			isUpdate.value = true
		}
		if (res.page) {
			pagerVisible.value = true
			config.value.total = Number(res.page.totalCount)
		}
		config.value.loading = false;
	} catch (error) {
		config.value.loading = false;
	}
};

// 删除当前项回调
const onTableDelRow = (item: EmptyObjectType) => {
	function delFun () {
		pageApi.delTableItem(props.type!, item.id).then(() =>{
			ElMessage.success(`删除成功！`);
			getTableData();
			onDelAfter && onDelAfter(item);
		})
	}
	// emits('delBefore', item, delFun)
	if (onDelBefore) onDelBefore(item, delFun)
	else delFun()
};
const onTableAdd = () => {
	isUpdate.value = false
	editEl.value?.handleAdd();
	onAddClick && onAddClick()
}
const onTableEdit = (item: EmptyObjectType) => {
	isUpdate.value = true
	editEl.value?.handleEdit(item);
	onEditClick && onEditClick(item);
}
// 分页改变时回调
const onSearch = (page: TableDemoPageType) => {
	param.value.pageIndex = page.pageIndex;
	param.value.pageSize = page.pageSize;
	param.value.sort = page.sort;
	param.value.desc = page.desc;
	getTableData();
};
// 拖动显示列排序回调
const onSortHeader = (data: TableColumn[]) => {
	columns.value = data;
};
getTableData();

defineExpose({
	getTableData
})
providePage && (providePage.handle.reload = getTableData)

</script>

<style scoped lang="scss">
.table-demo-container {
	.table-demo-padding {
		padding: 15px;
		.table-demo {
			flex: 1;
			overflow: hidden;
		}
	}
}
</style>
