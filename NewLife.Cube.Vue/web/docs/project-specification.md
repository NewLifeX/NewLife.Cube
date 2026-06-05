# Vite + Vue3 项目规范

## 目录

- [项目结构规范](#项目结构规范)
- [文件命名规范](#文件命名规范)
- [Vue组件规范](#Vue组件规范)
- [JavaScript/TypeScript规范](#JavaScriptTypeScript规范)
- [CSS/SCSS规范](#CSS/SCSS规范)
- [注释规范](#注释规范)
- [性能优化规范](#性能优化规范)
- [错误处理规范](#错误处理规范)
- [安全规范](#安全规范)
- [单元测试规范](#单元测试规范)
- [Git提交规范](#Git提交规范)
- [国际化规范](#国际化规范)
- [可访问性(A11y)规范](#可访问性A11y规范)
- [构建与部署规范](#构建与部署规范)

## 项目结构规范

```
cube-front/
├── public/                 # 静态资源目录
├── src/
│   ├── api/                # API接口定义
│   ├── assets/             # 项目资源文件
│   │   ├── icons/          # 图标
│   │   ├── images/         # 图片
│   │   └── styles/         # 全局样式
│   ├── components/         # 公共组件
│   │   ├── base/           # 基础组件
│   │   └── business/       # 业务组件
│   ├── composables/        # 组合式函数
│   ├── config/             # 配置文件
│   ├── constants/          # 常量定义
│   ├── directives/         # 自定义指令
│   ├── hooks/              # 自定义钩子
│   ├── layouts/            # 布局组件
│   ├── plugins/            # 插件
│   ├── router/             # 路由配置
│   ├── store/              # 状态管理
│   ├── types/              # 类型定义
│   ├── utils/              # 工具函数
│   ├── views/              # 页面视图
│   ├── App.vue             # 根组件
│   ├── main.ts             # 入口文件
│   └── env.d.ts            # 环境变量类型声明
├── tests/                  # 测试文件
├── .env                    # 环境变量
├── .eslintrc.js            # ESLint配置
├── .prettierrc.js          # Prettier配置
├── tsconfig.json           # TypeScript配置
├── vite.config.ts          # Vite配置
└── package.json            # 项目依赖
```

### 目录说明

- **api**: 按模块划分API请求
- **components**:
  - **base**: 基础UI组件，不包含业务逻辑
  - **business**: 包含业务逻辑的可复用组件
- **composables**: 可复用的组合式函数
- **views**: 按模块/功能划分的页面组件

## 文件命名规范

### 文件夹命名

- 使用kebab-case（短横线）命名法
- 例如：`user-management`，`data-analysis`

### 文件命名

1. **Vue组件文件**:

   - 使用PascalCase（帕斯卡）命名法
   - 例如：`UserProfile.vue`，`DataTable.vue`
   - 基础组件以`Base`为前缀，如`BaseButton.vue`
   - 单例组件以`The`为前缀，如`TheHeader.vue`

2. **TypeScript文件**:

   - 普通工具类文件使用kebab-case，如`date-utils.ts`
   - 类文件使用PascalCase，如`UserService.ts`
   - 类型定义文件使用`.d.ts`后缀，且使用PascalCase，如`ApiResponse.d.ts`

3. **样式文件**:

   - 使用kebab-case，如`main-theme.scss`
   - 组件私有样式与组件同名，如`UserProfile.scss`

4. **测试文件**:
   - 使用与被测试文件相同的命名方式，后缀`.spec.ts`或`.test.ts`
   - 例如：`UserProfile.spec.ts`

## Vue组件规范

### 组件结构

```vue
<template>
  <!-- 模板内容 -->
</template>

<script setup lang="ts">
// 导入
import { ref } from 'vue'
import type { PropType } from 'vue'

// 类型定义

// Props定义
const props = defineProps({
  propName: {
    type: String as PropType<string>,
    required: true,
    default: '',
  },
})

// Emits定义
const emit = defineEmits<{
  (e: 'update', value: string): void
  (e: 'submit'): void
}>()

// 响应式状态
const count = ref(0)

// 计算属性

// 方法定义

// 生命周期钩子

// 侦听器
</script>

<style scoped lang="scss">
/* 组件样式 */
</style>
```

### Props规范

- 必须使用对象形式定义props，指定类型、默认值、是否必须等
- TypeScript项目中使用PropType指定复杂类型
- Props命名使用camelCase

### 事件命名

- 使用kebab-case命名事件
- 使用语义化名称，如`update:modelValue`、`item-click`

### 组件通信

- 父子组件：Props down, Events up
- 跨层级组件：Provide/Inject或状态管理
- 避免使用事件总线

## JavaScript/TypeScript规范

### TypeScript类型规范
- 使用TypeScript类型系统
- 对于复杂类型，使用TypeScript类型别名
- 对于函数参数和返回值，使用TypeScript类型定义
- 对于类型导入，使用type关键字，如`import { type User } from './types'`

### JavaScript/TypeScript命名规范

1. **变量命名**:

   - 使用camelCase
   - 布尔值类型变量使用`is`、`has`、`can`等前缀
   - 例如：`userName`、`isVisible`、`hasPermission`

2. **常量命名**:

   - 使用UPPER_SNAKE_CASE
   - 例如：`MAX_COUNT`、`API_BASE_URL`

3. **函数命名**:

   - 使用camelCase
   - 动词开头，表明操作
   - 例如：`getUserInfo()`、`handleSubmit()`

4. **类和接口命名**:

   - 使用PascalCase
   - 例如：`UserService`、`DataModel`
   - 接口名不要使用`I`前缀，例如使用`User`而非`IUser`

5. **类型命名**:
   - 使用PascalCase
   - 例如：`UserInfo`、`ApiResponse`

### 函数规范

- 一个函数只做一件事
- 函数参数不超过3个，多参数使用对象传递
- 使用函数声明式而非函数表达式
  - 为什么推荐使用函数声明式而非函数表达式
  - 函数声明式和函数表达式是JavaScript中定义函数的两种主要方式：
  
  ```javascript
  // 函数声明式
  function doSomething() {
    // 函数体
  }
  
  // 函数表达式
  const doSomething = function() {
    // 函数体
  };
  ```
  
  - 函数声明式的优势
  
    1. **变量提升(Hoisting)** - 函数声明会被提升到当前作用域顶部，允许在代码中任何位置调用，而函数表达式必须先定义后使用

    2. **代码可读性** - 函数声明在视觉上更容易识别为一个函数，开头的`function`关键字立即表明这是一个函数定义

    3. **调试友好** - 在错误堆栈跟踪中，函数声明会显示函数名，而匿名函数表达式则不会

    4. **语义明确** - 函数声明更清晰地表达"这是一个独立功能单元"的意图，而不是一个变量被赋予了函数值

    5. **维护性** - 在大型项目中使用一致的函数定义方式有助于代码维护

    6. **自文档化** - 函数声明总是强制命名，提高了代码的自解释性
- 避免副作用，保持函数纯净

### 异步处理

- 优先使用async/await代替Promise链
- 正确处理错误，使用try/catch
- 避免回调地狱

## CSS/SCSS规范

### CSS/SCSS命名规范

- 多个单词使用短横线（kebab-case）连接
- 例如：`.card`，`.card-title`，`.user-profile-section`

### 样式结构

- 避免深层次嵌套，最好不超过3层
- 组件样式使用`scoped`或CSS Module
- 全局样式放在assets/styles目录下

### 变量使用

- 颜色、字体、间距等使用CSS变量（变量文件放在styles/variables下）
- 避免硬编码值

## 注释规范

### 文件顶部注释

```typescript
/**
 * 文件描述
 * @author 作者名
 * @date 2023-01-01
 */
```

### 函数注释

```typescript
/**
 * 函数描述
 * @param {string} param1 - 参数1描述
 * @param {number} param2 - 参数2描述
 * @returns {boolean} 返回值描述
 */
function example(param1: string, param2: number): boolean {
  // 函数实现
}
```

### 复杂逻辑注释

- 对于复杂的业务逻辑，添加必要的注释
- 注释解释"为什么"这样做，而不是"做了什么"

## 性能优化规范

### Vue组件优化

- 合理使用`v-if`和`v-show`
- 使用`keep-alive`缓存组件状态
- 大型列表使用虚拟滚动
- 避免深层次的响应式对象
- 组件中使用`shallowRef`和`shallowReactive`来优化非递归监听场景

### 懒加载和代码分割

- 路由组件使用动态导入实现懒加载

  ```typescript
  const routes = [
    {
      path: '/user',
      component: () => import('./views/User.vue')
    }
  ]
  ```

- 大型组件库按需导入
- 图片资源使用懒加载

### 构建优化

- 生产环境启用代码压缩和tree-shaking
- 使用现代模式构建(`modern mode`)
- 优化依赖大小，定期审查并移除未使用的依赖

## 错误处理规范

### 前端错误

- 实现全局错误处理
- 对可预见的错误进行优雅降级
- 使用`try/catch`捕获异步操作错误

### 错误监控

- 集成错误监控系统(如Sentry)
- 记录用户操作路径，便于复现问题
- 设置错误严重程度，区分处理

## 安全规范

### 数据处理

- 所有用户输入必须验证和消毒
- 使用`v-html`时必须确保内容安全
- 不在前端存储敏感信息
- API响应数据使用TypeScript类型限制，避免运行时错误

### 认证与授权

- 敏感操作必须二次验证
- 实现合理的RBAC权限控制
- 使用HTTPS，确保Cookie设置`Secure`和`HttpOnly`标志

## 单元测试规范

### 测试覆盖

- 业务组件的核心功能必须有单元测试
- 工具函数必须有单元测试
- 最低测试覆盖率要求：70%

### 测试内容

- 组件测试：渲染结果、事件交互、Props验证
- 工具函数测试：边界情况、异常处理
- 复杂逻辑：分支覆盖

### 测试技术

- 使用Vue Test Utils测试组件
- 使用Vitest进行单元测试
- 使用Cypress进行端到端(E2E)测试
- 使用JSDOM模拟浏览器环境进行组件测试
- 单元测试命令：`pnpm test:unit`
- E2E测试命令：
  - 生产模式：`pnpm test:e2e`
  - 开发模式：`pnpm test:e2e:dev`

### 测试最佳实践

- 单元测试应该快速且独立，不依赖外部服务
- 使用Mock函数模拟API调用和第三方服务
- 为异步组件编写测试时使用`flushPromises`
- 组件测试应聚焦于组件API而非实现细节
- E2E测试应覆盖关键用户流程和业务场景
- 使用测试驱动开发(TDD)方法开发核心功能

## Git提交规范

- 使用Angular提交规范
- 格式：`<type>(<scope>): <subject>`
- 类型(type)：
  - feat: 新功能
  - fix: 修复bug
  - docs: 文档更新
  - style: 代码风格修改
  - refactor: 重构
  - test: 测试相关
  - chore: 构建过程或辅助工具变更

### 示例

```
feat(user): 添加用户认证功能
fix(api): 修复数据请求超时问题
docs(readme): 更新安装说明
```

## 国际化规范

### 文本管理

- 所有用户可见的文本必须使用i18n
- 禁止在代码中硬编码文本
- 使用命名空间组织翻译键名

### 格式规范

- 翻译键名使用点符号层次结构，如`user.profile.title`
- 包含变量的文本使用命名参数，如`{name}已登录`
- 日期、数字、货币等使用i18n格式化函数

## 可访问性(A11y)规范

### 语义化HTML

- 使用正确的HTML标签表达内容含义
- 表单控件必须有关联的`label`
- 图片必须有`alt`属性

### 键盘导航

- 所有交互元素必须可通过键盘访问
- 合理使用`tabindex`属性
- 实现合适的焦点管理

### 颜色与对比度

- 确保文本与背景色对比度符合WCAG标准(最低4.5:1)
- 不仅使用颜色传达信息(考虑色盲用户)

## 构建与部署规范

### 环境配置

- 使用`.env`文件区分开发/测试/生产环境
- 环境变量必须以`VITE_`前缀
- 敏感配置不应提交到代码库

### 部署流程

- 使用CI/CD自动化部署
- 发布前必须通过所有测试
- 实现灰度发布或A/B测试机制

## 代码审查清单

- [ ] 代码是否符合项目规范
- [ ] 是否有重复代码可以优化
- [ ] 是否有潜在的性能问题
- [ ] 是否有适当的错误处理
- [ ] 是否有必要的注释
- [ ] 是否编写了测试
