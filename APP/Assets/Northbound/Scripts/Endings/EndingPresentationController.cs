using System;
using Northbound.Core;
using Northbound.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Northbound.Endings
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class EndingPresentationController : MonoBehaviour
    {
        private InputGate inputGate;
        private IDisposable inputLease;
        private CanvasGroup canvasGroup;
        private Image sceneBackground;
        private Image horizonBand;
        private Image roadLine;
        private Text titleLabel;
        private Text stagingLabel;
        private Text carriedDetailLabel;
        private Text endCardLabel;
        private Text creditsLabel;
        private Button returnToTitleButton;

        public bool IsShowing { get; private set; }
        public EndingContext CurrentContext { get; private set; }
        public string VisibleStaging => stagingLabel != null ? stagingLabel.text : string.Empty;
        public string VisibleCarriedDetail => carriedDetailLabel != null ? carriedDetailLabel.text : string.Empty;
        public string VisibleEndCard => endCardLabel != null ? endCardLabel.text : string.Empty;
        public Color VisibleBackgroundColor => sceneBackground != null ? sceneBackground.color : Color.clear;
        public event Action ReturnedToTitle;

        private void Awake()
        {
            EnsureVisuals();
            Hide();
        }

        private void Update()
        {
            if (IsShowing && Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                ReturnToTitle();
            }
        }

        private void OnDestroy()
        {
            inputLease?.Dispose();
        }

        public void Initialize(InputGate gate)
        {
            inputGate = gate ?? throw new ArgumentNullException(nameof(gate));
        }

        public void Show(EndingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CurrentContext = context;
            inputLease?.Dispose();
            inputLease = inputGate?.Acquire(this);
            ApplyTheme(context);
            titleLabel.text = TitleFor(context);
            var historyEcho = GameText.IsChinese ? context.HistoryEchoTextChinese : context.HistoryEchoText;
            stagingLabel.text = StagingFor(context) +
                (string.IsNullOrWhiteSpace(historyEcho) ? string.Empty : $"\n\n{historyEcho}");
            carriedDetailLabel.text = CarriedDetailFor(context);
            endCardLabel.text = EndCardFor(context);
            creditsLabel.text = GameText.T(
                "Northbound\n\nCredits\nNorthbound Team\n\nPress Enter to return to the title",
                "Northbound\n\n制作人员\nNorthbound Team\n\n按回车返回标题");
            returnToTitleButton.GetComponentInChildren<Text>(true).text = GameText.T("Return to Title", "返回标题");
            foreach (var label in GetComponentsInChildren<Text>(true)) GameText.ApplyFont(label);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsShowing = true;
        }

        public void ReturnToTitle()
        {
            if (!IsShowing)
            {
                return;
            }

            Hide();
            ReturnedToTitle?.Invoke();
        }

        public void Cancel()
        {
            Hide();
        }

        private void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            inputLease?.Dispose();
            inputLease = null;
            IsShowing = false;
        }

        private void EnsureVisuals()
        {
            var canvas = gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            sceneBackground = CreateImage("Ending Scene", Vector2.zero, Vector2.one);
            horizonBand = CreateImage("Horizon", new Vector2(0f, .33f), new Vector2(1f, .48f));
            roadLine = CreateImage("Road", new Vector2(.46f, 0f), new Vector2(.54f, .36f));
            titleLabel = CreateLabel("Title", new Vector2(.15f, .72f), new Vector2(.85f, .88f), 42, TextAnchor.MiddleCenter);
            stagingLabel = CreateLabel("Staging", new Vector2(.2f, .49f), new Vector2(.8f, .68f), 24, TextAnchor.MiddleCenter);
            carriedDetailLabel = CreateLabel("Carried Detail", new Vector2(.2f, .405f), new Vector2(.8f, .48f), 19, TextAnchor.MiddleCenter);
            endCardLabel = CreateLabel("End Card", new Vector2(.14f, .24f), new Vector2(.86f, .39f), 28, TextAnchor.MiddleCenter);
            creditsLabel = CreateLabel("Credits and Return", new Vector2(.2f, .05f), new Vector2(.8f, .2f), 20, TextAnchor.MiddleCenter);
            returnToTitleButton = CreateButton("Return to Title", new Vector2(.4f, .01f), new Vector2(.6f, .07f));
            returnToTitleButton.onClick.AddListener(ReturnToTitle);
        }

        private Text CreateLabel(string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize, TextAnchor alignment)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(.95f, .93f, .86f, 1f);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private Image CreateImage(string objectName, Vector2 anchorMin, Vector2 anchorMax)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private Button CreateButton(string objectName, Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = CreateLabel("Label", Vector2.zero, Vector2.one, 20, TextAnchor.MiddleCenter);
            label.transform.SetParent(buttonObject.transform, false);
            label.text = "Return to Title";
            return buttonObject.GetComponent<Button>();
        }

        private static string TitleFor(EndingContext context)
        {
            switch (context.Direction)
            {
                case EndingDirection.Northbound: return GameText.T("Northbound", "向北");
                case EndingDirection.HomeChosen: return GameText.T("Home, Chosen", "选择故乡");
                case EndingDirection.NoMap: return GameText.T("No Map", "无图之路");
                case EndingDirection.PauseJourney: return GameText.T("Pause the Journey", "暂缓启程");
                default: return GameText.T("Not Alone", "并不孤单");
            }
        }

        private static string StagingFor(EndingContext context)
        {
            return context.Direction switch
            {
                EndingDirection.Northbound => GameText.T(
                    "The station wagon catches the first light. Jamie takes the second key, carrying every answer that made this departure their own.",
                    "旅行车迎上第一束晨光。杰米接过第二把钥匙，也带上了那些让这次出发真正属于自己的回答。"),
                EndingDirection.HomeChosen => GameText.T(
                    "Greybridge wakes one window at a time. Jamie stays because staying has become a choice, not an inheritance.",
                    "格雷布里奇的窗户一扇接一扇亮起。杰米留下，不再是因为习惯，而是因为终于作出了自己的选择。"),
                EndingDirection.NoMap => GameText.T(
                    "Jamie takes the road that belongs to neither the old promise nor the old fear. The keepsake remains, but it no longer decides the route.",
                    "杰米走上一条既不属于旧约定、也不属于旧恐惧的路。纪念物仍在身边，却不再替杰米决定方向。"),
                EndingDirection.PauseJourney => GameText.T(
                    "Jamie returns to the rooftop and lets Friday pass. The journey is not cancelled; for the first time, its timing belongs to everyone who must live with it.",
                    "杰米回到屋顶，让星期五安静地过去。旅程并未取消；只是第一次，出发的时间属于每个必须承担它的人。"),
                _ => GameText.T(
                    "Morning arrives with another person still in the frame.",
                    "清晨到来时，画面里仍有另一个人。")
            };
        }

        private static string EndCardFor(EndingContext context)
        {
            if (!GameText.IsChinese) return context.EndCard;
            return context.Direction switch
            {
                EndingDirection.Northbound => "有些约定把我们带向前方；也有些约定，会追问我们愿意被它带走多久。",
                EndingDirection.HomeChosen => "当留下是一种选择，留下就不是旅程的缺席。",
                EndingDirection.NoMap => "并不是每一条路，都从一个目的地开始。",
                EndingDirection.PauseJourney => "当选择终于属于自己，给自己时间也可以是一种方向。",
                _ => "人生可以由方向决定，也可以由我们愿意与谁相遇来决定。"
            };
        }

        private void ApplyTheme(EndingContext context)
        {
            Color background;
            Color horizon;
            Color route;
            switch (context.Direction)
            {
                case EndingDirection.Northbound:
                    background = new Color(.035f, .075f, .14f, 1f);
                    horizon = new Color(.24f, .42f, .5f, 1f);
                    route = new Color(.95f, .72f, .32f, .9f);
                    break;
                case EndingDirection.HomeChosen:
                    background = new Color(.12f, .16f, .12f, 1f);
                    horizon = new Color(.68f, .4f, .2f, 1f);
                    route = new Color(.98f, .79f, .46f, .9f);
                    break;
                case EndingDirection.NoMap:
                    background = new Color(.12f, .16f, .2f, 1f);
                    horizon = new Color(.36f, .44f, .5f, 1f);
                    route = new Color(.78f, .82f, .79f, .8f);
                    break;
                case EndingDirection.PauseJourney:
                    background = new Color(.095f, .085f, .15f, 1f);
                    horizon = new Color(.3f, .27f, .42f, 1f);
                    route = new Color(.58f, .72f, .76f, .75f);
                    break;
                default:
                    background = new Color(.08f, .11f, .13f, 1f);
                    horizon = new Color(.3f, .38f, .4f, 1f);
                    route = new Color(.85f, .75f, .48f, .8f);
                    break;
            }

            sceneBackground.color = background;
            horizonBand.color = horizon;
            roadLine.color = route;
        }

        private static string CarriedDetailFor(EndingContext context)
        {
            var detail = context.CarriedPropId switch
            {
                "second_key" => GameText.T("The second key turns in Jamie's hand.", "第二把钥匙在杰米手中轻轻转动。"),
                "garage_light_switch" => GameText.T("The garage light stays on.", "车库的灯一直亮着。"),
                "bus_stop_bench" => GameText.T("The empty bus-stop bench becomes a place to begin.", "空着的公交站长椅，成了重新开始的地方。"),
                "notebook_write_date" => GameText.T("Jamie writes the date on the notebook's first blank page.", "杰米在笔记本第一张空白页上写下日期。"),
                "notebook_blank_page" => GameText.T("A blank notebook page waits without demanding an answer.", "笔记本的空白页安静等待，不催促任何答案。"),
                "photo_hold_to_sunrise" => GameText.T("The photograph catches the sunrise.", "照片迎住了升起的晨光。"),
                "photo_rooftop_dawn" => GameText.T("The photograph rests beside Jamie in the rooftop dawn.", "屋顶晨光里，照片静静放在杰米身旁。"),
                "house_key_unlock_door" => GameText.T("The old house key opens a door that has not been named yet.", "旧房门钥匙打开了一扇尚未命名的门。"),
                "house_key_in_pocket" => GameText.T("The house key remains warm in Jamie's pocket.", "房门钥匙在杰米口袋里仍带着温度。"),
                "map_fold_keep" => GameText.T("Jamie folds the map and keeps it without obeying it.", "杰米把地图折好收起，却不再听命于它。"),
                "folded_map_beside_arrow" => GameText.T("The folded map lies beside the painted arrow.", "折好的地图放在那道画出的箭头旁。"),
                _ => GameText.T("What Jamie carried has changed what this road means.", "杰米一路带来的东西，已经改变了这条路的意义。")
            };
            return GameText.T("CARRIED FORWARD  ", "一路带来  ") + detail;
        }
    }
}
