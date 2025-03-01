using UnityEngine;

namespace RoofTops
{
    public class MaterialToggleController : MonoBehaviour
    {
        [Header("Material Settings")]
        public Material targetMaterial;

        [Header("Initial State")]
        public bool initialVertexColorState = false;
        public bool initialPathState = false;

        private void Start()
        {
            if (targetMaterial != null)
            {
                // Set initial state
                targetMaterial.SetFloat("_UseVertexColor", initialVertexColorState ? 1 : 0);
                targetMaterial.SetFloat("_UsePath", initialPathState ? 1 : 0);
            }
        }

        private void Update()
        {
            if (GameManager.Instance.HasGameStarted)
            {
                // Just disable the component - material will use its default values
                enabled = false;
            }
        }
    }
}