import React from 'react';
import { Button } from 'antd';
import { LoginOutlined, SettingOutlined, SkinOutlined, UserOutlined } from '@ant-design/icons';
import './Header.css';
import { useNavigate } from "react-router-dom";


const Header: React.FC = () => {
  const navigate = useNavigate();
  // TODO: Replace with actual admin role check from authentication context
  const isAdmin = true; // Placeholder for admin user check

  return (
    <header className="header">
     <div className="logo"
        onClick={() => navigate("/")}>
          <h2>MySite</h2>
      </div>
      <div className="header-right">
        {isAdmin && (
          <Button type="link" href="/user-management" icon={<UserOutlined />} >
            Users
          </Button>
        )}
        <Button type="link" href="/outfits" icon={<SkinOutlined />} >
          Outfits
        </Button>
        <Button type="link" href="/login" icon={<LoginOutlined />} >
          Login
        </Button>
        <Button type="link" href="/settings" icon={<SettingOutlined />} >
          Settings
        </Button>
      </div>
    </header>
  );
};

export default Header;
