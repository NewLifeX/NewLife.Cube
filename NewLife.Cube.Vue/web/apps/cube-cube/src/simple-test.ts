// 简化版本的测试路由 - 用于调试
export const routes = [
  {
    path: '/Cube/SimpleTest',
    name: 'cube-simple-test',
    component: () => Promise.resolve({
      template: '<div style="padding: 20px;"><h1>简单测试页面</h1><p>如果看到这个页面，说明路由工作正常</p></div>'
    }),
  }
];

console.log('cube-cube 路由已导出:', routes);
