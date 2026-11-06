using UnityEngine;

namespace CardHome
{
    // Точки домов кампании на фоне Art/Map/Campaign(Wide).png — доли от размера картинки:
    // x слева направо, y снизу вверх (как anchorMin/anchorMax в UI). Индекс = номер дома в HouseDatabase.All.
    public static class CampaignPoints
    {
        public static readonly Vector2[] Portrait =
        {
            new Vector2(0.4463f, 0.2195f),
            new Vector2(0.1732f, 0.2781f),
            new Vector2(0.4155f, 0.3535f),
            new Vector2(0.6939f, 0.4175f),
            new Vector2(0.5696f, 0.5167f),
            new Vector2(0.3464f, 0.5819f),
            new Vector2(0.1286f, 0.6818f),
            new Vector2(0.3932f, 0.756f),
            new Vector2(0.661f, 0.7195f),
            new Vector2(0.7566f, 0.808f)
        };

        public static readonly Vector2[] Landscape =
        {
            new Vector2(0.3953f, 0.2104f),
            new Vector2(0.2793f, 0.3124f),
            new Vector2(0.3254f, 0.4495f),
            new Vector2(0.2231f, 0.542f),
            new Vector2(0.241f, 0.6642f),
            new Vector2(0.3714f, 0.7556f),
            new Vector2(0.4629f, 0.6504f),
            new Vector2(0.5987f, 0.6557f),
            new Vector2(0.7171f, 0.678f),
            new Vector2(0.8248f, 0.763f)
        };

        public static Vector2[] For(bool portrait) => portrait ? Portrait : Landscape;
    }
}
