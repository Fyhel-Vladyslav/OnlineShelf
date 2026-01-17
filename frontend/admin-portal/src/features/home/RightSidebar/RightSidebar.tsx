import React from 'react';
import './RightSidebar.css';

const RightSidebar: React.FC = () => {
  const images = [
    { id: 1, src: '/vite.svg', alt: 'Image 1' },
    { id: 2, src: '/vite.svg', alt: 'Image 2' },
    { id: 3, src: '/vite.svg', alt: 'Image 3' },
    { id: 4, src: '/vite.svg', alt: 'Image 4' },
  ];

  const handleImageClick = (id: number) => {
    alert(`Image ${id} clicked`);
  };

  return (
    <div className="right-sidebar-content">
      {images.map(img => (
        <img
          key={img.id}
          src={img.src}
          alt={img.alt}
          className="sidebar-img"
          onClick={() => handleImageClick(img.id)}
        />
      ))}
    </div>
  );
};

export default RightSidebar;
