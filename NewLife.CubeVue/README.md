# newlife-cube-vueui

- 整体框架参考 https://github.com/xxred/Easy.Front-End
- UI 库 https://element.eleme.cn/
- api 文档地址 http://81.69.253.197:8000/index.html
- 旧版魔方 http://81.69.253.197:8000/Admin

## 环境

- 下载安装 nodejs： https://nodejs.org/dist/v14.15.5/node-v14.15.5-x64.msi
- 注意安装完之后需要重新打开命令行窗口，命令才会生效
- 安装 yarn：`npm i yarn -g`

## 安装依赖

```bash
yarn install
```

### 开发

```bash
yarn serve
```

### 构建

```bash
yarn build
```

### TODO

- [ ] 文件、数据库等部分页面，需要参照旧版进行自定义
- [x] 角色编辑页自定义，参考 https://github.com/xxred/Easy.Front-End/blob/master/src/views/Role/form.vue
- [x] http 请求错误拦截，500 状态弹窗提示
- [x] 表单编辑页调整，可滚动，页内表单，自动排列
- [x] 列表页字段排序支持
- [x] 手机浏览菜单侧边栏自动收缩
- [ ] 导航栏头像与名称角色放一起
- [ ] 点击头像的下拉添加个人信息，个人信息编辑页添加
- [x] 组件覆盖
