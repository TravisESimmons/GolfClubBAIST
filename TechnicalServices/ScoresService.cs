using GolfBAIST.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace GolfBAIST.TechnicalServices
{
    public class ScoresService
    {
        private readonly string connectionString = @"Server=localhost\SQLEXPRESS;Database=GolfBAIST_Local;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Score> GetScores()
        {
            List<Score> scores = new List<Score>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT * FROM Scores", connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Score score = new Score
                    {
                        ScoreID = (int)reader["ScoreID"],
                        TeeTimeID = (int)reader["TeeTimeID"],
                        MemberID = (int)reader["MemberID"],
                        Date = (DateTime)reader["Date"],
                        CourseName = reader["CourseName"] as string,
                        CourseRating = (decimal)reader["CourseRating"],
                        SlopeRating = (int)reader["SlopeRating"],
                        HoleScores = reader["HoleScores"] as string,
                        TotalScore = (int)reader["TotalScore"],
                        Differential = reader["Differential"] != DBNull.Value ? Convert.ToDouble(reader["Differential"]) : 0.0
                    };
                    scores.Add(score);
                }
            }

            return scores;
        }


        public Score? GetScoreByMemberAndTeeTime(int memberId, int teeTimeId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
            SELECT * FROM Scores
            WHERE MemberID = @MemberID AND TeeTimeID = @TeeTimeID", connection);

                command.Parameters.AddWithValue("@MemberID", memberId);
                command.Parameters.AddWithValue("@TeeTimeID", teeTimeId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Score
                        {
                            ScoreID = (int)reader["ScoreID"],
                            TeeTimeID = (int)reader["TeeTimeID"],
                            MemberID = (int)reader["MemberID"],
                            Date = (DateTime)reader["Date"],
                            CourseName = reader["CourseName"].ToString(),
                            CourseRating = (decimal)reader["CourseRating"],
                            SlopeRating = (int)reader["SlopeRating"],
                            HoleScores = reader["HoleScores"].ToString(),
                            TotalScore = (int)reader["TotalScore"],
                            Differential = reader["Differential"] != DBNull.Value ? Convert.ToDouble(reader["Differential"]) : 0.0
                        };
                    }
                }
            }

            return null;
        }

        public Score GetScoreByID(int scoreId)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT * FROM Scores WHERE ScoreID = @ScoreID", conn);
            cmd.Parameters.AddWithValue("@ScoreID", scoreId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Score
                {
                    ScoreID = (int)reader["ScoreID"],
                    TeeTimeID = (int)reader["TeeTimeID"],
                    MemberID = (int)reader["MemberID"],
                    Date = (DateTime)reader["Date"],
                    CourseName = reader["CourseName"].ToString(),
                    CourseRating = (decimal)reader["CourseRating"],
                    SlopeRating = (int)reader["SlopeRating"],
                    HoleScores = reader["HoleScores"].ToString(),
                    TotalScore = (int)reader["TotalScore"],
                    Differential = reader["Differential"] != DBNull.Value ? Convert.ToDouble(reader["Differential"]) : 0.0
                };
            }

            return null;
        }


        public int AddScore(Score score)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
            INSERT INTO Scores (TeeTimeID, MemberID, Date, CourseName, CourseRating, SlopeRating, HoleScores, TotalScore, Differential)
            VALUES (@TeeTimeID, @MemberID, @Date, @CourseName, @CourseRating, @SlopeRating, @HoleScores, @TotalScore, @Differential);
            SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@TeeTimeID", score.TeeTimeID);
                command.Parameters.AddWithValue("@MemberID", score.MemberID);
                command.Parameters.AddWithValue("@Date", score.Date);
                command.Parameters.AddWithValue("@CourseName", score.CourseName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourseRating", score.CourseRating);
                command.Parameters.AddWithValue("@SlopeRating", score.SlopeRating);
                command.Parameters.AddWithValue("@HoleScores", score.HoleScores ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TotalScore", score.TotalScore);
                command.Parameters.AddWithValue("@Differential", score.Differential);

                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }



        public bool UpdateScore(Score score)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(@"
        UPDATE Scores
        SET CourseName = @CourseName,
            CourseRating = @CourseRating,
            SlopeRating = @SlopeRating,
            HoleScores = @HoleScores,
            TotalScore = @TotalScore,
            Differential = @Differential,
            Date = @Date
        WHERE ScoreID = @ScoreID", connection);

                command.Parameters.AddWithValue("@CourseName", score.CourseName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourseRating", score.CourseRating);
                command.Parameters.AddWithValue("@SlopeRating", score.SlopeRating);
                command.Parameters.AddWithValue("@HoleScores", score.HoleScores ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TotalScore", score.TotalScore);

                double differential = Math.Round(score.Differential, 1);
                if (double.IsNaN(differential) || double.IsInfinity(differential))
                {
                    differential = 0.0;
                }
                command.Parameters.AddWithValue("@Differential", differential);

                command.Parameters.AddWithValue("@Date", score.Date);
                command.Parameters.AddWithValue("@ScoreID", score.ScoreID);

                return command.ExecuteNonQuery() > 0;
            }
        }



        public void UpdateHoleScores(int scoreId, Dictionary<int, int> updatedStrokes)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string holeScoresJson = JsonSerializer.Serialize(updatedStrokes);

                using (var command = new SqlCommand("UPDATE Scores SET HoleScores = @HoleScores WHERE ScoreID = @ScoreID", connection))
                {
                    command.Parameters.AddWithValue("@HoleScores", holeScoresJson);
                    command.Parameters.AddWithValue("@ScoreID", scoreId);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void NormalizeHoleScoresFormat()
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var selectCmd = new SqlCommand("SELECT ScoreID, HoleScores FROM Scores", connection);
            using var reader = selectCmd.ExecuteReader();

            var scoresToFix = new List<(int ScoreID, string NewHoleScores)>();

            while (reader.Read())
            {
                var scoreId = (int)reader["ScoreID"];
                var holeScoresRaw = reader["HoleScores"] as string;

                if (string.IsNullOrWhiteSpace(holeScoresRaw)) continue;

                try
                {

                    JsonSerializer.Deserialize<Dictionary<int, int>>(holeScoresRaw);
                    continue;
                }
                catch
                {

                }

                try
                {

                    var legacy = JsonSerializer.Deserialize<Dictionary<string, string>>(holeScoresRaw);

                    if (legacy == null)
                        continue;

                    var normalized = new Dictionary<int, int>();

                    foreach (var kvp in legacy)
                    {

                        if (kvp.Key.StartsWith("Hole") && int.TryParse(kvp.Value, out var val))
                        {
                            if (int.TryParse(kvp.Key.Replace("Hole", "").Trim(), out var holeNumber))
                            {
                                normalized[holeNumber] = val;
                            }
                        }
                    }

                    if (normalized.Count > 0)
                    {
                        var newJson = JsonSerializer.Serialize(normalized);
                        scoresToFix.Add((scoreId, newJson));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Skipped ScoreID={scoreId} due to malformed JSON: {ex.Message}");
                }
            }

            reader.Close();

            foreach (var score in scoresToFix)
            {
                var updateCmd = new SqlCommand("UPDATE Scores SET HoleScores = @HoleScores WHERE ScoreID = @ScoreID", connection);
                updateCmd.Parameters.AddWithValue("@HoleScores", score.NewHoleScores);
                updateCmd.Parameters.AddWithValue("@ScoreID", score.ScoreID);
                updateCmd.ExecuteNonQuery();
            }

            Console.WriteLine($"✅ Normalized {scoresToFix.Count} scores.");
        }



    }
}
