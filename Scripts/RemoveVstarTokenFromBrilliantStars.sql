-- Removes the VSTAR Token from Brilliant Stars. It is not a card.
--
-- A ONE-OFF CLEANUP, NOT A MIGRATION. It corrects one bad row that got into the catalog; it says
-- nothing about the schema and a rebuilt database should not have to replay it. It ran once
-- against the live catalog on 2026-09-23 and is kept here so the correction is on record and can
-- be re-applied if the row ever comes back. See "Rules for catalog data" in CLAUDE.md.
--
-- WHAT IT WAS. The token is the cardboard marker that ships in every Brilliant Stars pack for
-- tracking the once-per-game VSTAR Power. TCGplayer sells it, so it carries a product rarity in
-- their data and it was loaded like any other card -- with no collector number and with cardType
-- UNKNOWN, the placeholder written when the source gives nothing to map.
--
-- WHAT IT COST. It was the only card in Brilliant Stars with rarity "Promo", so the set carried a
-- [Promo] row in [rarityBySetFilterOption]: a Promo option in the advanced search for a set that
-- has no promos, and a row in /admin/setRarity that no pull rate could ever fill.
--
-- IT IS NOT IN THE CSV EXPORTS. All 172 files under TCGCSV/ were searched by parsed productId and
-- by name, and neither 264925 nor a "VSTAR Token" row appears in any of them -- including both
-- Brilliant Stars files. So the row came from somewhere else, an older export or a direct
-- TCGplayer fetch, and re-running the ETL over the current CSVs will not bring it back.
--
-- IDENTIFIED BY tcgPlayerId, not by the local row id: ids are assigned per database and differ
-- between one catalog and the next, while the TCGplayer product id is the card's own identity.
--
-- CHECKED BEFORE DELETING: no binderCards or binderTray row in the application database referred
-- to this card and it carried no cardTags. Its one nonPkmnCardText row is removed here with it.
--
-- The image file this card pointed at is left alone; it lives outside the repository.

DELETE FROM [nonPkmnCardText]
WHERE [cardId] IN (SELECT [id] FROM [cards] WHERE [tcgPlayerId] = 264925);

DELETE FROM [cards]
WHERE [tcgPlayerId] = 264925;

-- Only once the card is gone, and only if nothing else in the set claims that rarity -- written
-- as a check rather than a straight delete so that re-running this, or running it against a
-- catalog where Brilliant Stars has a genuine promo, cannot remove a row that is still in use.
DELETE FROM [rarityBySetFilterOption]
WHERE [rarity] = 'Promo'
  AND [setId] = (SELECT [id] FROM [sets] WHERE [code] = 'SWSH09')
  AND NOT EXISTS (
      SELECT 1 FROM [cards]
      WHERE [cards].[setId] = [rarityBySetFilterOption].[setId]
        AND [cards].[rarity] = [rarityBySetFilterOption].[rarity]
  );

-- The browsers cache their copy of [rarityBySet], and a group has just lost a row, so the stamp
-- moves with it in the same script -- same reasoning as every seeding script before this one.
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
