# XCode 协作指令

本指令面向新生命团队（NewLife）开源数据中间件 XCode，帮助 Copilot 在 .NET 项目中正确使用 XCode 进行数据建模和实体操作。

---

## 1. XCode 定位与边界

### 1.1 技术栈定位

```
NewLife.Core（基础库）
       ↓
   NewLife.XCode（数据中间件）← 本指令
       ↓
   NewLife.Cube（Web 快速开发框架）→ cube.instructions.md
```

### 1.2 职责边界

| 层级 | 职责 | 指令文件 |
|------|------|---------|
| **NewLife.Core** | 基础扩展、日志、缓存、网络等 | `copilot-instructions.md` |
| **NewLife.XCode** | 数据建模、ORM、实体增删改查、数据库操作 | 本指令 |
| **NewLife.Cube** | Web 管理后台、控制器、视图、权限 | `cube.instructions.md` |

### 1.3 本指令覆盖范围

**包含**：
- 数据模型设计（Model.xml）
- 实体类生成与使用
- 数据库 CRUD 操作
- 项目初始化与 XCode 接入

**不包含**（由 cube.instructions.md 负责）：
- Web 控制器逻辑
- 视图与前端交互
- 权限管理配置
- 魔方区域深度定制

---

## 2. 使用场景与快速入口

### 2.1 场景一：已有项目接入 XCode

1. 引入 NuGet 包：`dotnet add package NewLife.XCode`
2. 创建或编辑 `Model.xml` 进行数据建模
3. 执行 `xcode` 命令生成实体类
4. 在业务代码中使用实体类进行数据操作

### 2.2 场景二：从零创建新项目

1. 安装模板：`dotnet new install NewLife.Templates`
2. 创建数据类库：`dotnet new xcode -n {系统名}.Data`
3. 创建应用项目：
   - Web 应用：`dotnet new cube -n {系统名}Web`
   - 控制台应用：`dotnet new nconsole -n {系统名}App`
4. 编辑 `Model.xml` 进行数据建模
5. 执行 `xcode` 生成实体类

### 2.3 核心工作流

```
理解业务需求 → 数据建模(Model.xml) → 生成实体类(xcode) → 编写业务代码
```

**关键点**：无论哪种场景，核心都是 **充分理解业务需求，在 Model.xml 中设计合理的数据表结构**。

---

## 3. 环境准备

### 3.1 前置要求

```powershell
# 检查 .NET SDK（要求 8.0+）
dotnet --version

# 安装全局工具 xcodetool
dotnet tool install xcodetool -g
```

### 3.2 模板版本检查

NewLife.Templates 模板会持续更新，使用前需检查版本：

```powershell
# 查看模板详情
dotnet new details NewLife.Templates
```

**判定规则**：输出包含类似 `包版本: 1.1.2025.820-beta1836`
- 其中 `1.1.2025.820` 表示 v1.1，发布日期为 2025-08-20
- **要求**：发布日期需 > 2025-08-01（即严格晚于 2025-08-01）

```powershell
# 若未安装或版本过旧，执行安装/更新
dotnet new install NewLife.Templates
```

> **强约束**：仅当未安装或发布日期 ≤ 2025-08-01 时才执行安装；否则保持现状，避免无谓变更。

### 3.3 模板说明

| 模板 | 命令 | 用途 |
|------|------|------|
| `xcode` | `dotnet new xcode -n Name.Data` | 数据类库项目 |
| `cube` | `dotnet new cube -n NameWeb` | Web 管理后台 |
| `nconsole` | `dotnet new nconsole -n NameApp` | 控制台应用 |

---

## 4. Model.xml 完整参考

### 4.1 文件结构

```xml
<?xml version="1.0" encoding="utf-8"?>
<EntityModel xmlns:xs="http://www.w3.org/2001/XMLSchema-instance" 
             xs:schemaLocation="https://newlifex.com https://newlifex.com/Model202509.xsd" 
             xmlns="https://newlifex.com/Model202509.xsd">
  <Option>
    <!-- 全局配置 -->
  </Option>
  <Tables>
    <Table>
      <Columns>
        <Column />
      </Columns>
      <Indexes>
        <Index />
      </Indexes>
    </Table>
  </Tables>
</EntityModel>
```

### 4.2 Option 配置项

| 配置项 | 说明 | 示例 |
|--------|------|------|
| `Namespace` | 命名空间 | `Zero.Data` |
| `ConnName` | 数据库连接名 | `Zero` |
| `Output` | 实体类输出目录 | `.\` |
| `BaseClass` | 实体基类 | `Entity` |
| `ChineseFileName` | 使用中文文件名 | `True` |
| `Nullable` | 生成可空引用类型 | `True` |
| `HasIModel` | 实现 IModel 接口 | `True` |
| `ModelClass` | 模型类模板 | `{name}Model` |
| `ModelsOutput` | 模型类输出目录 | `.\Models\` |
| `ModelInterface` | 模型接口模板 | `I{name}` |
| `InterfacesOutput` | 接口输出目录 | `.\Interfaces\` |
| `NameFormat` | 命名格式 | `Default`/`Upper`/`Lower`/`Underline` |
| `DisplayName` | 魔方区域显示名 | `订单管理` |
| `CubeOutput` | 魔方控制器输出目录 | `../../Web/Areas/Order` |

### 4.3 Table 属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `Name` | 实体类名 | `User` |
| `TableName` | 数据库表名（可选，默认同 Name） | `sys_user` |
| `Description` | 表说明 | `用户。用户账号信息` |
| `ConnName` | 独立连接名（覆盖全局） | `Log` |
| `BaseType` | 基类（支持实体继承） | `EntityBase` |
| `InsertOnly` | 仅插入模式（日志表优化） | `True` |
| `IsView` | 视图标识 | `True` |

### 4.4 Column 属性完整参考

#### 基础属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `Name` | 属性名 | `UserName` |
| `ColumnName` | 数据库列名（可选） | `user_name` |
| `DataType` | 数据类型 | `Int32`/`Int64`/`String`/`DateTime`/`Boolean`/`Double`/`Decimal` |
| `Description` | 字段说明 | `用户名。登录账号` |
| `Length` | 字符串长度 | `50`/`200`/`-1`（大文本） |
| `Precision` | 数值精度 | `18` |
| `Scale` | 小数位数 | `2` |

#### 主键与标识

| 属性 | 说明 | 示例 |
|------|------|------|
| `PrimaryKey` | 主键 | `True` |
| `Identity` | 自增标识 | `True` |
| `Master` | 主字段（业务主要字段） | `True` |

#### 约束与默认值

| 属性 | 说明 | 示例 |
|------|------|------|
| `Nullable` | 允许空 | `False` |
| `DefaultValue` | 默认值 | `0`/`''`/`getdate()` |

#### 映射关系（Map）

格式：`表名@主键@显示字段@属性名`

| 格式 | 说明 | 示例 |
|------|------|------|
| `Table@Id@Name` | 基本映射（三段） | `Role@Id@Name` |
| `Table@Id@Name@RoleName` | 指定属性名（四段） | `Role@Id@Name@RoleName` |
| `NS.Table@Id@Path@AreaPath` | 完整命名空间 | `XCode.Membership.Area@Id@Path@AreaPath` |

#### 元素类型（ItemType）

用于魔方前端渲染和数据验证：

| ItemType | 说明 |
|----------|------|
| `image` | 图片上传 |
| `file` | 文件上传 |
| `mail` | 邮箱格式 |
| `mobile` | 手机号格式 |
| `url` | URL 链接 |
| `TimeSpan` | 时间间隔（秒转可读格式） |
| `GMK` | 字节数转 GB/MB/KB |
| `html` | HTML 富文本 |
| `code` | 代码编辑器 |
| `json` | JSON 编辑器 |

#### 显示选项（ShowIn）

控制字段在魔方各区域的显示，支持三种语法：

**语法一：具名列表（推荐）**
```
ShowIn="List,Search"          # List和Search显示
ShowIn="-EditForm,-Detail"    # 编辑表单和详情隐藏
ShowIn="All,-Detail"          # 全部显示，详情隐藏
ShowIn="None,Search,Add"      # 全部隐藏，搜索和添加显示
```

区域别名：`List(L)`、`Detail(D)`、`AddForm(Add/A)`、`EditForm(Edit/E)`、`Search(S)`、`Form(F)`（同时控制 Add 和 Edit）

**语法二：管道分隔**
```
ShowIn="Y|Y|N||A"   # List=显示|Detail=显示|Add=隐藏|Edit=自动|Search=自动
```

**语法三：5字符掩码**
```
ShowIn="11110"      # 1=显示, 0=隐藏, A/?/-=自动
```

#### 分表字段（DataScale）

| 值 | 说明 |
|----|------|
| `time` | 大数据单表的时间字段（用于雪花 ID） |
| `timeShard:yyMMdd` | 分表字段，按日期格式分表 |

#### 其他属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `Type` | 枚举类型 | `XCode.Membership.SexKinds` |
| `Category` | 表单分组 | `登录信息`/`扩展` |
| `Attribute` | 额外特性 | `XmlIgnore, IgnoreDataMember` |
| `Model` | 是否包含在模型类中 | `False` |
| `RawType` | 原始数据库类型 | `varchar(50)` |

### 4.5 Index 属性

| 属性 | 说明 | 示例 |
|------|------|------|
| `Columns` | 索引列（逗号分隔） | `Name`/`Category,CreateTime` |
| `Unique` | 唯一索引 | `True` |

### 4.6 完整示例

```xml
<?xml version="1.0" encoding="utf-8"?>
<EntityModel xmlns:xs="http://www.w3.org/2001/XMLSchema-instance" 
             xs:schemaLocation="https://newlifex.com https://newlifex.com/Model202509.xsd" 
             xmlns="https://newlifex.com/Model202509.xsd">
  <Option>
    <Namespace>Order.Data</Namespace>
    <ConnName>Order</ConnName>
    <Output>.\</Output>
    <ChineseFileName>True</ChineseFileName>
    <Nullable>True</Nullable>
    <HasIModel>True</HasIModel>
    <DisplayName>订单管理</DisplayName>
    <CubeOutput>../../OrderWeb/Areas/Order</CubeOutput>
  </Option>
  <Tables>
    <Table Name="Order" Description="订单。电商订单主表">
      <Columns>
        <Column Name="Id" DataType="Int64" PrimaryKey="True" DataScale="time" Description="编号" />
        <Column Name="OrderNo" DataType="String" Master="True" Length="50" Nullable="False" Description="订单号" />
        <Column Name="UserId" DataType="Int32" Map="User@Id@Name" Description="用户" />
        <Column Name="Status" DataType="Int32" Type="Order.Data.OrderStatus" Description="状态" />
        <Column Name="TotalAmount" DataType="Decimal" Precision="18" Scale="2" Description="总金额" />
        <Column Name="Remark" DataType="String" Length="500" Description="备注" Category="扩展" />
        <Column Name="CreateUser" DataType="String" Description="创建者" Model="False" Category="扩展" />
        <Column Name="CreateTime" DataType="DateTime" Nullable="False" Description="创建时间" Category="扩展" />
        <Column Name="UpdateTime" DataType="DateTime" Description="更新时间" Model="False" Category="扩展" />
      </Columns>
      <Indexes>
        <Index Columns="OrderNo" Unique="True" />
        <Index Columns="UserId" />
        <Index Columns="Status,CreateTime" />
      </Indexes>
    </Table>
  </Tables>
</EntityModel>
```

---

## 5. 实体类操作指南

### 5.1 基础 CRUD

```csharp
// 新增
var entity = new User { Name = "test", Password = "123456" };
entity.Insert();

// 查询单个
var user = User.FindByKey(1);
var user = User.Find(User._.Name == "test");

// 查询列表
var list = User.FindAll();
var list = User.FindAll(User._.Status == 1, User._.Id.Desc(), null, 0, 10);

// 更新
user.Name = "newName";
user.Update();

// 删除
user.Delete();

// 保存（自动判断 Insert/Update）
entity.Save();
```

### 5.2 高级查询

```csharp
// 分页查询
var page = new PageParameter { PageIndex = 1, PageSize = 20 };
var list = User.FindAll(User._.Status == 1, page);

// 条件组合
var where = new WhereExpression();
where &= User._.Status == 1;
where &= User._.CreateTime >= DateTime.Today;
if (!key.IsNullOrEmpty()) where &= User._.Name.Contains(key);
var list = User.FindAll(where, page);

// 统计
var count = User.FindCount(User._.Status == 1);

// 查询最大/最小值
var maxId = User.FindMax(User._.Id, null);
```

### 5.3 批量操作

```csharp
// 批量插入
var list = new List<User>();
list.Insert();

// 批量更新
User.Update(User._.Status == 2, User._.Status == 1);

// 批量删除
User.Delete(User._.Status == 0);
```

### 5.4 异步操作

```csharp
var user = await User.FindAsync(User._.Id == 1);
var list = await User.FindAllAsync(User._.Status == 1, page);
await entity.InsertAsync();
await entity.SaveAsync();  // 异步保存，用于日志等高频写入
```

### 5.5 缓存查询

```csharp
// 实体缓存（适用于小表）
var list = User.FindAllWithCache();

// 单对象缓存（按主键）
var user = User.FindByKeyWithCache(1);
```

---

## 6. 多模块项目结构

对于复杂业务系统，建议按模块组织：

```
Zero.Data/
├── Order/           # 订单模块
│   ├── Order.xml    # 订单模型
│   ├── 订单.cs
│   └── 订单明细.cs
├── Product/         # 商品模块
│   ├── Product.xml  # 商品模型
│   ├── 商品.cs
│   └── 分类.cs
└── Member/          # 会员模块
    ├── Member.xml   # 会员模型
    └── 会员.cs
```

每个模块目录内有独立的 `*.xml` 模型文件，在各自目录执行 `xcode` 命令生成实体类。

---

## 7. xcode 命令参考

```powershell
# 在模型文件所在目录执行（自动查找所有 *.xml）
xcode

# 指定模型文件
xcode Model.xml
xcode Order.xml
```

**执行效果**：
1. 读取 XML 模型文件
2. 生成实体类（`*.cs`）
3. 生成模型类（如配置了 `ModelClass`）
4. 生成接口（如配置了 `ModelInterface`）
5. 生成数据字典（`*.htm`）
6. 生成魔方控制器（如配置了 `CubeOutput`）

---

## 8. 常见问题

### 8.1 模型文件命名

- 默认：`Model.xml`
- 推荐：`{系统英文名}.xml` 或 `{模块名}.xml`
- 复杂项目：每个模块目录一个模型文件

### 8.2 实体类生成位置

- 实体类生成在 `xcode` 命令执行目录
- 可通过 `Output` 配置项指定输出目录
- 魔方控制器通过 `CubeOutput` 指定

### 8.3 数据库连接

在应用配置文件中配置连接字符串：

```json
{
  "ConnectionStrings": {
    "Order": "Server=.;Database=Order;Uid=sa;Pwd=xxx"
  }
}
```

连接名对应 Model.xml 中的 `ConnName`。

---

## 9. 与 Cube 的协作

当需要生成 Web 管理界面时：

1. 在 Model.xml 中配置 `CubeOutput` 指向 Web 项目的 Areas 目录
2. 配置 `DisplayName` 作为魔方区域名称
3. 执行 `xcode` 自动生成控制器
4. 深度定制请参考 `cube.instructions.md`

---

## 10. Copilot 行为指引

### 10.1 数据建模时

1. **充分理解业务**：在设计表结构前，确保理解业务场景和数据关系
2. **合理设计主键**：
   - 普通表：`Int32` 自增主键
   - 大数据表：`Int64` + `DataScale="time"`（雪花 ID）
3. **必要的索引**：为查询条件字段添加索引
4. **字段长度**：String 类型必须指定合理的 `Length`

### 10.2 生成代码时

1. 确保在正确目录执行 `xcode`
2. 生成后检查编译是否通过
3. 如需修改生成的代码，应修改 Model.xml 后重新生成

### 10.3 边界意识

- 数据模型和实体操作 → 本指令
- Web 控制器和界面 → `cube.instructions.md`
- 基础编码规范 → `copilot-instructions.md`
