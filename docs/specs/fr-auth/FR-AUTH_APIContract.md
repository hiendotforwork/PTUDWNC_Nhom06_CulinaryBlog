# FR-AUTH API Contract (OpenAPI Specification)

> **Tài liệu nguồn:** FR-AUTH_BaoCao.md v1.0.0  
> **Phạm vi:** OpenAPI 3.0 Specification cho module Authentication  
> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 04/06/2026

---

## 1. Tổng quan API

### 1.1. Base Information

| Item | Value |
|------|-------|
| **Base URL** | `https://api.culinaryblog.com/api/v1` |
| **API Version** | v1 |
| **Content-Type** | application/json |
| **Accept** | application/json |
| **Authentication** | Bearer Token (JWT) |

### 1.2. OpenAPI Document

```yaml
openapi: 3.0.3
info:
  title: Culinary Blog - Authentication API
  description: |
    API for user authentication and profile management.
    
    ## Authentication
    Most endpoints require Bearer token authentication. Obtain tokens via:
    - `POST /auth/register` - Register and get tokens
    - `POST /auth/login` - Login and get tokens
    - `POST /auth/google` - Google OAuth login
    
    ## Rate Limiting
    Authentication endpoints are rate limited:
    - 10 requests per minute per IP for /auth/register, /auth/login
    - 30 requests per minute per IP for /auth/refresh
  version: '1.0'
  contact:
    name: Culinary Blog API Support
    email: api-support@culinaryblog.com
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT

servers:
  - url: https://api.culinaryblog.com/api/v1
    description: Production
  - url: https://staging-api.culinaryblog.com/api/v1
    description: Staging
  - url: http://localhost:5000/api/v1
    description: Development

tags:
  - name: Authentication
    description: User registration, login, and token management
  - name: Profile
    description: User profile operations

paths:
```

---

## 2. Authentication Endpoints

### 2.1. Register

```yaml
/auth/register:
  post:
    tags:
      - Authentication
    summary: Register a new user account
    description: |
      Creates a new user account with the Author role.
      Returns tokens for immediate login (auto-login).
      Sends welcome and verification emails asynchronously.
    operationId: register
    security: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/RegisterRequest'
          example:
            displayName: "Nguyễn Văn A"
            email: "user@example.com"
            userName: "nguyenvana"
            password: "Password123!"
    responses:
      '201':
        description: User registered successfully
        headers:
          X-Request-Id:
            schema:
              type: string
            description: Request correlation ID
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AuthResponse'
            example:
              accessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
              refreshToken: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
              expiresAt: "2026-06-04T15:45:00Z"
              user:
                id: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                displayName: "Nguyễn Văn A"
                email: "user@example.com"
                userName: "nguyenvana"
                avatarUrl: null
                roles:
                  - "Author"
      '409':
        $ref: '#/components/responses/Conflict'
      '422':
        $ref: '#/components/responses/ValidationError'
      '429':
        $ref: '#/components/responses/RateLimitExceeded'
```

---

### 2.2. Login

```yaml
/auth/login:
  post:
    tags:
      - Authentication
    summary: Login with email and password
    description: |
      Authenticates user with email and password credentials.
      Returns new access and refresh tokens on success.
      Implements brute force protection with account lockout.
    operationId: login
    security: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/LoginRequest'
          example:
            email: "user@example.com"
            password: "Password123!"
    responses:
      '200':
        description: Login successful
        headers:
          X-Request-Id:
            schema:
              type: string
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AuthResponse'
      '401':
        $ref: '#/components/responses/Unauthorized'
      '423':
        description: Account locked due to too many failed attempts
        content:
          application/problem+json:
            schema:
              $ref: '#/components/schemas/ProblemDetails'
            example:
              type: "https://culinaryblog.com/errors/auth/AUTH_LOGIN_002"
              title: "Account locked"
              status: 423
              detail: "Your account has been locked due to multiple failed login attempts. Please try again after 15 minutes."
              extensions:
                code: "AUTH_ACCOUNT_LOCKED"
                unlockAt: "2026-06-04T15:45:00Z"
                retryAfterSeconds: 900
      '429':
        $ref: '#/components/responses/RateLimitExceeded'
```

---

### 2.3. Google OAuth Login

```yaml
/auth/google:
  post:
    tags:
      - Authentication
    summary: Login or register with Google OAuth
    description: |
      Authenticates or registers a user via Google OAuth 2.0.
      Frontend uses Auth.js v5 to handle the OAuth flow and sends
      the verified ID token to this endpoint.
      
      **Flow:**
      1. Frontend redirects to Google consent screen
      2. Google redirects back with authorization code
      3. Auth.js v5 exchanges code for tokens and verifies
      4. Frontend sends verified ID token to this endpoint
      5. Backend creates account or links to existing email
    operationId: googleLogin
    security: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/GoogleLoginRequest'
          example:
            idToken: "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
            email: "user@gmail.com"
            name: "Google User"
            avatarUrl: "https://lh3.googleusercontent.com/..."
    responses:
      '200':
        description: Google login successful
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AuthResponse'
      '401':
        $ref: '#/components/responses/Unauthorized'
      '502':
        description: Google API unavailable
        content:
          application/problem+json:
            schema:
              $ref: '#/components/schemas/ProblemDetails'
```

---

### 2.4. Refresh Token

```yaml
/auth/refresh:
  post:
    tags:
      - Authentication
    summary: Refresh access token
    description: |
      Exchanges a valid refresh token for a new pair of access and refresh tokens.
      Implements token rotation - the old token is invalidated and a new one issued.
      Detects and alerts on token reuse attacks.
    operationId: refreshToken
    security: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/RefreshTokenRequest'
          example:
            refreshToken: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    responses:
      '200':
        description: Token refreshed successfully
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/AuthResponse'
      '401':
        $ref: '#/components/responses/Unauthorized'
      '429':
        $ref: '#/components/responses/RateLimitExceeded'
```

---

### 2.5. Logout

```yaml
/auth/logout:
  post:
    tags:
      - Authentication
    summary: Logout and revoke refresh token
    description: |
      Logs out the user by revoking the refresh token.
      The access token remains valid until expiration (15 minutes).
      Client should discard the access token.
    operationId: logout
    security:
      - BearerAuth: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/LogoutRequest'
          example:
            refreshToken: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    responses:
      '204':
        description: Logout successful
      '401':
        $ref: '#/components/responses/Unauthorized'
```

---

### 2.6. Verify Email

```yaml
/auth/verify-email:
  get:
    tags:
      - Authentication
    summary: Verify email address
    description: |
      Verifies user's email address using the token sent via email.
      Token expires after 24 hours and is single-use.
      Upon verification, the user's emailConfirmed flag is set to true.
    operationId: verifyEmail
    security: []
    parameters:
      - name: token
        in: query
        required: true
        description: Email verification token
        schema:
          type: string
        example: "abc123def456..."
    responses:
      '200':
        description: Email verified successfully
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/EmailVerificationResponse'
            example:
              message: "Email đã được xác nhận thành công"
              emailConfirmed: true
      '400':
        description: Invalid or expired token
        content:
          application/problem+json:
            schema:
              $ref: '#/components/schemas/ProblemDetails'
```

---

## 3. Profile Endpoints

### 3.1. Get Current User Profile

```yaml
/auth/me:
  get:
    tags:
      - Profile
    summary: Get current user profile
    description: Returns the authenticated user's profile information.
    operationId: getCurrentUser
    security:
      - BearerAuth: []
    responses:
      '200':
        description: Profile retrieved successfully
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UserProfile'
            example:
              id: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              displayName: "Nguyễn Văn A"
              email: "user@example.com"
              userName: "nguyenvana"
              avatarUrl: "https://storage.googleapis.com/avatars/user123.jpg"
              bio: "Yêu thích nấu ăn và chia sẻ công thức"
              roles:
                - "Author"
              emailConfirmed: true
              createdAt: "2026-06-01T10:00:00Z"
      '401':
        $ref: '#/components/responses/Unauthorized'
      '404':
        description: User not found
        content:
          application/problem+json:
            schema:
              $ref: '#/components/schemas/ProblemDetails'
```

---

### 3.2. Update Current User Profile

```yaml
/auth/me:
  patch:
    tags:
      - Profile
    summary: Update current user profile
    description: |
      Updates the authenticated user's profile.
      All fields are optional - only provided fields are updated.
      Email and userName cannot be changed via this endpoint.
    operationId: updateProfile
    security:
      - BearerAuth: []
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/UpdateProfileRequest'
          examples:
            updateDisplayName:
              summary: Update display name only
              value:
                displayName: "Tên Mới"
            updateAvatar:
              summary: Update avatar only
              value:
                avatarUrl: "https://storage.googleapis.com/avatars/new-avatar.jpg"
            updateBio:
              summary: Update bio only
              value:
                bio: "Giới thiệu ngắn về bản thân"
            updateAll:
              summary: Update all fields
              value:
                displayName: "Tên Mới"
                avatarUrl: "https://storage.googleapis.com/avatars/new-avatar.jpg"
                bio: "Giới thiệu ngắn về bản thân"
    responses:
      '200':
        description: Profile updated successfully
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UserProfile'
      '401':
        $ref: '#/components/responses/Unauthorized'
      '422':
        $ref: '#/components/responses/ValidationError'
```

---

## 4. Schemas

### 4.1. Request Schemas

```yaml
components:
  schemas:
    RegisterRequest:
      type: object
      required:
        - displayName
        - email
        - userName
        - password
      properties:
        displayName:
          type: string
          minLength: 2
          maxLength: 100
          description: User's display name
          example: "Nguyễn Văn A"
        email:
          type: string
          format: email
          maxLength: 256
          description: User's email address
          example: "user@example.com"
        userName:
          type: string
          minLength: 3
          maxLength: 30
          pattern: '^[a-zA-Z0-9_]+$'
          description: Unique username (alphanumeric and underscore only)
          example: "nguyenvana"
        password:
          type: string
          format: password
          minLength: 8
          maxLength: 128
          description: |
            Password must contain:
            - At least 8 characters
            - At least 1 uppercase letter
            - At least 1 lowercase letter
            - At least 1 digit
            - At least 1 special character (!@#$%^&*...)
          example: "Password123!"

    LoginRequest:
      type: object
      required:
        - email
        - password
      properties:
        email:
          type: string
          format: email
          description: User's email address
          example: "user@example.com"
        password:
          type: string
          format: password
          description: User's password
          example: "Password123!"

    GoogleLoginRequest:
      type: object
      required:
        - idToken
      properties:
        idToken:
          type: string
          description: Google ID token verified by Auth.js v5
          example: "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
        email:
          type: string
          format: email
          description: Email extracted from Google token (optional)
          example: "user@gmail.com"
        name:
          type: string
          description: Display name from Google profile (optional)
          example: "Google User"
        avatarUrl:
          type: string
          format: uri
          description: Avatar URL from Google profile (optional)
          example: "https://lh3.googleusercontent.com/..."

    RefreshTokenRequest:
      type: object
      required:
        - refreshToken
      properties:
        refreshToken:
          type: string
          description: Valid refresh token
          example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

    LogoutRequest:
      type: object
      required:
        - refreshToken
      properties:
        refreshToken:
          type: string
          description: Refresh token to revoke
          example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

    UpdateProfileRequest:
      type: object
      properties:
        displayName:
          type: string
          minLength: 2
          maxLength: 100
          description: New display name
          example: "Tên Mới"
        avatarUrl:
          type: string
          format: uri
          maxLength: 500
          description: New avatar URL (must be HTTPS)
          example: "https://storage.googleapis.com/avatars/new-avatar.jpg"
        bio:
          type: string
          maxLength: 500
          description: Short biography
          example: "Yêu thích nấu ăn và chia sẻ công thức"
```

### 4.2. Response Schemas

```yaml
    AuthResponse:
      type: object
      properties:
        accessToken:
          type: string
          description: JWT access token (valid for 15 minutes)
          example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        refreshToken:
          type: string
          description: Refresh token (valid for 7 days)
          example: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        expiresAt:
          type: string
          format: date-time
          description: Access token expiration time
          example: "2026-06-04T15:45:00Z"
        user:
          $ref: '#/components/schemas/UserInfo'

    UserInfo:
      type: object
      properties:
        id:
          type: string
          format: uuid
          description: User's unique identifier
          example: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        displayName:
          type: string
          description: User's display name
          example: "Nguyễn Văn A"
        email:
          type: string
          format: email
          description: User's email address
          example: "user@example.com"
        userName:
          type: string
          description: User's username
          example: "nguyenvana"
        avatarUrl:
          type: string
          nullable: true
          description: Avatar URL
          example: "https://storage.googleapis.com/avatars/user123.jpg"
        roles:
          type: array
          items:
            type: string
          description: User's roles
          example: ["Author"]

    UserProfile:
      allOf:
        - $ref: '#/components/schemas/UserInfo'
        - type: object
          properties:
            bio:
              type: string
              nullable: true
              description: User's biography
              example: "Yêu thích nấu ăn và chia sẻ công thức"
            roles:
              type: array
              items:
                type: string
              description: User's roles
              example: ["Author"]
            emailConfirmed:
              type: boolean
              description: Whether email has been verified
              example: true
            createdAt:
              type: string
              format: date-time
              description: Account creation timestamp
              example: "2026-06-01T10:00:00Z"

    EmailVerificationResponse:
      type: object
      properties:
        message:
          type: string
          description: Success message
          example: "Email đã được xác nhận thành công"
        emailConfirmed:
          type: boolean
          description: Confirmation status
          example: true

    ProblemDetails:
      type: object
      properties:
        type:
          type: string
          format: uri
          description: Error type URI
          example: "https://culinaryblog.com/errors/auth/AUTH_REG_001"
        title:
          type: string
          description: Human-readable error title
          example: "Email already registered"
        status:
          type: integer
          description: HTTP status code
          example: 409
        detail:
          type: string
          description: Detailed error message
          example: "The email address 'user@example.com' is already registered."
        instance:
          type: string
          description: Request path that caused the error
          example: "/api/v1/auth/register"
        extensions:
          type: object
          properties:
            code:
              type: string
              description: Application error code
              example: "AUTH_EMAIL_EXISTS"
            timestamp:
              type: string
              format: date-time
              description: Error timestamp
            traceId:
              type: string
              description: Request trace ID for debugging
            errors:
              type: array
              description: Validation errors (for 422 responses)
              items:
                $ref: '#/components/schemas/ValidationError'
            retryAfterSeconds:
              type: integer
              description: Seconds to wait (for rate limit)
            unlockAt:
              type: string
              format: date-time
              description: Account unlock time (for locked account)

    ValidationError:
      type: object
      properties:
        field:
          type: string
          description: Field that failed validation
          example: "password"
        message:
          type: string
          description: Validation error message
          example: "Password must be at least 8 characters with 1 uppercase, 1 digit, and 1 special character."
        code:
          type: string
          description: Validation error code
          example: "PASSWORD_TOO_WEAK"
```

---

## 5. Security Components

```yaml
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: |
        JWT Bearer token authentication.
        Obtain token via POST /auth/login or POST /auth/register.
        
        Usage:
        ```
        Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        ```

  responses:
    Unauthorized:
      description: Unauthorized - Invalid or missing authentication
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_TOKEN_004"
            title: "Access denied"
            status: 401
            detail: "Your access token is invalid or has expired."
            extensions:
              code: "AUTH_ACCESS_TOKEN_INVALID"

    Forbidden:
      description: Forbidden - Insufficient permissions
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_FORBIDDEN"
            title: "Access denied"
            status: 403
            detail: "You do not have permission to access this resource."

    NotFound:
      description: Resource not found
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_PROFILE_001"
            title: "Profile not found"
            status: 404
            detail: "The requested user profile could not be found."

    Conflict:
      description: Conflict - Resource already exists
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_REG_001"
            title: "Email already registered"
            status: 409
            detail: "The email address 'user@example.com' is already registered in the system."
            extensions:
              code: "AUTH_EMAIL_EXISTS"
              field: "email"

    ValidationError:
      description: Validation failed
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_REG_003"
            title: "Validation failed"
            status: 422
            detail: "One or more fields failed validation."
            extensions:
              code: "VALIDATION_ERROR"
              errors:
                - field: "password"
                  message: "Password must be at least 8 characters with 1 uppercase, 1 digit, and 1 special character."
                  code: "PASSWORD_TOO_WEAK"
                - field: "displayName"
                  message: "Display name must be at least 2 characters."
                  code: "DISPLAYNAME_TOO_SHORT"

    RateLimitExceeded:
      description: Rate limit exceeded
      headers:
        Retry-After:
          schema:
            type: integer
          description: Seconds to wait before retrying
          example: 60
        X-RateLimit-Limit:
          schema:
            type: integer
          description: Maximum requests allowed
          example: 10
        X-RateLimit-Remaining:
          schema:
            type: integer
          description: Remaining requests in current window
          example: 0
        X-RateLimit-Reset:
          schema:
            type: integer
          description: Unix timestamp when the limit resets
          example: 1717500060
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://culinaryblog.com/errors/auth/AUTH_RATE_001"
            title: "Rate limit exceeded"
            status: 429
            detail: "Too many requests. Please wait 60 seconds before trying again."
            extensions:
              code: "RATE_LIMIT_EXCEEDED"
              retryAfterSeconds: 60
              limit: 10
              window: "1 minute"
```

---

## 6. Rate Limiting Headers

All authentication endpoints include rate limit headers in responses:

```yaml
# Example Response Headers
X-RateLimit-Limit: 10
X-RateLimit-Remaining: 9
X-RateLimit-Reset: 1717500060
```

### Rate Limits by Endpoint

| Endpoint | Limit | Window | Scope |
|----------|-------|--------|-------|
| POST /auth/register | 10 | 1 minute | Per IP |
| POST /auth/login | 10 | 1 minute | Per IP |
| POST /auth/google | 10 | 1 minute | Per IP |
| POST /auth/refresh | 30 | 1 minute | Per IP |
| POST /auth/logout | 10 | 1 minute | Per IP |
| GET /auth/me | 60 | 1 minute | Per IP |
| PATCH /auth/me | 10 | 1 minute | Per IP |

---

## 7. Example API Calls

### 7.1. Registration (cURL)

```bash
curl -X POST https://api.culinaryblog.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: abc123" \
  -d '{
    "displayName": "Nguyễn Văn A",
    "email": "user@example.com",
    "userName": "nguyenvana",
    "password": "Password123!"
  }'
```

### 7.2. Login (cURL)

```bash
curl -X POST https://api.culinaryblog.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: def456" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'
```

### 7.3. Get Profile (cURL)

```bash
curl -X GET https://api.culinaryblog.com/api/v1/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "X-Correlation-Id: ghi789"
```

### 7.4. Update Profile (cURL)

```bash
curl -X PATCH https://api.culinaryblog.com/api/v1/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: jkl012" \
  -d '{
    "displayName": "Tên Mới",
    "bio": "Cập nhật giới thiệu bản thân"
  }'
```

---

## 8. Postman Collection

A Postman collection is available at:

```
/docs/postman/FR-AUTH.postman_collection.json
```

### Import and Configure

1. Import the collection into Postman
2. Create an environment with variables:
   - `baseUrl`: `https://api.culinaryblog.com/api/v1`
   - `accessToken`: (will be set by Login request)
   - `refreshToken`: (will be set by Login request)
3. Run the collection's "Auth Flow" folder to test registration, login, and profile operations

---

**Document Status:** Complete  
**Last Updated:** 04/06/2026  
**Reviewed by:** ________________  
**Date:** ________________
