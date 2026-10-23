import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import App from './App';
import { useAppStore } from './stores/app';
import { createAppTheme } from './theme';
import api from './api';

// SSO 前端回调：后端在 source=front-end 场景重定向到 /#token=xxx，写入令牌后清理地址栏
const ssoHash = window.location.hash;
if (ssoHash.startsWith('#token=')) {
  api.tokenManager.setToken(decodeURIComponent(ssoHash.slice('#token='.length)));
  window.history.replaceState(null, '', window.location.pathname + window.location.search);
}

function Root() {
  const darkMode = useAppStore((s) => s.darkMode);
  const theme = createAppTheme(darkMode);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </ThemeProvider>
  );
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <Root />
  </React.StrictMode>
);
