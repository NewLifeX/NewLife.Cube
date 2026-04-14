import { useAppStore } from '@/stores/app';
import { useUserStore } from '@/stores/user';
const appStore = useAppStore();
const userStore = useUserStore();
const stats = [
    { label: '用户数', value: '-', prefix: '' },
    { label: '角色数', value: '-', prefix: '' },
    { label: '在线数', value: '-', prefix: '' },
    { label: '今日访问', value: '-', prefix: '' },
];
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
__VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({
    ...{ style: {} },
});
(__VLS_ctx.userStore.displayName);
__VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
    ...{ style: {} },
});
(__VLS_ctx.appStore.siteTitle);
const __VLS_0 = {}.TRow;
/** @type {[typeof __VLS_components.TRow, typeof __VLS_components.tRow, typeof __VLS_components.TRow, typeof __VLS_components.tRow, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    gutter: (16),
}));
const __VLS_2 = __VLS_1({
    gutter: (16),
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
for (const [stat] of __VLS_getVForSourceType((__VLS_ctx.stats))) {
    const __VLS_4 = {}.TCol;
    /** @type {[typeof __VLS_components.TCol, typeof __VLS_components.tCol, typeof __VLS_components.TCol, typeof __VLS_components.tCol, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        key: (stat.label),
        span: (3),
    }));
    const __VLS_6 = __VLS_5({
        key: (stat.label),
        span: (3),
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
    __VLS_7.slots.default;
    const __VLS_8 = {}.TCard;
    /** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
    // @ts-ignore
    const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
        bordered: (true),
    }));
    const __VLS_10 = __VLS_9({
        bordered: (true),
    }, ...__VLS_functionalComponentArgsRest(__VLS_9));
    __VLS_11.slots.default;
    const __VLS_12 = {}.TStatistic;
    /** @type {[typeof __VLS_components.TStatistic, typeof __VLS_components.tStatistic, ]} */ ;
    // @ts-ignore
    const __VLS_13 = __VLS_asFunctionalComponent(__VLS_12, new __VLS_12({
        title: (stat.label),
        value: (stat.value),
        prefix: (stat.prefix),
    }));
    const __VLS_14 = __VLS_13({
        title: (stat.label),
        value: (stat.value),
        prefix: (stat.prefix),
    }, ...__VLS_functionalComponentArgsRest(__VLS_13));
    var __VLS_11;
    var __VLS_7;
}
var __VLS_3;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            appStore: appStore,
            userStore: userStore,
            stats: stats,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
