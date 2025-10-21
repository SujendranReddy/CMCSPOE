using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CMCS.Models;

namespace CMCS.Data
{
    public static class ClaimDataStore
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "claims.json");

        private static readonly object _lock = new object();
        private static List<Claim> _claims = new List<Claim>();
        private static int _nextId = 1;

        static ClaimDataStore()
        {
            LoadData();
        }


        public static List<Claim> GetAllClaims()
        {
            lock (_lock) { return _claims.Select(c => Clone(c)).ToList(); }
        }

        public static Claim? GetClaimById(int claimId)
        {
            lock (_lock) { return _claims.FirstOrDefault(c => c.ClaimID == claimId); }
        }

        public static Claim? GetClaimByMonth(string month)
        {
            lock (_lock)
            {
                return _claims.FirstOrDefault(c => string.Equals(c.Month, month, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static void AddClaim(Claim claim)
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));

            lock (_lock)
            {
                claim.ClaimID = _nextId++;
                claim.SubmittedOn = DateTime.UtcNow;

                claim.VerificationStatus = ClaimVerificationStatus.Pending;
                claim.ApprovalStatus = ClaimApprovalStatus.Pending;

                claim.EncryptedDocuments ??= new List<string>();
                claim.OriginalDocuments ??= new List<string>();
                claim.VerifiedBy ??= "-";
                claim.ApprovedBy ??= "-";

                _claims.Add(Clone(claim));
                SaveData();
            }
        }
        public static void UpdateVerificationStatus(int claimId, ClaimVerificationStatus newStatus, string userName = "-", string role = "-")
        {
            lock (_lock)
            {
                var c = _claims.FirstOrDefault(x => x.ClaimID == claimId);
                if (c == null) return;

                c.VerificationStatus = newStatus;
                c.VerifiedBy = $"{userName} ({role})";
                c.VerifiedOn = DateTime.UtcNow;

                SaveData();
            }
        }


        public static void UpdateApprovalStatus(int claimId, ClaimApprovalStatus newStatus, string userName = "-", string role = "-")
        {
            lock (_lock)
            {
                var c = _claims.FirstOrDefault(x => x.ClaimID == claimId);
                if (c == null) return;

                c.ApprovalStatus = newStatus;
                c.ApprovedBy = $"{userName} ({role})";
                c.ApprovedOn = DateTime.UtcNow;

                SaveData();
            }
        }


        public static void AppendEncryptedDocuments(int claimId, IEnumerable<string> fileNames)
        {
            if (fileNames == null) return;

            lock (_lock)
            {
                var c = _claims.FirstOrDefault(x => x.ClaimID == claimId);
                if (c == null) return;

                c.EncryptedDocuments ??= new List<string>();
                c.EncryptedDocuments.AddRange(fileNames);
                SaveData();
            }
        }

        public static void AppendOriginalDocuments(int claimId, IEnumerable<string> fileNames)
        {
            if (fileNames == null) return;

            lock (_lock)
            {
                var c = _claims.FirstOrDefault(x => x.ClaimID == claimId);
                if (c == null) return;

                c.OriginalDocuments ??= new List<string>();
                c.OriginalDocuments.AddRange(fileNames);
                SaveData();
            }
        }


        public static int GetApprovalPendingCount()
        {
            lock (_lock) { return _claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Pending); }
        }

        public static int GetApprovedCount()
        {
            lock (_lock) { return _claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Approved); }
        }

        public static int GetApprovalRejectedCount()
        {
            lock (_lock) { return _claims.Count(c => c.ApprovalStatus == ClaimApprovalStatus.Rejected); }
        }

        public static int GetVerificationPendingCount()
        {
            lock (_lock) { return _claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Pending); }
        }

        public static int GetVerifiedCount()
        {
            lock (_lock) { return _claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Verified); }
        }

        public static int GetVerificationRejectedCount()
        {
            lock (_lock) { return _claims.Count(c => c.VerificationStatus == ClaimVerificationStatus.Rejected); }
        }


        private static void SaveData()
        {
            lock (_lock)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };

                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_claims, options);
                File.WriteAllText(FilePath, json);
            }
        }

        private static void LoadData()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(FilePath))
                    {
                        _claims = new List<Claim>();
                        _nextId = 1;
                        return;
                    }

                    var json = File.ReadAllText(FilePath);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    };

                    _claims = JsonSerializer.Deserialize<List<Claim>>(json, options) ?? new List<Claim>();
                    _nextId = _claims.Any() ? _claims.Max(c => c.ClaimID) + 1 : 1;

                    foreach (var c in _claims)
                    {
                        c.EncryptedDocuments ??= new List<string>();
                        c.OriginalDocuments ??= new List<string>();
                        c.VerifiedBy ??= "-";
                        c.ApprovedBy ??= "-";
                    }
                }
                catch
                {
                    _claims = new List<Claim>();
                    _nextId = 1;
                }
            }
        }

        private static Claim Clone(Claim src)
        {
            return new Claim
            {
                ClaimID = src.ClaimID,
                Month = src.Month,
                HoursWorked = src.HoursWorked,
                HourlyRate = src.HourlyRate,
                VerificationStatus = src.VerificationStatus,
                ApprovalStatus = src.ApprovalStatus,
                SubmittedOn = src.SubmittedOn,
                SubmittedBy = src.SubmittedBy,
                VerifiedBy = src.VerifiedBy ?? "-",
                VerifiedOn = src.VerifiedOn,
                ApprovedBy = src.ApprovedBy ?? "-",
                ApprovedOn = src.ApprovedOn,
                EncryptedDocuments = src.EncryptedDocuments != null ? new List<string>(src.EncryptedDocuments) : new List<string>(),
                OriginalDocuments = src.OriginalDocuments != null ? new List<string>(src.OriginalDocuments) : new List<string>()
            };
        }
    }
}
