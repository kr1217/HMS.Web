# 🏥 Operation Workflow & System Architecture

## 📋 **How the Operation Recommendation System Works**

### **Current Workflow:**

```
Doctor → Recommends Operation → Patient Notified → Admin Schedules → Operation Performed
```

### **Detailed Flow:**

#### **Step 1: Doctor Recommends Operation**
- **Who:** Doctor (after consultation/appointment)
- **Where:** `/Doctor/RecommendOperation/{PatientId}`
- **What Happens:**
  1. Doctor selects operation type (from package list or custom)
  2. Sets scheduled date and urgency level
  3. Adds notes about required medicines/equipment
  4. Submits recommendation
  
- **Database Actions:**
  - Creates record in `PatientOperations` table with Status = "Recommended"
  - Links to `DoctorId` and `PatientId`
  - Stores operation details, scheduled date, urgency

- **Notifications:**
  - ✅ **Patient is notified** via `Notifications` table
  - ❌ **Admin is NOT currently notified** (this is a gap!)

#### **Step 2: Patient Views Recommendation**
- **Who:** Patient
- **Where:** `/Patient/Operations` page
- **What Patient Sees:**
  - Operation name and description
  - Scheduled date
  - Urgency level
  - Doctor's notes
  - Estimated cost (if package selected)
  - Status: "Recommended"

- **Patient Actions:**
  - Can view the recommendation
  - Currently **cannot accept/reject** (this is a feature gap!)

#### **Step 3: Admin Schedules Operation** ⚠️ **CURRENT GAP**
- **Problem:** Admin has no dedicated interface to:
  - View pending operation recommendations
  - Approve/schedule operations
  - Assign OT rooms and staff
  - Update operation status

- **How Admin Currently Knows:**
  - ❌ No automatic notification
  - ❌ No dedicated "Pending Operations" dashboard
  - ✅ Can manually query `PatientOperations` table where Status = "Recommended"

---

## 🔧 **Recommended Improvements**

### **1. Admin Operation Management Module**

Create `/Admin/Operations` page with:
- **Pending Recommendations Tab:**
  - List all operations with Status = "Recommended"
  - Show patient name, doctor name, operation type, urgency
  - Action buttons: Approve, Reject, Reschedule

- **Scheduled Operations Tab:**
  - Calendar view of upcoming operations
  - Assign OT rooms, nurses, equipment
  - Track pre-op requirements

- **Completed Operations Tab:**
  - History of performed operations
  - Link to billing (for surgical consumption tracking)

### **2. Enhanced Notification System**

**When Doctor Recommends Operation:**
```csharp
// Notify Patient
NotificationRepo.CreateNotification(new Notification {
    PatientId = PatientId,
    Title = "Operation Recommended",
    Message = $"Dr. {CurrentDoctor.FullName} has recommended: {NewOperation.PackageName}",
    CreatedDate = DateTime.Now,
    IsRead = false
});

// Notify Admin (NEW!)
NotificationRepo.CreateNotification(new Notification {
    UserId = adminUserId, // Or create AdminNotifications table
    Title = "New Operation Recommendation",
    Message = $"Dr. {CurrentDoctor.FullName} recommended {NewOperation.PackageName} for {Patient.FullName}. Urgency: {NewOperation.Urgency}",
    CreatedDate = DateTime.Now,
    IsRead = false
});
```

### **3. Patient Acceptance Workflow**

Add to `/Patient/Operations`:
- **Accept Button:** Patient confirms they want the operation
- **Reject Button:** Patient declines
- **Request Reschedule:** Patient requests different date

**Status Flow:**
```
Recommended → Patient Accepted → Admin Approved → Scheduled → In Progress → Completed
            ↘ Patient Rejected (closes workflow)
```

### **4. Admin Dashboard Integration**

Add to Admin Dashboard:
- **Pending Operations Card:**
  - Count of operations awaiting approval
  - Urgent operations highlighted
  - Quick link to operations management

---

## 🎯 **Current System Capabilities**

### **What Works:**
✅ Doctor can recommend operations
✅ Patient receives notification
✅ Operation details stored in database
✅ Doctor can specify urgency and requirements
✅ System supports both package and custom operations

### **What's Missing:**
❌ Admin notification when operation recommended
❌ Admin interface to approve/schedule operations
❌ Patient ability to accept/reject recommendations
❌ OT room and staff assignment
❌ Pre-op checklist management
❌ Integration with surgical billing (Phase 5)

---

## 💡 **Quick Implementation Guide**

### **Immediate Fix: Admin Notifications**

Add to `RecommendOperation.razor` (line 238):

```csharp
// Get admin user ID
var adminUser = await UserManager.FindByEmailAsync("admin@hospital.com");
if (adminUser != null)
{
    NotificationRepo.CreateNotification(new Notification
    {
        UserId = adminUser.Id, // Requires adding UserId to Notifications table
        Title = "New Operation Recommendation",
        Message = $"Dr. {CurrentDoctor.FullName} recommended {NewOperation.PackageName} for {Patient.FullName}. Urgency: {NewOperation.Urgency}. Scheduled: {NewOperation.ScheduledDate:d}",
        CreatedDate = DateTime.Now,
        IsRead = false
    });
}
```

### **Medium-term: Admin Operations Page**

Create `/Admin/Operations.razor`:
```razor
@page "/admin/operations"
@attribute [Authorize(Roles = "Admin")]

<h3>Operation Management</h3>

<RadzenTabs>
    <Tabs>
        <RadzenTabsItem Text="Pending Recommendations">
            <!-- List operations where Status = "Recommended" -->
            <!-- Show patient, doctor, urgency, scheduled date -->
            <!-- Actions: Approve, Reject, Reschedule -->
        </RadzenTabsItem>
        
        <RadzenTabsItem Text="Scheduled">
            <!-- Calendar view of approved operations -->
            <!-- Assign OT rooms, staff, equipment -->
        </RadzenTabsItem>
        
        <RadzenTabsItem Text="Completed">
            <!-- History with billing links -->
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>
```

---

## 📊 **Database Schema**

### **Current: PatientOperations Table**
```sql
CREATE TABLE PatientOperations (
    OperationId INT PRIMARY KEY IDENTITY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    PackageId INT NULL,
    PackageName NVARCHAR(200),
    ScheduledDate DATETIME NOT NULL,
    Status NVARCHAR(50), -- Recommended, Scheduled, In Progress, Completed, Cancelled
    Urgency NVARCHAR(50), -- Low, Medium, High, Critical
    Notes NVARCHAR(MAX),
    CreatedDate DATETIME DEFAULT GETDATE()
);
```

### **Suggested Enhancement:**
```sql
ALTER TABLE PatientOperations ADD PatientAccepted BIT NULL;
ALTER TABLE PatientOperations ADD AdminApprovedBy NVARCHAR(450) NULL;
ALTER TABLE PatientOperations ADD AdminApprovedDate DATETIME NULL;
ALTER TABLE PatientOperations ADD OTRoomId INT NULL;
ALTER TABLE PatientOperations ADD AssignedNurseId INT NULL;
```

---

## 🚀 **Summary**

**Current State:**
- Doctor recommends → Patient notified → **Admin must manually check database**

**Recommended State:**
- Doctor recommends → Patient notified + **Admin notified** → Patient accepts → **Admin approves and schedules** → Operation performed → **Billing generated (Phase 5)**

**Priority Fixes:**
1. ✅ **HIGH:** Add admin notifications when operations recommended
2. ✅ **HIGH:** Create Admin Operations Management page
3. ✅ **MEDIUM:** Add patient acceptance workflow
4. ✅ **MEDIUM:** Add OT room/staff assignment
5. ✅ **LOW:** Integrate with Phase 5 surgical billing

---

This document explains the current workflow and identifies gaps for future enhancement!
