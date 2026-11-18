import { StarOutlined, StarFilled } from '@ant-design/icons';

interface FavouriteButtonProps {
  isFavorite: boolean;
  onToggle: () => void;
  style?: React.CSSProperties;
}

export const FavouriteButton: React.FC<FavouriteButtonProps> = ({ isFavorite, onToggle, style }) => {
  return (
    <div 
      onClick={(e) => {
        e.stopPropagation(); // 👈 This prevents the dropzone from being clicked
        e.preventDefault();
        onToggle();
      }} 
      style={{ cursor: 'pointer', fontSize: '24px', ...style }}
    >
      {isFavorite ? <StarFilled style={{ color: '#fadb14' }} /> : <StarOutlined />}
    </div>
  );
};
