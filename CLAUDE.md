# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Tong quan

`SystemChatBoxRealtime` la ung dung chat realtime xay dung bang ASP.NET Core theo kien truc client-server, gom 2 service:

- `ApplicationServer`: Web API quan ly du lieu va nghiep vu chat
- `WebServer`: MVC front-end, proxy API va WebSocket cho realtime

He thong ho tro:

- nhan tin text
- gui anh va audio
- tao phong chat 1-1 va phong nhom
- thong bao realtime
- WebRTC signaling cho goi audio/video

## Kien truc

### 1. ApplicationServer

Web API `net9.0` lam backend chinh:

- CRUD conversation, message, user, profile, notification
- Entity Framework Core + SQL Server
- Cookie authentication dong bo voi `WebServer`
- CORS cho phep `WebServer` truy cap

### 2. WebServer

ASP.NET Core MVC `net9.0` lam lop giao dien:

- render trang login, register, home, profile
- proxy `/api/*` sang `ApplicationServer`
- mo endpoint WebSocket `/ws`
- forward cookie sang backend qua `HttpClient`

### 3. Realtime

- WebSocket handler nam trong `WebServer/Services/WebSocketHandler.cs`
- client co the `subscribe` / `unsubscribe` theo `conversationId`
- ho tro `ping` / `pong`
- ho tro `call.send` de gui payload WebRTC

## Chay ung dung

### Yeu cau

- .NET 9 SDK
- SQL Server
- 2 file `.env` hoac bien moi truong tuong duong

### Bien moi truong

#### `ApplicationServer/.env`

```powershell
DB_Connection=Server=...;Database=...;User Id=...;Password=...;Trust Server Certificate=True
WebServer_Origin=http://localhost:5007
```

#### `WebServer/.env`

```powershell
ApiClients__Auth__BaseUrl=http://localhost:5296/
```

Luu y:

- `ApplicationServer` dang doc `DB_Connection` tu environment, khong doc connection string trong `appsettings.json`
- `WebServer` doc `ApiClients:Auth:BaseUrl` tu config
- ca 2 service dung chung thu muc `shared-data-protection-keys/` de cookie auth hoat dong dong bo

### Chay local

```powershell
cd ApplicationServer
dotnet run

cd ..\WebServer
dotnet run
```

### Default ports theo `launchSettings.json`

- `ApplicationServer`: `http://localhost:5296` / `https://localhost:7268`
- `WebServer`: `http://localhost:5007` / `https://localhost:7231`

## API chinh

### ApplicationServer

#### Auth

- `POST /api/auth/login`
- `POST /api/auth/register`

#### Conversations

- `POST /api/conversations` - tao / lay phong 1-1
- `POST /api/conversations/group` - tao phong nhom
- `GET /api/conversations/threads?accountId=X` - lay danh sach thread
- `GET /api/conversations/{id}/messages?me=X&limit=Y&beforeMessageId=Z`
- `POST /api/conversations/{id}/messages`
- `POST /api/conversations/{id}/messages/image`
- `POST /api/conversations/{id}/messages/audio`
- `POST /api/conversations/{id}/mark-read?me=X`
- `GET /api/conversations/{id}/members?me=X`
- `DELETE /api/conversations/{id}/members/{memberId}?actorId=X`
- `GET /api/conversations/{id}/peer?meId=X`

#### Users

- `GET /api/users/{id}`
- `GET /api/users/search?email=X`

#### Profile

- `GET /api/profile/me`
- `PUT /api/profile/me`
- `POST /api/profile/me/avatar`
- `POST /api/profile/me/cover`
- `GET /api/profile/by-email?email=X`
- `GET /api/profile/{userId}`

#### Notifications

- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `POST /api/notifications/{id}/read`
- `POST /api/notifications/read-all`
- `POST /api/notifications/chat-message`

### WebServer

- `POST /auth/login`
- `POST /auth/register`
- `GET /notifications`
- `GET /notifications/unread-count`
- `POST /notifications/{id}/read`
- `POST /notifications/read-all`
- `GET /ws`

## Realtime message types

### WebSocket control

- `subscribe`
- `unsubscribe`
- `ping`
- `pong`
- `call.send`

### Message types

- `text`
- `image`
- `audio`

## Cau truc thu muc

```text
SystemChatBoxRealtime/
|-- ApplicationServer/
|   |-- Controllers/
|   |-- Dtos/
|   |-- Models/
|   |-- Program.cs
|   `-- ApplicationServer.csproj
|-- WebServer/
|   |-- Controllers/
|   |-- Services/
|   |-- ViewModels/
|   |-- Views/
|   |-- wwwroot/
|   |-- Program.cs
|   `-- WebServer.csproj
|-- shared-data-protection-keys/
|-- README.md
`-- SystemChatBoxRealtime.sln
```

## Ghi chu

- cookie auth co thoi han 8 gio
- tin nhan anh / audio duoc luu trong `wwwroot/uploads/`
- WebSocket endpoint duoc map thu cong tai `/ws`
- thong bao chat duoc tao tu controller notifications
