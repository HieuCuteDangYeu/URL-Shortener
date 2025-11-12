# API Gateway - Simplified Authentication Setup

## Overview
This API Gateway uses **Ocelot** with JWT Bearer authentication. The gateway validates JWT tokens and forwards authenticated requests to downstream services.

## Architecture Principles

### ? What the API Gateway Does
- **Token Validation**: Validates JWT tokens (signature, expiry, issuer, audience)
- **Request Routing**: Routes requests to appropriate microservices
- **Token Forwarding**: Forwards validated JWT tokens to downstream services

### ? What the API Gateway Does NOT Do
- **Authorization**: Role-based authorization is handled by each downstream service
- **Database Queries**: No direct database access - the gateway is stateless
- **Business Logic**: All business logic resides in the microservices

## Configuration

### JWT Settings (appsettings.json)
```json
{
  "JWT": {
    "Secret": "your-secret-key",
    "Issuer": "https://localhost:5004",
    "Audience": "https://localhost:5000"
  }
}
```

### Route Authentication (ocelot.json)
Routes requiring authentication include:
```json
"AuthenticationOptions": {
  "AuthenticationProviderKey": "Bearer",
  "AllowedScopes": []
}
```

## Protected Routes

### Authenticated Routes (Require JWT Token)
- `GET /Url` - Retrieve URLs
- `GET /analytics` - Analytics queries
- `POST /analytics` - Submit analytics
- `GET/POST/PATCH/DELETE /users` - User management
- `POST /auth/revoke` - Revoke tokens

### Public Routes (No Authentication)
- `POST /Url` - Create short URL (public)
- `POST /auth/register` - User registration
- `POST /auth/login` - User login
- `POST /auth/refresh` - Refresh token

## How It Works

1. **Client sends request** with `Authorization: Bearer <token>` header
2. **API Gateway validates token** using JWT middleware
3. **If valid**: Request is forwarded to downstream service with the JWT token
4. **If invalid**: Returns 401 Unauthorized
5. **Downstream service** performs its own authorization checks based on claims in the JWT

## Best Practices

### API Gateway Responsibilities
- Keep the gateway thin and fast
- Only validate JWT tokens
- Let services handle their own authorization

### Downstream Service Responsibilities
- Extract user claims from JWT token
- Implement role-based authorization
- Validate user permissions for specific operations
- Handle business logic and data access

## Example: How Authorization Works

```
Client ? API Gateway ? User Service
  |           |              |
  |     Validates JWT        |
  |      (signature,         |
  |       expiry, etc)       |
  |           |              |
  |    Forwards request      |
  |      with JWT       ? Checks user role
  |                         from JWT claims
  |                              |
  |                         Enforces
  |                         authorization
  |                         (e.g., Admin only)
```

## Token Claims
The downstream services can access these claims from the JWT:
- `sub` or `NameIdentifier` - User ID
- `email` - User email
- `role` - User role(s)
- Custom claims as needed

## Testing

### Without Authentication (Public Endpoint)
```bash
curl -X POST https://localhost:5000/Url \
  -H "Content-Type: application/json" \
  -d '{"originalUrl": "https://example.com"}'
```

### With Authentication
```bash
# Login first
TOKEN=$(curl -X POST https://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}' \
  | jq -r '.token')

# Use token for authenticated request
curl -X GET https://localhost:5000/Url \
  -H "Authorization: Bearer $TOKEN"
```

## Troubleshooting

### 401 Unauthorized
- Token is missing, expired, or invalid
- Check JWT secret, issuer, and audience configuration
- Verify token hasn't expired

### 403 Forbidden
- Token is valid but user lacks required permissions
- This is handled by the downstream service, not the gateway
- Check service-specific authorization logic

## Migration Notes

### What Was Removed
- ? `RoleAuthorizationMiddleware` - Redundant with Ocelot
- ? `DatabaseRoleAuthorizationHandler` - Authorization moved to services
- ? Custom authorization policies - Handled by downstream services
- ? Direct database access from gateway
- ? Complex route-to-policy mappings

### What Was Simplified
- ? Single JWT authentication configuration
- ? Ocelot's built-in authentication
- ? Stateless gateway design
- ? Clear separation of concerns
