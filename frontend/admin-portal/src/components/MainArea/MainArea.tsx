import { Tree } from 'antd';
import type { GetProps } from 'antd';

type DirectoryTreeProps = GetProps<typeof Tree.DirectoryTree>;

const { DirectoryTree } = Tree;
import './MainArea.css';

const treeData = [
  {
    title: 'parent 0',
    key: '0-0',
    children: [
      { title: 'leaf 0-0', key: '0-0-0', isLeaf: true },
      { title: 'leaf 0-1', key: '0-0-1', isLeaf: true },
    ],
  },
  {
    title: 'parent 1',
    key: '0-1',
    children: [
      { title: 'leaf 1-0', key: '0-1-0', isLeaf: true },
      { title: 'leaf 1-1', key: '0-1-1', isLeaf: true },
    ],
  },
];
export const MainArea = () => {
  const onSelect: DirectoryTreeProps['onSelect'] = (keys, info) => {
    console.log('Trigger Select', keys, info);
  };

  const onDrop: DirectoryTreeProps['onDrop'] = (info) => {
    console.log('onDrop', info);
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
