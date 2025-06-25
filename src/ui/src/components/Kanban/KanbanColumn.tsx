import React from 'react';
import { useDroppable } from '@dnd-kit/core';
import {
  SortableContext,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import {
  makeStyles,
  shorthands,
  tokens,
  Title3,
  Body2,
  mergeClasses,
} from '@fluentui/react-components';
import { TicketDto } from '../../types/api';
import TicketCard from './TicketCard';

const useStyles = makeStyles({
  column: {
    minWidth: '300px',
    width: '300px',
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius('8px'),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    display: 'flex',
    flexDirection: 'column',
    height: 'fit-content',
    maxHeight: 'calc(100vh - 200px)',
  },
  columnHeader: {
    ...shorthands.padding('16px'),
    ...shorthands.borderBottom('1px', 'solid', tokens.colorNeutralStroke2),
    backgroundColor: tokens.colorNeutralBackground3,
    borderTopLeftRadius: '8px',
    borderTopRightRadius: '8px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  columnTitle: {
    fontWeight: tokens.fontWeightSemibold,
  },
  ticketCount: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground2,
    ...shorthands.padding('2px', '8px'),
    ...shorthands.borderRadius('12px'),
    fontSize: '12px',
    fontWeight: tokens.fontWeightSemibold,
  },
  columnContent: {
    ...shorthands.padding('8px'),
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    overflowY: 'auto',
    flex: 1,
    minHeight: '200px',
  },
  dropZone: {
    minHeight: '100px',
    ...shorthands.border('2px', 'dashed', 'transparent'),
    ...shorthands.borderRadius('4px'),
    transition: 'border-color 0.2s ease',
  },
  dropZoneActive: {
    ...shorthands.borderColor(tokens.colorBrandStroke1),
    backgroundColor: tokens.colorBrandBackground,
  },
  emptyMessage: {
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    ...shorthands.padding('32px', '16px'),
    fontStyle: 'italic',
  },
});

interface KanbanColumnProps {
  id: string;
  title: string;
  tickets: TicketDto[];
}

const KanbanColumn: React.FC<KanbanColumnProps> = ({ id, title, tickets }) => {
  const styles = useStyles();
  
  const { setNodeRef, isOver } = useDroppable({
    id,
  });

  return (
    <div className={styles.column}>
      <div className={styles.columnHeader}>
        <Title3 className={styles.columnTitle}>{title}</Title3>
        <div className={styles.ticketCount}>
          {tickets.length}
        </div>
      </div>
      
      <div 
        ref={setNodeRef}
        className={mergeClasses(
          styles.columnContent,
          styles.dropZone,
          isOver && styles.dropZoneActive
        )}
      >
        <SortableContext
          items={tickets.map(ticket => ticket.id)}
          strategy={verticalListSortingStrategy}
        >
          {tickets.length === 0 ? (
            <Body2 className={styles.emptyMessage}>
              No tickets
            </Body2>
          ) : (
            tickets.map((ticket) => (
              <TicketCard key={ticket.id} ticket={ticket} />
            ))
          )}
        </SortableContext>
      </div>
    </div>
  );
};

export default KanbanColumn;