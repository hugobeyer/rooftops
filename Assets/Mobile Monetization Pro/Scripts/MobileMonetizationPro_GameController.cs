using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_GameController : MonoBehaviour
    {
        public static MobileMonetizationPro_GameController instance;

        [HideInInspector]
        public bool IsGameStarted = false;

        public GameObject MenuUI;
        public GameObject GamePlayUI;
        public Button PlayButton;

        private void Awake()
        {
            instance = this;
        }
        public void PlayTheGame()
        {
            MenuUI.SetActive(false);
            GamePlayUI.SetActive(true);
            PlayButton.gameObject.SetActive(false);
            IsGameStarted = true;
        }


    }
}