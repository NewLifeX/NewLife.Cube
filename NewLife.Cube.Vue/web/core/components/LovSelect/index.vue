<script lang="ts">
// 组件入口：重新导出真正实现，使目录可被 `import X from './LovSelect'` 直接引用。
// 组件名由 LovSelect.vue 内的 defineOptions({ name: 'LovSelect' }) 保证，
// 不受此处文件名（index）影响，DevTools 仍显示为 LovSelect。
export { default } from './LovSelect.vue';
</script>
