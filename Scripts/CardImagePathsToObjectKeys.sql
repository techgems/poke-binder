-- Turns each card's imageUrl from a file path on the machine that ran the ETL into the object key
-- of that file in the card-image bucket:
--
--   C:\ImagesTCG\Ruby & Sapphire\83475_in_1000x1000.jpg  ->  Ruby & Sapphire/83475_in_1000x1000.jpg
--
-- The app appends the key to CardImages:BaseUrl, the CDN in front of the bucket, and no longer
-- knows where the ETL kept the files. The bucket is a copy of C:\ImagesTCG -- the AppHost's seeder
-- uploads it as it is -- so a path below that folder is already the key, with forward slashes.
--
-- THIS IS NOT A MIGRATION. It corrects catalog data; see "Rules for catalog data" in CLAUDE.md.
-- Re-runnable: a key no longer starts with the folder, so a second run matches nothing.
--
-- RUN IT AGAIN AFTER EVERY ETL LOAD. The ETL still records file paths, and the app cannot turn a
-- file path into a URL: until this has run, every card the load added points at an image the CDN
-- does not have.
--
-- Run it against the catalog the app reads:
--
--   sqlite3 Databases/TcgCatalog.db < Scripts/CardImagePathsToObjectKeys.sql

UPDATE [cards]
SET [imageUrl] = replace(substr([imageUrl], length('C:\ImagesTCG\') + 1), '\', '/')
WHERE [imageUrl] LIKE 'C:\ImagesTCG\%';
