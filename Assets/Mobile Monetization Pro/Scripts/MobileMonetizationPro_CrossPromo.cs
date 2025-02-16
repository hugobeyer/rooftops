using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_CrossPromo : MonoBehaviour
    {
        public static MobileMonetizationPro_CrossPromo instance;

        [System.Serializable]
        public enum CrossPromotype
        {
            VideoType,
            ImageType
        }

        public CrossPromotype ChooseCrossPromoType;

        [System.Serializable]
        public enum OptionsForDisplayingSprites
        {
            DisplaySequencly,
            DisplayRandomly,
        }

        public VideoPlayer videoPlayer;
        public OptionsForDisplayingSprites ChooseDisplayOption;

        [System.Serializable]
        public class SpritesWithlink
        {
            public Sprite SpriteToDisplay;
            public string LinkToGame;
            public Button DownloadButton;
        }

        [System.Serializable]
        public class VideosWithlink
        {
            public VideoClip VideoToDisplay;
            public string LinkToGame;
            public Button DownloadButton;
        }

        public RawImage RawImageComponent;
        public RenderTexture RenderTextureComponent;

        public Image ImageComponent;

        public List<VideosWithlink> AddVideos = new List<VideosWithlink>();
        public List<SpritesWithlink> AddSprites = new List<SpritesWithlink>();

        [System.Serializable]
        public enum OptionsToChangeSprites
        {
            BasedOnTimer,
            BasedOnSession,
            BasedOnAppOpens,
        }

        public OptionsToChangeSprites DecideWhenToShowNextPromo;

        public int NoOfAppOpensToCheckBeforeNewPromo = 2;

        public int NoOfSessionsToCheckBeforeNewPromo = 20;

        public float MinTimeToWaitBeforeChangingPromo = 10;
        public float MaxTimeToWaitBeforeChangingPromo = 20;

        public bool StopCrossPromotionAfterClick = true;
        public GameObject CrossPromotionToDeactive;

        int Count;
        int AppOpenCount;
        string DownloadLink;
        string link;


        private void Start()
        {
            ShowSprites();
        }
        public void ShowSprites()
        {
            if (PlayerPrefs.GetInt("DoNotDisplayCrossPromo") == 0)
            {
                Count = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                int GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);

                bool GoForIt = false;
                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                {
                    if (GetSpriteNumber < AddSprites.Count)
                    {
                        GoForIt = true;
                    }
                }
                else
                {
                    if (GetSpriteNumber < AddVideos.Count)
                    {
                        GoForIt = true;
                    }
                }

                if (GoForIt)
                {
                    if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnTimer)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage == true || MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == false)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }

                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                            MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession = true;
                        }
                    }
                    else if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnSession)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.SessionCounts >= NoOfSessionsToCheckBeforeNewPromo || MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == false)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly && MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession == true)
                            {
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            MobileMonetizationPro_CrossPromoManager.instance.SessionCounts = 0;
                            MobileMonetizationPro_CrossPromoManager.instance.IsVeryFirstSession = true;
                        }
                        MobileMonetizationPro_CrossPromoManager.instance.SessionChecks();
                    }
                    else if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnAppOpens)
                    {
                        if (PlayerPrefs.GetInt("CrossPromoAppOpenCount") >= NoOfAppOpensToCheckBeforeNewPromo)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplayRandomly)
                            {
                                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                                {
                                    Count = Random.Range(0, AddSprites.Count);
                                }
                                else
                                {
                                    Count = Random.Range(0, AddVideos.Count);
                                }
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly)
                            {
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            PlayerPrefs.SetInt("CrossPromoAppOpenCount", 0);
                        }

                        if (PlayerPrefs.GetInt("CrossPromoIsAppOpened") == 0)
                        {
                            AppOpenCount = PlayerPrefs.GetInt("CrossPromoAppOpenCount", 0);
                            AppOpenCount++;
                            PlayerPrefs.SetInt("CrossPromoAppOpenCount", AppOpenCount);
                            PlayerPrefs.Save();

                            PlayerPrefs.SetInt("CrossPromoIsAppOpened", 1);
                        }
                    }
                }

                GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);

                bool CheckForSprite = false;

                if (ChooseCrossPromoType == CrossPromotype.ImageType)
                {
                    if (GetSpriteNumber >= AddSprites.Count)
                    {
                        PlayerPrefs.SetInt("PromoSpriteToDisplay", 0);
                        MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                    }
                    GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                    ImageComponent.sprite = AddSprites[GetSpriteNumber].SpriteToDisplay;
                    link = AddSprites[GetSpriteNumber].LinkToGame;
                    GameToDownload();
                    AddSprites[GetSpriteNumber].DownloadButton.onClick.AddListener(() => ButtonToClick());
                }
                else
                {
                    if (GetSpriteNumber >= AddVideos.Count)
                    {
                        PlayerPrefs.SetInt("PromoSpriteToDisplay", 0);
                        MobileMonetizationPro_CrossPromoManager.instance.BeginTimerForChangingCrossPromo(MinTimeToWaitBeforeChangingPromo, MaxTimeToWaitBeforeChangingPromo);
                    }

                    GetSpriteNumber = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                    videoPlayer.clip = AddVideos[GetSpriteNumber].VideoToDisplay;
                    videoPlayer.targetTexture = RenderTextureComponent;
                    link = AddVideos[GetSpriteNumber].LinkToGame;
                    GameToDownload();
                    AddVideos[GetSpriteNumber].DownloadButton.onClick.AddListener(() => ButtonToClick());
                }



            }
            else
            {
                CrossPromotionToDeactive.SetActive(false);
            }
        }
        public void GameToDownload()
        {
            DownloadLink = link;
        }
        public void ButtonToClick()
        {
            Application.OpenURL(DownloadLink);
            if (StopCrossPromotionAfterClick == true)
            {
                CrossPromotionToDeactive.SetActive(false);
                PlayerPrefs.SetInt("DoNotDisplayCrossPromo", 1);
            }
        }
        private void Update()
        {
            if (PlayerPrefs.GetInt("DoNotDisplayCrossPromo") == 0)
            {
                if (MobileMonetizationPro_CrossPromoManager.instance != null)
                {
                    if (DecideWhenToShowNextPromo == OptionsToChangeSprites.BasedOnTimer)
                    {
                        if (MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage == true)
                        {
                            if (ChooseDisplayOption == OptionsForDisplayingSprites.DisplaySequencly)
                            {
                                Count = PlayerPrefs.GetInt("PromoSpriteToDisplay", 0);
                                ++Count;
                                PlayerPrefs.SetInt("PromoSpriteToDisplay", Count);
                            }
                            ShowSprites();
                            MobileMonetizationPro_CrossPromoManager.instance.CanChangePromoImage = false;
                        }
                    }
                }
            }
        }

    }
}
