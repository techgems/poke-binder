-- Removes the VSTAR Token from Brilliant Stars. It is not a card.
--
-- The token is the cardboard marker that ships in every Brilliant Stars pack for tracking the
-- once-per-game VSTAR Power. TCGplayer sells it, so it carries a product rarity in their data and
-- the ETL loaded it like anything else -- with no collector number, and with cardType UNKNOWN,
-- which is the placeholder the ETL writes when the source gives it nothing to map.
--
-- What it cost while it was here: it was the only card in Brilliant Stars with rarity "Promo", so
-- the set carried a [Promo] row in [rarityBySetFilterOption], which put a Promo option in the
-- advanced search for a set that has no promos and an unweightable row in /admin/setRarity that
-- no pull rate could ever fill.
--
-- IDENTIFIED BY tcgPlayerId, not by the local row id: ids are assigned per database and differ
-- between one catalog and the next, while the TCGplayer product id is the card's own identity and
-- is what the ETL upserts on.
--
-- CHECKED BEFORE DELETING: no binderCards or binderTray row in the application database refers to
-- this card, and it carries no cardTags. It has one nonPkmnCardText row, removed here with it.
--
-- THE ETL WILL PUT IT BACK. It upserts every row of the TCGplayer CSV on tcgPlayerId, so the next
-- load of this set re-creates both the card and the rarity row. Keeping it out for good means the
-- loader learning to skip rows with no collector number -- which is the set-loading page's
-- validation step in NEXT-STEPS, where "rows whose card type maps to UNKNOWN" is exactly this.
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
