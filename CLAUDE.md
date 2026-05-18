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

No test projects exist in this repository.

## Architecture

### Request Flow

`Program.cs` configures the pipeline: HTTPS redirect → static files → URL rewriting → routing → MVC. A 301 redirect rewrites legacy `/home/YearPlanner` to `/YearPlanner`.

### Controllers

- **HomeController** — Main site pages (Index, FAQs, PrivacyPolicy, CodeOfConduct, ContactUs). Contact form uses reCAPTCHA (`CaptchaValidatorAttribute`) and a honeypot field to prevent spam, then sends email via Gmail API.
- **TrainingController** — Static training course pages.
- **YearPlannerController** — Year planner view with optional PDF export via Rotativa.
- **FacebookService** — Singleton that fetches recent Facebook page posts with in-memory caching (not a controller).

### External Integrations

All Google API access flows through `Common/GoogleApiHelper.cs` which uses service account credentials. It provides authenticated clients for:
- **Google Calendar API** (`Google.Apis.Calendar.v3`) — event retrieval
- **Gmail API** (`Google.Apis.Gmail.v1`) — sending contact form emails

### Data

Static JSON files in `App_Data/` are loaded by service classes:
- `faqs.json` / `faqsContact.json` → served via `HomeController`
- `membershiprates.json` → loaded by `MembershipRatesService` in `Models/`

### Configuration (`appsettings.json`)

Key settings mapped to `Settings.cs`:
- `reCAPTCHA` — site/secret keys for contact form protection
- `ContactEmail` — destination address for contact form submissions
- `PdfSwitches` — Rotativa CLI options for PDF generation

### Views

The home page is composed of partial views under `Views/Home/` (e.g., `_home.cshtml`, `_aboutus.cshtml`, `_membership.cshtml`). Three layouts exist: `_Layout.cshtml` (standard), `_Layout_empty.cshtml`, and `_Layout_fullscreen.cshtml`.

Training courses each have individual partial views under `Views/Training/`.

### Key Helpers (`Common/`)

- `GoogleApiHelper` — Service account authentication for Google APIs
- `CalendarAdapter` — Maps `Google.Apis.Calendar.v3` event objects to internal view models
- `CaptchaValidatorAttribute` — Action filter that validates reCAPTCHA tokens server-side
- `BrowserJsonFormatter` — Returns `text/html` content-type for JSON when requested from a browser (useful for `/api/calendar/` debugging)
- Extension methods: `HtmlHelperExtensions`, `LinqExtensions`, `DateExtensions`
