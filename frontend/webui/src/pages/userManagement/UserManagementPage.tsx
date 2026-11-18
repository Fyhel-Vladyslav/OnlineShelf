import React, { useState } from 'react';
import { Table, Button, Space, Modal, Tag } from 'antd';
import { EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useUsers } from '@/hooks/users/useUsers';
import { useDeleteUser } from '@/hooks/users/useDeleteUser';
import type { UserDto } from '@/api/users/userApi';
import './UserManagementPage.css';

type AlignType = 'left' | 'center' | 'right';


const UserManagementPage: React.FC = () => {
  const navigate = useNavigate();
  const { data: users, isLoading, error } = useUsers();
  const deleteUserMutation = useDeleteUser();
  const [deleteModalVisible, setDeleteModalVisible] = useState(false);
  const [userToDelete, setUserToDelete] = useState<string | null>(null);

  const handleEdit = (user: UserDto) => {
    navigate(`/user-management/edit/${user.id}`);
  };
 
  const handleDelete = (userId: string) => {
    setUserToDelete(userId);
    setDeleteModalVisible(true);
  };

  const confirmDelete = () => {
    if (userToDelete) {
      deleteUserMutation.mutate(userToDelete);
    }
    setDeleteModalVisible(false);
    setUserToDelete(null);
  };

  const cancelDelete = () => {
    setDeleteModalVisible(false);
    setUserToDelete(null);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: '15%',
    },
    {
      title: 'Role',
      dataIndex: 'roles',
      key: 'roles',
      width: '8%',
      align: 'right' as AlignType,
      render: (roles: string[]) => {
        if (!roles || roles.length === 0) {
          return <span style={{ color: 'gray', textAlign: 'center', display: 'block' }}>no roles</span>;
        }
        return (
          <Space size="small">
            {roles.map((role, index) => (
              <Tag key={index} color="blue">
                {role}
              </Tag>
            ))}
          </Space>
        );
      },
    },
    {
      title: 'Login',
      dataIndex: 'login',
      key: 'login',
      width: '52%',
    },
    {
      title: 'Actions',
      key: 'actions',
      width: '25%',
      align: 'right' as AlignType,
      render: ( record: UserDto) => (
        <Space size="middle">
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            Edit
          </Button>
          <Button
            type="link"
            danger
            icon={<DeleteOutlined />}
            onClick={() => handleDelete(record.id)}
          >
            Delete
          </Button>
        </Space>
      ),
    },
  ];

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error loading users</div>;

  return (
    <div className="user-management-page">
      <h1>User Management</h1>
      <Table
        columns={columns}
        dataSource={users}
        rowKey="id"
        pagination={false}
        className="user-table"
      />
      <Modal
        title="Confirm Delete"
        open={deleteModalVisible}
        onOk={confirmDelete}
        onCancel={cancelDelete}
        okText="Delete"
        cancelText="Cancel"
        okButtonProps={{ danger: true }}
      >
        <p>Are you sure you want to delete this user?</p>
      </Modal>
    </div>
  );
};

export default UserManagementPage;
