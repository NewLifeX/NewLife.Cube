// 性能补丁：@visactor/vtable-gantt 缩放级别切换时 4 次全量场景图重建（refreshAll）冗余，
// 应用后降为 2 次，千条数据切换等级响应 <1s（详见 openspec OSC-0019 verify.md）。
// 幂等：已应用则跳过；依赖升级后若匹配目标不存在则告警（需人工重新审视补丁）。
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');

const patches = [
  {
    file: 'node_modules/@visactor/vtable-gantt/es/zoom-scale/ZoomScaleManager.js',
    from: 'this.gantt.recalculateTimeScale(), !0;',
    to: '!0;',
    why: 'switchToLevel 去掉紧随的 recalculateTimeScale（setMillisecondsPerPixel 会用正确 mpp 重算，属冗余第 2 次 refreshAll）',
  },
  {
    file: 'node_modules/@visactor/vtable-gantt/es/Gantt.js',
    from: 'this.recalculateTimeScale(), this._updateSize(), this.scenegraph.refreshAll();',
    to: 'this.recalculateTimeScale(), this._updateSize();',
    why: 'setMillisecondsPerPixel 去掉末尾 refreshAll（recalculateTimeScale 已含同等刷新，属纯冗余第 4 次）',
  },
  {
    file: 'node_modules/@visactor/vtable-gantt/es/Gantt.js',
    from: 'this._generateTimeLineDateMap(), this.timeLineHeaderLevel = this.parsedOptions.sortedTimelineScales.length, \n        this.scenegraph.refreshAll(), updateSplitLineAndResizeLine(this)',
    to: 'this._generateTimeLineDateMap(), this.timeLineHeaderLevel = this.parsedOptions.sortedTimelineScales.length, \n        updateSplitLineAndResizeLine(this)',
    why: 'updateScales 去掉过渡 refreshAll（仅被 switchToLevel 调用，随后 setMillisecondsPerPixel 会用正确 mpp 完整渲染；切级 3 次 refreshAll → 1 次，千条任务条从 >1s 降到 <1s）',
  },
];

let changed = 0;
for (const p of patches) {
  const full = path.join(root, p.file);
  if (!fs.existsSync(full)) {
    console.warn(`[patch-vtable-gantt] 文件不存在，跳过：${p.file}`);
    continue;
  }
  let text = fs.readFileSync(full, 'utf8');
  if (text.includes(p.to) && !text.includes(p.from)) {
    console.log(`[patch-vtable-gantt] 已应用，跳过：${p.file}`);
    continue;
  }
  if (!text.includes(p.from)) {
    console.warn(`[patch-vtable-gantt] 未找到待替换片段，可能依赖已升级，请人工核对：${p.file}`);
    console.warn(`  期望片段：${p.from}`);
    continue;
  }
  text = text.replace(p.from, p.to);
  fs.writeFileSync(full, text, 'utf8');
  changed++;
  console.log(`[patch-vtable-gantt] 已应用：${p.file} —— ${p.why}`);
}
console.log(
  changed > 0 ? `[patch-vtable-gantt] 完成，共应用 ${changed} 处。` : '[patch-vtable-gantt] 无需变更。',
);
