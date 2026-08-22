# LogisticsERP.Web

The Next.js operations portal. The current route is the manager workspace; the employee and operational dashboards can join this shell later without introducing a second frontend application.

## Run locally

1. Start the API on `https://localhost:7112`.
2. Copy `.env.local.example` to `.env.local` if the API uses another address.
3. Run `npm install` and `npm run dev` from this directory.

The API development CORS settings already allow `http://localhost:3000`.

## Production API domain

The public API is hosted at `https://gate.premiumasp.net`. The production build must receive that address at build time:

```powershell
Copy-Item .env.production.example .env.production.local
npm run build
```

`NEXT_PUBLIC_API_BASE_URL` is embedded in the browser bundle, so changing the value requires a new frontend build and deployment.

## Preference and session model

- The app reads the signed-in user from `GET /api/user-profile/me`.
- Language, theme, and density are changed through `PATCH /api/user-profile/me/preferences`, so they are stored in the existing Identity user record and follow the user across devices.
- `ar` applies RTL and Arabic copy; `en` applies LTR and English copy.
- Access and refresh tokens are stored only in `sessionStorage`, not `localStorage`, while the API has a token-body authentication contract. A production BFF with secure `HttpOnly` cookies should replace this browser-side token storage before public deployment.
- The manager route accepts `MANAGER` and `SYSTEM_ADMIN` roles. It also respects the API's required-password-change state before rendering the dashboard.
