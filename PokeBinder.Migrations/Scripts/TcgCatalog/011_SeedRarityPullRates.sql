-- What each rarity is worth, per set: the packs-to-pull value the binder sorts by.
--
-- SOURCE. Every value carrying a sample size is the "1 in N packs" figure published by the
-- TCGplayer (formerly eBay) Authentication Center, which opens booster packs before a set's
-- release and publishes per-rarity rates with a 95% confidence interval. One article per set; the
-- sample is noted against each set because it varies from ~700 packs to more than 8,000, and a
-- 700-pack number is a much looser thing than an 8,000-pack one.
--
-- Nothing here is derived from another set, averaged or guessed. Two things do not come from the
-- source, and both say so where they are written:
--
--   * Common, Uncommon and Rare at 1, which is barely an exception: no study measures them
--     because a pack holds commons and uncommons by construction, and its rare slot holds a Rare
--     or better, so one pack is what it takes.
--   * Black White Rare at 496, which is a judgement. TCGplayer found none in 700+ packs and
--     published the rate as "Unknown", so this number is the catalog owner's, not theirs.
--
-- What is still NULL after this, and why:
--
--   * The odd single-card rarity the articles skip: Holo Rare in Black Bolt and Phantasmal
--     Flames, Promo in Mega Evolution.
--   * Shrouded Fable, Crown Zenith and everything older, and the promo sets. See NEXT-STEPS.md --
--     the source either does not exist or does not line up with how this catalog buckets its
--     rarities.
--
-- UNIT. Packs of that set you expect to open before seeing that rarity once. Bigger is rarer, and
-- the unit is the same in every set, which is what makes 540 in one set and 20 in another
-- comparable. Two rarities in one set may share a value, and the base rarities all share 1.
--
-- ROWS ARE MATCHED BY (set code, rarity name), not by id: ids are assigned by whatever loaded the
-- set and differ between one catalog database and the next, while the code and the rarity string
-- are the set's own facts. A pair that matches nothing updates nothing, which is what should
-- happen to a set this database has not loaded.

WITH seed(code, rarity, pullRate, nameOrder) AS (
    VALUES
-- Pre-existing rows, carried forward unchanged so a rebuilt catalog keeps them. These were
-- entered by hand before this script and are not from the source below -- which is why a few
-- of them put Rare at 3 where the seeded sets below put it at 1.
    -- BASE01
    ('BASE01', 'Common', 1, 1),
    ('BASE01', 'Uncommon', 1, 2),
    ('BASE01', 'Rare', 3, 3),
    ('BASE01', 'Holo Rare', 3, 4),
    -- BASE02
    ('BASE02', 'Common', 1, 1),
    ('BASE02', 'Uncommon', 1, 2),
    ('BASE02', 'Rare', 1, 3),
    ('BASE02', 'Holo Rare', 3, 4),
    -- BASE03
    ('BASE03', 'Common', 1, 1),
    ('BASE03', 'Uncommon', 1, 2),
    ('BASE03', 'Rare', 1, 3),
    ('BASE03', 'Holo Rare', 3, 4),
    -- BASE04
    ('BASE04', 'Common', 1, 1),
    ('BASE04', 'Uncommon', 1, 2),
    ('BASE04', 'Rare', 1, 3),
    ('BASE04', 'Holo Rare', 3, 4),
    -- BASE05
    ('BASE05', 'Common', 1, 1),
    ('BASE05', 'Uncommon', 1, 2),
    ('BASE05', 'Rare', 1, 3),
    ('BASE05', 'Holo Rare', 3, 4),
    ('BASE05', 'Secret Rare', 60, 5),
    -- BASE06
    ('BASE06', 'Common', 1, 1),
    ('BASE06', 'Uncommon', 1, 2),
    ('BASE06', 'Rare', 1, 3),
    ('BASE06', 'Holo Rare', 4, 4),
    -- BASE07
    ('BASE07', 'Common', 1, 1),
    ('BASE07', 'Uncommon', 1, 2),
    ('BASE07', 'Rare', 1, 3),
    ('BASE07', 'Holo Rare', 4, 4),
    -- NEO01
    ('NEO01', 'Common', 1, 1),
    ('NEO01', 'Uncommon', 1, 2),
    ('NEO01', 'Rare', 3, 3),
    ('NEO01', 'Holo Rare', 1, 4),
    -- NEO02
    ('NEO02', 'Common', 1, 1),
    ('NEO02', 'Uncommon', 1, 2),
    ('NEO02', 'Rare', 4, 3),
    ('NEO02', 'Holo Rare', 4, 4),
    -- NEO03
    ('NEO03', 'Common', 1, 1),
    ('NEO03', 'Uncommon', 1, 2),
    ('NEO03', 'Rare', 3, 3),
    ('NEO03', 'Holo Rare', 4, 4),
    ('NEO03', 'Secret Rare', 18, 5),
    -- NEO04
    ('NEO04', 'Common', 1, 1),
    ('NEO04', 'Uncommon', 1, 2),
    ('NEO04', 'Rare', 1, 3),
    ('NEO04', 'Holo Rare', 4, 4),
    ('NEO04', 'Secret Rare', 12, 5),
    -- LC01
    ('LC01', 'Common', 1, 1),
    ('LC01', 'Uncommon', 1, 2),
    ('LC01', 'Rare', 1, 3),
    ('LC01', 'Holo Rare', 3, 4),
    -- ECARD01
    ('ECARD01', 'Common', 1, 1),
    ('ECARD01', 'Uncommon', 1, 2),
    ('ECARD01', 'Rare', 1, 3),
    ('ECARD01', 'Holo Rare', 3, 4),
    -- ECARD02
    ('ECARD02', 'Common', 1, 1),
    ('ECARD02', 'Uncommon', 1, 2),
    ('ECARD02', 'Rare', 1, 3),
    ('ECARD02', 'Holo Rare', 3, 4),
    ('ECARD02', 'Secret Rare', 18, 5),
    -- ECARD03
    ('ECARD03', 'Common', 1, 1),
    ('ECARD03', 'Uncommon', 1, 2),
    ('ECARD03', 'Rare', 1, 3),
    ('ECARD03', 'Holo Rare', 3, 4),
    ('ECARD03', 'Secret Rare', 18, 5),
    -- EX01
    ('EX01', 'Common', 1, 1),
    ('EX01', 'Uncommon', 1, 2),
    ('EX01', 'Rare', 1, 3),
    ('EX01', 'Holo Rare', 8, 4),
    ('EX01', 'Ultra Rare', 12, 5),

-- Measured pull rates, TCGplayer/eBay Authentication Center, plus the three base rarities at 1.
--
-- Common, Uncommon and Rare are not measured by anybody and are not guesses either: every pack
-- holds commons and uncommons, and its rare slot holds a Rare or something better, so one pack
-- is what it takes. They are here for the sets below and for no others, because a set whose
-- base rarities are weighted and whose chase rarities are not would sort its commons ABOVE its
-- ultra rares -- an unweighted row has no value, and no value sorts last.
--
-- Name order is deliberately NULL: nobody has ranked these rarities by name yet, and a rate is
-- not a ranking.
    -- SV01  (sample: 8000+ packs)
    ('SV01', 'Hyper Rare', 54, NULL),
    ('SV01', 'Special Illustration Rare', 32, NULL),
    ('SV01', 'Ultra Rare', 15, NULL),
    ('SV01', 'Illustration Rare', 13, NULL),
    ('SV01', 'Double Rare', 7, NULL),
    ('SV01', 'Rare', 1, NULL),
    ('SV01', 'Uncommon', 1, NULL),
    ('SV01', 'Common', 1, NULL),
    -- SV02  (sample: 8000+ packs)
    ('SV02', 'Hyper Rare', 57, NULL),
    ('SV02', 'Special Illustration Rare', 32, NULL),
    ('SV02', 'Ultra Rare', 15, NULL),
    ('SV02', 'Illustration Rare', 13, NULL),
    ('SV02', 'Double Rare', 7, NULL),
    ('SV02', 'Rare', 1, NULL),
    ('SV02', 'Uncommon', 1, NULL),
    ('SV02', 'Common', 1, NULL),
    -- SV03  (sample: 8000+ packs)
    ('SV03', 'Hyper Rare', 52, NULL),
    ('SV03', 'Special Illustration Rare', 32, NULL),
    ('SV03', 'Ultra Rare', 15, NULL),
    ('SV03', 'Illustration Rare', 13, NULL),
    ('SV03', 'Double Rare', 7, NULL),
    ('SV03', 'Rare', 1, NULL),
    ('SV03', 'Uncommon', 1, NULL),
    ('SV03', 'Common', 1, NULL),
    -- SV04  (sample: 8000+ packs)
    ('SV04', 'Hyper Rare', 82, NULL),
    ('SV04', 'Special Illustration Rare', 47, NULL),
    ('SV04', 'Ultra Rare', 15, NULL),
    ('SV04', 'Illustration Rare', 13, NULL),
    ('SV04', 'Double Rare', 6, NULL),
    ('SV04', 'Rare', 1, NULL),
    ('SV04', 'Uncommon', 1, NULL),
    ('SV04', 'Common', 1, NULL),
    -- SV-PF  (sample: 1500+ packs)
    ('SV-PF', 'Hyper Rare', 62, NULL),
    ('SV-PF', 'Special Illustration Rare', 58, NULL),
    ('SV-PF', 'Ultra Rare', 15, NULL),
    ('SV-PF', 'Illustration Rare', 14, NULL),
    ('SV-PF', 'Shiny Ultra Rare', 13, NULL),
    ('SV-PF', 'Double Rare', 6, NULL),
    ('SV-PF', 'Shiny Rare', 4, NULL),
    ('SV-PF', 'Rare', 1, NULL),
    ('SV-PF', 'Uncommon', 1, NULL),
    ('SV-PF', 'Common', 1, NULL),
    -- SV05  (sample: 8000+ packs)
    ('SV05', 'Hyper Rare', 139, NULL),
    ('SV05', 'Special Illustration Rare', 86, NULL),
    ('SV05', 'ACE SPEC Rare', 20, NULL),
    ('SV05', 'Ultra Rare', 15, NULL),
    ('SV05', 'Illustration Rare', 13, NULL),
    ('SV05', 'Double Rare', 6, NULL),
    ('SV05', 'Rare', 1, NULL),
    ('SV05', 'Uncommon', 1, NULL),
    ('SV05', 'Common', 1, NULL),
    -- SV06  (sample: 8000+ packs)
    ('SV06', 'Hyper Rare', 146, NULL),
    ('SV06', 'Special Illustration Rare', 86, NULL),
    ('SV06', 'ACE SPEC Rare', 20, NULL),
    ('SV06', 'Ultra Rare', 15, NULL),
    ('SV06', 'Illustration Rare', 13, NULL),
    ('SV06', 'Double Rare', 6, NULL),
    ('SV06', 'Rare', 1, NULL),
    ('SV06', 'Uncommon', 1, NULL),
    ('SV06', 'Common', 1, NULL),
    -- SV07  (sample: 8000+ packs)
    ('SV07', 'Hyper Rare', 137, NULL),
    ('SV07', 'Special Illustration Rare', 90, NULL),
    ('SV07', 'ACE SPEC Rare', 20, NULL),
    ('SV07', 'Ultra Rare', 15, NULL),
    ('SV07', 'Illustration Rare', 13, NULL),
    ('SV07', 'Double Rare', 6, NULL),
    ('SV07', 'Rare', 1, NULL),
    ('SV07', 'Uncommon', 1, NULL),
    ('SV07', 'Common', 1, NULL),
    -- SV08  (sample: 8000+ packs)
    ('SV08', 'Hyper Rare', 188, NULL),
    ('SV08', 'Special Illustration Rare', 87, NULL),
    ('SV08', 'ACE SPEC Rare', 20, NULL),
    ('SV08', 'Ultra Rare', 15, NULL),
    ('SV08', 'Illustration Rare', 13, NULL),
    ('SV08', 'Double Rare', 6, NULL),
    ('SV08', 'Rare', 1, NULL),
    ('SV08', 'Uncommon', 1, NULL),
    ('SV08', 'Common', 1, NULL),
    -- SV-PE  (sample: 1200+ packs)
    ('SV-PE', 'Hyper Rare', 180, NULL),
    ('SV-PE', 'Special Illustration Rare', 45, NULL),
    ('SV-PE', 'ACE SPEC Rare', 21, NULL),
    ('SV-PE', 'Ultra Rare', 13, NULL),
    ('SV-PE', 'Double Rare', 6, NULL),
    ('SV-PE', 'Rare', 1, NULL),
    ('SV-PE', 'Uncommon', 1, NULL),
    ('SV-PE', 'Common', 1, NULL),
    -- SV09  (sample: 8000+ packs)
    ('SV09', 'Hyper Rare', 137, NULL),
    ('SV09', 'Special Illustration Rare', 86, NULL),
    ('SV09', 'Ultra Rare', 15, NULL),
    ('SV09', 'Illustration Rare', 12, NULL),
    ('SV09', 'Double Rare', 5, NULL),
    ('SV09', 'Rare', 1, NULL),
    ('SV09', 'Uncommon', 1, NULL),
    ('SV09', 'Common', 1, NULL),
    -- SV10  (sample: 8000+ packs)
    ('SV10', 'Hyper Rare', 149, NULL),
    ('SV10', 'Special Illustration Rare', 94, NULL),
    ('SV10', 'Ultra Rare', 16, NULL),
    ('SV10', 'Illustration Rare', 12, NULL),
    ('SV10', 'Double Rare', 5, NULL),
    ('SV10', 'Rare', 1, NULL),
    ('SV10', 'Uncommon', 1, NULL),
    ('SV10', 'Common', 1, NULL),
    -- SV-WF  (sample: 700+combined packs)
    ('SV-WF', 'Special Illustration Rare', 80, NULL),
    ('SV-WF', 'Ultra Rare', 17, NULL),
    ('SV-WF', 'Illustration Rare', 6, NULL),
    ('SV-WF', 'Double Rare', 5, NULL),
    ('SV-WF', 'Rare', 1, NULL),
    ('SV-WF', 'Uncommon', 1, NULL),
    ('SV-WF', 'Common', 1, NULL),
    -- SV-BB  (sample: 700+combined packs)
    ('SV-BB', 'Special Illustration Rare', 80, NULL),
    ('SV-BB', 'Ultra Rare', 17, NULL),
    ('SV-BB', 'Illustration Rare', 6, NULL),
    ('SV-BB', 'Double Rare', 5, NULL),
    ('SV-BB', 'Rare', 1, NULL),
    ('SV-BB', 'Uncommon', 1, NULL),
    ('SV-BB', 'Common', 1, NULL),
    -- ME01  (sample: 5000+ packs)
    ('ME01', 'Mega Hyper Rare', 1260, NULL),
    ('ME01', 'Special Illustration Rare', 101, NULL),
    ('ME01', 'Ultra Rare', 12, NULL),
    ('ME01', 'Illustration Rare', 9, NULL),
    ('ME01', 'Double Rare', 5, NULL),
    ('ME01', 'Rare', 1, NULL),
    ('ME01', 'Uncommon', 1, NULL),
    ('ME01', 'Common', 1, NULL),
    -- ME02  (sample: 5000+ packs)
    ('ME02', 'Mega Hyper Rare', 1260, NULL),
    ('ME02', 'Special Illustration Rare', 80, NULL),
    ('ME02', 'Ultra Rare', 12, NULL),
    ('ME02', 'Illustration Rare', 9, NULL),
    ('ME02', 'Double Rare', 5, NULL),
    ('ME02', 'Rare', 1, NULL),
    ('ME02', 'Uncommon', 1, NULL),
    ('ME02', 'Common', 1, NULL),
    -- ME-AH  (sample: 2000+ packs)
    ('ME-AH', 'Mega Hyper Rare', 540, 9),
    ('ME-AH', 'Special Illustration Rare', 70, 8),
    ('ME-AH', 'Mega Attack Rare', 29, 7),
    ('ME-AH', 'Ultra Rare', 21, 6),
    ('ME-AH', 'Illustration Rare', 9, 5),
    ('ME-AH', 'Double Rare', 5, 4),
    ('ME-AH', 'Rare', 1, 3),
    ('ME-AH', 'Uncommon', 1, 2),
    ('ME-AH', 'Common', 1, 1),
    -- ME03  (sample: 3500+ packs)
    ('ME03', 'Mega Hyper Rare', 1786, NULL),
    ('ME03', 'Special Illustration Rare', 81, NULL),
    ('ME03', 'Ultra Rare', 12, NULL),
    ('ME03', 'Illustration Rare', 9, NULL),
    ('ME03', 'Double Rare', 5, NULL),
    ('ME03', 'Rare', 1, NULL),
    ('ME03', 'Uncommon', 1, NULL),
    ('ME03', 'Common', 1, NULL),

    -- Black White Rare, entered by hand rather than measured.
    --
    -- TCGplayer opened more than 700 packs across Black Bolt and White Flare and found none,
    -- so the published rate is "Unknown" -- not a small number, an absent one. 496 is the
    -- catalog owner's figure and is recorded here so the two sets sort with their rarest card
    -- at the top instead of below their commons, which is where an unweighted row lands.
    ('SV-BB', 'Black White Rare', 496, NULL),
    ('SV-WF', 'Black White Rare', 496, NULL)
)
UPDATE [rarityBySetFilterOption] AS target
SET [pullRateRarityOrder] = COALESCE(seed.pullRate, target.[pullRateRarityOrder]),
    [nameRarityOrder]     = COALESCE(seed.nameOrder, target.[nameRarityOrder])
FROM seed
JOIN [sets] ON [sets].[code] = seed.code
-- COALESCE above rather than a bare assignment: a seed row that carries only one of the two
-- values must not blank the other. Every value here is therefore additive -- running this cannot
-- turn a weighted row back into an unweighted one.
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
