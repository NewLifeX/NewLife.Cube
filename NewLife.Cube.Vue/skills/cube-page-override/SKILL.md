---
name: cube-page-override
description: |
  使用 cube-front 的 Section 机制覆盖/扩展页面组件。
  当用户说"覆盖页面"、"扩展页面"、"自定义组件"、"修改框架组件"时使用。
  Section 机制允许在不修改框架源码的情况下，覆盖搜索栏、表格、操作按钮等页面区块。
---

# Cube-Front 页面覆盖 (Section)

## 什么时候用

当用户需要修改/覆盖框架默认组件（如搜索栏、表格、分页），又不希望修改框架源码时使用。

## Section 机制原理

```
┌─────────────────────────────────────────────────┐
│                  框架组件                        │
│  ┌─────────┐  ┌─────────┐  ┌─────────────────┐  │
│  │ Header  │  │ Content │  │    Footer       │  │
│  └─────────┘  └─────────┘  └─────────────────┘  │
│       ↑           ↑              ↑              │
│       │           │              │              │
│   ┌───┴───┐   ┌───┴───┐      ┌───┴───┐         │
│   │Section│   │Section│      │Section│         │
│   │覆盖   │   │覆盖   │      │覆盖   │         │
│   └───────┘   └───────┘      └───────┘         │
└─────────────────────────────────────────────────┘
```

## 命名规则

| 规则 | 说明 |
|------|------|
| **大写开头** | 文件名必须以大写字母开头，如 `ListSearchBar.vue` |
| **PascalCase** | 多个单词时首字母都大写，如 `UserDetailHeader.vue` |
| **禁止小写** | `index.vue`、`form.vue` 等小写开头的文件不会被扫描 |

## 常用 Section 覆盖点

| Section 名称 | 覆盖位置 | 用途 |
|-------------|---------|------|
| `ListSearchBar.vue` | 列表页面搜索栏 | 自定义搜索条件 |
| `ListToolbar.vue` | 列表工具栏 | 添加自定义按钮 |
| `TableColumns.vue` | 表格列配置 | 修改列定义 |
| `FormFields.vue` | 表单字段 | 自定义表单控件 |
| `DetailHeader.vue` | 详情页头部 | 自定义详情头部 |
| `Pagination.vue` | 分页组件 | 自定义分页样式 |

## 创建 Section 组件

### 示例：覆盖列表页搜索栏

**1. 在应用的 views 目录下创建**

```
apps/<app-name>/src/views/
└── User/
    ├── index.vue              # 列表主页面
    └── ListSearchBar.vue      # 搜索栏覆盖组件
```

**2. 编写 Section 组件**

```vue
<template>
  <div class="custom-search-bar">
    <el-row :gutter="16">
      <el-col :span="8">
        <el-input
          v-model="searchForm.name"
          placeholder="请输入用户名"
          clearable
        />
      </el-col>
      <el-col :span="8">
        <el-select v-model="searchForm.status" placeholder="请选择状态">
          <el-option label="启用" :value="1" />
          <el-option label="禁用" :value="0" />
        </el-select>
      </el-col>
      <el-col :span="8">
        <el-button type="primary" @click="handleSearch">搜索</el-button>
        <el-button @click="handleReset">重置</el-button>
        <el-button type="success" @click="handleExport">导出</el-button>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';

// 搜索表单
const searchForm = reactive({
  name: '',
  status: undefined as number | undefined,
});

// 搜索事件 - 触发父组件刷新
const emit = defineEmits<{
  search: [form: typeof searchForm];
  reset: [];
}>();

function handleSearch() {
  emit('search', searchForm);
}

function handleReset() {
  searchForm.name = '';
  searchForm.status = undefined;
  emit('reset');
}

function handleExport() {
  // 自定义导出逻辑
  console.log('导出数据');
}
</script>

<style scoped>
.custom-search-bar {
  padding: 16px;
  background: #fff;
  border-radius: 4px;
}
</style>
```

### 示例：覆盖工具栏

```vue
<template>
  <div class="custom-toolbar">
    <el-space>
      <el-button type="primary" @click="handleAdd">
        <el-icon><Plus /></el-icon>
        新增用户
      </el-button>
      <el-button @click="handleBatchImport">
        <el-icon><Upload /></el-icon>
        批量导入
      </el-button>
      <el-button type="danger" @click="handleBatchDelete">
        <el-icon><Delete /></el-icon>
        批量删除
      </el-button>
    </el-space>
  </div>
</template>

<script setup lang="ts">
import { Plus, Upload, Delete } from '@element-plus/icons-vue';

function handleAdd() {
  // 新增逻辑
}

function handleBatchImport() {
  // 批量导入逻辑
}

function handleBatchDelete() {
  // 批量删除逻辑
}
</script>

<style scoped>
.custom-toolbar {
  padding: 12px 16px;
  border-bottom: 1px solid #ebeef5;
}
</style>
```

## 与父组件通信

Section 组件通过 `defineEmits` 与父组件通信：

```typescript
// 定义事件
const emit = defineEmits<{
  search: [form: object];
  reset: [];
  refresh: [];
  open: [id: number];
}>();

// 触发事件
emit('search', searchForm);
emit('reset');
emit('refresh');
emit('open', 123);
```

## 自动注册

Vite 插件会自动扫描并注册 Section 组件：

1. 插件在构建时扫描 `views/` 目录
2. 收集所有以大写字母开头的 `.vue` 文件
3. 通过 `virtual:cube-front-sections` 虚拟模块导出
4. `initApp()` 时自动调用 `registerPageSections()` 注册

## 调试 Section

确认 Section 是否生效：

1. 打开浏览器开发者工具
2. 在 Vue Devtools 中查看 `pageSections` 状态
3. 或检查控制台输出的 Section 注册信息

## 限制

- Section 组件只能覆盖框架预设的覆盖点
- 需要先确认框架是否有对应的覆盖点
- 组件名称必须严格遵守大写开头规则

## 示例：完整覆盖用户列表页

```
apps/<app-name>/src/views/User/
├── index.vue              # 主页面（由后端路由提供）
├── ListSearchBar.vue      # 覆盖搜索栏
├── ListToolbar.vue        # 覆盖工具栏
└── TableColumns.vue       # 覆盖表格列（如果支持）
```