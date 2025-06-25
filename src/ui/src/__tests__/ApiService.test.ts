import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ReportMetricsDto, TicketTrendDto, ReportFiltersDto, TicketPriority, TicketCategory } from '../types/api';

// Create mock functions first
const mockGet = vi.fn();
const mockPut = vi.fn();
const mockPost = vi.fn();
const mockDelete = vi.fn();

// Mock axios
vi.mock('axios', () => ({
  default: {
    create: () => ({
      get: mockGet,
      put: mockPut,
      post: mockPost,
      delete: mockDelete,
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

// Import ApiService after mocking axios
const { default: ApiService } = await import('../services/api');

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

  describe('Reporting Methods', () => {
    it('should have getReportMetrics method', () => {
      expect(typeof ApiService.getReportMetrics).toBe('function');
    });

    it('should have getReportTrends method', () => {
      expect(typeof ApiService.getReportTrends).toBe('function');
    });

    it('should have exportTickets method', () => {
      expect(typeof ApiService.exportTickets).toBe('function');
    });

    it('should have exportAuditLogs method', () => {
      expect(typeof ApiService.exportAuditLogs).toBe('function');
    });

    it('should call getReportMetrics with correct parameters', async () => {
      const mockMetrics: ReportMetricsDto = {
        mttaMinutes: 120,
        mttrMinutes: 480,
        slaCompliancePercentage: 85.5,
        totalTickets: 150,
        openTickets: 45,
        closedTickets: 105,
        fromDate: new Date('2024-01-01'),
        toDate: new Date('2024-01-31'),
      };

      mockGet.mockResolvedValue({ data: mockMetrics });

      const filters: ReportFiltersDto = {
        fromDate: '2024-01-01',
        toDate: '2024-01-31',
        priority: TicketPriority.High,
        category: TicketCategory.Incident,
      };

      const result = await ApiService.getReportMetrics(filters);

      expect(mockGet).toHaveBeenCalledWith('/reports/metrics?fromDate=2024-01-01&toDate=2024-01-31&priority=3&category=1');
      expect(result).toEqual(mockMetrics);
    });

    it('should call getReportTrends with correct parameters', async () => {
      const mockTrends: TicketTrendDto[] = [
        {
          date: new Date('2024-01-01'),
          openTickets: 10,
          closedTickets: 5,
        },
        {
          date: new Date('2024-01-02'),
          openTickets: 8,
          closedTickets: 7,
        },
      ];

      mockGet.mockResolvedValue({ data: mockTrends });

      const result = await ApiService.getReportTrends('2024-01-01', '2024-01-31');

      expect(mockGet).toHaveBeenCalledWith('/reports/trends?fromDate=2024-01-01&toDate=2024-01-31&groupBy=day');
      expect(result).toEqual(mockTrends);
    });

    it('should call exportTickets with correct parameters', async () => {
      const mockBlob = new Blob(['csv,data'], { type: 'text/csv' });
      mockGet.mockResolvedValue({ data: mockBlob });

      const filters: ReportFiltersDto = {
        fromDate: '2024-01-01',
        toDate: '2024-01-31',
        priority: TicketPriority.Medium,
      };

      const result = await ApiService.exportTickets(filters);

      expect(mockGet).toHaveBeenCalledWith('/reports/export/tickets?fromDate=2024-01-01&toDate=2024-01-31&priority=2', {
        responseType: 'blob',
      });
      expect(result).toEqual(mockBlob);
    });

    it('should call exportAuditLogs with correct parameters', async () => {
      const mockBlob = new Blob(['audit,log,data'], { type: 'text/csv' });
      mockGet.mockResolvedValue({ data: mockBlob });

      const result = await ApiService.exportAuditLogs('2024-01-01', '2024-01-31', 123);

      expect(mockGet).toHaveBeenCalledWith('/reports/export/audit-logs?fromDate=2024-01-01&toDate=2024-01-31&ticketId=123', {
        responseType: 'blob',
      });
      expect(result).toEqual(mockBlob);
    });

    it('should handle API errors gracefully in getReportMetrics', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockGet.mockRejectedValue(new Error('Network error'));

      const result = await ApiService.getReportMetrics();

      expect(result).toBeNull();
      expect(consoleSpy).toHaveBeenCalledWith('Failed to fetch report metrics:', expect.any(Error));
      
      consoleSpy.mockRestore();
    });

    it('should handle API errors gracefully in getReportTrends', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockGet.mockRejectedValue(new Error('Network error'));

      const result = await ApiService.getReportTrends('2024-01-01', '2024-01-31');

      expect(result).toEqual([]);
      expect(consoleSpy).toHaveBeenCalledWith('Failed to fetch report trends:', expect.any(Error));
      
      consoleSpy.mockRestore();
    });

    it('should handle API errors gracefully in exportTickets', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockGet.mockRejectedValue(new Error('Export failed'));

      const result = await ApiService.exportTickets();

      expect(result).toBeNull();
      expect(consoleSpy).toHaveBeenCalledWith('Failed to export tickets:', expect.any(Error));
      
      consoleSpy.mockRestore();
    });

    it('should handle API errors gracefully in exportAuditLogs', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      mockGet.mockRejectedValue(new Error('Export failed'));

      const result = await ApiService.exportAuditLogs('2024-01-01', '2024-01-31');

      expect(result).toBeNull();
      expect(consoleSpy).toHaveBeenCalledWith('Failed to export audit logs:', expect.any(Error));
      
      consoleSpy.mockRestore();
    });

    it('should build query parameters correctly with undefined values', async () => {
      mockGet.mockResolvedValue({ data: {} });

      const filters: ReportFiltersDto = {
        fromDate: '2024-01-01',
        // toDate, priority, category are undefined
      };

      await ApiService.getReportMetrics(filters);

      expect(mockGet).toHaveBeenCalledWith('/reports/metrics?fromDate=2024-01-01');
    });
  });
});
