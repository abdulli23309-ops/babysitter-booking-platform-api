# Babysitter Booking Platform - Backend API

A comprehensive **ASP.NET Web API** backend for a smart babysitter booking and baby monitoring platform with **AI-powered cry detection** using the YamNet machine learning model.

The API provides complete functionality for babysitter hiring, job management, real-time notifications, availability scheduling, review systems, and intelligent babysitter-parent matching.

---

# 🎯 Features

* Parent and Babysitter registration & authentication
* Babysitting job creation and management
* Babysitter bidding and job acceptance system
* Real-time babysitter matching algorithm
* Babysitter hourly availability management
* AI-powered baby cry detection using YamNet
* Real-time notifications and alerts
* Ratings and review system
* Profile image upload support
* Child profile management
* RESTful API architecture with Swagger documentation

---

# 🏗️ Project Architecture

## API Controllers

### `ParentController.cs`

Handles:

* Parent registration
* Profile management
* Child management
* Job posting

### `BabySitterController.cs`

Handles:

* Babysitter registration
* Profile updates
* Availability scheduling

### `JobsController.cs`

Handles:

* Job creation
* Bid management
* Job status updates

### `MatchingController.cs`

Handles:

* Intelligent babysitter matching

### `CryDetectionController.cs`

Handles:

* Audio analysis
* Baby cry detection alerts

### `NotificationsController.cs`

Handles:

* Real-time notifications

### `ReviewController.cs`

Handles:

* Ratings and reviews

### `ImageController.cs`

Handles:

* Image upload and retrieval

### `ChildrenController.cs`

Handles:

* Child profile management

---

# 🗄️ Database

The complete SQL Server database script is included inside the API repository:

Database Folder:
https://github.com/abdulli23309-ops/babysitter-booking-platform-api/tree/main/database

The database includes:

* Complete SQL Server schema
* Tables and relationships
* Sample seed data for testing

## Main Database Tables

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

# 🛠️ Technologies Used

* **Framework:** ASP.NET Web API 2.0
* **Language:** C#
* **Database:** SQL Server
* **ORM:** Entity Framework 6 (Database-First)
* **Machine Learning:** YamNet
* **API Documentation:** Swagger (Swashbuckle)

---

# 🚀 Setup Instructions

## Prerequisites

* Visual Studio 2022+
* .NET Framework 4.7.2+
* SQL Server 2019+
* SQL Server Management Studio (SSMS)

---

# 📥 Installation

## 1. Clone Repository

```bash
git clone https://github.com/abdulli23309-ops/babysitter-booking-platform-api.git
```

---

## 2. Setup Database

Open the SQL file from:

```bash
database/schema-and-seed.sql
```

Execute the script in SQL Server Management Studio (SSMS).

---

## 3. Configure Connection String

Open:

```bash
Web.config
```

Update your SQL Server connection string:

```xml
<connectionStrings>
  <add name="Model1"
       connectionString="Server=YOUR_SERVER;Database=BabysitterDB;Trusted_Connection=true;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

---

## 4. Open Project

Open:

```bash
WebApplication2.slnx
```

Restore all NuGet packages.

---

## 5. Run the Application

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

## Parent APIs

* `POST /api/parent/register` → Register parent
* `PUT /api/parent/{id}` → Update parent profile
* `POST /api/parent/child` → Add child

---

## Babysitter APIs

* `POST /api/babysitter/register` → Register babysitter
* `PUT /api/babysitter/{id}` → Update babysitter profile
* `POST /api/babysitter/availability` → Set availability

---

## Job & Matching APIs

* `POST /api/parent/job` → Create babysitting job
* `GET /api/matching/find` → Find matching babysitters
* `POST /api/jobs/{jobId}/bid` → Submit bid

---

## Cry Detection APIs

* `POST /api/crydetection/analyze` → Analyze baby cry audio
* `GET /api/crydetection/alerts/{parentId}` → Get cry alerts

---

## Review APIs

* `POST /api/review/{jobId}` → Submit review
* `GET /api/review/babysitter/{sitterId}` → Get babysitter reviews

---

# 🤝 Frontend Repository

Frontend Project:
https://github.com/abdulli23309-ops/babysitter-booking-platform

---

# 👨‍💻 Author

Abdullah Saleem

GitHub:
https://github.com/abdulli23309-ops
