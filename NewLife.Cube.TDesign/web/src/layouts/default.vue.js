import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { closeAllTabs, closeTab, createTab, openTab, shouldAutoCollapseSidebar } from '@newlifex/cube';
import { ViewListIcon, SunnyIcon, MoonIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
const appStore = useAppStore();
const userStore = useUserStore();
// --- 多标签页 ---
const tabs = ref([]);
const activeTabId = ref('');
function syncTab() {
    const tab = createTab(route.path, route.fullPath, route.meta.title || route.path);
    tabs.value = openTab(tabs.value, tab);
    activeTabId.value = tab.id;
}
function activateTab(tab) {
    if (tab.id === activeTabId.value)
        return;
    void router.push(tab.fullPath);
}
function closeTabAndNavigate(id) {
    const result = closeTab(tabs.value, id, activeTabId.value);
    tabs.value = result.tabs;
    if (result.nextActiveId) {
        activeTabId.value = result.nextActiveId;
        const next = tabs.value.find((t) => t.id === result.nextActiveId);
        if (next)
            void router.push(next.fullPath);
    }
}
function closeAll() {
    tabs.value = closeAllTabs(tabs.value);
    const home = tabs.value[0];
    if (home) {
        activeTabId.value = home.id;
        void router.push(home.fullPath);
    }
}
watch(() => route.fullPath, syncTab, { immediate: true });
const router = useRouter();
const route = useRoute();
const userDropdown = [{ content: '退出登录', value: 'logout' }];
function buildUrl(item) {
    return item.url?.startsWith('/') ? item.url : `/${item.url ?? ''}`;
}
function handleUserAction(data) {
    if (data.value === 'logout') {
        userStore.logout();
        router.push('/login');
    }
}
// 窄屏使用抽屉菜单；宽屏恢复用户手动展开
const isNarrow = ref(false);
const mobileMenu = ref(false);
let autoCollapsed = false;
function syncResponsive() {
    const narrow = shouldAutoCollapseSidebar(window.innerWidth);
    isNarrow.value = narrow;
    if (narrow) {
        if (!appStore.collapsed) {
            appStore.collapsed = true;
            autoCollapsed = true;
        }
    }
    else {
        mobileMenu.value = false;
        if (autoCollapsed) {
            appStore.collapsed = false;
            autoCollapsed = false;
        }
    }
}
watch(() => router.currentRoute.value.fullPath, () => {
    mobileMenu.value = false;
});
onMounted(async () => {
    syncResponsive();
    window.addEventListener('resize', syncResponsive);
    try {
        const res = await api.site.getSite();
        if (res?.data)
            appStore.siteConfig = res.data;
    }
    catch { /* ignore */ }
    await userStore.fetchUserInfo();
    if (!userStore.isLoggedIn) {
        router.push('/login');
        return;
    }
    await userStore.fetchMenus();
});
onBeforeUnmount(() => window.removeEventListener('resize', syncResponsive));
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['t-layout']} */ ;
/** @type {__VLS_StyleScopedClasses['layout-content']} */ ;
// CSS variable injection 
// CSS variable injection end 
const __VLS_0 = {}.TLayout;
/** @type {[typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({}));
const __VLS_2 = __VLS_1({}, ...__VLS_functionalComponentArgsRest(__VLS_1));
var __VLS_4 = {};
__VLS_3.slots.default;
if (!__VLS_ctx.isNarrow) {
    const __VLS_5 = {}.TAside;
    /** @type {[typeof __VLS_components.TAside, typeof __VLS_components.tAside, typeof __VLS_components.TAside, typeof __VLS_components.tAside, ]} */ ;
    // @ts-ignore
    const __VLS_6 = __VLS_asFunctionalComponent(__VLS_5, new __VLS_5({
        width: (__VLS_ctx.appStore.collapsed ? '64px' : '232px'),
        ...{ style: {} },
    }));
    const __VLS_7 = __VLS_6({
        width: (__VLS_ctx.appStore.collapsed ? '64px' : '232px'),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_6));
    __VLS_8.slots.default;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    if (!__VLS_ctx.appStore.collapsed) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.appStore.siteTitle);
    }
    const __VLS_9 = {}.TMenu;
    /** @type {[typeof __VLS_components.TMenu, typeof __VLS_components.tMenu, typeof __VLS_components.TMenu, typeof __VLS_components.tMenu, ]} */ ;
    // @ts-ignore
    const __VLS_10 = __VLS_asFunctionalComponent(__VLS_9, new __VLS_9({
        ...{ 'onChange': {} },
        collapsed: (__VLS_ctx.appStore.collapsed),
        value: (__VLS_ctx.$route.path),
        expandMutex: true,
    }));
    const __VLS_11 = __VLS_10({
        ...{ 'onChange': {} },
        collapsed: (__VLS_ctx.appStore.collapsed),
        value: (__VLS_ctx.$route.path),
        expandMutex: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_10));
    let __VLS_13;
    let __VLS_14;
    let __VLS_15;
    const __VLS_16 = {
        onChange: ((v) => __VLS_ctx.$router.push(v))
    };
    __VLS_12.slots.default;
    for (const [item] of __VLS_getVForSourceType((__VLS_ctx.userStore.menus))) {
        (item.name);
        if (item.children?.length) {
            const __VLS_17 = {}.TSubmenu;
            /** @type {[typeof __VLS_components.TSubmenu, typeof __VLS_components.tSubmenu, typeof __VLS_components.TSubmenu, typeof __VLS_components.tSubmenu, ]} */ ;
            // @ts-ignore
            const __VLS_18 = __VLS_asFunctionalComponent(__VLS_17, new __VLS_17({
                value: (item.url || item.name),
                title: (item.displayName || item.name),
            }));
            const __VLS_19 = __VLS_18({
                value: (item.url || item.name),
                title: (item.displayName || item.name),
            }, ...__VLS_functionalComponentArgsRest(__VLS_18));
            __VLS_20.slots.default;
            for (const [child] of __VLS_getVForSourceType((item.children))) {
                const __VLS_21 = {}.TMenuItem;
                /** @type {[typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, ]} */ ;
                // @ts-ignore
                const __VLS_22 = __VLS_asFunctionalComponent(__VLS_21, new __VLS_21({
                    key: (child.name),
                    value: (__VLS_ctx.buildUrl(child)),
                }));
                const __VLS_23 = __VLS_22({
                    key: (child.name),
                    value: (__VLS_ctx.buildUrl(child)),
                }, ...__VLS_functionalComponentArgsRest(__VLS_22));
                __VLS_24.slots.default;
                (child.displayName || child.name);
                var __VLS_24;
            }
            var __VLS_20;
        }
        else {
            const __VLS_25 = {}.TMenuItem;
            /** @type {[typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, ]} */ ;
            // @ts-ignore
            const __VLS_26 = __VLS_asFunctionalComponent(__VLS_25, new __VLS_25({
                value: (__VLS_ctx.buildUrl(item)),
            }));
            const __VLS_27 = __VLS_26({
                value: (__VLS_ctx.buildUrl(item)),
            }, ...__VLS_functionalComponentArgsRest(__VLS_26));
            __VLS_28.slots.default;
            (item.displayName || item.name);
            var __VLS_28;
        }
    }
    var __VLS_12;
    var __VLS_8;
}
const __VLS_29 = {}.TDrawer;
/** @type {[typeof __VLS_components.TDrawer, typeof __VLS_components.tDrawer, typeof __VLS_components.TDrawer, typeof __VLS_components.tDrawer, ]} */ ;
// @ts-ignore
const __VLS_30 = __VLS_asFunctionalComponent(__VLS_29, new __VLS_29({
    visible: (__VLS_ctx.mobileMenu),
    placement: "left",
    size: "232px",
    footer: (false),
    header: (__VLS_ctx.appStore.siteTitle),
}));
const __VLS_31 = __VLS_30({
    visible: (__VLS_ctx.mobileMenu),
    placement: "left",
    size: "232px",
    footer: (false),
    header: (__VLS_ctx.appStore.siteTitle),
}, ...__VLS_functionalComponentArgsRest(__VLS_30));
__VLS_32.slots.default;
const __VLS_33 = {}.TMenu;
/** @type {[typeof __VLS_components.TMenu, typeof __VLS_components.tMenu, typeof __VLS_components.TMenu, typeof __VLS_components.tMenu, ]} */ ;
// @ts-ignore
const __VLS_34 = __VLS_asFunctionalComponent(__VLS_33, new __VLS_33({
    ...{ 'onChange': {} },
    value: (__VLS_ctx.$route.path),
    expandMutex: true,
}));
const __VLS_35 = __VLS_34({
    ...{ 'onChange': {} },
    value: (__VLS_ctx.$route.path),
    expandMutex: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_34));
let __VLS_37;
let __VLS_38;
let __VLS_39;
const __VLS_40 = {
    onChange: ((v) => __VLS_ctx.$router.push(v))
};
__VLS_36.slots.default;
for (const [item] of __VLS_getVForSourceType((__VLS_ctx.userStore.menus))) {
    (item.name);
    if (item.children?.length) {
        const __VLS_41 = {}.TSubmenu;
        /** @type {[typeof __VLS_components.TSubmenu, typeof __VLS_components.tSubmenu, typeof __VLS_components.TSubmenu, typeof __VLS_components.tSubmenu, ]} */ ;
        // @ts-ignore
        const __VLS_42 = __VLS_asFunctionalComponent(__VLS_41, new __VLS_41({
            value: (item.url || item.name),
            title: (item.displayName || item.name),
        }));
        const __VLS_43 = __VLS_42({
            value: (item.url || item.name),
            title: (item.displayName || item.name),
        }, ...__VLS_functionalComponentArgsRest(__VLS_42));
        __VLS_44.slots.default;
        for (const [child] of __VLS_getVForSourceType((item.children))) {
            const __VLS_45 = {}.TMenuItem;
            /** @type {[typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, ]} */ ;
            // @ts-ignore
            const __VLS_46 = __VLS_asFunctionalComponent(__VLS_45, new __VLS_45({
                key: (child.name),
                value: (__VLS_ctx.buildUrl(child)),
            }));
            const __VLS_47 = __VLS_46({
                key: (child.name),
                value: (__VLS_ctx.buildUrl(child)),
            }, ...__VLS_functionalComponentArgsRest(__VLS_46));
            __VLS_48.slots.default;
            (child.displayName || child.name);
            var __VLS_48;
        }
        var __VLS_44;
    }
    else {
        const __VLS_49 = {}.TMenuItem;
        /** @type {[typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, typeof __VLS_components.TMenuItem, typeof __VLS_components.tMenuItem, ]} */ ;
        // @ts-ignore
        const __VLS_50 = __VLS_asFunctionalComponent(__VLS_49, new __VLS_49({
            value: (__VLS_ctx.buildUrl(item)),
        }));
        const __VLS_51 = __VLS_50({
            value: (__VLS_ctx.buildUrl(item)),
        }, ...__VLS_functionalComponentArgsRest(__VLS_50));
        __VLS_52.slots.default;
        (item.displayName || item.name);
        var __VLS_52;
    }
}
var __VLS_36;
var __VLS_32;
const __VLS_53 = {}.TLayout;
/** @type {[typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, ]} */ ;
// @ts-ignore
const __VLS_54 = __VLS_asFunctionalComponent(__VLS_53, new __VLS_53({}));
const __VLS_55 = __VLS_54({}, ...__VLS_functionalComponentArgsRest(__VLS_54));
__VLS_56.slots.default;
const __VLS_57 = {}.THeader;
/** @type {[typeof __VLS_components.THeader, typeof __VLS_components.tHeader, typeof __VLS_components.THeader, typeof __VLS_components.tHeader, ]} */ ;
// @ts-ignore
const __VLS_58 = __VLS_asFunctionalComponent(__VLS_57, new __VLS_57({
    ...{ style: {} },
}));
const __VLS_59 = __VLS_58({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_58));
__VLS_60.slots.default;
const __VLS_61 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_62 = __VLS_asFunctionalComponent(__VLS_61, new __VLS_61({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}));
const __VLS_63 = __VLS_62({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}, ...__VLS_functionalComponentArgsRest(__VLS_62));
let __VLS_65;
let __VLS_66;
let __VLS_67;
const __VLS_68 = {
    onClick: (...[$event]) => {
        __VLS_ctx.isNarrow ? (__VLS_ctx.mobileMenu = true) : (__VLS_ctx.appStore.collapsed = !__VLS_ctx.appStore.collapsed);
    }
};
__VLS_64.slots.default;
{
    const { icon: __VLS_thisSlot } = __VLS_64.slots;
    const __VLS_69 = {}.ViewListIcon;
    /** @type {[typeof __VLS_components.ViewListIcon, typeof __VLS_components.viewListIcon, ]} */ ;
    // @ts-ignore
    const __VLS_70 = __VLS_asFunctionalComponent(__VLS_69, new __VLS_69({}));
    const __VLS_71 = __VLS_70({}, ...__VLS_functionalComponentArgsRest(__VLS_70));
}
var __VLS_64;
const __VLS_73 = {}.TBreadcrumb;
/** @type {[typeof __VLS_components.TBreadcrumb, typeof __VLS_components.tBreadcrumb, typeof __VLS_components.TBreadcrumb, typeof __VLS_components.tBreadcrumb, ]} */ ;
// @ts-ignore
const __VLS_74 = __VLS_asFunctionalComponent(__VLS_73, new __VLS_73({
    ...{ style: {} },
}));
const __VLS_75 = __VLS_74({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_74));
__VLS_76.slots.default;
const __VLS_77 = {}.TBreadcrumbItem;
/** @type {[typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, ]} */ ;
// @ts-ignore
const __VLS_78 = __VLS_asFunctionalComponent(__VLS_77, new __VLS_77({
    to: "/",
}));
const __VLS_79 = __VLS_78({
    to: "/",
}, ...__VLS_functionalComponentArgsRest(__VLS_78));
__VLS_80.slots.default;
var __VLS_80;
if (__VLS_ctx.$route.path !== '/') {
    const __VLS_81 = {}.TBreadcrumbItem;
    /** @type {[typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, ]} */ ;
    // @ts-ignore
    const __VLS_82 = __VLS_asFunctionalComponent(__VLS_81, new __VLS_81({}));
    const __VLS_83 = __VLS_82({}, ...__VLS_functionalComponentArgsRest(__VLS_82));
    __VLS_84.slots.default;
    (__VLS_ctx.$route.path);
    var __VLS_84;
}
var __VLS_76;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_85 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_86 = __VLS_asFunctionalComponent(__VLS_85, new __VLS_85({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}));
const __VLS_87 = __VLS_86({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}, ...__VLS_functionalComponentArgsRest(__VLS_86));
let __VLS_89;
let __VLS_90;
let __VLS_91;
const __VLS_92 = {
    onClick: (__VLS_ctx.appStore.toggleDark)
};
__VLS_88.slots.default;
{
    const { icon: __VLS_thisSlot } = __VLS_88.slots;
    if (__VLS_ctx.appStore.darkMode) {
        const __VLS_93 = {}.SunnyIcon;
        /** @type {[typeof __VLS_components.SunnyIcon, typeof __VLS_components.sunnyIcon, ]} */ ;
        // @ts-ignore
        const __VLS_94 = __VLS_asFunctionalComponent(__VLS_93, new __VLS_93({}));
        const __VLS_95 = __VLS_94({}, ...__VLS_functionalComponentArgsRest(__VLS_94));
    }
    else {
        const __VLS_97 = {}.MoonIcon;
        /** @type {[typeof __VLS_components.MoonIcon, typeof __VLS_components.moonIcon, ]} */ ;
        // @ts-ignore
        const __VLS_98 = __VLS_asFunctionalComponent(__VLS_97, new __VLS_97({}));
        const __VLS_99 = __VLS_98({}, ...__VLS_functionalComponentArgsRest(__VLS_98));
    }
}
var __VLS_88;
const __VLS_101 = {}.TDropdown;
/** @type {[typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, ]} */ ;
// @ts-ignore
const __VLS_102 = __VLS_asFunctionalComponent(__VLS_101, new __VLS_101({
    ...{ 'onClick': {} },
    options: (__VLS_ctx.userDropdown),
    trigger: "click",
    ...{ style: {} },
}));
const __VLS_103 = __VLS_102({
    ...{ 'onClick': {} },
    options: (__VLS_ctx.userDropdown),
    trigger: "click",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_102));
let __VLS_105;
let __VLS_106;
let __VLS_107;
const __VLS_108 = {
    onClick: (__VLS_ctx.handleUserAction)
};
__VLS_104.slots.default;
const __VLS_109 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_110 = __VLS_asFunctionalComponent(__VLS_109, new __VLS_109({
    theme: "default",
    variant: "text",
}));
const __VLS_111 = __VLS_110({
    theme: "default",
    variant: "text",
}, ...__VLS_functionalComponentArgsRest(__VLS_110));
__VLS_112.slots.default;
(__VLS_ctx.userStore.displayName);
var __VLS_112;
var __VLS_104;
var __VLS_60;
const __VLS_113 = {}.TContent;
/** @type {[typeof __VLS_components.TContent, typeof __VLS_components.tContent, typeof __VLS_components.TContent, typeof __VLS_components.tContent, ]} */ ;
// @ts-ignore
const __VLS_114 = __VLS_asFunctionalComponent(__VLS_113, new __VLS_113({
    ...{ class: "layout-content" },
}));
const __VLS_115 = __VLS_114({
    ...{ class: "layout-content" },
}, ...__VLS_functionalComponentArgsRest(__VLS_114));
__VLS_116.slots.default;
if (__VLS_ctx.tabs.length > 1) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "tab-strip" },
    });
    for (const [t] of __VLS_getVForSourceType((__VLS_ctx.tabs))) {
        const __VLS_117 = {}.TTag;
        /** @type {[typeof __VLS_components.TTag, typeof __VLS_components.tTag, typeof __VLS_components.TTag, typeof __VLS_components.tTag, ]} */ ;
        // @ts-ignore
        const __VLS_118 = __VLS_asFunctionalComponent(__VLS_117, new __VLS_117({
            ...{ 'onClick': {} },
            key: (t.id),
            theme: (t.id === __VLS_ctx.activeTabId ? 'primary' : 'default'),
        }));
        const __VLS_119 = __VLS_118({
            ...{ 'onClick': {} },
            key: (t.id),
            theme: (t.id === __VLS_ctx.activeTabId ? 'primary' : 'default'),
        }, ...__VLS_functionalComponentArgsRest(__VLS_118));
        let __VLS_121;
        let __VLS_122;
        let __VLS_123;
        const __VLS_124 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.tabs.length > 1))
                    return;
                __VLS_ctx.activateTab(t);
            }
        };
        __VLS_120.slots.default;
        (t.title);
        var __VLS_120;
    }
    const __VLS_125 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_126 = __VLS_asFunctionalComponent(__VLS_125, new __VLS_125({
        ...{ 'onClick': {} },
        variant: "text",
        size: "small",
    }));
    const __VLS_127 = __VLS_126({
        ...{ 'onClick': {} },
        variant: "text",
        size: "small",
    }, ...__VLS_functionalComponentArgsRest(__VLS_126));
    let __VLS_129;
    let __VLS_130;
    let __VLS_131;
    const __VLS_132 = {
        onClick: (__VLS_ctx.closeAll)
    };
    __VLS_128.slots.default;
    var __VLS_128;
}
const __VLS_133 = {}.RouterView;
/** @type {[typeof __VLS_components.RouterView, typeof __VLS_components.routerView, ]} */ ;
// @ts-ignore
const __VLS_134 = __VLS_asFunctionalComponent(__VLS_133, new __VLS_133({}));
const __VLS_135 = __VLS_134({}, ...__VLS_functionalComponentArgsRest(__VLS_134));
var __VLS_116;
var __VLS_56;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['layout-content']} */ ;
/** @type {__VLS_StyleScopedClasses['tab-strip']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            ViewListIcon: ViewListIcon,
            SunnyIcon: SunnyIcon,
            MoonIcon: MoonIcon,
            appStore: appStore,
            userStore: userStore,
            tabs: tabs,
            activeTabId: activeTabId,
            activateTab: activateTab,
            closeAll: closeAll,
            userDropdown: userDropdown,
            buildUrl: buildUrl,
            handleUserAction: handleUserAction,
            isNarrow: isNarrow,
            mobileMenu: mobileMenu,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
