-- The emails the app sends, as Liquid templates that are edited at runtime: one row per kind of
-- email. PokeBinder.Emails renders a row against that email's model and hands the result to
-- whichever provider sends mail.
--
-- [name] is what the code asks for ("sign-in") and is unique, so the code never depends on an id,
-- which differs between databases. [text] is the plain-text alternative and may be null for an
-- email that goes out as HTML only. [updatedAt] is Unix seconds, like [binder].[createdAt].
--
-- No rows here. A template is content that gets edited, not schema, so a migration that inserted
-- one would be the wrong tool for every later correction to it.

CREATE TABLE IF NOT EXISTS [emailTemplates] (
    [id] INTEGER PRIMARY KEY,
    [name] TEXT NOT NULL,
    [subject] TEXT NOT NULL,
    [html] TEXT NOT NULL,
    [text] TEXT NULL,
    [updatedAt] INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS [IX_emailTemplates_name]
    ON [emailTemplates] ([name]);
