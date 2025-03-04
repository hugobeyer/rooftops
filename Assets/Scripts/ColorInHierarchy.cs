using UnityEngine;

public class ColorInHierarchy : MonoBehaviour
{
    public Color color = Color.white;

    public void SetColor(float r, float g, float b)
    {
        color = new Color(r, g, b);
    }

    public void SetColor(float r, float g, float b, float a)
    {
        color = new Color(r, g, b, a);
    }
} 