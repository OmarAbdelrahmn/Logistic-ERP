-- Generated from the Riyadh housing workbook. Run after AddHousingPendingOccupants.
-- Replaces the linked Al Naseem 1 room assignments. Existing periods remain as history.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM [migration].[__ApplicationMigrationsHistory]
               WHERE [MigrationId] LIKE '%AddHousingPendingOccupants')
    THROW 51000, 'Apply the housing migration before seeding.', 1;
IF NOT EXISTS (SELECT 1 FROM [app].[Housing] WHERE [Id] = '01a0d372-4ac7-7e6e-bd47-cbfbe074dafd'
               AND [CityId] = '019c18d5-62e1-7000-8000-000000000004' AND [IsDeleted] = 0)
    THROW 51001, 'The linked Riyadh Al Naseem housing was not found.', 1;
IF EXISTS (SELECT 1 FROM [app].[HousingRooms] WHERE [HousingId] = '01a0d372-4ac7-7e6e-bd47-cbfbe074dafd'
           AND [Name] = N'1' AND [IsDeleted] = 0)
    THROW 51002, 'Riyadh housing workbook has already been imported.', 1;

CREATE TABLE #HousingSource ([Place] nvarchar(100) PRIMARY KEY, [Code] nvarchar(32), [NameEn] nvarchar(200));
INSERT INTO #HousingSource VALUES
    (N'الأمير بندر 1', N'RYD-PRINCE-BANDAR-1', N'Prince Bandar 1'),
    (N'النسيم 1', N'الرياض النسيم', N'Riyadh Al Naseem 1'),
    (N'النسيم 2', N'RYD-NASEEM-2', N'Riyadh Al Naseem 2'),
    (N'النسيم 3', N'RYD-NASEEM-3', N'Riyadh Al Naseem 3');
CREATE TABLE #RoomSource ([Place] nvarchar(100), [Number] nvarchar(100), [Capacity] int, [Notes] nvarchar(2000));
INSERT INTO #RoomSource VALUES
    (N'الأمير بندر 1', N'1', 8, N'موجود بها 4 سراير ب دورين'),
    (N'الأمير بندر 1', N'2', 5, N'موجود 3 سراير ب دورين'),
    (N'الأمير بندر 1', N'3', 5, N'موجود بالغرفة 1 سرير فردى
و 2 سرير دورين '),
    (N'الأمير بندر 1', N'4', 6, N'موجود بالغرفة 3 سرير بدورين'),
    (N'النسيم 1', N'1', 5, N'موجود 3 سرير بدورين'),
    (N'النسيم 1', N'2', 8, N'موجود 4 سرير بدورين'),
    (N'النسيم 2', N'1', 8, N'موجود 4 سرير بدورين'),
    (N'النسيم 2', N'2', 6, N'موجود 3 سرير بدورين '),
    (N'النسيم 2', N'3', 8, N'موجود 4 سرير بدورين'),
    (N'النسيم 2', N'4', 7, N'موجود 4 سرير بدورين'),
    (N'النسيم 3', N'1', 9, N'موجود 4 سرير بدورين وواحد سرير فردى'),
    (N'النسيم 3', N'2', 5, N'موجود 3 سرير بدورين'),
    (N'النسيم 3', N'3', 8, N'موجود 4 سرير بدورين'),
    (N'النسيم 3', N'4', 6, N'موجود 3 سرير بدورين');
CREATE TABLE #PersonSource ([Place] nvarchar(100), [Number] nvarchar(100), [Iqama] nvarchar(20), [Name] nvarchar(200), [SourceRow] int);
INSERT INTO #PersonSource VALUES
    (N'الأمير بندر 1', N'1', NULL, N'محمود خطيب', 2),
    (N'الأمير بندر 1', N'1', N'2541115800', N'حمزه اسعد ابو بكر حامد', 3),
    (N'الأمير بندر 1', N'1', N'2627962364', N'ابراهيم السيد ابراهيم متولي', 4),
    (N'الأمير بندر 1', N'1', N'2621386362', N'متولى محمد متولى', 5),
    (N'الأمير بندر 1', N'1', N'2619171495', N'عرفه احمد عبدالمجيد', 6),
    (N'الأمير بندر 1', N'2', N'2623781321', N'احمد محمد نجيب شلبي', 10),
    (N'الأمير بندر 1', N'2', N'2599549231', N'ابراهيم محمد محمد كريم', 14),
    (N'الأمير بندر 1', N'2', N'2549020267', N'احمد حلمى محمد عماره', 15),
    (N'الأمير بندر 1', N'3', N'2563494190', N'الحسين عبد الحميد عبد الحليم سعود', 16),
    (N'الأمير بندر 1', N'3', N'2510966068', N'محمد احمد احمد محمد', 17),
    (N'الأمير بندر 1', N'3', N'2564109326', N'طه انور السادات احمد محمد', 18),
    (N'الأمير بندر 1', N'3', N'2619451707', N'ياسر عطا احمد محمد', 19),
    (N'الأمير بندر 1', N'4', N'2564289847', N'مؤمن عبده احمد محمد', 21),
    (N'الأمير بندر 1', N'4', NULL, N'محمد سعيد', 22),
    (N'الأمير بندر 1', N'4', N'2620593968', N'محمد عبدالنبى نبوى جابر', 23),
    (N'الأمير بندر 1', N'4', N'2563297650', N'هشام حنفي ابو عمره', 24),
    (N'النسيم 1', N'1', N'2618019182', N'مد مد مد سليم', 30),
    (N'النسيم 1', N'1', N'2643890250', N'محمد سوجول سوجول اوسين اوسين', 31),
    (N'النسيم 1', N'1', N'2548227202', N'مد ريبون علي', 33),
    (N'النسيم 1', N'2', N'2576050401', N'مد مامون خان', 36),
    (N'النسيم 1', N'2', N'2636655173', N'ال مغير حسين', 37),
    (N'النسيم 1', N'2', N'2612570016', N'شاهين محمود شاهين', 38),
    (N'النسيم 1', N'2', N'2585086503', N'مد رقيب حسين', 40),
    (N'النسيم 1', N'2', N'2643584044', N'عريف مريضة', 41),
    (N'النسيم 2', N'1', N'2624498370', N'جميل فتحي محمود شندي', 44),
    (N'النسيم 2', N'1', N'2624498321', N'امير بدر محمد محمود', 45),
    (N'النسيم 2', N'1', N'2577660661', N'اسلام احمد فتحى محمود', 46),
    (N'النسيم 2', N'1', N'2615854565', N'محمود يحى', 47),
    (N'النسيم 2', N'1', N'2623790736', N'احمد رجب سيد مرعي', 48),
    (N'النسيم 2', N'1', N'2630315410', N'حماده محمود احمد محمود', 49),
    (N'النسيم 2', N'1', N'2620274197', N'اسلام احمد محمود احمد', 50),
    (N'النسيم 2', N'1', N'2622291686', N'ابراهيم محمد عبد التواب السيد', 51),
    (N'النسيم 2', N'2', N'2597772777', N'سمير هاني سمير ابراهيم', 52),
    (N'النسيم 2', N'2', N'2630243315', N'احمد صلاح الدين حسن حموده', 53),
    (N'النسيم 2', N'2', N'2610794063', N'غيضان سليم حسين', 54),
    (N'النسيم 2', N'2', N'2581511629', N'حاتم محمد عبدالله على', 55),
    (N'النسيم 2', N'3', N'2621224373', N'اسلام خلاف عبدالموجود احمد', 59),
    (N'النسيم 2', N'3', N'2627814839', N'محمود السيد عطيه السيد', 60),
    (N'النسيم 2', N'3', N'2623793433', N'اسماعيل جمال اسماعيل محمد', 62),
    (N'النسيم 2', N'3', N'2612590345', N'سعيد عبده عبد المنعم محمود', 64),
    (N'النسيم 2', N'4', N'2623006349', N'سلامه محمد كامل عبدالعزيز', 66),
    (N'النسيم 2', N'4', N'2615560725', N'حسين ياسر طه محمد', 68),
    (N'النسيم 2', N'4', N'2588987777', N'علاء بدوى مصطفى', 70),
    (N'النسيم 2', N'4', N'2621321203', N'محرم ابراهيم عثمان احمد', 71),
    (N'النسيم 2', N'4', N'2626679514', N'احمد فتحي احمد عبدالحليم', 72),
    (N'النسيم 2', N'4', N'2640150187', N'زياد جميل فتحي شندي', 73),
    (N'النسيم 3', N'1', N'2594426104', N'عماد احمد علي احمد الحجلي', 76),
    (N'النسيم 3', N'1', N'2623827850', N'محمود ابراهيم محمود حزيمه', 80),
    (N'النسيم 3', N'1', N'2352329052', N'عبدالعزيز محمد ثابت مقبل', 81),
    (N'النسيم 3', N'1', N'2571787197', N'ابراهيم جمعه يوسف جمعه', 82),
    (N'النسيم 3', N'2', N'2536120732', N'عمرو سامى حسن حامد', 84),
    (N'النسيم 3', N'2', N'2629897691', N'محمد سامى حسن حامد', 85),
    (N'النسيم 3', N'2', NULL, N'محمد شكرى', 86),
    (N'النسيم 3', N'3', N'2630314744', N'Md Rahad Hossain', 91),
    (N'النسيم 3', N'3', N'2577524115', N'MUHAMMAD - - KHAIRUL', 92),
    (N'النسيم 3', N'3', N'2633076662', N'md masud', 95),
    (N'النسيم 3', N'3', N'2636655421', N'مد شكيب ميا', 97),
    (N'النسيم 3', N'4', N'2627167543', N'صمونيل نادي عزيز امين', 98),
    (N'النسيم 3', N'4', N'2627168715', N'ماركو صليب فهيم اسكندر', 99),
    (N'النسيم 3', N'4', N'2629565173', N'عبدالله مسعد عبدالله رزق', 101),
    (N'النسيم 3', N'4', N'2627732478', N'نور جمال بدوى طه', 102);
CREATE TABLE #EquipmentSource ([Place] nvarchar(100), [Number] nvarchar(100), [Name] nvarchar(100), [Quantity] int);
INSERT INTO #EquipmentSource VALUES
    (N'الأمير بندر 1', N'1', N'مرتبة', 8),
    (N'الأمير بندر 1', N'1', N'وسادة', 8),
    (N'الأمير بندر 1', N'1', N'مكيف', 1),
    (N'الأمير بندر 1', N'2', N'مكيف', 1),
    (N'الأمير بندر 1', N'2', N'مرتبة', 6),
    (N'الأمير بندر 1', N'2', N'وسادة', 6),
    (N'الأمير بندر 1', NULL, N'غسالة', 2),
    (N'الأمير بندر 1', NULL, N'ثلاجة', 1),
    (N'الأمير بندر 1', NULL, N'بوتجاز', 1),
    (N'الأمير بندر 1', NULL, N'اسطوانة غاز', 1),
    (N'الأمير بندر 1', N'3', N'مرتبة', 5),
    (N'الأمير بندر 1', N'3', N'وسادة', 5),
    (N'الأمير بندر 1', N'3', N'مكيف', 1),
    (N'الأمير بندر 1', N'4', N'مكيف', 1),
    (N'الأمير بندر 1', N'4', N'مرتبة', 6),
    (N'الأمير بندر 1', N'4', N'وسادة', 6),
    (N'النسيم 1', N'1', N'مكيف', 1),
    (N'النسيم 1', N'1', N'مرتبة', 6),
    (N'النسيم 1', N'1', N'وسادة', 6),
    (N'النسيم 1', NULL, N'غسالة', 1),
    (N'النسيم 1', NULL, N'ثلاجة', 1),
    (N'النسيم 1', NULL, N'بوتجاز', 1),
    (N'النسيم 1', NULL, N'اسطوانة غاز', 1),
    (N'النسيم 1', N'2', N'مكيف', 1),
    (N'النسيم 1', N'2', N'مرتبة', 8),
    (N'النسيم 1', N'2', N'وسادة', 8),
    (N'النسيم 2', N'1', N'مكيف', 1),
    (N'النسيم 2', N'1', N'مرتبة', 8),
    (N'النسيم 2', N'1', N'وسادة', 8),
    (N'النسيم 2', NULL, N'غسالة', 2),
    (N'النسيم 2', NULL, N'ثلاجة', 1),
    (N'النسيم 2', NULL, N'بوتجاز', 1),
    (N'النسيم 2', NULL, N'اسطوانة غاز', 1),
    (N'النسيم 2', N'2', N'مكيف', 1),
    (N'النسيم 2', N'2', N'مرتبة', 6),
    (N'النسيم 2', N'2', N'وسادة', 6),
    (N'النسيم 2', N'3', N'مكيف', 1),
    (N'النسيم 2', N'3', N'مرتبة', 8),
    (N'النسيم 2', N'3', N'وسادة', 8),
    (N'النسيم 2', N'4', N'مكيف', 1),
    (N'النسيم 2', N'4', N'مرتبة', 8),
    (N'النسيم 2', N'4', N'وسادة', 8),
    (N'النسيم 3', N'1', N'مكيف', 1),
    (N'النسيم 3', N'1', N'مرتبة', 9),
    (N'النسيم 3', N'1', N'وسادة', 9),
    (N'النسيم 3', NULL, N'ثلاجة', 1),
    (N'النسيم 3', NULL, N'غسالة', 1),
    (N'النسيم 3', NULL, N'بوتجاز', 1),
    (N'النسيم 3', NULL, N'اسطوانة غاز', 1),
    (N'النسيم 3', N'2', N'مرتبة', 6),
    (N'النسيم 3', N'2', N'وسادة', 6),
    (N'النسيم 3', N'2', N'مكيف', 1),
    (N'النسيم 3', N'3', N'مرتبة', 8),
    (N'النسيم 3', N'3', N'وسادة', 8),
    (N'النسيم 3', N'3', N'مكيف', 1),
    (N'النسيم 3', N'4', N'مرتبة', 6),
    (N'النسيم 3', N'4', N'وسادة', 6),
    (N'النسيم 3', N'4', N'مكيف', 1);

IF EXISTS (
    SELECT 1 FROM #PersonSource p
    JOIN [app].[Employees] e ON e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0
    GROUP BY p.[Iqama] HAVING COUNT(e.[Id]) > 1)
    THROW 51003, 'Duplicate employee iqama; no housing was changed.', 1;
IF EXISTS (
    SELECT 1 FROM #PersonSource p
    JOIN [app].[Employees] e ON e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0
    JOIN [app].[HousingResidencePeriods] x ON x.[EmployeeId] = e.[Id] AND x.[EffectiveTo] IS NULL
    JOIN [app].[HousingRooms] r ON r.[Id] = x.[RoomId]
    WHERE r.[HousingId] <> '01a0d372-4ac7-7e6e-bd47-cbfbe074dafd')
    THROW 51005, 'A workbook resident is already assigned to another housing.', 1;

-- Close old current residence periods and archive the linked housing's old rooms.
UPDATE p SET [EffectiveTo] = DATEADD(day, -1, CAST(GETDATE() AS date)),
    [MoveOutReason] = N'Replaced by Riyadh housing workbook import'
FROM [app].[HousingResidencePeriods] p
JOIN [app].[HousingRooms] r ON r.[Id] = p.[RoomId]
WHERE r.[HousingId] = '01a0d372-4ac7-7e6e-bd47-cbfbe074dafd' AND p.[EffectiveTo] IS NULL;
UPDATE r SET [CurrentOccupancy] = 0, [IsDeleted] = 1,
    [DeletionReason] = N'Replaced by Riyadh housing workbook import'
FROM [app].[HousingRooms] r
WHERE r.[HousingId] = '01a0d372-4ac7-7e6e-bd47-cbfbe074dafd' AND r.[IsDeleted] = 0;

INSERT INTO [app].[Housing] ([Id], [Code], [NameAr], [NameEn], [CityId], [Status], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), s.[Code], s.[Place], s.[NameEn], '019c18d5-62e1-7000-8000-000000000004', 2, SYSUTCDATETIME(), 0
FROM #HousingSource s
WHERE s.[Place] <> N'النسيم 1'
  AND NOT EXISTS (SELECT 1 FROM [app].[Housing] h WHERE h.[Code] = s.[Code]);

INSERT INTO [app].[HousingWarehouses] ([Id], [HousingId], [NameAr], [NameEn], [IsDefault], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), h.[Id], N'المستودع الافتراضي', N'Default warehouse', 1, SYSUTCDATETIME(), 0
FROM #HousingSource s JOIN [app].[Housing] h ON h.[Code] = s.[Code]
WHERE NOT EXISTS (SELECT 1 FROM [app].[HousingWarehouses] w WHERE w.[HousingId] = h.[Id] AND w.[IsDeleted] = 0);

INSERT INTO [app].[HousingFloors] ([Id], [HousingId], [Name], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), h.[Id], N'1', SYSUTCDATETIME(), 0
FROM #HousingSource s JOIN [app].[Housing] h ON h.[Code] = s.[Code]
WHERE NOT EXISTS (SELECT 1 FROM [app].[HousingFloors] f WHERE f.[HousingId] = h.[Id] AND f.[Name] = N'1' AND f.[IsDeleted] = 0);

INSERT INTO [app].[HousingRooms] ([Id], [HousingId], [FloorId], [Name], [Capacity], [CurrentOccupancy], [Notes], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), h.[Id], f.[Id], s.[Number], s.[Capacity], 0, s.[Notes], SYSUTCDATETIME(), 0
FROM #RoomSource s
JOIN #HousingSource hs ON hs.[Place] = s.[Place]
JOIN [app].[Housing] h ON h.[Code] = hs.[Code]
JOIN [app].[HousingFloors] f ON f.[HousingId] = h.[Id] AND f.[Name] = N'1' AND f.[IsDeleted] = 0
WHERE NOT EXISTS (SELECT 1 FROM [app].[HousingRooms] r WHERE r.[HousingId] = h.[Id] AND r.[Name] = s.[Number] AND r.[IsDeleted] = 0);

-- Existing people are assigned by iqama. Name-only people are kept separate from Employees.
INSERT INTO [app].[HousingResidencePeriods]
    ([Id], [EmployeeId], [RoomId], [EffectiveFrom], [AssignedByUserId], [CreatedAtUtc], [SourceReference], [CapacityOverrideUsed])
SELECT NEWID(), e.[Id], r.[Id], CAST(GETDATE() AS date), '00000000-0000-0000-0000-000000000000', SYSUTCDATETIME(), N'Riyadh housing workbook', 0
FROM #PersonSource p
JOIN [app].[Employees] e ON e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0
JOIN #HousingSource hs ON hs.[Place] = p.[Place]
JOIN [app].[Housing] h ON h.[Code] = hs.[Code]
JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = p.[Number] AND r.[IsDeleted] = 0
WHERE p.[Iqama] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [app].[HousingResidencePeriods] x WHERE x.[EmployeeId] = e.[Id] AND x.[EffectiveTo] IS NULL);

INSERT INTO [app].[HousingExternalOccupants] ([Id], [RoomId], [Name], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), r.[Id], p.[Name], SYSUTCDATETIME(), 0
FROM #PersonSource p
JOIN #HousingSource hs ON hs.[Place] = p.[Place]
JOIN [app].[Housing] h ON h.[Code] = hs.[Code]
JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = p.[Number] AND r.[IsDeleted] = 0
WHERE p.[Iqama] IS NULL;

INSERT INTO [app].[HousingPendingOccupants] ([Id], [RoomId], [IqamaNo], [Name], [SourceRow], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), r.[Id], p.[Iqama], p.[Name], p.[SourceRow], SYSUTCDATETIME(), 0
FROM #PersonSource p
JOIN #HousingSource hs ON hs.[Place] = p.[Place]
JOIN [app].[Housing] h ON h.[Code] = hs.[Code]
JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = p.[Number] AND r.[IsDeleted] = 0
WHERE p.[Iqama] IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM [app].[Employees] e WHERE e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0);

INSERT INTO [app].[HousingEquipment] ([Id], [FloorId], [RoomId], [Name], [Quantity], [CreatedAtUtc], [IsDeleted])
SELECT NEWID(), CASE WHEN s.[Number] IS NULL THEN f.[Id] END,
       CASE WHEN s.[Number] IS NOT NULL THEN r.[Id] END,
       s.[Name], s.[Quantity], SYSUTCDATETIME(), 0
FROM #EquipmentSource s
JOIN #HousingSource hs ON hs.[Place] = s.[Place]
JOIN [app].[Housing] h ON h.[Code] = hs.[Code]
JOIN [app].[HousingFloors] f ON f.[HousingId] = h.[Id] AND f.[Name] = N'1' AND f.[IsDeleted] = 0
LEFT JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[Name] = s.[Number] AND r.[IsDeleted] = 0;

UPDATE r SET [CurrentOccupancy] = x.[Occupants]
FROM [app].[HousingRooms] r
CROSS APPLY (SELECT
    (SELECT COUNT(*) FROM [app].[HousingResidencePeriods] p WHERE p.[RoomId] = r.[Id] AND p.[EffectiveTo] IS NULL) +
    (SELECT COUNT(*) FROM [app].[HousingExternalOccupants] e WHERE e.[RoomId] = r.[Id] AND e.[IsDeleted] = 0) +
    (SELECT COUNT(*) FROM [app].[HousingPendingOccupants] u WHERE u.[RoomId] = r.[Id] AND u.[IsDeleted] = 0) AS [Occupants]) x
WHERE r.[IsDeleted] = 0 AND r.[HousingId] IN (SELECT h.[Id] FROM [app].[Housing] h JOIN #HousingSource s ON s.[Code] = h.[Code]);

IF EXISTS (SELECT 1 FROM [app].[HousingRooms] r WHERE r.[CurrentOccupancy] > r.[Capacity])
    THROW 51004, 'Imported room exceeds capacity.', 1;
COMMIT TRANSACTION;
SELECT h.[NameAr], COUNT(r.[Id]) AS [Rooms], SUM(r.[CurrentOccupancy]) AS [Occupants]
FROM #HousingSource s JOIN [app].[Housing] h ON h.[Code] = s.[Code]
JOIN [app].[HousingRooms] r ON r.[HousingId] = h.[Id] AND r.[IsDeleted] = 0
GROUP BY h.[NameAr];
SELECT p.[SourceRow], p.[Iqama], p.[Name] AS [UnmatchedName], p.[Place], p.[Number]
FROM #PersonSource p
WHERE p.[Iqama] IS NOT NULL AND NOT EXISTS
    (SELECT 1 FROM [app].[Employees] e WHERE e.[IqamaNo] = p.[Iqama] AND e.[IsDeleted] = 0);
