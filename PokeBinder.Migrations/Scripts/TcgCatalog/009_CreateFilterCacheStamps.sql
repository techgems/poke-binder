-- One row per advanced-search filter group that the browser is allowed to cache. The stamp is an
-- opaque value the client stores alongside its copy of that group and hands back on its next
-- visit: equal means the copy is current, different means re-fetch that group and nothing else.
--
-- Only the five groups that age are here. Card types and super types are deliberately absent --
-- they stay embedded in the binder page, so they never need a stamp, a storage slot or a fetch.
--
-- Nothing derives these values. They are bumped by hand from /Admin/FilterCache, and -- once
-- LoadSet is built -- by that page for [Sets] and [RarityBySet] in the same transaction as the
-- rows it loads, so new data and a new stamp cannot land separately.
CREATE TABLE IF NOT EXISTS [filterCacheStamps] (
    [groupName] TEXT NOT NULL PRIMARY KEY,
    [stamp] TEXT NOT NULL,
    [lastBumpedUnix] INTEGER NOT NULL
) WITHOUT ROWID;

-- Seeded so the admin screen opens on the five groups rather than on an empty table, and so the
-- filters have a stamp to be served with before anyone has bumped anything.
--
-- The values are random rather than literal, and that is the point: a catalog database rebuilt
-- from scratch hands out stamps no client has ever held, instead of re-issuing the same five
-- constants to a browser still holding data from the database that came before it. GUID-shaped
-- only so that a seeded stamp and a bumped one (Guid.NewGuid from the app) look alike in the admin
-- table; nothing parses either, the comparison is string equality.
--
-- OR IGNORE so re-running against a database seeded by hand cannot fail on the primary key.
INSERT OR IGNORE INTO [filterCacheStamps] ([groupName], [stamp], [lastBumpedUnix])
SELECT
    [groupName],
    lower(
        substr(hex(randomblob(4)), 1, 8) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(2)) || '-' ||
        hex(randomblob(6))
    ),
    unixepoch()
-- The names are the FilterCacheGroup enum's member names: EF stores the enum by name, so these
-- strings are the contract between the table and the code, and renaming a member is a migration.
FROM (
    SELECT 'Pokemon' AS [groupName]
    UNION ALL SELECT 'Generations'
    UNION ALL SELECT 'Series'
    UNION ALL SELECT 'Sets'
    UNION ALL SELECT 'RarityBySet'
);
