# Babysitter Booking Platform - Backend API

ASP.NET Web API with SQL Server database for a comprehensive babysitter booking and management system with AI-powered cry detection.

## 🎯 Features

- **User Management**: Parent and Babysitter registration & authentication
- **Job Management**: Create, bid, accept, and manage babysitting jobs
- **Real-time Matching**: Intelligent babysitter-parent matching algorithm
- **Availability Management**: Babysitters can set hourly availability slots
- **Cry Detection**: YamNet ML model integration for baby cry detection
- **Notifications**: Real-time job alerts and updates
- **Ratings & Reviews**: Post-job review and rating system
- **Image Management**: Profile picture uploads for parents, babysitters, and children

## 🏗️ Project Architecture

### Controllers (API Endpoints)
- `ParentController.cs` - Parent profile & job management
- `BabySitterController.cs` - Babysitter profile & availability
- `JobsController.cs` - Job creation, bidding, and status updates
- `MatchingController.cs` - Intelligent matching algorithm
- `CryDetectionController.cs` - ML-based cry detection
- `NotificationsController.cs` - Real-time notifications
- `ReviewController.cs` - Ratings and reviews
- `ImageController.cs` - Image upload/retrieval
- `ChildrenController.cs` - Child profile management

### Data Models
- **Parent** - Guardian/parent user
- **Babysitter** - Babysitter user  
- **Child** - Child under parent's care
- **Job** - Babysitting job request
- **Bid** - Babysitter bids on jobs
- **SitterAvailability** - Babysitter hourly availability
- **CryAlert** - Cry detection alerts
- **Review** - Job reviews and ratings
- **Notification** - User notifications

## 🛠️ Technologies Used

- **Framework**: ASP.NET Web API 2.0
- **Database**: SQL Server with Entity Framework 6
- **ORM**: Entity Framework (Database-First approach)
- **ML Integration**: YamNet model for cry detection
- **API Documentation**: Swagger (Swashbuckle)

## 🚀 Setup Instructions

### Prerequisites
- Visual Studio 2022+
- .NET Framework 4.7.2+
- SQL Server 2019+

### Installation

1. **Clone the repository**
```bash
   git clone https://github.com/abdulli23309-ops/babysitter-booking-platform-api.git
```

2. **Update Database Connection**
   - Open `Web.config`
   - Update connection string to your SQL Server:
```xml
   <connectionStrings>
     <add name="Model1" connectionString="Server=YOUR_SERVER;Database=BabysitterDB;Trusted_Connection=true;" 
          providerName="System.Data.SqlClient" />
   </connectionStrings>
```

3. **Open in Visual Studio**
   - Open `WebApplication2.slnx`
   - Restore NuGet packages

4. **Update Database**
   - Package Manager Console: `Update-Database`

5. **Run the Application**
   - Press `F5` to start
   - API: `https://localhost:PORT/`
   - Swagger UI: `https://localhost:PORT/swagger/`

## 📡 Key API Endpoints

### Parent
- `POST /api/parent/register` - Register parent
- `PUT /api/parent/{id}` - Update profile
- `POST /api/parent/child` - Add child

### Babysitter
- `POST /api/babysitter/register` - Register babysitter
- `PUT /api/babysitter/{id}` - Update profile
- `POST /api/babysitter/availability` - Set availability

### Jobs & Matching
- `POST /api/parent/job` - Create job
- `GET /api/matching/find` - Find babysitters
- `POST /api/jobs/{jobId}/bid` - Submit bid

### Cry Detection
- `POST /api/crydetection/analyze` - Analyze audio for crying
- `GET /api/crydetection/alerts/{parentId}` - Get cry alerts

### Reviews
- `POST /api/review/{jobId}` - Submit review
- `GET /api/review/babysitter/{sitterId}` - Get babysitter reviews

## 👨‍💼 Author

Abdullah Saleem - [@abdulli23309-ops](https://github.com/abdulli23309-ops)

## 🤝 Frontend Repository

[babysitter-booking-platform](https://github.com/abdulli23309-ops/babysitter-booking-platform)
