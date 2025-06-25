# API Service

This directory contains the backend API service for the Azure Platform Support Workflow Management System.

## Features

- REST API endpoints for tickets, alerts, and users
- Azure Monitor alert webhook endpoint
- Authentication and authorization with Entra ID
- SLA calculation engine
- File attachment handling

## Technology Stack

- ASP.NET Core API
- PostgreSQL database via EF Core
- OpenAPI documentation
- Azure SDK for .NET

## Local Development

TBD: Instructions for local development setup

## API Endpoints

- `/tickets` - Ticket management endpoints
- `/alerts` - Azure Monitor alert webhook
- `/users` - User management endpoints
- `/attachments` - File attachment endpoints

## Architecture

The API follows a clean architecture pattern with the following layers:
- Controllers (API endpoints)
- Services (Business logic)
- Data Access (Repository pattern)
- Models (Domain entities)
