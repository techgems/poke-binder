CREATE TABLE IF NOT EXISTS [cardCondition] (
    [id] INTEGER PRIMARY KEY,
    [fullName] TEXT
);

CREATE TABLE IF NOT EXISTS [games] (
    [id] INTEGER PRIMARY KEY,
    [slug] TEXT UNIQUE,
    [name] TEXT
);

CREATE TABLE IF NOT EXISTS [generations] (
    [id] INTEGER PRIMARY KEY,
    [slug] TEXT UNIQUE,
    [name] TEXT,
    [startDateUnix] INTEGER,
    [endDateUnix] INTEGER,
    [gameId] INTEGER,
    FOREIGN KEY ([gameId]) REFERENCES [games]([id])
);

CREATE TABLE IF NOT EXISTS [sets] (
    [id] INTEGER PRIMARY KEY,
    [code] TEXT UNIQUE,
    [name] TEXT,
    [fullName] TEXT,
    [releaseDateUnix] INTEGER,
    [imageUrl] TEXT,
    [generationId] INTEGER,
    [priorityOrder] INTEGER DEFAULT 0,
    [dateLoadedUnix] INTEGER,
    FOREIGN KEY ([generationId]) REFERENCES [generations]([id])
);

CREATE TABLE IF NOT EXISTS [cards] (
    [id] INTEGER PRIMARY KEY,
    [tcgPlayerId] INTEGER UNIQUE,
    [setId] INTEGER,
    [name] TEXT,
    [rarity] TEXT,
    [cardNumber] TEXT,
    [stage] TEXT,
    [cardType] TEXT,
    [hp] INTEGER,
    [imageUrl] TEXT,
    [maskImageOneUrl] TEXT,
    [maskImageTwoUrl] TEXT,
    [hasImageDownloadAttempt] INTEGER DEFAULT 0,
    FOREIGN KEY ([setId]) REFERENCES [sets]([id])
);
