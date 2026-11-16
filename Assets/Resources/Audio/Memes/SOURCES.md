# Meme âm thanh khi quái vật chết (Enemy Death SFX)

Thư mục này chứa 27 meme âm thanh được dùng bởi script `MemeDeathSound.cs`
(`Assets/Scripts/Gameplay/MemeDeathSound.cs`). Mỗi khi một quái vật bị tiêu diệt,
`MonsterCombatController.DieRoutine()` gọi `PlayDeathMeme()` để phát ngẫu nhiên
một clip trong danh sách (không lặp lại clip vừa phát).

`MemeDeathSound` **tự nạp toàn bộ clip trong thư mục này** bằng
`Resources.LoadAll<AudioClip>("Audio/Memes")`, nên không cần gắn tay 22 clip vào
từng quái trong Scene nữa. Muốn thêm meme mới chỉ cần copy file `.wav` vào đây.
Nếu một quái cần bộ clip riêng, vẫn có thể kéo clip vào ô **Meme Clips** của nó để
ghi đè danh sách tự nạp.

## Danh sách file

| File | Nội dung | Thời lượng | Nguồn (MyInstants) |
|---|---|---|---|
| `meme_oidoioi_myinstants.wav` | "Ối dồi ôi" (Thầy Huấn) | 0.95 s | https://www.myinstants.com/en/instant/oi-doi-oi-32780/ |
| `meme_10diem_myinstants.wav` | "10 điểm" | 4.09 s | https://www.myinstants.com/en/instant/10-diem-64132/ |
| `meme_bruh_myinstants.wav` | "Bruh" | 0.82 s | https://www.myinstants.com/en/instant/bruh-sound-effect-26614/ |
| `meme_vineboom_myinstants.wav` | "Vine Boom" | 1.25 s | https://www.myinstants.com/en/instant/vine-boom-sound-70972/ |
| `meme_oiban_oi_myinstants.wav` | "Ôi bạn ơi" | 1.06 s | https://www.myinstants.com/en/instant/oi-ban-oi-75860/ |
| `meme_anhkhongmuondau_myinstants.wav` | "Anh không làm gì đâu" (bản gần nhất với "Anh không muốn đâu", cắt 0.10–3.70 s từ clip gốc 19 s) | 3.60 s | https://www.myinstants.com/en/instant/anh-khong-lam-gi-dau-80211/ |
| `meme_gaugau_myinstants.wav` | Tiếng chó sủa "gâu gâu" | 2.74 s | https://www.myinstants.com/en/instant/dog-bark-97859/ |
| `meme_hihi_myinstants.wav` | "Hihihi" (tiếng cười) | 1.95 s | https://www.myinstants.com/en/instant/hihihi-haaa-38581/ |
| `meme_sut_myinstants.wav` | Tiếng sút bóng | 0.79 s | https://www.myinstants.com/en/instant/soccer-kick-12537/ |
| `meme_bum_myinstants.wav` | "Bùm" (tiếng nổ meme) | 1.51 s | https://www.myinstants.com/en/instant/explosion-meme-33169/ |
| `meme_ghechuaghechua_myinstants.wav` | "Ghê chưa ghê chưa" | 4.83 s | https://www.myinstants.com/en/instant/ghe-chua-ghe-chua-91478/ |
| `meme_thaygiaobacoui_myinstants.wav` | "Thầy giáo ba cười" (cắt 3.30–6.90 s từ clip gốc 10.8 s) | 3.60 s | https://www.myinstants.com/en/instant/thay-giao-ba-cuoi-91990/ |
| `meme_heheboy_myinstants.wav` | "Hehe boy" | 1.86 s | https://www.myinstants.com/en/instant/hehe-boy-71168/ |
| `meme_khetday_tts.wav` | "Khét đấy!" (giọng TTS tiếng Việt, hạ pitch) | 0.94 s | Google Translate TTS (vi) |
| `meme_nonlam_tts.wav` | "Non lắm!" (giọng TTS tiếng Việt, hạ pitch) | 0.92 s | Google Translate TTS (vi) |
| `meme_oithoithuaroi_tts.wav` | "Ôi thôi, thua rồi!" (giọng TTS tiếng Việt, hạ pitch) | 1.57 s | Google Translate TTS (vi) |
| `meme_banthigheroi_myinstants.wav` | "Bạn thì ghê rồi" (MixiGaming – "Bạn thì kinh rồi") | 1.92 s | https://www.myinstants.com/en/instant/ban-thi-kinh-roi-18655/ |
| `meme_imausonno_tts.wav` | "I'm a son of a gun, damn it" – **bản đoán** theo phiên âm "i mẩu san nồ đam mịt" (TTS tiếng Anh, hạ pitch) | 2.40 s | Google Translate TTS (en) |
| `meme_wiishop_myinstants.wav` | Nhạc Wii Shop Channel (cắt 4,8 s, fade out) | 4.80 s | https://www.myinstants.com/en/instant/wii-shop-channel-bgm-35864/ |
| `meme_sadviolin_myinstants.wav` | Nhạc violin buồn (meme) | 4.55 s | https://www.myinstants.com/en/instant/meme-violin-sad-violin-1460/ |
| `meme_amongus_myinstants.wav` | Among Us – role reveal | 4.57 s | https://www.myinstants.com/en/instant/among-us-role-reveal-sound-34956/ |
| `meme_coffindance_myinstants.wav` | Coffin Dance / Astronomia (cắt 4,8 s, fade out) | 4.80 s | https://www.myinstants.com/en/instant/coffin-dance-meme-song-99895/ |
| `meme_fahhh_myinstants.wav` | "FAHHHH" (tiếng hét meme) | 1.66 s | https://www.myinstants.com/en/instant/fahhhhh-89304/ |
| `meme_spongebobfail_myinstants.wav` | SpongeBob Fail | 3.28 s | https://www.myinstants.com/en/instant/spongebob-fail-11236/ |
| `meme_animewow_myinstants.wav` | Anime Wow | 3.38 s | https://www.myinstants.com/en/instant/anime-wow/ |
| `meme_bonecrack_myinstants.wav` | Bone Crack | 0.74 s | https://www.myinstants.com/en/instant/bone-crack-23901/ |
| `meme_chickentree_myinstants.wav` | Chicken on tree screaming (cắt 5 s, fade out) | 5.00 s | https://www.myinstants.com/en/instant/chicken-on-tree-screaming-53890/ |

## Thông số kỹ thuật

- Định dạng: WAV PCM 16-bit, mono, 44.1 kHz.
- Chuẩn hóa loudness: `loudnorm I=-16 LUFS, TP=-1.5 dB` để các clip nghe đều nhau.
- Tăng âm: clip được đẩy loudness lên mức nghe rõ khi chơi (tổng cộng khoảng
  +9.5 dB so với bản gốc, limiter giữ đỉnh ≤ -1 dBTP). `MemeDeathSound.volume`
  đặt 1.0 (Unity giới hạn âm lượng nguồn ở 1.0, nên muốn to hơn 100% phải tăng
  trực tiếp trong file).
- 5 clip thêm ngày 14/11/2026 (`fahhh`, `spongebobfail`, `animewow`, `bonecrack`,
  `chickentree`) được chuẩn hóa theo RMS ≈ -12 dBFS (tương đương các clip cũ) và
  limiter giữ đỉnh ≤ -1 dBTP.
- Khi meme phát, `AudioManager.DuckBGMForMeme()` hạ nhạc nền còn 18% trong đúng
  thời lượng clip rồi fade trả lại sau 0.4 s, để meme không bị nhạc nền lấn.
- Import settings: `Load Type = Decompress On Load`, `Force To Mono = On`,
  `Preload Audio Data = On` (xem file `.meta` tương ứng).
- Thời lượng: tất cả đều dưới 5 giây.

## Lưu ý bản quyền

Các clip trên MyInstants do người dùng tải lên, phần lớn là trích đoạn từ video/
chương trình gốc nên **có thể vẫn thuộc bản quyền của tác giả gốc**. Chúng phù hợp
cho mục đích học tập / đồ án / bản demo nội bộ. Nếu phát hành thương mại, hãy thay
bằng âm thanh CC0 (ví dụ Freesound lọc CC0) hoặc âm thanh tự tạo.

Các file `*_tts.wav` không tìm thấy bản thu meme gốc trên các thư viện công khai,
nên được tạo bằng TTS của Google Translate rồi hạ pitch + nén nhẹ để nghe gần giọng
meme hơn:

- `meme_khetday_tts.wav`, `meme_nonlam_tts.wav`, `meme_oithoithuaroi_tts.wav` – TTS tiếng Việt.
- `meme_imausonno_tts.wav` – **bản đoán**: cụm "i mẩu san nồ đam mịt" chỉ tìm thấy
  video YouTube cùng tên (không có bản tải công khai), nên tạm đọc câu tiếng Anh
  gần nhất về mặt phiên âm ("I'm a son of a gun, damn it"). Nếu có file gốc, chỉ cần
  thay thế file cùng tên.

Có thể thay bằng bản thu thật nếu tìm được.

## Cách gắn thêm cho quái vật mới

1. Chọn GameObject quái vật → **Add Component → Meme Death Sound**.
2. Để trống ô **Meme Clips** (script tự nạp random từ thư mục này), hoặc kéo clip
   riêng vào nếu muốn bộ âm thanh khác cho quái đó.
3. Đảm bảo quái vật có `MonsterCombatController`; script tự gọi khi quái vật chết.
