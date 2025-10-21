Module: PROG6212

Author: Sujendran Reddy

Version: Part 2 Submission

Youtube Link: https://youtu.be/hzcTGrePsNI?si=Hvbx2_6fNbfXnuRN 

**OVERVIEW**
The Contract Monthly Claim System (CMCS) is a basic MVC-based app that manages the submission, 
verification, and approval of claims made by lecturers.

**WORKFLOW**
1. Lecturer - submits claim
2. Programme Coordinator - verifies or rejects claims
3. Academic Manager - approves or rejects claims

All data is stored in encrypted JSON files instead of a database following the project requirements.

**Lecturer Feedback Fix Summary**
**Original Feedback:**

“A coordinator will verify and a manager will approve. You have allowed for approval and verification 
to be done by both coordinators and managers. Remember, a claim gets submitted by a lecturer, verified 
by a coordinator, and approved by a manager (PC and AM can deny a claim as well). Your design does not 
have fields to keep track of who verified/approved/denied a claim. That information must be displayed 
back to the lecturer.”

**Resolution Implemented:**

-Restricted verification actions to Programme Coordinators only.
-Restricted approval/rejection actions to Academic Managers only.
-Added VerifiedBy and ApprovedBy properties to Claim model.
-Updated ClaimDataStore and controllers to persist this data.
-Displayed this information on dashboards and detail pages for full transparency.



**Unit Testing**
ClaimDataStoreTests.cs
Verifies:
Claims receive an ID and default pending status.
Claims update correctly on verification and approval.
JSON read/write integrity.
Uses xUnit for structured testing.


**Security**
Claims are encrypted when stored.
Data is decrypted on load.
No sensitive information is exposed in plain text.

**Technologies Used**
ASP.NET Core MVC (C#)
Razor Views
xUnit Testing
JSON File Persistence
AES Encryption



**Changes Implemented from Part 1 → Part 2**

Data Storage – Encrypted JSON file storage
Workflow Logic – Coordinator verifies; Manager approves
Claim Model – Added VerifiedBy and ApprovedBy fields
Claim Status Enum for both Verification and Approval – Added full flow: Pending, Verified/Approved, Rejected
Lecturer Feedback Fix – Role restrictions enforced (PC = verify, AM = approve/reject)
UI Improvements – Clean tables, spacing, shadows, and color-coded badges
Popup Functionality – Added name entry popups for verification and approval
Status Display – Color-coded status cells for clarity
Claim Details View – Displays verification and approval info clearly
