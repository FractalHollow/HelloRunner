using TMPro;
using UnityEngine;

public class AboutMenu : MonoBehaviour
{
    const string VersionToken = "{VERSION}";

    public PanelFader fader;
    [Header("Static Info")]
    public string gameTitle = "My Endless Runner";
    public string websiteURL = "https://example.com";
    public string contactEmail = "support@example.com";
 
    [Header("Version Display")]
    public TMP_Text versionText;
    [TextArea]
    public string versionTextTemplate = "Version {VERSION}";

    void Awake()
    {
        EnsureFader();
        RefreshVersionText();
    }

    // Called by Settings or Pause to show About
    public void Open()
    {
        RefreshVersionText();
        gameObject.SetActive(true);
        EnsureFader()?.FadeIn();
    }

    public void Close()
    {
        var panelFader = EnsureFader();
        if (panelFader) panelFader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
    }

    PanelFader EnsureFader()
    {
        if (!fader)
            fader = GetComponent<PanelFader>();

        if (!fader)
            fader = PanelFader.Ensure(gameObject);

        return fader;
    }

    // Buttons
    public void OpenWebsite()
    {
        if (!string.IsNullOrEmpty(websiteURL))
            Application.OpenURL(websiteURL);
    }

    public void EmailUs()
    {
        // Percent-encode subject/body if you want to add them
        string mailto = $"mailto:{contactEmail}";
        Application.OpenURL(mailto);
    }

    public void RefreshVersionText()
    {
        if (!versionText)
        {
            Debug.LogWarning($"[AboutMenu] No versionText assigned on '{gameObject.name}'.");
            return;
        }

        versionText.text = GetVersionString();
    }

    public string GetVersionString()
    {
        string template = string.IsNullOrEmpty(versionTextTemplate)
            ? $"Version {VersionToken}"
            : versionTextTemplate;

        if (!template.Contains(VersionToken))
        {
            Debug.LogWarning(
                $"[AboutMenu] versionTextTemplate on '{gameObject.name}' is missing {VersionToken}. Falling back to the default version line.");
            template = $"Version {VersionToken}";
        }

        return template.Replace(VersionToken, Application.version);
    }
}
