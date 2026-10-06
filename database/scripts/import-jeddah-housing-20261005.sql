-- Jeddah housing reconciliation, supplied workbook: بيانات_السكن_الموحدة_ERP_جدة.xlsx
-- SHA-256: 63a9d01296e68f85709a5892496a8ab0177e99b3911b526e32a09962627ce6b3
-- Source sheet: بيانات الرفع لنظام ERP. 3 housing records, 27 rooms, 215 beds, 139 occupants.
-- Reuses existing housing, floor and room IDs. Keeps existing residence periods unchanged.
-- No employee records are created or changed; missing identities use pending/name-only records.
-- Requires a bit command parameter @Commit. False executes and verifies, then rolls back.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF @Commit IS NULL SET @Commit=0;
DECLARE @now datetimeoffset(7)=TODATETIMEOFFSET(SYSUTCDATETIME(),'+00:00');
DECLARE @effective date='2026-10-05';
DECLARE @actor uniqueidentifier='019c18d5-62e1-7000-c000-000000000001';
DECLARE @source nvarchar(200)=N'Jeddah housing workbook 2026-10-05';
DECLARE @housing table (Id uniqueidentifier PRIMARY KEY, OldCode nvarchar(32), Code nvarchar(32), NameAr nvarchar(200), NameEn nvarchar(200), District nvarchar(200), Rooms int, Capacity int, Occupants int, BeforeJson nvarchar(max));
INSERT @housing (Id,OldCode,Code,NameAr,NameEn,District,Rooms,Capacity,Occupants) VALUES
    (N'01a0a427-51c1-7477-b93e-cee215d22268', N'JEDDAH SAFA', N'safa', N'سكن الصفا', N'JEDDAH SAFA', N'حي الصفا', 12, 84, 56),
    (N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'SAMER VILA1', N'SAMER', N'سكن السامر فيلا الاجواد', N'SAMER VILA 1', N'حي الأجواد / السامر', 9, 85, 53),
    (N'01a0a5e0-535c-7236-afa6-3708df20f662', N'MECANIC', N'MECANIC', N'سكن السامر ميكانيكي (سكن جديد)', N'MECANIC', N'حي السامر', 6, 46, 30);

DECLARE @floors table (Id uniqueidentifier PRIMARY KEY,HousingId uniqueidentifier,Name nvarchar(100),OldName nvarchar(100));
INSERT @floors VALUES
    (N'460aa809-6d94-4221-b91b-c8c451ebb698', N'01a0a427-51c1-7477-b93e-cee215d22268', N'الدور الأول', N'1'),
    (N'01a10b15-805f-7649-b992-8ab99132ef5a', N'01a0a427-51c1-7477-b93e-cee215d22268', N'الدور الثاني', NULL),
    (N'01a10b15-805f-75ab-9b1d-de1100693853', N'01a0a427-51c1-7477-b93e-cee215d22268', N'الدور الثالث', NULL),
    (N'33bd880e-e714-41fb-a606-44b2852b31cb', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'الدور الأول - شقة 1', N'1'),
    (N'01a10b15-805f-7472-94b5-85e67caec87f', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'الدور الثاني - شقة 2', NULL),
    (N'01a10b15-805f-7d19-9d54-42f3d7188863', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'الدور الأرضي', NULL),
    (N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'الدور الأرضي', N'1'),
    (N'01a10b15-805f-73fb-9c75-e57a74e7d31a', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'الدور الأرضي - الورشة', NULL);

DECLARE @rooms table (Id uniqueidentifier PRIMARY KEY,HousingId uniqueidentifier,FloorId uniqueidentifier,OldName nvarchar(100),Name nvarchar(100),Capacity int,Occupants int);
INSERT @rooms VALUES
    (N'964529df-bda7-413c-8270-10e8e6a1d701', N'01a0a427-51c1-7477-b93e-cee215d22268', N'460aa809-6d94-4221-b91b-c8c451ebb698', N'1', N'غرفة 1', 10, 7),
    (N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'01a0a427-51c1-7477-b93e-cee215d22268', N'460aa809-6d94-4221-b91b-c8c451ebb698', N'2', N'غرفة 2', 8, 6),
    (N'01a0bf31-835b-7dd2-9663-68f635802ecc', N'01a0a427-51c1-7477-b93e-cee215d22268', N'460aa809-6d94-4221-b91b-c8c451ebb698', N'3', N'غرفة 3', 9, 3),
    (N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'01a0a427-51c1-7477-b93e-cee215d22268', N'460aa809-6d94-4221-b91b-c8c451ebb698', N'4', N'غرفة 4', 9, 7),
    (N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-7649-b992-8ab99132ef5a', N'5', N'غرفة 5', 7, 6),
    (N'01a0bf31-d10a-7bf1-bd2d-6244d8e26995', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-7649-b992-8ab99132ef5a', N'6', N'غرفة 6', 6, 3),
    (N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-7649-b992-8ab99132ef5a', N'7', N'غرفة 7', 6, 6),
    (N'01a0bf32-2a46-7f78-a538-f8926702e7ba', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-7649-b992-8ab99132ef5a', N'8', N'غرفة 8', 3, 3),
    (N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-75ab-9b1d-de1100693853', N'9', N'غرفة 9', 8, 4),
    (N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-75ab-9b1d-de1100693853', N'10', N'غرفة 10', 6, 4),
    (N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-75ab-9b1d-de1100693853', N'11', N'غرفة 11', 6, 6),
    (N'01a0bf32-956e-7580-b4b7-aace2cf8c13f', N'01a0a427-51c1-7477-b93e-cee215d22268', N'01a10b15-805f-75ab-9b1d-de1100693853', N'12', N'غرفة 12', 6, 1),
    (N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'33bd880e-e714-41fb-a606-44b2852b31cb', N'1', N'غرفة 1', 10, 5),
    (N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'33bd880e-e714-41fb-a606-44b2852b31cb', N'2', N'غرفة 2', 9, 6),
    (N'01a0b04e-157f-7ab4-8c68-e04a1070b506', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'33bd880e-e714-41fb-a606-44b2852b31cb', N'3', N'غرفة 3', 8, 4),
    (N'01a0b04e-3849-7b27-9ae5-63039b192391', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'33bd880e-e714-41fb-a606-44b2852b31cb', N'4', N'غرفة 4', 6, 3),
    (N'01a0b04e-5201-79fd-8e1c-20034f66fe32', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'01a10b15-805f-7472-94b5-85e67caec87f', N'5', N'غرفة 5', 4, 4),
    (N'01a0b04e-8305-7cfc-b20c-17ba4f011360', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'01a10b15-805f-7472-94b5-85e67caec87f', N'6', N'غرفة 6', 4, 3),
    (N'01a0b04e-9985-7e7f-90b1-f4b159f2e8cd', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'01a10b15-805f-7472-94b5-85e67caec87f', N'7', N'غرفة 7', 4, 2),
    (N'01a0b04f-1542-765c-bead-6cc277629be3', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'01a10b15-805f-7d19-9d54-42f3d7188863', N'8', N'غرفة 8', 24, 15),
    (N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'01a0a428-5ea7-710e-8e7f-2e8550e2c8be', N'01a10b15-805f-7d19-9d54-42f3d7188863', N'9', N'غرفة 9', 16, 11),
    (N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'1', N'غرفة 1', 10, 8),
    (N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'2', N'غرفة 2', 8, 7),
    (N'01a0bf35-02bc-7458-a7cf-a5ecf15f93a0', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'3', N'غرفة 3', 8, 4),
    (N'01a0bf35-19a4-7854-bcb0-0162a71151ec', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'4', N'غرفة 4', 7, 4),
    (N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'40e8dcab-df87-44e6-b480-21c75b4bdc9d', N'5', N'غرفة 5', 9, 4),
    (N'01a10b15-8061-71ae-8368-fb3cac974794', N'01a0a5e0-535c-7236-afa6-3708df20f662', N'01a10b15-805f-73fb-9c75-e57a74e7d31a', N'6', N'غرفة 6', 4, 3);

DECLARE @people table (SourceRow int PRIMARY KEY,RoomId uniqueidentifier,IqamaNo varchar(10),Name nvarchar(200),OccupantType nvarchar(100),PlatformWork nvarchar(200),Notes nvarchar(1000),PeriodId uniqueidentifier,OccupantId uniqueidentifier,EmployeeId uniqueidentifier);
INSERT @people (SourceRow,RoomId,IqamaNo,Name,OccupantType,PlatformWork,Notes,PeriodId,OccupantId) VALUES
    (2, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2612975926', N'مد زكريا زكريا احمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-73fe-b429-eee0478b3144', N'01a10b15-8061-7b26-88be-3dc444c01765'),
    (3, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2603760543', N'مد نظيم الدين', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-75e4-a4f4-3c9ad3d1af4e', N'01a10b15-8061-7b7b-8682-18656dcc7514'),
    (4, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2602833002', N'امتياز محمد ناهيد ناهيد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-79c9-98a7-b8063452d74a', N'01a10b15-8061-7d9c-b5b4-b3a46d2e665e'),
    (5, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2619453430', N'هارون الرشيد اشرف', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7d0e-9e35-ae104cd7feb1', N'01a10b15-8061-730e-a227-0ce587e39c7c'),
    (6, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2602433449', N'امران رحمن', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7f94-b348-0d7100decd2a', N'01a10b15-8061-7b5b-8867-30c028a05402'),
    (7, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2494774173', N'قمر ال حسن', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-75b9-b8d0-5bb645086988', N'01a10b15-8061-775c-a548-9b2074bce5a3'),
    (8, N'964529df-bda7-413c-8270-10e8e6a1d701', N'2603760170', N'محمد ريحان محمد ريحان', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7b76-8c12-892a94c614b8', N'01a10b15-8061-772a-b7fb-0cf8633f33a2'),
    (12, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'2611729878', N'راسل ميا', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7174-94c0-c4a2e9f4bb4c', N'01a10b15-8061-72d5-9ba5-99dc4dab4e29'),
    (13, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'2347120533', N'روبيل ميا خورشيد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-78ce-971c-f9bef951fafe', N'01a10b15-8061-79a9-a872-9d32732a0a85'),
    (14, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'2597241476', N'المغير حسين', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7656-85a9-109aeba3f767', N'01a10b15-8061-7c51-a569-427bf715678d'),
    (15, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', N'2509950487', N'بابل رفيق', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7d46-a341-fc6afb8d9aab', N'01a10b15-8061-7446-aacc-771dc039e2f8'),
    (16, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', NULL, N'محمد حسنين', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7a9d-b2fd-0f9f91f746fb', N'01a10b15-8061-7166-be35-6cc13d380d5c'),
    (17, N'01a0a5a7-0d7d-7963-8bda-b26a34d7622c', NULL, N'محمد دانيال', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-748d-982e-1bc099ce53fc', N'01a10b15-8061-7fe3-bb1b-d978d98df228'),
    (20, N'01a0bf31-835b-7dd2-9663-68f635802ecc', N'2576804195', N'عادل ابو احمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-73c3-848d-95496fc0b46d', N'01a10b15-8061-7234-9fc1-629d1c738471'),
    (21, N'01a0bf31-835b-7dd2-9663-68f635802ecc', N'2614040620', N'محمد شعبان عبدالله ابراهيم', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7998-8bfa-fcede6b8334b', N'01a10b15-8061-70b0-83a3-bd68086b3c8f'),
    (22, N'01a0bf31-835b-7dd2-9663-68f635802ecc', N'2609805110', N'كريم ربيع عبدالوهاب حسين احمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7111-bcda-2309f7159fc0', N'01a10b15-8061-7997-97a5-6e321b4b9318'),
    (29, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2551645738', N'احمد عزت احمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7dfa-801e-ada558670a0a', N'01a10b15-8061-72a7-a323-0fdea2ee9073'),
    (30, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2603496007', N'ايمن عبدالواحد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-798e-af55-cbd36da5bbaa', N'01a10b15-8061-7d52-ab58-e29ad3d3bd93'),
    (31, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2535636027', N'حسن نعمان', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7cf7-a1e9-2e23f73b494c', N'01a10b15-8061-7a6d-8fad-4dabead662e2'),
    (32, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2584539486', N'احمد عبدالواحد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7066-a6c5-f6a74942fcab', N'01a10b15-8061-7a7c-9cd3-5a00a9e27a51'),
    (33, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2620007464', N'احمد محمود عبد السلام', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-73a2-8ee2-f84ab2963c00', N'01a10b15-8061-74c5-9f22-426a3b66155d'),
    (34, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2581625023', N'محمد عماد علي', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7e88-96cc-8ecd896ef8aa', N'01a10b15-8061-77ee-939a-8b91a962b69e'),
    (35, N'01a0bf31-97c4-7931-b3cb-03e2cb8815cc', N'2545844504', N'عبد الرحمن زغلول', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7ff5-9ccf-143e3c0636f2', N'01a10b15-8061-7da8-8e1a-4f6a4f81dbe6'),
    (38, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2587645553', N'احمد طلعت', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-73ca-a18e-185c89c88c70', N'01a10b15-8061-78bc-9b07-fe0543158a75'),
    (39, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2591712555', N'حسن عبد السلام', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7acd-aa8e-07d8169913a8', N'01a10b15-8061-742b-985d-4beb793ca220'),
    (40, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2559746785', N'مروان عبد الامام', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-72a4-ba8f-829c19a716ca', N'01a10b15-8061-7fb6-980a-4eb262588ba5'),
    (41, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2589653290', N'احمد محمد عثمان', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7008-812b-deeb0e973603', N'01a10b15-8061-7251-a0c0-6f48ede179aa'),
    (42, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2611916483', N'ابراهيم طه', N'خارجي', N'كيتا', N'-', N'01a10b15-8061-7699-b348-56acb983712c', N'01a10b15-8061-7721-839c-775a12145205'),
    (43, N'01a0bf31-bd57-7fb0-af61-c6b8c7f98b74', N'2580392567', N'محمد عفيفي', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-796d-91eb-a80086c4f884', N'01a10b15-8061-73a5-a1c9-859c0d60570f'),
    (45, N'01a0bf31-d10a-7bf1-bd2d-6244d8e26995', N'2550771675', N'ابراهيم محمود', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-78f8-ab65-0a26699c9205', N'01a10b15-8061-76ac-9ef3-31ba8e9202ff'),
    (46, N'01a0bf31-d10a-7bf1-bd2d-6244d8e26995', N'2577794361', N'هشام عزت', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7909-a0a0-3b00acc2c1e9', N'01a10b15-8061-7b14-953e-2e87eebf9cb6'),
    (47, N'01a0bf31-d10a-7bf1-bd2d-6244d8e26995', N'2597443189', N'شهرزاد اقبال', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7d68-a56d-300f396869f2', N'01a10b15-8061-7102-a11f-8cee9feee53f'),
    (51, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'2505831657', N'شوقي فريد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7aa1-92c8-a84856bfdf14', N'01a10b15-8061-7461-be63-203cfbd579d0'),
    (52, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'2620868998', N'اسلام حسني', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-734e-8885-99619d94b0e1', N'01a10b15-8061-7398-9887-8d6243a68b08'),
    (53, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'2590110595', N'محمد رفعت', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-74b1-8ad5-4192541b0459', N'01a10b15-8061-7fbe-99af-bfee2d96bdbb'),
    (54, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'2593657261', N'عبدالرحمن يوسف', N'كفالة', N'اجازة', N'-', N'01a10b15-8061-7d2d-8fc2-e0eb13e87435', N'01a10b15-8061-7f23-89ef-24759baad540'),
    (55, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', N'2630354393', N'اسلام سمير', N'خارجي', N'كيتا', N'-', N'01a10b15-8061-799c-b133-bea8a65b490a', N'01a10b15-8061-7c3f-bd36-3509fff40d9c'),
    (56, N'01a0bf32-0ead-728e-a3c6-0ebb20cd0827', NULL, N'ابراهيم زعلوق', N'خارجي', N'كيتا', N'-', N'01a10b15-8061-779f-840f-d2c98f2e2cb3', N'01a10b15-8061-76b6-98a5-34bd501f5f4b'),
    (57, N'01a0bf32-2a46-7f78-a538-f8926702e7ba', N'2591323973', N'حسان', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-76d8-80b3-fc6a2b858294', N'01a10b15-8061-78e6-86b6-084aa330f42d'),
    (58, N'01a0bf32-2a46-7f78-a538-f8926702e7ba', N'2572718712', N'احمد صبري', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-71ff-a01c-063d1033ca0f', N'01a10b15-8061-7570-ada7-ba26cda2d49f'),
    (59, N'01a0bf32-2a46-7f78-a538-f8926702e7ba', N'2537012359', N'ياسر محمد محمد فايد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7834-9028-70e65c7a6967', N'01a10b15-8061-775a-afb4-e34b30631e4b'),
    (60, N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', NULL, N'معوض', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-71bc-847b-e4711be8ef0e', N'01a10b15-8061-79bb-88ee-e3359300c35c'),
    (61, N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', N'2621110614', N'ادهم حسن', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7180-861c-7173792c4bd7', N'01a10b15-8061-7561-bf5a-58c6134606df'),
    (62, N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', N'2618841361', N'محمد سيد محمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-71af-a208-820193cdf4bd', N'01a10b15-8061-7a57-8fc5-20941a7593bb'),
    (63, N'01a0bf32-4d95-76ab-8c7e-ea7f79afb5f2', N'2620008447', N'احمد هشام', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-780b-8981-9d895ac286c3', N'01a10b15-8061-7d55-ba5e-0fb05086da3d'),
    (68, N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'2585700301', N'عبدالرحمن  الجمل', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-72ef-9512-f31bb2acae3c', N'01a10b15-8061-71d5-848d-4dc0d555b1b1'),
    (69, N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'2615930167', N'احمد عاطف', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7bbc-8904-58810098aebc', N'01a10b15-8061-7b95-86cc-73edd004e506'),
    (70, N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'2623569254', N'يوسف عبدالمقصود', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-79b6-98db-3163a01a4290', N'01a10b15-8061-7268-b1a1-3c6e28bcd7be'),
    (71, N'01a0bf32-6803-7ead-82a5-654c4cd61396', N'2643360973', N'باسم وجيه', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7a09-bd0b-b0ed8b166301', N'01a10b15-8061-7723-b728-3079ad5f294f'),
    (74, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2627732189', N'يسري ايمن', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-72d1-a179-e24ba6a259d3', N'01a10b15-8061-7c82-9f07-23c1867903d4'),
    (75, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2630564751', N'اسلام محمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7646-91b6-9a0d280e8cd2', N'01a10b15-8061-7db6-8d8c-aefa3cdfc6ae'),
    (76, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2630315881', N'كريم احمد محمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7f27-aa99-aede2a1a5cdc', N'01a10b15-8061-73c5-a325-43a6dd95e6b7'),
    (77, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2630343412', N'محمود عبد السلام', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-733b-8864-3b0a257078c4', N'01a10b15-8061-708d-82c6-28a425f019f9'),
    (78, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2630898415', N'احمد ماهر مهدي', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7ce7-b709-c74a504c5a56', N'01a10b15-8061-7cbe-8985-f36af921e1b7'),
    (79, N'01a0bf32-7ed3-7b0c-9a16-74c8e3e6b2df', N'2620688800', N'حسام الدين محمد', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7a83-afbe-0513f1573f62', N'01a10b15-8061-72c3-a7fd-436292eee5d1'),
    (80, N'01a0bf32-956e-7580-b4b7-aace2cf8c13f', N'2577662378', N'مجدي رجب', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7214-a6ef-bc3f3de75c7b', N'01a10b15-8061-7103-a77a-a924274d5e5a'),
    (86, N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'2616110728', N'محمد عادل محمد عقده', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7a7a-b150-f17d16e0a5e6', N'01a10b15-8061-77cc-87c1-e9fd9c7284b6'),
    (87, N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'2580422653', N'محمود محمد عبد العال الخراشي', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-782e-8e92-b732de483cb1', N'01a10b15-8061-715a-bdc5-cc6da5d9b1f6'),
    (88, N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'2614052658', N'محمد احمد محمد نوار', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-766e-97a3-6d81e8e61828', N'01a10b15-8061-7705-80d9-54f28196b3ad'),
    (89, N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'2575173816', N'مصطفي عبدالكريم عبده ابوحسين', N'كفالة', N'كيتا', N'حادث', N'01a10b15-8061-73e4-8a4c-a0ad45b6236a', N'01a10b15-8061-7669-880c-c2ad48154023'),
    (90, N'c25d6333-095f-4e54-bb39-674b21fe4e52', N'2588354015', N'احمد حسني', N'كفالة', N'كيتا', N'حادث', N'01a10b15-8061-7e32-8de7-e65155dd2a7e', N'01a10b15-8061-7a55-bc4b-40caed548129'),
    (96, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2545005833', N'حسن نجيب', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7464-847d-88cf07b40bc4', N'01a10b15-8061-7b5d-9518-c155718a2661'),
    (97, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2597582275', N'ابراهيم العدوي', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-72e6-88ef-9270c99c63f8', N'01a10b15-8061-72f9-9f1c-cde033701f71'),
    (98, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2564959662', N'عبدالله مصطفي حميده سعد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7587-8a00-244c8e128c47', N'01a10b15-8061-7f36-9a4f-f9d2ca70de98'),
    (99, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2596606422', N'سيد حسني', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7d04-bc4e-e64bf4001109', N'01a10b15-8061-7b02-9545-90cdb079a34c'),
    (100, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2555284278', N'اسلام مصطفي محمد احمد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7895-8395-dc51a1874dbf', N'01a10b15-8061-7a85-af42-a6dfeab2c6e8'),
    (101, N'01a0a5e0-dafd-7cba-8b68-463ce6cea61f', N'2527203737', N'محمد رضا', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7a79-a152-0569dd7504e3', N'01a10b15-8061-775d-ae37-c2cc9049c885'),
    (105, N'01a0b04e-157f-7ab4-8c68-e04a1070b506', N'2636161834', N'هشام  احمد السيد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-76ae-a537-9a10684ba4b9', N'01a10b15-8061-788f-b9f7-e794146fe0a8'),
    (106, N'01a0b04e-157f-7ab4-8c68-e04a1070b506', N'2567197658', N'على بسيونى محمد هيكل', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7f27-b458-5718cbb5a8e7', N'01a10b15-8061-702c-b535-c785a7c1dea9'),
    (107, N'01a0b04e-157f-7ab4-8c68-e04a1070b506', N'2620778767', N'محمد جلال محمود فهمي', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7e51-a40c-021db91ce72f', N'01a10b15-8061-740c-8d38-8379b81f23d3'),
    (108, N'01a0b04e-157f-7ab4-8c68-e04a1070b506', NULL, N'مصطفى عارف', N'كفالة', N'كيتا', N'اجازة', N'01a10b15-8061-7a3b-b12b-8667e91dc88c', N'01a10b15-8061-7200-a54a-45bba8adf3f6'),
    (113, N'01a0b04e-3849-7b27-9ae5-63039b192391', N'2567173170', N'حسام محمد السيد سليمان', N'خارجي', N'كيتا', N'خارجي', N'01a10b15-8061-7b07-aed5-93bafeee13bf', N'01a10b15-8061-75dd-a27e-10236d8e2f83'),
    (114, N'01a0b04e-3849-7b27-9ae5-63039b192391', N'2591566365', N'حماده على مرعى سليمان', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-74c8-b215-710a7d1c0853', N'01a10b15-8061-7e1f-9e49-0849deabcdb1'),
    (115, N'01a0b04e-3849-7b27-9ae5-63039b192391', N'2531511265', N'عبدالحميد عادل', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7d91-86ae-3ac46bcca3e7', N'01a10b15-8061-73c5-b8e5-415c8ac53594'),
    (119, N'01a0b04e-5201-79fd-8e1c-20034f66fe32', NULL, N'التماس', N'كفالة', N'كيتا', N'عامل', N'01a10b15-8061-754d-80e6-fae202b36bd9', N'01a10b15-8061-7d82-9436-16293d0e35af'),
    (120, N'01a0b04e-5201-79fd-8e1c-20034f66fe32', N'2581119480', N'محمد سمان محمد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7d46-861a-5ff43f64c6e9', N'01a10b15-8061-7498-914f-f7dd43962fb3'),
    (121, N'01a0b04e-5201-79fd-8e1c-20034f66fe32', N'2605375407', N'راجا علي', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7c1c-a6eb-a598800bf698', N'01a10b15-8061-776b-b9e2-e1541a0bfee5'),
    (122, N'01a0b04e-5201-79fd-8e1c-20034f66fe32', N'2632579013', N'حسان زبير', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-79cc-b76c-786958d57142', N'01a10b15-8061-765c-9534-2d19f1ec93d8'),
    (123, N'01a0b04e-8305-7cfc-b20c-17ba4f011360', N'2578632750', N'فارس محمد مصباح الزغبي', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7f0c-a984-1df91fe229f2', N'01a10b15-8061-7ab2-b431-c8662a313cbc'),
    (124, N'01a0b04e-8305-7cfc-b20c-17ba4f011360', N'2581056997', N'خالد وليد رمزي عطية', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-763d-a4b7-a36bd37591a9', N'01a10b15-8061-72e3-ae57-dd103c264cef'),
    (125, N'01a0b04e-8305-7cfc-b20c-17ba4f011360', N'2578632909', N'محمود مصطفي محمود السعدوني', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-78ab-8f11-e150413086ec', N'01a10b15-8061-799b-930f-4e1bfaeb86dc'),
    (127, N'01a0b04e-9985-7e7f-90b1-f4b159f2e8cd', N'2580424253', N'محمود ياسر احمد محمود', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-71ff-a9dc-fdebfe1eca4c', N'01a10b15-8061-7157-917a-36267b78768e'),
    (128, N'01a0b04e-9985-7e7f-90b1-f4b159f2e8cd', N'2592789974', N'حسن شعبان سيد', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-72ca-9449-3e1a8b40c3b8', N'01a10b15-8061-7a26-87e7-4b32c3144c1b'),
    (131, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2487867026', N'مد أفضل سردار', N'كفالة', N'كيتا', N'حادث', N'01a10b15-8061-7789-ac47-2af097a120cb', N'01a10b15-8061-717b-8d8f-42f9b7f4c029'),
    (132, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2517672024', N'مد تبارك حسين', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7713-95ff-a69cb39565d2', N'01a10b15-8061-768f-af16-117cb3a2f4ca'),
    (133, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2615898091', N'فاروق ا', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7729-9a8a-76386a72d371', N'01a10b15-8061-7954-90fb-e8c41e8d0597'),
    (134, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2502057660', N'رقيب ميا', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7f67-af54-56ebb3fcb078', N'01a10b15-8061-7608-a537-a6e307421762'),
    (135, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2618863134', N'مد نظام', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7ef8-97ce-977b9a1abedb', N'01a10b15-8061-7f7f-b7df-aaec7aa5b66c'),
    (136, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2566240384', N'مد علال مياه', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-736f-bbba-878900b08910', N'01a10b15-8061-78b3-bbac-9d5e11f91eb4'),
    (137, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2609703588', N'مد ايمون ميا', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7bcc-9f3f-3d408c4c45a3', N'01a10b15-8061-71c5-96f7-beafae25dbf1'),
    (138, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2589753140', N'محمد زكريا اوسين', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7ddd-9313-6c884e30f796', N'01a10b15-8061-715f-8ec3-362282c49c07'),
    (139, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2582720773', N'طائف عبد الغني', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-73fb-8ca4-b03e929f3423', N'01a10b15-8061-7782-ad07-5cfb989e08b4'),
    (140, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2618245001', N'جهير اسلام', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-70c1-85e8-045f7872d611', N'01a10b15-8061-7f5d-b666-54c85707854d'),
    (141, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2555828033', N'محمد ديدال اسلام ديدار', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7f72-929e-a3f667d3f25e', N'01a10b15-8061-7f8a-bdc2-27aff25fb15a'),
    (142, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2589753306', N'سال احمد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7011-9637-88913d31f1be', N'01a10b15-8061-7361-be84-20a8a47a29d6'),
    (143, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2591472507', N'شكيل', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7a2e-8710-9ba73b373e29', N'01a10b15-8061-7217-aee8-32e117c1181c'),
    (144, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2615898232', N'حسين علي', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7b0d-b3d9-3af674bb4a48', N'01a10b15-8061-76ad-9f6d-4ea3d88f367f'),
    (145, N'01a0b04f-1542-765c-bead-6cc277629be3', N'2636062180', N'اسماعيل حسين الشاوون', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7da2-bd6f-8a48c0aa12a0', N'01a10b15-8061-7223-88d1-2ee16014642b'),
    (155, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2439739612', N'سابوج احمد', N'كفالة', N'كيتا', N'سيارة', N'01a10b15-8061-7cbb-afe9-a44f458ccee5', N'01a10b15-8061-78a8-b10d-ee362b6dcf78'),
    (156, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640405243', N'أختر ا ا حوسان', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-72c2-b7bc-0248d55bdb7a', N'01a10b15-8061-703b-a7b5-b4349a6e0f50'),
    (157, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2639883103', N'حضرت مد مونجو', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7b40-b9c4-57f584e22e47', N'01a10b15-8061-7bb9-a6ec-1451c93bacc6'),
    (158, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2639883293', N'محمد محبوب', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7cd5-9148-112e24dafb1c', N'01a10b15-8061-7500-a38f-681337f76525'),
    (159, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640335853', N'مد ارمان', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7c90-8c3c-6335e6efaee0', N'01a10b15-8061-7aa7-a750-85221540694c'),
    (160, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640405433', N'محمد سيف الله', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-75c4-bc8a-40bb5b917f98', N'01a10b15-8061-7367-b8e2-0c0a61c15406'),
    (161, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640405714', N'محمد روني ا ا أحمد', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7627-9d63-f16bac3f208e', N'01a10b15-8061-74bd-ab09-08fd50c147e2'),
    (162, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640405862', N'سين علام', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-76c8-8dd2-92334c4085f8', N'01a10b15-8061-7fe0-a025-46d611ad8773'),
    (163, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2639882725', N'محمد راشد راسل ميا ميا', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7717-abe3-71a73164494e', N'01a10b15-8061-7634-942c-c298c7fc0ee4'),
    (164, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2640405607', N'محمد ارفال', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7312-9b55-733392af2ad7', N'01a10b15-8061-71d7-a08b-67ee1441b32d'),
    (165, N'01a0b04f-596f-7edb-9787-2f990f35f4b9', N'2639777610', N'محمد ساكوت', N'كفالة', N'كيتا', N'دباب', N'01a10b15-8061-7915-80ba-27a3b238f828', N'01a10b15-8061-7b5c-8dd0-02b90fd60808'),
    (171, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2577093202', N'محمد حوريف الدين خان', N'كفالة', N'كيتا', N'لا يعمل', N'01a10b15-8061-75cd-b2e7-35bded4fa00a', N'01a10b15-8061-74a4-9cd5-9490ce825235'),
    (172, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2605750997', N'جيدني اسين', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-7ec5-bde2-398b398ddef9', N'01a10b15-8061-7ebb-b3e4-c2bd045755c4'),
    (173, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2574089302', N'مد سومون سومون سومون', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-75c6-b87d-e37560a8b03c', N'01a10b15-8061-7df3-823a-67d33c6b67b1'),
    (174, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2575597774', N'مد حسين سيمون', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-7d4b-aacd-e51cb582ee65', N'01a10b15-8061-7d59-bf3d-142d8e380484'),
    (175, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2629019411', N'مد ا حسين شهادات', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-7021-bd9a-51c7cffd1d38', N'01a10b15-8061-7e8e-a677-074c059edeee'),
    (176, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2590889420', N'مد ال كريم عبد', N'كفالة', N'كيتا', N'مصاب', N'01a10b15-8061-7487-a1f6-16b4f5ad843d', N'01a10b15-8061-7b7e-8b0e-221154d2b025'),
    (177, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2573670342', N'مد ماسود رانا', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-7b1b-b8e2-76317290fd43', N'01a10b15-8061-7b47-bf44-6519b10656ab'),
    (178, N'01a0a5e1-6e75-77cd-870e-3b16206f177e', N'2510890805', N'محمد مرشد علام', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-7056-9c19-af8aed96f3cf', N'01a10b15-8061-7fe5-9f83-70b98ea1ba9c'),
    (181, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2594172450', N'محمد شاه شاه عثمان', N'كفالة', N'كيتا', N'مصاب', N'01a10b15-8061-7760-9368-59d456163b67', N'01a10b15-8061-7ade-84ea-855372cf14f5'),
    (182, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2641773193', N'محمد وقاص صايف الله', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-789d-9bbc-7b16f4c7f394', N'01a10b15-8061-799a-bda7-46f9a7f0a06b'),
    (183, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2574083222', N'علي حسان محمود شاهد', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-792c-beb1-0d9f9f1f80fb', N'01a10b15-8061-74a8-bc1e-86fcc9eb7f07'),
    (184, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2639372743', N'مدثر علي', N'كفالة', N'امازون', N'شغال', N'01a10b15-8061-75df-a22b-cb833a1afc97', N'01a10b15-8061-7148-8921-f6a8337a9ddd'),
    (185, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2594172286', N'عرفان شاه عريف علي', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-765f-8d3d-25cba27185ca', N'01a10b15-8061-7fb0-a1fc-030a026210e4'),
    (186, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2605873690', N'محمد اليم هدايت وسيم', N'كفالة', N'كيتا', N'لا يعمل', N'01a10b15-8061-7169-aa84-efc5c6190760', N'01a10b15-8061-7f11-a12f-db06c35b676b'),
    (187, N'01a0bf34-e0b4-7e6a-93ce-87c0d38caccb', N'2553353455', N'عاطف علي عبدالستار', N'كفالة', N'هنجر', N'خارج الكفاله', N'01a10b15-8061-7f07-9911-78296389ba95', N'01a10b15-8061-70b2-ba08-1c8e9d5d3501'),
    (189, N'01a0bf35-02bc-7458-a7cf-a5ecf15f93a0', N'2625106170', N'RIDOY HASSAN', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-7e3f-8c93-317caee5c0f5', N'01a10b15-8061-7a4e-9bee-48dc7196060d'),
    (190, N'01a0bf35-02bc-7458-a7cf-a5ecf15f93a0', N'2574897464', N'محمد سجون ميا سجون', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-736a-a434-1923837a799b', N'01a10b15-8061-7d4b-bfb5-f66be725c491'),
    (191, N'01a0bf35-02bc-7458-a7cf-a5ecf15f93a0', N'2625106865', N'محمد نويون ميا', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-74d3-9dc1-c57560198296', N'01a10b15-8061-7ea1-96ef-4dd193a7c640'),
    (192, N'01a0bf35-02bc-7458-a7cf-a5ecf15f93a0', N'2509129876', N'محمد شهد الله', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-739e-9228-79a036e854e0', N'01a10b15-8061-7974-88e6-db2400b9c117'),
    (197, N'01a0bf35-19a4-7854-bcb0-0162a71151ec', N'2539395836', N'ا بو شا ترا', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-7d0d-a510-0c6bcf9e1a7f', N'01a10b15-8061-7548-843c-652d65e6f6e8'),
    (198, N'01a0bf35-19a4-7854-bcb0-0162a71151ec', N'2601104405', N'مد باشو ميا موتيل', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-7ca9-ad20-daeabbf563e2', N'01a10b15-8061-7c1d-b539-285db8cd08df'),
    (199, N'01a0bf35-19a4-7854-bcb0-0162a71151ec', N'2609815044', N'مد ابوزول اوسين', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-7c37-a53e-2f8758eeb55c', N'01a10b15-8061-7782-bc7c-e6a38b9047cc'),
    (200, N'01a0bf35-19a4-7854-bcb0-0162a71151ec', N'2584127175', N'ابو ابو الحسين الحسين', N'كفالة', N'كيتا', N'شغال', N'01a10b15-8061-7b66-b758-950ba3980df2', N'01a10b15-8061-7ae9-8bf9-65ddb244b21c'),
    (204, N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', N'2547821708', N'مد بابو ل مياه', N'خارجي', N'ميكانيكي', N'شغال', N'01a10b15-8061-7bcd-a706-529ba012a078', N'01a10b15-8061-72db-a2d0-bcd12345ddb4'),
    (205, N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', NULL, N'monirujjaman', N'كفالة', N'لا يوجد أقامه', N'جديد', N'01a10b15-8061-731e-ac34-f4268ebbb6a2', N'01a10b15-8061-77eb-b258-b154e1f1ee49'),
    (206, N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', N'2626683110', N'ناهد ميا', N'كفالة', N'هنجر', N'شغال', N'01a10b15-8061-75dd-8e54-0aed5e14f190', N'01a10b15-8061-7d3f-92da-151f6c3f7755'),
    (207, N'01a0bf35-3d3e-75b0-847b-ccdd7897cf9d', N'2610711232', N'مد لاودين مد لاودين', N'كفالة', N'امازون', N'شغال', N'01a10b15-8061-72ca-869a-b110e64bc61a', N'01a10b15-8061-727a-8125-0ffc60535b16'),
    (213, N'01a10b15-8061-71ae-8368-fb3cac974794', NULL, N'محمد صبري', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7bd1-9d82-687e48c8a794', N'01a10b15-8061-7cf9-bf9a-137ee1757aa8'),
    (214, N'01a10b15-8061-71ae-8368-fb3cac974794', NULL, N'عبدالرحمن السمكري', N'خارجي', N'كيتا', N'-', N'01a10b15-8061-751a-8d74-5fca35d01726', N'01a10b15-8061-769a-a942-de2bce480271'),
    (215, N'01a10b15-8061-71ae-8368-fb3cac974794', NULL, N'نادر الميكانيكي', N'كفالة', N'كيتا', N'-', N'01a10b15-8061-7b46-a8e5-48720fa73780', N'01a10b15-8061-779f-a8de-4241b07ab037');

DECLARE @updatedHousing int=0,@updatedFloors int=0,@createdFloors int=0,@updatedRooms int=0,@createdRooms int=0,@assigned int=0,@createdPending int=0,@createdNameOnly int=0,@resolvedPending int=0,@occupancyUpdates int=0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lockResult int;
    EXEC @lockResult=sys.sp_getapplock @Resource='Housing:Jeddah:Workbook20261005',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
    IF @lockResult<0 THROW 51100,'Could not lock the Jeddah housing import.',1;
    IF EXISTS (SELECT 1 FROM audit.AuditEntries WITH (HOLDLOCK) WHERE Source=N'JeddahHousingExternalRenters20261005')
        THROW 51115,'The user subsequently classified the 15 unmatched residents as external renters. This original importer is superseded.',1;
    IF EXISTS (SELECT 1 FROM @housing s LEFT JOIN app.Housing h WITH (UPDLOCK,HOLDLOCK) ON h.Id=s.Id
               WHERE h.Id IS NULL OR h.IsDeleted=1 OR h.CityId<>'019c18d5-62e1-7000-8000-000000000002' OR h.Status<>2 OR h.Code NOT IN (s.Code,s.OldCode))
        THROW 51101,'A reviewed Jeddah housing record changed or is unavailable.',1;
    IF EXISTS (SELECT 1 FROM @housing s JOIN app.Housing h WITH (UPDLOCK,HOLDLOCK) ON h.Code=s.Code AND h.Id<>s.Id)
        THROW 51102,'A workbook housing code is already used by another housing.',1;
    IF EXISTS (SELECT 1 FROM app.HousingRooms r WITH (UPDLOCK,HOLDLOCK) JOIN @housing h ON h.Id=r.HousingId WHERE r.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @rooms s WHERE s.Id=r.Id))
        THROW 51103,'An additional Jeddah room needs reconciliation before importing.',1;
    IF EXISTS (SELECT 1 FROM app.HousingFloors f WITH (UPDLOCK,HOLDLOCK) JOIN @housing h ON h.Id=f.HousingId WHERE f.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @floors s WHERE s.Id=f.Id))
        THROW 51104,'An additional Jeddah floor needs reconciliation before importing.',1;
    IF EXISTS (SELECT 1 FROM @rooms s JOIN app.HousingRooms r ON r.Id=s.Id WHERE r.HousingId<>s.HousingId OR r.IsDeleted=1 OR r.Name NOT IN (s.OldName,s.Name))
        THROW 51105,'A reviewed room identity or name changed.',1;
    IF EXISTS (SELECT 1 FROM @floors s JOIN app.HousingFloors f ON f.Id=s.Id WHERE f.HousingId<>s.HousingId OR f.IsDeleted=1 OR (f.Name<>s.Name AND (s.OldName IS NULL OR f.Name<>s.OldName)))
        THROW 51106,'A reviewed floor identity or name changed.',1;
    UPDATE p SET EmployeeId=e.Id FROM @people p JOIN app.Employees e WITH (UPDLOCK,HOLDLOCK) ON e.IqamaNo=p.IqamaNo AND e.IsDeleted=0;
    IF EXISTS (SELECT 1 FROM @people p JOIN app.HousingResidencePeriods x WITH (UPDLOCK,HOLDLOCK) ON x.EmployeeId=p.EmployeeId AND x.EffectiveTo IS NULL WHERE x.RoomId<>p.RoomId)
        THROW 51107,'A listed employee already occupies a different room; no assignments were moved.',1;
    IF EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.EffectiveTo IS NULL AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.EmployeeId=x.EmployeeId AND p.RoomId=x.RoomId))
        THROW 51108,'An existing Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.IqamaNo IS NULL AND p.RoomId=x.RoomId AND p.Name=x.Name))
        THROW 51109,'An existing name-only Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WITH (UPDLOCK,HOLDLOCK) JOIN app.HousingRooms r ON r.Id=x.RoomId JOIN @housing h ON h.Id=r.HousingId WHERE x.IsDeleted=0 AND NOT EXISTS (SELECT 1 FROM @people p WHERE p.IqamaNo=x.IqamaNo AND p.RoomId=x.RoomId))
        THROW 51110,'An existing pending Jeddah resident is missing from the workbook.',1;
    IF EXISTS (SELECT 1 FROM @people p JOIN app.HousingPendingOccupants x WITH (UPDLOCK,HOLDLOCK) ON x.IqamaNo=p.IqamaNo AND x.IsDeleted=0 WHERE x.RoomId<>p.RoomId)
        THROW 51111,'A listed pending iqama occupies another room.',1;

    UPDATE s SET BeforeJson=(SELECT h.Code,h.NameAr,h.NameEn,h.AddressDistrict,h.AddressCity,
        JSON_QUERY((SELECT f.Id,f.Name FROM app.HousingFloors f WHERE f.HousingId=h.Id AND f.IsDeleted=0 FOR JSON PATH)) AS Floors,
        JSON_QUERY((SELECT r.Id,r.FloorId,r.Name,r.Capacity,r.CurrentOccupancy FROM app.HousingRooms r WHERE r.HousingId=h.Id AND r.IsDeleted=0 FOR JSON PATH)) AS Rooms
        FROM app.Housing h WHERE h.Id=s.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER) FROM @housing s;

    UPDATE h SET Code=s.Code,NameAr=s.NameAr,NameEn=s.NameEn,AddressCity=N'جدة',AddressDistrict=s.District,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.Housing h JOIN @housing s ON s.Id=h.Id
    WHERE h.Code COLLATE Latin1_General_100_BIN2<>s.Code COLLATE Latin1_General_100_BIN2 OR h.NameAr<>s.NameAr OR h.NameEn COLLATE Latin1_General_100_BIN2<>s.NameEn COLLATE Latin1_General_100_BIN2 OR ISNULL(h.AddressCity,N'')<>N'جدة' OR ISNULL(h.AddressDistrict,N'')<>s.District;
    SET @updatedHousing=@@ROWCOUNT;
    UPDATE f SET Name=s.Name,UpdatedAtUtc=@now,UpdatedByUserId=@actor FROM app.HousingFloors f JOIN @floors s ON s.Id=f.Id WHERE f.Name<>s.Name;
    SET @updatedFloors=@@ROWCOUNT;
    INSERT app.HousingFloors (Id,HousingId,Name,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT s.Id,s.HousingId,s.Name,@now,@actor,0 FROM @floors s WHERE NOT EXISTS (SELECT 1 FROM app.HousingFloors f WHERE f.Id=s.Id);
    SET @createdFloors=@@ROWCOUNT;
    UPDATE r SET FloorId=s.FloorId,Name=s.Name,Capacity=s.Capacity,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingRooms r JOIN @rooms s ON s.Id=r.Id WHERE r.FloorId<>s.FloorId OR r.Name<>s.Name OR r.Capacity<>s.Capacity;
    SET @updatedRooms=@@ROWCOUNT;
    INSERT app.HousingRooms (Id,HousingId,FloorId,Name,Capacity,CurrentOccupancy,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT s.Id,s.HousingId,s.FloorId,s.Name,s.Capacity,0,@now,@actor,0 FROM @rooms s WHERE NOT EXISTS (SELECT 1 FROM app.HousingRooms r WHERE r.Id=s.Id);
    SET @createdRooms=@@ROWCOUNT;
    -- Pending iqamas are resolved only when a real matching employee exists.
    UPDATE x SET IsDeleted=1,DeletedAtUtc=@now,DeletedByUserId=@actor,DeletionReason=N'Resolved by '+@source,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingPendingOccupants x JOIN @people p ON p.IqamaNo=x.IqamaNo AND p.RoomId=x.RoomId WHERE p.EmployeeId IS NOT NULL AND x.IsDeleted=0;
    SET @resolvedPending=@@ROWCOUNT;
    INSERT app.HousingResidencePeriods (Id,EmployeeId,RoomId,EffectiveFrom,AssignedByUserId,CreatedAtUtc,CreatedByUserId,SourceReference,MoveInReason,CapacityOverrideUsed)
    SELECT p.PeriodId,p.EmployeeId,p.RoomId,@effective,@actor,@now,@actor,@source+N', ERP row '+CONVERT(nvarchar(10),p.SourceRow),
        N'حسب كشف السكن: '+p.Name+N'؛ التصنيف: '+p.OccupantType+N'؛ العمل: '+p.PlatformWork+N'؛ الملاحظات: '+p.Notes,0
    FROM @people p WHERE p.EmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WHERE x.EmployeeId=p.EmployeeId AND x.EffectiveTo IS NULL);
    SET @assigned=@@ROWCOUNT;
    INSERT app.HousingPendingOccupants (Id,RoomId,IqamaNo,Name,SourceRow,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT p.OccupantId,p.RoomId,p.IqamaNo,p.Name,p.SourceRow,@now,@actor,0 FROM @people p
    WHERE p.IqamaNo IS NOT NULL AND p.EmployeeId IS NULL AND NOT EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WHERE x.IqamaNo=p.IqamaNo AND x.IsDeleted=0);
    SET @createdPending=@@ROWCOUNT;
    INSERT app.HousingExternalOccupants (Id,RoomId,Name,CreatedAtUtc,CreatedByUserId,IsDeleted)
    SELECT p.OccupantId,p.RoomId,p.Name,@now,@actor,0 FROM @people p WHERE p.IqamaNo IS NULL
    AND NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0);
    SET @createdNameOnly=@@ROWCOUNT;
    UPDATE r SET CurrentOccupancy=q.Occupants,UpdatedAtUtc=@now,UpdatedByUserId=@actor
    FROM app.HousingRooms r JOIN @rooms s ON s.Id=r.Id
    CROSS APPLY (SELECT (SELECT COUNT(*) FROM app.HousingResidencePeriods x WHERE x.RoomId=r.Id AND x.EffectiveTo IS NULL)
        +(SELECT COUNT(*) FROM app.HousingPendingOccupants x WHERE x.RoomId=r.Id AND x.IsDeleted=0)
        +(SELECT COUNT(*) FROM app.HousingExternalOccupants x WHERE x.RoomId=r.Id AND x.IsDeleted=0) AS Occupants) q
    WHERE r.CurrentOccupancy<>q.Occupants;
    SET @occupancyUpdates=@@ROWCOUNT;

    IF EXISTS (SELECT 1 FROM @rooms s LEFT JOIN app.HousingRooms r ON r.Id=s.Id WHERE r.Id IS NULL OR r.IsDeleted=1 OR r.HousingId<>s.HousingId OR r.FloorId<>s.FloorId OR r.Name<>s.Name OR r.Capacity<>s.Capacity OR r.CurrentOccupancy<>s.Occupants)
        THROW 51112,'Room identity, capacity or occupancy failed workbook reconciliation.',1;
    IF EXISTS (SELECT 1 FROM @people p WHERE (p.EmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingResidencePeriods x WHERE x.EmployeeId=p.EmployeeId AND x.RoomId=p.RoomId AND x.EffectiveTo IS NULL))
        OR (p.EmployeeId IS NULL AND p.IqamaNo IS NOT NULL AND NOT EXISTS (SELECT 1 FROM app.HousingPendingOccupants x WHERE x.IqamaNo=p.IqamaNo AND x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0))
        OR (p.IqamaNo IS NULL AND NOT EXISTS (SELECT 1 FROM app.HousingExternalOccupants x WHERE x.RoomId=p.RoomId AND x.Name=p.Name AND x.IsDeleted=0)))
        THROW 51113,'An occupant was not recorded in the exact workbook room.',1;
    IF EXISTS (SELECT 1 FROM @housing s CROSS APPLY (SELECT COUNT(*) AS Rooms,SUM(r.Capacity) AS Capacity,SUM(r.CurrentOccupancy) AS Occupants FROM app.HousingRooms r WHERE r.HousingId=s.Id AND r.IsDeleted=0) q WHERE q.Rooms<>s.Rooms OR q.Capacity<>s.Capacity OR q.Occupants<>s.Occupants)
        THROW 51114,'Housing totals failed workbook reconciliation.',1;

    IF @updatedHousing+@updatedFloors+@createdFloors+@updatedRooms+@createdRooms+@assigned+@createdPending+@createdNameOnly+@resolvedPending+@occupancyUpdates>0
    INSERT audit.AuditEntries (Id,EventId,ActorType,Action,Category,EntityType,EntityId,OccurredAtUtc,CorrelationId,Reason,BeforeJson,AfterJson,Source,SchemaVersion,CreatedAtUtc)
    SELECT NEWID(),NEWID(),N'System',N'Imported',N'Housing',N'Housing',s.Id,@now,N'JeddahHousing20261005',
        N'Workbook reconciliation; SHA-256 63a9d01296e68f85709a5892496a8ab0177e99b3911b526e32a09962627ce6b3',s.BeforeJson,
        (SELECT h.Code,h.NameAr,h.NameEn,h.AddressDistrict,h.AddressCity,
            JSON_QUERY((SELECT f.Id,f.Name FROM app.HousingFloors f WHERE f.HousingId=h.Id AND f.IsDeleted=0 FOR JSON PATH)) AS Floors,
            JSON_QUERY((SELECT r.Id,r.FloorId,r.Name,r.Capacity,r.CurrentOccupancy FROM app.HousingRooms r WHERE r.HousingId=h.Id AND r.IsDeleted=0 FOR JSON PATH)) AS Rooms,
            JSON_QUERY((SELECT p.SourceRow,p.IqamaNo,p.Name,p.OccupantType,p.PlatformWork,p.Notes,p.RoomId,p.EmployeeId FROM @people p JOIN @rooms r ON r.Id=p.RoomId WHERE r.HousingId=h.Id FOR JSON PATH)) AS Occupants
        FROM app.Housing h WHERE h.Id=s.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER),N'JeddahHousingWorkbookImport',1,@now FROM @housing s;

    SELECT h.Code,h.NameAr,COUNT(r.Id) AS Rooms,SUM(r.Capacity) AS Capacity,SUM(r.CurrentOccupancy) AS Occupants,SUM(r.Capacity-r.CurrentOccupancy) AS Vacancies
    FROM @housing s JOIN app.Housing h ON h.Id=s.Id JOIN app.HousingRooms r ON r.HousingId=h.Id AND r.IsDeleted=0 GROUP BY h.Code,h.NameAr ORDER BY h.Code;
    SELECT p.SourceRow,h.Code,f.Name AS Floor,r.Name AS Room,p.IqamaNo,p.Name,
        CASE WHEN p.IqamaNo IS NULL THEN 'NameOnly' ELSE 'PendingIqama' END AS RecordType
    FROM @people p JOIN app.HousingRooms r ON r.Id=p.RoomId JOIN app.Housing h ON h.Id=r.HousingId JOIN app.HousingFloors f ON f.Id=r.FloorId WHERE p.EmployeeId IS NULL ORDER BY p.SourceRow;
    IF @Commit=1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
    SELECT CASE WHEN @Commit=1 THEN 'Imported' ELSE 'ValidatedAndRolledBack' END AS ImportStatus,
        @updatedHousing AS UpdatedHousing,@updatedFloors AS UpdatedFloors,@createdFloors AS CreatedFloors,@updatedRooms AS UpdatedRooms,@createdRooms AS CreatedRooms,
        @assigned AS NewEmployeeAssignments,@createdPending AS NewPendingOccupants,@createdNameOnly AS NewNameOnlyOccupants,@resolvedPending AS ResolvedPending,@occupancyUpdates AS UpdatedRoomOccupancy;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
