using UnityEngine;

namespace Perigee
{
    // Сектор вспышки Красного карлика: объект сцены. Картинка, цвет и размер заданы в инспекторе,
    // код поворачивает сектор, меняет его прозрачность и включает лучи.
    public class FlareSector : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] Visual visual;
        // лучи внутри сектора: видны только во время удара; что лежит внутри, решает владелец
        [SerializeField] GameObject rays;

        // предупреждение: бледный мигающий сектор. centre = середина сектора в градусах, age = секунды с начала предупреждения
        public void Warn(float centre, float age)
        {
            // Visual запоминает цвет при включении, поэтому сначала объект, потом прозрачность
            gameObject.SetActive(true);
            rays.SetActive(false);
            // середина картинки сектора смотрит по оси +Y
            transform.localRotation = Quaternion.Euler(0f, 0f, centre - 90f);
            visual.SetAlpha(config.flareWarnAlpha * (0.5f + 0.5f * Mathf.Cos(age * config.flareWarnHz * 2f * Mathf.PI)));
        }

        // удар: сектор в полную силу, с лучами
        public void Burst()
        {
            rays.SetActive(true);
            visual.SetAlpha(1f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
