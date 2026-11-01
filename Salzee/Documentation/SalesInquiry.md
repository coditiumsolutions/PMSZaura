# Sales Inquiry Module

## Overview

Manages inbound property inquiries (leads) from potential customers. Staff can track each inquiry through its lifecycle -- from initial contact to conversion or closure. Includes agent assignment, follow-up scheduling, and a performance report.

**Controller:** `SalesInquiryController.cs`
**Model:** `PropertyInquiry.cs`
**Views:** `Views/SalesInquiry/`

---

## Database Table: `PropertyInquiry`

| Column | Type | Notes |
|--------|------|-------|
| InquiryID | INT PK (auto) | Auto-increment identity |
| FullName | NVARCHAR(150) | Inquirer's name (required) |
| PhoneNumber | NVARCHAR(50) | Inquirer's phone (required) |
| EmailAddress | NVARCHAR(150) | Optional email |
| InquiryType | NVARCHAR(100) | Type/category of inquiry |
| Message | NVARCHAR(MAX) | Free-text message from inquirer |
| SubmittedAt | DATETIME | When the inquiry was received |
| IPAddress | NVARCHAR(50) | Source IP (if submitted online) |
| Status | NVARCHAR(50) | `New`, `Contacted`, `Follow-up`, `Converted`, `Closed` |
| AssignedTo | NVARCHAR(100) | Name of the assigned sales agent |
| FollowUpDate | DATETIME | Scheduled follow-up date |
| Notes | NVARCHAR(MAX) | Timestamped internal notes (appended) |
| IsContacted | BIT | Whether the inquirer has been contacted |
| CreatedAt | DATETIME | Auto-set |

---

## Inquiry Lifecycle

```
New  -->  Contacted  -->  Follow-up  -->  Converted
                                      -->  Closed
```

- **New:** Default status on creation.
- **Contacted:** Set when `MarkAsContacted` is called; also sets `IsContacted = true` and appends contact method + timestamp to Notes.
- **Follow-up:** Set when a follow-up date is scheduled.
- **Converted:** Lead became a customer.
- **Closed:** No further action needed.

---

## Key Features

1. **Status Filter:** Index page filters by status; shows dashboard counts (Total, New, Contacted, Converted).
2. **Mark as Contacted:** Records the contact method (Phone, Email, WhatsApp, etc.) with a timestamp in Notes.
3. **Add Notes:** Appends timestamped, user-attributed notes to the inquiry.
4. **Assign To:** Assigns a sales agent from the active users list (by FullName).
5. **Set Follow-Up:** Schedules a follow-up date and moves status to `Follow-up`.
6. **Performance Report:** Groups all inquiries by `AssignedTo` agent and shows counts per status (New, Contacted, Follow-up, Converted, Closed).

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List inquiries with status filter and dashboard counts |
| `UpdateStatus` | Edit | Change inquiry status |
| `MarkAsContacted` | Edit | Flag as contacted with method |
| `AddNotes` | Edit | Append timestamped notes |
| `AssignTo` | Edit | Assign to a sales agent |
| `SetFollowUp` | Edit | Set follow-up date |
| `PerformanceReport` | Read | Sales agent performance breakdown |
| `Delete` | Admin | Permanently remove an inquiry |
