import './HomePage.css';
import wardrobe from '@/assets/images/wardrobe.png';
import leftDoor from '@/assets/images/left_door.jpg';
import rightDoor from '@/assets/images/right_door.jpg';
import { useNavigate } from 'react-router-dom';

const HomePage = () => {
  const navigate = useNavigate();

  // Посилання на фонові картинки
  const images = {
    wardrobe_back: wardrobe,
    wardrobe_left_door: leftDoor,
    wardrobe_right_door: rightDoor
  };

  return (
    <div className="container">
      {/* Ліва частина: text1 та text2 */}
      <div className="left-column">
        <div className="parent-card left-card">
          <span className="card-title">MAKE MY OWN STYLE</span>
        </div>
        <div className="parent-card left-card">
          <span className="card-title">SELECT TODAYS OPTION</span>
        </div>
      </div>

      {/* Права частина: text3 + кольорові зони */}
      <div className="right-column">
        <div className="parent-card right-card"  onClick={() => navigate("/shelfs")}>
          <span className="card-title">WARDROBE</span>
          
          <div 
            className="wardrobe-zone" 
            style={{ 
              backgroundImage: `url(${images.wardrobe_back})` 
            }}
          >
            {/* ліві двері окремо */}
            <div 
              className="left-door" 
              style={{ 
                backgroundImage: `url(${images.wardrobe_left_door})` 
              }}
              >
            </div>
              {/* праві двері окремо, статично  */}
            <div 
              className="right-door" 
              style={{ 
                backgroundImage: `url(${images.wardrobe_right_door})` 
              }}>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default HomePage;