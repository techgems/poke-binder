-- What a rarity is worth, per set. A rarity's name does not say how hard it is to pull and the
-- answer differs between sets: a Special Illustration Rare is about 70 packs of opening in
-- [ME: Ascended Heroes] and about 20 in the 30th anniversary set, and a Mega Hyper Rare is about
-- 540 in the first of those. So the value an ordering needs is per row, which is what this table
-- already is -- one row per (setId, rarity).
--
-- Both columns are numbered so that BIGGER MEANS RARER. It is already true of a pull rate by
-- construction, and [nameRarityOrder] is numbered to match so that one label ("rare first") can
-- sit over whichever of the two a sort is using.
--
-- Both are nullable, and null means unweighted: nobody has said yet what this rarity is worth in
-- this set. Nothing here can guess either value -- a pull rate is a fact about the print run and a
-- name ordering is a judgement -- so every existing row starts null and /admin/setRarity is what
-- fills them in.

-- How many packs it takes before you are likely to have pulled this rarity from this set. Bigger
-- is harder, so bigger is rarer, and the unit is the same in every set: 540 here and 20 there mean
-- the same thing, which is what makes the value comparable across sets rather than a rank within
-- one.
--
-- Whole packs, and two rarities in one set may hold the same number: a pull rate is a
-- measurement, not a ranking, and two rarities really can take about the same number of packs.
-- Which is also what a rarity that comes in most packs is entered as -- a small number rather
-- than a fraction -- so the common tiers can quite legitimately round onto each other. A sort by
-- this column therefore meets ties, and needs the same tie-break it needs for the rarities nobody
-- has weighted.
ALTER TABLE [rarityBySetFilterOption] ADD COLUMN [pullRateRarityOrder] INTEGER NULL;

-- Deliberately NOT tied to probability: it ranks by what the rarity is CALLED, so the same name
-- lands near itself across sets and a sort can keep all the Special Illustration Rares together
-- instead of scattering them by how generous each set happened to be.
--
-- Same direction as the column above: Common low, Special Illustration Rare high.
ALTER TABLE [rarityBySetFilterOption] ADD COLUMN [nameRarityOrder] INTEGER NULL;

-- The browser caches its copy of [rarityBySet] against the group's stamp and knows nothing about
-- the shape of the rows in it. A browser that cached the group before this migration therefore
-- keeps rows without the two new fields -- from its point of view nothing has changed -- so the
-- stamp is moved here, by the script that changes the shape, rather than left to somebody
-- remembering /admin/filterCache on deploy day.
--
-- GUID-shaped and random, matching what the seed in 009 writes and what Guid.NewGuid gives the
-- Bump button. Nothing parses a stamp; the comparison is string equality.
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
