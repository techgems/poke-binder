-- The Sun & Moon era, weighted from community counts.
--
-- WEAKER EVIDENCE THAN 011 AND 013, AND IN ITS OWN SCRIPT FOR THAT REASON. TCGplayer's
-- Authentication Center series does not reach back this far -- it starts at Evolving Skies -- so
-- there is no measured study with a sample size and a confidence interval for any set below. The
-- figures are ThePriceDex's per-set pull-rate tables, which describe themselves as "estimates
-- primarily sourced from <set> pull rates research and based on community data" and state no
-- sample size. Used on instruction that community counts are acceptable here. One source per set
-- with nothing to cross-check against, so read every number below as approximate.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS the same way 013 and 014 do it, with the membership read off
-- the cards themselves:
--   * [Ultra Rare] holds the GX cards and the Full Arts, so it is "Rare Holo GX" + "Ultra Rare"
--     added together -- and in Cosmic Eclipse the "Character Rare" alt arts as well, which the
--     catalog files there too.
--   * [Secret Rare] holds the gold and rainbow secrets. Where a set keeps a separate
--     [Rainbow Rare] row -- Burning Shadows, Celestial Storm, Cosmic Eclipse -- the rainbow rate
--     goes there and [Secret Rare] takes the gold rate alone. Everywhere else the two are added.
--   * [Prism Rare] and [Shiny Holo Rare] map one-for-one where the set has them.
--   * [Holo Rare] is this era's one improvement on the Sword & Shield scripts: ThePriceDex
--     publishes a "Rare Holo" rate, so it is a real figure here rather than the 3 that 013 had to
--     assume.
--
-- Common, Uncommon and Rare are 1, as everywhere else. The source agrees -- it gives Rare as
-- 1 in 1.5 and commons and uncommons as several per pack.
--
-- TWO SETS ARE LEFT UNWEIGHTED. Detective Pikachu was never a booster product, so it has no pull
-- rate to find and ThePriceDex has no page for it; SM: Promos is promos. Neither is an omission.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- SM01 Sun & Moon: GX 1/9, FA 1/23.1, secret 1/100, rainbow 1/75, holo 1/6.4
    ('SM01', 'Secret Rare', 43),
    ('SM01', 'Ultra Rare', 6),
    ('SM01', 'Holo Rare', 6),
    ('SM01', 'Rare', 1),
    ('SM01', 'Uncommon', 1),
    ('SM01', 'Common', 1),
    -- SM02 Guardians Rising: GX 1/9, FA 1/23.4, secret 1/111.1, rainbow 1/66.7, holo 1/6.4
    ('SM02', 'Secret Rare', 42),
    ('SM02', 'Ultra Rare', 6),
    ('SM02', 'Holo Rare', 6),
    ('SM02', 'Rare', 1),
    ('SM02', 'Uncommon', 1),
    ('SM02', 'Common', 1),
    -- SM03 Burning Shadows: GX 1/9, FA 1/23.3, secret 1/102.6, rainbow 1/71.0, holo 1/6.4
    ('SM03', 'Secret Rare', 103),
    ('SM03', 'Rainbow Rare', 71),
    ('SM03', 'Ultra Rare', 6),
    ('SM03', 'Holo Rare', 6),
    ('SM03', 'Rare', 1),
    ('SM03', 'Uncommon', 1),
    ('SM03', 'Common', 1),
    -- SM-SL Shining Legends: GX 1/9, FA 1/24.2, secret 1/197.8, rainbow 1/49.5, shining 1/11.3, holo 1/1.4
    ('SM-SL', 'Secret Rare', 40),
    ('SM-SL', 'Shiny Holo Rare', 11),
    ('SM-SL', 'Ultra Rare', 7),
    ('SM-SL', 'Holo Rare', 1),
    ('SM-SL', 'Uncommon', 1),
    ('SM-SL', 'Common', 1),
    -- SM04 Crimson Invasion: GX 1/12, FA 1/22.2, secret 1/120, rainbow 1/75, holo 1/5.5
    ('SM04', 'Secret Rare', 46),
    ('SM04', 'Ultra Rare', 8),
    ('SM04', 'Holo Rare', 6),
    ('SM04', 'Rare', 1),
    ('SM04', 'Uncommon', 1),
    ('SM04', 'Common', 1),
    -- SM05 Ultra Prism: GX 1/12, FA 1/22.7, secret 1/93.8, rainbow 1/83.3, prism 1/12, holo 1/5.5
    ('SM05', 'Secret Rare', 44),
    ('SM05', 'Prism Rare', 12),
    ('SM05', 'Ultra Rare', 8),
    ('SM05', 'Holo Rare', 6),
    ('SM05', 'Rare', 1),
    ('SM05', 'Uncommon', 1),
    ('SM05', 'Common', 1),
    -- SM06 Forbidden Light: GX 1/12, FA 1/24.3, secret 1/125, rainbow 1/83.3, prism 1/12, holo 1/5.3
    ('SM06', 'Secret Rare', 50),
    ('SM06', 'Prism Rare', 12),
    ('SM06', 'Ultra Rare', 8),
    ('SM06', 'Holo Rare', 5),
    ('SM06', 'Rare', 1),
    ('SM06', 'Uncommon', 1),
    ('SM06', 'Common', 1),
    -- SM07 Celestial Storm: GX 1/9, FA 1/24.5, secret 1/123.6, rainbow 1/82.4, prism 1/18, holo 1/6.2
    ('SM07', 'Secret Rare', 124),
    ('SM07', 'Rainbow Rare', 82),
    ('SM07', 'Prism Rare', 18),
    ('SM07', 'Ultra Rare', 7),
    ('SM07', 'Holo Rare', 6),
    ('SM07', 'Rare', 1),
    ('SM07', 'Uncommon', 1),
    ('SM07', 'Common', 1),
    -- SM-DM Dragon Majesty: GX 1/6.7, FA 1/17.4, secret 1/46.9, rainbow 1/46.9, prism 1/10, holo 1/1.3
    ('SM-DM', 'Secret Rare', 23),
    ('SM-DM', 'Prism Rare', 10),
    ('SM-DM', 'Ultra Rare', 5),
    ('SM-DM', 'Holo Rare', 1),
    ('SM-DM', 'Uncommon', 1),
    ('SM-DM', 'Common', 1),
    -- SM08 Lost Thunder: GX 1/10, FA 1/23.3, secret 1/102.6, rainbow 1/71.0, prism 1/8.2, holo 1/6.0
    ('SM08', 'Secret Rare', 42),
    ('SM08', 'Prism Rare', 8),
    ('SM08', 'Ultra Rare', 7),
    ('SM08', 'Holo Rare', 6),
    ('SM08', 'Rare', 1),
    ('SM08', 'Uncommon', 1),
    ('SM08', 'Common', 1),
    -- SM09 Team Up: GX 1/10, FA 1/24, secret 1/120, rainbow 1/60, prism 1/18, holo 1/6.0
    ('SM09', 'Secret Rare', 40),
    ('SM09', 'Prism Rare', 18),
    ('SM09', 'Ultra Rare', 7),
    ('SM09', 'Holo Rare', 6),
    ('SM09', 'Rare', 1),
    ('SM09', 'Uncommon', 1),
    ('SM09', 'Common', 1),
    -- SM10 Unbroken Bonds: GX 1/10, FA 1/24.2, secret 1/131.9, rainbow 1/56.5, holo 1/6.0
    ('SM10', 'Secret Rare', 40),
    ('SM10', 'Ultra Rare', 7),
    ('SM10', 'Holo Rare', 6),
    ('SM10', 'Rare', 1),
    ('SM10', 'Uncommon', 1),
    ('SM10', 'Common', 1),
    -- SM11 Unified Minds: GX 1/7.8, FA 1/22.4, secret 1/111.1, rainbow 1/76.9, holo 1/7.2
    ('SM11', 'Secret Rare', 45),
    ('SM11', 'Ultra Rare', 6),
    ('SM11', 'Holo Rare', 7),
    ('SM11', 'Rare', 1),
    ('SM11', 'Uncommon', 1),
    ('SM11', 'Common', 1),
    -- SM-HF Hidden Fates: GX 1/6.7, FA 1/25.6, rainbow 1/90.9, holo 1/5.0
    ('SM-HF', 'Secret Rare', 91),
    ('SM-HF', 'Ultra Rare', 5),
    ('SM-HF', 'Holo Rare', 5),
    ('SM-HF', 'Rare', 1),
    ('SM-HF', 'Uncommon', 1),
    ('SM-HF', 'Common', 1),
    -- SM12 Cosmic Eclipse: GX 1/7.8, FA 1/26.2, character rare 1/10, secret 1/111.1, rainbow 1/71.4, holo 1/6.9
    ('SM12', 'Secret Rare', 111),
    ('SM12', 'Rainbow Rare', 71),
    ('SM12', 'Ultra Rare', 4),
    ('SM12', 'Holo Rare', 7),
    ('SM12', 'Rare', 1),
    ('SM12', 'Uncommon', 1),
    ('SM12', 'Common', 1)
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
