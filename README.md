# System Chat Box Realtime

## 1. Giới thiệu

**System Chat Box Realtime** là một hệ thống chat thời gian thực được xây dựng theo mô hình **Client – Server**, phục vụ nhu cầu nhắn tin realtime giữa nhiều người dùng.  
Hệ thống được thiết kế theo hướng **mở rộng, tách lớp rõ ràng**, dễ dàng triển khai và vận hành trong môi trường container với Docker.

---

## 2. Kiến trúc tổng thể

Hệ thống được chia thành các thành phần chính:

- **WebServer (Client)**  
  - ASP.NET Core MVC  
  - Cung cấp giao diện người dùng  
  - Kết nối realtime tới Server thông qua WebSocket  

- **SystemChatBoxRealtime (Server)**  
  - ASP.NET Core Web API  
  - Xử lý nghiệp vụ chat  
  - Quản lý kết nối WebSocket  
  - Lưu trữ dữ liệu thông qua Entity Framework  

- **Database**  
  - Lưu trữ thông tin người dùng, phòng chat, tin nhắn  

- **Docker**  
  - Đóng gói và triển khai toàn bộ hệ thống  

---

## 3. Công nghệ sử dụng

### Backend (Server)
- ASP.NET Core Web API
- Entity Framework Core
- WebSocket
- RESTful API
- Dependency Injection

### Frontend (Client)
- ASP.NET Core MVC
- HTML, CSS, JavaScript
- WebSocket Client

### DevOps & Deployment
- Docker
- Mô hình Client – Server

---

## 4. Chức năng chính

- Kết nối realtime giữa client và server bằng WebSocket
- Gửi và nhận tin nhắn theo thời gian thực
- Quản lý phiên kết nối người dùng
- Lưu trữ lịch sử tin nhắn
- Tách biệt rõ ràng giữa Client và Server
- Dễ dàng mở rộng cho nhiều client trong tương lai

---

## 5. Cấu trúc thư mục

```text
SystemChatBoxRealtime/
│
├── SystemChatBoxRealtime/     # Backend - ASP.NET Core Web API
│
├── WebServer/                 # Frontend - ASP.NET Core MVC
│
├── .dockerignore
├── .gitignore
├── SystemChatBoxRealtime.sln
└── README.md
