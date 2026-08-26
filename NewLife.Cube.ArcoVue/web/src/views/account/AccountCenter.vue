<template>
  <div class="account-center">
    <div class="account-surface">
      <div class="account-header">
        <h2>账号中心</h2>
      </div>
      <a-tabs :active-key="tab" @change="onTabChange">
        <a-tab-pane key="profile" title="资料">
          <a-spin :loading="profileLoading" style="width: 100%">
            <div v-if="ssoProfileUrl" class="form-groups">
              <section class="form-group">
                <a-row :gutter="16" justify="center">
                  <a-col :span="12">
                    <p class="hint">当前已配置跳转用户中心，请在用户中心修改资料。</p>
                  </a-col>
                </a-row>
              </section>
            </div>
            <a-form v-else layout="vertical" class="account-form">
              <div class="form-groups">
                <section class="form-group">
                  <a-row
                    v-for="f in profileFields"
                    :key="f.name"
                    :gutter="16"
                    justify="center"
                  >
                    <a-col :span="12">
                      <a-form-item :label="f.displayName || f.name">
                        <FieldInput
                          :field="f"
                          :model-value="profileValue(f.name)"
                          :disabled="!!f.readOnly"
                          @update:model-value="onProfileField(f.name, $event)"
                        />
                      </a-form-item>
                    </a-col>
                  </a-row>
                </section>
              </div>
            </a-form>
          </a-spin>
        </a-tab-pane>
        <a-tab-pane key="password" title="密码">
          <div v-if="ssoPasswordUrl" class="form-groups">
            <section class="form-group">
              <a-row :gutter="16" justify="center">
                <a-col :span="12">
                  <p class="hint">当前已配置跳转用户中心，请在用户中心修改密码。</p>
                </a-col>
              </a-row>
            </section>
          </div>
          <a-form v-else layout="vertical" class="account-form">
            <div class="form-groups">
              <section class="form-group">
                <a-row
                  v-for="f in passwordFields"
                  :key="f.name"
                  :gutter="16"
                  justify="center"
                >
                  <a-col :span="12">
                    <a-form-item :label="f.displayName" :tooltip="f.description">
                      <a-input-password
                        v-model="passwordForm[f.name]"
                        allow-clear
                        style="width: 100%"
                      />
                    </a-form-item>
                  </a-col>
                </a-row>
              </section>
            </div>
          </a-form>
        </a-tab-pane>
        <a-tab-pane key="security" title="安全">
          <SecuritySettings v-if="tab === 'security'" section="mfa" />
        </a-tab-pane>
        <a-tab-pane key="binds" title="绑定">
          <SecuritySettings v-if="tab === 'binds'" section="binds" />
        </a-tab-pane>
      </a-tabs>
    </div>
    <div v-if="footerKind" class="account-footer">
      <a-space>
        <a-button type="primary" :loading="footerLoading" @click="onFooterClick">
          {{ footerLabel }}
        </a-button>
      </a-space>
    </div>
  </div>
</template>

<script setup lang="ts">
import FieldInput from '@/components/FieldInput.vue';
import SecuritySettings from './SecuritySettings.vue';
import { useAccountCenter } from './useAccountCenter';

defineOptions({ name: 'AccountCenter' });

const {
  tab,
  onTabChange,
  ssoProfileUrl,
  ssoPasswordUrl,
  profileLoading,
  profileFields,
  passwordFields,
  passwordForm,
  onProfileField,
  profileValue,
  footerKind,
  footerLabel,
  footerLoading,
  onFooterClick,
} = useAccountCenter();
</script>

<style scoped>
.account-center {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
/* 主题表面：与多维视图 list-panel / 魔方设置 obj-surface 同源 */
.account-surface {
  min-width: 0;
  padding: 16px 16px 4px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.account-header {
  margin-bottom: 16px;
}
.account-header h2 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: var(--color-text-1);
}
.account-form {
  width: 100%;
}
.form-groups {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 12px;
}
.form-group {
  padding: 16px 16px 4px;
  background: var(--color-bg-1);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
.hint {
  color: var(--color-text-3);
  font-size: 13px;
  margin: 0 0 12px;
}
/* 底部卡片：参照魔方设置 obj-footer / 列表 list-pager */
.account-footer {
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px;
  background: var(--color-bg-2);
  border: 1px solid var(--color-border-2);
  border-radius: 8px;
}
</style>
