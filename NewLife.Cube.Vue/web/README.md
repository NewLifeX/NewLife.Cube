# cube-front

## 推荐的IDE设置

[VSCode](https://code.visualstudio.com/) + [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar)（并禁用Vetur）。

## `.vue`导入在TS中的类型支持

TypeScript默认无法处理`.vue`导入的类型信息，所以我们用`vue-tsc`替换`tsc` CLI进行类型检查。在编辑器中，我们需要[Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar)使TypeScript语言服务能够识别`.vue`类型。

## 自定义配置

查看[Vite配置参考](https://vite.dev/config/)。

## 项目设置

```sh
pnpm install
```

### 接口配置

.env.development文件中，修改VITE_API_URL为魔方后台API地址，例如：

```sh
VITE_API_URL = https://cube3.newlifex.com
```

### 编译和开发热重载

```sh
pnpm dev
```

### 类型检查、编译和生产环境压缩

```sh
pnpm build
```

### 使用[Vitest](https://vitest.dev/)运行单元测试

```sh
pnpm test:unit
```

### 使用[Cypress](https://www.cypress.io/)运行端到端测试

```sh
pnpm test:e2e:dev
```

这将针对Vite开发服务器运行端到端测试。
它比生产构建要快得多。

但在部署之前，仍然建议使用`test:e2e`测试生产构建（例如在CI环境中）：

```sh
pnpm build
pnpm test:e2e
```

### 使用[ESLint](https://eslint.org/)进行代码检查

```sh
pnpm lint
```
