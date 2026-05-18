# Content Update Guide

This guide explains how to update the three main content areas of the Guildford BSAC website. No programming knowledge is needed — all changes are made by editing simple text files.

---

## How changes are published

1. Make your changes to the files described below
2. Commit and push to GitHub (the `main` branch)
3. GitHub automatically builds and deploys the site to the server — this takes about 2–3 minutes

You can watch the deployment progress in the **Actions** tab on GitHub.

---

## Committee Members

**File:** `App_Data/team.json`

Each committee member is one block in the list. Here's an example:

```json
{
  "Name": "Jane Smith",
  "ImgSrc": "jane_smith.jpg",
  "Role": "Chairperson",
  "Description": "A short sentence about the role."
}
```

### To edit a member's details
Find their block and change the relevant value. For example, to update a role:
```json
"Role": "New Role Title"
```

### To add a new member
Copy an existing block, paste it at the end of the list, and fill in the details. Make sure there is a comma after the `}` of the previous entry:

```json
  {
    "Name": "Sally Evans",
    "ImgSrc": "sally_evans.jpg",
    "Role": "Welfare Officer",
    "Description": "Responsible for ensuring the safety, wellbeing & inclusion of all members."
  },
  {
    "Name": "New Person",
    "ImgSrc": "new_person.jpg",
    "Role": "Their Role",
    "Description": "Description of what they do."
  }
```

Note: no comma after the **last** entry in the list.

### To remove a member
Delete their entire block (from `{` to `}`), including the comma before or after it.

### Photos
- Photo files go in `Content/Images/team/`
- Use lowercase filenames with underscores, e.g. `jane_smith.jpg`
- The `ImgSrc` value must exactly match the filename
- Recommended size: roughly square, at least 300×300 pixels

---

## FAQs

There are two FAQ files:

| File | Used on |
|------|---------|
| `App_Data/faqs.json` | The main FAQs page |
| `App_Data/faqsContact.json` | The Contact Us page |

Each FAQ is a block like this:

```json
{
  "Index": 5,
  "Question": "How much does it cost?",
  "Answer": "It costs £X per month."
}
```

The `Answer` field can contain basic HTML such as `<p>`, `<a href="...">`, `<ul>`, `<li>`. Plain text also works fine.

### To edit a question or answer
Find the block and change the `"Question"` or `"Answer"` value.

### To add a new FAQ
Copy an existing block, paste it at the end of the list, change the `"Index"` to the next number, and fill in the question and answer. Remember the comma rules (comma after every block except the last).

### To remove an FAQ
Delete the entire block for that FAQ, including the comma before or after it.

---

## Membership Rates

**File:** `App_Data/membershiprates.json`

**Important:** When rates change, always **add a new entry** to the list — do not edit the existing ones. The site automatically uses the most recent entry whose `EffectiveDate` is in the past. Old entries remain as a historical record.

**You can add next year's rates in advance.** Set the `EffectiveDate` to a future date (e.g. `"2026-03-01T00:00:00"`) and the site will continue showing the current rates until that date, then switch over automatically. This means you can enter the rates right after the AGM and not have to remember to update the site in March.

Each entry looks like this:

```json
{
  "EffectiveDate": "2024-03-01T00:00:00",
  "AgmApprovedDate": "2023-10-24T00:00:00",
  "BsacFullMemberAnnualRate": 68.50,
  "ClubJoiningFee": 50.00,
  "ClubJoiningFeeWithTraining": 75.00,
  "DiveCrewCost": 545.00,
  "ClubMembershipMonthlyRate": 17.50
}
```

### Field guide

| Field | What it is |
|-------|-----------|
| `EffectiveDate` | Date the new rates take effect (usually 1 March) |
| `AgmApprovedDate` | Date the AGM approved the rates |
| `BsacFullMemberAnnualRate` | Annual BSAC membership fee (£) |
| `ClubJoiningFee` | One-off joining fee without training (£) |
| `ClubJoiningFeeWithTraining` | One-off joining fee including training package (£) |
| `DiveCrewCost` | Cost of external fast-track training via Divecrew (£) |
| `ClubMembershipMonthlyRate` | Monthly club membership fee (£) |

The site calculates the annual total and example costs automatically from these values.

### Date format
Always use this format: `"YYYY-MM-DDT00:00:00"` — for example `"2025-03-01T00:00:00"`.

---

## JSON rules (important!)

If the file is invalid JSON the site will show an error. The most common mistakes:

- **Commas between items, but not after the last one** — every `}` except the final one needs a comma after it
- **Quotes around text values** — `"Name": "Jane Smith"` not `Name: Jane Smith`
- **No quotes around numbers** — `"Rate": 68.50` not `"Rate": "68.50"`

**Before saving, paste the file contents into [jsonlint.com](https://jsonlint.com) to check for errors.**

---

## Setting up GitHub deployment (one-time setup)

The deployment uses GitHub Actions with FTP credentials stored as secrets. These are already set up if the site is deploying automatically. If you need to reconfigure them:

1. Go to the repository on GitHub → **Settings** → **Secrets and variables** → **Actions**
2. Add the following secrets:

| Secret name | Value |
|-------------|-------|
| `FTP_HOST` | FTP hostname (from Plesk, e.g. `ftp.guildford-bsac.com`) |
| `FTP_USERNAME` | FTP username |
| `FTP_PASSWORD` | FTP password |
| `FTP_REMOTE_PATH` | Remote folder path (e.g. `/httpdocs/gbsacCore/`) |

The `appsettings.json` file (which contains API keys) is **excluded** from deployment — it lives permanently on the server and is not overwritten when you push changes.
