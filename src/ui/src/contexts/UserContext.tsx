/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { UserDto, UserRole } from '../types/api';

interface UserContextType {
  currentUser: UserDto | null;
  isLoading: boolean;
  error: string | null;
  hasRole: (role: UserRole) => boolean;
  canAssignTickets: () => boolean;
  canViewAllTickets: () => boolean;
}

const UserContext = createContext<UserContextType | undefined>(undefined);

interface UserProviderProps {
  children: ReactNode;
}

export const UserProvider: React.FC<UserProviderProps> = ({ children }) => {
  const [currentUser, setCurrentUser] = useState<UserDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // TODO: In a real app, this would get the current user from MSAL or similar
    // For now, we'll simulate a duty manager user
    const loadCurrentUser = async () => {
      try {
        setIsLoading(true);
        // Simulate API call delay
        await new Promise((resolve) => setTimeout(resolve, 1000));

        // Mock current user - in real app this would come from authentication
        const mockUser: UserDto = {
          id: 999,
          email: 'current.user@company.com',
          firstName: 'Current',
          lastName: 'User',
          role: UserRole.Manager, // Duty Manager
          isActive: true,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        };

        setCurrentUser(mockUser);
      } catch {
        setError('Failed to load user information');
      } finally {
        setIsLoading(false);
      }
    };

    loadCurrentUser();
  }, []);

  const hasRole = (role: UserRole): boolean => {
    return currentUser?.role === role;
  };

  const canAssignTickets = (): boolean => {
    return currentUser?.role === UserRole.Manager || currentUser?.role === UserRole.Admin;
  };

  const canViewAllTickets = (): boolean => {
    return currentUser?.role !== UserRole.Viewer;
  };

  const value: UserContextType = {
    currentUser,
    isLoading,
    error,
    hasRole,
    canAssignTickets,
    canViewAllTickets,
  };

  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
};

export const useUser = (): UserContextType => {
  const context = useContext(UserContext);
  if (context === undefined) {
    throw new Error('useUser must be used within a UserProvider');
  }
  return context;
};
