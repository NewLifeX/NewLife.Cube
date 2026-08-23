/**
 * Mermaid 图表渲染工具（CDN 懒加载）
 * 移植自 StarChat 前端 mermaidHelper.ts / mermaidLazy.ts：
 * - 规范化修复 LLM 生成的 Mermaid 代码（normalizeMermaidCode）
 * - 首次遇到图表才从 CDN 加载 mermaid 并初始化一次（不打包 2.6MB）
 * - mermaid.parse 校验 + render 渲染 SVG，失败回退源码展示
 */

/** 修复 LLM 生成的 `&amp;` HTML 实体，在 Mermaid 代码中还原为 `&` */
function fixAmpersandEntities(code: string): string {
  return code.replace(/&amp;/g, '&')
}

/** 移除 LLM 生成的 `&nbsp;` HTML 实体（Mermaid 语法位置的无效 token） */
function fixNbspInCode(code: string): string {
  return code.replace(/&nbsp;/g, ' ')
}

/** 修复 LLM 将 Markdown 表格管道 `|` 与 Mermaid 连线 `-->` 混淆（行首 |--> → -->） */
function fixPipeArrowConfusion(code: string): string {
  return code.replace(/^(\s*)\|-->/gm, '$1-->')
}

/** 删除 LLM 幻想的"布局控制"伪指令行，如 layoutTB[隐藏默认连线方向] */
function removePseudoLayoutDirectives(code: string): string {
  return code.replace(/^\s*layout(?:TB|LR|RL|BT)\s*\[[^\n]*\n?/gim, '')
}

/** 修复 classDef 中 stroke-dasharray 值含空格的问题（保留第一个数值） */
function fixClassDefStrokeDasharray(code: string): string {
  return code.replace(/(stroke-dasharray)\s*:\s*(\d+(?:\.\d+)?)(?:\s+[\d.]+)+/g, '$1:$2')
}

/** 修复 classDef 应用语法（:::）中多余冒号和前导空格，`A[Node] :::::className` → `A[Node]:::className` */
function fixClassApplicationColons(code: string): string {
  return code.replace(/\s*(:{3,})\s*(\w+)/g, ':::$2')
}

/** 修复节点标签 [...] 中含有 `|` 的情况（PIPE 解析错误），自动包裹为 ["..."] */
function fixPipesInNodeLabels(code: string): string {
  return code.replace(/\[([^\[\]"]*\|[^\[\]"]*)\]/g, (_, content: string) => `["${content}"]`)
}

/** 将节点标签和边标签内的字面量 \n（反斜杠+n）转换为 <br/>，配合 htmlLabels 产生换行 */
function fixNewlinesInLabels(code: string): string {
  const br = '<br/>'
  return code
    .replace(/\(\(([^()]*)\)\)/g, (_, c: string) => `((${c.replace(/\\n/g, br)}))`)
    .replace(/\(([^()]*)\)/g, (_, c: string) => `(${c.replace(/\\n/g, br)})`)
    .replace(/\["([^"]*)"\]/g, (_, c: string) => `["${c.replace(/\\n/g, br)}"]`)
    .replace(/\[([^\[\]"]*)\]/g, (_, c: string) => `[${c.replace(/\\n/g, br)}]`)
    .replace(/\|([^|]*)\|/g, (_, c: string) => `|${c.replace(/\\n/g, br)}|`)
}

/** 移除 classDef 中 Mermaid 不支持的 SVG 几何属性（rx、ry 等） */
function fixClassDefInvalidProps(code: string): string {
  return code.replace(/^(\s*classDef\b[^\n]*)/gm, (line) => line.replace(/,\s*r[xy]\s*:\s*[\d.]+/g, ''))
}

/** 修复 LLM 将 <br/> 及追加文本写在节点标签括号之外的问题，收纳回括号内 */
function fixBrAfterNodeLabel(code: string): string {
  return code.replace(
    /\[([^\[\]"]+)\](<br\/>)([^:\n\[\]{}|]*)(:::[\w]+)?/g,
    (_m, label: string, br: string, extra: string, cls?: string) => `["${label}${br}${extra.trimEnd()}"]${cls ?? ''}`,
  )
}

/** 修复 LLM 在方括号节点标签内嵌入双引号的问题，替换为单引号 */
function fixQuotesInBracketLabels(code: string): string {
  return code.replace(/\[([^"\[\]\n{}<>|][^\[\]\n{}<>]*)\]/g, (m, label: string) =>
    label.includes('"') ? `[${label.replace(/"/g, "'")}]` : m,
  )
}

/** 修复 gantt 图的纯时间 dateFormat（如 HH:mm），补虚拟日期 2000-01-01，配合 axisFormat %H:%M 只显示时间 */
function fixGanttTimeFormat(code: string): string {
  if (!/^\s*gantt\b/m.test(code)) return code
  const dfMatch = code.match(/^(\s*dateFormat\s+)(.+)$/m)
  if (!dfMatch) return code
  const fmt = dfMatch[2].trim()
  if (/[YMDd]/.test(fmt)) return code
  const dummyDate = '2000-01-01'
  const nextDate = '2000-01-02'
  code = code.replace(/^(\s*dateFormat\s+).+$/m, `$1YYYY-MM-DD ${fmt}`)
  const lines = code.split('\n')
  const result: string[] = []
  for (const line of lines) {
    // 用 lastIndexOf 精准定位任务名与属性的分隔符 " :"
    const sepIdx = line.lastIndexOf(' :')
    if (sepIdx < 0) {
      result.push(line)
      continue
    }
    const desc = line.substring(0, sepIdx)
    const attrs = line.substring(sepIdx + 2)
    // 收集属性中所有裸 HH:MM 及其位置
    const timeRe = /\b(\d{1,2}:\d{2})\b/g
    const matches: Array<{ full: string; time: string; index: number }> = []
    let m: RegExpExecArray | null
    while ((m = timeRe.exec(attrs)) !== null) {
      matches.push({ full: m[0], time: m[1], index: m.index })
    }
    if (matches.length === 0) {
      result.push(line)
      continue
    }
    // 最后一个时间 < 倒数第二个时视为跨午夜，用翌日
    let lastDate = dummyDate
    if (matches.length >= 2) {
      const prev = matches[matches.length - 2].time
      const last = matches[matches.length - 1].time
      if (last < prev) lastDate = nextDate
    }
    // 从右向左替换，避免 index 偏移
    let fixedAttrs = attrs
    for (let i = matches.length - 1; i >= 0; i--) {
      const t = matches[i]
      const date = (i === matches.length - 1 && matches.length >= 2) ? lastDate : dummyDate
      const replacement = `${date} ${t.time}`
      fixedAttrs = fixedAttrs.substring(0, t.index) + replacement + fixedAttrs.substring(t.index + t.full.length)
    }
    result.push(`${desc} : ${fixedAttrs}`)
  }
  return result.join('\n')
}

/** 字符级扫描：转义标签上下文（引号/边标签/方括号/圆括号）内的花括号 {}，防止被误判为菱形节点 */
function escapeBracesInLabels(code: string): string {
  const stack: string[] = []
  let result = ''
  for (let i = 0; i < code.length; i++) {
    const ch = code[i]
    const top = stack[stack.length - 1]
    const inQuoted = top === '"'
    if (ch === '"') {
      if (top === '"') stack.pop()
      else stack.push('"')
      result += ch
      continue
    }
    if (inQuoted) {
      result += ch === '{' ? '&#123;' : ch === '}' ? '&#125;' : ch
      continue
    }
    if (ch === '|') {
      if (top === '|') stack.pop()
      else stack.push('|')
      result += ch
      continue
    }
    if (ch === '[' || ch === '(') {
      stack.push(ch)
      result += ch
      continue
    }
    if (ch === ']' && top === '[') {
      stack.pop()
      result += ch
      continue
    }
    if (ch === ')' && top === '(') {
      stack.pop()
      result += ch
      continue
    }
    if ((ch === '{' || ch === '}') && stack.length > 0) {
      result += ch === '{' ? '&#123;' : '&#125;'
      continue
    }
    result += ch
  }
  return result
}

/** 规范化 Mermaid 代码：修复 LLM 常见生成缺陷（与 StarChat mermaidHelper.normalizeMermaidCode 同套） */
export function normalizeMermaidCode(code: string): string {
  let result = fixGanttTimeFormat(code)
  result = fixAmpersandEntities(result)
  result = fixNbspInCode(result)
  result = fixPipeArrowConfusion(result)
  result = removePseudoLayoutDirectives(result)
  result = fixClassDefStrokeDasharray(result)
  result = fixClassApplicationColons(result)
  result = fixClassDefInvalidProps(result)
  result = fixPipesInNodeLabels(result)
  result = fixNewlinesInLabels(result)
  result = fixBrAfterNodeLabel(result)
  result = fixQuotesInBracketLabels(result)
  result = escapeBracesInLabels(result)
  return result
}

/** XSS 防护：剥离 script/iframe 等标签与 on* 事件属性（securityLevel 为 loose 时允许 HTML） */
export function sanitizeMermaidCode(code: string): string {
  return String(code)
    .replace(/<\/?(?:script|iframe|object|embed|form|base|link|meta|applet)(\s[^>]*)?>/gi, '')
    .replace(/\son\w+\s*=\s*("[^"]*"|'[^']*'|[^\s>]+)/gi, '')
}

/* Mermaid 库懒加载状态：只加载一次，多个图表块并发共享 */
let _mermaidLoading: Promise<MermaidApi> | null = null
let _mermaidInit = false

/** Mermaid 全局对象的最小接口（window.mermaid） */
interface MermaidApi {
  initialize(config: Record<string, unknown>): void
  parse(text: string, options: { suppressErrors: boolean }): Promise<boolean>
  render(id: string, text: string): Promise<{ svg: string }>
}

/** 按需从 CDN 加载 mermaid 并执行一次性初始化（地址来自 GetAiConfig 的 MermaidUrl） */
export function getMermaid(url: string): Promise<MermaidApi> {
  const mm = (window as unknown as { mermaid?: MermaidApi }).mermaid
  if (mm?.render) return Promise.resolve(mm)
  if (_mermaidLoading) return _mermaidLoading
  const src = url || 'https://registry.npmmirror.com/mermaid/11.12.3/files/dist/mermaid.min.js'
  _mermaidLoading = new Promise<MermaidApi>((resolve, reject) => {
    const s = document.createElement('script')
    s.src = src
    s.async = true
    s.onload = () => {
      try {
        const loaded = (window as unknown as { mermaid?: MermaidApi }).mermaid
        if (!loaded) throw new Error('mermaid 未暴露全局变量')
        if (!_mermaidInit) {
          loaded.initialize({ startOnLoad: false, theme: 'default', securityLevel: 'loose' })
          _mermaidInit = true
        }
        resolve(loaded)
      } catch (e) {
        _mermaidLoading = null
        reject(e)
      }
    }
    s.onerror = () => {
      _mermaidLoading = null
      reject(new Error(`mermaid 加载失败: ${src}`))
    }
    document.head.appendChild(s)
  })
  return _mermaidLoading
}

/** 解析可渲染的 Mermaid 代码：先原始、后规范化，用 mermaid.parse 校验，均失败返回 null */
export function resolveRenderableMermaidCode(mermaid: MermaidApi, code: string): Promise<string | null> {
  const candidates = [code]
  const normalized = normalizeMermaidCode(code)
  if (normalized !== code) candidates.push(normalized)
  let chain: Promise<string | null> = Promise.resolve(null)
  for (const candidate of candidates) {
    chain = chain.then((found) => {
      if (found) return found
      return mermaid
        .parse(candidate, { suppressErrors: true })
        // parse 对非法代码解析为 false（不抛异常），必须校验布尔结果
        .then((ok) => (ok ? candidate : null))
        .catch(() => null)
    })
  }
  return chain
}

let _mermaidSeq = 0

/** 回退：将图表占位还原为源码代码块展示 */
function restoreMermaidSource(holder: HTMLElement, raw: string): void {
  const pre = document.createElement('pre')
  const code = document.createElement('code')
  code.className = 'language-mermaid'
  code.textContent = raw
  pre.appendChild(code)
  holder.parentNode?.replaceChild(pre, holder)
}

/** 渲染单个 mermaid 代码块为 SVG，失败回退源码 */
function renderMermaidBlock(codeEl: HTMLElement, mermaid: MermaidApi): void {
  const pre = codeEl.parentElement
  if (!pre) return
  const raw = (codeEl.textContent || '').replace(/\n+$/, '')
  const holder = document.createElement('div')
  holder.className = 'ai-mermaid'
  holder.textContent = '⏳ 图表渲染中...'
  pre.parentNode?.replaceChild(holder, pre)

  resolveRenderableMermaidCode(mermaid, sanitizeMermaidCode(raw)).then((renderable) => {
    if (!renderable) {
      restoreMermaidSource(holder, raw)
      return
    }
    const id = `ai-mermaid-${++_mermaidSeq}`
    mermaid
      .render(id, renderable)
      .then((rs) => {
        const svg = rs?.svg
        if (!svg) {
          restoreMermaidSource(holder, raw)
          return
        }
        holder.innerHTML = svg
        const svgEl = holder.querySelector('svg')
        if (svgEl?.getAttribute) {
          // 从 viewBox 推算自然宽度，避免 SVG 被强制拉伸到容器宽度
          const vb = (svgEl.getAttribute('viewBox') || '').trim().split(/[\s,]+/)
          if (vb.length >= 4) {
            const w = parseFloat(vb[2])
            if (w > 0) svgEl.setAttribute('width', String(Math.ceil(w)))
          }
          svgEl.style.maxWidth = '100%'
          svgEl.style.height = 'auto'
          svgEl.style.display = 'block'
        }
        // 源码切换：<details> 展开查看原始 mermaid 源码
        const details = document.createElement('details')
        const summary = document.createElement('summary')
        summary.textContent = '查看源码'
        details.appendChild(summary)
        const src = document.createElement('pre')
        src.textContent = raw
        details.appendChild(src)
        holder.appendChild(details)
      })
      .catch(() => restoreMermaidSource(holder, raw))
  })
}

/** 渲染容器内所有 mermaid 代码块为 SVG。CDN 加载失败时保持源码块展示 */
export function renderMermaidBlocks(container: HTMLElement | null | undefined, mermaidUrl: string): void {
  if (!container) return
  const codes = container.querySelectorAll<HTMLElement>('pre code.language-mermaid')
  if (!codes.length) return
  getMermaid(mermaidUrl)
    .then((mermaid) => {
      for (const code of codes) renderMermaidBlock(code, mermaid)
    })
    .catch(() => {
      // CDN 加载失败：保持源码块展示，无需额外处理
    })
}
