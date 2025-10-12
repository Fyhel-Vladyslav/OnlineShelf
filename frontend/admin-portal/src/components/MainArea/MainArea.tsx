import { Tree } from 'antd';
import type { GetProps } from 'antd';
import { useState } from 'react';

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
];

export const MainArea = () => {
  const [treeData, setTreeData] = useState<TreeDataNode[]>(initialTreeData);

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

  const onSelect: DirectoryTreeProps['onSelect'] = (keys, info) => {
    console.log('Trigger Select', keys, info);
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
    if (dropToGap) {
      // Insert as sibling
  
      const parentPath = parseInt(targetPath[0]);
      const siblingIndex = parseInt(targetPath[targetPath.length - 1]);
      const insertPos = siblingIndex + (dropPosition > 0 ? 1 : dropPosition < 0 ? 0 : 0);
      newTreeData = myInsertNode(newTreeData, parentPath, draggedNode, insertPos) 
    } else {
      // Insert as child
      const parentPath =  parseInt(targetPath[0]);
      let insertPos=0;
      if(targetPath.length==2)
        insertPos = parseInt(targetPath[1])

      newTreeData = myInsertNode(newTreeData, parentPath, draggedNode, insertPos) 
    }

    // let newTreeData = removeNode(treeData, dragPath);
    // if (dropToGap) {
    //   // Insert as sibling
    //   const targetPath = findPath(newTreeData, targetKey);
    //   if (!targetPath) return;
    //   const parentPath = targetPath.slice(0, -1);
    //   console.log("parentPath ", parentPath)
    //   console.log("targetPath ", targetPath)
    //   const siblingIndex = parseInt(targetPath[targetPath.length - 1]);
    //   console.log("siblingIndex ", siblingIndex)
    //   const insertPos = siblingIndex + (dropPosition > 0 ? 1 : dropPosition < 0 ? 0 : 0);
    //   newTreeData = insertNode(newTreeData, parentPath, draggedNode, insertPos - siblingIndex);
    // } else {
    //   // Insert as child
    //   const targetPath = findPath(newTreeData, targetKey);
    //   if (!targetPath) return;
    //   const childPath = [...targetPath, '0'];
    //   newTreeData = insertNode(newTreeData, childPath, draggedNode, 0);
    // }

    setTreeData(newTreeData);
    console.log('Updated treeData:', newTreeData);
  };


  return (
    <div className="main-area">
    
    <div className="tree-container">
        <DirectoryTree
        style={{ font: '24px Courier New, monospace' }}
      multiple
      draggable
      defaultExpandAll
      onSelect={onSelect}
      onDrop={onDrop}
      treeData={treeData}
    />
  </div>
  </div>
  );
};
