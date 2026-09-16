# CLAUDE.md

This file provides guidance to Claude Code when working with the frontend code in this directory.

## Project Structure

```
frontend/
├── app/                    # Next.js App Router (page.tsx, layout.tsx, globals.css)
├── public/                 # Static assets
├── node_modules/           # Dependencies
└── *.config.*             # Configuration files
```

## Common Commands

```bash
# Install dependencies
pnpm install

# Development server (http://localhost:3000)
pnpm dev

# Production build
pnpm build

# Start production server
pnpm start

# Lint
pnpm lint
```

## Tech Stack

- **Next.js 16** with App Router
- **React 19**
- **TypeScript**
- **Tailwind CSS 4**
- **pnpm** for package management

## Development Notes

- API backend runs separately at `http://localhost:5058`
- Use `@/*` path alias for imports (configured in tsconfig.json)
- The `app/` directory uses the Next.js App Router pattern
- ESLint 9 with flat config format
