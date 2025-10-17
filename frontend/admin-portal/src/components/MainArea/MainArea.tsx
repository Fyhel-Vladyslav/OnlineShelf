import { Tree, Button } from 'antd';
import type { GetProps } from 'antd';
import { useState } from 'react';
import { EditOutlined } from '@ant-design/icons';
import { EditModal } from './EditModal';

type DirectoryTreeProps = GetProps<typeof Tree.DirectoryTree>;

type TreeDataNode = {
  title: string;
  key: string;
  children?: TreeDataNode[];
  isLeaf?: boolean;
};


const { DirectoryTree } = Tree;
import './MainArea.css';

const initialTreeData: TreeDataNode[] = [
  {
    title: 'parent 0',
    key: '0-0',
    children: [
      { title: 'a1', key: '0-0-0', isLeaf: true },
      { title: 'a2', key: '0-0-1', isLeaf: true },
    ],
  },
  {
    title: 'parent 1',
    key: '0-1',
    children: [
      { title: 'b1', key: '0-1-0', isLeaf: true },
      { title: 'b2', key: '0-1-1', isLeaf: true },
      { title: 'b3', key: '0-1-2', isLeaf: true },
      { title: 'b4', key: '0-1-3', isLeaf: true },
    ],
  },
  {
    title: 'parent 2',
    key: '0-2',
    children: [
    ],
  },
];

export const MainArea = () => {
  const [treeData, setTreeData] = useState<TreeDataNode[]>(initialTreeData);
  const [draggedKey, setDraggedKey] = useState<string | null>(null);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [selectedLeaf, setSelectedLeaf] = useState<TreeDataNode | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const findNode = (data: TreeDataNode[], key: string): TreeDataNode | null => {
    for (const item of data) {
      if (item.key === key) return item;
      if (item.children) {
        const found = findNode(item.children, key);
        if (found) return found;
      }
    }
    return null;
  };

  const findPath = (data: TreeDataNode[], key: string, path: string[] = []): string[] | null => {
    for (let i = 0; i < data.length; i++) {
      const item = data[i];
      if (item.key === key) return [...path, i.toString()];
      if (item.children) {
        const found = findPath(item.children, key, [...path, i.toString()]);
        if (found) return found;
      }
    }
    return null;
  };

  const removeNode = (data: TreeDataNode[], path: string[]): TreeDataNode[] => {
    const [index, ...rest] = path;
    if (rest.length === 0) {
      return data.filter((_, i) => i !== parseInt(index));
    } else {
      return data.map((item, i) =>
        i === parseInt(index) ? { ...item, children: removeNode(item.children || [], rest) } : item
      );
    }
  };

  const insertNode = (data: TreeDataNode[], path: string[], node: TreeDataNode, position: number): TreeDataNode[] => {
    const [index, ...rest] = path;
    if (rest.length === 0) {
      const newData = [...data];
      newData.splice(parseInt(index) + position, 0, node);
      return newData;
    } else {
      return data.map((item, i) =>
        i === parseInt(index) ? { ...item, children: insertNode(item.children || [], rest, node, position) } : item
      );
    }
  };

  const myInsertNode = (
    data: TreeDataNode[],
    parentPosition: number,
    node: TreeDataNode,
    position: number
  ): TreeDataNode[] => {
    const newData = [...data];
    const parentElement = { ...newData[parentPosition] };
    const children = [...(parentElement.children || [])];
    children.splice(position, 0, node);
    parentElement.children = children;
    newData[parentPosition] = parentElement;
  
    return newData;
  };

  const addParent = (data: TreeDataNode[]) => {
    const newIndex = data.length;
    const newNode: TreeDataNode = {
      title: `parent ${newIndex}`,
      key: `0-${newIndex}`,
      children: [],
    };
    return [...data, newNode];
  };

  const updateTree = (data: TreeDataNode[], targetKey: string, newChild: TreeDataNode): TreeDataNode[] => {
    return data.map(node => {
      if (node.key === targetKey) {
        return { ...node, children: [...(node.children || []), newChild] };
      } else if (node.children) {
        return { ...node, children: updateTree(node.children, targetKey, newChild) };
      }
      return node;
    });
  };

  const addChild = (parentKey: string) => {
    const parentNode = findNode(treeData, parentKey);
    if (!parentNode) return;
    const childIndex = parentNode.children ? parentNode.children.length : 0;
    const newChild: TreeDataNode = {
      title: `new child`,
      key: `${parentKey}-${childIndex}`,
      isLeaf: true,
    };
    setTreeData(prev => updateTree(prev, parentKey, newChild));
  };

  const fetchData = async (key: string) => {
    // Mock API call
    return new Promise<{ name: string; description: string }>((resolve) => {
      setTimeout(() => {
        resolve({
          name: `Name for ${key}`,
          description: `Description for ${key}`,
        });
      }, 0);
    });
  };

  const handleLeafClick = async (node: TreeDataNode) => {
    setSelectedLeaf(node);
    setIsModalVisible(true);
    const data = await fetchData(node.key);
    setName(data.name);
    setDescription(data.description);
  };

  const handleSave = async () => {
    // Mock save to server
    console.log('Saving:', { key: selectedLeaf?.key, name, description });
    setIsModalVisible(false);
  };

  const handleCancel = async () => {
    if (selectedLeaf) {
      const data = await fetchData(selectedLeaf.key);
      setName(data.name);
      setDescription(data.description);
    }
  };

  const handleClose = () => {
    setIsModalVisible(false);
  };

  const onSelect: DirectoryTreeProps['onSelect'] = (keys, info) => {
    console.log('Trigger Select', keys, info);
  };

  const onDragStart: DirectoryTreeProps['onDragStart'] = (info) => {
    const deleteZone = document.querySelector(".tree-delete-zone");
    if (deleteZone) deleteZone.classList.remove("Hidden");
    console.log("da");
    setDraggedKey(info.node.key as string);
     };

  const onDragEnd: DirectoryTreeProps['onDragEnd'] = () => {
    const deleteZone = document.querySelector(".tree-delete-zone");
    if (deleteZone) deleteZone.classList.add("Hidden");
    console.log("piz");
    setDraggedKey(null);
    };

  const onDrop: DirectoryTreeProps['onDrop'] = (info) => {
    const { dragNode, node, dropPosition, dropToGap } = info;
    const dragKey = dragNode.key as string;
    const targetKey = node.key as string;

    const draggedNode = findNode(treeData, dragKey);
    if (!draggedNode) return;

    const dragPath = findPath(treeData, dragKey);
    if (!dragPath) return;

    let newTreeData = treeData;
    const targetPath = findPath(newTreeData, targetKey);
    if (!targetPath) return;

    newTreeData = removeNode(treeData, dragPath);
    if (!draggedNode.isLeaf) {
      // Relocate as sibling at root level
      const targetRootIndex = parseInt(targetPath[0]);
      let insertPos;
      if (dropToGap) {
        insertPos = targetRootIndex + (dropPosition > 0 ? 1 : dropPosition < 0 ? 0 : 0);
      } else {
        insertPos = targetRootIndex + 1;
      }
      const newData = [...newTreeData];
      newData.splice(insertPos, 0, draggedNode);
      newTreeData = newData;
    } else {
      if (dropToGap) {
        // Insert as sibling
        const parentPath = parseInt(targetPath[0]);
        const siblingIndex = parseInt(targetPath[targetPath.length - 1]);
        const insertPos = siblingIndex + (dropPosition > 0 ? 1 : dropPosition < 0 ? 0 : 0);
        newTreeData = myInsertNode(newTreeData, parentPath, draggedNode, insertPos);
      } else {
        // Insert as child
        const parentPath = parseInt(targetPath[0]);
        let insertPos = 0;
        if (targetPath.length == 2) insertPos = parseInt(targetPath[1]);
        newTreeData = myInsertNode(newTreeData, parentPath, draggedNode, insertPos);
      }
    }

    setTreeData(newTreeData);
    console.log('Updated treeData:', newTreeData);
  };

  const titleRender = (node: TreeDataNode) => {
    return (
      <span>
        {node.title}
        <Button
          type="text"
          icon={<EditOutlined />}
          size="small"
          onClick={(e) => {
            e.stopPropagation();
            handleLeafClick(node);
          }}
          style={{ marginLeft: 8, fontSize: 12 }}
        />
        {!node.isLeaf && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              addChild(node.key as string);
            }}
            style={{ marginLeft: 8, fontSize: 12 }}
          >
            +
          </button>
        )}
      </span>
    );
  };

  return (
    <div className="main-area">
      <div className="tree-container">
         <div
          className="tree-delete-zone Hidden"
          onDragOver={(e) => e.preventDefault()}
          onDrop={(e) => {
            e.currentTarget.classList.add("Hidden");
            if (draggedKey) {
              const path = findPath(treeData, draggedKey);
              if (path) {
                setTreeData(removeNode(treeData, path));
                setDraggedKey(null);
              }
            }
          }}
        >
          Drop here to delete
        </div>

        <DirectoryTree
          style={{ font: '24px Courier New, monospace' }}
          multiple
          draggable
          defaultExpandAll
          selectedKeys={[]}
          onSelect={onSelect}
          onDragStart={onDragStart}
          onDragEnd={onDragEnd}
          onDrop={onDrop}
          titleRender={titleRender}
          treeData={treeData}
        />
        <div onClick={() => setTreeData(addParent(treeData))}> + add parent</div>
      </div>

      <EditModal
        isModalVisible={isModalVisible}
        selectedLeaf={selectedLeaf}
        name={name}
        description={description}
        onNameChange={setName}
        onDescriptionChange={setDescription}
        onSave={handleSave}
        onCancel={handleCancel}
        onClose={handleClose}
      />
    </div>
  );
};
