using UnityEngine;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_FreezeX : MonoBehaviour
    {
        public bool ShouldFreezeX = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                Rigidbody playerRigidbody = other.gameObject.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    if (ShouldFreezeX)
                    {
                        // Freeze the X-axis position
                        playerRigidbody.constraints |= RigidbodyConstraints.FreezePositionX;
                    }
                    else
                    {
                        // Unfreeze the X-axis position
                        playerRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionX;
                    }
                }
            }
        }
    }
}