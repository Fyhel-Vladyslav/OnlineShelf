import React, { useState } from 'react';
import './SettingsPage.css';

interface Settings {
  email: string;
  parameter1: string;
  parameter2: string;
}

const SettingsPage: React.FC = () => {
  const [settings, setSettings] = useState<Settings>({
    email: 'user@example.com',
    parameter1: 'value1',
    parameter2: 'value2',
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setSettings(prev => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleRestorePassword = () => {
    alert('Password restore initiated!');
    // Add logic to handle password restore
  };

  return (
    <div className="settings-page">
      <div className="settings-container">
        <h2>Settings</h2>
        <form className="settings-form">
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              name="email"
              value={settings.email}
              onChange={handleInputChange}
              placeholder="Enter your email"
            />
          </div>
          <div className="form-group">
            <label htmlFor="parameter1">Parameter 1</label>
            <input
              type="text"
              id="parameter1"
              name="parameter1"
              value={settings.parameter1}
              onChange={handleInputChange}
              placeholder="Enter parameter 1"
            />
          </div>
          <div className="form-group">
            <label htmlFor="parameter2">Parameter 2</label>
            <input
              type="text"
              id="parameter2"
              name="parameter2"
              value={settings.parameter2}
              onChange={handleInputChange}
              placeholder="Enter parameter 2"
            />
          </div>
          <button type="button" onClick={handleRestorePassword} className="restore-btn">
            Restore My Password
          </button>
        </form>
      </div>
    </div>
  );
};

export default SettingsPage;
