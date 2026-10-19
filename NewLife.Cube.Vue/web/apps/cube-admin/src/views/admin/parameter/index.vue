<script setup lang="ts">
/**
 * 参数管理 —— 默认列表页 + 一处单元格定制
 *
 * 这里原先是全仓唯一一处 Section 覆盖（`ListTableContent.vue`，整张表格手写一遍），
 * 存在的唯一理由是把 Kind（枚举，后端下发数字）渲染成「普通/系统/用户」标签。
 * CubeTable 的 `#col-<字段名>` 就是为这种单列定制准备的，所以不再需要复制整张表：
 * 列集、搜索、分页、增删改查、导入导出全部回到框架默认行为。
 */
import CubeTable from '@newlifex/cube-vue/core/components/CubeTable/CubeTable.vue';
import ListChartButton from '@newlifex/cube-vue/core/views/components/ListChartButton.vue';
import ListPageHeader from '@newlifex/cube-vue/core/views/components/ListPageHeader.vue';
import { getKind } from './parameter-kind';
</script>

<template>
  <CubeTable>
    <template #header>
      <ListPageHeader title="参数管理" />
    </template>

    <!-- 这页原本是默认页 + Section 覆盖，自带「图表」；改成自定义视图后显式放回来，保持能力不变 -->
    <template #toolbar-extra>
      <ListChartButton />
    </template>

    <!-- 枚举列：后端下发 Int32，按 Cube 侧的枚举映射表显示成标签 -->
    <template #col-Kind="{ value }">
      <el-tag :type="getKind(value).type || undefined" effect="plain" round>
        {{ getKind(value).text }}
      </el-tag>
    </template>
  </CubeTable>
</template>
