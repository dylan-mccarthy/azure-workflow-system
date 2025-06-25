import { describe, it, expect, vi, beforeEach } from 'vitest';
import ApiService from '../services/api';

// Mock axios
vi.mock('axios', () => ({
  default: {
    create: () => ({
      get: vi.fn(),
      put: vi.fn(),
      post: vi.fn(),
      delete: vi.fn(),
      interceptors: {
        request: {
          use: vi.fn(),
        },
      },
    }),
  },
}));

describe('ApiService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should be defined', () => {
    expect(ApiService).toBeDefined();
  });

  it('should have getTickets method', () => {
    expect(typeof ApiService.getTickets).toBe('function');
  });

  it('should have getUsers method', () => {
    expect(typeof ApiService.getUsers).toBe('function');
  });

  it('should have assignTicket method', () => {
    expect(typeof ApiService.assignTicket).toBe('function');
  });

  it('should have updateTicket method', () => {
    expect(typeof ApiService.updateTicket).toBe('function');
  });

  it('should have getEngineers method', () => {
    expect(typeof ApiService.getEngineers).toBe('function');
  });
});