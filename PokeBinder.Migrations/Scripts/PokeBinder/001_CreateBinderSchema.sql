CREATE TABLE IF NOT EXISTS [binderSizes] (
    [id] INTEGER PRIMARY KEY,
    [cardCount] INTEGER NOT NULL,
    [description] TEXT NOT NULL,
    [x] INTEGER NOT NULL,
    [y] INTEGER NOT NULL,
    [pages] INTEGER GENERATED ALWAYS AS ([cardCount] / ([x] * [y])) STORED
);

CREATE TABLE IF NOT EXISTS [binder] (
    [id] INTEGER PRIMARY KEY,
    [name] TEXT NOT NULL,
    [description] TEXT NULL,
    [createdAt] INTEGER NOT NULL,
    [userId] INTEGER NOT NULL,
    [binderSizeId] INTEGER NOT NULL,
    FOREIGN KEY ([binderSizeId]) REFERENCES [binderSizes]([id])
);

CREATE TABLE IF NOT EXISTS [binderCards] (
    [binderId] INTEGER NOT NULL,
    [cardId] INTEGER NOT NULL,
    [indexInBinder] INTEGER NOT NULL,
    [isMissing] INTEGER NULL,
    PRIMARY KEY ([binderId], [indexInBinder]),
    FOREIGN KEY ([binderId]) REFERENCES [binder]([id])
);

CREATE TABLE IF NOT EXISTS [binderTray] (
    [binderId] INTEGER NOT NULL,
    [cardId] INTEGER NOT NULL,
    [quantity] INTEGER NOT NULL CHECK ([quantity] > 0),
    PRIMARY KEY ([binderId], [cardId]),
    FOREIGN KEY ([binderId]) REFERENCES [binder]([id])
);
