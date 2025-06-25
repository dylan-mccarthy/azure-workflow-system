import React, { useState, useEffect } from 'react';
import {
  makeStyles,
  shorthands,
  tokens,
  Title1,
  Title2,
  Body1,
  Card,
  CardHeader,
  Button,
  Input,
  Dropdown,
  Option,
  Spinner,
  MessageBar,
} from '@fluentui/react-components';
import {
  DocumentArrowDownRegular,
  CalendarRegular,
  ChartMultipleRegular,
  LockClosedRegular,
} from '@fluentui/react-icons';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
} from 'recharts';
import { ApiService } from '../services/api';
import { useUser } from '../contexts/UserContext';
import {
  ReportMetricsDto,
  TicketTrendDto,
  ReportFiltersDto,
  TicketPriority,
  TicketCategory,
  TicketStatus,
  getPriorityLabel,
  getCategoryLabel,
  getStatusLabel,
} from '../types/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '24px',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: '16px',
  },
  filtersCard: {
    ...shorthands.padding('16px'),
  },
  filtersGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: '16px',
    marginBottom: '16px',
  },
  exportButtons: {
    display: 'flex',
    gap: '8px',
  },
  metricsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: '16px',
  },
  metricCard: {
    ...shorthands.padding('16px'),
    textAlign: 'center',
  },
  metricValue: {
    fontSize: '32px',
    fontWeight: 'bold',
    color: tokens.colorBrandForeground1,
  },
  metricLabel: {
    fontSize: '14px',
    color: tokens.colorNeutralForeground2,
  },
  chartContainer: {
    ...shorthands.padding('16px'),
  },
  chartTitle: {
    marginBottom: '16px',
  },
  chartsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))',
    gap: '16px',
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '200px',
  },
  accessDenied: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '400px',
    gap: '16px',
    color: tokens.colorNeutralForeground2,
  },
  accessDeniedIcon: {
    fontSize: '48px',
  },
});

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8'];

const ReportsPage: React.FC = () => {
  const styles = useStyles();
  const { canViewReports, canExportData, currentUser } = useUser();
  const [metrics, setMetrics] = useState<ReportMetricsDto | null>(null);
  const [trends, setTrends] = useState<TicketTrendDto[]>([]);
  const [filters, setFilters] = useState<ReportFiltersDto>({
    fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0],
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check access permissions
  if (!canViewReports()) {
    return (
      <div className={styles.accessDenied}>
        <LockClosedRegular className={styles.accessDeniedIcon} />
        <Title2>Access Denied</Title2>
        <Body1>You don't have permission to view reports. Please contact your administrator.</Body1>
      </div>
    );
  }

  const loadData = async () => {
    setLoading(true);
    setError(null);
    try {
      const [metricsData, trendsData] = await Promise.all([
        ApiService.getReportMetrics(filters),
        ApiService.getReportTrends(filters.fromDate, filters.toDate),
      ]);

      setMetrics(metricsData);
      setTrends(trendsData);
    } catch (err) {
      setError('Failed to load report data. Please try again.');
      console.error('Error loading report data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleFilterChange = (field: keyof ReportFiltersDto, value: any) => {
    setFilters((prev) => ({ ...prev, [field]: value }));
  };

  const handleApplyFilters = () => {
    loadData();
  };

  const handleExport = async (type: 'tickets' | 'audit-logs') => {
    try {
      const blob =
        type === 'tickets'
          ? await ApiService.exportTickets(filters)
          : await ApiService.exportAuditLogs(filters.fromDate, filters.toDate);

      if (blob) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = `${type.replace('-', '_')}_export_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      }
    } catch (err) {
      setError(`Failed to export ${type}. Please try again.`);
      console.error(`Error exporting ${type}:`, err);
    }
  };

  const formatMinutes = (minutes: number): string => {
    if (minutes < 60) return `${Math.round(minutes)}m`;
    const hours = Math.floor(minutes / 60);
    const mins = Math.round(minutes % 60);
    return `${hours}h ${mins}m`;
  };

  const pieChartData = metrics
    ? [
        { name: 'Open', value: metrics.openTickets, color: '#FF8042' },
        { name: 'Closed', value: metrics.closedTickets, color: '#00C49F' },
      ]
    : [];

  const trendChartData = trends.map((trend) => ({
    date: new Date(trend.date).toLocaleDateString(),
    open: trend.openTickets,
    closed: trend.closedTickets,
  }));

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Reports & Analytics</Title1>
        {canExportData() && (
          <div className={styles.exportButtons}>
            <Button
              appearance="secondary"
              icon={<DocumentArrowDownRegular />}
              onClick={() => handleExport('tickets')}
            >
              Export Tickets
            </Button>
            <Button
              appearance="secondary"
              icon={<DocumentArrowDownRegular />}
              onClick={() => handleExport('audit-logs')}
            >
              Export Audit Logs
            </Button>
          </div>
        )}
      </div>

      {error && <MessageBar intent="error">{error}</MessageBar>}

      {/* Filters */}
      <Card className={styles.filtersCard}>
        <CardHeader header={<Title2>Filters</Title2>} />
        <div className={styles.filtersGrid}>
          <div>
            <Body1>From Date</Body1>
            <Input
              type="date"
              value={filters.fromDate || ''}
              onChange={(_, data) => handleFilterChange('fromDate', data.value)}
            />
          </div>
          <div>
            <Body1>To Date</Body1>
            <Input
              type="date"
              value={filters.toDate || ''}
              onChange={(_, data) => handleFilterChange('toDate', data.value)}
            />
          </div>
          <div>
            <Body1>Priority</Body1>
            <Dropdown
              placeholder="All priorities"
              value={filters.priority ? getPriorityLabel(filters.priority) : ''}
              onOptionSelect={(_, data) =>
                handleFilterChange(
                  'priority',
                  data.optionValue ? parseInt(data.optionValue) : undefined,
                )
              }
            >
              <Option value="">All priorities</Option>
              {Object.values(TicketPriority)
                .filter((p) => typeof p === 'number')
                .map((priority) => (
                  <Option key={priority} value={priority.toString()}>
                    {getPriorityLabel(priority as TicketPriority)}
                  </Option>
                ))}
            </Dropdown>
          </div>
          <div>
            <Body1>Category</Body1>
            <Dropdown
              placeholder="All categories"
              value={filters.category ? getCategoryLabel(filters.category) : ''}
              onOptionSelect={(_, data) =>
                handleFilterChange(
                  'category',
                  data.optionValue ? parseInt(data.optionValue) : undefined,
                )
              }
            >
              <Option value="">All categories</Option>
              {Object.values(TicketCategory)
                .filter((c) => typeof c === 'number')
                .map((category) => (
                  <Option key={category} value={category.toString()}>
                    {getCategoryLabel(category as TicketCategory)}
                  </Option>
                ))}
            </Dropdown>
          </div>
        </div>
        <Button appearance="primary" onClick={handleApplyFilters}>
          Apply Filters
        </Button>
      </Card>

      {loading ? (
        <div className={styles.loadingContainer}>
          <Spinner size="large" />
        </div>
      ) : (
        <>
          {/* Key Metrics */}
          {metrics && (
            <div className={styles.metricsGrid}>
              <Card className={styles.metricCard}>
                <div className={styles.metricValue}>{formatMinutes(metrics.mttaMinutes)}</div>
                <div className={styles.metricLabel}>Mean Time to Acknowledgment</div>
              </Card>
              <Card className={styles.metricCard}>
                <div className={styles.metricValue}>{formatMinutes(metrics.mttrMinutes)}</div>
                <div className={styles.metricLabel}>Mean Time to Resolution</div>
              </Card>
              <Card className={styles.metricCard}>
                <div className={styles.metricValue}>
                  {Math.round(metrics.slaCompliancePercentage)}%
                </div>
                <div className={styles.metricLabel}>SLA Compliance</div>
              </Card>
              <Card className={styles.metricCard}>
                <div className={styles.metricValue}>{metrics.totalTickets}</div>
                <div className={styles.metricLabel}>Total Tickets</div>
              </Card>
            </div>
          )}

          {/* Charts */}
          <div className={styles.chartsGrid}>
            {/* Open vs Closed Pie Chart */}
            {metrics && (
              <Card className={styles.chartContainer}>
                <Title2 className={styles.chartTitle}>Open vs Closed Tickets</Title2>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={pieChartData}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ name, value }) => `${name}: ${value}`}
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="value"
                    >
                      {pieChartData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </Card>
            )}

            {/* Trend Line Chart */}
            {trendChartData.length > 0 && (
              <Card className={styles.chartContainer}>
                <Title2 className={styles.chartTitle}>Ticket Trends Over Time</Title2>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={trendChartData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="open" stroke="#FF8042" name="Open" />
                    <Line type="monotone" dataKey="closed" stroke="#00C49F" name="Closed" />
                  </LineChart>
                </ResponsiveContainer>
              </Card>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default ReportsPage;
