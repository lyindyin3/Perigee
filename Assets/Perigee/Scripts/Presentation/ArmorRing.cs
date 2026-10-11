using UnityEngine;

namespace Perigee
{
    // Серебряное кольцо на радиусе, где внутренний спутник бьёт ровно на armorBase:
    // ближе к звезде удар пробивает броню, дальше нет.
    public class ArmorRing : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;

        SpriteRenderer ring;
        SpriteRenderer shield;

        void Awake()
        {
            Color color = config.silver;
            color.a = config.armorRingAlpha;
            ring = ShapeFactory.NewRenderer("Ring", transform, ShapeFactory.Ring(config.armorRingTicks, config.armorRingWidth, config.ringDashFill), color, config.sortRings, config.spriteMaterial);
            // клин вершиной вниз читается как щит
            shield = ShapeFactory.NewRenderer("Shield", transform, ShapeFactory.Wedge(), config.silver, config.sortRings, config.spriteMaterial);
        }

        void Update()
        {
            // если удар пробивает броню и вдали, кольцо не нужно
            bool visible = satellites.Power(config.powerFarRadius) < config.armorBase;
            ring.enabled = visible;
            shield.enabled = visible;

            float far = config.powerFar + loop.Stats.powerFarBonus;
            float t = (config.powerNear - config.armorBase / loop.Stats.powerMul) / (config.powerNear - far);
            float r = config.rMin + Mathf.Clamp01(t) * (config.powerFarRadius - config.rMin);

            ring.transform.localScale = new Vector3(r, r, 1f);
            ring.transform.Rotate(0f, 0f, config.armorRingSpin * Time.deltaTime);
            shield.transform.localPosition = new Vector3(0f, r, 0f);
            shield.transform.localScale = new Vector3(config.armorShieldSize, config.armorShieldSize, 1f);
        }
    }
}
