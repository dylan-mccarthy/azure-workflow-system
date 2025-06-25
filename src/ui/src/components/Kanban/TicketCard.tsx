import React from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  makeStyles,
  shorthands,
  tokens,
  Title3,
  Body2,
  Badge,
  mergeClasses,
} from '@fluentui/react-components';
import {
  AlertRegular,
  PersonRegular,
  CalendarRegular,
  ReOrderDotsVerticalRegular,
} from '@fluentui/react-icons';
import {
  TicketDto,
  TicketPriority,
  getStatusLabel,
  getPriorityLabel,
  getCategoryLabel,
} from '../../types/api';

const useStyles = makeStyles({
  card: {
    backgroundColor: tokens.colorNeutralBackground1,
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius('6px'),
    ...shorthands.padding('12px'),
    cursor: 'grab',
    transition: 'all 0.2s ease',
    '&:hover': {
      ...shorthands.borderColor(tokens.colorBrandStroke2),
      boxShadow: tokens.shadow4,
    },
  },
  cardDragging: {
    opacity: 0.6,
    transform: 'rotate(5deg)',
    boxShadow: tokens.shadow8,
    cursor: 'grabbing',
  },
  cardHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    ...shorthands.margin('0', '0', '8px', '0'),
  },
  ticketTitle: {
    fontSize: '14px',
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: '1.3',
    flex: 1,
    ...shorthands.margin('0', '8px', '0', '0'),
  },
  dragHandle: {
    color: tokens.colorNeutralForeground3,
    cursor: 'grab',
    '&:hover': {
      color: tokens.colorNeutralForeground1,
    },
  },
  ticketId: {
    color: tokens.colorNeutralForeground3,
    fontSize: '12px',
    ...shorthands.margin('0', '0', '8px', '0'),
  },
  ticketDescription: {
    color: tokens.colorNeutralForeground2,
    fontSize: '13px',
    lineHeight: '1.4',
    display: '-webkit-box',
    webkitLineClamp: '2',
    webkitBoxOrient: 'vertical',
    overflow: 'hidden',
    ...shorthands.margin('0', '0', '12px', '0'),
  },
  badges: {
    display: 'flex',
    gap: '4px',
    flexWrap: 'wrap',
    ...shorthands.margin('0', '0', '8px', '0'),
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    fontSize: '12px',
    color: tokens.colorNeutralForeground3,
  },
  assignee: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  slaInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
  },
  slaBreach: {
    color: tokens.colorPaletteRedForeground1,
  },
  slaWarning: {
    color: tokens.colorPaletteDarkOrangeForeground1,
  },
  cardWarning: {
    ...shorthands.border('2px', 'solid', tokens.colorPaletteDarkOrangeBackground2),
  },
  cardBreach: {
    ...shorthands.border('2px', 'solid', tokens.colorPaletteRedBackground2),
  },
  // Priority colors
  priorityLow: {
    backgroundColor: tokens.colorPaletteGreenBackground1,
    color: tokens.colorPaletteGreenForeground1,
  },
  priorityMedium: {
    backgroundColor: tokens.colorPaletteYellowBackground1,
    color: tokens.colorPaletteYellowForeground1,
  },
  priorityHigh: {
    backgroundColor: tokens.colorPaletteDarkOrangeBackground1,
    color: tokens.colorPaletteDarkOrangeForeground1,
  },
  priorityCritical: {
    backgroundColor: tokens.colorPaletteRedBackground1,
    color: tokens.colorPaletteRedForeground1,
  },
  priorityEmergency: {
    backgroundColor: tokens.colorPaletteDarkRedBackground2,
    color: tokens.colorPaletteDarkRedForeground2,
  },
});

interface TicketCardProps {
  ticket: TicketDto;
  isDragging?: boolean;
}

const TicketCard: React.FC<TicketCardProps> = ({ ticket, isDragging = false }) => {
  const styles = useStyles();

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging: isSortableDragging,
  } = useSortable({
    id: ticket.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  const getPriorityBadgeStyle = (priority: TicketPriority) => {
    switch (priority) {
      case TicketPriority.Low:
        return styles.priorityLow;
      case TicketPriority.Medium:
        return styles.priorityMedium;
      case TicketPriority.High:
        return styles.priorityHigh;
      case TicketPriority.Critical:
        return styles.priorityCritical;
      case TicketPriority.Emergency:
        return styles.priorityEmergency;
      default:
        return '';
    }
  };

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleDateString();
    } catch {
      return dateString;
    }
  };

  const formatSlaCountdown = (remainingMinutes?: number) => {
    if (!remainingMinutes) return null;
    
    if (remainingMinutes < 0) {
      const overdue = Math.abs(remainingMinutes);
      if (overdue < 60) return `${overdue}m overdue`;
      const hours = Math.floor(overdue / 60);
      const mins = overdue % 60;
      return mins > 0 ? `${hours}h ${mins}m overdue` : `${hours}h overdue`;
    }
    
    if (remainingMinutes < 60) return `${remainingMinutes}m left`;
    const hours = Math.floor(remainingMinutes / 60);
    const mins = remainingMinutes % 60;
    return mins > 0 ? `${hours}h ${mins}m left` : `${hours}h left`;
  };

  const getSlaStatusClass = () => {
    if (ticket.isSlaBreach) return styles.slaBreach;
    if (ticket.isImminentSlaBreach) return styles.slaWarning;
    return '';
  };

  const getCardBorderClass = () => {
    if (ticket.isSlaBreach) return styles.cardBreach;
    if (ticket.isImminentSlaBreach) return styles.cardWarning;
    return '';
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={mergeClasses(
        styles.card,
        getCardBorderClass(),
        (isDragging || isSortableDragging) && styles.cardDragging,
      )}
      aria-label={`Ticket ${ticket.id}: ${ticket.title}. Priority: ${getPriorityLabel(ticket.priority)}. Status: ${getStatusLabel(ticket.status)}. ${ticket.assignedTo ? `Assigned to ${ticket.assignedTo.firstName} ${ticket.assignedTo.lastName}` : 'Unassigned'}`}
      {...attributes}
      {...listeners}
    >
      <div className={styles.cardHeader}>
        <Body2 className={styles.ticketId}>#{ticket.id}</Body2>
        <ReOrderDotsVerticalRegular className={styles.dragHandle} />
      </div>

      <Title3 className={styles.ticketTitle}>{ticket.title}</Title3>

      {ticket.description && (
        <Body2 className={styles.ticketDescription}>{ticket.description}</Body2>
      )}

      <div className={styles.badges}>
        <Badge appearance="filled" className={getPriorityBadgeStyle(ticket.priority)}>
          {getPriorityLabel(ticket.priority)}
        </Badge>
        <Badge appearance="outline">{getStatusLabel(ticket.status)}</Badge>
        <Badge appearance="outline">{getCategoryLabel(ticket.category)}</Badge>
      </div>

      <div className={styles.footer}>
        <div className={styles.assignee}>
          <PersonRegular />
          <span>
            {ticket.assignedTo
              ? `${ticket.assignedTo.firstName} ${ticket.assignedTo.lastName}`
              : 'Unassigned'}
          </span>
        </div>

        {ticket.slaTargetDate && (
          <div className={mergeClasses(styles.slaInfo, getSlaStatusClass())}>
            <CalendarRegular />
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
              <span style={{ fontSize: '11px' }}>{formatDate(ticket.slaTargetDate)}</span>
              {ticket.slaRemainingMinutes !== undefined && (
                <span style={{ fontSize: '10px', fontWeight: 'bold' }}>
                  {formatSlaCountdown(ticket.slaRemainingMinutes)}
                </span>
              )}
            </div>
            {(ticket.isSlaBreach || ticket.isImminentSlaBreach) && <AlertRegular />}
          </div>
        )}
      </div>
    </div>
  );
};

export default TicketCard;
