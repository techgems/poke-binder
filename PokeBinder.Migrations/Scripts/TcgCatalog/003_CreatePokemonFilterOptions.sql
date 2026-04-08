CREATE TABLE IF NOT EXISTS [pokemonFilterOptions] (
    [id] INTEGER NOT NULL PRIMARY KEY,
    [pokedexNumber] INTEGER NOT NULL,
    [name] TEXT NOT NULL,
    [generationId] INTEGER NOT NULL,
    [alternateName] TEXT,
    FOREIGN KEY ([generationId]) REFERENCES [generationFilterOptions]([id])
) WITHOUT ROWID;
