// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { computeDragOffset, isInteractiveTarget } from './useMapAddDrag';

describe('useMapAddDrag（添加记录弹层任意位置拖动）', () => {
  it('位移小于阈值（3px）返回 null（视作点击，不产生位移）', () => {
    expect(computeDragOffset({ x: 100, y: 100, tx: 0, ty: 0 }, 101, 101)).toBeNull(); // |1|+|1|=2
    expect(computeDragOffset({ x: 100, y: 100, tx: 0, ty: 0 }, 102, 100)).toBeNull(); // |2|+|0|=2
  });

  it('达到/超过阈值返回累计偏移（跟随指针全程）', () => {
    expect(computeDragOffset({ x: 100, y: 100, tx: 0, ty: 0 }, 103, 100)).toEqual({ tx: 3, ty: 0 });
    expect(computeDragOffset({ x: 100, y: 100, tx: 10, ty: 20 }, 106, 105)).toEqual({ tx: 16, ty: 25 });
  });

  it('可在已平移基础上继续拖动并回拖', () => {
    expect(computeDragOffset({ x: 100, y: 100, tx: 40, ty: 40 }, 96, 97)).toEqual({ tx: 36, ty: 37 });
    expect(computeDragOffset({ x: 100, y: 100, tx: 40, ty: 40 }, 100, 101)).toBeNull(); // 回拖 |0|+|1|=1
  });

  it('交互元素（输入/文本域/按钮/链接/下拉包装）按住不启动拖动', () => {
    const host = document.createElement('div');
    host.innerHTML = `
      <div id="plain">文字</div>
      <label id="label">标签</label>
      <input id="input" />
      <textarea id="textarea"></textarea>
      <button id="button">按钮</button>
      <a id="link" href="#">链接</a>
      <div class="arco-input-wrapper"><input id="inner" /></div>
    `;
    expect(isInteractiveTarget(host.querySelector('#plain'))).toBe(false);
    expect(isInteractiveTarget(host.querySelector('#label'))).toBe(false);
    expect(isInteractiveTarget(host.querySelector('#input'))).toBe(true);
    expect(isInteractiveTarget(host.querySelector('#textarea'))).toBe(true);
    expect(isInteractiveTarget(host.querySelector('#button'))).toBe(true);
    expect(isInteractiveTarget(host.querySelector('#link'))).toBe(true);
    // 内部输入框向上命中外层包装
    expect(isInteractiveTarget(host.querySelector('#inner'))).toBe(true);
    expect(isInteractiveTarget(null)).toBe(false);
  });
});
