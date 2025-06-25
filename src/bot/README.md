# Teams Bot

This directory contains the Microsoft Teams bot for the Azure Platform Support Workflow Management System.

## Features

- Teams bot for creating tickets via `/ticket` command
- Natural language parsing for ticket details
- Ticket creation confirmation messages with links
- Error handling and retry logic
- Authentication and authorization

## Technology Stack

- Bot Framework SDK v4
- Node.js
- TypeScript
- Azure Bot Service
- Azure Functions (for bot logic)

## Local Development

TBD: Instructions for local development setup

## Bot Commands

- `/ticket [description]` - Create a new ticket with the provided description

## Architecture

The bot follows a modular architecture:

- Commands (Bot command handlers)
- Services (Integration with API)
- NLP (Natural language processing)
- Models (TypeScript interfaces)
- Middleware (Authentication, logging)
- Adapters (Bot Framework integration)
