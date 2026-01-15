import React, { useState, useRef } from 'react';
import { Upload, Button, message } from 'antd';
import { UploadOutlined, DeleteOutlined } from '@ant-design/icons';
import type { UploadProps } from 'antd';

interface ImagePickerProps {
  value?: string;
  onChange?: (value: string) => void;
}

const ImagePicker: React.FC<ImagePickerProps> = ({ value, onChange }) => {
  const [imageUrl, setImageUrl] = useState<string | undefined>(value);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const result = e.target?.result as string;
      setImageUrl(result);
      onChange?.(result);
    };
    reader.readAsDataURL(file);
    return false; // Prevent default upload behavior
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelect(files[0]);
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
  };

  const handleSelectNew = () => {
    fileInputRef.current?.click();
  };

  const handleSave = () => {
    message.success('Image saved successfully');
  };

  const handleDelete = () => {
    setImageUrl(undefined);
    onChange?.('');
    message.success('Image deleted');
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
          backgroundColor: imageUrl ? 'transparent' : '#fafafa',
          cursor: 'pointer',
        }}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onClick={handleSelectNew}
      >
        {imageUrl ? (
          <img
            src={imageUrl}
            alt="Item"
            style={{ maxWidth: '100%', maxHeight: '180px', objectFit: 'contain' }}
          />
        ) : (
          <div>
            <div style={{ fontSize: '24px', color: '#d9d9d9' }}>+</div>
            <div style={{ color: '#d9d9d9' }}>Drag image here</div>
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
          if (file) {
            handleFileSelect(file);
          }
        }}
      />
      <div style={{ marginBottom: '16px' }}>
        <Button icon={<UploadOutlined />} onClick={handleSelectNew} style={{ marginRight: '8px' }}>
          Select New Picture
        </Button>
        <Button icon={<DeleteOutlined />} onClick={handleDelete} danger style={{ marginRight: '8px' }}>
          Delete Picture
        </Button>
        <Button type="primary" onClick={handleSave}>
          Save
        </Button>
      </div>
      <Upload {...uploadProps}>
        {/* Hidden upload component for additional functionality if needed */}
      </Upload>
    </div>
  );
};

export default ImagePicker;
