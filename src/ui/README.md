# Azure Workflow System - Frontend UI

This directory contains the frontend web application for the Azure Platform Support Workflow Management System.

## Features

- **Kanban board with swim lanes** for ticket management by assignee
- **Drag-and-drop functionality** for ticket assignment using @dnd-kit
- **Ticket creation and management interface** with rich ticket cards
- **SLA countdown visualization** with priority and breach indicators
- **Role-based dashboards and views** with permission controls
- **Reporting and analytics dashboard** (planned)
- **File attachment upload and download** (planned)
- **Responsive design** that works on desktop and mobile
- **Accessibility compliance** following WCAG 2.1 AA guidelines

## Technology Stack

- **React 19** with TypeScript for type safety
- **Vite** for fast development and build tooling
- **Fluent UI v9** for Microsoft design system components
- **@dnd-kit** for accessible drag-and-drop functionality  
- **React Router** for navigation
- **Axios** for API communication
- **Azure Authentication Library for React** (planned for MSAL integration)
- **Vitest** and **Testing Library** for unit and integration tests

## Local Development

### Prerequisites
- Node.js 18+ 
- npm 8+

### Setup
```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Run tests
npm run test

# Run tests once
npm run test:run

# Lint code
npm run lint
```

### Development Server
The development server runs on `http://localhost:5173` by default.

### API Configuration
The UI expects the API to be running at `https://localhost:7000/api` by default. This can be configured via the `VITE_API_BASE_URL` environment variable.

## Pages

- **Dashboard** - Overview of open tickets and SLA status (planned)
- **Kanban Board** - Main ticket management interface with swim lanes
- **Ticket Detail** - Individual ticket view and edit (planned)
- **Reports** - MTTA, MTTR, and other metrics (planned)
- **Administration** - User, role, and SLA configuration (planned)

## Architecture

The UI follows a modern React component architecture:

- **Components** - Reusable UI elements organized by feature
  - `Layout/` - Application shell and navigation
  - `Kanban/` - Kanban board, columns, and ticket cards
- **Pages** - Screen layouts and page-level logic
- **Hooks** - Shared logic and state management (planned)
- **Services** - API integration layer with typed service classes
- **Context** - Application state management (UserContext)
- **Types** - TypeScript definitions matching backend DTOs

## Key Components

### KanbanBoard
Main kanban interface with drag-and-drop swim lanes:
- Displays tickets in columns by assignee (Unassigned + Engineer columns)
- Supports drag-and-drop for ticket assignment
- Calls `PUT /api/tickets/{id}/assignee` when tickets are moved
- Updates ticket status automatically when assigned

### TicketCard  
Rich ticket display component:
- Shows ticket ID, title, description, priority, status, category
- Color-coded priority badges
- SLA target date with breach indicators
- Assignee information
- Accessible with proper ARIA labels and keyboard support

### Layout
Application shell with:
- Navigation sidebar with role-based menu items
- Header with user information and role display
- Responsive design that adapts to screen size

## Role-Based Access

The application supports role-based views:
- **Viewer** - Read-only access
- **Engineer** - Can view tickets, limited assignment
- **Manager** - Can assign tickets and manage workflow  
- **Admin** - Full system access

Permission checks are handled via the `UserContext`:
- `canAssignTickets()` - Managers and Admins only
- `canViewAllTickets()` - All roles except Viewer
- `hasRole(role)` - Check specific role

## API Integration

The `ApiService` class provides typed methods for:
- `getTickets(params?)` - Fetch tickets with optional filtering
- `getUsers()` - Fetch all users
- `getEngineers()` - Fetch active engineers for swim lanes
- `assignTicket(id, assignData)` - Assign ticket to user
- `updateTicket(id, updateData)` - Update ticket properties

## Testing

The project includes:
- **Unit tests** for individual components using Vitest + Testing Library
- **API service tests** with mocked HTTP calls
- **Accessibility testing** via Testing Library's built-in a11y checks

Run tests with:
```bash
npm run test          # Watch mode
npm run test:run      # Single run
npm run test:ui       # UI mode (if installed)
```

## Build and Deployment

```bash
# Production build
npm run build

# Preview production build locally  
npm run preview
```

Build output goes to `/dist` directory and can be served by any static file server.

## Future Enhancements

- **Azure AD/Entra ID authentication** with MSAL
- **Real-time updates** with SignalR
- **Advanced filtering and search**
- **Ticket creation and editing forms**
- **Bulk operations**
- **Reporting dashboard**
- **Mobile-first responsive improvements**
- **Offline support with service workers**
