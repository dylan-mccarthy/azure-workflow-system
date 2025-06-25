import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import ReportsPage from '../pages/ReportsPage';
import { useUser } from '../contexts/UserContext';
import { UserRole, ReportMetricsDto, TicketTrendDto } from '../types/api';
import ApiService from '../services/api';

// Mock API Service
vi.mock('../services/api', () => ({
  default: {
    getReportMetrics: vi.fn(),
    getReportTrends: vi.fn(),
    exportTickets: vi.fn(),
    exportAuditLogs: vi.fn(),
  },
}));

// Mock the useUser hook
vi.mock('../contexts/UserContext', () => ({
  useUser: vi.fn(),
}));

// Mock Recharts to avoid rendering issues in tests
vi.mock('recharts', () => ({
  LineChart: ({ children }: any) => <div data-testid="line-chart">{children}</div>,
  Line: ({ name }: any) => <div data-testid={`line-${name}`}></div>,
  XAxis: () => <div data-testid="x-axis"></div>,
  YAxis: () => <div data-testid="y-axis"></div>,
  CartesianGrid: () => <div data-testid="cartesian-grid"></div>,
  Tooltip: () => <div data-testid="tooltip"></div>,
  ResponsiveContainer: ({ children }: any) => <div data-testid="responsive-container">{children}</div>,
  PieChart: ({ children }: any) => <div data-testid="pie-chart">{children}</div>,
  Pie: () => <div data-testid="pie"></div>,
  Cell: () => <div data-testid="cell"></div>,
}));

const mockUser = {
  id: 1,
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  role: UserRole.Manager,
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

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

const createMockUserContext = (role: UserRole) => ({
  currentUser: { ...mockUser, role },
  isLoading: false,
  error: null,
  hasRole: (userRole: UserRole) => userRole === role,
  canAssignTickets: () => [UserRole.Manager, UserRole.Admin].includes(role),
  canViewAllTickets: () => role !== UserRole.Viewer,
  canViewReports: () => [UserRole.Engineer, UserRole.Manager, UserRole.Admin].includes(role),
  canExportData: () => [UserRole.Manager, UserRole.Admin].includes(role),
});

const renderWithProviders = (component: React.ReactElement, userRole: UserRole = UserRole.Manager) => {
  const mockUserContext = createMockUserContext(userRole);
  vi.mocked(useUser).mockReturnValue(mockUserContext);
  
  return render(
    <FluentProvider theme={webLightTheme}>
      {component}
    </FluentProvider>
  );
};

describe('ReportsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Setup default mock returns
    vi.mocked(ApiService.getReportMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(ApiService.getReportTrends).mockResolvedValue(mockTrends);
  });

  describe('Access Control', () => {
    it('shows access denied for viewers', () => {
      renderWithProviders(<ReportsPage />, UserRole.Viewer);

      expect(screen.getByText('Access Denied')).toBeInTheDocument();
      expect(screen.getByText("You don't have permission to view reports. Please contact your administrator.")).toBeInTheDocument();
    });

    it('allows engineers to view reports', async () => {
      renderWithProviders(<ReportsPage />, UserRole.Engineer);

      await waitFor(() => {
        expect(screen.getByText('Reports & Analytics')).toBeInTheDocument();
      });
    });

    it('allows managers to view reports and export data', async () => {
      renderWithProviders(<ReportsPage />, UserRole.Manager);

      await waitFor(() => {
        expect(screen.getByText('Reports & Analytics')).toBeInTheDocument();
        expect(screen.getByText('Export Tickets')).toBeInTheDocument();
        expect(screen.getByText('Export Audit Logs')).toBeInTheDocument();
      });
    });

    it('hides export buttons for engineers', async () => {
      renderWithProviders(<ReportsPage />, UserRole.Engineer);

      await waitFor(() => {
        expect(screen.getByText('Reports & Analytics')).toBeInTheDocument();
      });

      expect(screen.queryByText('Export Tickets')).not.toBeInTheDocument();
      expect(screen.queryByText('Export Audit Logs')).not.toBeInTheDocument();
    });
  });

  describe('Data Loading', () => {
    it('displays metrics after successful load', async () => {
      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('2h 0m')).toBeInTheDocument(); // MTTA
        expect(screen.getByText('8h 0m')).toBeInTheDocument(); // MTTR
        expect(screen.getByText('86%')).toBeInTheDocument(); // SLA Compliance (rounded)
        expect(screen.getByText('150')).toBeInTheDocument(); // Total Tickets
      });
    });

    it('shows error message when API call fails', async () => {
      vi.mocked(ApiService.getReportMetrics).mockRejectedValue(new Error('API Error'));

      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('Failed to load report data. Please try again.')).toBeInTheDocument();
      });
    });
  });

  describe('UI Components', () => {
    it('renders filter controls', async () => {
      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('Filters')).toBeInTheDocument();
        expect(screen.getByText('From Date')).toBeInTheDocument();
        expect(screen.getByText('To Date')).toBeInTheDocument();
        expect(screen.getByText('Priority')).toBeInTheDocument();
        expect(screen.getByText('Category')).toBeInTheDocument();
        expect(screen.getByText('Apply Filters')).toBeInTheDocument();
      });
    });

    it('renders charts', async () => {
      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('Open vs Closed Tickets')).toBeInTheDocument();
        expect(screen.getByText('Ticket Trends Over Time')).toBeInTheDocument();
        expect(screen.getByTestId('pie-chart')).toBeInTheDocument();
        expect(screen.getByTestId('line-chart')).toBeInTheDocument();
      });
    });
  });

  describe('Metric Formatting', () => {
    it('formats minutes correctly for MTTA/MTTR', async () => {
      const metricsWithVariousTimes = {
        ...mockMetrics,
        mttaMinutes: 45, // 45 minutes
        mttrMinutes: 90, // 1h 30m
      };
      
      vi.mocked(ApiService.getReportMetrics).mockResolvedValue(metricsWithVariousTimes);

      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('45m')).toBeInTheDocument(); // MTTA
        expect(screen.getByText('1h 30m')).toBeInTheDocument(); // MTTR
      });
    });

    it('rounds SLA compliance percentage', async () => {
      const metricsWithDecimalSla = {
        ...mockMetrics,
        slaCompliancePercentage: 87.67,
      };
      
      vi.mocked(ApiService.getReportMetrics).mockResolvedValue(metricsWithDecimalSla);

      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(screen.getByText('88%')).toBeInTheDocument(); // Rounded SLA
      });
    });
  });

  describe('API Integration', () => {
    it('calls API methods on component mount', async () => {
      renderWithProviders(<ReportsPage />);

      await waitFor(() => {
        expect(ApiService.getReportMetrics).toHaveBeenCalledTimes(1);
        expect(ApiService.getReportTrends).toHaveBeenCalledTimes(1);
      });
    });

    it('export functions are available for managers', async () => {
      renderWithProviders(<ReportsPage />, UserRole.Manager);

      await waitFor(() => {
        expect(screen.getByText('Export Tickets')).toBeInTheDocument();
        expect(screen.getByText('Export Audit Logs')).toBeInTheDocument();
      });

      // Verify the export functions exist
      expect(typeof ApiService.exportTickets).toBe('function');
      expect(typeof ApiService.exportAuditLogs).toBe('function');
    });
  });
});