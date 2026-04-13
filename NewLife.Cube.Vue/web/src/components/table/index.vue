<template>
	<div class="table-container">
		<Search :search="search" @search="onSearch" v-model="searchDataRef">
			<template #handle-after>
				<el-button v-auths="getBtnAuth(Auth.ADD)" size="default" type="primary" @click="onAdd">添加 </el-button>
				<el-popconfirm v-if="state.selectlist.length > 0" title="确定删除选中项吗？" @confirm="onBatchDel">
					<template #reference>
						<el-button v-auths="getBtnAuth(Auth.DEL)" size="default" type="danger">删除选中({{ state.selectlist.length }})</el-button>
					</template>
				</el-popconfirm>
				<el-dropdown v-if="auths(getBtnAuth(Auth.EXPORT))" trigger="click" @command="onExport" class="ml-2.5">
					<el-button size="default">导出<el-icon class="el-icon--right"><ArrowDown /></el-icon></el-button>
					<template #dropdown>
						<el-dropdown-menu>
							<el-dropdown-item command="Excel">导出 Excel</el-dropdown-item>
							<el-dropdown-item command="Csv">导出 CSV</el-dropdown-item>
							<el-dropdown-item command="Json">导出 JSON</el-dropdown-item>
							<el-dropdown-item command="Xml">导出 XML</el-dropdown-item>
							<el-dropdown-item command="ExcelTemplate" divided>导出模板</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
				<el-upload v-if="auths(getBtnAuth(Auth.IMPORT))" :show-file-list="false" :before-upload="onImportFile" accept=".xlsx,.xls,.csv,.json,.zip" class="ml-2.5 inline-block">
					<el-button size="default">导入</el-button>
				</el-upload>
			</template>
			<template v-for="item in search.filter(item => item.slot)" :key="item.prop" #[`${item.slot!}`]="data">
				<slot :name="item.slot" :model="data.model" :prop="data.prop"></slot>
			</template>
		</Search>
		<slot name="table-top"></slot>
		<el-table
			style="width: 100%"
			row-key="id"
			stripe
			:data="data"
			:border="setBorder"
			v-bind="$attrs"
			v-loading="loading"
			:show-summary="!!stat"
			:summary-method="getSummary"
			@selection-change="onSelectionChange"
			@sort-change="onChangeSort">
			<el-table-column type="selection" :reserve-selection="true" width="40" fixed="left" v-if="isSelection && tableColumns.length" />
			<el-table-column type="index" label="序号" width="60" v-if="isSerialNo && tableColumns.length" />
			<template v-for="(item, index) in tableColumns" :key="index">
				<el-table-column
					show-overflow-tooltip
					:prop="item.prop"
					:width="item.width"
					:label="item.label"
					:sortable="item.sort ? 'custom' : false"
					v-if="(item.if === undefined || item.if) && (item.isCheck === undefined || item.isCheck)"
				>
					<template v-slot="scope">
						<slot v-if="item.slot" :name="item.slot" :scope="scope" :model="scope.row" :prop="item.prop"></slot>
						<template v-else-if="item.url">
							<a v-if="item.dataAction" href="javascript:void(0)" class="el-link el-link--primary" @click="onCellAction(item, scope.row)">
								{{ scope.row[item.prop] }}
							</a>
							<router-link v-else-if="!item.target || item.target === '_self'" :to="resolveUrl(item.url, scope.row)" class="el-link el-link--primary">
								{{ scope.row[item.prop] }}
							</router-link>
							<a v-else :href="resolveUrl(item.url, scope.row)" :target="item.target" class="el-link el-link--primary">
								{{ scope.row[item.prop] }}
							</a>
						</template>
						<template v-else-if="item.component === 'upload'">
							<!-- <img :src="scope.row[item.prop]" width="50px" height="50px" /> -->
							<el-image :src="imgBaseUrl + scope.row[item.prop]" class="w-12 h-12 rounded" :preview-src-list="[imgBaseUrl + scope.row[item.prop]]" preview-teleported fit="fill"></el-image>
						</template>
						<template v-else-if="item.component === 'switch'">
							<el-tag :type="scope.row[item.prop] ? '' : 'info'" effect="dark">
								<Icon v-if="scope.row[item.prop]" icon="material-symbols:check" class="text-lg"></Icon>
								<Icon v-else icon="material-symbols:close" class="text-lg"></Icon>
							</el-tag>
						</template>
						<template v-else-if="item.props?.storeKey">
							{{ options[item.props.storeKey]?.find(option => option.value === scope.row[item.prop])?.label }}
						</template>
						<!-- <template v-else>
							{{ scope.row[item.prop] }}
						</template> -->
					</template>
				</el-table-column>
			</template>
			<el-table-column
				v-if="isOperate && tableColumns.length && auths(getBtnAuth(Auth.LOOK, Auth.SET, Auth.DEL))"
				label="操作"
				:width="operateWidth"
				fixed="right">
				<template v-slot="scope">
					<slot name="table-handle-before" :scope="scope"></slot>
					<el-button v-auths="getBtnAuth(Auth.LOOK)" text type="info" @click="onDetail(scope.row)">查看</el-button>
					<el-button v-auths="getBtnAuth(Auth.SET)" text type="primary" @click="onEdit(scope.row)">修改</el-button>
					<el-popconfirm title="确定删除吗？" @confirm="onDel(scope.row)">
						<template #reference>
							<el-button v-auths="getBtnAuth(Auth.DEL)" text type="danger">删除</el-button>
						</template>
					</el-popconfirm>
					<slot name="table-handle-after" :scope="scope"></slot>
				</template>
			</el-table-column>
			<template #empty>
				<el-empty description="暂无数据" />
			</template>
			<slot></slot>
		</el-table>
		<div class="table-footer mt15">
			<el-pagination
				v-if="pagerVisible"
				v-model:current-page="state.page.pageIndex"
				v-model:page-size="state.page.pageSize"
				:pager-count="5"
				:page-sizes="[10, 20, 30]"
				:total="total"
				layout="total, sizes, prev, pager, next, jumper"
				background
				@size-change="onHandleSizeChange"
				@current-change="onHandleCurrentChange"
			>
			</el-pagination>
			<div class="table-footer-tool">
				<SvgIcon name="iconfont icon-shuaxin" :size="22" title="刷新" @click="onRefreshTable" />
				<el-popover
					placement="top-end"
					trigger="click"
					transition="el-zoom-in-top"
					popper-class="table-tool-popper"
					:width="300"
					:persistent="false"
					@show="onSetTable"
				>
					<template #reference>
						<SvgIcon name="iconfont icon-quanjushezhi_o" :size="22" title="设置" />
					</template>
					<template #default>
						<div class="tool-box">
							<el-tooltip content="拖动进行排序" placement="top-start">
								<SvgIcon name="fa fa-question-circle-o" :size="17" class="ml11" color="#909399" />
							</el-tooltip>
							<el-checkbox
								v-model="state.checkListAll"
								:indeterminate="state.checkListIndeterminate"
								class="ml10 mr1"
								label="列显示"
								@change="onCheckAllChange"
							/>
							<el-checkbox v-model="getConfig.isSerialNo" class="ml12 mr1" label="序号" />
							<el-checkbox v-model="getConfig.isSelection" class="ml12 mr1" label="多选" />
						</div>
						<el-scrollbar>
							<draggable
								class="tool-sortable"
								v-model="tableColumns" 
								item-key="prop">
								<template #item="{element}">
									<div class="tool-sortable-item">
										<i class="fa fa-arrows-alt handle cursor-pointer"></i>
										<el-checkbox v-model="element.isCheck" size="default" class="ml12 mr8" :label="element.label" @change="onCheckChange" />
									</div>
								</template>
							</draggable>
							<!-- <div ref="toolSetRef" class="tool-sortable">
								<div class="tool-sortable-item" v-for="v in columns" :key="v.prop" :data-key="v.prop">
									<i class="fa fa-arrows-alt handle cursor-pointer"></i>
									<el-checkbox v-model="v.isCheck" size="default" class="ml12 mr8" :label="v.label" @change="onCheckChange" />
								</div>
							</div> -->
						</el-scrollbar>
					</template>
				</el-popover>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="netxTable">
import { reactive, computed, nextTick, watch, Ref } from 'vue';
import { ColumnSortHandler, ColumnSortParams, ElMessage } from 'element-plus';
import { ArrowDown } from '@element-plus/icons-vue';
import '/@/theme/tableTool.scss';
import Search from './search.vue';
import { ColumnConfig } from '../form/model/form';
import { Auth, TableColumn } from './type';
import draggable from 'vuedraggable'
import { useVModel } from '@vueuse/core';
import { auths } from '/@/utils/authFunction';
import { useEnumOptions } from '/@/stores/enumOptions';
// import useVModel from '/@/hook/useVModel';
interface Param {
	pageIndex?: number;
	pageSize?: number;
	sort?: string,
	desc?: boolean,
}
export interface Props {
	total?: number;
  loading?: boolean;
  isBorder?: boolean;
  isSerialNo?: boolean;
  isSelection?: boolean;
  isOperate?: boolean;
	operateWidth?: number;
	authId?: number;
	data: EmptyObjectType[];
	stat?: EmptyObjectType | null;
	columns: TableColumn[];
	search: ColumnConfig[];
	param: Param;
	pagerVisible: boolean;
	searchData: EmptyObjectType;
}
const imgBaseUrl = import.meta.env.VITE_IMG_BASE_URL
// 定义父组件传过来的值
const props = withDefaults(defineProps<Props>(), {
	total: 0, // 列表总数
	loading: true, // loading 加载
	isBorder: false, // 是否显示表格边框
	isSerialNo: false, // 是否显示表格序号
	isSelection: true, // 是否显示表格多选
	isOperate: true, // 是否显示表格操作栏
	data: () => [],
	stat: null,
	columns: () => [],
	search: () => [],
	param: () => ({
		pageIndex: 1,
		pageSize: 10,
		sort: '',
		desc: false,
	}),
	pagerVisible: true,
	searchData: () => ({})
});

const { options } = useEnumOptions()

// 解析单元格链接，将 {Id}/{Name} 等变量替换为行数据
const resolveUrl = (url: string, row: EmptyObjectType): string => {
	return url.replace(/\{(\w+)\}/g, (_, key) => {
		const val = row[key] ?? row[key.charAt(0).toLowerCase() + key.slice(1)];
		return val !== undefined ? encodeURIComponent(String(val)) : '';
	});
}

const operateWidth = computed(() => {
	if (props.operateWidth)
		return props.operateWidth
	return (auths(getBtnAuth(Auth.LOOK)) ? 55 : 0) + (auths(getBtnAuth(Auth.SET)) ? 55 : 0) + (auths(getBtnAuth(Auth.DEL)) ? 55 : 0)
})

// 定义子组件向父组件传值/事件
const emit = defineEmits(['del', 'edit', 'add', 'detail', 'batchDel', 'export', 'import', 'cellAction', 'search', 'sortHeader', 'update:searchData', 'update:columns']);
const tableColumns = useVModel(props, 'columns', emit) as Ref<TableColumn[]>;
// 定义变量内容
const state = reactive({
	page: props.param,
	selectlist: [] as EmptyObjectType[],
	checkListAll: true,
	checkListIndeterminate: false,
});
// const searchDataRef = useVModel(props, 'searchData', emit);
const searchDataRef = computed({
	get () {
		return props.searchData || {}
	},
	set (val) {
		emit('update:searchData', val)
	}
})

// 设置边框显示/隐藏
const setBorder = computed(() => {
	return props.isBorder ? true : false;
});
// 获取父组件 配置项（必传）
const getConfig = computed(() => {
	return {
		isSerialNo: props.isSerialNo,
		isSelection: props.isSelection,
	};
});
// 设置 tool columns 数据
// const setHeader = computed(() => {
// 	// console.log('props.columns', props.columns)
// 	return tableColumns.value.filter((v) => v.isCheck);
// });
watch(tableColumns, (val) => {
	val.forEach(item => {
		if (item.isCheck === undefined) {
			item.isCheck = true
		}
	})
}, {
	deep: true,
	immediate: true
})
// tool 列显示全选改变时
const onCheckAllChange = <T>(val: T) => {
	if (val) props.columns.forEach((v) => (v.isCheck = true));
	else props.columns.forEach((v) => (v.isCheck = false));
	state.checkListIndeterminate = false;
};
// tool 列显示当前项改变时
const onCheckChange = () => {
	const headers = props.columns.filter((v) => v.isCheck).length;
	state.checkListAll = headers === props.columns.length;
	state.checkListIndeterminate = headers > 0 && headers < props.columns.length;
};
// 表格多选改变时，用于导出
const onSelectionChange = (val: EmptyObjectType[]) => {
	state.selectlist = val;
};
const onEdit = (row: EmptyObjectType) => {
	emit('edit', row);
}
const onDetail = (row: EmptyObjectType) => {
	emit('detail', row);
}
// 删除当前项
const onDel = (row: EmptyObjectType) => {
	emit('del', row);
};
const onAdd = () => {
	emit('add');
}
// 批量删除
const onBatchDel = () => {
	emit('batchDel', state.selectlist);
}
// 导出
const onExport = (format: string) => {
	emit('export', format);
}
// 导入文件
const onImportFile = (file: File) => {
	emit('import', file);
	return false; // 阻止 el-upload 默认上传
}
// 单元格 AJAX 动作
const onCellAction = (item: TableColumn, row: EmptyObjectType) => {
	const url = resolveUrl(item.url!, row);
	emit('cellAction', url, row);
}
// 分页改变
const onHandleSizeChange = (val: number) => {
	state.page.pageSize = val;
	state.page.pageIndex = 1
	emit('search', state.page);
};
// 分页改变
const onHandleCurrentChange = (val: number) => {
	state.page.pageIndex = val;
	emit('search', state.page);
};
// 搜索时，分页还原成默认
const pageReset = () => {
	state.page.pageIndex = 1;
	emit('search', state.page);
};
// 刷新
const onRefreshTable = () => {
	emit('search', state.page);
};
const onChangeSort = (data: { prop: string; order: 'descending' | 'ascending' | null }) => {
	state.page.sort = data.order ? data.prop : '';
	state.page.desc = data.order === 'descending';
	emit('search', state.page);
}
// 设置
const onSetTable = () => {
	nextTick(() => {
		// const sortable = Sortable.create(toolSetRef.value, {
		// 	handle: '.handle',
		// 	dataIdAttr: 'data-key',
		// 	animation: 150,
		// 	onEnd: () => {
		// 		const headerList: EmptyObjectType[] = [];
		// 		sortable.toArray().forEach((val) => {
		// 			props.columns.forEach((v) => {
		// 				if (v.prop === val) headerList.push({ ...v });
		// 			});
		// 		});
		// 		emit('sortHeader', headerList);
		// 	},
		// });
	});
};

const onSearch = (data: EmptyObjectType) => {
	state.page = Object.assign({}, state.page, { ...data });
	pageReset();
};
const getBtnAuth = (...auths: Auth[]) => {
	if (props.authId) {
		return [props.authId + '#' + Auth.ALL, ...auths.map(val => props.authId + '#' + val)]
	}
	return []
}

// 统计行
const getSummary = ({ columns: cols }: { columns: any[] }) => {
	if (!props.stat) return cols.map(() => '');
	return cols.map((col: any, index: number) => {
		if (index === 0) return '合计';
		const prop = col.property;
		if (!prop) return '';
		const val = props.stat?.[prop];
		return val !== undefined && val !== null ? String(val) : '';
	});
}

// 暴露变量
defineExpose({
	pageReset,
});
</script>

<style scoped lang="scss">
.table-container {
	display: flex;
	flex-direction: column;
	.el-table {
		flex: 1;
	}
	.table-footer {
		display: flex;
		.table-footer-tool {
			flex: 1;
			display: flex;
			align-items: center;
			justify-content: flex-end;
			i {
				margin-right: 10px;
				cursor: pointer;
				color: var(--el-text-color-regular);
				&:last-of-type {
					margin-right: 0;
				}
			}
		}
	}
}
</style>
