import { ref, watch } from 'vue';
import { Message } from '@arco-design/web-vue';
import cubeApi from '@/api';
import { formatApiError } from '@/core/utils/apiError';
import { getValueByKey } from '@/core/utils/url';
import type { OpsAutomationButton } from '@/core/utils/opsAction';
import type { ListContext } from './listContext';

/**
 * DefaultList 自动化入口：顶栏抽屉 + 行按钮规则（OSC-260815fa86）
 */
export function useListAutomation(ctx: ListContext) {
  const automationDrawerVisible = ref(false);
  const automationButtons = ref<OpsAutomationButton[]>([]);

  async function loadAutomationButtons() {
    const typePath = ctx.typePath.value;
    if (!typePath) {
      automationButtons.value = [];
      return;
    }
    try {
      const res = await cubeApi.automation.list({
        typePath: typePath.replace(/^\/+/, ''),
        enable: true,
        triggerKind: 'button',
      });
      automationButtons.value = (res.data ?? []).map((x) => ({
        id: x.id,
        name: x.buttonLabel || x.name || '运行',
      }));
    } catch {
      automationButtons.value = [];
    }
  }

  watch(ctx.typePath, () => {
    void loadAutomationButtons();
  }, { immediate: true });

  watch(automationDrawerVisible, (v, was) => {
    if (was && !v) void loadAutomationButtons();
  });

  function openAutomationDrawer() {
    automationDrawerVisible.value = true;
  }

  async function runAutomationButton(payload: { action: string; row: Record<string, unknown> }) {
    const id = payload.action.slice('auto:'.length);
    const recordKey = getValueByKey(payload.row, ctx.pkField.value);
    try {
      await cubeApi.automation.run({ automationId: id, recordKey: recordKey as string | number });
      Message.success('已开始运行');
    } catch (err) {
      Message.error(formatApiError(err, '运行失败'));
    }
  }

  return {
    automationDrawerVisible,
    automationButtons,
    loadAutomationButtons,
    openAutomationDrawer,
    runAutomationButton,
  };
}
