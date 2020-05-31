## 魔方 NewLife.Cube
魔方是一个快速Web开发平台，能够快速搭建系统原型，而又具有足够灵活的可扩展性！  
内部集成了用户权限管理、模板继承、SSO登录、OAuth服务端、数据导出与分享等多个功能模块，默认模板在真实项目中经历过单表100亿数据添删改查的考验。  

演示站点：https://cube.newlifex.com  
函数计算：http://cube.feifan.link  
SSO中心：https://sso.newlifex.com  

魔方教程：https://www.yuque.com/smartstone/cube  
XCode教程：https://www.yuque.com/smartstone/xcode  
核心库教程：https://www.yuque.com/smartstone/nx  

---
### 特性
* 通用权限管理，用户、角色、菜单、权限，支持控制器Action权限控制
* 多数据库，支持 `MySql / SQLite / Sql Server / Oracle / SqlCe / Access`
* 免部署，系统自动创建数据库表结构，以及初始化数据，无需人工干涉
* 强大的视图引擎，支持子项目视图重写父项目相同位置视图，任意覆盖修改默认界面

---
### ASP.NET Core 安装
* 在 *Visual Studio* 中新建`ASP.NET Core Web`项目
* 通过 *NuGet* 引用`NewLife.Cube.Core`，或自己编译最新的[魔方](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`Web.config`的`<connectionStrings>`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`\<add name="Membership" connectionString="Data Source=~\App_Data\Membership.db" providerName="Sqlite"/>`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`SQLite`/`Oracle`/`MySql`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`
* 推荐安装 *Visual Studio* 插件 *Razor Generator*，给`.cshtml`文件设置`自定义工具``RazorGenerator`，可以把`.cshtml`编译生成到`DLL`里面
* 项目发布时只需要拷贝`Bin`、`web.config`、`Global.asax`，以及其它自己添加的资源文件

---
### ASP.NET MVC 安装
* 在 *Visual Studio* 中新建`ASP.NET MVC`项目
* 通过 *NuGet* 引用`NewLife.Cube`，或自己编译最新的[魔方](http://github.com/NewLifeX/NewLife.Cube)源码
* 在`Web.config`的`<connectionStrings>`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`\<add name="Membership" connectionString="Data Source=~\App_Data\Membership.db" providerName="Sqlite"/>`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`MySql`/`SQLite`/`Oracle`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`
* 推荐安装 *Visual Studio* 插件 *Razor Generator*，给`.cshtml`文件设置`自定义工具``RazorGenerator`，可以把`.cshtml`编译生成到`DLL`里面
* 项目发布时只需要拷贝`Bin`、`web.config`、`Global.asax`，以及其它自己添加的资源文件

---
### 教程
[【演示】教务系统](http://cube.newlifex.com)
[【源码】教务系统](http://git.newlifex.com/Stone/CubeDemo)

[【教程】魔方平台NewLife.Cube基础教程（附例程源码）](http://www.newlifex.com/showtopic-1483.aspx)
[【教程】魔方平台NewLife.Cube模板结构详解](http://www.newlifex.com/showtopic-1491.aspx)


## 新生命开源项目矩阵
各项目默认支持net4.5/net4.0/netstandard2.0  

|                               项目                               | 年份  |  状态  | .NET Core | 说明                                                |
| :--------------------------------------------------------------: | :---: | :----: | :-------: | --------------------------------------------------- |
|                             基础组件                             |       |        |           | 支撑其它中间件以及产品项目                          |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 维护中 |     √     | 算法、日志、网络、RPC、序列化、缓存、多线程         |
|              [XCode](https://github.com/NewLifeX/X)              | 2005  | 维护中 |     √     | 数据中间件，MySQL、SQLite、SqlServer、Oracle        |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 维护中 |     √     | 网络库，千万级吞吐率，学习gRPC、Thrift              |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 维护中 |     √     | Web魔方，企业级快速开发框架，集成OAuth              |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 维护中 |     √     | 服务管理框架，Windows服务、Linux的Systemd           |
|                              中间件                              |       |        |           | 对接各知名中间件平台                                |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | 维护中 |     √     | Redis客户端，微秒级延迟，百亿级项目验证             |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | 维护中 |     √     | 支持Apache RocketMQ和阿里云消息队列，十亿级项目验证 |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 维护中 |     √     | 物联网消息协议，客户端支持阿里云物联网              |
|     [NewLife.LoRa](https://github.com/NewLifeX/NewLife.LoRa)     | 2016  | 维护中 |     √     | 超低功耗的物联网远程通信协议LoRaWAN                 |
|   [NewLife.Thrift](https://github.com/NewLifeX/NewLife.Thrift)   | 2019  | 维护中 |     √     | Thrift协议实现                                      |
|     [NewLife.Hive](https://github.com/NewLifeX/NewLife.Hive)     | 2019  | 维护中 |     √     | 纯托管读写Hive，Hadoop数据仓库，基于Thrift协议      |
|             [NoDb](https://github.com/NewLifeX/NoDb)             | 2017  | 开发中 |     √     | NoSQL数据库，百万级kv读写性能，持久化               |
|      [NewLife.Ftp](https://github.com/NewLifeX/NewLife.Ftp)      | 2008  | 维护中 |     √     | Ftp客户端实现                                       |
|                             产品平台                             |       |        |           | 产品平台级，编译部署即用，个性化自定义              |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 维护中 |     √     | 蚂蚁调度系统，大数据实时计算平台                    |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 维护中 |     √     | 星尘，微服务平台，分布式平台                        |
|            [XLink](https://github.com/NewLifeX/XLink)            | 2016  | 维护中 |     √     | 物联网云平台                                        |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 维护中 |     √     | 产品级反向代理                                      |
|          [XScript](https://github.com/NewLifeX/XScript)          | 2010  | 维护中 |     ×     | C#脚本引擎                                          |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 维护中 |   C++11   | 嵌入式操作系统，完全独立自主，ARM Cortex-M芯片架构  |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | 维护中 |     ×     | Git管理系统                                         |
|                               其它                               |       |        |           |                                                     |
|           [XCoder](https://github.com/NewLifeX/XCoder)           | 2006  | 维护中 |     √     | 码神工具，开发者必备                                |
|        [XTemplate](https://github.com/NewLifeX/XTemplate)        | 2008  | 维护中 |     √     | 模版引擎，T4(Text Template)语法                     |
|       [X组件 .NET2.0](https://github.com/NewLifeX/X_NET20)       | 2002  | 存档中 |  .NET2.0  | 日志、网络、RPC、序列化、缓存、Windows服务、多线程  |

#### 新生命开发团队
`新生命团队始于2002年，部分开源项目具有15年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://www.NewLifeX.com  
国外：https://github.com/NewLifeX  
博客：https://nnhy.cnblogs.com  
QQ群：1600800/1600838  
