# Hospital Management System (HMS)

A modern, full-stack **Blazor Web App** implemented with **.NET 8+** (Interactive Server Mode) for managing hospital operations, including doctor shifts, patient appointments, and real-time notifications.

## 🚀 Features

### 🏥 Core Modules
*   **Authentication & Authorization**: Secure identity management with Role-Based Access Control (RBAC) for **Doctors** and **Patients**.
*   **Responsive UI**: Built with Bootstrap and custom CSS for a premium, clean aesthetic.

### 👨‍⚕️ Doctor Portal
*   **Dashboard**: Overview of appointments, active cases, and revenue.
*   **Profile Management**: Comprehensive professional profile setup (Specialization, License, Fees).
*   **Shift Management**: Visual tool to add, edit, and manage weekly availability (shifts).
*   **Appointment Handling**:
    *   View pending requests.
    *   **Approve/Reject** appointments with real-time validation.
    *   Instant visual feedback upon action.

### 😷 Patient Portal
*   **Dashboard**: Quick stats and easy navigation.
*   **Appointment Booking**: Search for doctors, view valid shifts, and book appointments.
*   **Real-time Status**:
    *   View "Approved" (Green), "Pending" (Yellow), or "Rejected" (Red) status badges.
    *   **Interactive Notifications**: Clicking the notification bell takes you directly to the relevant update.

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

## 🔄 Recent Updates
*   **Post-Op Handover Module**: Dedicated queue and logic for transferring patients from OT to specialized wards.
*   **Strict Ward Enforcement**: Smart routing engine ensures patients go to specialty wards (e.g. Cardiology) based on surgery type.
*   **Real-time Surgical HUD**: 1-second countdown timers for active surgeries on Admin/OT dashboards.
*   **Persistent Notifications**: Role-based alert system for multi-department coordination.
*   **Revenue Tracking**: Implemented dynamic revenue calculation for doctors (Daily & Total).
*   **Profile Editor**: Added comprehensive "Edit Profile" capability for doctors.
*   **Fixes**: Resolved data mapping issues for Consultation Fees and file upload stability.
*   **Foundation**: Laid the groundwork for a future **Admin Module**.

### 💼 Admin Module (Phase 3 Complete)
*   **Financial Core**:
    *   **Shift Management (Anti-Fraud)**: Strict "Clock In/Out" system for staff. Revenue is tracked per shift and variance reports are auto-generated.
    *   **Doctor Settlements**: Automated commission calculation engine (Revenue Share vs Fixed Salary).
    *   **Shift Reports**: Detailed analytics dashboard comparing System Expected Cash vs Physical Cash Count.
*   **Hospital Facilities**:
    *   Digital Twin of hospital infrastructure (Wards, Rooms, Beds).
    *   Real-time Bed Occupancy tracking (Green=Free, Red=Occupied, Yellow=Maintenance).
*   **Staff Registry**:
    *   Comprehensive HR system to manage Doctors, Nurses, and Admin staff.
    *   Role-Based Access Control logic fully implemented.


---
*Created by [kr1217](https://github.com/kr1217)*
