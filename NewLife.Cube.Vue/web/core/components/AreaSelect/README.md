# AreaSelect 组件

地区选择控件（el-cascader 懒加载）：按级行政区逐层懒加载，回显经 `/Cube/AreaParents` 还原路径。

> 本 README 遵循技能 `vue-component-visual-loop` §七 模板。CT 覆盖目录规范要求（component-visual-dev.md §二）。

---

## Part A：功能需求与验证

### 1. 功能需求（Functional Requirements）

- **FR1（逐层懒加载）**：作为业务表单使用者，我希望展开面板后按层级懒加载下级行政区，以便从树中选区。验收：展开面板先渲染根层（省/直辖市），点开节点再加载其子级。
- **FR2（回显）**：作为使用者，我希望 `modelValue` 传入已选 id 时组件经 `/Cube/AreaParents` 还原并回显完整路径文本，以便编辑态还原。验收：输入框显示路径 label（如「北京市 / 市辖区 / 东城区」），不得裸数字。
- **FR3（v-model）**：作为调用方，我希望选叶子后 `update:modelValue` 抛出叶子 id，以便表单集成。
- **FR4（空态显式）**：作为使用者，当某节点无子级或加载失败时，面板显示明确的「暂无数据」空态而非静默空白。

### 2. 功能 ↔ Story ↔ 测试 对应表

| 功能 | Story（`?story=`） | CT 断言（AreaSelect.ct.spec.ts） | 截图 |
|---|---|---|---|
| FR1 逐层懒加载五区真渲染 | `AreaSelect/LazyRoot` | 展开面板含「北京市」→点开→五区（110101东城/110102西城/110105朝阳/110106丰台/110108海淀），且 not.toContainText('暂无数据') | dropdown 基线 |
| FR2 路径回显不裸数字 | `AreaSelect/Echo` | input value 匹配 `/东城区/`，目录 not 含 `110101` | input 基线 |

### 3. 数据面（CT mock）

`web/ct/mocks/request.ts` Area 路由表：`/Cube/AreaChilds?id=0`→北京市(110000)；`id=110000`→五区；`/Cube/AreaParents`→北京/市辖区/东城链。未命中路由保持 noop 信封（既有 story 零影响）。

---

## Part B：缺陷复盘（2026-11-11）

**组合缺陷（「暂无数据」定谳）**：`index.vue` 模板 `:options="[]"` 字面量 prop（每次渲染新引用）× element-plus 2.14.8 cascader-panel `watch(() => props.options, initStore, {deep:true})`（引用一变即 store 全量重建、清掉 lazy 已灌数据）× lazyGuard「发起即 mark」（偏离 `areaSelect.logic.ts` 契约：mark 应在失败/空结果之后）→ store 清空重渲染再触发 lazyLoad 被 canLoad 短路 `resolve([])` → 面板永久空。

**门禁盲区修复**：既有 Vitest 直调 `lazyProps.lazyLoad(...)` 不经面板 store（假绿根因）；补 story+ct.spec 走真浏览器面板交互关闭该盲区（jsdom 永远测不出 store 重建清数据）。

**红线**：lazy 数据走 `/Cube/AreaChilds`（单段 URL）；空态显式「暂无数据」。

### 修复落地（fix① + fix②）

- **fix①**：模板去掉 `:options="[]"` 字面量 prop——store 不再被渲染期新引用反复重建。
- **fix②**：`areaSelect.logic.ts` 的 `createLazyGuard` 契约归位——**只有失败/空结果才 mark**；成功不 mark，同节点可重入（`.then` 内 `options.length===0` 才 mark）。lazyLoad 入口先 `canLoad(id)` 短路，失败 catch mark + `resolve([])`，`finally` 释放 loading。该契约被第 6 个单测钉死（成功后重入必须重新发请求、失败后重入必须短路）。
- **单测计时陷阱**：mount 自动触发的根请求，其 `.then(mark)` 微任务链在断言计数前未落定 → 断言前先 `await new Promise(r => setTimeout(r, 0))` 冲刷微任务，否则 rootCount 虚高（曾致钉子测试红）。

### Echo 用例根因与重设计

「挂载即回显」在 element-plus lazy cascader 上**结构性不可达**：modelValue 的 label 解析依赖 cascader-panel store 加载到对应节点，而挂载时面板未展开 → 无 lazyLoad → 无 label。原「仅挂载断言 input 有值」的写法是伪需求；Echo spec 已重设计为**交互驱动**：点击链 `北京市 → 东城区` 后断言 `input` value 匹配 `/东城区/` 且不含裸数字。「挂载即回显」若要做需另立机制（挂载即静默预载两级），已记 P3 backlog。

### CT 环境备忘（后来者少踩坑）

- **vite 别名铁律**：CT vite.config.ts 里 RegExp `find` 只替换**匹配段**——`{ find: /.*utils\/request$/ }` 必须贪婪写法，`(^|\/)utils\/request$` 这类锚定开头/分组的写法会把 replacement 拼在捕获段之后，破坏模块路径（首跑实测 import 崩）。包名字符串别名（`@newlifex/cube-vue/core/utils/request`）排在数组前面，优先命中。vite.config.ts 改动不 HMR，需整进程重启 vite；`.vue` 改动可 HMR。
- **mock 树形态**：CT mock `/Cube/AreaChilds` 为 **2 级直挂**（北京市 → 五区，跳过「市辖区」），Echo 交互链是 `北京市 → 东城区` 两步；`/Cube/AreaParents` 仍提供 3 级链供路径说明。mock 拦截在模块层，命中桩即无真 HTTP；DOM/值状态是硬证据。
- **vitest 调用**：web 包 `test:unit` 脚本已内置 `--config vitest.config.unit.ts`，CLI 追加第二份 `--config` 会 cac 崩溃；只传位置参数（如 `pnpm test:unit -- core/components/AreaSelect`）。
- **门禁命令**：主题 token 门禁在 **web 包**：`pnpm run check:theme:refresh`（根目录无此脚本）；前置仓库根 `pnpm install`。
- **并行 flake 提醒**：`pnpm test:ct` 多 worker 并行时 LovSelect 族曾出现 16 例并发红、单文件重跑 13 绿的时序现象（另 3 例为 CT mock 中英文与 spec 断言错位，属 i18n 中文化未收口，非本组件）。定位 AreaSelect 相关结论时务必**单文件重跑**确认。

### P3 backlog

- 「挂载即回显」：挂载时静默预载两级路径（新增机制，非本卡范围），另立卡处理。
