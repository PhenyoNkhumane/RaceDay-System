IF OBJECT_ID(N'dbo.SessionCache', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.SessionCache (
		Id NVARCHAR(449) NOT NULL,
		Value VARBINARY(MAX) NOT NULL,
		ExpiresAtTime DATETIMEOFFSET NOT NULL,
		SlidingExpirationInSeconds BIGINT NULL,
		AbsoluteExpiration DATETIMEOFFSET NULL,
		CONSTRAINT PK_SessionCache PRIMARY KEY CLUSTERED (Id ASC)
	);
END;
GO

IF NOT EXISTS (
	SELECT 1
	FROM sys.indexes
	WHERE name = N'IX_SessionCache_ExpiresAtTime'
	  AND object_id = OBJECT_ID(N'dbo.SessionCache')
)
BEGIN
	CREATE NONCLUSTERED INDEX IX_SessionCache_ExpiresAtTime
		ON dbo.SessionCache (ExpiresAtTime ASC);
END;
GO
