import { describe, expect, it } from 'vitest';
import { normalizeMermaidCode, sanitizeMermaidCode } from '../mermaidRenderer';

/** Mermaid 代码规范化修复（移植自 StarChat mermaidHelper）单元测试 */
describe('normalizeMermaidCode', () => {
  it('还原 &amp; 为 &', () => {
    expect(normalizeMermaidCode('flowchart TD\n  A[入参校验 &amp; 协议转换] --> B')).toContain('入参校验 & 协议转换');
  });

  it('移除 &nbsp;', () => {
    expect(normalizeMermaidCode('flowchart TD\n  A[开始&nbsp;处理]')).not.toContain('&nbsp;');
  });

  it('行首 |--> 修复为 -->', () => {
    expect(normalizeMermaidCode('flowchart TD\n  |--> 说明')).toContain('\n  --> 说明');
  });

  it('删除伪布局指令 layoutTB[...]', () => {
    expect(normalizeMermaidCode('flowchart TD\n  layoutTB[隐藏默认连线方向]\n  A --> B')).not.toContain('layoutTB');
  });

  it('classDef 应用多余冒号修复 ::::: → :::', () => {
    expect(normalizeMermaidCode('flowchart TD\n  A[开始] :::::highlight\n  classDef highlight fill:#f96')).toContain(
      'A[开始]:::highlight',
    );
  });

  it('节点标签内花括号被转义（防误判为菱形节点）', () => {
    const out = normalizeMermaidCode('flowchart TD\n  D[device:{id}:latest]');
    expect(out).toContain('&#123;');
    expect(out).not.toContain('[device:{id}:latest]');
  });

  it('gantt 纯时间 dateFormat 补虚拟日期', () => {
    const out = normalizeMermaidCode('gantt\ndateFormat HH:mm\n  任务A : 09:00, 10:30');
    expect(out).toContain('dateFormat YYYY-MM-DD HH:mm');
    expect(out).toContain('2000-01-01 09:00');
    expect(out).toContain('2000-01-01 10:30');
  });
});

/** Mermaid 代码 XSS 清洗单元测试 */
describe('sanitizeMermaidCode', () => {
  it('剥离 script 标签', () => {
    const out = sanitizeMermaidCode('flowchart TD\n  A --> B<script>alert(1)</script>');
    expect(out).not.toContain('<script>');
  });

  it('剥离 on* 事件属性', () => {
    const out = sanitizeMermaidCode('flowchart TD\n  A --> B <span onclick="alert(1)">x</span>');
    expect(out).not.toContain('onclick');
  });
});
