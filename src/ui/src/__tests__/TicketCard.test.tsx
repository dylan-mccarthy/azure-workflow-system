import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import TicketCard from '../components/Kanban/TicketCard';
import { TicketDto, TicketStatus, TicketPriority, TicketCategory, UserRole } from '../types/api';

// Mock DnD Kit
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

const renderWithProvider = (component: React.ReactElement) => {
  return render(
    <FluentProvider theme={webLightTheme}>
      {component}
    </FluentProvider>
  );
};

describe('TicketCard', () => {
  it('renders ticket information correctly', () => {
    renderWithProvider(<TicketCard ticket={mockTicket} />);
    
    expect(screen.getByText('Test Ticket')).toBeInTheDocument();
    expect(screen.getByText('Test description')).toBeInTheDocument();
    expect(screen.getByText('#1')).toBeInTheDocument();
    expect(screen.getByText('Medium')).toBeInTheDocument();
    expect(screen.getByText('New')).toBeInTheDocument();
    expect(screen.getByText('Incident')).toBeInTheDocument();
    expect(screen.getByText('Jane Engineer')).toBeInTheDocument();
  });

  it('shows unassigned when no assignee', () => {
    const unassignedTicket = { ...mockTicket, assignedTo: undefined };
    renderWithProvider(<TicketCard ticket={unassignedTicket} />);
    
    expect(screen.getByText('Unassigned')).toBeInTheDocument();
  });

  it('applies priority styling correctly', () => {
    renderWithProvider(<TicketCard ticket={mockTicket} />);
    
    const priorityBadge = screen.getByText('Medium');
    expect(priorityBadge).toBeInTheDocument();
  });
});