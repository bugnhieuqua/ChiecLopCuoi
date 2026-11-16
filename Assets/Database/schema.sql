-- ====================================================================
-- CHIẾC LỐP CUỐI - GAME DATABASE SCHEMA & DEFAULT CONFIGURATION
-- Lưu trữ cấu hình tiến độ, nâng cấp, độ khó và âm thanh.
-- ====================================================================

-- 1. Bảng cấu hình & tiến độ trò chơi
CREATE TABLE IF NOT EXISTS GameConfig (
    config_key TEXT PRIMARY KEY,
    value_real REAL,
    value_text TEXT
);

-- 2. Bảng dữ liệu người chơi
CREATE TABLE IF NOT EXISTS PlayerData (
    id INTEGER PRIMARY KEY,
    tires INTEGER DEFAULT 0,
    population INTEGER DEFAULT 0,
    last_save TEXT
);

INSERT OR IGNORE INTO PlayerData (id, tires, population, last_save)
VALUES (1, 0, 0, CURRENT_TIMESTAMP);

-- Khởi tạo mặc định tiến độ 6 màn chơi, nâng cấp, độ khó và âm thanh
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
