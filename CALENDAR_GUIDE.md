# Calendar Guide

## How it works

The Year Planner page on the website reads events directly from five Guildford BSAC Google Calendars using a read-only Google service account. Events are fetched live each time the page loads and displayed by month for the selected year.

The website does **not** store any calendar data — everything comes from Google Calendar. To add, edit, or remove events on the website, you simply update the relevant Google Calendar.

## The five calendars

| Calendar | Google Calendar ID | iCal URL |
|---|---|---|
| GBSAC Hard Boat | `c_eo5g5mq6klkuhoc9us9r7e26fc@group.calendar.google.com` | [Subscribe](https://calendar.google.com/calendar/ical/c_eo5g5mq6klkuhoc9us9r7e26fc%40group.calendar.google.com/public/basic.ics) |
| GBSAC Soft Boat | `c_1v9tf1n7rali13nvf95ak4lc7k@group.calendar.google.com` | [Subscribe](https://calendar.google.com/calendar/ical/c_1v9tf1n7rali13nvf95ak4lc7k%40group.calendar.google.com/public/basic.ics) |
| GBSAC Social | `c_670f59ed3f5r9fbd4molm2225s@group.calendar.google.com` | [Subscribe](https://calendar.google.com/calendar/ical/c_670f59ed3f5r9fbd4molm2225s%40group.calendar.google.com/public/basic.ics) |
| GBSAC Training | `c_2cedqq8lnc5d016ggctuse8nko@group.calendar.google.com` | [Subscribe](https://calendar.google.com/calendar/ical/c_2cedqq8lnc5d016ggctuse8nko%40group.calendar.google.com/public/basic.ics) |
| Tides | `c_3orvtp2a88fve2ffsca6vd8tqc@group.calendar.google.com` | [Subscribe](https://calendar.google.com/calendar/ical/c_3orvtp2a88fve2ffsca6vd8tqc%40group.calendar.google.com/public/basic.ics) |

> **Note:** If you ever need to add a new calendar to the Year Planner, the calendar's ID must be added to `Controllers/YearPlannerController.cs` and the service account must be granted read access to that calendar (see [Sharing a calendar with the service account](#sharing-a-calendar-with-the-service-account) below).

## Adding and editing events

Anyone in the `committee@guildford-bsac.com` Google Group can add and edit events on the club calendars.

1. Go to [calendar.google.com](https://calendar.google.com) and sign in with your club or personal Google account
2. Select the appropriate calendar from the left-hand panel
3. Add, edit, or delete events as normal — make sure you select the correct club calendar, not your personal one
4. Changes will appear on the website the next time the Year Planner page is loaded

> Events that span two months are automatically split and shown in both months on the Year Planner.

## Subscribing to the club calendars on your personal account

All five calendars are public and can be added to any calendar app using their iCal links from the table above. Once subscribed, club events will appear alongside your personal calendar.

> Note: subscribed calendars are read-only — to add or edit events you still need to use the club Google account or have been granted edit access.

### Google Calendar

1. Open [calendar.google.com](https://calendar.google.com)
2. In the left panel, click **+ Other calendars** → **From URL**
3. Paste the iCal URL and click **Add calendar**
4. Repeat for each of the five calendars

Full instructions: [Subscribe to someone else's calendar – Google Calendar Help](https://support.google.com/calendar/answer/37100)

### Outlook

1. In Outlook, go to **Calendar** → **Add calendar** → **Subscribe from web**
2. Paste the iCal URL and click **Import**
3. Repeat for each of the five calendars

Full instructions: [Add a calendar in Outlook.com – Microsoft Support](https://support.microsoft.com/en-us/office/add-a-calendar-in-outlook-com-or-outlook-on-the-web-4e9f6e5b-d07c-41d3-a65e-2b4a53e57c10)

### Yahoo Mail

Full instructions: [Follow and unfollow calendars in Yahoo Mail](https://help.yahoo.com/kb/follow-unfollow-calendars-new-yahoo-mail-sln28241.html)

### Apple Calendar (Mac / iPhone)

Full instructions: [Subscribe to calendars on Mac – Apple Support](https://support.apple.com/en-gb/guide/calendar/icl1022/mac)

## Sharing a calendar with other people

To give someone edit access to add or update events:

1. Go to [calendar.google.com](https://calendar.google.com) and sign in with the club account
2. In the left panel, hover over the calendar and click the three-dot menu → **Settings and sharing**
3. Under **Share with specific people or groups**, click **+ Add people and groups**
4. Enter their Google account email address
5. Choose their permission level:
   - **See all event details** — read only
   - **Make changes to events** — can add and edit events
   - **Make changes and manage sharing** — full control, including sharing with others
6. Click **Send** — they will receive an email invitation

## Sharing a calendar with the service account

The website reads calendars using a Google service account. If you create a new calendar that needs to appear on the Year Planner, you must share it with the service account before adding it to the code.

1. Follow the steps in [Sharing a calendar with other people](#sharing-a-calendar-with-other-people) above
2. Use the service account email address as the recipient (this is stored in the Google Cloud Console — ask the previous site owner if you need it)
3. Grant **See all event details** (read only) — the website never writes to the calendar
4. Add the new calendar's ID to `Controllers/YearPlannerController.cs` and deploy
