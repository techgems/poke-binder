-- The EX era, weighted from community counts -- ThePriceDex again, with the same caveats as 015
-- through 017: self-described estimates from community data, no sample sizes, one source per set.
--
-- FOLDED INTO THIS CATALOG'S BUCKETS, membership read off the cards themselves:
--   * [Ultra Rare] holds the "ex" cards and, from Team Rocket Returns onwards, the Gold Stars --
--     Mudkip Star, Entei Star, Charizard Star and the rest sit in the same row as Blastoise ex.
--     So it is "Rare Holo EX" + "Gold Star" added, for the nine sets that have Gold Stars, and
--     the ex rate alone for the six that do not.
--   * [Secret Rare] takes the Secret Rare rate where the source gives one.
--   * [Holo Rare] is the plain holo rate -- except in Unseen Forces, where the 28 Unown cards are
--     filed as Holo Rares in this catalog, so the source's "Unown Special Rare" 1 in 5 is added
--     to the holo rate. That is why that set reads 2 where its neighbours read 4.
--
-- Common, Uncommon and Rare are 1, as everywhere else.
--
-- SIX SECRET RARES HAVE NO RATE AND ARE LEFT NULL: Hidden Legends, Deoxys, Emerald, Delta
-- Species, Legend Maker and Holon Phantoms. Each holds exactly one secret card -- Groudon,
-- Rocket's Raikou ex, Farfetch'd, Azumarill, Pikachu (Delta Species) and Mew -- and the source
-- lists no rate for any of them. Be aware of what that costs: an unweighted row sorts below a
-- weighted one, so in those six sets the rarest card in the set currently sorts beneath its
-- commons. They are six values to type into /admin/setRarity, and they are deliberately not
-- guessed here.
--
-- RUBY & SAPPHIRE IS ABSENT ON PURPOSE. It is already weighted by hand, and its ThePriceDex page
-- is plainly broken -- commons at "1 in 7.8 packs" when every pack holds four or five of them,
-- holo rares rarer than the ex cards. Nothing on that page was used, and the hand-entered values
-- are left exactly as they are.
--
-- Matched by (set code, rarity name) rather than by id, and COALESCE so a value already in place
-- is not blanked -- both for the reasons 011 gives at greater length.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    -- EX02 Sandstorm: ex 1/6, holo 1/6
    ('EX02', 'Ultra Rare', 6),
    ('EX02', 'Holo Rare', 6),
    ('EX02', 'Rare', 1),
    ('EX02', 'Uncommon', 1),
    ('EX02', 'Common', 1),
    -- EX03 Dragon: ex 1/6, secret 1/30, holo 1/7.5
    ('EX03', 'Secret Rare', 30),
    ('EX03', 'Ultra Rare', 6),
    ('EX03', 'Holo Rare', 8),
    ('EX03', 'Rare', 1),
    ('EX03', 'Uncommon', 1),
    ('EX03', 'Common', 1),
    -- EX04 Team Magma vs Team Aqua: ex 1/12, secret 1/52, holo 1/4.3
    ('EX04', 'Secret Rare', 52),
    ('EX04', 'Ultra Rare', 12),
    ('EX04', 'Holo Rare', 4),
    ('EX04', 'Rare', 1),
    ('EX04', 'Uncommon', 1),
    ('EX04', 'Common', 1),
    -- EX05 Hidden Legends: ex 1/12, holo 1/4
    ('EX05', 'Ultra Rare', 12),
    ('EX05', 'Holo Rare', 4),
    ('EX05', 'Rare', 1),
    ('EX05', 'Uncommon', 1),
    ('EX05', 'Common', 1),
    -- EX06 FireRed & LeafGreen: ex 1/12, secret 1/36, holo 1/4.5
    ('EX06', 'Secret Rare', 36),
    ('EX06', 'Ultra Rare', 12),
    ('EX06', 'Holo Rare', 5),
    ('EX06', 'Rare', 1),
    ('EX06', 'Uncommon', 1),
    ('EX06', 'Common', 1),
    -- EX07 Team Rocket Returns: ex 1/13.5 + gold star 1/108, secret 1/220, holo 1/4.1
    ('EX07', 'Secret Rare', 220),
    ('EX07', 'Ultra Rare', 12),
    ('EX07', 'Holo Rare', 4),
    ('EX07', 'Rare', 1),
    ('EX07', 'Uncommon', 1),
    ('EX07', 'Common', 1),
    -- EX08 Deoxys: ex 1/13.5 + gold star 1/108, holo 1/4
    ('EX08', 'Ultra Rare', 12),
    ('EX08', 'Holo Rare', 4),
    ('EX08', 'Rare', 1),
    ('EX08', 'Uncommon', 1),
    ('EX08', 'Common', 1),
    -- EX09 Emerald: ex 1/12, holo 1/4
    ('EX09', 'Ultra Rare', 12),
    ('EX09', 'Holo Rare', 4),
    ('EX09', 'Rare', 1),
    ('EX09', 'Uncommon', 1),
    ('EX09', 'Common', 1),
    -- EX10 Unseen Forces: ex 1/14.6 + gold star 1/108, secret 1/175.5, holo 1/4 + Unown 1/5.0
    ('EX10', 'Secret Rare', 176),
    ('EX10', 'Ultra Rare', 13),
    ('EX10', 'Holo Rare', 2),
    ('EX10', 'Rare', 1),
    ('EX10', 'Uncommon', 1),
    ('EX10', 'Common', 1),
    -- EX11 Delta Species: ex 1/36 + gold star 1/108, holo 1/3.4
    ('EX11', 'Ultra Rare', 27),
    ('EX11', 'Holo Rare', 3),
    ('EX11', 'Rare', 1),
    ('EX11', 'Uncommon', 1),
    ('EX11', 'Common', 1),
    -- EX12 Legend Maker: ex 1/18 + gold star 1/108, holo 1/3.7
    ('EX12', 'Ultra Rare', 15),
    ('EX12', 'Holo Rare', 4),
    ('EX12', 'Rare', 1),
    ('EX12', 'Uncommon', 1),
    ('EX12', 'Common', 1),
    -- EX13 Holon Phantoms: ex 1/36 + gold star 1/83.7, holo 1/3.4
    ('EX13', 'Ultra Rare', 25),
    ('EX13', 'Holo Rare', 3),
    ('EX13', 'Rare', 1),
    ('EX13', 'Uncommon', 1),
    ('EX13', 'Common', 1),
    -- EX14 Crystal Guardians: ex 1/12 + gold star 1/141.4, holo 1/4.1
    ('EX14', 'Ultra Rare', 11),
    ('EX14', 'Holo Rare', 4),
    ('EX14', 'Rare', 1),
    ('EX14', 'Uncommon', 1),
    ('EX14', 'Common', 1),
    -- EX15 Dragon Frontiers: ex 1/12 + gold star 1/121, holo 1/4.1
    ('EX15', 'Ultra Rare', 11),
    ('EX15', 'Holo Rare', 4),
    ('EX15', 'Rare', 1),
    ('EX15', 'Uncommon', 1),
    ('EX15', 'Common', 1),
    -- EX16 Power Keepers: ex 1/12 + gold star 1/80.7, holo 1/4.2
    ('EX16', 'Ultra Rare', 10),
    ('EX16', 'Holo Rare', 4),
    ('EX16', 'Rare', 1),
    ('EX16', 'Uncommon', 1),
    ('EX16', 'Common', 1)
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
