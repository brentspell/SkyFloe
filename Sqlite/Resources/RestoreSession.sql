CREATE TABLE Header (
   Version Integer NOT NULL,
   Archive VarChar(256) NOT NULL
);
CREATE TABLE Retrieval (
   ID Integer PRIMARY KEY NOT NULL,
   Name VarChar(512) NULL,
   BlobID Integer NOT NULL,
   Offset BigInt NOT NULL,
   Length BigInt NOT NULL
);
CREATE INDEX IX_Retrieval_BlobID
   ON Retrieval (BlobID);
CREATE TABLE Entry (
   ID Integer PRIMARY KEY NOT NULL,
   RetrievalID Integer NULL REFERENCES RetrievalID (ID),
   State SmallInt NOT NULL CHECK (State IN (1, 2, 3)),
   Offset BigInt NOT NULL,
   Length BigInt NOT NULL
);
CREATE UNIQUE INDEX AK_Entry_RetrievalID_Offset
   ON Entry (RetrievalID, Offset);
