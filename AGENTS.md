# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

Real-time chat application built with ASP.NET Core using a Client-Server architecture. The system supports text, image, audio messages, and WebRTC-based video/audio calls.

## Architecture

**Two-service architecture:**

1. **ApplicationServer** (.NET 9 Web API)
   - REST API for CRUD operations (conversations, messages, users)
   - Entity Framework Core with SQL Server
   - Dtos layered in `Dtos/` folder (Auth, Conversations, User)

2. **WebServer** (.NET 9 MVC + WebSockets)
   - MVC views for chat UI
   - Custom WebSocket endpoint (`/ws`) for real-time messaging
   - HttpClient services calling ApplicationServer APIs
   - Cookie-based authentication

3. **Realtime Communication:**
   - WebSocket: `WebSocketHandler.cs` + `RealtimeHub.cs` for socket management
   - Subscriptions: clients subscribe to conversation IDs to receive messages
   - WebRTC: `webrtc-service.js` handles peer connections (offer/answer/ICE)

## Running the Application

### Prerequisites
- .NET 9 SDK
- SQL Server database
- Environment variables configured (via `.env` file using DotNetEnv)

### Starting Services

```powershell
# Run ApplicationServer (API)
cd ApplicationServer
dotnet run

# Run WebServer (MVC + WebSocket)
cd WebServer
dotnet run
```

### Environment Configuration

Both servers use `DotNetEnv` to load environment variables from `.env` files:

**ApplicationServer/.env:**
```
DB_CONNECTION=Server=...;Database=...;User Id=...;Password=...
```

**WebServer/.env:**
```
API_CLIENTS__AUTH__BASE_URL=http://localhost:5xxx/  # ApplicationServer URL
```

## Key Message Types

**WebSocket Messages:**
- `subscribe` / `unsubscribe` - Join conversation channels
- `ping` / `pong` - Keep-alive
- `call.send` - WebRTC signaling (offer/answer/ICE)

**Message Types (WebSocket):**
- `message-text`, `message-image`, `message-audio`

## Frontend JavaScript Architecture

**Core services** (`wwwroot/js/`):
- `ws-client.js` - WebSocket connection + subscription
- `webrtc-service.js` - WebRTC peer connection management
- `chatService.js` - HTTP API client for conversations/messages
- `authService.js` - Authentication operations

**Page-specific modules** (`wwwroot/js/pages/chat/`):
- `chat_realtime.js` - Real-time message rendering via WebSocket
- `chat_composer.js` - Message input (text, image, audio recording)
- `threads.js` - Thread list rendering
- `video-call-ui.js` - Video call UI handling

## Database Schema (EF Core)

Key entities in `SocialNetworkContext`:
- `Account` - User accounts (email, password, profile)
- `Conversation` - Chat rooms (group or 1-1)
- `ConversationMember` - Conversation memberships
- `Message` - Messages (text, image, audio types)
- `Friendship` - Friend requests/connections
- `Post`, `PostComment`, `PostLike` - Social feed features

## Common Operations

### Running Tests
```powershell
dotnet test
```

### Adding Entity Framework Migration
```powershell
cd ApplicationServer
dotnet ef migrations add MigrationName
```

### Database Update
```powershell
cd ApplicationServer
dotnet ef database update
```

## API Endpoints (ApplicationServer)

**AuthController:**
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login

**ConversationsController:**
- `POST /api/conversations` - Create/get 1-1 conversation
- `GET /api/conversations/threads?accountId=X` - Get user's conversation threads
- `GET /api/conversations/{id}/messages?me=X&limit=Y` - Get messages (paginated)
- `POST /api/conversations/{id}/messages` - Send text message
- `POST /api/conversations/{id}/messages/image` - Send image message
- `POST /api/conversations/{id}/messages/audio` - Send audio message
- `GET /api/conversations/{id}/peer?meId=X` - Get peer info for WebRTC

**UsersController:**
- `GET /api/users/search?email=X` - Search users by email
- `GET /api/users/{id}/personal` - Get user personal info

## Docker Deployment

Each service has its own Dockerfile targeting .NET 9. Services expose ports 8080/8081.

## Important Notes

- Authentication uses cookie-based auth with 8-hour expiry
- WebSocket endpoint is manually mapped at `/ws` in WebServer
- Images/audio are saved to `wwwroot/uploads/chat/` and `wwwroot/uploads/voice/`
- Real-time delivery: WebSocket broadcasts to all sockets subscribed to a conversation
- WebRTC uses STUN servers (Google) for NAT traversal