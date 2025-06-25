import React from 'react';
import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import {
  restrictToWindowEdges,
} from '@dnd-kit/modifiers';
import {
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import { TicketDto, UserDto, TicketStatus } from '../../types/api';
import KanbanColumn from './KanbanColumn';
import TicketCard from './TicketCard';

const useStyles = makeStyles({
  kanbanContainer: {
    display: 'flex',
    gap: '16px',
    height: '100%',
    overflowX: 'auto',
    ...shorthands.padding('8px'),
  },
});

interface KanbanBoardProps {
  tickets: TicketDto[];
  engineers: UserDto[];
  onTicketMove: (ticketId: number, newAssigneeId?: number) => Promise<void>;
}

const KanbanBoard: React.FC<KanbanBoardProps> = ({
  tickets,
  engineers,
  onTicketMove,
}) => {
  const styles = useStyles();
  const [activeTicket, setActiveTicket] = React.useState<TicketDto | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  // Create columns: Unassigned + Engineer columns
  const columns = [
    {
      id: 'unassigned',
      title: 'Unassigned',
      tickets: tickets.filter(ticket => !ticket.assignedTo && ticket.status !== TicketStatus.Closed),
    },
    ...engineers.map(engineer => ({
      id: `engineer-${engineer.id}`,
      title: `${engineer.firstName} ${engineer.lastName}`,
      engineerId: engineer.id,
      tickets: tickets.filter(ticket => 
        ticket.assignedTo?.id === engineer.id && 
        ticket.status !== TicketStatus.Closed
      ),
    })),
  ];

  const handleDragStart = (event: DragStartEvent) => {
    const { active } = event;
    const ticket = tickets.find(t => t.id === Number(active.id));
    setActiveTicket(ticket || null);
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveTicket(null);

    if (!over) return;

    const ticketId = Number(active.id);
    const columnId = String(over.id);

    // Determine new assignee
    let newAssigneeId: number | undefined;
    if (columnId === 'unassigned') {
      newAssigneeId = undefined; // Unassign
    } else if (columnId.startsWith('engineer-')) {
      newAssigneeId = Number(columnId.replace('engineer-', ''));
    }

    // Find current ticket
    const currentTicket = tickets.find(t => t.id === ticketId);
    if (!currentTicket) return;

    // Check if assignment actually changed
    const currentAssigneeId = currentTicket.assignedTo?.id;
    if (currentAssigneeId === newAssigneeId) return;

    // Call the move handler
    await onTicketMove(ticketId, newAssigneeId);
  };

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      modifiers={[restrictToWindowEdges]}
    >
      <div className={styles.kanbanContainer}>
        {columns.map((column) => (
          <KanbanColumn
            key={column.id}
            id={column.id}
            title={column.title}
            tickets={column.tickets}
          />
        ))}
      </div>

      <DragOverlay>
        {activeTicket ? <TicketCard ticket={activeTicket} isDragging /> : null}
      </DragOverlay>
    </DndContext>
  );
};

export default KanbanBoard;