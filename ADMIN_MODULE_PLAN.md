# 🏥 SYSTEM ADMIN MODULE — IMPLEMENTATION PLAN

> **Philosophy**: Doctors treat. Patients consume. **Admin controls money, assets, people, risk, and compliance.**

This document tracks the incremental implementation of the Admin Module.

---

## 📅 PHASE 1: FOUNDATION & HR (The "Who")
**Goal:** Establish the Admin Control Plane and manage the workforce.

- [x] **1.1 Admin Infrastructure**
    - [x] Create `AdminLayout` (distinct from Patient/Doctor layouts).
    - [x] Build `Admin/Dashboard` (The "HQ" landing page).
    - [x] Implement robust Role-Based Access Control (RBAC) specifically for Admin routes.

- [x] **1.2 Staff Registry (HR)**
    - [x] Extend `User` model to support new roles: `Admin`, `Nurse`, `Receptionist`.
    - [x] Build **Staff Management UI** (CRUD) to hire/fire/edit staff.
    - [x] Manage Doctor contracts (Fixed Salary vs. Revenue Share configurations).

- [x] **1.3 UI/UX Polish (Technical Debt)**
    - [x] **Fix AdminSidebar**: Create `AdminNavMenu.razor.css` to fix styling, width, and missing hover effects.
    - [x] **Fix Layout Conflicts**: Ensure `AdminLayout` classes don't clash with Patient Layout styles.
    - [x] **Dashboard Cards**: Fix `RadzenCard` padding/height issues to prevent "squashed" look.
    - [x] **Grid Scroll**: Set fixed height for `StaffManagement` grid for better UX.

---

## 🏨 PHASE 2: INFRASTRUCTURE & FACILITIES (The "Where")
**Goal:** Digitalize the physical hospital to manage capacity.

- [x] **2.1 Facility Master Data**
    - [x] Define **Wards / Departments**.
    - [x] Define **Room Types** (e.g., General Ward, Private, ICU) and **Daily Rates**.

- [x] **2.2 Bed Management System**
    - [x] Create **Bed Registry** (mapped to Rooms).
    - [x] Build **Live Bed Status Dashboard** (Green=Free, Red=Occupied, Yellow=Maintenance).
    - [x] Integrate Bed Selection into the Patient Admission flow.
    - [x] Auto-calculate room charges based on Bed Type.

---

## 💰 PHASE 3: FINANCIAL CORE (The "Money") ✅ **COMPLETE**
**Goal:** "Show me exactly where money comes from and where it goes." **(CRITICAL)**

- [x] **3.1 Shift Management (Anti-Fraud)**
    - [x] Implement **Shift Constraints**: Users must "Clock In" to transact. (Clock-In/Out UI with ShiftWidget)
    - [x] **Transaction Tagging**: All payments tagged with `ShiftID` + `OperatorID`.
    - [x] **Shift Closure**: Report generation (Cash vs. Digital comparison) and "Locking" of the shift.
    - [x] **Shift Reporting**: Comprehensive shift reports with variance analysis (`/admin/shift-report`)

- [x] **3.2 Revenue & Payouts**
    - [x] **Doctor Settlement Engine**: Auto-calculate Doctor Payable based on completed appointments & configured commission rates.
    - [x] **Settlement UI**: Process doctor payments with historical tracking (`/admin/settlements`)
    - [ ] **Expense Logging**: Interface to log non-clinical costs (Maintenance, Utilities).

**Phase 3 Deliverables:**
- ✅ Shift Widget (Start/End shift with cash tracking)
- ✅ Transaction tagging for all bills
- ✅ Shift Reports page with variance analysis
- ✅ Doctor Settlements page with commission tracking
- ✅ Doctor Payments history
- ✅ Anti-fraud measures (must clock in to transact)
- ✅ Complete audit trail for financial transactions


---

## 👁️ PHASE 4: OVERSIGHT & AUDIT (The "Flow")
**Goal:** High-level visualization and accountability.

- [ ] **4.1 Operational Oversight**
    - [ ] **OT Schedule View**: Admin view of all upcoming surgeries.
    - [ ] **Discharge Workflow**: Approval gate for ensuring bills are paid before discharge.

- [ ] **4.2 Audit & Compliance**
    - [ ] **Audit Trail**: Immutable logs for sensitive actions (e.g., "Price Override", "Bill Deletion").
    - [ ] **The "CEO Dashboard"**: Aggregated KPIs (Occupancy %, Today's Revenue, Outstanding Dues).

---

## 🏥 PHASE 5: SURGICAL CONSUMPTION & BILLING (The "Bill")
**Goal:** Track every item used in surgery and automate the final invoice.

- [ ] **5.1 Chargeable Items Catalog**
    - [ ] Build **Master Catalog** for Medicines, Surgical Equipment, and Lab tests with unit pricing.
- [ ] **5.2 Surgery Consumption Log**
    - [ ] Create **Consumption Entry UI** for nurses/admins to log items used during a `PatientOperation`.
    - [ ] Implement **Fee Management**: Track Surgeon consultation and OT charges per surgery.
- [ ] **5.3 Financial Integration**
    - [ ] Auto-generate **Surgical Invoice** line items for the final Patient Bill.
    - [ ] Update **CEO Dashboard** to reflect surgery-specific revenue.

---

*Note: General Inventory Management (Stock Levels/Procurement) is out of scope. We focus exclusively on Patient Consumption & Billing.*
