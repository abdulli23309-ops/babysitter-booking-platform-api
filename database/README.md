# Database Setup

## Files

- `schema-and-seed.sql` - Complete database schema, functions, triggers, stored procedures, and sample data

## How to Create the Database

### Method 1: Using SQL Server Management Studio (SSMS)

1. Open **SQL Server Management Studio**
2. Connect to your SQL Server instance
3. Click **New Query**
4. Open the file: `database/schema-and-seed.sql`
5. Click **Execute** (F5)

### Method 2: Using PowerShell

```powershell
sqlcmd -S YOUR_SERVER_NAME -i "database/schema-and-seed.sql"
```

Replace `YOUR_SERVER_NAME` with your SQL Server instance (e.g., `LOCALHOST\SQLEXPRESS`)

## Database Structure

### Tables
- **Parent** - Guardian user accounts
- **Babysitter** - Babysitter user accounts
- **Child** - Children registered by parents
- **Job** - Babysitting job requests
- **Bid** - Babysitter bids on jobs
- **JobTimeSlot** - Time slots for each job
- **SitterAvailability** - Babysitter availability slots
- **TimeSlot** - Predefined hourly time slots (8 AM - 10 PM)
- **Review** - Job reviews and ratings
- **Notification** - User notifications
- **CryAlert** - Baby cry detection alerts

### Key Functions
- `fn_CalculateAge(@DOB)` - Calculates age from date of birth

### Key Triggers
- `trg_AutoCalculateSitterAge` - Auto-updates babysitter age
- `trg_AutoCalculateChildAge` - Auto-updates child age
- `trg_CheckDuplicateParent` - Prevents duplicate parent registrations
- `trg_CheckDuplicateSitter` - Prevents duplicate babysitter registrations

### Key Stored Procedures
- `sp_UserLogin` - Handles login for both Parent and Babysitter roles

## Sample Data

The script includes sample data:
- 4 Parents (Ali Khan, Sara Ahmed, Usman Tariq, Hina Malik)
- 3 Babysitters (Ayesha Khan, Fatima Noor, Zara Ali)
- 4 Children with various special requirements
- 7 Time Slots (8 AM - 10 PM in 2-hour blocks)
- 44 Job requests in various cities (Lahore, Karachi, Islamabad, Rawalpindi)

## Query Examples

### Get all parents
```sql
SELECT * FROM Parent;
```

### Get all available jobs
```sql
SELECT * FROM Job WHERE Status = 'Open';
```

### Get babysitter with highest average rating
```sql
SELECT TOP 1 b.FullName, AVG(r.Rating) AS AvgRating
FROM Babysitter b
LEFT JOIN Review r ON b.Sitter_ID = r.Sitter_ID
GROUP BY b.Sitter_ID, b.FullName
ORDER BY AvgRating DESC;
```

### Find matching babysitters for a job
```sql
SELECT b.*, sa.AvailableDate
FROM Babysitter b
JOIN SitterAvailability sa ON b.Sitter_ID = sa.Sitter_ID
WHERE sa.City = 'Islamabad' 
  AND sa.AvailableDate = '2026-04-12'
ORDER BY b.ExperienceYears DESC;
```

## Resetting Database

To clear all data and reseed:

```sql
-- Drop tables and recreate
DROP TABLE CryAlert;
DROP TABLE Review;
DROP TABLE Notification;
DROP TABLE Bid;
DROP TABLE JobTimeSlot;
DROP TABLE SitterAvailability;
DROP TABLE Job;
DROP TABLE TimeSlot;
DROP TABLE Child;
DROP TABLE Babysitter;
DROP TABLE Parent;

-- Then re-run the schema script
```

## Connection String

Update your `Web.config` with:

```xml
<connectionStrings>
  <add name="Model1" 
       connectionString="Server=DESKTOP-XXXXX\SQLEXPRESS;Database=BabysitterDB;Trusted_Connection=true;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

Replace `DESKTOP-XXXXX\SQLEXPRESS` with your SQL Server instance name.