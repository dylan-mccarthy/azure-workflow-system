import axios from 'axios';
import {
  TicketDto,
  UserDto,
  AssignTicketDto,
  UpdateTicketDto,
  AttachmentDto,
  TicketStatus,
  TicketPriority,
  TicketCategory,
} from '../types/api';

// API base URL - this would typically come from environment variables
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7000/api';

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add auth token to requests (will be set up with MSAL later)
apiClient.interceptors.request.use((config) => {
  // TODO: Add MSAL token here
  // const token = getAccessToken();
  // if (token) {
  //   config.headers.Authorization = `Bearer ${token}`;
  // }
  return config;
});

// Add error handling interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error);

    // Check if it's a network error (API not available)
    if (error.code === 'ECONNREFUSED' || error.code === 'ERR_NETWORK') {
      console.warn('API server is not available. Running in offline mode.');
      // You could implement offline fallback here
    }

    return Promise.reject(error);
  },
);

export interface GetTicketsParams {
  status?: TicketStatus;
  priority?: TicketPriority;
  category?: TicketCategory;
  assignedToId?: number;
}

export class ApiService {
  // Tickets
  static async getTickets(params?: GetTicketsParams): Promise<TicketDto[]> {
    try {
      const response = await apiClient.get<TicketDto[]>('/tickets', { params });
      return response.data;
    } catch (error) {
      console.error('Failed to fetch tickets:', error);
      // Return empty array as fallback to prevent UI crashes
      return [];
    }
  }

  static async getTicket(id: number): Promise<TicketDto | null> {
    try {
      const response = await apiClient.get<TicketDto>(`/tickets/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch ticket ${id}:`, error);
      return null;
    }
  }

  static async assignTicket(id: number, assignData: AssignTicketDto): Promise<boolean> {
    try {
      await apiClient.put(`/tickets/${id}/assignee`, assignData);
      return true;
    } catch (error) {
      console.error(`Failed to assign ticket ${id}:`, error);
      return false;
    }
  }

  static async updateTicket(id: number, updateData: UpdateTicketDto): Promise<boolean> {
    try {
      await apiClient.put(`/tickets/${id}`, updateData);
      return true;
    } catch (error) {
      console.error(`Failed to update ticket ${id}:`, error);
      return false;
    }
  }

  // Users
  static async getUsers(): Promise<UserDto[]> {
    try {
      const response = await apiClient.get<UserDto[]>('/users');
      return response.data;
    } catch (error) {
      console.error('Failed to fetch users:', error);
      // Return empty array as fallback
      return [];
    }
  }

  static async getUser(id: number): Promise<UserDto | null> {
    try {
      const response = await apiClient.get<UserDto>(`/users/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch user ${id}:`, error);
      return null;
    }
  }

  // Engineers for swim lanes
  static async getEngineers(): Promise<UserDto[]> {
    try {
      const users = await this.getUsers();
      return users.filter((user) => user.role === 2 && user.isActive); // Engineer role = 2
    } catch (error) {
      console.error('Failed to fetch engineers:', error);
      return [];
    }
  }

  // Attachments
  static async getTicketAttachments(ticketId: number): Promise<AttachmentDto[]> {
    try {
      const response = await apiClient.get<AttachmentDto[]>(`/attachments?ticketId=${ticketId}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch attachments for ticket ${ticketId}:`, error);
      return [];
    }
  }

  static async uploadAttachment(ticketId: number, file: File): Promise<AttachmentDto | null> {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await apiClient.post<AttachmentDto>(
        `/attachments/upload?ticketId=${ticketId}`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        },
      );
      return response.data;
    } catch (error) {
      console.error(`Failed to upload attachment for ticket ${ticketId}:`, error);
      return null;
    }
  }

  static async deleteAttachment(attachmentId: number): Promise<boolean> {
    try {
      await apiClient.delete(`/attachments/${attachmentId}`);
      return true;
    } catch (error) {
      console.error(`Failed to delete attachment ${attachmentId}:`, error);
      return false;
    }
  }

  static getAttachmentDownloadUrl(attachmentId: number): string {
    return `${API_BASE_URL}/attachments/${attachmentId}/download`;
  }
}

export default ApiService;
