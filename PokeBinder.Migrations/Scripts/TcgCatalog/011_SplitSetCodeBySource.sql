-- One code per data source on [sets]. The one [code] the table had was TCGplayer's (ME02, ME-AH),
-- loaded from TCGplayer's CSVs; TCGdex names the same sets differently (me02, me02.5), and more
-- sources may follow. A single column would mean whichever source last wrote it, so each source
-- gets one of its own.

-- The existing values are TCGplayer's, so the column is renamed rather than copied. The rename keeps
-- the column's UNIQUE NOT NULL: every set the catalog holds came from TCGplayer.
ALTER TABLE [sets] RENAME COLUMN [code] TO [tcgPlayerCode];

-- The TCGdex set id, which LoadSet fetches a set by and the price refresh matches a set by.
--
-- Nullable: the sets already loaded get theirs from Scripts/SeedTcgDexSetCodes.sql, which is catalog
-- data and so not run here, and a set TCGdex does not have keeps null for good.
ALTER TABLE [sets] ADD COLUMN [tcgDexCode] TEXT NULL;

-- Unique where set. SQLite treats every null as distinct in a unique index, so any number of sets
-- may still be without one.
CREATE UNIQUE INDEX IF NOT EXISTS [IX_sets_tcgDexCode] ON [sets] ([tcgDexCode]);
