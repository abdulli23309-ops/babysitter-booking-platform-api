# Babysitter Booking Platform API

A comprehensive **ASP.NET Web API** backend for a smart babysitter booking and baby monitoring system powered by **AI-based baby cry detection** using the YamNet machine learning model.

Built with **ASP.NET Web API 2.0**, **SQL Server**, and **Entity Framework 6**, the platform provides secure babysitter hiring, job management, real-time notifications, availability tracking, and intelligent matching between parents and babysitters.

---

## 🚀 Features

### 👨‍👩‍👧 User Management

* Parent and Babysitter registration & authentication
* Profile management with image uploads
* Secure login system using stored procedures

### 📋 Babysitting Job Management

* Create and manage babysitting jobs
* Babysitters can bid on jobs
* Parents can accept or reject bids
* Job tracking and status updates

### 🤖 Intelligent Matching System

* Smart babysitter-parent matching algorithm
* Matching based on:

  * City
  * Availability
  * Experience
  * Ratings

### 📅 Availability Management

* Babysitters can create hourly availability schedules
* Time-slot based booking system

### 👶 AI Cry Detection

* Integrated YamNet machine learning model
* Detects baby crying from audio input
* Sends alerts and notifications to parents

### 🔔 Real-Time Notifications

* Instant job alerts
* Cry detection notifications
* Booking and bid updates

### ⭐ Ratings & Reviews

* Post-job rating system
* Babysitter reviews and average ratings

### 🖼️ Image Management

* Upload and manage profile pictures for:

  * Parents
  * Babysitters
  * Children

---

# 🏗️ Project Architecture

## 📂 Controllers (API Endpoints)

### ParentController.cs

Handles:

* Parent registration
* Profile updates
* Child management
* Job posting

### BabySitterController.cs

Handles:

* Babysitter registration
* Profile management
* Availability scheduling

### JobsController.cs

Handles:

* Job creation
* Bidding system
* Job status updates

### MatchingController.cs

Handles:

* Intelligent babysitter matching

### CryDetectionController.cs

Handles:

* YamNet audio analysis
* Cry alert generation

### NotificationsController.cs

Handles:

* Real-time notification delivery

### ReviewController.cs

Handles:

* Ratings and review submissions

### ImageController.cs

Handles:

* Image upload and retrieval

### ChildrenController.cs

Handles:

* Child profile management

---

# 🗄️ Database Architecture

The project includes a complete SQL Server database setup located in:

`/database/schema-and-seed.sql`

The database script contains:

* Full database schema
* Relationships
* Triggers
* Stored procedures
* Functions
* Sample seed data

---

## 📊 Database Tables

### Core Tables

* `Parent`
* `Babysitter`
* `Child`
* `Job`
* `Bid`
* `JobTimeSlot`
* `SitterAvailability`
* `TimeSlot`
* `Review`
* `Notification`
* `CryAlert`

---

## ⚙️ SQL Functions

### `fn_CalculateAge(@DOB)`

Automatically calculates age from date of birth.

---

## 🔄 SQL Triggers

### `trg_AutoCalculateSitterAge`

Automatically updates babysitter age.

### `trg_AutoCalculateChildAge`

Automatically updates child age.

### `trg_CheckDuplicateParent`

Prevents duplicate parent registration.

### `trg_CheckDuplicateSitter`

Prevents duplicate babysitter registration.

---

## 🛠️ Stored Procedures

### `sp_UserLogin`

Handles login authentication for:

* Parent
* Babysitter

---

## 🌱 Sample Seed Data

The database includes:

* 4 Parents
* 3 Babysitters
* 4 Children
* Multiple jobs across:

  * Islamabad
  * Rawalpindi
  * Lahore
  * Karachi
* Predefined hourly time slots

---

# 🛠️ Technologies Used

* **Framework:** ASP.NET Web API 2.0
* **Database:** SQL Server
* **ORM:** Entity Framework 6 (Database-First)
* **Machine Learning:** YamNet Model
* **API Documentation:** Swagger / Swashbuckle
* **Language:** C#
* **Database Scripts:** T-SQL

---

# ⚡ Setup Instructions

## 📌 Prerequisites

* Visual Studio 2022+
* SQL Server 2019+
* .NET Framework 4.7.2+
* SQL Server Management Studio (SSMS)

---

# 📥 Installation

## 1️⃣ Clone Repository

```bash
git clone https://github.com/abdulli23309-ops/babysitter-booking-platform-api.git
```

---

## 2️⃣ Setup Database

### Method 1 — Using SQL Server Management Studio (Recommended)

1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Click **New Query**
4. Open:

```bash
database/schema-and-seed.sql
```

5. Click **Execute (F5)**

---

### Method 2 — Using PowerShell

```bash
sqlcmd -S YOUR_SERVER_NAME -i "database/schema-and-seed.sql"
```

Replace:

```bash
YOUR_SERVER_NAME
```

with your SQL Server instance name.

Example:

```bash
LOCALHOST\SQLEXPRESS
```

---

## 3️⃣ Configure Connection String

Open:

```bash
Web.config
```

Update:

```xml
<connectionStrings>
  <add name="Model1"
       connectionString="Server=YOUR_SERVER_NAME;Database=BabysitterDB;Trusted_Connection=true;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

---

## 4️⃣ Open Project

Open:

```bash
WebApplication2.slnx
```

Restore all NuGet packages.

---

## 5️⃣ Update Database

Run in Package Manager Console:

```powershell
Update-Database
```

---

## 6️⃣ Run Application

Press:

```bash
F5
```

API Base URL:

```bash
https://localhost:PORT/
```

Swagger Documentation:

```bash
https://localhost:PORT/swagger/
```

---

# 📡 Key API Endpoints

## 👨 Parent APIs

| Method | Endpoint               | Description           |
| ------ | ---------------------- | --------------------- |
| POST   | `/api/parent/register` | Register Parent       |
| PUT    | `/api/parent/{id}`     | Update Parent Profile |
| POST   | `/api/parent/child`    | Add Child             |

---

## 👩 Babysitter APIs

| Method | Endpoint                       | Description               |
| ------ | ------------------------------ | ------------------------- |
| POST   | `/api/babysitter/register`     | Register Babysitter       |
| PUT    | `/api/babysitter/{id}`         | Update Babysitter Profile |
| POST   | `/api/babysitter/availability` | Set Availability          |

---

## 📋 Job APIs

| Method | Endpoint                | Description      |
| ------ | ----------------------- | ---------------- |
| POST   | `/api/parent/job`       | Create Job       |
| GET    | `/api/matching/find`    | Find Babysitters |
| POST   | `/api/jobs/{jobId}/bid` | Submit Bid       |

---

## 👶 Cry Detection APIs

| Method | Endpoint                              | Description    |
| ------ | ------------------------------------- | -------------- |
| POST   | `/api/crydetection/analyze`           | Analyze Audio  |
| GET    | `/api/crydetection/alerts/{parentId}` | Get Cry Alerts |

---

## ⭐ Review APIs

| Method | Endpoint                            | Description            |
| ------ | ----------------------------------- | ---------------------- |
| POST   | `/api/review/{jobId}`               | Submit Review          |
| GET    | `/api/review/babysitter/{sitterId}` | Get Babysitter Reviews |

---

# 📈 Example SQL Queries

## Get All Parents

```sql
SELECT * FROM Parent;
```

---

## Get Open Jobs

```sql
SELECT * FROM Job WHERE Status = 'Open';
```

---

## Highest Rated Babysitter

```sql
SELECT TOP 1 b.FullName, AVG(r.Rating) AS AvgRating
FROM Babysitter b
LEFT JOIN Review r ON b.Sitter_ID = r.Sitter_ID
GROUP BY b.Sitter_ID, b.FullName
ORDER BY AvgRating DESC;
```

---

## Find Matching Babysitters

```sql
SELECT b.*, sa.AvailableDate
FROM Babysitter b
JOIN SitterAvailability sa ON b.Sitter_ID = sa.Sitter_ID
WHERE sa.City = 'Islamabad'
  AND sa.AvailableDate = '2026-04-12'
ORDER BY b.ExperienceYears DESC;
```

---

# 🔗 GitHub Repositories

## Backend API Repository

GitHub: https://github.com/abdulli23309-ops/babysitter-booking-platform-api

## Frontend Repository

GitHub: https://github.com/abdulli23309-ops/babysitter-booking-platform

---

# 👨‍💻 Author

## Abdullah Saleem

GitHub:
https://github.com/abdulli23309-ops
