CREATE TABLE IF NOT EXISTS [rarityBySetFilterOption] (
    [id] INTEGER NOT NULL PRIMARY KEY,
    [setId] INTEGER NOT NULL,
    [rarity] TEXT NOT NULL,
    FOREIGN KEY ([setId]) REFERENCES [sets]([id]),
    UNIQUE ([setId], [rarity])
)
