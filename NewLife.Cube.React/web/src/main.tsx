/**
 * 应用入口
 *
 * 创建 React 根节点并挂载 App。全局样式在此引入。
 */
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles/global.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
