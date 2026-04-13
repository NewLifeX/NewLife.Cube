import { provide, ref, Ref } from "vue";
import { ColumnConfig } from "../components/form/model/form";
import { TableColumn } from "../components/table/type";
import { PageHandle, UsePageProps } from "../components/page/model";
import providePageKey from "../components/page/provide/key";
import { getCurrentInstance } from "vue";
import { reactive } from "vue";
// 各区域配置项（响应式）
// 各区域表单值（响应式）
/**
 * 页面配置
 * @param pageProps 配置规则
 * @returns  setting: 回调方法; columns: 配置; forms: 表单;
 */
export default function usePage (props: UsePageProps) {
  const tableColumns = ref([]) as Ref<TableColumn[]>;
  const searchColumns = ref([]) as Ref<ColumnConfig[]>;
  const editColumns = ref([]) as Ref<ColumnConfig[]>;
  const addColumns = ref([]) as Ref<ColumnConfig[]>;
  const detailColumns = ref([]) as Ref<ColumnConfig[]>;

  const searchForm = ref<EmptyObjectType>({});
  const infoForm = ref<EmptyObjectType>({});

  const handle = reactive({} as PageHandle)
  
  provide(providePageKey, {
    pageProps: props,
    tableColumns,
    searchColumns,
    editColumns,
    addColumns,
    detailColumns,
    searchForm,
    infoForm,
    handle,
  });

  return {
    // 配置相关
    tableColumns,
    searchColumns,
    editColumns,
    addColumns,
    detailColumns,
    // 表单相关
    searchForm,
    infoForm,
    handle,
  }
}