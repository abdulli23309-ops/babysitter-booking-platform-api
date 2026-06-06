CREATE TABLE Parent (
    Parent_ID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(150) NOT NULL,
    EmailAddress NVARCHAR(150) UNIQUE NOT NULL,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL, -- Store hashed passwords in production
    PhoneNumber NVARCHAR(20),
    PictureAddress NVARCHAR(MAX), -- URL or file path for the frontend
    Address NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Table for Babysitter Registration and Login
CREATE TABLE Babysitter (
    Sitter_ID INT PRIMARY KEY IDENTITY(1,1),
    FullName NVARCHAR(150) NOT NULL,
    EmailAddress NVARCHAR(150) UNIQUE NOT NULL,
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Password NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20),
    PictureAddress NVARCHAR(MAX),
    DOB DATE NOT NULL,
    Age INT, -- Managed by trigger
    ExperienceYears INT,
    HourlyRate DECIMAL(10, 2),
    AvailabilityStatus NVARCHAR(50) DEFAULT 'Available',
    CreatedAt DATETIME DEFAULT GETDATE()
);
CREATE TABLE Child (
    Child_ID INT PRIMARY KEY IDENTITY(1,1),
    Parent_ID INT FOREIGN KEY REFERENCES Parent(Parent_ID),
    ChildName NVARCHAR(100) NOT NULL,
    DOB DATE NOT NULL,
    Age INT, -- Automatically calculated via trigger
    Gender NVARCHAR(20),
    PictureAddress NVARCHAR(MAX), -- For the "Set Child Profile" screen
    SpecialRequirements NVARCHAR(MAX)
);
CREATE TABLE Job (
    Job_ID INT PRIMARY KEY IDENTITY(1,1),
    Parent_ID INT FOREIGN KEY REFERENCES Parent(Parent_ID),
    Child_ID INT FOREIGN KEY REFERENCES Child(Child_ID),
    Title NVARCHAR(200),
    Description NVARCHAR(MAX),
    JobDate DATE NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Open' -- Open, Assigned, Completed, Cancelled
);
CREATE TABLE Notification (
    Notification_ID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL, -- ID of Parent or Sitter
    UserRole NVARCHAR(20) NOT NULL, -- 'Parent' or 'Sitter'
    Message NVARCHAR(MAX),
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);


-- 5. Create Bid Table (Linked to Job and Babysitter)
CREATE TABLE Bid (
    Bid_ID INT PRIMARY KEY IDENTITY(1,1),
    Job_ID INT FOREIGN KEY REFERENCES Job(Job_ID),
    Sitter_ID INT FOREIGN KEY REFERENCES Babysitter(Sitter_ID),
    ProposedPrice DECIMAL(10, 2),
    BidStatus NVARCHAR(50) DEFAULT 'Pending', -- Pending, Accepted, Rejected
    BidDate DATETIME DEFAULT GETDATE()
);

CREATE FUNCTION fn_CalculateAge (@DOB DATE)
RETURNS INT
AS
BEGIN
    DECLARE @Age INT;
    -- DATEDIFF alone isn't accurate for birthdays, so we subtract 1 if the birthday hasn't happened yet this year
    SET @Age = DATEDIFF(YEAR, @DOB, GETDATE()) - 
               CASE 
                   WHEN (MONTH(@DOB) > MONTH(GETDATE())) OR 
                        (MONTH(@DOB) = MONTH(GETDATE()) AND DAY(@DOB) > DAY(GETDATE())) 
                   THEN 1 
                   ELSE 0 
               END;
    RETURN @Age;
END;
GO

CREATE TRIGGER trg_AutoCalculateSitterAge
ON Babysitter
AFTER INSERT, UPDATE
AS
BEGIN
    -- Only run the update if the DOB column was touched
    IF UPDATE(DOB)
    BEGIN
        UPDATE Babysitter
        SET Age = dbo.fn_CalculateAge(i.DOB)
        FROM Babysitter b
        INNER JOIN inserted i ON b.Sitter_ID = i.Sitter_ID;
    END
END;
GO

CREATE TRIGGER trg_AutoCalculateChildAge
ON Child
AFTER INSERT, UPDATE
AS
BEGIN
    IF UPDATE(DOB)
    BEGIN
        UPDATE Child
        SET Age = dbo.fn_CalculateAge(i.DOB)
        FROM Child c
        INNER JOIN inserted i ON c.Child_ID = i.Child_ID;
    END
END;
GO

-- Trigger for Parent Table
CREATE TRIGGER trg_CheckDuplicateParent
ON Parent
INSTEAD OF INSERT
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM Parent p 
        JOIN inserted i ON p.EmailAddress = i.EmailAddress OR p.Username = i.Username
    )
    BEGIN
        RAISERROR('Registration Failed: A user with this Email or Username already exists.', 16, 1);
        ROLLBACK TRANSACTION;
    END
    ELSE
    BEGIN
        INSERT INTO Parent (FullName, EmailAddress, Username, Password, PhoneNumber, Address, PictureAddress)
        SELECT FullName, EmailAddress, Username, Password, PhoneNumber, Address, PictureAddress FROM inserted;
    END
END;
GO

-- Trigger for Babysitter Table
ALTER TRIGGER trg_CheckDuplicateSitter
ON Babysitter
AFTER INSERT
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM Babysitter b 
        JOIN inserted i 
        ON b.EmailAddress = i.EmailAddress 
        OR b.Username = i.Username
        WHERE b.Sitter_ID <> i.Sitter_ID
    )
    BEGIN
        RAISERROR('Registration Failed: A babysitter with this Email or Username already exists.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;

ALTER PROCEDURE sp_UserLogin
    @InputUser NVARCHAR(50),
    @InputPass NVARCHAR(255),
    @Role NVARCHAR(20) 
AS
BEGIN
    SET NOCOUNT ON;

    IF @Role = 'Parent'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Parent WHERE Username = @InputUser)
        BEGIN
            SELECT CAST(NULL AS INT) AS UserID, 'User not found' AS Message;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM Parent WHERE Username = @InputUser AND Password = @InputPass)
        BEGIN
            SELECT CAST(NULL AS INT) AS UserID, 'Wrong password' AS Message;
            RETURN;
        END

        SELECT 
            Parent_ID AS UserID,
            FullName,
            EmailAddress,
            Username,
            Password,
            PhoneNumber,
            PictureAddress,
            Address,
            CAST(NULL AS DATE) AS DOB,
            CAST(NULL AS INT) AS Age,
            CAST(NULL AS INT) AS ExperienceYears,
            CAST(NULL AS DECIMAL(10,2)) AS HourlyRate,
            CAST(NULL AS NVARCHAR(50)) AS AvailabilityStatus,
            CreatedAt,
            'Login Successful' AS Message
        FROM Parent 
        WHERE Username = @InputUser AND Password = @InputPass;
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Babysitter WHERE Username = @InputUser)
        BEGIN
            SELECT CAST(NULL AS INT) AS UserID, 'User not found' AS Message;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM Babysitter WHERE Username = @InputUser AND Password = @InputPass)
        BEGIN
            SELECT CAST(NULL AS INT) AS UserID, 'Wrong password' AS Message;
            RETURN;
        END

        SELECT 
            Sitter_ID AS UserID,
            FullName,
            EmailAddress,
            Username,
            Password,
            PhoneNumber,
            PictureAddress,
            NULL AS Address,
            DOB,
            Age,
            ExperienceYears,
            HourlyRate,
            AvailabilityStatus,
            CreatedAt,
            'Login Successful' AS Message
        FROM Babysitter 
        WHERE Username = @InputUser AND Password = @InputPass;
    END
END
-- 1. Register a Parent
INSERT INTO Parent (FullName, EmailAddress, Username, Password, PhoneNumber, Address, PictureAddress)
VALUES ('Abdullah Saleem', 'abdullah@email.com', 'abdullah_s', 'Pass123', '0300-1234567', 'Islamabad', 'pic_parent.jpg');

-- 2. Register a Babysitter (DOB set to exactly 20 years ago)
INSERT INTO Babysitter (FullName, EmailAddress, Username, Password, PhoneNumber, DOB, ExperienceYears, HourlyRate, PictureAddress)
VALUES ('Jane Doe', 'jane@email.com', 'janedoe', 'SitterPass', '0311-7654321', '2006-03-28', 2, 500, 'pic_sitter.jpg');

-- 3. Register a Child for the Parent
INSERT INTO Child (Parent_ID, ChildName, DOB, Gender, PictureAddress, SpecialRequirements)
VALUES (1, 'Baby Ali', '2023-01-01', 'Male', 'baby_pic.jpg', 'No Peanuts');

-- VERIFY AGE: Check if Age columns were auto-filled
SELECT FullName, DOB, Age FROM Babysitter;
SELECT ChildName, DOB, Age FROM Child;

-- TEST: Duplicate Email (Should fail)
INSERT INTO Parent (FullName, EmailAddress, Username, Password)
VALUES ('Duplicate User', 'abdullah@email.com', 'new_user', 'Pass123');

-- TEST: Duplicate Username (Should fail)
INSERT INTO Babysitter (FullName, EmailAddress, Username, Password, DOB)
VALUES ('New Sitter', 'new_email@email.com', 'janedoe', 'Pass123', '1995-10-10');

-- 1. SUCCESSFUL LOGIN
EXEC sp_UserLogin @InputUser = 'abdullah_s', @InputPass = 'Pass123', @Role = 'Parent';

-- 2. WRONG PASSWORD (Should throw error)
EXEC sp_UserLogin @InputUser = 'abdullah_s', @InputPass = 'WrongPass', @Role = 'Parent';

-- 3. USER DOES NOT EXIST (Should throw error)
EXEC sp_UserLogin @InputUser = 'UnknownUser', @InputPass = 'anyPass', @Role = 'Sitter';

-- 1. Parent posts a job
INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
VALUES (1, 1, 'Weekend Care', 'Need help on Saturday evening', '2026-04-05', 'Open');

-- 2. Sitter places a bid
INSERT INTO Bid (Job_ID, Sitter_ID, ProposedPrice, BidStatus)
VALUES (1, 1, 600, 'Pending');

-- VERIFY: See the job with its associated bid
SELECT j.Title, j.Status, b.ProposedPrice, s.FullName as SitterName
FROM Job j
JOIN Bid b ON j.Job_ID = b.Job_ID
JOIN Babysitter s ON b.Sitter_ID = s.Sitter_ID;

select * from Parent;
select * from Babysitter;-- 1. First, make sure the trigger is gone to avoid conflicts
IF OBJECT_ID('trg_CheckDuplicateParent', 'TR') IS NOT NULL
    DROP TRIGGER trg_CheckDuplicateParent;

-- 2. Add Unique Constraints to the table
-- This is faster and more reliable than a trigger for Entity Framework
ALTER TABLE Parent
ADD CONSTRAINT UQ_Parent_Email UNIQUE (EmailAddress);

ALTER TABLE Parent
ADD CONSTRAINT UQ_Parent_Username UNIQUE (Username);

CREATE TABLE TimeSlot (
    Slot_ID INT PRIMARY KEY IDENTITY(1,1),
    StartTime TIME,
    EndTime TIME
);
INSERT INTO TimeSlot (StartTime, EndTime)
VALUES 
('14:00', '16:00'),
('16:00', '18:00'),
('18:00', '20:00');

CREATE TABLE JobTimeSlot (
    JobSlot_ID INT PRIMARY KEY IDENTITY(1,1),
    Job_ID INT FOREIGN KEY REFERENCES Job(Job_ID),
    Slot_ID INT FOREIGN KEY REFERENCES TimeSlot(Slot_ID)
);
CREATE TABLE SitterAvailability (
    Availability_ID INT PRIMARY KEY IDENTITY(1,1),
    Sitter_ID INT FOREIGN KEY REFERENCES Babysitter(Sitter_ID),
    AvailableDate DATE,
    Slot_ID INT FOREIGN KEY REFERENCES TimeSlot(Slot_ID)
);

CREATE PROCEDURE sp_GetMatchingSitters
    @JobID INT
AS
BEGIN
    SELECT DISTINCT 
        b.Sitter_ID,
        b.FullName,
        b.HourlyRate
    FROM Babysitter b
    JOIN SitterAvailability sa ON b.Sitter_ID = sa.Sitter_ID
    JOIN JobTimeSlot js ON sa.Slot_ID = js.Slot_ID
    JOIN Job j ON j.Job_ID = js.Job_ID
    WHERE j.Job_ID = @JobID
    AND sa.AvailableDate = j.JobDate;
END;
EXEC sp_GetMatchingSitters @JobID = 1;
SELECT * FROM Job;
SELECT * FROM JobTimeSlot;
SELECT * FROM SitterAvailability;

INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
VALUES (1, 1, 'Evening Babysitting', 'Need sitter for evening', '2026-04-05', 'Open');

INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
VALUES 
(1, 1),   -- 2pm–4pm
(1, 2);   -- 4pm–6pm

INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID)
VALUES 
(1, '2026-04-05', 1),  -- MATCH ✅
(1, '2026-04-05', 2);  -- MATCH ✅

INSERT INTO Babysitter (FullName, EmailAddress, Username, Password, DOB)
VALUES ('Wrong Sitter', 'wrong@email.com', 'wronguser', '1234', '2000-01-01');

INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID)
VALUES 
(2, '2026-04-06', 1); -- ❌ Different date → will NOT match

EXEC sp_GetMatchingSitters @JobID = 1;

DELETE FROM JobTimeSlot;
DELETE FROM SitterAvailability;
DELETE FROM Job;
DELETE FROM Bid;

DBCC CHECKIDENT ('Job', RESEED, 0);

INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
VALUES 
(1, 1, 'Morning Care', 'Need sitter in morning', '2026-04-05', 'Open'),
(1, 1, 'Afternoon Care', 'Need sitter in afternoon', '2026-04-05', 'Open'),
(1, 1, 'Evening Care', 'Need sitter in evening', '2026-04-05', 'Open');

INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
VALUES 
(1, 1),  -- 2pm–4pm
(2, 2),  -- 4pm–6pm
(3, 3);  -- 6pm–8pm

INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID)
VALUES 
(1, '2026-04-05', 1),  -- matches Job 1 ✅
(1, '2026-04-05', 3);  -- matches Job 3 ✅


INSERT INTO Parent (FullName, EmailAddress, Username, Password, PhoneNumber, Address, PictureAddress)
VALUES 
('Ali Khan', 'ali@email.com', 'alikhan', '1234', '03001111111', 'Lahore', 'ali.jpg'),
('Sara Ahmed', 'sara@email.com', 'saraahmed', '1234', '03002222222', 'Karachi', 'sara.jpg'),
('Usman Tariq', 'usman@email.com', 'usmantariq', '1234', '03003333333', 'Islamabad', 'usman.jpg'),
('Hina Malik', 'hina@email.com', 'hina', '1234', '03004444444', 'Rawalpindi', 'hina.jpg');

INSERT INTO Babysitter (FullName, EmailAddress, Username, Password, PhoneNumber, DOB, ExperienceYears, HourlyRate, PictureAddress)
VALUES 
('Ayesha Khan', 'ayesha@email.com', 'ayesha', '1234', '03110000001', '2000-05-10', 3, 400, 'sitter1.jpg'),
('Fatima Noor', 'fatima@email.com', 'fatima', '1234', '03110000002', '1998-08-15', 5, 600, 'sitter2.jpg'),
('Zara Ali', 'zara@email.com', 'zara', '1234', '03110000003', '2002-02-20', 2, 350, 'sitter3.jpg');

INSERT INTO Child (Parent_ID, ChildName, DOB, Gender, PictureAddress, SpecialRequirements)
VALUES 
(9, 'Baby Ali', '2023-01-01', 'Male', 'baby1.jpg', 'None'),
(10, 'Ayan Khan', '2022-06-10', 'Male', 'baby2.jpg', 'Milk Allergy'),
(11, 'Sara Noor', '2021-09-05', 'Female', 'baby3.jpg', 'Needs attention'),
(12, 'Hamza Tariq', '2020-12-12', 'Male', 'baby4.jpg', 'Hyperactive');

INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
VALUES 
(9, 5, 'Morning Care', 'Need sitter in morning', '2026-04-05', 'Open'),
(10, 6, 'Afternoon Care', 'Need sitter in afternoon', '2026-04-05', 'Open'),
(11, 7, 'Evening Care', 'Need sitter in evening', '2026-04-05', 'Open'),
(12, 4, 'Full Day Care', 'Need full day babysitting', '2026-04-05', 'Open');

DELETE FROM TimeSlot;

DBCC CHECKIDENT ('TimeSlot', RESEED, 0);

INSERT INTO TimeSlot (StartTime, EndTime)
VALUES 
('08:00', '10:00'),  -- 1
('10:00', '12:00'),  -- 2
('12:00', '14:00'),  -- 3
('14:00', '16:00'),  -- 4
('16:00', '18:00'),  -- 5
('18:00', '20:00'),  -- 6
('20:00', '22:00');  -- 7

select * from Parent
select * from child
select * from jobTimeSlot
select * from Babysitter
select * from Job

ALTER TABLE Job
ADD Payment DECIMAL(10,2);

UPDATE Job SET Payment = 400 WHERE Job_ID = 1;
UPDATE Job SET Payment = 500 WHERE Job_ID = 2;
UPDATE Job SET Payment = 600 WHERE Job_ID = 3;
UPDATE Job SET Payment = 450 WHERE Job_ID = 12;
UPDATE Job SET Payment = 550 WHERE Job_ID = 13;
UPDATE Job SET Payment = 650 WHERE Job_ID = 14;
UPDATE Job SET Payment = 700 WHERE Job_ID = 15;

DELETE FROM JobTimeSlot;

INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
VALUES
(1, 1),
(1, 2),

(2, 3),
(2, 4),

(3, 5),
(3, 6),

(12, 1),
(12, 3),
(12, 5);

SELECT * FROM JobTimeSlot WHERE Job_ID = 12

INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID)
VALUES
-- Sitter 1 (Morning + Evening)
(1, '2026-04-05', 1),
(1, '2026-04-05', 2),
(1, '2026-04-05', 6),

-- Sitter 3 (Afternoon)
(3, '2026-04-05', 3),
(3, '2026-04-05', 4),

-- Sitter 4 (Full Day)
(4, '2026-04-05', 1),
(4, '2026-04-05', 3),
(4, '2026-04-05', 5),
(4, '2026-04-05', 6),

-- Sitter 5 (Evening only)
(5, '2026-04-05', 5),
(5, '2026-04-05', 6),

-- Sitter 6 (Morning + Afternoon)
(6, '2026-04-05', 1),
(6, '2026-04-05', 3),

-- Sitter 7 (Night only)
(7, '2026-04-05', 7),

-- Sitter 8 (All slots 🔥)
(8, '2026-04-05', 1),
(8, '2026-04-05', 2),
(8, '2026-04-05', 3),
(8, '2026-04-05', 4),
(8, '2026-04-05', 5),
(8, '2026-04-05', 6),
(8, '2026-04-05', 7);

EXEC sp_GetMatchingSitters @JobID = 12;
EXEC sp_GetMatchingSitters @JobID = 13;
EXEC sp_GetMatchingSitters @JobID = 15;

EXEC sp_GetMatchingJobsForSitter @SitterID = 8;

SELECT 
    j.Job_ID,
    ts.StartTime,
    ts.EndTime
FROM Job j
JOIN JobTimeSlot js ON j.Job_ID = js.Job_ID
JOIN TimeSlot ts ON js.Slot_ID = ts.Slot_ID
WHERE j.Job_ID = 12;

ALTER TABLE Babysitter
ADD Rating DECIMAL(2,1) DEFAULT 4.0;
select * from Babysitter
UPDATE Babysitter SET Rating = 4.3 WHERE Sitter_ID = 4
UPDATE Babysitter SET Rating = 3.2 WHERE Sitter_ID = 5;
UPDATE Babysitter SET Rating = 4.8 WHERE Sitter_ID = 6;
UPDATE Babysitter SET Rating = 5.0 WHERE Sitter_ID = 7;
UPDATE Babysitter SET Rating = 4.4 WHERE Sitter_ID = 8;

SELECT * FROM Bid;
select * from job;
UPDATE Job SET AssignedSitter_ID = 4 WHERE Job_ID = 1;
UPDATE Job SET AssignedSitter_ID = 5 WHERE Job_ID = 2;




SELECT * FROM Job;
SELECT * FROM Child;
SELECT * FROM Parent;
SELECT * FROM JobTimeSlot;
SELECT * FROM TimeSlot;
select * from Review;
select * from SitterAvailability
select * from Babysitter

ALTER TABLE SitterAvailability
ADD City NVARCHAR(100);

INSERT INTO Review (Job_ID, Parent_ID, Sitter_ID, Rating, Comment)
VALUES 
(12, 9, 4, 4.5, 'Very cooperative parent'),
(12, 9, 5, 4.0, 'Good communication'),
(12, 9, 6, 5.0, 'Excellent experience'),
(12, 9, 7, 3.5, 'Average experience');
INSERT INTO Review (Job_ID, Parent_ID, Sitter_ID, Rating, Comment)
VALUES 
(13, 10, 4, 3.0, 'Okay job'),
(13, 10, 5, 3.5, 'Not bad'),
(13, 10, 6, 4.0, 'Good parent');

SELECT TOP 1 * FROM SitterAvailability

UPDATE SitterAvailability SET City = 'Islamabad' WHERE Availability_ID = 12;
UPDATE SitterAvailability SET City = 'Rawalpindi' WHERE Availability_ID = 13;
UPDATE SitterAvailability SET City = 'Lahore' WHERE Availability_ID = 14;
UPDATE SitterAvailability SET City = 'Islamabad' WHERE Availability_ID = 1;
UPDATE SitterAvailability SET City = 'Rawalpindi' WHERE Availability_ID = 3;
UPDATE Job SET City = 'Islamabad' WHERE Job_ID = 2;

ALTER TABLE Babysitter DROP COLUMN Rating;

ALTER TABLE Babysitter
DROP CONSTRAINT DF__Babysitte__Ratin__0F624AF8;
SELECT 
    b.Sitter_ID,
    b.FullName,
    b.HourlyRate,
    ISNULL(AVG(r.Rating), 0) AS Rating
FROM Babysitter b
LEFT JOIN Review r ON b.Sitter_ID = r.Sitter_ID
GROUP BY b.Sitter_ID, b.FullName, b.HourlyRate;

ALTER TABLE Review
ADD ReviewerRole NVARCHAR(20); -- 'Parent' or 'Sitter'
CREATE TABLE Review (
    Review_ID INT PRIMARY KEY IDENTITY(1,1),

    Job_ID INT FOREIGN KEY REFERENCES Job(Job_ID),

    Reviewer_ID INT NOT NULL,
    ReviewerRole NVARCHAR(20) NOT NULL,   -- 'Parent' or 'Sitter'

    ReviewFor_ID INT NOT NULL,
    ReviewForRole NVARCHAR(20) NOT NULL,   -- 'Parent' or 'Sitter'

    Rating DECIMAL(2,1) NOT NULL,
    Comment NVARCHAR(MAX),

    CreatedAt DATETIME DEFAULT GETDATE()
);
INSERT INTO Review (Job_ID, Reviewer_ID, ReviewerRole, ReviewFor_ID, ReviewForRole, Rating, Comment)
VALUES 
(1, 1, 'Sitter', 9, 'Parent', 4.5, 'Good parent'),
(1, 2, 'Sitter', 9, 'Parent', 4.0, 'Nice communication');
INSERT INTO Review (Job_ID, Reviewer_ID, ReviewerRole, ReviewFor_ID, ReviewForRole, Rating, Comment)
VALUES 
(1, 9, 'Parent', 1, 'Sitter', 5.0, 'Excellent sitter'),
(1, 9, 'Parent', 1, 'Sitter', 4.5, 'Very good');
UPDATE Job
SET AssignedSitter_ID = NULL;

-- Disable all foreign keys
EXEC sp_msforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT ALL";

-- Delete child tables first (safe full reset)
DELETE FROM Review;
DELETE FROM Bid;
DELETE FROM SitterAvailability;
DELETE FROM JobTimeSlot;
DELETE FROM Job;
DELETE FROM Child;
DELETE FROM Babysitter;
DELETE FROM Parent;
DELETE FROM TimeSlot;

-- Re-enable constraints
EXEC sp_msforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL";

DBCC CHECKIDENT ('Parent', RESEED, 0);
DBCC CHECKIDENT ('Child', RESEED, 0);
DBCC CHECKIDENT ('Babysitter', RESEED, 0);
DBCC CHECKIDENT ('Job', RESEED, 0);
DBCC CHECKIDENT ('TimeSlot', RESEED, 0);
DBCC CHECKIDENT ('JobTimeSlot', RESEED, 0);
DBCC CHECKIDENT ('SitterAvailability', RESEED, 0);
DBCC CHECKIDENT ('Review', RESEED, 0);
DBCC CHECKIDENT ('Bid', RESEED, 0);

INSERT INTO Parent (FullName, EmailAddress, Username, Password, PhoneNumber, Address, PictureAddress)
VALUES 
('Ali Khan', 'ali@email.com', 'alikhan', '1234', '03001111111', 'Lahore', 'ali.jpg'),
('Sara Ahmed', 'sara@email.com', 'saraahmed', '1234', '03002222222', 'Karachi', 'sara.jpg'),
('Usman Tariq', 'usmantariq@email.com', 'usmantariq', '1234', '03003333333', 'Islamabad', 'usman.jpg'),
('Hina Malik', 'hina@email.com', 'hina', '1234', '03004444444', 'Rawalpindi', 'hina.jpg');
INSERT INTO Babysitter (FullName, EmailAddress, Username, Password, PhoneNumber, DOB, ExperienceYears, HourlyRate, PictureAddress)
VALUES 
('Ayesha Khan', 'ayesha@email.com', 'ayesha', '1234', '03110000001', '2000-05-10', 3, 400, 'sitter1.jpg'),
('Fatima Noor', 'fatima@email.com', 'fatima', '1234', '03110000002', '1998-08-15', 5, 600, 'sitter2.jpg'),
('Zara Ali', 'zara@email.com', 'zara', '1234', '03110000003', '2002-02-20', 2, 350, 'sitter3.jpg');
INSERT INTO Child (Parent_ID, ChildName, DOB, Gender, PictureAddress, SpecialRequirements)
VALUES 
(1, 'Baby Ali', '2023-01-01', 'Male', 'baby1.jpg', 'None'),
(2, 'Ayan Khan', '2022-06-10', 'Male', 'baby2.jpg', 'Milk Allergy'),
(3, 'Sara Noor', '2021-09-05', 'Female', 'baby3.jpg', 'Needs attention'),
(4, 'Hamza Tariq', '2020-12-12', 'Male', 'baby4.jpg', 'Hyperactive');
INSERT INTO TimeSlot (StartTime, EndTime)
VALUES 
('08:00','10:00'),
('10:00','12:00'),
('12:00','14:00'),
('14:00','16:00'),
('16:00','18:00'),
('18:00','20:00'),
('20:00','22:00');
INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
VALUES 
(1,1,'Morning Care','Help required','2026-04-12','Open'),
(2,2,'Afternoon Care','Help required','2026-04-13','Open'),
(3,3,'Evening Care','Help required','2026-04-14','Open'),
(4,4,'Full Day Care','Help required','2026-04-15','Open');
INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
VALUES
(1,1),(1,2),
(2,3),(2,4),
(3,5),(3,6),
(4,1),(4,3),(4,5);
INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID, City)
VALUES
(1,'2026-04-12',1,'Islamabad'),
(1,'2026-04-12',2,'Islamabad'),
(2,'2026-04-13',3,'Lahore'),
(3,'2026-04-14',5,'Rawalpindi');
DECLARE @i INT = 1;

WHILE @i <= 40
BEGIN
    INSERT INTO Job (Parent_ID, Child_ID, Title, Description, JobDate, Status)
    VALUES (
        ((@i - 1) % 4) + 1,
        ((@i - 1) % 4) + 1,
        CONCAT('Job Request ', @i),
        'Babysitting required',
        DATEADD(DAY, @i, GETDATE()),
        'Open'
    );

    SET @i = @i + 1;
END;
-- 👨 Parents
SELECT * FROM Parent;


-- 👶 Children
SELECT * FROM Child;

-- 👩‍🍼 Babysitters
SELECT * FROM Babysitter;

-- 📅 Jobs
SELECT * FROM Job;




-- ⏰ Time Slots
SELECT * FROM TimeSlot;

-- 🔗 Job ↔ TimeSlot mapping
SELECT * FROM JobTimeSlot;

-- 📍 Sitter Availability
SELECT * FROM SitterAvailability;

-- 💰 BidsewF
SELECT * FROM Bid;

-- ⭐ Reviews
SELECT * FROM Review;

-- 🔔 Notifications
SELECT * FROM Notification;


SELECT 
    j.Job_ID,
    j.Title,
    j.JobDate,
    j.Status,
    j.Parent_ID,
    j.Child_ID,
    p.FullName AS ParentName,
    c.ChildName
FROM Job j
JOIN Parent p ON j.Parent_ID = p.Parent_ID
JOIN Child c ON j.Child_ID = c.Child_ID;
SELECT 
    sa.Availability_ID,
    sa.Sitter_ID,
    b.FullName,
    sa.AvailableDate,
    sa.Slot_ID,
    sa.City
FROM SitterAvailability sa
JOIN Babysitter b ON sa.Sitter_ID = b.Sitter_ID
ORDER BY sa.Sitter_ID;
SELECT 
    j.Job_ID,
    b.Sitter_ID,
    b.FullName,
    sa.AvailableDate,
    sa.Slot_ID
FROM Job j
JOIN JobTimeSlot js ON j.Job_ID = js.Job_ID
JOIN SitterAvailability sa ON js.Slot_ID = sa.Slot_ID
JOIN Babysitter b ON sa.Sitter_ID = b.Sitter_ID
WHERE sa.AvailableDate = j.JobDate;

-- ═══════════════════════════════════════════════════════════════════
-- FIX ALL 44 JOBS: city, payment, timeslots
-- Run this once in SSMS
-- ═══════════════════════════════════════════════════════════════════

-- 1. Copy parent city into every job that has no city
UPDATE j
SET j.City = p.Address
FROM Job j
JOIN Parent p ON j.Parent_ID = p.Parent_ID
WHERE j.City IS NULL OR j.City = '';

-- Safety fallback
UPDATE Job SET City = 'Islamabad' WHERE City IS NULL OR City = '';

-- 2. Fix NULL payment
UPDATE Job SET Payment = 500 WHERE Payment IS NULL;

-- 3. Make all jobs open
UPDATE Job SET Status = 'Open', AssignedSitter_ID = NULL;

-- 4. Add JobTimeSlot rows for ALL jobs that currently have none
--    We round-robin through slots 1-7 so each job gets at least one slot
--    (jobs 1-4 already have slots from your seed, so we skip those)
INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
SELECT j.Job_ID,
       -- Give each job 2 slots based on its ID so variety exists
       ((j.Job_ID - 1) % 7) + 1         AS Slot_ID
FROM Job j
WHERE NOT EXISTS (
    SELECT 1 FROM JobTimeSlot js WHERE js.Job_ID = j.Job_ID
);

-- Add a second slot per job (offset by 1)
INSERT INTO JobTimeSlot (Job_ID, Slot_ID)
SELECT j.Job_ID,
       ((j.Job_ID) % 7) + 1             AS Slot_ID
FROM Job j
WHERE NOT EXISTS (
    SELECT 1 FROM JobTimeSlot js
    WHERE js.Job_ID = j.Job_ID
      AND js.Slot_ID = ((j.Job_ID) % 7) + 1
)
AND NOT EXISTS (
    -- Don't duplicate slot already added above
    SELECT 1 FROM JobTimeSlot js
    WHERE js.Job_ID = j.Job_ID
      AND js.Slot_ID = ((j.Job_ID - 1) % 7) + 1
      AND ((j.Job_ID - 1) % 7) + 1 = ((j.Job_ID) % 7) + 1
);

-- 5. Verify — every job should now have a city, payment, and at least 1 slot
SELECT
    j.Job_ID,
    j.Title,
    j.JobDate,
    j.City,
    j.Payment,
    j.Status,
    STRING_AGG(CAST(js.Slot_ID AS VARCHAR), ',') AS SlotIds
FROM Job j
LEFT JOIN JobTimeSlot js ON j.Job_ID = js.Job_ID
GROUP BY j.Job_ID, j.Title, j.JobDate, j.City, j.Payment, j.Status
ORDER BY j.Job_ID;

-- 6. See distinct cities (type one of these in the app)
SELECT DISTINCT City, COUNT(*) AS Jobs FROM Job GROUP BY City ORDER BY City;

-- See what's actually in DB for sitter 2
SELECT * FROM SitterAvailability WHERE Sitter_ID = 2 ORDER BY AvailableDate;

-- Clear stale data and let the sitter re-save fresh
DELETE FROM SitterAvailability WHERE Sitter_ID = 2;

INSERT INTO SitterAvailability (Sitter_ID, AvailableDate, Slot_ID, City)
VALUES
(7, '2026-04-13', 3, 'Rawalpindi'),
(7, '2026-04-13', 4, 'Rawalpindi');

SELECT * FROM SitterAvailability
WHERE Sitter_ID = 7;


-- Check Sitter 3's experience and rating
SELECT Sitter_ID, FullName, ExperienceYears 
FROM Babysitter WHERE Sitter_ID = 3;

-- Check availability for the required slots (1-5) on those dates
SELECT * FROM SitterAvailability
WHERE Sitter_ID = 3 
  AND City = 'Islamabad' 
  AND AvailableDate IN ('2026-04-12','2026-04-13','2026-04-14')
  AND Slot_ID IN (1,2,3,4,5);

 -- 1. Delete the parent with incorrect picture storing (Billo Rani)
 DELETE FROM Parent WHERE Parent_ID = 33;

 DELETE FROM Child WHERE Child_ID = 26;
Delete from Babysitter Where Sitter_ID=18;

-- Delete the repeat‑day jobs created during testing
-- Delete child rows first
DELETE FROM JobTimeSlot WHERE Job_ID IN (67, 68, 69, 70);

-- Now delete the jobs
DELETE FROM Job WHERE Job_ID IN (67, 68, 69, 70);

-- Delete the test availability for Sara (20) and Nimra (21) on those dates
CREATE TABLE CryAlert (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Timestamp       DATETIME        NOT NULL,
    Level           NVARCHAR(50)    NOT NULL,
    RoomName        NVARCHAR(100)   NOT NULL,
    JobId           INT             NULL,
    ParentId        INT             NULL,
    BabysitterId    INT             NULL,
    CreatedAt       DATETIME        DEFAULT GETDATE()
);