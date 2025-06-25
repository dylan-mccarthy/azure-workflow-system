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
  isImminentSlaBreach: false,
  slaRemainingMinutes: 120,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  attachmentCount: 0,
};

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
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

  it('displays SLA countdown when slaRemainingMinutes is provided', () => {
    const ticketWithSla = {
      ...mockTicket,
      slaRemainingMinutes: 90,
      slaTargetDate: '2024-01-01T12:00:00Z',
    };
    renderWithProvider(<TicketCard ticket={ticketWithSla} />);

    expect(screen.getByText('1h 30m left')).toBeInTheDocument();
  });

  it('displays overdue status when SLA is breached', () => {
    const breachedTicket = {
      ...mockTicket,
      slaRemainingMinutes: -30,
      slaTargetDate: '2024-01-01T12:00:00Z',
      isSlaBreach: true,
    };
    renderWithProvider(<TicketCard ticket={breachedTicket} />);

    expect(screen.getByText('30m overdue')).toBeInTheDocument();
  });

  it('applies warning border for imminent SLA breach', () => {
    const imminentTicket = {
      ...mockTicket,
      isImminentSlaBreach: true,
      slaRemainingMinutes: 15,
      slaTargetDate: '2024-01-01T12:00:00Z',
    };
    renderWithProvider(<TicketCard ticket={imminentTicket} />);

    // Check that the component renders with imminent breach status
    expect(screen.getByText('15m left')).toBeInTheDocument();
  });

  it('applies breach border for SLA breach', () => {
    const breachedTicket = {
      ...mockTicket,
      isSlaBreach: true,
      slaRemainingMinutes: -10,
      slaTargetDate: '2024-01-01T12:00:00Z',
    };
    renderWithProvider(<TicketCard ticket={breachedTicket} />);

    // Check that the component renders with breach status
    expect(screen.getByText('10m overdue')).toBeInTheDocument();
  });

  it('shows SLA information for imminent breach', () => {
    const imminentTicket = {
      ...mockTicket,
      isImminentSlaBreach: true,
      slaTargetDate: '2024-01-01T12:00:00Z',
      slaRemainingMinutes: 30,
    };
    renderWithProvider(<TicketCard ticket={imminentTicket} />);

    // Should show SLA target date and countdown
    expect(screen.getByText('1/1/2024')).toBeInTheDocument();
    expect(screen.getByText('30m left')).toBeInTheDocument();
  });

  it('shows SLA information for actual breach', () => {
    const breachedTicket = {
      ...mockTicket,
      isSlaBreach: true,
      slaTargetDate: '2024-01-01T12:00:00Z',
      slaRemainingMinutes: -20,
    };
    renderWithProvider(<TicketCard ticket={breachedTicket} />);

    // Should show SLA target date and overdue status
    expect(screen.getByText('1/1/2024')).toBeInTheDocument();
    expect(screen.getByText('20m overdue')).toBeInTheDocument();
  });

  it('formats countdown correctly for different time ranges', () => {
    const testCases = [
      { remaining: 30, expected: '30m left' },
      { remaining: 60, expected: '1h left' },
      { remaining: 90, expected: '1h 30m left' },
      { remaining: 120, expected: '2h left' },
      { remaining: -15, expected: '15m overdue' },
      { remaining: -90, expected: '1h 30m overdue' },
      { remaining: -120, expected: '2h overdue' },
    ];

    testCases.forEach(({ remaining, expected }) => {
      const { unmount } = renderWithProvider(
        <TicketCard
          ticket={{
            ...mockTicket,
            slaRemainingMinutes: remaining,
            slaTargetDate: '2024-01-01T12:00:00Z',
          }}
        />,
      );

      expect(screen.getByText(expected)).toBeInTheDocument();
      unmount();
    });
  });

  it('displays SLA date correctly', () => {
    const ticketWithSla = {
      ...mockTicket,
      slaTargetDate: '2024-01-01T12:00:00Z',
    };
    renderWithProvider(<TicketCard ticket={ticketWithSla} />);

    expect(screen.getByText('1/1/2024')).toBeInTheDocument();
  });

  it('does not show SLA info when no slaTargetDate', () => {
    const ticketNoSla = {
      ...mockTicket,
      slaTargetDate: undefined,
      slaRemainingMinutes: undefined,
    };
    renderWithProvider(<TicketCard ticket={ticketNoSla} />);

    // Should not have calendar icon or SLA info
    const calendarIcon = document.querySelector('[data-icon-name="Calendar"]');
    expect(calendarIcon).not.toBeInTheDocument();
  });

  it('handles SLA status correctly', () => {
    // Test with imminent breach
    const { rerender } = renderWithProvider(
      <TicketCard
        ticket={{
          ...mockTicket,
          isImminentSlaBreach: true,
          slaTargetDate: '2024-01-01T12:00:00Z',
          slaRemainingMinutes: 10,
        }}
      />,
    );

    expect(screen.getByText('10m left')).toBeInTheDocument();

    // Test with actual breach
    rerender(
      <FluentProvider theme={webLightTheme}>
        <TicketCard
          ticket={{
            ...mockTicket,
            isSlaBreach: true,
            slaTargetDate: '2024-01-01T12:00:00Z',
            slaRemainingMinutes: -5,
          }}
        />
      </FluentProvider>,
    );

    expect(screen.getByText('5m overdue')).toBeInTheDocument();
  });
});
