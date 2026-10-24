-- Card labels are short tags the collector gives the cards in a binder: up to three per card, in
-- the pocket's bottom-left corner, picked from a list the binder owns. A label is a short name
-- and one of eight colours; the collector creates them from the Label modal or from Settings,
-- applies them to as many cards as they like, renames and recolors them in Settings, and deletes
-- them from Settings too. Rename and recolor propagate across every chip of that label in the
-- binder; delete cascades through the join rows below.
--
-- Two tables:
--
-- [binderCardLabels] holds one row per label the binder owns. (binderId, name) is unique so the
-- picker never shows two "Trade" rows and the validator does not need a second pass to catch it.
-- ON DELETE CASCADE on [binderId] so labels go with their binder; this is stricter than the
-- existing [binderCards] FK, which leans on the app to clean up, but a label row left behind
-- after its binder is gone has nothing to point at.
--
-- [binderCardLabelAssignments] is the join row per (card, label) pair. [binderCards] has no
-- surrogate id (its primary key is composite), so the FK is composite too. ON DELETE CASCADE on
-- both sides keeps the join clean: a pocket emptied through SaveBinderChanges deletes its
-- [binderCards] row, and the join rows go with it; a label deleted from the binder takes every
-- chip of it with it. [orderIndex] is where in the chip strip the label sits (0, 1, 2), and the
-- CHECK is where the three-labels-per-card cap lives at the database level -- a belt alongside
-- the validator's braces.

CREATE TABLE IF NOT EXISTS [binderCardLabels] (
    [id] INTEGER PRIMARY KEY,
    [binderId] INTEGER NOT NULL,
    [name] TEXT NOT NULL,
    [color] TEXT NOT NULL,
    FOREIGN KEY ([binderId]) REFERENCES [binder]([id]) ON DELETE CASCADE
);

-- Unique per binder: "Trade" on binder A and "Trade" on binder B coexist, but one binder cannot
-- carry two labels with the same name.
CREATE UNIQUE INDEX IF NOT EXISTS [IX_binderCardLabels_binderId_name]
    ON [binderCardLabels] ([binderId], [name]);

-- Every read is "the labels belonging to one binder".
CREATE INDEX IF NOT EXISTS [IX_binderCardLabels_binderId]
    ON [binderCardLabels] ([binderId]);

CREATE TABLE IF NOT EXISTS [binderCardLabelAssignments] (
    [binderId] INTEGER NOT NULL,
    [indexInBinder] INTEGER NOT NULL,
    [labelId] INTEGER NOT NULL,
    [orderIndex] INTEGER NOT NULL CHECK ([orderIndex] BETWEEN 0 AND 2),
    PRIMARY KEY ([binderId], [indexInBinder], [labelId]),
    FOREIGN KEY ([binderId], [indexInBinder])
        REFERENCES [binderCards] ([binderId], [indexInBinder]) ON DELETE CASCADE,
    FOREIGN KEY ([labelId])
        REFERENCES [binderCardLabels] ([id]) ON DELETE CASCADE
);

-- The save path reads "the assignments on one card", which the PK's leading columns cover; the
-- delete-label path reads "the assignments of one label", which is the direction the PK does not.
CREATE INDEX IF NOT EXISTS [IX_binderCardLabelAssignments_labelId]
    ON [binderCardLabelAssignments] ([labelId]);
