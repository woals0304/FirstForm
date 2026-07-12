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

        private LayoutElement stageLayout;
        private LayoutElement centerLayout;
        private LayoutElement[] statePanelLayouts;

        private Image sky;
        private Image sun;
        private Image farMountain;
        private Image nearMountain;
        private Image ground;
        private Image mistOne;
        private Image mistTwo;
        private Image flashOverlay;
        private Image slashEffect;
        private Image aura;

        private RectTransform playerRoot;
        private CanvasGroup playerCanvas;
        private Image playerBody;
        private Image playerRobe;
        private Image playerHead;
        private RectTransform playerSword;

        private RectTransform enemyRoot;
        private CanvasGroup enemyCanvas;
        private Image enemyBody;
        private Image enemyRobe;
        private Image enemyHead;
        private Image enemyAccent;
        private RectTransform enemyWeapon;
        private GameObject enemyShield;
        private GameObject enemySecondBlade;

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
        private float hitFlashTimer;
        private Vector2 playerBasePosition;
        private Vector2 enemyBasePosition;
        private Coroutine delayedLayoutRebuild;

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
            slashEffect = CreateAnchoredImage("SlashEffect", transform, new Color(0.76f, 0.94f, 1f, 0f), new Vector2(0.5f, 0.44f), new Vector2(220f, 16f), Vector2.zero);
            slashEffect.rectTransform.localEulerAngles = new Vector3(0f, 0f, 18f);
            flashOverlay = CreateStretchImage("HitFlash", transform, new Color(1f, 0.25f, 0.18f, 0f), Vector2.zero, Vector2.one);
            flashOverlay.transform.SetAsLastSibling();
            warningRoot.transform.SetAsLastSibling();
        }

        /// <summary>
        /// 장면과 정보 카드가 합쳐서 일정한 높이를 쓰도록 LayoutElement를 연결합니다.
        /// </summary>
        internal void ConfigureLayout(GameObject centerPanel, params GameObject[] statePanels)
        {
            stageLayout = GetComponent<LayoutElement>();
            centerLayout = centerPanel != null ? centerPanel.GetComponent<LayoutElement>() : null;
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

            if (warningText != null && visible)
            {
                SetTmpText(warningText, "강공 예고  ·  " + (string.IsNullOrEmpty(attackName) ? "기세가 모입니다" : attackName));
            }
        }

        /// <summary>
        /// 플레이어의 자동 공격을 짧은 전진과 검광으로 표현합니다.
        /// </summary>
        internal void PlayPlayerAttack()
        {
            playerAttackTimer = 0.24f;
        }

        /// <summary>
        /// 적 공격을 짧은 전진으로 표현합니다.
        /// </summary>
        internal void PlayEnemyAttack()
        {
            enemyAttackTimer = 0.28f;
        }

        /// <summary>
        /// 실제 피해를 받았을 때 화면에 짧은 붉은 피격 플래시를 표시합니다.
        /// </summary>
        internal void PlayPlayerHit()
        {
            hitFlashTimer = 0.18f;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            playerAttackTimer = Mathf.Max(0f, playerAttackTimer - delta);
            enemyAttackTimer = Mathf.Max(0f, enemyAttackTimer - delta);
            hitFlashTimer = Mathf.Max(0f, hitFlashTimer - delta);

            float playerLunge = playerAttackTimer > 0f ? Mathf.Sin((1f - playerAttackTimer / 0.24f) * Mathf.PI) * 72f : 0f;
            float enemyLunge = enemyAttackTimer > 0f ? Mathf.Sin((1f - enemyAttackTimer / 0.28f) * Mathf.PI) * 62f : 0f;
            float bob = Mathf.Sin(time * 2.1f) * 3f;

            if (playerRoot != null)
            {
                Vector2 deathDrift = currentState == FirstFormGameState.Death ? new Vector2(0f, Mathf.Sin(time * 1.2f) * 10f + 18f) : Vector2.zero;
                playerRoot.anchoredPosition = playerBasePosition + new Vector2(playerLunge, bob) + deathDrift;
                playerSword.localEulerAngles = new Vector3(0f, 0f, playerAttackTimer > 0f ? -34f : -15f + Mathf.Sin(time * 1.4f) * 2f);
            }

            if (enemyRoot != null)
            {
                enemyRoot.anchoredPosition = enemyBasePosition + new Vector2(-enemyLunge, -bob * 0.7f);
            }

            if (slashEffect != null)
            {
                float slashAlpha = playerAttackTimer > 0f ? Mathf.Clamp01(playerAttackTimer / 0.12f) : 0f;
                Color color = slashEffect.color;
                color.a = slashAlpha * 0.8f;
                slashEffect.color = color;
                slashEffect.rectTransform.anchoredPosition = new Vector2(-20f + playerLunge * 2.2f, 4f);
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
        /// 상태별로 장면 스테이지와 기존 정보 카드의 높이를 재배분합니다.
        /// </summary>
        private void ApplyLayout(FirstFormGameState state)
        {
            float stageHeight = 400f;
            float centerHeight = 420f;

            switch (state)
            {
                case FirstFormGameState.FirstFormSelection:
                    stageHeight = 180f;
                    centerHeight = 640f;
                    break;
                case FirstFormGameState.Training:
                case FirstFormGameState.Exploration:
                    stageHeight = 450f;
                    centerHeight = 370f;
                    break;
                case FirstFormGameState.ExplorationEvent:
                    stageHeight = 260f;
                    centerHeight = 560f;
                    break;
                case FirstFormGameState.BattleVictory:
                    stageHeight = 370f;
                    centerHeight = 450f;
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    stageHeight = 240f;
                    centerHeight = 580f;
                    break;
                case FirstFormGameState.Death:
                    stageHeight = 430f;
                    centerHeight = 390f;
                    break;
                case FirstFormGameState.BodySelection:
                    stageHeight = 200f;
                    centerHeight = 620f;
                    break;
            }

            SetLayoutHeight(stageLayout, stageHeight);
            SetLayoutHeight(centerLayout, centerHeight);
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
            playerBasePosition = new Vector2(0f, -28f);
            enemyBasePosition = new Vector2(250f, -28f);

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
                    playerBasePosition = new Vector2(50f, -32f);
                    break;
                case FirstFormGameState.Exploration:
                case FirstFormGameState.ExplorationEvent:
                    SetPalette(new Color(0.72f, 0.84f, 0.76f), new Color(0.39f, 0.58f, 0.45f), new Color(0.18f, 0.38f, 0.30f), new Color(0.42f, 0.53f, 0.39f));
                    SetSceneText(state == FirstFormGameState.Exploration ? "산길 출행" : "강호의 갈림길", state == FirstFormGameState.Exploration ? "안개 너머의 기척을 따라 걷습니다" : "낡은 흔적 앞에서 발걸음을 고릅니다");
                    explorationProps.SetActive(true);
                    playerBasePosition = new Vector2(-120f, -32f);
                    break;
                case FirstFormGameState.Battle:
                    SetPalette(new Color(0.58f, 0.72f, 0.75f), new Color(0.34f, 0.49f, 0.50f), new Color(0.15f, 0.29f, 0.31f), new Color(0.28f, 0.36f, 0.35f));
                    SetSceneText("산중 대치", "자동 공방 · 강공은 선택 개입");
                    enemyRoot.gameObject.SetActive(true);
                    enemyGaugeRoot.SetActive(true);
                    playerBasePosition = new Vector2(-245f, -34f);
                    enemyBasePosition = new Vector2(245f, -34f);
                    break;
                case FirstFormGameState.BattleVictory:
                    SetPalette(new Color(0.76f, 0.85f, 0.72f), new Color(0.47f, 0.61f, 0.43f), new Color(0.22f, 0.39f, 0.29f), new Color(0.43f, 0.51f, 0.38f));
                    SetSceneText("승리의 숨", "검을 거두고 다음 길을 정합니다");
                    enemyRoot.gameObject.SetActive(true);
                    enemyCanvas.alpha = 0.28f;
                    enemyRoot.localEulerAngles = new Vector3(0f, 0f, -12f);
                    playerBasePosition = new Vector2(-120f, -32f);
                    enemyBasePosition = new Vector2(250f, -72f);
                    break;
                case FirstFormGameState.BreakthroughSelection:
                    SetPalette(new Color(0.83f, 0.82f, 0.70f), new Color(0.59f, 0.56f, 0.42f), new Color(0.34f, 0.33f, 0.25f), new Color(0.52f, 0.48f, 0.35f));
                    SetSceneText("경지의 문턱", "숨을 가라앉히고 다음 경지를 바라봅니다");
                    aura.gameObject.SetActive(true);
                    playerBasePosition = new Vector2(0f, -64f);
                    playerGaugeRoot.SetActive(false);
                    break;
                case FirstFormGameState.Death:
                    SetPalette(new Color(0.72f, 0.79f, 0.82f), new Color(0.46f, 0.54f, 0.58f), new Color(0.24f, 0.30f, 0.34f), new Color(0.38f, 0.43f, 0.45f));
                    SetSceneText("혼백", "육신은 멎었으나 익힌 감각은 흐려지지 않습니다");
                    playerCanvas.alpha = 0.48f;
                    playerBasePosition = new Vector2(0f, 4f);
                    playerGaugeRoot.SetActive(false);
                    sun.color = new Color(0.82f, 0.91f, 1f, 0.72f);
                    break;
                case FirstFormGameState.BodySelection:
                    SetPalette(new Color(0.84f, 0.90f, 0.91f), new Color(0.61f, 0.70f, 0.70f), new Color(0.38f, 0.48f, 0.49f), new Color(0.64f, 0.69f, 0.66f));
                    SetSceneText("새 육신", "세 갈래 인연이 혼백 앞에 모습을 드러냅니다");
                    playerRoot.gameObject.SetActive(false);
                    playerGaugeRoot.SetActive(false);
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
            Color ink = new Color(0.08f, 0.16f, 0.18f, 1f);
            playerRobe = CreateAnchoredImage("Robe", playerRoot, ink, new Vector2(0.5f, 0.36f), new Vector2(112f, 128f), Vector2.zero, GetTriangleSprite());
            playerRobe.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            playerBody = CreateAnchoredImage("Body", playerRoot, ink, new Vector2(0.5f, 0.47f), new Vector2(76f, 105f), Vector2.zero);
            playerHead = CreateAnchoredImage("Head", playerRoot, ink, new Vector2(0.5f, 0.75f), new Vector2(60f, 60f), Vector2.zero, GetCircleSprite());
            CreateAnchoredImage("HairKnot", playerRoot, ink, new Vector2(0.56f, 0.89f), new Vector2(28f, 28f), Vector2.zero, GetCircleSprite());
            CreateAnchoredImage("Sash", playerRoot, new Color(0.31f, 0.58f, 0.61f, 1f), new Vector2(0.5f, 0.44f), new Vector2(92f, 12f), Vector2.zero);
            CreateAnchoredImage("LeftLeg", playerRoot, ink, new Vector2(0.39f, 0.16f), new Vector2(24f, 75f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, 8f);
            CreateAnchoredImage("RightLeg", playerRoot, ink, new Vector2(0.61f, 0.16f), new Vector2(24f, 75f), Vector2.zero).rectTransform.localEulerAngles = new Vector3(0f, 0f, -8f);
            playerSword = CreateAnchoredImage("Sword", playerRoot, new Color(0.84f, 0.91f, 0.91f, 1f), new Vector2(0.82f, 0.54f), new Vector2(145f, 10f), Vector2.zero).rectTransform;
            playerSword.localEulerAngles = new Vector3(0f, 0f, -15f);
        }

        private void BuildEnemySilhouette()
        {
            enemyRoot = CreateRoot("EnemySilhouette", transform, new Vector2(0.70f, 0.30f), new Vector2(210f, 270f));
            enemyCanvas = enemyRoot.gameObject.AddComponent<CanvasGroup>();
            Color ink = new Color(0.12f, 0.16f, 0.17f, 1f);
            enemyRobe = CreateAnchoredImage("Robe", enemyRoot, ink, new Vector2(0.5f, 0.34f), new Vector2(132f, 144f), Vector2.zero, GetTriangleSprite());
            enemyRobe.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            enemyBody = CreateAnchoredImage("Body", enemyRoot, ink, new Vector2(0.5f, 0.49f), new Vector2(96f, 120f), Vector2.zero);
            enemyHead = CreateAnchoredImage("Head", enemyRoot, ink, new Vector2(0.5f, 0.76f), new Vector2(66f, 66f), Vector2.zero, GetCircleSprite());
            enemyAccent = CreateAnchoredImage("Accent", enemyRoot, new Color(0.66f, 0.30f, 0.22f, 1f), new Vector2(0.5f, 0.45f), new Vector2(112f, 14f), Vector2.zero);
            CreateAnchoredImage("LeftLeg", enemyRoot, ink, new Vector2(0.39f, 0.14f), new Vector2(28f, 82f), Vector2.zero);
            CreateAnchoredImage("RightLeg", enemyRoot, ink, new Vector2(0.61f, 0.14f), new Vector2(28f, 82f), Vector2.zero);
            enemyWeapon = CreateAnchoredImage("Weapon", enemyRoot, new Color(0.17f, 0.19f, 0.19f, 1f), new Vector2(0.18f, 0.55f), new Vector2(120f, 12f), Vector2.zero).rectTransform;
            enemyWeapon.localEulerAngles = new Vector3(0f, 0f, 28f);
            enemyShield = CreateAnchoredImage("Shield", enemyRoot, new Color(0.28f, 0.31f, 0.31f, 1f), new Vector2(0.78f, 0.49f), new Vector2(64f, 90f), Vector2.zero).gameObject;
            enemySecondBlade = CreateAnchoredImage("SecondBlade", enemyRoot, new Color(0.78f, 0.84f, 0.83f, 1f), new Vector2(0.82f, 0.56f), new Vector2(100f, 8f), Vector2.zero).gameObject;
            enemySecondBlade.transform.localEulerAngles = new Vector3(0f, 0f, -30f);
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
            sceneTitleText = CreateText("SceneTitle", transform, font, 28f, FontStyles.Bold, new Color(0.08f, 0.16f, 0.18f, 1f), TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.78f), new Vector2(0.58f, 0.99f));
            sceneCaptionText = CreateText("SceneCaption", transform, font, 23f, FontStyles.Normal, new Color(0.14f, 0.24f, 0.25f, 0.92f), TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.60f), new Vector2(0.78f, 0.80f));
        }

        private void BuildGauges(TMP_FontAsset font)
        {
            playerGaugeRoot = CreateGaugePanel("PlayerGauge", transform, new Vector2(0.03f, 0.04f), new Vector2(0.43f, 0.25f));
            playerHealthText = CreateText("HealthLabel", playerGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.96f));
            playerHealthFill = CreateGauge("HealthBar", playerGaugeRoot.transform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.63f), new Color(0.82f, 0.25f, 0.22f, 1f));
            playerEnergyText = CreateText("EnergyLabel", playerGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.50f));
            playerEnergyFill = CreateGauge("EnergyBar", playerGaugeRoot.transform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.17f), new Color(0.22f, 0.55f, 0.78f, 1f));

            enemyGaugeRoot = CreateGaugePanel("EnemyGauge", transform, new Vector2(0.47f, 0.72f), new Vector2(0.97f, 0.94f));
            enemyHealthText = CreateText("EnemyLabel", enemyGaugeRoot.transform, font, 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.95f));
            enemyHealthFill = CreateGauge("EnemyHealthBar", enemyGaugeRoot.transform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.34f), new Color(0.88f, 0.30f, 0.21f, 1f));
        }

        private void BuildWarning(TMP_FontAsset font)
        {
            warningRoot = CreateGaugePanel("StrongAttackWarning", transform, new Vector2(0.25f, 0.40f), new Vector2(0.75f, 0.57f));
            Image image = warningRoot.GetComponent<Image>();
            image.color = new Color(0.28f, 0.055f, 0.045f, 0.94f);
            warningCanvas = warningRoot.AddComponent<CanvasGroup>();
            warningText = CreateText("WarningText", warningRoot.transform, font, 28f, FontStyles.Bold, new Color(1f, 0.86f, 0.67f, 1f), TextAlignmentOptions.Center, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f));
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
