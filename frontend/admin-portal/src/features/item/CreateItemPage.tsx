import React, { useState } from 'react';
import { Form, Input, Button, Space, message, Select, ColorPicker } from 'antd';

import { useNavigate } from 'react-router-dom';
import { useShelfs } from '@/hooks/shelfs/useShelfs';
import type { CreateItemDto } from '@/api/shelfs/itemsApi';
import { useCreateItem } from '@/hooks/items/useCreateItem';
import { useAttributeValues } from '@/hooks/items/useAttributeValues';
import ImagePicker from '@/components/ImagePicker/ImagePicker';
import ImageShow from '@/components/ImageShow/ImageShow';
import { useNotification } from '@/notification/useNotification';
import { isAxiosError } from 'axios';
import { FavouriteButton } from '@/components/IsFavourite/IsFavourite';


const { Option } = Select;

const CreateItemPage: React.FC = () => {
  const { notify } = useNotification();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const isFavoriteFormValue = Form.useWatch('isFavorite', form);
  const [loading, setLoading] = useState(false);
  const { data: shelfs, isLoading: shelfsLoading } = useShelfs();
  const { data: attributeValues, isLoading: attributesLoading } = useAttributeValues();
  const [isDirty, setIsDirty] = useState(false);
  const createItem = useCreateItem();
  const isFavorite = isFavoriteFormValue || false;
  // Get attribute options by type name
  const getAttributeOptions = (typeName: string) => {
    const attribute = attributeValues?.find(attr => attr.typeName === typeName);
    return attribute?.options || [];
  };



  if (shelfsLoading || attributesLoading) {
    return <div>Loading...</div>;
  }
  const handleToggle = () => {
    
    form.setFieldValue('isFavorite', !isFavorite);
  };
  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      console.log('Creating item:', values);

      const payload: CreateItemDto = {
        name: values.name,
        shelfId: values.shelfId,
        bigImage: values.bigImage,
        attributeColorMain: values.attributeColorMain,
        attributeColorSecond: values.attributeColorSecond,
        attributeType: values.attributeType,
        attributeSeason: values.attributeSeason,
        attributePattern: values.attributePattern,
        attributeMatterial: values.attributeMatterial,
        isFavorite: values.isFavorite,
      };

      createItem.mutate(payload, {
        onSuccess: () => {
          message.success('Item created successfully');
        },
        onError: (error) => {
          message.error('Failed to create item');
          let code: number | undefined;
          if (isAxiosError(error)) {
            code = error.response?.status;
          }
          notify({
            code: code,
            text: error.message,
            time: 3000,
        })
        }
      });
    } catch (error) {
      message.error('Failed to update item');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="item-edit-page">
      <h1>New Item</h1>
      <div style={{ display: 'flex', gap: '20px' }}>
        <div style={{ flex: '60%' }}>
          <Form
            form={form}
            layout="horizontal"
            onFinish={handleSubmit}
            style={{ maxWidth: 800 }}
            labelCol={{ span: 6 }}
            wrapperCol={{ span: 18 }}
            onValuesChange={() => {
              const current = form.getFieldsValue(true);
              
              // For create page, check if required fields have values
              const hasName = current.name && current.name.trim().length > 0;
              const hasShelfId = current.shelfId !== undefined && current.shelfId !== null;
              
              setIsDirty(hasName && hasShelfId);
            }}
          >
            <Form.Item
              name="name"
              label={<span style={{ fontSize: '16px', color: 'white' }}>Name</span>}
              rules={[{ required: true, message: 'Please input the name!' }]}
            >
              <Input style={{ width: '100%' }} />
            </Form.Item>

            <Form.Item
              name="shelfId"
              label={<span style={{ fontSize: '16px', color: 'white' }}>Shelf</span>}
              rules={[{ required: true, message: 'Please select a shelf!' }]}
            >
              <Select placeholder="Select a shelf" style={{ width: '100%' }}>
                {shelfs?.map(shelf => (
                  <Option key={shelf.id} value={shelf.id}>
                    {shelf.name}
                  </Option>
                ))}
              </Select>
            </Form.Item>

            {/* Color Pickers - Row 1 */}
            <div style={{ display: 'flex', gap: '20px' }}>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributeColorMain"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Color 1</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}

                  initialValue="#000000"
                  // This intercepts the output of ColorPicker and saves only the hex string to the form
                  getValueFromEvent={(color) => {
                    return typeof color === 'string' ? color : color?.toHexString();
                  }}
                >
                  <ColorPicker defaultValue="#000000" showText style={{width: '100px'}}/>
                </Form.Item>
              </div>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributeColorSecond"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Color 2</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}
                  initialValue="#000000"
                  getValueFromEvent={(color) => {
                    return typeof color === 'string' ? color : color?.toHexString();
                  }}
                >
                  <ColorPicker defaultValue="#000000" showText style={{width: '100px'}}/>
                </Form.Item>
              </div>
            </div>

            {/* Type and Season - Row 2 */}
            <div style={{ display: 'flex', gap: '20px' }}>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributeType"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Type</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}
                >
                  <Select placeholder="Select Type" style={{ width: '100%' }}>
                    {getAttributeOptions('Type').map(option => (
                      <Option key={option.key} value={option.key}>
                        {option.value}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </div>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributeSeason"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Season</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}
                >
                  <Select placeholder="Select Season" style={{ width: '100%' }}>
                    {getAttributeOptions('Season').map(option => (
                      <Option key={option.key} value={option.key}>
                        {option.value}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </div>
            </div>

            {/* Pattern and Material - Row 3 */}
            <div style={{ display: 'flex', gap: '20px' }}>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributePattern"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Pattern</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}
                >
                  <Select placeholder="Select Pattern" style={{ width: '100%' }}>
                    {getAttributeOptions('Pattern').map(option => (
                      <Option key={option.key} value={option.key}>
                        {option.value}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </div>
              <div style={{ flex: 1 }}>
                <Form.Item
                  name="attributeMatterial"
                  label={<span style={{ fontSize: '16px', color: 'white' }}>Material</span>}
                  labelCol={{ span: 6 }}
                  wrapperCol={{ span: 18 }}
                >
                  <Select placeholder="Select Material" style={{ width: '100%' }}>
                    {getAttributeOptions('Material').map(option => (
                      <Option key={option.key} value={option.key}>
                        {option.value}
                      </Option>
                    ))}
                  </Select>
                </Form.Item>
              </div>
            </div>

            {/* isFavorite managed by overlay button, no form item needed */}
            <Form.Item name="isFavorite" initialValue={false} hidden>
            <Input />
          </Form.Item>
            <Form.Item wrapperCol={{ span: 24 }}>
              <div style={{ textAlign: 'center' }}>
                <Space>
                  <Button type="primary" htmlType="submit" loading={loading} disabled={!isDirty}
                    style={!isDirty ? { 
                      backgroundColor: "#ffffff", 
                      color: "rgba(0, 0, 0, 0.25)",
                      borderColor: "#d9d9d9"
                    } : {}}>
                    Save
                  </Button>
                  <Button onClick={() => navigate('/')}>
                    Back
                  </Button>
                </Space>
              </div>
            </Form.Item>
          </Form>
        </div>
        <div style={{ flex: '40%' }}>
        <div style={{ position: 'relative', width: '100%', display: 'inline-block' }}>
          <FavouriteButton 
              isFavorite={!!isFavorite} // ensure it passes a boolean
              onToggle={handleToggle}
              style={{
                position: 'absolute' as const,
                top: '8px',
                right: '8px',
                zIndex: 10,
                backgroundColor: 'rgba(0,0,0,0.6)',
                borderRadius: '50%',
                padding: '4px'
              }}
            />
          <div style={{ marginBottom: '20px', padding: '10px', minHeight: '200px', border: '1px solid #d9d9d9', borderRadius: '4px', position: 'relative' }}>
            <ImageShow
              src={""}
              alt="Current Item"
              style={{ maxHeight: '60%' }}
            />

          </div>
            <ImagePicker
              onChange={(value: string) => form.setFieldsValue({ bigImage: value })}
            />
          
            </div>
        </div>
      </div>
    </div>
  );
};

export default CreateItemPage;

