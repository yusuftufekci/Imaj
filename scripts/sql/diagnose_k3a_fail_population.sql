-- ============================================================================
-- diagnose_k3a_fail_population.sql
-- ============================================================================
-- AMAC: Prod'da JobProdCat snapshot kirliligi (K3a fail) populasyonunu olc.
--
-- K3a kurali (legacy): JobProdCat.GrossAmount == SUM(JobProd.NetAmount per ProdCatID)
--
-- Cikti 5 bolum:
--   1) Genel sayim (kirli iş / kirli satir / toplam iş)
--   2) Reference penceresi (kirlilik hangi referans araliginda yogun?)
--   3) Reference bucket dagilimi (10K'lik kovalarda kac kirli iş)
--   4) StateID dagilimi (faturalanmis / acik / iptal vs.)
--   5) Sample 10 kirli iş ornegi (manuel inceleme icin)
-- ============================================================================

SET NOCOUNT ON;

-- Calisma seti: tum aktif JobProdCat + hesaplanmis Prod_NetSum
IF OBJECT_ID('tempdb..#k3') IS NOT NULL DROP TABLE #k3;

SELECT
    j.Id          AS JobId,
    j.Reference,
    j.StateID,
    j.ProdSum,
    jpc.Id        AS JobProdCatId,
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
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

-- Kirli satir = K3a fail
IF OBJECT_ID('tempdb..#bad') IS NOT NULL DROP TABLE #bad;
SELECT *
INTO #bad
FROM #k3
WHERE ABS(Snap_Gross - Prod_NetSum) >= 0.01;

-- ----------------------------------------------------------------------------
-- 1) Genel sayim
-- ----------------------------------------------------------------------------
PRINT '=== 1) Genel Sayim ===';
SELECT
    (SELECT COUNT(*)              FROM #k3)                          AS Total_JobProdCat_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #k3)                          AS Total_Jobs,
    (SELECT COUNT(*)              FROM #bad)                         AS K3aFail_Rows,
    (SELECT COUNT(DISTINCT JobId) FROM #bad)                         AS K3aFail_Jobs,
    CAST(
        100.0 * (SELECT COUNT(DISTINCT JobId) FROM #bad)
              / NULLIF((SELECT COUNT(DISTINCT JobId) FROM #k3), 0)
        AS DECIMAL(9,4)
    )                                                                AS K3aFail_Jobs_Pct;

-- ----------------------------------------------------------------------------
-- 2) Reference penceresi
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 2) K3a Fail Reference Penceresi ===';
SELECT
    MIN(Reference) AS Min_Bad_Ref,
    MAX(Reference) AS Max_Bad_Ref,
    (SELECT MIN(Reference) FROM Job) AS Db_Min_Ref,
    (SELECT MAX(Reference) FROM Job) AS Db_Max_Ref
FROM #bad;

-- ----------------------------------------------------------------------------
-- 3) Reference bucket dagilimi (10K araliklar)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 3) Reference Bucket Dagilimi (kirli iş sayisi) ===';
SELECT
    (Reference / 10000) * 10000 AS BucketStart,
    (Reference / 10000) * 10000 + 9999 AS BucketEnd,
    COUNT(DISTINCT JobId) AS Bad_Jobs
FROM #bad
GROUP BY (Reference / 10000)
ORDER BY BucketStart;

-- ----------------------------------------------------------------------------
-- 4) StateID dagilimi
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 4) StateID Dagilimi ===';
SELECT
    b.StateID,
    COUNT(DISTINCT b.JobId) AS Bad_Jobs
FROM #bad b
GROUP BY b.StateID
ORDER BY Bad_Jobs DESC;

-- ----------------------------------------------------------------------------
-- 5) Sample 10 ornek (en yuksek 5 ref + en dusuk 5 ref)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 5a) Sample: En Dusuk 5 Reference ===';
SELECT TOP 5
    Reference, StateID, ProdSum, ProdCatID,
    Snap_Gross, Snap_Disc, Snap_Net, Prod_NetSum,
    (Snap_Gross - Prod_NetSum) AS Diff_Gross_vs_ProdNet
FROM #bad
ORDER BY Reference ASC;

PRINT '';
PRINT '=== 5b) Sample: En Yuksek 5 Reference ===';
SELECT TOP 5
    Reference, StateID, ProdSum, ProdCatID,
    Snap_Gross, Snap_Disc, Snap_Net, Prod_NetSum,
    (Snap_Gross - Prod_NetSum) AS Diff_Gross_vs_ProdNet
FROM #bad
ORDER BY Reference DESC;

-- ----------------------------------------------------------------------------
-- 6) Anormal durum (Net > Prod_NetSum) varsa otomatik fix anormaldir
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 6) Anormal Vakalar (Snap_Net > Prod_NetSum) ===';
SELECT COUNT(*) AS Abnormal_Rows
FROM #bad
WHERE (Snap_Net - Prod_NetSum) > 0.01;

PRINT '';
PRINT 'NOT: Anormal sayisi > 0 ise, fix_job_snapshot_generic.sql o iş icin ROLLBACK';
PRINT '     verir; manuel inceleme gerekir.';
