# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 8.0 MVC web application for the Guildford BSAC (British Sub-Aqua Club) diving club website.

## Build & Run

```powershell
dotnet build
dotnet run
```

- HTTPS: `https://localhost:7235`
- HTTP: `http://localhost:5235`
- App runs under path base `/gbsacCore`

Tests are in `GuildfordBsac.Web.Tests/`. Run with `dotnet test GuildfordBsac.Web.Tests/GuildfordBsac.Web.Tests.csproj`.

## Architecture

### Request Flow

`Program.cs` configures the pipeline: HTTPS redirect → response compression → static files (with `Cache-Control` headers) → security headers/CSP → URL rewriting → routing → MVC. A 301 redirect rewrites legacy `/home/YearPlanner` to `/YearPlanner`. A `/health` endpoint is also mapped.

### Controllers

- **HomeController** — Main site pages (Index, FAQs, PrivacyPolicy, CodeOfConduct, ContactUs). Contact form uses reCAPTCHA (`ReCaptchaValidator`) and a honeypot field to prevent spam, then sends email via `IEmailService`.
- **TrainingController** — Static training course pages.
- **YearPlannerController** — Year planner view with optional PDF export via Rotativa. Year parameter is clamped to current −5/+1.
- **FacebookService** — Singleton that fetches recent Facebook page posts with in-memory caching (not a controller).

### External Integrations

Google API access is split across two service interfaces:
- `IGoogleCalendarClient` / `GoogleApiService` (`Services/`) — calendar event retrieval using service account credentials
- `IEmailService` / `GoogleApiService` (`Services/`) — sending contact form emails via Gmail API

Both are implemented by the same `GoogleApiService` singleton (shared credential).

Secrets (`AppSettings__ServiceAccount__PrivateKey`, `AppSettings__RecaptchaApiKey`, `Facebook__PageAccessToken`) are injected at runtime via **Plesk .NET Settings → Environment Variables** — they are NOT stored in committed files or generated at deploy time.

### Data

Static JSON files in `App_Data/` are loaded by service classes:
- `faqs.json` / `faqsContact.json` → served via `IFaqService` (use `FaqType.General` / `FaqType.Contact`)
- `membershiprates.json` → loaded by `MembershipRatesService` in `Models/`

### Configuration (`appsettings.json`)

Key settings mapped to `Settings.cs` (namespace `GuildfordBsac.Web.Configuration`):
- `RecaptchaSiteKey` — public site key embedded in the contact form HTML
- `RecaptchaApiKey` — Google Cloud API key for reCAPTCHA Enterprise (set via Plesk env var in production)
- `ContactEmail` — destination address for contact form submissions
- `WkHtmlPdfCustomSwitches` — Rotativa CLI options for PDF generation
- `ServiceAccount.PrivateKey` — GCP service account key (set via Plesk env var in production)

### Views

The home page is composed of partial views under `Views/Home/` (e.g., `_home.cshtml`, `_aboutus.cshtml`, `_membership.cshtml`). Three layouts exist: `_Layout.cshtml` (standard), `_Layout_empty.cshtml`, and `_Layout_fullscreen.cshtml`.

Training courses each have individual partial views under `Views/Training/`.

YearPlanner footer is extracted to `Views/YearPlanner/_YearPlannerFooter.cshtml`.

### Key Helpers (`Common/`)

- `CalendarAdapter` — Maps `Google.Apis.Calendar.v3` event objects to internal view models; validates `BackgroundColor` against `#RRGGBB` pattern; skips events with unparseable dates rather than throwing
- `ReCaptchaValidator` (in `CaptchaValidatorAttribute.cs`) — validates reCAPTCHA tokens server-side
- `BrowserJsonFormatter` — Returns `text/html` content-type for JSON when requested from a browser (useful for `/api/calendar/` debugging)
- Extension methods: `HtmlHelperExtensions`, `LinqExtensions`, `DateExtensions`

### Logging

Serilog writes structured logs to rolling daily files at `logs/app-<date>.log` (14-day retention) and to the console. IIS stdout logging is disabled in production (`stdoutLogEnabled="false"`).

## Conventions

- When editing a `.css` file, always check whether a corresponding `.less` file exists and apply the same change there.
- Settings class is in `namespace GuildfordBsac.Web.Configuration` (file: `Settings.cs` at project root).
- FAQ service uses `FaqType` enum, not a filename string.
