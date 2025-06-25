import React, { useState, useEffect, useCallback } from 'react';
import {
  makeStyles,
  shorthands,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogContent,
  DialogBody,
  DialogActions,
  Button,
  Text,
  Badge,
  Title3,
  Body1,
  Divider,
  Spinner,
} from '@fluentui/react-components';
import { PersonRegular, CalendarRegular } from '@fluentui/react-icons';
import {
  TicketDto,
  AttachmentDto,
  getStatusLabel,
  getPriorityLabel,
  getCategoryLabel,
} from '../../types/api';
import ApiService from '../../services/api';
import AttachmentManager from '../Attachments/AttachmentManager';

const useStyles = makeStyles({
  dialog: {
    maxWidth: '800px',
    width: '90vw',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: '20px',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: '16px',
  },
  ticketInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  badges: {
    display: 'flex',
    gap: '8px',
    flexWrap: 'wrap',
  },
  details: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
  },
  detailRow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ...shorthands.padding('40px'),
    gap: '12px',
  },
});

interface TicketDetailModalProps {
  ticket: TicketDto;
  trigger: React.ReactElement;
}

const TicketDetailModal: React.FC<TicketDetailModalProps> = ({ ticket, trigger }) => {
  const styles = useStyles();
  const [isOpen, setIsOpen] = useState(false);
  const [attachments, setAttachments] = useState<AttachmentDto[]>([]);
  const [isLoadingAttachments, setIsLoadingAttachments] = useState(false);

  const loadAttachments = useCallback(async () => {
    setIsLoadingAttachments(true);
    try {
      const ticketAttachments = await ApiService.getTicketAttachments(ticket.id);
      setAttachments(ticketAttachments);
    } catch (error) {
      console.error('Failed to load attachments:', error);
    } finally {
      setIsLoadingAttachments(false);
    }
  }, [ticket.id]);

  useEffect(() => {
    if (isOpen) {
      loadAttachments();
    }
  }, [isOpen, loadAttachments]);

  const getPriorityColor = (priority: number) => {
    switch (priority) {
      case 1:
        return 'informative'; // Low
      case 2:
        return 'subtle'; // Medium
      case 3:
        return 'warning'; // High
      case 4:
        return 'severe'; // Critical
      case 5:
        return 'danger'; // Emergency
      default:
        return 'subtle';
    }
  };

  const getStatusColor = (status: number) => {
    switch (status) {
      case 1:
        return 'informative'; // New
      case 2:
        return 'warning'; // Triaged
      case 3:
        return 'subtle'; // Assigned
      case 4:
        return 'brand'; // In Progress
      case 5:
        return 'success'; // Resolved
      case 6:
        return 'subtle'; // Closed
      default:
        return 'subtle';
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(event, data) => setIsOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>{trigger}</DialogTrigger>
      <DialogSurface className={styles.dialog}>
        <DialogBody>
          <DialogTitle>
            <div className={styles.header}>
              <div className={styles.ticketInfo}>
                <Title3>
                  #{ticket.id} - {ticket.title}
                </Title3>
                <div className={styles.badges}>
                  <Badge color={getStatusColor(ticket.status)} appearance="filled">
                    {getStatusLabel(ticket.status)}
                  </Badge>
                  <Badge color={getPriorityColor(ticket.priority)} appearance="filled">
                    {getPriorityLabel(ticket.priority)}
                  </Badge>
                  <Badge appearance="outline">{getCategoryLabel(ticket.category)}</Badge>
                </div>
              </div>
            </div>
          </DialogTitle>

          <DialogContent className={styles.content}>
            <div className={styles.details}>
              <div>
                <Text weight="semibold">Description</Text>
                <Body1>{ticket.description || 'No description provided'}</Body1>
              </div>

              <div className={styles.detailRow}>
                <PersonRegular />
                <Text>
                  <strong>Created by:</strong> {ticket.createdBy.firstName}{' '}
                  {ticket.createdBy.lastName} ({ticket.createdBy.email})
                </Text>
              </div>

              {ticket.assignedTo && (
                <div className={styles.detailRow}>
                  <PersonRegular />
                  <Text>
                    <strong>Assigned to:</strong> {ticket.assignedTo.firstName}{' '}
                    {ticket.assignedTo.lastName} ({ticket.assignedTo.email})
                  </Text>
                </div>
              )}

              <div className={styles.detailRow}>
                <CalendarRegular />
                <Text>
                  <strong>Created:</strong> {new Date(ticket.createdAt).toLocaleString()}
                </Text>
              </div>

              {ticket.slaTargetDate && (
                <div className={styles.detailRow}>
                  <CalendarRegular />
                  <Text>
                    <strong>SLA Target:</strong> {new Date(ticket.slaTargetDate).toLocaleString()}
                    {ticket.isSlaBreach && (
                      <Badge color="danger" appearance="filled" style={{ marginLeft: '8px' }}>
                        SLA Breach
                      </Badge>
                    )}
                  </Text>
                </div>
              )}
            </div>

            <Divider />

            {isLoadingAttachments ? (
              <div className={styles.loading}>
                <Spinner size="small" />
                <Text>Loading attachments...</Text>
              </div>
            ) : (
              <AttachmentManager
                ticketId={ticket.id}
                attachments={attachments}
                onAttachmentsChange={setAttachments}
              />
            )}
          </DialogContent>

          <DialogActions>
            <Button appearance="primary" onClick={() => setIsOpen(false)}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default TicketDetailModal;
