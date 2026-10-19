<script setup lang="ts">
/**
 * CubeTable 分页区（内置默认实现）
 *
 * 1-based 页码（与后端 `pageIndex` 口径一致），换每页条数回到第 1 页由引擎负责。
 */
import type { CubeEngineContext } from '../../engine/types';

withDefaults(
  defineProps<{
    ctx: CubeEngineContext;
    pageSizes?: number[];
    layout?: string;
  }>(),
  { pageSizes: () => [10, 20, 50, 100], layout: 'total, sizes, prev, pager, next, jumper' },
);
</script>

<template>
  <div class="ctp">
    <el-pagination
      background
      :layout="layout"
      :total="ctx.total.value"
      :page-sizes="pageSizes"
      :current-page="ctx.page.value"
      :page-size="ctx.pageSize.value"
      @current-change="(p: number) => ctx.changePage(p)"
      @size-change="(s: number) => ctx.changePageSize(s)"
    />
  </div>
</template>

<style scoped>
.ctp {
  display: flex;
  justify-content: flex-end;
  padding-top: 12px;
}
</style>
