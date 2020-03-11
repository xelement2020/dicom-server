-- NOTE: This script DROPS AND RECREATES all database objects.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md


/*************************************************************
    Drop existing objects
**************************************************************/

DECLARE @sql nvarchar(max) =''

SELECT @sql = @sql + 'DROP PROCEDURE dicom.' + name + '; '
FROM sys.procedures

SELECT @sql = @sql + 'DROP TABLE dicom.' + name + '; '
FROM sys.tables

SELECT @sql = @sql + 'DROP TYPE dicom.' + name + '; '
FROM sys.table_types

SELECT @sql = @sql + 'DROP SEQUENCE dicom.' + name + '; '
FROM sys.sequences

SELECT @sql

EXEC(@sql)

GO

DROP SCHEMA dicom

GO

/*************************************************************
    Configure database
**************************************************************/

-- Enable RCSI
IF ((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON
END

-- Avoid blocking queries when statistics need to be rebuilt
IF ((SELECT is_auto_update_stats_async_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON
END

-- Use ANSI behavior for null values
IF ((SELECT is_ansi_nulls_on FROM sys.databases WHERE database_id = DB_ID()) = 0) BEGIN
    ALTER DATABASE CURRENT SET ANSI_NULLS ON
END

GO

/*************************************************************
    Schema bootstrap
**************************************************************/
CREATE SCHEMA dicom
GO

CREATE TABLE dicom.SchemaVersion
(
    Version int PRIMARY KEY,
    Status varchar(10)
)

INSERT INTO dicom.SchemaVersion
VALUES
    (1, 'started')

GO

--
--  STORED PROCEDURE
--      SelectCurrentSchemaVersion
--
--  DESCRIPTION
--      Selects the current completed schema version
--
--  RETURNS
--      The current version as a result set
--
CREATE PROCEDURE dicom.SelectCurrentSchemaVersion
AS
BEGIN
    SET NOCOUNT ON

    SELECT MAX(Version)
    FROM SchemaVersion
    WHERE Status = 'complete'
END
GO

--
--  STORED PROCEDURE
--      UpsertSchemaVersion
--
--  DESCRIPTION
--      Creates or updates a new schema version entry
--
--  PARAMETERS
--      @version
--          * The version number
--      @status
--          * The status of the version
--
CREATE PROCEDURE dicom.UpsertSchemaVersion
    @version int,
    @status varchar(10)
AS
    SET NOCOUNT ON

    IF EXISTS(SELECT *
        FROM dicom.SchemaVersion
        WHERE Version = @version)
    BEGIN
        UPDATE dicom.SchemaVersion
        SET Status = @status
        WHERE Version = @version
    END
    ELSE
    BEGIN
        INSERT INTO dicom.SchemaVersion
            (Version, Status)
        VALUES
            (@version, @status)
    END
GO
/*************************************************************
    Tables
**************************************************************/
--Mapping table for dicom retrieval
CREATE TABLE dicom.Instance (
	--instance keys
	StudyInstanceUID NVARCHAR(64) NOT NULL,
	SeriesInstanceUID NVARCHAR(64) NOT NULL,
	SOPInstanceUID NVARCHAR(64) NOT NULL,
	--data consitency columns
	Watermark BIGINT NOT NULL,
	Status TINYINT NOT NULL,
    LastStatusUpdatesDate DATETIME2(7) NOT NULL,
    --audit columns
	CreatedDate DATETIME2(7) NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_Instance ON dicom.Instance
(
    StudyInstanceUID,
    SeriesInstanceUID,
    SOPInstanceUID
)

--Table containing normalized standard Study tags
CREATE TABLE dicom.StudyMetadataCore (
	--Key
	ID BIGINT NOT NULL, --PK
	--instance keys
	StudyInstanceUID NVARCHAR(64) NOT NULL,
    Version INT NOT NULL,
	--patient and study core
	PatientID NVARCHAR(64) NOT NULL,
	PatientName NVARCHAR(64) NULL,
	--PatientNameIndex AS REPLACE(PatientName, '^', ' '), --FT index, TODO code gen not working 
	ReferringPhysicianName NVARCHAR(64) NULL,
	StudyDate DATE NULL,
	StudyDescription NVARCHAR(64) NULL,
	AccessionNumer NVARCHAR(16)  NULL,
)

--Table containing normalized standard Series tags
CREATE TABLE dicom.SeriesMetadataCore (
	--Key
	ID BIGINT NOT NULL, --FK
	--instance keys
	SeriesInstanceUID NVARCHAR(64) NOT NULL,
    Version INT NOT NULL,
	--series core
	Modality NVARCHAR(16) NULL,
	PerformedProcedureStepStartDate DATE NULL
) 
GO

/*************************************************************
    Sequence for generating unique 12.5ns "tick" components that are added
    to a base ID based on the timestamp to form a unique resource surrogate ID
**************************************************************/

CREATE SEQUENCE dicom.MetadataIdSequence
        AS BIGINT
        START WITH 0
        INCREMENT BY 1
        MINVALUE 0
        NO CYCLE
        CACHE 1000000
GO

/*************************************************************
    Stored procedures for adding an instance.
**************************************************************/
--
-- STORED PROCEDURE
--     AddInstance
--
-- DESCRIPTION
--     adds a DICOM instance
--
-- PARAMETERS
--     @studyInstanceUid
--         * A bigint to which a value between [0, 80000) is added, forming a unique ResourceSurrogateId.
--         * This value should be the current UTC datetime, truncated to millisecond precision, with its 100ns ticks component bitshifted left by 3.
--     @resourceTypeId
--         * The ID of the resource type (See ResourceType table)
--     @resourceid
--         * The resource ID (must be the same as the in the resource itself)
--     @allowCreate
--         * If false, an error is thrown if the resource does not already exist
--     @isDeleted
--         * Whether this resource marks the resource as deleted
--     @updatedDateTime
--         * The last modified time in the resource
--     @keepHistory
--         * Whether the existing version of the resource should be preserved
--     @requestMethod
--         * The HTTP method/verb used for the request
--     @rawResource
--         * A compressed UTF16-encoded JSON document
--     @resourceWriteClaims
--         * Claims on the principal that performed the write
--     @compartmentAssignments
--         * Compartments that the resource is part of
--     @referenceSearchParams
--         * Extracted reference search params
--     @tokenSearchParams
--         * Extracted token search params
--     @tokenTextSearchParams
--         * The text representation of extracted token search params
--     @stringSearchParams
--         * Extracted string search params
--     @numberSearchParams
--         * Extracted number search params
--     @quantitySearchParams
--         * Extracted quantity search params
--     @uriSearchParams
--         * Extracted URI search params
--     @dateTimeSearchParms
--         * Extracted datetime search params
--     @referenceTokenCompositeSearchParams
--         * Extracted reference$token search params
--     @tokenTokenCompositeSearchParams
--         * Extracted token$token tokensearch params
--     @tokenDateTimeCompositeSearchParams
--         * Extracted token$datetime search params
--     @tokenQuantityCompositeSearchParams
--         * Extracted token$quantity search params
--     @tokenStringCompositeSearchParams
--         * Extracted token$string search params
--     @tokenNumberNumberCompositeSearchParams
--         * Extracted token$number$number search params
--
-- RETURN VALUE
--         The version of the resource as a result set. Will be empty if no insertion was done.
--

CREATE PROCEDURE dicom.AddInstance
    @studyInstanceUID VARCHAR(64),
    @seriesInstanceUID VARCHAR(64),
    @sopInstanceUID VARCHAR(64),
    @patientId VARCHAR(64) = NULL,
    @patientName VARCHAR(64),
    @referringPhysicianName VARCHAR(64) = NULL,
    @studyDate DATE = NULL,
    @studyDescription VARCHAR(64) = NULL,
    @accessionNumber VARCHAR(64) = NULL,
    @modality VARCHAR(16) = NULL,
    @performedProcedureStepStartDate DATE = NULL
AS
    SET NOCOUNT ON

    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @currentDate DATETIME2(7) = GETUTCDATE()
    DECLARE @existingStatus TINYINT

    SELECT @existingStatus = Status
    FROM dicom.Instance WITH (UPDLOCK, HOLDLOCK)
    WHERE StudyInstanceUID = @studyInstanceUID AND SeriesInstanceUID = @seriesInstanceUID AND SOPInstanceUID = @sopInstanceUID

    IF (@existingStatus IS NULL) BEGIN
        -- The instance does not exist, insert it.
        INSERT INTO dicom.Instance
            (StudyInstanceUID, SeriesInstanceUID, SOPInstanceUID, Watermark, Status, LastStatusUpdatesDate, CreatedDate)
        VALUES
            (@studyInstanceUID, @seriesInstanceUID, @sopInstanceUID, 0, 0, @currentDate, @currentDate)

        -- Update the study metadata if needed.
        DECLARE @metadataId BIGINT

        SELECT @metadataId = ID
        FROM dicom.StudyMetadataCore WITH (NOLOCK)
        WHERE StudyInstanceUID = @studyInstanceUID

        IF (@metadataId IS NULL) BEGIN
            SET @metadataId = NEXT VALUE FOR dicom.MetadataIdSequence

            INSERT INTO dicom.StudyMetadataCore
                (ID, StudyInstanceUID, Version, PatientID, PatientName, ReferringPhysicianName, StudyDate, StudyDescription, AccessionNumer)
            VALUES
                (@metadataId, @studyInstanceUID, 0, @patientId, @patientName, @referringPhysicianName, @studyDate, @studyDescription, @accessionNumber)
        END
        --ELSE BEGIN
          -- TODO: handle the versioning
        --END

        IF NOT EXISTS (SELECT * FROM dicom.SeriesMetadataCore WHERE ID = @metadataId AND SeriesInstanceUID = @seriesInstanceUID) BEGIN
            INSERT INTO dicom.SeriesMetadataCore
                (ID, SeriesInstanceUID, Version, Modality, PerformedProcedureStepStartDate)
            VALUES
                (@metadataId, @seriesInstanceUID, 0, @modality, @performedProcedureStepStartDate)
        END
        --ELSE BEGIN
          -- TODO: handle the versioning
        --END
    END
    ELSE BEGIN
        -- TODO: handle the conflict case
        THROW 50409, 'Instance already exists', 1;
    END

    COMMIT TRANSACTION
GO