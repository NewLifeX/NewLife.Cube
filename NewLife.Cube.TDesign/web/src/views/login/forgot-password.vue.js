import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { MessagePlugin } from 'tdesign-vue-next';
import { api } from '@/api';
const router = useRouter();
const step = ref('input');
const sending = ref(false);
const submitting = ref(false);
const countdown = ref(0);
const error = ref('');
const form = reactive({
    username: '',
    channel: 'Sms',
    code: '',
    newPassword: '',
    confirmPassword: '',
});
let _timer = null;
const startCountdown = (seconds = 60) => {
    if (_timer)
        clearInterval(_timer);
    countdown.value = seconds;
    _timer = setInterval(() => {
        countdown.value -= 1;
        if (countdown.value <= 0) {
            clearInterval(_timer);
            _timer = null;
        }
    }, 1000);
};
const onSendCode = async () => {
    if (!form.username) {
        MessagePlugin.warning('请输入手机号或邮箱');
        return;
    }
    sending.value = true;
    error.value = '';
    try {
        await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
        step.value = 'verify';
        startCountdown();
        MessagePlugin.success('验证码已发送');
    }
    catch (e) {
        const msg = e instanceof Error ? e.message : '发送失败，请稍后重试';
        MessagePlugin.error(msg);
    }
    finally {
        sending.value = false;
    }
};
const onResend = async () => {
    if (countdown.value > 0)
        return;
    sending.value = true;
    error.value = '';
    try {
        await api.user.sendCode({ channel: form.channel, username: form.username, action: 'reset' });
        startCountdown();
        MessagePlugin.success('验证码已重新发送');
    }
    catch (e) {
        const msg = e instanceof Error ? e.message : '发送失败';
        MessagePlugin.error(msg);
    }
    finally {
        sending.value = false;
    }
};
const onConfirmReset = async () => {
    if (!form.code) {
        error.value = '请输入验证码';
        return;
    }
    if (!form.newPassword) {
        error.value = '请输入新密码';
        return;
    }
    if (form.newPassword !== form.confirmPassword) {
        error.value = '两次密码不一致';
        return;
    }
    submitting.value = true;
    error.value = '';
    try {
        await api.user.resetPassword({
            username: form.username,
            code: form.code,
            newPassword: form.newPassword,
            confirmPassword: form.confirmPassword,
        });
        MessagePlugin.success('密码重置成功，请重新登录');
        router.push('/login');
    }
    catch (e) {
        const msg = e instanceof Error ? e.message : '重置失败，请重试';
        error.value = msg;
    }
    finally {
        submitting.value = false;
    }
};
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "forgot-wrap" },
});
const __VLS_0 = {}.TCard;
/** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    ...{ class: "forgot-card" },
    bordered: (false),
}));
const __VLS_2 = __VLS_1({
    ...{ class: "forgot-card" },
    bordered: (false),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
{
    const { header: __VLS_thisSlot } = __VLS_3.slots;
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "forgot-header" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({});
}
if (__VLS_ctx.error) {
    const __VLS_4 = {}.TAlert;
    /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        theme: "error",
        message: (__VLS_ctx.error),
        ...{ style: {} },
    }));
    const __VLS_6 = __VLS_5({
        theme: "error",
        message: (__VLS_ctx.error),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
}
if (__VLS_ctx.step === 'input') {
    const __VLS_8 = {}.TForm;
    /** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        labelWidth: (0),
    }));
    const __VLS_10 = __VLS_9({
        labelWidth: (0),
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
    __VLS_11.slots.default;
    const __VLS_12 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({}));
    const __VLS_14 = __VLS_13({}, ...__VLS_functionalComponentArgsRest(__VLS_13));
    __VLS_15.slots.default;
    const __VLS_16 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({
        modelValue: (__VLS_ctx.form.username),
        placeholder: "请输入手机号或邮箱",
        size: "large",
        clearable: true,
    }));
    const __VLS_18 = __VLS_17({
        modelValue: (__VLS_ctx.form.username),
        placeholder: "请输入手机号或邮箱",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_17));
    var __VLS_15;
    const __VLS_20 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({}));
    const __VLS_22 = __VLS_21({}, ...__VLS_functionalComponentArgsRest(__VLS_21));
    __VLS_23.slots.default;
    const __VLS_24 = {}.TRadioGroup;
    /** @type {[typeof __VLS_components.TRadioGroup, typeof __VLS_components.tRadioGroup, typeof __VLS_components.TRadioGroup, typeof __VLS_components.tRadioGroup, ]} */ ;
    // @ts-ignore
    const __VLS_25 = __VLS_asFunctionalComponent(__VLS_24, new __VLS_24({
        modelValue: (__VLS_ctx.form.channel),
    }));
    const __VLS_26 = __VLS_25({
        modelValue: (__VLS_ctx.form.channel),
    }, ...__VLS_functionalComponentArgsRest(__VLS_25));
    __VLS_27.slots.default;
    const __VLS_28 = {}.TRadio;
    /** @type {[typeof __VLS_components.TRadio, typeof __VLS_components.tRadio, typeof __VLS_components.TRadio, typeof __VLS_components.tRadio, ]} */ ;
    // @ts-ignore
    const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
        value: "Sms",
    }));
    const __VLS_30 = __VLS_29({
        value: "Sms",
    }, ...__VLS_functionalComponentArgsRest(__VLS_29));
    __VLS_31.slots.default;
    var __VLS_31;
    const __VLS_32 = {}.TRadio;
    /** @type {[typeof __VLS_components.TRadio, typeof __VLS_components.tRadio, typeof __VLS_components.TRadio, typeof __VLS_components.tRadio, ]} */ ;
    // @ts-ignore
    const __VLS_33 = __VLS_asFunctionalComponent(__VLS_32, new __VLS_32({
        value: "Mail",
    }));
    const __VLS_34 = __VLS_33({
        value: "Mail",
    }, ...__VLS_functionalComponentArgsRest(__VLS_33));
    __VLS_35.slots.default;
    var __VLS_35;
    var __VLS_27;
    var __VLS_23;
    const __VLS_36 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({}));
    const __VLS_38 = __VLS_37({}, ...__VLS_functionalComponentArgsRest(__VLS_37));
    __VLS_39.slots.default;
    const __VLS_40 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.sending),
    }));
    const __VLS_42 = __VLS_41({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.sending),
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    let __VLS_44;
    let __VLS_45;
    let __VLS_46;
    const __VLS_47 = {
        onClick: (__VLS_ctx.onSendCode)
    };
    __VLS_43.slots.default;
    var __VLS_43;
    var __VLS_39;
    const __VLS_48 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_49 = __VLS_asFunctionalComponent(__VLS_48, new __VLS_48({}));
    const __VLS_50 = __VLS_49({}, ...__VLS_functionalComponentArgsRest(__VLS_49));
    __VLS_51.slots.default;
    const __VLS_52 = {}.TLink;
    /** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        ...{ 'onClick': {} },
        theme: "primary",
    }));
    const __VLS_54 = __VLS_53({
        ...{ 'onClick': {} },
        theme: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    let __VLS_56;
    let __VLS_57;
    let __VLS_58;
    const __VLS_59 = {
        onClick: (...[$event]) => {
            if (!(__VLS_ctx.step === 'input'))
                return;
            __VLS_ctx.router.push('/login');
        }
    };
    __VLS_55.slots.default;
    var __VLS_55;
    var __VLS_51;
    var __VLS_11;
}
else {
    const __VLS_60 = {}.TForm;
    /** @type {[typeof __VLS_components.TForm, typeof __VLS_components.tForm, typeof __VLS_components.TForm, typeof __VLS_components.tForm, ]} */ ;
    // @ts-ignore
    const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
        labelWidth: (0),
    }));
    const __VLS_62 = __VLS_61({
        labelWidth: (0),
    }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    __VLS_63.slots.default;
    const __VLS_64 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({}));
    const __VLS_66 = __VLS_65({}, ...__VLS_functionalComponentArgsRest(__VLS_65));
    __VLS_67.slots.default;
    const __VLS_68 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({
        modelValue: (__VLS_ctx.form.code),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }));
    const __VLS_70 = __VLS_69({
        modelValue: (__VLS_ctx.form.code),
        placeholder: "请输入验证码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_69));
    __VLS_71.slots.default;
    {
        const { suffix: __VLS_thisSlot } = __VLS_71.slots;
        const __VLS_72 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }));
        const __VLS_74 = __VLS_73({
            ...{ 'onClick': {} },
            variant: "text",
            disabled: (__VLS_ctx.countdown > 0),
            loading: (__VLS_ctx.sending),
        }, ...__VLS_functionalComponentArgsRest(__VLS_73));
        let __VLS_76;
        let __VLS_77;
        let __VLS_78;
        const __VLS_79 = {
            onClick: (__VLS_ctx.onResend)
        };
        __VLS_75.slots.default;
        (__VLS_ctx.countdown > 0 ? `${__VLS_ctx.countdown}s` : '重新发送');
        var __VLS_75;
    }
    var __VLS_71;
    var __VLS_67;
    const __VLS_80 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({}));
    const __VLS_82 = __VLS_81({}, ...__VLS_functionalComponentArgsRest(__VLS_81));
    __VLS_83.slots.default;
    const __VLS_84 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
        modelValue: (__VLS_ctx.form.newPassword),
        type: "password",
        placeholder: "请输入新密码",
        size: "large",
        clearable: true,
    }));
    const __VLS_86 = __VLS_85({
        modelValue: (__VLS_ctx.form.newPassword),
        type: "password",
        placeholder: "请输入新密码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_85));
    var __VLS_83;
    const __VLS_88 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({}));
    const __VLS_90 = __VLS_89({}, ...__VLS_functionalComponentArgsRest(__VLS_89));
    __VLS_91.slots.default;
    const __VLS_92 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_93 = __VLS_asFunctionalComponent(__VLS_92, new __VLS_92({
        modelValue: (__VLS_ctx.form.confirmPassword),
        type: "password",
        placeholder: "请再次输入新密码",
        size: "large",
        clearable: true,
    }));
    const __VLS_94 = __VLS_93({
        modelValue: (__VLS_ctx.form.confirmPassword),
        type: "password",
        placeholder: "请再次输入新密码",
        size: "large",
        clearable: true,
    }, ...__VLS_functionalComponentArgsRest(__VLS_93));
    var __VLS_91;
    const __VLS_96 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({}));
    const __VLS_98 = __VLS_97({}, ...__VLS_functionalComponentArgsRest(__VLS_97));
    __VLS_99.slots.default;
    const __VLS_100 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.submitting),
    }));
    const __VLS_102 = __VLS_101({
        ...{ 'onClick': {} },
        theme: "primary",
        block: true,
        size: "large",
        loading: (__VLS_ctx.submitting),
    }, ...__VLS_functionalComponentArgsRest(__VLS_101));
    let __VLS_104;
    let __VLS_105;
    let __VLS_106;
    const __VLS_107 = {
        onClick: (__VLS_ctx.onConfirmReset)
    };
    __VLS_103.slots.default;
    var __VLS_103;
    var __VLS_99;
    const __VLS_108 = {}.TFormItem;
    /** @type {[typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, typeof __VLS_components.TFormItem, typeof __VLS_components.tFormItem, ]} */ ;
    // @ts-ignore
    const __VLS_109 = __VLS_asFunctionalComponent(__VLS_108, new __VLS_108({}));
    const __VLS_110 = __VLS_109({}, ...__VLS_functionalComponentArgsRest(__VLS_109));
    __VLS_111.slots.default;
    const __VLS_112 = {}.TSpace;
    /** @type {[typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, ]} */ ;
    // @ts-ignore
    const __VLS_113 = __VLS_asFunctionalComponent(__VLS_112, new __VLS_112({}));
    const __VLS_114 = __VLS_113({}, ...__VLS_functionalComponentArgsRest(__VLS_113));
    __VLS_115.slots.default;
    const __VLS_116 = {}.TLink;
    /** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
    // @ts-ignore
    const __VLS_117 = __VLS_asFunctionalComponent(__VLS_116, new __VLS_116({
        ...{ 'onClick': {} },
    }));
    const __VLS_118 = __VLS_117({
        ...{ 'onClick': {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_117));
    let __VLS_120;
    let __VLS_121;
    let __VLS_122;
    const __VLS_123 = {
        onClick: (...[$event]) => {
            if (!!(__VLS_ctx.step === 'input'))
                return;
            __VLS_ctx.step = 'input';
        }
    };
    __VLS_119.slots.default;
    var __VLS_119;
    const __VLS_124 = {}.TDivider;
    /** @type {[typeof __VLS_components.TDivider, typeof __VLS_components.tDivider, ]} */ ;
    // @ts-ignore
    const __VLS_125 = __VLS_asFunctionalComponent(__VLS_124, new __VLS_124({
        layout: "vertical",
    }));
    const __VLS_126 = __VLS_125({
        layout: "vertical",
    }, ...__VLS_functionalComponentArgsRest(__VLS_125));
    const __VLS_128 = {}.TLink;
    /** @type {[typeof __VLS_components.TLink, typeof __VLS_components.tLink, typeof __VLS_components.TLink, typeof __VLS_components.tLink, ]} */ ;
    // @ts-ignore
    const __VLS_129 = __VLS_asFunctionalComponent(__VLS_128, new __VLS_128({
        ...{ 'onClick': {} },
        theme: "primary",
    }));
    const __VLS_130 = __VLS_129({
        ...{ 'onClick': {} },
        theme: "primary",
    }, ...__VLS_functionalComponentArgsRest(__VLS_129));
    let __VLS_132;
    let __VLS_133;
    let __VLS_134;
    const __VLS_135 = {
        onClick: (...[$event]) => {
            if (!!(__VLS_ctx.step === 'input'))
                return;
            __VLS_ctx.router.push('/login');
        }
    };
    __VLS_131.slots.default;
    var __VLS_131;
    var __VLS_115;
    var __VLS_111;
    var __VLS_63;
}
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['forgot-wrap']} */ ;
/** @type {__VLS_StyleScopedClasses['forgot-card']} */ ;
/** @type {__VLS_StyleScopedClasses['forgot-header']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            router: router,
            step: step,
            sending: sending,
            submitting: submitting,
            countdown: countdown,
            error: error,
            form: form,
            onSendCode: onSendCode,
            onResend: onResend,
            onConfirmReset: onConfirmReset,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
