// CT 用 request 桩：组件（如 LovSelect）会 import request，但 CT 场景不依赖真实后端，
// 用 noop 替代即可，避免拉入 axios/universal-cookie 等重依赖链。
const noop = async () => ({ data: null, code: 0, msg: '' });

export default { get: noop, post: noop, put: noop, delete: noop };
export const get = noop;
export const post = noop;
export const put = noop;
export const del = noop;
