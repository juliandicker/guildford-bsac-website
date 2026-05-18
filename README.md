# Guildford Sub-Aqua Club Website

ASP.NET Core 8.0 MVC application for the [Guildford BSAC](https://www.guildford-bsac.com) diving club. Hosted on Netcetera via Plesk, served under the path base `/gbsacCore`.

## Architecture

- **HomeController** — main site pages, contact form (reCAPTCHA + Gmail API)
- **TrainingController** — static training course pages
- **YearPlannerController** — year planner view with optional PDF/PNG export via Rotativa
- **FacebookService** — singleton that fetches recent Facebook page posts (in-memory cache, 30 min TTL)
- Static JSON in `App_Data/` — FAQs (`faqs.json`, `faqsContact.json`), membership rates (`membershiprates.json`), and committee members (`team.json`)

See [CONTENT_GUIDE.md](CONTENT_GUIDE.md) for instructions on updating content without code changes.

## Local Development

**Prerequisites:** .NET 8 SDK

```powershell
dotnet run
```

- HTTPS: `https://localhost:7235/gbsacCore`
- HTTP: `http://localhost:5235/gbsacCore`

The app runs without Google or Facebook credentials — calendar and contact form will fail gracefully, Facebook posts section will be empty.

## Secrets

All credentials are stored in Bitwarden under the `GBSAC` folder. Retrieve them from there before working on anything that touches Google APIs or the contact form.

Set up [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local dev:

```powershell
dotnet user-secrets init
dotnet user-secrets set "AppSettings:ServiceAccount:PrivateKey" "<private key from Bitwarden>"
dotnet user-secrets set "AppSettings:RecaptchaSecret" "<secret from Bitwarden>"
dotnet user-secrets set "Facebook:PageAccessToken" "<token from Bitwarden>"
```

In production, these are set as environment variables in Plesk using `__` as the section separator (e.g. `AppSettings__ServiceAccount__PrivateKey`).

## reCAPTCHA

The contact form is protected by [Google reCAPTCHA v2](https://developers.google.com/recaptcha/) ("I'm not a robot" checkbox).

**Admin console:** [google.com/recaptcha/admin](https://www.google.com/recaptcha/admin) — log in with the club Google account to manage keys and view traffic stats.

The site is registered under the domain `guildford-bsac.com`. Two keys are required:

| Key | Where used |
|-----|-----------|
| **Site key** (`RecaptchaSiteKey`) | Embedded in the contact form HTML — safe to be public, stored in `appsettings.json` |
| **Secret key** (`RecaptchaSecret`) | Server-side token validation — must be kept secret, stored in User Secrets / Plesk env var |

To rotate keys or register a new domain:
1. Go to the [reCAPTCHA admin console](https://www.google.com/recaptcha/admin)
2. Select the site → Settings → update domains or generate new keys
3. Update `RecaptchaSiteKey` in `appsettings.json` and `RecaptchaSecret` in User Secrets (dev) and Plesk (production)
4. Update the Bitwarden entry

## Google APIs

**GCP project:** `gbsac-312212` — [console.cloud.google.com](https://console.cloud.google.com)  
**Service account:** `contact-form@gbsac-312212.iam.gserviceaccount.com`  
**Delegated user:** `gbsacadmin@guildford-bsac.com`

The service account has domain-wide delegation and is granted two scopes:
- `https://www.googleapis.com/auth/calendar.readonly` — reading club calendar events
- `https://www.googleapis.com/auth/gmail.send` — sending contact form emails

### Rotating the service account key

If the key is compromised or needs rotating:

1. In GCP Console → IAM → Service Accounts → `contact-form@...` → Keys → Add Key → JSON
2. Update `AppSettings:ServiceAccount:PrivateKey` in User Secrets (dev) and Plesk (production)
3. Delete the old key in GCP Console
4. Update the Bitwarden entry with the new JSON file

### Adding or removing calendar feeds

Calendar IDs are hardcoded in `YearPlannerController.cs` (`_calendarIds`). Each must be shared with `gbsacadmin@guildford-bsac.com` in Google Calendar settings.

## Facebook API

Posts on the home page are fetched from the `GuildfordBSAC` Facebook page using the Graph API v10.0.

The `Facebook:PageAccessToken` in config must be a **Page Access Token** (not a User token). To generate one:

1. Go to [developers.facebook.com](https://developers.facebook.com) → your app → Tools → Graph API Explorer
2. Select the app, then generate a User token with `pages_read_engagement` permission
3. Exchange it for a long-lived Page Access Token using the token debugger
4. Set the token in User Secrets (dev) or Plesk env var (`Facebook__PageAccessToken`) in production

Page tokens generated this way do not expire as long as the user password doesn't change. If posts stop appearing, check the token hasn't been invalidated via the [Token Debugger](https://developers.facebook.com/tools/debug/accesstoken/).
