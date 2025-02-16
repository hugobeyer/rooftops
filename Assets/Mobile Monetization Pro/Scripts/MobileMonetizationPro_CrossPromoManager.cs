using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_CrossPromoManager : MonoBehaviour
    {
        public static MobileMonetizationPro_CrossPromoManager instance;

        [HideInInspector]
        public bool CanChangePromoImage = false;

        [HideInInspector]
        public int SessionCounts;

        [HideInInspector]
        public bool IsVeryFirstSession = false;


        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                //PlayerPrefs.SetInt("PromoSpriteToDisplay", 0);
                PlayerPrefs.SetInt("CrossPromoIsAppOpened", 0);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // If an instance already exists, destroy this duplicate
                Destroy(gameObject);
            }
        }
        public void BeginTimerForChangingCrossPromo(float Min, float Max)
        {
            StartCoroutine(Coro(Min, Max));
        }
        IEnumerator Coro(float Min, float Max)
        {
            float Timer = Random.Range(Min, Max);
            yield return new WaitForSeconds(Timer);
            CanChangePromoImage = true;
        }
        public void SessionChecks()
        {
            ++SessionCounts;
        }
    }
}