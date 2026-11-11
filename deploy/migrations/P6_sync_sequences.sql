-- =====================================================================
--  P6: صيانة المخطط — مزامنة sequences PostgreSQL
--
--  المشكلة: عند نقل البيانات من SQLite أو زراعتها بمعرّفات صريحة، تبقى
--  الـ sequences متأخرة عن بيانات الجدول. النتيجة: أي INSERT جديد يفشل بـ
--  "duplicate key value violates unique constraint" على الجدول نفسه.
--
--  الحل: ضبط كل sequence على MAX(id) لكل جدول. (غير مدمّر — setval فقط)
--  آمن للتشغيل المتكرر (idempotent).
--
--  ملاحظة: التطبيق نفسه ينفّذ هذه المزامنة تلقائيًا عند الإقلاع
--  (SchemaMaintenanceService) — هذا الملف للتشغيل اليدوي/الاستعادة.
-- =====================================================================

DO $$
DECLARE
    r        RECORD;
    v_seq    TEXT;
    v_max    BIGINT;
    v_cur    BIGINT;
    v_fixed  INTEGER := 0;
BEGIN
    FOR r IN
        SELECT c.relname AS seq_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind = 'S'
          AND n.nspname = 'public'
          AND c.relname LIKE '%_id_seq'
    LOOP
        v_seq := r.seq_name;

        -- اسم الجدول المشتق من اسم الـ sequence
        BEGIN
            EXECUTE format('SELECT COALESCE(MAX(id),0) FROM public.%I',
                           split_part(v_seq, '_id_seq', 1)) INTO v_max;
        EXCEPTION WHEN undefined_table THEN
            CONTINUE;
        END;

        EXECUTE format('SELECT last_value FROM public.%I', v_seq) INTO v_cur;

        IF v_max > v_cur THEN
            EXECUTE format('SELECT setval(%L, %s, true)', 'public.' || v_seq, v_max);
            RAISE NOTICE 'synced % : % -> %', v_seq, v_cur, v_max;
            v_fixed := v_fixed + 1;
        END IF;
    END LOOP;

    RAISE NOTICE 'Total sequences synced: %', v_fixed;
END $$;
