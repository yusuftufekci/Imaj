-- ============================================================================
-- check_job_with_history.sql
-- ============================================================================
-- AMAC: Bir is icin SUR DURUM (K3 invariants) + ZAMAN CIZGISI (JobLog)
--       birlikte gosterilir. Boylece kirliligin DEPLOY SONRASI olup olmadigi
--       net anlasilir.
--
-- KULLANIM: @ref'i degistir, calistir.
--           Hem 521350 (fix sonrasi kontrol) hem 521538 (yeni kirli) icin
--           ayri ayri calistir.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @ref INT = 521538;   -- <<< DEGISTIR (521350 ve 521538 icin ayri calistir)

DECLARE @jobId DECIMAL(28,0);
SELECT @jobId = Id FROM Job WHERE Reference = @ref;

IF @jobId IS NULL
BEGIN
    PRINT 'Job bulunamadi: ' + CAST(@ref AS VARCHAR(20));
    RETURN;
END

-- ----------------------------------------------------------------------------
-- 1) Temel job
-- ----------------------------------------------------------------------------
PRINT '=== 1) Job ===';
SELECT Reference, StateID, ProdSum, WorkSum, CustomerID, FunctionID, InvoLineID, Name
FROM Job WHERE Id = @jobId;

-- ----------------------------------------------------------------------------
-- 2) JobLog (zaman cizgisi)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 2) JobLog (en eskiden yeniye) — DEPLOY 18.05.2026 ===';
SELECT
    jl.Id,
    jl.ActionDT,
    jl.LogActionID,
    xla.Name AS ActionName,
    u.Code   AS UserCode,
    u.Name   AS UserName,
    CASE
        WHEN jl.ActionDT >= '2026-05-18' THEN 'POST_DEPLOY'
        ELSE 'PRE_DEPLOY'
    END AS Era
FROM JobLog jl
LEFT JOIN [User] u ON u.Id = jl.UserID
LEFT JOIN XLogAction xla ON xla.LogActionID = jl.LogActionID AND xla.LanguageID = 1
WHERE jl.JobID = @jobId
ORDER BY jl.ActionDT, jl.Id;

-- ----------------------------------------------------------------------------
-- 3) JobProdCat (Deleted=0 ve =1 tum satirlar) — Stamp degerleri zaman ipucu
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 3) JobProdCat (tum satirlar) ===';
SELECT
    jpc.Id, jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscPercentage AS Snap_DiscPct,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    jpc.Deleted, jpc.Stamp
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId
ORDER BY jpc.Deleted, jpc.ProdCatID, jpc.Id;

-- ----------------------------------------------------------------------------
-- 4) JobProd (Deleted=0 ve =1)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 4) JobProd ===';
SELECT
    jp.Id AS JobProdId,
    jp.ProductID,
    p.Code AS ProductCode,
    p.ProdCatID AS Current_ProdCatID,
    jp.Quantity, jp.Price,
    jp.GrossAmount AS JP_Gross,
    jp.NetAmount   AS JP_Net,
    jp.Deleted, jp.Stamp
FROM JobProd jp
LEFT JOIN Product p ON p.Id = jp.ProductID
WHERE jp.JobID = @jobId
ORDER BY jp.Deleted, jp.Id;

-- ----------------------------------------------------------------------------
-- 5) K3a / K3b ozet
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 5) K3a/K3b (aktif kategoriler) ===';
WITH agg AS (
    SELECT
        p.ProdCatID,
        SUM(jp.NetAmount)   AS Prod_NetSum,
        SUM(jp.GrossAmount) AS Prod_GrossSum
    FROM JobProd jp
    JOIN Product p ON p.Id = jp.ProductID
    WHERE jp.JobID = @jobId AND jp.Deleted = 0
    GROUP BY p.ProdCatID
)
SELECT
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    ISNULL(a.Prod_NetSum,   0) AS Prod_NetSum,
    ISNULL(a.Prod_GrossSum, 0) AS Prod_GrossSum,
    CASE WHEN ABS(jpc.GrossAmount - ISNULL(a.Prod_NetSum, 0)) < 0.01
         THEN 'PASS' ELSE 'FAIL' END AS K3a,
    CASE WHEN ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) < 0.01
         THEN 'PASS' ELSE 'FAIL' END AS K3b,
    -- Snap_Gross = Prod_GrossSum mu? (Bug deseni)
    CASE WHEN ABS(jpc.GrossAmount - ISNULL(a.Prod_GrossSum, 0)) < 0.01
         THEN 'YES_MATCHES_PROD_GROSS' ELSE 'NO' END AS BugPattern
FROM JobProdCat jpc
LEFT JOIN agg a ON a.ProdCatID = jpc.ProdCatID
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
ORDER BY jpc.ProdCatID;

PRINT '';
PRINT '=== 6) K1 (Job.ProdSum == SUM(JobProdCat.NetAmount aktif)) ===';
SELECT
    (SELECT ProdSum FROM Job WHERE Id = @jobId) AS Job_ProdSum,
    (SELECT ISNULL(SUM(NetAmount), 0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0) AS JobProdCat_NetSum,
    CASE WHEN ABS(
            (SELECT ProdSum FROM Job WHERE Id = @jobId)
          - (SELECT ISNULL(SUM(NetAmount), 0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0)
        ) < 0.01 THEN 'PASS' ELSE 'FAIL' END AS K1;
