import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './layouts/Layout';
import Login from './pages/login/Login';
import Home from './pages/home/Home';
import DynamicPage from './pages/dynamic/DynamicPage';
import { useAppStore } from './stores/app';

export default function App() {
  const darkMode = useAppStore((s) => s.darkMode);

  return (
    <div className={darkMode ? 'dark' : ''}>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/home" replace />} />
          <Route path="home" element={<Home />} />
          <Route path="*" element={<DynamicPage />} />
        </Route>
      </Routes>
    </div>
  );
}
