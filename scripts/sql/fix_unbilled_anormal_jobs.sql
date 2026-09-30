-- ============================================================================
-- fix_unbilled_anormal_jobs.sql
-- ============================================================================
-- AMAC: Faturalanmamis 10 anormal isin K3a_FAIL snapshot'ini legacy-dogru
--       degere indir.
--
-- KAPSAM (whitelist - sadece bu 10 referans):
--   521169, 521330, 521331, 521341, 521637, 521639, 521643, 521702,
--   521777, 521859
--
-- ELLENMEYECEKLER:
--   521329, 521409, 521633  -> StateID 140 (faturalanmis), dokunulmaz.
--   Diger tum isler ve diger ProdCatID'ler -> dokunulmaz.
--
-- FIX KURALI (her kirli JobProdCat satiri icin):
--   target_Gross   = sum(active JobProd.NetAmount per ProdCatID)   = Prod_NetSum
--   target_Net     = target_Gross                                   = Prod_NetSum
--   target_Disc    = 0
--   target_DiscPct = 0
--
-- ETKI:
--   - Musteri faturalanmadi: ProdSum dusuyor ama henuz fatura yok, sorun yok.
--   - Job.ProdSum'i da yeni ProdSum (sum(JobProdCat.NetAmount aktif)) ile guncelle.
--
-- DEFAULT: ROLLBACK. Sonuc uygunsa altta COMMIT yap, tekrar calistir.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ----------------------------------------------------------------------------
-- 0) Beyaz liste
-- ----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#refs') IS NOT NULL DROP TABLE #refs;
CREATE TABLE #refs (Reference INT PRIMARY KEY);
INSERT INTO #refs (Reference) VALUES
    (521169), (521330), (521331), (521341), (521637),
    (521639), (521643), (521702), (521777), (521859);

IF OBJECT_ID('tempdb..#targets') IS NOT NULL DROP TABLE #targets;

SELECT
    j.Id            AS JobId,
    j.Reference,
    j.StateID,
    j.ProdSum       AS Job_ProdSum_Before,
    jpc.Id          AS JobProdCatId,
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    ISNULL((SELECT SUM(jp.NetAmount)
            FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0) AS Prod_NetSum
INTO #targets
FROM Job j
JOIN #refs r ON r.Reference = j.Reference
JOIN JobProdCat jpc ON jpc.JobID = j.Id AND jpc.Deleted = 0
WHERE
    -- Sadece K3a_FAIL ve anormal (Net > Prod_NetSum) olan satirlari hedefle.
    -- Diger satirlara (cat 20, 27 vb. saglikli olanlar) DOKUNMA.
    (jpc.NetAmount
       - ISNULL((SELECT SUM(jp.NetAmount)
                 FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
                 WHERE jp.JobID = j.Id AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0)) > 0.01;

-- Guvenlik kontrolu: faturalanmis bir is icin satir gelmemeli
DECLARE @InvoicedAccidentallyTargeted INT;
SELECT @InvoicedAccidentallyTargeted = COUNT(*)
FROM #targets WHERE StateID = 140;

IF @InvoicedAccidentallyTargeted > 0
BEGIN
    PRINT '!!! HATA: Faturalanmis is hedef setine girmis. ABORT.';
    SELECT * FROM #targets WHERE StateID = 140;
    RETURN;
END

-- ----------------------------------------------------------------------------
-- 1) [BEFORE] — hedef satirlar ve etki
-- ----------------------------------------------------------------------------
PRINT '=== [BEFORE] Hedef JobProdCat satirlari ===';
SELECT
    Reference, StateID, ProdCatID,
    Snap_Gross  AS Old_Gross,
    Snap_Disc   AS Old_Disc,
    Snap_Net    AS Old_Net,
    Prod_NetSum AS New_Gross_And_Net,
    (Snap_Net - Prod_NetSum) AS ProdSum_Delta_Decrease,
    Job_ProdSum_Before
FROM #targets
ORDER BY Reference, ProdCatID;

PRINT '';
PRINT '=== Toplam etki ===';
SELECT
    COUNT(*)                         AS Target_Rows,
    COUNT(DISTINCT JobId)            AS Target_Jobs,
    SUM(Snap_Net - Prod_NetSum)      AS Total_ProdSum_Decrease
FROM #targets;

-- ----------------------------------------------------------------------------
-- 2) Yedek + UPDATE
-- ----------------------------------------------------------------------------
BEGIN TRANSACTION;

-- Yedek tablo
DECLARE @BackupTable SYSNAME = N'JobProdCat_Backup_Unbilled10_'
                              + CONVERT(VARCHAR(8),  GETDATE(), 112)
                              + N'_' + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '');
DECLARE @sql NVARCHAR(MAX) = N'
    SELECT jpc.*, GETDATE() AS BackupAt
    INTO ' + QUOTENAME(@BackupTable) + N'
    FROM JobProdCat jpc
    WHERE jpc.Id IN (SELECT JobProdCatId FROM #targets);
';
EXEC sp_executesql @sql;
PRINT 'Yedek tablo (JobProdCat): ' + @BackupTable;

-- Yedek tablo (Job)
DECLARE @JobBackupTable SYSNAME = N'Job_Backup_Unbilled10_'
                                + CONVERT(VARCHAR(8),  GETDATE(), 112)
                                + N'_' + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '');
DECLARE @sql2 NVARCHAR(MAX) = N'
    SELECT j.*, GETDATE() AS BackupAt
    INTO ' + QUOTENAME(@JobBackupTable) + N'
    FROM Job j
    WHERE j.Id IN (SELECT DISTINCT JobId FROM #targets);
';
EXEC sp_executesql @sql2;
PRINT 'Yedek tablo (Job)       : ' + @JobBackupTable;

-- JobProdCat update
UPDATE jpc
SET GrossAmount    = t.Prod_NetSum,
    DiscAmount     = 0,
    NetAmount      = t.Prod_NetSum,
    DiscPercentage = 0,
    Stamp          = 1
FROM JobProdCat jpc
JOIN #targets t ON t.JobProdCatId = jpc.Id;

DECLARE @rowsJPC INT = @@ROWCOUNT;
PRINT 'Updated JobProdCat rows: ' + CAST(@rowsJPC AS VARCHAR(10));

-- Job.ProdSum'i K1 invariant'ina uyumlu hale getir (etkilenen isler icin)
UPDATE j
SET ProdSum = s.NewProdSum,
    Stamp   = 1
FROM Job j
JOIN (
    SELECT t.JobId, SUM(jpc.NetAmount) AS NewProdSum
    FROM #targets t
    JOIN JobProdCat jpc ON jpc.JobID = t.JobId AND jpc.Deleted = 0
    GROUP BY t.JobId
) s ON s.JobId = j.Id;

DECLARE @rowsJob INT = @@ROWCOUNT;
PRINT 'Updated Job rows       : ' + CAST(@rowsJob AS VARCHAR(10));

-- ----------------------------------------------------------------------------
-- 3) [AFTER] — invariant dogrulamasi
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== [AFTER] Hedef setinde K3a_FAIL kalan satir sayisi ===';
SELECT COUNT(*) AS Still_K3a_Fail
FROM JobProdCat jpc
JOIN #targets t ON t.JobProdCatId = jpc.Id
WHERE ABS(jpc.GrossAmount
        - ISNULL((SELECT SUM(jp.NetAmount)
                  FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
                  WHERE jp.JobID = jpc.JobID AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0)) >= 0.01;

PRINT '';
PRINT '=== [AFTER] K3b kontrolu ===';
SELECT COUNT(*) AS K3b_Fail
FROM JobProdCat jpc
JOIN #targets t ON t.JobProdCatId = jpc.Id
WHERE ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) >= 0.01;

PRINT '';
PRINT '=== [AFTER] K1 kontrolu (etkilenen isler) ===';
SELECT COUNT(*) AS K1_Fail
FROM (SELECT DISTINCT JobId FROM #targets) tj
JOIN Job j ON j.Id = tj.JobId
CROSS APPLY (
    SELECT ISNULL(SUM(NetAmount), 0) AS NetSum
    FROM JobProdCat
    WHERE JobID = j.Id AND Deleted = 0
) s
WHERE ABS(j.ProdSum - s.NetSum) >= 0.01;

PRINT '';
PRINT '=== [AFTER] Yeni durum ozeti ===';
SELECT
    j.Reference, j.StateID,
    j.ProdSum AS Job_ProdSum_After,
    (SELECT GrossAmount FROM JobProdCat WHERE Id = t.JobProdCatId) AS New_Gross,
    (SELECT DiscAmount  FROM JobProdCat WHERE Id = t.JobProdCatId) AS New_Disc,
    (SELECT NetAmount   FROM JobProdCat WHERE Id = t.JobProdCatId) AS New_Net
FROM #targets t
JOIN Job j ON j.Id = t.JobId
ORDER BY j.Reference;

PRINT '';
PRINT '=== ONEMLI ===';
PRINT '1. Yukaridaki tum AFTER FAIL sayilari 0 olmali.';
PRINT '2. Faturalanmis 3 ise (521329, 521409, 521633) dokunulmadi.';
PRINT '3. Yedekler: ' + @BackupTable + '  ve  ' + @JobBackupTable;
PRINT '4. Sonuc uygunsa altta ROLLBACK -> COMMIT yap, scripti tekrar calistir.';
PRINT '';

ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
