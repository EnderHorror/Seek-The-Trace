using UnityEngine;

namespace DefaultNamespace
{
    public class MathUtility
    {
        public static Vector2 Tangent(Vector2 dir)
        {
            return new Vector2(dir.y/dir.x,1);
        }
    }
}