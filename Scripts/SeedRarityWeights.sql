-- What each rarity is worth in each set: the packs-to-pull value the binder sorts by, and the
-- by-name ranking beside it.
--
-- THIS IS NOT A MIGRATION, AND IT IS NOT RUN BY DbUp. Migrations own the schema and the data the
-- app cannot start without. These values are neither: the catalog works without them, they change
-- whenever somebody corrects a weight at /admin/setRarity, and they are as variable as the card
-- data itself. They live here so they can be re-run, regenerated and diffed without touching the
-- schema history. See "Rules for catalog data" in CLAUDE.md.
--
-- GENERATED FROM THE DATABASE, which is the authority. 753 rows across 123 sets,
-- dumped 2026-09-24. It replaces eleven migration scripts (011 through
-- 021) that wrote these columns one era at a time as sources were found; the values below are
-- those scripts' result, plus every correction made by hand since, which is why the database and
-- not those scripts is what this was built from.
--
-- **Regenerate it after editing weights by hand**, or it goes stale -- and a stale run puts the
-- old values back, because each statement below writes what it holds. That is the trade for being
-- able to restore a rebuilt catalog exactly.
--
-- RUN IT AFTER THE SETS AND CARDS ARE LOADED. Every statement resolves its set by code, so a set
-- the catalog does not have yet is silently skipped rather than created.
--
-- WHERE THE NUMBERS CAME FROM is recorded per set, because the rows are not all the same quality:
--
--   * measured -- the TCGplayer (formerly eBay) Authentication Center opens packs before release
--     and publishes per-rarity rates with sample sizes and confidence intervals. The strongest
--     evidence here, and the sample size is noted per set because it ranges from 700 to 8,000+.
--   * community estimate -- ThePriceDex's per-set tables, self-described as estimates from
--     community data with no sample size, one source per set and nothing to cross-check against.
--   * entered by hand -- the vintage WotC and EX Ruby & Sapphire rows, which predate all of the
--     research.
--   * rescaled -- Legendary Treasures alone. Its source figures sum to 1.76 rare-slot cards per
--     pack when a pack holds one, so each probability was divided by 1.76 before use: the source's
--     own relative odds, with the scale it got wrong divided out.
--
-- Individual values that no source backs, wherever they appear: Black White Rare at 496; Holo Rare
-- at 3 across the Sword & Shield sets, which no study measures; Common, Uncommon and Rare at 1,
-- which is the pack's construction rather than a measurement; and Astral Radiance's Radiant Rare
-- taken from Lost Origin, its own article having never measured it.
--
-- WHAT IS ABSENT: the promo rows of the promo and non-booster sets, six EX-era Secret Rares that
-- no source rates, and three odd singles. Those rows have no value in the database either, and an
-- absent row here means an unweighted row there.
--
-- Rows are matched by (set code, rarity name) rather than by id, because ids are assigned per
-- database while a set's code and a rarity's name are the set's own facts.

-- BASE01  Base Set -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Rare', 3, 3),
    ('Common', 1, 1),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE02  Jungle -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE03  Fossil -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE04  Base Set 2 -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE05  Team Rocket -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 60, 5),
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE06  Gym Heroes -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 4, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BASE07  Gym Challenge -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 4, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BASE07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- NEO01  Neo Genesis -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Rare', 3, 3),
    ('Common', 1, 1),
    ('Holo Rare', 1, 4),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'NEO01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- NEO02  Neo Discovery -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 4, 4),
    ('Rare', 4, 3),
    ('Common', 1, 1),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'NEO02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- NEO03  Neo Revelation -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 18, 5),
    ('Holo Rare', 4, 4),
    ('Rare', 3, 3),
    ('Common', 1, 1),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'NEO03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- NEO04  Neo Destiny -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 12, 5),
    ('Holo Rare', 4, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'NEO04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- LC01  Legendary Collection -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'LC01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ECARD01  Expedition Base Set -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ECARD01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ECARD02  Aquapolis -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 18, 5),
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ECARD02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ECARD03  Skyridge -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 18, 5),
    ('Holo Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ECARD03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX01  Ruby & Sapphire -- entered by hand, predating the research
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 12, 5),
    ('Holo Rare', 8, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX02  Sandstorm -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX03  Dragon -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 30, NULL),
    ('Holo Rare', 8, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX04  Team Magma vs Team Aqua -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 52, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX05  Hidden Legends -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX06  FireRed & LeafGreen -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 36, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX07  Team Rocket Returns -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 220, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX08  Deoxys -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX09  Emerald -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX10  Unseen Forces -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 176, NULL),
    ('Ultra Rare', 13, NULL),
    ('Holo Rare', 2, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX11  Delta Species -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 27, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX11'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX12  Legend Maker -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 15, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX12'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX13  Holon Phantoms -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 25, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX13'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX14  Crystal Guardians -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 11, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX14'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX15  Dragon Frontiers -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 11, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX15'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- EX16  Power Keepers -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 10, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'EX16'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP01  Diamond & Pearl -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 36, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP02  Mysterious Treasures -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 36, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP03  Secret Wonders -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 72, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP04  Great Encounters -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 36, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP05  Majestic Dawn -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 36, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP06  Legends Awakened -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- DP07  Stormfront -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Shiny Holo Rare', 40, NULL),
    ('Secret Rare', 36, NULL),
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'DP07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- PL01  Platinum -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 36, NULL),
    ('Shiny Holo Rare', 36, NULL),
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'PL01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- PL02  Rising Rivals -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 36, NULL),
    ('Ultra Rare', 9, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'PL02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- PL03  Supreme Victors -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Shiny Holo Rare', 40, NULL),
    ('Secret Rare', 34, NULL),
    ('Ultra Rare', 10, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'PL03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- PL04  Arceus -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Shiny Holo Rare', 54, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 2, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'PL04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- HGSS01  HeartGold & SoulSilver -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 4, NULL),
    ('Ultra Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'HGSS01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- HGSS02  Unleashed -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 5, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'HGSS02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- HGSS03  Undaunted -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 5, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'HGSS03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- HGSS04  Triumphant -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 5, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'HGSS04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- COL  Call Of Legends -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Shiny Holo Rare', 18, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'COL'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW01  Black & White -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 72, NULL),
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW02  Emerging Powers -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW03  Noble Victories -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 72, NULL),
    ('Ultra Rare', 18, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW04  Next Destinies -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 108, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW05  Dark Explorers -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 108, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW06  Dragons Exalted -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 108, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW07  Boundaries Crossed -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 98, NULL),
    ('Rare Ace', 18, NULL),
    ('Ultra Rare', 10, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW08  Plasma Storm -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 75, NULL),
    ('Rare Ace', 24, NULL),
    ('Ultra Rare', 10, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW09  Plasma Freeze -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 120, NULL),
    ('Rare Ace', 36, NULL),
    ('Ultra Rare', 11, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW10  Plasma Blast -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 113, NULL),
    ('Rare Ace', 24, NULL),
    ('Ultra Rare', 12, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- BW-LD  Legendary Treasures -- ThePriceDex, rescaled -- see the note above
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 190, NULL),
    ('Ultra Rare', 11, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'BW-LD'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY00  Kalos Starter Set -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Common', NULL, 1)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY00'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY01  XY -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Ultra Rare', 9, NULL),
    ('Holo Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY02  Flashfire -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 114, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY03  Furious Fists -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 103, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY04  Phantom Forces -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 114, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY05  Primal Clash -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY06  Roaring Skies -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY07  Ancient Origins -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 50, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY08  BREAKthrough -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Rare BREAK', 13, NULL),
    ('Ultra Rare', 7, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY09  BREAKpoint -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Rare BREAK', 15, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY-GEN  Generations -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Holo Rare', 10, NULL),
    ('Ultra Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY-GEN'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY10  Fates Collide -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Rare BREAK', 15, NULL),
    ('Holo Rare', 7, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY11  Steam Siege -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 125, NULL),
    ('Rare BREAK', 15, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY11'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- XY12  Evolutions -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Rare BREAK', 18, NULL),
    ('Secret Rare', 8, NULL),
    ('Holo Rare', 7, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'XY12'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM01  Sun & Moon -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 43, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM02  Guardians Rising -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 42, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM03  Burning Shadows -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 103, NULL),
    ('Rainbow Rare', 71, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM-SL  Shining Legends -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 40, NULL),
    ('Shiny Holo Rare', 11, NULL),
    ('Ultra Rare', 7, NULL),
    ('Common', 1, NULL),
    ('Holo Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM-SL'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM04  Crimson Invasion -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 46, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM05  Ultra Prism -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 44, NULL),
    ('Prism Rare', 12, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM06  Forbidden Light -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 50, NULL),
    ('Prism Rare', 12, NULL),
    ('Ultra Rare', 8, NULL),
    ('Holo Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM07  Celestial Storm -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 124, NULL),
    ('Rainbow Rare', 82, NULL),
    ('Prism Rare', 18, NULL),
    ('Ultra Rare', 7, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM-DM  Dragon Majesty -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 23, NULL),
    ('Prism Rare', 10, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Holo Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM-DM'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM08  Lost Thunder -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 42, NULL),
    ('Prism Rare', 8, NULL),
    ('Ultra Rare', 7, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM09  Team Up -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 40, NULL),
    ('Prism Rare', 18, NULL),
    ('Ultra Rare', 7, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM10  Unbroken Bonds -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 40, NULL),
    ('Ultra Rare', 7, NULL),
    ('Holo Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM11  Unified Minds -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 45, NULL),
    ('Holo Rare', 7, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM11'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM-HF  Hidden Fates -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 91, NULL),
    ('Holo Rare', 5, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM-HF'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SM12  Cosmic Eclipse -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 111, NULL),
    ('Rainbow Rare', 71, NULL),
    ('Holo Rare', 7, NULL),
    ('Ultra Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SM12'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH01  Sword & Shield -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 47, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH02  Rebel Clash -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 41, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH03  Darkness Ablaze -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 49, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH-CP  Champion's Path -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 35, NULL),
    ('Ultra Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Holo Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH-CP'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH04  Vivid Voltage -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 42, NULL),
    ('Amazing Rare', 18, NULL),
    ('Holo Rare', 5, NULL),
    ('Ultra Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH-SF  Shining Fates -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 84, NULL),
    ('Amazing Rare', 17, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 5, NULL),
    ('Shiny Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH-SF'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH05  Battle Styles -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 52, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH06  Chilling Reign -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 49, NULL),
    ('Holo Rare', 6, NULL),
    ('Ultra Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH07  Evolving Skies -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 49, NULL),
    ('Ultra Rare', 5, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH-CEL25  Celebrations -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 150, NULL),
    ('Classic Collection', 3, NULL),
    ('Ultra Rare', 2, NULL),
    ('Holo Rare', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH-CEL25'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH08  Fusion Strike -- measured: TCGplayer, published only as an infographic, 4,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 52, NULL),
    ('Ultra Rare', 6, NULL),
    ('Holo Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH09  Brilliant Stars -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 43, 5),
    ('Holo Rare', 6, 6),
    ('Ultra Rare', 3, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH10  Astral Radiance -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 49, NULL),
    ('Radiant Rare', 20, NULL),
    ('Holo Rare', 3, NULL),
    ('Ultra Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH-PGO  Pokémon GO -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 24, NULL),
    ('Radiant Rare', 17, NULL),
    ('Ultra Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Holo Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH-PGO'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH11  Lost Origin -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 34, NULL),
    ('Radiant Rare', 20, NULL),
    ('Holo Rare', 3, NULL),
    ('Ultra Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH11'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH12  Silver Tempest -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 32, NULL),
    ('Radiant Rare', 22, NULL),
    ('Holo Rare', 3, NULL),
    ('Ultra Rare', 3, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH12'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SWSH-CZ  Crown Zenith -- measured: TCGplayer Authentication Center, 1,900+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Secret Rare', 65, NULL),
    ('Radiant Rare', 22, NULL),
    ('Holo Rare', 3, NULL),
    ('Ultra Rare', 2, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SWSH-CZ'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV01  Scarlet & Violet -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 54, NULL),
    ('Special Illustration Rare', 32, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 7, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV02  Paldea Evolved -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 57, NULL),
    ('Special Illustration Rare', 32, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 7, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV03  Obsidian Flames -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 52, NULL),
    ('Special Illustration Rare', 32, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 7, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV04  Paradox Rift -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 82, NULL),
    ('Special Illustration Rare', 47, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV04'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV-PF  Paldean Fates -- measured: TCGplayer Authentication Center, 1,500+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 62, NULL),
    ('Special Illustration Rare', 58, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 14, NULL),
    ('Shiny Ultra Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Shiny Rare', 4, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV-PF'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV05  Temporal Forces -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 139, NULL),
    ('Special Illustration Rare', 86, NULL),
    ('ACE SPEC Rare', 20, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV05'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV06  Twilight Masquerade -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 146, NULL),
    ('Special Illustration Rare', 86, NULL),
    ('ACE SPEC Rare', 20, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV06'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV-SF  Shrouded Fable -- community estimate: ThePriceDex, no sample size stated
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 118, NULL),
    ('Special Illustration Rare', 64, NULL),
    ('ACE SPEC Rare', 20, NULL),
    ('Ultra Rare', 14, NULL),
    ('Illustration Rare', 12, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV-SF'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV07  Stellar Crown -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 137, NULL),
    ('Special Illustration Rare', 90, NULL),
    ('ACE SPEC Rare', 20, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV07'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV08  Surging Sparks -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 188, NULL),
    ('Special Illustration Rare', 87, NULL),
    ('ACE SPEC Rare', 20, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV08'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV-PE  Prismatic Evolutions -- measured: TCGplayer Authentication Center, 1,200+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 180, NULL),
    ('Special Illustration Rare', 45, NULL),
    ('ACE SPEC Rare', 21, NULL),
    ('Ultra Rare', 13, NULL),
    ('Double Rare', 6, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV-PE'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV09  Journey Together -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 137, NULL),
    ('Special Illustration Rare', 86, NULL),
    ('Ultra Rare', 15, NULL),
    ('Illustration Rare', 12, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV09'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV10  Destined Rivals -- measured: TCGplayer Authentication Center, 8,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Hyper Rare', 149, NULL),
    ('Special Illustration Rare', 94, NULL),
    ('Ultra Rare', 16, NULL),
    ('Illustration Rare', 12, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV10'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV-WF  White Flare -- measured: TCGplayer Authentication Center, 700+ packs, combined with Black Bolt
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Black White Rare', 496, NULL),
    ('Special Illustration Rare', 80, NULL),
    ('Ultra Rare', 17, NULL),
    ('Illustration Rare', 6, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV-WF'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- SV-BB  Black Bolt -- measured: TCGplayer Authentication Center, 700+ packs, combined with White Flare
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Black White Rare', 496, NULL),
    ('Special Illustration Rare', 80, NULL),
    ('Ultra Rare', 17, NULL),
    ('Illustration Rare', 6, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'SV-BB'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ME01  Mega Evolution -- measured: TCGplayer Authentication Center, 5,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Mega Hyper Rare', 1260, NULL),
    ('Special Illustration Rare', 101, NULL),
    ('Ultra Rare', 12, NULL),
    ('Illustration Rare', 9, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ME01'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ME02  ME: Phantasmal Flames -- measured: TCGplayer Authentication Center, 5,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Mega Hyper Rare', 1260, NULL),
    ('Special Illustration Rare', 80, NULL),
    ('Ultra Rare', 12, NULL),
    ('Illustration Rare', 9, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ME02'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ME-AH  ME: Ascended Heroes -- measured: TCGplayer Authentication Center, 2,000+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Mega Hyper Rare', 540, 9),
    ('Special Illustration Rare', 70, 8),
    ('Mega Attack Rare', 29, 7),
    ('Ultra Rare', 21, 6),
    ('Illustration Rare', 9, 5),
    ('Double Rare', 5, 4),
    ('Common', 1, 1),
    ('Rare', 1, 3),
    ('Uncommon', 1, 2)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ME-AH'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];

-- ME03  ME: Perfect Order -- measured: TCGplayer Authentication Center, 3,500+ packs
WITH v(rarity, pullRate, nameOrder) AS (VALUES
    ('Mega Hyper Rare', 1786, NULL),
    ('Special Illustration Rare', 81, NULL),
    ('Ultra Rare', 12, NULL),
    ('Illustration Rare', 9, NULL),
    ('Double Rare', 5, NULL),
    ('Common', 1, NULL),
    ('Rare', 1, NULL),
    ('Uncommon', 1, NULL)
)
INSERT INTO [rarityBySetFilterOption] ([setId], [rarity], [pullRateRarityOrder], [nameRarityOrder])
SELECT [sets].[id], v.[rarity], v.[pullRate], v.[nameOrder]
FROM v JOIN [sets] ON [sets].[code] = 'ME03'
WHERE true
ON CONFLICT([setId], [rarity]) DO UPDATE SET
    [pullRateRarityOrder] = excluded.[pullRateRarityOrder],
    [nameRarityOrder] = excluded.[nameRarityOrder];
