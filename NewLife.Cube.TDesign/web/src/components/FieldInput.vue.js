import { computed } from 'vue';
import { resolveWidget } from '@cube/field-mapping';
const props = defineProps();
const emit = defineEmits();
const wt = computed(() => resolveWidget(props.field).widget);
const options = computed(() => {
    if (!props.field.dataSource)
        return [];
    return Object.entries(props.field.dataSource).map(([value, label]) => ({ value, label }));
});
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
if (__VLS_ctx.wt === 'switch') {
    const __VLS_0 = {}.TSwitch;
    /** @type {[typeof __VLS_components.TSwitch, typeof __VLS_components.tSwitch, ]} */ ;
    // @ts-ignore
    const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
        ...{ 'onChange': {} },
        value: (!!__VLS_ctx.modelValue),
    }));
    const __VLS_2 = __VLS_1({
        ...{ 'onChange': {} },
        value: (!!__VLS_ctx.modelValue),
    }, ...__VLS_functionalComponentArgsRest(__VLS_1));
    let __VLS_4;
    let __VLS_5;
    let __VLS_6;
    const __VLS_7 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_8 = {};
    var __VLS_3;
}
else if (__VLS_ctx.wt === 'select') {
    const __VLS_9 = {}.TSelect;
    /** @type {[typeof __VLS_components.TSelect, typeof __VLS_components.tSelect, typeof __VLS_components.TSelect, typeof __VLS_components.tSelect, ]} */ ;
    // @ts-ignore
    const __VLS_10 = __VLS_asFunctionalComponent(__VLS_9, new __VLS_9({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        clearable: true,
        ...{ style: {} },
    }));
    const __VLS_11 = __VLS_10({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        clearable: true,
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_10));
    let __VLS_13;
    let __VLS_14;
    let __VLS_15;
    const __VLS_16 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_17 = {};
    __VLS_12.slots.default;
    for (const [opt] of __VLS_getVForSourceType((__VLS_ctx.options))) {
        const __VLS_18 = {}.TOption;
        /** @type {[typeof __VLS_components.TOption, typeof __VLS_components.tOption, ]} */ ;
        // @ts-ignore
        const __VLS_19 = __VLS_asFunctionalComponent(__VLS_18, new __VLS_18({
            key: (opt.value),
            value: (opt.value),
            label: (opt.label),
        }));
        const __VLS_20 = __VLS_19({
            key: (opt.value),
            value: (opt.value),
            label: (opt.label),
        }, ...__VLS_functionalComponentArgsRest(__VLS_19));
    }
    var __VLS_12;
}
else if (__VLS_ctx.wt === 'number') {
    const __VLS_22 = {}.TInputNumber;
    /** @type {[typeof __VLS_components.TInputNumber, typeof __VLS_components.tInputNumber, ]} */ ;
    // @ts-ignore
    const __VLS_23 = __VLS_asFunctionalComponent(__VLS_22, new __VLS_22({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        theme: "normal",
        ...{ style: {} },
    }));
    const __VLS_24 = __VLS_23({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        theme: "normal",
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_23));
    let __VLS_26;
    let __VLS_27;
    let __VLS_28;
    const __VLS_29 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_30 = {};
    var __VLS_25;
}
else if (__VLS_ctx.wt === 'datetime') {
    const __VLS_31 = {}.TDatePicker;
    /** @type {[typeof __VLS_components.TDatePicker, typeof __VLS_components.tDatePicker, ]} */ ;
    // @ts-ignore
    const __VLS_32 = __VLS_asFunctionalComponent(__VLS_31, new __VLS_31({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        enableTimePicker: true,
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        ...{ style: {} },
    }));
    const __VLS_33 = __VLS_32({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        enableTimePicker: true,
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_32));
    let __VLS_35;
    let __VLS_36;
    let __VLS_37;
    const __VLS_38 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_39 = {};
    var __VLS_34;
}
else if (__VLS_ctx.wt === 'date') {
    const __VLS_40 = {}.TDatePicker;
    /** @type {[typeof __VLS_components.TDatePicker, typeof __VLS_components.tDatePicker, ]} */ ;
    // @ts-ignore
    const __VLS_41 = __VLS_asFunctionalComponent(__VLS_40, new __VLS_40({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        ...{ style: {} },
    }));
    const __VLS_42 = __VLS_41({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请选择' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        ...{ style: {} },
    }, ...__VLS_functionalComponentArgsRest(__VLS_41));
    let __VLS_44;
    let __VLS_45;
    let __VLS_46;
    const __VLS_47 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_48 = {};
    var __VLS_43;
}
else if (__VLS_ctx.wt === 'textarea' || __VLS_ctx.wt === 'html') {
    const __VLS_49 = {}.TTextarea;
    /** @type {[typeof __VLS_components.TTextarea, typeof __VLS_components.tTextarea, ]} */ ;
    // @ts-ignore
    const __VLS_50 = __VLS_asFunctionalComponent(__VLS_49, new __VLS_49({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        autosize: ({ minRows: 3 }),
    }));
    const __VLS_51 = __VLS_50({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
        autosize: ({ minRows: 3 }),
    }, ...__VLS_functionalComponentArgsRest(__VLS_50));
    let __VLS_53;
    let __VLS_54;
    let __VLS_55;
    const __VLS_56 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_57 = {};
    var __VLS_52;
}
else if (__VLS_ctx.wt === 'password') {
    const __VLS_58 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_59 = __VLS_asFunctionalComponent(__VLS_58, new __VLS_58({
        ...{ 'onChange': {} },
        type: "password",
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
    }));
    const __VLS_60 = __VLS_59({
        ...{ 'onChange': {} },
        type: "password",
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_59));
    let __VLS_62;
    let __VLS_63;
    let __VLS_64;
    const __VLS_65 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_66 = {};
    var __VLS_61;
}
else if (__VLS_ctx.wt === 'readonly') {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
        ...{ style: {} },
    });
    (__VLS_ctx.modelValue ?? '-');
}
else {
    const __VLS_67 = {}.TInput;
    /** @type {[typeof __VLS_components.TInput, typeof __VLS_components.tInput, ]} */ ;
    // @ts-ignore
    const __VLS_68 = __VLS_asFunctionalComponent(__VLS_67, new __VLS_67({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
    }));
    const __VLS_69 = __VLS_68({
        ...{ 'onChange': {} },
        value: (__VLS_ctx.modelValue),
        placeholder: ('请输入' + (__VLS_ctx.field.displayName || __VLS_ctx.field.name)),
    }, ...__VLS_functionalComponentArgsRest(__VLS_68));
    let __VLS_71;
    let __VLS_72;
    let __VLS_73;
    const __VLS_74 = {
        onChange: ((v) => __VLS_ctx.emit('update:modelValue', v))
    };
    var __VLS_75 = {};
    var __VLS_70;
}
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            emit: emit,
            wt: wt,
            options: options,
        };
    },
    __typeEmits: {},
    __typeProps: {},
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
    __typeEmits: {},
    __typeProps: {},
});
; /* PartiallyEnd: #4569/main.vue */
