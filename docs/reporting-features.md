# Azure Workflow System - Reporting Features

## Overview

The Azure Workflow System includes comprehensive reporting and analytics capabilities designed to help teams track performance metrics, monitor SLA compliance, and export data for auditing and external analysis.

## Key Metrics

### MTTA (Mean Time to Acknowledgment)
- **Definition**: Average time from ticket creation to first acknowledgment (assignment)
- **Calculation**: `(Sum of all assigned ticket times - creation times) / Number of assigned tickets`
- **Target**: ≤ 10 minutes (as per PRD Section 2)
- **Display**: Human-readable format (e.g., "5m", "1h 30m")

### MTTR (Mean Time to Resolution)
- **Definition**: Average time from ticket creation to resolution
- **Calculation**: `(Sum of all resolved ticket resolution times - creation times) / Number of resolved tickets`
- **Target**: 25% reduction vs. baseline (as per PRD Section 2)
- **Display**: Human-readable format (e.g., "2h 15m", "1d 4h")

### SLA Compliance
- **Definition**: Percentage of tickets resolved within their SLA target dates
- **Calculation**: `(Number of SLA-compliant tickets / Total tickets with SLA) × 100`
- **Target**: ≥ 95% (as per PRD Section 2)
- **Display**: Percentage (e.g., "95%")

## Reports Page Features

### Dashboard View
- **Key Metrics Cards**: Display MTTA, MTTR, SLA compliance, and total tickets
- **Date Range Filtering**: Filter data by custom date ranges
- **Priority/Category Filtering**: Filter by ticket priority and category
- **Real-time Updates**: Metrics update when filters are applied

### Visualizations
- **Ticket Trends Line Chart**: Shows open vs. closed tickets over time
- **Open vs. Closed Pie Chart**: Visual breakdown of current ticket status
- **Interactive Charts**: Built with Recharts library for responsive design

### Data Export
- **CSV Export**: Download ticket data and audit logs in CSV format
- **Filtered Export**: Export respects current filter settings
- **Audit Trail**: All exports include comprehensive ticket and user data

## Role-Based Access Control

### Permissions by Role

| Feature | Viewer | Engineer | Manager | Admin |
|---------|--------|----------|---------|-------|
| View Reports | ❌ | ✅ | ✅ | ✅ |
| View Metrics | ❌ | ✅ | ✅ | ✅ |
| View Charts | ❌ | ✅ | ✅ | ✅ |
| Export Data | ❌ | ❌ | ✅ | ✅ |

### Implementation
- **Frontend**: Navigation item only shown to authorized users
- **Backend**: API endpoints protected with `RequireRole` attribute
- **Access Denied Page**: Graceful handling for unauthorized access attempts

## API Endpoints

### GET /api/reports/metrics
- **Purpose**: Retrieve key performance metrics
- **Authorization**: Engineer, Manager, Admin
- **Parameters**: 
  - `fromDate` (optional): Start date for metrics calculation
  - `toDate` (optional): End date for metrics calculation
  - `priority` (optional): Filter by ticket priority
  - `category` (optional): Filter by ticket category
- **Response**: ReportMetricsDto with MTTA, MTTR, SLA compliance, and ticket counts

### GET /api/reports/trends
- **Purpose**: Get ticket trend data for charting
- **Authorization**: Engineer, Manager, Admin
- **Parameters**:
  - `fromDate` (optional): Start date for trends
  - `toDate` (optional): End date for trends
  - `groupBy` (optional): Grouping interval (default: "day")
- **Response**: Array of TicketTrendDto with daily open/closed counts

### GET /api/reports/export/tickets
- **Purpose**: Export ticket data as CSV
- **Authorization**: Manager, Admin only
- **Parameters**: Same filtering options as metrics endpoint
- **Response**: CSV file download with comprehensive ticket data

### GET /api/reports/export/audit-logs
- **Purpose**: Export audit logs as CSV
- **Authorization**: Manager, Admin only
- **Parameters**:
  - `fromDate` (optional): Start date for audit logs
  - `toDate` (optional): End date for audit logs
  - `ticketId` (optional): Filter by specific ticket
- **Response**: CSV file download with audit trail data

## CSV Export Format

### Tickets Export
```csv
ID,Title,Description,Status,Priority,Category,CreatedBy,AssignedTo,SLA_Target,SLA_Breach,Created,Updated,Resolved,Closed
1,"Database issue","Cannot connect to prod DB",Resolved,High,Incident,"John Doe","Jane Smith","2024-01-01 10:00:00",false,"2024-01-01 09:00:00","2024-01-01 09:30:00","2024-01-01 09:45:00",""
```

### Audit Logs Export
```csv
ID,TicketID,Action,Details,User,OldValues,NewValues,Created
1,1,"Ticket Created","New incident ticket created","John Doe","","","2024-01-01 09:00:00"
2,1,"Ticket Assigned","Assigned to Jane Smith","John Doe","","AssignedToId: 2","2024-01-01 09:15:00"
```

## Usage Examples

### Viewing Reports
1. Navigate to "Reports" in the sidebar (if authorized)
2. Use date range filters to specify time period
3. Apply priority/category filters as needed
4. Click "Apply Filters" to update metrics and charts

### Exporting Data
1. Set desired filters on the Reports page
2. Click "Export Tickets" or "Export Audit Logs" (if authorized)
3. File will download automatically with timestamp in filename

### Interpreting Metrics
- **Low MTTA**: Good responsiveness to new tickets
- **Low MTTR**: Efficient problem resolution
- **High SLA Compliance**: Meeting service level agreements
- **Trend Analysis**: Use charts to identify patterns and peak times

## Technical Implementation

### Backend
- **Entity Framework**: Efficient database queries with proper indexing
- **Authorization Filters**: Role-based access control at API level
- **CSV Generation**: StringBuilder-based CSV creation for performance
- **Error Handling**: Graceful handling of data export failures

### Frontend
- **React Components**: Modular, reusable reporting components
- **Recharts Library**: Professional charting with responsive design
- **TypeScript**: Type-safe API integration and data handling
- **Fluent UI**: Consistent design system integration

### Performance Considerations
- **Database Indexing**: Optimized queries for large datasets
- **Pagination**: Future consideration for very large exports
- **Caching**: Consider implementing for frequently accessed metrics
- **Lazy Loading**: Charts load data asynchronously

## Future Enhancements

### Planned Features
- **Scheduled Reports**: Automated email delivery of key metrics
- **Custom Dashboards**: User-configurable reporting dashboards
- **Advanced Filtering**: Additional filter options (assignee, resource tags)
- **Data Retention**: Configurable data retention policies for exports

### Integration Opportunities
- **Power BI Integration**: Direct connector for advanced analytics
- **Azure Monitor**: Integration with Azure monitoring and alerting
- **External ITSM**: Export compatibility with ServiceNow, Jira, etc.

## Troubleshooting

### Common Issues
- **Access Denied**: Verify user role and permissions
- **No Data**: Check date range and filtering settings
- **Export Failures**: Verify file download permissions and browser settings
- **Performance**: Consider reducing date range for large datasets

### Support
For technical support or feature requests, contact the Azure Platform Support team at support@azureworkflow.com.