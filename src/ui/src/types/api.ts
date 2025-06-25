// API Types matching the backend DTOs

export enum TicketStatus {
  New = 1,
  Triaged = 2,
  Assigned = 3,
  InProgress = 4,
  Resolved = 5,
  Closed = 6,
}

export enum TicketPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4,
  Emergency = 5,
}

export enum TicketCategory {
  Incident = 1,
  Access = 2,
  NewResource = 3,
  Change = 4,
  Alert = 5,
}

export enum UserRole {
  Viewer = 1,
  Engineer = 2,
  Manager = 3,
  Admin = 4,
}

export interface UserDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface TicketDto {
  id: number;
  title: string;
  description?: string;
  status: TicketStatus;
  priority: TicketPriority;
  category: TicketCategory;
  azureResourceId?: string;
  alertId?: string;
  createdBy: UserDto;
  assignedTo?: UserDto;
  slaTargetDate?: string;
  isSlaBreach: boolean;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  closedAt?: string;
  attachmentCount: number;
}

export interface AssignTicketDto {
  assignedToId?: number;
}

export interface UpdateTicketDto {
  title?: string;
  description?: string;
  status?: TicketStatus;
  priority?: TicketPriority;
  category?: TicketCategory;
  azureResourceId?: string;
  alertId?: string;
}

export interface AttachmentDto {
  id: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  blobUrl: string;
  ticketId: number;
  createdAt: string;
  uploadedBy: UserDto;
}

export interface CreateAttachmentDto {
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  blobUrl: string;
}

// Helper functions for display
export const getStatusLabel = (status: TicketStatus): string => {
  switch (status) {
    case TicketStatus.New:
      return 'New';
    case TicketStatus.Triaged:
      return 'Triaged';
    case TicketStatus.Assigned:
      return 'Assigned';
    case TicketStatus.InProgress:
      return 'In Progress';
    case TicketStatus.Resolved:
      return 'Resolved';
    case TicketStatus.Closed:
      return 'Closed';
    default:
      return 'Unknown';
  }
};

export const getPriorityLabel = (priority: TicketPriority): string => {
  switch (priority) {
    case TicketPriority.Low:
      return 'Low';
    case TicketPriority.Medium:
      return 'Medium';
    case TicketPriority.High:
      return 'High';
    case TicketPriority.Critical:
      return 'Critical';
    case TicketPriority.Emergency:
      return 'Emergency';
    default:
      return 'Unknown';
  }
};

export const getCategoryLabel = (category: TicketCategory): string => {
  switch (category) {
    case TicketCategory.Incident:
      return 'Incident';
    case TicketCategory.Access:
      return 'Access';
    case TicketCategory.NewResource:
      return 'New Resource';
    case TicketCategory.Change:
      return 'Change';
    case TicketCategory.Alert:
      return 'Alert';
    default:
      return 'Unknown';
  }
};

export const getRoleLabel = (role: UserRole): string => {
  switch (role) {
    case UserRole.Viewer:
      return 'Viewer';
    case UserRole.Engineer:
      return 'Engineer';
    case UserRole.Manager:
      return 'Manager';
    case UserRole.Admin:
      return 'Admin';
    default:
      return 'Unknown';
  }
};

// Reporting Types
export interface ReportMetricsDto {
  mttaMinutes: number;
  mttrMinutes: number;
  slaCompliancePercentage: number;
  totalTickets: number;
  openTickets: number;
  closedTickets: number;
  fromDate: string;
  toDate: string;
}

export interface TicketTrendDto {
  date: string;
  openTickets: number;
  closedTickets: number;
}

export interface ReportFiltersDto {
  fromDate?: string;
  toDate?: string;
  priority?: TicketPriority;
  category?: TicketCategory;
  status?: TicketStatus;
}
