# Meme K.O. & nhạc gặp Boss

Thư mục này chứa các meme "K.O." phát khi một đấu thủ gục ngã, cùng nhạc dồn dập
cho đoạn Cow Lốp chạm mặt Boss Nữ.

## 1. Meme K.O. (`MemesKO/`)

Được dùng bởi `MemeKoSound.cs` (`Assets/Scripts/Gameplay/MemeKoSound.cs`).
`DuelEncounterManager.ShowKOSplashThenResultRoutine()` gọi `MemeKoSound.PlayRandom()`
ngay lúc màn hình K.O. đập xuống (áp dụng cho cả thắng và thua), chọn ngẫu nhiên
một clip và không lặp lại clip vừa phát.

| File | Nội dung | Thời lượng | Nguồn (MyInstants) |
|---|---|---|---|
| `ko_fatality_myinstants.wav` | "Fatality!" (Mortal Kombat) | 2.12 s | https://www.myinstants.com/media/sounds/fatality-mortal-kombat-sound-effect-hd.mp3 |
| `ko_oof_myinstants.wav` | "Oof" (Roblox) | 1.00 s | https://www.myinstants.com/media/sounds/roblox-death-sound_1.mp3 |
| `ko_youdied_myinstants.wav` | "You Died" (Dark Souls, cắt 4.2 s + fade out) | 4.20 s | https://www.myinstants.com/media/sounds/dark-souls-you-died-sound-effect_hm5sYFG.mp3 |
| `ko_wasted_myinstants.wav` | "Wasted" (GTA V, cắt 4.2 s + fade out) | 4.20 s | https://www.myinstants.com/media/sounds/gta-v-death-sound-effect-102.mp3 |

- Định dạng: WAV PCM 16-bit, mono, 44.1 kHz.
- Chuẩn hóa loudness: `loudnorm I=-16 LUFS, TP=-1.5 dB`.
- Tăng âm: đẩy loudness lên mức nghe rõ khi chơi (limiter giữ đỉnh ≤ -1 dBTP).
- Khi meme K.O. phát, `AudioManager.DuckBGMForMeme()` hạ nhạc nền còn 18%
  trong thời lượng clip rồi fade trả lại sau 0.4 s.
- Import settings: `Load Type = Decompress On Load`, `Force To Mono = On`,
  `Preload Audio Data = On`.
- `MemeKoSound` tự nạp toàn bộ clip trong `Assets/Resources/Audio/MemesKO`
  (`Resources.LoadAll<AudioClip>("Audio/MemesKO")`), không cần gắn tay trong Scene.
  Muốn thêm meme K.O. mới: chỉ cần copy file `.wav` vào thư mục này.
- Thêm clip khác: đặt tên `ko_[tên]_[nguồn].wav` cho dễ quản lý.

## 2. Nhạc gặp Boss (`Assets/Resources/Audio/bgm_boss_encounter.wav`)

- Nội dung: Metin2 – "Follow War God" (bản gốc 52.35 s, giữ nguyên để loop).
- Định dạng: WAV PCM 16-bit, stereo, 44.1 kHz; `loudnorm I=-18 LUFS, TP=-1.5 dB`.
- Import settings: `Load Type = Streaming`, `Compression Format = Vorbis`,
  `Quality = 0.7`, `Preload Audio Data = Off`.
- `AudioManager.PlayBossEncounterMusic(holdSeconds)` được gọi trong
  `DuelEncounterManager.PlayEncounterSequence()`: hạ nhỏ nhạc nền chiến đấu
  (còn 8% âm lượng), fade-in nhạc gặp Boss, giữ `holdSeconds` giây
  (`bannerDuration + 2.5 s`), rồi fade-out 1.2 s và trả lại âm lượng BGM.
- `AudioManager.StopBossEncounterMusic()` được gọi khi chuyển Scene để bảo đảm
  trạng thái âm thanh luôn sạch.

## 3. Lưu ý bản quyền

Các clip trên MyInstants do người dùng tải lên, phần lớn là trích đoạn từ video/
chương trình gốc nên **có thể vẫn thuộc bản quyền của tác giả gốc**. Chúng phù hợp
cho mục đích học tập / đồ án / bản demo nội bộ. Nếu phát hành thương mại, hãy thay
bằng âm thanh CC0 (ví dụ Freesound lọc CC0) hoặc âm thanh tự tạo.
