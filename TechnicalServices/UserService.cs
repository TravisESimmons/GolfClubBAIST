using GolfBAIST.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GolfBAIST.TechnicalServices
{
    public class UserService
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<UserService> _logger;

        public UserService(ILogger<UserService> logger)
        {
            _logger = logger;
        }

        public void AddUser(User user, SqlConnection connection, SqlTransaction transaction)
        {
            SqlCommand command = new SqlCommand(
                 @"INSERT INTO Users (Username, Password, Email, Role, MemberID, IsEmployee, FirstName, LastName)
                     VALUES (@Username, @Password, @Email, @Role, @MemberID, @IsEmployee, @FirstName, @LastName)", connection);

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Role", user.Role ?? "Employee");
            command.Parameters.AddWithValue("@MemberID", user.MemberID);
            command.Parameters.AddWithValue("@IsEmployee", 1);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);


            command.ExecuteNonQuery();
        }

        public void AddEmployee(User user)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
            INSERT INTO Users (Username, Password, Email, Role, MemberID, IsEmployee, FirstName, LastName)
            VALUES (@Username, @Password, @Email, @Role, @MemberID, @IsEmployee, @FirstName, @LastName)", connection);

                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@Role", user.Role);

                // If MemberID is 0 or null, insert DBNull
                if (!user.MemberID.HasValue || user.MemberID == 0)
                    command.Parameters.AddWithValue("@MemberID", DBNull.Value);
                else
                    command.Parameters.AddWithValue("@MemberID", user.MemberID.Value);


                command.Parameters.AddWithValue("@IsEmployee", 1);
                command.Parameters.AddWithValue("@FirstName", user.FirstName);
                command.Parameters.AddWithValue("@LastName", user.LastName);

                command.ExecuteNonQuery();
            }
        }



        // Validate user login
        public string ValidateLogin(string username, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT Role 
                      FROM Users 
                      WHERE Username = @Username AND Password = @Password", connection);

                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);

                _logger.LogInformation("Executing ValidateLogin query: Username={Username}", username);
                string role = command.ExecuteScalar() as string;
                _logger.LogInformation("ValidateLogin query result: {Role}", role);

                return role;
            }
        }

        public void CreateUser(string username, string password, string email, string role, string firstName, string lastName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand(@"
            INSERT INTO Users (Username, Password, Email, Role, IsEmployee, FirstName, LastName)
            VALUES (@Username, @Password, @Email, @Role, 0, @FirstName, @LastName)", connection);

                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);

                cmd.ExecuteNonQuery();
                _logger.LogInformation("Created new user account for {Username} with role {Role}", username, role);
            }
        }



        public void UpgradeApplicantUser(string email, int newMemberId, SqlConnection conn, SqlTransaction tx)
        {
            var cmd = new SqlCommand(@"
        UPDATE Users
        SET Role = 'User',
            MemberID = @MemberID
        WHERE Email = @Email", conn, tx);

            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@MemberID", newMemberId);

            cmd.ExecuteNonQuery();
            _logger.LogInformation("⬆️ Upgraded user {Email} to Role=User with MemberID={MemberID}", email, newMemberId);
        }


        public int GetMemberID(string username)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MemberID 
                      FROM Users 
                      WHERE Username = @Username", connection);

                command.Parameters.AddWithValue("@Username", username);

                object result = command.ExecuteScalar();

                if (result != null)
                {
                    return (int)result;
                }
                else
                {
                    throw new Exception("MemberID not found for the given username.");
                }
            }
        }

        public int? GetMembershipTypeID(int memberId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MembershipTypeID 
                      FROM Members 
                      WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                _logger.LogInformation("Executing GetMembershipTypeID query: MemberID={MemberID}", memberId);
                object result = command.ExecuteScalar();

                if (result != null)
                {
                    int membershipTypeID = (int)result;
                    _logger.LogInformation("GetMembershipTypeID query result: {MembershipTypeID}", membershipTypeID);
                    return membershipTypeID;
                }
                else
                {
                    _logger.LogInformation("No MembershipTypeID found for MemberID={MemberID}", memberId);
                    return null;
                }
            }
        }

        public User GetUserByUsername(string username)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"SELECT * FROM Users WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            Role = reader["Role"].ToString(),
                            MemberID = reader["MemberID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["MemberID"]),
                            IsEmployee = Convert.ToBoolean(reader["IsEmployee"]),
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString()
                        };
                    }
                }
            }

            return null;
        }


        public static class RoleHelper
        {
            public static bool IsEmployeeRole(string role)
            {
                return role == "ShopClerk" || role == "Committee" || role == "Admin";
            }
        }

    }
}