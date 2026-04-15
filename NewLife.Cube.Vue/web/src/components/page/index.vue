<template>
	<div class="table-demo-container layout-padding">
		<div class="table-demo-padding layout-padding-view layout-padding-auto h-full">
			<div v-if="chartList.length > 0" class="chart-area mb15">
				<div v-for="(chart, idx) in chartList" :key="idx" class="chart-item" :ref="el => setChartRef(el as HTMLElement, idx)"></div>
			</div>
			<Table
				v-if="wrapper !== 'div'"
				class="table-demo"
				ref="tableRef"
				v-bind="config"
				v-model:columns="columns"
				v-model:search-data="searchForm"
				:authId="authId"
				:data="data"
				:stat="stat"
				:search="search"
				:param="param"
				:pager-visible="pagerVisible"
				@del="onTableDelRow"
				@add="onTableAdd"
				@edit="onTableEdit"
				@detail="onTableDetail"
				@batchDel="onBatchDel"
				@export="onExport"
				@import="onImport"
				@cellAction="onCellAction"
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
				:config="isDetail ? detailConfig : (isUpdate ? editConfig : addConfig)"
				:key="type"
				v-model="editForm"
				:isUpdate="isUpdate"
				:isDetail="isDetail"
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
import { computed, defineAsyncComponent, inject, markRaw, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import * as echarts from 'echarts';
import { ColumnKind, usePageApi } from '../../api/page';
import request from '/@/utils/request';
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
const { searchForm, editForm, editConfig, addConfig, detailConfig, search, columns } = useGetColumnsForm(props, emits, providePage)
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
const isDetail = ref(false);
const pagerVisible = ref(false);
const data = ref<EmptyObjectType[]>([]);
const stat = ref<EmptyObjectType | null>(null);
const editEl = ref<InstanceType<typeof Edit>>();
const config = ref<TableConfigType>({
	total: 0, // 列表总数
	loading: true, // loading 加载
	isBorder: false, // 是否显示表格边框
	isSerialNo: false, // 是否显示表格序号
	isSelection: true, // 是否显示表格多选
	isOperate: true, // 是否显示表格操作栏
	tableLayout: 'fixed', // fixed 稳定；auto 按内容浮动
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
		stat.value = res.stat || null
		if (res.page) {
			pagerVisible.value = true
			config.value.total = Number(res.page.totalCount)
		}
		config.value.loading = false;
	} catch (error) {
		config.value.loading = false;
	}
};

// 图表
const chartList = ref<any[]>([]);
const chartInstances = ref<any[]>([]);

const setChartRef = (el: HTMLElement | null, idx: number) => {
	if (!el) return;
	nextTick(() => {
		// 销毁旧实例
		if (chartInstances.value[idx]) {
			chartInstances.value[idx].dispose();
		}
		const instance = markRaw(echarts.init(el));
		const option = chartList.value[idx];
		if (option) {
			instance.setOption(option);
		}
		chartInstances.value[idx] = instance;
	});
};

const loadChartData = async () => {
	try {
		const res = await pageApi.getChartData(props.type!);
		if (Array.isArray(res.data) && res.data.length > 0) {
			chartList.value = res.data;
		} else {
			chartList.value = [];
		}
	} catch {
		chartList.value = [];
	}
};

// 窗口大小变化时自适应
const onResize = () => {
	for (const inst of chartInstances.value) {
		inst?.resize();
	}
};
window.addEventListener('resize', onResize);
onBeforeUnmount(() => {
	window.removeEventListener('resize', onResize);
	for (const inst of chartInstances.value) {
		inst?.dispose();
	}
});

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
	isDetail.value = false
	editEl.value?.handleAdd();
	onAddClick && onAddClick()
}
const onTableEdit = (item: EmptyObjectType) => {
	isUpdate.value = true
	isDetail.value = false
	editEl.value?.handleEdit(item);
	onEditClick && onEditClick(item);
}
const onTableDetail = (item: EmptyObjectType) => {
	isUpdate.value = true
	isDetail.value = true
	editEl.value?.handleEdit(item);
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
// 导出
const onExport = (format: string) => {
	const url = pageApi.getExportUrl(props.type!, format);
	window.open(url, '_blank');
};
// 导入
const onImport = async (file: File) => {
	try {
		const res = await pageApi.importFile(props.type!, file);
		ElMessage.success(res.message || '导入成功');
		getTableData();
	} catch (e) {
		// 错误已由 request 拦截器处理
	}
};
// 批量删除
const onBatchDel = async (rows: EmptyObjectType[]) => {
	const keys = rows.map(r => r.id).filter(Boolean);
	if (keys.length === 0) return;
	try {
		const res = await pageApi.deleteSelect(props.type!, keys.map(String));
		ElMessage.success(res.message || `删除成功`);
		getTableData();
	} catch (e) {
		// 错误已由 request 拦截器处理
	}
};
// 单元格 AJAX 动作
const onCellAction = async (url: string) => {
	try {
		const res = await request({ url, method: 'post' });
		ElMessage.success(res.message || '操作成功');
		getTableData();
	} catch {
		// 错误已由 request 拦截器处理
	}
};
getTableData();
loadChartData();

defineExpose({
	getTableData
})
providePage && (providePage.handle.reload = getTableData)

</script>

<style scoped lang="scss">
.table-demo-container {
	.table-demo-padding {
		padding: 16px;
		.chart-area {
			display: flex;
			flex-wrap: wrap;
			gap: 15px;
			.chart-item {
				flex: 1;
				min-width: 400px;
				height: 400px;
				background: var(--el-bg-color);
				border-radius: 8px;
				border: 1px solid var(--el-border-color-lighter);
			}
		}
		.table-demo {
			flex: 1;
			overflow: hidden;
			background: var(--el-bg-color);
			border: 1px solid var(--el-border-color-lighter);
			border-radius: 8px;
			padding: 12px;
		}
	}
}
</style>
