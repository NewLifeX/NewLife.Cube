<script setup lang="ts">
/**
 * CubeTable 分页栏（对齐 MES `ListPage/PaginationBar.vue`）
 *
 * 左侧「共 N 条」+ extra 插槽，右侧「分页器（含跳页）+ 每页条数下拉」。
 * 页码 1-based（与后端 `pageIndex` 口径一致）；换每页条数回第 1 页由引擎负责。
 */
import { computed } from 'vue';
import type { CubeEngineContext } from '../../engine/types';

const props = withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    /** 可选每页条数 */
    pageSizes?: number[];
    /** 总数单位（左侧「共 N 条」） */
    unit?: string;
    /** 每页条数单位（下拉「N 条/页」），缺省跟随 unit */
    sizeUnit?: string;
  }>(),
  { pageSizes: () => [10, 20, 50], unit: '条', sizeUnit: undefined },
);

/** 每页条数下拉单位：未单独指定时跟随总数单位 */
const sizeUnitLabel = computed(() => props.sizeUnit ?? props.unit);

/**
 * 页码/条数写成可读写计算属性，而不是「prop + change 事件」：
 * el-pagination 对后者会打 deprecation 警告，且 v-model 才是它的受控口径。
 */
const page = computed({
  get: () => props.ctx.page.value,
  set: (v: number) => void props.ctx.changePage(v),
});
const pageSize = computed({
  get: () => props.ctx.pageSize.value,
  set: (v: number) => void props.ctx.changePageSize(v),
});
</script>

<template>
  <div class="ctp">
    <div class="ctp-left">
      <span class="ctp-total">共 {{ ctx.total.value }} {{ unit }}</span>
      <slot name="extra"></slot>
    </div>
    <div class="ctp-group">
      <el-pagination
        v-model:current-page="page"
        :page-size="pageSize"
        layout="prev, pager, next, jumper"
        :total="ctx.total.value"
        background
      />
      <el-select v-model="pageSize" class="ctp-size">
        <el-option v-for="s in pageSizes" :key="s" :value="s" :label="`${s} ${sizeUnitLabel}/页`" />
      </el-select>
    </div>
  </div>
</template>

<style scoped>
.ctp {
  flex-shrink: 0;
  min-height: 60px;
  padding: 0 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid var(--el-border-color-lighter);
}

.ctp-left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.ctp-total {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.ctp-group {
  display: flex;
  align-items: center;
  gap: 16px;
}

.ctp-size {
  width: 110px;
}
</style>
