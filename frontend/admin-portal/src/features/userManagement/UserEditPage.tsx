import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Select, Switch, InputNumber, Space, message } from 'antd';
import { useParams, useNavigate } from 'react-router-dom';
import './UserEditPage.css';
import './UserManagementPage.css';
import { useUser } from '@/hooks/users/useUser';

const { Option } = Select;


const UserEditPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const { data: user, isLoading, error } = useUser(id);

  useEffect(() => {
    if (user) {
      form.setFieldsValue(user);
    }
  }, [user, form]);

  useEffect(() => {
    if (error) {
      message.error("User not found");
      navigate("/user-management");
    }
  }, [error, navigate]);

  if (isLoading) {
    return <div>Loading user...</div>;
  }

  const handleSubmit = async (values: any) => {
    setLoading(true);
    try {
      // Mock API call - replace with actual API
      console.log('Updating user:', values);
      message.success('User updated successfully');
      navigate('/user-management');
    } catch (error) {
      message.error('Failed to update user');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="user-edit-page">
      <h1>Edit User</h1>
      <Form
        form={form}
        layout="horizontal"
        onFinish={handleSubmit}
        style={{ maxWidth: 800 }}
        labelCol={{ span: 6 }}
        wrapperCol={{ span: 18 }}
      >
        <Form.Item
          name="email"
          label="Email"
          rules={[
            { required: true, message: 'Please input the email!' },
            { type: 'email', message: 'Please enter a valid email!' }
          ]}
        >
          <Input />
        </Form.Item>

        <Form.Item
          name="login"
          label="Login"
          rules={[{ required: true, message: 'Please input the login!' }]}
        >
          <Input />
        </Form.Item>

        <Form.Item
          name="state"
          label="State"
          rules={[{ required: true, message: 'Please input the state!' }]}
        >
          <InputNumber min={0} max={10} />
        </Form.Item>

        <Form.Item
          name="avatar"
          label="Avatar"
        >
        <Button onClick={() => navigate('/reset-user-password')}>
              Reset password
            </Button>
        </Form.Item>

        <Form.Item
          name="role"
          label="Roles"
          rules={[{ required: true, message: 'Please select at least one role!' }]}
        >
          <Select mode="multiple" placeholder="Select roles">
            <Option value="admin">Admin</Option>
            <Option value="user">User</Option>
            <Option value="moderator">Moderator</Option>
            <Option value="editor">Editor</Option>
          </Select>
        </Form.Item>

        <Form.Item wrapperCol={{ span: 24 }}>
  <div style={{ textAlign: 'center' }}>
    <Space>
      <Button type="primary" htmlType="submit" loading={loading}>
        Save
      </Button>
      <Button onClick={() => navigate('/user-management')}>
        Cancel
      </Button>
    </Space>
  </div>
</Form.Item>
      </Form>
    </div>
  );
};

export default UserEditPage;
