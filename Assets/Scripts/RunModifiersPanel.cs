using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RunModifiersPanel : MonoBehaviour
{
    [Header("Wiring")]
    public Toggle toggleSpeed;
    public Toggle toggleHazards;
    public TMP_Text headerNote;
    public GameObject lockedGroup;

    GameManager gm;

    void OnEnable()
    {
        gm = FindObjectOfType<GameManager>();
        bool unlocked = PlayerPrefs.GetInt("mods_unlocked", 0) == 1;

        if (lockedGroup) lockedGroup.SetActive(!unlocked);

        if (!unlocked)
        {
            if (toggleSpeed)   toggleSpeed.interactable = false;
            if (toggleHazards) toggleHazards.interactable = false;
            return;
        }

        if (toggleSpeed)
        {
            toggleSpeed.interactable = true;
            toggleSpeed.isOn = PlayerPrefs.GetInt("mod_speed_on", 0) == 1;
            toggleSpeed.onValueChanged.RemoveAllListeners();
            toggleSpeed.onValueChanged.AddListener(v => {
                PlayerPrefs.SetInt("mod_speed_on", v ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        if (toggleHazards)
        {
            toggleHazards.interactable = true;
            toggleHazards.isOn = PlayerPrefs.GetInt("mod_hazards_on", 0) == 1;
            toggleHazards.onValueChanged.RemoveAllListeners();
            toggleHazards.onValueChanged.AddListener(v => {
                PlayerPrefs.SetInt("mod_hazards_on", v ? 1 : 0);
                PlayerPrefs.Save();
            });
        }
    }

    // Called by the Back button
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
