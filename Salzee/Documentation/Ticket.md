# Ticket Module (Customer Care / CRO)

## Overview

A simple helpdesk system for recording and tracking customer queries, complaints, and service requests. Tickets are created by CRO (Customer Relations Officer) staff and can be assigned to team members for resolution.

**Controller:** `TicketController.cs`
**Model:** `Ticket.cs`
**Views:** `Views/Ticket/`

---

## Database Table: `Tickets`

| Column | Type | Notes |
|--------|------|-------|
| TicketID | CHAR(10) PK | Random GUID-based 10-char ID (collision-checked) |
| CustomerID | NVARCHAR(150) | Optional customer reference |
| Email | NVARCHAR(150) | Complainant email |
| Contact | NVARCHAR(256) | Complainant phone |
| Title | NVARCHAR(MAX) | Ticket subject |
| Description | NVARCHAR(MAX) | Full description of the issue |
| CROComments | NVARCHAR(MAX) | Timestamped CRO notes (appended) |
| Status | NVARCHAR(256) | See statuses below |
| CreatedBy | NVARCHAR(256) | User who created the ticket |
| AssignedTo | NVARCHAR(256) | User assigned to handle the ticket |
| TicketClosingDate | DATETIME | Set when resolved/discarded/duplicate |
| CreatedAt | DATETIME | Auto-set |

---

## Ticket Statuses

| Status | Meaning |
|--------|---------|
| Pending | Initial state; awaiting assignment |
| Assigned | Ticket has been assigned to someone |
| Ongoing | Work in progress |
| Resolved | Issue resolved; closing date is set |
| Discarded | Not actionable; closing date is set |
| Duplicate | Already exists; closing date is set |

The index page shows a count for each status.

---

## Key Features

1. **Create:** CRO records a new ticket with customer info, title, and description. Status defaults to `Pending`, CreatedBy is the logged-in user's name.
2. **Update Status:** Changes the ticket status. Setting to `Resolved`, `Discarded`, or `Duplicate` auto-sets `TicketClosingDate`.
3. **CRO Comments:** Appends timestamped, user-attributed comments (not a replacement -- always additive).
4. **Assign To:** Assigns a ticket to an active user by name.
5. **Delete:** Permanently removes a ticket (Admin only).

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List tickets with status filter and per-status counts |
| `Create` | Edit | Record a new ticket |
| `UpdateStatus` | - | Change ticket status |
| `AddCROComments` | - | Append CRO notes |
| `AssignTo` | - | Assign to a user |
| `Delete` | Admin | Permanently remove a ticket |
