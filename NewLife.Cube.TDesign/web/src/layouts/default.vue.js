import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ViewListIcon, SunnyIcon, MoonIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
const appStore = useAppStore();
const userStore = useUserStore();
const router = useRouter();
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
onMounted(async () => {
    try {
        const res = await api.user.getSiteInfo();
        if (res?.data)
            appStore.siteInfo = res.data;
    }
    catch { /* ignore */ }
    await userStore.fetchUserInfo();
    if (!userStore.isLoggedIn) {
        router.push('/login');
        return;
    }
    await userStore.fetchMenus();
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['t-layout']} */ ;
// CSS variable injection 
// CSS variable injection end 
const __VLS_0 = {}.TLayout;
/** @type {[typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({}));
const __VLS_2 = __VLS_1({}, ...__VLS_functionalComponentArgsRest(__VLS_1));
var __VLS_4 = {};
__VLS_3.slots.default;
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
const __VLS_29 = {}.TLayout;
/** @type {[typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, typeof __VLS_components.TLayout, typeof __VLS_components.tLayout, ]} */ ;
// @ts-ignore
const __VLS_30 = __VLS_asFunctionalComponent(__VLS_29, new __VLS_29({}));
const __VLS_31 = __VLS_30({}, ...__VLS_functionalComponentArgsRest(__VLS_30));
__VLS_32.slots.default;
const __VLS_33 = {}.THeader;
/** @type {[typeof __VLS_components.THeader, typeof __VLS_components.tHeader, typeof __VLS_components.THeader, typeof __VLS_components.tHeader, ]} */ ;
// @ts-ignore
const __VLS_34 = __VLS_asFunctionalComponent(__VLS_33, new __VLS_33({
    ...{ style: {} },
}));
const __VLS_35 = __VLS_34({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_34));
__VLS_36.slots.default;
const __VLS_37 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_38 = __VLS_asFunctionalComponent(__VLS_37, new __VLS_37({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}));
const __VLS_39 = __VLS_38({
    ...{ 'onClick': {} },
    theme: "default",
    variant: "text",
    shape: "square",
}, ...__VLS_functionalComponentArgsRest(__VLS_38));
let __VLS_41;
let __VLS_42;
let __VLS_43;
const __VLS_44 = {
    onClick: (...[$event]) => {
        __VLS_ctx.appStore.collapsed = !__VLS_ctx.appStore.collapsed;
    }
};
__VLS_40.slots.default;
{
    const { icon: __VLS_thisSlot } = __VLS_40.slots;
    const __VLS_45 = {}.ViewListIcon;
    /** @type {[typeof __VLS_components.ViewListIcon, typeof __VLS_components.viewListIcon, ]} */ ;
    // @ts-ignore
    const __VLS_46 = __VLS_asFunctionalComponent(__VLS_45, new __VLS_45({}));
    const __VLS_47 = __VLS_46({}, ...__VLS_functionalComponentArgsRest(__VLS_46));
}
var __VLS_40;
const __VLS_49 = {}.TBreadcrumb;
/** @type {[typeof __VLS_components.TBreadcrumb, typeof __VLS_components.tBreadcrumb, typeof __VLS_components.TBreadcrumb, typeof __VLS_components.tBreadcrumb, ]} */ ;
// @ts-ignore
const __VLS_50 = __VLS_asFunctionalComponent(__VLS_49, new __VLS_49({
    ...{ style: {} },
}));
const __VLS_51 = __VLS_50({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_50));
__VLS_52.slots.default;
const __VLS_53 = {}.TBreadcrumbItem;
/** @type {[typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, ]} */ ;
// @ts-ignore
const __VLS_54 = __VLS_asFunctionalComponent(__VLS_53, new __VLS_53({
    to: "/",
}));
const __VLS_55 = __VLS_54({
    to: "/",
}, ...__VLS_functionalComponentArgsRest(__VLS_54));
__VLS_56.slots.default;
var __VLS_56;
if (__VLS_ctx.$route.path !== '/') {
    const __VLS_57 = {}.TBreadcrumbItem;
    /** @type {[typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, typeof __VLS_components.TBreadcrumbItem, typeof __VLS_components.tBreadcrumbItem, ]} */ ;
    // @ts-ignore
    const __VLS_58 = __VLS_asFunctionalComponent(__VLS_57, new __VLS_57({}));
    const __VLS_59 = __VLS_58({}, ...__VLS_functionalComponentArgsRest(__VLS_58));
    __VLS_60.slots.default;
    (__VLS_ctx.$route.path);
    var __VLS_60;
}
var __VLS_52;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div)({
    ...{ style: {} },
});
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
    onClick: (__VLS_ctx.appStore.toggleDark)
};
__VLS_64.slots.default;
{
    const { icon: __VLS_thisSlot } = __VLS_64.slots;
    if (__VLS_ctx.appStore.darkMode) {
        const __VLS_69 = {}.SunnyIcon;
        /** @type {[typeof __VLS_components.SunnyIcon, typeof __VLS_components.sunnyIcon, ]} */ ;
        // @ts-ignore
        const __VLS_70 = __VLS_asFunctionalComponent(__VLS_69, new __VLS_69({}));
        const __VLS_71 = __VLS_70({}, ...__VLS_functionalComponentArgsRest(__VLS_70));
    }
    else {
        const __VLS_73 = {}.MoonIcon;
        /** @type {[typeof __VLS_components.MoonIcon, typeof __VLS_components.moonIcon, ]} */ ;
        // @ts-ignore
        const __VLS_74 = __VLS_asFunctionalComponent(__VLS_73, new __VLS_73({}));
        const __VLS_75 = __VLS_74({}, ...__VLS_functionalComponentArgsRest(__VLS_74));
    }
}
var __VLS_64;
const __VLS_77 = {}.TDropdown;
/** @type {[typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, typeof __VLS_components.TDropdown, typeof __VLS_components.tDropdown, ]} */ ;
// @ts-ignore
const __VLS_78 = __VLS_asFunctionalComponent(__VLS_77, new __VLS_77({
    ...{ 'onClick': {} },
    options: (__VLS_ctx.userDropdown),
    trigger: "click",
    ...{ style: {} },
}));
const __VLS_79 = __VLS_78({
    ...{ 'onClick': {} },
    options: (__VLS_ctx.userDropdown),
    trigger: "click",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_78));
let __VLS_81;
let __VLS_82;
let __VLS_83;
const __VLS_84 = {
    onClick: (__VLS_ctx.handleUserAction)
};
__VLS_80.slots.default;
const __VLS_85 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_86 = __VLS_asFunctionalComponent(__VLS_85, new __VLS_85({
    theme: "default",
    variant: "text",
}));
const __VLS_87 = __VLS_86({
    theme: "default",
    variant: "text",
}, ...__VLS_functionalComponentArgsRest(__VLS_86));
__VLS_88.slots.default;
(__VLS_ctx.userStore.displayName);
var __VLS_88;
var __VLS_80;
var __VLS_36;
const __VLS_89 = {}.TContent;
/** @type {[typeof __VLS_components.TContent, typeof __VLS_components.tContent, typeof __VLS_components.TContent, typeof __VLS_components.tContent, ]} */ ;
// @ts-ignore
const __VLS_90 = __VLS_asFunctionalComponent(__VLS_89, new __VLS_89({
    ...{ style: {} },
}));
const __VLS_91 = __VLS_90({
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_90));
__VLS_92.slots.default;
const __VLS_93 = {}.RouterView;
/** @type {[typeof __VLS_components.RouterView, typeof __VLS_components.routerView, ]} */ ;
// @ts-ignore
const __VLS_94 = __VLS_asFunctionalComponent(__VLS_93, new __VLS_93({}));
const __VLS_95 = __VLS_94({}, ...__VLS_functionalComponentArgsRest(__VLS_94));
var __VLS_92;
var __VLS_32;
var __VLS_3;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            ViewListIcon: ViewListIcon,
            SunnyIcon: SunnyIcon,
            MoonIcon: MoonIcon,
            appStore: appStore,
            userStore: userStore,
            userDropdown: userDropdown,
            buildUrl: buildUrl,
            handleUserAction: handleUserAction,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
