using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SQLite;
using UnityEngine;

namespace ChiecLopCuoi.Database
{
    public class SqliteParameter
    {
        public string ParameterName { get; set; }
        public object Value { get; set; }

        public SqliteParameter(string name, object value)
        {
            ParameterName = !string.IsNullOrEmpty(name) && name.StartsWith("@") ? name : "@" + name;
            Value = value;
        }
    }

    public class SqliteDataReader : IDisposable
    {
        private readonly List<Dictionary<string, object>> _rows;
        private readonly List<string> _columns;
        private int _currentIndex = -1;

        public SqliteDataReader(List<Dictionary<string, object>> rows, List<string> columns)
        {
            _rows = rows ?? new List<Dictionary<string, object>>();
            _columns = columns ?? new List<string>();
        }

        public bool Read()
        {
            _currentIndex++;
            return _currentIndex < _rows.Count;
        }

        public object GetValue(int i)
        {
            if (_currentIndex < 0 || _currentIndex >= _rows.Count) return null;
            if (i < 0 || i >= _columns.Count) return null;
            string col = _columns[i];
            return _rows[_currentIndex].TryGetValue(col, out var val) ? val : null;
        }

        public float GetFloat(int i)
        {
            var val = GetValue(i);
            if (val == null || val is DBNull) return 0f;
            return Convert.ToSingle(val, CultureInfo.InvariantCulture);
        }

        public string GetString(int i)
        {
            var val = GetValue(i);
            if (val == null || val is DBNull) return string.Empty;
            return Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public int GetInt32(int i)
        {
            var val = GetValue(i);
            if (val == null || val is DBNull) return 0;
            return Convert.ToInt32(val, CultureInfo.InvariantCulture);
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Lớp quản lý kết nối SQLite đồng bộ sử dụng com.gilzoide.sqlite-net (SQLite-net + SQLite3).
    /// Hỗ trợ Windows/Editor lẫn WebGL (idbvfs ánh xạ IndexedDB) đồng thời giữ nguyên API tĩnh cho GameRepository và Tests.
    /// </summary>
    public static class DatabaseManager
    {
        private const string DbFileName = "game_runtime.db";

        private static readonly byte[] SqliteHeaderMagic = Encoding.ASCII.GetBytes("SQLite format 3\0");
        private static readonly object _lock = new object();

        private static SQLiteConnection _connection;
        private static string _dbPath;
        private static bool _isInitialized;
        private static bool _quittingHooked;

        public static string DatabasePath => _dbPath;
        public static bool IsInitialized => _isInitialized;

        private const string DefaultSchemaSql = @"
CREATE TABLE IF NOT EXISTS GameConfig (
    config_key TEXT PRIMARY KEY,
    value_real REAL,
    value_text TEXT
);

CREATE TABLE IF NOT EXISTS PlayerData (
    id INTEGER PRIMARY KEY,
    tires INTEGER DEFAULT 0,
    population INTEGER DEFAULT 0,
    last_save TEXT
);

INSERT OR IGNORE INTO PlayerData (id, tires, population, last_save)
VALUES (1, 0, 0, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO GameConfig (config_key, value_real, value_text) VALUES
('collected_tires_total', 0.0, 'Tổng số chiếc lốp tích lũy qua các lượt thắng'),
('spent_tires_total', 0.0, 'Tổng số lốp đã chi cho nâng cấp'),
('upgrade_hp_tier', 0.0, 'Cấp nâng cấp máu tối đa (0..3)'),
('upgrade_atk_tier', 0.0, 'Cấp nâng cấp sát thương (0..3)'),
('difficulty_mode', 0.0, 'Chế độ độ khó (0 = Thường, 1 = Khó)'),
('SampleScene_stars', 0.0, 'Số sao kỷ lục Màn 1 (0..3)'),
('level2_stars', 0.0, 'Số sao kỷ lục Màn 2 (0..3)'),
('level3_stars', 0.0, 'Số sao kỷ lục Màn 3 (0..3)'),
('level4_stars', 0.0, 'Số sao kỷ lục Màn 4 (0..3)'),
('level5_stars', 0.0, 'Số sao kỷ lục Màn 5 (0..3)'),
('level6_stars', 0.0, 'Số sao kỷ lục Màn 6 (0..3)'),
('audio_bgm_volume', 0.8, 'Âm lượng Nhạc nền (0..1)'),
('audio_sfx_volume', 1.0, 'Âm lượng Âm thanh hiệu ứng/meme (0..1)');

INSERT OR REPLACE INTO GameConfig (config_key, value_real, value_text)
VALUES ('config_schema_version', 5.0, 'Cleaned action-RPG schema version');
";

        private static readonly string[] LegacyUnusedConfigKeys =
        {
            "enemy_statue_x", "castle_spawn_x", "castle_boundary_x", "ground_bottom_y",
            "diag_step_x", "diag_step_y", "col_spacing_x", "club_start_x", "bow_start_x",
            "min_formation_x", "max_cols_per_type", "hoe_work_duration", "hoe_stand_offset_x",
            "hoe_target_prop", "move_speed", "enemy_spawn_interval", "enemy_boss_interval",
            "enemy_tires_start", "enemy_form1_tire_cost", "enemy_form2_tire_cost",
            "enemy_boss_tire_cost", "enemy_tires_per_work", "enemy_spawn_x",
            "enemy_front_start_x", "enemy_col_spacing_x", "enemy_diag_step_x",
            "enemy_diag_step_y", "enemy_max_cols", "player_statue_x",
            "enemy_hoe_target_props", "enemy_hoe_work_duration", "enemy_hoe_stand_offset_x"
        };

        public static string GetDefaultDatabasePath()
        {
#if UNITY_EDITOR
            string dir = Path.Combine(Application.dataPath, "Database");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, DbFileName);
#else
            return Path.Combine(Application.persistentDataPath, DbFileName);
#endif
        }

        public static void Initialize(string customDbPath = null)
        {
            lock (_lock)
            {
                string targetPath = string.IsNullOrEmpty(customDbPath)
                    ? GetDefaultDatabasePath()
                    : customDbPath;

                if (_isInitialized && _connection != null &&
                    string.Equals(_dbPath, targetPath, StringComparison.Ordinal))
                {
                    return;
                }

                CloseConnectionInternal();

                _dbPath = targetPath;
                EnsureParentDirectory(_dbPath);

#if !UNITY_WEBGL || UNITY_EDITOR
                // Khi chạy build Desktop/Mobile lần đầu (không truyền customDbPath của test),
                // copy file .db mẫu từ StreamingAssets nếu chưa tồn tại trên persistentDataPath.
                if (string.IsNullOrEmpty(customDbPath))
                {
                    SeedNativeFileFromStreamingAssetsIfMissing(_dbPath);
                }
                BackupNonSqliteFileIfAny(_dbPath);
#endif

                Debug.Log($"<color=cyan>[DatabaseManager]</color> Kết nối SQLite tại: {_dbPath}");

                _connection = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                _connection.BusyTimeout = TimeSpan.FromSeconds(3);

                ExecuteSchema();
                MigrateConfigData();

                if (!_quittingHooked)
                {
                    _quittingHooked = true;
                    Application.quitting += Close;
                }

                _isInitialized = true;
                Debug.Log("<color=green>[DatabaseManager]</color> Kết nối SQLite (unity-sqlite-net) thành công!");
            }
        }

        /// <summary>
        /// Đóng kết nối SQLite an toàn để giải phóng lock và đồng bộ dữ liệu xuống đĩa / IndexedDB.
        /// </summary>
        public static void Close()
        {
            lock (_lock)
            {
                CloseConnectionInternal();
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Được gọi sau khi WebGL tải xong file .db nhị phân từ StreamingAssets:
        /// giải mã bằng SQLiteConnection.Deserialize rồi đồng bộ bảng/dòng mặc định vào DB hiện hành nếu chưa có.
        /// </summary>
        public static void ApplySeedBytesFromStreamingAssets(byte[] dbBytes)
        {
            if (dbBytes == null || dbBytes.Length < SqliteHeaderMagic.Length)
            {
                return;
            }

            lock (_lock)
            {
                if (!_isInitialized || _connection == null)
                {
                    Initialize();
                }

                try
                {
                    using (var seedMem = new SQLiteConnection(":memory:"))
                    {
                        seedMem.Deserialize(dbBytes, null, SQLite3.DeserializeFlags.ReadOnly);

                        var hasGameConfig = ReadScalarFromConnection(
                            seedMem,
                            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='GameConfig';",
                            0);
                        if (hasGameConfig <= 0) return;

                        // Sao chép toàn bộ cấu hình gốc bằng INSERT OR IGNORE để không ghi đè tiến trình vừa ghi.
                        using (var reader = ExecuteReaderOnConnection(seedMem, "SELECT config_key, value_real, value_text FROM GameConfig;", null))
                        {
                            _connection.RunInTransaction(() =>
                            {
                                while (reader.Read())
                                {
                                    string key = reader.GetString(0);
                                    float valReal = reader.GetFloat(1);
                                    string valText = reader.GetString(2);
                                    ExecuteNonQueryOnConnection(
                                        _connection,
                                        "INSERT OR IGNORE INTO GameConfig (config_key, value_real, value_text) VALUES (@key, @val, @txt);",
                                        new SqliteParameter("@key", key),
                                        new SqliteParameter("@val", valReal),
                                        new SqliteParameter("@txt", valText));
                                }
                            });
                        }

                        using (var reader = ExecuteReaderOnConnection(seedMem, "SELECT id, tires, population, last_save FROM PlayerData;", null))
                        {
                            _connection.RunInTransaction(() =>
                            {
                                while (reader.Read())
                                {
                                    int id = reader.GetInt32(0);
                                    int tires = reader.GetInt32(1);
                                    int pop = reader.GetInt32(2);
                                    string lastSave = reader.GetString(3);
                                    ExecuteNonQueryOnConnection(
                                        _connection,
                                        "INSERT OR IGNORE INTO PlayerData (id, tires, population, last_save) VALUES (@id, @tires, @pop, @save);",
                                        new SqliteParameter("@id", id),
                                        new SqliteParameter("@tires", tires),
                                        new SqliteParameter("@pop", pop),
                                        new SqliteParameter("@save", lastSave));
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] ApplySeedBytesFromStreamingAssets warning: {ex.Message}");
                }
            }
        }

        public static int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
        {
            if (!_isInitialized || _connection == null) Initialize();
            lock (_lock)
            {
                return ExecuteNonQueryOnConnection(_connection, sql, parameters);
            }
        }

        public static T ExecuteScalar<T>(string sql, T defaultValue, params SqliteParameter[] parameters)
        {
            if (!_isInitialized || _connection == null) Initialize();
            lock (_lock)
            {
                return ReadScalarFromConnection(_connection, sql, defaultValue, parameters);
            }
        }

        public static void ExecuteReader(string sql, Action<SqliteDataReader> onRead, params SqliteParameter[] parameters)
        {
            if (!_isInitialized || _connection == null) Initialize();
            lock (_lock)
            {
                try
                {
                    using (var reader = ExecuteReaderOnConnection(_connection, sql, parameters))
                    {
                        onRead?.Invoke(reader);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] ExecuteReader warning ({sql}): {ex.Message}");
                }
            }
        }

        private static void CloseConnectionInternal()
        {
            if (_connection != null)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] Close connection warning: {ex.Message}");
                }
                finally
                {
                    _connection = null;
                }
            }
        }

        private static void EnsureParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch { }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private static void SeedNativeFileFromStreamingAssetsIfMissing(string targetPath)
        {
            try
            {
                if (File.Exists(targetPath)) return;
                string streamingPath = Path.Combine(Application.streamingAssetsPath, DbFileName);
                if (File.Exists(streamingPath))
                {
                    File.Copy(streamingPath, targetPath, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] SeedNativeFileFromStreamingAssetsIfMissing warning: {ex.Message}");
            }
        }

        private static void BackupNonSqliteFileIfAny(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                var info = new FileInfo(path);
                if (info.Length == 0) return;

                byte[] header = new byte[SqliteHeaderMagic.Length];
                int readCount;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    readCount = fs.Read(header, 0, header.Length);
                }

                bool matches = readCount == SqliteHeaderMagic.Length;
                if (matches)
                {
                    for (int i = 0; i < SqliteHeaderMagic.Length; i++)
                    {
                        if (header[i] != SqliteHeaderMagic[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                }

                if (!matches)
                {
                    string backupPath = path + ".json.bak";
                    File.Copy(path, backupPath, overwrite: true);
                    File.Delete(path);
                    Debug.LogWarning($"[DatabaseManager] Phát hiện file CSDL cũ không phải định dạng SQLite, đã sao lưu tại '{backupPath}' và tạo lại SQLite DB sạch.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] BackupNonSqliteFileIfAny warning: {ex.Message}");
            }
        }
#endif

        private static void ExecuteSchema()
        {
            string script = DefaultSchemaSql;

#if UNITY_EDITOR
            try
            {
                string schemaPath = Path.Combine(Application.dataPath, "Database", "schema.sql");
                if (File.Exists(schemaPath))
                {
                    string fileScript = File.ReadAllText(schemaPath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(fileScript))
                    {
                        script = fileScript;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Read schema.sql fallback: {ex.Message}");
            }
#endif

            _connection.ExecuteScript(script);
        }

        private static void MigrateConfigData()
        {
            const float migrationVersion = 5f;
            float currentVersion = ReadScalarFromConnection(
                _connection,
                "SELECT value_real FROM GameConfig WHERE config_key = @key LIMIT 1;",
                0f,
                new SqliteParameter("@key", "config_schema_version"));

            if (currentVersion >= migrationVersion)
            {
                return;
            }

            _connection.RunInTransaction(() =>
            {
                for (int i = 0; i < LegacyUnusedConfigKeys.Length; i++)
                {
                    ExecuteNonQueryOnConnection(
                        _connection,
                        "DELETE FROM GameConfig WHERE config_key = @key;",
                        new SqliteParameter("@key", LegacyUnusedConfigKeys[i]));
                }

                ExecuteNonQueryOnConnection(
                    _connection,
                    "INSERT OR REPLACE INTO GameConfig (config_key, value_real, value_text) VALUES ('config_schema_version', 5.0, 'Cleaned action-RPG schema version');",
                    null);
            });

            Debug.Log("[DatabaseManager] Migrated GameConfig to v5 (SQLite).");
        }

        private static int ExecuteNonQueryOnConnection(SQLiteConnection conn, string sql, params SqliteParameter[] parameters)
        {
            if (conn == null || string.IsNullOrWhiteSpace(sql)) return 0;

            var cmd = conn.CreateCommand(sql);
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    if (p != null && !string.IsNullOrEmpty(p.ParameterName))
                    {
                        cmd.Bind(p.ParameterName, p.Value);
                    }
                }
            }
            return cmd.ExecuteNonQuery();
        }

        private static T ReadScalarFromConnection<T>(SQLiteConnection conn, string sql, T defaultValue, params SqliteParameter[] parameters)
        {
            if (conn == null || string.IsNullOrWhiteSpace(sql)) return defaultValue;

            try
            {
                using (var reader = ExecuteReaderOnConnection(conn, sql, parameters))
                {
                    if (reader.Read())
                    {
                        object val = reader.GetValue(0);
                        if (val != null && !(val is DBNull))
                        {
                            return (T)Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] ExecuteScalar warning ({sql}): {ex.Message}");
            }

            return defaultValue;
        }

        private static readonly IntPtr SqliteTransient = new IntPtr(-1);

        private static SqliteDataReader ExecuteReaderOnConnection(SQLiteConnection conn, string sql, params SqliteParameter[] parameters)
        {
            if (conn == null || string.IsNullOrWhiteSpace(sql))
            {
                return new SqliteDataReader(null, null);
            }

            var stmt = SQLite3.Prepare2(conn.Handle, sql);
            try
            {
                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        if (p == null || string.IsNullOrEmpty(p.ParameterName)) continue;
                        int idx = SQLite3.BindParameterIndex(stmt, p.ParameterName);
                        if (idx <= 0) continue;
                        BindRawParameter(stmt, idx, p.Value);
                    }
                }

                int colCount = SQLite3.ColumnCount(stmt);
                var columns = new List<string>(colCount);
                for (int i = 0; i < colCount; i++)
                {
                    columns.Add(SQLite3.ColumnName16(stmt, i) ?? $"col_{i}");
                }

                var rows = new List<Dictionary<string, object>>();
                SQLite3.Result stepResult;
                while ((stepResult = SQLite3.Step(stmt)) == SQLite3.Result.Row)
                {
                    var row = new Dictionary<string, object>(colCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < colCount; i++)
                    {
                        var colType = SQLite3.ColumnType(stmt, i);
                        object colVal;
                        switch (colType)
                        {
                            case SQLite3.ColType.Integer:
                                colVal = SQLite3.ColumnInt64(stmt, i);
                                break;
                            case SQLite3.ColType.Float:
                                colVal = SQLite3.ColumnDouble(stmt, i);
                                break;
                            case SQLite3.ColType.Text:
                                colVal = SQLite3.ColumnString(stmt, i);
                                break;
                            case SQLite3.ColType.Blob:
                                colVal = SQLite3.ColumnByteArray(stmt, i);
                                break;
                            case SQLite3.ColType.Null:
                            default:
                                colVal = null;
                                break;
                        }
                        row[columns[i]] = colVal;
                    }
                    rows.Add(row);
                }

                if (stepResult != SQLite3.Result.Done)
                {
                    throw SQLiteException.New(stepResult, SQLite3.GetErrmsg(conn.Handle));
                }

                return new SqliteDataReader(rows, columns);
            }
            finally
            {
                SQLite3.Finalize(stmt);
            }
        }

        private static void BindRawParameter(
#if USE_WP8_NATIVE_SQLITE || USE_SQLITEPCL_RAW
            SQLite3.Sqlite3Statement stmt,
#else
            IntPtr stmt,
#endif
            int index,
            object value)
        {
            if (value == null || value is DBNull)
            {
                SQLite3.BindNull(stmt, index);
            }
            else if (value is int iVal)
            {
                SQLite3.BindInt(stmt, index, iVal);
            }
            else if (value is byte || value is sbyte || value is short || value is ushort)
            {
                SQLite3.BindInt(stmt, index, Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }
            else if (value is bool bVal)
            {
                SQLite3.BindInt(stmt, index, bVal ? 1 : 0);
            }
            else if (value is uint || value is long || value is ulong)
            {
                SQLite3.BindInt64(stmt, index, Convert.ToInt64(value, CultureInfo.InvariantCulture));
            }
            else if (value is float || value is double || value is decimal)
            {
                SQLite3.BindDouble(stmt, index, Convert.ToDouble(value, CultureInfo.InvariantCulture));
            }
            else if (value is byte[] bytes)
            {
                SQLite3.BindBlob(stmt, index, bytes, bytes.Length, SqliteTransient);
            }
            else
            {
                string str = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                SQLite3.BindText(stmt, index, str, -1, SqliteTransient);
            }
        }
    }
}
