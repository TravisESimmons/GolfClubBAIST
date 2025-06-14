
using GolfBAIST.Models;
using System.Data;
using Microsoft.Data.SqlClient;

using System.Data.SqlTypes;



namespace GolfBAIST.TechnicalServices
{
    public class TeeTimesService
    {
        private readonly string _connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly ILogger<TeeTimesService> _logger;
        public TeeTimesService(ILogger<TeeTimesService> logger)
        {
            _logger = logger;
        }

        // VALIDATION
        public bool IsValidInterval(TimeSpan startTime, TimeSpan endTime)
        {
            _logger.LogInformation("Validating time intervals: StartTime={StartTime}, EndTime={EndTime}", startTime, endTime);

            // Ensure interval is exactly 8 minutes
            bool isValidInterval = (endTime - startTime).TotalMinutes == 8;

            // Log specifics
            if (!isValidInterval)
            {
                _logger.LogWarning("Invalid interval: {StartTime} to {EndTime} is not exactly 8 minutes.", startTime, endTime);
                return false;
            }

            // Ensure StartTime and EndTime are divisible by 8
            bool isValidStart = startTime.Minutes % 8 == 0;
            bool isValidEnd = endTime.Minutes % 8 == 0;

            if (!isValidStart || !isValidEnd)
            {
                _logger.LogWarning("Invalid start or end time alignment: StartTime={StartTime}, EndTime={EndTime}.", startTime, endTime);
            }

            return isValidStart && isValidEnd && isValidInterval;
        }
        public void ValidateTeeTimeParameters(TeeTime teeTime)
        {
            _logger.LogInformation("Validating TeeTime: Date={Date}, StartTime={StartTime}, EndTime={EndTime}, MemberID={MemberID}",
                teeTime.Date, teeTime.StartTime, teeTime.EndTime, teeTime.MemberID);

            // Check date range
            if (teeTime.Date < DateTime.Today)
            {
                throw new ArgumentException("Tee times must be booked for today or a future date.");
            }

            // SQL Server valid date range check (optional if DateTime is from date picker)
            if (teeTime.Date < new DateTime(1753, 1, 1) || teeTime.Date > new DateTime(9999, 12, 31))
            {
                throw new ArgumentException("Invalid Date");
            }

            // Start time must be valid
            if (teeTime.StartTime.TotalHours < 6 || teeTime.StartTime.TotalHours >= 20)
            {
                throw new ArgumentException("Start time must be between 6:00 AM and 8:00 PM.");
            }

            // End time must be valid and after start time
            if (teeTime.EndTime <= teeTime.StartTime)
            {
                throw new ArgumentException("End time must be after start time.");
            }

            // Must be exactly 8-minute interval
            var expectedEnd = teeTime.StartTime.Add(TimeSpan.FromMinutes(8));
            if (teeTime.EndTime != expectedEnd)
            {
                throw new ArgumentException("Tee time must be exactly 8 minutes.");
            }

            // MemberID check
            if (teeTime.MemberID <= 0)
            {
                throw new ArgumentException("Invalid MemberID");
            }

            // Player count check
            if (teeTime.Players < 1 || teeTime.Players > 4)
            {
                throw new ArgumentException("Player count must be between 1 and 4.");
            }

            // Optional: validate additional members
            if (teeTime.AdditionalMemberIDs != null)
            {
                foreach (var id in teeTime.AdditionalMemberIDs)
                {
                    if (id <= 0)
                    {
                        throw new ArgumentException($"Invalid additional member ID: {id}");
                    }

                    var member = GetMemberById(id);
                    if (member == null)
                    {
                        throw new ArgumentException($"Member ID {id} does not exist.");
                    }
                }
            }
        }
        public bool ValidateMemberIDs(List<int> memberIDs, TimeSpan bookingTime)
        {
            foreach (var memberId in memberIDs)
            {
                int? membershipTypeID = GetMembershipTypeID(memberId);
                if (!membershipTypeID.HasValue || !IsValidTeeTime(membershipTypeID.Value, bookingTime))
                {
                    return false;
                }
            }
            return true;
        }
        public bool IsValidTeeTime(int membershipTypeID, TimeSpan time)
        {
            var TeeTimeRules = new Dictionary<int, List<(TimeSpan, TimeSpan)>>()
        {
            { 1, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(23, 59, 59)) } }, // Gold Shareholder
            { 2, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(23, 59, 59)) } }, // Gold Associate
            { 3, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(15, 0, 0)), (new TimeSpan(17, 30, 0), new TimeSpan(23, 59, 59)) } }, // Silver Shareholder Spouse
            { 4, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(15, 0, 0)), (new TimeSpan(17, 30, 0), new TimeSpan(23, 59, 59)) } }, // Silver Associate Spouse
            { 5, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(15, 0, 0)), (new TimeSpan(18, 0, 0), new TimeSpan(23, 59, 59)) } }, // Bronze Pee Wee
            { 6, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(15, 0, 0)), (new TimeSpan(18, 0, 0), new TimeSpan(23, 59, 59)) } }, // Bronze Junior
            { 7, new List<(TimeSpan, TimeSpan)> { (new TimeSpan(0, 0, 0), new TimeSpan(15, 0, 0)), (new TimeSpan(18, 0, 0), new TimeSpan(23, 59, 59)) } }, // Bronze Intermediate
            { 8, new List<(TimeSpan, TimeSpan)>() } // Copper Social
        };

            if (TeeTimeRules.TryGetValue(membershipTypeID, out var allowedTimes))
            {
                foreach (var (start, end) in allowedTimes)
                {
                    if (time >= start && time <= end)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public bool IsTimeSlotAvailable(DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeTeeTimeId = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(*) 
              FROM TeeTimes 
              WHERE Date = @Date 
                AND StartTime = @StartTime 
                AND EndTime = @EndTime
                AND (@ExcludeTeeTimeID IS NULL OR TeeTimeID != @ExcludeTeeTimeID)", connection);

                command.Parameters.AddWithValue("@Date", date);
                command.Parameters.AddWithValue("@StartTime", startTime);
                command.Parameters.AddWithValue("@EndTime", endTime);
                command.Parameters.AddWithValue("@ExcludeTeeTimeID", (object?)excludeTeeTimeId ?? DBNull.Value);

                int count = (int)command.ExecuteScalar();
                return count == 0;
            }
        }


        // CRUD TEE TIME  
        public bool AddTeeTime(TeeTime teeTime, out int newTeeTimeID, out int newScoreID)
        {
            _logger.LogInformation("Executing stored procedure AddTeeTime...");

            newTeeTimeID = 0;
            newScoreID = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand("AddTeeTime", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    // Input parameters
                    command.Parameters.AddWithValue("@Date", teeTime.Date);
                    command.Parameters.AddWithValue("@StartTime", teeTime.StartTime);
                    command.Parameters.AddWithValue("@EndTime", teeTime.EndTime);
                    command.Parameters.AddWithValue("@MemberID", teeTime.MemberID);
                    command.Parameters.AddWithValue("@Players", teeTime.Players);
                    command.Parameters.AddWithValue("@Phone", teeTime.Phone);
                    command.Parameters.AddWithValue("@Carts", teeTime.Carts);
                    command.Parameters.AddWithValue("@EmployeeName", teeTime.EmployeeName ?? (object)DBNull.Value);

                    // Add these lines to handle up to 3 additional members
                    command.Parameters.AddWithValue("@AdditionalMemberID1", teeTime.AdditionalMemberIDs.Count > 0 ? teeTime.AdditionalMemberIDs[0] : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AdditionalMemberID2", teeTime.AdditionalMemberIDs.Count > 1 ? teeTime.AdditionalMemberIDs[1] : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AdditionalMemberID3", teeTime.AdditionalMemberIDs.Count > 2 ? teeTime.AdditionalMemberIDs[2] : (object)DBNull.Value);

                    // Output parameters
                    SqlParameter outputTeeTimeID = new SqlParameter("@NewTeeTimeID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputTeeTimeID);

                    SqlParameter outputScoreID = new SqlParameter("@NewScoreID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputScoreID);

                    int rowsAffected = command.ExecuteNonQuery();

                    // Retrieve the output values
                    if (outputTeeTimeID.Value != DBNull.Value && outputScoreID.Value != DBNull.Value)
                    {
                        newTeeTimeID = Convert.ToInt32(outputTeeTimeID.Value);
                        newScoreID = Convert.ToInt32(outputScoreID.Value);
                        _logger.LogInformation("Tee time successfully inserted with TeeTimeID={TeeTimeID}, ScoreID={ScoreID}", newTeeTimeID, newScoreID);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Stored procedure executed, but output values were NULL.");
                        return false;
                    }
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Exception: {Message}", sqlEx.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in AddTeeTime.");
                }
            }

            return false;
        }
        public bool UpdateTeeTime(TeeTime teeTime)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(@"
                    UPDATE TeeTimes
                    SET 
                        Date = @Date,
                        StartTime = @StartTime,
                        EndTime = @EndTime,
                        Players = @Players,
                        Carts = @Carts,
                        AdditionalMemberID1 = @AdditionalMemberID1,
                        AdditionalMemberID2 = @AdditionalMemberID2,
                        AdditionalMemberID3 = @AdditionalMemberID3
                    WHERE TeeTimeID = @TeeTimeID", connection);


                    command.Parameters.AddWithValue("@Date", teeTime.Date);


                    command.Parameters.AddWithValue("@TeeTimeID", teeTime.TeeTimeID);
                    command.Parameters.AddWithValue("@StartTime", teeTime.StartTime);
                    command.Parameters.AddWithValue("@EndTime", teeTime.EndTime);
                    command.Parameters.AddWithValue("@Carts", (object?)teeTime.Carts ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AdditionalMemberID1", teeTime.AdditionalMemberIDs.Count > 0 ? (object)teeTime.AdditionalMemberIDs[0] : DBNull.Value);
                    command.Parameters.AddWithValue("@AdditionalMemberID2", teeTime.AdditionalMemberIDs.Count > 1 ? (object)teeTime.AdditionalMemberIDs[1] : DBNull.Value);
                    command.Parameters.AddWithValue("@AdditionalMemberID3", teeTime.AdditionalMemberIDs.Count > 2 ? (object)teeTime.AdditionalMemberIDs[2] : DBNull.Value);
                    command.Parameters.AddWithValue("@Players", teeTime.Players);

                    int rows = command.ExecuteNonQuery();
                    _logger.LogInformation("TeeTimeID={TeeTimeID} updated. Rows affected: {Rows}", teeTime.TeeTimeID, rows);

                    return rows > 0;
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL error during TeeTime update.");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during TeeTime update.");
                    return false;
                }
            }
        }
        public bool DeleteTeeTime(int teeTimeID)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Delete from Scores 
                    SqlCommand deleteScores = new SqlCommand("DELETE FROM Scores WHERE TeeTimeID = @TeeTimeID", conn, transaction);
                    deleteScores.Parameters.AddWithValue("@TeeTimeID", teeTimeID);
                    deleteScores.ExecuteNonQuery();

                    // Delete from TeeTimePlayers
                    SqlCommand deletePlayers = new SqlCommand("DELETE FROM TeeTimePlayers WHERE TeeTimeID = @TeeTimeID", conn, transaction);
                    deletePlayers.Parameters.AddWithValue("@TeeTimeID", teeTimeID);
                    deletePlayers.ExecuteNonQuery();

                    // Delete from TeeTimes
                    SqlCommand deleteTeeTime = new SqlCommand("DELETE FROM TeeTimes WHERE TeeTimeID = @TeeTimeID", conn, transaction);
                    deleteTeeTime.Parameters.AddWithValue("@TeeTimeID", teeTimeID);
                    int rowsAffected = deleteTeeTime.ExecuteNonQuery();

                    transaction.Commit();
                    return rowsAffected > 0;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        // JOIN TEE TIME 
        public bool JoinTeeTime(int teeTimeId, int memberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(@"
            UPDATE TeeTimes
            SET Players = Players + 1,
                AdditionalMemberID1 = CASE WHEN AdditionalMemberID1 IS NULL THEN @MemberID ELSE AdditionalMemberID1 END,
                AdditionalMemberID2 = CASE WHEN AdditionalMemberID1 IS NOT NULL AND AdditionalMemberID2 IS NULL THEN @MemberID ELSE AdditionalMemberID2 END,
                AdditionalMemberID3 = CASE WHEN AdditionalMemberID2 IS NOT NULL AND AdditionalMemberID3 IS NULL THEN @MemberID ELSE AdditionalMemberID3 END
            WHERE TeeTimeID = @TeeTimeID AND Players < 4", connection);

                command.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
                command.Parameters.AddWithValue("@MemberID", memberId);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        // GET
        public List<TeeTime> GetTeeTimes()
        {
            List<TeeTime> teeTimes = new List<TeeTime>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(@"
                        SELECT t.*, s.ScoreID 
                        FROM TeeTimes t 
                        LEFT JOIN Scores s ON t.TeeTimeID = s.TeeTimeID", connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        TeeTime teeTime = new TeeTime
                        {
                            TeeTimeID = (int)reader["TeeTimeID"],
                            Date = (DateTime)reader["Date"],
                            StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                            EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                            MemberID = (int)reader["MemberID"],
                            Players = (int)reader["Players"],
                            Phone = reader["Phone"] as string,
                            Carts = reader["Carts"] as int?,
                            EmployeeName = reader["EmployeeName"] as string,
                            ScoreID = reader.IsDBNull(reader.GetOrdinal("ScoreID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ScoreID")),
                            AdditionalMemberIDs = new List<int>()
                        };

                        if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID1")))
                            teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID1")));
                        if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID2")))
                            teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID2")));
                        if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID3")))
                            teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID3")));

                        teeTimes.Add(teeTime);
                    }
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL Error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                }
                finally
                {
                    connection.Close();
                    _logger.LogInformation("Connection closed.");
                }
            }

            return teeTimes;
        }
        public List<TeeTime> GetAllTeeTimes()
        {
            return GetTeeTimes();
        }
        public List<TimeSlot> GetAvailableTimeSlots(DateTime date)
        {
            var startHour = 6;
            var endHour = 20;
            var intervalMinutes = 8;
            var allSlots = new List<TimeSlot>();

            for (int hour = startHour; hour < endHour; hour++)
            {
                for (int minute = 0; minute < 60; minute += intervalMinutes)
                {
                    var startTime = new TimeSpan(hour, minute, 0);
                    var endTime = startTime.Add(TimeSpan.FromMinutes(intervalMinutes));

                    allSlots.Add(new TimeSlot
                    {
                        StartTime = startTime.ToString(@"hh\:mm"),
                        EndTime = endTime.ToString(@"hh\:mm")
                    });
                }
            }

            var bookedSlots = new List<TimeSpan>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT StartTime FROM TeeTimes WHERE Date = @Date", connection);
                command.Parameters.AddWithValue("@Date", date.Date);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            bookedSlots.Add(reader.GetTimeSpan(0));
                        }
                    }
                }
            }

            return allSlots
                .Where(slot => !bookedSlots.Any(booked =>
                    booked.ToString(@"hh\:mm") == slot.StartTime))
                .ToList();
        }
        public List<TeeTime> GetJoinableTeeTimes()
        {
            var joinableTeeTimes = new List<TeeTime>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
            SELECT TeeTimeID, Date, StartTime, EndTime, MemberID, Players, Phone, Carts, EmployeeName 
            FROM TeeTimes
            WHERE Players < 4", connection);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var teeTime = new TeeTime
                        {
                            TeeTimeID = reader.GetInt32(0),
                            Date = reader.GetDateTime(1),
                            StartTime = !reader.IsDBNull(2) ? reader.GetTimeSpan(2) : TimeSpan.Zero,
                            EndTime = !reader.IsDBNull(3) ? reader.GetTimeSpan(3) : TimeSpan.Zero,
                            MemberID = reader.GetInt32(4),
                            Players = reader.GetInt32(5),
                            Phone = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                            Carts = !reader.IsDBNull(7) ? reader.GetInt32(7) : (int?)null,
                            EmployeeName = !reader.IsDBNull(8) ? reader.GetString(8) : null
                        };

                        joinableTeeTimes.Add(teeTime);
                    }
                }
            }

            return joinableTeeTimes;
        }
        public List<Member> GetPlayersByTeeTimeID(int teeTimeId)
        {
            List<Member> players = new();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT m.MemberID, m.FirstName, m.LastName
        FROM TeeTimes t
        JOIN Members m ON
            m.MemberID = t.MemberID OR
            m.MemberID = t.AdditionalMemberID1 OR
            m.MemberID = t.AdditionalMemberID2 OR
            m.MemberID = t.AdditionalMemberID3
        WHERE t.TeeTimeID = @TeeTimeID", conn);

            cmd.Parameters.AddWithValue("@TeeTimeID", teeTimeId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                players.Add(new Member
                {
                    MemberID = (int)reader["MemberID"],
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString()
                });
            }

            return players;
        }
        public Dictionary<int, string> GetNamesForExistingPlayers(int teeTimeId, int currentUserId, string role)
        {
            Dictionary<int, string> memberNames = new();

            // Get the tee time record
            var teeTime = GetTeeTimes().FirstOrDefault(t => t.TeeTimeID == teeTimeId);
            if (teeTime == null)
                return memberNames;

            // Collect all unique player IDs from this tee time
            List<int> playerIds = new() { teeTime.MemberID };
            if (teeTime.AdditionalMemberIDs != null)
            {
                playerIds.AddRange(teeTime.AdditionalMemberIDs.Where(id => id != 0));
            }

            // Only allow full names if the user is an employee or a participant
            bool isEmployee = role == "Employee";
            bool isInTeeTime = playerIds.Contains(currentUserId);

            if (isEmployee || isInTeeTime)
            {

                return GetMemberNames(playerIds);
            }

            foreach (int id in playerIds)
            {
                memberNames[id] = $"Member #{id}";
            }

            return memberNames;
        }
        public int GetScoreCountForTeeTime(int teeTimeId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Scores WHERE TeeTimeID = @TeeTimeID", conn);
            cmd.Parameters.AddWithValue("@TeeTimeID", teeTimeId);

            return (int)cmd.ExecuteScalar();
        }

        // MEMBERS 
        public Member GetMemberById(int memberId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MemberID, FirstName, LastName, Phone
              FROM Members
              WHERE MemberID = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Member
                        {
                            MemberID = reader.GetInt32(reader.GetOrdinal("MemberID")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone"))
                        };
                    }
                }
            }

            return null;
        }
        public List<TeeTime> GetTeeTimesByMemberID(int memberId)
        {
            var teeTimes = new List<TeeTime>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT t.TeeTimeID, t.Date, t.StartTime, t.EndTime, t.MemberID, t.Players, t.Phone, t.Carts, t.EmployeeName, 
                     t.AdditionalMemberID1, t.AdditionalMemberID2, t.AdditionalMemberID3, t.CancellationRequested, s.ScoreID
              FROM TeeTimes t
              LEFT JOIN Scores s ON t.TeeTimeID = s.TeeTimeID
              WHERE t.MemberID = @MemberID 
                 OR t.AdditionalMemberID1 = @MemberID 
                 OR t.AdditionalMemberID2 = @MemberID 
                 OR t.AdditionalMemberID3 = @MemberID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var teeTime = new TeeTime
                            {
                                TeeTimeID = reader.GetInt32(reader.GetOrdinal("TeeTimeID")),
                                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                                StartTime = reader.IsDBNull(reader.GetOrdinal("StartTime")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                                EndTime = reader.IsDBNull(reader.GetOrdinal("EndTime")) ? TimeSpan.Zero : reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                                MemberID = reader.GetInt32(reader.GetOrdinal("MemberID")),
                                Players = reader.GetInt32(reader.GetOrdinal("Players")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
                                Carts = reader.IsDBNull(reader.GetOrdinal("Carts")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Carts")),
                                EmployeeName = reader.IsDBNull(reader.GetOrdinal("EmployeeName")) ? null : reader.GetString(reader.GetOrdinal("EmployeeName")),
                                CancellationRequested = reader.IsDBNull(reader.GetOrdinal("CancellationRequested")) ? false : reader.GetBoolean(reader.GetOrdinal("CancellationRequested")),
                                ScoreID = reader.IsDBNull(reader.GetOrdinal("ScoreID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("ScoreID")),
                                AdditionalMemberIDs = new List<int>()
                            };

                            if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID1")))
                                teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID1")));
                            if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID2")))
                                teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID2")));
                            if (!reader.IsDBNull(reader.GetOrdinal("AdditionalMemberID3")))
                                teeTime.AdditionalMemberIDs.Add(reader.GetInt32(reader.GetOrdinal("AdditionalMemberID3")));

                            teeTimes.Add(teeTime);
                        }
                        catch (SqlNullValueException ex)
                        {
                            _logger.LogError(ex, "Null value exception occurred while reading TeeTime.");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error occurred while reading TeeTime.");
                            throw;
                        }
                    }
                }
            }

            return teeTimes;
        }
        public Dictionary<int, string> GetMemberNames(List<int> memberIds)
        {
            Dictionary<int, string> memberNames = new Dictionary<int, string>();

            if (memberIds.Count == 0)
            {
                _logger.LogWarning("No member IDs provided to fetch names.");
                return memberNames;
            }

            _logger.LogInformation("Fetching names for member IDs: {MemberIDs}", string.Join(", ", memberIds));

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT MemberID, FirstName, LastName 
                      FROM Members 
                      WHERE MemberID IN (" + string.Join(",", memberIds) + ")", connection);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int memberId = reader.GetInt32(0);
                        string fullName = $"{reader.GetString(1)} {reader.GetString(2)}";
                        memberNames[memberId] = fullName;
                    }
                }
            }

            _logger.LogInformation("Fetched member names: {MemberNames}", string.Join(", ", memberNames.Values));

            return memberNames;
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

        // CANCELLATION  
        public List<TeeTime> GetCancellationRequests()
        {
            List<TeeTime> teeTimes = new List<TeeTime>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"SELECT TeeTimeID, Date, StartTime, EndTime, MemberID, Players, Phone, Carts, EmployeeName 
              FROM TeeTimes 
              WHERE CancellationRequested = 1", connection);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var teeTime = new TeeTime
                            {
                                TeeTimeID = reader.GetInt32(0),
                                Date = reader.GetDateTime(1),
                                StartTime = reader.GetTimeSpan(2),
                                EndTime = reader.GetTimeSpan(3),
                                MemberID = reader.GetInt32(4),
                                Players = reader.GetInt32(5),
                                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Carts = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                                EmployeeName = reader.IsDBNull(8) ? null : reader.GetString(8)
                            };
                            teeTimes.Add(teeTime);
                        }
                        catch (SqlNullValueException ex)
                        {

                            Console.WriteLine($"Null value encountered: {ex.Message}");
                        }
                    }
                }
            }

            return teeTimes;
        }
        public void RequestCancellation(int teeTimeId, int requestingMemberId, string role)
        {
            var teeTime = GetTeeTimes().FirstOrDefault(t => t.TeeTimeID == teeTimeId);

            if (teeTime == null)
                throw new ArgumentException("Tee time not found.");

            bool isOwner = teeTime.MemberID == requestingMemberId;
            bool isAdditional = teeTime.AdditionalMemberIDs.Contains(requestingMemberId);
            bool isEmployee = role == "Employee";

            if (!(isOwner || isAdditional || isEmployee))
                throw new UnauthorizedAccessException("You are not authorized to request cancellation.");

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            var command = new SqlCommand(
                @"UPDATE TeeTimes SET CancellationRequested = 1 WHERE TeeTimeID = @TeeTimeID", connection);

            command.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
            command.ExecuteNonQuery();

            _logger.LogInformation("Cancellation requested by MemberID {MemberID} for TeeTimeID {TeeTimeID}", requestingMemberId, teeTimeId);
        }

        public void ApproveCancellation(int teeTimeId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Delete Score 
                    SqlCommand deleteScoreCommand = new SqlCommand(
    @"DELETE FROM Scores WHERE TeeTimeID = @TeeTimeID", connection, transaction);

                    deleteScoreCommand.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
                    deleteScoreCommand.ExecuteNonQuery();

                    // Delete Players
                    SqlCommand deletePlayersCommand = new SqlCommand(
                        @"DELETE FROM TeeTimePlayers WHERE TeeTimeID = @TeeTimeID", connection, transaction);
                    deletePlayersCommand.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
                    deletePlayersCommand.ExecuteNonQuery();

                    // Delete TeeTime
                    SqlCommand deleteTeeTimeCommand = new SqlCommand(
                        @"DELETE FROM TeeTimes WHERE TeeTimeID = @TeeTimeID", connection, transaction);
                    deleteTeeTimeCommand.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
                    deleteTeeTimeCommand.ExecuteNonQuery();

                    transaction.Commit();
                    _logger.LogInformation("✅ FULL DELETE: TeeTime, Score, and Players removed for TeeTimeID: {TeeTimeID}", teeTimeId);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "🔥 DELETE FAILED: {Message}", ex.Message);
                    throw;
                }
            }
        }
        public void DenyCancellation(int teeTimeId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    @"UPDATE TeeTimes 
              SET CancellationRequested = 0 
              WHERE TeeTimeID = @TeeTimeID", connection);

                command.Parameters.AddWithValue("@TeeTimeID", teeTimeId);
                command.ExecuteNonQuery();
            }
        }

        public bool RemoveAdditionalMember(int teeTimeId, int memberId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
        SELECT AdditionalMemberID1, AdditionalMemberID2, AdditionalMemberID3
        FROM TeeTimes
        WHERE TeeTimeID = @TeeTimeID", connection);
            command.Parameters.AddWithValue("@TeeTimeID", teeTimeId);

            using var reader = command.ExecuteReader();
            if (!reader.Read()) return false;

            int? id1 = reader.IsDBNull(0) ? null : reader.GetInt32(0);
            int? id2 = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            int? id3 = reader.IsDBNull(2) ? null : reader.GetInt32(2);

            reader.Close();

            string columnToClear = null;

            if (id1 == memberId) columnToClear = "AdditionalMemberID1";
            else if (id2 == memberId) columnToClear = "AdditionalMemberID2";
            else if (id3 == memberId) columnToClear = "AdditionalMemberID3";

            if (columnToClear == null)
                return false;

            var updateCmd = new SqlCommand($@"
        UPDATE TeeTimes
        SET {columnToClear} = NULL, Players = Players - 1
        WHERE TeeTimeID = @TeeTimeID", connection);
            updateCmd.Parameters.AddWithValue("@TeeTimeID", teeTimeId);

            int affected = updateCmd.ExecuteNonQuery();
            return affected > 0;
        }


    }
}