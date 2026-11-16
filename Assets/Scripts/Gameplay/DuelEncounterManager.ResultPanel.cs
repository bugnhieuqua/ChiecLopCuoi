using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.Gameplay
{
    public partial class DuelEncounterManager
    {
        public void BindResultKOPanel()
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;

            var koTf = hudCanvas.transform.Find("Result_KO_Panel");
            if (koTf == null) return;

            resultKoPanel = koTf.gameObject;

            var headerTf = koTf.Find("Result_Stone_Board/Result_Header_Text");
            if (headerTf != null)
            {
                resultHeaderText = headerTf.GetComponent<TMP_Text>();
                var headerRt = headerTf as RectTransform;
                if (headerRt != null)
                {
                    headerRt.anchorMin = new Vector2(0.5f, 0.5f);
                    headerRt.anchorMax = new Vector2(0.5f, 0.5f);
                    headerRt.pivot = new Vector2(0.5f, 0.5f);
                    headerRt.anchoredPosition = new Vector2(0f, 140f);
                    headerRt.sizeDelta = new Vector2(340f, 54f);
                }
                if (resultHeaderText != null)
                {
                    resultHeaderText.fontSize = 42f;
                    resultHeaderText.alignment = TextAlignmentOptions.Center;
                }
                if (resultHeaderText != null && !_cachedHeaderDefaults)
                {
                    _cachedHeaderDefaults = true;
                    _origHeaderColor = resultHeaderText.color;
                    _origHeaderEnableGradient = resultHeaderText.enableVertexGradient;
                    _origHeaderGradient = resultHeaderText.colorGradient;
                }
            }

            // Gỡ bỏ KO_Result_Crest bên trên bia đá theo thiết kế chuẩn (không che Zone_Objective_Banner)
            Transform oldCrest = koTf.Find("KO_Result_Crest");
            if (oldCrest != null)
            {
                oldCrest.gameObject.SetActive(false);
            }

            var rematchTf = koTf.Find("Result_Stone_Board/Result_Buttons_Row/Btn_Rematch");
            if (rematchTf != null)
            {
                btnRematch = rematchTf.GetComponent<Button>();
                if (btnRematch != null)
                {
                    btnRematch.onClick.RemoveAllListeners();
                    btnRematch.onClick.AddListener(OnRetryClicked);
                }
            }

            var homeTf = koTf.Find("Result_Stone_Board/Result_Buttons_Row/Btn_Home");
            if (homeTf != null)
            {
                btnHome = homeTf.GetComponent<Button>();
                if (btnHome != null)
                {
                    btnHome.onClick.RemoveAllListeners();
                    btnHome.onClick.AddListener(OnReturnMainMenuClicked);
                }
            }

            var contTf = koTf.Find("Result_Stone_Board/Result_Buttons_Row/Btn_Continue");
            if (contTf != null)
            {
                btnContinue = contTf.GetComponent<Button>();
                if (btnContinue != null)
                {
                    btnContinue.onClick.RemoveAllListeners();
                    btnContinue.onClick.AddListener(OnContinueClicked);
                }
            }

            var musicTf = koTf.Find("Result_Stone_Board/Music_Row/Slider_Music");
            if (musicTf != null)
            {
                resultMusicSlider = musicTf.GetComponent<Slider>();
                if (resultMusicSlider != null)
                {
                    resultMusicSlider.onValueChanged.RemoveAllListeners();
                    resultMusicSlider.onValueChanged.AddListener(val =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(val);
                    });
                }
            }
            _resultMusicIconImage = AudioManager.BindMuteIconButton(
                koTf.Find("Result_Stone_Board/Music_Row/Icon_Music"),
                () => { if (AudioManager.Instance != null) AudioManager.Instance.ToggleBgmMute(); });

            var soundTf = koTf.Find("Result_Stone_Board/Sound_Row/Slider_Sound");
            if (soundTf != null)
            {
                resultSoundSlider = soundTf.GetComponent<Slider>();
                if (resultSoundSlider != null)
                {
                    resultSoundSlider.onValueChanged.RemoveAllListeners();
                    resultSoundSlider.onValueChanged.AddListener(val =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(val);
                    });
                }
            }
            _resultSoundIconImage = AudioManager.BindMuteIconButton(
                koTf.Find("Result_Stone_Board/Sound_Row/Icon_Sound"),
                () => { if (AudioManager.Instance != null) AudioManager.Instance.ToggleSfxMute(); });

            SyncResultAudioControls();

            var starsTf = koTf.Find("Result_Stone_Board/Result_Stars_Row");
            if (starsTf != null)
            {
                resultStarImages = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    var st = starsTf.Find($"Star_{i + 1}");
                    if (st != null) resultStarImages[i] = st.GetComponent<Image>();
                }
                if (resultGoldStarSprite == null && resultStarImages[0] != null)
                {
                    resultGoldStarSprite = resultStarImages[0].sprite;
                }
            }

            var rewardIconTf = koTf.Find("Result_Stone_Board/Result_Reward_Row/Icon_Reward_Tire");
            if (rewardIconTf != null)
            {
                var rewardIconImg = rewardIconTf.GetComponent<Image>();
                Sprite tireSp = Resources.Load<Sprite>("Tires/tire_02");
                if (rewardIconImg != null && tireSp != null)
                {
                    rewardIconImg.sprite = tireSp;
                    rewardIconImg.preserveAspect = true;
                }
                var rewardIconRt = rewardIconTf as RectTransform;
                if (rewardIconRt != null)
                {
                    rewardIconRt.anchoredPosition = new Vector2(-46f, 0f);
                    rewardIconRt.sizeDelta = new Vector2(32f, 32f);
                }
            }

            var rewardTf = koTf.Find("Result_Stone_Board/Result_Reward_Row/Result_Reward_Text");
            if (rewardTf != null)
            {
                var rewardTextRt = rewardTf as RectTransform;
                if (rewardTextRt != null)
                {
                    rewardTextRt.anchoredPosition = new Vector2(30f, 0f);
                    rewardTextRt.sizeDelta = new Vector2(118f, 36f);
                }
                resultRewardText = rewardTf.GetComponent<TMP_Text>();
                if (resultRewardText != null)
                {
                    ChiecLopCuoi.UI.ThemeFontManager.ApplyLoginFont(resultRewardText);
                    resultRewardText.alignment = TextAlignmentOptions.Left;
                }
            }
        }

        public void ShowMatchResult(bool playerWon, string reason)
        {
            _isShowingResult = true;
            _lastResultPlayerWon = playerWon;

            // Tuyệt đối tắt dải lụa đen (Duel_Encounter_Banner) khi kết thúc trận đấu
            if (encounterOverlayRoot != null)
            {
                encounterOverlayRoot.SetActive(false);
            }

            // Nhạc thắng/thua được phát khi bảng kết quả hiện ra (sau màn K.O.),
            // nhường chỗ cho meme K.O. ở ShowKOSplashThenResultRoutine.
            if (playerWon)
            {
                _lastSecondsRemaining = BattleManager.Instance != null ? BattleManager.Instance.LastSecondsRemainingAtFinish : 0;
                _lastEarnedStars = GameRepository.EvaluateStarsByRemainingSeconds(true, _lastSecondsRemaining);
                string activeScene = SceneManager.GetActiveScene().name;
                _lastDeltaTires = GameRepository.SaveLevelClearRecord(activeScene, _lastEarnedStars, out _);
            }
            else
            {
                _lastSecondsRemaining = 0;
                _lastEarnedStars = 0;
                _lastDeltaTires = 0;
            }

            if (resultKoPanel == null)
            {
                BindResultKOPanel();
            }

            if (_resultSequenceRoutine != null)
            {
                StopCoroutine(_resultSequenceRoutine);
            }
            _resultSequenceRoutine = StartCoroutine(ShowKOSplashThenResultRoutine(playerWon));
        }

        private IEnumerator ShowKOSplashThenResultRoutine(bool playerWon)
        {
            var notebookCtrl = FindFirstObjectByType<HUDNotebookController>(FindObjectsInactive.Include);
            if (notebookCtrl != null) notebookCtrl.SetKOLabelVisible(true);

            // Meme K.O.: phát ngay lúc màn hình K.O. đập xuống
            MemeKoSound.PlayRandom();

            Sprite koSplashSprite = Resources.Load<Sprite>("UI/ui_ko_splash");
            if (koSplashSprite != null)
            {
                yield return StartCoroutine(PlayKOSplashSequence(koSplashSprite));
            }

            PresentResultKOPanel(playerWon);
            _resultSequenceRoutine = null;
        }

        private void PresentResultKOPanel(bool playerWon)
        {
            // Nhạc thắng/thua phát khi bảng kết quả hiện ra
            if (AudioManager.Instance != null)
            {
                if (playerWon) AudioManager.Instance.PlayVictory();
                else AudioManager.Instance.PlayDefeat();
            }

            if (resultKoPanel == null) return;

            if (encounterOverlayRoot != null)
            {
                encounterOverlayRoot.SetActive(false);
            }

            Transform oldCrest = resultKoPanel.transform.Find("KO_Result_Crest");
            if (oldCrest != null)
            {
                oldCrest.gameObject.SetActive(false);
            }

            ApplyResultWinLoseVisual(playerWon);
            ApplyResultStarsAndRewardVisual(playerWon);

            SyncResultAudioControls();

            if (btnContinue != null)
            {
                btnContinue.interactable = playerWon;
                var img = btnContinue.GetComponent<Image>();
                if (img != null)
                {
                    img.color = playerWon ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
            }

            resultKoPanel.transform.localScale = Vector3.one;
            // Giữ Zone_Objective_Banner hiển thị rõ nét ngay phía trên Result_Stone_Board (không bị đè mờ)
            var hudCanvas = resultKoPanel.transform.parent;
            if (hudCanvas != null)
            {
                Transform objBanner = hudCanvas.Find("Zone_Objective_Banner");
                if (objBanner != null)
                {
                    int targetIdx = Mathf.Max(0, objBanner.GetSiblingIndex() - 1);
                    resultKoPanel.transform.SetSiblingIndex(targetIdx);
                }
            }
            resultKoPanel.SetActive(true);

            if (playerWon && _lastEarnedStars > 0)
            {
                StartTireRewardFlySequence(_lastEarnedStars);
            }
        }

        private void SyncResultAudioControls()
        {
            if (AudioManager.Instance == null) return;
            HandleResultBgmVolumeChanged(AudioManager.Instance.bgmVolume);
            HandleResultSfxVolumeChanged(AudioManager.Instance.sfxVolume);
        }

        private void HandleResultBgmVolumeChanged(float value)
        {
            if (resultMusicSlider != null)
            {
                resultMusicSlider.SetValueWithoutNotify(value);
            }
            AudioManager.UpdateMuteIconVisual(_resultMusicIconImage, value <= 0.001f);
        }

        private void HandleResultSfxVolumeChanged(float value)
        {
            if (resultSoundSlider != null)
            {
                resultSoundSlider.SetValueWithoutNotify(value);
            }
            AudioManager.UpdateMuteIconVisual(_resultSoundIconImage, value <= 0.001f);
        }

        private void ApplyResultStarsAndRewardVisual(bool playerWon)
        {
            int stars = playerWon ? Mathf.Clamp(_lastEarnedStars, 0, 3) : 0;
            if (resultStarImages != null)
            {
                for (int i = 0; i < resultStarImages.Length; i++)
                {
                    var img = resultStarImages[i];
                    if (img == null) continue;
                    bool lit = i < stars;
                    if (lit && resultGoldStarSprite != null) img.sprite = resultGoldStarSprite;
                    else if (!lit && resultEmptyStarSprite != null) img.sprite = resultEmptyStarSprite;
                    img.color = lit ? Color.white : new Color(0.45f, 0.48f, 0.54f, 0.90f);
                }
            }

            if (resultRewardText != null)
            {
                ChiecLopCuoi.UI.ThemeFontManager.ApplyLoginFont(resultRewardText);
                if (playerWon && stars > 0)
                {
                    resultRewardText.text = $"+{stars} Lốp";
                    resultRewardText.color = new Color32(42, 24, 16, 255);
                }
                else
                {
                    resultRewardText.text = "+0 Lốp";
                    resultRewardText.color = new Color32(140, 30, 26, 255);
                }
            }
        }

        public void StartTireRewardFlySequence(int tireCount)
        {
            StopAndClearTireFlySequence();
            if (tireCount <= 0 || !gameObject.activeInHierarchy) return;
            _tireFlyRoutine = StartCoroutine(PlayTireRewardFlyRoutine(tireCount));
        }

        private IEnumerator PlayTireRewardFlyRoutine(int tireCount)
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) yield break;

            Sprite tireSp = Resources.Load<Sprite>("Tires/tire_02");
            Sprite[] tireSprites = tireSp != null ? new[] { tireSp } : Resources.LoadAll<Sprite>("Tires");
            if (tireSprites == null || tireSprites.Length == 0) yield break;

            for (int i = 0; i < tireCount; i++)
            {
                Sprite sp = tireSprites[Random.Range(0, tireSprites.Length)];
                StartCoroutine(AnimateSingleRewardTire(hudCanvas.transform, sp, i));
                yield return new WaitForSecondsRealtime(0.28f);
            }

            _tireFlyRoutine = null;
        }

        private IEnumerator AnimateSingleRewardTire(Transform canvasTf, Sprite sprite, int index)
        {
            if (canvasTf == null || sprite == null) yield break;

            var go = new GameObject($"Reward_Fly_Tire_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasTf, false);
            go.transform.SetAsLastSibling();
            _activeRewardTires.Add(go);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);

            // Bắt đầu từ cạnh phải màn hình -> cong xuống góc dưới bên trái
            Vector2 startPos = new Vector2(540f, 30f + index * 46f);
            Vector2 ctrlPos = new Vector2(60f, -150f + index * 24f);
            Vector2 endPos = new Vector2(-530f, -270f);
            rt.anchoredPosition = startPos;

            float fadeInDur = 0.22f;
            float moveDur = 0.72f;
            float fadeOutDur = 0.26f;
            float totalDur = fadeInDur + moveDur + fadeOutDur;
            float elapsed = 0f;

            while (elapsed < totalDur)
            {
                if (go == null) yield break;
                elapsed += Time.unscaledDeltaTime;

                float alpha;
                float scale;
                if (elapsed <= fadeInDur)
                {
                    float u = elapsed / fadeInDur;
                    alpha = Mathf.SmoothStep(0f, 1f, u);
                    scale = Mathf.Lerp(0.65f, 1.12f, u);
                }
                else if (elapsed <= fadeInDur + moveDur)
                {
                    alpha = 1f;
                    scale = Mathf.Lerp(1.12f, 0.92f, (elapsed - fadeInDur) / moveDur);
                }
                else
                {
                    float u = Mathf.Clamp01((elapsed - fadeInDur - moveDur) / fadeOutDur);
                    alpha = 1f - Mathf.SmoothStep(0f, 1f, u);
                    scale = Mathf.Lerp(0.92f, 0.45f, u);
                }

                float moveT = Mathf.Clamp01(elapsed / (fadeInDur + moveDur));
                float eased = Mathf.SmoothStep(0f, 1f, moveT);
                float inv = 1f - eased;
                Vector2 pos = (inv * inv) * startPos + (2f * inv * eased) * ctrlPos + (eased * eased) * endPos;

                rt.anchoredPosition = pos;
                rt.localScale = new Vector3(scale, scale, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, elapsed * 240f);
                img.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            if (go != null)
            {
                _activeRewardTires.Remove(go);
                Destroy(go);
            }
        }

        private void StopAndClearTireFlySequence()
        {
            if (_tireFlyRoutine != null)
            {
                StopCoroutine(_tireFlyRoutine);
                _tireFlyRoutine = null;
            }
            for (int i = _activeRewardTires.Count - 1; i >= 0; i--)
            {
                if (_activeRewardTires[i] != null) Destroy(_activeRewardTires[i]);
            }
            _activeRewardTires.Clear();
        }

        public void OnContinueClicked()
        {
            _isShowingResult = false;
            StopAndClearTireFlySequence();
            HideKOSplashImmediate();

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "level6" && _lastResultPlayerWon)
            {
                if (GameRepository.GetClearedLevelCount() >= 6)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                    if (resultKoPanel != null) resultKoPanel.SetActive(false);
                    var hudCanvas = GameObject.Find("HUD_Canvas");
                    if (hudCanvas != null)
                    {
                        ChiecLopCuoi.UI.VictoryFinalePanel.ShowOrCreate(hudCanvas.transform);
                        return;
                    }
                }
                else
                {
                    Time.timeScale = 1f;
                    if (Application.CanStreamedLevelBeLoaded("LevelSelectScene"))
                    {
                        SceneManager.LoadScene("LevelSelectScene");
                        return;
                    }
                }
            }

            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(nextSceneName) && nextSceneName != activeScene && Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                ChiecLopCuoi.UI.LoadingSceneController.LoadWithLoading(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(activeScene);
            }
        }

        public void OnRetryClicked()
        {
            _isShowingResult = false;
            StopAndClearTireFlySequence();
            HideKOSplashImmediate();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnReturnMainMenuClicked()
        {
            _isShowingResult = false;
            StopAndClearTireFlySequence();
            HideKOSplashImmediate();
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("LoginScene"))
            {
                SceneManager.LoadScene("LoginScene");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private TMP_FontAsset ResolveWinLoseFont()
        {
            var globalFont = ChiecLopCuoi.UI.ThemeFontManager.GetGlobalUiFont();
            if (globalFont != null)
            {
                winLoseFont = globalFont;
                winLoseFont.TryAddCharacters("WINLOST");
                return winLoseFont;
            }
            return winLoseFont;
        }

        private void ApplyResultWinLoseVisual(bool playerWon)
        {
            if (resultHeaderText == null) return;

            Transform parentBoard = resultHeaderText.transform.parent;
            Transform gfxTf = parentBoard != null ? parentBoard.Find("Result_Header_Graphic") : null;
            if (gfxTf != null)
            {
                gfxTf.gameObject.SetActive(false);
            }

            resultHeaderText.enabled = true;
            resultHeaderText.text = playerWon ? "WIN" : "LOST";

            if (playerWon)
            {
                if (_cachedHeaderDefaults)
                {
                    resultHeaderText.color = _origHeaderColor;
                    resultHeaderText.enableVertexGradient = _origHeaderEnableGradient;
                    resultHeaderText.colorGradient = _origHeaderGradient;
                }
            }
            else
            {
                resultHeaderText.color = new Color(1f, 0.30f, 0.25f, 1f);
                if (_cachedHeaderDefaults && _origHeaderEnableGradient)
                {
                    resultHeaderText.enableVertexGradient = true;
                    resultHeaderText.colorGradient = new VertexGradient(
                        new Color(1f, 0.42f, 0.36f, 1f),
                        new Color(1f, 0.16f, 0.16f, 1f),
                        new Color(1f, 0.85f, 0.38f, 1f),
                        new Color(0.90f, 0.28f, 0.24f, 1f));
                }
            }
        }

        private IEnumerator PlayKOSplashSequence(Sprite koSplashSprite)
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) yield break;

            if (_koSplashOverlay == null)
            {
                Transform existing = hudCanvas.transform.Find("KO_Splash_Overlay");
                _koSplashOverlay = existing != null
                    ? existing.gameObject
                    : new GameObject("KO_Splash_Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                if (existing == null) _koSplashOverlay.transform.SetParent(hudCanvas.transform, false);
            }

            RectTransform overlayRt = _koSplashOverlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image backdrop = _koSplashOverlay.GetComponent<Image>();
            backdrop.color = new Color(0.02f, 0.01f, 0.04f, 0.48f);
            backdrop.raycastTarget = false;

            CanvasGroup cg = _koSplashOverlay.GetComponent<CanvasGroup>();
            if (cg == null) cg = _koSplashOverlay.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            Transform flashTf = _koSplashOverlay.transform.Find("KO_Flash");
            GameObject flashGo = flashTf != null
                ? flashTf.gameObject
                : new GameObject("KO_Flash", typeof(RectTransform), typeof(Image));
            if (flashTf == null) flashGo.transform.SetParent(_koSplashOverlay.transform, false);
            RectTransform flashRt = flashGo.GetComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero;
            flashRt.anchorMax = Vector2.one;
            flashRt.offsetMin = Vector2.zero;
            flashRt.offsetMax = Vector2.zero;
            Image flashImg = flashGo.GetComponent<Image>();
            flashImg.color = new Color(1f, 0.90f, 0.55f, 0.45f);
            flashImg.raycastTarget = false;

            Transform graphicTf = _koSplashOverlay.transform.Find("KO_Graphic");
            GameObject graphicGo = graphicTf != null
                ? graphicTf.gameObject
                : new GameObject("KO_Graphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (graphicTf == null) graphicGo.transform.SetParent(_koSplashOverlay.transform, false);
            RectTransform graphicRt = graphicGo.GetComponent<RectTransform>();
            graphicRt.anchorMin = new Vector2(0.5f, 0.5f);
            graphicRt.anchorMax = new Vector2(0.5f, 0.5f);
            graphicRt.pivot = new Vector2(0.5f, 0.5f);
            graphicRt.sizeDelta = new Vector2(880f, 440f);
            graphicRt.anchoredPosition = new Vector2(0f, 20f);

            Image koImg = graphicGo.GetComponent<Image>();
            koImg.sprite = koSplashSprite;
            koImg.preserveAspect = true;
            koImg.raycastTarget = false;

            _koSplashOverlay.transform.SetAsLastSibling();
            _koSplashOverlay.SetActive(true);

            // Pha 1: Đập mạnh K.O. từ phóng to xuống (0.18s) kèm lóe sáng & rung nhẹ
            float slamDuration = 0.18f;
            float t = 0f;
            while (t < slamDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / slamDuration);
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                float scale = Mathf.Lerp(2.25f, 0.96f, eased);
                graphicRt.localScale = new Vector3(scale, scale, 1f);
                flashImg.color = new Color(1f, 0.90f, 0.55f, Mathf.Lerp(0.50f, 0f, u));
                float shake = (1f - u) * 14f;
                graphicRt.anchoredPosition = new Vector2(Random.Range(-shake, shake), 20f + Random.Range(-shake, shake));
                yield return null;
            }
            flashImg.color = new Color(1f, 0.90f, 0.55f, 0f);

            // Pha 2: Giữ & nhịp thở nhẹ (0.95s)
            float holdDuration = 0.95f;
            t = 0f;
            while (t < holdDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / holdDuration);
                float settle = Mathf.Lerp(0.96f, 1.03f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, u * 3f)));
                float shake = Mathf.Max(0f, 1f - u * 4f) * 6f;
                graphicRt.anchoredPosition = new Vector2(Random.Range(-shake, shake), 20f + Random.Range(-shake, shake));
                graphicRt.localScale = new Vector3(settle, settle, 1f);
                yield return null;
            }

            // Pha 3: Mờ dần mượt mà trước khi hiện Result_KO_Panel (0.20s)
            float fadeDuration = 0.20f;
            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeDuration);
                cg.alpha = 1f - u;
                float s = Mathf.Lerp(1.03f, 1.12f, u);
                graphicRt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            HideKOSplashImmediate();
        }

        private void HideKOSplashImmediate()
        {
            if (_koSplashOverlay != null)
            {
                _koSplashOverlay.SetActive(false);
            }
            var notebookCtrl = FindFirstObjectByType<HUDNotebookController>(FindObjectsInactive.Include);
            if (notebookCtrl != null) notebookCtrl.SetKOLabelVisible(false);
        }
    }
}
