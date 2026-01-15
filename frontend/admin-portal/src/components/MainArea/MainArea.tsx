import { Tree, Button } from 'antd';
import type { GetProps } from 'antd';
import { useState, useEffect } from 'react';
import { EditOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { EditModal } from './EditModal';
import { useShelfs } from '@/hooks/shelfs/useShelfs';
import './MainArea.css';
import { type UpdateShelfDto } from '@/api/shelfs/shelfsApi';
import { useUpdateShelf } from '@/hooks/shelfs/useUpdateShelf';

type DirectoryTreeProps = GetProps<typeof Tree.DirectoryTree>;

type TreeDataNode = {
  title: string;
  key: string;
  children?: TreeDataNode[];
  isLeaf?: boolean;
};


const { DirectoryTree } = Tree;

export const MainArea = () => {
  const navigate = useNavigate();
  const [treeData, setTreeData] = useState<TreeDataNode[]>([]);
  const { data: shelfs, isLoading, error } = useShelfs();
  const updateShelf = useUpdateShelf();
  
  console.log(shelfs);
  
  useEffect(() => {
    if (shelfs) {
      const transformedData: TreeDataNode[] = shelfs.map(shelf => ({
        title: shelf.name,
        key: shelf.id,
        children: shelf.items.map(item => ({
          title: item.name,
          key: item.id,
          isLeaf: true,
        })),
      }));
      setTreeData(transformedData);
    }
  }, [shelfs]);
  const [draggedKey, setDraggedKey] = useState<string | null>(null);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [selectedLeaf, setSelectedLeaf] = useState<TreeDataNode | null>(null);
  const [name, setShelfName] = useState('');

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
    // Find the item name from shelfs data
    
    if (shelfs) {
      let shelf = shelfs.find(sh => sh.id === key);
        if (shelf) {
          return { name: shelf.name };
        }

      }
    return { name: '' };
  };

  const handleLeafClick = async (node: TreeDataNode) => {
    setSelectedLeaf(node);
    setIsModalVisible(true);
    const data = await fetchData(node.key);
    setShelfName(data.name);
  };

  const handleSave = async () => {
      //setLoading(true);
      const shelfId = selectedLeaf?.key;
      try {
        if(shelfId == null || shelfId=="")
          {
            console.log("error getting id of shelf");
            return 
          }
        const payload: UpdateShelfDto = {
          id: shelfId,
          name:name,
        };
        console.log('Saving:',payload);


        updateShelf.mutate(payload, {
          onSuccess: () => {
            console.log('Shelf updated successfully');
            setIsModalVisible(false);
          },
          onError: () => {
            console.log('Failed to update Shelf', {shelfId});
          }
        });
      } catch (error) {
        console.log('Failed to update Shelf', {shelfId});
      } finally {
        //setLoading(false);
      }
  };

  const handleCancel = async () => {
    if (selectedLeaf) {
      const data = await fetchData(selectedLeaf.key);
      setShelfName(data.name);
    }
  };

  const handleClose = () => {
    setIsModalVisible(false);
  };

  const onSelect: DirectoryTreeProps['onSelect'] = (keys, info) => {
    console.log('onClick', keys, info);
    if (info.node.isLeaf) {
      navigate(`/item-edit/${info.node.key}`);
    }
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
    console.log("da2");
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

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error loading shelfs</div>;

  const titleRender = (node: TreeDataNode) => {
    return (
      <span>
        {node.title}
        {!node.isLeaf && (
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
        )}
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
        onNameChange={setShelfName}
        onSave={handleSave}
        onCancel={handleCancel}
        onClose={handleClose}
      />
    </div>
  );
};
