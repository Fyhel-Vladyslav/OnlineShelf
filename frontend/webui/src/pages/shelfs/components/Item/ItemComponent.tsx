import React from 'react';
import styles from './ItemComponent.module.css';
import type { ItemPreviewDto } from '@/api/shelfs/itemsApi';
import { useDraggable } from '@dnd-kit/core';
import noImageSmall from '@/assets/images/noPhotoLoadedSmall.jpg';
import { useNavigate } from 'react-router-dom';
import { useImage } from '@/hooks/items/useImage';

interface ItemProps {
    item: ItemPreviewDto;
    isOverlay?: boolean;
  }
  const Item: React.FC<ItemProps> = ({ item, isOverlay = false }) => {
    const navigate = useNavigate();
    const {attributes,listeners,setNodeRef,isDragging,} = useDraggable({
      id: item.id,
      data: { type: 'item' },
    });
    // smallImage — ім'я файлу, саму картинку тягнемо через ImageService
    const { data: imageUrl } = useImage(item.smallImage || undefined);
    // const handleClick = (e: React.MouseEvent) => {
      const handleClick = () => {  
    console.log("da");
      
      // Якщо під час кліку ми почали тягнути, dnd-kit зазвичай зупинить подію.
      // Переходимо на сторінку айтема
      navigate(`/item-edit/${item.id}`);
    };
    return (
      <div
        ref={isOverlay ? undefined : setNodeRef}
        {...(!isOverlay ? listeners : {})}
        {...(!isOverlay ? attributes : {})}
        onClick={handleClick}
        className={styles.itemContainer}
        style={{
          opacity: !isOverlay && isDragging ? 0 : 1,
          cursor: isOverlay ? 'grabbing' : 'grab',
          backgroundColor: 'white',
        }}
      >
        <div className={styles.imageWrapper}>
          <img
            src={imageUrl ?? noImageSmall}
            alt=""
            className={styles.previewImage}
          />
        </div>
  
        <div className={styles.itemName}>
          {item.name}
        </div>
      </div>
    );
  };
  
  export default Item;