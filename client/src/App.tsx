// קומפוננטת שורש — SmartHome
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import Layout from './components/Layout';
import LoginPage from './pages/Auth/LoginPage';
import DashboardPage from './pages/Dashboard/DashboardPage';
import TasksPage from './pages/Tasks/TasksPage';
import ShoppingPage from './pages/Shopping/ShoppingPage';
import BudgetPage from './pages/Budget/BudgetPage';
import CalendarPage from './pages/Calendar/CalendarPage';
import GiftsPage from './pages/Gifts/GiftsPage';
import InventoryPage from './pages/Inventory/InventoryPage';
import MedicalPage from './pages/Medical/MedicalPage';
import VehiclePage from './pages/Vehicle/VehiclePage';
import LeaderboardPage from './pages/Gamification/LeaderboardPage';
import AdminPage from './pages/Admin/AdminPage';

// הגנה על routes שדורשים כניסה
function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { token, user } = useAuthStore();
  if (!token || !user) return <Navigate to="/login" replace />;
  if (!user.approved)   return <Navigate to="/login" replace />;
  return <Layout>{children}</Layout>;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route path="/dashboard"   element={<PrivateRoute><DashboardPage /></PrivateRoute>} />
        <Route path="/tasks"       element={<PrivateRoute><TasksPage /></PrivateRoute>} />
        <Route path="/shopping"    element={<PrivateRoute><ShoppingPage /></PrivateRoute>} />
        <Route path="/budget"      element={<PrivateRoute><BudgetPage /></PrivateRoute>} />
        <Route path="/calendar"    element={<PrivateRoute><CalendarPage /></PrivateRoute>} />
        <Route path="/gifts"       element={<PrivateRoute><GiftsPage /></PrivateRoute>} />
        <Route path="/inventory"   element={<PrivateRoute><InventoryPage /></PrivateRoute>} />
        <Route path="/medical"     element={<PrivateRoute><MedicalPage /></PrivateRoute>} />
        <Route path="/vehicle"     element={<PrivateRoute><VehiclePage /></PrivateRoute>} />
        <Route path="/leaderboard" element={<PrivateRoute><LeaderboardPage /></PrivateRoute>} />
        <Route path="/admin"       element={<PrivateRoute><AdminPage /></PrivateRoute>} />

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
