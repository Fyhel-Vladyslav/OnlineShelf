import React, { useState, useRef, useEffect } from 'react';
import { Button, message, Upload } from 'antd';
import { UploadOutlined, DeleteOutlined } from '@ant-design/icons';
import type { UploadProps } from 'antd';

interface ImageShowProps {
  // Пропси для інтеграції з AntD Form.Item (керування обраним файлом)
  value?: File;
  onChange?: (value?: File) => void;
  // Початковий URL картинки, що завантажується з сервера
  src?: string;
  alt?: string;
  style?: React.CSSProperties;
  onDeleteUrl?: () => void;
}

const ImageShow: React.FC<ImageShowProps> = ({ value, onChange, src, alt = "Image", style, onDeleteUrl }) => {
  const [previewUrl, setPreviewUrl] = useState<string | undefined>();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Визначаємо джерело відображення: пріоритет у нового файлу, потім URL з бази, потім заглушка
  const defaultPlaceholder = '/src/assets/images/noPhotoLoaded.jpg';
  const [currentImageSrc, setCurrentImageSrc] = useState<string>(src || defaultPlaceholder);

  // 1. Синхронізація, якщо прийшов новий файл з форми (наприклад, скидання форми)
  useEffect(() => {
    if (value && value instanceof File) {
      const objectUrl = URL.createObjectURL(value);
      setPreviewUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    } else {
      setPreviewUrl(undefined);
    }
  }, [value]);

  // 2. Синхронізація, якщо змінився початковий URL речі (наприклад, дані з сервера довантажились)
  useEffect(() => {
    if (src) {
      setCurrentImageSrc(src);
    } else if (!value) {
      setCurrentImageSrc(defaultPlaceholder);
    }
  }, [src, value]);

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelect(files[0]);
    }
  };

  const handleSelectNew = (e?: React.MouseEvent) => {
    // Зупиняємо спливання подій, якщо клікнули саме на кнопку
    e?.stopPropagation();
    fileInputRef.current?.click();
  };

  const handleFileSelect = (file: File) => {
    const objectUrl = URL.createObjectURL(file);
    setPreviewUrl(objectUrl);

    // Додаємо безпечну перевірку: викликаємо onChange тільки якщо він є
    if (onChange) {
      onChange(file);
    } else {
      console.warn("ImageShow використовується як звичайний компонент (без Form.Item), стан форми не оновлено.");
    }
    return false;
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    setPreviewUrl(undefined);
    setCurrentImageSrc(defaultPlaceholder);
    
    if (onChange) onChange(undefined);
    if (onDeleteUrl) onDeleteUrl(); // <--- ДОДАНО: очищуємо bigImageName у формі
    
    if (fileInputRef.current) fileInputRef.current.value = '';
    message.success('Фото видалено');
  };

  const uploadProps: UploadProps = {
    beforeUpload: handleFileSelect,
    showUploadList: false,
  };

  // Визначаємо фінальний src для тегу img
  const displaySrc = previewUrl || currentImageSrc;

  return (
    <div style={{ width: '100%', ...style }}>
      {/* 
        Контейнер зображення, що тепер працює як Drag & Drop зона
        та клікабельна область для вибору файлу
      */}
      <div 
        style={{ 
          position: 'relative', 
          width: '100%', 
          backgroundColor: '#1a1a1a',
          overflow: 'hidden',
          cursor: 'pointer',
          border: '2px dashed rgba(255, 255, 255, 0.4)', // Робимо рамку штрихованою (Dashed)
          transition: 'border-color 0.3s'
        }}
        onDrop={handleDrop}
        onDragOver={(e) => e.preventDefault()}
        onClick={() => handleSelectNew()}
      >
        <img
          src={displaySrc}
          alt={alt}
          style={{
            width: "100%",
            height: 'auto',
            display: 'block',
            objectFit: 'contain'
          }}
          onError={(e) => {
            const target = e.target as HTMLImageElement;
            const fallbackPath = '/src/assets/noPhotoLoaded.jpg';
            if (target.src !== window.location.origin + fallbackPath) {
              target.src = fallbackPath;
            }
          }}
        />
      </div>

      {/* Прихований інпут для стандартного вибору файлів */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        style={{ display: 'none' }}
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file) handleFileSelect(file);
        }}
      />

      {/* Блок кнопок управління зображенням знизу на ImageShow */}
      <div 
        style={{ 
          display: 'flex', 
          flexWrap: 'wrap',           // <--- Додано: Дозволяє кнопкам переходити на 2-й рядок
          gap: '12px', 
          width: '100%', 
          maxWidth: '500px',         // <--- Максимальна ширина групи кнопок
          alignItems: 'center', 
          justifyContent: 'center',  // <--- Вирівнювання кнопок по центру (і в 1, і в 2 рядки)
          margin: '12px auto 0'       // <--- Центрування самого контейнера у батьківському блоці
        }}
      >
        <Button 
          icon={<UploadOutlined />} 
          onClick={handleSelectNew} 
          style={{ 
            flex: '1 1 200px',        // <--- Гнучкість: росте від 200px, переходить на новий рядок якщо менше
            backgroundColor: '#f5f5f5', 
            color: '#4a4a4a', 
            height: '40px', 
            fontSize: '16px', 
            borderRadius: '8px' 
          }}
        >
          Завантажити фото
        </Button>
        
        <Button 
          onClick={handleDelete}
          style={{ 
            flex: '1 1 200px',        // <--- Гнучкість: росте від 200px, переходить на новий рядок якщо менше
            backgroundColor: '#f1f1f1', 
            border: '1px solid #d37a78', 
            borderRadius: '10px', 
            color: '#e75b5a', 
            fontSize: '16px', 
            height: '40px', 
            display: 'flex', 
            alignItems: 'center', 
            justifyContent: 'center' 
          }} 
          icon={<DeleteOutlined style={{ color: '#e75b5a', fontSize: '20px' }} />}
        >
          Видалити фото
        </Button>
      </div>

      <Upload {...uploadProps} style={{ display: 'none' }} />
    </div>
  );
};

export default ImageShow;