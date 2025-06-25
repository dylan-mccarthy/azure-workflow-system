import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import Layout from './components/Layout/Layout';
import KanbanPage from './pages/KanbanPage';
import './App.css'

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <Router>
        <Layout>
          <Routes>
            <Route path="/" element={<KanbanPage />} />
            <Route path="/kanban" element={<KanbanPage />} />
          </Routes>
        </Layout>
      </Router>
    </FluentProvider>
  );
}

export default App;
