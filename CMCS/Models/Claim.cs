using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CMCS.Models
{
    public class Claim
    {
        [Key]
        public int ClaimID { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        [Range(1, 999)]
        public int HoursWorked { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;

        public ClaimVerificationStatus VerificationStatus { get; set; } = ClaimVerificationStatus.Pending;
        public ClaimApprovalStatus ApprovalStatus { get; set; } = ClaimApprovalStatus.Pending;

        public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; }      
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } 

        public string VerifiedBy { get; set; } = "-";
        public DateTime? VerifiedOn { get; set; }

        public string ApprovedBy { get; set; } = "-";
        public DateTime? ApprovedOn { get; set; }

        [NotMapped]
        public List<string> EncryptedDocuments { get; set; } = new();

        [NotMapped]
        public List<string> OriginalDocuments { get; set; } = new();

        public string EncryptedDocumentsJson { get; set; } = "[]";
        public string OriginalDocumentsJson { get; set; } = "[]";

        public void LoadDocumentLists()
        {
            EncryptedDocuments = JsonSerializer.Deserialize<List<string>>(EncryptedDocumentsJson) ?? new();
            OriginalDocuments = JsonSerializer.Deserialize<List<string>>(OriginalDocumentsJson) ?? new();
        }

        public void SaveDocumentLists()
        {
            EncryptedDocumentsJson = JsonSerializer.Serialize(EncryptedDocuments);
            OriginalDocumentsJson = JsonSerializer.Serialize(OriginalDocuments);
        }
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
