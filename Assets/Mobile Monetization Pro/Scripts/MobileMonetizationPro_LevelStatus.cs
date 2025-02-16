using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_LevelStatus : MonoBehaviour
    {
        public GameObject LevelComplete;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                LevelComplete.SetActive(true);
                MobileMonetizationPro_GameController.instance.IsGameStarted = false;
            }
        }

        public void RestartGame()
        {
            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
