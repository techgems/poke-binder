CREATE TABLE IF NOT EXISTS [tags] (
    [id] INTEGER PRIMARY KEY,
    [name] TEXT NOT NULL UNIQUE,
    [extendedName] TEXT
);

CREATE TABLE IF NOT EXISTS [cardTags] (
    [cardId] INTEGER NOT NULL,
    [tagId] INTEGER NOT NULL,
    PRIMARY KEY ([cardId], [tagId]),
    FOREIGN KEY ([cardId]) REFERENCES [cards]([id]),
    FOREIGN KEY ([tagId]) REFERENCES [tags]([id])
);
