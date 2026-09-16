# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Culinary Blog** is a Vietnamese food recipe sharing platform with three main modules:

| Module | Owner | Description |
|--------|-------|-------------|
| FR-AUTH | Nguyễn Ngọc Thanh Hiền | Authentication & User Management |
| FR-SRCH | Nguyễn Hưng Thịnh | Search & Pagination |
| FR-RCP | Ngô Văn Chương | Recipe Management |
| FR-CAT | Nguyễn Văn An | Category Management |

## Monorepo Structure

```
/
├── frontend/               # Next.js 16 + TypeScript + Tailwind CSS
│   ├── app/               # App Router (pages, layouts, components)
│   ├── public/            # Static assets
│   └── package.json       # Dependencies (managed by pnpm)
│
└── src/                   # ASP.NET Core Backend
    ├── CulinaryBlog.API/
    ├── CulinaryBlog.Application/
    ├── CulinaryBlog.Domain/
    └── CulinaryBlog.Infrastructure/
```

## Common Commands

### Frontend (Next.js)
```bash
cd frontend
pnpm install              # Install dependencies
pnpm dev                  # Development server (http://localhost:3000)
pnpm build                # Production build
pnpm start                # Start production server
pnpm lint                 # Run ESLint
```

### Backend (.NET)
```bash
# Build the entire solution
dotnet build

# Run the API (Development: http://localhost:5058)
dotnet run --project src/CulinaryBlog.API

# Add a migration
cd src/CulinaryBlog.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../CulinaryBlog.API
```

## Architecture

### Frontend Stack
- **Next.js 16** with App Router
- **React 19**
- **TypeScript**
- **Tailwind CSS 4**
- **pnpm** for package management
- Path alias: `@/*` maps to project root

### Backend Stack (Clean Architecture)
```
src/
├── CulinaryBlog.API           # Presentation Layer (Controllers, Middleware)
├── CulinaryBlog.Application   # Application Layer (CQRS Commands/Queries via MediatR)
├── CulinaryBlog.Domain        # Domain Layer (Entities, Value Objects)
└── CulinaryBlog.Infrastructure # Infrastructure Layer (EF Core, PostgreSQL, MinIO, Redis)
```

- **.NET 10** with ASP.NET Core Web API
- **MediatR** for CQRS pattern
- **Mapster** for object mapping
- **Entity Framework Core 10** with **Npgsql** for PostgreSQL

## Important Files

| Path | Purpose |
|------|---------|
| `frontend/app/` | Next.js App Router directory |
| `frontend/CLAUDE.md` | Frontend-specific guidance |
| `src/CulinaryBlog.API/Program.cs` | API entry point |
| `src/CulinaryBlog.Infrastructure/` | DbContext, Entity configurations |
| `docs/specs/fr-auth/` | API contracts, test plans, security specs |

## API Documentation

OpenAPI endpoint available at `/openapi` (Scalar UI) when backend runs in Development mode.

## Development Notes

- **Frontend**: `http://localhost:3000` (Next.js dev server)
- **Backend**: `http://localhost:5058` (ASP.NET Core)
- PostgreSQL, Redis, MinIO configs needed in `appsettings.Development.json` (gitignored)
- Frontend and backend run as separate processes
