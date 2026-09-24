-- The eleven Sword & Shield sets TCGplayer never measured, weighted from ThePriceDex instead.
--
-- 013 and 014 covered the six sets with a measured source -- Evolving Skies, Astral Radiance, Lost
-- Origin, Silver Tempest, Crown Zenith and Fusion Strike. These are the rest, and they come from
-- the same community-estimate source as 015 through 019, with the same caveats: no sample sizes,
-- one source per set. Where a set appears in both this script and a measured one, the measured
-- values were written first and COALESCE leaves them alone -- nothing here overwrites 013 or 014.
--
-- EVERY PAGE WAS CHECKED BEFORE USE against the constraint that a pack holds one rare-slot card:
-- the main-slot rarities have to sum to about one card per pack. All eleven pass (0.98 to 1.01).
-- Brilliant Stars sums to 1.13 because its Trainer Gallery is a second slot, which is the
-- expected shape rather than an error. This is the test that caught Legendary Treasures in 020.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS, membership read off the cards themselves:
--   * [Ultra Rare] holds V, VMAX, VSTAR and the Full Arts, so those rates are added -- and in
--     Brilliant Stars the whole Trainer Gallery as well, all thirty TG cards being filed there,
--     which is why it reads 3 where its neighbours read 5 or 6.
--   * [Secret Rare] holds the golds and the rainbows, so those two rates are added.
--   * [Amazing Rare], [Radiant Rare] and [Classic Collection] map one-for-one.
--   * [Shiny Holo Rare] in Shining Fates is the 122-card Shiny Vault, so the shiny, shiny V and
--     shiny VMAX rates are added into it.
--
-- TWO SETS NEED A WORD:
--   * Shining Fates' [Secret Rare] holds exactly one card, Alcremie VMAX (Secret), so it takes
--     the rainbow rate rather than the source's "SV Secret Rare", which describes Shiny Vault
--     cards this row does not contain.
--   * Celebrations has no Common, Uncommon or Rare rows at all -- it is a 25-card anniversary set
--     -- and its [Holo Rare] row gets 1 because the source gives holos as 3.1 cards per pack
--     rather than as odds. Its [Ultra Rare] lands at 2, the most generous in the catalog, which
--     is correct for a set whose packs are half V cards.
--
-- Common, Uncommon and Rare are 1 wherever those rows exist. The [Promo] rows in Celebrations and
-- Brilliant Stars are left null, as promo rows are everywhere else.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- SWSH01 Sword & Shield: UR = V 1/7.0 + VMAX 1/45.5 + full art 1/26.7 -> 5 ; Secret = secret 1/109.9 + rainbow 1/81.3 -> 47 ; Holo 1/5.5 -> 6
    ('SWSH01', 'Secret Rare', 47),
    ('SWSH01', 'Ultra Rare', 5),
    ('SWSH01', 'Holo Rare', 6),
    ('SWSH01', 'Rare', 1),
    ('SWSH01', 'Uncommon', 1),
    ('SWSH01', 'Common', 1),
    -- SWSH02 Rebel Clash: UR = V 1/7.9 + VMAX 1/29.4 + full art 1/26.6 -> 5 ; Secret = secret 1/105.3 + rainbow 1/66.7 -> 41 ; Holo 1/5.7 -> 6
    ('SWSH02', 'Secret Rare', 41),
    ('SWSH02', 'Ultra Rare', 5),
    ('SWSH02', 'Holo Rare', 6),
    ('SWSH02', 'Rare', 1),
    ('SWSH02', 'Uncommon', 1),
    ('SWSH02', 'Common', 1),
    -- SWSH03 Darkness Ablaze: UR = V 1/7.9 + VMAX 1/26.0 + full art 1/26.0 -> 5 ; Secret = secret 1/114.9 + rainbow 1/84.0 -> 49 ; Holo 1/5.9 -> 6
    ('SWSH03', 'Secret Rare', 49),
    ('SWSH03', 'Ultra Rare', 5),
    ('SWSH03', 'Holo Rare', 6),
    ('SWSH03', 'Rare', 1),
    ('SWSH03', 'Uncommon', 1),
    ('SWSH03', 'Common', 1),
    -- SWSH-CP Champion's Path: UR = V 1/6.4 + VMAX 1/28.2 + full art 1/18.6 -> 4 ; Secret = secret 1/75.8 + rainbow 1/63.7 -> 35 ; Holo 1/1.4 -> 1
    ('SWSH-CP', 'Secret Rare', 35),
    ('SWSH-CP', 'Ultra Rare', 4),
    ('SWSH-CP', 'Holo Rare', 1),
    ('SWSH-CP', 'Uncommon', 1),
    ('SWSH-CP', 'Common', 1),
    -- SWSH04 Vivid Voltage: UR = V 1/7.9 + VMAX 1/23.3 + full art 1/25.2 -> 5 ; Secret = secret 1/90.1 + rainbow 1/78.7 -> 42 ; Holo 1/4.6 -> 5 ; Amazing Rare 1/17.5 -> 18
    ('SWSH04', 'Secret Rare', 42),
    ('SWSH04', 'Amazing Rare', 18),
    ('SWSH04', 'Ultra Rare', 5),
    ('SWSH04', 'Holo Rare', 5),
    ('SWSH04', 'Rare', 1),
    ('SWSH04', 'Uncommon', 1),
    ('SWSH04', 'Common', 1),
    -- SWSH-SF Shining Fates: UR = V 1/9.2 + VMAX 1/18.4 + full art 1/30.8 -> 5 ; Secret = rainbow 1/84.0 -> 84 ; Holo 1/5.6 -> 6 ; Shiny = shiny 1/4.4 + shiny V 1/19.8 + shiny VMAX 1/25.5 -> 3 ; Amazing Rare 1/17.4 -> 17
    ('SWSH-SF', 'Secret Rare', 84),
    ('SWSH-SF', 'Shiny Holo Rare', 3),
    ('SWSH-SF', 'Amazing Rare', 17),
    ('SWSH-SF', 'Ultra Rare', 5),
    ('SWSH-SF', 'Holo Rare', 6),
    ('SWSH-SF', 'Rare', 1),
    ('SWSH-SF', 'Uncommon', 1),
    ('SWSH-SF', 'Common', 1),
    -- SWSH05 Battle Styles: UR = V 1/12.4 + VMAX 1/24.8 + full art 1/27.4 -> 6 ; Secret = secret 1/117 + rainbow 1/93.6 -> 52 ; Holo 1/5.6 -> 6
    ('SWSH05', 'Secret Rare', 52),
    ('SWSH05', 'Ultra Rare', 6),
    ('SWSH05', 'Holo Rare', 6),
    ('SWSH05', 'Rare', 1),
    ('SWSH05', 'Uncommon', 1),
    ('SWSH05', 'Common', 1),
    -- SWSH06 Chilling Reign: UR = V 1/12.7 + VMAX 1/23.7 + full art 1/25.0 -> 6 ; Secret = secret 1/100 + rainbow 1/96.2 -> 49 ; Holo 1/5.6 -> 6
    ('SWSH06', 'Secret Rare', 49),
    ('SWSH06', 'Ultra Rare', 6),
    ('SWSH06', 'Holo Rare', 6),
    ('SWSH06', 'Rare', 1),
    ('SWSH06', 'Uncommon', 1),
    ('SWSH06', 'Common', 1),
    -- SWSH-CEL25 Celebrations: UR = V 1/2.8 + VMAX 1/13 + full art 1/25 -> 2 ; Secret = secret 1/150 -> 150 ; Holo 3.1 cards per pack -> 1 ; Classic Collection 1/2.5 -> 3
    ('SWSH-CEL25', 'Secret Rare', 150),
    ('SWSH-CEL25', 'Classic Collection', 3),
    ('SWSH-CEL25', 'Ultra Rare', 2),
    ('SWSH-CEL25', 'Holo Rare', 1),
    -- SWSH09 Brilliant Stars: UR = V 1/7 + VSTAR 1/43 + VMAX 1/96 + full art 1/24.3 + TG holo 1/11.4 + TG V 1/81.4 + TG VMAX 1/97.6 + TG UR 1/97.6 + TG secret 1/109.1 -> 3 ; Secret = secret 1/117 + rainbow 1/68 -> 43 ; Holo 1/5.7 -> 6
    ('SWSH09', 'Secret Rare', 43),
    ('SWSH09', 'Ultra Rare', 3),
    ('SWSH09', 'Holo Rare', 6),
    ('SWSH09', 'Rare', 1),
    ('SWSH09', 'Uncommon', 1),
    ('SWSH09', 'Common', 1),
    -- SWSH-PGO Pokémon GO: UR = V 1/6 + VSTAR 1/40 + VMAX 1/53 + full art 1/19.4 -> 4 ; Secret = secret 1/63 + rainbow 1/38.7 -> 24 ; Holo 1/1.4 -> 1 ; Radiant Rare 1/17 -> 17
    ('SWSH-PGO', 'Secret Rare', 24),
    ('SWSH-PGO', 'Radiant Rare', 17),
    ('SWSH-PGO', 'Ultra Rare', 4),
    ('SWSH-PGO', 'Holo Rare', 1),
    ('SWSH-PGO', 'Uncommon', 1),
    ('SWSH-PGO', 'Common', 1)
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
