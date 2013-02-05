CREATE TABLE Header (
   Version Integer NOT NULL
);
CREATE TABLE Session (
   ID Integer PRIMARY KEY NOT NULL,
   Archive VarChar(256) NOT NULL,
   State SmallInt NOT NULL CHECK (State IN (1, 2, 3)),
   Flags Integer NOT NULL,
   Retrieved BigInt NOT NULL,
   Created DateTime DEFAULT CURRENT_TIMESTAMP NOT NULL
);
CREATE TABLE PathMap (
   ID Integer PRIMARY KEY NOT NULL,
   SessionID Integer NOT NULL REFERENCES Session (ID) ON DELETE CASCADE,
   NodeID Integer NOT NULL,
   Path VarChar(512) NOT NULL
);
CREATE UNIQUE INDEX AK_PathMap_SessionID_NodeID
   ON PathMap (SessionID, NodeID);
CREATE TABLE Retrieval (
   ID Integer PRIMARY KEY NOT NULL,
   SessionID Integer NOT NULL REFERENCES Session (ID) ON DELETE CASCADE,
   Blob VarChar(512) NOT NULL,
   Name VarChar(512) NULL,
   Offset BigInt NOT NULL,
   Length BigInt NOT NULL
);
CREATE UNIQUE INDEX AK_Retrieval_SessionID_Blob_Offset
   ON Retrieval (SessionID, Blob, Offset);
CREATE TABLE Entry (
   ID Integer PRIMARY KEY NOT NULL,
   BackupEntryID Integer NOT NULL,
   SessionID Integer NOT NULL REFERENCES Session (ID) ON DELETE CASCADE,
   RetrievalID Integer NOT NULL REFERENCES Retrieval (ID) ON DELETE CASCADE,
   State SmallInt NOT NULL CHECK (State IN (1, 2, 3)),
   Offset BigInt NOT NULL,
   Length BigInt NOT NULL
);
CREATE UNIQUE INDEX AK_RetrievalID_Offset
   ON Entry (RetrievalID, Offset);
CREATE UNIQUE INDEX AK_Entry_SessionID_State_RetrievalID_Offset
   ON Entry (SessionID, State, RetrievalID, Offset);
