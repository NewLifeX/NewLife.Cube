// 简化版本的测试路由 - 用于调试
export const routes = [
  {
    path: '/Cube/SimpleTest',
    name: 'cube-simple-test',
    component: () => Promise.resolve({
      template: '<div style="padding: 20px;"><h1>Simple test page</h1><p>If you can see this page, routing works.</p></div>'
    }),
  }
];

console.log('cube-cube routes exported:', routes);
