import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { RegisterCategory } from '@cube/api-core';
import { api } from '@/api';
const router = useRouter();
const route = useRoute();
const activeTab = ref('password');
const oauthMode = ref(false);
const loading = ref(false);
const sending = ref(false);
const countdown = ref(0);
const error = ref('');
const config = ref(null);
const form = reactive({
    username: '',
    email: '',
    mobile: '',
    emailCodeTarget: '',
    code: '',
    password: '',
    confirmPassword: '',
    oauthToken: '',
});
let timer = null;
const enableSmsRegister = computed(() => !!(config.value?.enableSmsRegister ?? config.value?.enableSms));
const enableMailRegister = computed(() => !!(config.value?.enableMailRegister ?? config.value?.enableMail));
const startCountdown = () => {
    if (timer)
        clearInterval(timer);
    countdown.value = 60;
    timer = setInterval(() => {
        countdown.value -= 1;
        if (countdown.value <= 0 && timer) {
            clearInterval(timer);
            timer = null;
        }
    }, 1000);
};
const sendCode = async (channel) => {
    const username = channel === 'Sms' ? form.mobile : form.emailCodeTarget;
    if (!username) {
        error.value = channel === 'Sms' ? '请输入手机号' : '请输入邮箱地址';
        return;
    }
    sending.value = true;
    error.value = '';
    try {
        await api.user.sendCode({ channel, username, action: 'register' });
        startCountdown();
    }
    catch (err) {
        error.value = err?.message || '发送失败';
    }
    finally {
        sending.value = false;
    }
};
const onSubmit = async () => {
    if (!form.password || !form.confirmPassword) {
        error.value = '请输入密码和确认密码';
        return;
    }
    if (form.password !== form.confirmPassword) {
        error.value = '两次密码不一致';
        return;
    }
    if (activeTab.value === 'phone' && (!form.mobile || !form.code)) {
        error.value = '请填写手机号和验证码';
        return;
    }
    if (activeTab.value === 'email' && (!form.emailCodeTarget || !form.code)) {
        error.value = '请填写邮箱和验证码';
        return;
    }
    const payload = oauthMode.value
        ? { registerCategory: RegisterCategory.OAuthBind, oauthToken: form.oauthToken, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword }
        : activeTab.value === 'phone'
            ? { registerCategory: RegisterCategory.Phone, username: form.username || form.mobile, mobile: form.mobile, email: form.email, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
            : activeTab.value === 'email'
                ? { registerCategory: RegisterCategory.Email, username: form.username || form.emailCodeTarget, email: form.emailCodeTarget, code: form.code, password: form.password, confirmPassword: form.confirmPassword }
                : { registerCategory: RegisterCategory.Password, username: form.username, email: form.email, password: form.password, confirmPassword: form.confirmPassword };
    loading.value = true;
    error.value = '';
    try {
        const res = await api.user.register(payload);
        const token = res.data?.accessToken || res.data?.token;
        if (token) {
            api.tokenManager.setToken(token);
            router.push('/');
            return;
        }
        router.push('/login');
    }
    catch (err) {
        error.value = err?.message || '注册失败';
    }
    finally {
        loading.value = false;
    }
};
onMounted(async () => {
    try {
        const cfg = await api.user.getLoginConfig();
        config.value = cfg.data;
    }
    catch { /* ignore */ }
    const oauthToken = route.query.oauthToken || '';
    if (!oauthToken)
        return;
    oauthMode.value = true;
    form.oauthToken = oauthToken;
    try {
        const rs = await api.user.getOAuthPendingInfo(oauthToken);
        form.username = rs.data?.username || '';
        form.email = rs.data?.email || '';
        form.mobile = rs.data?.mobile || '';
    }
    catch {
        error.value = 'OAuth预填信息已过期，请重新发起登录';
    }
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "register-wrap" },
});
const __VLS_0 = {}.TCard;
/** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ class: "register-card" },
    bordered: (false),
}));
const __VLS_2 = __VLS_1({
    ...{ class: "register-card" },
    bordered: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_3.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "register-header" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({});
}
if (!__VLS_ctx.oauthMode) {
    const __VLS_4 = {}.TTabs;
    /** @type {[typeof __VLS_components.TTabs, typeof __VLS_components.tTabs, typeof __VLS_components.TTabs, typeof __VLS_components.tTabs, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        modelValue: (__VLS_ctx.activeTab),
    }));
    const __VLS_6 = __VLS_5({
        modelValue: (__VLS_ctx.activeTab),
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
    __VLS_7.slots.default;
    const __VLS_8 = {}.TTabPanel;
    /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        value: "password",
        label: "账号注册",
    }));
    const __VLS_10 = __VLS_9({
        value: "password",
        label: "账号注册",
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
    if (__VLS_ctx.enableSmsRegister) {
        const __VLS_12 = {}.TTabPanel;
        /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
        // @ts-ignore
        const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
            value: "phone",
            label: "手机注册",
        }));
        const __VLS_14 = __VLS_13({
            value: "phone",
            label: "手机注册",
        }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    }
    if (__VLS_ctx.enableMailRegister) {
        const __VLS_16 = {}.TTabPanel;
        /** @type {[typeof __VLS_components.TTabPanel, typeof __VLS_components.tTabPanel, ]} */ ;
        // @ts-ignore
        const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
            value: "email",
            label: "邮箱注册",
        }));
        const __VLS_18 = __VLS_17({
            value: "email",
            label: "邮箱注册",
        }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    }
    var __VLS_7;
}
if (__VLS_ctx.oauthMode) {
    const __VLS_20 = {}.TAlert;
    /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
    // @ts-ignore
    const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
        theme: "info",
        message: "第三方账号首次登录，请补全密码完成本地账号创建",
        ...{ style: {} },
    }));
    const __VLS_22 = __VLS_21({
        theme: "info",
        message: "第三方账号首次登录，请补全密码完成本地账号创建",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_21));
}
const __VLS_24 = {}.TForm;
/** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
// @ts-ignore
const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
    labelWidth: (0),
}));
const __VLS_26 = __VLS_25({
    labelWidth: (0),
}, ...__VLS_functionalComponentArgsRest(__VLS_25));
__VLS_27.slots.default;
if (__VLS_ctx.activeTab === 'password' || __VLS_ctx.oauthMode) {
    const __VLS_28 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({}));
    const __VLS_30 = __VLS_29({}, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    const __VLS_32 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
        modelValue: (__VLS_ctx.form.username),
        placeholder: "用户名",
        readonly: (__VLS_ctx.oauthMode),
    }));
    const __VLS_34 = __VLS_33({
        modelValue: (__VLS_ctx.form.username),
        placeholder: "用户名",
        readonly: (__VLS_ctx.oauthMode),
    }, ...__VLS_functionalComponentArgsRest(__VLS_33));
    var __VLS_31;
}
if (__VLS_ctx.activeTab === 'password' || __VLS_ctx.activeTab === 'email' || __VLS_ctx.oauthMode) {
    const __VLS_36 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({}));
    const __VLS_38 = __VLS_37({}, ...__VLS_functionalComponentArgsRest(__VLS_37));
    __VLS_39.slots.default;
    const __VLS_40 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        modelValue: (__VLS_ctx.form.email),
        placeholder: "邮箱",
    }));
    const __VLS_42 = __VLS_41({
        modelValue: (__VLS_ctx.form.email),
        placeholder: "邮箱",
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    var __VLS_39;
}
if (__VLS_ctx.activeTab === 'phone') {
    const __VLS_44 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({}));
    const __VLS_46 = __VLS_45({}, ...__VLS_functionalComponentArgsRest(__VLS_45));
    __VLS_47.slots.default;
    const __VLS_48 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({
        modelValue: (__VLS_ctx.form.mobile),
        placeholder: "手机号",
    }));
    const __VLS_50 = __VLS_49({
        modelValue: (__VLS_ctx.form.mobile),
        placeholder: "手机号",
    }, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    {
        const { suffix: __VLS_thisSlot } = __VLS_51.slots;
        const __VLS_52 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }));
        const __VLS_54 = __VLS_53({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }, ...__VLS_functionalComponentArgsRest(__VLS_53));
        let __VLS_56;
        let __VLS_57;
        let __VLS_58;
        const __VLS_59 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.activeTab === 'phone'))
                    return;
                __VLS_ctx.sendCode('Sms');
            }
        };
        __VLS_55.slots.default;
        (__VLS_ctx.countdown > 0 ? `${__VLS_ctx.countdown}s` : '发送验证码');
        var __VLS_55;
    }
    var __VLS_51;
    var __VLS_47;
}
if (__VLS_ctx.activeTab === 'email') {
    const __VLS_60 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({}));
    const __VLS_62 = __VLS_61({}, ...__VLS_functionalComponentArgsRest(__VLS_61));
    __VLS_63.slots.default;
    const __VLS_64 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
        modelValue: (__VLS_ctx.form.emailCodeTarget),
        placeholder: "邮箱地址",
    }));
    const __VLS_66 = __VLS_65({
        modelValue: (__VLS_ctx.form.emailCodeTarget),
        placeholder: "邮箱地址",
    }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    {
        const { suffix: __VLS_thisSlot } = __VLS_67.slots;
        const __VLS_68 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }));
        const __VLS_70 = __VLS_69({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }, ...__VLS_functionalComponentArgsRest(__VLS_69));
        let __VLS_72;
        let __VLS_73;
        let __VLS_74;
        const __VLS_75 = {
            onClick: (...[$event]) => {
                if (!(__VLS_ctx.activeTab === 'email'))
                    return;
                __VLS_ctx.sendCode('Mail');
            }
        };
        __VLS_71.slots.default;
        (__VLS_ctx.countdown > 0 ? `${__VLS_ctx.countdown}s` : '发送验证码');
        var __VLS_71;
    }
    var __VLS_67;
    var __VLS_63;
}
if (__VLS_ctx.activeTab === 'phone' || __VLS_ctx.activeTab === 'email') {
    const __VLS_76 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_77 = __VLS_asFunctionalComponent(__VLS_76, new __VLS_76({}));
    const __VLS_78 = __VLS_77({}, ...__VLS_functionalComponentArgsRest(__VLS_77));
    __VLS_79.slots.default;
    const __VLS_80 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
        modelValue: (__VLS_ctx.form.code),
        placeholder: "验证码",
    }));
    const __VLS_82 = __VLS_81({
        modelValue: (__VLS_ctx.form.code),
        placeholder: "验证码",
    }, ...__VLS_functionalComponentArgsRest(__VLS_81));
    var __VLS_79;
}
const __VLS_84 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({}));
const __VLS_86 = __VLS_85({}, ...__VLS_functionalComponentArgsRest(__VLS_85));
__VLS_87.slots.default;
const __VLS_88 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
    modelValue: (__VLS_ctx.form.password),
    type: "password",
    placeholder: "密码",
}));
const __VLS_90 = __VLS_89({
    modelValue: (__VLS_ctx.form.password),
    type: "password",
    placeholder: "密码",
}, ...__VLS_functionalComponentArgsRest(__VLS_89));
var __VLS_87;
const __VLS_92 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({}));
const __VLS_94 = __VLS_93({}, ...__VLS_functionalComponentArgsRest(__VLS_93));
__VLS_95.slots.default;
const __VLS_96 = {}.TInput;
/** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
// @ts-ignore
const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
    modelValue: (__VLS_ctx.form.confirmPassword),
    type: "password",
    placeholder: "确认密码",
}));
const __VLS_98 = __VLS_97({
    modelValue: (__VLS_ctx.form.confirmPassword),
    type: "password",
    placeholder: "确认密码",
}, ...__VLS_functionalComponentArgsRest(__VLS_97));
var __VLS_95;
const __VLS_100 = {}.TFormItem;
/** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
// @ts-ignore
const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({}));
const __VLS_102 = __VLS_101({}, ...__VLS_functionalComponentArgsRest(__VLS_101));
__VLS_103.slots.default;
const __VLS_104 = {}.TButton;
/** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
// @ts-ignore
const __VLS_105 = __VLS_asFunctionalComponent(__VLS_104, new __VLS_104({
    ...{ 'onClick': {} },
    theme: "primary",
    block: true,
    loading: (__VLS_ctx.loading),
}));
const __VLS_106 = __VLS_105({
    ...{ 'onClick': {} },
    theme: "primary",
    block: true,
    loading: (__VLS_ctx.loading),
}, ...__VLS_functionalComponentArgsRest(__VLS_105));
let __VLS_108;
let __VLS_109;
let __VLS_110;
const __VLS_111 = {
    onClick: (__VLS_ctx.onSubmit)
};
__VLS_107.slots.default;
(__VLS_ctx.oauthMode ? '完成绑定并登录' : '立即注册');
var __VLS_107;
var __VLS_103;
var __VLS_27;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "register-footer-link" },
});
__VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
const __VLS_112 = {}.TLink;
/** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
// @ts-ignore
const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({
    ...{ 'onClick': {} },
    theme: "primary",
}));
const __VLS_114 = __VLS_113({
    ...{ 'onClick': {} },
    theme: "primary",
}, ...__VLS_functionalComponentArgsRest(__VLS_113));
let __VLS_116;
let __VLS_117;
let __VLS_118;
const __VLS_119 = {
    onClick: (...[$event]) => {
        __VLS_ctx.router.push('/login');
    }
};
__VLS_115.slots.default;
var __VLS_115;
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['register-wrap']} */ ;
/** @type {__VLS_StyleScopedClasses['register-card']} */ ;
/** @type {__VLS_StyleScopedClasses['register-header']} */ ;
/** @type {__VLS_StyleScopedClasses['register-footer-link']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            router: router,
            activeTab: activeTab,
            oauthMode: oauthMode,
            loading: loading,
            sending: sending,
            countdown: countdown,
            form: form,
            enableSmsRegister: enableSmsRegister,
            enableMailRegister: enableMailRegister,
            sendCode: sendCode,
            onSubmit: onSubmit,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
