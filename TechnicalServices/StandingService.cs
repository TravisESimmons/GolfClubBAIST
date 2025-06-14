using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Collections.Generic;
using GolfBAIST.Models;

namespace GolfBAIST.TechnicalServices
{
    public class StandingService
    {
        private readonly string _connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<StandingService> _logger;

        public StandingService(ILogger<StandingService> logger)
        {
            _logger = logger;
        }

        // CREATE STANDING TEE TIME 

        public bool CreateStandingTeeTimeRequest(int memberId, string dayOfWeek, TimeSpan startTime, TimeSpan endTime, DateTime startDate, DateTime endDate, List<int> playerIds)
        {
            try
            {
                _logger.LogInformation("Attempting to create standing tee time request for MemberID={MemberID}", memberId);

                // Validate 
                ValidateStandingTeeTimeRequest(new StandingTeeTimeRequest
                {
                    MemberID = memberId,
                    RequestedDayOfWeek = dayOfWeek,
                    RequestedStartTime = startTime,
                    RequestedEndTime = endTime,
                    StartDate = startDate,
                    EndDate = endDate,
                    AdditionalPlayer1ID = playerIds.Count > 1 ? playerIds[1] : (int?)null,
                    AdditionalPlayer2ID = playerIds.Count > 2 ? playerIds[2] : (int?)null,
                    AdditionalPlayer3ID = playerIds.Count > 3 ? playerIds[3] : (int?)null
                });

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("AddStandingTeeTime", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@MemberID", memberId);
                    command.Parameters.AddWithValue("@RequestedDayOfWeek", dayOfWeek);
                    command.Parameters.AddWithValue("@RequestedStartTime", startTime);
                    command.Parameters.AddWithValue("@RequestedEndTime", endTime);
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    for (int i = 1; i < playerIds.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@AdditionalPlayer{i}ID", playerIds[i]);
                    }

                    int rowsAffected = command.ExecuteNonQuery();
                    _logger.LogInformation("Standing tee time request created successfully. Rows affected: {RowsAffected}", rowsAffected);

                    // Handle both positive values and -1 (which stored procedures often return)
                    return rowsAffected != 0;
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL Error: {Message}", ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
                return false;
            }
        }

        // MEMBER VALDIATION

        public List<StandingTeeTimeRequest> GetStandingTeeTimesByMemberID(int memberId)
        {
            List<StandingTeeTimeRequest> standingTeeTimes = new List<StandingTeeTimeRequest>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT * 
              FROM StandingTeeTimes 
              WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        StandingTeeTimeRequest standingTeeTime = new StandingTeeTimeRequest
                        {
                            RequestID = (int)reader["RequestID"],
                            MemberID = (int)reader["MemberID"],
                            RequestedDayOfWeek = reader["RequestedDayOfWeek"].ToString(),
                            RequestedStartTime = (TimeSpan)reader["RequestedStartTime"],
                            RequestedEndTime = (TimeSpan)reader["RequestedEndTime"],
                            StartDate = (DateTime)reader["StartDate"],
                            EndDate = (DateTime)reader["EndDate"],
                            AdditionalPlayer1ID = reader["AdditionalPlayer1ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer1ID"] : null,
                            AdditionalPlayer2ID = reader["AdditionalPlayer2ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer2ID"] : null,
                            AdditionalPlayer3ID = reader["AdditionalPlayer3ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer3ID"] : null,
                            ApprovedTeeTime = reader["ApprovedTeeTime"] != DBNull.Value ? (TimeSpan?)reader["ApprovedTeeTime"] : null,
                            ApprovedBy = reader["ApprovedBy"] != DBNull.Value ? reader["ApprovedBy"].ToString() : null,
                            ApprovedDate = reader["ApprovedDate"] != DBNull.Value ? (DateTime?)reader["ApprovedDate"] : null,
                            CancellationRequested = reader["CancellationRequested"] != DBNull.Value && (bool)reader["CancellationRequested"]
                        };
                        standingTeeTimes.Add(standingTeeTime);
                    }
                }
            }

            return standingTeeTimes;
        }


        public int? GetMembershipTypeID(int memberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MembershipTypeID 
                  FROM Members 
                  WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                object result = command.ExecuteScalar();
                return result != null ? (int?)result : null;
            }
        }


        // STANDING VALIDATION 

        public bool IsValidInterval(TimeSpan startTime, TimeSpan endTime)
        {
            _logger.LogInformation("Validating time intervals: StartTime={StartTime}, EndTime={EndTime}", startTime, endTime);

            bool isValidInterval = (endTime - startTime).TotalMinutes == 8;


            if (!isValidInterval)
            {
                _logger.LogWarning("Invalid interval: {StartTime} to {EndTime} is not exactly 8 minutes.", startTime, endTime);
                return false;
            }

            bool isValidStart = startTime.Minutes % 8 == 0;
            bool isValidEnd = endTime.Minutes % 8 == 0;

            if (!isValidStart || !isValidEnd)
            {
                _logger.LogWarning("Invalid start or end time alignment: StartTime={StartTime}, EndTime={EndTime}.", startTime, endTime);
            }

            return isValidStart && isValidEnd && isValidInterval;
        }

        // NEEDS HASEXISTINGSTANDING TEE TIME 

        public void ValidateStandingTeeTimeRequest(StandingTeeTimeRequest request)
        {
            // Check for Existing Requests
            //if (HasStandingTeeTimeThisWeek(request.MemberID))
            //{
            //    throw new ArgumentException("Each shareholder member can only have one standing tee time request per week.");
            //}

            // Validate Foursome
            if (!request.AdditionalPlayer1ID.HasValue || !request.AdditionalPlayer2ID.HasValue || !request.AdditionalPlayer3ID.HasValue)
            {
                throw new ArgumentException("Standing tee time requests must be for a foursome (4 players).");
            }

            // Validate Requested Day and Time
            if (!IsValidDayOfWeek(request.RequestedDayOfWeek))
            {
                throw new ArgumentException("Invalid requested day of the week.");
            }

            if (request.RequestedStartTime.TotalHours < 0 || request.RequestedStartTime.TotalHours >= 24)
            {
                throw new ArgumentException("Invalid requested start time.");
            }

            if (request.RequestedEndTime.TotalHours < 0 || request.RequestedEndTime.TotalHours >= 24)
            {
                throw new ArgumentException("Invalid requested end time.");
            }

            // Validate Start and End Dates
            if (request.StartDate < DateTime.Today)
            {
                throw new ArgumentException("Start date cannot be in the past.");
            }

            if (request.EndDate < request.StartDate)
            {
                throw new ArgumentException("End date cannot be before start date.");
            }

            // Assign Priority 
            request.PriorityNumber = AssignPriority(request);
        }

        private bool IsValidDayOfWeek(string dayOfWeek)
        {
            return Enum.TryParse(dayOfWeek, true, out DayOfWeek _);
        }

        public bool HasStandingTeeTimeThisWeek(int memberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(*) 
              FROM StandingTeeTimes 
              WHERE MemberID = @MemberID 
              AND DATEPART(week, StartDate) = DATEPART(week, GETDATE())
              AND DATEPART(year, StartDate) = DATEPART(year, GETDATE())", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                int count = (int)command.ExecuteScalar();
                _logger.LogInformation("HasStandingTeeTimeThisWeek: MemberID={MemberID}, Count={Count}", memberId, count);
                return count > 0;
            }
        }

        // EMPLOYEES CONTROL

        public List<StandingTeeTimeRequest> GetPendingStandingTeeTimeRequests()
        {
            var requests = new List<StandingTeeTimeRequest>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT * 
              FROM StandingTeeTimes
              WHERE ApprovedBy IS NULL", connection);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var request = new StandingTeeTimeRequest
                        {
                            RequestID = (int)reader["RequestID"],
                            MemberID = (int)reader["MemberID"],
                            RequestedDayOfWeek = reader["RequestedDayOfWeek"].ToString(),
                            RequestedStartTime = reader.IsDBNull("RequestedStartTime") ? TimeSpan.Zero : (TimeSpan)reader["RequestedStartTime"],
                            RequestedEndTime = reader.IsDBNull("RequestedEndTime") ? TimeSpan.Zero : (TimeSpan)reader["RequestedEndTime"],
                            StartDate = reader.IsDBNull("StartDate") ? DateTime.MinValue : (DateTime)reader["StartDate"],
                            EndDate = reader.IsDBNull("EndDate") ? DateTime.MinValue : (DateTime)reader["EndDate"],
                            AdditionalPlayer1ID = reader["AdditionalPlayer1ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer1ID"] : null,
                            AdditionalPlayer2ID = reader["AdditionalPlayer2ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer2ID"] : null,
                            AdditionalPlayer3ID = reader["AdditionalPlayer3ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer3ID"] : null,
                            CancellationRequested = reader["CancellationRequested"] != DBNull.Value && (bool)reader["CancellationRequested"]
                        };

                        requests.Add(request);
                    }
                }
            }

            return requests;
        }


        private int AssignPriority(StandingTeeTimeRequest request)
        {
            return 1;
        }


        public void RequestCancellation(int requestId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                _logger.LogInformation("RequestCancellation: Connection opened for Standing RequestID: {RequestID}", requestId);
                SqlCommand command = new SqlCommand(
                    @"UPDATE StandingTeeTimes 
              SET CancellationRequested = 1 
              WHERE RequestID = @RequestID", connection);

                command.Parameters.AddWithValue("@RequestID", requestId);
                int rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation("Standing RequestCancellation: Rows affected: {RowsAffected} for RequestID: {RequestID}", rowsAffected, requestId);
            }
        }


        public bool ApproveStandingTeeTimeRequest(int requestId, string approvedBy)
        {
            try
            {
                _logger.LogInformation("Attempting to approve standing tee time request for RequestID={RequestID}", requestId);

                bool success = false;

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("ApproveStandingTeeTimeRequest", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.AddWithValue("@RequestID", requestId);
                    command.Parameters.AddWithValue("@ApprovedTeeTime", DBNull.Value); // Assuming the approved time is the same as requested time
                    command.Parameters.AddWithValue("@ApprovedBy", approvedBy);

                    int rowsAffected = command.ExecuteNonQuery();
                    success = rowsAffected > 0;
                    _logger.LogInformation("Standing tee time request approved successfully. Rows affected: {RowsAffected}", rowsAffected);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while approving standing tee time request for RequestID={RequestID}", requestId);
                return false;
            }
        }

        public void DenyStandingTeeTimeRequest(int requestId)
        {
            try
            {
                _logger.LogInformation("Attempting to deny standing tee time request for RequestID={RequestID}", requestId);

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(
                        @"DELETE FROM StandingTeeTimes
                          WHERE RequestID = @RequestID", connection);

                    command.Parameters.AddWithValue("@RequestID", requestId);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Standing tee time request denied successfully. Rows affected: {RowsAffected}", rowsAffected);
                    }
                    else
                    {
                        _logger.LogWarning("No rows affected while denying standing tee time request for RequestID={RequestID}", requestId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while denying standing tee time request for RequestID={RequestID}", requestId);
            }
        }


        public List<StandingTeeTimeRequest> GetStandingCancellationRequests()
        {
            var requests = new List<StandingTeeTimeRequest>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("SELECT * FROM StandingTeeTimes WHERE CancellationRequested = 1", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var request = new StandingTeeTimeRequest
                {
                    RequestID = (int)reader["RequestID"],
                    MemberID = (int)reader["MemberID"],
                    RequestedDayOfWeek = reader["RequestedDayOfWeek"].ToString(),
                    RequestedStartTime = reader["RequestedStartTime"] != DBNull.Value ? (TimeSpan)reader["RequestedStartTime"] : TimeSpan.Zero,
                    RequestedEndTime = reader["RequestedEndTime"] != DBNull.Value ? (TimeSpan)reader["RequestedEndTime"] : TimeSpan.Zero,
                    StartDate = reader["StartDate"] != DBNull.Value ? (DateTime)reader["StartDate"] : DateTime.MinValue,
                    EndDate = reader["EndDate"] != DBNull.Value ? (DateTime)reader["EndDate"] : DateTime.MinValue,
                    AdditionalPlayer1ID = reader["AdditionalPlayer1ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer1ID"] : null,
                    AdditionalPlayer2ID = reader["AdditionalPlayer2ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer2ID"] : null,
                    AdditionalPlayer3ID = reader["AdditionalPlayer3ID"] != DBNull.Value ? (int?)reader["AdditionalPlayer3ID"] : null,
                    ApprovedTeeTime = reader["ApprovedTeeTime"] != DBNull.Value ? (TimeSpan?)reader["ApprovedTeeTime"] : null,
                    ApprovedBy = reader["ApprovedBy"] != DBNull.Value ? reader["ApprovedBy"].ToString() : null,
                    ApprovedDate = reader["ApprovedDate"] != DBNull.Value ? (DateTime?)reader["ApprovedDate"] : null,
                    CancellationRequested = reader["CancellationRequested"] != DBNull.Value && (bool)reader["CancellationRequested"]
                };
                requests.Add(request);
            }
            return requests;
        }


        public void DeleteStandingRequest(int requestId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM StandingTeeTimes WHERE RequestID = @RequestID", conn);
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.ExecuteNonQuery();
        }

        public void DenyCancellation(int requestId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var cmd = new SqlCommand("UPDATE StandingTeeTimes SET CancellationRequested = 0 WHERE RequestID = @RequestID", conn);
            cmd.Parameters.AddWithValue("@RequestID", requestId);
            cmd.ExecuteNonQuery();
        }

    }
}




