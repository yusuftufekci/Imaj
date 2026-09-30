-- ============================================================================
-- diagnose_k3a_fail_single_job.sql
-- ============================================================================
-- AMAC: Tek bir kirli iş icin ham veri sek. K3a kirliliginin DOGAL
--       sebebini bulmaya yarar (kategori atamasi degisti mi? manuel
--       mudahale mi? yoksa kod path'i mi?).
--
-- KULLANIM: @ref'e bir kirli isin Reference'ini ver, calistir.
--           Onerilen test: Pattern A => 521169  (Disc=0, Gross>ProdNet)
--                          Pattern B => 521827  (Disc>0, Gross>ProdNet)
-- ============================================================================

SET NOCOUNT ON;

DECLARE @ref INT = 521169;   -- <<< DEGISTIR

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
PRINT '=== JobProdCat (snapshot - sadece kirli kategori dahil hepsi) ===';
SELECT
    jpc.Id, jpc.ProdCatID, jpc.GrossAmount, jpc.DiscPercentage, jpc.DiscAmount,
    jpc.NetAmount, jpc.Deleted, jpc.Stamp
FROM JobProdCat jpc
WHERE jpc.JobID = @jobId
ORDER BY jpc.ProdCatID, jpc.Id;

PRINT '';
PRINT '=== JobProd (her satir) — Deleted=0 ve =1 dahil ===';
SELECT
    jp.Id              AS JobProdId,
    jp.ProductID,
    p.Code             AS ProductCode,
    p.ProdCatID        AS Current_Product_ProdCatID,
    jp.Quantity, jp.Price,
    jp.GrossAmount     AS JP_Gross,
    jp.NetAmount       AS JP_Net,
    jp.Deleted, jp.Stamp,
    jp.Notes
FROM JobProd jp
LEFT JOIN Product p ON p.Id = jp.ProductID
WHERE jp.JobID = @jobId
ORDER BY jp.Deleted, jp.Id;

PRINT '';
PRINT '=== Ozet: JobProdCat vs sum(JobProd.NetAmount) (sadece Deleted=0) ===';
WITH agg AS (
    SELECT
        p.ProdCatID,
        SUM(CASE WHEN jp.Deleted = 0 THEN jp.NetAmount   ELSE 0 END) AS Active_Net_Sum,
        SUM(CASE WHEN jp.Deleted = 0 THEN jp.GrossAmount ELSE 0 END) AS Active_Gross_Sum,
        SUM(CASE WHEN jp.Deleted = 1 THEN jp.NetAmount   ELSE 0 END) AS Deleted_Net_Sum,
        SUM(jp.NetAmount)                                            AS All_Net_Sum
    FROM JobProd jp
    JOIN Product p ON p.Id = jp.ProductID
    WHERE jp.JobID = @jobId
    GROUP BY p.ProdCatID
)
SELECT
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.NetAmount   AS Snap_Net,
    a.Active_Net_Sum,
    a.Active_Gross_Sum,
    a.Deleted_Net_Sum,
    a.All_Net_Sum,
    -- Hipotez kontrolu: snapshot soft-deleted JobProd'lari da iceriyor mu?
    CASE WHEN ABS(jpc.GrossAmount - ISNULL(a.All_Net_Sum, 0)) < 0.01
         THEN 'YES_INCLUDES_DELETED' ELSE 'NO' END AS H1_GrossMatchesIfWeIncludeDeleted,
    -- Hipotez kontrolu: snapshot JobProd.GrossAmount toplamina mi esit?
    CASE WHEN ABS(jpc.GrossAmount - ISNULL(a.Active_Gross_Sum, 0)) < 0.01
         THEN 'YES_GROSS_NOT_NET' ELSE 'NO' END AS H2_GrossMatchesJobProdGross,
    -- Hipotez kontrolu: Product.ProdCatID baska bir kategoriye tasinmis mi?
    CASE WHEN EXISTS (SELECT 1 FROM JobProd jp2 JOIN Product p2 ON p2.Id = jp2.ProductID
                      WHERE jp2.JobID = @jobId AND jp2.Deleted = 0 AND p2.ProdCatID <> jpc.ProdCatID
                        AND p2.ProdCatID IN (SELECT ProdCatID FROM JobProdCat WHERE JobID = @jobId))
         THEN 'CHECK_MANUALLY' ELSE 'NO_OBVIOUS_SIGNAL' END AS H3_CategoryReassignmentSignal
FROM JobProdCat jpc
LEFT JOIN agg a ON a.ProdCatID = jpc.ProdCatID
WHERE jpc.JobID = @jobId AND jpc.Deleted = 0
ORDER BY jpc.ProdCatID;
