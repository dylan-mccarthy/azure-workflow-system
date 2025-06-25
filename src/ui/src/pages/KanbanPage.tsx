import React, { useState, useEffect } from 'react';
import {
  makeStyles,
  shorthands,
  Title2,
  Spinner,
  Body1,
  MessageBar,
  MessageBarBody,
} from '@fluentui/react-components';
import KanbanBoard from '../components/Kanban/KanbanBoard';
import { TicketDto, UserDto, TicketStatus } from '../types/api';
import { useUser } from '../contexts/UserContext';
import ApiService from '../services/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
  },
  header: {
    ...shorthands.margin('0', '0', '24px', '0'),
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '200px',
    gap: '12px',
  },
});

const KanbanPage: React.FC = () => {
  const styles = useStyles();
  const { canAssignTickets, canViewAllTickets } = useUser();
  const [tickets, setTickets] = useState<TicketDto[]>([]);
  const [engineers, setEngineers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        setError(null);

        if (!canViewAllTickets()) {
          setError('You do not have permission to view tickets.');
          return;
        }

        const [ticketsData, engineersData] = await Promise.all([
          ApiService.getTickets(),
          ApiService.getEngineers(),
        ]);

        setTickets(ticketsData);
        setEngineers(engineersData);
      } catch (err) {
        console.error('Failed to load data:', err);
        setError('Failed to load kanban data. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [canViewAllTickets]);

  const handleTicketMove = async (ticketId: number, newAssigneeId?: number) => {
    if (!canAssignTickets()) {
      setError('You do not have permission to assign tickets.');
      return;
    }

    try {
      if (newAssigneeId !== undefined) {
        // Assign the ticket
        await ApiService.assignTicket(ticketId, { assignedToId: newAssigneeId });

        // Update local state - ticket assignment
        setTickets((prevTickets) =>
          prevTickets.map((ticket) =>
            ticket.id === ticketId
              ? {
                  ...ticket,
                  assignedTo: newAssigneeId
                    ? engineers.find((eng) => eng.id === newAssigneeId)
                    : undefined,
                  status: newAssigneeId ? TicketStatus.Assigned : TicketStatus.New,
                }
              : ticket,
          ),
        );

        // Clear any previous errors
        setError(null);
      }
    } catch (err) {
      console.error('Failed to move ticket:', err);
      setError('Failed to assign ticket. Please try again.');
    }
  };

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>
          <Spinner size="medium" />
          <Body1>Loading kanban board...</Body1>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Kanban Board</Title2>
      </div>

      <KanbanBoard tickets={tickets} engineers={engineers} onTicketMove={handleTicketMove} />
    </div>
  );
};

export default KanbanPage;
