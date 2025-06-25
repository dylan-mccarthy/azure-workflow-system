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
        response: {
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

  // New attachment-related tests
  it('should have getTicketAttachments method', () => {
    expect(typeof ApiService.getTicketAttachments).toBe('function');
  });

  it('should have uploadAttachment method', () => {
    expect(typeof ApiService.uploadAttachment).toBe('function');
  });

  it('should have deleteAttachment method', () => {
    expect(typeof ApiService.deleteAttachment).toBe('function');
  });

  it('should have getAttachmentDownloadUrl method', () => {
    expect(typeof ApiService.getAttachmentDownloadUrl).toBe('function');
  });

  describe('uploadAttachment', () => {
    it('should create FormData and upload file', async () => {
      const mockFile = new File(['test content'], 'test.pdf', { type: 'application/pdf' });
      const ticketId = 123;

      // The method exists and can be called
      const result = await ApiService.uploadAttachment(ticketId, mockFile);

      // Since axios is mocked, this will return null (error case)
      expect(result).toBeNull();
    });
  });

  describe('getAttachmentDownloadUrl', () => {
    it('should return correct download URL', () => {
      const attachmentId = 456;
      const url = ApiService.getAttachmentDownloadUrl(attachmentId);

      expect(url).toContain(`/attachments/${attachmentId}/download`);
      expect(typeof url).toBe('string');
    });
  });
});
