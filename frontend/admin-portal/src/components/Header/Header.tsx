import React from 'react';
import { Button } from 'antd';
import { LoginOutlined, SettingOutlined } from '@ant-design/icons';
import './Header.css';
import { useNavigate } from "react-router-dom";


const Header: React.FC = () => {
  const navigate = useNavigate();
  return (
    <header className="header">
     <div className="logo"
        onClick={() => navigate("/")}>  
          <h2>MySite</h2>
      </div>
      <div className="header-right">
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
