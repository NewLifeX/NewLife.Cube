<script setup lang="ts">
/**
 * 工作台内容卡主体
 *
 * 按后端 Widget 名称/类型渲染：Profile（个人信息）、QuickLink（快捷入口）、
 * LoginLog（登录与在线）、Kv（系统信息等键值卡），其余对象型数据走通用键值表兜底。
 * 后端可能返回 HTML 片段（如系统信息的“更多信息”链接），这里只解析 href/text，
 * 不使用 v-html，避免把服务端字符串直接当 HTML 注入。
 */
import { computed } from 'vue';
import { Link as LinkIcon } from '@element-plus/icons-vue';
import type {
  LoginLogWidgetData,
  QuickLinkWidgetData,
  WorkbenchProfile,
  WorkbenchQuickLink,
  WorkbenchWidget,
} from '../../types/workbench';
import { asRecord, extractLink, formatCell, isExternalLink, normalizeRoles } from './widget';

interface Props {
  /** 内容卡元数据与数据 */
  widget: WorkbenchWidget;
}

const props = defineProps<Props>();

const payload = computed(() => asRecord(props.widget.data));
const profile = computed(() => payload.value as WorkbenchProfile);
const profileName = computed(() => profile.value.displayName || profile.value.name || '-');
const profileInitial = computed(() => Array.from(profileName.value)[0] || '?');
const roles = computed(() => normalizeRoles(profile.value.roleNames));

const quickLinks = computed<WorkbenchQuickLink[]>(() => {
  const data = payload.value as QuickLinkWidgetData;
  return Array.isArray(data.links) ? data.links.filter((item) => item?.url) : [];
});

const loginLog = computed(() => payload.value as LoginLogWidgetData);
const logins = computed(() => (Array.isArray(loginLog.value.logins) ? loginLog.value.logins : []));
const onlines = computed(() => (Array.isArray(loginLog.value.onlines) ? loginLog.value.onlines : []));

const valueRows = computed(() =>
  Object.entries(payload.value).map(([key, value]) => ({
    key,
    text: formatCell(value),
    link: extractLink(value),
  })),
);

const plainText = computed(() => {
  const data = props.widget.data;
  if (data === null || data === undefined || data === '') return '';
  if (typeof data === 'object') return Array.isArray(data) ? formatCell(data) : '';

  return formatCell(data);
});
</script>

<template>
  <div class="wb-body">
    <!-- 个人信息 -->
    <template v-if="widget.name === 'Profile'">
      <div class="wb-profile">
        <div class="wb-profile-head">
          <el-avatar :size="48" class="wb-avatar">{{ profileInitial }}</el-avatar>
          <div class="wb-profile-title">
            <div class="wb-profile-name">{{ profileName }}</div>
            <div class="wb-profile-meta">
              <el-tag v-if="profile.online" type="success" size="small" effect="plain">在线</el-tag>
              <el-tag v-else type="info" size="small" effect="plain">离线</el-tag>
              <el-tag v-for="role in roles" :key="role" size="small" effect="plain">{{ role }}</el-tag>
            </div>
          </div>
        </div>
        <dl class="wb-kv">
          <dt>登录次数</dt>
          <dd>{{ profile.logins ?? 0 }}</dd>
          <dt>上次登录</dt>
          <dd>{{ profile.lastLogin || '-' }}</dd>
          <dt>登录 IP</dt>
          <dd>{{ profile.lastLoginIP || '-' }}</dd>
          <dt>注册时间</dt>
          <dd>{{ profile.registerTime || '-' }}</dd>
        </dl>
      </div>
    </template>

    <!-- 快捷入口 -->
    <template v-else-if="widget.name === 'QuickLink'">
      <div v-if="quickLinks.length" class="wb-links">
        <router-link v-for="link in quickLinks" :key="link.url" :to="link.url" class="wb-link">
          <el-icon><LinkIcon /></el-icon>
          <span>{{ link.name }}</span>
        </router-link>
      </div>
      <el-empty v-else :image-size="48" description="暂无快捷入口" />
    </template>

    <!-- 登录与在线 -->
    <template v-else-if="widget.name === 'LoginLog'">
      <div class="wb-loginlog">
        <section class="wb-loginlog-section">
          <h4>最近登录</h4>
          <ul v-if="logins.length" class="wb-list">
            <li v-for="(item, index) in logins" :key="`login-${index}`">
              <span class="wb-list-main">{{ item.userName || '-' }}</span>
              <span class="wb-list-sub">{{ item.action || '-' }}</span>
              <time class="wb-list-time">{{ item.createTime || '-' }}</time>
            </li>
          </ul>
          <el-empty v-else :image-size="40" description="暂无登录记录" />
        </section>

        <section class="wb-loginlog-section">
          <h4>在线用户</h4>
          <ul v-if="onlines.length" class="wb-list">
            <li v-for="(item, index) in onlines" :key="`online-${index}`">
              <span class="wb-list-main">{{ item.name || '-' }}</span>
              <span class="wb-list-sub">{{ item.oAuthProvider || '本地登录' }}</span>
              <time class="wb-list-time">{{ item.createTime || '-' }}</time>
            </li>
          </ul>
          <el-empty v-else :image-size="40" description="暂无在线用户" />
        </section>
      </div>
    </template>

    <!-- 键值卡 / 通用对象卡 -->
    <dl v-else-if="valueRows.length" class="wb-kv">
      <template v-for="row in valueRows" :key="row.key">
        <dt>{{ row.key }}</dt>
        <dd>
          <a v-if="row.link && isExternalLink(row.link.href)" :href="row.link.href" target="_blank" rel="noopener noreferrer">
            {{ row.link.text }}
          </a>
          <router-link v-else-if="row.link" :to="row.link.href">{{ row.link.text }}</router-link>
          <span v-else>{{ row.text }}</span>
        </dd>
      </template>
    </dl>

    <p v-else-if="plainText" class="wb-text">{{ plainText }}</p>
    <el-empty v-else :image-size="48" description="暂无数据" />
  </div>
</template>

<style lang="scss" scoped>
.wb-body {
  min-height: 80px;
}

.wb-profile-head {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 14px;
}

.wb-avatar {
  flex: none;
  background: var(--el-color-primary-light-8);
  color: var(--el-color-primary);
  font-size: 20px;
  font-weight: 600;
}

.wb-profile-name {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.wb-profile-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 6px;
}

.wb-kv {
  display: grid;
  grid-template-columns: minmax(72px, max-content) minmax(0, 1fr);
  gap: 8px 16px;
  margin: 0;
  font-size: 13px;

  dt {
    color: var(--el-text-color-secondary);
  }

  dd {
    margin: 0;
    color: var(--el-text-color-primary);
    overflow-wrap: anywhere;
  }

  a {
    color: var(--el-color-primary);
    text-decoration: none;
  }
}

.wb-links {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 8px;
}

.wb-link {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 36px;
  padding: 0 10px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: var(--el-border-radius-base);
  color: var(--el-text-color-primary);
  text-decoration: none;
  transition:
    border-color 0.2s,
    color 0.2s,
    background-color 0.2s;

  &:hover {
    border-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
  }

  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.wb-loginlog {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 16px;
}

.wb-loginlog-section {
  min-width: 0;

  h4 {
    margin: 0 0 8px;
    font-size: 13px;
    font-weight: 600;
    color: var(--el-text-color-regular);
  }
}

.wb-list {
  display: grid;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;

  li {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 2px 12px;
    padding-bottom: 8px;
    border-bottom: 1px dashed var(--el-border-color-lighter);
    font-size: 13px;

    &:last-child {
      padding-bottom: 0;
      border-bottom: 0;
    }
  }
}

.wb-list-main {
  overflow: hidden;
  color: var(--el-text-color-primary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.wb-list-sub,
.wb-list-time {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.wb-list-time {
  grid-column: 1 / -1;
  font-variant-numeric: tabular-nums;
}

.wb-text {
  margin: 0;
  color: var(--el-text-color-regular);
  overflow-wrap: anywhere;
}
</style>
