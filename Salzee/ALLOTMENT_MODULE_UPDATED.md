# 🏠 Allotment Module - Updated Structure

## ✅ Module Complete with 3 Pages

### 1. **Index Page** (`/Allotment/Index`) - Main Dashboard ⭐

**Location:** Sidebar → Allotment (default landing page)

**Features:**

#### 📊 Statistics Cards (Top Row)
- **Total Allotments** - Count of all property allotments
- **Allotted Properties** - Properties currently allotted
- **Available Properties** - Properties ready for allotment
- **Pending Approval** - Allotments awaiting approval

#### 🔍 Filters & Search
- **Filter by Project** - Dropdown to filter allotments by project
- **Filter by Status** - Filter by workflow status (Pending Approval, Approved, Rejected)
- **Search Bar** - Search by Allotment ID, Customer ID, Property ID, Customer Name, or Plot Number
- **Reset Button** - Clear all filters

#### 📋 Allotments Grid (Table)
Displays all allotments with columns:
- Allotment ID
- Property (Plot No, Block, Size)
- Customer (Name, Customer ID)
- Project (Badge)
- Type (Regular, Transfer, Balloting, Special)
- Date (Date and time)
- Status (Badge with icon)
- Actions (View Property, View Customer)

#### 📈 Chart: Property Allocation by Project
- **Bar Chart** showing Allotted vs Not Allotted properties per project
- Visual breakdown of property allocation status
- Helps identify which projects need more allotments
- Interactive tooltips with total properties count

---

### 2. **Create Allotment** (`/Allotment/Create`)

**Access:** Click "Create Allotment" button from Index page

**3-Step Wizard:**

#### Step 1: Search Customer
- Enter Customer ID
- Click Search
- System validates eligibility
- Shows customer details if valid

#### Step 2: Select Property
- Auto-loads filtered properties
- Filtered by customer's size and project
- Shows property details

#### Step 3: Complete Allotment
- Select allotment type
- Add comments
- Upload attachment (optional)
- Submit

**Navigation:**
- "View All Allotments" - Returns to Index
- "Un-Allot Property" - Goes to UnAllot page

---

### 3. **Un-Allot Property** (`/Allotment/UnAllot`)

**Access:** Click "Un-Allot Property" button from Index or Create page

**Features:**
- Table of all current allotments
- Shows property and customer details
- Un-allot button with confirmation modal
- Requires reason for un-allotment
- Activity logged

**Navigation:**
- "View All Allotments" - Returns to Index
- "Create Allotment" - Goes to Create page

---

## 🎯 Navigation Flow

```
Sidebar → Allotment
    ↓
Index Page (Main Dashboard)
    ├─→ Create Allotment Button → Create Page
    │       ├─→ View All Allotments → Back to Index
    │       └─→ Un-Allot Property → UnAllot Page
    │
    └─→ Un-Allot Property Button → UnAllot Page
            ├─→ View All Allotments → Back to Index
            └─→ Create Allotment → Create Page
```

---

## 📊 Chart Details

**Type:** Grouped Bar Chart (Chart.js)

**Data Displayed:**
- X-axis: Project Names
- Y-axis: Number of Properties
- Green Bars: Allotted Properties
- Blue Bars: Not Allotted (Available) Properties
- Tooltip: Shows total properties on hover

**Example:**
```
Project: Juba Smart City Phase 1
├── Allotted: 45 properties
├── Not Allotted: 35 properties
└── Total: 80 properties
```

**Benefits:**
- Quick visual overview of allocation status
- Identify projects needing attention
- Track allocation progress
- Management reporting

---

## 🎨 UI Features

### Index Page
- Purple gradient header
- Modern statistics cards with icons
- Advanced filtering capabilities
- Sortable table columns
- Responsive design
- Interactive chart with hover tooltips

### Filters
- Project dropdown (All Projects or specific)
- Status dropdown (All, Pending Approval, Approved, Rejected)
- Search box (instant filter)
- Reset button (clear all filters)

### Actions in Grid
- 🏠 View Property - Opens Property Details
- 👤 View Customer - Opens Customer Details

---

## 🚀 Usage Examples

### Example 1: View All Allotments in a Project
1. Go to Allotment module (sidebar)
2. Select project from "Filter by Project" dropdown
3. Grid automatically updates
4. Chart shows allocation for that project

### Example 2: Find Pending Approvals
1. Go to Allotment module
2. Select "Pending Approval" from "Filter by Status"
3. See all allotments waiting for approval
4. Click View Property/Customer for details

### Example 3: Search for Specific Allotment
1. Go to Allotment module
2. Type in search box (e.g., "CUST005" or "Plot A-123")
3. Grid filters instantly
4. Click actions to view details

### Example 4: Check Allocation Progress
1. Go to Allotment module
2. Scroll to chart section
3. See visual breakdown per project
4. Identify projects with low allocation rates

---

## 📁 Files Updated

### New Files:
- ✅ `Controllers/AllotmentController.cs` - Added Index action
- ✅ `Views/Allotment/Index.cshtml` - Main dashboard page
- ✅ `Views/Allotment/Create.cshtml` - 3-step wizard
- ✅ `Views/Allotment/UnAllot.cshtml` - Cancel allotments

### Updated Files:
- ✅ `Views/Shared/_Layout.cshtml` - Menu points to Index
- ✅ `modules.txt` - Updated menu structure

---

## ✅ Ready to Test

**Main URL:** `http://172.20.229.3:8099/Allotment` or `/Allotment/Index`

**Test Flow:**
1. Click "Allotment" in sidebar → Should open Index page
2. See statistics cards at top
3. See filters and search
4. See grid of allotments (if any exist)
5. Scroll down to see chart
6. Click "Create Allotment" → Opens Create wizard
7. Click "Un-Allot Property" → Opens UnAllot page

---

## 📊 Chart Library

**Using:** Chart.js (CDN loaded in Index page)
- Modern, responsive charts
- Interactive tooltips
- Mobile-friendly
- No additional installation needed

---

**Status:** ✅ Complete and Ready to Use!
**Main Entry Point:** Sidebar → Allotment → Index Page

