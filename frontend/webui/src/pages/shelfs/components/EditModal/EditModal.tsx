import { Modal, Input, Button } from 'antd';
import './EditModal.css';
import type { ShelfsDto } from '@/api/shelfs/shelfsApi';

interface EditModalProps {
  isModalVisible: boolean;
  name: string;
  shelf: ShelfsDto | null;
  onNameChange: (value: string) => void;
  onSave: () => void;
  onCancel: () => void;
  onClose: () => void;
}

export const EditModal = ({
  isModalVisible,
  name,
  onNameChange,
  onSave,
  onCancel,
  onClose,
}: EditModalProps) => {
  return (
    <Modal
      open={isModalVisible}
      onCancel={onClose}
      footer={null}
      width="50%"
      style={{ top: '50%', transform: 'translateY(-50%)' }}
      styles={{ mask: { backdropFilter: 'blur(5px)' } }}
      closable={false}
    >
      <Button onClick={onClose} style={{ position: 'absolute', top: '10px', right: '10px', zIndex: 1 }}>X</Button>
      <div className="edit-modal-content">
        <div className="edit-modal-form">
          <div className="form-field">
            <label>Name:</label>
            <Input value={name} onChange={(e) => onNameChange(e.target.value)} />
          </div>
          <div className="form-buttons">
            <Button onClick={onSave}>Save</Button>
            <Button onClick={onCancel}>Cancel</Button>
          </div>
        </div>
      </div>
    </Modal>
  );
};
