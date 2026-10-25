<template>
  <div class="area-container">
    <el-card class="box-card">
      <template #header>
        <div class="card-header">
          <h3>{{ t('areaAdmin.title') }}</h3>
          <el-button type="primary" @click="handleAdd">{{ t('areaAdmin.add') }}</el-button>
        </div>
      </template>

      <CubeListToolbarSearch
        :on-search="SearchData"
        :on-reset="ResetData"
        :on-callback="callback"
      />

      <el-table
        :data="tableData"
        style="width: 100%"
        v-loading="loading"
        row-key="id"
        default-expand-all
        :tree-props="{ children: 'children' }"
      >
        <el-table-column prop="id" label="ID" width="80" />
        <el-table-column prop="name" :label="t('areaAdmin.name')" />
        <el-table-column prop="fullName" :label="t('areaAdmin.fullName')" />
        <el-table-column prop="code" :label="t('getArea.code')" />
        <el-table-column prop="pinyin" :label="t('getArea.pinyin')" />
        <el-table-column prop="jianpin" :label="t('areaAdmin.jianpin')" />
        <el-table-column prop="longitude" :label="t('getArea.longitude')" />
        <el-table-column prop="latitude" :label="t('getArea.latitude')" />
        <el-table-column :label="t('getArea.level')" width="100">
          <template #default="scope">
            <el-tag :type="getLevelType(scope.row.level)">
              {{ getLevelText(scope.row.level) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('userSearch.status')" width="100">
          <template #default="scope">
            <el-tag :type="scope.row.enable ? 'success' : 'danger'">
              {{ scope.row.enable ? t('common.yes') : t('common.no') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="t('common.actions')" width="180">
          <template #default="scope">
            <el-button type="primary" size="small" @click="handleEdit(scope.row)">
              {{ t('button.edit') }}
            </el-button>
            <el-button type="danger" size="small" @click="handleDelete(scope.row)">
              {{ t('button.delete') }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <CubeListPager
        :total="queryParams.total"
        :current-page="queryParams.pageIndex"
        :page-size="queryParams.pageSize"
        :on-current-change="CurrentPageChange"
        :on-size-change="PageSizeChange"
        :on-callback="callback"
      />
    </el-card>

    <!-- 地区表单对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="formType === 'add' ? t('areaAdmin.add') : t('areaAdmin.edit')"
      width="600px"
    >
      <el-form ref="formRef" :model="form" :rules="formRules" label-width="100px">
        <el-form-item :label="t('areaAdmin.areaName')" prop="name">
          <el-input v-model="form.name" :placeholder="t('areaAdmin.areaNameHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.fullName')" prop="fullName">
          <el-input v-model="form.fullName" :placeholder="t('areaAdmin.fullNameHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.areaCode')" prop="code">
          <el-input v-model="form.code" :placeholder="t('areaAdmin.areaCodeHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.parentArea')" prop="parentId">
          <el-select
            v-model="form.parentId"
            :placeholder="t('areaAdmin.parentAreaHint')"
            clearable
            filterable
          >
            <el-option :label="t('areaAdmin.noParent')" :value="0" />
            <el-option
              v-for="area in areaOptions"
              :key="area.id"
              :label="area.fullName || area.name"
              :value="area.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('getArea.level')" prop="level">
          <el-select v-model="form.level" :placeholder="t('areaAdmin.levelHint')">
            <el-option :label="t('areaAdmin.levelProvince')" :value="1" />
            <el-option :label="t('areaAdmin.levelCity')" :value="2" />
            <el-option :label="t('areaAdmin.levelDistrict')" :value="3" />
            <el-option :label="t('areaAdmin.levelTown')" :value="4" />
          </el-select>
        </el-form-item>
        <el-form-item :label="t('getArea.pinyin')" prop="pinyin">
          <el-input v-model="form.pinyin" :placeholder="t('areaAdmin.pinyinHint')" />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.jianpin')" prop="jianpin">
          <el-input v-model="form.jianpin" :placeholder="t('areaAdmin.jianpinHint')" />
        </el-form-item>
        <el-form-item :label="t('getArea.longitude')" prop="longitude">
          <el-input-number
            v-model="form.longitude"
            :placeholder="t('areaAdmin.longitudeHint')"
            :precision="6"
          />
        </el-form-item>
        <el-form-item :label="t('getArea.latitude')" prop="latitude">
          <el-input-number
            v-model="form.latitude"
            :placeholder="t('areaAdmin.latitudeHint')"
            :precision="6"
          />
        </el-form-item>
        <el-form-item :label="t('areaAdmin.enableStatus')" prop="enable">
          <el-switch v-model="form.enable" :active-value="true" :inactive-value="false" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">{{ t('button.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ t('button.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">

import { ref, reactive, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { FormInstance, FormRules } from 'element-plus';
import { request } from '@newlifex/cube-vue/core/utils/request';
import { apiDataToList } from '@newlifex/cube-vue/core/utils/api-helpers';
import CubeListToolbarSearch from '@newlifex/cube-vue/core/components/CubeListToolbarSearch.vue';
import CubeListPager from '@newlifex/cube-vue/core/components/CubeListPager.vue';
import { pageInfoDefault } from '@newlifex/cube-vue/core/types/common';
import type { BaseEntity } from '@newlifex/cube-vue/core/types/common';

// 地区类型接口，继承 BaseEntity
interface Area extends BaseEntity {
  name: string;
  fullName: string;
  code: string;
  parentId: number;
  level: number;
  pinyin: string;
  jianpin: string;
  longitude: number;
  latitude: number;
  enable: boolean;
  children?: Area[];
}

// 响应式数据
const loading = ref(false);
const dialogVisible = ref(false);
const formType = ref<'add' | 'edit'>('add');
const formRef = ref<FormInstance | null>(null);
const tableData = ref<Area[]>([]);
const areaOptions = ref<Area[]>([]);
const { t } = useI18n();

// 分页与搜索参数
const queryParams = reactive({
  q: '',
  level: undefined as number | undefined,
  ...pageInfoDefault
});

// 地区表单
const form = reactive<Partial<Area>>({
  name: '',
  fullName: '',
  code: '',
  parentId: 0,
  level: 1,
  pinyin: '',
  jianpin: '',
  longitude: 0,
  latitude: 0,
  enable: true,
});

// 表单验证规则
const formRules = reactive<FormRules>({
  name: [
    { required: true, message: t('areaAdmin.areaNameRequired'), trigger: 'blur' }
  ],
  code: [
    { required: true, message: t('areaAdmin.areaCodeRequired'), trigger: 'blur' }
  ],
  level: [
    { required: true, message: t('areaAdmin.levelRequired'), trigger: 'change' }
  ]
});

// 获取级别类型
const getLevelType = (level: number): 'success' | 'primary' | 'warning' | 'danger' | 'info' => {
  const types: Record<number, 'success' | 'primary' | 'warning' | 'danger' | 'info'> = {
    1: 'success',
    2: 'primary',
    3: 'warning',
    4: 'danger'
  };
  return types[level] || 'info';
};

// 获取级别文本
const getLevelText = (level: number) => {
  const texts = [
    '',
    t('areaAdmin.levelProvince'),
    t('areaAdmin.levelCity'),
    t('areaAdmin.levelDistrict'),
    t('areaAdmin.levelTown'),
  ];
  return texts[level] || t('areaAdmin.levelUnknown');
};

// 加载地区选项
const loadAreaOptions = async () => {
  try {
    // 获取所有地区用于下拉选择（不分页）
    const response = await request.get('/Cube/Area', {
      params: { pageSize: 1000, pageIndex: 1 }
    });

    console.log('area options response:', response);

    if (response && response.data) {
      const { list } = apiDataToList<Area>(response);
      areaOptions.value = list;
      console.log('area options data:', areaOptions.value);
    }
  } catch (error) {
    console.error('Failed to load area options:', error);
  }
};


// 组件回调函数
const callback = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, e?.params || {});
  loadData();
};

// 加载表格数据（用于搜索、分页等操作）
const loadData = async () => {
  loading.value = true;
  try {
    const response = await request.get('/Cube/Area', { params: queryParams });
    const { list, page } = apiDataToList<Area>(response);
    tableData.value = list;
    queryParams.total = page?.totalCount || list.length;
  } catch (error) {
    ElMessage.error(t('common.loadFailed'));
    console.error('Failed to load data:', error);
    tableData.value = [];
    queryParams.total = 0;
  } finally {
    loading.value = false;
  }
};

// 搜索按钮点击事件
const SearchData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { pageIndex: 1 }, e || {});
};

// 重置按钮点击事件
const ResetData = (e?: Record<string, unknown>) => {
  Object.assign(queryParams, { q: '', level: undefined, pageIndex: 1 }, e || {});
};

// 新增
const handleAdd = () => {
  formType.value = 'add';
  resetForm();
  // 确保地区选项是最新的
  if (areaOptions.value.length === 0) {
    loadAreaOptions();
  }
  dialogVisible.value = true;
};

// 编辑
const handleEdit = (row: Area) => {
  formType.value = 'edit';
  Object.assign(form, row);
  // 确保地区选项是最新的
  if (areaOptions.value.length === 0) {
    loadAreaOptions();
  }
  dialogVisible.value = true;
};

// 页码变更处理
const CurrentPageChange = (page: number) => {
  queryParams.pageIndex = page;
};

// 每页显示条数变更处理
const PageSizeChange = (size: number) => {
  queryParams.pageSize = size;
  queryParams.pageIndex = 1;
};

// 删除
const handleDelete = (row: Area) => {
  ElMessageBox.confirm(
    t('areaAdmin.deleteConfirm', { name: row.fullName || row.name }),
    t('fileManager.confirmDelete'),
    {
      confirmButtonText: t('button.confirm'),
      cancelButtonText: t('button.cancel'),
      type: 'warning',
    }
  ).then(async () => {
    try {
      await request.delete(`/Cube/Area/${row.id}`);
      ElMessage.success(t('common.deleteSuccess'));
      loadData();
    } catch (error) {
      ElMessage.error(t('common.deleteFailed'));
      console.error('Failed to delete:', error);
    }
  });
};

// 重置表单
const resetForm = () => {
  Object.assign(form, {
    id: undefined,
    name: '',
    fullName: '',
    code: '',
    parentId: 0,
    level: 1,
    pinyin: '',
    jianpin: '',
    longitude: 0,
    latitude: 0,
    enable: true,
  });
  formRef.value?.clearValidate();
};

// 提交表单
const submitForm = () => {
  if (!formRef.value) return;

  formRef.value.validate(async (valid: boolean) => {
    if (valid) {
      try {
        if (formType.value === 'add') {
          await request.post('/Cube/Area', form);
          ElMessage.success(t('common.addSuccess'));
        } else {
          await request.put(`/Cube/Area/${form.id}`, form);
          ElMessage.success(t('common.updateSuccess'));
        }
        dialogVisible.value = false;
        loadData();
      } catch (error) {
        ElMessage.error(
          formType.value === 'add' ? t('common.addFailed') : t('common.updateFailed')
        );
        console.error('Operation failed:', error);
      }
    }
  });
};

// 初始化
onMounted(() => {
  loadAreaOptions();
  loadData();
});
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
