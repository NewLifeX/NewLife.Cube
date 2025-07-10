# 魔方 NewLife.Cube

![GitHub top language](https://img.shields.io/github/languages/top/newlifex/newlife.cube?logo=github)
![GitHub License](https://img.shields.io/github/license/newlifex/newlife.cube?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.cube.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.cube.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.cube.core?label=dev%20nuget&logo=nuget)

魔方是一个快速Web开发平台，能够快速搭建系统原型，而又具有足够灵活的可扩展性！  
内部集成了用户权限管理、模板继承、SSO登录、OAuth服务端、数据导出与分享等多个功能模块，默认模板在真实项目中经历过单表100亿数据添删改查的考验。  

演示站点：<https://cube.newlifex.com> `CentOS7 + CDN`  
SSO中心：<https://sso.newlifex.com> `OAuth服务端`  

魔方教程：<https://newlifex.com/cube>  
XCode教程：<https://newlifex.com/xcode>  
核心库教程：<https://newlifex.com/core>  


## 快速拥有

​	使用NewLife组件的最简便方式是从Nuget引用，例如在项目Nuget管理中搜索`NewLife.Cube.Core` 并引入。

​	NewLife组件由社区共创20多年，使用MIT开源协议，**任何人可任意修改并再次发行**（无需声明来源）！许多企业基于此构建内部开发框架时，甚至可通过批量替换源码中所有`NewLife`字符串为贵公司名实现私有化定制。

​	团队始终秉承开放态度，不仅支持VisualStudio（最新正式版）打开解决方案编译，也兼容`dotnet build`命令行编译，项目文件摒弃复杂功能以追求简单易用，真正做到开箱即用。

​	我们公开强命名证书`newlife.snk`以支持独自编译替换程序集。



​	命令行中运行以下命令快速体验NewLife组件：

```
dotnet new install NewLife.Templates
dotnet new cube --name CubeWeb
dotnet new xcode --name Zero.Data
cd CubeWeb
dotnet build
start http://localhost:6080
dotnet run
```



---

### 快速部署用户中心

1. 拉取[源码](https://github.com/NewLifeX/NewLife.Cube)并编译CubeSSO项目，切换到输出目录Bin/SSO，按需修改appsettings.json配置，打包exe/dll/appsettings.json/CubeSSO.runtimeconfig.json等文件，并发布到服务器。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://*:8080;https://*:8081",
  //"StarServer": "http://star.newlifex.com:6600",
  "ConnectionStrings": {
    "Membership": "Data Source=..\\Data\\Membership.db;provider=sqlite",
    "Cube": "Data Source=..\\Data\\Cube.db;provider=sqlite",
    "Log": "Data Source=..\\Data\\Log.db;provider=sqlite"

    //"Membership": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    //"Cube": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql",
    //"Log": "Server=.;Port=3306;Database=Membership;Uid=root;Pwd=root;provider=mysql"
  }
}
```

2. 启动并访问SSO系统，首次登录可以用admin/admin进去，也可以使用第三方登录（新生命用户中心），第一个用户将会作为系统管理员，同时禁用admin。

3. 作为用户中心正式部署时，需要关闭SSO的第三方登录，魔方设置，OAuth设置，禁用所有。

   ![OAuthConfig](Doc/OAuthConfig.png)

4. 修改系统名称为新生命用户中心，魔方设置，系统设置，显示名称



---

### 第三代魔方

启动第三代魔方的设计，主要方向是借助前后端分离技术重构现代化用户界面，在2023年3月份完成第一个最小可用版（vue）。  
后端接口源码已合并到魔方代码库的`master`分支，各前端代码库独立，欢迎大家积极参与！  

Vue版：https://vue.newlifex.com, https://quickvue.newlifex.com  
Antd版：https://antd.newlifex.com  
Swagger：https://cube3.newlifex.com/swagger/index.html  

#### 项目参与须知

1. 参与者加入github上的NewLifeX团队，自由向魔方dev分支提交代码或修改文档。  
2. 用于前后端分离的WebApi版魔方后台是 `NewLife.Cube`，原 `NewLife.CubeNC` 保留MVC继续维护。  
3. 欢迎增加更多的前端项目，每一种前端新建独立代码库，如`Antd`则新建 `NewLife.CubeAntd`。  
4. 大家在文档或代码处，标注负责人。  
5. 源码库使用github，以及新生命团队糖果库（可申请权限）
6. 项目待办任务管理 https://github.com/orgs/NewLifeX/projects/1 。  

#### 目标蓝图

第三代魔方的远景目标，预计用2~3年时间完成。

1. 重构为现代化用户界面，保留魔方默认视图以及视图定制的思想，让下游项目在迁移到第三代魔方时，尽可能少修改代码
2. 前后端分离技术，支持`Vue/React/Angular/Blazor`等主流前端框架
3. 魔方理念和用法保持不变，新建WebApi项目后从Nuget引入`NewLife.Cube`，加入模型生成实体类和Controller即可得到默认皮肤的界面，需要定制时才写前端代码
4. 增强移动端支持，混合式手机APP、小程序
5. 增强支持数据大屏

#### 春雨计划

春雨计划，定于2023年3月完成第一个最小vue可用版，待办项如下（欢迎补充）：

1. [*] 在dev分支新建WebApi项目 `NewLife.Cube`，占用Asp.Net 4.5的坑位（已弃用），将来发布包也是 `NewLife.Cube`
2. [*] 专属于NetCore版的代码，转移到 `NewLife.CubeNC` 目录，尽量保留代码提交历史，方便将来查找
3. [*] 设计全新的 `EntityController`和`EntityReadonlyController`，只为前端提供接口
4. [*] 编写接口文档
5. [*] 设计vue版主页（框架页），前端项目是 `NewLife.CubeVue`，vue项目调用后端 `CubeDemo`
6. [*] 设计vue版登录页
7. [] 设计vue版用户列表页和表单页
8. [] 设计vue版角色列表页和表单页

Vue版前端代码库：  
https://github.com/NewLifeX/NewLife.CubeVue
http://git.newlifex.com/NewLife/NewLife.CubeVue

https://github.com/NewLifeX/NewLife.QuickVue
https://git.newlifex.com/NewLife/NewLife.QuickVue

### 非主线任务

支持vue之外的前端框架，不限于3月份完成。  

1. [*] 新增Blazor，项目 NewLife.CubeBlazor。 @张善友 @张炳彬
2. [*] 新建AntDesign，项目 NewLife.CubeAntd。 @Van

Antd版前端代码库：  
https://github.com/NewLifeX/NewLife.CubeAntd
http://git.newlifex.com/NewLife/NewLife.CubeAntd

Blazor版前端代码库：  
https://github.com/NewLifeX/NewLife.CubeBlazor
http://git.newlifex.com/NewLife/NewLife.CubeBlazor

### WebApi接口说明
1. 接口地址 https://cube3.newlifex.com/swagger/index.html
2. 登录地址 `/Admin/User/Login` ， 测试账号 `admin/admin`，`test/test`
3. JWT令牌传递方式：请求头 Authentication（推荐）、Cookie、Url参数token
4. 首页框架获取菜单 `/Admin/Index/GetMenuTree`
5. 每个控制器，都有一个 `/{Area}/{Controller}/GetFields` 接口，获取可用于展示的字段信息，如 https://cube3.newlifex.com/Cube/App/GetFields?kind=1，kind参数可选List/Detail/AddForm/EditForm
6. 控制器主路由对应列表页数据获取接口，调用各控制器的Search查找数据，由于查询参数多变，接口入参没有固定模型，而是直接从请求字符串中获取参数。如 https://cube3.newlifex.com/Cube/Area?parentid=0&pageSize=7
7. 列表页接口，返回数据中pager为分页信息
8. 列表页接口，返回数据中state为统计行，如用户统计 https://cube3.newlifex.com/Admin/UserStat
9. 详情接口 `/{Area}/{Controller}/Detail`，参数id固定为主键查询，如 https://cube3.newlifex.com/Cube/Area?id=450921
10. 新增接口 `/{Area}/{Controller}/Insert`，Post需要新增的实体对象
11. 修改接口 `/{Area}/{Controller}/Update`，Post需要修改的实体对象，务必带有主键
12. 删除接口 `/{Area}/{Controller}/Delete`，Get删除参数id指定的数据


---

### 魔方特性

* 通用权限管理，用户、角色、菜单、权限，支持控制器Action权限控制
* 多数据库，支持 `MySql / SQLite / Sql Server / Oracle / PostgreSql / SqlCe / Access`
* 免部署，系统自动创建数据库表结构，以及初始化数据，无需人工干涉
* 强大的视图引擎，支持子项目视图重写父项目相同位置视图，任意覆盖修改默认界面

---

### ASP.NET Core 安装

* 在 *Visual Studio* 中新建`ASP.NET Core Web`项目
* 通过 *NuGet* 引用`NewLife.Cube.Core`，或自己编译最新的[魔方 NewLife.CubeNC](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`appsettings.json`的`ConnectionStrings`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`Data Source=..\Data\Membership.db`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`MySql`/`SQLite`/`Oracle`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`，也可以使用`NewLife`等第三方OAuth登录，首个进入系统的用户抢得管理员，原`admin`禁用
* 项目发布时只需要拷贝`*.dll`、`appsettings.json`、`*.deps.json`、`*.runtimeconfig.json`，以及其它自己添加的资源文件

---

### ASP.NET MVC 安装

* 在 *Visual Studio* 中新建`ASP.NET MVC`项目
* 通过 *NuGet* 引用`NewLife.Cube`，或自己编译最新的[魔方 NewLife.Cube](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`Web.config`的`<connectionStrings>`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`\<add name="Membership" connectionString="Data Source=..\Data\Membership.db" providerName="Sqlite"/>`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`MySql`/`SQLite`/`Oracle`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`，也可以使用`NewLife`等第三方OAuth登录，首个进入系统的用户抢得管理员，原`admin`禁用
* 推荐安装 *Visual Studio* 插件 *Razor Generator*，给`.cshtml`文件设置`自定义工具`为`RazorGenerator`，可以把`.cshtml`编译生成到`DLL`里面
* 项目发布时只需要拷贝`Bin`、`web.config`、`Global.asax`，以及其它自己添加的资源文件

## 新生命项目矩阵
各项目默认支持net9.0/netstandard2.1/netstandard2.0/net4.62/net4.5，旧版（2024.0801）支持net4.0/net2.0  

|                               项目                               | 年份  | 说明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基础组件                             |       | 支撑其它中间件以及产品项目                                                                  |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心库，日志、配置、缓存、网络、序列化、APM性能追踪                                         |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大数据中间件，单表百亿级，MySql/SQLite/SqlServer/Oracle/PostgreSql/达梦，自动分表，读写分离 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 网络库，单机千万级吞吐率（2266万tps），单机百万级连接（400万Tcp长连接）                     |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 协议通信库，提供CS应用通信框架，支持Http/RPC通信框架，高吞吐，物联网设备低开销易接入        |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速开发平台，集成了用户权限、SSO登录、OAuth服务端等，单表100亿级项目验证               |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服务管理组件，把应用安装成为操作系统守护进程，Windows服务、Linux的Systemd                   |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero零代脚手架，基于NewLife组件生态的项目模板NewLife.Templates，Web、WebApi、Service        |
|                              中间件                              |       | 对接知名中间件平台                                                                          |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis客户端，微秒级延迟，百万级吞吐，丰富的消息队列，百亿级数据量项目验证                   |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ纯托管客户端，支持Apache RocketMQ和阿里云消息队列，十亿级项目验                     |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物联网消息协议，MqttClient/MqttServer，客户端支持阿里云物联网                               |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT标准库，定义物联网领域的各种通信协议标准规范                                             |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基于IoT标准库实现，支持ZeroIoT平台和IoTEdge网关            |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西门子PLC协议，基于IoT标准库实现，支持IoT平台和IoTEdge                                      |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地图组件库，封装百度地图、高德地图、腾讯地图、天地图                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音频编解码库，PCM/ADPCMA/G711A/G722U/WAV/AAC                                                |
|                             产品平台                             |       | 产品平台级，编译部署即用，个性化自定义                                                      |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星尘，分布式服务平台，节点管理、APM监控中心、配置中心、注册中心、发布中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 蚂蚁调度，分布式大数据计算平台（实时/离线），蚂蚁搬家分片思想，万亿级数据量项目验证         |
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企业ERP，产品管理、客户管理、销售管理、供应商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 码神工具，众多开发者工具，网络、串口、加解密、正则表达式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 简易文件存储，支持分布式系统中文件集中存储。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 产品级反向代理，NAT代理、Http代理                                                           |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http压力测试工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git源代码管理系统                                                                           |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式操作系统，完全独立自主，支持ARM Cortex-M芯片架构                                      |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工业计算机，物联网边缘网关，高性能.NET8主机，应用于工业、农业、交通、医疗             |
|                          FIoT物联网平台                          | 2020  | 物联网整体解决方案，建筑、环保、农业，软硬件及大数据分析一体化，单机十万级点位项目验证      |
|                        UWB高精度室内定位                         | 2020  | 厘米级（10~20cm）高精度室内定位，软硬件一体化，与其它系统联动，大型展厅项目验证             |



## 新生命开发团队
![XCode](https://newlifex.com/logo.png)  

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的80多个开源项目已被广泛应用于各行业，Nuget累计下载量高达400余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。  

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。  

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838  
微信公众号：  
![智能大石头](https://newlifex.com/Stone.jpg)  