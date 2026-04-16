CREATE TABLE IF NOT EXISTS [cardCondition] (
    [id] INTEGER PRIMARY KEY,
    [fullName] TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS [games] (
    [id] INTEGER PRIMARY KEY,
    [slug] TEXT UNIQUE NOT NULL,
    [name] TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS [generations] (
    [id] INTEGER PRIMARY KEY,
    [slug] TEXT UNIQUE NOT NULL,
    [name] TEXT NOT NULL,
    [startDateUnix] INTEGER NOT NULL,
    [endDateUnix] INTEGER NULL,
    [gameId] INTEGER NOT NULL,
    FOREIGN KEY ([gameId]) REFERENCES [games]([id])
);

CREATE TABLE IF NOT EXISTS [sets] (
    [id] INTEGER PRIMARY KEY,
    [code] TEXT UNIQUE NOT NULL,
    [name] TEXT NOT NULL,
    [fullName] TEXT NOT NULL,
    [releaseDateUnix] INTEGER NOT NULL,
    [imageUrl] TEXT NULL,
    [generationId] INTEGER NOT NULL,
    [priorityOrder] INTEGER NULL,
    [dateLoadedUnix] INTEGER NOT NULL,
    FOREIGN KEY ([generationId]) REFERENCES [generations]([id])
);

CREATE TABLE IF NOT EXISTS [cards] (
    [id] INTEGER PRIMARY KEY,
    [tcgPlayerId] INTEGER UNIQUE,
    [scrydexId] INTEGER NULL UNIQUE,
    [setId] INTEGER NOT NULL,
    [name] TEXT NOT NULL,
    [rarity] TEXT NOT NULL,
    [cardNumber] TEXT NOT NULL,
    [stage] TEXT NULL,
    [cardType] TEXT NOT NULL,
    [cardSubType] TEXT NOT NULL,
    [artist] TEXT NULL,
    [imageUrl] TEXT NULL,
    [maskImageOneUrl] TEXT NULL,
    [maskImageTwoUrl] TEXT NULL,
    [hasImageDownloadAttempt] INTEGER DEFAULT 0,
    FOREIGN KEY ([setId]) REFERENCES [sets]([id])
);
