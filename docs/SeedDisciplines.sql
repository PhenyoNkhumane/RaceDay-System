USE RaceDay;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

INSERT INTO dbo.Discipline (Name, Description)
SELECT source.Name, source.Description
FROM (VALUES
	(N'Running', N'Running events, including road races, fun runs and competitive running events.'),
	(N'Track & Field', N'Track races, relays, jumps, and throwing events.'),
	(N'Trail Running', N'Off-road running events on natural terrain.'),
	(N'Road Cycling', N'Road-based cycling races and time trials.'),
	(N'Mountain Biking', N'Off-road cycling events on mountain-bike trails.'),
	(N'Swimming', N'Pool and open-water swimming events.'),
	(N'Triathlon', N'Multisport events combining swimming, cycling, and running.'),
	(N'Duathlon', N'Multisport events combining running and cycling.'),
	(N'Obstacle Course Racing', N'Running events with physical obstacles.'),
	(N'Power Walking', N'Competitive race-walking and power-walking events.')
) AS source(Name, Description)
WHERE NOT EXISTS (
	SELECT 1
	FROM dbo.Discipline AS existing
	WHERE existing.Name = source.Name
);

COMMIT TRANSACTION;
GO

SELECT Id, Name, Description
FROM dbo.Discipline
ORDER BY Name;
GO
