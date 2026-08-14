<template>
  <div class="default-object">
    <div class="obj-layout">
      <!-- 左侧：配置页列表；多分组对象（如魔方设置）的分组作为其子菜单管理 -->
      <aside class="obj-side">
        <div class="obj-side__head">配置项</div>
        <a-spin :loading="pagesLoading" class="obj-side__spin">
          <a-menu
            :selected-keys="[activeKey]"
            :open-keys="openKeys"
            @menu-item-click="onMenuClick"
            @update:open-keys="(ks: string[]) => (openKeys = ks)"
          >
            <template v-for="p in objectPages" :key="p.type">
              <a-sub-menu
                v-if="p.type === currentType && isMultiCategory"
                :key="'grp-' + p.type"
                :title="p.name"
              >
                <a-menu-item v-for="c in categories" :key="p.type + '#' + c">
                  {{ c }}
                </a-menu-item>
              </a-sub-menu>
              <a-menu-item v-else :key="p.type">{{ p.name }}</a-menu-item>
            </template>
          </a-menu>
        </a-spin>
      </aside>

      <!-- 右侧：主题表面 + Category 分组表单（不折叠、流式 6/12）+ 底部面板 -->
      <main class="obj-main">
        <div class="obj-surface">
          <div class="obj-header">
            <h2>{{ title }}</h2>
          </div>
          <a-alert v-if="loadError" type="warning" show-icon class="obj-alert">
            {{ loadError }}
          </a-alert>
          <a-spin :loading="loading" style="display: block">
            <a-empty v-if="!loading && !loadError && !fields.length" description="无字段" />
            <template v-else-if="!loading">
              <a-form :model="form" layout="vertical" class="obj-form">
                <div class="form-groups">
                  <section v-for="g in visibleGroups" :key="g.category" class="form-group">
                    <h3 class="form-group__title">{{ g.category }}</h3>
                    <!-- 每个配置项占一行，居中排版，宽 6/12（全宽控件 12/12） -->
                    <a-row
                      v-for="f in g.fields"
                      :key="f.name"
                      :gutter="16"
                      justify="center"
                    >
                      <a-col :span="isFullWidthControl(resolveControl(f)) ? 24 : 12">
                        <a-form-item
                          :label="f.displayName || f.name"
                          :tooltip="f.description || undefined"
                        >
                          <FieldInput
                            :field="f"
                            :model-value="form[f.name]"
                            :disabled="!canUpdate"
                            :type-path="currentType"
                            @update:model-value="form[f.name] = $event"
                          />
                        </a-form-item>
                      </a-col>
                    </a-row>
                  </section>
                </div>
              </a-form>
            </template>
          </a-spin>
        </div>

        <!-- 底部面板：参照默认列表视图的 list-pager，保存操作常驻底部 -->
        <div v-if="canUpdate && fields.length && !loadError" class="obj-footer">
          <a-space>
            <a-button type="primary" :loading="saving" @click="save">保存</a-button>
          </a-space>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
/**
 * DefaultObject — 通用 ObjectController 配置中心（OSC-2608139feb 魔方设置优化）
 * 契约：GET type + GetFields + FieldInput + PUT；左列表（自动注入 Object 页）右配置；
 * 多分组对象的 Category 作为左子菜单管理；右侧分组不折叠、流式布局（标签+编辑框 6/12）；
 * description 经 form-item tooltip 展示；底部常驻保存面板。
 */
import FieldInput from '@/components/FieldInput.vue';
import { isFullWidthControl, resolveControl } from '@/core/utils/fieldControl';
import { useDefaultObject } from './useDefaultObject';

const props = defineProps<{
  type: string;
  authId?: number;
}>();

const {
  currentType,
  objectPages,
  pagesLoading,
  loading,
  loadError,
  fields,
  categories,
  isMultiCategory,
  visibleGroups,
  activeKey,
  openKeys,
  form,
  canUpdate,
  saving,
  title,
  onSelectPage,
  onSelectCategory,
  save,
} = useDefaultObject(props);

/** 菜单点击：key 含 # 为「对象#分组」切分组，否则切配置页 */
function onMenuClick(key: string) {
  const idx = key.indexOf('#');
  if (idx >= 0) {
    const type = key.slice(0, idx);
    if (type === currentType.value) onSelectCategory(key.slice(idx + 1));
    else onSelectPage(type);
  } else {
    onSelectPage(key);
  }
}
</script>

<style scoped>
.default-object {
  padding: 0 4px;
}
.obj-layout {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}
.obj-side {
  flex: 0 0 200px;
  padding: 12px 8px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
  min-height: 200px;
}
.obj-side__head {
  padding: 0 12px 8px;
  font-size: 13px;
  font-weight: 600;
  color: var(--color-text-3);
}
.obj-side__spin {
  display: block;
}
.obj-side :deep(.arco-menu) {
  width: 100%;
  background: transparent;
}
.obj-main {
  flex: 1 1 auto;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}
/* 主题表面：与默认列表视图 list-surface 同源的主题变量外壳 */
.obj-surface {
  padding: 16px 16px 4px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.obj-header {
  margin-bottom: 16px;
}
.obj-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text-1);
}
.obj-alert {
  margin-bottom: 16px;
}
.obj-form {
  width: 100%;
}
/* 分组外壳：与实体编辑表单 FormContent.form-group 同一主题样式，占满面板宽度 */
.form-groups {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;
}
.form-group {
  padding: 16px 16px 4px;
  background: var(--color-bg-1);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.form-group__title {
  margin: 0 0 12px;
  font-size: 14px;
  font-weight: 600;
  line-height: 22px;
  color: var(--color-text-1);
}
/* 底部面板：参照默认列表视图 list-pager 的常驻底部操作区，保存按钮右对齐 */
.obj-footer {
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
</style>
