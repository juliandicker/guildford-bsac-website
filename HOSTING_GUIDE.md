# Hosting Guide

## Overview

| | |
|---|---|
| **Domain registrar** | Namecheap |
| **Hosting provider** | Netcetera (Windows shared hosting) |
| **Control panel** | Plesk — accessed via the Netcetera client portal |
| **Server** | IIS on Windows, IP `82.25.22.26` |
| **Live URL** | https://guildford-bsac.com |

---

## Deployments

Deployments are fully automated. You should rarely need to touch Plesk or the server directly.

**How it works:**
1. A code change is committed and pushed to the `main` branch on GitHub
2. GitHub Actions automatically builds the app, injects secrets into `web.config`, and deploys to the server using Web Deploy (MSDeploy)
3. The live site is updated within a few minutes

**To monitor a deployment:**
1. Go to the repository at https://github.com/juliandicker/guildford-bsac-website
2. Click the **Actions** tab
3. The latest workflow run shows whether the deployment succeeded or failed — click it for detailed logs

**If a deployment fails**, check the Actions log first — it will usually tell you exactly what went wrong.

---

## GitHub secrets

The deployment workflow uses the following secrets and variables, stored in the GitHub repository under **Settings → Secrets and variables → Actions**:

| Name | Type | Purpose |
|---|---|---|
| `RECAPTCHA_SECRET` | Secret | reCAPTCHA server-side validation key |
| `SERVICE_ACCOUNT_PRIVATE_KEY` | Secret | Google service account private key (calendar & Gmail) |
| `FACEBOOK_PAGE_ACCESS_TOKEN` | Secret | Facebook page API token |
| `DEPLOY_PASSWORD` | Secret | Netcetera Web Deploy password |
| `DEPLOY_URL` | Variable | Netcetera Web Deploy endpoint URL |
| `DEPLOY_USERNAME` | Variable | Netcetera Web Deploy username |

If any of these are missing or expired, the deployment or the live site will break. The actual values are in the private handover document.

---

## Plesk control panel

Plesk is used for monitoring, logs, and managing the .NET Core app configuration. Day-to-day you won't need it, but it's useful for diagnosing problems.

**Key areas:**

- **Websites & Domains → guildford-bsac.com → .NET Core** — shows the app status, startup file, and environment variables; also lets you enable stdout logging
- **Files** — a browser-based file manager for `httpdocs` (the deployed app folder)
- **Logs** — stdout logs from the app (if enabled) are written to `httpdocs\logs\`

### Checking the site is running

1. Log in to Plesk via the Netcetera client portal
2. Go to **Websites & Domains → guildford-bsac.com → .NET Core**
3. The app status shows whether the process is running

### Reading startup logs

If the site returns a 500 error, enable logging:

1. In the .NET Core panel, under **Logs**, tick **Redirect stdout/stderr to a file** and save
2. Visit the site to trigger a startup attempt
3. Go to **Files → httpdocs → logs** and open the latest `stdout_*.log` file
4. Disable logging again once you've diagnosed the issue (log files grow quickly)

### Restarting the app

If the site is up but behaving unexpectedly, you can restart the app process:

1. In the .NET Core panel, click **Stop** then **Start**

---

## DNS (Namecheap)

The domain `guildford-bsac.com` is registered with Namecheap. DNS is managed there and points to the Netcetera server.

**Login:** username `waterside53`, password in the private handover document. The account has two-factor authentication (2FA) enabled — you will need to set up an authenticator app (Microsoft Authenticator or Google Authenticator) to log in. Ask the previous site owner to add your device during the handover.

**Current DNS records — do not change these without understanding the impact:**

| Type | Host | Value | Purpose |
|---|---|---|---|
| A | `@` | `82.25.22.26` | Points the root domain to the Netcetera server |
| CNAME | `www` | `cp2477.netcetera.co.uk.` | Points the www subdomain to the Netcetera server |
| CNAME | `groups` | `ghs.googlehosted.com.` | Google-hosted subdomain (groups.guildford-bsac.com) |
| CNAME | `library` | `ghs.googlehosted.com.` | Google-hosted subdomain (library.guildford-bsac.com) |

> The `groups` and `library` subdomains point to Google-hosted services. Do not remove them — they are in active use by the club.

**Domain renewal:** check the expiry date in Namecheap and make sure auto-renewal is enabled. Letting the domain lapse would take the website and all club email addresses offline. The renewal date is in the private handover document.

---

## SSL certificate

The site uses a free **Let's Encrypt** certificate managed by Plesk. It covers both `guildford-bsac.com` and `www.guildford-bsac.com`.

| | |
|---|---|
| **Certificate** | Let's Encrypt (free, managed by Plesk) |
| **Covered domains** | guildford-bsac.com, www.guildford-bsac.com |
| **Renewal** | Automatic — Plesk renews it before it expires |
| **HTTP → HTTPS redirect** | Enabled (301 redirect) |

You should not need to do anything to maintain the certificate — Plesk handles renewal automatically. If the site ever shows a certificate warning, go to **Websites & Domains → guildford-bsac.com → SSL/TLS Certificates** in Plesk and click **Reissue Certificate** to force a renewal.

> `webmail.guildford-bsac.com` shows as not secured in Plesk — this is expected and not a concern for the main website.
