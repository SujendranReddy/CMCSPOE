using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class Claim
    {
        [Required]
        public int ClaimID { get; set; }

        [Required]
        public string Lecturer { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        [Range(1, 999)]
        public int HoursWorked { get; set; }

        [Required]
        [Range(100, 2000)]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;

        // Two distinct statuses
        public ClaimVerificationStatus VerificationStatus { get; set; } = ClaimVerificationStatus.Pending;
        public ClaimApprovalStatus ApprovalStatus { get; set; } = ClaimApprovalStatus.Pending;

        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;
        public string SubmittedBy { get; set; } = "-";

        public string VerifiedBy { get; set; } = "-";
        public DateTime? VerifiedOn { get; set; }

        public string ApprovedBy { get; set; } = "-";
        public DateTime? ApprovedOn { get; set; }

        public List<string> EncryptedDocuments { get; set; } = new();
        public List<string> OriginalDocuments { get; set; } = new();
    }

    public enum ClaimVerificationStatus
    {
        Pending,
        Verified,
        Rejected
    }

    public enum ClaimApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
