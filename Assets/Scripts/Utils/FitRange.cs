public static class FitRange
{
    public enum FitType
    {
        MinMax,    // Use full range
        Average,   // Center around average
        Center     // Center around midpoint
    }

    public static float Fit(float value, float oldMin, float oldMax, float newMin, float newMax, FitType fitType = FitType.MinMax)
    {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;
        
        switch (fitType)
        {
            case FitType.MinMax:
                return (((value - oldMin) * newRange) / oldRange) + newMin;
            
            case FitType.Average:
                float avg = (oldMin + oldMax) * 0.5f;
                return ((value - avg) * newRange / oldRange) + ((newMin + newMax) * 0.5f);
            
            case FitType.Center:
                return ((value - 0.5f) * newRange) + ((newMin + newMax) * 0.5f);
            
            default:
                return value;
        }
    }
} 