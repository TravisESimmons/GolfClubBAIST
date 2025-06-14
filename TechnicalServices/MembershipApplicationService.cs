using GolfBAIST.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GolfBAIST.TechnicalServices
{
    public class MembershipApplicationService
    {
        private readonly string _connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<MembershipApplicationService> _logger;

        public MembershipApplicationService(ILogger<MembershipApplicationService> logger)
        {
            _logger = logger;
            _logger.LogInformation("✅ MembershipApplicationService initialized.");
        }


        public void SubmitApplication(MembershipApplication app)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand(@"
                INSERT INTO MembershipApplications 
                (LastName, FirstName, Address, PostalCode, Phone, AlternatePhone, Email, DateOfBirth, Occupation, CompanyName, ConsentToEmail, AgreementAcknowledged, MembershipTypeID, Sponsor1, Sponsor2, Status, SubmittedDate)
                VALUES 
                (@LastName, @FirstName, @Address, @PostalCode, @Phone, @AlternatePhone, @Email, @DateOfBirth, @Occupation, @CompanyName, @ConsentToEmail, @AgreementAcknowledged, @MembershipTypeID, @Sponsor1, @Sponsor2, 'Pending', GETDATE())",
                conn);

            cmd.Parameters.AddWithValue("@LastName", app.LastName);
            cmd.Parameters.AddWithValue("@FirstName", app.FirstName);
            cmd.Parameters.AddWithValue("@Address", app.Address);
            cmd.Parameters.AddWithValue("@PostalCode", app.PostalCode);
            cmd.Parameters.AddWithValue("@Phone", app.Phone);
            cmd.Parameters.AddWithValue("@AlternatePhone", (object?)app.AlternatePhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", app.Email);
            cmd.Parameters.AddWithValue("@DateOfBirth", app.DateOfBirth);
            cmd.Parameters.AddWithValue("@Occupation", app.Occupation);
            cmd.Parameters.AddWithValue("@CompanyName", app.CompanyName);
            cmd.Parameters.AddWithValue("@ConsentToEmail", app.ConsentToEmail);
            cmd.Parameters.AddWithValue("@AgreementAcknowledged", app.AgreementAcknowledged);
            cmd.Parameters.AddWithValue("@MembershipTypeID", app.MembershipTypeID);
            cmd.Parameters.AddWithValue("@Sponsor1", (object?)app.Sponsor1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Sponsor2", (object?)app.Sponsor2 ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public List<MembershipApplication> GetPendingApplications()
        {
            var list = new List<MembershipApplication>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM MembershipApplications WHERE Status = 'Pending'", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MembershipApplication
                {
                    ApplicationID = (int)reader["ApplicationID"],
                    LastName = reader["LastName"].ToString(),
                    FirstName = reader["FirstName"].ToString(),
                    Email = reader["Email"].ToString(),
                    Status = reader["Status"].ToString(),
                    SubmittedDate = (DateTime)reader["SubmittedDate"],
                    DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime)reader["DateOfBirth"] : DateTime.MinValue,
                    MembershipTypeID = (int)reader["MembershipTypeID"]
                });
            }
            return list;
        }

        public MembershipApplication GetApplicationById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM MembershipApplications WHERE ApplicationID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new MembershipApplication
                {
                    ApplicationID = (int)reader["ApplicationID"],
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    Address = reader["Address"].ToString(),
                    PostalCode = reader["PostalCode"].ToString(),
                    Phone = reader["Phone"].ToString(),
                    AlternatePhone = reader["AlternatePhone"] as string,
                    Email = reader["Email"].ToString(),
                    DateOfBirth = (DateTime)reader["DateOfBirth"],
                    Occupation = reader["Occupation"].ToString(),
                    CompanyName = reader["CompanyName"].ToString(),
                    ConsentToEmail = (bool)reader["ConsentToEmail"],
                    AgreementAcknowledged = (bool)reader["AgreementAcknowledged"],
                    MembershipTypeID = (int)reader["MembershipTypeID"],
                    Sponsor1 = reader["Sponsor1"] as int?,
                    Sponsor2 = reader["Sponsor2"] as int?,
                    Status = reader["Status"].ToString(),
                    DenialReason = reader["DenialReason"] as string,
                    SubmittedDate = (DateTime)reader["SubmittedDate"],
                    DecisionDate = reader["DecisionDate"] as DateTime?
                };
            }

            return null;
        }

        // APPROVE – with conn/tx
        public void ApproveApplication(int applicationId, SqlConnection conn, SqlTransaction tx)
        {
            var cmd = new SqlCommand(@"
                UPDATE MembershipApplications 
                SET Status = 'Approved', DecisionDate = GETDATE() 
                WHERE ApplicationID = @id", conn, tx);
            cmd.Parameters.AddWithValue("@id", applicationId);
            cmd.ExecuteNonQuery();
        }

        // APPROVE – overload with auto connection
        public void ApproveApplication(int applicationId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var tx = conn.BeginTransaction();

            ApproveApplication(applicationId, conn, tx);

            tx.Commit();
        }

        // DENY – with conn/tx
        public void DenyApplication(int applicationId, string reason, SqlConnection conn, SqlTransaction tx)
        {
            var cmd = new SqlCommand(@"
                UPDATE MembershipApplications 
                SET Status = 'Denied', DenialReason = @reason, DecisionDate = GETDATE() 
                WHERE ApplicationID = @id", conn, tx);
            cmd.Parameters.AddWithValue("@id", applicationId);
            cmd.Parameters.AddWithValue("@reason", reason ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        // DENY – overload with auto connection
        public void DenyApplication(int applicationId, string reason)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var tx = conn.BeginTransaction();

            DenyApplication(applicationId, reason, conn, tx);

            tx.Commit();
        }

        public void ApproveApplicationAndCreateMemberAndUser(MembershipApplication app, UserService userService, MemberService memberService)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var tx = conn.BeginTransaction();

            _logger.LogInformation("🔁 Begin transaction for approving ApplicationID={AppID}", app.ApplicationID);

            try
            {
                _logger.LogInformation("📥 Creating Member record for {FirstName} {LastName}...", app.FirstName, app.LastName);

                var member = new Member
                {
                    FirstName = app.FirstName,
                    LastName = app.LastName,
                    Address = app.Address,
                    PostalCode = app.PostalCode,
                    Phone = app.Phone,
                    AlternatePhone = app.AlternatePhone,
                    Email = app.Email,
                    DateOfBirth = app.DateOfBirth,
                    Occupation = app.Occupation,
                    CompanyName = app.CompanyName,
                    ConsentToEmail = app.ConsentToEmail,
                    AgreementAcknowledged = app.AgreementAcknowledged,
                    MembershipTypeID = app.MembershipTypeID,
                    Sponsor1 = app.Sponsor1,
                    Sponsor2 = app.Sponsor2,
                    MembershipStatus = "Active"
                };


                int memberId = memberService.AddMember(member, conn, tx);
                _logger.LogInformation("✅ Member created with MemberID={MemberID}", memberId);

                _logger.LogInformation("📤 Creating User for approved member: {Email}", app.Email);

                var user = new User
                {
                    Username = app.Email,
                    Password = "Temp123!",
                    Email = app.Email,
                    Role = "User",
                    MemberID = memberId
                };

                userService.AddUser(user, conn, tx);
                _logger.LogInformation("✅ User account created for {Email}", user.Email);

                _logger.LogInformation("🟢 Approving ApplicationID={AppID}", app.ApplicationID);
                ApproveApplication(app.ApplicationID, conn, tx);
                _logger.LogInformation("✅ Application status updated to Approved");

                tx.Commit();
                _logger.LogInformation("🎉 Transaction COMMITTED for ApplicationID={AppID}", app.ApplicationID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ROLLBACK triggered during approval for ApplicationID={AppID}", app.ApplicationID);
                tx.Rollback();
                throw;
            }
        }

        public void SimpleApprove(int applicationId, MemberService memberService, UserService userService)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Get application
            var app = GetApplicationById(applicationId);
            if (app == null) throw new Exception("Application not found.");

            var member = new Member
            {
                FirstName = app.FirstName,
                LastName = app.LastName,
                Address = app.Address,
                PostalCode = app.PostalCode,
                Phone = app.Phone,
                AlternatePhone = app.AlternatePhone,
                Email = app.Email,
                DateOfBirth = app.DateOfBirth,
                Occupation = app.Occupation,
                CompanyName = app.CompanyName,
                ConsentToEmail = app.ConsentToEmail,
                AgreementAcknowledged = app.AgreementAcknowledged,
                MembershipTypeID = app.MembershipTypeID,
                Sponsor1 = app.Sponsor1,
                Sponsor2 = app.Sponsor2,
                MembershipStatus = "Active"
            };

            int newMemberId;
            using var tx = conn.BeginTransaction();
            newMemberId = memberService.AddMember(member, conn, tx);

            // Create user
            var user = new User
            {
                Username = app.Email,
                Password = "Temp123!",
                Email = app.Email,
                Role = "User",
                MemberID = newMemberId
            };
            userService.UpgradeApplicantUser(app.Email, newMemberId, conn, tx);


            // Mark application approved
            var cmd = new SqlCommand(@"
        UPDATE MembershipApplications
        SET Status = 'Approved', DecisionDate = GETDATE()
        WHERE ApplicationID = @AppID", conn, tx);
            cmd.Parameters.AddWithValue("@AppID", applicationId);
            cmd.ExecuteNonQuery();

            tx.Commit();
        }


    }
}
