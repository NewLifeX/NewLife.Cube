import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './layouts/Layout';
import Login from './pages/login/Login';
import Home from './pages/home/Home';
import DynamicPage from './pages/dynamic/DynamicPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to="/home" replace />} />
        <Route path="home" element={<Home />} />
        <Route path="*" element={<DynamicPage />} />
      </Route>
    </Routes>
  );
}
