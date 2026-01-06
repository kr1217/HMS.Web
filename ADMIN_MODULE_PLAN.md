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
    - [x] **New Role**: `Teller` (Financial transactions/Cashier).
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

## 💰 PHASE 3: FINANCIAL CORE (The "Money")
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

- [x] **3.3 Teller Implementation**
    - [x] **Teller Profile**: Profile specific for accepting payments from patients.
    - [x] **Cash Custody**: Tellers are responsible for physical cash handling and shift reconciliation.

- [x] **3.4 Detailed Surgical Billing (Enhanced)**
    - [x] **Detailed Recommendations**: Doctors specify medicines & equipment for operations.
    - [x] **Admin Approval & Costing**: Admin admits patient and sets approved costs for Op, Meds, and Eq.
    - [x] **Automated Discharge Billing**: Final bill auto-populated with approved Surgical costs + Room Rent.
    - [x] **Patient Transparency**: Patients receive bill notifications and view detailed invoices in their portal.

**Phase 3 Deliverables:**
- ✅ Shift Widget (Start/End shift with cash tracking)
- ✅ Transaction tagging for all bills
- ✅ Shift Reports page with variance analysis
- ✅ Doctor Settlements page with commission tracking
- ✅ Doctor Payments history
- ✅ Anti-fraud measures (must clock in to transact)
- ✅ Complete audit trail for financial transactions
- ✅ End-to-End Surgical Billing Flow (Recommendation -> Admin Costing -> Discharge -> Patient View)

---

- [x] **3.5 Enhanced Financial & Operational Workflow (Immediate)**
    - [x] **Automated Teller Shifts**: Auto-start shift on login, auto-close on logout.
    - [x] **Surgical Team Notifications**: Notify doctors and supporting staff upon operation scheduling.
    - [x] **Advance Payment Gate**: Enforce admission deposit payment before bed assignment.
    - [x] **User-Friendly Invoice**: Professional, printable layout for patient bills (HTML/PDF ready).
    - [x] **OT Management & Scheduling**:
        - [x] **OT Registry**: Manage 3-5 Operation Theaters.
        - [x] **Conflict Detection**: Automated check for Surgeon/OT availability before booking.
        - [x] **Waitlist/Queue**: Handling for unavailable resources (Surgeon/OT/Bed).
    - [x] **Advanced OT Scheduling**:
        - [x] **Visual Schedule**: Month/Week view of OT bookings (`/admin/ot-schedule`).
        - [x] **Time-Based Conflicts**: Precise duration-aware conflict detection.
        - [x] **Duration Tracking**: Added DurationMinutes to operations.
        - [x] **Delay Management**: Visual alerts for overdue ops and "Extend Duration" workflow.

---


- [x] **3.6 Workflow Refinement (Operational Polish)**
    - [x] **Global Shift Automation**: Removal of manual start/end shift buttons; automated shift tracking for Admin, Teller, and Doctor.
    - [x] **Decoupled Admission**: Separate "OT Scheduling" from "Ward Admission".
    - [x] **Deposit Workflow**: Admin generates deposit bill -> Teller collects -> Admin confirms -> Patient scheduled.
    - [x] **Logout Fixes**: Robust logout triggers shift closure.

---

- [x] **3.7 Post-Operative Continuity (The "Handover")**
    - [x] **Persistent Post-Op Queue**: Dedicated `/admin/pending-transfers` page to ensure no patient is "lost" after surgery.
    - [x] **Handover State Machine**: Added `IsTransferred` to operations to track handover completion independently of admission.
    - [x] **Strict Ward Enforcement**: Smart routing engine locks patients to their specialty recovery wards (e.g., Cardiology -> Cardio Ward) with automated General Ward fallback.
    - [x] **Real-Time Live Control**: 1-second UI heartbeat for surgical countdowns on both Admin and OT Staff dashboards.
    - [x] **Push Notifications**: Persistent role-based alerts for Admin upon surgery completion.

---

## 👁️ PHASE 4: OVERSIGHT & AUDIT (The "Flow")
**Goal:** High-level visualization, financial accountability, and enterprise-grade settlement.

- [x] **4.1 Operational Oversight**
    - [x] **Pending Ops View**: Admin view of all recommended surgeries for admission (`/admin/operation-requests`).
    - [x] **Discharge Workflow**: Approval gate for ensuring bills are paid before discharge.
    - [ ] **Daily EOD Processing**: Automated "Close Day" to accrue daily room rents for active patients.

- [ ] **4.2 Enterprise Doctor Settlements (The "Audit")**
    - [ ] **Schema Upgrade**: Add `OperatingSurgeonId` to Operations and 3-part Pay Profile (Consult/Rec/Surgery) to Doctors.
    - [ ] **Settlement Ledger**: Create immutable ledger for accruing fees (Pending -> Approved -> Paid).
    - [ ] **Role triggers**: Auto-generate ledger entries for Completed Appointments (Consult Fee) and Operations (Rec/Surgery Fee).
    - [ ] **Approval Workflow**: Admin dashboard to validate patient payment before releasing doctor payout (`/admin/doctor-ledger`).

- [ ] **4.3 Exception & Override Management** & **Audit**
    - [ ] **Override Controls**: mandatory reasons for price/fee overrides; supervisor approval for large variances.
    - [ ] **Audit Trail**: Immutable logs for sensitive actions (e.g., "Price Override", "Bill Deletion").
    - [ ] **The "CEO Dashboard"**: Aggregated KPIs (Occupancy %, Net Revenue, Doctor Payout Efficiency).

---

## 🏥 PHASE 5: PATIENT LIFECYCLE & RECORDS (The "State")
**Goal:** Formalize the patient journey and ensuring compliance.

- [ ] **5.1 Patient Status Machine**
    - [ ] Explicit states: `Registered` -> `Admitted` -> `Surgery Scheduled` -> `Financial Clearance` -> `Discharged`.
    - [ ] **Discharge Block**: Prevent discharge without Financial Clearance (Bed Release + Bill Settlement).
    - [ ] **Advance Payment Integration**: Part of the admission state transition.
    - [ ] **Smart Discharge Summary**: Auto-generate clinical summary from Diagnosis + Surgery + Prescriptions.

- [ ] **5.2 Legal & Consent Management**
    - [ ] Digital Consent Forms (Surgery, Anesthesia) with timestamps.
    - [ ] Document Retention policies (Archival).

---

## ⚙️ PHASE 6: ASSETS, ROSTERS & CONFIG (The "Engine")
**Goal:** Manage physical resources and system rules.

- [ ] **6.1 Medical Equipment Lifecycle**
    - [ ] **Equipment Registry**: Status tracking (Active/Maintenance/Decommissioned).
    - [ ] **Scheduling Conflicts**: Block surgeries if equipment is under maintenance.

- [ ] **6.2 Staff Rosters & Workload**
    - [ ] **Duty Rosters**: Nurse/Staff assignments beyond simple shifts.
    - [ ] **Overtime & Violation Alerts**: Flag over-allocated staff.

- [ ] **6.3 System Configuration & Tariffs**
    - [ ] **Master Config UI**: Pricing rules, tax, defaults managed by Admin (not code).
    - [ ] **Tariff Engine**: Different price lists for General, Corporate, and VIP patients.
    - [ ] **Department Analytics**: Revenue/Occupancy by Department (e.g., Ortho vs Cardio).

---

## 🛡️ PHASE 7: RISK & EXTENSIONS (The "Future")
**Goal:** Enterprise safety and third-party integration.

- [ ] **7.1 Incident & Risk Management**
    - [ ] Incident Reporting (Errors, Falls) & Complaint Registry.
- [ ] **7.2 Insurance & Third-Party Payers**
    - [ ] Insurance Provider Master, Coverage Limits, Co-pay logic.
- [ ] **7.3 Advanced Data Export**
    - [ ] CSV/Excel exports for financial summaries and regulatory reports.

---

*Note: General Inventory Management (Stock Levels/Procurement) remains out of scope unless prioritizing Phase 5/6 intersections.*
