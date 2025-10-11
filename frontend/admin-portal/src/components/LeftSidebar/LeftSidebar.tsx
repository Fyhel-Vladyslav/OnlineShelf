import React, { useState } from 'react';
import './LeftSidebar.css';

const LeftSidebar: React.FC = () => {
  const [isOpen, setIsOpen] = useState(true);

  const buttons = [
    { id: 1, text: 'Dashboard', mockup: '📊' },
    { id: 2, text: 'Users', mockup: '👥' },
    { id: 3, text: 'Settings', mockup: '⚙️' },
    { id: 4, text: 'Reports', mockup: '📈' },
  ];

  return (
    <aside className={`left-sidebar ${isOpen ? 'open' : 'closed'}`}>
      {isOpen && (
        <div className="left-sidebar-content">
          {buttons.map(btn => (
            <button key={btn.id} className="sidebar-btn">
              <span className="mockup">{btn.mockup}</span>
              <span>{btn.text}</span>
            </button>
          ))}
        </div>
      )}
      <button className="toggle-btn" onClick={() => setIsOpen(!isOpen)}>
        {isOpen ? '◀' : '▶'}
      </button>
    </aside>
  );
};

export default LeftSidebar;
