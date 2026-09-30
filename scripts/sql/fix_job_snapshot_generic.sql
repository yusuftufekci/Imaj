-- ============================================================================
-- fix_job_snapshot_generic.sql
-- ============================================================================
-- AMAC: Bir job icin JobProdCat snapshot ini legacy konvansiyona uydur.
--       Legacy kural: JobProdCat.GrossAmount == SUM(JobProd.NetAmount per category)
--                     JobProdCat.NetAmount   == GrossAmount - DiscAmount
--
-- STRATEJI: Her kategori snapshot satirinda:
--   - target_Gross = SUM(JobProd.NetAmount) for that ProdCat
--   - target_Net   = mevcut NetAmount (musteri faturasi degismez)
--   - target_Disc  = target_Gross - target_Net
--   Eger target_Disc < 0 olursa (Net > Gross), bu DURUM ANORMAL -> ROLLBACK.
--
-- KULLANIM: @ref degerini istediginiz Job.Reference ile degistir.
--           Default ROLLBACK; COMMIT icin elle aktar.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ref INT = 521350;     -- <<< BURAYI DEGISTIR

DECLARE @jobId DECIMAL(28,0);
SELECT @jobId = Id FROM Job WHERE Reference = @ref;

IF @jobId IS NULL
BEGIN
    PRINT 'Job bulunamadi. Reference: ' + CAST(@ref AS VARCHAR(20));
    RETURN;
END

PRINT '=== Job ozeti ===';
SELECT Id, Reference, StateID, ProdSum, WorkSum, Name FROM Job WHERE Id = @jobId;

-- ----------------------------------------------------------------------------
-- TESHIS: mevcut snapshot ve hedef degerler
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== [BEFORE] Mevcut JobProdCat + hedef hesap ===';
SELECT
    jpc.Id,
    jpc.ProdCatID,
    jpc.GrossAmount AS Cur_Gross,
    jpc.DiscAmount  AS Cur_Disc,
    jpc.NetAmount   AS Cur_Net,
    ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
              WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0) AS Target_Gross,
    jpc.NetAmount AS Target_Net,
    (ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
              WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0) - jpc.NetAmount) AS Target_Disc,
    CASE
        WHEN ABS(jpc.GrossAmount
               - ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
                          WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0)) >= 0.01
        THEN 'K3a_FAIL'
        WHEN ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) >= 0.01
        THEN 'K3b_FAIL'
        ELSE 'PASS'
    END AS Current_Status
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
ORDER BY jpc.ProdCatID;

-- ----------------------------------------------------------------------------
-- Negatif Disc kontrolu (Net > sum(JobProd.Net) olursa anormal)
-- ----------------------------------------------------------------------------
DECLARE @NegDiscCount INT;
SELECT @NegDiscCount = COUNT(*)
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
  AND (jpc.NetAmount -
       ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
                 WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0)) > 0.01;

IF @NegDiscCount > 0
BEGIN
    PRINT '';
    PRINT '!!! ANORMAL DURUM: ' + CAST(@NegDiscCount AS VARCHAR(10))
        + ' kategoride snapshot.Net > sum(JobProd.Net).';
    PRINT '    Bu, urunlere indirim DISI bir KATEGORI ZAMMI olmus demek olabilir';
    PRINT '    veya gercek bir veri tutarsizligi. Otomatik fix YAPILMAYACAK.';
    PRINT '    ROLLBACK ve manuel inceleme gerekiyor.';
    RETURN;
END

BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------
-- 1) Yedek tablo
-- ----------------------------------------------------------------------------
DECLARE @BackupTable SYSNAME = N'JobProdCat_Backup_Job' + CAST(@ref AS VARCHAR(20))
                              + N'_' + CONVERT(VARCHAR(8), GETDATE(), 112);
DECLARE @sql NVARCHAR(MAX) = N'
    SELECT *, GETDATE() AS BackupAt
    INTO ' + QUOTENAME(@BackupTable) + '
    FROM JobProdCat
    WHERE JobID = ' + CAST(@jobId AS NVARCHAR(20)) + ';
';
EXEC sp_executesql @sql;
PRINT 'Yedek tablo olusturuldu: ' + @BackupTable;

-- ----------------------------------------------------------------------------
-- 2) UPDATE: her satir icin Gross = sum(JobProd.Net), Disc = Gross - Net, Net sabit
-- ----------------------------------------------------------------------------
WITH targets AS (
    SELECT
        jpc.Id,
        ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
                  WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0) AS TargetGross,
        jpc.NetAmount AS TargetNet
    FROM JobProdCat jpc
    WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
)
UPDATE jpc
SET GrossAmount = t.TargetGross,
    DiscAmount  = t.TargetGross - t.TargetNet,
    Stamp       = 1
FROM JobProdCat jpc
JOIN targets t ON t.Id = jpc.Id;

DECLARE @RowsAffected INT = @@ROWCOUNT;
PRINT 'Updated rows: ' + CAST(@RowsAffected AS VARCHAR(10));

-- ----------------------------------------------------------------------------
-- 3) Validator invariant'larini son durum uzerinde dogrula
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== [AFTER] JobProdCat snapshot + invariant kontrolu ===';
SELECT
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
              WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0) AS Prod_NetSum,
    CASE
        WHEN ABS(jpc.GrossAmount
               - ISNULL((SELECT SUM(jp.NetAmount) FROM JobProd jp JOIN Product p ON p.Id=jp.ProductID
                          WHERE jp.JobID=@jobId AND jp.Deleted=0 AND p.ProdCatID=jpc.ProdCatID), 0)) >= 0.01
        THEN 'K3a_FAIL'
        WHEN ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) >= 0.01
        THEN 'K3b_FAIL'
        ELSE 'PASS'
    END AS K3_Result
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
ORDER BY jpc.ProdCatID;

PRINT '';
PRINT '=== K1 (Job.ProdSum == SUM(JobProdCat.NetAmount)) ===';
SELECT
    (SELECT ProdSum FROM Job WHERE Id = @jobId) AS Job_ProdSum,
    (SELECT ISNULL(SUM(NetAmount),0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0) AS JobProdCat_NetSum,
    CASE WHEN ABS((SELECT ProdSum FROM Job WHERE Id = @jobId)
                - (SELECT ISNULL(SUM(NetAmount),0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0)) < 0.01
         THEN 'PASS' ELSE 'FAIL' END AS K1_Result;

PRINT '';
PRINT '=== ONEMLI ===';
PRINT 'Tum satirlarda K3_Result = PASS, K1_Result = PASS olmali.';
PRINT 'Oyleyse: alttaki ROLLBACK TRANSACTION satirini COMMIT TRANSACTION yap, scripti tekrar calistir.';
PRINT 'Yedek: ' + @BackupTable;
PRINT '';

ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
