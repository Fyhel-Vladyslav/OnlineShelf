import { Modal, Input, Button } from 'antd';
import './EditModal.css';

type TreeDataNode = {
  title: string;
  key: string;
  children?: TreeDataNode[];
  isLeaf?: boolean;
};

interface EditModalProps {
  isModalVisible: boolean;
  selectedLeaf: TreeDataNode | null;
  name: string;
  onNameChange: (value: string) => void;
  onSave: () => void;
  onCancel: () => void;
  onClose: () => void;
}

export const EditModal = ({
  isModalVisible,
  selectedLeaf,
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
      width={selectedLeaf?.isLeaf ? "75%" : "50%"}
      style={{ top: '50%', transform: 'translateY(-50%)' }}
      styles={{ mask: { backdropFilter: 'blur(5px)' } }}
      closable={false}
    >
      <Button onClick={onClose} style={{ position: 'absolute', top: '10px', right: '10px', zIndex: 1 }}>X</Button>
      <div className="edit-modal-content">
        {selectedLeaf?.isLeaf && (
          <div className="edit-modal-image">
            <img
              src="https://via.placeholder.com/400x400?text=Huge+Picture"
              alt="Leaf Image"
            />
          </div>
        )}
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
