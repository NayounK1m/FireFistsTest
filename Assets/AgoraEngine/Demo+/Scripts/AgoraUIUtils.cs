using UnityEngine;
namespace agora_utilities
{
    public class AgoraUIUtils
    {
        public static Vector2 GetScaledDimension(float width, float height, float WindowSideLength)
        {
            float newWidth = width;
            float newHeight = height;
            float ratio = (float)height / (float)width;
            if (width > height)
            {
                newHeight = WindowSideLength;
                newWidth = WindowSideLength / ratio;
            }
            else
            {
                newHeight = WindowSideLength * ratio;
                newWidth = WindowSideLength;
            }
            return new Vector2(newWidth, newHeight);
        }
        
        public static Vector2 GetRandomPosition(int i)
        {
            switch(i)
            {
                case 0:
                return new Vector2(30, 0);

                case 1:
                return new Vector2(60, 0);

                case 2:
                return new Vector2(90, 0);

                case 3:
                return new Vector2(120, 0);

                default:
                return new Vector2(0, 0);

            }
        }
    }
}
