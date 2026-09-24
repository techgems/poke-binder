-- Legendary Treasures, the one set in this catalog whose source figures are corrected before use.
--
-- THE PROBLEM. ThePriceDex's page for this set gives Secret Rare 1 in 108, Ultra Rare 1 in 4,
-- Rare Holo EX 1 in 6, Rare Holo 1 in 2 and Rare 1 in 1.2. Those are the rarities that compete
-- for a Black & White pack's single rare slot, and as probabilities they sum to 1.76 cards per
-- pack -- a pack cannot hold 1.76 rare-slot cards. Every other set in the era checks out against
-- the same test: Plasma Blast's rare-slot rarities sum to 1.04.
--
-- THE CORRECTION. The page's relative odds are kept and its scale is divided out: each
-- probability is divided by 1.759 so the slot sums to one card per pack, which is the same as
-- multiplying each "1 in N" by 1.759. Nothing is invented and no number is imported from another
-- set -- this is the source's own data, rescaled by the constraint it violates.
--
--   Secret Rare  1/108 -> 1/190   Ultra Rare  1/6 -> 1/11   Holo Rare  1/2 -> 1/4
--
-- WHY THE RESULT IS BELIEVABLE. It lands the EX cards at 11 packs, against 18 for Next Destinies
-- and Plasma Blast and 13.5 for Plasma Storm -- and contemporary accounts say Legendary Treasures'
-- EX cards were easier to pull than Next Destinies'. The holo rate lands at 4, where the rest of
-- the era sits at 4.2 to 4.5. Uncorrected, this set would have claimed an EX is as easy to find
-- as a holo rare and twice as easy as a Rare.
--
-- MAPPING. This catalog's [Ultra Rare] row holds the 12 regular EX cards, which is the source's
-- "Rare Holo EX" tier; its five Full Art EX are not loaded here, so the source's own "Ultra Rare"
-- tier has no row to write to and is unused. [Secret Rare] is the two Full Art Secret Rares.
-- The Radiant Collection subset is not loaded either, so the RC rates are unused as well.
--
-- Common, Uncommon and Rare are 1, as everywhere else.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    ('BW-LD', 'Secret Rare', 190),
    ('BW-LD', 'Ultra Rare', 11),
    ('BW-LD', 'Holo Rare', 4),
    ('BW-LD', 'Rare', 1),
    ('BW-LD', 'Uncommon', 1),
    ('BW-LD', 'Common', 1)
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
