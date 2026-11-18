import React, { useEffect, useState } from 'react';
import { Form, Input, Button, Space, message, Select, ColorPicker, Spin } from 'antd';
import { useShelfs } from '@/hooks/shelfs/useShelfs';
import { useAttributeValues } from '@/hooks/items/useAttributeValues';
import ImageShow from '@/components/ImageShow/ImageShow';
import { FavouriteButton } from '@/components/IsFavourite/IsFavourite';
import { useImage } from '@/hooks/items/useImage';
import { useRecogniozeExistingImage } from '@/hooks/image/useRecogniozeExistingImage';

const { Option } = Select;

interface ItemFormProps {
  initialValues?: any;
  loading: boolean;
  onSubmit: (values: any, bigImageName: string | undefined) => void;
  onBack: () => void;
}

export const ItemForm: React.FC<ItemFormProps> = ({ initialValues, loading, onSubmit, onBack }) => {
  const [form] = Form.useForm();
  const isFavoriteFormValue = Form.useWatch('isFavorite', form);
  const [isDirty, setIsDirty] = useState(false);
  
  const { data: shelfs, isLoading: shelfsLoading } = useShelfs();
  const { data: attributeValues, isLoading: attributesLoading } = useAttributeValues();
  
  const [bigImageName, setBigImageName] = useState<string | undefined>(initialValues?.bigImage); 
  const { data: imageItem, isLoading: imageLoading } = useImage(bigImageName);
  
  const isFavorite = isFavoriteFormValue || false;

  const {
    refetch: refetchRecognizedAttributes,
    isFetching: isRecognizing,
  } = useRecogniozeExistingImage(bigImageName);

  // Синхронізація початкових значень при завантаженні (актуально для редагування)
  useEffect(() => {
    if (initialValues) {
      form.setFieldsValue(initialValues);
      setBigImageName(initialValues.bigImage);
      setIsDirty(false);
    }
  }, [initialValues, form]);

  const getAttributeOptions = (typeName: string) => {
    const attribute = attributeValues?.find(attr => attr.typeName === typeName);
    return attribute?.options || [];
  };

  const handleToggleFavorite = () => {
    setIsDirty(true);
    form.setFieldValue('isFavorite', !isFavorite);
  };

  if (shelfsLoading || attributesLoading) {
    return <div style={{ color: 'white', textAlign: 'center' }}>Loading form options...</div>;
  }

  return (
    <Form
      form={form}
      layout="horizontal"
      labelAlign="left"
      onFinish={(values) => onSubmit(values, bigImageName)}
      onValuesChange={() => {
        const current = form.getFieldsValue(true);
        const hasName = current.name && current.name.trim().length > 0;
        const hasShelfId = current.shelfId !== undefined && current.shelfId !== null;
        setIsDirty(hasName && hasShelfId && JSON.stringify(current) !== JSON.stringify(initialValues));
      }}
      style={{ width: '100%', maxWidth: '1000px', background: 'transparent', boxShadow: 'none', padding: 0 }} 
    >
      {/* --- БЛОК ФОТОЗОНИ --- */}
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginBottom: '40px', width: '100%', position: 'relative' }}>
        <FavouriteButton 
          isFavorite={!!isFavorite}
          onToggle={handleToggleFavorite}
          style={{ position: 'absolute', top: '20px', right: '20px', zIndex: 10, backgroundColor: 'transparent', border: 'none', padding: 0 }}
          //iconStyle={{ fontSize: '36px', color: '#ffb900' }}
        />

        <Form.Item name="bigImage" valuePropName="value" style={{ width: '100%', marginBottom: 0 ,borderRadius: '6px',padding: 10,border: '1px solid rgba(255, 255, 255, 0.4)',}}>
          <ImageShow 
            src={imageItem || ''} 
            alt="Current Item"
            onDeleteUrl={() => {
              setBigImageName(undefined);
              setIsDirty(true); // Форма стає dirty, бо ми видалили існуючу картинку
            }}
          />
        </Form.Item>

        {/* На ItemForm залишаються ТІЛЬКИ кнопки розпізнавання, без дублюючої кнопки Видалити */}
        <div style={{ display: 'flex', gap: '15px', width: '100%', maxWidth: '100%', marginTop: '20px' }}>
          <Button style={{ backgroundColor: '#f5f5f5', color: '#4a4a4a', height: '60px', width: '50%', fontSize: '16px', borderRadius: '8px', textWrap: 'wrap', lineHeight: '1.2' }}>
            Detect using camera
          </Button>
          <Button 
            style={{ backgroundColor: '#f5f5f5', color: '#4a4a4a', height: '60px', width: '50%', fontSize: '16px', borderRadius: '8px', textWrap: 'wrap', lineHeight: '1.2' }}
            loading={isRecognizing}
            onClick={async () => {
              try {
                const res = await refetchRecognizedAttributes();
                const attrs = res.data;
                if (!attrs) { message.warning('Nothing recognized'); return; }
                setIsDirty(true);
                form.setFieldsValue({ attributeMatterial: attrs.attributeMaterial, attributeType: attrs.attributeType });
              } catch { message.error('Failed to recognize image'); }
            }}
          >
            Detect current image
          </Button>
        </div>
      </div>

      {/* --- ПОЛЯ ФОРМИ --- */}
      <Form.Item name="name" label={<span style={{ fontSize: '18px', color: 'white' }}>Name</span>} rules={[{ required: true, message: 'Input name!' }]} style={{ marginBottom: '25px' }} labelCol={{ span: 4 }} wrapperCol={{ span: 20 }}>
        <Input style={{ backgroundColor: 'transparent', color: 'white', border: '1px solid rgba(255, 255, 255, 0.4)', height: '40px' }} />
      </Form.Item>

      <Form.Item name="shelfId" label={<span style={{ fontSize: '18px', color: 'white' }}>Shelf</span>} rules={[{ required: true, message: 'Select shelf!' }]} style={{ marginBottom: '50px' }} labelCol={{ span: 4 }} wrapperCol={{ span: 20 }}>
        <Select placeholder="Select a shelf" style={{ width: '100%', height: '40px' }} className="dark-select" bordered={false}>
          {shelfs?.map(shelf => (<Option key={shelf.id} value={shelf.id}>{shelf.name}</Option>))}
        </Select>
      </Form.Item>

      {/* Рядок 1 — Кольори */}
      <div className="attribute-row" style={{ display: 'flex', flexWrap: 'wrap', gap: '20px', marginBottom: '25px' }}>
        <div className="attribute-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributeColorMain" label={<span style={{ fontSize: '16px', color: 'white' }}>Color 1</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <ColorPicker showText className="color-picker-trigger" style={{ backgroundColor: '#333', border: 'none', color: 'white', width: 'auto' }}/>
          </Form.Item>
        </div>
        <div className="attribute-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributeColorSecond" label={<span style={{ fontSize: '16px', color: 'white' }}>Color 2</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <ColorPicker showText className="color-picker-trigger" style={{ backgroundColor: '#333', border: 'none', color: 'white', width: 'auto' }}/>
          </Form.Item>
        </div>
      </div>

      {/* Рядок 2 — Type та Season */}
      <div className="attribute-row" style={{ display: 'flex', flexWrap: 'wrap', gap: '20px', marginBottom: '25px' }}>
        <div className="attribute-field attribute-selector-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributeType" label={<span style={{ fontSize: '16px', color: 'white' }}>Type</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <Select placeholder="Select Type" style={{ width: '100%', height: '40px' }} className="dark-select-attribute" bordered={false}>
              {getAttributeOptions('Type').map(option => <Option key={option.key} value={option.key}>{option.value}</Option>)}
            </Select>
          </Form.Item>
        </div>
        <div className="attribute-field attribute-selector-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributeSeason" label={<span style={{ fontSize: '16px', color: 'white' }}>Season</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <Select placeholder="Select Season" style={{ width: '100%', height: '40px' }} className="dark-select-attribute" bordered={false}>
              {getAttributeOptions('Season').map(option => <Option key={option.key} value={option.key}>{option.value}</Option>)}
            </Select>
          </Form.Item>
        </div>
      </div>

      {/* Рядок 3 — Pattern та Material */}
      <div className="attribute-row" style={{ display: 'flex', flexWrap: 'wrap', gap: '20px', marginBottom: '40px' }}>
        <div className="attribute-field attribute-selector-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributePattern" label={<span style={{ fontSize: '16px', color: 'white' }}>Pattern</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <Select placeholder="Select Pattern" style={{ width: '100%', height: '40px' }} className="dark-select-attribute" bordered={false}>
              {getAttributeOptions('Pattern').map(option => <Option key={option.key} value={option.key}>{option.value}</Option>)}
            </Select>
          </Form.Item>
        </div>
        <div className="attribute-field attribute-selector-field" style={{ flex: '1 1 250px' }}>
          <Form.Item name="attributeMatterial" label={<span style={{ fontSize: '16px', color: 'white' }}>Material</span>} labelCol={{ span: 6 }} wrapperCol={{ span: 18 }} style={{ marginBottom: 0 }}>
            <Select placeholder="Select Material" style={{ width: '100%', height: '40px' }} className="dark-select-attribute" bordered={false}>
              {getAttributeOptions('Material').map(option => <Option key={option.key} value={option.key}>{option.value}</Option>)}
            </Select>
          </Form.Item>
        </div>
      </div>

      <Form.Item name="isFavorite" initialValue={false} hidden><Input /></Form.Item>
      
      {/* КНОПКИ ЗБЕРЕЖЕННЯ */}
      <Form.Item wrapperCol={{ span: 24 }} style={{ textAlign: 'center', marginTop: '60px', marginBottom: 0 }}>
        <Space size="middle">
          <Button type="primary" htmlType="submit" loading={loading} disabled={!isDirty} style={{ backgroundColor: isDirty ? "#377cf6" : "#f1f1f1", color: isDirty ? "white" : "rgba(0, 0, 0, 0.4)", borderRadius: '6px', fontSize: '18px', padding: '8px 25px' }}>
            Save
          </Button>
          <Button onClick={onBack} style={{ backgroundColor: "white", color: "#3c3c3c", borderRadius: '6px', fontSize: '18px', padding: '8px 25px' }}>
            Back
          </Button>
        </Space>
      </Form.Item>

      <style>{`
        .ant-select-selector { border-radius: 6px !important; font-size: 16px !important; }
        .dark-select .ant-select-selector, .dark-select-attribute .ant-select-selector { background-color: transparent !important; border: 1px solid rgba(255, 255, 255, 0.4) !important; color: white !important; box-shadow: none !important; }
        .dark-select-attribute .ant-select-selector { height: 40px !important; }
        .dark-select .ant-select-selection-item, .dark-select-attribute .ant-select-selection-item { color: white !important; line-height: 38px !important; }
        .ant-select-arrow { color: rgba(255, 255, 255, 0.6) !important; font-size: 14px !important; }
        .ant-color-picker-trigger { border: 1px solid rgba(255, 255, 255, 0.4) !important; border-radius: 6px !important; }
        .ant-color-picker-trigger-text { color: white !important; font-size: 14px !important; }
      `}</style>
    </Form>
  );
};