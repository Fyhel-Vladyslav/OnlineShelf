import React, { useState, useRef, useEffect } from 'react';
import { Button, message, Upload } from 'antd';
import { UploadOutlined, DeleteOutlined } from '@ant-design/icons';
import type { UploadProps } from 'antd';

interface ImagePickerProps {
  value?: File;
  onChange?: (value?: File) => void;
}

const ImagePicker: React.FC<ImagePickerProps> = ({ value, onChange }) => {
  const [previewUrl, setPreviewUrl] = useState<string | undefined>();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Синхронізація з формою
  useEffect(() => {
    if (value && value instanceof File) {
      const objectUrl = URL.createObjectURL(value);
      setPreviewUrl(objectUrl);
      return () => URL.revokeObjectURL(objectUrl);
    } else {
      setPreviewUrl(undefined);
    }
  }, [value]);

  const handleFileSelect = (file: File) => {
    console.log("1. [ImagePicker] Файл вибрано всередині компонента:", file.name);
    
    // Створюємо прев'ю
    const objectUrl = URL.createObjectURL(file);
    setPreviewUrl(objectUrl);

    // ПЕРЕДАЄМО ФАЙЛ У ФОРМУ
    if (onChange) {
      console.log("2. [ImagePicker] Викликаю onChange для передачі в Form.Item");
      onChange(file);
    } else {
      console.error("Помилка: onChange не переданий в ImagePicker!");
    }

    return false; // Блокуємо стандартну поведінку AntD Upload
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelect(files[0]);
    }
  };

  const handleSelectNew = () => {
    fileInputRef.current?.click();
  };

  const handleDelete = () => {
    setPreviewUrl(undefined);
    if (onChange) onChange(undefined);
    if (fileInputRef.current) fileInputRef.current.value = '';
    message.success('Фото видалено');
  };

  const uploadProps: UploadProps = {
    beforeUpload: handleFileSelect,
    showUploadList: false,
  };

  return (
    <div style={{ textAlign: 'center' }}>
      <div
        style={{
          border: '2px dashed #d9d9d9',
          borderRadius: '8px',
          padding: '20px',
          marginBottom: '16px',
          minHeight: '80px',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: previewUrl ? 'transparent' : '#fafafa',
          cursor: 'pointer',
        }}
        onDrop={handleDrop}
        onDragOver={(e) => e.preventDefault()}
        onClick={handleSelectNew}
      >
        {previewUrl ? (
          <img src={previewUrl} alt="Item preview" style={{ width: '100%', maxHeight: '180px', objectFit: 'contain' }} />
        ) : (
          <div>
            <div style={{ fontSize: '24px', color: '#d9d9d9' }}>+</div>
            <div style={{ color: '#d9d9d9' }}>Натисніть або перетягніть фото</div>
          </div>
        )}
      </div>

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

      <div style={{ marginBottom: '16px', display: 'flex', justifyContent: 'space-between' }}>
        <Button icon={<UploadOutlined />} onClick={handleSelectNew} style={{ width: "48%" }}>
          Вибрати
        </Button>
        <Button icon={<DeleteOutlined />} onClick={handleDelete} danger style={{ width: "48%" }}>
          Видалити
        </Button>
      </div>

      <Upload {...uploadProps} style={{ display: 'none' }} />
    </div>
  );
};

export default ImagePicker;