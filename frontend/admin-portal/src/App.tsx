import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { StrictMode } from 'react';
import Layout from './components/Layout/Layout';
import HomePage from './pages/home/HomePage';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import SettingsPage from './pages/settings/SettingsPage';
import UserManagementPage from './pages/userManagement/UserManagementPage';
import UserEditPage from './pages/userManagement/UserEditPage';
import ItemEditPage from './pages/item/ItemEditPage';
import CreateItemPage from './pages/item/CreateItemPage';
import './App.css';
import { NotificationRoot } from './notification/NotificationRoot';
import ShelfsPage from './pages/shelfs/ShelfsPage';
import { ProtectedRoute } from './components/ProtectedRoute/ProtectedRoute';

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
        
            <Route element={<ProtectedRoute />}>
              <Route path="/settings" element={<SettingsPage />} />
            </Route>
            <Route element={<ProtectedRoute />}> 
              <Route path="/user-management" element={<UserManagementPage />} />
            </Route>
            <Route element={<ProtectedRoute />}> 
              <Route path="/user-management/edit/:id" element={<UserEditPage />} />
            </Route>
            <Route element={<ProtectedRoute />}> 
              <Route path="/create-item" element={<CreateItemPage />} />
            </Route>
            <Route element={<ProtectedRoute />}> 
              <Route path="/item-edit/:id" element={<ItemEditPage />} />
            </Route>
            <Route element={<ProtectedRoute />}> 
              <Route path="/shelfs" element={<ShelfsPage />} />
            </Route>
          </Routes>
        </Layout>
      </Router>
    </StrictMode>
  );
}

export default App;
