using UnityEngine;
using UnityEngine.UI;

namespace MobileMonetizationPro
{
    public class MobileMonetizationPro_PlayerController : MonoBehaviour
    {
        public Slider sizeSlider;
        public float minSize = 0.5f;
        public float maxSize = 2.0f;
        public float forwardSpeed = 5.0f;
        public float rotationSpeed = 50.0f;
        public Camera playerCamera;
        public Vector3 cameraOffset = new Vector3(0f, 2f, -5f); // Offset for camera position relative to the player

        public float fallMultiplier = 2.5f; // Adjust this value to control the falling speed multiplier

        private Rigidbody rb;
        private Transform playerTransform;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            playerTransform = transform;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX; // Freeze rotation on X and Z axes
        }

        void FixedUpdate()
        {
            // Change the size of the sphere based on the slider value
            float newSize = Mathf.Lerp(minSize, maxSize, sizeSlider.value);
            playerTransform.localScale = Vector3.one * newSize;

            if (MobileMonetizationPro_GameController.instance.IsGameStarted == true)
            {
                // Calculate forward movement in global space
                Vector3 forwardMovement = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;

                // Move the player forward in global space
                rb.MovePosition(rb.position + forwardMovement);

                // Rotate the player continuously along the global X-axis
                float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
                Quaternion deltaRotation = Quaternion.Euler(rotationAmount, 0f, 0f);
                rb.MoveRotation(deltaRotation * rb.rotation);

                // Follow the player with the camera
                if (playerCamera != null)
                {
                    // Calculate the desired camera position with offset
                    Vector3 desiredCameraPosition = playerTransform.position + cameraOffset;

                    // Smoothly move the camera to the desired position
                    playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredCameraPosition, Time.deltaTime * forwardSpeed);
                }
            }

            // Increase the falling speed if the player is falling
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            }
        }
    }
}
//using UnityEngine;
//using UnityEngine.UI;

//public class PlayerController : MonoBehaviour
//{
//    public Slider sizeSlider;
//    public float minSize = 0.5f;
//    public float maxSize = 2.0f;
//    public float forwardSpeed = 5.0f;
//    public float rotationSpeed = 50.0f;
//    public Camera playerCamera;
//    public Vector3 cameraOffset = new Vector3(0f, 2f, -5f); // Offset for camera position relative to the player

//    private Rigidbody rb;
//    private Transform playerTransform;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//        playerTransform = transform;
//        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX; // Freeze rotation on X and Z axes
//    }

//    void FixedUpdate()
//    {

//            // Change the size of the sphere based on the slider value
//            float newSize = Mathf.Lerp(minSize, maxSize, sizeSlider.value);
//            playerTransform.localScale = Vector3.one * newSize;

//        if (GameController.instance.IsGameStarted == true)
//        {
//            // Calculate forward movement in global space
//            Vector3 forwardMovement = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;

//            // Move the player forward in global space
//            rb.MovePosition(rb.position + forwardMovement);

//            // Rotate the player continuously along the global X-axis
//            float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
//            Quaternion deltaRotation = Quaternion.Euler(rotationAmount, 0f, 0f);
//            rb.MoveRotation(deltaRotation * rb.rotation);

//            // Follow the player with the camera
//            if (playerCamera != null)
//            {
//                // Calculate the desired camera position with offset
//                Vector3 desiredCameraPosition = playerTransform.position + cameraOffset;

//                // Smoothly move the camera to the desired position
//                playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desiredCameraPosition, Time.deltaTime * forwardSpeed);
//            }
//        }

//    }
//}






//using UnityEngine;
//using UnityEngine.UI;

//public class PlayerController : MonoBehaviour
//{
//    public Slider sizeSlider;
//    public float minSize = 0.5f;
//    public float maxSize = 2.0f;
//    public float forwardSpeed = 5.0f;
//    public float rotationSpeed = 50.0f;

//    private Rigidbody rb;
//    private Transform playerTransform;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//        playerTransform = transform;
//        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX; // Freeze rotation on X and Z axes
//    }

//    void FixedUpdate()
//    {
//        // Change the size of the sphere based on the slider value
//        float newSize = Mathf.Lerp(minSize, maxSize, sizeSlider.value);
//        playerTransform.localScale = Vector3.one * newSize;

//        // Calculate forward movement in global space
//        Vector3 forwardMovement = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;

//        // Move the player forward in global space
//        rb.MovePosition(rb.position + forwardMovement);

//        // Rotate the player continuously along the global X-axis
//        float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
//        Quaternion deltaRotation = Quaternion.Euler(rotationAmount, 0f, 0f);
//        rb.MoveRotation(deltaRotation * rb.rotation);
//    }
//}






//using UnityEngine;
//using UnityEngine.UI;

//public class PlayerController : MonoBehaviour
//{
//    public Slider sizeSlider;
//    public float minSize = 0.5f;
//    public float maxSize = 2.0f;
//    public float speed = 5.0f;
//    public GameObject cameraToFollow;
//    public float offsetX = 0f;
//    public float offsetY = 2f;
//    public float offsetZ = -5f;

//    private Rigidbody rb;
//    private Transform playerTransform;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//        playerTransform = transform;
//        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;
//    }

//    void FixedUpdate()
//    {
//        // Change the size of the sphere based on the slider value
//        float newSize = Mathf.Lerp(minSize, maxSize, sizeSlider.value);
//        playerTransform.localScale = Vector3.one * newSize;

//        // Move the sphere automatically in the forward direction
//        Vector3 moveDirection = playerTransform.forward;
//        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);

//        // Rotate the ball in the forward direction
//        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
//        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * speed));

//        // Move the camera to follow the player with offset
//        if (cameraToFollow != null)
//        {
//            Vector3 desiredPosition = playerTransform.position +
//                playerTransform.right * offsetX +
//                playerTransform.up * offsetY +
//                playerTransform.forward * offsetZ;

//            cameraToFollow.transform.position = Vector3.Lerp(cameraToFollow.transform.position, desiredPosition, Time.deltaTime * speed);
//            cameraToFollow.transform.rotation = playerTransform.rotation;
//        }
//    }
//}
