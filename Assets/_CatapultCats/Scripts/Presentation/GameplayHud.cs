using System.Collections.Generic;
using CatapultCats.Core;
using CatapultCats.Levels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CatapultCats.Presentation
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private LevelFlowController flow;
        [SerializeField] private Sprite catIcon;
        [SerializeField] private Sprite panelSprite;

        private static readonly Color Ink = new Color32(37, 65, 68, 255);
        private static readonly Color Cream = new Color32(255, 250, 235, 255);
        private static readonly Color Teal = new Color32(25, 124, 122, 255);
        private readonly List<Image> cats = new List<Image>();
        private ShotCounter observedShots;
        private RectTransform safeRoot;
        private Text levelLabel;
        private Text catsLabel;
        private Text hint;
        private Text resultTitle;
        private Text resultDetail;
        private Text nextLabel;
        private Button nextButton;
        private GameObject resultOverlay;
        private CanvasGroup resultFade;
        private Font font;
        private Rect lastSafeArea;
        private bool tutorialDismissed;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private void OnEnable()
        {
            if (flow == null) return;
            flow.LevelChanged += HandleLevelChanged;
            flow.LoadFailed += HandleLoadFailed;
            if (flow.Attempt != null) flow.Attempt.StateChanged += HandleState;
            if (flow.CurrentLevel != null) HandleLevelChanged(flow.CurrentLevel);
        }

        private void Start()
        {
            // Bootstrap and HUD Start order is immaterial: events plus this catch-up cover both.
            if (flow != null && flow.CurrentLevel != null) HandleLevelChanged(flow.CurrentLevel);
        }

        private void OnDisable()
        {
            if (observedShots != null) observedShots.Changed -= RefreshCats;
            if (flow == null) return;
            flow.LevelChanged -= HandleLevelChanged;
            flow.LoadFailed -= HandleLoadFailed;
            if (flow.Attempt != null) flow.Attempt.StateChanged -= HandleState;
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea && Screen.width > 0 && Screen.height > 0)
            {
                lastSafeArea = Screen.safeArea;
                safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
                safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            }

            if (resultOverlay.activeSelf)
            {
                resultFade.alpha = Mathf.MoveTowards(resultFade.alpha, 1f, Time.unscaledDeltaTime * 6f);
            }
        }

        private void LateUpdate()
        {
            // A single first-level reminder; never overlap a drag's trajectory or repeat after a shot.
            var slingshot = flow != null && flow.Attempt != null ? flow.Attempt.Slingshot : null;
            bool show = !tutorialDismissed && flow != null && flow.CurrentIndex == 0 &&
                flow.Attempt != null && flow.Attempt.State == AttemptState.Aiming && slingshot != null &&
                slingshot.State == CatapultCats.Launch.SlingshotController2D.LaunchState.Ready;
            hint.gameObject.SetActive(show);
            if (!show || slingshot.InputCamera == null) return;

            // Position only this text above the actual ground, respecting the existing camera/safe area.
            Vector3 screenPoint = slingshot.InputCamera.WorldToScreenPoint(
                new Vector3(0f, LevelValidation.GroundTopY + 0.42f, 0f));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screenPoint, null, out Vector2 local))
                hint.rectTransform.anchoredPosition = new Vector2(0f, local.y - safeRoot.rect.yMin);
        }

        private void HandleLevelChanged(LevelDefinition level)
        {
            if (observedShots != null) observedShots.Changed -= RefreshCats;
            observedShots = flow.Attempt.Shots;
            observedShots.Changed += RefreshCats;
            int index = flow.CurrentIndex;
            string label = FriendlyName(level.LevelId);
            levelLabel.text = index < 0 ? "PLAYTEST / " + label : $"{index + 1:00} / {flow.Sequence.Count:00}    {label}";
            RefreshCats(observedShots.RemainingShots);
            HandleState(flow.Attempt.State);
        }

        private static string FriendlyName(string id)
        {
            int separator = id.IndexOf('_');
            string name = separator < 0 ? id : id.Substring(separator + 1);
            var label = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1])) label.Append(' ');
                label.Append(name[i] == '_' ? ' ' : name[i]);
            }
            return label.ToString();
        }

        private void RefreshCats(int remaining)
        {
            catsLabel.text = $"CATS LEFT  {remaining}";
            for (int i = 0; i < cats.Count; i++)
            {
                cats[i].gameObject.SetActive(i < observedShots.MaximumShots);
                cats[i].color = i < remaining ? Color.white : new Color(1f, 1f, 1f, 0.22f);
            }
        }

        private void HandleState(AttemptState state)
        {
            if (state == AttemptState.CatInFlight) tutorialDismissed = true;
            bool won = state == AttemptState.Won;
            bool failed = state == AttemptState.Failed;
            resultOverlay.SetActive(won || failed);
            resultFade.alpha = 0f;
            hint.text = "DRAG THE CAT BACK  /  RELEASE TO LAUNCH";
            if (!won && !failed) return;
            resultTitle.text = won ? "Nice shot!" : "One more try?";
            bool final = won && flow.CurrentIndex >= 0 && !flow.HasNextLevel;
            resultDetail.text = won ? final ? "You cleared the backyard. All levels complete!"
                : "The mice have been outsmarted." : "Try a different angle. The mice are still here.";
            nextButton.gameObject.SetActive(won && flow.CurrentIndex >= 0);
            nextLabel.text = final ? "PLAY AGAIN" : "NEXT LEVEL";
        }

        private void HandleLoadFailed(string message)
        {
            resultOverlay.SetActive(true);
            resultFade.alpha = 1f;
            resultTitle.text = "Level unavailable";
            resultDetail.text = message;
            nextButton.gameObject.SetActive(false);
        }

        private void Build()
        {
            var canvasObject = new GameObject("GameplayHUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            safeRoot = Rect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform header = Rect("Header", safeRoot, new Vector2(0f, 1f), Vector2.one,
                new Vector2(24f, -112f), new Vector2(-24f, -24f));
            Panel(header, Cream, false);
            RectTransform labelRect = Rect("Level", header, Vector2.zero, Vector2.one,
                new Vector2(28f, 12f), new Vector2(-490f, -12f));
            levelLabel = Label(labelRect, "CATAPULT CATS", 30, Ink, TextAnchor.MiddleLeft);
            levelLabel.resizeTextForBestFit = true;
            levelLabel.resizeTextMinSize = 18;
            levelLabel.resizeTextMaxSize = 30;
            RectTransform stock = Fixed("Cats", header, new Vector2(1f, 0.5f), new Vector2(-333f, 0f), new Vector2(270f, 80f));
            catsLabel = Label(Rect("Count", stock, new Vector2(0f, 0.6f), Vector2.one,
                Vector2.zero, Vector2.zero), "CATS LEFT", 18, Ink, TextAnchor.MiddleLeft);
            for (int i = 0; i < LevelDefinition.MaximumCatCount; i++)
            {
                RectTransform iconRect = Fixed("Cat " + i, stock, new Vector2(0f, 0.35f),
                    new Vector2(22f + i * 45f, 0f), new Vector2(36f, 36f));
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.sprite = catIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                cats.Add(icon);
            }
            MakeButton(Fixed("Retry", header, new Vector2(1f, 0.5f), new Vector2(-103f, 0f),
                new Vector2(154f, 56f)), "RETRY", () => flow.Retry());
            hint = Label(Fixed("Hint", safeRoot, new Vector2(0.5f, 0f), new Vector2(0f, 39f),
                new Vector2(760f, 44f)), "DRAG THE CAT BACK  /  RELEASE TO LAUNCH", 28,
                new Color32(27, 45, 47, 255), TextAnchor.MiddleCenter);
            hint.gameObject.SetActive(false);

            RectTransform overlay = Rect("ResultOverlay", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            resultOverlay = overlay.gameObject;
            Panel(overlay, new Color(0.12f, 0.23f, 0.25f, 0.45f), true, false);
            resultFade = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform card = Fixed("ResultCard", overlay, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(620f, 350f));
            Panel(card, Cream, true);
            resultTitle = Label(Fixed("Title", card, new Vector2(0.5f, 1f), new Vector2(0f, -80f),
                new Vector2(560f, 70f)), "Nice shot!", 52, Teal, TextAnchor.MiddleCenter);
            resultDetail = Label(Fixed("Detail", card, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f),
                new Vector2(530f, 95f)), "", 25, Ink, TextAnchor.MiddleCenter);
            resultDetail.resizeTextForBestFit = true;
            resultDetail.resizeTextMinSize = 18;
            resultDetail.resizeTextMaxSize = 25;
            MakeButton(Fixed("Retry", card, new Vector2(0.5f, 0f), new Vector2(-137f, 67f),
                new Vector2(235f, 62f)), "RETRY", () => flow.Retry());
            nextButton = MakeButton(Fixed("Continue", card, new Vector2(0.5f, 0f), new Vector2(137f, 67f),
                new Vector2(235f, 62f)), "NEXT LEVEL", () => flow.ContinueAfterWin());
            nextLabel = nextButton.GetComponentInChildren<Text>();
            resultOverlay.SetActive(false);
            if (EventSystem.current == null)
            {
                var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
        }

        private void Panel(RectTransform rect, Color color, bool blocks, bool rounded = true)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded ? panelSprite : null;
            image.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = blocks;
        }

        private Button MakeButton(RectTransform rect, string label, UnityAction action)
        {
            Panel(rect, Teal, true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.9f, 0.9f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(Rect("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                label, 21, Cream, TextAnchor.MiddleCenter);
            return button;
        }

        private Text Label(RectTransform rect, string content, int size, Color color, TextAnchor alignment)
        {
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Fixed(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }
    }
}
