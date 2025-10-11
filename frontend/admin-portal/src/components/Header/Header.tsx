import React from 'react';
import { Button } from 'antd';
import { LoginOutlined, SettingOutlined } from '@ant-design/icons';
import './Header.css';

const Header: React.FC = () => {
  return (
    <header className="header">
      <h1>Admin Portal</h1>
      <div className="header-right">
        <Button type="link" href="/auth" icon={<LoginOutlined />} >
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
