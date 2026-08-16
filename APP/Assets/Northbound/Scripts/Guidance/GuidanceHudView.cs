using UnityEngine;
using UnityEngine.UI;
using Northbound.Core;
using Northbound.UI;

namespace Northbound.Guidance
{
    public sealed class GuidanceHudView : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 IndicatorPadding = new Vector2(170f, 92f);
        private Text chapterText;
        private Text objectiveText;
        private Text instructionText;
        private RectTransform objectivePanelRect;
        private GameObject objectivePanelBorder;
        private GameObject directionIndicator;
        private RectTransform directionIndicatorRect;
        private RectTransform directionArrowRect;
        private Text directionLabel;
        private Transform destinationTarget;
        private CanvasGroup presentationGroup;
        private bool keepDirectionVisibleOnscreen;
        private GameObject missionCompletionCard;
        private Text missionCompletionTitle;
        private Text missionCompletionAction;
        private string completedMissionTitleEnglish = string.Empty;
        private bool completionRequiresExit;
        private float completionVisibleRemaining;

        public bool DirectionIndicatorVisible => directionIndicator != null && directionIndicator.activeSelf;
        public bool PresentationVisible => presentationGroup == null || presentationGroup.alpha > .5f;
        public bool MissionCompletionVisible => missionCompletionCard != null && missionCompletionCard.activeSelf;
        public RectTransform DirectionIndicatorRect => directionIndicatorRect;
        public RectTransform DirectionArrowRect => directionArrowRect;
        public string DirectionLabel => directionLabel != null ? directionLabel.text : string.Empty;
        public string MissionCompletionTitle => missionCompletionTitle != null ? missionCompletionTitle.text : string.Empty;
        public string MissionCompletionAction => missionCompletionAction != null ? missionCompletionAction.text : string.Empty;
        public string CurrentObjectiveText => objectiveText != null ? objectiveText.text : string.Empty;
        public string CurrentInstructionText => instructionText != null ? instructionText.text : string.Empty;
        public Vector2 ObjectivePanelSize => objectivePanelRect != null ? objectivePanelRect.sizeDelta : Vector2.zero;
        public bool HasGoldObjectivePanelBorder => objectivePanelBorder != null;

        public static GuidanceHudView Create()
        {
            var root = new GameObject("Guidance HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            var view = root.AddComponent<GuidanceHudView>();
            view.Build();
            return view;
        }

        public void Show(string location, string objective, string nextAction)
        {
            chapterText.text = GameText.T($"GO TO: {location}", $"目的地：{location}");
            objectiveText.text = GameText.T($"NOW: {objective}", $"当前任务：{objective}");
            instructionText.text = nextAction;
            GameText.ApplyFont(chapterText);
            GameText.ApplyFont(objectiveText);
            GameText.ApplyFont(instructionText);
        }

        public void ShowDestination(Transform target, string localizedLocationName, bool keepVisibleOnscreen = false)
        {
            destinationTarget = target;
            keepDirectionVisibleOnscreen = keepVisibleOnscreen;
            directionLabel.text = localizedLocationName ?? string.Empty;
            GameText.ApplyFont(directionLabel);
            if (destinationTarget == null || string.IsNullOrWhiteSpace(directionLabel.text))
            {
                directionIndicator.SetActive(false);
            }
        }

        public void SetPresentationVisible(bool visible)
        {
            presentationGroup ??= GetComponent<CanvasGroup>();
            if (presentationGroup == null) return;
            presentationGroup.alpha = visible ? 1f : 0f;
            presentationGroup.interactable = visible;
            presentationGroup.blocksRaycasts = visible;
            if (!visible) directionIndicator?.SetActive(false);
        }

        public void ShowMissionComplete(string missionTitleEnglish, bool requiresExit)
        {
            completedMissionTitleEnglish = missionTitleEnglish ?? string.Empty;
            completionRequiresExit = requiresExit;
            completionVisibleRemaining = requiresExit ? float.PositiveInfinity : 6f;
            RefreshMissionCompletionText();
            missionCompletionCard.SetActive(true);
        }

        public void UpdateMissionCompletionContext(bool requiresExit)
        {
            if (!MissionCompletionVisible) return;
            completionRequiresExit = requiresExit;
            completionVisibleRemaining = requiresExit ? float.PositiveInfinity : 6f;
            RefreshMissionCompletionText();
        }

        public void ClearMissionComplete()
        {
            completedMissionTitleEnglish = string.Empty;
            completionRequiresExit = false;
            completionVisibleRemaining = 0f;
            missionCompletionCard?.SetActive(false);
        }

        public static bool TryResolveOffscreenIndicator(
            Vector3 viewportPoint,
            Vector2 canvasSize,
            Vector2 edgePadding,
            out Vector2 anchoredPosition,
            out float rotationDegrees)
        {
            anchoredPosition = Vector2.zero;
            rotationDegrees = 0f;
            if (viewportPoint.z > 0f && viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                viewportPoint.y >= 0f && viewportPoint.y <= 1f)
            {
                return false;
            }

            var direction = new Vector2(viewportPoint.x - .5f, viewportPoint.y - .5f);
            if (viewportPoint.z <= 0f) direction = -direction;
            if (direction.sqrMagnitude < .0001f) direction = Vector2.up;

            var halfExtents = new Vector2(
                Mathf.Max(1f, canvasSize.x * .5f - edgePadding.x),
                Mathf.Max(1f, canvasSize.y * .5f - edgePadding.y));
            var horizontalScale = Mathf.Abs(direction.x) > .0001f ? halfExtents.x / Mathf.Abs(direction.x) : float.PositiveInfinity;
            var verticalScale = Mathf.Abs(direction.y) > .0001f ? halfExtents.y / Mathf.Abs(direction.y) : float.PositiveInfinity;
            anchoredPosition = direction * Mathf.Min(horizontalScale, verticalScale);
            rotationDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return true;
        }

        private void Build()
        {
            presentationGroup = GetComponent<CanvasGroup>();
            var panel = new GameObject("Objective Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            objectivePanelRect = panel.GetComponent<RectTransform>();
            objectivePanelRect.anchorMin = objectivePanelRect.anchorMax = new Vector2(0, 1);
            objectivePanelRect.pivot = new Vector2(0, 1);
            objectivePanelRect.anchoredPosition = new Vector2(34, -34);
            objectivePanelRect.sizeDelta = new Vector2(720, 224);
            panel.GetComponent<Image>().color = new Color(.055f, .065f, .075f, .92f);
            CreateObjectivePanelBorder(panel.transform);
            chapterText = AddText(panel.transform, "Chapter", 24, new Vector2(28, -20), new Color(.8f, .84f, .87f), 664, 32);
            objectiveText = AddText(panel.transform, "Objective", 40, new Vector2(28, -57), new Color(1f, .83f, .42f), 664, 58);
            instructionText = AddText(panel.transform, "Instruction", 30, new Vector2(28, -125), Color.white, 664, 78);
            objectiveText.fontStyle = FontStyle.Bold;
            objectiveText.resizeTextForBestFit = true;
            objectiveText.resizeTextMinSize = 26;
            objectiveText.resizeTextMaxSize = 40;
            instructionText.resizeTextForBestFit = true;
            instructionText.resizeTextMinSize = 21;
            instructionText.resizeTextMaxSize = 30;
            CreateMissionCompletionCard();
            CreateDirectionIndicator();
            CreatePauseButton();
            GameText.LanguageChanged += RefreshMissionCompletionText;
        }

        private void CreateObjectivePanelBorder(Transform panel)
        {
            objectivePanelBorder = new GameObject("Gold Objective Panel Border", typeof(RectTransform));
            objectivePanelBorder.transform.SetParent(panel, false);
            var root = objectivePanelBorder.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            AddEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 3f));
            AddEdge("Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 3f), new Vector2(0f, 3f));
            AddEdge("Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(3f, 0f), new Vector2(3f, 0f));
            AddEdge("Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-3f, 0f), new Vector2(3f, 0f));

            void AddEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta)
            {
                var edge = new GameObject(edgeName, typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(objectivePanelBorder.transform, false);
                var rect = edge.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = position;
                rect.sizeDelta = sizeDelta;
                var image = edge.GetComponent<Image>();
                image.color = new Color(1f, .72f, .12f, .95f);
                image.raycastTarget = false;
            }
        }

        private void OnDestroy() => GameText.LanguageChanged -= RefreshMissionCompletionText;

        private void Update()
        {
            if (!MissionCompletionVisible || completionRequiresExit || !PresentationVisible) return;
            completionVisibleRemaining -= Time.unscaledDeltaTime;
            if (completionVisibleRemaining <= 0f) ClearMissionComplete();
        }

        private void LateUpdate()
        {
            if (!PresentationVisible)
            {
                directionIndicator?.SetActive(false);
                return;
            }

            if (directionIndicator == null || destinationTarget == null || !destinationTarget.gameObject.activeInHierarchy)
            {
                directionIndicator?.SetActive(false);
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                directionIndicator.SetActive(false);
                return;
            }

            var canvasRect = transform as RectTransform;
            var canvasSize = canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f
                ? canvasRect.rect.size
                : ReferenceResolution;
            var viewportPoint = camera.WorldToViewportPoint(destinationTarget.position);
            if (!TryResolveOffscreenIndicator(viewportPoint, canvasSize, IndicatorPadding,
                    out var position, out var rotation))
            {
                if (!keepDirectionVisibleOnscreen)
                {
                    directionIndicator.SetActive(false);
                    return;
                }

                var halfExtents = canvasSize * .5f - IndicatorPadding;
                position = new Vector2(
                    Mathf.Clamp((viewportPoint.x - .5f) * canvasSize.x, -halfExtents.x, halfExtents.x),
                    Mathf.Clamp((viewportPoint.y - .5f) * canvasSize.y - 112f, -halfExtents.y, halfExtents.y));
                rotation = 90f;
            }

            directionIndicator.SetActive(true);
            directionIndicatorRect.anchoredPosition = position;
            directionArrowRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void CreateDirectionIndicator()
        {
            directionIndicator = new GameObject("Edge Direction Indicator", typeof(RectTransform), typeof(CanvasGroup));
            directionIndicator.transform.SetParent(transform, false);
            directionIndicatorRect = directionIndicator.GetComponent<RectTransform>();
            directionIndicatorRect.anchorMin = directionIndicatorRect.anchorMax = directionIndicatorRect.pivot = new Vector2(.5f, .5f);
            directionIndicatorRect.sizeDelta = new Vector2(300f, 94f);

            directionArrowRect = new GameObject("Gold Direction Arrow", typeof(RectTransform)).GetComponent<RectTransform>();
            directionArrowRect.SetParent(directionIndicator.transform, false);
            directionArrowRect.anchorMin = directionArrowRect.anchorMax = directionArrowRect.pivot = new Vector2(.5f, .5f);
            directionArrowRect.anchoredPosition = new Vector2(0f, 24f);
            directionArrowRect.sizeDelta = new Vector2(62f, 42f);
            AddArrowLine("Arrow Shaft", new Vector2(-5f, 0f), new Vector2(38f, 7f), 0f);
            AddArrowLine("Arrow Head Upper", new Vector2(17f, 7f), new Vector2(23f, 7f), 42f);
            AddArrowLine("Arrow Head Lower", new Vector2(17f, -7f), new Vector2(23f, 7f), -42f);

            var labelSurface = new GameObject("Destination Label Surface", typeof(RectTransform), typeof(Image));
            labelSurface.transform.SetParent(directionIndicator.transform, false);
            var surfaceRect = labelSurface.GetComponent<RectTransform>();
            surfaceRect.anchorMin = surfaceRect.anchorMax = surfaceRect.pivot = new Vector2(.5f, .5f);
            surfaceRect.anchoredPosition = new Vector2(0f, -25f);
            surfaceRect.sizeDelta = new Vector2(290f, 38f);
            var surfaceImage = labelSurface.GetComponent<Image>();
            surfaceImage.color = new Color(.055f, .065f, .075f, .9f);
            surfaceImage.raycastTarget = false;

            directionLabel = AddText(labelSurface.transform, "Destination Label", 23, Vector2.zero,
                new Color(1f, .83f, .42f), 272f, 34f);
            var labelRect = directionLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(.5f, .5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-18f, -4f);
            directionLabel.alignment = TextAnchor.MiddleCenter;
            directionLabel.fontStyle = FontStyle.Bold;
            directionLabel.resizeTextForBestFit = true;
            directionLabel.resizeTextMinSize = 17;
            directionLabel.resizeTextMaxSize = 23;
            directionIndicator.SetActive(false);

            void AddArrowLine(string name, Vector2 position, Vector2 size, float rotation)
            {
                var line = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Shadow));
                line.transform.SetParent(directionArrowRect, false);
                var lineRect = line.GetComponent<RectTransform>();
                lineRect.anchorMin = lineRect.anchorMax = lineRect.pivot = new Vector2(.5f, .5f);
                lineRect.anchoredPosition = position;
                lineRect.sizeDelta = size;
                lineRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
                var image = line.GetComponent<Image>();
                image.color = new Color(1f, .78f, .23f);
                image.raycastTarget = false;
                var shadow = line.GetComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, .8f);
                shadow.effectDistance = new Vector2(2f, -2f);
                shadow.useGraphicAlpha = true;
            }
        }

        private void CreateMissionCompletionCard()
        {
            missionCompletionCard = new GameObject("Mission Completion", typeof(RectTransform), typeof(Image), typeof(Shadow));
            missionCompletionCard.transform.SetParent(transform, false);
            var rect = missionCompletionCard.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.08f, 0f);
            rect.anchorMax = new Vector2(.92f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(0f, 164f);
            var image = missionCompletionCard.GetComponent<Image>();
            image.color = new Color(.055f, .048f, .058f, .97f);
            image.raycastTarget = false;
            var shadow = missionCompletionCard.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .7f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;

            missionCompletionTitle = AddText(missionCompletionCard.transform, "Mission Complete Title", 31,
                new Vector2(34f, -24f), new Color(1f, .83f, .42f), 1420f, 42f);
            missionCompletionTitle.fontStyle = FontStyle.Bold;
            missionCompletionTitle.resizeTextForBestFit = true;
            missionCompletionTitle.resizeTextMinSize = 22;
            missionCompletionTitle.resizeTextMaxSize = 31;

            missionCompletionAction = AddText(missionCompletionCard.transform, "Mission Complete Next Action", 30,
                new Vector2(34f, -78f), Color.white, 1420f, 60f);
            missionCompletionAction.resizeTextForBestFit = true;
            missionCompletionAction.resizeTextMinSize = 21;
            missionCompletionAction.resizeTextMaxSize = 30;
            missionCompletionCard.SetActive(false);
        }

        private void RefreshMissionCompletionText()
        {
            if (missionCompletionTitle == null || missionCompletionAction == null || string.IsNullOrWhiteSpace(completedMissionTitleEnglish)) return;
            missionCompletionTitle.text = GameText.T(
                $"MISSION COMPLETE - {completedMissionTitleEnglish}",
                $"任务完成 - {GameText.Objective(completedMissionTitleEnglish)}");
            missionCompletionAction.text = completionRequiresExit
                ? GameText.T(
                    "Go to the marked door and press E / Enter to exit the room.",
                    "前往金色标记的门口，按 E / 回车离开房间。")
                : GameText.T(
                    "Follow the gold guide to the next story objective.",
                    "跟随金色指引，前往下一个剧情目标。");
            GameText.ApplyFont(missionCompletionTitle);
            GameText.ApplyFont(missionCompletionAction);
        }

        private void CreatePauseButton()
        {
            var root = new GameObject("Pause", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-34f, -34f);
            rect.sizeDelta = new Vector2(58f, 58f);
            root.GetComponent<Image>().color = new Color(.055f, .065f, .075f, .92f);
            var button = root.GetComponent<Button>();
            button.onClick.AddListener(() => GameBootstrap.Instance?.Menus?.Pause());

            var label = new GameObject("Pause Icon", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(root.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "II";
        }

        private static Text AddText(Transform parent, string name, int size, Vector2 position, Color color, float width, float height)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, height);
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
