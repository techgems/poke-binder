-- Values are seeded by the ETL alongside the rest of the row; this only adds the column.
ALTER TABLE [cardTypeFilterOption] ADD COLUMN [imageUrl] TEXT NULL;
