CREATE TABLE IF NOT EXISTS [pkmnCardText] (
    [id] INTEGER PRIMARY KEY,
    [cardId] INTEGER NOT NULL,
    [hp] TEXT NULL,
    [resistance] TEXT NULL,
    [weaknesses] TEXT NULL,
    [flavorText] TEXT NULL,
    [retreat] TEXT NULL,
    [attackList] TEXT NULL,
    [ability] TEXT NULL,
    [evolvesFrom] TEXT NULL,
    [dexNumber] TEXT NULL,
    [stage] TEXT NULL,
    FOREIGN KEY([cardId]) REFERENCES [cards]([id])
);

CREATE TABLE IF NOT EXISTS [nonPkmnCardText] (
    [id] INTEGER PRIMARY KEY,
    [cardId] INTEGER NOT NULL UNIQUE REFERENCES [cards]([id]),
    [text] TEXT NULL,
    FOREIGN KEY([cardId]) REFERENCES [cards]([id])
);

ALTER TABLE [cards] ADD COLUMN [pkmnCardTextId] INTEGER NULL REFERENCES [pkmnCardText]([id]);
ALTER TABLE [cards] ADD COLUMN [nonPkmnCardTextId] INTEGER NULL REFERENCES [nonPkmnCardText]([id]);