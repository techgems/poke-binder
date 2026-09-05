-- How many pages a binder has is a property of that binder, not of its grid: two collectors can
-- buy the same 3x3 portfolio in a 20-page and a 40-page edition. So [pages] moves to [binder], and
-- what stays on [binderSizes] is [defaultPages] -- the length binders of that grid are usually
-- sold with, used to prefill the field when a binder is created. It is a recommendation, not a
-- limit.
--
-- [binderSizes].[cardCount] goes away with it. It was only ever x * y * pages, and with pages gone
-- there is nothing left on the row to multiply. The derived counts now live on the entities as
-- read-only properties (BinderSize.CardsPerPage, BinderSize.DefaultCardCount, Binder.CardCount),
-- so there is one definition of the arithmetic instead of a stored copy to keep in sync.
--
-- Both tables are rebuilt rather than altered: SQLite cannot drop a column that a stored generated
-- column reads, and cannot add a CHECK constraint to an existing table.

PRAGMA foreign_keys = off;
PRAGMA legacy_alter_table = on;

-- ---------------------------------------------------------------------------------------------
-- binder: gains its own page count
-- ---------------------------------------------------------------------------------------------
CREATE TABLE [binder_new] (
    [id] INTEGER PRIMARY KEY,
    [name] TEXT NOT NULL,
    [description] TEXT NULL,
    [createdAt] INTEGER NOT NULL,
    [userId] INTEGER NOT NULL,
    [binderSizeId] INTEGER NOT NULL,
    [pages] INTEGER NOT NULL CHECK ([pages] > 0),
    FOREIGN KEY ([binderSizeId]) REFERENCES [binderSizes]([id])
);

INSERT INTO [binder_new] ([id], [name], [description], [createdAt], [userId], [binderSizeId], [pages])
SELECT
    b.[id],
    b.[name],
    b.[description],
    b.[createdAt],
    b.[userId],
    b.[binderSizeId],
    -- Existing binders keep the length their size used to dictate, which is the length they were
    -- created under. The MAX guards the new CHECK against a size row that computed to zero pages.
    MAX(COALESCE(s.[pages], 1), 1)
FROM [binder] b
LEFT JOIN [binderSizes] s ON s.[id] = b.[binderSizeId];

DROP TABLE [binder];
ALTER TABLE [binder_new] RENAME TO [binder];

-- Every read of this table is "the binders belonging to one user".
CREATE INDEX IF NOT EXISTS [IX_binder_userId] ON [binder] ([userId]);

-- ---------------------------------------------------------------------------------------------
-- binderSizes: pages becomes a plain stored default, cardCount is dropped, and the one label
-- splits in two -- [name] is what the grid is called ("3x3"), [description] is the blurb that says
-- what that buys you ("9 cards per page"). One string holding both left the UI with nothing to
-- show but the whole thing, wherever it was shown.
-- ---------------------------------------------------------------------------------------------
CREATE TABLE [binderSizes_new] (
    [id] INTEGER PRIMARY KEY,
    [name] TEXT NOT NULL,
    [description] TEXT NOT NULL,
    [x] INTEGER NOT NULL CHECK ([x] > 0),
    [y] INTEGER NOT NULL CHECK ([y] > 0),
    [defaultPages] INTEGER NOT NULL CHECK ([defaultPages] > 0)
);

INSERT INTO [binderSizes_new] ([id], [name], [description], [x], [y], [defaultPages])
SELECT
    s.[id],
    -- Whatever the old single label said is the name; the blurb it did not have is derived from
    -- the grid, which is the only thing it could have been. The parentheses are load-bearing:
    -- SQLite binds || tighter than *, so without them this multiplies x by a string.
    s.[description],
    (s.[x] * s.[y]) || ' cards per page',
    s.[x],
    s.[y],
    MAX(COALESCE(s.[pages], 1), 1)
FROM [binderSizes] s;

DROP TABLE [binderSizes];
ALTER TABLE [binderSizes_new] RENAME TO [binderSizes];

-- A grid identifies the row, so the seed below can stay an INSERT OR IGNORE and nobody can add
-- "3x3" twice under two ids. 4x5 and 5x4 are deliberately different rows: same 20 pockets, but a
-- card lands in a different place on the page.
CREATE UNIQUE INDEX IF NOT EXISTS [IX_binderSizes_x_y] ON [binderSizes] ([x], [y]);

PRAGMA legacy_alter_table = off;
PRAGMA foreign_keys = on;
