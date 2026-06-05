# Boreal Admin — 设计系统文档

> 唯一参考源：`design/front/out/layouts/light-1.html`
> 所有 hex 值、尺寸数值均从源文件精确提取，不依赖记忆。

---

## 1. Visual Theme & Atmosphere

### 设计定位

Boreal Admin 是专业行政管理后台，采用 **Boreal（北方森林）** 主题，取意北方针叶林的自然沉稳——深林绿、暖白、薄荷绿构成的三层视觉系统，传达专注、权威与高效感。

### 核心视觉三层

| 层级 | 色系                         | 对应区域           | 功能                       |
| ---- | ---------------------------- | ------------------ | -------------------------- |
| 顶层 | 深林绿 `#1a3328`             | 顶部导航栏         | 权威感、品牌识别、定向操作 |
| 中层 | 暖白 `#f4f5f1`               | 页面内容区背景     | 低疲劳度长时阅读           |
| 强调 | 薄荷绿 `#4ec685` / `#1d7040` | 交互元素、状态标记 | 视觉焦点引导               |

### 与纯摄影系设计的对比定位

Boreal 不使用全屏摄影背景或重渐变。与 SpaceX 等外向展示型设计相反，本设计优先**信息密度**与**操作效率**，色彩系统服务于数据层次化而非视觉震撼。

---

## 2. Color Palette & Roles

### 顶导系列

| 变量      | Hex 值    | 角色说明                                          |
| --------- | --------- | ------------------------------------------------- |
| `--tn`    | `#1a3328` | 顶导背景色，深林绿主色                            |
| `--tn-b`  | `#224538` | 顶导分隔线/边框，稍浅的林绿                       |
| `--tn-t`  | `#74b898` | 顶导文字/图标默认色，薄荷绿中调                   |
| `--tn-th` | `#e0f5ea` | 顶导文字 hover 色，近白薄荷                       |
| `--tn-ac` | `#4ec685` | 顶导强调色（Logo 背景、active 底线、mega 顶边框） |

### 内容区系列

| 变量     | Hex 值    | 角色说明              |
| -------- | --------- | --------------------- |
| `--bg`   | `#f4f5f1` | 页面背景，暖灰白      |
| `--card` | `#ffffff` | 卡片/面板背景，纯白   |
| `--bd`   | `#e0e6da` | 分隔线/边框，绿调浅灰 |
| `--t1`   | `#192418` | 主文字色，近黑深绿    |
| `--t2`   | `#4a604a` | 次文字色，中绿灰      |
| `--t3`   | `#87a080` | 辅助/说明文字，浅绿灰 |

### 强调系列

| 变量     | Hex 值    | 角色说明                             |
| -------- | --------- | ------------------------------------ |
| `--ac`   | `#1d7040` | 主强调色（链接、hover 激活态）       |
| `--ac-l` | `#e8f5ee` | 强调浅色背景（hover fill、tag 背景） |
| `--ac-b` | `#c8e8d5` | 强调边框色（hover 边框）             |

### 状态色系列

| 变量   | Hex 值    | 变量（浅） | Hex 值（浅） | 语义             |
| ------ | --------- | ---------- | ------------ | ---------------- |
| `--ok` | `#16a34a` | `--okl`    | `#dcfce7`    | 成功/正常/启用   |
| `--wn` | `#d97706` | `--wnl`    | `#fef3c7`    | 警告/进行中      |
| `--er` | `#dc2626` | `--erl`    | `#fee2e2`    | 错误/失败/禁用   |
| `--in` | `#2563eb` | `--inl`    | `#dbeafe`    | 信息/待审核/蓝色 |
| `--pr` | `#7c3aed` | `--prl`    | `#ede9fe`    | 特殊/紫色        |

### 阴影

| 变量    | 值                                                             | 用途                               |
| ------- | -------------------------------------------------------------- | ---------------------------------- |
| `--sh`  | `0 1px 3px rgba(25,36,24,0.06), 0 1px 2px rgba(25,36,24,0.04)` | 卡片默认阴影（轻）                 |
| `--shm` | `0 4px 18px rgba(25,36,24,0.09)`                               | 卡片 hover 阴影（中）              |
| topnav  | `0 2px 20px rgba(0,0,0,0.22)`                                  | 顶导 sticky 阴影（写死，不用变量） |

### 其他变量

| 变量     | 值                             | 说明                          |
| -------- | ------------------------------ | ----------------------------- |
| `--r`    | `8px`                          | 基础圆角半径                  |
| `--ease` | `cubic-bezier(0.4, 0, 0.2, 1)` | 标准缓动曲线（Material ease） |
| `--tn-h` | `60px`                         | 顶导高度                      |
| `--ir-w` | `52px`                         | 图标侧轨宽度                  |

---

## 3. Typography Rules

### 字体系统

```css
@import url('https://fonts.googleapis.com/css2?family=Libre+Baskerville:wght@400;700&family=Fira+Sans:wght@300;400;500;600&family=JetBrains+Mono:wght@400;500&display=swap');
```

| 字体族              | 分类       | 用途                             |
| ------------------- | ---------- | -------------------------------- |
| `Libre Baskerville` | serif      | 标题、卡片头、页面大标题         |
| `Fira Sans`         | sans-serif | 正文、导航、按钮、表格、全局默认 |
| `JetBrains Mono`    | monospace  | 数字值、版本号、日期、代码       |

### 字号层级表

| 角色                     | 字体              | 字号   | 字重    | 其他特征                                               |
| ------------------------ | ----------------- | ------ | ------- | ------------------------------------------------------ |
| 页面大标题 `.ph-title`   | Libre Baskerville | 27px   | 700     | `letter-spacing: -0.025em`, `line-height: 1.2`         |
| Logo 名 `.tn-name`       | Libre Baskerville | 18px   | 400     | `letter-spacing: 0.01em`, color `#fff`                 |
| 卡片标题 `.ch-t`         | Libre Baskerville | 14px   | 400     | color `--t1`                                           |
| 导航项 `.tn-link`        | Fira Sans         | 13.5px | 500     | `white-space: nowrap`                                  |
| 正文/表格单元格 `.dt td` | Fira Sans         | 13px   | 400     | color `--t1`                                           |
| 快捷按钮 `.q-btn`        | Fira Sans         | 13px   | 500     | color `--t2`                                           |
| 面包屑 `.bc`             | Fira Sans         | 13px   | 400/500 | 当前页 500                                             |
| 页面副标题 `.ph-sub`     | Fira Sans         | 13px   | 400     | color `--t3`                                           |
| 动态流标题 `.fi-title`   | Fira Sans         | 13px   | 400     | color `--t1`                                           |
| 徽标 `.tag`              | Fira Sans         | 11px   | 500     | `border-radius: 10px`                                  |
| 日期徽标 `.dt-badge`     | JetBrains Mono    | 11.5px | 400     | border 包边                                            |
| 表格时间 `.mono`         | JetBrains Mono    | 11.5px | 400     | color `--t3`                                           |
| 动态流元信息 `.fi-meta`  | Fira Sans         | 11.5px | 400     | color `--t3`                                           |
| 列表表头 `.dt th`        | Fira Sans         | 10.5px | 600     | `text-transform: uppercase`, `letter-spacing: 0.065em` |
| mega 列标题 `.mc-title`  | Fira Sans         | 10.5px | 600     | `text-transform: uppercase`, `letter-spacing: 0.1em`   |
| 快捷入口标题 `.mq-title` | Fira Sans         | 10.5px | 600     | `text-transform: uppercase`                            |
| 统计卡标签 `.sc-l`       | Fira Sans         | 11px   | 600     | `text-transform: uppercase`, `letter-spacing: 0.07em`  |
| 统计卡数值 `.sc-v`       | JetBrains Mono    | 26px   | 500     | `letter-spacing: -0.03em`                              |
| 版本号 `.tn-ver`         | JetBrains Mono    | 9px    | 400     | `opacity: 0.65`, color `--tn-t`                        |

### 排版原则

- 标题（页面级）用 Libre Baskerville serif，赋予权威感与仪式感
- 正文全部 Fira Sans，低字重（400/500）保持阅读舒适度
- 所有数字型数据（统计值、时间戳、版本号）用 JetBrains Mono，保持对齐整洁
- 表头和分类标签统一 `text-transform: uppercase` + 加大 `letter-spacing`，形成视觉层次

---

## 4. Component Stylings

### 布局组件

#### TopNav（`.topnav`）

```
height: 60px (--tn-h)
background: #1a3328 (--tn)
display: flex; align-items: center
padding: 0 18px 0 14px
position: sticky; top: 0; z-index: 100
box-shadow: 0 2px 20px rgba(0,0,0,0.22)
```

#### Logo 区（`.tn-logo`）

```
display: flex; align-items: center; gap: 10px
padding-right: 18px
border-right: 1px solid #224538 (--tn-b)
margin-right: 6px; flex-shrink: 0
```

Logo 标记（`.tn-mark`）：`width: 32px; height: 32px; background: #4ec685; border-radius: 8px`

#### Nav Item（`.tn-item` / `.tn-link`）

| 状态   | background               | color     | border-bottom           |
| ------ | ------------------------ | --------- | ----------------------- |
| 默认   | transparent              | `#74b898` | `2px solid transparent` |
| hover  | `rgba(255,255,255,0.04)` | `#e0f5ea` | transparent             |
| active | transparent              | `#4ec685` | `2px solid #4ec685`     |
| open   | `rgba(255,255,255,0.05)` | `#e0f5ea` | transparent             |

```
padding: 0 13px; height: 100%
font-size: 13.5px; font-weight: 500
transition: color 0.15s var(--ease), background 0.15s var(--ease)
```

展开箭头 `.chv`：`transition: transform 0.2s var(--ease)`，open 时 `rotate(180deg)`

#### Mega Menu（`.mega`）

```
position: fixed; left: 0; right: 0; top: 60px
background: #fff
border-top: 2px solid #4ec685 (--tn-ac)
box-shadow: 0 10px 40px rgba(0,0,0,0.12)
padding: 22px 28px 24px
z-index: 200
display: none → flex（open 时）
gap: 0; flex-wrap: nowrap
```

#### Mega Menu Column（`.mega-col` / `.mc-title` / `.mc-item`）

`.mega-col`：`min-width: 150px; padding: 0 24px 0 0; margin: 0 0 14px`

`.mega-col-sep`（列分隔）：`width: 1px; background: #e0e6da; margin: 0 10px 14px`

`.mc-title`：
```
font-size: 10.5px; font-weight: 600
text-transform: uppercase; letter-spacing: 0.1em
color: #87a080; margin-bottom: 9px
padding-bottom: 7px; border-bottom: 1px solid #e0e6da
```

`.mc-item`：
```
display: flex; align-items: center; gap: 7px
padding: 5px 8px; border-radius: 5px
color: #4a604a; font-size: 13px
transition: background 0.12s, color 0.12s
::before dot: 4px circle, background #e0e6da
hover: background #e8f5ee; color #1d7040; dot → #1d7040
```

#### Quick Links Panel（`.mega-quick` / `.mq-item`）

`.mega-quick`：`width: 210px; flex-shrink: 0; border-left: 1px solid #e0e6da; padding-left: 22px`

`.mq-item`：`padding: 9px 10px; border-radius: 7px; margin-bottom: 4px; hover: background #e8f5ee`

`.mq-icon`：`width: 32px; height: 32px; border-radius: 7px; font-size: 15px`

#### Mega Overlay（`.mega-ov`）

```
position: fixed; inset: 0; top: 60px
background: rgba(0,0,0,0.06); z-index: 150
display: none → block（.show）
```

#### Action Buttons（`.tn-btn`）

```
width: 34px; height: 34px; border-radius: 8px
border: none; background: rgba(255,255,255,0.06)
color: #74b898
transition: background 0.15s, color 0.15s
hover: background rgba(255,255,255,0.12); color #e0f5ea
```

通知红点（`.tn-dot`）：`position: absolute; top: 5px; right: 5px; width: 6px; height: 6px; background: #f05a28; border: 1.5px solid #1a3328; border-radius: 50%`

#### User Area（`.tn-user` / `.tn-av`）

`.tn-user`：`padding: 4px 10px 4px 8px; border-radius: 8px; color: #74b898; hover: background rgba(255,255,255,0.07)`

`.tn-av`：`width: 28px; height: 28px; border-radius: 50%; background: linear-gradient(135deg, #4ec685 0%, #a8e6c8 100%); font-size: 11px; font-weight: 700; color: #1a3328`

#### Icon Rail（`.icon-rail` / `.ir-btn` / `.ir-tip`）

`.icon-rail`：
```
width: 52px (--ir-w)
background: #fff
border-right: 1px solid #e0e6da
display: flex; flex-direction: column; align-items: center
padding: 14px 0; gap: 4px; flex-shrink: 0
```

`.ir-btn`：
```
width: 36px; height: 36px; border-radius: 8px
color: #87a080 (--t3)
background: transparent; border: none
transition: background 0.14s, color 0.14s
hover / .on: background #e8f5ee; color #1d7040
```

`.ir-tip`（tooltip）：
```
position: absolute; left: 44px; top: 50%; transform: translateY(-50%)
background: #111; color: #fff; font-size: 11px
padding: 3px 8px; border-radius: 4px; white-space: nowrap
opacity: 0 → 1（ir-btn:hover）; z-index: 300
```

`.ir-sep`：`width: 24px; height: 1px; background: #e0e6da; margin: 4px 0`

#### Sub Header（`.sub-hd` / `.bc`）

`.sub-hd`：
```
background: #fff
border-bottom: 1px solid #e0e6da
padding: 0 24px; height: 44px
display: flex; align-items: center; gap: 6px; flex-shrink: 0
```

`.bc`：`display: flex; align-items: center; gap: 5px; font-size: 13px`

`.bc-s`：`color: #87a080`  `.bc-cur`：`color: #192418; font-weight: 500`  `.bc-sep`：`color: #87a080; font-size: 11px`

#### Content Area（`.content`）

```
flex: 1; overflow-y: auto; padding: 24px
scrollbar: width 6px; thumb: #c8d4c8; border-radius: 3px
```

---

### 内容组件

#### Stats Card（`.sc`）

```
background: #fff; border: 1px solid #e0e6da
border-radius: 8px; padding: 18px 20px; box-shadow: var(--sh)
position: relative; overflow: hidden
transition: box-shadow 0.18s var(--ease), transform 0.18s var(--ease)
hover: box-shadow var(--shm); transform: translateY(-2px)
::after 顶色条: height 3px（各子项颜色不同）
  :nth-child(1) → #1d7040 (--ac)
  :nth-child(2) → #2563eb (--in)
  :nth-child(3) → #d97706 (--wn)
  :nth-child(4) → #7c3aed (--pr)
```

`.sc-l`：`font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.07em; color: #87a080; margin-bottom: 9px`

`.sc-v`：`font-family: JetBrains Mono; font-size: 26px; font-weight: 500; letter-spacing: -0.03em; margin-bottom: 6px`

`.sc-t`：`font-size: 12px; .up → color #16a34a; .dn → color #dc2626`

#### Data Table（`.dt`）

```
width: 100%; border-collapse: collapse
th: font-size 10.5px; font-weight 600; text-transform uppercase
    letter-spacing 0.065em; color #87a080; padding: 0 16px 10px
    border-bottom: 1px solid #e0e6da
td: padding 10px 16px; font-size 13px; color #192418
    border-bottom: 1px solid #e0e6da
tr:last-child td: border-bottom none
tr:hover td: background #f9fbf9
```

#### Tag / Badge（`.tag`）

```
display: inline-flex; align-items: center; gap: 4px
font-size: 11px; font-weight: 500
padding: 2px 7px; border-radius: 10px
::before: width 5px; height 5px; border-radius 50%（状态点）
```

| class     | background | color     | dot       |
| --------- | ---------- | --------- | --------- |
| `.tag.ok` | `#dcfce7`  | `#16a34a` | `#16a34a` |
| `.tag.wn` | `#fef3c7`  | `#d97706` | `#d97706` |
| `.tag.er` | `#fee2e2`  | `#dc2626` | `#dc2626` |
| `.tag.in` | `#dbeafe`  | `#2563eb` | `#2563eb` |

#### Feed Item（`.fi`）

```
display: flex; gap: 11px; padding: 12px 16px
border-bottom: 1px solid #e0e6da; align-items: flex-start
transition: background 0.12s; hover: background #fafcf9

.fi-dot: width 8px; height 8px; border-radius 50%; margin-top 5px
  .g → #16a34a  .a → #d97706  .r → #dc2626  .b → #2563eb
.fi-title: font-size 13px; color #192418; margin-bottom 2px
.fi-meta: font-size 11.5px; color #87a080
```

#### Quick Action Button（`.q-btn`）

```
display: flex; align-items: center; gap: 8px
padding: 9px 16px; border-radius: 7px
border: 1px solid #e0e6da; background: #fff
font-family: Fira Sans; font-size: 13px; color: #4a604a; font-weight: 500
transition: background 0.14s, border-color 0.14s, color 0.14s
hover: background #e8f5ee; border-color #c8e8d5; color #1d7040
```

#### Card（`.card` / `.ch`）

`.card`：
```
background: #fff; border: 1px solid #e0e6da
border-radius: 8px; box-shadow: var(--sh); overflow: hidden
```

`.ch`（卡片头）：`padding: 14px 18px; border-bottom: 1px solid #e0e6da; display: flex; align-items: center; justify-content: space-between`

`.ch-t`：`font-family: Libre Baskerville; font-size: 14px; color: #192418`

`.ch-a`（卡片操作链接）：`font-size: 12px; color: #1d7040; cursor: pointer; hover: text-decoration underline`

#### Page Header（`.ph` / `.ph-title` / `.ph-sub`）

```
.ph: display flex; align-items flex-end; justify-content space-between
     margin-bottom: 22px
.ph-title: font-family Libre Baskerville; font-size 27px; font-weight 700
           color #192418; letter-spacing -0.025em; line-height 1.2; margin-bottom 3px
.ph-sub: font-size 13px; color #87a080
```

#### Date Badge（`.dt-badge`）

```
font-family: JetBrains Mono; font-size: 11.5px; color: #87a080
background: #f4f5f1; border: 1px solid #e0e6da
padding: 5px 11px; border-radius: 6px
```

---

## 5. Layout Principles

### 完整布局结构

```
┌─────────────────────────────────────────────────────────┐
│  .topnav  [sticky, z:100, h:60px, bg:#1a3328]           │
│  Logo │ Nav Items (+ Mega Menu, z:200) │ Actions+User   │
└─────────────────────────────────────────────────────────┘
┌────────┬────────────────────────────────────────────────┐
│        │  .main-wrap                                    │
│  .icon │  ┌──────────────────────────────────────────┐  │
│  -rail │  │  .sub-hd  [h:44px, bg:#fff, border-b]   │  │
│  w:52px│  ├──────────────────────────────────────────┤  │
│  bg:#fff│  │  .content  [flex:1, overflow-y:auto]    │  │
│ border-r│  │  padding: 24px                          │  │
│        │  │  .ph (page header, mb:22px)             │  │
│        │  │  .quick-row (mb:20px)                   │  │
│        │  │  .stats (4-col grid, gap:14px, mb:20px) │  │
│        │  │  .cg (2-col grid: 1fr 310px, gap:14px)  │  │
│        │  └──────────────────────────────────────────┘  │
└────────┴────────────────────────────────────────────────┘

.body-wrap: display flex; height: calc(100vh - 60px)
```

### 尺寸系统

| 尺寸               | 值          | 作用             |
| ------------------ | ----------- | ---------------- |
| `--tn-h`           | `60px`      | 顶导高度         |
| `--ir-w`           | `52px`      | 图标侧轨宽度     |
| sub-hd             | `44px`      | 面包屑栏高度     |
| content padding    | `24px`      | 内容区四周内边距 |
| card border-radius | `8px (--r)` | 卡片圆角         |

### 间距系统（从 HTML 中提取实际值）

| 场景                 | 值                                         |
| -------------------- | ------------------------------------------ |
| 统计卡片 grid gap    | `14px`                                     |
| 内容双列 grid gap    | `14px`                                     |
| 快捷操作行 gap       | `10px`                                     |
| 卡片 padding         | `14px 18px`（头部）/ `18px 20px`（统计卡） |
| 页头 margin-bottom   | `22px`                                     |
| 快捷行 margin-bottom | `20px`                                     |
| 统计行 margin-bottom | `20px`                                     |
| 表格 td padding      | `10px 16px`                                |
| Feed item padding    | `12px 16px`                                |
| quick-btn padding    | `9px 16px`                                 |
| mc-item padding      | `5px 8px`                                  |
| mq-item padding      | `9px 10px`                                 |
| mega padding         | `22px 28px 24px`                           |

### 圆角体系

| 场景                           | 值               |
| ------------------------------ | ---------------- |
| `--r` 基础（卡片、stats card） | `8px`            |
| Logo 标记 `.tn-mark`           | `8px`            |
| 按钮类 `.tn-btn`, `.ir-btn`    | `8px`            |
| 用户区 `.tn-user`              | `8px`            |
| `.mq-icon`                     | `7px`            |
| `.mq-item`                     | `7px`            |
| `.q-btn`                       | `7px`            |
| 日期徽标 `.dt-badge`           | `6px`            |
| 列菜单项 `.mc-item`            | `5px`            |
| Tag `.tag`                     | `10px`（椭圆型） |
| 头像 `.tn-av`                  | `50%`（圆形）    |
| Scrollbar thumb                | `3px`            |
| `.ir-tip` tooltip              | `4px`            |

### Grid 使用

- 统计卡：`grid-template-columns: repeat(4, 1fr); gap: 14px`
- 内容双列：`grid-template-columns: 1fr 310px; gap: 14px`

---

## 6. Depth & Elevation

### z-index 层级

| 层             | z-index | 元素       |
| -------------- | ------- | ---------- |
| topnav         | 100     | `.topnav`  |
| mega overlay   | 150     | `.mega-ov` |
| mega menu      | 200     | `.mega`    |
| ir-tip tooltip | 300     | `.ir-tip`  |

### 阴影层级

| 层级        | 值                                                                           | 触发场景         |
| ----------- | ---------------------------------------------------------------------------- | ---------------- |
| 轻（默认）  | `var(--sh)` = `0 1px 3px rgba(25,36,24,0.06), 0 1px 2px rgba(25,36,24,0.04)` | 卡片静止态       |
| 中（hover） | `var(--shm)` = `0 4px 18px rgba(25,36,24,0.09)`                              | stats card hover |
| 重（顶导）  | `0 2px 20px rgba(0,0,0,0.22)`                                                | topnav sticky    |
| mega 弹出   | `0 10px 40px rgba(0,0,0,0.12)`                                               | mega menu 展开   |

### Sticky 阴影行为

顶导使用 `position: sticky; top: 0` 配合 `box-shadow: 0 2px 20px rgba(0,0,0,0.22)`，滚动时始终压住内容产生层次感。

---

## 7. Do's and Don'ts

### Do's

1. 所有卡片统一使用 `.card` 类（`#fff` + `border: 1px solid #e0e6da` + `border-radius: 8px` + `box-shadow: var(--sh)`）
2. 数字型数据（统计值、时间、版本号）一律使用 `JetBrains Mono`
3. 页面大标题使用 `Libre Baskerville` 27px，卡片标题使用同字族 14px
4. 状态标签使用 `.tag.ok/.wn/.er/.in` 四态，绝不自创新状态色
5. 强调色交互（hover fill）统一使用 `--ac-l` (`#e8f5ee`) + `--ac-b` border
6. 表头使用 `text-transform: uppercase` + `letter-spacing: 0.065em` 保持统一
7. 所有过渡动画使用 `var(--ease)` 缓动曲线
8. Icon Rail 的 `.ir-btn` 激活态使用 `.on` class，与 hover 样式相同
9. 面包屑当前页用 `.bc-cur`（500 字重），父级用 `.bc-s`（t3 色）
10. 顶导分隔线统一使用 `--tn-b` 颜色

### Don'ts

1. 不要在顶导区域使用白色背景或任何亮色背景
2. 不要在内容区使用纯黑文字——t1 是 `#192418` 而非 `#000`
3. 不要创建新的 CSS 变量来替换 `--bd`、`--bg` 等已有变量
4. 不要在卡片头部（`.ch`）之外使用粗字重标题
5. 不要将 `.tag` 用于超过 4 字的文本
6. 不要给 `.stats .sc` 的顶色条使用非系统色（必须取自状态色或强调色）
7. 不要在 mega menu 之外使用 `position: fixed` 弹出层（除非 z-index 遵守层级规则）
8. 不要省略 `transition` 属性——所有交互元素必须有过渡
9. 不要在图标侧轨（`.icon-rail`）中放文字标签

---

## 8. Responsive Behavior

本设计面向管理后台，**最小支持宽度 1200px**，不设移动端响应。

| 行为           | 规则                                                    |
| -------------- | ------------------------------------------------------- |
| 最小宽度       | 1200px（管理后台），不考虑移动适配                      |
| 顶导 sticky    | `position: sticky; top: 0`，滚动时始终固定在顶部        |
| body-wrap 高度 | `height: calc(100vh - 60px)`，随视口变化                |
| Mega Menu      | `position: fixed; left: 0; right: 0`，始终全宽展开      |
| Icon Rail      | `width: 52px; flex-shrink: 0`，固定宽度，不随内容区变化 |
| 内容区滚动     | `.content { overflow-y: auto }`，只有内容区内部滚动     |
| 统计卡片 grid  | `repeat(4, 1fr)`，内容区宽度决定每列宽度，不响应式折行  |
| 双列内容 grid  | `1fr 310px`，右侧动态流固定 310px                       |

---

## 9. Agent Prompt Guide

以下 prompt 模板可直接用于 AI 生成符合本设计系统的组件代码。所有模板使用本文档的 `:root` 变量，不引入新变量。

### Prompt 1：重现完整 TopNav

```
使用 Boreal Admin 设计系统重现顶部导航栏（.topnav），要求：
- CSS 变量：--tn=#1a3328，--tn-b=#224538，--tn-t=#74b898，--tn-th=#e0f5ea，--tn-ac=#4ec685
- height: 60px，position: sticky, top: 0, z-index: 100
- box-shadow: 0 2px 20px rgba(0,0,0,0.22)
- 左侧 Logo（.tn-logo）：32px 薄荷绿圆角标记 + Libre Baskerville 18px 白色品牌名 + JetBrains Mono 9px 版本号
- 中部 Nav（.tn-nav .tn-item .tn-link）：13.5px 500字重，颜色 --tn-t，hover --tn-th，active 底线 --tn-ac 2px
- 每个带子菜单的 nav item 点击展开 .mega（fixed全宽，top:60px，border-top:2px solid --tn-ac，z-index:200）
- Mega 内含 .mega-cols（多列，.mega-col/.mc-title/.mc-item）+ .mega-quick（width:210px，右侧快捷入口）
- 遮罩层 .mega-ov（fixed，rgba(0,0,0,0.06)，z-index:150）
- 右侧 .tn-acts：搜索/通知/帮助按钮（.tn-btn，34px圆角方形）+ 分隔线 + 用户头像+名（.tn-user/.tn-av）
- 字体：Libre Baskerville + Fira Sans + JetBrains Mono，从 Google Fonts 引入
```

### Prompt 2：重现 Icon Rail

```
使用 Boreal Admin 设计系统重现图标侧轨（.icon-rail），要求：
- width: 52px（--ir-w），background: #fff，border-right: 1px solid #e0e6da
- flex-direction: column，align-items: center，padding: 14px 0，gap: 4px
- 按钮 .ir-btn：36px×36px，border-radius: 8px，颜色 #87a080（--t3）
  hover 和 .on 状态：background #e8f5ee（--ac-l），color #1d7040（--ac）
- Tooltip .ir-tip：position:absolute left:44px，background:#111，font-size:11px，
  padding:3px 8px，border-radius:4px，hover时opacity从0到1，z-index:300
- 分隔线 .ir-sep：24px wide，1px high，background #e0e6da，margin:4px 0
- 包含：工作台/用户中心/商品管理/订单管理 + 分隔线 + 数据报表/系统设置 共 6 个图标
```

### Prompt 3：重现 Stats Cards（4列）

```
使用 Boreal Admin 设计系统重现统计卡片行（.stats），要求：
- 容器 .stats：grid-template-columns: repeat(4, 1fr)，gap: 14px，margin-bottom: 20px
- 每个卡片 .sc：background #fff，border: 1px solid #e0e6da，border-radius: 8px
  padding: 18px 20px，box-shadow: 0 1px 3px rgba(25,36,24,0.06), 0 1px 2px rgba(25,36,24,0.04)
  hover: box-shadow 0 4px 18px rgba(25,36,24,0.09)，transform: translateY(-2px)
  ::after 顶色条（height:3px）：第1张=#1d7040，第2张=#2563eb，第3张=#d97706，第4张=#7c3aed
- 卡片内部：
  .sc-l：11px uppercase 600 letter-spacing:0.07em color:#87a080
  .sc-v：JetBrains Mono 26px 500 letter-spacing:-0.03em
  .sc-t：12px，.up → color #16a34a，.dn → color #dc2626
- 入场动画 fadeUp：opacity 0→1，translateY 5px→0，0.38s ease，各子项 delay 0.05s 递增
- 数据示例：今日PV 12,847 / 活跃用户 3,291 / 今日订单额 ¥86,420 / 待处理工单 47
```

### Prompt 4：重现 Data Table + Tag

```
使用 Boreal Admin 设计系统重现数据表格（.card + .dt + .tag），要求：
- 外层 .card：#fff，border: 1px solid #e0e6da，border-radius: 8px，overflow: hidden
- 卡片头 .ch：padding: 14px 18px，border-bottom: 1px solid #e0e6da，flex space-between
  .ch-t：Libre Baskerville 14px color #192418
  .ch-a：12px color #1d7040，hover underline
- 表格 .dt：width:100%，border-collapse: collapse
  th：10.5px 600 uppercase letter-spacing:0.065em color:#87a080 padding:0 16px 10px border-bottom
  td：13px color #192418 padding:10px 16px border-bottom: 1px solid #e0e6da
  tr:last-child td: border-bottom none
  tr:hover td: background #f9fbf9
- 数字列使用 .mono（JetBrains Mono 11.5px color #87a080）
- Tag：.tag.ok（#dcfce7/#16a34a）/.tag.wn（#fef3c7/#d97706）/.tag.er（#fee2e2/#dc2626）/.tag.in（#dbeafe/#2563eb）
  padding: 2px 7px，border-radius: 10px，::before 5px 圆点
```

### Prompt 5：重现状态 Tag（ok/wn/er/in）

```
使用 Boreal Admin 设计系统重现状态徽标（.tag），要求：
- 基础样式：display inline-flex，align-items center，gap 4px
  font-size 11px，font-weight 500，padding 2px 7px，border-radius 10px
  ::before：width 5px，height 5px，border-radius 50%（状态圆点）
- 四种状态：
  .tag.ok：background #dcfce7，color #16a34a，dot #16a34a（成功/正常/启用）
  .tag.wn：background #fef3c7，color #d97706，dot #d97706（警告/进行中）
  .tag.er：background #fee2e2，color #dc2626，dot #dc2626（错误/失败/禁用）
  .tag.in：background #dbeafe，color #2563eb，dot #2563eb（信息/待审核）
- 不要拉伸宽度，应为内容自适应宽度（inline-flex）
- 可组合使用（同一行多个 tag）
```
