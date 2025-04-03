using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyASCS.Services.Implementations
{
    public class ImageStorageService
    {
          private const string DatabaseFile = "MyASCS.db";

        public ImageStorageService()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool dbExists = File.Exists(DatabaseFile);
            
            using var connection = new SqliteConnection($"Data Source={DatabaseFile}");
            connection.Open();

            string createTablesQuery = @"
            CREATE TABLE IF NOT EXISTS staffs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                staff_id INTEGER NOT NULL,
                started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (staff_id) REFERENCES staffs(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS images (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id INTEGER NOT NULL,
                staff_id INTEGER NOT NULL,
                position TEXT NOT NULL,
                file_path TEXT NOT NULL,
                uploaded INTEGER DEFAULT 0,
                FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE,
                FOREIGN KEY (staff_id) REFERENCES staffs(id) ON DELETE CASCADE
            );";

            using var command = new SqliteCommand(createTablesQuery, connection);
            command.ExecuteNonQuery();

            // Ensure default staff exists
            if (!dbExists)
            {
                InsertDefaultStaff(connection);
            }
        }
        
        private void InsertDefaultStaff(SqliteConnection connection)
        {
            string checkStaffQuery = "SELECT COUNT(*) FROM staffs;";
            using var checkStaffCommand = new SqliteCommand(checkStaffQuery, connection);
            int staffCount = Convert.ToInt32(checkStaffCommand.ExecuteScalar());

            if (staffCount == 0)
            {
                string insertStaffQuery = "INSERT INTO staffs (name) VALUES ('John Doe');";
                using var insertCommand = new SqliteCommand(insertStaffQuery, connection);
                insertCommand.ExecuteNonQuery();
            }
        }

        public int CreateSession(int staffId)
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFile}");
            connection.Open();

            var query = "INSERT INTO sessions (staff_id) VALUES (@staff_id); SELECT last_insert_rowid();";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@staff_id", staffId);
            
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void SaveImageRecord(int sessionId, int staffId, string position, string filePath)
        {
            using var connection = new SqliteConnection($"Data Source={DatabaseFile}");
            connection.Open();

            string query = "INSERT INTO images (session_id, staff_id, position, file_path) VALUES (@session_id, @staff_id, @position, @file_path);";
            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@session_id", sessionId);
            command.Parameters.AddWithValue("@staff_id", staffId);
            command.Parameters.AddWithValue("@position", position);
            command.Parameters.AddWithValue("@file_path", filePath);

            command.ExecuteNonQuery();
        }
    }
}
