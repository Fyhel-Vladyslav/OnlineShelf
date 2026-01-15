import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Space, message, Select } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import { useShelfs } from '@/hooks/shelfs/useShelfs';
import { useItem } from '@/hooks/items/useItem';
import type { UpdateItemDto } from '@/api/shelfs/itemsApi';
import { useUpdateItem } from '@/hooks/items/useUpdateItem';
import ImagePicker from '@/components/ImagePicker/ImagePicker';
import ImageShow from '@/components/ImageShow/ImageShow';

const { Option } = Select;

const ItemEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const { data: item, isLoading: itemLoading } = useItem(id && id !== 'undefined' ? id : undefined);
  const { data: shelfs, isLoading: shelfsLoading } = useShelfs();
  const [initialValues, setInitialValues] = useState<any>(null);
  const [isDirty, setIsDirty] = useState(false);
  const updateItem = useUpdateItem();

  useEffect(() => {
    if (item) {
      form.setFieldsValue(item);
      setInitialValues(item);
    }
  }, [item, form]);

  // Don't render anything if id is not available yet
  if (!id || id === 'undefined') {
    return <div>Loading...</div>;
  }

  if (shelfsLoading || itemLoading) {
    return <div>Loading...</div>;
  }

  if (!item) {
    message.error("Item not found");
    navigate("/");
    return null;
  }

  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      console.log('Updating item:', values);

      const payload: UpdateItemDto = {
        id: item.id,
        name: values.name,
        shelfId: values.shelfId,
        bigImage: values.bigImage,
        smallImage: values.smallImage,
      };

      updateItem.mutate(payload, {
        onSuccess: () => {
          message.success('Item updated successfully');
          const updatedValues = form.getFieldsValue(true);
          setInitialValues(updatedValues);
          setIsDirty(false);
        },
        onError: () => {
          message.error('Failed to update item');
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
      <h1>Edit Item</h1>
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
              if (!initialValues) return;

              const current = form.getFieldsValue(true);

              setIsDirty(JSON.stringify(current) !== JSON.stringify(initialValues));
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
        <div style={{ marginBottom: '20px', padding: '10px', minHeight: '200px', border: '1px solid #d9d9d9', borderRadius: '4px' }}>
          <ImageShow
            src={""}
            alt="Current Item"
            style={{ maxHeight: '60%' }}
          />
        </div>
          <ImagePicker
            value={item.bigImage}
            onChange={(value: string) => form.setFieldsValue({ bigImage: value })}
          />
        </div>
      </div>
    </div>
  );
};

export default ItemEditPage;
