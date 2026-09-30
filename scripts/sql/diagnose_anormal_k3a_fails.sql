-- ============================================================================
-- diagnose_anormal_k3a_fails.sql
-- ============================================================================
-- AMAC: 13 anormal K3a_FAIL kaydini (Snap_Net > sum(JobProd.NetAmount))
--       derin inceleyip her birinin GERCEK HIKAYESINI ortaya cikar.
--
-- Anormal sebebi: snapshot Net'i (musteri faturasi tutari), JobProd
--                 NetAmount toplam'indan buyuk. Yani:
--                 - ya JobProd sonradan kuculdu, snapshot dokunulmadi
--                 - ya da snapshot manuel/eski kodla sisirilmis
--
-- KULLANIM: Direkt calistir, ImajProdData uzerinde.
-- ============================================================================

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1) Anormal seti tespit et + ozet
-- ----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#abn') IS NOT NULL DROP TABLE #abn;

SELECT
    j.Id            AS JobId,
    j.Reference,
    j.StateID,
    j.ProdSum       AS Job_ProdSum,
    jpc.Id          AS JobProdCatId,
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    ISNULL((SELECT SUM(jp.NetAmount)
            FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0) AS Prod_NetSum,
    ISNULL((SELECT SUM(jp.GrossAmount)
            FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0) AS Prod_GrossSum,
    ISNULL((SELECT SUM(jp.NetAmount)
            FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id AND jp.Deleted = 1 AND p.ProdCatID = jpc.ProdCatID), 0) AS Deleted_Prod_NetSum,
    ISNULL((SELECT SUM(jp.NetAmount)
            FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
            WHERE jp.JobID = j.Id AND p.ProdCatID = jpc.ProdCatID), 0) AS All_Prod_NetSum
INTO #abn
FROM Job j
JOIN JobProdCat jpc ON jpc.JobID = j.Id AND jpc.Deleted = 0
WHERE (jpc.NetAmount
       - ISNULL((SELECT SUM(jp.NetAmount)
                 FROM JobProd jp JOIN Product p ON p.Id = jp.ProductID
                 WHERE jp.JobID = j.Id AND jp.Deleted = 0 AND p.ProdCatID = jpc.ProdCatID), 0)) > 0.01;

PRINT '=== 1) Anormal Ozet ===';
SELECT
    Reference, StateID, ProdCatID,
    Snap_Gross, Snap_Disc, Snap_Net,
    Prod_NetSum,
    Deleted_Prod_NetSum,
    All_Prod_NetSum,
    (Snap_Net - Prod_NetSum) AS Net_OverActiveProd,
    -- H1: Silinmis JobProd'lari da eklersek Snap_Net'i tutuyor mu?
    CASE WHEN ABS(Snap_Net - All_Prod_NetSum) < 0.01
         THEN 'YES_INCLUDES_DELETED'
         ELSE 'NO' END AS H1_SnapNet_Matches_All_JobProd,
    -- H2: Snap_Gross = sum(JobProd.GrossAmount aktif) mi?
    CASE WHEN ABS(Snap_Gross - Prod_GrossSum) < 0.01
         THEN 'YES_MATCHES_PROD_GROSS'
         ELSE 'NO' END AS H2_SnapGross_Matches_ProdGross
FROM #abn
ORDER BY (Snap_Net - Prod_NetSum) DESC;

-- ----------------------------------------------------------------------------
-- 2) Her anormal is icin JobLog zaman cizgisi
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 2) JobLog (anormal isler icin, kronolojik) — DEPLOY 18.05.2026 ===';
SELECT
    j.Reference,
    jl.ActionDT,
    xla.Name AS ActionName,
    u.Code   AS UserCode,
    CASE WHEN jl.ActionDT >= '2026-05-18' THEN 'POST_DEPLOY' ELSE 'PRE_DEPLOY' END AS Era
FROM #abn a
JOIN Job j      ON j.Id = a.JobId
JOIN JobLog jl  ON jl.JobID = j.Id
LEFT JOIN [User] u ON u.Id = jl.UserID
LEFT JOIN XLogAction xla ON xla.LogActionID = jl.LogActionID AND xla.LanguageID = 1
ORDER BY j.Reference, jl.ActionDT, jl.Id;

-- ----------------------------------------------------------------------------
-- 3) Anormal isler icin JobProdCat tum satirlari (Deleted=0 ve =1)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 3) JobProdCat tum satirlar (anormal isler) ===';
SELECT
    j.Reference,
    jpc.Id,
    jpc.ProdCatID,
    jpc.GrossAmount AS Snap_Gross,
    jpc.DiscPercentage AS Snap_DiscPct,
    jpc.DiscAmount  AS Snap_Disc,
    jpc.NetAmount   AS Snap_Net,
    jpc.Deleted,
    jpc.Stamp
FROM (SELECT DISTINCT JobId, Reference FROM #abn) a
JOIN Job j ON j.Id = a.JobId
JOIN JobProdCat jpc ON jpc.JobID = j.Id
ORDER BY j.Reference, jpc.ProdCatID, jpc.Deleted, jpc.Id;

-- ----------------------------------------------------------------------------
-- 4) Anormal isler icin JobProd tum satirlari (Deleted=0 ve =1)
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 4) JobProd tum satirlar (anormal isler) ===';
SELECT
    j.Reference,
    jp.Id AS JobProdId,
    jp.ProductID,
    p.Code AS ProductCode,
    p.ProdCatID AS Current_ProdCatID,
    jp.Quantity,
    jp.Price,
    jp.GrossAmount AS JP_Gross,
    jp.NetAmount   AS JP_Net,
    jp.Deleted,
    jp.Stamp
FROM (SELECT DISTINCT JobId, Reference FROM #abn) a
JOIN Job j ON j.Id = a.JobId
JOIN JobProd jp ON jp.JobID = j.Id
LEFT JOIN Product p ON p.Id = jp.ProductID
ORDER BY j.Reference, jp.Deleted, jp.Id;

-- ----------------------------------------------------------------------------
-- 5) Faturalandi mi? InvoLine baglantisi var mi?
-- ----------------------------------------------------------------------------
PRINT '';
PRINT '=== 5) Fatura baglantisi ===';
SELECT
    j.Reference,
    j.StateID,
    j.InvoLineID,
    il.InvoiceID,
    inv.Reference AS InvoiceRef,
    inv.StateID   AS InvoiceStateID,
    inv.GrossAmount AS Invoice_Gross,
    inv.NetAmount   AS Invoice_Net
FROM (SELECT DISTINCT JobId, Reference FROM #abn) a
JOIN Job j ON j.Id = a.JobId
LEFT JOIN InvoLine il ON il.Id = j.InvoLineID
LEFT JOIN Invoice  inv ON inv.Id = il.InvoiceID
ORDER BY j.Reference;
