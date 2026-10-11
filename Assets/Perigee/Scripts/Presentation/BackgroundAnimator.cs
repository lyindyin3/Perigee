using UnityEngine;

namespace Perigee
{
    public class BackgroundAnimator : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Transform ringsRoot;

        SpriteRenderer[] rings;

        void Awake()
        {
            Color color = config.cream;
            color.a = config.ringAlpha;

            rings = new SpriteRenderer[config.rings.Length];
            for (int i = 0; i < rings.Length; i++)
            {
                RingDef def = config.rings[i];
                // толщина задаётся в мировых единицах, поэтому в текстуре она делится на радиус кольца
                Sprite sprite = ShapeFactory.Ring(def.tickCount, config.ringWidth / def.radius, config.ringDashFill);
                rings[i] = ShapeFactory.NewRenderer("Ring" + i, ringsRoot, sprite, color, config.sortRings, config.spriteMaterial);
                rings[i].transform.localScale = new Vector3(def.radius, def.radius, 1f);
            }
        }
    }
}
