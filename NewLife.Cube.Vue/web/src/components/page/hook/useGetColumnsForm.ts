import { inject, ref, Ref, unref, watch } from "vue";
import { ColumnConfig } from "../../form/model/form";
import { TableColumn } from "../../table/type";
import { ColumnIn, UsePageProps, EventSettingRef, ProvidePage } from "../model";
// import providePageKey from "../provide/key";
import { ColumnKind, usePageApi } from "/@/api/page";
import { deepMerge, getInfoFields, getSearchFields, getTableFields, toCamelCase } from "/@/utils/other";
interface Props {
  type?: string;
	searchData?: EmptyObjectType;
}
interface Emits {
	(e: 'update:searchData', val: EmptyObjectType): void;
}

export default function useGetColumnsForm (props: Props, emits: Emits, providePage?: ProvidePage) {  
  // const providePage = inject(providePageKey)
  
  const editConfig = (providePage?.editColumns || ref([])) as Ref<ColumnConfig[]>;
  const addConfig = (providePage?.addColumns || ref([])) as Ref<ColumnConfig[]>;
  const search = (providePage?.searchColumns || ref([])) as Ref<ColumnConfig[]>;
  const columns = (providePage?.tableColumns || ref([])) as Ref<TableColumn[]>;
  
  const editForm = providePage?.infoForm || ref({});
  const searchForm = providePage?.searchForm || ref<EmptyObjectType>(props.searchData || {});
  watch(() => props.searchData, (val) => {
    searchForm.value = val || {}
  })
  watch(searchForm, (val) => {
    emits('update:searchData', val)
  })
    
  // 配置修改
  const setting = (oldSetting: EventSettingRef) => {
    // 当prop配置数组时需要拆分成单个的配置(cascader组件除外)，若存在同名配置则合并配置
    let columns = providePage?.pageProps.columns?.reduce((total, item) => {
      if (item.component === 'cascader') {
        total.push(item)
      } else {
        let propArray = Array.isArray(item.prop) ? item.prop : [item.prop]
        propArray.forEach((prop, i) => {
          total.push({ ...item, prop, index: item.index !== undefined ? item.index + i : undefined})
        })
      }
      return total
    }, [] as Array<(ColumnConfig | TableColumn) & ColumnIn>) || []
    let newArr = columns.filter(item => item.in === undefined || item.in === oldSetting.type || (Array.isArray(item.in) && item.in.some(v => v === oldSetting.type)))
    let oldArrVal: Ref<Array<ColumnConfig | TableColumn>> = oldSetting.config
    newArr.forEach((newItem) => {
      if (newItem.component === 'cascader' && Array.isArray(newItem.prop)) {
        oldArrVal.value = oldArrVal.value.filter(oldItem => (newItem.prop as string[]).every(v => v !== oldItem.prop))
        oldArrVal.value.push(newItem)
      } else {
        let oldItem = oldArrVal.value.find(v => v.prop === newItem.prop)
        if (oldItem) {
          // 合并配置
          oldItem = deepMerge(oldItem, newItem)
        } else {
          // 追加配置
          oldArrVal.value.push(newItem)
        }
      }
    })
    // 排序
    let sortArr = oldArrVal.value.filter(item => item.index !== undefined).sort((item1, item2) => item1.index! - item2.index!)
    oldArrVal.value = oldArrVal.value.filter(item => item.index === undefined)
    sortArr.forEach(item => {
      oldArrVal.value.splice(item.index!, 0, item)
    })
  }

  const pageApi = usePageApi()
  pageApi.getColumns(props.type, ColumnKind.SEARCH).then(res => {
    search.value = getSearchFields(res.data)
    setting({ type: ColumnKind.SEARCH, config: search })
  })
  pageApi.getColumns(props.type, ColumnKind.LIST).then(res => {
    columns.value = getTableFields(res.data)
    setting({ type: ColumnKind.LIST, config: columns })
  })
  pageApi.getColumns(props.type, ColumnKind.EDIT).then(res => {
    editConfig.value = getInfoFields(res.data)
    setting({ type: ColumnKind.EDIT, config: editConfig })
    console.log('editConfig', editConfig.value)
  })
  pageApi.getColumns(props.type, ColumnKind.ADD).then(res => {
    addConfig.value = getInfoFields(res.data)
    setting({ type: ColumnKind.ADD, config: addConfig })
  })

  return {
    searchForm,
    editForm,
    editConfig,
    addConfig,
    search,
    columns
  }
}