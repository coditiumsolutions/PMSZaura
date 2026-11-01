# 🏠 Allotment Module - Complete Documentation

## Overview

The Allotment Module manages the assignment of properties to customers in the PMS system. It enforces the critical business rule: **1 Property = 1 Customer**.

---

## 📋 Module Features

### 1. Create Allotment (`/Allotment/Create`)

**Workflow:**

#### Step 1: Search Customer
- Enter Customer ID and click Search
- System validates:
  - ✅ Customer exists
  - ✅ Customer status is "Active"
  - ✅ Customer does NOT already have a property
  - ✅ Customer has a Payment Plan assigned

**Error Messages:**
- "Customer not found with ID: XXXX"
- "Customer is not Active. Current Status: {Status}"
- "Customer already has a property allotted"
- "Customer does not have a Payment Plan assigned"

#### Step 2: Select Property
- System automatically loads available properties filtered by:
  - ✅ Property Status = "Available"
  - ✅ Property Size = Customer's Registered Size
  - ✅ Property Project = Customer's Payment Plan Project

**Smart Filtering Example:**
- Customer Registered Size: "10 Marla"
- Customer Plan Project: "PROJECT001"
- Result: Only shows 10 Marla properties in PROJECT001 that are Available

**Error Messages:**
- "No available properties found matching Size: {Size} in Project: {Project}"

#### Step 3: Complete Allotment
- Select Allotment Type:
  - Regular
  - Transfer
  - Balloting
  - Special

- Add Comments (Optional)
- Upload Attachment (Optional)
  - Supported formats: PDF, JPG, PNG, DOC, DOCX
  - Stored in: `/wwwroot/uploads/allotments/`

**On Submit:**
1. Creates new Allotment record with unique ID (ALLOT00001)
2. Updates Property Status from "Available" to "Allotted"
3. Saves attachment (if provided)
4. Logs activity in ActivityLog table
5. Redirects to Property Details page

---

### 2. Un-Allot Property (`/Allotment/UnAllot`)

**Features:**
- View all current allotments in a table
- See property details, customer details, dates, status
- Un-allot (cancel) an allotment with reason

**Un-Allot Process:**
1. Click "Un-Allot" button
2. Modal appears with confirmation
3. Enter reason for un-allotment (required)
4. Confirm action

**On Confirm:**
1. Deletes Allotment record
2. Updates Property Status back to "Available"
3. Logs activity with reason in ActivityLog
4. Property becomes available for re-allotment

**⚠️ Warning:** Un-allotment is permanent and cannot be undone!

---

## 🔧 Technical Implementation

### Controller: `AllotmentController.cs`

**Actions:**

1. **Create (GET)**
   - Display allotment creation form

2. **SearchCustomer (POST - AJAX)**
   - Validates customer eligibility
   - Returns customer data as JSON

3. **GetAvailableProperties (POST - AJAX)**
   - Returns filtered properties as JSON
   - Filters by customer's size and project

4. **CreateAllotment (POST)**
   - Validates all conditions
   - Creates allotment record
   - Handles file upload
   - Updates property status
   - Logs activity

5. **UnAllot (GET)**
   - Shows list of all allotments

6. **ProcessUnAllot (POST)**
   - Deletes allotment
   - Updates property status
   - Logs activity with reason

---

### Database Tables Used

#### Allotment Table
```sql
CREATE TABLE Allotment (
    AllotmentID CHAR(10) PRIMARY KEY,
    PropertyID CHAR(10) FOREIGN KEY REFERENCES Property(PropertyID),
    CustomerID CHAR(10) FOREIGN KEY REFERENCES Customers(CustomerID),
    AllottedBy CHAR(10) FOREIGN KEY REFERENCES Users(UserID),
    AllotmentDate DATETIME DEFAULT GETDATE(),
    ApprovedBy NVARCHAR(50),
    AllottmentType NVARCHAR(50),
    WorkFlowStatus NVARCHAR(250),
    Comments NVARCHAR(max),
    AdditionalInfo NVARCHAR(max),
    CONSTRAINT UQ_Allotment_PropertyID UNIQUE (PropertyID)  -- Enforces 1:1 rule
);
```

#### Related Tables
- **Customers:** Customer information
- **Property:** Property information
- **PaymentPlan:** Links customer to project
- **Projects:** Project details
- **Attachments:** File uploads
- **ActivityLog:** Audit trail

---

## 🎯 Business Rules Enforced

### 1. One Property = One Customer
- Database: UNIQUE constraint on `Allotment.PropertyID`
- Code: Validation checks before creating allotment
- UI: Error messages prevent duplicate allotments

### 2. One Customer = One Property
- Code: Checks if customer already has allotment
- UI: Shows error if customer already has property
- Solution: Create new customer entry for additional properties

### 3. Size Matching
- Properties shown must match customer's registered size
- Prevents wrong-sized property allotment
- Ensures customer gets what they paid for

### 4. Project Matching
- Properties shown must be in customer's plan project
- Prevents cross-project allotments
- Maintains project integrity

### 5. Status Validation
- Customer must be "Active"
- Property must be "Available"
- Prevents allotment to inactive/suspended customers

---

## 📁 File Structure

```
Controllers/
  └── AllotmentController.cs       (Business logic)

Views/
  └── Allotment/
      ├── Create.cshtml             (Create allotment UI)
      └── UnAllot.cshtml            (View/cancel allotments)

wwwroot/
  └── uploads/
      └── allotments/               (Uploaded documents)
          └── ALLOT00001_document.pdf

Models/
  └── Allotment.cs                  (Data model - existing)
```

---

## 🚀 How to Use

### For Staff/Admin:

**Creating an Allotment:**

1. Navigate to **Allotment** in sidebar
2. Enter Customer ID (e.g., CUST001)
3. Click **Search**
4. Review customer information
5. Select property from filtered list
6. Choose allotment type
7. Add comments if needed
8. Upload documents (optional)
9. Click **Allot Property**

**Un-Allotting (Cancelling):**

1. Click **Un-Allot Property** button
2. Find the allotment to cancel
3. Click **Un-Allot** button
4. Enter reason for cancellation
5. Click **Confirm Un-Allot**

---

## ⚠️ Important Notes

### Multiple Properties for Same Person

**Wrong Approach:** ❌
- Assign multiple properties to same CustomerID

**Correct Approach:** ✅
- Create NEW Customer entry
- Use NEW RegistrationID
- Assign NEW Payment Plan
- Link to same person but different membership

**Why?**
- One Registration = One Payment Plan = One Property
- Keeps payment tracking clean
- Maintains business logic integrity

### Un-Allotment Impact

When you un-allot:
- ✅ Property becomes available again
- ✅ Customer can be assigned new property
- ✅ Allotment record is deleted (not archived)
- ⚠️ Action cannot be undone

**Best Practice:** 
- Use un-allot only when absolutely necessary
- Document reason thoroughly
- Consider using Transfer module instead if changing ownership

---

## 🔐 Security & Validation

### Backend Validation
- ✅ AntiForgeryToken on all POST requests
- ✅ User authentication required
- ✅ Database constraint enforcement
- ✅ File upload validation
- ✅ SQL injection prevention (EF Core)

### Frontend Validation
- ✅ Required field validation
- ✅ AJAX validation before form submission
- ✅ File type restrictions
- ✅ Confirmation dialogs for destructive actions

### Audit Trail
- ✅ All allotments logged in ActivityLog
- ✅ Un-allotment reasons recorded
- ✅ User ID captured for all actions
- ✅ Timestamps on all records

---

## 📊 Example Workflow

### Scenario: Allotting Property to New Customer

**Customer Details:**
- Customer ID: CUST005
- Name: John Doe
- Status: Active
- Registered Size: 10 Marla
- Payment Plan: Plan001 (Project: Juba Smart City Phase 1)

**Steps:**
1. Admin searches for CUST005
2. System validates: ✅ Active, ✅ No existing property, ✅ Has plan
3. System loads properties: 10 Marla plots in Phase 1 that are Available
4. Admin selects Plot PROP001 (Block A, 10 Marla)
5. Selects type: "Regular"
6. Adds comment: "First allotment to customer"
7. Uploads NOC document
8. Clicks Allot

**Result:**
- Allotment ALLOT00001 created
- PROP001 status: Available → Allotted
- Document saved to `/uploads/allotments/`
- Activity logged
- Admin redirected to Property Details page

---

## 🛠️ Troubleshooting

### "Customer already has a property allotted"
**Solution:** 
- Check if this is correct
- If customer needs another property, create new customer entry
- Use different RegistrationID for the new membership

### "No available properties found"
**Causes:**
- No properties match customer's size
- No properties in customer's project
- All matching properties already allotted

**Solution:**
- Add more properties to the project
- Check customer's registered size is correct
- Check property sizes are correctly entered

### "Property is not available"
**Causes:**
- Property already allotted to another customer
- Property status is not "Available"

**Solution:**
- Un-allot the property first (if appropriate)
- Check property status
- Use different property

---

## ✅ Testing Checklist

- [ ] Search for valid active customer
- [ ] Search for inactive customer (should show error)
- [ ] Search for customer already with property (should show error)
- [ ] Properties filtered correctly by size and project
- [ ] Allotment creates successfully
- [ ] Property status updates to "Allotted"
- [ ] File upload works
- [ ] Attachment saved correctly
- [ ] Activity logged
- [ ] Un-allot removes allotment
- [ ] Property status returns to "Available"
- [ ] Un-allot reason saved in activity log
- [ ] Cannot create duplicate allotment (database constraint)

---

## 📞 Support

For questions or issues with the Allotment Module:
1. Check this documentation first
2. Review business rules in `modules.txt`
3. Check database constraints
4. Review activity logs for audit trail

---

**Status:** ✅ Module Complete and Ready to Use
**Version:** 1.0
**Last Updated:** October 13, 2025





Level Number	COA Level Wise
Level 1	X-00-000-0000-00000
       Level 2	X-XX-000-0000-00000
            Level 3	X-XX-XXX-0000-00000
                 Level 4	X-XX-XXX-XXXX-00000
                        Level 5 is Posting/Entry Account	X-XX-XXX-XXXX-XXXXX


S No	COA Code	Narration	Level #


1	1-00-000-0000-00000	ASSET	1
