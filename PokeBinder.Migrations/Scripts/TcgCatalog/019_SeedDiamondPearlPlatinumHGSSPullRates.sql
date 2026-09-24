-- Diamond & Pearl, Platinum and HeartGold & SoulSilver, weighted from community counts --
-- ThePriceDex again, with the same caveats as 015 through 018: self-described estimates from
-- community data, no sample sizes, one source per set.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS, membership read off the cards themselves. Three eras with
-- three different ideas of what the top rarity is, and the catalog keeps one [Ultra Rare] row for
-- all of them:
--   * Diamond & Pearl and Platinum: the Lv.X cards.
--   * Rising Rivals also files its five Rotom forms in [Ultra Rare] beside the nine Lv.X, so that
--     set is Lv.X + Rotom added -- which is why it reads 9 where the sets around it read 18.
--   * HeartGold & SoulSilver: the Primes and the LEGEND halves together, so Prime + LEGEND added.
--     Each of those sets also files one Alph Lithograph in the same row; no source rates it, and
--     it is a small enough part of the row to leave the total as it stands.
--
-- TWO SETS FOLD SOMETHING INTO [Holo Rare] and read far lower than their neighbours as a result:
--   * Arceus, where the nine Arceus AR cards are filed as Holo Rares, so the holo rate and the
--     "Arceus Special Rare" rate are added and the row lands at 2.
--   * Call of Legends has no [Ultra Rare] row at all; its eleven shiny legendaries are
--     [Shiny Holo Rare] and take the "Shiny Legendary" rate directly.
--
-- [Secret Rare] and [Shiny Holo Rare] take their rates directly where a set has those rows.
-- Several sets' sources give a Secret Rare rate for rows this catalog does not have -- Mysterious
-- Treasures and the HGSS sets among them -- and those rates are simply unused rather than
-- written somewhere they do not belong.
--
-- Common, Uncommon and Rare are 1, as everywhere else. Triumphant's single [Promo] row is left
-- null, as the other promo rows are.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- DP01 Diamond & Pearl: Lv.X 1/36 -> 36 ; holo 1/3.3 -> 3
    ('DP01', 'Ultra Rare', 36),
    ('DP01', 'Holo Rare', 3),
    ('DP01', 'Rare', 1),
    ('DP01', 'Uncommon', 1),
    ('DP01', 'Common', 1),
    -- DP02 Mysterious Treasures: Lv.X 1/36 -> 36 ; holo 1/3.3 -> 3
    ('DP02', 'Ultra Rare', 36),
    ('DP02', 'Holo Rare', 3),
    ('DP02', 'Rare', 1),
    ('DP02', 'Uncommon', 1),
    ('DP02', 'Common', 1),
    -- DP03 Secret Wonders: Lv.X 1/72 -> 72 ; holo 1/3.1 -> 3
    ('DP03', 'Ultra Rare', 72),
    ('DP03', 'Holo Rare', 3),
    ('DP03', 'Rare', 1),
    ('DP03', 'Uncommon', 1),
    ('DP03', 'Common', 1),
    -- DP04 Great Encounters: Lv.X 1/36 -> 36 ; holo 1/3.3 -> 3
    ('DP04', 'Ultra Rare', 36),
    ('DP04', 'Holo Rare', 3),
    ('DP04', 'Rare', 1),
    ('DP04', 'Uncommon', 1),
    ('DP04', 'Common', 1),
    -- DP05 Majestic Dawn: Lv.X 1/36 -> 36 ; holo 1/3.3 -> 3
    ('DP05', 'Ultra Rare', 36),
    ('DP05', 'Holo Rare', 3),
    ('DP05', 'Rare', 1),
    ('DP05', 'Uncommon', 1),
    ('DP05', 'Common', 1),
    -- DP06 Legends Awakened: Lv.X 1/18 -> 18 ; holo 1/3.6 -> 4
    ('DP06', 'Ultra Rare', 18),
    ('DP06', 'Holo Rare', 4),
    ('DP06', 'Rare', 1),
    ('DP06', 'Uncommon', 1),
    ('DP06', 'Common', 1),
    -- DP07 Stormfront: Lv.X 1/18 -> 18 ; holo 1/4.0 -> 4
    ('DP07', 'Secret Rare', 36),
    ('DP07', 'Shiny Holo Rare', 40),
    ('DP07', 'Ultra Rare', 18),
    ('DP07', 'Holo Rare', 4),
    ('DP07', 'Rare', 1),
    ('DP07', 'Uncommon', 1),
    ('DP07', 'Common', 1),
    -- PL01 Platinum: Lv.X 1/18 -> 18 ; holo 1/4.0 -> 4
    ('PL01', 'Secret Rare', 36),
    ('PL01', 'Shiny Holo Rare', 36),
    ('PL01', 'Ultra Rare', 18),
    ('PL01', 'Holo Rare', 4),
    ('PL01', 'Rare', 1),
    ('PL01', 'Uncommon', 1),
    ('PL01', 'Common', 1),
    -- PL02 Rising Rivals: Lv.X 1/18 + Rotom 1/18 -> 9 ; holo 1/4.0 -> 4
    ('PL02', 'Secret Rare', 36),
    ('PL02', 'Ultra Rare', 9),
    ('PL02', 'Holo Rare', 4),
    ('PL02', 'Rare', 1),
    ('PL02', 'Uncommon', 1),
    ('PL02', 'Common', 1),
    -- PL03 Supreme Victors: Lv.X 1/10.3 -> 10 ; holo 1/4.8 -> 5
    ('PL03', 'Secret Rare', 34),
    ('PL03', 'Shiny Holo Rare', 40),
    ('PL03', 'Ultra Rare', 10),
    ('PL03', 'Holo Rare', 5),
    ('PL03', 'Rare', 1),
    ('PL03', 'Uncommon', 1),
    ('PL03', 'Common', 1),
    -- PL04 Arceus: Lv.X 1/12 -> 12 ; holo 1/4 + Arceus AR 1/4 -> 2
    ('PL04', 'Shiny Holo Rare', 54),
    ('PL04', 'Ultra Rare', 12),
    ('PL04', 'Holo Rare', 2),
    ('PL04', 'Rare', 1),
    ('PL04', 'Uncommon', 1),
    ('PL04', 'Common', 1),
    -- HGSS01 HeartGold & SoulSilver: Prime 1/6 + LEGEND 1/12 -> 4 ; holo 1/4.2 -> 4
    ('HGSS01', 'Ultra Rare', 4),
    ('HGSS01', 'Holo Rare', 4),
    ('HGSS01', 'Rare', 1),
    ('HGSS01', 'Uncommon', 1),
    ('HGSS01', 'Common', 1),
    -- HGSS02 Unleashed: Prime 1/7.2 + LEGEND 1/12 -> 5 ; holo 1/4.1 -> 4
    ('HGSS02', 'Ultra Rare', 5),
    ('HGSS02', 'Holo Rare', 4),
    ('HGSS02', 'Rare', 1),
    ('HGSS02', 'Uncommon', 1),
    ('HGSS02', 'Common', 1),
    -- HGSS03 Undaunted: Prime 1/7.2 + LEGEND 1/14.4 -> 5 ; holo 1/3.9 -> 4
    ('HGSS03', 'Ultra Rare', 5),
    ('HGSS03', 'Holo Rare', 4),
    ('HGSS03', 'Rare', 1),
    ('HGSS03', 'Uncommon', 1),
    ('HGSS03', 'Common', 1),
    -- HGSS04 Triumphant: Prime 1/7.2 + LEGEND 1/14.4 -> 5 ; holo 1/3.9 -> 4
    ('HGSS04', 'Ultra Rare', 5),
    ('HGSS04', 'Holo Rare', 4),
    ('HGSS04', 'Rare', 1),
    ('HGSS04', 'Uncommon', 1),
    ('HGSS04', 'Common', 1),
    -- COL Call of Legends: holo 1/3.0 -> 3
    ('COL', 'Shiny Holo Rare', 18),
    ('COL', 'Holo Rare', 3),
    ('COL', 'Rare', 1),
    ('COL', 'Uncommon', 1),
    ('COL', 'Common', 1)
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
