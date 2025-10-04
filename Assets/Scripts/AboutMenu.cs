using UnityEngine;

public class AboutMenu : MonoBehaviour
{
    public PanelFader fader;
    [Header("Static Info")]
    public string gameTitle = "My Endless Runner";
    public string websiteURL = "https://example.com";
    public string contactEmail = "support@example.com";

    // Called by Settings or Pause to show About
    public void Open()
    {
        gameObject.SetActive(true);
        if (fader) fader.FadeIn();
    }

    public void Close()
    {
        if (fader) fader.FadeOut(() => gameObject.SetActive(false));
        else gameObject.SetActive(false);
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

    // Optional: call from a TMP text via OnEnable if you want to inject version text
    public string GetVersionString() => $"Version {Application.version}";
}
