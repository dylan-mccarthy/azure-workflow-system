import React, { useState, useRef } from 'react';
import {
  makeStyles,
  shorthands,
  tokens,
  Button,
  Text,
  MessageBar,
  MessageBarBody,
  Spinner,
} from '@fluentui/react-components';
import {
  AttachRegular,
  DocumentRegular,
  DeleteRegular,
  WarningRegular,
  EyeRegular,
} from '@fluentui/react-icons';
import { AttachmentDto } from '../../types/api';
import ApiService from '../../services/api';
import FilePreview from './FilePreview';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px',
  },
  uploadArea: {
    ...shorthands.border('2px', 'dashed', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius('8px'),
    ...shorthands.padding('24px'),
    textAlign: 'center',
    backgroundColor: tokens.colorNeutralBackground2,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
    '&:hover': {
      ...shorthands.borderColor(tokens.colorBrandStroke2),
      backgroundColor: tokens.colorNeutralBackground1,
    },
  },
  uploadAreaDragOver: {
    ...shorthands.borderColor(tokens.colorBrandStroke2),
    backgroundColor: tokens.colorBrandBackground2,
  },
  uploadText: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '8px',
  },
  hiddenInput: {
    display: 'none',
  },
  attachmentList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  attachmentItem: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    ...shorthands.padding('12px'),
    ...shorthands.border('1px', 'solid', tokens.colorNeutralStroke2),
    ...shorthands.borderRadius('6px'),
    backgroundColor: tokens.colorNeutralBackground1,
  },
  attachmentInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flex: 1,
  },
  attachmentDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  },
  attachmentActions: {
    display: 'flex',
    gap: '8px',
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
  },
  loading: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
});

interface AttachmentManagerProps {
  ticketId: number;
  attachments: AttachmentDto[];
  onAttachmentsChange: (attachments: AttachmentDto[]) => void;
}

const AttachmentManager: React.FC<AttachmentManagerProps> = ({
  ticketId,
  attachments,
  onAttachmentsChange,
}) => {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB

  const validateFile = (file: File): string | null => {
    if (file.size > MAX_FILE_SIZE) {
      return `File size exceeds 100 MB limit. Selected file is ${(file.size / (1024 * 1024)).toFixed(1)} MB.`;
    }
    return null;
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  const handleFileSelect = async (files: FileList | null) => {
    if (!files || files.length === 0) return;

    const file = files[0];
    const validationError = validateFile(file);

    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setIsUploading(true);

    try {
      const newAttachment = await ApiService.uploadAttachment(ticketId, file);
      if (newAttachment) {
        onAttachmentsChange([...attachments, newAttachment]);
      } else {
        setError('Failed to upload file. Please try again.');
      }
    } catch {
      setError('Failed to upload file. Please try again.');
    } finally {
      setIsUploading(false);
    }
  };

  const handleDelete = async (attachmentId: number) => {
    const success = await ApiService.deleteAttachment(attachmentId);
    if (success) {
      onAttachmentsChange(attachments.filter((a) => a.id !== attachmentId));
    } else {
      setError('Failed to delete attachment. Please try again.');
    }
  };

  const handleDownload = (attachment: AttachmentDto) => {
    const downloadUrl = ApiService.getAttachmentDownloadUrl(attachment.id);
    window.open(downloadUrl, '_blank');
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    handleFileSelect(e.dataTransfer.files);
  };

  const handleUploadClick = () => {
    fileInputRef.current?.click();
  };

  return (
    <div className={styles.container}>
      <Text weight="semibold">Attachments</Text>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>
            <div className={styles.errorMessage}>
              <WarningRegular /> {error}
            </div>
          </MessageBarBody>
        </MessageBar>
      )}

      <div
        className={`${styles.uploadArea} ${isDragOver ? styles.uploadAreaDragOver : ''}`}
        onClick={handleUploadClick}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <div className={styles.uploadText}>
          <AttachRegular style={{ fontSize: '24px', color: tokens.colorNeutralForeground2 }} />
          <Text>Click to browse or drag and drop files here</Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground2 }}>
            Maximum file size: 100 MB
          </Text>
        </div>
        <input
          ref={fileInputRef}
          type="file"
          className={styles.hiddenInput}
          onChange={(e) => handleFileSelect(e.target.files)}
          accept="*/*"
        />
      </div>

      {isUploading && (
        <div className={styles.loading}>
          <Spinner size="tiny" />
          <Text>Uploading file...</Text>
        </div>
      )}

      {attachments.length > 0 && (
        <div className={styles.attachmentList}>
          {attachments.map((attachment) => (
            <div key={attachment.id} className={styles.attachmentItem}>
              <div className={styles.attachmentInfo}>
                <DocumentRegular style={{ color: tokens.colorNeutralForeground2 }} />
                <div className={styles.attachmentDetails}>
                  <Text weight="semibold">{attachment.fileName}</Text>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground2 }}>
                    {formatFileSize(attachment.fileSizeBytes)} • Uploaded by{' '}
                    {attachment.uploadedBy.firstName} {attachment.uploadedBy.lastName}
                  </Text>
                </div>
              </div>
              <div className={styles.attachmentActions}>
                <FilePreview
                  attachment={attachment}
                  trigger={
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<EyeRegular />}
                      title="Preview file"
                    >
                      Preview
                    </Button>
                  }
                />
                <Button appearance="subtle" size="small" onClick={() => handleDownload(attachment)}>
                  Download
                </Button>
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<DeleteRegular />}
                  onClick={() => handleDelete(attachment.id)}
                  title="Delete attachment"
                />
              </div>
            </div>
          ))}
        </div>
      )}

      {attachments.length === 0 && !isUploading && (
        <Text style={{ color: tokens.colorNeutralForeground2, textAlign: 'center' }}>
          No attachments yet
        </Text>
      )}
    </div>
  );
};

export default AttachmentManager;
