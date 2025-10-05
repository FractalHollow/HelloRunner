using UnityEngine;
using TMPro;

public class Currency : MonoBehaviour
{
    public static Currency I;

    const string KEY_WISPS = "wisps_total";

    int wisps;
    public int Total => wisps;

    public System.Action<int> OnChanged; // notify UI

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);

        wisps = PlayerPrefs.GetInt(KEY_WISPS, 0);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        wisps += amount;
        PlayerPrefs.SetInt(KEY_WISPS, wisps);
        OnChanged?.Invoke(wisps);
    }

    public bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (wisps < amount) return false;
        wisps -= amount;
        PlayerPrefs.SetInt(KEY_WISPS, wisps);
        OnChanged?.Invoke(wisps);
        return true;
    }
}
