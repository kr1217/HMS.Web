# Hospital Management System (HMS)

A modern, full-stack **Blazor Web App** implemented with **.NET 8+** (Interactive Server Mode) for managing hospital operations, including doctor shifts, patient appointments, and real-time notifications.

## 🚀 Features

### 🏥 Core Modules
*   **Authentication & Authorization**: Secure identity management with Role-Based Access Control (RBAC) for **Admin**, **Doctors**, **Patients**, and **Tellers**.
*   **Responsive UI**: Built with Radzen Blazor and custom Enterprise CSS for a premium, clean aesthetic.

### 🏛️ Admin Operational Dashboard (Command Center)
*   **Real-Time Command Center**: Operational dashboard for orchestration (not just reporting).
*   **Surgical Workflow Control**: Live countdowns, theater heatmaps, and extended duration alerts.
*   **Patient Flow Monitoring**: Managed queues for Admissions, Post-Op Transfers, and Financial Clearance.
*   **Intelligence & Analytics**:
    *   **Patient Loss Intelligence**: Automated tracking of lost revenue and rejected procedural requests.
    *   **Operational KPIs**: UT utilization, Bed occupancy rates, and Financial throughput per hour.
*   **Administrative Actions**: Authorize/Reject surgery requests with standardized reasoning and notifications.

### 👨‍⚕️ Doctor Portal
*   **Dashboard**: Overview of appointments, active cases, and revenue.
*   **Profile Management**: Comprehensive professional profile setup (Specialization, License, Fees).
*   **Shift Management**: Visual tool to add, edit, and manage weekly availability (shifts).
*   **Surgical Recommendations**: Direct interface to recommend complex procedures for administrative approval.
*   **Appointment Handling**:
    *   View pending requests.
    *   **Approve/Reject** appointments with real-time validation.
    *   Instant visual feedback upon action.

### 💰 Teller & Finance Module
*   **Shift Management**: Clock-in/Clock-out with cash drawer reconciliation and shift revenue auditing.
*   **Advance Payments**: Secure collection of surgery deposits with automated workflow triggers (OT Scheduling).
*   **Settlement Engine**: Doctor commission auditing and payment processing.

### 😷 Patient Portal
*   **Dashboard**: Quick stats and easy navigation.
*   **Appointment Booking**: Search for doctors, view valid shifts, and book appointments.
*   **Real-time Status**:
    *   View "Approved" (Green), "Pending" (Yellow), or "Rejected" (Red) status badges.
    *   **Interactive Notifications**: Clicking the notification bell takes you directly to the relevant update (Surgery status, Payments, Rejections).

## 🛠️ Tech Stack
*   **Framework**: ASP.NET Core Blazor (Interactive Server)
*   **Language**: C# / .NET 9.0 (or 8.0)
*   **Database**: Microsoft SQL Server (ADO.NET / Stored Procedures / Direct Queries)
*   **Styling**: Bootstrap 5, Bootstrap Icons, Custom CSS
*   **State Management**: Circuit-based server-side state

## ⚙️ Setup & Installation

1.  **Prerequisites**:
    *   [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later)
    *   Microsoft SQL Server

2.  **Database Configuration**:
    *   Update the connection string in `HMS.Web/appsettings.json`:
        ```json
        "ConnectionStrings": {
          "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=HospitalManagement;Integrated Security=True;TrustServerCertificate=True;"
        }
        ```
    *   Ensure the database `HospitalManagement` exists or let EF Core / SQL scripts initialize it.

3.  **Run the Application**:
    ```bash
    cd HMS.Web
    dotnet run
    ```
    The application will start at `http://localhost:5139`.

## 🧪 Usage Workflow

1.  **Register a Doctor**: Sign up -> Complete Profile -> Add Shifts.
2.  **Register a Patient**: Sign up -> Complete Profile.
3.  **Book Appointment**: Patient logs in -> Books slot during Doctor's shift.
4.  **Approve**: Doctor logs in -> Sees request -> Clicks Approve.
5.  **Notify**: Patient sees notification -> Clicks to view confirmed status.

## 📂 Project Structure
*   `HMS.Web`: Main Blazor Server project.
*   `HMS.Web/Components/Pages`: Razor components for UI (Doctor/, Patient/, etc.).
*   `HMS.Web/DAL`: Data Access Layer (Repositories) for direct SQL interaction.
*   `HMS.Web/Models`: Entity definitions.

---
*Created by [kr1217](https://github.com/kr1217)*

## 🌟 Key Capabilities
### 🔌 Doctor ↔ Patient Communication Model
The system enforces a strict separation of concerns where doctors and patients **never** communicate directly. All interaction is mediated through shared domain entities, ensuring security, auditability, and distinct role management.

*   **Appointment Workflow**: 
    *   Patient requests (Status: Pending) → Doctor Approves/Rejects/Reschedules → Patient Notified.
*   **Prescriptions**: Doctor creates and digitally signs → Patient views and downloads (Immutable).
*   **Medical Reports**: Doctor uploads reports → Patient downloads safely.
*   **Operations**: Doctor recommends procedures with estimated costs → Patient reviews details.

### 👨‍⚕️ Doctor Portal (Business Logic)
*   **Dashboard Analytics**: 
    *   Real-time view of **Revenue Today**, **Total Revenue**, **Active Cases**, and **Completed Appointments**.
    *   Visual "Today's Schedule" and "Pending Requests" overview.
*   **Shift Management**: Doctors define their availability (Days/Times), which validates all incoming patient requests.
*   **Clinical Tools**:
    *   **Prescribe**: Digital prescription generation with medicine, dosage, and notes.
    *   **Reports**: Secure file upload for lab results and radiology.
    *   **Operations**: Recommendation engine for surgical procedures.
*   **Profile Control**: Full control over professional details, consultation fees, and contact info.

### 😷 Patient Portal
*   **Zero-Friction Booking**: Smart booking system that only shows slots during a doctor's active shifts.
*   **Medical History**: Centralized view of all past prescriptions, reports, and operation recommendations.
*   **Live Updates**: Real-time status badges and notifications for all interactions.

## 🛡️ Engineering & Architecture
### Robustness & Error Handling
We have implemented a **Defense-in-Depth** strategy to ensure high availability and user confidence:
*   **Comprehensive Exception Handling**: All critical paths (Payment, Booking, Data Loading) are wrapped in `try-catch` blocks to degrade gracefully rather than crash.
*   **User Feedback Loops**: Integrated `NotificationService` across all user-facing components to provide immediate, human-readable feedback.
*   **Self-Documenting Codebase**: Every single file features a standardized header detailing its **Purpose** and **Dependencies**.

### Performance Optimizations
*   **Background Services**: Heavy maintenance tasks (like Daily Revenue Accrual) are offloaded to `HospitalBackgroundService`.
*   **Smart Querying**: Repositories use targeted SQL queries with simplified indexing strategies.
*   **Asynchronous I/O**: Full adoption of `async/await` patterns in Data Access Layer (DAL).

## 🔄 Recent Updates & Roadmap
### ✅ Completed Features
*   **Admin Command Center**: Real-time surgical monitoring with theater heatmaps and automated delays tracking.
*   **Patient Loss System**: Integrated audit trail for rejected surgeries and revenue leakage (Patient Loss Events).
*   **Patient Operations**: Resilient interfaces for `Appointments`, `Bills`, and `Dashboard`.
*   **Teller Operations**: Secure cash handling workflows for `CollectAdvance` and `PaymentDialog` with Shift reconciliation.
*   **Enterprise Features**: Real-time Bed/OT utilization KPIs, Automated Doctor Notifications, and Shift Management.

### 🧠 Engineering Principles
1.  **Data Integrity**: Financial transactions are atomic. Consistency > Availability in billing modules.
2.  **Validation First**: All inputs validated at UI and Repository layers.
3.  **Query Optimization**: Strict adherence to `SELECT [Columns]` over `SELECT *`.
4.  **Maintainability**: Code explains "Why" it exists via comprehensive headers.

---
*Maintained by [kr1217](https://github.com/kr1217)*
