-- The XY era, weighted from community counts -- same source and the same caveats as 015.
--
-- ThePriceDex's per-set pull-rate tables again: "estimates primarily sourced from <set> pull rates
-- research and based on community data", no sample size stated, one source per set with nothing to
-- cross-check against. TCGplayer's measured series does not reach anywhere near this far back.
-- Read every number below as approximate.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS, membership read off the cards themselves:
--   * [Ultra Rare] holds the EX and M EX cards together with the Full Arts, so it is
--     "Rare Holo EX" + "Ultra Rare" added.
--   * [Secret Rare] holds the secret-numbered cards and takes the source's Secret Rare rate
--     directly. XY has no rainbow rarity, so nothing is folded in here the way 015 had to.
--   * [Rare BREAK] and [Holo Rare] map one-for-one.
--
-- Common, Uncommon and Rare are 1, as everywhere else.
--
-- TWO SETS READ ODDLY AND ARE CORRECT ANYWAY:
--   * Evolutions' Secret Rare is 1 in 8, not the ~1 in 125 the rest of the era runs at. Its five
--     secrets are the retro-styled cards that turn up in the reverse-holo slot rather than gold
--     chase cards, so they really are that common.
--   * Generations' Ultra Rare is 1 in 4, the most generous in the catalog. Its Radiant Collection
--     subset is not loaded here, so only the main-set EX and Full Art rates are used; the RC rates
--     the source also lists are deliberately ignored, since no row in this catalog holds them.
--
-- WHAT IS LEFT UNWEIGHTED, none of it an omission: Generations' single [Promo] row, which no study
-- covers; Double Crisis and the Kalos Starter Set, neither sold as booster packs -- ThePriceDex
-- has no page for either -- and XY: Promos.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- XY01 XY: EX 1/12, full art 1/36, holo 1/4.3
    ('XY01', 'Ultra Rare', 9),
    ('XY01', 'Holo Rare', 4),
    ('XY01', 'Rare', 1),
    ('XY01', 'Uncommon', 1),
    ('XY01', 'Common', 1),
    -- XY02 Flashfire: EX 1/11.3, full art 1/25.7, secret 1/114.3, holo 1/5.1
    ('XY02', 'Secret Rare', 114),
    ('XY02', 'Ultra Rare', 8),
    ('XY02', 'Holo Rare', 5),
    ('XY02', 'Rare', 1),
    ('XY02', 'Uncommon', 1),
    ('XY02', 'Common', 1),
    -- XY03 Furious Fists: EX 1/11.4, full art 1/25.7, secret 1/102.9, holo 1/5.1
    ('XY03', 'Secret Rare', 103),
    ('XY03', 'Ultra Rare', 8),
    ('XY03', 'Holo Rare', 5),
    ('XY03', 'Rare', 1),
    ('XY03', 'Uncommon', 1),
    ('XY03', 'Common', 1),
    -- XY04 Phantom Forces: EX 1/10.9, full art 1/25.8, secret 1/114.3, holo 1/5.1
    ('XY04', 'Secret Rare', 114),
    ('XY04', 'Ultra Rare', 8),
    ('XY04', 'Holo Rare', 5),
    ('XY04', 'Rare', 1),
    ('XY04', 'Uncommon', 1),
    ('XY04', 'Common', 1),
    -- XY05 Primal Clash: EX 1/9, full art 1/17.0, secret 1/125, holo 1/6.4
    ('XY05', 'Secret Rare', 125),
    ('XY05', 'Ultra Rare', 6),
    ('XY05', 'Holo Rare', 6),
    ('XY05', 'Rare', 1),
    ('XY05', 'Uncommon', 1),
    ('XY05', 'Common', 1),
    -- XY06 Roaring Skies: EX 1/9, full art 1/17.0, secret 1/125, holo 1/6.4
    ('XY06', 'Secret Rare', 125),
    ('XY06', 'Ultra Rare', 6),
    ('XY06', 'Holo Rare', 6),
    ('XY06', 'Rare', 1),
    ('XY06', 'Uncommon', 1),
    ('XY06', 'Common', 1),
    -- XY07 Ancient Origins: EX 1/9, full art 1/21.4, secret 1/50, holo 1/6.4
    ('XY07', 'Secret Rare', 50),
    ('XY07', 'Ultra Rare', 6),
    ('XY07', 'Holo Rare', 6),
    ('XY07', 'Rare', 1),
    ('XY07', 'Uncommon', 1),
    ('XY07', 'Common', 1),
    -- XY08 BREAKthrough: EX 1/11.3, full art 1/17.0, secret 1/125, BREAK 1/12.9, holo 1/5.6
    ('XY08', 'Secret Rare', 125),
    ('XY08', 'Rare BREAK', 13),
    ('XY08', 'Ultra Rare', 7),
    ('XY08', 'Holo Rare', 6),
    ('XY08', 'Rare', 1),
    ('XY08', 'Uncommon', 1),
    ('XY08', 'Common', 1),
    -- XY09 BREAKpoint: EX 1/10, full art 1/17.0, secret 1/125, BREAK 1/15, holo 1/6.0
    ('XY09', 'Secret Rare', 125),
    ('XY09', 'Rare BREAK', 15),
    ('XY09', 'Ultra Rare', 6),
    ('XY09', 'Holo Rare', 6),
    ('XY09', 'Rare', 1),
    ('XY09', 'Uncommon', 1),
    ('XY09', 'Common', 1),
    -- XY-GEN Generations: EX 1/4.9, full art 1/45, holo 1/9.5
    ('XY-GEN', 'Ultra Rare', 4),
    ('XY-GEN', 'Holo Rare', 10),
    ('XY-GEN', 'Rare', 1),
    ('XY-GEN', 'Uncommon', 1),
    ('XY-GEN', 'Common', 1),
    -- XY10 Fates Collide: EX 1/8.2, full art 1/17.0, secret 1/125, BREAK 1/15, holo 1/6.9
    ('XY10', 'Secret Rare', 125),
    ('XY10', 'Rare BREAK', 15),
    ('XY10', 'Ultra Rare', 6),
    ('XY10', 'Holo Rare', 7),
    ('XY10', 'Rare', 1),
    ('XY10', 'Uncommon', 1),
    ('XY10', 'Common', 1),
    -- XY11 Steam Siege: EX 1/15, full art 1/17.0, secret 1/125, BREAK 1/15, holo 1/5.0
    ('XY11', 'Secret Rare', 125),
    ('XY11', 'Rare BREAK', 15),
    ('XY11', 'Ultra Rare', 8),
    ('XY11', 'Holo Rare', 5),
    ('XY11', 'Rare', 1),
    ('XY11', 'Uncommon', 1),
    ('XY11', 'Common', 1),
    -- XY12 Evolutions: EX 1/8.2, full art 1/15, secret 1/8.1, BREAK 1/18, holo 1/6.9
    ('XY12', 'Secret Rare', 8),
    ('XY12', 'Rare BREAK', 18),
    ('XY12', 'Ultra Rare', 5),
    ('XY12', 'Holo Rare', 7),
    ('XY12', 'Rare', 1),
    ('XY12', 'Uncommon', 1),
    ('XY12', 'Common', 1)
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
