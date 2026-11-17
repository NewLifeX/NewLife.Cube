const local: App.I18n.Schema = {
  system: {
    title: 'Soybean 管理系统',
    updateTitle: '系统版本更新通知',
    updateContent: '检测到系统有新版本发布，是否立即刷新页面？',
    updateConfirm: '立即刷新',
    updateCancel: '稍后再说'
  },
  common: {
    action: '操作',
    add: '新增',
    addSuccess: '添加成功',
    backToHome: '返回首页',
    batchDelete: '批量删除',
    cancel: '取消',
    close: '关闭',
    check: '勾选',
    expandColumn: '展开列',
    columnSetting: '列设置',
    config: '配置',
    confirm: '确认',
    delete: '删除',
    deleteSuccess: '删除成功',
    confirmDelete: '确认删除吗？',
    edit: '编辑',
    warning: '警告',
    error: '错误',
    index: '序号',
    keywordSearch: '请输入关键词搜索',
    logout: '退出登录',
    logoutConfirm: '确认退出登录吗？',
    lookForward: '敬请期待',
    modify: '修改',
    modifySuccess: '修改成功',
    noData: '无数据',
    operate: '操作',
    pleaseCheckValue: '请检查输入的值是否合法',
    refresh: '刷新',
    reset: '重置',
    search: '搜索',
    switch: '切换',
    tip: '提示',
    trigger: '触发',
    update: '更新',
    updateSuccess: '更新成功',
    view: '查看',
    userCenter: '个人中心',
    yesOrNo: {
      yes: '是',
      no: '否'
    }
  },
  request: {
    logout: '请求失败后登出用户',
    logoutMsg: '用户状态失效，请重新登录',
    logoutWithModal: '请求失败后弹出模态框再登出用户',
    logoutWithModalMsg: '用户状态失效，请重新登录',
    refreshToken: '请求的token已过期，刷新token',
    tokenExpired: 'token已过期'
  },
  theme: {
    themeSchema: {
      title: '主题模式',
      light: '亮色模式',
      dark: '暗黑模式',
      auto: '跟随系统'
    },
    grayscale: '灰色模式',
    colourWeakness: '色弱模式',
    layoutMode: {
      title: '布局模式',
      vertical: '左侧菜单模式',
      'vertical-mix': '左侧菜单混合模式',
      horizontal: '顶部菜单模式',
      'horizontal-mix': '顶部菜单混合模式',
      reverseHorizontalMix: '一级菜单与子级菜单位置反转'
    },
    recommendColor: '应用推荐算法的颜色',
    recommendColorDesc: '推荐颜色的算法参照',
    themeColor: {
      title: '主题颜色',
      primary: '主色',
      info: '信息色',
      success: '成功色',
      warning: '警告色',
      error: '错误色',
      followPrimary: '跟随主色'
    },
    scrollMode: {
      title: '滚动模式',
      wrapper: '外层滚动',
      content: '主体滚动'
    },
    page: {
      animate: '页面切换动画',
      mode: {
        title: '页面切换动画类型',
        'fade-slide': '滑动',
        fade: '淡入淡出',
        'fade-bottom': '底部消退',
        'fade-scale': '缩放消退',
        'zoom-fade': '渐变',
        'zoom-out': '闪现',
        none: '无'
      }
    },
    fixedHeaderAndTab: '固定头部和标签栏',
    header: {
      height: '头部高度',
      breadcrumb: {
        visible: '显示面包屑',
        showIcon: '显示面包屑图标'
      },
      multilingual: {
        visible: '显示多语言按钮'
      },
      globalSearch: {
        visible: '显示全局搜索按钮'
      }
    },
    tab: {
      visible: '显示标签栏',
      cache: '标签栏信息缓存',
      height: '标签栏高度',
      mode: {
        title: '标签栏风格',
        chrome: '谷歌风格',
        button: '按钮风格'
      }
    },
    sider: {
      inverted: '深色侧边栏',
      width: '侧边栏宽度',
      collapsedWidth: '侧边栏折叠宽度',
      mixWidth: '混合布局侧边栏宽度',
      mixCollapsedWidth: '混合布局侧边栏折叠宽度',
      mixChildMenuWidth: '混合布局子菜单宽度'
    },
    footer: {
      visible: '显示底部',
      fixed: '固定底部',
      height: '底部高度',
      right: '底部局右'
    },
    watermark: {
      visible: '显示全屏水印',
      text: '水印文本',
      enableUserName: '启用用户名水印'
    },
    themeDrawerTitle: '主题配置',
    pageFunTitle: '页面功能',
    configOperation: {
      copyConfig: '复制配置',
      copySuccessMsg: '复制成功，请替换 src/theme/settings.ts 中的变量 themeSettings',
      resetConfig: '重置配置',
      resetSuccessMsg: '重置成功'
    }
  },
  route: {
    login: '登录',
    workbench: '工作台',
    403: '无权限',
    404: '页面不存在',
    500: '服务器错误',
    'iframe-page': '外链页面',
    dynamic: '动态页面',
    exception: '异常页',
    exception_403: '403',
    exception_404: '404',
    exception_500: '500',
    document: '文档',
    document_project: '项目文档',
    'document_project-link': '项目文档(外链)',
    document_vue: 'Vue文档',
    document_vite: 'Vite文档',
    document_unocss: 'UnoCSS文档',
    document_naive: 'Naive UI文档',
    document_antd: 'Ant Design Vue文档',
    'document_element-plus': 'Element Plus文档',
    document_alova: 'Alova文档',
  },
  page: {
    list: {
      loadFailed: '数据加载失败',
      retry: '重试',
      advanced: '高级',
      import: '导入',
      importSuccess: '导入成功',
      readOnly: '只读',
      selected: '已选 {count} 项',
      confirmBatchDelete: '确认删除选中的 {count} 项吗？',
      enableSuccess: '启用成功',
      disableSuccess: '禁用成功'
    },
    form: {
      addTitle: '新增{name}',
      editTitle: '编辑{name}',
      viewTitle: '查看{name}',
      required: '{label}不能为空',
      basicInfo: '基础信息',
      extensionInfo: '扩展信息',
      rootMenu: '顶级菜单',
      permissionMenu: '菜单',
      permissionGranted: '授权',
      permissionHint: '按菜单勾选操作权限，未勾选的菜单不授予访问权限',
      permissionNone: '暂无可配置的权限清单',
      permissionLoadFailed: '权限清单加载失败，请刷新重试'
    },
    workbench: {
      greeting: '你好，{name}',
      quickLinks: '快捷入口',
      systemInfo: '系统信息',
      empty: '暂无可展示内容',
      monitorWaiting: '等待监控数据…',
      cpu: 'CPU',
      memory: '内存',
      editLayout: '调整布局',
      finishLayout: '完成',
      saveLayout: '保存布局',
      resetLayout: '恢复默认',
      dragHint: '拖动卡片可调整顺序；点击卡片右上角"隐藏"可移除卡片',
      hiddenCards: '已隐藏的卡片',
      noHiddenCards: '暂无隐藏卡片',
      hideCard: '隐藏',
      layoutSaved: '布局已保存',
      layoutSaveFailed: '布局保存失败',
      layoutReset: '已恢复默认布局',
      logins: '登录次数',
      lastLogin: '最后登录',
      lastLoginIp: '最后登录IP',
      registerTime: '注册时间'
    },
    login: {
      common: {
        loginOrRegister: '登录 / 注册',
        userNamePlaceholder: '请输入用户名',
        phonePlaceholder: '请输入手机号',
        codePlaceholder: '请输入验证码',
        captchaPlaceholder: '请输入图片验证码',
        passwordPlaceholder: '请输入密码',
        confirmPasswordPlaceholder: '请再次输入密码',
        codeLogin: '验证码登录',
        confirm: '确定',
        back: '返回',
        validateSuccess: '验证成功',
        loginSuccess: '登录成功',
        welcomeBack: '欢迎回来，{userName} ！'
      },
      pwdLogin: {
        title: '密码登录',
        rememberMe: '记住我',
        forgetPassword: '忘记密码？',
        register: '注册账号',
        otherAccountLogin: '其他账号登录',
        otherLoginMode: '其他登录方式',
        superAdmin: '超级管理员',
        admin: '管理员',
        user: '普通用户'
      },
      codeLogin: {
        title: '验证码登录',
        getCode: '获取验证码',
        reGetCode: '{time}秒后重新获取',
        sendCodeSuccess: '验证码发送成功',
        imageCodePlaceholder: '请输入图片验证码'
      },
      register: {
        title: '注册账号',
        agreement: '我已经仔细阅读并接受',
        protocol: '《用户协议》',
        policy: '《隐私权政策》'
      },
      resetPwd: {
        title: '重置密码'
      },
      bindWeChat: {
        title: '绑定微信'
      }
    },
    about: {
      title: '关于',
      introduction: `SoybeanAdmin 是一个优雅且功能强大的后台管理模板，基于最新的前端技术栈，包括 Vue3, Vite5, TypeScript, Pinia 和 UnoCSS。它内置了丰富的主题配置和组件，代码规范严谨，实现了自动化的文件路由系统。此外，它还采用了基于 ApiFox 的在线Mock数据方案。SoybeanAdmin 为您提供了一站式的后台管理解决方案，无需额外配置，开箱即用。同样是一个快速学习前沿技术的最佳实践。`,
      projectInfo: {
        title: '项目信息',
        version: '版本',
        latestBuildTime: '最新构建时间',
        githubLink: 'Github 地址',
        previewLink: '预览地址'
      },
      prdDep: '生产依赖',
      devDep: '开发依赖'
    },
    home: {
      branchDesc:
        '为了方便大家开发和更新合并，我们对main分支的代码进行了精简，只保留了首页菜单，其余内容已移至example分支进行维护。预览地址显示的内容即为example分支的内容。',
      greeting: '早安，{userName}, 今天又是充满活力的一天!',
      weatherDesc: '今日多云转晴，20℃ - 25℃!',
      projectCount: '项目数',
      todo: '待办',
      message: '消息',
      downloadCount: '下载量',
      registerCount: '注册量',
      schedule: '作息安排',
      study: '学习',
      work: '工作',
      rest: '休息',
      entertainment: '娱乐',
      visitCount: '访问量',
      turnover: '成交额',
      dealCount: '成交量',
      projectNews: {
        title: '项目动态',
        moreNews: '更多动态',
        desc1: 'Soybean 在2021年5月28日创建了开源项目 soybean-admin!',
        desc2: 'Yanbowe 向 soybean-admin 提交了一个bug，多标签栏不会自适应。',
        desc3: 'Soybean 准备为 soybean-admin 的发布做充分的准备工作!',
        desc4: 'Soybean 正在忙于为soybean-admin写项目说明文档！',
        desc5: 'Soybean 刚才把工作台页面随便写了一些，凑合能看了！'
      },
      creativity: '创意'
    },
    function: {
      tab: {
        tabOperate: {
          title: '标签页操作',
          addTab: '添加标签页',
          addTabDesc: '跳转到关于页面',
          closeTab: '关闭标签页',
          closeCurrentTab: '关闭当前标签页',
          closeAboutTab: '关闭"关于"标签页',
          addMultiTab: '添加多标签页',
          addMultiTabDesc1: '跳转到多标签页页面',
          addMultiTabDesc2: '跳转到多标签页页面(带有查询参数)'
        },
        tabTitle: {
          title: '标签页标题',
          changeTitle: '修改标题',
          change: '修改',
          resetTitle: '重置标题',
          reset: '重置'
        }
      },
      multiTab: {
        routeParam: '路由参数',
        backTab: '返回 function_tab'
      },
      toggleAuth: {
        toggleAccount: '切换账号',
        authHook: '权限钩子函数 `hasAuth`',
        superAdminVisible: '超级管理员可见',
        adminVisible: '管理员可见',
        adminOrUserVisible: '管理员和用户可见'
      },
      request: {
        repeatedErrorOccurOnce: '重复请求错误只出现一次',
        repeatedError: '重复请求错误',
        repeatedErrorMsg1: '自定义请求错误 1',
        repeatedErrorMsg2: '自定义请求错误 2'
      }
    },
    alova: {
      scenes: {
        captchaSend: '发送验证码',
        autoRequest: '自动请求',
        visibilityRequestTips: '浏览器窗口切换自动请求数据',
        pollingRequestTips: '每3秒自动请求一次',
        networkRequestTips: '网络重连后自动请求',
        refreshTime: '更新时间',
        startRequest: '开始请求',
        stopRequest: '停止请求',
        requestCrossComponent: '跨组件触发请求',
        triggerAllRequest: '手动触发所有自动请求'
      }
    },
    manage: {
      common: {
        status: {
          enable: '启用',
          disable: '禁用'
        }
      },
      role: {
        title: '角色列表',
        roleName: '角色名称',
        roleCode: '角色编码',
        roleStatus: '角色状态',
        roleDesc: '角色描述',
        menuAuth: '菜单权限',
        buttonAuth: '按钮权限',
        form: {
          roleName: '请输入角色名称',
          roleCode: '请输入角色编码',
          roleStatus: '请选择角色状态',
          roleDesc: '请输入角色描述'
        },
        addRole: '新增角色',
        editRole: '编辑角色'
      },
      user: {
        title: '用户列表',
        userName: '用户名',
        userGender: '性别',
        nickName: '昵称',
        userPhone: '手机号',
        userEmail: '邮箱',
        userStatus: '用户状态',
        userRole: '用户角色',
        form: {
          userName: '请输入用户名',
          userGender: '请选择性别',
          nickName: '请输入昵称',
          userPhone: '请输入手机号',
          userEmail: '请输入邮箱',
          userStatus: '请选择用户状态',
          userRole: '请选择用户角色'
        },
        addUser: '新增用户',
        editUser: '编辑用户',
        gender: {
          male: '男',
          female: '女'
        }
      },
      menu: {
        home: '首页',
        title: '菜单列表',
        id: 'ID',
        parentId: '父级菜单ID',
        menuType: '菜单类型',
        menuName: '菜单名称',
        routeName: '路由名称',
        routePath: '路由路径',
        pathParam: '路径参数',
        layout: '布局',
        page: '页面组件',
        i18nKey: '国际化key',
        icon: '图标',
        localIcon: '本地图标',
        iconTypeTitle: '图标类型',
        order: '排序',
        constant: '常量路由',
        keepAlive: '缓存路由',
        href: '外链',
        hideInMenu: '隐藏菜单',
        activeMenu: '高亮的菜单',
        multiTab: '支持多页签',
        fixedIndexInTab: '固定在页签中的序号',
        query: '路由参数',
        button: '按钮',
        buttonCode: '按钮编码',
        buttonDesc: '按钮描述',
        menuStatus: '菜单状态',
        form: {
          home: '请选择首页',
          menuType: '请选择菜单类型',
          menuName: '请输入菜单名称',
          routeName: '请输入路由名称',
          routePath: '请输入路由路径',
          pathParam: '请输入路径参数',
          page: '请选择页面组件',
          layout: '请选择布局组件',
          i18nKey: '请输入国际化key',
          icon: '请输入图标',
          localIcon: '请选择本地图标',
          order: '请输入排序',
          keepAlive: '请选择是否缓存路由',
          href: '请输入外链',
          hideInMenu: '请选择是否隐藏菜单',
          activeMenu: '请选择高亮的菜单的路由名称',
          multiTab: '请选择是否支持多标签',
          fixedInTab: '请选择是否固定在页签中',
          fixedIndexInTab: '请输入固定在页签中的序号',
          queryKey: '请输入路由参数Key',
          queryValue: '请输入路由参数Value',
          button: '请选择是否按钮',
          buttonCode: '请输入按钮编码',
          buttonDesc: '请输入按钮描述',
          menuStatus: '请选择菜单状态'
        },
        addMenu: '新增菜单',
        editMenu: '编辑菜单',
        addChildMenu: '新增子菜单',
        type: {
          directory: '目录',
          menu: '菜单'
        },
        iconType: {
          iconify: 'iconify图标',
          local: '本地图标'
        }
      }
    }
  },
  settingsCenter: {
    title: '设置中心',
    subtitle: '集中管理系统运行参数与各模块配置项',
    save: '保存设置',
    saveSuccess: '设置已保存',
    unavailable: '暂无权限或加载失败',
    entries: {
      Core: '基础设置',
      Sys: '系统设置',
      Star: '星尘设置',
      XCode: '数据中间件',
      Cube: '设置中心',
      SmsConfig: '短信配置',
      MailConfig: '邮件配置',
      OAuthConfig: 'OAuth 配置',
      AccessRule: '访问规则'
    }
  },
  dbAdmin: {
    title: '数据库管理',
    subtitle: '数据库备份与管理操作',
    listTitle: '数据库列表',
    name: '名称',
    type: '类型',
    version: '版本',
    driver: '驱动',
    driverVersion: '驱动版本',
    entities: '实体数',
    tables: '表数',
    backups: '备份数',
    viewTables: '查看表',
    viewEntities: '查看实体',
    viewDiff: '模型差异',
    compact: '压缩',
    backup: '数据库备份',
    backupAndCompress: '备份并压缩',
    downloadSchema: '下载表结构',
    tableStructureTitle: '表结构',
    entityStructureTitle: '实体结构',
    diffTitle: '模型差异',
    diffEmpty: '数据库与实体模型无差异',
    noEntityModel: '无实体模型',
    extraColumns: '多出的字段',
    columnName: '列名',
    displayName: '显示名',
    dataType: '数据类型',
    length: '长度',
    precision: '精度',
    keyFlag: '键',
    nullable: '允许空',
    description: '说明',
    compactSuccess: '数据库压缩完成',
    compactFailed: '数据库压缩失败',
    compressConfirm: '备份并压缩操作可能需要较长时间，确定要继续吗？',
    confirmAction: '确认操作',
    compressSuccess: '数据库备份并压缩成功',
    compressFailed: '数据库备份并压缩失败',
    backupSuccess: '数据库备份成功',
    backupFailed: '数据库备份失败',
    downloadFailed: '文件下载失败'
  },
  serverInfo: {
    title: '服务器信息',
    subtitle: '服务器运行状态、程序集与菜单树',
    systemInfo: '系统信息',
    noSystemInfo: '暂无系统信息',
    serverVars: '服务端变量',
    serverHeaders: '服务端请求头',
    requestInfo: '请求信息',
    headerName: '名称',
    attrName: '属性',
    value: '值',
    loadedModules: '已加载模块',
    filterModules: '输入关键字过滤模块',
    fileName: '文件名',
    productName: '产品名称',
    companyName: '公司',
    version: '版本',
    size: '大小',
    assemblies: '程序集列表',
    filterAssemblies: '输入关键字过滤程序集',
    memoryFree: '释放内存',
    memoryFreeSuccess: '内存已释放',
    restart: '重启服务',
    restartConfirm: '重启服务会中断当前所有请求，确定要继续吗？',
    restartTitle: '确认重启',
    assemblyName: '名称',
    assemblyTitle: '标题',
    fileVersion: '文件版本',
    assemblyVersion: '程序集版本',
    compileTime: '编译时间',
    location: '位置',
    menuTree: '菜单树',
    selectModule: '选择模块',
    all: '全部',
    hidden: '隐藏',
    newWindow: '新窗口',
    noServerVars: '暂无服务端变量',
    noMenu: '暂无菜单',
    noData: '暂无数据',
    labelSystem: '系统',
    labelPath: '路径',
    labelHost: '主机',
    labelLocal: '本地地址',
    labelRemote: '远程地址',
    labelApplication: '应用',
    labelApplicationTitle: '应用标题',
    labelVersion: '版本',
    labelOs: '操作系统',
    labelOsVersion: '系统版本',
    labelMachineId: '机器标识',
    labelMachineProduct: '机器型号',
    labelCpu: 'CPU',
    labelOpenTime: '启动时间',
    labelServerTime: '服务器时间',
    labelMemory: '内存',
    labelProcessTime: '进程时间',
    labelGc: 'GC'
  },
  widgetAdmin: {
    title: '工作台部件',
    subtitle: '管理首页工作台卡片的分组、顺序与启用状态',
    groupOrder: '分组顺序',
    group: '分组',
    widgetName: '部件',
    cols: '占用列数',
    sort: '排序',
    enable: '启用',
    itemOrder: '部件顺序',
    moveUp: '上移',
    moveDown: '下移',
    adminOnly: '仅管理员'
  },
  lovAdmin: {
    title: '值集管理',
    subtitle: '维护枚举值集与列表型值集配置',
    add: '新增值集',
    addTitle: '新增值集',
    editTitle: '编辑值集',
    keyword: '关键词',
    keywordHint: '编码/名称搜索',
    type: '类型',
    all: '全部',
    typeEnum: '枚举型',
    typeList: '列表型',
    typeEnumFull: '枚举型 (ENUM)',
    typeListFull: '列表型 (LIST)',
    code: '值集编码',
    codeRequired: '请输入值集编码',
    codeTip: '完整编码 = 前缀 + 语义名，如 Enum.OrderStatus 或 List.Department',
    displayName: '显示名称',
    displayNameHint: '值集的显示名称',
    valueField: '值字段',
    valueFieldHint: '如 userId',
    labelField: '标签字段',
    labelFieldHint: '如 userName',
    source: '来源',
    sourceAuto: '自动',
    sourceManual: '手工',
    enabled: '启用',
    createTime: '创建时间',
    remark: '备注',
    enumValues: '枚举值',
    listConfig: '列表配置',
    lovType: '值集类型',
    deleteConfirm: '确定要删除值集 "{code}" 吗？',
    confirmDelete: '确认删除',
    loadFailed: '加载值集失败',
    saveFailed: '保存值集失败',
    deleteFailed: '删除值集失败'
  },
  lovConfig: {
    title: '值集配置 — {code}',
    enumItemsTab: '枚举值',
    autoManagedTip: '该值集由代码自动管理(C#枚举)，枚举值不可手工编辑',
    valueColumn: '值(Value)',
    labelColumn: '标签(Label)',
    addEnumItem: '+ 新增枚举值',
    requestUrl: '请求地址',
    requestUrlTip: '仅后端可见，不下发到前端',
    method: '请求方式',
    proxyRequest: '是否代理请求',
    proxyRequestTip:
      '开启后由后端代理转发外部数据源（地址对前端不可见、规避跨域）；关闭则由前端直接请求上方「请求地址」。',
    sameAppUrlTip: '请求地址以 / 开头（同源同应用），已强制前端直连，不可开启代理。',
    pageable: '是否分页',
    pageNumField: '页码参数名',
    pageSizeField: '每页条数参数名',
    dataPath: '数据路径',
    dataPathTip: '数据列表所在的 JSON 路径，如 data.list 或直接 data',
    totalPath: '总数路径',
    totalPathTip: '总记录数所在的 JSON 路径，如 page.totalCount',
    fixedParams: '固定参数(JSON)',
    searchFieldsTab: '搜索字段',
    fieldName: '字段名',
    displayTitle: '显示标题',
    controlType: '控件类型',
    refLov: '关联值集',
    addSearchField: '+ 新增搜索字段',
    editSearchField: '编辑搜索字段',
    fieldNameHint: '如 deptName',
    titleHint: '如 部门名称',
    controlInput: '文本输入 (input)',
    controlSelect: '下拉选择 (select)',
    controlLov: '值集选择 (lov)',
    refLovHint: '如 Enum.Department',
    paramType: '传参方式',
    required: '必填',
    defaultValue: '默认值',
    tableColumnsTab: '表格列',
    columnTitle: '标题',
    columnWidth: '宽度',
    columnAlign: '对齐',
    addColumn: '+ 新增列',
    editColumn: '编辑表格列',
    fieldNameHintStatus: '如 status',
    titleHintStatus: '如 状态',
    columnWidthPx: '列宽(px)',
    align: '对齐方式',
    alignLeft: '左',
    alignCenter: '中',
    alignRight: '右',
    sortable: '可排序',
    refLovHintStatus: '如 Enum.Status',
    refLovTip: '选中后自动对列值做翻译',
    formatType: '格式化类型',
    formatTypeHint: '与关联值集互斥',
    saveConfig: '保存配置',
    loadFailed: '加载配置失败',
    saveSuccess: '保存成功',
    saveFailed: '保存失败',
    sortLabel: '排序',
    enabledLabel: '启用'
  },
  form: {
    required: '不能为空',
    userName: {
      required: '请输入用户名',
      invalid: '用户名格式不正确'
    },
    phone: {
      required: '请输入手机号',
      invalid: '手机号格式不正确'
    },
    pwd: {
      required: '请输入密码',
      invalid: '密码格式不正确，6-18位字符，包含字母、数字、下划线'
    },
    confirmPwd: {
      required: '请输入确认密码',
      invalid: '两次输入密码不一致'
    },
    code: {
      required: '请输入验证码',
      invalid: '验证码格式不正确'
    },
    email: {
      required: '请输入邮箱',
      invalid: '邮箱格式不正确'
    }
  },
  dropdown: {
    closeCurrent: '关闭',
    closeOther: '关闭其它',
    closeLeft: '关闭左侧',
    closeRight: '关闭右侧',
    closeAll: '关闭所有'
  },
  icon: {
    themeConfig: '主题配置',
    themeSchema: '主题模式',
    lang: '切换语言',
    fullscreen: '全屏',
    fullscreenExit: '退出全屏',
    reload: '刷新页面',
    collapse: '折叠菜单',
    expand: '展开菜单',
    pin: '固定',
    unpin: '取消固定'
  },
  datatable: {
    itemCount: '共 {total} 条'
  }
};

export default local;
