Module: PROG6212

Author: Sujendran Reddy

Version: Part 3 Final Submission

Youtube Link:

**OVERVIEW**
The Contract Monthly Claim System (CMCS) is a basic MVC-based app that manages the submission, 
verification, and approval of claims made by lecturers.

**Part 3 Enhancements:*
- Introduced HR as a "super user" role.
- Migrated from JSON file storage to Entity Framework (EF) database.
- Implemented automation and validation functionalities. 
- Implemented sessions and login funcntionality (Identity). 

*Workflow*
*HR*
1. HR Management: The HR user can perform full CRUD functionality on all user profiles.
2. Report Generation: The HR user can generates reports, which can be filtered by date, or claim status.
3. No Public Registration: Users cannot self register.

*Lecturer*
1. Login Required: Lecturers must log in to access functionalities.
2. Automated Data: Data created about the user created by the HR is automatically populated into the claim's model.
3. Validation: A claim submission exceeding max monthly hours are not saved in the database. For example if a users maximum hours are 5 per month, they submitted a claim of 3 hour for January 2025, and then another for 5 for the same month and year. An error message would tell the user they have exceeded their monthly limit and as a result will only be paid for 5 hours.
4. Auto Calculation: total amounts are calculated automatically.
5. Claim Tracking: Lecturers can view the status(verification and approval) of their claims.

*Coordinator and Manager*
1. Role-Specific Access: Coordinators verify claims, Managers approve/reject claims.
2. Role-based Login: Users are only able to access functionalities for their specified role.

**Changes Implemented from Part 2 to Part 3**
1. New Role: HR not present, HR added as super user.
2. User Management: Any one could create roles, HR creates and updates all users.
3. Lecturer Input: Manual hourly rate input, Auto-filled from HR data and auto-calculation
4. Validation: Minimal, Hours cannot exceed max allowed.
5. Login: Not semi implemented, Full login with sessions.
6. Reports: Not available, HR can generate reports.
7. Authroization & Authenticaion: Not implemented, Full implemented role-based.

**Security**
- Sensitive data is protected by identity e.g. passwords.
- Only authorized roles can access specific functions.

**User Guide**
1. Login using HR credentials :
   Email: hr@system.com
   Password: Admin#1234
2. Create users for lecturer, coordinator and manager.
**Lecturer**
3. Logout of HR using nav-bar top right "logout", login as a Lecturer.
4. Submit a claim.
5. Return to the dashboard and click details to view the claims details.
**Programme Coordinator**
6. Logout of lecturer, login as Coordinator.
7. Verify or reject claims.
8. View details to see verification status updated and verified by populated by users name.
**Manager**
9. Logout of Coordinator, login as Manager.
10. Approve or reject claims.
11. View details to see approval status updated and approved by populated by users name.
**HR**
12. Logout of Manager, login as HR.
13. Click on generate report.
14. Select filters or do not.
15. Generate the report and view it.


**SQL CODE**
CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId]    NVARCHAR (150) NOT NULL,
    [ProductVersion] NVARCHAR (32)  NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED ([MigrationId] ASC)
);


CREATE TABLE [dbo].[AspNetRoleClaims] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [RoleId]     NVARCHAR (450) NOT NULL,
    [ClaimType]  NVARCHAR (MAX) NULL,
    [ClaimValue] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetRoleClaims_RoleId]
    ON [dbo].[AspNetRoleClaims]([RoleId] ASC);



CREATE TABLE [dbo].[AspNetRoles] (
    [Id]               NVARCHAR (450) NOT NULL,
    [Name]             NVARCHAR (256) NULL,
    [NormalizedName]   NVARCHAR (256) NULL,
    [ConcurrencyStamp] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex]
    ON [dbo].[AspNetRoles]([NormalizedName] ASC) WHERE ([NormalizedName] IS NOT NULL);

CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [UserId]     NVARCHAR (450) NOT NULL,
    [ClaimType]  NVARCHAR (MAX) NULL,
    [ClaimValue] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserClaims_UserId]
    ON [dbo].[AspNetUserClaims]([UserId] ASC);

CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider]       NVARCHAR (450) NOT NULL,
    [ProviderKey]         NVARCHAR (450) NOT NULL,
    [ProviderDisplayName] NVARCHAR (MAX) NULL,
    [UserId]              NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider] ASC, [ProviderKey] ASC),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserLogins_UserId]
    ON [dbo].[AspNetUserLogins]([UserId] ASC);

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR (450) NOT NULL,
    [RoleId] NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_AspNetUserRoles_RoleId]
    ON [dbo].[AspNetUserRoles]([RoleId] ASC);

CREATE TABLE [dbo].[AspNetUsers] (
    [Id]                   NVARCHAR (450)     NOT NULL,
    [FirstName]            NVARCHAR (MAX)     NOT NULL,
    [LastName]             NVARCHAR (MAX)     NOT NULL,
    [HourlyRate]           DECIMAL (18, 2)    NOT NULL,
    [MaxHoursPerMonth]     INT                NOT NULL,
    [UserName]             NVARCHAR (256)     NULL,
    [NormalizedUserName]   NVARCHAR (256)     NULL,
    [Email]                NVARCHAR (256)     NULL,
    [NormalizedEmail]      NVARCHAR (256)     NULL,
    [EmailConfirmed]       BIT                NOT NULL,
    [PasswordHash]         NVARCHAR (MAX)     NULL,
    [SecurityStamp]        NVARCHAR (MAX)     NULL,
    [ConcurrencyStamp]     NVARCHAR (MAX)     NULL,
    [PhoneNumber]          NVARCHAR (MAX)     NULL,
    [PhoneNumberConfirmed] BIT                NOT NULL,
    [TwoFactorEnabled]     BIT                NOT NULL,
    [LockoutEnd]           DATETIMEOFFSET (7) NULL,
    [LockoutEnabled]       BIT                NOT NULL,
    [AccessFailedCount]    INT                NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [EmailIndex]
    ON [dbo].[AspNetUsers]([NormalizedEmail] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex]
    ON [dbo].[AspNetUsers]([NormalizedUserName] ASC) WHERE ([NormalizedUserName] IS NOT NULL);

CREATE TABLE [dbo].[AspNetUserTokens] (
    [UserId]        NVARCHAR (450) NOT NULL,
    [LoginProvider] NVARCHAR (450) NOT NULL,
    [Name]          NVARCHAR (450) NOT NULL,
    [Value]         NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId] ASC, [LoginProvider] ASC, [Name] ASC),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[Claims] (
    [ClaimID]                INT             IDENTITY (1, 1) NOT NULL,
    [Month]                  NVARCHAR (MAX)  NOT NULL,
    [HoursWorked]            INT             NOT NULL,
    [HourlyRate]             DECIMAL (18, 2) NOT NULL,
    [VerificationStatus]     INT             NOT NULL,
    [ApprovalStatus]         INT             NOT NULL,
    [SubmittedOn]            DATETIME2 (7)   NOT NULL,
    [VerifiedBy]             NVARCHAR (MAX)  NOT NULL,
    [VerifiedOn]             DATETIME2 (7)   NULL,
    [ApprovedBy]             NVARCHAR (MAX)  NOT NULL,
    [ApprovedOn]             DATETIME2 (7)   NULL,
    [EncryptedDocumentsJson] NVARCHAR (MAX)  NOT NULL,
    [OriginalDocumentsJson]  NVARCHAR (MAX)  NOT NULL,
    [UserId]                 NVARCHAR (450)  DEFAULT (N'') NOT NULL,
    CONSTRAINT [PK_Claims] PRIMARY KEY CLUSTERED ([ClaimID] ASC),
    CONSTRAINT [FK_Claims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Claims_UserId]
    ON [dbo].[Claims]([UserId] ASC);



    
   
