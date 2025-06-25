import React, { ReactNode } from 'react';
import { makeStyles, shorthands, tokens, Title1, Body1, Spinner } from '@fluentui/react-components';
import {
  GridKanbanRegular,
  PersonRegular,
  DocumentRegular,
  SettingsRegular,
} from '@fluentui/react-icons';
import { useUser } from '../../contexts/UserContext';
import { getRoleLabel } from '../../types/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
  },
  header: {
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.padding('16px', '24px'),
    ...shorthands.borderBottom('1px', 'solid', tokens.colorNeutralStroke2),
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  headerTitle: {
    color: tokens.colorBrandForeground1,
  },
  main: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  sidebar: {
    width: '240px',
    backgroundColor: tokens.colorNeutralBackground3,
    ...shorthands.padding('16px'),
    ...shorthands.borderRight('1px', 'solid', tokens.colorNeutralStroke2),
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
  },
  content: {
    flex: 1,
    ...shorthands.padding('24px'),
    overflow: 'auto',
  },
  navItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    ...shorthands.padding('8px', '12px'),
    ...shorthands.borderRadius('4px'),
    cursor: 'pointer',
    color: tokens.colorNeutralForeground1,
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Pressed,
    },
  },
  navItemActive: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground2,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground2Hover,
    },
  },
});

interface LayoutProps {
  children: ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const styles = useStyles();
  const { currentUser, isLoading } = useUser();

  const navItems = [
    { icon: <GridKanbanRegular />, label: 'Kanban Board', path: '/kanban', active: true },
    { icon: <DocumentRegular />, label: 'All Tickets', path: '/tickets' },
    { icon: <PersonRegular />, label: 'My Tickets', path: '/my-tickets' },
    { icon: <SettingsRegular />, label: 'Settings', path: '/settings' },
  ];

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <Title1 className={styles.headerTitle}>Azure Workflow System</Title1>
        {isLoading ? (
          <Spinner size="small" />
        ) : (
          <Body1>
            {currentUser
              ? `${currentUser.firstName} ${currentUser.lastName} (${getRoleLabel(currentUser.role)})`
              : 'Welcome, User'}
          </Body1>
        )}
      </header>

      <main className={styles.main}>
        <nav className={styles.sidebar}>
          {navItems.map((item) => (
            <div
              key={item.path}
              className={`${styles.navItem} ${item.active ? styles.navItemActive : ''}`}
            >
              {item.icon}
              <Body1>{item.label}</Body1>
            </div>
          ))}
        </nav>

        <div className={styles.content}>{children}</div>
      </main>
    </div>
  );
};

export default Layout;
