using UnityEngine;
using TMPro;

public class DisplayTridotStats : MonoBehaviour
{
    public TMP_Text tridotText;
    private static int currentRunTridotes = 0;  // Just for this run

    void Start()
    {
        if (tridotText == null)
        {
            tridotText = GetComponent<TMP_Text>();
        }
        currentRunTridotes = 0;  // Reset at start
    }

    void Update()
    {
        // Just show current run's tridots count
        tridotText.text = $"{currentRunTridotes}";
    }

    public static void AddTridot()
    {
        currentRunTridotes++;
    }
}