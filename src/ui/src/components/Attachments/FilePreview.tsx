import React from 'react';
import {
  makeStyles,
  shorthands,
  tokens,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogContent,
  DialogBody,
  DialogActions,
  Button,
  Text,
} from '@fluentui/react-components';
import {
  DocumentRegular,
  ImageRegular,
  VideoRegular,
  MicRegular,
  CodeRegular,
} from '@fluentui/react-icons';
import { AttachmentDto } from '../../types/api';
import ApiService from '../../services/api';

const useStyles = makeStyles({
  dialog: {
    maxWidth: '90vw',
    maxHeight: '90vh',
    width: 'auto',
    height: 'auto',
    overflow: 'hidden',
  },
  previewContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '16px',
    ...shorthands.padding('16px'),
  },
  previewImage: {
    maxWidth: '100%',
    maxHeight: '70vh',
    objectFit: 'contain',
    ...shorthands.borderRadius('8px'),
  },
  previewText: {
    maxWidth: '100%',
    maxHeight: '70vh',
    overflow: 'auto',
    ...shorthands.padding('16px'),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius('8px'),
    fontFamily: 'monospace',
    fontSize: '14px',
    lineHeight: '1.4',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  previewVideo: {
    maxWidth: '100%',
    maxHeight: '70vh',
    ...shorthands.borderRadius('8px'),
  },
  previewAudio: {
    width: '100%',
    maxWidth: '400px',
  },
  notSupported: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '16px',
    ...shorthands.padding('40px'),
    color: tokens.colorNeutralForeground2,
  },
  fileInfo: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '8px',
    ...shorthands.padding('16px'),
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.borderRadius('8px'),
  },
});

interface FilePreviewProps {
  attachment: AttachmentDto;
  trigger: React.ReactElement;
}

const FilePreview: React.FC<FilePreviewProps> = ({ attachment, trigger }) => {
  const styles = useStyles();
  const [isOpen, setIsOpen] = React.useState(false);

  const isImage = attachment.contentType.startsWith('image/');
  const isVideo = attachment.contentType.startsWith('video/');
  const isAudio = attachment.contentType.startsWith('audio/');
  const isText =
    attachment.contentType.startsWith('text/') ||
    attachment.contentType === 'application/json' ||
    attachment.contentType === 'application/xml';
  const isPdf = attachment.contentType === 'application/pdf';

  const getFileIcon = () => {
    if (isImage) return <ImageRegular style={{ fontSize: '48px' }} />;
    if (isVideo) return <VideoRegular style={{ fontSize: '48px' }} />;
    if (isAudio) return <MicRegular style={{ fontSize: '48px' }} />;
    if (isText) return <CodeRegular style={{ fontSize: '48px' }} />;
    return <DocumentRegular style={{ fontSize: '48px' }} />;
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  const downloadUrl = ApiService.getAttachmentDownloadUrl(attachment.id);

  const renderPreview = () => {
    if (isImage) {
      return <img src={downloadUrl} alt={attachment.fileName} className={styles.previewImage} />;
    }

    if (isVideo) {
      return (
        <video src={downloadUrl} controls className={styles.previewVideo}>
          Your browser does not support the video tag.
        </video>
      );
    }

    if (isAudio) {
      return (
        <audio src={downloadUrl} controls className={styles.previewAudio}>
          Your browser does not support the audio tag.
        </audio>
      );
    }

    if (isPdf) {
      return (
        <iframe
          src={downloadUrl}
          style={{ width: '100%', height: '70vh', border: 'none' }}
          title={attachment.fileName}
        />
      );
    }

    if (isText && attachment.fileSizeBytes < 1024 * 1024) {
      // Only preview text files under 1MB
      return (
        <div className={styles.previewText}>
          <iframe
            src={downloadUrl}
            style={{ width: '100%', height: '100%', border: 'none' }}
            title={attachment.fileName}
          />
        </div>
      );
    }

    return (
      <div className={styles.notSupported}>
        {getFileIcon()}
        <Text>Preview not available for this file type</Text>
        <Button appearance="primary" onClick={() => window.open(downloadUrl, '_blank')}>
          Download to view
        </Button>
      </div>
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={(event, data) => setIsOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>{trigger}</DialogTrigger>
      <DialogSurface className={styles.dialog}>
        <DialogBody>
          <DialogTitle>
            <Text weight="semibold">{attachment.fileName}</Text>
          </DialogTitle>

          <DialogContent>
            <div className={styles.fileInfo}>
              <Text size={200}>
                {attachment.contentType} • {formatFileSize(attachment.fileSizeBytes)}
              </Text>
              <Text size={200}>
                Uploaded by {attachment.uploadedBy.firstName} {attachment.uploadedBy.lastName}
              </Text>
            </div>

            <div className={styles.previewContainer}>{renderPreview()}</div>
          </DialogContent>

          <DialogActions>
            <Button appearance="secondary" onClick={() => window.open(downloadUrl, '_blank')}>
              Download
            </Button>
            <Button appearance="primary" onClick={() => setIsOpen(false)}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default FilePreview;
