import React, { useState } from 'react';
import {
  DndContext,
  closestCenter,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  verticalListSortingStrategy,
  useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import './MainArea.css';

interface TreeNode {
  id: string;
  title: string;
  children?: TreeNode[];
}

const initialTree: TreeNode[] = [
  {
    id: '1',
    title: 'Tile 1',
    children: [
      { id: '1-1', title: 'Tile 1-1' },
      { id: '1-2', title: 'Tile 1-2' },
    ],
  },
  {
    id: '2',
    title: 'Tile 2',
    children: [
      { id: '2-1', title: 'Tile 2-1' },
      { id: '2-2', title: 'Tile 2-2' },
    ],
  },
];

const Tile: React.FC<{ node: TreeNode }> = ({ node }) => {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: node.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    paddingLeft: '20px',
    border: '1px solid #ccc',
    marginBottom: '4px',
    backgroundColor: 'white',
    cursor: 'grab',
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      {node.title}
      {node.children && node.children.length > 0 && (
        <div style={{ paddingLeft: '20px' }}>
          {node.children.map((child) => (
            <Tile key={child.id} node={child} />
          ))}
        </div>
      )}
    </div>
  );
};

const MainArea: React.FC = () => {
  const [tree, setTree] = useState<TreeNode[]>(initialTree);

  const sensors = useSensors(useSensor(PointerSensor));

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      // For simplicity, only allow reordering at the root level
      const oldIndex = tree.findIndex((node) => node.id === active.id);
      const newIndex = tree.findIndex((node) => node.id === over.id);
      if (oldIndex !== -1 && newIndex !== -1) {
        const newTree = arrayMove(tree, oldIndex, newIndex);
        setTree(newTree);
        console.log('Tree reordered:', newTree);
      }
    }
  };

  return (
    <main className="main-area">
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={tree.map((node) => node.id)} strategy={verticalListSortingStrategy}>
          {tree.map((node) => (
            <Tile key={node.id} node={node} />
          ))}
        </SortableContext>
      </DndContext>
    </main>
  );
};

export default MainArea;
