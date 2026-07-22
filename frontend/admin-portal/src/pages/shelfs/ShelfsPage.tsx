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
import { useDeleteItem } from '@/hooks/shelfs/useDeleteItem';
import { useDeleteShelf } from '@/hooks/shelfs/useDeleteShelf';
import { notificationColours } from '@/notification/notificationColours';
import { isAxiosError } from 'axios';
import { useNavigate } from 'react-router-dom';
import { useMoveItem } from '@/hooks/items/useMoveItem';
import type { MoveItemParams } from '@/api/shelfs/itemsApi';
import { Modal } from 'antd'; // Imported Modal
import ImageShow from '@/components/ImageShow/ImageShow';

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
  
  // State to manage the declarative delete confirmation modal
  const [shelfToDelete, setShelfToDelete] = useState<string | null>(null);

  // Sidebar state (collapsed by default)
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  const toggleSidebar = () => {
    setIsSidebarOpen(prev => !prev);
  };

  const updateShelf = useUpdateShelf();
  const createShelf = useCreateShelf();
  const moveItem = useMoveItem();
  const deleteItem = useDeleteItem();
  const deleteShelf = useDeleteShelf();
  const navigate = useNavigate();

  const sensors = useSensors(
    useSensor(MouseSensor, {
      activationConstraint: {
        distance: 5, 
      },
    }),
    useSensor(TouchSensor, {
      activationConstraint: {
        delay: 250, 
        tolerance: 5,
      },
    })
  );

  const handleMoveItem = (itemId: string, shelfId: string) => {
    const payload: MoveItemParams = {
      itemId: itemId,
      newShelfId: shelfId
    };
    
    moveItem.mutate(payload, {
      onError: (error) => { 
        console.log('Failed to move Item');
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
  }

  const handleCreateShelfClick = () => {
    let defaultName = "new shelf";
    if(shelfs != null && shelfs.length > 0) {
      for(let i = 1; i < shelfs.length + 1; i++) {
        if(shelfs.find(sh => sh.name == `${defaultName} ${i}`) == null) {
          defaultName += ` ${i}`;
          break;
        }
      }
    }
    setNewName(defaultName);
    setIsModalVisible(true);
  }

  const handleCreateItemClick = () => {
    navigate("/create-item");
  }
  
  const handleSave = () => {
    if(selectedShelf == null) {
      try {
        const payload: CreateShelfDto = {
          name: newShelfName,
        };
        createShelf.mutate(payload, {
          onSuccess: () => {
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
        notify({
          colour: notificationColours.Red,
          code: 500,
          text: "Unexpected error",
          time: 3000,
        })
      }
    } else {
      if(selectedShelf?.id && newShelfName) {
        try {
          const payload: UpdateShelfDto = {
            id: selectedShelf.id,
            name: newShelfName,
          };
  
          updateShelf.mutate(payload, {
            onSuccess: () => {
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
        }
      }
    }
  };

  const handleCancel = () => {
    setNewName(selectedShelf?.name || "");
  };

  const handleClose = () => {
    setIsModalVisible(false);
    setNewName('');
    setSelectedShelf(null);
  };

  // Extracted core delete logic
  const executeDeleteShelf = (id: string) => {
    deleteShelf.mutate(id, {
      onSuccess: () => {
        console.log('Shelf deleted successfully');
        setShelfToDelete(null); // Close modal on success
      },
      onError: (error) => { 
        console.log('Failed to delete Shelf');
        let code: number | undefined;
        if (isAxiosError(error)) {
          code = error.response?.status;
        }
        notify({
          code: code,
          text: error.message,
          time: 3000,
        });
        setShelfToDelete(null); // Close modal even on error
      }
    });
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

      if (overId === 'delete-zone') {
        if (activeType === 'item') {
          deleteItem.mutate(activeId, {
            onSuccess: () => console.log('Item deleted successfully'),
            onError: (error) => { 
              let code: number | undefined;
              if (isAxiosError(error)) code = error.response?.status;
              notify({ code, text: error.message, time: 3000 });
            }
          });
        }

        if (activeType === 'shelf') {
          const targetShelf = shelfsData.find(s => s.id === activeId);
          // If shelf has items, open the confirmation modal via state
          if (targetShelf?.items.length && targetShelf.items.length > 0) {
            setShelfToDelete(activeId); 
          } else {
            // Delete immediately if empty
            executeDeleteShelf(activeId); 
          }
        }
      } else if (activeType === 'item' && overId.startsWith('shelf-')) {
        const targetShelfId = overId.replace('shelf-', '');
        handleMoveItem(activeId, targetShelfId);
      }
    }
    setActiveId(null);
    setActiveType(null);
  };

  if (isLoading) return <div>Loading...</div>;
  if (error) {
    notify({ text: error.message, time: 3000 });
    return <div>Error loading shelfs</div>;
  }

  return (
    <DndContext sensors={sensors} onDragStart={handleDragStart} onDragEnd={handleDragEnd}>
      <DragOverlay>
        {activeType === 'item' && activeItem && (
          <Item item={activeItem} isOverlay />
        )}
      </DragOverlay>

      <div className={styles.pageWrapper} style={{ '--sidebar-w': isSidebarOpen ? 'clamp(200px, 20%, 300px)' : '0px' } as React.CSSProperties}>
        {/* Left Sidebar */}
        <aside className={styles.sidebar}>
          <div className={styles.sidebarInner}>
            <ImageShow />
          </div>
        </aside>

        {/* Hamburger Toggle Button */}
        <button
          className={styles.hamburgerBtn}
          onClick={toggleSidebar}
          aria-label={isSidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
        >
          {isSidebarOpen ? '✕' : '☰'}
        </button>

        {/* Main Content Area */}
        <div className={styles.mainArea}>
          <header className={styles.controls}>
            <div>
              <span>order by :</span>
              <button className={styles.filterBtn}>shelves</button>
              <button className={styles.filterBtn}>items</button>
            </div>
            <div style={{display: 'flex', float: 'right'}}>
              <button className={styles.filterBtn} onClick={(e) => { e.currentTarget.blur(); handleCreateShelfClick(); }}>Create new shelf</button>
              <button className={styles.filterBtn} onClick={(e) => { e.currentTarget.blur(); handleCreateItemClick(); }}>Create item</button>
            </div>
          </header>

          <main className={styles.gridContainer}>
            {shelfsData.map((shelf) => (
              <Shelf key={shelf.id} shelf={shelf} setSelectedShelf={setSelectedShelf} setName={setNewName} setIsModalVisible={setIsModalVisible} />
            ))}
          </main>
        
          {isDragging && (
            <DeleteZone id="delete-zone" />
          )}
        </div>
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

      {/* Declarative Modal to fix the Ant Design warning */}
      <Modal
        title="Confirm delete shelf"
        open={!!shelfToDelete} // Converts string id to boolean
        onOk={() => {
          if (shelfToDelete) {
            executeDeleteShelf(shelfToDelete);
          }
        }}
        onCancel={() => setShelfToDelete(null)} // Close on cancel
        okText="Yes, delete"
        cancelText="Cancel"
      >
        <p>This shelf contains items. Are you sure you want to delete it?</p>
      </Modal>

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