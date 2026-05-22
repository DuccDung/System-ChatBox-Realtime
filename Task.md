# Task - Module Bai Viet / Like / Comment / Share

## Trang thai hien tai

- Trang thai: da co implement, dang can review va chot pham vi cuoi
- Project lien quan: `ApplicationServer`, `WebServer`
- Muc tieu hien tai: co 1 luong bai viet rieng, khong anh huong UI chat cu

## Ket luan da chot

- Like: luu trong SQL Server
- Share: luu reference toi bai viet goc
- Comment: ho tro comment cha - comment con, khong gioi han do sau
- Xoa comment: xoa mem
- Feed: lay toan bo bai viet
- UI bai viet: dung page/layout rieng de tach khoi giao dien chat

## Pham vi da co trong code

### Backend - ApplicationServer

Da co:

- `ApplicationServer/Controllers/PostsController.cs`
- `ApplicationServer/Dtos/Posts/PostDtos.cs`
- `ApplicationServer/Models/Post.cs`
- `ApplicationServer/Models/PostComment.cs`
- `ApplicationServer/Models/PostLike.cs`
- `ApplicationServer/Models/PostShare.cs`
- `ApplicationServer/Models/PostMedium.cs`
- mapping trong `ApplicationServer/Models/SocialNetworkContext.cs`

API hien co:

- `GET /api/posts/feed`
- `GET /api/posts/user/{accountId}`
- `GET /api/posts/{postId}`
- `POST /api/posts`
- `PUT /api/posts/{postId}`
- `DELETE /api/posts/{postId}`
- `POST /api/posts/{postId}/like`
- `DELETE /api/posts/{postId}/like`
- `POST /api/posts/{postId}/share`
- `DELETE /api/posts/{postId}/share`
- `GET /api/posts/{postId}/likes`
- `GET /api/posts/{postId}/shares`
- `POST /api/posts/{postId}/comments`
- `PUT /api/posts/{postId}/comments/{commentId}`
- `DELETE /api/posts/{postId}/comments/{commentId}`

### Frontend - WebServer

Da co:

- `WebServer/Controllers/PostsController.cs`
- `WebServer/Interfaces/IPostService.cs`
- `WebServer/Services/PostService.cs`
- `WebServer/Dtos/PostDtos.cs`
- `WebServer/Views/Posts/Feed.cshtml`
- `WebServer/Views/Posts/Detail.cshtml`
- `WebServer/Views/Shared/Partials/_PostCard.cshtml`
- `WebServer/Views/Shared/Partials/_CommentNode.cshtml`
- `WebServer/Views/Shared/_LayoutPosts.cshtml`
- `WebServer/wwwroot/css/pages/posts/main.css`

UI hien tai:

- co trang feed rieng cho bai viet
- co trang detail rieng cho tung bai viet
- page bai viet dung layout rieng, khong dung `_Topbar` cua chat
- khong keo `home.css` vao trang bai viet nua

## Cach hoat dong hien tai

### Feed

- lay danh sach bai viet
- hien thi author, content, media, like/comment/share count
- cho phep like / unlike
- cho phep share / unshare
- co nut vao trang chi tiet

### Detail

- hien thi 1 bai viet don le
- hien thi comment theo cay cha - con
- cho phep them comment moi
- cho phep reply comment con
- cho phep sua / xoa mem comment theo quyen chu so huu

### Soft delete

- post: `IsRemove`
- comment: `IsRemove`
- feed va detail loc cac ban ghi da xoa mem

## Trang thai backend du lieu

### Post

- da co `IsRemove`
- da co `UpdateAt`
- da co media attachment qua `PostMedium`

### Comment

- da co `ParentCommentId`
- da co `IsRemove`
- da co `UpdateAt`
- da co cay comment cha - con

### Like

- da co bang `PostLike`
- da co rang buoc tranh like trung

### Share

- da co bang `PostShare`
- share dang luu theo kieu reference
- co dem share va danh sach nguoi share

## Gia tri can giu nguyen

- UI chat cu khong nen bi sua tiep
- page bai viet phai doc lap de de bao tri
- thay doi ve social feed sau nay phai di qua page posts rieng

## Viec can review them truoc khi dong task

1. Kiem tra lai migration / schema SQL Server tren moi truong deploy.
2. Kiem tra quyen truy cap cac hanh dong post/comment/share neu co phan role sau nay.
3. Neu can, bo sung trang `Posts/User/{accountId}` de xem toan bo bai viet cua 1 user.
4. Neu muon UI dep hon, tach them toolbar / filter / composer rieng cho posts.

## Ghi chu

- Module nay da vuot qua buoc lap task, nen file nay hien tai la tai lieu trang thai va checklist review.
- Neu ban chap nhan, buoc tiep theo la on dinh hoa UI/validation va chot migration deployment.
