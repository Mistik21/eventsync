CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "TelegramID" bigint NOT NULL,
    "Name" character varying(100) NOT NULL,
    "AttendedEventCount" integer NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "Events" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "MainImageUrl" text NOT NULL,
    "EventStartTime" timestamp with time zone NOT NULL,
    "CollectionStatus" integer NOT NULL,
    "CollectedBudget" integer NOT NULL,
    "Budget" integer NOT NULL,
    "Organizer" uuid NOT NULL,
    CONSTRAINT "PK_Events" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Events_Users_Organizer" FOREIGN KEY ("Organizer") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Activities" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "Creator" uuid NOT NULL,
    "АctivityStartTime" timestamp with time zone NOT NULL,
    "АctivityEndTime" timestamp with time zone NOT NULL,
    "MaxParticipants" integer NOT NULL,
    "EventId" uuid,
    CONSTRAINT "PK_Activities" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Activities_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id"),
    CONSTRAINT "FK_Activities_Users_Creator" FOREIGN KEY ("Creator") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Cars" (
    "Id" uuid NOT NULL,
    "Driver" uuid NOT NULL,
    "MaxNumberSeats" integer NOT NULL,
    "Description" text NOT NULL,
    "LocationEnd_Address" text NOT NULL,
    "LocationEnd_Coordinates" text NOT NULL,
    "EventId" uuid,
    CONSTRAINT "PK_Cars" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Cars_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id"),
    CONSTRAINT "FK_Cars_Users_Driver" FOREIGN KEY ("Driver") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Costs" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Amount" integer NOT NULL,
    "EventId" uuid,
    CONSTRAINT "PK_Costs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Costs_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id")
);

CREATE TABLE "EventParticipants" (
    "EventId" uuid NOT NULL,
    "ParticipantsId" uuid NOT NULL,
    CONSTRAINT "PK_EventParticipants" PRIMARY KEY ("EventId", "ParticipantsId"),
    CONSTRAINT "FK_EventParticipants_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventParticipants_Users_ParticipantsId" FOREIGN KEY ("ParticipantsId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Items" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "Count" integer NOT NULL,
    "ResponsibleID" uuid,
    "UrlShop" text,
    "EventId" uuid,
    CONSTRAINT "PK_Items" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Items_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id"),
    CONSTRAINT "FK_Items_Users_ResponsibleID" FOREIGN KEY ("ResponsibleID") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE TABLE "ActivityParticipants" (
    "ActivityId" uuid NOT NULL,
    "ParticipantsId" uuid NOT NULL,
    CONSTRAINT "PK_ActivityParticipants" PRIMARY KEY ("ActivityId", "ParticipantsId"),
    CONSTRAINT "FK_ActivityParticipants_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ActivityParticipants_Users_ParticipantsId" FOREIGN KEY ("ParticipantsId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "CarPassengers" (
    "CarId" uuid NOT NULL,
    "PassengersId" uuid NOT NULL,
    CONSTRAINT "PK_CarPassengers" PRIMARY KEY ("CarId", "PassengersId"),
    CONSTRAINT "FK_CarPassengers_Cars_CarId" FOREIGN KEY ("CarId") REFERENCES "Cars" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CarPassengers_Users_PassengersId" FOREIGN KEY ("PassengersId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Activities_Creator" ON "Activities" ("Creator");

CREATE INDEX "IX_Activities_EventId" ON "Activities" ("EventId");

CREATE INDEX "IX_ActivityParticipants_ParticipantsId" ON "ActivityParticipants" ("ParticipantsId");

CREATE INDEX "IX_CarPassengers_PassengersId" ON "CarPassengers" ("PassengersId");

CREATE INDEX "IX_Cars_Driver" ON "Cars" ("Driver");

CREATE INDEX "IX_Cars_EventId" ON "Cars" ("EventId");

CREATE INDEX "IX_Costs_EventId" ON "Costs" ("EventId");

CREATE INDEX "IX_EventParticipants_ParticipantsId" ON "EventParticipants" ("ParticipantsId");

CREATE INDEX "IX_Events_Organizer" ON "Events" ("Organizer");

CREATE INDEX "IX_Items_EventId" ON "Items" ("EventId");

CREATE INDEX "IX_Items_ResponsibleID" ON "Items" ("ResponsibleID");

CREATE UNIQUE INDEX "IX_Users_TelegramID" ON "Users" ("TelegramID");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261118162247_InitialPostgres', '9.0.4');

COMMIT;

