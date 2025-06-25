import axios from 'axios';
import { TicketDto, UserDto, AssignTicketDto, UpdateTicketDto, TicketStatus, TicketPriority, TicketCategory } from '../types/api';

// API base URL - this would typically come from environment variables
const API_BASE_URL = process.env.VITE_API_BASE_URL || 'https://localhost:7000/api';

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

export interface GetTicketsParams {
  status?: TicketStatus;
  priority?: TicketPriority;
  category?: TicketCategory;
  assignedToId?: number;
}

export class ApiService {
  // Tickets
  static async getTickets(params?: GetTicketsParams): Promise<TicketDto[]> {
    const response = await apiClient.get<TicketDto[]>('/tickets', { params });
    return response.data;
  }

  static async getTicket(id: number): Promise<TicketDto> {
    const response = await apiClient.get<TicketDto>(`/tickets/${id}`);
    return response.data;
  }

  static async assignTicket(id: number, assignData: AssignTicketDto): Promise<void> {
    await apiClient.put(`/tickets/${id}/assignee`, assignData);
  }

  static async updateTicket(id: number, updateData: UpdateTicketDto): Promise<void> {
    await apiClient.put(`/tickets/${id}`, updateData);
  }

  // Users
  static async getUsers(): Promise<UserDto[]> {
    const response = await apiClient.get<UserDto[]>('/users');
    return response.data;
  }

  static async getUser(id: number): Promise<UserDto> {
    const response = await apiClient.get<UserDto>(`/users/${id}`);
    return response.data;
  }

  // Engineers for swim lanes
  static async getEngineers(): Promise<UserDto[]> {
    const users = await this.getUsers();
    return users.filter(user => user.role === 2 && user.isActive); // Engineer role = 2
  }
}

export default ApiService;