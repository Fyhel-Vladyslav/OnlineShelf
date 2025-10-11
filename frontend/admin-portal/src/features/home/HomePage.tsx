import Header from '../../components/Header/Header';
import LeftSidebar from '../../components/LeftSidebar/LeftSidebar';
import { MainArea } from '../../components/MainArea/MainArea';
import './HomePage.css';

const HomePage: React.FC = () => {
  //const [rightSidebarOpen, setRightSidebarOpen] = useState(false);

  return (
    <div className="home-page">
      <Header />
      <div className="content-area">
        <div className="left-sidebar"><LeftSidebar /></div>
        <MainArea /> 
           
        {/* <div className={`right-sidebar ${rightSidebarOpen ? 'open' : 'closed'}`}>
          <RightSidebar />
        </div>
        <button className="right-toggle-btn" onClick={() => setRightSidebarOpen(!rightSidebarOpen)}>
          {rightSidebarOpen ? '▲' : '▼'}
        </button> */}
      </div>
    </div>
  );
};

export default HomePage;
