import React, { useState } from 'react';
import styles from './ShelfsPage.module.css';
import type { CreateShelfDto, ShelfsDto, UpdateShelfDto } from '@/api/shelfs/shelfsApi';
import Shelf from './components/Shelf/ShelfComponent';
import { DndContext, DragOverlay, MouseSensor, TouchSensor, useDroppable, useSensor, useSensors, type DragEndEvent, type DragStartEvent } from '@dnd-kit/core';
import Item from './components/Item/ItemComponent';

import { useShelfs } from '@/hooks/shelfs/useShelfs';
import { useNotification } from '@/notification/useNotification';
import { EditModal } from './components/EditModal/EditModal';

import { useUpdateShelf } from '@/hooks/shelfs/useUpdateShelf';
import { useCreateShelf } from '@/hooks/shelfs/useCreateShelf';
import { notificationColours } from '@/notification/notificationColours';
import { isAxiosError } from 'axios';
import { useNavigate } from 'react-router-dom';

// // Демо-дані для прикладу
// const mockData: ShelfsDto[] = [
//   { id: '1', name: 'name 1', userId: 'u1', items: [{ id: 'i1', name: 'item1', smallImage: '' }] },
//   { id: '2', name: 'name2', userId: 'u1', items: [{ id: 'i2', name: 'item', smallImage: '' }] },
//   { id: '3', name: 'name 3', userId: 'u1', items: [
//       { id: 'i3', name: 'item', smallImage: '' },
//       { id: 'i4', name: 'item', smallImage: '' },
//       { id: 'i5', name: 'item', smallImage: '' },
//       { id: 'i6', name: 'item', smallImage: '' }
//     ] 
//   },
//   { id: '4', name: 'name 4', userId: 'u1', items: [] },
//   { id: '5', name: 'name5', userId: 'u1', items: [{ id: 'i7', name: 'item', smallImage: '' }] },
// ];


const ShelfsPage: React.FC = () => {
  const { data: shelfs, isLoading, error } = useShelfs();
  const shelfsData = shelfs ?? [];
  const [activeId, setActiveId] = useState<string | null>(null);
  const activeItem = shelfsData.flatMap(s => s.items).find(i => i.id === activeId);
  const [activeType, setActiveType] = useState<'shelf' | 'item' | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const { notify } = useNotification();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [newShelfName, setNewName] = useState('');
  const [selectedShelf, setSelectedShelf] = useState<ShelfsDto | null>(null);

  const updateShelf = useUpdateShelf();
  const createShelf = useCreateShelf();
  const navigate = useNavigate();

const sensors = useSensors(
  useSensor(MouseSensor, {
    activationConstraint: {
      distance: 5, 
    },
  }),
  useSensor(TouchSensor, {
    activationConstraint: {
      delay: 250, // Для мобільних: затримка перед тягненням
      tolerance: 5,
    },
  })
);

  // Хендлери (ваша логіка)
  const handleMoveItem = (itemId: string, shelfId: string) => console.log(`Move ${itemId} to ${shelfId}`);
  const handleDeleteItem = (itemId: string) => console.log(`Delete Item ${itemId}`);
  const handleDeleteShelf = (shelfId: string) => console.log(`Delete Shelf ${shelfId}`);


  const handleCreateShelfClick = () =>{
    let defaultName = "new shelf";
    if(shelfs!= null && shelfs.length>0)
    for(let i =1; i<shelfs.length+1;i++)
    {
      if(shelfs.find(sh => sh.name == `${defaultName} ${i}`)==null){
        defaultName += ` ${i}`;
        break;
      }
    };
    setNewName(defaultName);
    setIsModalVisible(true);
  }


  const handleCreateItemClick = () =>{
    navigate("/create-item");
  }
  
  const handleSave = () => {
    // Logic for saving shelf (create or update)
    console.log('Save shelf:', name, selectedShelf);

    if(selectedShelf==null)
    {
      //creating new shelf because we dont have any id to update
      try {

      const payload: CreateShelfDto = {
        name:newShelfName,
      };
      console.log('Saving:',payload);
        createShelf.mutate(payload, {
          onSuccess: () => {
            console.log('Shelf created successfully');
            setIsModalVisible(false);
            setNewName('');
            setSelectedShelf(null);
                        
            notify({
              text: `Shelf ${newShelfName} created successfully`,
              colour: notificationColours.Green,
              time: 3000,
          })
          },
          onError: (error) => { 
              console.log('Failed to create Shelf');
              let code: number | undefined;

              // Use the type guard INSIDE the function body
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
        console.error('Failed to create Shelf');
        notify({
          colour: notificationColours.Red,
          code: 500,
          text: "Unexpected error",
          time: 3000,
        })
        console.log(error);
        
      } finally {
        //setLoading(false);
      }
    }
    else
    {
      //checking if id exist there
      if(selectedShelf?.id && newShelfName) 
      {
        //trying to update existing shelf
        try {
          const payload: UpdateShelfDto = {
            id: selectedShelf.id,
            name:newShelfName,
          };
          console.log('Saving:',payload);
  
  
          updateShelf.mutate(payload, {
            onSuccess: () => {
              console.log('Shelf updated successfully');
              notify({
                text: `Shelf ${newShelfName} updated successfully`,
                colour: notificationColours.Green,
                time: 3000,
            })
              setIsModalVisible(false);
              setNewName('');
              setSelectedShelf(null);
            },
            onError: () => {
              console.log('Failed to update Shelf', selectedShelf);
            }
          });
        } catch (error) {
          console.log('Failed to update Shelf', selectedShelf);
        } finally {
          //setLoading(false);
        }
      }
      else
        console.log("failed to get shelf");
    }

  };

  const handleCancel = () => {
    setNewName(selectedShelf?.name||"");
  };

  const handleClose = () => {
    setIsModalVisible(false);
    setNewName('');
    setSelectedShelf(null);
  };

  const handleDragStart = (event: DragStartEvent) => {
    const { active } = event;
    setActiveId(active.id as string);
    setActiveType(active.data.current?.type);
    setIsDragging(true);
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    setIsDragging(false);

    if (over) {
      const overId = over.id as string;
      const activeId = active.id as string;

      // Логіка видалення
      if (overId === 'delete-zone') {
        if (activeType === 'item') handleDeleteItem(activeId);
        if (activeType === 'shelf') handleDeleteShelf(activeId);
      } 
      // Логіка переміщення Item на іншу Shelf
      else if (activeType === 'item' && overId.startsWith('shelf-')) {
        const targetShelfId = overId.replace('shelf-', '');
        handleMoveItem(activeId, targetShelfId);
      }
    }
    setActiveId(null);
    setActiveType(null);
  };

  if (isLoading) return <div>Loading...</div>;
  if (error) {
    notify({    
      text: error.message,
      time: 3000,
    })
    return <div>Error loading shelfs</div>;
  }

  return (
    
    <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <DragOverlay style={{ }}>
      {activeType === 'item' && activeItem && (
        <Item
          item={activeItem}
          isOverlay
        />
      )}
    </DragOverlay>


      <div className={styles.pageWrapper}>
        <header className={styles.controls}>
          <div>
            <span>order by :</span>
            <button className={styles.filterBtn}>shelves</button>
            <button className={styles.filterBtn}>items</button>
          </div>
          <div style={{display: 'flex', float: 'right'}}>
            <button className={styles.filterBtn} onClick={(e) => { e.currentTarget.blur(); handleCreateShelfClick(); }}>Create new shelf</button>
            <button className={styles.filterBtn}onClick={(e) => { e.currentTarget.blur(); handleCreateItemClick(); }}>Create item</button>
          </div>
        </header>

        <main className={styles.gridContainer}>
          {shelfsData.map((shelf) => (
            <Shelf key={shelf.id} shelf={shelf} setSelectedShelf={setSelectedShelf} setName={setNewName} setIsModalVisible={setIsModalVisible} />
          ))}
        </main>
       {/* 2. Червона зона видалення */}
        {isDragging && (
          <DeleteZone id="delete-zone" />
        )}
      </div>
            <EditModal
              isModalVisible={isModalVisible}
              name={newShelfName}
              shelf={selectedShelf}
              onNameChange={setNewName}
              onSave={handleSave}
              onCancel={handleCancel}
              onClose={handleClose}
            />
    </DndContext>
  );
};

// Допоміжний компонент зони видалення
const DeleteZone = ({ id }: { id: string }) => {
  const { setNodeRef, isOver } = useDroppable({ id });
  return (
    <div 
      ref={setNodeRef} 
      className={`${styles.deleteZone} ${isOver ? styles.deleteZoneActive : ''}`}
    >
      DELETE
    </div>
  );
};

export default ShelfsPage;