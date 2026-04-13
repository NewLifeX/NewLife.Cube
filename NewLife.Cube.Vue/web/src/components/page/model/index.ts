import { TableProps } from "element-plus";
import { Ref } from "vue";
import { ColumnConfig } from "../../form/model/form";
import { TableColumn, TableMoreProps } from "../../table/type";
import { ColumnKind } from "/@/api/page";

export type EditWrapper = 'div' | 'dialog' | 'drawer';

export interface TableDemoState {
	tableData: {
		data: EmptyObjectType[];
		header: TableColumn[];
		config: {
			total: number;
			loading: boolean;
			isBorder: boolean;
			isSelection: boolean;
			isSerialNo: boolean;
			isOperate: boolean;
		};
		search: ColumnConfig[];
		param: {
			pageNum: number;
			pageSize: number;
		};
	};
}

type EventSettingRefConfig<T> = T extends ColumnKind.LIST ? Array<TableColumn> : Array<ColumnConfig>

export interface EventSettingRef {
	type: ColumnKind;
	config: Ref<EventSettingRefConfig<EventSettingRef['type']>>;
	formData?: Ref<EmptyObjectType>;
}

// type UsePagePropsList = {
// 	in: ColumnKind.LIST
// } & TableColumn
// type UsePagePropsForm = {
// 	in: ColumnKind.ADD | ColumnKind.DETAIL | ColumnKind.EDIT | ColumnKind.SEARCH
// } & ColumnConfig
// type UsePagePropsCommon = ({in?: ColumnKind[]} & TableColumn) | ({ in?: ColumnKind[]} & ColumnConfig);

export interface ColumnIn {
	in?: ColumnKind | Array<ColumnKind>;
}
export interface MoreProp extends ColumnIn {
	/**参数名 */
	prop: string | Array<string>;
}
export interface UsePageProps {
	/** 表单配置 */
	columns?: Array<(Omit<TableColumn, 'prop'> & MoreProp) | (Omit<ColumnConfig, 'prop'> & MoreProp)>;
	/** 表格配置 */
	tableConfig?: Partial<TableProps<EmptyObjectType>> & Partial<TableMoreProps> & {
		api?: (...props: EmptyArrayType) => Promise<EmptyObjectType | Array<EmptyObjectType>>;
		url?: string;
		requestProps?: EmptyObjectType;
		handleWidth?: number;
	},
	detailConfig?: {
		api?: (...props: EmptyArrayType) => Promise<EmptyObjectType | Array<EmptyObjectType>>;
		url?: string;
		requestProps?: EmptyObjectType | ((row: EmptyObjectType) => EmptyObjectType);
	},
	addConfig?: {
		api?: (...props: EmptyArrayType) => Promise<EmptyObjectType | Array<EmptyObjectType>>;
		url?: string;
		requestProps?: EmptyObjectType;
	},
	editConfig?: {
		api?: (...props: EmptyArrayType) => Promise<EmptyObjectType | Array<EmptyObjectType>>;
		url?: string;
		requestProps?: EmptyObjectType;
	},
	onAddClick?: () => void;
	onAddBefore?: (data: EmptyObjectType, addFun: Function) => void;
	onAddAfter?: (data: EmptyObjectType) => void;
	onEditClick?: (data: EmptyObjectType) => void;
	onEditBefore?: (data: EmptyObjectType, editFun: Function) => void;
	onEditAfter?: (data: EmptyObjectType) => void;
	onDelBefore?: (data: EmptyObjectType, delFun: Function) => void;
	onDelAfter?: (data: EmptyObjectType) => void;
}

export interface PageHandle {
	reload: () => void;
}

export interface ProvidePage {
	tableColumns: Ref<TableColumn[]>;
	searchColumns: Ref<ColumnConfig[]>;
	editColumns: Ref<ColumnConfig[]>;
	addColumns: Ref<ColumnConfig[]>;
	detailColumns: Ref<ColumnConfig[]>;
	searchForm: Ref<EmptyObjectType>;
	infoForm: Ref<EmptyObjectType>;
	pageProps: UsePageProps;
	handle: PageHandle,
}
