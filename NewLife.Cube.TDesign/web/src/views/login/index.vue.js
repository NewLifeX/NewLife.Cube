import { ref, onMounted, computed } from 'vue';
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
const codeUsername = ref('');
const codeVal = ref('');
const mailUsername = ref('');
const mailCodeVal = ref('');
const loading = ref(false);
const codeLoading = ref(false);
const mailLoading = ref(false);
const smsCountdown = ref(0);
const mailCountdown = ref(0);
const error = ref('');
const activeTab = ref('password');
const loginConfig = ref(null);
const logoSrc = computed(() => appStore.siteInfo?.loginLogo || loginConfig.value?.logo || '');
onMounted(async () => {
    try {
        const [siteRes, configRes] = await Promise.all([
            api.user.getSiteInfo(),
            api.user.getLoginConfig(),
        ]);
        if (siteRes?.data)
            appStore.siteInfo = siteRes.data;
        loginConfig.value = configRes?.data ?? null;
        if (configRes?.data?.allowLogin === false) {
            if (configRes.data.enableSms)
                activeTab.value = 'sms';
            else if (configRes.data.enableMail)
                activeTab.value = 'email';
        }
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
async function sendCode(channel, uname) {
    const name = uname ?? codeUsername.value;
    if (!name) {
        error.value = channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址';
        return;
    }
    error.value = '';
    try {
        await api.user.sendCode({ channel, username: name, action: 'login' });
        const countRef = channel === 'Sms' ? smsCountdown : mailCountdown;
        countRef.value = 60;
        const timer = setInterval(() => {
            countRef.value--;
            if (countRef.value <= 0)
                clearInterval(timer);
        }, 1000);
    }
    catch (err) {
        error.value = err?.message ?? '发送失败';
    }
}
async function handleCodeLogin(loginCategory, uname, code) {
    const name = uname ?? codeUsername.value;
    const codeStr = code ?? codeVal.value;
    const loadingRef = loginCategory === 1 ? codeLoading : mailLoading;
    if (!name) {
        error.value = loginCategory === 1 ? '请输入手机号' : '请输入邮箱地址';
        return;
    }
    if (!codeStr) {
        error.value = '请输入验证码';
        return;
    }
    loadingRef.value = true;
    error.value = '';
    try {
        const res = await api.user.loginByCode({ username: name, password: codeStr, loginCategory });
        if (res.data?.accessToken) {
            api.tokenManager.setToken(res.data.accessToken);
        }
        await userStore.fetchMenus();
        router.push('/');
    }
    catch (err) {
        error.value = err?.message ?? '登录失败';
    }
    finally {
        loadingRef.value = false;
    }
}
function goOAuth(name) {
    window.location.href = `/Sso/Login/${name}?r=${encodeURIComponent(new URLSearchParams(window.location.search).get('redirect') || '/')}`;
}
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['login-header']} */ ;
/** @type {__VLS_StyleScopedClasses['login-footer']} */ ;
/** @type {__VLS_StyleScopedClasses['login-footer']} */ ;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "login-wrap" },
});
const __VLS_0 = {}.TCard;
/** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ class: "login-card" },
    bordered: (false),
}));
const __VLS_2 = __VLS_1({
    ...{ class: "login-card" },
    bordered: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_3.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "login-header" },
    });
    if (__VLS_ctx.logoSrc) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.img)({
            src: (__VLS_ctx.logoSrc),
            ...{ class: "login-logo" },
            alt: "",
        });
    }
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({});
    (__VLS_ctx.appStore.siteTitle);
    if (__VLS_ctx.loginConfig?.loginTip) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
            ...{ class: "login-tip" },
        });
        (__VLS_ctx.loginConfig.loginTip);
    }
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
const __VLS_8 = {}.TTabs;
/** @type {[typeof __VLS_components.TTabs, typeof __VLS_components.tTabs, typeof __VLS_components.TTabs, typeof __VLS_components.tTabs, ]} */ ;
// @ts-ignore
const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
    modelValue: (__VLS_ctx.activeTab),
}));
const __VLS_10 = __VLS_9({
    modelValue: (__VLS_ctx.activeTab),
}, ...__VLS_functionalComponentArgsRest(__VLS_9));
__VLS_11.slots.default;
if (__VLS_ctx.loginConfig?.allowLogin !== false) {
    const __VLS_12 = {}.TTabPanel;
    /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
        value: "password",
        label: "密码登录",
    }));
    const __VLS_14 = __VLS_13({
        value: "password",
        label: "密码登录",
    }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    __VLS_15.slots.default;
    const __VLS_16 = {}.TForm;
    /** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        ...{ 'onSubmit': {} },
        labelWidth: (0),
        ...{ style: {} },
    }));
    const __VLS_18 = __VLS_17({
        ...{ 'onSubmit': {} },
        labelWidth: (0),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    let __VLS_20;
    let __VLS_21;
    let __VLS_22;
    const __VLS_23 = {
        onSubmit: (__VLS_ctx.handleLogin)
    };
    __VLS_19.slots.default;
    const __VLS_24 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({}));
    const __VLS_26 = __VLS_25({}, ...__VLS_functionalComponentArgsRest(__VLS_25));
    __VLS_27.slots.default;
    const __VLS_28 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
        modelValue: (__VLS_ctx.username),
        placeholder: "请输入用户名",
        size: "large",
        clearable: true,
    }));
    const __VLS_30 = __VLS_29({
        modelValue: (__VLS_ctx.username),
        placeholder: "请输入用户名",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    {
        const { 'prefix-icon': __VLS_thisSlot } = __VLS_31.slots;
        const __VLS_32 = {}.UserIcon;
        /** @type {[typeof __VLS_components.UserIcon, typeof __VLS_components.userIcon, ]} */ ;
        // @ts-ignore
        const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({}));
        const __VLS_34 = __VLS_33({}, ...__VLS_functionalComponentArgsRest(__VLS_33));
    }
    var __VLS_31;
    var __VLS_27;
    const __VLS_36 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({}));
    const __VLS_38 = __VLS_37({}, ...__VLS_functionalComponentArgsRest(__VLS_37));
    __VLS_39.slots.default;
    const __VLS_40 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        modelValue: (__VLS_ctx.password),
        type: "password",
        placeholder: "请输入密码",
        size: "large",
        clearable: true,
    }));
    const __VLS_42 = __VLS_41({
        modelValue: (__VLS_ctx.password),
        type: "password",
        placeholder: "请输入密码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    __VLS_43.slots.default;
    {
        const { 'prefix-icon': __VLS_thisSlot } = __VLS_43.slots;
        const __VLS_44 = {}.LockOnIcon;
        /** @type {[typeof __VLS_components.LockOnIcon, typeof __VLS_components.lockOnIcon, ]} */ ;
        // @ts-ignore
        const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({}));
        const __VLS_46 = __VLS_45({}, ...__VLS_functionalComponentArgsRest(__VLS_45));
    }
    var __VLS_43;
    var __VLS_39;
    const __VLS_48 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({}));
    const __VLS_50 = __VLS_49({}, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    const __VLS_52 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        theme: "primary",
        type: "submit",
        block: true,
        size: "large",
        loading: (__VLS_ctx.loading),
    }));
    const __VLS_54 = __VLS_53({
        theme: "primary",
        type: "submit",
        block: true,
        size: "large",
        loading: (__VLS_ctx.loading),
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    __VLS_55.slots.default;
    var __VLS_55;
    var __VLS_51;
    var __VLS_19;
    var __VLS_15;
}
if (__VLS_ctx.loginConfig?.enableSms) {
    const __VLS_56 = {}.TTabPanel;
    /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
    // @ts-ignore
    const __VLS_57 = __VLS_asFunctionalComponent(__VLS_56, new __VLS_56({
        value: "sms",
        label: "手机验证码",
    }));
    const __VLS_58 = __VLS_57({
        value: "sms",
        label: "手机验证码",
    }, ...__VLS_functionalComponentArgsRest(__VLS_57));
    __VLS_59.slots.default;
    const __VLS_60 = {}.TForm;
    /** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
        labelWidth: (0),
        ...{ style: {} },
    }));
    const __VLS_62 = __VLS_61({
        labelWidth: (0),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    __VLS_63.slots.default;
    const __VLS_64 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({}));
    const __VLS_66 = __VLS_65({}, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    const __VLS_68 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        modelValue: (__VLS_ctx.codeUsername),
        placeholder: "请输入手机号",
        size: "large",
        clearable: true,
    }));
    const __VLS_70 = __VLS_69({
        modelValue: (__VLS_ctx.codeUsername),
        placeholder: "请输入手机号",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    var __VLS_67;
    const __VLS_72 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({}));
    const __VLS_74 = __VLS_73({}, ...__VLS_functionalComponentArgsRest(__VLS_73));
    __VLS_75.slots.default;
    const __VLS_76 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({
        modelValue: (__VLS_ctx.codeVal),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }));
    const __VLS_78 = __VLS_77({
        modelValue: (__VLS_ctx.codeVal),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    {
        const { suffix: __VLS_thisSlot } = __VLS_79.slots;
        const __VLS_80 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.smsCountdown > 0),
        }));
        const __VLS_82 = __VLS_81({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.smsCountdown > 0),
        }, ...__VLS_functionalComponentArgsRest(__VLS_81));
        let __VLS_84;
        let __VLS_85;
        let __VLS_86;
        const __VLS_87 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.loginConfig?.enableSms))
                    return;
                __VLS_ctx.sendCode('Sms');
            }
        };
        __VLS_83.slots.default;
        (__VLS_ctx.smsCountdown > 0 ? `${__VLS_ctx.smsCountdown}s后重发` : '获取验证码');
        var __VLS_83;
    }
    var __VLS_79;
    var __VLS_75;
    const __VLS_88 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({}));
    const __VLS_90 = __VLS_89({}, ...__VLS_functionalComponentArgsRest(__VLS_89));
    __VLS_91.slots.default;
    const __VLS_92 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.codeLoading),
    }));
    const __VLS_94 = __VLS_93({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.codeLoading),
    }, ...__VLS_functionalComponentArgsRest(__VLS_93));
    let __VLS_96;
    let __VLS_97;
    let __VLS_98;
    const __VLS_99 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.loginConfig?.enableSms))
                return;
            __VLS_ctx.handleCodeLogin(1);
        }
    };
    __VLS_95.slots.default;
    var __VLS_95;
    var __VLS_91;
    var __VLS_63;
    var __VLS_59;
}
if (__VLS_ctx.loginConfig?.enableMail) {
    const __VLS_100 = {}.TTabPanel;
    /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        value: "email",
        label: "邮箱验证码",
    }));
    const __VLS_102 = __VLS_101({
        value: "email",
        label: "邮箱验证码",
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    __VLS_103.slots.default;
    const __VLS_104 = {}.TForm;
    /** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
    // @ts-ignore
    const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
        labelWidth: (0),
        ...{ style: {} },
    }));
    const __VLS_106 = __VLS_105({
        labelWidth: (0),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_105));
    __VLS_107.slots.default;
    const __VLS_108 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({}));
    const __VLS_110 = __VLS_109({}, ...__VLS_functionalComponentArgsRest(__VLS_109));
    __VLS_111.slots.default;
    const __VLS_112 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
        modelValue: (__VLS_ctx.mailUsername),
        placeholder: "请输入邮箱地址",
        size: "large",
        clearable: true,
    }));
    const __VLS_114 = __VLS_113({
        modelValue: (__VLS_ctx.mailUsername),
        placeholder: "请输入邮箱地址",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_113));
    var __VLS_111;
    const __VLS_116 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({}));
    const __VLS_118 = __VLS_117({}, ...__VLS_functionalComponentArgsRest(__VLS_117));
    __VLS_119.slots.default;
    const __VLS_120 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_121 = __VLS_asFunctionalComponent(__VLS_120, new __VLS_120({
        modelValue: (__VLS_ctx.mailCodeVal),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }));
    const __VLS_122 = __VLS_121({
        modelValue: (__VLS_ctx.mailCodeVal),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_121));
    __VLS_123.slots.default;
    {
        const { suffix: __VLS_thisSlot } = __VLS_123.slots;
        const __VLS_124 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.mailCountdown > 0),
        }));
        const __VLS_126 = __VLS_125({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.mailCountdown > 0),
        }, ...__VLS_functionalComponentArgsRest(__VLS_125));
        let __VLS_128;
        let __VLS_129;
        let __VLS_130;
        const __VLS_131 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.loginConfig?.enableMail))
                    return;
                __VLS_ctx.sendCode('Mail', __VLS_ctx.mailUsername);
            }
        };
        __VLS_127.slots.default;
        (__VLS_ctx.mailCountdown > 0 ? `${__VLS_ctx.mailCountdown}s后重发` : '获取验证码');
        var __VLS_127;
    }
    var __VLS_123;
    var __VLS_119;
    const __VLS_132 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_133 = __VLS_asFunctionalComponent(__VLS_132, new __VLS_132({}));
    const __VLS_134 = __VLS_133({}, ...__VLS_functionalComponentArgsRest(__VLS_133));
    __VLS_135.slots.default;
    const __VLS_136 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_137 = __VLS_asFunctionalComponent(__VLS_136, new __VLS_136({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.mailLoading),
    }));
    const __VLS_138 = __VLS_137({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.mailLoading),
    }, ...__VLS_functionalComponentArgsRest(__VLS_137));
    let __VLS_140;
    let __VLS_141;
    let __VLS_142;
    const __VLS_143 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.loginConfig?.enableMail))
                return;
            __VLS_ctx.handleCodeLogin(2, __VLS_ctx.mailUsername, __VLS_ctx.mailCodeVal);
        }
    };
    __VLS_139.slots.default;
    var __VLS_139;
    var __VLS_135;
    var __VLS_107;
    var __VLS_103;
}
var __VLS_11;
if (__VLS_ctx.loginConfig?.providers?.length) {
    const __VLS_144 = {}.TDivider;
    /** @type {[typeof __VLS_components.TDivider, typeof __VLS_components.tDivider, typeof __VLS_components.TDivider, typeof __VLS_components.tDivider, ]} */ ;
    // @ts-ignore
    const __VLS_145 = __VLS_asFunctionalComponent(__VLS_144, new __VLS_144({}));
    const __VLS_146 = __VLS_145({}, ...__VLS_functionalComponentArgsRest(__VLS_145));
    __VLS_147.slots.default;
    var __VLS_147;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "oauth-list" },
    });
    for (const [item] of __VLS_getVForSourceType((__VLS_ctx.loginConfig.providers))) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ onClick: (...[$event]) => {
                    if (!(__VLS_ctx.loginConfig?.providers?.length))
                        return;
                    __VLS_ctx.goOAuth(item.name);
                } },
            key: (item.name),
            ...{ class: "oauth-item" },
            title: (item.nickName || item.name),
        });
        if (item.logo) {
            __VLS_asFunctionalElement(__VLS_intrinsicElements.img)({
                src: (item.logo),
                ...{ class: "oauth-logo" },
                alt: (item.nickName || item.name),
            });
        }
        else {
            __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                ...{ class: "oauth-fallback" },
            });
            ((item.nickName || item.name).charAt(0).toUpperCase());
        }
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
            ...{ class: "oauth-name" },
        });
        (item.nickName || item.name);
    }
}
if (__VLS_ctx.loginConfig?.allowRegister) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "register-link" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
    const __VLS_148 = {}.TLink;
    /** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
    // @ts-ignore
    const __VLS_149 = __VLS_asFunctionalComponent(__VLS_148, new __VLS_148({
        ...{ 'onClick': {} },
        theme: "primary",
    }));
    const __VLS_150 = __VLS_149({
        ...{ 'onClick': {} },
        theme: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_149));
    let __VLS_152;
    let __VLS_153;
    let __VLS_154;
    const __VLS_155 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.loginConfig?.allowRegister))
                return;
            __VLS_ctx.router.push('/register');
        }
    };
    __VLS_151.slots.default;
    var __VLS_151;
}
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "forgot-link" },
});
const __VLS_156 = {}.TLink;
/** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
// @ts-ignore
const __VLS_157 = __VLS_asFunctionalComponent(__VLS_156, new __VLS_156({
    ...{ 'onClick': {} },
    theme: "primary",
}));
const __VLS_158 = __VLS_157({
    ...{ 'onClick': {} },
    theme: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_157));
let __VLS_160;
let __VLS_161;
let __VLS_162;
const __VLS_163 = {
    onClick: (...[$event]) => {
        __VLS_ctx.router.push('/forgot-password');
    }
};
__VLS_159.slots.default;
var __VLS_159;
if (__VLS_ctx.appStore.siteInfo?.copyright || __VLS_ctx.appStore.siteInfo?.registration) {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "login-footer" },
    });
    if (__VLS_ctx.appStore.siteInfo.copyright) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
        __VLS_asFunctionalDirective(__VLS_directives.vHtml)(null, { ...__VLS_directiveBindingRestFields, value: (__VLS_ctx.appStore.siteInfo.copyright) }, null, null);
    }
    if (__VLS_ctx.appStore.siteInfo.registration) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
        __VLS_asFunctionalElement(__VLS_intrinsicElements.a, __VLS_intrinsicElements.a)({
            href: "https://www.beianx.cn/",
            target: "_blank",
            rel: "noopener noreferrer",
        });
        (__VLS_ctx.appStore.siteInfo.registration);
    }
}
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['login-wrap']} */ ;
/** @type {__VLS_StyleScopedClasses['login-card']} */ ;
/** @type {__VLS_StyleScopedClasses['login-header']} */ ;
/** @type {__VLS_StyleScopedClasses['login-logo']} */ ;
/** @type {__VLS_StyleScopedClasses['login-tip']} */ ;
/** @type {__VLS_StyleScopedClasses['oauth-list']} */ ;
/** @type {__VLS_StyleScopedClasses['oauth-item']} */ ;
/** @type {__VLS_StyleScopedClasses['oauth-logo']} */ ;
/** @type {__VLS_StyleScopedClasses['oauth-fallback']} */ ;
/** @type {__VLS_StyleScopedClasses['oauth-name']} */ ;
/** @type {__VLS_StyleScopedClasses['register-link']} */ ;
/** @type {__VLS_StyleScopedClasses['forgot-link']} */ ;
/** @type {__VLS_StyleScopedClasses['login-footer']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            UserIcon: UserIcon,
            LockOnIcon: LockOnIcon,
            appStore: appStore,
            router: router,
            username: username,
            password: password,
            codeUsername: codeUsername,
            codeVal: codeVal,
            mailUsername: mailUsername,
            mailCodeVal: mailCodeVal,
            loading: loading,
            codeLoading: codeLoading,
            mailLoading: mailLoading,
            smsCountdown: smsCountdown,
            mailCountdown: mailCountdown,
            error: error,
            activeTab: activeTab,
            loginConfig: loginConfig,
            logoSrc: logoSrc,
            handleLogin: handleLogin,
            sendCode: sendCode,
            handleCodeLogin: handleCodeLogin,
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
