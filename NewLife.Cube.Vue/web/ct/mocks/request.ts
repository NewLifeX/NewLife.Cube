// CT 用 request 桩：默认 noop（返回空数据信封），避免拉入 axios/universal-cookie 等重依赖链。
// AreaSelect 的 story 需要真数据面（el-cascader lazy 面板真渲染断言，弥补
// 「jsdom 直调 lazyLoad 不经面板」的门禁盲区），这里按 URL 提供最小路由表；
// 未命中路由的请求保持原 noop 形状不变（既有 story / CT 基线零影响）。
// 形态兼容：default 既可函数式调用 request({...})，也可对象式 request.get(...)。
type RequestStubOptions = { url?: string; method?: string; params?: Record<string, unknown> };
type Envelope = { data: unknown; code: number; msg: string };

const noopEnvelope = (): Envelope => ({ data: null, code: 0, msg: '' });

// 北京市 → 市辖区(level2) → 五区(level3) 最小树（五区=东城/西城/朝阳/丰台/海淀），供 lazy 面板真渲染。
const AREA_ROOT = [{ id: 110000, name: '北京市', level: 1 }];
const AREA_DISTRICTS = [
  { id: 110101, name: '东城区', level: 3 },
  { id: 110102, name: '西城区', level: 3 },
  { id: 110105, name: '朝阳区', level: 3 },
  { id: 110106, name: '丰台区', level: 3 },
  { id: 110108, name: '海淀区', level: 3 },
];
const AREA_PARENTS = [
  { id: 110000, name: '北京市', level: 1 },
  { id: 110100, name: '市辖区', level: 2 },
  { id: 110101, name: '东城区', level: 3 },
];

async function routeRequest(options: RequestStubOptions | undefined): Promise<Envelope> {
  const url = options?.url ?? '';
  const params = (options?.params ?? {}) as { id?: unknown };
  if (url === '/Cube/AreaChilds') {
    const id = Number(params.id ?? 0);
    if (id === 0) return { data: AREA_ROOT, code: 0, msg: '' };
    if (id === 110000) return { data: AREA_DISTRICTS, code: 0, msg: '' };
    return noopEnvelope();
  }
  if (url === '/Cube/AreaParents') {
    return { data: AREA_PARENTS, code: 0, msg: '' };
  }
  return noopEnvelope();
}

const postStub = async (..._args: unknown[]): Promise<Envelope> => noopEnvelope();

const requestStub = Object.assign(routeRequest, {
  get: routeRequest,
  post: postStub,
  put: postStub,
  delete: postStub,
});

export default requestStub;
export const get = routeRequest;
export const post = postStub;
export const put = postStub;
export const del = postStub;
