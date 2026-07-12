import React from 'react';
import styles from './ShelfComponent.module.css';
import type { ShelfsDto } from '@/api/shelfs/shelfsApi';
import Item from '@/features/shelfs/components/Item/ItemComponent';
import { useDraggable, useDroppable } from '@dnd-kit/core';
import { Button } from 'antd';
import { EditOutlined } from '@ant-design/icons';


const Shelf: React.FC<{
  shelf: ShelfsDto;
  setSelectedShelf: (shelf: ShelfsDto | null) => void;
  setName: (name: string) => void;
  setIsModalVisible: (visible: boolean) => void;
}> = ({ shelf, setSelectedShelf, setName, setIsModalVisible }) => {
  // Зробимо полицю такою, що може приймати предмети
  const { setNodeRef: setDroppableRef } = useDroppable({
    id: `shelf-${shelf.id}`,
  });

  // Зробимо полицю такою, що можна тягнути
  const { attributes, listeners, setNodeRef: setDraggableRef, transform } = useDraggable({
    id: shelf.id,
    data: { type: 'shelf' }
  });

  const style = transform ? {
    transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
  } : undefined;

  return (
    <div 
      ref={setDroppableRef} 
      className={styles.shelfDroppableArea}
    >
      <div 
        ref={setDraggableRef} 
        style={style} 
        className={styles.shelfCard}
        {...listeners} 
        {...attributes}
      >
        <Button
            type="text"
            icon={<EditOutlined />}
            size="small"
            onClick={(e) => {
              e.stopPropagation();              
              setIsModalVisible(true);
              setSelectedShelf(shelf);
              setName(shelf.name);
            }}
            style={{ margin: 8, fontSize: 12, display: 'flex', float: 'right', }}
          />
        <div className={styles.shelfHeader}>
          <h3 className={styles.shelfTitle}>{shelf.name}</h3>
        </div>
        <div className={styles.shelfContent}>
          {shelf.items.length === 0 && <div>No items</div>}
          {shelf.items.map((item) => <Item key={item.id} item={item} />)}
        </div>
      </div>
    </div>
  );
};
export default Shelf;

// <div className={styles.shelfContent}>
// {hasItems ? (
//   shelf.items.map((item) => <Item key={item.id} item={item} />)
// ) : (
//   <div className={styles.noItems}>no items</div>
// )}
// </div>