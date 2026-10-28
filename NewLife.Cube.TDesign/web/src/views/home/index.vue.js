import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { appendMonitorPoint, buildMonitorChart, buildWorkbenchView, buildWidgetLayout, createWidgetLayoutLoader, createWorkbenchLoader, mergeWidgetsWithLayout, moveWidget, MONITOR_POLL_INTERVAL, } from '@newlifex/cube';
import { api } from '@/api';
const router = useRouter();
const loader = createWorkbenchLoader(api);
const layoutLoader = createWidgetLayoutLoader(api);
const raw = ref(null);
const loading = ref(false);
const error = ref('');
const monitorPoints = ref([]);
// --- 卡片布局：拖动排序 / 隐藏 / 持久化 ---
const layout = ref({});
const visibleWidgets = ref([]);
const hiddenWidgets = ref([]);
const layoutEditing = ref(false);
const savingLayout = ref(false);
const draggingName = ref('');
const layoutTip = ref('');
let timer = null;
let polling = false;
const profileLabels = ['登录次数', '最后登录', '登录地址', '注册时间'];
const view = computed(() => (raw.value ? buildWorkbenchView(raw.value) : null));
const latestMonitor = computed(() => monitorPoints.value[monitorPoints.value.length - 1] ?? null);
const chart = computed(() => buildMonitorChart(monitorPoints.value, 320, 120));
const today = computed(() => new Date().toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' }));
const greeting = computed(() => {
    const hour = new Date().getHours();
    if (hour < 6)
        return '夜深了';
    if (hour < 12)
        return '早上好';
    if (hour < 14)
        return '中午好';
    if (hour < 18)
        return '下午好';
    return '晚上好';
});
async function load() {
    loading.value = true;
    error.value = '';
    try {
        const result = await loader.load();
        if (result.data) {
            raw.value = result.data;
            if (!layoutEditing.value) {
                layout.value = await layoutLoader.load();
                applyLayout(buildWorkbenchView(result.data).widgets);
            }
        }
        else
            error.value = result.error || '工作台加载失败';
    }
    catch (ex) {
        error.value = ex instanceof Error ? ex.message : String(ex);
    }
    finally {
        loading.value = false;
    }
}
async function pollMonitor() {
    if (polling)
        return;
    polling = true;
    try {
        monitorPoints.value = appendMonitorPoint(monitorPoints.value, await loader.pollMonitor());
    }
    catch {
        // 监控曲线属于增强信息，单次失败不打断工作台主流程
    }
    finally {
        polling = false;
    }
}
function startPolling() {
    if (timer)
        return;
    void pollMonitor();
    timer = setInterval(() => void pollMonitor(), MONITOR_POLL_INTERVAL);
}
function stopPolling() {
    if (!timer)
        return;
    clearInterval(timer);
    timer = null;
}
async function reload() {
    await load();
    if (view.value?.hasChartWidget)
        startPolling();
    else
        stopPolling();
}
// --- 卡片布局 ---
function applyLayout(widgets) {
    const merged = mergeWidgetsWithLayout(widgets, layout.value);
    visibleWidgets.value = merged.visible;
    hiddenWidgets.value = merged.hidden;
}
function startLayoutEdit() {
    layoutEditing.value = true;
    layoutTip.value = '拖动卡片调整顺序，点击隐藏图标可隐藏卡片';
}
function cancelLayoutEdit() {
    layoutEditing.value = false;
    layoutTip.value = '';
    if (view.value)
        applyLayout(view.value.widgets);
}
function onDragStart(name) {
    draggingName.value = name;
}
function onDropOn(target) {
    if (!layoutEditing.value || !draggingName.value)
        return;
    visibleWidgets.value = moveWidget(visibleWidgets.value, draggingName.value, target);
    draggingName.value = '';
}
function hideWidget(name) {
    const index = visibleWidgets.value.findIndex((item) => item.name === name);
    if (index < 0)
        return;
    const [widget] = visibleWidgets.value.splice(index, 1);
    hiddenWidgets.value.push(widget);
}
function restoreWidget(name) {
    const index = hiddenWidgets.value.findIndex((item) => item.name === name);
    if (index < 0)
        return;
    const [widget] = hiddenWidgets.value.splice(index, 1);
    visibleWidgets.value.push(widget);
}
async function saveLayoutChanges() {
    savingLayout.value = true;
    try {
        const map = buildWidgetLayout(visibleWidgets.value, hiddenWidgets.value);
        if (await layoutLoader.save(map)) {
            layout.value = map;
            layoutTip.value = '布局已保存';
        }
        else {
            layoutTip.value = '布局保存失败';
        }
    }
    finally {
        savingLayout.value = false;
    }
}
async function resetLayoutChanges() {
    savingLayout.value = true;
    try {
        if (await layoutLoader.reset()) {
            layout.value = {};
            layoutTip.value = '布局已重置';
            if (view.value)
                applyLayout(view.value.widgets);
        }
        else {
            layoutTip.value = '布局重置失败';
        }
    }
    finally {
        savingLayout.value = false;
    }
}
function openKpi(url) {
    if (url)
        void router.push(url);
}
onMounted(async () => {
    await load();
    if (view.value?.hasChartWidget)
        startPolling();
});
onBeforeUnmount(stopPolling);
debugger; /* PartiallyEnd: #3632/scriptSetup.vue */
const __VLS_ctx = {};
let __VLS_components;
let __VLS_directives;
/** @type {__VLS_StyleScopedClasses['line-cpu']} */ ;
/** @type {__VLS_StyleScopedClasses['line-mem']} */ ;
/** @type {__VLS_StyleScopedClasses['legend-cpu']} */ ;
/** @type {__VLS_StyleScopedClasses['legend-mem']} */ ;
/** @type {__VLS_StyleScopedClasses['loginlog']} */ ;
/** @type {__VLS_StyleScopedClasses['loginlog']} */ ;
/** @type {__VLS_StyleScopedClasses['loginlog']} */ ;
/** @type {__VLS_StyleScopedClasses['loginlog']} */ ;
// CSS variable injection 
// CSS variable injection end 
__VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
    ...{ class: "home-page" },
});
const __VLS_0 = {}.TLoading;
/** @type {[typeof __VLS_components.TLoading, typeof __VLS_components.tLoading, typeof __VLS_components.TLoading, typeof __VLS_components.tLoading, ]} */ ;
// @ts-ignore
const __VLS_1 = __VLS_asFunctionalComponent(__VLS_0, new __VLS_0({
    loading: (__VLS_ctx.loading && !__VLS_ctx.view),
    size: "small",
    ...{ style: {} },
}));
const __VLS_2 = __VLS_1({
    loading: (__VLS_ctx.loading && !__VLS_ctx.view),
    size: "small",
    ...{ style: {} },
}, ...__VLS_functionalComponentArgsRest(__VLS_1));
__VLS_3.slots.default;
if (__VLS_ctx.error && !__VLS_ctx.view) {
    const __VLS_4 = {}.TAlert;
    /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
    // @ts-ignore
    const __VLS_5 = __VLS_asFunctionalComponent(__VLS_4, new __VLS_4({
        theme: "error",
        message: (`工作台加载失败：${__VLS_ctx.error}`),
    }));
    const __VLS_6 = __VLS_5({
        theme: "error",
        message: (`工作台加载失败：${__VLS_ctx.error}`),
    }, ...__VLS_functionalComponentArgsRest(__VLS_5));
    __VLS_7.slots.default;
    {
        const { operation: __VLS_thisSlot } = __VLS_7.slots;
        const __VLS_8 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_9 = __VLS_asFunctionalComponent(__VLS_8, new __VLS_8({
            ...{ 'onClick': {} },
            size: "small",
            variant: "outline",
        }));
        const __VLS_10 = __VLS_9({
            ...{ 'onClick': {} },
            size: "small",
            variant: "outline",
        }, ...__VLS_functionalComponentArgsRest(__VLS_9));
        let __VLS_12;
        let __VLS_13;
        let __VLS_14;
        const __VLS_15 = {
            onClick: (__VLS_ctx.reload)
        };
        __VLS_11.slots.default;
        var __VLS_11;
    }
    var __VLS_7;
}
else {
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
        ...{ class: "banner" },
    });
    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
    __VLS_asFunctionalElement(__VLS_intrinsicElements.h2, __VLS_intrinsicElements.h2)({
        ...{ class: "banner-title" },
    });
    (__VLS_ctx.greeting);
    (__VLS_ctx.view?.displayName || '-');
    __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
        ...{ class: "banner-sub" },
    });
    (__VLS_ctx.today);
    if (__VLS_ctx.view?.roleText) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
        (__VLS_ctx.view.roleText);
    }
    const __VLS_16 = {}.TSpace;
    /** @type {[typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, ]} */ ;
    // @ts-ignore
    const __VLS_17 = __VLS_asFunctionalComponent(__VLS_16, new __VLS_16({}));
    const __VLS_18 = __VLS_17({}, ...__VLS_functionalComponentArgsRest(__VLS_17));
    __VLS_19.slots.default;
    if (!__VLS_ctx.layoutEditing) {
        const __VLS_20 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_21 = __VLS_asFunctionalComponent(__VLS_20, new __VLS_20({
            ...{ 'onClick': {} },
            variant: "outline",
        }));
        const __VLS_22 = __VLS_21({
            ...{ 'onClick': {} },
            variant: "outline",
        }, ...__VLS_functionalComponentArgsRest(__VLS_21));
        let __VLS_24;
        let __VLS_25;
        let __VLS_26;
        const __VLS_27 = {
            onClick: (__VLS_ctx.startLayoutEdit)
        };
        __VLS_23.slots.default;
        var __VLS_23;
    }
    else {
        const __VLS_28 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_29 = __VLS_asFunctionalComponent(__VLS_28, new __VLS_28({
            ...{ 'onClick': {} },
            variant: "outline",
            loading: (__VLS_ctx.savingLayout),
        }));
        const __VLS_30 = __VLS_29({
            ...{ 'onClick': {} },
            variant: "outline",
            loading: (__VLS_ctx.savingLayout),
        }, ...__VLS_functionalComponentArgsRest(__VLS_29));
        let __VLS_32;
        let __VLS_33;
        let __VLS_34;
        const __VLS_35 = {
            onClick: (__VLS_ctx.saveLayoutChanges)
        };
        __VLS_31.slots.default;
        var __VLS_31;
        const __VLS_36 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_37 = __VLS_asFunctionalComponent(__VLS_36, new __VLS_36({
            ...{ 'onClick': {} },
            variant: "outline",
            loading: (__VLS_ctx.savingLayout),
        }));
        const __VLS_38 = __VLS_37({
            ...{ 'onClick': {} },
            variant: "outline",
            loading: (__VLS_ctx.savingLayout),
        }, ...__VLS_functionalComponentArgsRest(__VLS_37));
        let __VLS_40;
        let __VLS_41;
        let __VLS_42;
        const __VLS_43 = {
            onClick: (__VLS_ctx.resetLayoutChanges)
        };
        __VLS_39.slots.default;
        var __VLS_39;
        const __VLS_44 = {}.TButton;
        /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
        // @ts-ignore
        const __VLS_45 = __VLS_asFunctionalComponent(__VLS_44, new __VLS_44({
            ...{ 'onClick': {} },
            variant: "text",
        }));
        const __VLS_46 = __VLS_45({
            ...{ 'onClick': {} },
            variant: "text",
        }, ...__VLS_functionalComponentArgsRest(__VLS_45));
        let __VLS_48;
        let __VLS_49;
        let __VLS_50;
        const __VLS_51 = {
            onClick: (__VLS_ctx.cancelLayoutEdit)
        };
        __VLS_47.slots.default;
        var __VLS_47;
    }
    const __VLS_52 = {}.TButton;
    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
    // @ts-ignore
    const __VLS_53 = __VLS_asFunctionalComponent(__VLS_52, new __VLS_52({
        ...{ 'onClick': {} },
        loading: (__VLS_ctx.loading),
        variant: "outline",
    }));
    const __VLS_54 = __VLS_53({
        ...{ 'onClick': {} },
        loading: (__VLS_ctx.loading),
        variant: "outline",
    }, ...__VLS_functionalComponentArgsRest(__VLS_53));
    let __VLS_56;
    let __VLS_57;
    let __VLS_58;
    const __VLS_59 = {
        onClick: (__VLS_ctx.reload)
    };
    __VLS_55.slots.default;
    var __VLS_55;
    var __VLS_19;
    if (__VLS_ctx.error) {
        const __VLS_60 = {}.TAlert;
        /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
        // @ts-ignore
        const __VLS_61 = __VLS_asFunctionalComponent(__VLS_60, new __VLS_60({
            ...{ class: "banner-alert" },
            theme: "warning",
            message: (__VLS_ctx.error),
        }));
        const __VLS_62 = __VLS_61({
            ...{ class: "banner-alert" },
            theme: "warning",
            message: (__VLS_ctx.error),
        }, ...__VLS_functionalComponentArgsRest(__VLS_61));
    }
    if (__VLS_ctx.layoutTip) {
        const __VLS_64 = {}.TAlert;
        /** @type {[typeof __VLS_components.TAlert, typeof __VLS_components.tAlert, ]} */ ;
        // @ts-ignore
        const __VLS_65 = __VLS_asFunctionalComponent(__VLS_64, new __VLS_64({
            ...{ class: "banner-alert" },
            theme: "info",
            message: (__VLS_ctx.layoutTip),
        }));
        const __VLS_66 = __VLS_65({
            ...{ class: "banner-alert" },
            theme: "info",
            message: (__VLS_ctx.layoutTip),
        }, ...__VLS_functionalComponentArgsRest(__VLS_65));
    }
    if (__VLS_ctx.layoutEditing && __VLS_ctx.hiddenWidgets.length) {
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "hidden-widgets" },
        });
        __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
            ...{ class: "hidden-title" },
        });
        const __VLS_68 = {}.TSpace;
        /** @type {[typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, typeof __VLS_components.TSpace, typeof __VLS_components.tSpace, ]} */ ;
        // @ts-ignore
        const __VLS_69 = __VLS_asFunctionalComponent(__VLS_68, new __VLS_68({}));
        const __VLS_70 = __VLS_69({}, ...__VLS_functionalComponentArgsRest(__VLS_69));
        __VLS_71.slots.default;
        for (const [widget] of __VLS_getVForSourceType((__VLS_ctx.hiddenWidgets))) {
            const __VLS_72 = {}.TTag;
            /** @type {[typeof __VLS_components.TTag, typeof __VLS_components.tTag, typeof __VLS_components.TTag, typeof __VLS_components.tTag, ]} */ ;
            // @ts-ignore
            const __VLS_73 = __VLS_asFunctionalComponent(__VLS_72, new __VLS_72({
                ...{ 'onClick': {} },
                key: (widget.name),
                ...{ class: "hidden-tag" },
            }));
            const __VLS_74 = __VLS_73({
                ...{ 'onClick': {} },
                key: (widget.name),
                ...{ class: "hidden-tag" },
            }, ...__VLS_functionalComponentArgsRest(__VLS_73));
            let __VLS_76;
            let __VLS_77;
            let __VLS_78;
            const __VLS_79 = {
                onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.error && !__VLS_ctx.view))
                        return;
                    if (!(__VLS_ctx.layoutEditing && __VLS_ctx.hiddenWidgets.length))
                        return;
                    __VLS_ctx.restoreWidget(widget.name);
                }
            };
            __VLS_75.slots.default;
            (widget.title);
            var __VLS_75;
        }
        var __VLS_71;
    }
    if (__VLS_ctx.view?.kpis.length) {
        const __VLS_80 = {}.TRow;
        /** @type {[typeof __VLS_components.TRow, typeof __VLS_components.tRow, typeof __VLS_components.TRow, typeof __VLS_components.tRow, ]} */ ;
        // @ts-ignore
        const __VLS_81 = __VLS_asFunctionalComponent(__VLS_80, new __VLS_80({
            gutter: ([16, 16]),
            ...{ class: "kpis" },
        }));
        const __VLS_82 = __VLS_81({
            gutter: ([16, 16]),
            ...{ class: "kpis" },
        }, ...__VLS_functionalComponentArgsRest(__VLS_81));
        __VLS_83.slots.default;
        for (const [kpi] of __VLS_getVForSourceType((__VLS_ctx.view.kpis))) {
            const __VLS_84 = {}.TCol;
            /** @type {[typeof __VLS_components.TCol, typeof __VLS_components.tCol, typeof __VLS_components.TCol, typeof __VLS_components.tCol, ]} */ ;
            // @ts-ignore
            const __VLS_85 = __VLS_asFunctionalComponent(__VLS_84, new __VLS_84({
                key: (kpi.name),
                xs: (12),
                sm: (6),
                md: (3),
            }));
            const __VLS_86 = __VLS_85({
                key: (kpi.name),
                xs: (12),
                sm: (6),
                md: (3),
            }, ...__VLS_functionalComponentArgsRest(__VLS_85));
            __VLS_87.slots.default;
            const __VLS_88 = {}.TCard;
            /** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
            // @ts-ignore
            const __VLS_89 = __VLS_asFunctionalComponent(__VLS_88, new __VLS_88({
                ...{ 'onClick': {} },
                ...{ class: (['kpi', `kpi-${kpi.color}`]) },
                bordered: (true),
                hoverShadow: true,
            }));
            const __VLS_90 = __VLS_89({
                ...{ 'onClick': {} },
                ...{ class: (['kpi', `kpi-${kpi.color}`]) },
                bordered: (true),
                hoverShadow: true,
            }, ...__VLS_functionalComponentArgsRest(__VLS_89));
            let __VLS_92;
            let __VLS_93;
            let __VLS_94;
            const __VLS_95 = {
                onClick: (...[$event]) => {
                    if (!!(__VLS_ctx.error && !__VLS_ctx.view))
                        return;
                    if (!(__VLS_ctx.view?.kpis.length))
                        return;
                    __VLS_ctx.openKpi(kpi.url);
                }
            };
            __VLS_91.slots.default;
            __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                ...{ class: "kpi-label" },
            });
            (kpi.label);
            __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                ...{ class: "kpi-value" },
            });
            (kpi.value);
            if (kpi.trend) {
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "kpi-trend" },
                });
                (kpi.trend);
            }
            var __VLS_91;
            var __VLS_87;
        }
        var __VLS_83;
    }
    if (__VLS_ctx.visibleWidgets.length) {
        const __VLS_96 = {}.TRow;
        /** @type {[typeof __VLS_components.TRow, typeof __VLS_components.tRow, typeof __VLS_components.TRow, typeof __VLS_components.tRow, ]} */ ;
        // @ts-ignore
        const __VLS_97 = __VLS_asFunctionalComponent(__VLS_96, new __VLS_96({
            gutter: ([16, 16]),
            ...{ class: "widgets" },
        }));
        const __VLS_98 = __VLS_97({
            gutter: ([16, 16]),
            ...{ class: "widgets" },
        }, ...__VLS_functionalComponentArgsRest(__VLS_97));
        __VLS_99.slots.default;
        for (const [widget] of __VLS_getVForSourceType((__VLS_ctx.visibleWidgets))) {
            const __VLS_100 = {}.TCol;
            /** @type {[typeof __VLS_components.TCol, typeof __VLS_components.tCol, typeof __VLS_components.TCol, typeof __VLS_components.tCol, ]} */ ;
            // @ts-ignore
            const __VLS_101 = __VLS_asFunctionalComponent(__VLS_100, new __VLS_100({
                ...{ 'onDragstart': {} },
                ...{ 'onDragover': {} },
                ...{ 'onDrop': {} },
                key: (widget.name),
                xs: (12),
                md: (widget.cols),
                draggable: (__VLS_ctx.layoutEditing),
            }));
            const __VLS_102 = __VLS_101({
                ...{ 'onDragstart': {} },
                ...{ 'onDragover': {} },
                ...{ 'onDrop': {} },
                key: (widget.name),
                xs: (12),
                md: (widget.cols),
                draggable: (__VLS_ctx.layoutEditing),
            }, ...__VLS_functionalComponentArgsRest(__VLS_101));
            let __VLS_104;
            let __VLS_105;
            let __VLS_106;
            const __VLS_107 = {
                onDragstart: (...[$event]) => {
                    if (!!(__VLS_ctx.error && !__VLS_ctx.view))
                        return;
                    if (!(__VLS_ctx.visibleWidgets.length))
                        return;
                    __VLS_ctx.onDragStart(widget.name);
                }
            };
            const __VLS_108 = {
                onDragover: () => { }
            };
            const __VLS_109 = {
                onDrop: (...[$event]) => {
                    if (!!(__VLS_ctx.error && !__VLS_ctx.view))
                        return;
                    if (!(__VLS_ctx.visibleWidgets.length))
                        return;
                    __VLS_ctx.onDropOn(widget.name);
                }
            };
            __VLS_103.slots.default;
            const __VLS_110 = {}.TCard;
            /** @type {[typeof __VLS_components.TCard, typeof __VLS_components.tCard, typeof __VLS_components.TCard, typeof __VLS_components.tCard, ]} */ ;
            // @ts-ignore
            const __VLS_111 = __VLS_asFunctionalComponent(__VLS_110, new __VLS_110({
                bordered: (true),
                ...{ class: "widget" },
                dataWidget: (widget.name),
            }));
            const __VLS_112 = __VLS_111({
                bordered: (true),
                ...{ class: "widget" },
                dataWidget: (widget.name),
            }, ...__VLS_functionalComponentArgsRest(__VLS_111));
            __VLS_113.slots.default;
            {
                const { title: __VLS_thisSlot } = __VLS_113.slots;
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "widget-header" },
                });
                __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
                (widget.title);
                if (__VLS_ctx.layoutEditing) {
                    const __VLS_114 = {}.TButton;
                    /** @type {[typeof __VLS_components.TButton, typeof __VLS_components.tButton, typeof __VLS_components.TButton, typeof __VLS_components.tButton, ]} */ ;
                    // @ts-ignore
                    const __VLS_115 = __VLS_asFunctionalComponent(__VLS_114, new __VLS_114({
                        ...{ 'onClick': {} },
                        variant: "text",
                        size: "small",
                    }));
                    const __VLS_116 = __VLS_115({
                        ...{ 'onClick': {} },
                        variant: "text",
                        size: "small",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_115));
                    let __VLS_118;
                    let __VLS_119;
                    let __VLS_120;
                    const __VLS_121 = {
                        onClick: (...[$event]) => {
                            if (!!(__VLS_ctx.error && !__VLS_ctx.view))
                                return;
                            if (!(__VLS_ctx.visibleWidgets.length))
                                return;
                            if (!(__VLS_ctx.layoutEditing))
                                return;
                            __VLS_ctx.hideWidget(widget.name);
                        }
                    };
                    __VLS_117.slots.default;
                    var __VLS_117;
                }
            }
            if (widget.kind === 'monitor') {
                if (__VLS_ctx.monitorPoints.length) {
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                        ...{ class: "monitor" },
                    });
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.svg, __VLS_intrinsicElements.svg)({
                        viewBox: (`0 0 ${__VLS_ctx.chart.width} ${__VLS_ctx.chart.height}`),
                        ...{ class: "monitor-chart" },
                        preserveAspectRatio: "none",
                    });
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.polyline)({
                        ...{ class: "line-cpu" },
                        points: (__VLS_ctx.chart.cpuPoints),
                    });
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.polyline)({
                        ...{ class: "line-mem" },
                        points: (__VLS_ctx.chart.memPoints),
                    });
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                        ...{ class: "monitor-legend" },
                    });
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                        ...{ class: "legend-cpu" },
                    });
                    (__VLS_ctx.latestMonitor?.cpu ?? 0);
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                        ...{ class: "legend-mem" },
                    });
                    (__VLS_ctx.latestMonitor?.memory ?? 0);
                    if (__VLS_ctx.latestMonitor?.memUsed != null && __VLS_ctx.latestMonitor?.memTotal) {
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
                        (__VLS_ctx.latestMonitor.memUsed);
                        (__VLS_ctx.latestMonitor.memTotal);
                    }
                }
                else {
                    const __VLS_122 = {}.TEmpty;
                    /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
                    // @ts-ignore
                    const __VLS_123 = __VLS_asFunctionalComponent(__VLS_122, new __VLS_122({
                        description: "正在采集监控数据",
                    }));
                    const __VLS_124 = __VLS_123({
                        description: "正在采集监控数据",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_123));
                }
            }
            else if (widget.kind === 'profile' && widget.profile) {
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "profile-head" },
                });
                const __VLS_126 = {}.TAvatar;
                /** @type {[typeof __VLS_components.TAvatar, typeof __VLS_components.tAvatar, typeof __VLS_components.TAvatar, typeof __VLS_components.tAvatar, ]} */ ;
                // @ts-ignore
                const __VLS_127 = __VLS_asFunctionalComponent(__VLS_126, new __VLS_126({
                    size: "44px",
                }));
                const __VLS_128 = __VLS_127({
                    size: "44px",
                }, ...__VLS_functionalComponentArgsRest(__VLS_127));
                __VLS_129.slots.default;
                (widget.profile.initial);
                var __VLS_129;
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({});
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "profile-name" },
                });
                (widget.profile.displayName);
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "profile-tags" },
                });
                const __VLS_130 = {}.TTag;
                /** @type {[typeof __VLS_components.TTag, typeof __VLS_components.tTag, typeof __VLS_components.TTag, typeof __VLS_components.tTag, ]} */ ;
                // @ts-ignore
                const __VLS_131 = __VLS_asFunctionalComponent(__VLS_130, new __VLS_130({
                    size: "small",
                    theme: (widget.profile.online ? 'success' : 'default'),
                    variant: "light",
                }));
                const __VLS_132 = __VLS_131({
                    size: "small",
                    theme: (widget.profile.online ? 'success' : 'default'),
                    variant: "light",
                }, ...__VLS_functionalComponentArgsRest(__VLS_131));
                __VLS_133.slots.default;
                (widget.profile.online ? '在线' : '离线');
                var __VLS_133;
                for (const [role] of __VLS_getVForSourceType((widget.profile.roles))) {
                    const __VLS_134 = {}.TTag;
                    /** @type {[typeof __VLS_components.TTag, typeof __VLS_components.tTag, typeof __VLS_components.TTag, typeof __VLS_components.tTag, ]} */ ;
                    // @ts-ignore
                    const __VLS_135 = __VLS_asFunctionalComponent(__VLS_134, new __VLS_134({
                        key: (role),
                        size: "small",
                        variant: "light",
                    }));
                    const __VLS_136 = __VLS_135({
                        key: (role),
                        size: "small",
                        variant: "light",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_135));
                    __VLS_137.slots.default;
                    (role);
                    var __VLS_137;
                }
                const __VLS_138 = {}.TDescriptions;
                /** @type {[typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, ]} */ ;
                // @ts-ignore
                const __VLS_139 = __VLS_asFunctionalComponent(__VLS_138, new __VLS_138({
                    column: (2),
                    size: "small",
                    bordered: true,
                }));
                const __VLS_140 = __VLS_139({
                    column: (2),
                    size: "small",
                    bordered: true,
                }, ...__VLS_functionalComponentArgsRest(__VLS_139));
                __VLS_141.slots.default;
                for (const [row, index] of __VLS_getVForSourceType((widget.profile.rows))) {
                    const __VLS_142 = {}.TDescriptionsItem;
                    /** @type {[typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, ]} */ ;
                    // @ts-ignore
                    const __VLS_143 = __VLS_asFunctionalComponent(__VLS_142, new __VLS_142({
                        key: (row.key),
                        label: (__VLS_ctx.profileLabels[index] || row.key),
                    }));
                    const __VLS_144 = __VLS_143({
                        key: (row.key),
                        label: (__VLS_ctx.profileLabels[index] || row.key),
                    }, ...__VLS_functionalComponentArgsRest(__VLS_143));
                    __VLS_145.slots.default;
                    (row.text);
                    var __VLS_145;
                }
                var __VLS_141;
            }
            else if (widget.kind === 'quickLinks') {
                if (widget.links?.length) {
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                        ...{ class: "links" },
                    });
                    for (const [link] of __VLS_getVForSourceType((widget.links))) {
                        const __VLS_146 = {}.RouterLink;
                        /** @type {[typeof __VLS_components.RouterLink, typeof __VLS_components.routerLink, typeof __VLS_components.RouterLink, typeof __VLS_components.routerLink, ]} */ ;
                        // @ts-ignore
                        const __VLS_147 = __VLS_asFunctionalComponent(__VLS_146, new __VLS_146({
                            key: (link.url),
                            to: (link.url),
                            ...{ class: "link" },
                        }));
                        const __VLS_148 = __VLS_147({
                            key: (link.url),
                            to: (link.url),
                            ...{ class: "link" },
                        }, ...__VLS_functionalComponentArgsRest(__VLS_147));
                        __VLS_149.slots.default;
                        (link.name);
                        var __VLS_149;
                    }
                }
                else {
                    const __VLS_150 = {}.TEmpty;
                    /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
                    // @ts-ignore
                    const __VLS_151 = __VLS_asFunctionalComponent(__VLS_150, new __VLS_150({
                        description: "暂无快捷入口",
                    }));
                    const __VLS_152 = __VLS_151({
                        description: "暂无快捷入口",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_151));
                }
            }
            else if (widget.kind === 'loginLog') {
                __VLS_asFunctionalElement(__VLS_intrinsicElements.div, __VLS_intrinsicElements.div)({
                    ...{ class: "loginlog" },
                });
                __VLS_asFunctionalElement(__VLS_intrinsicElements.section, __VLS_intrinsicElements.section)({});
                __VLS_asFunctionalElement(__VLS_intrinsicElements.h4, __VLS_intrinsicElements.h4)({});
                if (widget.logins?.length) {
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.ul, __VLS_intrinsicElements.ul)({});
                    for (const [item, index] of __VLS_getVForSourceType((widget.logins))) {
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.li, __VLS_intrinsicElements.li)({
                            key: (`login-${index}`),
                        });
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
                        (item.title);
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                            ...{ class: "muted" },
                        });
                        (item.subtitle);
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.time, __VLS_intrinsicElements.time)({});
                        (item.time);
                    }
                }
                else {
                    const __VLS_154 = {}.TEmpty;
                    /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
                    // @ts-ignore
                    const __VLS_155 = __VLS_asFunctionalComponent(__VLS_154, new __VLS_154({
                        description: "暂无登录记录",
                    }));
                    const __VLS_156 = __VLS_155({
                        description: "暂无登录记录",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_155));
                }
                __VLS_asFunctionalElement(__VLS_intrinsicElements.section, __VLS_intrinsicElements.section)({});
                __VLS_asFunctionalElement(__VLS_intrinsicElements.h4, __VLS_intrinsicElements.h4)({});
                if (widget.onlines?.length) {
                    __VLS_asFunctionalElement(__VLS_intrinsicElements.ul, __VLS_intrinsicElements.ul)({});
                    for (const [item, index] of __VLS_getVForSourceType((widget.onlines))) {
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.li, __VLS_intrinsicElements.li)({
                            key: (`online-${index}`),
                        });
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
                        (item.title);
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({
                            ...{ class: "muted" },
                        });
                        (item.subtitle);
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.time, __VLS_intrinsicElements.time)({});
                        (item.time);
                    }
                }
                else {
                    const __VLS_158 = {}.TEmpty;
                    /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
                    // @ts-ignore
                    const __VLS_159 = __VLS_asFunctionalComponent(__VLS_158, new __VLS_158({
                        description: "暂无在线用户",
                    }));
                    const __VLS_160 = __VLS_159({
                        description: "暂无在线用户",
                    }, ...__VLS_functionalComponentArgsRest(__VLS_159));
                }
            }
            else if (widget.kind === 'kv' && widget.rows?.length) {
                const __VLS_162 = {}.TDescriptions;
                /** @type {[typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, typeof __VLS_components.TDescriptions, typeof __VLS_components.tDescriptions, ]} */ ;
                // @ts-ignore
                const __VLS_163 = __VLS_asFunctionalComponent(__VLS_162, new __VLS_162({
                    column: (1),
                    size: "small",
                    bordered: true,
                }));
                const __VLS_164 = __VLS_163({
                    column: (1),
                    size: "small",
                    bordered: true,
                }, ...__VLS_functionalComponentArgsRest(__VLS_163));
                __VLS_165.slots.default;
                for (const [row] of __VLS_getVForSourceType((widget.rows))) {
                    const __VLS_166 = {}.TDescriptionsItem;
                    /** @type {[typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, typeof __VLS_components.TDescriptionsItem, typeof __VLS_components.tDescriptionsItem, ]} */ ;
                    // @ts-ignore
                    const __VLS_167 = __VLS_asFunctionalComponent(__VLS_166, new __VLS_166({
                        key: (row.key),
                        label: (row.key),
                    }));
                    const __VLS_168 = __VLS_167({
                        key: (row.key),
                        label: (row.key),
                    }, ...__VLS_functionalComponentArgsRest(__VLS_167));
                    __VLS_169.slots.default;
                    if (row.link) {
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.a, __VLS_intrinsicElements.a)({
                            href: (row.link.href),
                            target: (row.link.external ? '_blank' : undefined),
                            rel: "noopener noreferrer",
                        });
                        (row.link.text);
                    }
                    else {
                        __VLS_asFunctionalElement(__VLS_intrinsicElements.span, __VLS_intrinsicElements.span)({});
                        (row.text);
                    }
                    var __VLS_169;
                }
                var __VLS_165;
            }
            else if (widget.kind === 'text') {
                __VLS_asFunctionalElement(__VLS_intrinsicElements.p, __VLS_intrinsicElements.p)({
                    ...{ class: "text" },
                });
                (widget.text);
            }
            else {
                const __VLS_170 = {}.TEmpty;
                /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
                // @ts-ignore
                const __VLS_171 = __VLS_asFunctionalComponent(__VLS_170, new __VLS_170({
                    description: "暂无数据",
                }));
                const __VLS_172 = __VLS_171({
                    description: "暂无数据",
                }, ...__VLS_functionalComponentArgsRest(__VLS_171));
            }
            var __VLS_113;
            var __VLS_103;
        }
        var __VLS_99;
    }
    if (__VLS_ctx.view && !__VLS_ctx.view.hasContent) {
        const __VLS_174 = {}.TEmpty;
        /** @type {[typeof __VLS_components.TEmpty, typeof __VLS_components.tEmpty, ]} */ ;
        // @ts-ignore
        const __VLS_175 = __VLS_asFunctionalComponent(__VLS_174, new __VLS_174({
            ...{ class: "empty" },
            description: "暂无工作台数据",
        }));
        const __VLS_176 = __VLS_175({
            ...{ class: "empty" },
            description: "暂无工作台数据",
        }, ...__VLS_functionalComponentArgsRest(__VLS_175));
    }
}
var __VLS_3;
/** @type {__VLS_StyleScopedClasses['home-page']} */ ;
/** @type {__VLS_StyleScopedClasses['banner']} */ ;
/** @type {__VLS_StyleScopedClasses['banner-title']} */ ;
/** @type {__VLS_StyleScopedClasses['banner-sub']} */ ;
/** @type {__VLS_StyleScopedClasses['banner-alert']} */ ;
/** @type {__VLS_StyleScopedClasses['banner-alert']} */ ;
/** @type {__VLS_StyleScopedClasses['hidden-widgets']} */ ;
/** @type {__VLS_StyleScopedClasses['hidden-title']} */ ;
/** @type {__VLS_StyleScopedClasses['hidden-tag']} */ ;
/** @type {__VLS_StyleScopedClasses['kpis']} */ ;
/** @type {__VLS_StyleScopedClasses['kpi']} */ ;
/** @type {__VLS_StyleScopedClasses['kpi-label']} */ ;
/** @type {__VLS_StyleScopedClasses['kpi-value']} */ ;
/** @type {__VLS_StyleScopedClasses['kpi-trend']} */ ;
/** @type {__VLS_StyleScopedClasses['widgets']} */ ;
/** @type {__VLS_StyleScopedClasses['widget']} */ ;
/** @type {__VLS_StyleScopedClasses['widget-header']} */ ;
/** @type {__VLS_StyleScopedClasses['monitor']} */ ;
/** @type {__VLS_StyleScopedClasses['monitor-chart']} */ ;
/** @type {__VLS_StyleScopedClasses['line-cpu']} */ ;
/** @type {__VLS_StyleScopedClasses['line-mem']} */ ;
/** @type {__VLS_StyleScopedClasses['monitor-legend']} */ ;
/** @type {__VLS_StyleScopedClasses['legend-cpu']} */ ;
/** @type {__VLS_StyleScopedClasses['legend-mem']} */ ;
/** @type {__VLS_StyleScopedClasses['profile-head']} */ ;
/** @type {__VLS_StyleScopedClasses['profile-name']} */ ;
/** @type {__VLS_StyleScopedClasses['profile-tags']} */ ;
/** @type {__VLS_StyleScopedClasses['links']} */ ;
/** @type {__VLS_StyleScopedClasses['link']} */ ;
/** @type {__VLS_StyleScopedClasses['loginlog']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['muted']} */ ;
/** @type {__VLS_StyleScopedClasses['text']} */ ;
/** @type {__VLS_StyleScopedClasses['empty']} */ ;
var __VLS_dollars;
const __VLS_self = (await import('vue')).defineComponent({
    setup() {
        return {
            loading: loading,
            error: error,
            monitorPoints: monitorPoints,
            visibleWidgets: visibleWidgets,
            hiddenWidgets: hiddenWidgets,
            layoutEditing: layoutEditing,
            savingLayout: savingLayout,
            layoutTip: layoutTip,
            profileLabels: profileLabels,
            view: view,
            latestMonitor: latestMonitor,
            chart: chart,
            today: today,
            greeting: greeting,
            reload: reload,
            startLayoutEdit: startLayoutEdit,
            cancelLayoutEdit: cancelLayoutEdit,
            onDragStart: onDragStart,
            onDropOn: onDropOn,
            hideWidget: hideWidget,
            restoreWidget: restoreWidget,
            saveLayoutChanges: saveLayoutChanges,
            resetLayoutChanges: resetLayoutChanges,
            openKpi: openKpi,
        };
    },
});
export default (await import('vue')).defineComponent({
    setup() {
        return {};
    },
});
; /* PartiallyEnd: #4569/main.vue */
