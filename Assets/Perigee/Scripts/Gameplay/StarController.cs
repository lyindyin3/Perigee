using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        Transform body;
        Transform rays;
        float pulsePhase;

        void Awake()
        {
            body = ShapeFactory.NewRenderer("Body", transform, ShapeFactory.Circle(config.outlineWidth / config.starRadius), config.cream, config.sortStar, config.spriteMaterial).transform;
            rays = ShapeFactory.NewRenderer("Rays", transform, ShapeFactory.Rays(config.starRayCount, config.starRaysInner / config.starRaysOuter, config.starRayFill), config.cream, config.sortStar, config.spriteMaterial).transform;
        }

        void Update()
        {
            body.localScale = new Vector3(config.starRadius, config.starRadius, 1f);
            rays.localScale = new Vector3(config.starRaysOuter, config.starRaysOuter, 1f);
            rays.Rotate(0f, 0f, -config.starRaysSpin * Time.deltaTime);

            pulsePhase = Mathf.Repeat(pulsePhase + config.starPulseHz * Time.deltaTime, 1f);
            float pulse = Mathf.Lerp(1f, config.starPulseScale, 0.5f - 0.5f * Mathf.Cos(pulsePhase * 2f * Mathf.PI));
            transform.localScale = new Vector3(pulse, pulse, 1f);
        }
    }
}
