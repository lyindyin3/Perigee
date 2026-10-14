using UnityEngine;

namespace Perigee
{
    // Серия убийств: каждое убийство продлевает окно, пустое окно или удар по звезде её обрывают. Ступень серии умножает опыт и пыль за кристалл.
    public class ComboSystem : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] AudioManager sfx;

        public int Combo { get; private set; }
        // секунды до обрыва
        public float Timer { get; private set; }
        // ступень с единицы по config.comboTiers; 0 = ниже первого порога
        public int Tier { get; private set; }

        // окно серии; ЛАВРОВЫЙ ВЕНОК его удлиняет
        public float Window => config.comboWindow + loop.Stats.Sum(StatId.ComboWindow);

        public float Mul => Tier == 0 ? 1f : config.comboMuls[Tier - 1];

        public void ResetRun()
        {
            Combo = 0;
            Timer = 0f;
            Tier = 0;
        }

        // любое убийство, кроме босса и его тел
        public void Kill()
        {
            Combo++;
            Timer = Window;
            if (Combo > loop.Run.bestCombo) loop.Run.bestCombo = Combo;

            int[] tiers = config.comboTiers;
            if (Tier < tiers.Length && Combo >= tiers[Tier])
            {
                Tier++;
                sfx.Play(SfxId.ComboTier);
            }
        }

        public void Tick(float dt)
        {
            if (Combo == 0) return;
            Timer -= dt;
            if (Timer <= 0f) Break();
        }

        // серия кончилась: окно истекло, звезда получила удар или забег закончился
        public void Break()
        {
            if (Combo == 0) return;

            if (Tier > 0) sfx.Play(SfxId.ComboBreak);
            // лучшая серия сохранения пишется при обрыве, а не на каждое убийство
            SaveData data = loop.Save.Data;
            if (Combo > data.bestCombo)
            {
                data.bestCombo = Combo;
                loop.Save.Save();
            }
            Combo = 0;
            Timer = 0f;
            Tier = 0;
        }
    }
}
