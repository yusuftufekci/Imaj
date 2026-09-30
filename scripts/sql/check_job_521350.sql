-- ============================================================================
-- check_job_521350.sql
-- ============================================================================
-- AMAC: 521350 icin K1/K3a/K3b invariant'larini ve ham veriyi kontrol et.
--       Fix scripti calistirildi ama hala hata aliniyor -> ne tutmuyor goster.
--
-- KULLANIM: @ref'i baska bir job icin kullanabilirsin.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @ref INT = 521350;

DECLARE @jobId DECIMAL(28,0);
SELECT @jobId = Id FROM Job WHERE Reference = @ref;

IF @jobId IS NULL
BEGIN
    PRINT 'Job bulunamadi: ' + CAST(@ref AS VARCHAR(20));
    RETURN;
END

PRINT '=== Job ===';
SELECT Reference, StateID, ProdSum, WorkSum, CustomerID, FunctionID, InvoLineID, Name
FROM Job WHERE Id = @jobId;

PRINT '';
PRINT '=== JobProdCat (Deleted=0 ve =1 tum satirlar) ===';
SELECT
    jpc.Id, jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscPercentage AS Snap_DiscPct,
    jpc.DiscAmount     AS Snap_Disc,
    jpc.NetAmount      AS Snap_Net,
    jpc.Deleted, jpc.Stamp
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId
ORDER BY jpc.Deleted, jpc.ProdCatID, jpc.Id;

PRINT '';
PRINT '=== JobProd (Deleted=0 ve =1) ===';
SELECT
    jp.Id AS JobProdId, jp.ProductID,
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

PRINT '';
PRINT '=== Aktif kategori bazinda K3a / K3b kontrolu ===';
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
    -- K3a: Snap_Gross == sum(JobProd.NetAmount per cat)
    CASE WHEN ABS(jpc.GrossAmount - ISNULL(a.Prod_NetSum, 0)) < 0.01
         THEN 'PASS' ELSE 'FAIL' END AS K3a,
    -- K3b: Snap_Net == Snap_Gross - Snap_Disc
    CASE WHEN ABS(jpc.NetAmount - (jpc.GrossAmount - jpc.DiscAmount)) < 0.01
         THEN 'PASS' ELSE 'FAIL' END AS K3b
FROM JobProdCat jpc
LEFT JOIN agg a ON a.ProdCatID = jpc.ProdCatID
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
ORDER BY jpc.ProdCatID;

PRINT '';
PRINT '=== K1 (Job.ProdSum == SUM(JobProdCat.NetAmount aktif)) ===';
SELECT
    (SELECT ProdSum FROM Job WHERE Id = @jobId) AS Job_ProdSum,
    (SELECT ISNULL(SUM(NetAmount), 0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0) AS JobProdCat_NetSum,
    CASE WHEN ABS(
            (SELECT ProdSum FROM Job WHERE Id = @jobId)
          - (SELECT ISNULL(SUM(NetAmount), 0) FROM JobProdCat WHERE JobID = @jobId AND Deleted = 0)
        ) < 0.01
        THEN 'PASS' ELSE 'FAIL' END AS K1;

PRINT '';
PRINT '=== Yedek tablolari (varsa) ===';
SELECT name, create_date
FROM sys.tables
WHERE name LIKE 'JobProdCat_Backup_Job' + CAST(@ref AS VARCHAR(20)) + '%'
ORDER BY create_date DESC;
