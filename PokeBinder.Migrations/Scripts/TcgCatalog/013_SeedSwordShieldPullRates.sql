-- The Sword & Shield sets TCGplayer measured, folded into the rarity buckets this catalog
-- actually keeps.
--
-- WHY THIS ONE NEEDS ARITHMETIC AND 011 DID NOT. For the Scarlet & Violet sets, TCGplayer
-- publishes a rate per rarity and the names line up with ours one-for-one. For Sword & Shield it
-- publishes SUB-RARITIES -- Normal V, Normal VMAX, Full-Art, Alt-Art V, Rainbow, Gold, and from
-- Brilliant Stars onwards a whole Trainer Gallery -- while this catalog keeps one [Ultra Rare]
-- row and one [Secret Rare] row per set, because that is what TCGplayer's own product data calls
-- them and that is what the ETL loaded.
--
-- The catalog's structure is kept exactly as it is. What changes is the number put in it: the
-- sub-rates that fall inside a bucket are ADDED, because they are mutually exclusive outcomes of
-- the same pack slot, and the total is inverted into packs-to-pull. Every addition is written out
-- below so the result can be checked against the article rather than taken on trust.
--
-- WHICH PRINTINGS EACH BUCKET HOLDS was read off the cards themselves, not assumed:
--   * [Ultra Rare] holds V, VMAX, VSTAR, Full Arts AND "(Alternate Full Art)" V cards -- alt-art
--     Vs are an Ultra Rare in TCGplayer's product data even when their collector number is past
--     the set total -- plus every Trainer Gallery / Galarian Gallery card that is not a secret.
--   * [Secret Rare] holds everything whose product name ends in "(Secret)": the rainbows, the
--     golds, the alt-art VMAX secrets, and the gold-and-black gallery cards.
--
-- TWO VALUES ARE NOT FROM THE SOURCE, and both are deliberate calls by the catalog owner:
--   * Holo Rare at 3. No study measures the plain holo slot -- the articles only cover the tiers
--     above it -- and 3 is what every WotC set in this catalog already uses, which is also about
--     what a third of rare slots being holo works out to.
--   * Astral Radiance's Radiant Rare at 20. Its own article never measured Radiant cards even
--     though the set introduced them; 20 is Lost Origin's figure, taken as the lower of the two
--     neighbouring sets that were measured (Lost Origin 20, Silver Tempest 22).
--
-- Common, Uncommon and Rare are 1, as in 011.
--
-- ONE KNOWN IMPRECISION. In Astral Radiance the two gold-and-black gallery secrets (TG29, TG30)
-- sit in this catalog's [Secret Rare] bucket, but that article folds them into its Trainer
-- Gallery total without breaking them out, so they are counted in [Ultra Rare] here. Lost Origin
-- and Silver Tempest do break that sub-rate out and it is moved across correctly for them. The
-- error is two cards out of thirty gallery slots and it cannot be fixed without a number the
-- article does not print.
--
-- The twelve remaining Sword & Shield sets -- Sword & Shield, Rebel Clash, Darkness Ablaze,
-- Champion's Path, Vivid Voltage, Shining Fates, Battle Styles, Chilling Reign, Celebrations,
-- Fusion Strike, Brilliant Stars and Pokémon GO -- have no pull-rate article at all and stay
-- unweighted. Fusion Strike's 4,000-pack count exists only as a TCGplayer social post, not as an
-- article with a table, which is why it is not used here.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- SWSH07 Evolving Skies (8,000+ packs)
    --   Ultra Rare  = Normal V 10.56 + Normal VMAX 5.60 + Full-Art 2.78 + Alt-Art V 1.10 = 20.04% -> 1 in 5
    --   Secret Rare = Alt-Art VMAX 0.30 + Rainbow 0.84 + Gold 0.91 = 2.05% -> 1 in 49
    ('SWSH07', 'Secret Rare', 49),
    ('SWSH07', 'Ultra Rare', 5),
    ('SWSH07', 'Holo Rare', 3),
    ('SWSH07', 'Rare', 1),
    ('SWSH07', 'Uncommon', 1),
    ('SWSH07', 'Common', 1),

    -- SWSH10 Astral Radiance (8,000+ packs)
    --   Ultra Rare  = Ultra Rare 20.03 + Trainer Gallery 12.58 = 32.61% -> 1 in 3
    --   Secret Rare = 2.05% -> 1 in 49
    ('SWSH10', 'Secret Rare', 49),
    ('SWSH10', 'Radiant Rare', 20),
    ('SWSH10', 'Ultra Rare', 3),
    ('SWSH10', 'Holo Rare', 3),
    ('SWSH10', 'Rare', 1),
    ('SWSH10', 'Uncommon', 1),
    ('SWSH10', 'Common', 1),

    -- SWSH11 Lost Origin (8,000+ packs)
    --   Ultra Rare  = Ultra Rare 19.95 + TG Non-V 8.29 + TG V/VMAX/Trainer 3.17 = 31.41% -> 1 in 3
    --   Secret Rare = Secret Rare 2.05 + TG Gold-and-Black VMAX 0.86 = 2.91% -> 1 in 34
    ('SWSH11', 'Secret Rare', 34),
    ('SWSH11', 'Radiant Rare', 20),
    ('SWSH11', 'Ultra Rare', 3),
    ('SWSH11', 'Holo Rare', 3),
    ('SWSH11', 'Rare', 1),
    ('SWSH11', 'Uncommon', 1),
    ('SWSH11', 'Common', 1),

    -- SWSH12 Silver Tempest (8,000+ packs)
    --   Ultra Rare  = Ultra Rare 18.98 + TG Non-V 8.25 + TG V/VMAX/Trainer 3.08 = 30.31% -> 1 in 3
    --   Secret Rare = Secret Rare 2.20 + TG Gold-and-Black VMAX 0.90 = 3.10% -> 1 in 32
    ('SWSH12', 'Secret Rare', 32),
    ('SWSH12', 'Radiant Rare', 22),
    ('SWSH12', 'Ultra Rare', 3),
    ('SWSH12', 'Holo Rare', 3),
    ('SWSH12', 'Rare', 1),
    ('SWSH12', 'Uncommon', 1),
    ('SWSH12', 'Common', 1),

    -- SWSH-CZ Crown Zenith (1,900+ packs)
    --   Ultra Rare  = Ultra Rare 20.50 + GG Non-V 22.40 + GG V or Trainer 12.00 = 54.90% -> 1 in 2
    --   Secret Rare = Secret Rare 0.75 + GG Gold 0.80 = 1.55% -> 1 in 65
    ('SWSH-CZ', 'Secret Rare', 65),
    ('SWSH-CZ', 'Radiant Rare', 22),
    ('SWSH-CZ', 'Ultra Rare', 2),
    ('SWSH-CZ', 'Holo Rare', 3),
    ('SWSH-CZ', 'Rare', 1),
    ('SWSH-CZ', 'Uncommon', 1),
    ('SWSH-CZ', 'Common', 1)
)
UPDATE [rarityBySetFilterOption] AS target
SET [pullRateRarityOrder] = COALESCE(seed.pullRate, target.[pullRateRarityOrder])
FROM seed
JOIN [sets] ON [sets].[code] = seed.code
WHERE target.[setId] = [sets].[id]
  AND target.[rarity] = seed.rarity;

-- The browsers cache their copy of [rarityBySet] against this stamp and know nothing about the
-- values inside it, so the stamp moves with the rows, in the same script that writes them.
UPDATE [filterCacheStamps]
SET [stamp] = lower(
        substr(hex(randomblob(4)), 1, 8) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(6))
    ),
    [lastBumpedUnix] = unixepoch()
WHERE [groupName] = 'RarityBySet';
