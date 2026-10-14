namespace Perigee
{
    // Магазин Мастерской: способности, облик и классы звёзд. Купленное лежит битами в сохранении.
    // С debugUnlockAll всё считается купленным, в сохранение это не пишется и пыль не тратится.
    public class Shop
    {
        readonly GameConfig config;
        readonly ISaveService save;

        public Shop(GameConfig config, ISaveService save)
        {
            this.config = config;
            this.save = save;
        }

        public bool Owned(AbilityId id)
        {
            return config.debugUnlockAll || (save.Data.abilitiesOwned & 1 << (int)id) != 0;
        }

        public bool CanBuy(AbilityDef def)
        {
            return !Owned(def.id) && save.Data.dust >= def.price;
        }

        public void Buy(AbilityDef def)
        {
            save.Data.dust -= def.price;
            save.Data.abilitiesOwned |= 1 << (int)def.id;
            save.Save();
        }

        public bool Owned(SkinId id)
        {
            return config.debugUnlockAll || (save.Data.skinsOwned & 1 << (int)id) != 0;
        }

        // за пыль продаются только облики с ценой: остальные дают победы и испытания
        public bool CanBuy(SkinDef def)
        {
            return !Owned(def.id) && def.price > 0 && save.Data.dust >= def.price;
        }

        public void Buy(SkinDef def)
        {
            save.Data.dust -= def.price;
            Give(def.id);
            save.Save();
        }

        // облик в награду; сохраняет тот, кто вызвал
        public void Give(SkinId id)
        {
            save.Data.skinsOwned |= 1 << (int)id;
        }

        public bool Selected(SkinDef def)
        {
            return Chosen(def.kind) == def.id;
        }

        // выбранный облик виден со следующего забега
        public void Select(SkinDef def)
        {
            if (def.kind == SkinKind.Satellite) save.Data.satelliteSkin = (int)def.id;
            else save.Data.starSkin = (int)def.id;
            save.Save();
        }

        // номера выбранных обликов в ArtSet.satelliteSkins и ArtSet.starSkins
        public int SatelliteSkin => ArtIndex(Chosen(SkinKind.Satellite));
        public int StarSkin => ArtIndex(Chosen(SkinKind.Star));

        // в SkinId сначала идут облики спутника, за ними облики звезды
        public static int ArtIndex(SkinId id)
        {
            return id < SkinId.Sun ? (int)id : id - SkinId.Sun;
        }

        // облик, выбранный при debugUnlockAll, без него может оказаться не купленным: тогда стоит обычный
        SkinId Chosen(SkinKind kind)
        {
            if (kind == SkinKind.Satellite)
            {
                var satellite = (SkinId)save.Data.satelliteSkin;
                return Owned(satellite) ? satellite : SkinId.Romb;
            }

            var star = (SkinId)save.Data.starSkin;
            return Owned(star) ? star : SkinId.Sun;
        }

        public bool Owned(StarClassId id)
        {
            return config.debugUnlockAll || (save.Data.starClassesOwned & 1 << (int)id) != 0;
        }

        // за пыль продаются только классы с ценой: остальные дают победы в эрах
        public bool CanBuy(StarClassDef def)
        {
            return !Owned(def.id) && def.price > 0 && save.Data.dust >= def.price;
        }

        public void Buy(StarClassDef def)
        {
            save.Data.dust -= def.price;
            Give(def.id);
            save.Save();
        }

        // класс в награду; сохраняет тот, кто вызвал
        public void Give(StarClassId id)
        {
            save.Data.starClassesOwned |= 1 << (int)id;
        }

        // класс для следующего забега; не купленный (после debugUnlockAll) заменяется Жёлтым
        public StarClassId StarClass
        {
            get
            {
                var id = (StarClassId)save.Data.lastStarClass;
                return Owned(id) ? id : StarClassId.Yellow;
            }
        }

        public void Select(StarClassId id)
        {
            save.Data.lastStarClass = (int)id;
            save.Save();
        }
    }
}
