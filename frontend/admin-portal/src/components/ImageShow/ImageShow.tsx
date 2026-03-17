import React from 'react';

interface ImageShowProps {
  src?: string;
  alt?: string;
  style?: React.CSSProperties;
}

const ImageShow: React.FC<ImageShowProps> = ({ src, alt = "Image", style }) => {
  const imageSrc = src || '/src/assets/images/noPhotoLoaded.jpg';
  return (
    <div style={{ textAlign: 'center', ...style }}>
      <img
        src={imageSrc}
        alt={alt}
        style={{
          maxWidth: '100%',
          maxHeight: '200px',
          objectFit: 'contain',
          border: '1px solid #d9d9d9',
          borderRadius: '4px',
          padding: '4px',
        }}
        onError={(e) => {
          // Fallback to placeholder if image fails to load
          const target = e.target as HTMLImageElement;
          if (target.src !== '/src/assets/noPhotoLoaded.jpg') {
            target.src = '/src/assets/noPhotoLoaded.jpg';
          }
        }}
      />
    </div>
  );
};

export default ImageShow;
