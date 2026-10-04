# Hướng dẫn tạo Google OAuth 2.0 Credentials

## Bước 1: Tạo Project trên Google Cloud Console

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo project mới hoặc chọn project hiện có
3. Đặt tên project: "Culinary Blog"

## Bước 2: Enable Google+ API

1. Vào **APIs & Services** > **Library**
2. Tìm "Google+ API"
3. Click **Enable**

## Bước 3: Configure OAuth Consent Screen

1. Vào **APIs & Services** > **OAuth consent screen**
2. Chọn **External**
3. Điền thông tin:
   - App name: Culinary Blog
   - User support email: your-email@gmail.com
   - Developer contact: your-email@gmail.com
4. Click **Save and Continue**

## Bước 4: Tạo OAuth 2.0 Credentials

1. Vào **APIs & Services** > **Credentials**
2. Click **Create Credentials** > **OAuth client ID**
3. Application type: **Web application**
4. Name: "Culinary Blog Web Client"
5. Authorized redirect URIs:
   - Development:
     - `http://localhost:5058/signin-google` (ASP.NET Core Google OAuth middleware callback)
     - `http://localhost:5058/api/v1/auth/google-callback`
   - Production:
     - `https://api.culinaryblog.com/signin-google`
     - `https://api.culinaryblog.com/api/v1/auth/google-callback`
6. Click **Create**
7. Copy **Client ID** và **Client Secret**

## Bước 5: Cấu hình trong ứng dụng

### Development (dotnet user-secrets)
```bash
cd src/CulinaryBlog.API
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOCSPX-YOUR_SECRET"
```

### Production (Environment Variables)
```bash
export Authentication__Google__ClientId="YOUR_CLIENT_ID.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="GOCSPX-YOUR_SECRET"
```

## Troubleshooting

### Lỗi "invalid_client"
- Kiểm tra Client ID và Client Secret đã đúng chưa
- Đảm bảo đã enable Google+ API

### Lỗi "redirect_uri_mismatch"
- Kiểm tra redirect URI trong Google Cloud Console khớp với callback URL
- Development: `http://localhost:5058/signin-google`

### Lỗi "access_denied"
- User đã từ chối cấp quyền
- Thử đăng nhập lại
