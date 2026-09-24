-- The Black & White era, weighted from community counts -- same source and the same caveats as
-- 015 and 016: ThePriceDex's per-set tables, self-described as estimates from community data, no
-- sample size, one source per set. Read every number as approximate.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS, membership read off the cards themselves:
--   * [Ultra Rare] holds the EX cards and the Full Arts, so it is "Rare Holo EX" + "Ultra Rare"
--     added -- except in Black & White, Emerging Powers and Noble Victories, which predate EX
--     cards entirely. Their [Ultra Rare] rows hold two to five Full Arts and nothing else, so the
--     source's single Ultra Rare rate is used as it stands, with nothing to add to it.
--   * [Secret Rare] takes the Secret Rare rate directly. In this era those are the shiny
--     legendary cards rather than gold chase cards. Emerging Powers has no such row and the
--     source gives no such rate, which agree.
--   * [Rare Ace] is the ACE SPEC cards of the Plasma sets, and maps one-for-one to "Rare ACE".
--   * [Holo Rare] maps one-for-one to "Rare Holo".
--
-- Common, Uncommon and Rare are 1, as everywhere else.
--
-- LEGENDARY TREASURES IS DELIBERATELY ABSENT, and it is the one set here I would not seed. Its
-- ThePriceDex page reads Ultra Rare 1 in 4, Rare Holo EX 1 in 6 and Rare Holo 1 in 2 -- roughly
-- three times more generous than every neighbouring set, and out of step with the corroborating
-- accounts of the period, which put an EX around 1 in 18 to 1 in 36. The set is a reprint set with
-- a Radiant Collection subset this catalog does not load, and the figures look like that subset
-- leaking into the main tiers. Seeding them would tell the binder an EX is easier to find than a
-- holo rare. It is left unweighted until there is a number worth trusting.
--
-- ALSO UNWEIGHTED, and not omissions: Dragon Vault, a mini set sold in boxes rather than boosters
-- and holding nothing but Holo Rares, and BW: Promos.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- BW01 Black & White: full art 1/18, secret 1/72, holo 1/3.8
    ('BW01', 'Secret Rare', 72),
    ('BW01', 'Ultra Rare', 18),
    ('BW01', 'Holo Rare', 4),
    ('BW01', 'Rare', 1),
    ('BW01', 'Uncommon', 1),
    ('BW01', 'Common', 1),
    -- BW02 Emerging Powers: full art 1/18, holo 1/3.6
    ('BW02', 'Ultra Rare', 18),
    ('BW02', 'Holo Rare', 4),
    ('BW02', 'Rare', 1),
    ('BW02', 'Uncommon', 1),
    ('BW02', 'Common', 1),
    -- BW03 Noble Victories: full art 1/18, secret 1/72, holo 1/3.8
    ('BW03', 'Secret Rare', 72),
    ('BW03', 'Ultra Rare', 18),
    ('BW03', 'Holo Rare', 4),
    ('BW03', 'Rare', 1),
    ('BW03', 'Uncommon', 1),
    ('BW03', 'Common', 1),
    -- BW04 Next Destinies: full art 1/36, EX 1/18, secret 1/108, holo 1/4.2
    ('BW04', 'Secret Rare', 108),
    ('BW04', 'Ultra Rare', 12),
    ('BW04', 'Holo Rare', 4),
    ('BW04', 'Rare', 1),
    ('BW04', 'Uncommon', 1),
    ('BW04', 'Common', 1),
    -- BW05 Dark Explorers: full art 1/36, EX 1/18, secret 1/108, holo 1/4.2
    ('BW05', 'Secret Rare', 108),
    ('BW05', 'Ultra Rare', 12),
    ('BW05', 'Holo Rare', 4),
    ('BW05', 'Rare', 1),
    ('BW05', 'Uncommon', 1),
    ('BW05', 'Common', 1),
    -- BW06 Dragons Exalted: full art 1/36, EX 1/18, secret 1/108, holo 1/4.2
    ('BW06', 'Secret Rare', 108),
    ('BW06', 'Ultra Rare', 12),
    ('BW06', 'Holo Rare', 4),
    ('BW06', 'Rare', 1),
    ('BW06', 'Uncommon', 1),
    ('BW06', 'Common', 1),
    -- BW07 Boundaries Crossed: full art 1/24.5, EX 1/18, secret 1/98.2, ACE 1/18, holo 1/4.4
    ('BW07', 'Secret Rare', 98),
    ('BW07', 'Rare Ace', 18),
    ('BW07', 'Ultra Rare', 10),
    ('BW07', 'Holo Rare', 4),
    ('BW07', 'Rare', 1),
    ('BW07', 'Uncommon', 1),
    ('BW07', 'Common', 1),
    -- BW08 Plasma Storm: full art 1/42.2, EX 1/13.5, secret 1/75, ACE 1/24, holo 1/4.5
    ('BW08', 'Secret Rare', 75),
    ('BW08', 'Rare Ace', 24),
    ('BW08', 'Ultra Rare', 10),
    ('BW08', 'Holo Rare', 5),
    ('BW08', 'Rare', 1),
    ('BW08', 'Uncommon', 1),
    ('BW08', 'Common', 1),
    -- BW09 Plasma Freeze: full art 1/26.3, EX 1/18, secret 1/120, ACE 1/36, holo 1/4.3
    ('BW09', 'Secret Rare', 120),
    ('BW09', 'Rare Ace', 36),
    ('BW09', 'Ultra Rare', 11),
    ('BW09', 'Holo Rare', 4),
    ('BW09', 'Rare', 1),
    ('BW09', 'Uncommon', 1),
    ('BW09', 'Common', 1),
    -- BW10 Plasma Blast: full art 1/35.5, EX 1/18, secret 1/112.5, ACE 1/24, holo 1/4.2
    ('BW10', 'Secret Rare', 113),
    ('BW10', 'Rare Ace', 24),
    ('BW10', 'Ultra Rare', 12),
    ('BW10', 'Holo Rare', 4),
    ('BW10', 'Rare', 1),
    ('BW10', 'Uncommon', 1),
    ('BW10', 'Common', 1)
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
