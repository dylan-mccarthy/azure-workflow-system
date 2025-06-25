# Azure Workflow System API Documentation

## Overview

The Azure Workflow System API provides endpoints for managing tickets, users, attachments, and processing Azure Monitor alerts. The API is built with ASP.NET Core 8 and uses OpenAPI/Swagger for documentation.

## Base URL

- Development: `https://localhost:7000`
- Swagger UI: `https://localhost:7000/swagger`

## Authentication

The API uses JWT Bearer token authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

### Users

#### GET /api/users
Get all users in the system.

**Authorization Required**: Yes  
**Response**: Array of UserDto objects

```json
[
  {
    "id": 1,
    "email": "admin@azureworkflow.com",
    "firstName": "System",
    "lastName": "Administrator",
    "fullName": "System Administrator",
    "role": "Admin",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

#### GET /api/users/{id}
Get a specific user by ID.

**Authorization Required**: Yes  
**Parameters**: `id` (integer) - User ID

#### POST /api/users
Create a new user.

**Authorization Required**: Yes (Admin role)  
**Request Body**:
```json
{
  "email": "newuser@azureworkflow.com",
  "firstName": "New",
  "lastName": "User",
  "role": "Engineer"
}
```

#### PUT /api/users/{id}
Update an existing user.

**Authorization Required**: Yes (Admin role)  
**Request Body**:
```json
{
  "firstName": "Updated",
  "lastName": "Name",
  "role": "Manager",
  "isActive": true
}
```

#### DELETE /api/users/{id}
Deactivate a user (soft delete).

**Authorization Required**: Yes (Admin role)

### Tickets

#### GET /api/tickets
Get all tickets with optional filtering.

**Authorization Required**: Yes  
**Query Parameters**:
- `status`: TicketStatus (New, Triaged, Assigned, InProgress, Resolved, Closed)
- `priority`: TicketPriority (Low, Medium, High, Critical, Emergency)
- `category`: TicketCategory (Incident, Access, NewResource, Change, Alert)
- `assignedToId`: Integer - Filter by assigned user

**Response**: Array of TicketDto objects

```json
[
  {
    "id": 1,
    "title": "Database connectivity issue",
    "description": "Unable to connect to production database",
    "status": "New",
    "priority": "High",
    "category": "Incident",
    "azureResourceId": "/subscriptions/.../resourceGroups/.../providers/...",
    "alertId": "alert-123",
    "createdBy": {
      "id": 1,
      "email": "admin@azureworkflow.com",
      "firstName": "System",
      "lastName": "Administrator",
      "role": "Admin"
    },
    "assignedTo": null,
    "slaTargetDate": "2024-01-01T08:00:00Z",
    "isSlaBreach": false,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z",
    "resolvedAt": null,
    "closedAt": null,
    "attachmentCount": 0
  }
]
```

#### GET /api/tickets/{id}
Get a specific ticket by ID.

**Authorization Required**: Yes  
**Parameters**: `id` (integer) - Ticket ID

#### POST /api/tickets
Create a new ticket.

**Authorization Required**: Yes  
**Request Body**:
```json
{
  "title": "New ticket title",
  "description": "Detailed description of the issue",
  "priority": "Medium",
  "category": "Incident",
  "azureResourceId": "/subscriptions/.../resourceGroups/...",
  "alertId": null,
  "assignedToId": 2
}
```

#### PUT /api/tickets/{id}
Update an existing ticket.

**Authorization Required**: Yes  
**Request Body**:
```json
{
  "title": "Updated title",
  "description": "Updated description",
  "status": "InProgress",
  "priority": "High",
  "category": "Incident"
}
```

#### PUT /api/tickets/{id}/assignee
Assign a ticket to a user.

**Authorization Required**: Yes  
**Request Body**:
```json
{
  "assignedToId": 3
}
```

#### DELETE /api/tickets/{id}
Delete a ticket.

**Authorization Required**: Yes (Admin or Manager role)

### Attachments

#### GET /api/attachments?ticketId={ticketId}
Get all attachments for a specific ticket.

**Authorization Required**: Yes  
**Query Parameters**: `ticketId` (integer) - Ticket ID

**Response**: Array of AttachmentDto objects

```json
[
  {
    "id": 1,
    "fileName": "screenshot.png",
    "contentType": "image/png",
    "fileSizeBytes": 1024000,
    "blobUrl": "https://storage.blob.core.windows.net/attachments/...",
    "ticketId": 1,
    "createdAt": "2024-01-01T00:00:00Z",
    "uploadedBy": {
      "id": 1,
      "email": "user@azureworkflow.com",
      "firstName": "John",
      "lastName": "Doe"
    }
  }
]
```

#### GET /api/attachments/{id}
Get a specific attachment by ID.

**Authorization Required**: Yes

#### POST /api/attachments/upload?ticketId={ticketId}
Upload a new file attachment to a ticket.

**Authorization Required**: Yes  
**Query Parameters**: `ticketId` (integer) - Ticket ID  
**Request Body**: multipart/form-data with file

**Note**: File size limit is 100 MB. Files exceeding this limit will return HTTP 413 status.

**Response**: AttachmentDto object with file metadata

#### POST /api/attachments?ticketId={ticketId}
Upload a new attachment to a ticket.

**Authorization Required**: Yes  
**Query Parameters**: `ticketId` (integer) - Ticket ID  
**Request Body**:
```json
{
  "fileName": "error-log.txt",
  "contentType": "text/plain",
  "fileSizeBytes": 5120,
  "blobUrl": "https://storage.blob.core.windows.net/attachments/..."
}
```

**Note**: File size limit is 100 MB.

#### GET /api/attachments/{id}/download
Download a specific attachment file.

**Authorization Required**: Yes

**Response**: File download with appropriate content-type headers

#### DELETE /api/attachments/{id}
Delete an attachment.

**Authorization Required**: Yes

### Alerts

#### POST /api/alerts
Webhook endpoint for Azure Monitor alerts. Creates tickets automatically from alert payloads.

**Authorization Required**: Yes (API Key via X-API-Key header)  
**Request Headers**: 
- `X-API-Key`: Your webhook API key
**Request Body**: Azure Monitor Common Alert Schema

```json
{
  "schemaId": "azureMonitorCommonAlertSchema",
  "data": {
    "essentials": {
      "alertId": "alert-123",
      "alertRule": "High CPU Usage",
      "severity": "Sev2",
      "signalType": "Metric",
      "monitorCondition": "Fired",
      "targetResource": [
        "/subscriptions/.../resourceGroups/.../providers/Microsoft.Compute/virtualMachines/vm1"
      ],
      "firedDateTime": "2024-01-01T00:00:00Z",
      "description": "CPU usage is above 80%"
    },
    "alertContext": {
      "conditionType": "SingleResourceMultipleMetricCriteria",
      "conditions": [
        {
          "metricName": "Percentage CPU",
          "metricUnit": "Percent",
          "metricValue": "85",
          "threshold": "80",
          "operator": "GreaterThan",
          "timeAggregation": "Average"
        }
      ]
    }
  }
}
```

**Response**:
```json
{
  "ticketId": 123,
  "message": "Ticket created successfully from alert",
  "alertId": "alert-123"
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid X-API-Key header
- `500 Internal Server Error`: Error processing alert payload

## Error Responses

All endpoints return standard HTTP status codes:

- `200` - Success
- `201` - Created (for POST requests)
- `204` - No Content (for PUT/DELETE requests)
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `500` - Internal Server Error

Error response format:
```json
{
  "title": "Error Title",
  "status": 400,
  "detail": "Detailed error message"
}
```

## Data Models

### Enums

#### UserRole
- `Viewer` (1) - Read-only access
- `Engineer` (2) - Can manage tickets and attachments
- `Manager` (3) - Can assign tickets and access reports
- `Admin` (4) - Full access to all features

#### TicketStatus
- `New` (1) - Newly created ticket
- `Triaged` (2) - Initial assessment completed
- `Assigned` (3) - Assigned to an engineer
- `InProgress` (4) - Work in progress
- `Resolved` (5) - Issue resolved, pending closure
- `Closed` (6) - Ticket closed

#### TicketPriority
- `Low` (1) - Low priority
- `Medium` (2) - Medium priority
- `High` (3) - High priority
- `Critical` (4) - Critical priority
- `Emergency` (5) - Emergency priority

#### TicketCategory
- `Incident` (1) - Production incident
- `Access` (2) - Access request
- `NewResource` (3) - New resource request
- `Change` (4) - Change request
- `Alert` (5) - Alert-generated ticket

## SLA Configuration

The system includes automatic SLA calculation based on priority and category:

| Priority | Category | Response Time | Resolution Time |
|----------|----------|---------------|-----------------|
| Critical | Incident | 15 minutes | 4 hours |
| High | Incident | 30 minutes | 8 hours |
| Medium | Incident | 1 hour | 24 hours |
| Low | Incident | 2 hours | 48 hours |
| Critical | Alert | 10 minutes | 2 hours |
| High | Alert | 15 minutes | 4 hours |
| Medium | Access | 4 hours | 24 hours |
| Medium | NewResource | 8 hours | 48 hours |

## Rate Limiting

Currently no rate limiting is implemented, but it's recommended for production use.

## Versioning

The API is currently at version 1.0. Future versions will maintain backward compatibility where possible.