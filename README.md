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

**Prerequisites:** .NET 8 SDK, [Visual C++ 2013 Redistributable (x86)](https://aka.ms/highdpimfc2013x86enu)

> The year planner PNG export uses `Rotativa\wkhtmltoimage.exe`, which is a 32-bit binary requiring the VC++ 2013 x86 runtime (`msvcr120.dll` in `C:\Windows\SysWOW64`). Without it the `/YearPlanner/Png` endpoint throws `IOException: The pipe is being closed`. Install via:
> ```powershell
> winget install Microsoft.VCRedist.2013.x86
> ```

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
dotnet user-secrets set "AppSettings:RecaptchaApiKey" "<API key from Bitwarden>"
dotnet user-secrets set "Facebook:PageAccessToken" "<token from Bitwarden>"
dotnet user-secrets set "AppSettings:ContactEmail" "<your email address>"
dotnet user-secrets set "AppSettings:ContactEmailBcc" ""
```

In production, credentials are injected by `deploy.yml` at deploy time into `web.config` from **GitHub Actions secrets** (encrypted) and **GitHub Actions variables** (non-sensitive). To add or rotate a value, update it under **Settings → Secrets and variables → Actions** in GitHub, then re-run the workflow.

| Name | Type | Purpose |
|------|------|---------|
| `SERVICE_ACCOUNT_PRIVATE_KEY` | Secret | GCP service account private key |
| `RECAPTCHA_API_KEY` | Secret | reCAPTCHA Enterprise API key |
| `FACEBOOK_PAGE_ACCESS_TOKEN` | Secret | Facebook Graph API page token |
| `DEPLOY_PASSWORD` | Secret | Plesk Web Deploy password |
| `CONTACT_EMAIL` | Variable | Destination address for contact form submissions |
| `CONTACT_EMAIL_BCC` | Variable | BCC address for contact form submissions (can be blank) |
| `DEPLOY_URL` | Variable | Plesk Web Deploy endpoint URL |
| `DEPLOY_USERNAME` | Variable | Plesk Web Deploy username |
| `SITE_URL` | Variable | Live site URL used by the ZAP security scan |

## Monitoring

The app exposes a health endpoint at:

```
https://www.guildford-bsac.com/health
```

It returns HTTP 200 when all required `App_Data/` files are present, or HTTP 503 if any are missing. The CI/CD pipeline polls this endpoint after every deploy to confirm the app started successfully.

**Uptime monitoring** is provided by [UptimeRobot](https://uptimerobot.com) (free tier, 5-minute interval). The free tier supports one alert email, so whoever is maintaining the site should set up their own personal UptimeRobot account pointing at the URL above. If the site goes down, UptimeRobot sends an email alert automatically.

## reCAPTCHA

The contact form is protected by [Google reCAPTCHA Enterprise](https://cloud.google.com/recaptcha) ("I'm not a robot" checkbox).

**Admin console:** [GCP Console → Security → reCAPTCHA](https://console.cloud.google.com/security/recaptcha) in project `gbsac-312212` — log in with the club Google account to manage keys and view traffic stats.

The site is registered under the domain `guildford-bsac.com`. Two values are required:

| Setting | Where used |
|---------|-----------|
| **Site key** (`RecaptchaSiteKey`) | Embedded in the contact form HTML — safe to be public, stored in `appsettings.json` |
| **API key** (`RecaptchaApiKey`) | Server-side token validation via reCAPTCHA Enterprise Assessment API — must be kept secret, stored in User Secrets / GitHub secret `RECAPTCHA_API_KEY` |

The API key is restricted to the reCAPTCHA Enterprise API only (no HTTP referrer restrictions — server-to-server calls require this).

To rotate keys or register a new domain:
1. Go to [GCP Console → Security → reCAPTCHA](https://console.cloud.google.com/security/recaptcha) in project `gbsac-312212`
2. Select the key → Settings → update domains or create a new key
3. Update `RecaptchaSiteKey` in `appsettings.json` and `Views/Home/_contact.cshtml`
4. Update `RecaptchaApiKey` in User Secrets (dev) and GitHub secret `RECAPTCHA_API_KEY` (production)
5. Update the Bitwarden entry

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

Calendar IDs are stored in `appsettings.json` under `AppSettings.CalendarIds`. Each must be shared with `gbsacadmin@guildford-bsac.com` in Google Calendar settings.

## Facebook API

Posts on the home page are fetched from the `GuildfordBSAC` Facebook page using the Graph API v10.0.

The `Facebook:PageAccessToken` in config must be a **Page Access Token** (not a User token). To generate one:

1. Go to [developers.facebook.com](https://developers.facebook.com) → your app → Tools → Graph API Explorer
2. Select the app, then generate a User token with `pages_read_engagement` permission
3. Exchange it for a long-lived Page Access Token using the token debugger
4. Set the token in User Secrets (dev) or Plesk env var (`Facebook__PageAccessToken`) in production

Page tokens generated this way do not expire as long as the user password doesn't change. If posts stop appearing, check the token hasn't been invalidated via the [Token Debugger](https://developers.facebook.com/tools/debug/accesstoken/).
