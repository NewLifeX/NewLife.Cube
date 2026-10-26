<script setup lang="ts">
// 目录名用全小写 oauthconfig：resolvePageComponent 对 OAuthConfig 的 kebab 候选是 o-auth-config，
// 写成 oauth-config 会匹配不到而回落 AutoPage。
import SettingsPage from '../settings/SettingsPage.vue';
</script>

<template>
  <SettingsPage />
</template>
