---
name: modal-organize
description: |
  命令式弹窗的代码组织规范与最佳实践。
  当用户问"弹窗怎么组织"、"弹窗放哪"、"弹窗目录结构"、"弹窗代码规范"、"新增弹窗怎么写"、"重构弹窗结构"时使用。
  核心原则：Vue 组件 + openXxx.ts 打开函数作为"一对"放在一起，按业务归属就近放置。
---

# 命令式弹窗代码组织规范

## 什么时候用

当需要新增弹窗、重构弹窗结构、或询问弹窗代码应该放在哪里时使用。

## 核心原则

**每个弹窗 = 一个文件夹 = Vue 组件 + openXxx.ts 打开函数**

高内聚、易维护、按业务归属就近放置。

---

## 推荐结构：页面内部聚合

假设页面是 `UserManagement`，有新增、编辑、详情、分配角色等弹窗。

```
src/
├── views/
│   └── user-management/
│       ├── index.vue                  # 主页面，只调用 openXxx 函数
│       ├── modals/                    # 该页面专属弹窗聚合目录
│       │   ├── add-edit-user/
│       │   │   ├── UserForm.vue       # 表单组件（纯UI+逻辑）
│       │   │   └── openUserForm.ts    # 打开新增/编辑弹窗的命令式函数
│       │   ├── user-detail/
│       │   │   ├── UserDetail.vue     # 详情组件
│       │   │   └── openUserDetail.ts
│       │   ├── assign-role/
│       │   │   ├── AssignRole.vue
│       │   │   └── openAssignRole.ts
│       │   └── confirm-delete/
│       │       ├── ConfirmDelete.vue
│       │       └── openConfirmDelete.ts
│       └── hooks/                     # 页面级通用 hooks（可选）
│           └── useUserActions.ts
```

---

## 文件内容示例

### 1. 打开函数（openUserForm.ts）

```typescript
import { useModal } from '@newlifex/cube-vue/core/composables/useModal';
import UserForm from './UserForm.vue';
import type { UserFormData } from './UserForm.vue';

/**
 * 打开新增用户弹窗
 */
export function openAddUser(): Promise<UserFormData | null> {
  return new Promise((resolve) => {
    const { openModal } = useModal();
    openModal({
      title: '新增用户',
      width: '600px',
      component: UserForm,
      componentProps: { mode: 'add' },
      onConfirm: async (data: UserFormData) => {
        // 调用新增 API
        await createUserApi(data);
        resolve(data);
      },
      onCancel: () => resolve(null),
      onClosed: () => resolve(null),
    });
  });
}

/**
 * 打开编辑用户弹窗
 */
export function openEditUser(user: User): Promise<UserFormData | null> {
  return new Promise((resolve) => {
    const { openModal } = useModal();
    openModal({
      title: '编辑用户',
      width: '600px',
      component: UserForm,
      componentProps: { mode: 'edit', modelValue: user },
      componentEvents: {
        'update:modelValue': (val: UserFormData) => { /* 可选：实时同步 */ },
      },
      onConfirm: async (data: UserFormData) => {
        await updateUserApi(user.id, data);
        resolve(data);
      },
      onCancel: () => resolve(null),
      onClosed: () => resolve(null),
    });
  });
}
```

### 2. 表单组件（UserForm.vue）

```vue
<template>
  <el-form :model="formData" label-width="80px">
    <el-form-item label="用户名" prop="name">
      <el-input v-model="formData.name" />
    </el-form-item>
    <el-form-item label="邮箱" prop="email">
      <el-input v-model="formData.email" />
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { computed } from 'vue';

export interface UserFormData {
  name: string;
  email: string;
}

const props = defineProps<{
  mode: 'add' | 'edit';
  modelValue?: Partial<UserFormData>;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: UserFormData];
}>();

const formData = computed<UserFormData>({
  get: () => ({
    name: props.modelValue?.name ?? '',
    email: props.modelValue?.email ?? '',
  }),
  set: (val) => emit('update:modelValue', val),
});
</script>
```

### 3. 主页面调用（index.vue）

```vue
<script setup lang="ts">
import { openAddUser, openEditUser } from './modals/add-edit-user/openUserForm';
import { openConfirmDelete } from './modals/confirm-delete/openConfirmDelete';

/**
 * 新增用户
 */
async function handleAdd() {
  const result = await openAddUser();
  if (result) {
    // 新增成功，刷新列表
    await fetchList();
  }
}

/**
 * 编辑用户
 */
async function handleEdit(row: User) {
  const result = await openEditUser(row);
  if (result) {
    await fetchList();
  }
}

/**
 * 删除用户
 */
async function handleDelete(row: User) {
  const confirmed = await openConfirmDelete(row);
  if (confirmed) {
    await deleteUserApi(row.id);
    await fetchList();
  }
}
</script>
```

---

## 为什么这样组织？

| 优势 | 说明 |
|------|------|
| 高内聚 | 弹窗的 UI（.vue）和调用入口（.ts）紧密耦合，修改一个弹窗只需在一个文件夹内完成 |
| 易定位 | 看到 `openAddUser`，直接在同级文件夹找 `UserForm.vue`，不用满项目搜索 |
| 按页面隔离 | 不同页面的弹窗互不干扰，删除页面直接连 `modals/` 一起删除，无残留 |
| 按需复用 | 如果多个页面共用同一个弹窗（如确认删除），可以将其提升到 `src/components/modals/` 公共区域 |
| 无全局污染 | 弹窗组件不会注册到全局，命令式调用函数也只是普通的 TS 导出，树摇友好 |

---

## 跨页面复用：提升到公共组件

当弹窗需要在多个页面使用时，将其提升为公共弹窗组件：

```
src/
├── components/
│   └── modals/                  # 全局共享弹窗
│       ├── confirm-delete/
│       │   ├── ConfirmDelete.vue
│       │   └── openConfirmDelete.ts
│       └── import-excel/
│           ├── ImportExcel.vue
│           └── openImportExcel.ts
├── views/
│   └── user-management/
│       ├── index.vue
│       └── modals/              # 页面专属弹窗仍保留
│           └── add-edit-user/
│               ├── UserForm.vue
│               └── openUserForm.ts
```

调用时从公共路径导入即可：

```typescript
import { openConfirmDelete } from '@/components/modals/confirm-delete/openConfirmDelete';
```

---

## Promise 化 vs 回调式

两种调用风格都支持，根据场景选择：

### Promise 化（推荐）

适合简单的确认/表单场景，调用方代码更简洁：

```typescript
const result = await openAddUser();
if (result) {
  // 处理成功逻辑
}
```

### 回调式

适合复杂场景（需要实时控制弹窗状态、loading 等）：

```typescript
const modal = openModal({
  title: '新增',
  component: UserForm,
  onConfirm: async () => {
    modal.setConfirmLoading(true);
    try {
      await save();
      modal.close();
    } finally {
      modal.setConfirmLoading(false);
    }
  },
});
```

---

## 命名约定

| 项目 | 约定 | 示例 |
|------|------|------|
| 文件夹 | kebab-case，描述弹窗用途 | `add-edit-user/`、`user-detail/` |
| Vue 组件 | PascalCase，描述内容 | `UserForm.vue`、`UserDetail.vue` |
| 打开函数文件 | `open` + 组件名（小驼峰） | `openUserForm.ts` |
| 打开函数名 | `open` + 用途（小驼峰） | `openAddUser()`、`openEditUser()` |

---

## 三种内容模式

useModal 支持三种内容模式，按复杂度选择：

| 模式 | 适用场景 | 示例 |
|------|----------|------|
| `component` | 复杂表单/交互，独立的 Vue 组件 | 新增/编辑用户表单 |
| `config` | 简单表单，字段配置化即可 | 简单的新增弹窗 |
| `render` | 完全自定义渲染逻辑 | 特殊的交互式弹窗 |

### component 模式（最常用）

```typescript
openModal({
  title: '新增用户',
  component: UserForm,
  componentProps: { mode: 'add' },
  onConfirm: async (data) => { /* ... */ },
});
```

### config 模式（简单表单）

```typescript
openModal({
  title: '新增产品',
  type: 'auto',
  config: [
    { prop: 'name', label: '产品名称', required: true, component: 'input' },
    { prop: 'categoryId', label: '类别', component: 'select', props: { options: categoryOptions } },
  ],
  modelValue: formData,
  onSubmitSuccess: (data) => { /* ... */ },
});
```

支持的组件类型：`input`、`select`、`textarea`（input + type）、`switch`、`inputNumber`、`radioGroup`、`checkboxGroup`、`datePicker`

---

## 检查清单

新增/重构弹窗时，确认以下几点：

1. ✅ 弹窗放在最近的业务归属目录下（页面内 modals/ 或 components/modals/）
2. ✅ 每个弹窗有自己的文件夹，包含 .vue 和 openXxx.ts
3. ✅ 主页面只导入 openXxx 函数，不直接引用弹窗组件
4. ✅ 表单数据类型从 Vue 组件导出，打开函数复用类型
5. ✅ 返回 Promise 的函数，取消/关闭时 resolve(null)
