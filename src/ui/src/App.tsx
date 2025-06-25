import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { UserProvider } from './contexts/UserContext';
import Layout from './components/Layout/Layout';
import KanbanPage from './pages/KanbanPage';
import ReportsPage from './pages/ReportsPage';
import './App.css';

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <UserProvider>
        <Router>
          <Layout>
            <Routes>
              <Route path="/" element={<KanbanPage />} />
              <Route path="/kanban" element={<KanbanPage />} />
              <Route path="/reports" element={<ReportsPage />} />
            </Routes>
          </Layout>
        </Router>
      </UserProvider>
    </FluentProvider>
  );
}

export default App;
