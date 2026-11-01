# 📝 Registration Module - Complete Documentation

## Overview

The Registration Module is the **FIRST STEP** in the customer onboarding process. It captures initial interest and contact information before creating a full customer profile.

---

## 🎯 Purpose

**Registration** is a lightweight form to collect basic information from potential customers:
- Full Name
- CNIC (National ID)
- Phone Number
- Email Address

After review and approval, registrations can be converted into full **Customer** records with payment plans and property allotments.

---

## 📋 Module Features

### 1. **Index Page** (`/Registration/Index`)

**Access:** Sidebar → Registrations

#### 📊 Statistics Cards
- **Total Registrations** - All registration records
- **Pending** - Awaiting review/approval
- **Approved** - Approved for customer creation
- **Converted to Customers** - Already converted

#### 🔍 Filters & Search
- **Filter by Status** - Pending, Approved, Rejected, All
- **Search** - By Registration ID, Name, CNIC, Phone, Email
- **Reset** - Clear all filters

#### 📋 Registrations Grid
Columns:
- Registration ID
- Full Name
- CNIC
- Contact (Phone + Email)
- Registration Date
- Status (Badge with icon)
- Customer Link (View Customer button if converted)
- Actions (View, Edit, Delete)

---

### 2. **Create Registration** (`/Registration/Create`)

**Simple Form with 4 Fields:**

**Personal Information:**
- Full Name * (Required)
- CNIC (Optional)

**Contact Information:**
- Phone Number * (Required)
- Email Address (Optional)

**On Submit:**
- Generates unique Registration ID (REG0000001)
- Sets Status to "Pending"
- Sets CreatedAt timestamp
- Logs activity
- Redirects to Index page

---

### 3. **Registration Details** (`/Registration/Details/{id}`)

**Main Panel:**
- Registration ID
- Status (Badge)
- Full Name
- CNIC
- Phone
- Email
- Registration Date

**Right Sidebar:**

**Update Status Card:**
- Shows current status
- Form to change status (Pending/Approved/Rejected)
- Update button
- Logs status change activity

**Customer Link Card:**
- If converted: Shows customer details + View Customer button
- If not converted: Shows "Not converted to customer yet"

---

### 4. **Edit Registration** (`/Registration/Edit/{id}`)

**Editable Fields:**
- Full Name
- CNIC
- Phone
- Email
- Status

**Actions:**
- Save Changes
- Cancel (returns to Details)

---

### 5. **Delete Registration** (`/Registration/Delete/{id}`)

**Warning Screen:**
- Shows all registration details
- Warns that action cannot be undone
- Prevents deletion if linked to customer(s)

**Validation:**
- ❌ Cannot delete if registration has customers
- ✅ Can delete if no customer link exists

---

## 🔄 Process Workflow

```
Step 1: New Registration
   ↓
   Status: Pending
   ↓
Step 2: Admin Review
   ↓
   ├─→ Approved → Can create Customer
   └─→ Rejected → End of process
   
Step 3: Customer Creation
   ↓
   Links Registration to Customer
   Customer gets:
   - Full profile
   - Payment Plan
   - Property eligibility
```

---

## 📊 Database Schema

### Registration Table
```sql
CREATE TABLE Registration (
    RegID CHAR(10) PRIMARY KEY,           -- Auto: REG0000001
    FullName NVARCHAR(150),               -- Required
    CNIC NVARCHAR(50),                    -- Optional
    Phone NVARCHAR(50),                   -- Required
    Email NVARCHAR(150),                  -- Optional
    CreatedAt DATETIME DEFAULT GETDATE(), -- Auto
    Status NVARCHAR(50) DEFAULT 'Pending' -- Pending/Approved/Rejected
);
```

### Relationships
- **Registration → Customer** (One-to-Many)
  - One Registration can have multiple Customers
  - Example: Same person buying multiple plots
  - Each customer entry needs separate Registration ID in practice

---

## 🎨 UI Features

### Index Page
- Purple gradient header
- Statistics cards with icons
- Status filter dropdown
- Search functionality
- DataTables integration (sortable, paginated)
- View Customer button (if converted)

### Forms
- Clean, modern design
- Validation on required fields
- Purple theme consistency
- Icon-based labels

### Details Page
- Organized information display
- Status update sidebar
- Customer link section
- Action buttons (Edit, Back)

---

## 🚀 Usage Examples

### Example 1: Create New Registration
1. Go to **Registrations** (sidebar)
2. Click **New Registration**
3. Enter:
   - Full Name: "John Doe"
   - CNIC: "12345-6789012-3"
   - Phone: "+211 123456789"
   - Email: "john@example.com"
4. Click **Create Registration**
5. Registration created with ID: REG0000001
6. Status: Pending

### Example 2: Approve Registration
1. Open Registration Details
2. In "Update Status" card
3. Select "Approved" from dropdown
4. Click **Update Status**
5. Registration status changed
6. Now eligible for customer creation

### Example 3: Convert to Customer
1. Registration must be Approved
2. Go to **Customers** → **Create Customer**
3. Select Registration from dropdown
4. Registration info auto-fills
5. Add payment plan and additional details
6. Create Customer
7. Customer now linked to Registration

### Example 4: View Converted Registrations
1. Go to **Registrations**
2. In grid, see "View Customer" button
3. Click to see full customer profile
4. Registration now shows as converted

---

## 🔒 Business Rules

### Registration Status
- **Pending** - Initial status, awaiting review
- **Approved** - Reviewed and approved, ready for customer creation
- **Rejected** - Not approved, cannot proceed

### Customer Relationship
- ✅ One Registration can link to multiple Customers
- ✅ Each Customer must have a Registration
- ❌ Cannot delete Registration if it has linked Customer(s)

### Data Validation
- **Required:** Full Name, Phone
- **Optional:** CNIC, Email
- **Auto-Generated:** Registration ID, Created Date, Status

---

## 🛡️ Security & Validation

### Backend
- ✅ Authorization required ([Authorize] attribute)
- ✅ AntiForgeryToken on all forms
- ✅ Model validation
- ✅ Business rule validation (customer link check)

### Frontend
- ✅ Required field validation
- ✅ Email format validation
- ✅ Confirmation dialogs for delete

### Audit Trail
- ✅ All actions logged in ActivityLog
- ✅ User ID captured
- ✅ Timestamps recorded
- ✅ Status changes tracked

---

## 📁 File Structure

```
Controllers/
  └── RegistrationController.cs    (Full CRUD operations + Status update)

Views/
  └── Registration/
      ├── Index.cshtml               (Main grid with filters & stats)
      ├── Create.cshtml              (New registration form)
      ├── Edit.cshtml                (Edit registration)
      ├── Details.cshtml             (View details + Status update)
      └── Delete.cshtml              (Delete confirmation)

Models/
  └── Registration.cs                (Data model - existing)
```

---

## 📈 Statistics Tracked

1. **Total Registrations** - Count of all registrations
2. **Pending** - Awaiting approval
3. **Approved** - Ready for customer creation
4. **Rejected** - Declined registrations
5. **Converted to Customers** - Successfully converted

---

## ✅ Testing Checklist

- [ ] Create new registration
- [ ] View registration in grid
- [ ] Search by name, CNIC, phone
- [ ] Filter by status (Pending, Approved, Rejected)
- [ ] Edit registration details
- [ ] Update status (Pending → Approved)
- [ ] View details page
- [ ] Delete registration (without customer)
- [ ] Try to delete registration with customer (should fail)
- [ ] Create customer from approved registration
- [ ] View customer link from registration details

---

## 🎯 Integration with Other Modules

### With Customer Module
- Registration selected in Customer Create form
- Auto-fills: Name, CNIC, Phone, Email
- Links created upon customer creation

### With ActivityLog
- All registration actions logged
- Status changes recorded
- User attribution maintained

---

## ⚠️ Important Notes

### Deleting Registrations
- ✅ Can delete if NO customers linked
- ❌ Cannot delete if customers exist
- **Error Message:** "Cannot delete registration. It is linked to customer(s)."

### Status Updates
- Can change status at any time
- Status change is logged
- Approved status recommended before customer creation

### ID Generation
- Format: REG + 7 digits (REG0000001)
- Auto-incremented
- Unique per registration

---

## 📞 Common Questions

**Q: Can I create a customer without a registration?**
A: Technically possible in code, but not recommended. Always create registration first.

**Q: What if I reject a registration by mistake?**
A: Simply update the status back to "Approved" from Details page.

**Q: Can I have multiple customers from one registration?**
A: Yes! If same person buys multiple plots, they'll have multiple customer entries but can link to same registration.

**Q: What's the difference between Registration and Customer?**
A: 
- **Registration:** Basic initial record (Name, Phone, Email)
- **Customer:** Full profile with payment plan, property allotment, payment tracking

---

**Status:** ✅ Module Complete and Ready to Use
**URL:** `http://172.20.229.3:8099/Registration`
**Version:** 1.0

