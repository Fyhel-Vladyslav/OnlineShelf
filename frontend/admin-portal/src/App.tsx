import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { StrictMode } from 'react';
import Layout from './components/Layout/Layout';
import HomePage from './features/home/HomePage';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import SettingsPage from './features/settings/SettingsPage';
import UserManagementPage from './features/userManagement/UserManagementPage';
import UserEditPage from './features/userManagement/UserEditPage';
import './App.css';

function App() {
  return (
    <StrictMode>
      <Router>
        <Layout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/user-management" element={<UserManagementPage />} />
            <Route path="/user-management/edit/:id" element={<UserEditPage />} />
          </Routes>
        </Layout>
      </Router>
    </StrictMode>
  );
}

export default App;
