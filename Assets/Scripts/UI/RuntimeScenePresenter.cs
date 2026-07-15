using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstForm
{
    /// <summary>
    /// 정식 아트가 없는 MVP에서 상태별 장면과 전투 연출을 단순 도형으로 표현합니다.
    /// 모든 Graphic은 입력을 막지 않으며 기존 개발 UI와 독립적으로 동작합니다.
    /// </summary>
    public sealed class RuntimeScenePresenter : MonoBehaviour
    {
        private static Sprite circleSprite;
        private static Sprite triangleSprite;
        private static Sprite ringSprite;
        private static Sprite slashArcSprite;

        private const float PlayerAttackDuration = 0.32f;
        private const float EnemyAttackDuration = 0.34f;
        private const float StrongAttackDuration = 0.48f;
        private const float HitReactionDuration = 0.22f;
        private static readonly Vector2 DefaultPlayerArtworkSize = new Vector2(320f, 320f);
        private static readonly Vector2 DefaultPlayerArtworkOffset = new Vector2(0f, 18f);
        private static readonly Vector2 DefaultEnemyArtworkSize = new Vector2(340f, 340f);
        private static readonly Vector2 DefaultEnemyArtworkOffset = new Vector2(0f, 16f);

        private LayoutElement stageLayout;
        private LayoutElement statusLayout;
        private LayoutElement centerLayout;
        private LayoutElement auxiliaryBarLayout;
        private LayoutElement soulGrowthLayout;
        private LayoutElement currentLootLayout;
        private LayoutElement logPanelLayout;
        private LayoutElement buttonPanelLayout;
        private LayoutElement soulGrowthInfoLayout;
        private LayoutElement soulGrowthTextLayout;
        private LayoutElement currentLootTextLayout;
        private LayoutElement logContentLayout;
        private LayoutElement[] statePanelLayouts;
        private RectTransform layoutRootRect;
        private VerticalLayoutGroup layoutRootGroup;
        private float lastAppliedRootHeight = -1f;
        private GameObject statusTitleObject;
        private GameObject statusGridObject;
        private GameObject soulGrowthButtonGrid;
        private GameObject logTitleObject;
        private GameObject debugButtonGroup;

        private Image sky;
        private Image sun;
        private Image farMountain;
        private Image nearMountain;
        private Image ground;
        private Image mistOne;
        private Image mistTwo;
        private Image flashOverlay;
        private Image slashEffect;
        private Image enemySlashEffect;
        private Image aura;

        private RectTransform playerRoot;
        private RectTransform playerVisualRoot;
        private RectTransform playerPlaceholderRoot;
        private CanvasGroup playerCanvas;
        private Image playerBody;
        private Image playerRobe;
        private Image playerHead;
        private RectTransform playerSword;
        private Image playerArtwork;
        private Image playerHitBurst;

        private RectTransform enemyRoot;
        private RectTransform enemyVisualRoot;
        private RectTransform enemyPlaceholderRoot;
        private CanvasGroup enemyCanvas;
        private Image enemyBody;
        private Image enemyRobe;
        private Image enemyHead;
        private Image enemyAccent;
        private RectTransform enemyWeapon;
        private GameObject enemyShield;
        private GameObject enemySecondBlade;
        private Image enemyArtwork;
        private Image enemyHitBurst;
        private Image enemyStrongAura;

        private GameObject trainingProps;
        private GameObject explorationProps;
        private GameObject selectionScrolls;
        private GameObject bodyCandidates;
        private CanvasGroup[] candidateCanvases;

        private GameObject playerGaugeRoot;
        private Image playerHealthFill;
        private Image playerEnergyFill;
        private TextMeshProUGUI playerHealthText;
        private TextMeshProUGUI playerEnergyText;
        private GameObject enemyGaugeRoot;
        private Image enemyHealthFill;
        private TextMeshProUGUI enemyHealthText;
        private GameObject warningRoot;
        private CanvasGroup warningCanvas;
        private TextMeshProUGUI warningText;
        private TextMeshProUGUI sceneTitleText;
        private TextMeshProUGUI sceneCaptionText;

        private FirstFormGameState currentState = FirstFormGameState.None;
        private EnemyArchetype currentEnemyArchetype = (EnemyArchetype)(-1);
        private string currentEnemyName = string.Empty;
        private bool strongAttackWarning;
        private float playerAttackTimer;
        private float enemyAttackTimer;
        private float enemyStrongAttackTimer;
        private float playerHitReactionTimer;
        private float enemyHitReactionTimer;
        private float hitFlashTimer;
        private Vector2 playerBasePosition;
        private Vector2 enemyBasePosition;
        private Coroutine delayedLayoutRebuild;
        private readonly RuntimeFrameTrack playerFrameTrack = new RuntimeFrameTrack();
        private readonly RuntimeFrameTrack enemyFrameTrack = new RuntimeFrameTrack();

        /// <summary>
        /// 장면에 필요한 도형, 실루엣, 게이지를 런타임에 한 번 생성합니다.
        /// </summary>
        internal void Initialize(TMP_FontAsset koreanFont)
        {
            Image stageImage = GetComponent<Image>();
            if (stageImage == null)
            {
                stageImage = gameObject.AddComponent<Image>();
            }

            stageImage.color = new Color(0.72f, 0.84f, 0.86f, 1f);
            stageImage.raycastTarget = false;
            CanvasGroup inputPassThrough = gameObject.AddComponent<CanvasGroup>();
            inputPassThrough.blocksRaycasts = false;
            inputPassThrough.interactable = false;

            sky = CreateStretchImage("Sky", transform, new Color(0.72f, 0.84f, 0.86f, 1f), Vector2.zero, Vector2.one);
            sun = CreateAnchoredImage("Sun", transform, new Color(1f, 0.91f, 0.63f, 0.9f), new Vector2(0.17f, 0.76f), new Vector2(92f, 92f), Vector2.zero, GetCircleSprite());

            farMountain = CreateStretchImage("FarMountains", transform, new Color(0.48f, 0.65f, 0.66f, 0.75f), new Vector2(0f, 0.24f), new Vector2(1f, 0.76f), GetTriangleSprite());
            nearMountain = CreateStretchImage("NearMountains", transform, new Color(0.24f, 0.43f, 0.43f, 0.82f), new Vector2(-0.12f, 0.11f), new Vector2(0.65f, 0.63f), GetTriangleSprite());
            nearMountain.rectTransform.localEulerAngles = new Vector3(0f, 180f, 0f);
            ground = CreateStretchImage("Ground", transform, new Color(0.35f, 0.50f, 0.47f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0.30f));
            mistOne = CreateStretchImage("MistOne", transform, new Color(0.94f, 0.97f, 0.96f, 0.28f), new Vector2(-0.08f, 0.31f), new Vector2(0.74f, 0.39f));
            mistTwo = CreateStretchImage("MistTwo", transform, new Color(0.94f, 0.97f, 0.96f, 0.20f), new Vector2(0.33f, 0.49f), new Vector2(1.12f, 0.56f));

            BuildTrainingProps();
            BuildExplorationProps();
            BuildSelectionScrolls();
            BuildBodyCandidates();
            BuildPlayerSilhouette();
            BuildEnemySilhouette();
            BuildSceneLabels(koreanFont);
            BuildGauges(koreanFont);
            BuildWarning(koreanFont);

            aura = CreateAnchoredImage("BreakthroughAura", transform, new Color(0.98f, 0.82f, 0.36f, 0.55f), new Vector2(0.5f, 0.46f), new Vector2(260f, 260f), Vector2.zero, GetRingSprite());
            aura.gameObject.SetActive(false);
            slashEffect = CreateAnchoredImage("PlayerSlashEffect", transform, new Color(0.76f, 0.94f, 1f, 0f), new Vector2(0.5f, 0.44f), new Vector2(360f, 130f), Vector2.zero, GetSlashArcSprite());
            slashEffect.rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
            enemySlashEffect = CreateAnchoredImage("EnemySlashEffect", transform, new Color(1f, 0.48f, 0.24f, 0f), new Vector2(0.5f, 0.46f), new Vector2(400f, 150f), Vector2.zero, GetSlashArcSprite());
            enemySlashEffect.rectTransform.localEulerAngles = new Vector3(0f, 180f, 172f);
            flashOverlay = CreateStretchImage("HitFlash", transform, new Color(1f, 0.25f, 0.18f, 0f), Vector2.zero, Vector2.one);
            flashOverlay.transform.SetAsLastSibling();
            warningRoot.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 장면과 정보 카드가 합쳐서 일정한 높이를 쓰도록 LayoutElement를 연결합니다.
        /// </summary>
        internal void ConfigureLayout(
            GameObject layoutRoot,
            GameObject statusBar,
            GameObject centerPanel,
            GameObject auxiliaryBar,
            GameObject soulGrowthPanel,
            GameObject currentLootPanel,
            GameObject logPanel,
            GameObject buttonPanel,
            params GameObject[] statePanels)
        {
            layoutRootRect = layoutRoot != null ? layoutRoot.GetComponent<RectTransform>() : null;
            layoutRootGroup = layoutRoot != null ? layoutRoot.GetComponent<VerticalLayoutGroup>() : null;
            stageLayout = GetComponent<LayoutElement>();
            statusLayout = GetLayout(statusBar);
            centerLayout = centerPanel != null ? centerPanel.GetComponent<LayoutElement>() : null;
            auxiliaryBarLayout = GetLayout(auxiliaryBar);
            soulGrowthLayout = GetLayout(soulGrowthPanel);
            currentLootLayout = GetLayout(currentLootPanel);
            logPanelLayout = GetLayout(logPanel);
            buttonPanelLayout = GetLayout(buttonPanel);
            statusTitleObject = FindChild(statusBar, "TitleText");
            statusGridObject = FindChild(statusBar, "StatusGrid");
            soulGrowthButtonGrid = FindChild(soulGrowthPanel, "SoulGrowthButtonGrid");
            logTitleObject = FindChild(logPanel, "BattleLogTitleText");
            debugButtonGroup = FindChild(auxiliaryBar, "DebugControlPanel/DebugControlGrid");
            soulGrowthInfoLayout = GetLayout(FindChild(soulGrowthPanel, "SoulGrowthInfoArea"));
            soulGrowthTextLayout = GetLayout(FindChild(soulGrowthPanel, "SoulGrowthInfoArea/SoulGrowthText"));
            currentLootTextLayout = GetLayout(FindChild(currentLootPanel, "CurrentLootText"));
            logContentLayout = GetLayout(FindChild(logPanel, "BattleLogContentRow"));
            statePanelLayouts = new LayoutElement[statePanels != null ? statePanels.Length : 0];

            for (int i = 0; i < statePanelLayouts.Length; i++)
            {
                statePanelLayouts[i] = statePanels[i] != null ? statePanels[i].GetComponent<LayoutElement>() : null;
            }
        }

        /// <summary>
        /// 현재 상태, 플레이어, 적 정보를 장면 팔레트와 게이지에 반영합니다.
        /// </summary>
        internal void Refresh(FirstFormGameState state, PlayerData player, EnemyData enemy)
        {
            gameObject.SetActive(state != FirstFormGameState.None);
            if (state == FirstFormGameState.None)
            {
                return;
            }

            bool stateChanged = currentState != state;
            bool enemyChanged = enemy != null && (currentEnemyArchetype != enemy.archetype || currentEnemyName != enemy.enemyName);
            if (stateChanged)
            {
                currentState = state;
                ApplyLayout(state);
                ApplyStateScene(state);
            }

            if (enemyChanged)
            {
                currentEnemyArchetype = enemy.archetype;
                currentEnemyName = enemy.enemyName;
                ApplyEnemyLook(enemy.archetype);
            }

            RefreshPlayerGauge(player);
            RefreshEnemyGauge(enemy);

            if (state == FirstFormGameState.Battle && player != null)
            {
                string skillName = player.HasFirstFormSkill ? player.firstFormSkill.skillName : "무공 미정";
                string traitName = enemy != null && !string.IsNullOrEmpty(enemy.traitName) ? enemy.traitName : "특성 없음";
                SetTmpText(sceneCaptionText, skillName + " · 적 특성 " + traitName);
            }
        }

        /// <summary>
        /// 접이식 보조 정보나 Debug 영역의 표시가 바뀌었을 때 현재 상태의 높이를 다시 계산합니다.
        /// </summary>
        internal void RefreshLayout()
        {
            if (currentState != FirstFormGameState.None)
            {
                ApplyLayout(currentState);
            }
        }

        /// <summary>
        /// 강공 예고를 장면 중앙의 붉은 경고와 맥동 효과로 표시합니다.
        /// </summary>
        internal void SetStrongAttackWarning(bool visible, string attackName)
        {
            strongAttackWarning = visible;
            if (warningRoot != null)
            {
                warningRoot.SetActive(visible);
            }
            if (enemyStrongAura != null)
            {
                enemyStrongAura.gameObject.SetActive(visible && enemyRoot != null && enemyRoot.gameObject.activeInHierarchy);
            }

            if (warningText != null && visible)
            {
                SetTmpText(
                    warningText,
                    "강공 예고  ·  " + (string.IsNullOrEmpty(attackName) ? "기세가 모입니다" : attackName) +
                    "\n대응 선택 또는 자동 대응");
            }
        }

        /// <summary>
        /// 플레이어의 자동 공격을 짧은 전진과 검광으로 표현합니다.
        /// </summary>
        internal void PlayPlayerAttack()
        {
            playerAttackTimer = PlayerAttackDuration;
            enemyHitReactionTimer = HitReactionDuration;
            playerFrameTrack.Play(RuntimeCharacterFrameState.Attack, PlayerAttackDuration);
            enemyFrameTrack.Play(RuntimeCharacterFrameState.Hit, HitReactionDuration);
        }

        /// <summary>
        /// 적 공격을 짧은 전진으로 표현합니다.
        /// </summary>
        internal void PlayEnemyAttack()
        {
            enemyAttackTimer = EnemyAttackDuration;
            float frameDuration = EnemyAttackDuration;
            if (strongAttackWarning)
            {
                enemyStrongAttackTimer = StrongAttackDuration;
                frameDuration = StrongAttackDuration;
            }

            enemyFrameTrack.Play(RuntimeCharacterFrameState.Attack, frameDuration);
        }

        /// <summary>
        /// 실제 피해를 받았을 때 화면에 짧은 붉은 피격 플래시를 표시합니다.
        /// </summary>
        internal void PlayPlayerHit()
        {
            hitFlashTimer = 0.18f;
            playerHitReactionTimer = HitReactionDuration;
            playerFrameTrack.Play(RuntimeCharacterFrameState.Hit, HitReactionDuration);
        }

        private void Update()
        {
            RefreshLayoutForSafeArea();

            float delta = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            playerAttackTimer = Mathf.Max(0f, playerAttackTimer - delta);
            enemyAttackTimer = Mathf.Max(0f, enemyAttackTimer - delta);
            enemyStrongAttackTimer = Mathf.Max(0f, enemyStrongAttackTimer - delta);
            playerHitReactionTimer = Mathf.Max(0f, playerHitReactionTimer - delta);
            enemyHitReactionTimer = Mathf.Max(0f, enemyHitReactionTimer - delta);
            hitFlashTimer = Mathf.Max(0f, hitFlashTimer - delta);

            playerFrameTrack.Update(delta, currentState != FirstFormGameState.Death && currentState != FirstFormGameState.BodySelection);
            enemyFrameTrack.Update(delta, currentState == FirstFormGameState.Battle);

            float playerAttackProgress = 1f - playerAttackTimer / PlayerAttackDuration;
            float enemyAttackProgress = 1f - enemyAttackTimer / EnemyAttackDuration;
            float playerLunge = playerAttackTimer > 0f ? Mathf.Sin(playerAttackProgress * Mathf.PI) * 96f : 0f;
            float enemyLungeDistance = enemyStrongAttackTimer > 0f ? 104f : 72f;
            float enemyLunge = enemyAttackTimer > 0f ? Mathf.Sin(enemyAttackProgress * Mathf.PI) * enemyLungeDistance : 0f;
            float playerRecoil = GetHitRecoil(playerHitReactionTimer, -24f);
            float enemyRecoil = GetHitRecoil(enemyHitReactionTimer, 28f);
            float bob = Mathf.Sin(time * 2.1f) * 3f;

            if (playerRoot != null)
            {
                Vector2 deathDrift = currentState == FirstFormGameState.Death ? new Vector2(0f, Mathf.Sin(time * 1.2f) * 10f + 18f) : Vector2.zero;
                playerRoot.anchoredPosition = playerBasePosition + new Vector2(playerLunge + playerRecoil, bob) + deathDrift;
                if (playerSword != null)
                {
                    playerSword.localEulerAngles = new Vector3(0f, 0f, playerAttackTimer > 0f ? -34f : -15f + Mathf.Sin(time * 1.4f) * 2f);
                }
            }

            if (enemyRoot != null)
            {
                enemyRoot.anchoredPosition = enemyBasePosition + new Vector2(-enemyLunge + enemyRecoil, -bob * 0.7f);
            }

            UpdateCharacterAnimation(time, playerAttackProgress, enemyAttackProgress);

            if (slashEffect != null)
            {
                float slashAlpha = playerAttackTimer > 0f ? Mathf.Sin(Mathf.Clamp01(playerAttackProgress) * Mathf.PI) : 0f;
                Color color = slashEffect.color;
                color.a = slashAlpha * 0.92f;
                slashEffect.color = color;
                slashEffect.rectTransform.anchoredPosition = new Vector2(-40f + playerLunge * 1.9f, 12f);
                slashEffect.rectTransform.localScale = Vector3.one * (0.88f + slashAlpha * 0.24f);
            }

            if (enemySlashEffect != null)
            {
                float strongProgress = 1f - enemyStrongAttackTimer / StrongAttackDuration;
                float strongAlpha = enemyStrongAttackTimer > 0f ? Mathf.Sin(Mathf.Clamp01(strongProgress) * Mathf.PI) : 0f;
                Color color = enemySlashEffect.color;
                color.a = strongAlpha * 0.90f;
                enemySlashEffect.color = color;
                enemySlashEffect.rectTransform.anchoredPosition = new Vector2(54f - enemyLunge * 1.6f, 18f);
                enemySlashEffect.rectTransform.localScale = Vector3.one * (0.92f + strongAlpha * 0.30f);
            }

            if (flashOverlay != null)
            {
                Color color = flashOverlay.color;
                color.a = hitFlashTimer > 0f ? hitFlashTimer / 0.18f * 0.22f : 0f;
                flashOverlay.color = color;
            }

            if (warningCanvas != null && warningRoot.activeSelf)
            {
                warningCanvas.alpha = 0.78f + Mathf.Sin(time * 8f) * 0.18f;
            }

            if (aura != null && aura.gameObject.activeSelf)
            {
                float pulse = 0.96f + Mathf.Sin(time * 2.8f) * 0.06f;
                aura.rectTransform.localScale = Vector3.one * pulse;
                Color color = aura.color;
                color.a = 0.40f + Mathf.Sin(time * 2.8f) * 0.12f;
                aura.color = color;
            }

            if (mistOne != null)
            {
                mistOne.rectTransform.anchoredPosition = new Vector2(Mathf.Repeat(time * 7f, 120f) - 60f, 0f);
                mistTwo.rectTransform.anchoredPosition = new Vector2(60f - Mathf.Repeat(time * 5f, 120f), 0f);
            }

            if (candidateCanvases != null)
            {
                for (int i = 0; i < candidateCanvases.Length; i++)
                {
                    candidateCanvases[i].alpha = 0.58f + Mathf.Sin(time * 1.7f + i * 1.4f) * 0.12f;
                }
            }
        }

        /// <summary>
        /// 실제 스프라이트와 기존 실루엣 모두에 공통으로 적용되는 대기, 공격, 피격 움직임을 갱신합니다.
        /// </summary>
        private void UpdateCharacterAnimation(float time, float playerAttackProgress, float enemyAttackProgress)
        {
            float playerBreath = 1f + Mathf.Sin(time * 2.15f) * 0.012f;
            float enemyBreath = 1f + Mathf.Sin(time * 1.85f + 0.7f) * 0.014f;
            float playerAttackScale = playerAttackTimer > 0f ? Mathf.Sin(Mathf.Clamp01(playerAttackProgress) * Mathf.PI) * 0.055f : 0f;
            float enemyAttackScale = enemyAttackTimer > 0f ? Mathf.Sin(Mathf.Clamp01(enemyAttackProgress) * Mathf.PI) * 0.06f : 0f;
            float deathTilt = currentState == FirstFormGameState.Death ? -9f : 0f;

            if (playerVisualRoot != null)
            {
                float hitShake = playerHitReactionTimer > 0f ? Mathf.Sin(time * 58f) * 7f : 0f;
                playerVisualRoot.anchoredPosition = new Vector2(hitShake, 0f);
                playerVisualRoot.localScale = new Vector3(playerBreath + playerAttackScale, 1f / playerBreath + playerAttackScale * 0.35f, 1f);
                playerVisualRoot.localEulerAngles = new Vector3(0f, 0f, deathTilt - playerAttackScale * 85f);
            }

            if (enemyVisualRoot != null)
            {
                float hitShake = enemyHitReactionTimer > 0f ? Mathf.Sin(time * 64f) * 8f : 0f;
                float chargePulse = strongAttackWarning ? 1f + Mathf.Sin(time * 8f) * 0.035f : 1f;
                enemyVisualRoot.anchoredPosition = new Vector2(hitShake, 0f);
                enemyVisualRoot.localScale = new Vector3((enemyBreath + enemyAttackScale) * chargePulse, (1f / enemyBreath + enemyAttackScale * 0.25f) * chargePulse, 1f);
                enemyVisualRoot.localEulerAngles = new Vector3(0f, 0f, enemyAttackScale * 70f + (strongAttackWarning ? Mathf.Sin(time * 8f) * 1.5f : 0f));
            }

            if (playerArtwork != null)
            {
                playerArtwork.color = playerHitReactionTimer > 0f ? new Color(1f, 0.66f, 0.62f, 1f) : Color.white;
            }

            if (enemyArtwork != null)
            {
                if (enemyHitReactionTimer > 0f)
                {
                    enemyArtwork.color = new Color(1f, 0.68f, 0.60f, 1f);
                }
                else if (strongAttackWarning)
                {
                    enemyArtwork.color = new Color(1f, 0.86f, 0.68f, 1f);
                }
                else
                {
                    enemyArtwork.color = Color.white;
                }
            }

            UpdateImpactBurst(playerHitBurst, playerHitReactionTimer, new Color(1f, 0.50f, 0.38f, 0.88f));
            UpdateImpactBurst(enemyHitBurst, enemyHitReactionTimer, new Color(0.72f, 0.94f, 1f, 0.90f));

            if (enemyStrongAura != null)
            {
                bool showAura = strongAttackWarning && enemyRoot != null && enemyRoot.gameObject.activeInHierarchy;
                enemyStrongAura.gameObject.SetActive(showAura);
                if (showAura)
                {
                    float pulse = 1f + Mathf.Sin(time * 8f) * 0.10f;
                    enemyStrongAura.rectTransform.localScale = Vector3.one * pulse;
                    enemyStrongAura.color = new Color(1f, 0.37f, 0.20f, 0.34f + Mathf.Sin(time * 8f) * 0.12f);
                }
            }
        }

        /// <summary>
        /// 피격 순간 캐릭터 주변에 번지는 짧은 원형 충격 효과를 갱신합니다.
        /// </summary>
        private static void UpdateImpactBurst(Image burst, float timer, Color color)
        {
            if (burst == null)
            {
                return;
            }

            bool visible = timer > 0f;
            burst.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            float progress = 1f - Mathf.Clamp01(timer / HitReactionDuration);
            color.a *= Mathf.Sin(progress * Mathf.PI);
            burst.color = color;
            burst.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.62f, 1.32f, progress);
        }

        /// <summary>
        /// 피격 방향으로 짧게 밀렸다가 원래 자리로 돌아오는 변위를 계산합니다.
        /// </summary>
        private static float GetHitRecoil(float timer, float distance)
        {
            if (timer <= 0f)
            {
                return 0f;
            }

            float progress = 1f - Mathf.Clamp01(timer / HitReactionDuration);
            return Mathf.Sin(progress * Mathf.PI) * distance;
        }

        /// <summary>
        /// 상태별로 장면 스테이지와 기존 정보 카드의 높이를 재배분합니다.
        /// </summary>
        private void ApplyLayout(FirstFormGameState state)
        {
            float statusHeight = 100f;
            float stageHeight = 860f;
            float centerHeight = 300f;
            float auxiliaryHeight = IsLayoutActive(auxiliaryBarLayout)
                ? (debugButtonGroup != null && debugButtonGroup.activeInHierarchy ? 136f : 64f)
                : 0f;
            float soulGrowthHeight = IsLayoutActive(soulGrowthLayout) ? 130f : 0f;
            float currentLootHeight = IsLayoutActive(currentLootLayout) ? 120f : 0f;
            float logHeight = IsLayoutActive(logPanelLayout) ? 110f : 0f;
            float buttonHeight = 150f;
            bool showDetailedStatus = state == FirstFormGameState.Training || state == FirstFormGameState.Exploration;

            switch (state)
            {
                case FirstFormGameState.FirstFormSelection:
                    stageHeight = 260f;
                    centerHeight = 650f;
                    break;
                case FirstFormGameState.Training:
                    statusHeight = 160f;
                    stageHeight = 1080f;
                    centerHeight = 210f;
                    break;
                case FirstFormGameState.Exploration:
                    statusHeight = 160f;
                    stageHeight = 1140f;
                    centerHeight = 180f;
                    buttonHeight = 0f;
                    break;
                case FirstFormGameState.ExplorationEvent:
                    stageHeight = 760f;
                    centerHeight = 620f;
                    break;
                case FirstFormGameState.Battle:
                    statusHeight = 74f;
                    stageHeight = 1060f;
                    centerHeight = 220f;
                    buttonHeight = 190f;
                    break;
                case FirstFormGameState.BattleVictory:
                    stageHeight = 880f;
                    centerHeight = 450f;
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    stageHeight = 860f;
                    centerHeight = 580f;
                    break;
                case FirstFormGameState.Death:
                    stageHeight = 1060f;
                    centerHeight = 300f;
                    break;
                case FirstFormGameState.BodySelection:
                    stageHeight = 720f;
                    centerHeight = 640f;
                    break;
            }

            FitHeightsToSafeArea(
                state,
                ref statusHeight,
                ref stageHeight,
                ref centerHeight,
                ref auxiliaryHeight,
                ref soulGrowthHeight,
                ref currentLootHeight,
                ref logHeight,
                ref buttonHeight);

            SetLayoutHeight(statusLayout, statusHeight);
            SetLayoutHeight(stageLayout, stageHeight);
            SetLayoutHeight(centerLayout, centerHeight);
            SetLayoutHeight(auxiliaryBarLayout, auxiliaryHeight);
            SetLayoutHeight(soulGrowthLayout, soulGrowthHeight);
            SetLayoutHeight(currentLootLayout, currentLootHeight);
            SetLayoutHeight(logPanelLayout, logHeight);
            SetLayoutHeight(buttonPanelLayout, buttonHeight);
            SetLayoutHeight(soulGrowthInfoLayout, 100f);
            SetLayoutHeight(soulGrowthTextLayout, 82f);
            SetLayoutHeight(currentLootTextLayout, 96f);
            SetLayoutHeight(logContentLayout, Mathf.Max(0f, logHeight - 24f));
            SetObjectActive(statusTitleObject, state != FirstFormGameState.Battle);
            SetObjectActive(statusGridObject, showDetailedStatus);
            SetObjectActive(soulGrowthButtonGrid, IsLayoutActive(soulGrowthLayout));
            SetObjectActive(logTitleObject, false);
            if (statePanelLayouts == null)
            {
                return;
            }

            float contentHeight = Mathf.Max(160f, centerHeight - 40f);
            for (int i = 0; i < statePanelLayouts.Length; i++)
            {
                SetLayoutHeight(statePanelLayouts[i], contentHeight);
            }

            ForceRootLayoutRebuild();
            if (delayedLayoutRebuild != null)
            {
                StopCoroutine(delayedLayoutRebuild);
            }
            delayedLayoutRebuild = StartCoroutine(ForceRootLayoutNextFrame());
        }

        /// <summary>
        /// Safe Area 또는 Game View 높이가 바뀌면 현재 상태의 높이 배분을 다시 계산합니다.
        /// </summary>
        private void RefreshLayoutForSafeArea()
        {
            if (currentState == FirstFormGameState.None || layoutRootRect == null)
            {
                return;
            }

            float currentHeight = layoutRootRect.rect.height;
            if (currentHeight > 1f && Mathf.Abs(currentHeight - lastAppliedRootHeight) >= 0.5f)
            {
                ApplyLayout(currentState);
            }
        }

        /// <summary>
        /// 안전 영역이 기준 높이보다 짧을 때 장면과 로그를 먼저 줄이고 버튼 높이는 보존합니다.
        /// </summary>
        private void FitHeightsToSafeArea(
            FirstFormGameState state,
            ref float statusHeight,
            ref float stageHeight,
            ref float centerHeight,
            ref float auxiliaryHeight,
            ref float soulGrowthHeight,
            ref float currentLootHeight,
            ref float logHeight,
            ref float buttonHeight)
        {
            if (layoutRootRect == null)
            {
                return;
            }

            float rootHeight = layoutRootRect.rect.height;
            lastAppliedRootHeight = rootHeight;
            if (rootHeight <= 1f)
            {
                return;
            }

            bool useCompactSpacing = rootHeight < 1800f;
            ConfigureRootSpacing(useCompactSpacing ? 8 : 18, useCompactSpacing ? 6f : 14f);

            int panelCount = CountVisibleSections(
                statusHeight,
                stageHeight,
                centerHeight,
                auxiliaryHeight,
                soulGrowthHeight,
                currentLootHeight,
                logHeight,
                buttonHeight);
            float layoutPadding = layoutRootGroup != null ? layoutRootGroup.padding.vertical : 0f;
            float layoutSpacing = layoutRootGroup != null ? layoutRootGroup.spacing * Mathf.Max(0, panelCount - 1) : 0f;
            float availableHeight = Mathf.Max(0f, rootHeight - layoutPadding - layoutSpacing);
            float totalHeight = statusHeight + stageHeight + centerHeight + auxiliaryHeight + soulGrowthHeight + currentLootHeight + logHeight + buttonHeight;
            float overflow = Mathf.Max(0f, totalHeight - availableHeight);
            if (overflow <= 0f)
            {
                return;
            }

            ReduceHeight(ref soulGrowthHeight, soulGrowthHeight > 0f ? 112f : 0f, ref overflow);
            ReduceHeight(ref currentLootHeight, currentLootHeight > 0f ? 100f : 0f, ref overflow);
            ReduceHeight(ref logHeight, logHeight > 0f ? 88f : 0f, ref overflow);
            ReduceHeight(ref centerHeight, GetMinimumCenterHeight(state, centerHeight), ref overflow);

            float minimumButtonHeight = state == FirstFormGameState.Battle ? 174f : 144f;
            ReduceHeight(ref buttonHeight, buttonHeight > 0f ? minimumButtonHeight : 0f, ref overflow);
            ReduceHeight(ref stageHeight, GetMinimumStageHeight(state, stageHeight, rootHeight), ref overflow);
        }

        private static int CountVisibleSections(params float[] heights)
        {
            int count = 0;
            if (heights == null)
            {
                return count;
            }

            for (int i = 0; i < heights.Length; i++)
            {
                if (heights[i] > 1f)
                {
                    count++;
                }
            }

            return count;
        }

        private void ConfigureRootSpacing(int padding, float spacing)
        {
            if (layoutRootGroup == null)
            {
                return;
            }

            if (layoutRootGroup.padding.top != padding || layoutRootGroup.padding.bottom != padding ||
                layoutRootGroup.padding.left != padding || layoutRootGroup.padding.right != padding)
            {
                layoutRootGroup.padding = new RectOffset(padding, padding, padding, padding);
            }

            if (!Mathf.Approximately(layoutRootGroup.spacing, spacing))
            {
                layoutRootGroup.spacing = spacing;
            }
        }

        private static float GetMinimumStageHeight(FirstFormGameState state, float currentHeight, float rootHeight)
        {
            float minimum;
            switch (state)
            {
                case FirstFormGameState.Battle:
                case FirstFormGameState.Training:
                case FirstFormGameState.Exploration:
                    minimum = rootHeight * 0.52f;
                    break;
                case FirstFormGameState.FirstFormSelection:
                    minimum = 220f;
                    break;
                case FirstFormGameState.ExplorationEvent:
                    minimum = 640f;
                    break;
                case FirstFormGameState.BattleVictory:
                    minimum = 720f;
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    minimum = 700f;
                    break;
                case FirstFormGameState.Death:
                    minimum = 760f;
                    break;
                case FirstFormGameState.BodySelection:
                    minimum = 600f;
                    break;
                default:
                    minimum = 600f;
                    break;
            }

            return Mathf.Min(currentHeight, minimum);
        }

        private static float GetMinimumCenterHeight(FirstFormGameState state, float currentHeight)
        {
            float minimum;
            switch (state)
            {
                case FirstFormGameState.Battle:
                    minimum = 200f;
                    break;
                case FirstFormGameState.Training:
                    minimum = 190f;
                    break;
                case FirstFormGameState.Exploration:
                    minimum = 150f;
                    break;
                case FirstFormGameState.FirstFormSelection:
                    minimum = 560f;
                    break;
                case FirstFormGameState.ExplorationEvent:
                    minimum = 560f;
                    break;
                case FirstFormGameState.BattleVictory:
                    minimum = 410f;
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    minimum = 540f;
                    break;
                case FirstFormGameState.Death:
                    minimum = 260f;
                    break;
                case FirstFormGameState.BodySelection:
                    minimum = 560f;
                    break;
                default:
                    minimum = 330f;
                    break;
            }

            return Mathf.Min(currentHeight, minimum);
        }

        private static void ReduceHeight(ref float height, float minimumHeight, ref float remainingOverflow)
        {
            if (remainingOverflow <= 0f)
            {
                return;
            }

            float reduction = Mathf.Min(Mathf.Max(0f, height - minimumHeight), remainingOverflow);
            height -= reduction;
            remainingOverflow -= reduction;
        }

        private IEnumerator ForceRootLayoutNextFrame()
        {
            yield return null;
            ForceRootLayoutRebuild();
            delayedLayoutRebuild = null;
        }

        private void ForceRootLayoutRebuild()
        {
            Canvas.ForceUpdateCanvases();
            RectTransform root = transform.parent as RectTransform;
            if (root != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// 상태마다 밝은 동양풍 팔레트, 소품, 실루엣 배치를 전환합니다.
        /// </summary>
        private void ApplyStateScene(FirstFormGameState state)
        {
            strongAttackWarning = false;
            warningRoot.SetActive(false);
            if (enemyStrongAura != null)
            {
                enemyStrongAura.gameObject.SetActive(false);
            }
            trainingProps.SetActive(false);
            explorationProps.SetActive(false);
            selectionScrolls.SetActive(false);
            bodyCandidates.SetActive(false);
            aura.gameObject.SetActive(false);
            enemyRoot.gameObject.SetActive(false);
            playerRoot.gameObject.SetActive(true);
            playerGaugeRoot.SetActive(true);
            enemyGaugeRoot.SetActive(false);
            playerCanvas.alpha = 1f;
            playerRoot.localScale = Vector3.one;
            enemyRoot.localScale = Vector3.one;
            bodyCandidates.transform.localScale = Vector3.one;
            if (playerVisualRoot != null)
            {
                playerVisualRoot.anchoredPosition = Vector2.zero;
                playerVisualRoot.localScale = Vector3.one;
                playerVisualRoot.localEulerAngles = Vector3.zero;
            }
            if (enemyVisualRoot != null)
            {
                enemyVisualRoot.anchoredPosition = Vector2.zero;
                enemyVisualRoot.localScale = Vector3.one;
                enemyVisualRoot.localEulerAngles = Vector3.zero;
            }
            playerBasePosition = new Vector2(0f, -28f);
            enemyBasePosition = new Vector2(250f, -28f);

            // 마지막 일격의 공격 프레임은 승리 화면으로 넘어가도 끝까지 보여줍니다.
            if (state != FirstFormGameState.BattleVictory)
            {
                playerFrameTrack.SetIdle();
            }
            if (state == FirstFormGameState.Battle)
            {
                enemyFrameTrack.SetIdle();
            }
            else if (state == FirstFormGameState.BattleVictory)
            {
                enemyFrameTrack.Play(RuntimeCharacterFrameState.Hit, HitReactionDuration, true);
            }
            else if (state == FirstFormGameState.Death)
            {
                playerFrameTrack.Play(RuntimeCharacterFrameState.Hit, HitReactionDuration, true);
            }

            switch (state)
            {
                case FirstFormGameState.FirstFormSelection:
                    SetPalette(new Color(0.78f, 0.87f, 0.84f), new Color(0.45f, 0.61f, 0.56f), new Color(0.26f, 0.42f, 0.38f), new Color(0.45f, 0.57f, 0.50f));
                    SetSceneText("입문 무공", "비어 있는 마음에 첫 수를 새깁니다");
                    selectionScrolls.SetActive(true);
                    playerBasePosition = new Vector2(0f, -56f);
                    playerGaugeRoot.SetActive(false);
                    break;
                case FirstFormGameState.Training:
                    SetPalette(new Color(0.68f, 0.83f, 0.87f), new Color(0.40f, 0.62f, 0.61f), new Color(0.20f, 0.42f, 0.40f), new Color(0.46f, 0.58f, 0.52f));
                    SetSceneText("청풍문 연무장", "고요한 호흡 사이로 검끝이 맑아집니다");
                    trainingProps.SetActive(true);
                    playerRoot.localScale = Vector3.one * 1.45f;
                    playerBasePosition = new Vector2(50f, -32f);
                    break;
                case FirstFormGameState.Exploration:
                case FirstFormGameState.ExplorationEvent:
                    SetPalette(new Color(0.72f, 0.84f, 0.76f), new Color(0.39f, 0.58f, 0.45f), new Color(0.18f, 0.38f, 0.30f), new Color(0.42f, 0.53f, 0.39f));
                    SetSceneText(state == FirstFormGameState.Exploration ? "산길 출행" : "강호의 갈림길", state == FirstFormGameState.Exploration ? "안개 너머의 기척을 따라 걷습니다" : "낡은 흔적 앞에서 발걸음을 고릅니다");
                    explorationProps.SetActive(true);
                    playerRoot.localScale = Vector3.one * 1.25f;
                    playerBasePosition = new Vector2(-120f, -32f);
                    break;
                case FirstFormGameState.Battle:
                    SetPalette(new Color(0.58f, 0.72f, 0.75f), new Color(0.34f, 0.49f, 0.50f), new Color(0.15f, 0.29f, 0.31f), new Color(0.28f, 0.36f, 0.35f));
                    SetSceneText("산중 대치", "자동 공방 · 강공은 선택 개입");
                    enemyRoot.gameObject.SetActive(true);
                    enemyGaugeRoot.SetActive(true);
                    playerRoot.localScale = Vector3.one * 1.15f;
                    enemyRoot.localScale = Vector3.one * 1.12f;
                    playerBasePosition = new Vector2(0f, -42f);
                    enemyBasePosition = new Vector2(0f, -42f);
                    break;
                case FirstFormGameState.BattleVictory:
                    SetPalette(new Color(0.76f, 0.85f, 0.72f), new Color(0.47f, 0.61f, 0.43f), new Color(0.22f, 0.39f, 0.29f), new Color(0.43f, 0.51f, 0.38f));
                    SetSceneText("승리의 숨", "검을 거두고 다음 길을 정합니다");
                    enemyRoot.gameObject.SetActive(true);
                    enemyCanvas.alpha = 0.28f;
                    enemyRoot.localEulerAngles = new Vector3(0f, 0f, -12f);
                    playerRoot.localScale = Vector3.one * 1.15f;
                    enemyRoot.localScale = Vector3.one * 1.08f;
                    playerBasePosition = new Vector2(0f, -32f);
                    enemyBasePosition = new Vector2(0f, -72f);
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    SetPalette(new Color(0.83f, 0.82f, 0.70f), new Color(0.59f, 0.56f, 0.42f), new Color(0.34f, 0.33f, 0.25f), new Color(0.52f, 0.48f, 0.35f));
                    SetSceneText("경지의 문턱", "숨을 가라앉히고 다음 경지를 바라봅니다");
                    aura.gameObject.SetActive(true);
                    playerRoot.localScale = Vector3.one * 1.35f;
                    playerBasePosition = new Vector2(0f, -64f);
                    playerGaugeRoot.SetActive(false);
                    break;
                case FirstFormGameState.Death:
                    SetPalette(new Color(0.72f, 0.79f, 0.82f), new Color(0.46f, 0.54f, 0.58f), new Color(0.24f, 0.30f, 0.34f), new Color(0.38f, 0.43f, 0.45f));
                    SetSceneText("혼백", "육신은 멎었으나 익힌 감각은 흐려지지 않습니다");
                    playerCanvas.alpha = 0.48f;
                    playerRoot.localScale = Vector3.one * 1.30f;
                    playerBasePosition = new Vector2(0f, 4f);
                    playerGaugeRoot.SetActive(false);
                    sun.color = new Color(0.82f, 0.91f, 1f, 0.72f);
                    break;
                case FirstFormGameState.BodySelection:
                    SetPalette(new Color(0.84f, 0.90f, 0.91f), new Color(0.61f, 0.70f, 0.70f), new Color(0.38f, 0.48f, 0.49f), new Color(0.64f, 0.69f, 0.66f));
                    SetSceneText("새 육신", "세 갈래 인연이 혼백 앞에 모습을 드러냅니다");
                    playerRoot.gameObject.SetActive(false);
                    playerGaugeRoot.SetActive(false);
                    bodyCandidates.transform.localScale = Vector3.one * 1.40f;
                    bodyCandidates.SetActive(true);
                    break;
            }

            if (state != FirstFormGameState.BattleVictory)
            {
                enemyCanvas.alpha = 1f;
                enemyRoot.localEulerAngles = Vector3.zero;
            }
        }

        /// <summary>
        /// 다섯 적의 체형, 색상, 장비 표식을 달리해 실루엣만으로도 구분되게 합니다.
        /// </summary>
        private void ApplyEnemyLook(EnemyArchetype archetype)
        {
            RuntimeCharacterFrameSet frameSet;
            bool useFrameAnimation = RuntimeCharacterArtLibrary.TryGetEnemyFrameSet(archetype, out frameSet);
            Sprite prototypeSprite = null;
            bool usePrototypeSprite = !useFrameAnimation && RuntimeCharacterArtLibrary.TryGetEnemySprite(archetype, out prototypeSprite);
            bool useArtwork = useFrameAnimation || usePrototypeSprite;
            if (enemyArtwork != null)
            {
                enemyArtwork.sprite = useFrameAnimation ? frameSet.idleFrames[0] : prototypeSprite;
                enemyArtwork.rectTransform.sizeDelta = useFrameAnimation ? frameSet.artworkSize : DefaultEnemyArtworkSize;
                enemyArtwork.rectTransform.anchoredPosition = useFrameAnimation ? frameSet.artworkOffset : DefaultEnemyArtworkOffset;
                enemyArtwork.gameObject.SetActive(useArtwork);
            }
            if (enemyPlaceholderRoot != null)
            {
                enemyPlaceholderRoot.gameObject.SetActive(!useArtwork);
            }

            if (useFrameAnimation)
            {
                enemyFrameTrack.Bind(enemyArtwork, frameSet);
            }
            else
            {
                enemyFrameTrack.Unbind();
            }

            enemyShield.SetActive(false);
            enemySecondBlade.SetActive(false);
            enemyRoot.localScale = Vector3.one;
            enemyWeapon.sizeDelta = new Vector2(120f, 12f);
            Color bodyColor = new Color(0.12f, 0.16f, 0.17f, 1f);
            Color accentColor = new Color(0.66f, 0.30f, 0.22f, 1f);

            switch (archetype)
            {
                case EnemyArchetype.SwiftScout:
                    enemyRoot.localScale = new Vector3(0.86f, 0.92f, 1f);
                    accentColor = new Color(0.20f, 0.55f, 0.54f, 1f);
                    enemySecondBlade.SetActive(true);
                    break;
                case EnemyArchetype.IronGuard:
                    enemyRoot.localScale = new Vector3(1.22f, 1.08f, 1f);
                    bodyColor = new Color(0.20f, 0.23f, 0.24f, 1f);
                    accentColor = new Color(0.50f, 0.55f, 0.56f, 1f);
                    enemyShield.SetActive(true);
                    break;
                case EnemyArchetype.EnergySapper:
                    enemyRoot.localScale = new Vector3(0.96f, 1.02f, 1f);
                    accentColor = new Color(0.46f, 0.34f, 0.62f, 1f);
                    enemyWeapon.sizeDelta = new Vector2(90f, 8f);
                    break;
                case EnemyArchetype.Berserker:
                    enemyRoot.localScale = new Vector3(1.28f, 1.14f, 1f);
                    bodyColor = new Color(0.20f, 0.13f, 0.13f, 1f);
                    accentColor = new Color(0.78f, 0.20f, 0.16f, 1f);
                    enemyWeapon.sizeDelta = new Vector2(150f, 18f);
                    break;
                case EnemyArchetype.StrongholdLeader:
                    enemyRoot.localScale = new Vector3(1.16f, 1.12f, 1f);
                    bodyColor = new Color(0.10f, 0.12f, 0.14f, 1f);
                    accentColor = new Color(0.65f, 0.18f, 0.17f, 1f);
                    enemyWeapon.sizeDelta = new Vector2(170f, 16f);
                    break;
            }

            if (currentState == FirstFormGameState.Battle)
            {
                enemyRoot.localScale *= 1.22f;
            }

            enemyBody.color = bodyColor;
            enemyHead.color = bodyColor;
            enemyRobe.color = bodyColor;
            enemyAccent.color = accentColor;
        }

        private void RefreshPlayerGauge(PlayerData player)
        {
            if (player == null)
            {
                SetFill(playerHealthFill, 0f);
                SetFill(playerEnergyFill, 0f);
                return;
            }

            SetFill(playerHealthFill, SafeRatio(player.health, player.maxHealth));
            SetFill(playerEnergyFill, SafeRatio(player.internalEnergy, player.maxInternalEnergy));
            SetTmpText(playerHealthText, "체력  " + player.health + " / " + player.maxHealth);
            SetTmpText(playerEnergyText, "내력  " + player.internalEnergy + " / " + player.maxInternalEnergy);
        }

        private void RefreshEnemyGauge(EnemyData enemy)
        {
            bool visible = currentState == FirstFormGameState.Battle && enemy != null;
            enemyGaugeRoot.SetActive(visible);
            if (!visible)
            {
                return;
            }

            SetFill(enemyHealthFill, SafeRatio(enemy.health, enemy.maxHealth));
            SetTmpText(enemyHealthText, enemy.enemyName + "  " + enemy.health + " / " + enemy.maxHealth);
        }

        private void SetPalette(Color skyColor, Color farColor, Color nearColor, Color groundColor)
        {
            sky.color = skyColor;
            farMountain.color = farColor;
            nearMountain.color = nearColor;
            ground.color = groundColor;
            sun.color = new Color(1f, 0.91f, 0.63f, 0.90f);
        }

        private void SetSceneText(string title, string caption)
        {
            SetTmpText(sceneTitleText, title);
            SetTmpText(sceneCaptionText, caption);
        }

        private void BuildPlayerSilhouette()
        {
            playerRoot = CreateRoot("PlayerSilhouette", transform, new Vector2(0.32f, 0.30f), new Vector2(190f, 250f));
            playerCanvas = playerRoot.gameObject.AddComponent<CanvasGroup>();
            playerVisualRoot = CreateRoot("PlayerVisual", playerRoot, new Vector2(0.5f, 0.5f), playerRoot.sizeDelta);
            playerPlaceholderRoot = CreateRoot("Placeholder", playerVisualRoot, new Vector2(0.5f, 0.5f), playerRoot.sizeDelta);
            Color ink = new Color(0.08f, 0.16f, 0.18f, 1f);
            playerRobe = CreateAnchoredImage("Robe", playerPlaceholderRoot, ink, new Vector2(0.5f, 0.36f), new Vector2(112f, 128f), Vector2.zero, GetTriangleSprite());
            playerRobe.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            playerBody = CreateAnchoredImage("Body", playerPlaceholderRoot, ink, new Vector2(0.5f, 0.47f), new Vector2(76f, 105f), Vector2.zero);
            playerHead = CreateAnchoredImage("Head", playerPlaceholderRoot, ink, new Vector2(0.5f, 0.75f), new Vector2(60f, 60f), Vector2.zero, GetCircleSprite());
            CreateAnchoredImage("HairKnot", playerPlaceholderRoot, ink, new Vector2(0.56f, 0.89f), new Vector2(28f, 28f), Vector2.zero, GetCircleSprite());
            CreateAnchoredImage("Sash", playerPlaceholderRoot, new Color(0.31f, 0.58f, 0.61f, 1f), new Vector2(0.5f, 0.44f), new Vector2(92f, 12f), Vector2.zero);
            CreateAnchoredImage("LeftLeg", playerPlaceholderRoot, ink, new Vector2(0.39f, 0.16f), new Vector2(24f, 75f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, 8f);
            CreateAnchoredImage("RightLeg", playerPlaceholderRoot, ink, new Vector2(0.61f, 0.16f), new Vector2(24f, 75f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
            playerSword = CreateAnchoredImage("Sword", playerPlaceholderRoot, new Color(0.84f, 0.91f, 0.91f, 1f), new Vector2(0.82f, 0.54f), new Vector2(145f, 10f), Vector2.zero).rectTransform;
            playerSword.localEulerAngles = new Vector3(0f, 0f, -15f);

            RuntimeCharacterFrameSet frameSet;
            bool useFrameAnimation = RuntimeCharacterArtLibrary.TryGetPlayerFrameSet(out frameSet);
            Sprite sprite = useFrameAnimation ? frameSet.idleFrames[0] : RuntimeCharacterArtLibrary.GetPlayerSprite();
            Vector2 artworkSize = useFrameAnimation ? frameSet.artworkSize : DefaultPlayerArtworkSize;
            Vector2 artworkOffset = useFrameAnimation ? frameSet.artworkOffset : DefaultPlayerArtworkOffset;
            playerArtwork = CreateAnchoredImage("Artwork", playerVisualRoot, Color.white, new Vector2(0.5f, 0.5f), artworkSize, artworkOffset, sprite);
            playerArtwork.preserveAspect = true;
            playerArtwork.gameObject.SetActive(sprite != null);
            playerPlaceholderRoot.gameObject.SetActive(sprite == null);
            if (useFrameAnimation)
            {
                playerFrameTrack.Bind(playerArtwork, frameSet);
            }

            playerHitBurst = CreateAnchoredImage("HitBurst", playerVisualRoot, new Color(1f, 0.5f, 0.38f, 0f), new Vector2(0.5f, 0.52f), new Vector2(190f, 190f), Vector2.zero, GetRingSprite());
            playerHitBurst.gameObject.SetActive(false);
        }

        private void BuildEnemySilhouette()
        {
            enemyRoot = CreateRoot("EnemySilhouette", transform, new Vector2(0.70f, 0.30f), new Vector2(210f, 270f));
            enemyCanvas = enemyRoot.gameObject.AddComponent<CanvasGroup>();
            enemyVisualRoot = CreateRoot("EnemyVisual", enemyRoot, new Vector2(0.5f, 0.5f), enemyRoot.sizeDelta);
            enemyPlaceholderRoot = CreateRoot("Placeholder", enemyVisualRoot, new Vector2(0.5f, 0.5f), enemyRoot.sizeDelta);
            Color ink = new Color(0.12f, 0.16f, 0.17f, 1f);
            enemyRobe = CreateAnchoredImage("Robe", enemyPlaceholderRoot, ink, new Vector2(0.5f, 0.34f), new Vector2(132f, 144f), Vector2.zero, GetTriangleSprite());
            enemyRobe.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            enemyBody = CreateAnchoredImage("Body", enemyPlaceholderRoot, ink, new Vector2(0.5f, 0.49f), new Vector2(96f, 120f), Vector2.zero);
            enemyHead = CreateAnchoredImage("Head", enemyPlaceholderRoot, ink, new Vector2(0.5f, 0.76f), new Vector2(66f, 66f), Vector2.zero, GetCircleSprite());
            enemyAccent = CreateAnchoredImage("Accent", enemyPlaceholderRoot, new Color(0.66f, 0.30f, 0.22f, 1f), new Vector2(0.5f, 0.45f), new Vector2(112f, 14f), Vector2.zero);
            CreateAnchoredImage("LeftLeg", enemyPlaceholderRoot, ink, new Vector2(0.39f, 0.14f), new Vector2(28f, 82f), Vector2.zero);
            CreateAnchoredImage("RightLeg", enemyPlaceholderRoot, ink, new Vector2(0.61f, 0.14f), new Vector2(28f, 82f), Vector2.zero);
            enemyWeapon = CreateAnchoredImage("Weapon", enemyPlaceholderRoot, new Color(0.17f, 0.19f, 0.19f, 1f), new Vector2(0.18f, 0.55f), new Vector2(120f, 12f), Vector2.zero).rectTransform;
            enemyWeapon.localEulerAngles = new Vector3(0f, 0f, 28f);
            enemyShield = CreateAnchoredImage("Shield", enemyPlaceholderRoot, new Color(0.28f, 0.31f, 0.31f, 1f), new Vector2(0.78f, 0.49f), new Vector2(64f, 90f), Vector2.zero).gameObject;
            enemySecondBlade = CreateAnchoredImage("SecondBlade", enemyPlaceholderRoot, new Color(0.78f, 0.84f, 0.83f, 1f), new Vector2(0.82f, 0.56f), new Vector2(100f, 8f), Vector2.zero).gameObject;
            enemySecondBlade.transform.localEulerAngles = new Vector3(0f, 0f, -30f);

            enemyStrongAura = CreateAnchoredImage("StrongAttackAura", enemyVisualRoot, new Color(1f, 0.37f, 0.20f, 0f), new Vector2(0.5f, 0.5f), new Vector2(270f, 270f), Vector2.zero, GetRingSprite());
            enemyStrongAura.transform.SetAsFirstSibling();
            enemyStrongAura.gameObject.SetActive(false);
            enemyArtwork = CreateAnchoredImage("Artwork", enemyVisualRoot, Color.white, new Vector2(0.5f, 0.5f), new Vector2(340f, 340f), new Vector2(0f, 16f));
            enemyArtwork.preserveAspect = true;
            enemyArtwork.gameObject.SetActive(false);
            enemyHitBurst = CreateAnchoredImage("HitBurst", enemyVisualRoot, new Color(0.72f, 0.94f, 1f, 0f), new Vector2(0.5f, 0.52f), new Vector2(210f, 210f), Vector2.zero, GetRingSprite());
            enemyHitBurst.gameObject.SetActive(false);
        }

        /// <summary>
        /// 한 Image의 프레임 상태를 독립적으로 진행하며, 세트가 없으면 아무 작업도 하지 않습니다.
        /// 공격과 피격 요청이 겹치면 마지막 요청을 우선해 자동 전투의 최신 결과를 보여줍니다.
        /// </summary>
        private sealed class RuntimeFrameTrack
        {
            private const float IdleFrameInterval = 0.25f;
            private static readonly int[] IdlePingPong = { 0, 1, 2, 3, 2, 1 };

            private Image target;
            private RuntimeCharacterFrameSet frameSet;
            private RuntimeCharacterFrameState state = RuntimeCharacterFrameState.Idle;
            private float elapsed;
            private float duration;
            private bool holdAtEnd;

            /// <summary>
            /// 새 캐릭터 프레임 세트를 연결하고 첫 대기 프레임부터 표시합니다.
            /// </summary>
            internal void Bind(Image image, RuntimeCharacterFrameSet frames)
            {
                target = image;
                frameSet = frames;
                SetIdle();
            }

            /// <summary>
            /// 단일 이미지 또는 도형 fallback으로 전환할 때 프레임 재생을 해제합니다.
            /// </summary>
            internal void Unbind()
            {
                target = null;
                frameSet = null;
                elapsed = 0f;
                duration = 0f;
                holdAtEnd = false;
            }

            /// <summary>
            /// 대기 루프로 돌아가 프레임 시간을 초기화합니다.
            /// </summary>
            internal void SetIdle()
            {
                state = RuntimeCharacterFrameState.Idle;
                elapsed = 0f;
                duration = 0f;
                holdAtEnd = false;
                ApplyFirstFrame();
            }

            /// <summary>
            /// 공격 또는 피격 프레임을 한 번 재생하고 필요하면 마지막 프레임을 유지합니다.
            /// </summary>
            internal void Play(RuntimeCharacterFrameState nextState, float playDuration, bool holdLastFrame = false)
            {
                if (target == null || frameSet == null)
                {
                    return;
                }

                state = nextState;
                elapsed = 0f;
                duration = Mathf.Max(0.01f, playDuration);
                holdAtEnd = holdLastFrame;
                ApplyFirstFrame();
            }

            /// <summary>
            /// 현재 프레임을 갱신하고 단발 재생이 끝나면 대기 상태로 복귀합니다.
            /// </summary>
            internal void Update(float deltaTime, bool allowIdleLoop)
            {
                if (target == null || frameSet == null)
                {
                    return;
                }

                Sprite[] frames = frameSet.GetFrames(state);
                if (frames == null || frames.Length == 0)
                {
                    return;
                }

                if (state == RuntimeCharacterFrameState.Idle)
                {
                    if (allowIdleLoop)
                    {
                        elapsed += Mathf.Max(0f, deltaTime);
                    }

                    int sequenceIndex = Mathf.FloorToInt(elapsed / IdleFrameInterval) % IdlePingPong.Length;
                    int frameIndex = Mathf.Min(IdlePingPong[sequenceIndex], frames.Length - 1);
                    ApplyFrame(frames[frameIndex]);
                    return;
                }

                elapsed += Mathf.Max(0f, deltaTime);
                float progress = Mathf.Clamp01(elapsed / duration);
                int oneShotIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length));
                ApplyFrame(frames[oneShotIndex]);

                if (elapsed < duration)
                {
                    return;
                }

                if (holdAtEnd || !allowIdleLoop)
                {
                    ApplyFrame(frames[frames.Length - 1]);
                    return;
                }

                SetIdle();
            }

            private void ApplyFirstFrame()
            {
                if (target == null || frameSet == null)
                {
                    return;
                }

                Sprite[] frames = frameSet.GetFrames(state);
                if (frames != null && frames.Length > 0)
                {
                    ApplyFrame(frames[0]);
                }
            }

            private void ApplyFrame(Sprite frame)
            {
                if (target != null && frame != null && target.sprite != frame)
                {
                    target.sprite = frame;
                }
            }
        }

        private void BuildTrainingProps()
        {
            trainingProps = new GameObject("TrainingProps", typeof(RectTransform));
            trainingProps.transform.SetParent(transform, false);
            StretchToParent(trainingProps.GetComponent<RectTransform>());
            RectTransform post = CreateRoot("WoodenPost", trainingProps.transform, new Vector2(0.22f, 0.26f), new Vector2(150f, 230f));
            Color wood = new Color(0.25f, 0.20f, 0.15f, 1f);
            CreateAnchoredImage("Post", post, wood, new Vector2(0.5f, 0.48f), new Vector2(34f, 210f), Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                CreateAnchoredImage("Arm" + i, post, wood, new Vector2(0.64f, 0.68f - i * 0.20f), new Vector2(105f, 16f), Vector2.zero);
            }

            RectTransform roof = CreateRoot("HallRoof", trainingProps.transform, new Vector2(0.80f, 0.26f), new Vector2(260f, 150f));
            CreateAnchoredImage("Roof", roof, new Color(0.13f, 0.28f, 0.29f, 0.88f), new Vector2(0.5f, 0.72f), new Vector2(250f, 42f), Vector2.zero);
            CreateAnchoredImage("Hall", roof, new Color(0.33f, 0.39f, 0.35f, 0.92f), new Vector2(0.5f, 0.32f), new Vector2(210f, 92f), Vector2.zero);
        }

        private void BuildExplorationProps()
        {
            explorationProps = new GameObject("ExplorationProps", typeof(RectTransform));
            explorationProps.transform.SetParent(transform, false);
            StretchToParent(explorationProps.GetComponent<RectTransform>());
            RectTransform path = CreateRoot("Path", explorationProps.transform, new Vector2(0.53f, 0.13f), new Vector2(360f, 160f));
            Image pathImage = CreateAnchoredImage("PathShape", path, new Color(0.68f, 0.67f, 0.56f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(340f, 150f), Vector2.zero, GetTriangleSprite());
            pathImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            RectTransform stone = CreateRoot("WayStone", explorationProps.transform, new Vector2(0.79f, 0.25f), new Vector2(90f, 150f));
            CreateAnchoredImage("Stone", stone, new Color(0.36f, 0.43f, 0.40f, 1f), new Vector2(0.5f, 0.44f), new Vector2(74f, 132f), Vector2.zero);
            CreateAnchoredImage("StoneCap", stone, new Color(0.28f, 0.35f, 0.33f, 1f), new Vector2(0.5f, 0.89f), new Vector2(88f, 18f), Vector2.zero);
        }

        private void BuildSelectionScrolls()
        {
            selectionScrolls = new GameObject("SelectionScrolls", typeof(RectTransform));
            selectionScrolls.transform.SetParent(transform, false);
            StretchToParent(selectionScrolls.GetComponent<RectTransform>());
            for (int i = 0; i < 3; i++)
            {
                float x = 0.30f + i * 0.20f;
                CreateAnchoredImage("Scroll" + i, selectionScrolls.transform, new Color(0.91f, 0.89f, 0.77f, 0.82f), new Vector2(x, 0.48f), new Vector2(92f, 126f), Vector2.zero);
            }
        }

        private void BuildBodyCandidates()
        {
            bodyCandidates = new GameObject("BodyCandidates", typeof(RectTransform));
            bodyCandidates.transform.SetParent(transform, false);
            StretchToParent(bodyCandidates.GetComponent<RectTransform>());
            candidateCanvases = new CanvasGroup[3];
            Color[] colors =
            {
                new Color(0.25f, 0.50f, 0.64f, 0.75f),
                new Color(0.48f, 0.34f, 0.54f, 0.75f),
                new Color(0.30f, 0.58f, 0.45f, 0.75f)
            };

            for (int i = 0; i < 3; i++)
            {
                RectTransform root = CreateRoot("Candidate" + i, bodyCandidates.transform, new Vector2(0.30f + i * 0.20f, 0.36f), new Vector2(120f, 190f));
                candidateCanvases[i] = root.gameObject.AddComponent<CanvasGroup>();
                CreateAnchoredImage("Aura", root, colors[i], new Vector2(0.5f, 0.48f), new Vector2(150f, 180f), Vector2.zero, GetCircleSprite());
                CreateAnchoredImage("Body", root, new Color(0.14f, 0.23f, 0.25f, 0.86f), new Vector2(0.5f, 0.38f), new Vector2(70f, 110f), Vector2.zero, GetTriangleSprite()).rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
                CreateAnchoredImage("Head", root, new Color(0.14f, 0.23f, 0.25f, 0.86f), new Vector2(0.5f, 0.72f), new Vector2(52f, 52f), Vector2.zero, GetCircleSprite());
            }
        }

        private void BuildSceneLabels(TMP_FontAsset font)
        {
            sceneTitleText = CreateText("SceneTitle", transform, font, 30f, FontStyles.Bold, new Color(0.08f, 0.16f, 0.18f, 1f), TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.89f), new Vector2(0.47f, 0.99f));
            sceneCaptionText = CreateText("SceneCaption", transform, font, 24f, FontStyles.Normal, new Color(0.14f, 0.24f, 0.25f, 0.92f), TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.80f), new Vector2(0.47f, 0.90f));
        }

        private void BuildGauges(TMP_FontAsset font)
        {
            playerGaugeRoot = CreateGaugePanel("PlayerGauge", transform, new Vector2(0.03f, 0.035f), new Vector2(0.45f, 0.18f));
            playerHealthText = CreateText("HealthLabel", playerGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.96f));
            playerHealthFill = CreateGauge("HealthBar", playerGaugeRoot.transform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.63f), new Color(0.82f, 0.25f, 0.22f, 1f));
            playerEnergyText = CreateText("EnergyLabel", playerGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.50f));
            playerEnergyFill = CreateGauge("EnergyBar", playerGaugeRoot.transform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.17f), new Color(0.22f, 0.55f, 0.78f, 1f));

            enemyGaugeRoot = CreateGaugePanel("EnemyGauge", transform, new Vector2(0.52f, 0.80f), new Vector2(0.97f, 0.94f));
            enemyHealthText = CreateText("EnemyLabel", enemyGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.95f));
            enemyHealthFill = CreateGauge("EnemyHealthBar", enemyGaugeRoot.transform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.34f), new Color(0.88f, 0.30f, 0.21f, 1f));
        }

        private void BuildWarning(TMP_FontAsset font)
        {
            warningRoot = CreateGaugePanel("StrongAttackWarning", transform, new Vector2(0.17f, 0.42f), new Vector2(0.83f, 0.62f));
            Image image = warningRoot.GetComponent<Image>();
            image.color = new Color(0.28f, 0.055f, 0.045f, 0.94f);
            warningCanvas = warningRoot.AddComponent<CanvasGroup>();
            warningText = CreateText("WarningText", warningRoot.transform, font, 32f, FontStyles.Bold, new Color(1f, 0.86f, 0.67f, 1f), TextAlignmentOptions.Center, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
            warningRoot.SetActive(false);
        }

        private static GameObject CreateGaugePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject panel = CreateObject(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.035f, 0.075f, 0.085f, 0.92f);
            image.raycastTarget = false;
            return panel;
        }

        private static Image CreateGauge(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color fillColor)
        {
            Image background = CreateStretchImage(name + "Background", parent, new Color(0.03f, 0.05f, 0.055f, 0.92f), anchorMin, anchorMax);
            Image fill = CreateStretchImage(name + "Fill", background.transform, fillColor, Vector2.zero, Vector2.one);
            return fill;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, FontStyles style, Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = CreateObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.ForceMeshUpdate(false, true);
            return text;
        }

        private static RectTransform CreateRoot(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            GameObject root = CreateObject(name, parent);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static Image CreateAnchoredImage(string name, Transform parent, Color color, Vector2 anchor, Vector2 size, Vector2 position, Sprite sprite = null)
        {
            GameObject imageObject = CreateObject(name, parent);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateStretchImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite = null)
        {
            GameObject imageObject = CreateObject(name, parent);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetTmpText(TMP_Text target, string value)
        {
            if (target == null || target.text == value)
            {
                return;
            }

            target.text = value;
            target.ForceMeshUpdate(false, true);
        }

        private static void SetLayoutHeight(LayoutElement layout, float height)
        {
            if (layout == null)
            {
                return;
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static LayoutElement GetLayout(GameObject target)
        {
            return target != null ? target.GetComponent<LayoutElement>() : null;
        }

        private static bool IsLayoutActive(LayoutElement layout)
        {
            return layout != null && layout.gameObject.activeInHierarchy;
        }

        private static GameObject FindChild(GameObject parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            Transform child = parent.transform.Find(path);
            return child != null ? child.gameObject : null;
        }

        private static void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetFill(Image fill, float value)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static float SafeRatio(float value, float maximum)
        {
            return maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite == null)
            {
                circleSprite = CreateShapeSprite("RuntimeCircle", delegate(float x, float y) { return x * x + y * y <= 1f; });
            }

            return circleSprite;
        }

        private static Sprite GetTriangleSprite()
        {
            if (triangleSprite == null)
            {
                triangleSprite = CreateShapeSprite("RuntimeTriangle", delegate(float x, float y) { return y >= -1f && y <= 1f - Mathf.Abs(x) * 2f; });
            }

            return triangleSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (ringSprite == null)
            {
                ringSprite = CreateShapeSprite("RuntimeRing", delegate(float x, float y)
                {
                    float distance = x * x + y * y;
                    return distance <= 1f && distance >= 0.72f;
                });
            }

            return ringSprite;
        }

        /// <summary>
        /// 기본 도형만으로도 검광이 얇은 호처럼 보이도록 런타임 알파 스프라이트를 생성합니다.
        /// </summary>
        private static Sprite GetSlashArcSprite()
        {
            if (slashArcSprite != null)
            {
                return slashArcSprite;
            }

            const int width = 192;
            const int height = 96;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "RuntimeSlashArcTexture";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = x / (width - 1f) * 2f - 1f;
                    float normalizedY = y / (height - 1f) * 2f - 1f;
                    float curveY = -0.36f + (1f - normalizedX * normalizedX) * 0.58f;
                    float thickness = Mathf.Lerp(0.045f, 0.14f, (normalizedX + 1f) * 0.5f);
                    float band = 1f - Mathf.Clamp01(Mathf.Abs(normalizedY - curveY) / thickness);
                    float tipFade = Mathf.Clamp01((1f - Mathf.Abs(normalizedX)) * 7f);
                    float alpha = band * band * tipFade;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            slashArcSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            slashArcSprite.name = "RuntimeSlashArc";
            slashArcSprite.hideFlags = HideFlags.HideAndDontSave;
            return slashArcSprite;
        }

        private static Sprite CreateShapeSprite(string name, System.Func<float, float, bool> contains)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name + "Texture";
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedX = x / (size - 1f) * 2f - 1f;
                    float normalizedY = y / (size - 1f) * 2f - 1f;
                    pixels[y * size + x] = contains(normalizedX, normalizedY) ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
