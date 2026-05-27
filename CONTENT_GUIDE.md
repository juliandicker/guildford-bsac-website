# Content Update Guide

This guide explains how to update the main content areas of the Guildford BSAC website. No programming knowledge is needed — all changes are made by editing simple text files directly in your browser.

---

## What you need

- A **GitHub account** — sign up free at [github.com](https://github.com) if you don't have one
- To be added as a **collaborator** on the repository — ask the website maintainer to invite you (they do this via Settings → Collaborators on GitHub)

---

## Opening the editor

Click this button to open the editor directly (sign in to GitHub first if prompted):

<a href="https://github.dev/juliandicker/guildford-bsac-website" target="_blank"><img src="https://img.shields.io/badge/Open%20in-github.dev-blue?logo=github" alt="Open in github.dev"></a>

Alternatively, go to [github.com/juliandicker/guildford-bsac-website](https://github.com/juliandicker/guildford-bsac-website) and press the **`.` (full stop) key** on your keyboard.

A code editor opens in your browser — no software to install. It looks like this:

- **Left panel:** file explorer — click folders to expand them, click a file to open it
- **Main area:** the file you're editing
- **Left sidebar icons:** the branch/source-control icon (looks like a Y shape) is how you save changes

---

## Editing a file

1. In the left panel, navigate to the file you want (e.g. `App_Data` → `team.json`)
2. Click the file to open it
3. Make your changes in the main editor area
4. When done, move on to **Saving your changes** below

---

## Saving your changes (publishing to the website)

1. Click the **source control icon** in the left sidebar — it looks like a branching Y shape, and will show a number badge when you have unsaved changes
2. Your changed files appear under **Changes**
3. Click the **`+` icon** next to each file to stage it (moves it to **Staged Changes**)
4. Type a short note in the **Message** box describing what you did — e.g. `Update membership rates for 2025` or `Add new committee member`
5. Click **Commit & Push**

The website will rebuild and go live within about **3 minutes**. You can watch progress in the **Actions** tab back on github.com.

---

## How changes are published

Once you commit and push, GitHub automatically builds and deploys the site. You don't need to do anything else.

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

To upload a new photo:

1. In the left panel, open `wwwroot` → `Content` → `Images` → `team`
2. Right-click the `team` folder and choose **Upload...**
3. Select the photo from your computer
4. The filename you upload must exactly match what you put in `ImgSrc` — use lowercase with underscores, e.g. `jane_smith.jpg`

Photo guidelines:
- Square crops work best (same width and height)
- At least 300×300 pixels; 600×600 is ideal
- Keep file size under 500 KB
- If there's no photo yet, use `placeholder.jpg` as the `ImgSrc` value until one is available

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

