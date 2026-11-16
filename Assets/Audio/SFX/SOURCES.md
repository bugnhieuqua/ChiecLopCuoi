# Nguồn âm thanh SFX

## Nhạc thắng / thua (dùng ở màn kết quả trận đấu)

| File | Nội dung | Thời lượng | Nguồn |
|---|---|---|---|
| `sfx_victory_royale.wav` | "Number one victory royale, yeah Fortnite we 'bout to get down" | 12.54 s | https://www.myinstants.com/en/instant/number-one-victory-royale-yeah-fortnite-we-bout-29836/ |
| `sfx_defeat_dexter.wav` | Dexter meme (nhạc "Dexter" – dùng khi thua) | 10.71 s | https://soundinstants.com/vi/sound/dexter-meme (bản MP3 tương đương: https://www.myinstants.com/en/instant/dexter-meme-26140/) |

Hai file trên được gán vào `AudioManager.sfxVictoryClip` / `sfxDefeatClip`.
`AudioManager.PlayVictory()` / `PlayDefeat()` tắt dần nhạc nền chiến đấu rồi phát
jingle để người chơi nghe rõ.

- Định dạng: WAV PCM 16-bit, stereo, 48 kHz.
- Chuẩn hóa loudness: ≈ -11.5 LUFS, limiter giữ đỉnh ≤ -1 dBTP.
- Clip victory đã cắt 1.31 s im lặng ở đầu.

## Các file cũ (giữ lại, không dùng cho thắng/thua nữa)

| File | Ghi chú |
|---|---|
| `sfx_victory_music.wav` | Vẫn dùng cho `sfxGateClearedClip` (mở cổng khu vực) và `PlayRageActivation`. |
| `sfx_defeat_music.wav` | Không còn được gán trong Scene. |
| `sfx_victory.wav`, `sfx_defeat.wav`, `sfx_victory_vo.wav`, `sfx_defeat_vo.wav` | Bản cũ, không được tham chiếu trong Scene. |

## Lưu ý bản quyền

Các clip lấy từ MyInstants / SoundInstants là bản người dùng tải lên, có thể vẫn
thuộc bản quyền của tác giả gốc (Fortnite, Dexter, ...). Phù hợp cho học tập / đồ
án / demo nội bộ. Nếu phát hành thương mại, hãy thay bằng âm thanh CC0 hoặc âm
thanh tự tạo.
