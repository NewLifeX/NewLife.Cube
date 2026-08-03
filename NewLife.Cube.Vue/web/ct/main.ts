import { createApp, h, reactive, type App } from 'vue';
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';
import { stories } from './stories';

const registry = new Map<string, { component: unknown; props?: Record<string, unknown> }>();
for (const s of stories) registry.set(s.id, s);

let currentApp: App<unknown> | null = null;
let mountEl: HTMLElement | null = null;
let currentProps: Record<string, unknown> = {};

function unmountCurrent() {
  if (currentApp) {
    currentApp.unmount();
    currentApp = null;
  }
  // 清理 ElDialog/ElOverlay 通过 Teleport 挂到 body 的残留节点，避免污染下一个 story 的截图
  // （Element Plus 弹窗在组件 unmount 后偶发 overlay 不随组件移除）
  document
    .querySelectorAll('body > .el-overlay, body > .el-dialog__wrapper, body > .v-modal')
    .forEach((n) => n.remove());
  if (mountEl) {
    mountEl.remove();
    mountEl = null;
  }
}

// 浏览器侧挂载契约：Playwright 通过 window.mountStory(id, props) 渲染指定变体。
// dialogVisible 默认 false，spec 中再 setStoryProps({ dialogVisible: true }) 触发 watch 加载数据，
// 与真实弹窗打开行为一致（避免改组件源码加 immediate）。
(window as unknown as Record<string, unknown>).mountStory = (id: string, props?: Record<string, unknown>) => {
  const s = registry.get(id);
  if (!s) throw new Error(`未知 story: ${id}`);
  unmountCurrent();
  mountEl = document.createElement('div');
  mountEl.classList.add('gal-preview');
  document.body.appendChild(mountEl);
  currentProps = reactive({ ...(props ?? s.props ?? {}) });
  currentApp = createApp({
    render: () => h(s.component as never, currentProps as never),
  });
  currentApp.use(ElementPlus);
  currentApp.mount(mountEl);
};

(window as unknown as Record<string, unknown>).setStoryProps = (patch: Record<string, unknown>) => {
  Object.assign(currentProps, patch);
};

(window as unknown as Record<string, unknown>).unmountStory = () => unmountCurrent();

// ── 浏览器手动浏览模式：渲染故事侧栏，点击即预览，支持 Vite HMR ──
// 无 ?story 参数时（人类直接打开根目录 / 即渲染 index.html）即进入此模式；Playwright 走 ?story= 深链，
// 不触发侧栏，window.mountStory 契约不变，CT 基线不受影响。
function renderGalleryNav() {
  const nav = document.getElementById('gallery-nav');
  if (!nav) return;
  document.body.classList.add('has-nav');
  nav.innerHTML = '<h3>组件故事（点击预览）</h3>';

  // 按 story id 的 '/' 前缀分组（如 LovSelectTable / LovSelect），避免故事多了侧栏过长。
  const groups = new Map<string, typeof stories>();
  for (const s of stories) {
    const key = s.id.includes('/') ? s.id.split('/')[0] : '(其他)';
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)!.push(s);
  }

  // 故事总数超过 25 时，分组默认收起，避免一打开就刷一长串。
  const defaultCollapsed = stories.length > 25;

  let firstLi: HTMLElement | null = null;
  for (const [groupKey, groupStories] of groups) {
    const groupEl = document.createElement('div');
    groupEl.className = 'nav-group';

    const header = document.createElement('div');
    header.className = 'nav-group-header';
    const toggle = document.createElement('span');
    toggle.className = 'nav-toggle';
    toggle.textContent = defaultCollapsed ? '▸' : '▾';
    const title = document.createElement('span');
    title.className = 'nav-group-title';
    title.textContent = `${groupKey} (${groupStories.length})`;
    header.append(toggle, title);
    groupEl.appendChild(header);

    const ul = document.createElement('ul');
    ul.className = 'nav-group-list';
    if (defaultCollapsed) ul.style.display = 'none';

    for (const s of groupStories) {
      const li = document.createElement('li');
      li.textContent = s.id.includes('/') ? s.id.slice(s.id.indexOf('/') + 1) : s.id;
      li.title = s.id;
      li.addEventListener('click', () => {
        ul.querySelectorAll('li').forEach((el) => el.classList.remove('active'));
        li.classList.add('active');
        (window as unknown as Record<string, unknown>).mountStory(s.id);
        // 仅表格型（LovSelectTable）默认打开弹窗，便于直接查看表格/回显/翻页效果；
        // 选择器型（LovSelect/*）不自动开弹窗，避免弹窗盖在 select 下拉后污染截图。
        if (s.id.startsWith('LovSelectTable')) {
          (window as unknown as Record<string, unknown>).setStoryProps({ dialogVisible: true });
        }
      });
      ul.appendChild(li);
      if (!firstLi) firstLi = li;
    }
    groupEl.appendChild(ul);

    // 组头点击：展开/收起该组（不影响其它组）
    header.addEventListener('click', () => {
      const hidden = ul.style.display === 'none';
      ul.style.display = hidden ? '' : 'none';
      toggle.textContent = hidden ? '▾' : '▸';
    });

    nav.appendChild(groupEl);
  }

  // 默认选中并预览第一个故事（defaultCollapsed 时先把它的组展开，便于看到高亮）
  if (defaultCollapsed && firstLi) {
    const firstUl = firstLi.closest('.nav-group-list') as HTMLElement | null;
    if (firstUl) {
      firstUl.style.display = '';
      const t = firstUl.previousElementSibling?.querySelector('.nav-toggle');
      if (t) t.textContent = '▾';
    }
  }
  if (firstLi) firstLi.click();
}

// 深链单个故事（?story=<id>）：直接挂载该故事、不显示侧栏，仍支持 Vite HMR。
// 与"无参数→侧栏"互斥：人类浏览用无参数（或任意非 ?story 参数）打开即看侧栏；
// Playwright 用 ?story= 一步挂载单故事，两者都不会出现白屏。
function mountOnlyStory(id: string) {
  const s = stories.find((x) => x.id === id);
  if (!s) {
    document.body.innerHTML =
      `<div style="padding:24px;color:#f56c6c;font-family:system-ui,sans-serif;line-height:1.8">` +
      `未知 story: <b>${id}</b><br/>可用故事：<br/>` +
      stories.map((x) => x.id).join('<br/>') +
      `</div>`;
    return;
  }
  (window as unknown as Record<string, unknown>).mountStory(id);
  // 仅表格型（LovSelectTable）默认打开弹窗，选择器型由测试/用户点击触发，避免误开弹窗遮挡截图。
  if (id.startsWith('LovSelectTable')) {
    (window as unknown as Record<string, unknown>).setStoryProps({ dialogVisible: true });
  }
}

const navParams = new URLSearchParams(window.location.search);
if (navParams.has('story')) {
  // 深链单故事（CT 规范入口 & 人类深链）：只渲染该组件、不显示侧栏，仍支持 Vite HMR。
  mountOnlyStory(navParams.get('story')!);
} else {
  // 无 ?story 参数（人类直接打开根目录 / 即渲染 index.html）：渲染故事侧栏，点击即预览。
  // 注意：CT 用例永远带 ?story=，因此不会走到这里，侧栏不会污染 CT 基线。
  renderGalleryNav();
}
