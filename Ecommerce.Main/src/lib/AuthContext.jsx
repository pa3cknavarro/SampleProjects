import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { EcommerceApiError, ecommerceApi } from '@/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoadingAuth, setIsLoadingAuth] = useState(true);
  const [authError, setAuthError] = useState(null);
  const [authChecked, setAuthChecked] = useState(false);

  const checkUserAuth = useCallback(async () => {
    setIsLoadingAuth(true);
    setAuthError(null);
    try {
      const currentUser = await ecommerceApi.auth.me();
      setUser(currentUser);
      setIsAuthenticated(true);
    } catch (error) {
      setUser(null);
      setIsAuthenticated(false);
      if (!(error instanceof EcommerceApiError) || error.apiError.status !== 401) {
        setAuthError(error instanceof Error ? error.message : 'Unable to verify your session.');
      }
    } finally {
      setIsLoadingAuth(false);
      setAuthChecked(true);
    }
  }, []);

  useEffect(() => {
    void checkUserAuth();
  }, [checkUserAuth]);

  const logout = useCallback(async (shouldRedirect = true) => {
    try {
      await ecommerceApi.auth.logout();
    } finally {
      setUser(null);
      setIsAuthenticated(false);
      if (shouldRedirect) window.location.assign('/login');
    }
  }, []);

  const navigateToLogin = useCallback(() => {
    const returnTo = `${window.location.pathname}${window.location.search}`;
    window.location.assign(`/login?returnTo=${encodeURIComponent(returnTo)}`);
  }, []);

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated,
      isLoadingAuth,
      isLoadingPublicSettings: false,
      authError,
      appPublicSettings: null,
      authChecked,
      logout,
      navigateToLogin,
      checkUserAuth,
      checkAppState: checkUserAuth,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
