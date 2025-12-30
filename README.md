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
