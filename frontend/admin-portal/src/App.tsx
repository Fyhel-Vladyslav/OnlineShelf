import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { StrictMode } from 'react';
import Layout from './components/Layout/Layout';
import HomePage from './features/home/HomePage';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import SettingsPage from './features/settings/SettingsPage';
import UserManagementPage from './features/userManagement/UserManagementPage';
import UserEditPage from './features/userManagement/UserEditPage';
import ItemEditPage from './features/item/ItemEditPage';
import CreateItemPage from './features/item/CreateItemPage';
import './App.css';
import { NotificationRoot } from './notification/NotificationRoot';
import ShelfsPage from './features/shelfs/ShelfsPage';

function App() {
  return (
    <StrictMode>
      <NotificationRoot />
      <Router>
        <Layout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/user-management" element={<UserManagementPage />} />
            <Route path="/user-management/edit/:id" element={<UserEditPage />} />
            <Route path="/create-item" element={<CreateItemPage />} />
            <Route path="/item-edit/:id" element={<ItemEditPage />} />
            <Route path="/shelfs" element={<ShelfsPage />} />
          </Routes>
        </Layout>
      </Router>
    </StrictMode>
  );
}

export default App;
