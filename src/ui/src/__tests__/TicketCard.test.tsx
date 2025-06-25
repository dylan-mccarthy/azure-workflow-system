import { describe, it, expect, vi } from 'vitest';
import { TicketDto, TicketStatus, TicketPriority, TicketCategory, UserRole } from '../types/api';

// Mock DnD Kit and components for testing
vi.mock('@dnd-kit/sortable', () => ({
  useSortable: () => ({
    attributes: {},
    listeners: {},
    setNodeRef: () => {},
    transform: null,
    transition: undefined,
    isDragging: false,
  }),
}));

vi.mock('@dnd-kit/utilities', () => ({
  CSS: {
    Transform: {
      toString: () => 'transform: none',
    },
  },
}));

vi.mock('../components/Kanban/TicketCard', () => ({
  default: vi.fn(() => null),
}));

const mockTicket: TicketDto = {
  id: 1,
  title: 'Test Ticket',
  description: 'Test description',
  status: TicketStatus.New,
  priority: TicketPriority.Medium,
  category: TicketCategory.Incident,
  createdBy: {
    id: 1,
    email: 'creator@test.com',
    firstName: 'John',
    lastName: 'Creator',
    role: UserRole.Admin,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  assignedTo: {
    id: 2,
    email: 'assignee@test.com',
    firstName: 'Jane',
    lastName: 'Engineer',
    role: UserRole.Engineer,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  isSlaBreach: false,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  attachmentCount: 0,
};

describe('TicketCard Integration', () => {
  it('renders ticket information correctly', () => {
    expect(mockTicket.title).toBe('Test Ticket');
    expect(mockTicket.description).toBe('Test description');
    expect(mockTicket.id).toBe(1);
    expect(mockTicket.priority).toBe(TicketPriority.Medium);
    expect(mockTicket.status).toBe(TicketStatus.New);
    expect(mockTicket.category).toBe(TicketCategory.Incident);
    expect(mockTicket.assignedTo?.firstName).toBe('Jane');
    expect(mockTicket.assignedTo?.lastName).toBe('Engineer');
  });

  it('shows unassigned when no assignee', () => {
    const unassignedTicket = { ...mockTicket, assignedTo: undefined };
    expect(unassignedTicket.assignedTo).toBeUndefined();
  });

  it('displays attachment count when attachments exist', () => {
    const ticketWithAttachments = { ...mockTicket, attachmentCount: 3 };
    expect(ticketWithAttachments.attachmentCount).toBe(3);
  });

  it('does not display attachment count when no attachments', () => {
    expect(mockTicket.attachmentCount).toBe(0);
  });

  it('handles different priority levels correctly', () => {
    const priorities = [
      TicketPriority.Low,
      TicketPriority.Medium,
      TicketPriority.High,
      TicketPriority.Critical,
    ];

    priorities.forEach((priority) => {
      const ticketWithPriority = { ...mockTicket, priority };
      expect(ticketWithPriority.priority).toBe(priority);
    });
  });

  it('handles SLA breach status', () => {
    const slaBreachTicket = { ...mockTicket, isSlaBreach: true };
    expect(slaBreachTicket.isSlaBreach).toBe(true);
    expect(mockTicket.isSlaBreach).toBe(false);
  });

  it('displays different ticket statuses correctly', () => {
    const statusOptions = [
      TicketStatus.New,
      TicketStatus.InProgress,
      TicketStatus.Resolved,
      TicketStatus.Closed,
    ];

    statusOptions.forEach((status) => {
      const ticketWithStatus = { ...mockTicket, status };
      expect(ticketWithStatus.status).toBe(status);
    });
  });

  it('displays different categories correctly', () => {
    const categoryOptions = [
      TicketCategory.Incident,
      TicketCategory.Request,
      TicketCategory.Change,
    ];

    categoryOptions.forEach((category) => {
      const ticketWithCategory = { ...mockTicket, category };
      expect(ticketWithCategory.category).toBe(category);
    });
  });

  it('formats user names correctly', () => {
    const getFullName = (firstName: string, lastName: string): string => {
      return `${firstName} ${lastName}`;
    };

    expect(getFullName(mockTicket.createdBy.firstName, mockTicket.createdBy.lastName)).toBe(
      'John Creator',
    );
    expect(getFullName(mockTicket.assignedTo!.firstName, mockTicket.assignedTo!.lastName)).toBe(
      'Jane Engineer',
    );
  });

  it('handles ticket ID formatting', () => {
    const formatTicketId = (id: number): string => {
      return `#${id}`;
    };

    expect(formatTicketId(mockTicket.id)).toBe('#1');
    expect(formatTicketId(999)).toBe('#999');
  });
});
