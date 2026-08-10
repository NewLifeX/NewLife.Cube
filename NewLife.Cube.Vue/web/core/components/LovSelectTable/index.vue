<script lang="ts">
// 组件入口：重新导出真正实现，使目录可被 `import X from './LovSelectTable'` 直接引用。
// 组件名由 LovSelectTable.vue 内的 defineOptions({ name: 'LovSelectTable' }) 保证，
// 不受此处文件名（index）影响，DevTools 仍显示为 LovSelectTable。
export { default } from './LovSelectTable.vue';
</script>
