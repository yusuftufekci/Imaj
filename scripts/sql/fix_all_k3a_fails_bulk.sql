-- ============================================================================
-- fix_all_k3a_fails_bulk.sql
-- ============================================================================
-- AMAC: Prod'daki tum K3a_FAIL aktif JobProdCat satirlarini tek seferde
--       legacy invariant'a uygun hale getirir.
--
-- KURAL: Her kirli JobProdCat satiri icin:
--   target_Gross = SUM(JobProd.NetAmount per ProdCatID)   (Deleted=0)
--   target_Net   = mevcut Net  (musteri faturasi DEGISMEZ)
--   target_Disc  = target_Gross - target_Net
--   target_DiscPct = (target_Gross > 0)
--                  ? CAST(target_Disc/target_Gross*100 AS TINYINT)
--                  : 0
--
-- ANORMAL ko sul:  target_Disc < 0  yani  target_Net > target_Gross
--                 (snapshot Net'i, JobProd Net toplam'indan buyuk)
--                 -> O satiri DEGISTIRME, anormal listeye al.
--
-- DEFAULT: ROLLBACK. Her sey beklenenle uyumluysa, dosyanin altindaki
--          ROLLBACK TRANSACTION satirini COMMIT TRANSACTION yap, scripti
--          tekrar calistir.
--
-- KULLANIM: ImajProdData uzerinde calistir.
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ----------------------------------------------------------------------------
-- 0) Kirli set'i (calismaya esas) #bad temp tablosuna al
-- ----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#k3') IS NOT NULL DROP TABLE #k3;
IF OBJECT_ID('tempdb..#bad') IS NOT NULL DROP TABLE #bad;
IF OBJECT_ID('tempdb..#abnormal') IS NOT NULL DROP TABLE #abnormal;
IF OBJECT_ID('tempdb..#fixable') IS NOT NULL DROP TABLE #fixable;

SELECT
    j.Id            AS JobId,
    j.Reference,
    j.StateID,
    jpc.Id          AS JobProdCatId,
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscPercentage AS Snap_DiscPct,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    ISNULL((SELECT SUM(jp.NetAmount)
            FROM JobProd jp
            JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id
              AND jp.Deleted = 0
              AND p.ProdCatID = jpc.ProdCatID), 0) AS Prod_NetSum
INTO #k3
FROM Job j
JOIN JobProdCat jpc ON jpc.JobID = j.Id AND jpc.Deleted = 0;

SELECT *
INTO #bad
FROM #k3
WHERE ABS(Snap_Gross - Prod_NetSum) >= 0.01;

-- Anormal: target_Disc = Prod_NetSum - Snap_Net < 0  ==>  Snap_Net > Prod_NetSum
SELECT *
INTO #abnormal
FROM #bad
WHERE (Snap_Net - Prod_NetSum) > 0.01;

-- Fixable: anormal olmayan + degerler arasinda gercek bir fark var
SELECT
    *,
    Prod_NetSum AS Target_Gross,
    Snap_Net    AS Target_Net,
    (Prod_NetSum - Snap_Net) AS Target_Disc,
    CASE WHEN Prod_NetSum > 0.0001
         THEN CAST(ROUND((Prod_NetSum - Snap_Net) * 100.0 / Prod_NetSum, 0) AS TINYINT)
         ELSE 0
    END AS Target_DiscPct
INTO #fixable
FROM #bad
WHERE NOT EXISTS (SELECT 1 FROM #abnormal a WHERE a.JobProdCatId = #bad.JobProdCatId);

-- ----------------------------------------------------------------------------
-- 1) BEFORE ozet
-- ----------------------------------------------------------------------------
PRINT '=== [BEFORE] Ozet ===';
SELECT
    (SELECT COUNT(*) FROM #k3)                                            AS Total_Active_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #k3)                              AS Total_Jobs,
    (SELECT COUNT(*) FROM #bad)                                           AS K3aFail_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #bad)                             AS K3aFail_Jobs,
    (SELECT COUNT(*) FROM #abnormal)                                      AS Abnormal_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #abnormal)                        AS Abnormal_Jobs,
    (SELECT COUNT(*) FROM #fixable)                                       AS Fixable_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #fixable)                         AS Fixable_Jobs;

PRINT '';
PRINT '=== Anormal satirlar (degistirilmeyecek, manuel inceleme) ===';
SELECT TOP 20 Reference, StateID, ProdCatID,
       Snap_Gross, Snap_Disc, Snap_Net, Prod_NetSum,
       (Snap_Net - Prod_NetSum) AS Net_OverProdNet
FROM #abnormal
ORDER BY (Snap_Net - Prod_NetSum) DESC;

PRINT '';
PRINT '=== Fixable ornek (en yuksek 10 fark) ===';
SELECT TOP 10 Reference, StateID, ProdCatID,
       Snap_Gross AS Old_Gross, Snap_Disc AS Old_Disc, Snap_Net AS Old_Net,
       Target_Gross, Target_Disc, Target_Net, Target_DiscPct,
       (Snap_Gross - Prod_NetSum) AS Diff
FROM #fixable
ORDER BY (Snap_Gross - Prod_NetSum) DESC;

-- ----------------------------------------------------------------------------
-- 2) YEDEK + UPDATE
-- ----------------------------------------------------------------------------
BEGIN TRANSACTION;

DECLARE @BackupTable SYSNAME = N'JobProdCat_Backup_Bulk_'
                              + CONVERT(VARCHAR(8),  GETDATE(), 112)
                              + N'_'
                              + REPLACE(CONVERT(VARCHAR(8), GETDATE(), 108), ':', '');
DECLARE @sql NVARCHAR(MAX) = N'
    SELECT jpc.*, GETDATE() AS BackupAt
    INTO ' + QUOTENAME(@BackupTable) + N'
    FROM JobProdCat jpc
    WHERE jpc.Id IN (SELECT JobProdCatId FROM #fixable);
';
EXEC sp_executesql @sql;
PRINT 'Yedek tablo: ' + @BackupTable;

UPDATE jpc
SET GrossAmount    = f.Target_Gross,
    DiscAmount     = f.Target_Disc,
    NetAmount      = f.Target_Net,
    DiscPercentage = f.Target_DiscPct,
    Stamp          = 1
FROM JobProdCat jpc
JOIN #fixable f ON f.JobProdCatId = jpc.Id;

DECLARE @rows INT = @@ROWCOUNT;
PRINT 'Updated JobProdCat rows: ' + CAST(@rows AS VARCHAR(10));

-- ----------------------------------------------------------------------------
-- 3) AFTER dogrulamasi (#fixable kapsamindaki kayitlar K3a/K3b PASS olmali)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== [AFTER] Hala K3a_FAIL kalan satir sayisi (fixable kapsaminda) ===';
SELECT COUNT(*) AS Still_K3a_Fail_In_Fixable_Set
FROM JobProdCat jpc
JOIN #fixable f ON f.JobProdCatId = jpc.Id
WHERE ABS(jpc.GrossAmount
        - ISNULL((SELECT SUM(jp.NetAmount)
                  FROM JobProd jp
                  JOIN Product p ON p.Id = jp.ProductID
                  WHERE jp.JobID = jpc.JobID
                    AND jp.Deleted = 0
                    AND p.ProdCatID = jpc.ProdCatID), 0)) >= 0.01;

PRINT '';
PRINT '=== [AFTER] K3b kontrolu (fixable kapsaminda) ===';
SELECT COUNT(*) AS K3b_Fail_In_Fixable_Set
FROM JobProdCat jpc
JOIN #fixable f ON f.JobProdCatId = jpc.Id
WHERE ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) >= 0.01;

PRINT '';
PRINT '=== [AFTER] K1 kontrolu (etkilenen iş ler — Job.ProdSum vs Active JobProdCat.Net toplami) ===';
SELECT COUNT(*) AS K1_Fail_Jobs
FROM (
    SELECT DISTINCT JobId FROM #fixable
) f
JOIN Job j ON j.Id = f.JobId
CROSS APPLY (
    SELECT ISNULL(SUM(NetAmount), 0) AS NetSum
    FROM JobProdCat
    WHERE JobID = j.Id AND Deleted = 0
) s
WHERE ABS(j.ProdSum - s.NetSum) >= 0.01;

PRINT '';
PRINT '=== ONEMLI ===';
PRINT '1. Yukarida [AFTER] sayilari hepsi 0 olmali (anormal hariç).';
PRINT '2. Anormal satirlar bu script tarafindan DEGISTIRILMEDI.';
PRINT '3. Yedek tablo: ' + @BackupTable;
PRINT '4. Eger sayilar uygunsa, ROLLBACK TRANSACTION satirini COMMIT yap,';
PRINT '   scripti tekrar calistir.';
PRINT '';

ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
