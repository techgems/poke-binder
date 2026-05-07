CREATE TABLE IF NOT EXISTS [users] (
    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
    [email] TEXT NOT NULL UNIQUE,
    [emailConfirmed] INTEGER NOT NULL DEFAULT 0,
    [passwordHash] TEXT NULL,
    [securityStamp] TEXT NULL,
    [concurrencyStamp] TEXT NULL,
    [phoneNumber] TEXT NULL,
    [phoneNumberConfirmed] INTEGER NOT NULL DEFAULT 0,
    [twoFactorEnabled] INTEGER NOT NULL DEFAULT 0,
    [lockoutEnd] TEXT NULL,
    [lockoutEnabled] INTEGER NOT NULL DEFAULT 0,
    [accessFailedCount] INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS [IX_users_email] ON [users] ([email]);

CREATE TABLE IF NOT EXISTS [roles] (
    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
    [name] TEXT NULL,
    [concurrencyStamp] TEXT NULL
);

CREATE TABLE IF NOT EXISTS [userRoles] (
    [userId] INTEGER NOT NULL,
    [roleId] INTEGER NOT NULL,
    PRIMARY KEY ([userId], [roleId]),
    FOREIGN KEY ([userId]) REFERENCES [users]([id]) ON DELETE CASCADE,
    FOREIGN KEY ([roleId]) REFERENCES [roles]([id]) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS [userClaims] (
    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
    [userId] INTEGER NOT NULL,
    [claimType] TEXT NULL,
    [claimValue] TEXT NULL,
    FOREIGN KEY ([userId]) REFERENCES [users]([id]) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS [IX_userClaims_userId] ON [userClaims] ([userId]);

CREATE TABLE IF NOT EXISTS [roleClaims] (
    [id] INTEGER PRIMARY KEY AUTOINCREMENT,
    [roleId] INTEGER NOT NULL,
    [claimType] TEXT NULL,
    [claimValue] TEXT NULL,
    FOREIGN KEY ([roleId]) REFERENCES [roles]([id]) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS [IX_roleClaims_roleId] ON [roleClaims] ([roleId]);

CREATE TABLE IF NOT EXISTS [userLogins] (
    [loginProvider] TEXT NOT NULL,
    [providerKey] TEXT NOT NULL,
    [providerDisplayName] TEXT NULL,
    [userId] INTEGER NOT NULL,
    PRIMARY KEY ([loginProvider], [providerKey]),
    FOREIGN KEY ([userId]) REFERENCES [users]([id]) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS [IX_userLogins_userId] ON [userLogins] ([userId]);

CREATE TABLE IF NOT EXISTS [userTokens] (
    [userId] INTEGER NOT NULL,
    [loginProvider] TEXT NOT NULL,
    [name] TEXT NOT NULL,
    [value] TEXT NULL,
    PRIMARY KEY ([userId], [loginProvider], [name]),
    FOREIGN KEY ([userId]) REFERENCES [users]([id]) ON DELETE CASCADE
);
