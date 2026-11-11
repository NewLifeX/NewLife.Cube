<script setup lang="ts">
/**
 * 用户角色快捷设置。只改主角色与角色组，不打开整张编辑表。
 */
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import request from '@newlifex/cube-vue/core/utils/request';
import { enumOptionValue } from '@newlifex/cube-vue/core/utils/fieldControl';

interface FieldMetaLite {
  name: string;
  displayName?: string;
  mapField?: string;
  dataSource?: Record<string, string>;
}

const props = defineProps<{ modelValue: boolean; userId: string; userName: string }>();
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; saved: [] }>();

const { t } = useI18n();

const visible = ref(props.modelValue);
const loading = ref(false);
const saving = ref(false);
const primaryLabel = ref('');
const groupLabel = ref('');
const primaryOptions = ref<Array<{ value: number; label: string }>>([]);
const groupOptions = ref<Array<{ value: number; label: string }>>([]);
const roleId = ref<number | null>(null);
const roleIds = ref<number[]>([]);
// 登录名。更新接口把 Name 当必填，只提交角色会被拒，保存时原样带回且不修改。
const accountName = ref('');

watch(
  () => props.modelValue,
  (value) => {
    visible.value = value;
    if (value) void load();
  },
);
watch(visible, (value) => emit('update:modelValue', value));

function payloadOf(res: unknown): Record<string, unknown> {
  if (res && typeof res === 'object' && 'data' in res) {
    const data = (res as { data?: unknown }).data;
    if (data && typeof data === 'object') return data as Record<string, unknown>;
  }
  return (res ?? {}) as Record<string, unknown>;
}

function toOptions(ds: Record<string, string> | undefined): Array<{ value: number; label: string }> {
  const options: Array<{ value: number; label: string }> = [];
  for (const [key, label] of Object.entries(ds ?? {})) {
    const value = enumOptionValue(key);
    if (typeof value === 'number') options.push({ value, label: String(label) });
  }
  return options;
}

function readNumber(row: Record<string, unknown>, keys: string[]): number | undefined {
  for (const key of keys) {
    if (!(key in row)) continue;
    const raw = row[key];
    if (raw === null || raw === undefined || raw === '') return undefined;
    const n = Number(raw);
    if (!Number.isNaN(n)) return n;
  }
  return undefined;
}

function readRoleIds(row: Record<string, unknown>, allowed: Set<number>): number[] {
  const raw = row.roleIds ?? row.RoleIds;
  if (raw == null || raw === '') return [];
  const parts = Array.isArray(raw) ? raw.map(String) : String(raw).split(',');
  const ids: number[] = [];
  for (const part of parts) {
    const text = part.trim();
    if (!/^-?\d+$/.test(text)) continue;
    const n = Number(text);
    if (allowed.has(n) && !ids.includes(n)) ids.push(n);
  }
  return ids;
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  try {
    const [metaRes, detailRes] = await Promise.all([
      request({ url: '/api/Admin/User/meta', method: 'get' }),
      request({ url: `/api/Admin/User/${props.userId}`, method: 'get' }),
    ]);
    const meta = payloadOf(metaRes);
    const detail = payloadOf(detailRes);
    const form = (Array.isArray(meta.form) ? meta.form : []) as FieldMetaLite[];
    const primary = form.find((field) => field.mapField === 'RoleID' || field.name === 'RoleName');
    const group = form.find((field) => field.name === 'RoleIds');

    primaryLabel.value = primary?.displayName || t('userAdmin.primaryRole');
    groupLabel.value = group?.displayName || t('userAdmin.roleGroup');
    primaryOptions.value = toOptions(primary?.dataSource);
    groupOptions.value = toOptions(group?.dataSource);

    const current = readNumber(detail, ['roleID', 'RoleID', 'roleId']);
    const known = current !== undefined && primaryOptions.value.some((item) => item.value === current);
    if (known) {
      roleId.value = current ?? null;
    } else {
      const label = String(detail.roleName ?? detail.RoleName ?? '');
      roleId.value = primaryOptions.value.find((item) => item.label === label)?.value ?? null;
    }
    const allowed = new Set(groupOptions.value.map((item) => item.value));
    roleIds.value = readRoleIds(detail, allowed);
    accountName.value = String(detail.name ?? detail.Name ?? '');
  } catch {
    ElMessage.error(t('common.loadFailed'));
    primaryOptions.value = [];
    groupOptions.value = [];
    roleId.value = null;
    roleIds.value = [];
    accountName.value = '';
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const ids = roleIds.value;
    await request({
      url: `/api/Admin/User/${props.userId}`,
      method: 'put',
      data: {
        name: accountName.value,
        roleID: roleId.value ?? 0,
        roleIds: ids.length ? `,${ids.join(',')},` : '',
      },
    });
    ElMessage.success(t('common.saveSuccess'));
    emit('saved');
    visible.value = false;
  } catch {
    /* 错误已由 request 拦截器提示 */
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <el-dialog v-model="visible" :title="`${t('userAdmin.setRole')} - ${props.userName}`" width="min(480px, calc(100vw - 24px))">
    <el-form v-loading="loading" label-width="88px">
      <el-form-item :label="primaryLabel">
        <el-select
          v-model="roleId"
          filterable
          clearable
          class="role-select"
          :placeholder="t('userAdmin.rolePlaceholder')"
        >
          <el-option v-for="item in primaryOptions" :key="item.value" :label="item.label" :value="item.value" />
        </el-select>
      </el-form-item>
      <el-form-item :label="groupLabel">
        <el-select
          v-model="roleIds"
          multiple
          filterable
          collapse-tags
          collapse-tags-tooltip
          class="role-select"
          :placeholder="t('userAdmin.roleGroupPlaceholder')"
        >
          <el-option v-for="item in groupOptions" :key="item.value" :label="item.label" :value="item.value" />
        </el-select>
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="visible = false">{{ t('button.cancel') }}</el-button>
      <el-button type="primary" :loading="saving" @click="save">{{ t('button.save') }}</el-button>
    </template>
  </el-dialog>
</template>

<style lang="scss" scoped>
.role-select {
  width: 100%;
}
</style>
