import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { UserIcon, LockOnIcon } from 'tdesign-icons-vue-next';
import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
import { api } from '@/api';
const appStore = useAppStore();
const userStore = useUserStore();
const router = useRouter();
const username = ref('');
const password = ref('');
const loading = ref(false);
const error = ref('');
const loginConfig = ref(null);
onMounted(async () => {
    try {
        const [siteRes, configRes] = await Promise.all([
            api.user.getSiteInfo(),
            api.user.getLoginConfig(),
        ]);
        if (siteRes?.data)
            appStore.siteInfo = siteRes.data;
        loginConfig.value = configRes?.data ?? null;
    }
    catch { /* ignore */ }
});
async function handleLogin() {
    if (!username.value || !password.value) {
        error.value = '请输入用户名和密码';
        return;
    }
    loading.value = true;
    error.value = '';
    try {
        const ok = await userStore.login(username.value, password.value);
        if (ok) {
            await userStore.fetchMenus();
            router.push('/');
        }
        else {
            error.value = '用户名或密码错误';
        }
    }
    catch {
        error.value = '登录失败，请检查网络';
    }
    finally {
        loading.value = false;
    }
}
function goOAuth(name) {
    window.location.href = `/Sso/Login?name=${encodeURIComponent(name)}`;
}
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ style: {} },
});
const __VLS_0 = {}.TCard;
/** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ style: {} },
    bordered: (false),
}));
const __VLS_2 = __VLS_1({
    ...{ style: {} },
    bordered: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_3.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({
        ...{ style: {} },
    });
    (__VLS_ctx.appStore.siteTitle);
}
if (__VLS_ctx.error) {
    const __VLS_4 = {}.TAlert;
    /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        theme: "error",
        message: (__VLS_ctx.error),
        ...{ style: {} },
        close: true,
    }));
    const __VLS_6 = __VLS_5({
        theme: "error",
        message: (__VLS_ctx.error),
        ...{ style: {} },
        close: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
}
const __VLS_8 = {}.TForm;
/** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    ...{ 'onSubmit': {} },
    labelWidth: (0),
}));
const __VLS_10 = __VLS_9({
    ...{ 'onSubmit': {} },
    labelWidth: (0),
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
let __VLS_12;
let __VLS_13;
let __VLS_14;
const __VLS_15 = {
    onSubmit: (__VLS_ctx.handleLogin)
};
__VLS_11.slots.default;
const __VLS_16 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({}));
const __VLS_18 = __VLS_17({}, ...__VLS_functionalComponentArgsRest(__VLS_17));
__VLS_19.slots.default;
const __VLS_20 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
    modelValue: (__VLS_ctx.username),
    placeholder: "请输入用户名",
    size: "large",
    clearable: true,
}));
const __VLS_22 = __VLS_21({
    modelValue: (__VLS_ctx.username),
    placeholder: "请输入用户名",
    size: "large",
    clearable: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_21));
__VLS_23.slots.default;
{
    const { 'prefix-icon': __VLS_thisSlot } = __VLS_23.slots;
    const __VLS_24 = {}.UserIcon;
    /** @type {[typeof __VLS_components.UserIcon, typeof __VLS_components.userIcon, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({}));
    const __VLS_26 = __VLS_25({}, ...__VLS_functionalComponentArgsRest(__VLS_25));
}
var __VLS_23;
var __VLS_19;
const __VLS_28 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({}));
const __VLS_30 = __VLS_29({}, ...__VLS_functionalComponentArgsRest(__VLS_29));
__VLS_31.slots.default;
const __VLS_32 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
    modelValue: (__VLS_ctx.password),
    type: "password",
    placeholder: "请输入密码",
    size: "large",
    clearable: true,
}));
const __VLS_34 = __VLS_33({
    modelValue: (__VLS_ctx.password),
    type: "password",
    placeholder: "请输入密码",
    size: "large",
    clearable: true,
}, ...__VLS_functionalComponentArgsRest(__VLS_33));
__VLS_35.slots.default;
{
    const { 'prefix-icon': __VLS_thisSlot } = __VLS_35.slots;
    const __VLS_36 = {}.LockOnIcon;
    /** @type {[typeof __VLS_components.LockOnIcon, typeof __VLS_components.lockOnIcon, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({}));
    const __VLS_38 = __VLS_37({}, ...__VLS_functionalComponentArgsRest(__VLS_37));
}
var __VLS_35;
var __VLS_31;
const __VLS_40 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({}));
const __VLS_42 = __VLS_41({}, ...__VLS_functionalComponentArgsRest(__VLS_41));
__VLS_43.slots.default;
const __VLS_44 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
    theme: "primary",
    type: "submit",
    block: true,
    size: "large",
    loading: (__VLS_ctx.loading),
}));
const __VLS_46 = __VLS_45({
    theme: "primary",
    type: "submit",
    block: true,
    size: "large",
    loading: (__VLS_ctx.loading),
}, ...__VLS_functionalComponentArgsRest(__VLS_45));
__VLS_47.slots.default;
var __VLS_47;
var __VLS_43;
var __VLS_11;
if (__VLS_ctx.loginConfig?.providers?.length) {
    const __VLS_48 = {}.TDivider;
    /** @type {[typeof __VLS_components.TDivider, typeof __VLS_components.tDivider, typeof __VLS_components.TDivider, typeof __VLS_components.tDivider, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({}));
    const __VLS_50 = __VLS_49({}, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    var __VLS_51;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ style: {} },
    });
    for (const [item] of __VLS_getVForSourceType((__VLS_ctx.loginConfig.providers))) {
        const __VLS_52 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
            ...{ 'onClick': {} },
            key: (item.name),
            variant: "outline",
        }));
        const __VLS_54 = __VLS_53({
            ...{ 'onClick': {} },
            key: (item.name),
            variant: "outline",
        }, ...__VLS_functionalComponentArgsRest(__VLS_53));
        let __VLS_56;
        let __VLS_57;
        let __VLS_58;
        const __VLS_59 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.loginConfig?.providers?.length))
                    return;
                __VLS_ctx.goOAuth(item.name);
            }
        };
        __VLS_55.slots.default;
        (item.nickName || item.name);
        var __VLS_55;
    }
}
var __VLS_3;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            UserIcon: UserIcon,
            LockOnIcon: LockOnIcon,
            appStore: appStore,
            username: username,
            password: password,
            loading: loading,
            error: error,
            loginConfig: loginConfig,
            handleLogin: handleLogin,
            goOAuth: goOAuth,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
