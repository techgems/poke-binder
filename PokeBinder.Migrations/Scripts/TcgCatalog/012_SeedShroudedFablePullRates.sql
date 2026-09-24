-- Shrouded Fable's pull rates, which are the one set in this catalog weighted from community
-- numbers rather than from a measured study.
--
-- WHY IT IS SEPARATE FROM 011. Every value in 011 came from the TCGplayer Authentication Center,
-- which opens packs itself and publishes a sample size and a confidence interval. Shrouded Fable
-- has no such article -- it was skipped -- so the only figures that exist are community pack
-- counts, and those are a weaker kind of number. Keeping them in their own script means the
-- answer to "where did this come from?" is the file the row is in.
--
-- THE SOURCES, and they disagree:
--
--   * PokéPatch, reporting Dripshop, "1,000+ packs": Ultra Rare 1:15, ACE SPEC 1:20,
--     Illustration Rare 1:12, Special Illustration Rare 1:67, Hyper Rare 1:144.
--   * ThePriceDex, community data with no sample size stated: Double Rare 1:6, Ultra Rare 1:14.3,
--     ACE SPEC 1:20, Illustration Rare 1:13, Special Illustration Rare 1:87.2, Hyper Rare 1:128.3.
--   * A 2,354-pack count from the PokeInvesting community, via skool and Pokemon Engage:
--     Illustration Rare 1/12, Special Illustration Rare 1/64, Gold 1/118 -- Gold being what this
--     catalog calls Hyper Rare.
--
-- WHERE THEY CONFLICT, THE LOWER NUMBER WINS, on instruction: Shrouded Fable is not a high-value
-- set, and an easier pull is the safer error to make for it. So Ultra Rare takes 14 rather than
-- 15, Special Illustration Rare 64 rather than 67 or 87, Hyper Rare 118 rather than 128 or 144.
-- Where only one source has a figure -- Double Rare -- that figure is used as it stands.
--
-- The 2,354-pack table also lists "Full Art 1/24" and "FA-SP 1/36", which are deliberately NOT
-- used: neither maps cleanly onto a rarity this catalog keeps, and guessing which of Ultra Rare
-- or Special Illustration Rare they belong to would be inventing a number rather than reading one.
--
-- Common, Uncommon and Rare are 1, as they are for every seeded set in 011: a pack holds commons
-- and uncommons by construction, and its rare slot holds a Rare or something better.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    ('SV-SF', 'Hyper Rare', 118),
    ('SV-SF', 'Special Illustration Rare', 64),
    ('SV-SF', 'ACE SPEC Rare', 20),
    ('SV-SF', 'Ultra Rare', 14),
    ('SV-SF', 'Illustration Rare', 12),
    ('SV-SF', 'Double Rare', 6),
    ('SV-SF', 'Rare', 1),
    ('SV-SF', 'Uncommon', 1),
    ('SV-SF', 'Common', 1)
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
