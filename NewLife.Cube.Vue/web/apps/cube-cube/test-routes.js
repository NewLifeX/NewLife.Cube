// 测试文件 - 验证cube-cube模块是否正确导出路由
import { routes } from '../src/main';

console.log('Cube-cube routes:', routes);

// 验证路由结构
routes.forEach((route, index) => {
  console.log(`Route ${index}:`, {
    path: route.path,
    name: route.name,
    component: route.component ? 'Component loaded' : 'No component'
  });
});

export default routes;
