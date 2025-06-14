using GolfBAIST.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GolfBAIST.TechnicalServices
{

    public class MemberService
    {
        private readonly string _connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<MemberService> _logger;

        public MemberService(ILogger<MemberService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Member> GetMembers()
        {
            List<Member> members = new List<Member>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Members", connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Member member = new Member
                    {
                        MemberID = (int)reader["MemberID"],
                        LastName = reader["LastName"] as string,
                        FirstName = reader["FirstName"] as string,
                        Address = reader["Address"] as string,
                        PostalCode = reader["PostalCode"] as string,
                        Phone = reader["Phone"] as string,
                        AlternatePhone = reader["AlternatePhone"] as string,
                        Email = reader["Email"] as string,
                        DateOfBirth = (DateTime)reader["DateOfBirth"],
                        MembershipTypeID = (int)reader["MembershipTypeID"],
                        Sponsor1 = reader["Sponsor1"] as int?,
                        Sponsor2 = reader["Sponsor2"] as int?,
                        MembershipStatus = reader["MembershipStatus"] as string,
                        AnnualDues = (decimal)reader["AnnualDues"],
                        FoodAndBeverageCharge = (decimal)reader["FoodAndBeverageCharge"],
                        OutstandingBalance = (decimal)reader["OutstandingBalance"]
                    };
                    members.Add(member);
                }
            }

            return members;
        }

        public Member GetMemberById(int id)
        {
            Member member = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Members WHERE MemberID = @MemberID", connection);
                command.Parameters.AddWithValue("@MemberID", id);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    member = new Member
                    {
                        MemberID = (int)reader["MemberID"],
                        LastName = reader["LastName"] as string,
                        FirstName = reader["FirstName"] as string,
                        Address = reader["Address"] as string,
                        PostalCode = reader["PostalCode"] as string,
                        Phone = reader["Phone"] as string,
                        AlternatePhone = reader["AlternatePhone"] as string,
                        Email = reader["Email"] as string,
                        DateOfBirth = (DateTime)reader["DateOfBirth"],
                        MembershipTypeID = (int)reader["MembershipTypeID"],
                        Sponsor1 = reader["Sponsor1"] as int?,
                        Sponsor2 = reader["Sponsor2"] as int?,
                        MembershipStatus = reader["MembershipStatus"] as string,
                        AnnualDues = reader["AnnualDues"] != DBNull.Value ? (decimal)reader["AnnualDues"] : 0,
                        FoodAndBeverageCharge = reader["FoodAndBeverageCharge"] != DBNull.Value ? (decimal)reader["FoodAndBeverageCharge"] : 0,
                        OutstandingBalance = reader["OutstandingBalance"] != DBNull.Value ? (decimal)reader["OutstandingBalance"] : 0

                    };
                }
            }

            return member;
        }

        public int AddMember(Member member, SqlConnection connection, SqlTransaction transaction)
        {
            SqlCommand command = new SqlCommand(
                @"AddMember", connection, transaction);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddWithValue("@LastName", member.LastName);
            command.Parameters.AddWithValue("@FirstName", member.FirstName);
            command.Parameters.AddWithValue("@Address", member.Address);
            command.Parameters.AddWithValue("@PostalCode", member.PostalCode);
            command.Parameters.AddWithValue("@Phone", member.Phone);
            command.Parameters.AddWithValue("@AlternatePhone", member.AlternatePhone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", member.Email);
            command.Parameters.AddWithValue("@DateOfBirth", member.DateOfBirth);

            command.Parameters.AddWithValue("@Occupation", member.Occupation);
            command.Parameters.AddWithValue("@CompanyName", member.CompanyName);
            command.Parameters.AddWithValue("@ConsentToEmail", member.ConsentToEmail);
            command.Parameters.AddWithValue("@AgreementAcknowledged", member.AgreementAcknowledged);

            command.Parameters.AddWithValue("@MembershipTypeID", member.MembershipTypeID);
            command.Parameters.AddWithValue("@Sponsor1", member.Sponsor1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Sponsor2", member.Sponsor2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MembershipStatus", member.MembershipStatus);

            return (int)command.ExecuteScalar();
        }


        public void UpdateMember(Member member)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"UPDATE Members 
                      SET LastName = @LastName, FirstName = @FirstName, Address = @Address, PostalCode = @PostalCode, Phone = @Phone, AlternatePhone = @AlternatePhone, Email = @Email, DateOfBirth = @DateOfBirth, MembershipTypeID = @MembershipTypeID, Sponsor1 = @Sponsor1, Sponsor2 = @Sponsor2, MembershipStatus = @MembershipStatus, AnnualDues = @AnnualDues, FoodAndBeverageCharge = @FoodAndBeverageCharge, OutstandingBalance = @OutstandingBalance 
                      WHERE MemberID = @MemberID",
                    connection);

                command.Parameters.AddWithValue("@LastName", member.LastName);
                command.Parameters.AddWithValue("@FirstName", member.FirstName);
                command.Parameters.AddWithValue("@Address", member.Address);
                command.Parameters.AddWithValue("@PostalCode", member.PostalCode);
                command.Parameters.AddWithValue("@Phone", member.Phone);
                command.Parameters.AddWithValue("@AlternatePhone", member.AlternatePhone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", member.Email);
                command.Parameters.AddWithValue("@DateOfBirth", member.DateOfBirth);
                command.Parameters.AddWithValue("@MembershipTypeID", member.MembershipTypeID);
                command.Parameters.AddWithValue("@Sponsor1", member.Sponsor1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Sponsor2", member.Sponsor2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MembershipStatus", member.MembershipStatus);
                command.Parameters.AddWithValue("@AnnualDues", member.AnnualDues);
                command.Parameters.AddWithValue("@FoodAndBeverageCharge", member.FoodAndBeverageCharge);
                command.Parameters.AddWithValue("@OutstandingBalance", member.OutstandingBalance);
                command.Parameters.AddWithValue("@MemberID", member.MemberID);

                command.ExecuteNonQuery();
            }
        }

        public void DeleteMember(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("DELETE FROM Members WHERE MemberID = @MemberID", connection);
                command.Parameters.AddWithValue("@MemberID", id);

                command.ExecuteNonQuery();
            }
        }

        public void ApproveMember(int memberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"UPDATE Members 
                      SET MembershipStatus = 'Active' 
                      WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);
                int rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation("ApproveMember: Rows affected: {RowsAffected} for MemberID: {MemberID}", rowsAffected, memberId);
            }
        }

        public void DenyMember(int memberId, string reason)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"UPDATE Members 
              SET MembershipStatus = 'Denied', DenialReason = @Reason 
              WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);
                command.Parameters.AddWithValue("@Reason", reason ?? (object)DBNull.Value);
                command.ExecuteNonQuery();

                _logger.LogInformation("Denied MemberID {MemberID} with reason: {Reason}", memberId, reason);
            }
        }


        public List<Member> GetPendingMembers()
        {
            List<Member> members = new List<Member>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MemberID, LastName, FirstName, Email, MembershipStatus 
                      FROM Members 
                      WHERE MembershipStatus = 'Pending'", connection);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Member member = new Member
                        {
                            MemberID = reader.GetInt32(0),
                            LastName = reader.GetString(1),
                            FirstName = reader.GetString(2),
                            Email = reader.GetString(3),
                            MembershipStatus = reader.GetString(4)
                        };
                        members.Add(member);
                    }
                }
            }

            return members;
        }
    }
}
