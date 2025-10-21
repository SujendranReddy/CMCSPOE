using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace CMCS.Tests
{
    public class ClaimDataStoreTests
    {
        [Fact]
        public void AddClaim_ShouldAssignIdAndPendingStatus()
        {
            var claim = new Claim
            {
                SubmittedBy = "John Doe",
                Month = "October",
                HoursWorked = 10,
                HourlyRate = 50 
            };

            ClaimDataStore.AddClaim(claim);
            var retrieved = ClaimDataStore.GetClaimById(claim.ClaimID);

            Assert.NotNull(retrieved);
            Assert.Equal(ClaimVerificationStatus.Pending, retrieved.VerificationStatus);
            Assert.Equal(ClaimApprovalStatus.Pending, retrieved.ApprovalStatus);
            Assert.NotNull(retrieved.OriginalDocuments);
            Assert.NotNull(retrieved.EncryptedDocuments);
            Assert.Empty(retrieved.OriginalDocuments);
            Assert.Empty(retrieved.EncryptedDocuments);
        }

        [Fact]
        public void UpdateVerificationStatus_ShouldChangeToVerified()
        {
            var claim = new Claim { SubmittedBy = "Jane", Month = "Nov", HoursWorked = 5, HourlyRate = 80 };
            ClaimDataStore.AddClaim(claim);

            ClaimDataStore.UpdateVerificationStatus(claim.ClaimID, ClaimVerificationStatus.Verified, "Coordinator A", "Coordinator");
            var updated = ClaimDataStore.GetClaimById(claim.ClaimID);

            Assert.Equal(ClaimVerificationStatus.Verified, updated.VerificationStatus);
            Assert.Equal("Coordinator A (Coordinator)", updated.VerifiedBy);
            Assert.NotNull(updated.VerifiedOn);
        }

        [Fact]
        public void UpdateApprovalStatus_ShouldChangeToApproved()
        {
            var claim = new Claim { SubmittedBy = "Tim", Month = "Dec", HoursWorked = 8, HourlyRate = 75 };
            ClaimDataStore.AddClaim(claim);

            ClaimDataStore.UpdateApprovalStatus(claim.ClaimID, ClaimApprovalStatus.Approved, "Manager B", "Manager");
            var updated = ClaimDataStore.GetClaimById(claim.ClaimID);

            Assert.Equal(ClaimApprovalStatus.Approved, updated.ApprovalStatus);
            Assert.Equal("Manager B (Manager)", updated.ApprovedBy);
            Assert.NotNull(updated.ApprovedOn);
        }


        [Fact]
        public async Task FileEncryptionService_ShouldEncryptAndDecryptSuccessfully()
        {
            var service = new FileEncryptionService();
            var testContent = "This is a secret file.";
            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            var outputPath = Path.Combine(Path.GetTempPath(), "testfile.enc");

            await service.EncryptFileAsync(inputStream, outputPath);
            var decryptedStream = await service.DecryptFileAsync(outputPath);
            var resultText = Encoding.UTF8.GetString(decryptedStream.ToArray());

            Assert.Equal(testContent, resultText);
        }

        [Fact]
        public void GetAllClaims_ShouldReturnListOfClaims()
        {
            var claim = new Claim { SubmittedBy = "Alex", Month = "Jan", HoursWorked = 12, HourlyRate = 60 };
            ClaimDataStore.AddClaim(claim);

            var allClaims = ClaimDataStore.GetAllClaims();

            Assert.NotEmpty(allClaims);
            Assert.Contains(allClaims, c => c.SubmittedBy == "Alex");
        }

        [Fact]
        public void AddClaim_ShouldThrowException_WhenClaimIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ClaimDataStore.AddClaim(null));
        }

        [Fact]
        public async Task DecryptFileAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            var service = new FileEncryptionService();
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.enc");

            var exception = await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await service.DecryptFileAsync(nonExistentPath)
            );

            Assert.Contains("Encrypted file not found", exception.Message);
        }

        [Fact]
        public void GetClaimById_ShouldReturnNull_WhenIdDoesNotExist()
        {
            var result = ClaimDataStore.GetClaimById(-999);
            Assert.Null(result);
        }
    }
}
