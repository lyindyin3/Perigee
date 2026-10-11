using System;
using System.Collections.Generic;
using UnityEngine;

namespace Perigee
{
    // Все формы рисуются в квадрате [-1..1]: единичный радиус = половина текстуры,
    // поэтому форма радиуса r получается через localScale = (r, r, 1).
    public static class ShapeFactory
    {
        const int Size = 512;
        const float Half = Size / 2f; // он же PPU
        const float Tau = Mathf.PI * 2f;

        static readonly float Sqrt3 = Mathf.Sqrt(3f);
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // hollow: толщина стенки в долях радиуса, 0 = сплошная форма
        public static Sprite Circle(float outline = 0f, float hollow = 0f)
        {
            return Build($"Circle_{outline}_{hollow}", outline, (x, y) => Hollow(Mathf.Sqrt(x * x + y * y) - 1f, hollow));
        }

        // Пунктирное кольцо, внешний край на радиусе 1. width в долях радиуса, fill в долях периода штриха.
        public static Sprite Ring(int tickCount, float width, float fill)
        {
            return Build($"Ring_{tickCount}_{width}_{fill}", 0f, (x, y) =>
            {
                float rho = Mathf.Sqrt(x * x + y * y);
                float period = Tau / tickCount;
                float off = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(y, x) / period + 0.5f, 1f) - 0.5f);
                return Mathf.Max(Band(rho, width), (off - fill * 0.5f) * period * rho);
            });
        }

        // Дуга от верхней точки по часовой стрелке.
        public static Sprite Arc(float degrees, float width)
        {
            return Build($"Arc_{degrees}_{width}", 0f, (x, y) =>
            {
                float rho = Mathf.Sqrt(x * x + y * y);
                float half = degrees * Mathf.Deg2Rad * 0.5f;
                // угловое расстояние до середины дуги
                float off = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(x, y) - half + Mathf.PI, Tau) - Mathf.PI);
                return Mathf.Max(Band(rho, width), (off - half) * rho);
            });
        }

        public static Sprite Triangle(float outline = 0f)
        {
            return Build($"Triangle_{outline}", outline, (x, y) => Polygon(x, y, 3));
        }

        public static Sprite Diamond(float outline = 0f, float hollow = 0f)
        {
            return Build($"Diamond_{outline}_{hollow}", outline, (x, y) => Hollow(Polygon(x, y, 4), hollow));
        }

        public static Sprite Hexagon(float outline = 0f)
        {
            return Build($"Hexagon_{outline}", outline, (x, y) => Polygon(x, y, 6));
        }

        // Сектор 60° с вершиной в (0, -1), вписанный в единичную окружность.
        public static Sprite Wedge(float outline = 0f, float hollow = 0f)
        {
            return Build($"Wedge_{outline}_{hollow}", outline, (x, y) => Hollow(WedgeDistance(x, y), hollow));
        }

        // Тело Бастиона: клин, ужатый на inset (место под серебряную обводку), с тремя чёрными линиями заклёпок.
        public static Sprite Bastion(float inset, float rivetLength, float rivetWidth, float rivetStep)
        {
            return Build($"Bastion_{inset}_{rivetLength}_{rivetWidth}_{rivetStep}", 0f, (x, y) => WedgeDistance(x, y) + inset, (x, y) =>
            {
                // расстояние по вертикали до ближайшей из трёх линий: y = -step, 0, +step
                float dy = Mathf.Abs(y - Mathf.Clamp(Mathf.Round(y / rivetStep), -1f, 1f) * rivetStep);
                return Mathf.Max(Mathf.Abs(x) - rivetLength, dy - rivetWidth);
            });
        }

        // Шестиугольник с тремя чёрными шестиугольными дырами.
        public static Sprite Hive(float holeRadius, float holeOffset)
        {
            return Build($"Hive_{holeRadius}_{holeOffset}", 0f, (x, y) => Polygon(x, y, 6), (x, y) =>
            {
                float d = float.MaxValue;
                for (int k = 0; k < 3; k++)
                {
                    float a = k * Tau / 3f;
                    float hx = (x - Mathf.Sin(a) * holeOffset) / holeRadius;
                    float hy = (y - Mathf.Cos(a) * holeOffset) / holeRadius;
                    d = Mathf.Min(d, Polygon(hx, hy, 6) * holeRadius);
                }
                return d;
            });
        }

        // Равнобедренный треугольник во весь квадрат: вершина в (0, 1), основание на y = -1.
        public static Sprite Spike()
        {
            return Build("Spike", 0f, (x, y) => Mathf.Max((2f * Mathf.Abs(x) + y - 1f) / Mathf.Sqrt(5f), -1f - y));
        }

        // Вертикальные полосы во весь квадрат. fill: доля периода, которую занимает полоса.
        public static Sprite Stripes(int count, float fill)
        {
            return Build($"Stripes_{count}_{fill}", 0f, (x, y) =>
            {
                float period = 2f / count;
                float off = Mathf.Abs(Mathf.Repeat((x + 1f) / period, 1f) - 0.5f);
                return Mathf.Max((off - fill * 0.5f) * period, Mathf.Abs(y) - 1f);
            });
        }

        // Лучи-клинья от радиуса inner до 1. fill: доля периода, которую луч занимает у основания.
        public static Sprite Rays(int count, float inner, float fill)
        {
            return Build($"Rays_{count}_{inner}_{fill}", 0f, (x, y) =>
            {
                float rho = Mathf.Sqrt(x * x + y * y);
                float period = Tau / count;
                float off = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(y, x) / period + 0.5f, 1f) - 0.5f);
                float taper = 1f - Mathf.InverseLerp(inner, 1f, rho);
                return Mathf.Max(Mathf.Max(inner - rho, rho - 1f), (off - fill * 0.5f * taper) * period * rho);
            });
        }

        public static Sprite Rect()
        {
            return Build("Rect", 0f, (x, y) => Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) - 1f);
        }

        public static SpriteRenderer NewRenderer(string name, Transform parent, Sprite sprite, Color color, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        static float Hollow(float d, float width)
        {
            return width > 0f ? Mathf.Max(d, -width - d) : d;
        }

        static float WedgeDistance(float x, float y)
        {
            float qy = y + 1f;
            float arc = Mathf.Sqrt(x * x + qy * qy) - Sqrt3;
            float side = Mathf.Abs(x) * Sqrt3 * 0.5f - qy * 0.5f;
            return Mathf.Max(arc, side);
        }

        static float Band(float rho, float width)
        {
            return Mathf.Max(rho - 1f, 1f - width - rho);
        }

        // Правильный n-угольник, вписанный в единичную окружность, первая вершина сверху.
        static float Polygon(float x, float y, int n)
        {
            float sector = Tau / n;
            // угол от верхней вершины, свёрнутый в сектор одной стороны
            float a = Mathf.Repeat(Mathf.Atan2(x, y), sector) - sector * 0.5f;
            return Mathf.Sqrt(x * x + y * y) * Mathf.Cos(a) - Mathf.Cos(sector * 0.5f);
        }

        // distance: расстояние до края формы в единицах радиуса, внутри формы оно отрицательное.
        // ink: расстояние до чёрного рисунка поверх формы, внутри рисунка оно отрицательное.
        static Sprite Build(string key, float outline, Func<float, float, float> distance, Func<float, float, float> ink = null)
        {
            if (cache.TryGetValue(key, out Sprite sprite)) return sprite;

            var pixels = new Color32[Size * Size];
            for (int py = 0; py < Size; py++)
            {
                float y = (py + 0.5f - Half) / Half;
                for (int px = 0; px < Size; px++)
                {
                    float x = (px + 0.5f - Half) / Half;
                    float d = distance(x, y) * Half;
                    byte alpha = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    // обводка: чёрная полоса шириной outline внутри края, тинт спрайта её не перекрашивает
                    byte white = outline > 0f ? (byte)(Mathf.Clamp01(0.5f - (d + outline * Half)) * 255f) : (byte)255;
                    if (ink != null) white = (byte)(Mathf.Clamp01(0.5f + ink(x, y) * Half) * 255f);
                    pixels[py * Size + px] = new Color32(white, white, white, alpha);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            sprite = Sprite.Create(texture, new UnityEngine.Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Half, 0, SpriteMeshType.FullRect);
            cache.Add(key, sprite);
            return sprite;
        }
    }
}
