-- Fusion Strike, from the only numbers TCGplayer ever published for it.
--
-- THE SOURCE IS AN INFOGRAPHIC, NOT AN ARTICLE. TCGplayer opened "over 4000 Fusion Strike Booster
-- Packs" and posted the result as a picture rather than as one of the pull-rate articles that
-- back 011 and 013: https://x.com/TCGplayer/status/1459273572665462786 (image
-- pbs.twimg.com/media/FEBhGLTXMAktefZ). The figures below were read off that image. It is the same
-- authority opening the same kind of sample, but it carries no confidence intervals and no
-- methodology note, and it cannot be re-read as text -- so if it ever needs checking, it needs
-- checking by eye.
--
-- WHAT THE IMAGE GIVES, per pack:
--   Alt-art VMAX 1/332   Alt-art V 1/180   Golden Rare 1/120   Rainbow Rare 1/127
--   Full-art Trainer 1/64   Full-art V 1/58   Holo VMAX 1/30
--   Hit Rate ("chance of opening an Ultra Rare or better") 1/5
--
-- FOLDED INTO THIS CATALOG'S BUCKETS the same way 013 does it, and the membership was read off
-- the cards themselves: [Ultra Rare] holds the 19 plain V, 8 VMAX, 15 Full Arts and 5
-- "(Alternate Full Art)" V cards; [Secret Rare] holds the 4 "(Alternate Art Secret)" VMAX plus
-- the 16 rainbow and gold secrets.
--
--   Secret Rare = 1/332 + 1/120 + 1/127 = 1.92% -> 1 in 52
--
--   Ultra Rare  = 20.00% ("Ultra Rare or better") - 1.92% (the secrets above) = 18.08% -> 1 in 6
--
-- THE ULTRA RARE VALUE IS THE SOFT ONE, and worth knowing before trusting it. The image gives no
-- rate for plain Pokémon V, which is the largest part of that bucket, so the bucket cannot be
-- added up from its parts the way every other set in 013 was -- its published pieces come to only
-- 7.18%. It is instead the headline hit rate minus the secrets. That headline is itself rounded
-- to one digit: read as 19% it gives 1 in 5.9, read as 21% it gives 1 in 5.2, so 5 and 6 are both
-- inside the source's own rounding and 6 is simply where the arithmetic lands at 20%.
--
-- Holo Rare at 3 and Common/Uncommon/Rare at 1, as in 013. The image's "Holo VMAX 1/30" is not
-- this row -- those are VMAX cards, which live in [Ultra Rare]; [Holo Rare] here is the 13 plain
-- holo rares, which no study measures.
WITH seed(code, rarity, pullRate) AS (
    VALUES
    ('SWSH08', 'Secret Rare', 52),
    ('SWSH08', 'Ultra Rare', 6),
    ('SWSH08', 'Holo Rare', 3),
    ('SWSH08', 'Rare', 1),
    ('SWSH08', 'Uncommon', 1),
    ('SWSH08', 'Common', 1)
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
