using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;

        SpriteRenderer body;
        SpriteRenderer rays;
        float pulsePhase;

        void Awake()
        {
            body = ShapeFactory.NewRenderer("Body", transform, ShapeFactory.Circle(config.outlineWidth / config.starRadius), config.cream, config.sortStar, config.spriteMaterial);
            rays = ShapeFactory.NewRenderer("Rays", transform, ShapeFactory.Rays(config.starRayCount, config.starRaysInner / config.starRaysOuter, config.starRayFill), config.cream, config.sortStar, config.spriteMaterial);
        }

        public void Damage(float amount)
        {
            loop.Run.hp = Mathf.Max(loop.Run.hp - amount, 0f);
        }

        public void Tick()
        {
            if (loop.Run.hp <= 0f) loop.SetState(GameState.Defeat);
        }

        void Update()
        {
            body.transform.localScale = new Vector3(config.starRadius, config.starRadius, 1f);
            rays.transform.localScale = new Vector3(config.starRaysOuter, config.starRaysOuter, 1f);
            rays.transform.Rotate(0f, 0f, -config.starRaysSpin * Time.deltaTime);

            float hpFraction = loop.Run.hp / config.starMaxHp;
            bool low = hpFraction < config.starLowHpFraction;

            pulsePhase = Mathf.Repeat(pulsePhase + (low ? config.starLowHpPulseHz : config.starPulseHz) * Time.deltaTime, 1f);
            float pulse = Mathf.Lerp(1f, config.starPulseScale, 0.5f - 0.5f * Mathf.Cos(pulsePhase * 2f * Mathf.PI));
            transform.localScale = new Vector3(pulse, pulse, 1f);

            // ниже порога звезда краснеет тем сильнее, чем меньше HP
            Color tint = low ? Color.Lerp(config.red, config.cream, hpFraction / config.starLowHpFraction) : config.cream;
            body.color = tint;
            rays.color = tint;
        }
    }
}
