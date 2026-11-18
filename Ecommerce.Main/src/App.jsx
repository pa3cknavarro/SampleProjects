import { Toaster } from "@/components/ui/toaster"
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClientInstance } from '@/lib/query-client'
import { BrowserRouter as Router, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import PageNotFound from './lib/PageNotFound';
import { AuthProvider, useAuth } from '@/lib/AuthContext';
// Imports the shared route scroll helper from the project-level components directory.
import ScrollToTop from '../components/ScrollToTop';
import OrderDetail from './pages/OrderDetail';
import Login from './pages/Login';
import Admin from './pages/Admin';
import Register from './pages/Register';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
// Imports the API-backed storefront.
import EcommerceApiStore from './ApiStore';

function RequireAuth({ children, adminOnly = false }) {
  const { user, isAuthenticated, isLoadingAuth } = useAuth();
  const location = useLocation();

  if (isLoadingAuth) return null;
  if (!isAuthenticated) {
    const returnTo = `${location.pathname}${location.search}`;
    return <Navigate to={`/login?returnTo=${encodeURIComponent(returnTo)}`} replace />;
  }
  if (adminOnly && user?.role !== 'Admin') return <Navigate to="/store" replace />;
  return children;
}

const AuthenticatedApp = () => {
  const { isLoadingAuth, authError } = useAuth();

  // Show loading spinner while checking the API session.
  if (isLoadingAuth) {
    return (
      <div className="fixed inset-0 flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-slate-200 border-t-slate-800 rounded-full animate-spin"></div>
      </div>
    );
  }

  // Render the main app
  return (
    <>
      {authError && <p className="p-3 text-center text-sm text-destructive" role="alert">{authError}</p>}
      <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />
      <Route path="/store" element={<RequireAuth><EcommerceApiStore /></RequireAuth>} />
      <Route path="/api-store" element={<Navigate to="/store" replace />} />
      <Route path="/orders/:id" element={<RequireAuth><OrderDetail /></RequireAuth>} />
      <Route path="/admin" element={<RequireAuth adminOnly><Admin /></RequireAuth>} />
      <Route path="/" element={<Navigate to="/store" replace />} />
      <Route path="*" element={<PageNotFound />} />
      </Routes>
    </>
  );
};


function App() {

  return (
    <AuthProvider>
      <QueryClientProvider client={queryClientInstance}>
        <Router>
          <ScrollToTop />
          <AuthenticatedApp />
        </Router>
        <Toaster />
      </QueryClientProvider>
    </AuthProvider>
  )
}

export default App