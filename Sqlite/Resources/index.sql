CREATE TABLE Header (
   Version Integer NOT NULL,
   CryptoIterations Integer NOT NULL,
   ArchiveSalt Binary(128) NOT NULL,
   PasswordHash Binary(256) NOT NULL,
   PasswordSalt Binary(128) NOT NULL
);
CREATE TABLE Blob (
   ID Integer PRIMARY KEY NOT NULL,
   Name VarChar(256) NOT NULL,
   Length BigInt NOT NULL,
   Created DateTime DEFAULT CURRENT_TIMESTAMP NOT NULL,
   Updated DateTime DEFAULT CURRENT_TIMESTAMP NOT NULL
);
CREATE TABLE Session (
   ID Integer PRIMARY KEY NOT NULL,
   State SmallInt NOT NULL CHECK (State IN (1, 2, 3)),
   Created DateTime DEFAULT CURRENT_TIMESTAMP NOT NULL,
   EstimatedLength BigInt NOT NULL,
   ActualLength BigInt NOT NULL
);
CREATE TABLE Node (
   ID Integer PRIMARY KEY NOT NULL,
   ParentID Integer NULL REFERENCES Node (ID),
   Type SmallInt NOT NULL CHECK (Type IN (1, 2, 3)),
   Name VarChar(256) NOT NULL,
   CONSTRAINT CK_Node_Type_ParentID 
      CHECK ((Type = 1 OR ParentID IS NOT NULL) AND (Type <> 1 OR ParentID IS NULL))
);
CREATE INDEX IX_Node_ParentID
   ON Node (ParentID);
CREATE TABLE Entry (
   ID Integer PRIMARY KEY NOT NULL,
   SessionID Integer NOT NULL REFERENCES Session (ID),
   NodeID Integer NOT NULL REFERENCES Node (ID),
   BlobID Integer NULL REFERENCES Blob (ID),
   State SmallInt NOT NULL CHECK (State IN (1, 2, 3, 4)),
   Offset BigInt NOT NULL,
   Length BigInt NOT NULL,
   Crc32 Binary(4) NOT NULL,
   CONSTRAINT CK_Entry_Completed
      CHECK (State <> 2 OR (BlobID IS NOT NULL AND Offset >= 0 AND Length >= 0))
);
CREATE UNIQUE INDEX AK_Entry_NodeID_SessionID
   ON Entry (NodeID, SessionID);
CREATE INDEX IX_Entry_SessionID_State 
   ON Entry (SessionID, State);
