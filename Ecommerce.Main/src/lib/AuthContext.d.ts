import type { AuthUser } from '../api';

export function useAuth(): {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoadingAuth: boolean;
  authError: string | null;
  checkUserAuth: () => Promise<void>;
  logout: (shouldRedirect?: boolean) => Promise<void>;
};
