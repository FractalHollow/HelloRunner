using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirstRunTutorial : MonoBehaviour
{
    public const string SeenKey = "first_run_tutorial_seen";

    const string SafeAreaName = "StartUI(SafeArea)";
    const float FadeDuration = 0.2f;

    [SerializeField] Sprite emberSprite;

    readonly Color overlayColor = new Color(0.035f, 0.055f, 0.085f, 0.94f);
    readonly Color cardColor = new Color(0.11f, 0.15f, 0.18f, 0.985f);
    readonly Color bodyColor = new Color(0.92f, 0.95f, 0.98f, 1f);
    readonly Color mutedColor = new Color(0.65f, 0.72f, 0.79f, 1f);
    readonly Color dotOffColor = new Color(1f, 1f, 1f, 0.2f);
    readonly Color buttonTextColor = new Color(0.12f, 0.08f, 0.03f, 1f);

    RectTransform overlayRoot;
    PanelFader overlayFader;
    Button skipButton;
    Button nextButton;
    Button startButton;
    Image[] dots;
    GameObject[] pages;
    Action onFinished;
    bool built;
    int currentPage;

    enum TutorialSpriteKind
    {
        None,
        Player,
        Ember
    }

    class PageBuildData
    {
        public string Name;
        public string Title;
        public string Body;
        public string TopLabel;
        public string BottomLabel;
        public string CenterLabel;
        public Color Accent;
        public TutorialSpriteKind SpriteKind;
        public float SpriteScale = 1f;
    }

    public bool IsShowing => overlayRoot && overlayRoot.gameObject.activeSelf;

    public bool ShouldShow()
    {
        return PlayerPrefs.GetInt(SeenKey, 0) == 0;
    }

    void Awake()
    {
        EnsureBuilt();
        HideInstant();
    }

    void OnDisable()
    {
        HideInstant();
    }

    public void Begin(Action onFinishedCallback)
    {
        EnsureBuilt();

        onFinished = onFinishedCallback;
        ShowPage(0);

        overlayRoot.SetAsLastSibling();
        overlayFader.FadeIn();
    }

    void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    void Skip()
    {
        CompleteTutorial();
    }

    void CompleteTutorial()
    {
        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();

        Action callback = onFinished;
        onFinished = null;

        overlayFader.FadeOut(() =>
        {
            callback?.Invoke();
        });
    }

    void HideInstant()
    {
        if (overlayFader)
            overlayFader.HideInstant();
    }

    void ShowPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
            return;

        currentPage = Mathf.Clamp(pageIndex, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i])
                pages[i].SetActive(i == currentPage);

            if (dots != null && i < dots.Length && dots[i])
                dots[i].color = (i == currentPage) ? GetPageAccent(i) : dotOffColor;
        }

        if (nextButton)
            nextButton.gameObject.SetActive(currentPage < pages.Length - 1);

        if (startButton)
            startButton.gameObject.SetActive(currentPage >= pages.Length - 1);
    }

    Color GetPageAccent(int pageIndex)
    {
        switch (pageIndex)
        {
            case 0: return new Color(0.96f, 0.63f, 0.31f, 1f);
            case 1: return new Color(0.95f, 0.42f, 0.38f, 1f);
            case 2: return new Color(0.31f, 0.84f, 0.74f, 1f);
            default: return new Color(0.99f, 0.82f, 0.34f, 1f);
        }
    }

    void EnsureBuilt()
    {
        if (built)
            return;

        built = true;

        RectTransform parent = FindOverlayParent();
        TMP_Text textTemplate = GetComponentInChildren<TMP_Text>(true);
        Button buttonTemplate = GetComponentInChildren<Button>(true);
        Sprite playerSprite = FindPlayerSprite();
        Sprite tutorialEmberSprite = FindEmberSprite();

        overlayRoot = CreateRect("FirstRunTutorialOverlay", parent);
        Stretch(overlayRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlayRoot.SetAsLastSibling();

        Image overlayImage = overlayRoot.gameObject.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;

        CanvasGroup group = overlayRoot.gameObject.AddComponent<CanvasGroup>();
        overlayFader = overlayRoot.gameObject.AddComponent<PanelFader>();
        overlayFader.group = group;
        overlayFader.duration = FadeDuration;

        RectTransform card = CreateRect("Card", overlayRoot);
        Stretch(card, new Vector2(0.06f, 0.075f), new Vector2(0.94f, 0.925f), Vector2.zero, Vector2.zero);
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = cardColor;

        RectTransform pagesRoot = CreateRect("Pages", card);
        Stretch(pagesRoot, new Vector2(0f, 0.12f), new Vector2(1f, 1f), new Vector2(28f, -20f), new Vector2(-28f, -140f));

        PageBuildData[] pageData =
        {
            new PageBuildData
            {
                Name = "Page_1_Move",
                Title = "Tap To Change Direction",
                Body = "You are always moving toward the top or bottom. Tap to change direction.",
                TopLabel = "",
                BottomLabel = "",
                CenterLabel = "TAP ANYWHERE",
                Accent = GetPageAccent(0),
                SpriteKind = TutorialSpriteKind.Player,
                SpriteScale = 1.5f
            },
            new PageBuildData
            {
                Name = "Page_2_Bounds",
                Title = "Stay Between The Walls",
                Body = "Do not hit the ceiling or floor.",
                TopLabel = "CEILING",
                BottomLabel = "FLOOR",
                CenterLabel = "STAY BETWEEN",
                Accent = GetPageAccent(1),
                SpriteKind = TutorialSpriteKind.Player,
                SpriteScale = 1.5f
            },
            new PageBuildData
            {
                Name = "Page_3_Embers",
                Title = "Dodge And Collect",
                Body = "Dodge enemies and collect Embers.",
                TopLabel = "DODGE",
                BottomLabel = "COLLECT EMBERS",
                CenterLabel = "KEEP MOVING",
                Accent = GetPageAccent(2),
                SpriteKind = TutorialSpriteKind.Ember
            },
            new PageBuildData
            {
                Name = "Page_4_Goal",
                Title = "Push Your Distance",
                Body = "How far can you make it?",
                TopLabel = "SURVIVE",
                BottomLabel = "GO FURTHER",
                CenterLabel = "GOOD LUCK",
                Accent = GetPageAccent(3),
                SpriteKind = TutorialSpriteKind.None
            }
        };

        pages = new GameObject[pageData.Length];
        dots = new Image[pageData.Length];

        for (int i = 0; i < pageData.Length; i++)
            pages[i] = CreatePage(pagesRoot, pageData[i], textTemplate, playerSprite, tutorialEmberSprite);

        RectTransform footer = CreateRect("Footer", card);
        Stretch(footer, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 20f), new Vector2(-28f, 132f));

        skipButton = CreateButton("SkipButton", footer, textTemplate, buttonTemplate, "Skip");
        Anchor(skipButton.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(175f, 0f), new Vector2(330f, 117f));
        skipButton.onClick.AddListener(Skip);

        nextButton = CreateButton("NextButton", footer, textTemplate, buttonTemplate, "Next");
        Anchor(nextButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-175f, 0f), new Vector2(330f, 117f));
        nextButton.onClick.AddListener(NextPage);

        startButton = CreateButton("StartButton", footer, textTemplate, buttonTemplate, "Start");
        Anchor(startButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-175f, 0f), new Vector2(330f, 117f));
        startButton.onClick.AddListener(CompleteTutorial);

        RectTransform dotsRoot = CreateRect("Dots", footer);
        Anchor(dotsRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 28f));

        for (int i = 0; i < dots.Length; i++)
        {
            RectTransform dot = CreateRect($"Dot_{i + 1}", dotsRoot);
            Anchor(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((-54f) + (36f * i), 0f), new Vector2(20f, 20f));

            Image dotImage = dot.gameObject.AddComponent<Image>();
            dotImage.color = dotOffColor;
            dots[i] = dotImage;
        }
    }

    GameObject CreatePage(Transform parent, PageBuildData data, TMP_Text textTemplate, Sprite playerSprite, Sprite tutorialEmberSprite)
    {
        RectTransform page = CreateRect(data.Name, parent);
        Stretch(page, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform accentBar = CreateRect("Accent", page);
        Stretch(accentBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(0f, 0f));
        Image accentImage = accentBar.gameObject.AddComponent<Image>();
        accentImage.color = data.Accent;

        TextMeshProUGUI title = CreateText("Title", page, textTemplate, 54f, FontStyles.Bold);
        Stretch(title.rectTransform, new Vector2(0f, 0.8f), new Vector2(1f, 1f), new Vector2(8f, -12f), new Vector2(-8f, -14f));
        title.text = data.Title;
        title.alignment = TextAlignmentOptions.Center;

        RectTransform artBox = CreateRect("ArtBox", page);
        Stretch(artBox, new Vector2(0.09f, 0.34f), new Vector2(0.91f, 0.72f), Vector2.zero, Vector2.zero);
        Image artImage = artBox.gameObject.AddComponent<Image>();
        artImage.color = new Color(data.Accent.r * 0.28f, data.Accent.g * 0.28f, data.Accent.b * 0.28f, 0.95f);

        TextMeshProUGUI topLabel = CreateText("TopLabel", artBox, textTemplate, 30f, FontStyles.Bold);
        Stretch(topLabel.rectTransform, new Vector2(0f, 0.77f), new Vector2(1f, 1f), new Vector2(12f, -8f), new Vector2(-12f, -16f));
        topLabel.alignment = TextAlignmentOptions.Center;
        topLabel.color = data.Accent;
        topLabel.text = data.TopLabel;

        TextMeshProUGUI centerLabel = CreateText("CenterLabel", artBox, textTemplate, 34f, FontStyles.Bold);
        Stretch(centerLabel.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.72f), Vector2.zero, Vector2.zero);
        centerLabel.alignment = TextAlignmentOptions.Center;
        centerLabel.color = bodyColor;
        centerLabel.text = data.CenterLabel;

        TextMeshProUGUI bottomLabel = CreateText("BottomLabel", artBox, textTemplate, 30f, FontStyles.Bold);
        Stretch(bottomLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), new Vector2(12f, 14f), new Vector2(-12f, 0f));
        bottomLabel.alignment = TextAlignmentOptions.Center;
        bottomLabel.color = data.Accent;
        bottomLabel.text = data.BottomLabel;

        Sprite panelSprite = GetPageSprite(data, playerSprite, tutorialEmberSprite);
        if (panelSprite)
        {
            RectTransform player = CreateRect("PlayerSprite", artBox);
            Anchor(player, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0f, -4f), new Vector2(132f, 132f));
            player.localScale = new Vector3(data.SpriteScale, data.SpriteScale, 1f);

            Image playerImage = player.gameObject.AddComponent<Image>();
            playerImage.sprite = panelSprite;
            playerImage.preserveAspect = true;
            playerImage.color = Color.white;
        }

        TextMeshProUGUI body = CreateText("Body", page, textTemplate, 34f, FontStyles.Normal);
        body.fontSize = 48f;
        Stretch(body.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.28f), Vector2.zero, Vector2.zero);
        body.alignment = TextAlignmentOptions.Center;
        body.color = bodyColor;
        body.enableWordWrapping = true;
        body.text = data.Body;

        return page.gameObject;
    }

    Sprite GetPageSprite(PageBuildData data, Sprite playerSprite, Sprite tutorialEmberSprite)
    {
        switch (data.SpriteKind)
        {
            case TutorialSpriteKind.Player:
                return playerSprite;
            case TutorialSpriteKind.Ember:
                return tutorialEmberSprite;
            default:
                return null;
        }
    }

    RectTransform FindOverlayParent()
    {
        Transform safeArea = transform.Find(SafeAreaName);
        if (safeArea is RectTransform safeAreaRect)
            return safeAreaRect;

        return transform as RectTransform;
    }

    Sprite FindPlayerSprite()
    {
        SkinDef selectedSkin = CosmeticsManager.I ? CosmeticsManager.I.GetSelectedDef() : null;
        if (selectedSkin)
        {
            if (selectedSkin.sprite)
                return selectedSkin.sprite;

            if (selectedSkin.idleFrames != null && selectedSkin.idleFrames.Length > 0 && selectedSkin.idleFrames[0])
                return selectedSkin.idleFrames[0];
        }

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (!gm || !gm.player)
            return null;

        Transform visual = gm.player.transform.Find("Visual");
        SpriteRenderer spriteRenderer = visual
            ? visual.GetComponentInChildren<SpriteRenderer>(true)
            : gm.player.GetComponentInChildren<SpriteRenderer>(true);

        return spriteRenderer ? spriteRenderer.sprite : null;
    }

    Sprite FindEmberSprite()
    {
        if (emberSprite)
            return emberSprite;

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm && gm.wispSpawner && gm.wispSpawner.wispPrefab)
        {
            SpriteRenderer prefabRenderer = gm.wispSpawner.wispPrefab.GetComponent<SpriteRenderer>();
            if (prefabRenderer && prefabRenderer.sprite)
                return prefabRenderer.sprite;
        }

        WispPickup liveWisp = FindFirstObjectByType<WispPickup>();
        if (liveWisp)
        {
            SpriteRenderer liveRenderer = liveWisp.GetComponent<SpriteRenderer>();
            if (liveRenderer && liveRenderer.sprite)
                return liveRenderer.sprite;
        }

        return null;
    }

    Button CreateButton(string name, Transform parent, TMP_Text textTemplate, Button buttonTemplate, string label)
    {
        RectTransform root = CreateRect(name, parent);
        Image image = root.gameObject.AddComponent<Image>();
        Button button = root.gameObject.AddComponent<Button>();

        if (buttonTemplate)
        {
            button.transition = buttonTemplate.transition;
            button.colors = buttonTemplate.colors;
            button.spriteState = buttonTemplate.spriteState;

            Image templateImage = buttonTemplate.targetGraphic as Image;
            if (templateImage)
            {
                image.sprite = templateImage.sprite;
                image.type = templateImage.type;
                image.preserveAspect = templateImage.preserveAspect;
                image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
                image.color = templateImage.color;
            }
        }
        else
        {
            image.color = new Color(0.22f, 0.34f, 0.42f, 1f);
        }

        button.targetGraphic = image;

        TextMeshProUGUI labelText = CreateText("Label", root, textTemplate, 48f, FontStyles.Bold);
        Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = buttonTextColor;
        labelText.text = label;

        return button;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, TMP_Text template, float size, FontStyles style)
    {
        RectTransform root = CreateRect(name, parent);
        TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();

        if (template)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
            text.enableAutoSizing = false;
        }

        text.fontSize = size;
        text.fontStyle = style;
        text.color = mutedColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return text;
    }

    RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
