-- CR260710-01 (SQL-aggregacio a Q&A-ban) teszteleshez szukseges seed adat.
-- 3x MVM villanyszamla (osszesen 52 100 HUF) + 1x Telekom elofizetes (9 990 HUF).
DO $$
DECLARE
    v_user_id uuid;
    v_doc1 uuid := gen_random_uuid();
    v_doc2 uuid := gen_random_uuid();
    v_doc3 uuid := gen_random_uuid();
    v_doc4 uuid := gen_random_uuid();
BEGIN
    SELECT id INTO v_user_id FROM app.user_account ORDER BY created_utc LIMIT 1;

    INSERT INTO app.document
        (id, title, original_file_name, mime_type, size_bytes, storage_path, sha256,
         source_type, is_private, processing_status, origin, created_by_user_account_id)
    VALUES
        (v_doc1, 'MVM villanyszámla - 4 honapja', 'cr260710-test1.pdf', 'application/pdf', 1000,
         '/tmp/cr260710-test1.pdf', md5('cr260710-1') || md5('cr260710-1b'),
         'Upload', false, 'Done', 'Manual', v_user_id),
        (v_doc2, 'MVM villanyszámla - 2 honapja', 'cr260710-test2.pdf', 'application/pdf', 1000,
         '/tmp/cr260710-test2.pdf', md5('cr260710-2') || md5('cr260710-2b'),
         'Upload', false, 'Done', 'Manual', v_user_id),
        (v_doc3, 'MVM villanyszámla - elozo honap', 'cr260710-test3.pdf', 'application/pdf', 1000,
         '/tmp/cr260710-test3.pdf', md5('cr260710-3') || md5('cr260710-3b'),
         'Upload', false, 'Done', 'Manual', v_user_id),
        (v_doc4, 'Telekom elofizetes - elozo honap', 'cr260710-test4.pdf', 'application/pdf', 1000,
         '/tmp/cr260710-test4.pdf', md5('cr260710-4') || md5('cr260710-4b'),
         'Upload', false, 'Done', 'Manual', v_user_id);

    INSERT INTO app.financial_record
        (id, document_id, record_type, vendor, amount, currency, issue_date, recurrence_period)
    VALUES
        (gen_random_uuid(), v_doc1, 'Invoice', 'MVM', 14200, 'HUF',
         (CURRENT_DATE - INTERVAL '4 months')::date, 'Monthly'),
        (gen_random_uuid(), v_doc2, 'Invoice', 'MVM', 15800, 'HUF',
         (CURRENT_DATE - INTERVAL '2 months')::date, 'Monthly'),
        (gen_random_uuid(), v_doc3, 'Invoice', 'MVM', 22100, 'HUF',
         (CURRENT_DATE - INTERVAL '1 months')::date, 'Monthly'),
        (gen_random_uuid(), v_doc4, 'Subscription', 'Telekom', 9990, 'HUF',
         (CURRENT_DATE - INTERVAL '1 months')::date, 'Monthly');
END $$;

-- Ellenorzo lekerdezes: 3 MVM sor, osszesen 52 100 HUF + 1 Telekom sor
SELECT d.title, f.vendor, f.amount, f.currency, f.issue_date
FROM app.financial_record f
JOIN app.document d ON d.id = f.document_id
WHERE f.vendor IN ('MVM', 'Telekom')
ORDER BY f.issue_date;
