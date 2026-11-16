const local: App.I18n.Schema = {
  system: {
    title: 'SoybeanAdmin',
    updateTitle: 'System Version Update Notification',
    updateContent: 'A new version of the system has been detected. Do you want to refresh the page immediately?',
    updateConfirm: 'Refresh immediately',
    updateCancel: 'Later'
  },
  common: {
    action: 'Action',
    add: 'Add',
    addSuccess: 'Add Success',
    backToHome: 'Back to home',
    batchDelete: 'Batch Delete',
    cancel: 'Cancel',
    close: 'Close',
    check: 'Check',
    expandColumn: 'Expand Column',
    columnSetting: 'Column Setting',
    config: 'Config',
    confirm: 'Confirm',
    delete: 'Delete',
    deleteSuccess: 'Delete Success',
    confirmDelete: 'Are you sure you want to delete?',
    edit: 'Edit',
    warning: 'Warning',
    error: 'Error',
    index: 'Index',
    keywordSearch: 'Please enter keyword',
    logout: 'Logout',
    logoutConfirm: 'Are you sure you want to log out?',
    lookForward: 'Coming soon',
    modify: 'Modify',
    modifySuccess: 'Modify Success',
    noData: 'No Data',
    operate: 'Operate',
    pleaseCheckValue: 'Please check whether the value is valid',
    refresh: 'Refresh',
    reset: 'Reset',
    search: 'Search',
    switch: 'Switch',
    tip: 'Tip',
    trigger: 'Trigger',
    update: 'Update',
    updateSuccess: 'Update Success',
    userCenter: 'User Center',
    yesOrNo: {
      yes: 'Yes',
      no: 'No'
    }
  },
  request: {
    logout: 'Logout user after request failed',
    logoutMsg: 'User status is invalid, please log in again',
    logoutWithModal: 'Pop up modal after request failed and then log out user',
    logoutWithModalMsg: 'User status is invalid, please log in again',
    refreshToken: 'The requested token has expired, refresh the token',
    tokenExpired: 'The requested token has expired'
  },
  theme: {
    themeSchema: {
      title: 'Theme Schema',
      light: 'Light',
      dark: 'Dark',
      auto: 'Follow System'
    },
    grayscale: 'Grayscale',
    colourWeakness: 'Colour Weakness',
    layoutMode: {
      title: 'Layout Mode',
      vertical: 'Vertical Menu Mode',
      horizontal: 'Horizontal Menu Mode',
      'vertical-mix': 'Vertical Mix Menu Mode',
      'horizontal-mix': 'Horizontal Mix menu Mode',
      reverseHorizontalMix: 'Reverse first level menus and child level menus position'
    },
    recommendColor: 'Apply Recommended Color Algorithm',
    recommendColorDesc: 'The recommended color algorithm refers to',
    themeColor: {
      title: 'Theme Color',
      primary: 'Primary',
      info: 'Info',
      success: 'Success',
      warning: 'Warning',
      error: 'Error',
      followPrimary: 'Follow Primary'
    },
    scrollMode: {
      title: 'Scroll Mode',
      wrapper: 'Wrapper',
      content: 'Content'
    },
    page: {
      animate: 'Page Animate',
      mode: {
        title: 'Page Animate Mode',
        fade: 'Fade',
        'fade-slide': 'Slide',
        'fade-bottom': 'Fade Zoom',
        'fade-scale': 'Fade Scale',
        'zoom-fade': 'Zoom Fade',
        'zoom-out': 'Zoom Out',
        none: 'None'
      }
    },
    fixedHeaderAndTab: 'Fixed Header And Tab',
    header: {
      height: 'Header Height',
      breadcrumb: {
        visible: 'Breadcrumb Visible',
        showIcon: 'Breadcrumb Icon Visible'
      },
      multilingual: {
        visible: 'Display multilingual button'
      },
      globalSearch: {
        visible: 'Display global search button'
      }
    },
    tab: {
      visible: 'Tab Visible',
      cache: 'Tag Bar Info Cache',
      height: 'Tab Height',
      mode: {
        title: 'Tab Mode',
        chrome: 'Chrome',
        button: 'Button'
      }
    },
    sider: {
      inverted: 'Dark Sider',
      width: 'Sider Width',
      collapsedWidth: 'Sider Collapsed Width',
      mixWidth: 'Mix Sider Width',
      mixCollapsedWidth: 'Mix Sider Collapse Width',
      mixChildMenuWidth: 'Mix Child Menu Width'
    },
    footer: {
      visible: 'Footer Visible',
      fixed: 'Fixed Footer',
      height: 'Footer Height',
      right: 'Right Footer'
    },
    watermark: {
      visible: 'Watermark Full Screen Visible',
      text: 'Watermark Text',
      enableUserName: 'Enable User Name Watermark'
    },
    themeDrawerTitle: 'Theme Configuration',
    pageFunTitle: 'Page Function',
    configOperation: {
      copyConfig: 'Copy Config',
      copySuccessMsg: 'Copy Success, Please replace the variable "themeSettings" in "src/theme/settings.ts"',
      resetConfig: 'Reset Config',
      resetSuccessMsg: 'Reset Success'
    }
  },
  route: {
    login: 'Login',
    workbench: 'Workbench',
    403: 'No Permission',
    404: 'Page Not Found',
    500: 'Server Error',
    'iframe-page': 'Iframe',
    dynamic: 'Dynamic page',
    exception: 'Exception',
    exception_403: '403',
    exception_404: '404',
    exception_500: '500',
    document: 'Document',
    document_project: 'Project Document',
    'document_project-link': 'Project Document(External Link)',
    document_vue: 'Vue Document',
    document_vite: 'Vite Document',
    document_unocss: 'UnoCSS Document',
    document_naive: 'Naive UI Document',
    document_antd: 'Ant Design Vue Document',
    'document_element-plus': 'Element Plus Document',
    document_alova: 'Alova Document',
  },
  page: {
    list: {
      loadFailed: 'Failed to load data',
      retry: 'Retry',
      advanced: 'Advanced',
      import: 'Import',
      importSuccess: 'Imported successfully',
      readOnly: 'Read only',
      selected: '{count} selected',
      confirmBatchDelete: 'Delete the selected {count} item(s)?',
      enableSuccess: 'Enabled successfully',
      disableSuccess: 'Disabled successfully'
    },
    form: {
      addTitle: 'Add {name}',
      editTitle: 'Edit {name}',
      required: '{label} is required',
      basicInfo: 'Basic info',
      extensionInfo: 'Extension info',
      rootMenu: 'Top level',
      permissionMenu: 'Menu',
      permissionGranted: 'Grant',
      permissionHint: 'Check actions per menu; unchecked menus are not granted',
      permissionNone: 'No permissions available',
      permissionLoadFailed: 'Failed to load permissions, please retry'
    },
    workbench: {
      greeting: 'Hello, {name}',
      quickLinks: 'Quick links',
      systemInfo: 'System info',
      empty: 'Nothing to display',
      monitorWaiting: 'Waiting for monitor data…',
      cpu: 'CPU',
      memory: 'Memory',
      editLayout: 'Edit layout',
      finishLayout: 'Done',
      saveLayout: 'Save layout',
      resetLayout: 'Reset',
      dragHint: 'Drag cards to reorder; click "Hide" on a card header to remove it',
      hiddenCards: 'Hidden cards',
      noHiddenCards: 'No hidden cards',
      hideCard: 'Hide',
      layoutSaved: 'Layout saved',
      layoutSaveFailed: 'Failed to save layout',
      layoutReset: 'Layout reset to default',
      logins: 'Logins',
      lastLogin: 'Last login',
      lastLoginIp: 'Last login IP',
      registerTime: 'Register time'
    },
    login: {
      common: {
        loginOrRegister: 'Login / Register',
        userNamePlaceholder: 'Please enter user name',
        phonePlaceholder: 'Please enter phone number',
        codePlaceholder: 'Please enter verification code',
        captchaPlaceholder: 'Please enter image captcha',
        passwordPlaceholder: 'Please enter password',
        confirmPasswordPlaceholder: 'Please enter password again',
        codeLogin: 'Verification code login',
        confirm: 'Confirm',
        back: 'Back',
        validateSuccess: 'Verification passed',
        loginSuccess: 'Login successfully',
        welcomeBack: 'Welcome back, {userName} !'
      },
      pwdLogin: {
        title: 'Password Login',
        rememberMe: 'Remember me',
        forgetPassword: 'Forget password?',
        register: 'Register',
        otherAccountLogin: 'Other Account Login',
        otherLoginMode: 'Other Login Mode',
        superAdmin: 'Super Admin',
        admin: 'Admin',
        user: 'User'
      },
      codeLogin: {
        title: 'Verification Code Login',
        getCode: 'Get verification code',
        reGetCode: 'Reacquire after {time}s',
        sendCodeSuccess: 'Verification code sent successfully',
        imageCodePlaceholder: 'Please enter image verification code'
      },
      register: {
        title: 'Register',
        agreement: 'I have read and agree to',
        protocol: '《User Agreement》',
        policy: '《Privacy Policy》'
      },
      resetPwd: {
        title: 'Reset Password'
      },
      bindWeChat: {
        title: 'Bind WeChat'
      }
    },
    about: {
      title: 'About',
      introduction: `SoybeanAdmin is an elegant and powerful admin template, based on the latest front-end technology stack, including Vue3, Vite5, TypeScript, Pinia and UnoCSS. It has built-in rich theme configuration and components, strict code specifications, and an automated file routing system. In addition, it also uses the online mock data solution based on ApiFox. SoybeanAdmin provides you with a one-stop admin solution, no additional configuration, and out of the box. It is also a best practice for learning cutting-edge technologies quickly.`,
      projectInfo: {
        title: 'Project Info',
        version: 'Version',
        latestBuildTime: 'Latest Build Time',
        githubLink: 'Github Link',
        previewLink: 'Preview Link'
      },
      prdDep: 'Production Dependency',
      devDep: 'Development Dependency'
    },
    home: {
      branchDesc:
        'For the convenience of everyone in developing and updating the merge, we have streamlined the code of the main branch, only retaining the homepage menu, and the rest of the content has been moved to the example branch for maintenance. The preview address displays the content of the example branch.',
      greeting: 'Good morning, {userName}, today is another day full of vitality!',
      weatherDesc: 'Today is cloudy to clear, 20℃ - 25℃!',
      projectCount: 'Project Count',
      todo: 'Todo',
      message: 'Message',
      downloadCount: 'Download Count',
      registerCount: 'Register Count',
      schedule: 'Work and rest Schedule',
      study: 'Study',
      work: 'Work',
      rest: 'Rest',
      entertainment: 'Entertainment',
      visitCount: 'Visit Count',
      turnover: 'Turnover',
      dealCount: 'Deal Count',
      projectNews: {
        title: 'Project News',
        moreNews: 'More News',
        desc1: 'Soybean created the open source project soybean-admin on May 28, 2021!',
        desc2: 'Yanbowe submitted a bug to soybean-admin, the multi-tab bar will not adapt.',
        desc3: 'Soybean is ready to do sufficient preparation for the release of soybean-admin!',
        desc4: 'Soybean is busy writing project documentation for soybean-admin!',
        desc5: 'Soybean just wrote some of the workbench pages casually, and it was enough to see!'
      },
      creativity: 'Creativity'
    },
    function: {
      tab: {
        tabOperate: {
          title: 'Tab Operation',
          addTab: 'Add Tab',
          addTabDesc: 'To about page',
          closeTab: 'Close Tab',
          closeCurrentTab: 'Close Current Tab',
          closeAboutTab: 'Close "About" Tab',
          addMultiTab: 'Add Multi Tab',
          addMultiTabDesc1: 'To MultiTab page',
          addMultiTabDesc2: 'To MultiTab page(with query params)'
        },
        tabTitle: {
          title: 'Tab Title',
          changeTitle: 'Change Title',
          change: 'Change',
          resetTitle: 'Reset Title',
          reset: 'Reset'
        }
      },
      multiTab: {
        routeParam: 'Route Param',
        backTab: 'Back function_tab'
      },
      toggleAuth: {
        toggleAccount: 'Toggle Account',
        authHook: 'Auth Hook Function `hasAuth`',
        superAdminVisible: 'Super Admin Visible',
        adminVisible: 'Admin Visible',
        adminOrUserVisible: 'Admin and User Visible'
      },
      request: {
        repeatedErrorOccurOnce: 'Repeated Request Error Occurs Once',
        repeatedError: 'Repeated Request Error',
        repeatedErrorMsg1: 'Custom Request Error 1',
        repeatedErrorMsg2: 'Custom Request Error 2'
      }
    },
    alova: {
      scenes: {
        captchaSend: 'Captcha Send',
        autoRequest: 'Auto Request',
        visibilityRequestTips: 'Automatically request when switching browser window',
        pollingRequestTips: 'It will request every 3 seconds',
        networkRequestTips: 'Automatically request after network reconnecting',
        refreshTime: 'Refresh Time',
        startRequest: 'Start Request',
        stopRequest: 'Stop Request',
        requestCrossComponent: 'Request Cross Component',
        triggerAllRequest: 'Manually Trigger All Automated Requests'
      }
    },
    manage: {
      common: {
        status: {
          enable: 'Enable',
          disable: 'Disable'
        }
      },
      role: {
        title: 'Role List',
        roleName: 'Role Name',
        roleCode: 'Role Code',
        roleStatus: 'Role Status',
        roleDesc: 'Role Description',
        menuAuth: 'Menu Auth',
        buttonAuth: 'Button Auth',
        form: {
          roleName: 'Please enter role name',
          roleCode: 'Please enter role code',
          roleStatus: 'Please select role status',
          roleDesc: 'Please enter role description'
        },
        addRole: 'Add Role',
        editRole: 'Edit Role'
      },
      user: {
        title: 'User List',
        userName: 'User Name',
        userGender: 'Gender',
        nickName: 'Nick Name',
        userPhone: 'Phone Number',
        userEmail: 'Email',
        userStatus: 'User Status',
        userRole: 'User Role',
        form: {
          userName: 'Please enter user name',
          userGender: 'Please select gender',
          nickName: 'Please enter nick name',
          userPhone: 'Please enter phone number',
          userEmail: 'Please enter email',
          userStatus: 'Please select user status',
          userRole: 'Please select user role'
        },
        addUser: 'Add User',
        editUser: 'Edit User',
        gender: {
          male: 'Male',
          female: 'Female'
        }
      },
      menu: {
        home: 'Home',
        title: 'Menu List',
        id: 'ID',
        parentId: 'Parent ID',
        menuType: 'Menu Type',
        menuName: 'Menu Name',
        routeName: 'Route Name',
        routePath: 'Route Path',
        pathParam: 'Path Param',
        layout: 'Layout Component',
        page: 'Page Component',
        i18nKey: 'I18n Key',
        icon: 'Icon',
        localIcon: 'Local Icon',
        iconTypeTitle: 'Icon Type',
        order: 'Order',
        constant: 'Constant',
        keepAlive: 'Keep Alive',
        href: 'Href',
        hideInMenu: 'Hide In Menu',
        activeMenu: 'Active Menu',
        multiTab: 'Multi Tab',
        fixedIndexInTab: 'Fixed Index In Tab',
        query: 'Query Params',
        button: 'Button',
        buttonCode: 'Button Code',
        buttonDesc: 'Button Desc',
        menuStatus: 'Menu Status',
        form: {
          home: 'Please select home',
          menuType: 'Please select menu type',
          menuName: 'Please enter menu name',
          routeName: 'Please enter route name',
          routePath: 'Please enter route path',
          pathParam: 'Please enter path param',
          page: 'Please select page component',
          layout: 'Please select layout component',
          i18nKey: 'Please enter i18n key',
          icon: 'Please enter iconify name',
          localIcon: 'Please enter local icon name',
          order: 'Please enter order',
          keepAlive: 'Please select whether to cache route',
          href: 'Please enter href',
          hideInMenu: 'Please select whether to hide menu',
          activeMenu: 'Please select route name of the highlighted menu',
          multiTab: 'Please select whether to support multiple tabs',
          fixedInTab: 'Please select whether to fix in the tab',
          fixedIndexInTab: 'Please enter the index fixed in the tab',
          queryKey: 'Please enter route parameter Key',
          queryValue: 'Please enter route parameter Value',
          button: 'Please select whether it is a button',
          buttonCode: 'Please enter button code',
          buttonDesc: 'Please enter button description',
          menuStatus: 'Please select menu status'
        },
        addMenu: 'Add Menu',
        editMenu: 'Edit Menu',
        addChildMenu: 'Add Child Menu',
        type: {
          directory: 'Directory',
          menu: 'Menu'
        },
        iconType: {
          iconify: 'Iconify Icon',
          local: 'Local Icon'
        }
      }
    }
  },
  settingsCenter: {
    title: 'Settings Center',
    subtitle: 'Manage system runtime options and module configurations in one place',
    save: 'Save',
    saveSuccess: 'Settings saved',
    unavailable: 'No permission or failed to load',
    entries: {
      Core: 'Basic Settings',
      Sys: 'System Settings',
      Star: 'Stardust Settings',
      XCode: 'Data Middleware',
      Cube: 'Settings Center',
      SmsConfig: 'SMS Config',
      MailConfig: 'Mail Config',
      OAuthConfig: 'OAuth Config',
      AccessRule: 'Access Rules'
    }
  },
  dbAdmin: {
    title: 'Database',
    subtitle: 'Database backup and management',
    listTitle: 'Databases',
    name: 'Name',
    type: 'Type',
    version: 'Version',
    driver: 'Driver',
    driverVersion: 'Driver Version',
    entities: 'Entities',
    tables: 'Tables',
    backups: 'Backups',
    viewTables: 'Tables',
    viewEntities: 'Entities',
    viewDiff: 'Model Diff',
    compact: 'Compact',
    backup: 'Backup',
    backupAndCompress: 'Backup & Compress',
    downloadSchema: 'Download Schema',
    tableStructureTitle: 'Table Structure',
    entityStructureTitle: 'Entity Structure',
    diffTitle: 'Model Diff',
    diffEmpty: 'No difference between database and entity model',
    noEntityModel: 'No Entity Model',
    extraColumns: 'Extra Columns',
    columnName: 'Column',
    displayName: 'Display Name',
    dataType: 'Data Type',
    length: 'Length',
    precision: 'Precision',
    keyFlag: 'Key',
    nullable: 'Nullable',
    description: 'Description',
    compactSuccess: 'Database compacted',
    compactFailed: 'Failed to compact database',
    compressConfirm: 'Backup and compress may take a long time. Continue?',
    confirmAction: 'Confirm',
    compressSuccess: 'Database backed up and compressed',
    compressFailed: 'Failed to backup and compress database',
    backupSuccess: 'Database backed up',
    backupFailed: 'Failed to backup database',
    downloadFailed: 'Failed to download file'
  },
  serverInfo: {
    title: 'Server Info',
    subtitle: 'Server status, assemblies and menu tree',
    systemInfo: 'System Info',
    noSystemInfo: 'No system info',
    serverVars: 'Server Variables',
    serverHeaders: 'Server Headers',
    requestInfo: 'Request Info',
    headerName: 'Name',
    attrName: 'Attribute',
    value: 'Value',
    loadedModules: 'Loaded Modules',
    filterModules: 'Filter modules by keyword',
    fileName: 'File Name',
    productName: 'Product',
    companyName: 'Company',
    version: 'Version',
    size: 'Size',
    assemblies: 'Assemblies',
    filterAssemblies: 'Filter assemblies by keyword',
    memoryFree: 'Free Memory',
    memoryFreeSuccess: 'Memory freed',
    restart: 'Restart',
    restartConfirm: 'Restarting will interrupt all current requests. Continue?',
    restartTitle: 'Confirm Restart',
    assemblyName: 'Name',
    assemblyTitle: 'Title',
    fileVersion: 'File Version',
    assemblyVersion: 'Assembly Version',
    compileTime: 'Compile Time',
    location: 'Location',
    menuTree: 'Menu Tree',
    selectModule: 'Select Module',
    all: 'All',
    hidden: 'Hidden',
    newWindow: 'New Window',
    noServerVars: 'No server variables',
    noMenu: 'No menu',
    noData: 'No data',
    labelSystem: 'System',
    labelPath: 'Path',
    labelHost: 'Host',
    labelLocal: 'Local Address',
    labelRemote: 'Remote Address',
    labelApplication: 'Application',
    labelApplicationTitle: 'Application Title',
    labelVersion: 'Version',
    labelOs: 'OS',
    labelOsVersion: 'OS Version',
    labelMachineId: 'Machine ID',
    labelMachineProduct: 'Machine Product',
    labelCpu: 'CPU',
    labelOpenTime: 'Start Time',
    labelServerTime: 'Server Time',
    labelMemory: 'Memory',
    labelProcessTime: 'Process Time',
    labelGc: 'GC'
  },
  widgetAdmin: {
    title: 'Workbench Widgets',
    subtitle: 'Manage groups, order and enabled state of workbench cards',
    groupOrder: 'Group Order',
    group: 'Group',
    widgetName: 'Widget',
    cols: 'Columns',
    sort: 'Sort',
    enable: 'Enabled',
    itemOrder: 'Widget Order',
    moveUp: 'Move Up',
    moveDown: 'Move Down',
    adminOnly: 'Admin Only'
  },
  lovAdmin: {
    title: 'Value Sets',
    subtitle: 'Maintain enum and list value sets',
    add: 'Add Value Set',
    addTitle: 'Add Value Set',
    editTitle: 'Edit Value Set',
    keyword: 'Keyword',
    keywordHint: 'Search by code or name',
    type: 'Type',
    all: 'All',
    typeEnum: 'Enum',
    typeList: 'List',
    typeEnumFull: 'Enum (ENUM)',
    typeListFull: 'List (LIST)',
    code: 'Code',
    codeRequired: 'Please enter the value set code',
    codeTip: 'Full code = prefix + semantic name, e.g. Enum.OrderStatus or List.Department',
    displayName: 'Display Name',
    displayNameHint: 'Display name of the value set',
    valueField: 'Value Field',
    valueFieldHint: 'e.g. userId',
    labelField: 'Label Field',
    labelFieldHint: 'e.g. userName',
    source: 'Source',
    sourceAuto: 'Auto',
    sourceManual: 'Manual',
    enabled: 'Enabled',
    createTime: 'Created At',
    remark: 'Remark',
    enumValues: 'Enum Values',
    listConfig: 'List Config',
    lovType: 'Value Set Type',
    deleteConfirm: 'Delete value set "{code}"?',
    confirmDelete: 'Confirm Delete',
    loadFailed: 'Failed to load value sets',
    saveFailed: 'Failed to save value set',
    deleteFailed: 'Failed to delete value set'
  },
  lovConfig: {
    title: 'Value Set Config — {code}',
    enumItemsTab: 'Enum Values',
    autoManagedTip: 'This value set is managed by code (C# enum); items cannot be edited manually',
    valueColumn: 'Value',
    labelColumn: 'Label',
    addEnumItem: '+ Add Enum Value',
    requestUrl: 'Request URL',
    requestUrlTip: 'Backend only, never sent to the frontend',
    method: 'Method',
    proxyRequest: 'Proxy Request',
    proxyRequestTip:
      'When enabled, the backend proxies the external data source (hidden from the frontend, avoids CORS); otherwise the frontend requests the URL directly.',
    sameAppUrlTip: 'URL starts with / (same origin), direct request is enforced and proxy is disabled.',
    pageable: 'Paginated',
    pageNumField: 'Page Number Field',
    pageSizeField: 'Page Size Field',
    dataPath: 'Data Path',
    dataPathTip: 'JSON path of the data list, e.g. data.list or just data',
    totalPath: 'Total Path',
    totalPathTip: 'JSON path of the total count, e.g. page.totalCount',
    fixedParams: 'Fixed Params (JSON)',
    searchFieldsTab: 'Search Fields',
    fieldName: 'Field',
    displayTitle: 'Title',
    controlType: 'Control',
    refLov: 'Ref Value Set',
    addSearchField: '+ Add Search Field',
    editSearchField: 'Edit Search Field',
    fieldNameHint: 'e.g. deptName',
    titleHint: 'e.g. Department',
    controlInput: 'Input',
    controlSelect: 'Select',
    controlLov: 'Value Set (lov)',
    refLovHint: 'e.g. Enum.Department',
    paramType: 'Param Type',
    required: 'Required',
    defaultValue: 'Default Value',
    tableColumnsTab: 'Table Columns',
    columnTitle: 'Title',
    columnWidth: 'Width',
    columnAlign: 'Align',
    addColumn: '+ Add Column',
    editColumn: 'Edit Column',
    fieldNameHintStatus: 'e.g. status',
    titleHintStatus: 'e.g. Status',
    columnWidthPx: 'Width (px)',
    align: 'Alignment',
    alignLeft: 'Left',
    alignCenter: 'Center',
    alignRight: 'Right',
    sortable: 'Sortable',
    refLovHintStatus: 'e.g. Enum.Status',
    refLovTip: 'Translate column values automatically when selected',
    formatType: 'Format Type',
    formatTypeHint: 'Mutually exclusive with ref value set',
    saveConfig: 'Save Config',
    loadFailed: 'Failed to load config',
    saveSuccess: 'Saved',
    saveFailed: 'Failed to save',
    sortLabel: 'Sort',
    enabledLabel: 'Enabled'
  },
  form: {
    required: 'Cannot be empty',
    userName: {
      required: 'Please enter user name',
      invalid: 'User name format is incorrect'
    },
    phone: {
      required: 'Please enter phone number',
      invalid: 'Phone number format is incorrect'
    },
    pwd: {
      required: 'Please enter password',
      invalid: '6-18 characters, including letters, numbers, and underscores'
    },
    confirmPwd: {
      required: 'Please enter password again',
      invalid: 'The two passwords are inconsistent'
    },
    code: {
      required: 'Please enter verification code',
      invalid: 'Verification code format is incorrect'
    },
    email: {
      required: 'Please enter email',
      invalid: 'Email format is incorrect'
    }
  },
  dropdown: {
    closeCurrent: 'Close Current',
    closeOther: 'Close Other',
    closeLeft: 'Close Left',
    closeRight: 'Close Right',
    closeAll: 'Close All'
  },
  icon: {
    themeConfig: 'Theme Configuration',
    themeSchema: 'Theme Schema',
    lang: 'Switch Language',
    fullscreen: 'Fullscreen',
    fullscreenExit: 'Exit Fullscreen',
    reload: 'Reload Page',
    collapse: 'Collapse Menu',
    expand: 'Expand Menu',
    pin: 'Pin',
    unpin: 'Unpin'
  },
  datatable: {
    itemCount: 'Total {total} items'
  }
};

export default local;
