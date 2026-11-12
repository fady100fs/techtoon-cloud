-- =====================================================================
--  P7: نظام الصلاحيات والأدوار
--
--  الوضع السابق:
--    * جدول roles موجود لكنه فارغ (لا توجد أدوار).
--    * لا يوجد جدول يربط الدور بالصلاحيات.
--    * user_permissions فارغ → لا أحد يملك أي صلاحية.
--    * users.role = '' → حتى المدير لا يملك صلاحيات.
--
--  ما يفعله هذا الترحيل:
--    1) ينشئ role_permissions (ارتباط الدور بالصلاحية).
--    2) يزرع الأدوار النظامية الستة.
--    3) يزرع صلاحيات كل دور.
--    4) يربط المستخدمين الحاليين بالدور المناسب.
--    5) يضمن أن كل مستخدم له role صالح.
--
--  آمن للتشغيل المتكرر (idempotent).
-- =====================================================================

BEGIN;

-- ---------- 1) جدول صلاحيات الأدوار ----------
CREATE TABLE IF NOT EXISTS role_permissions (
    id              SERIAL PRIMARY KEY,
    role_id         INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_key  VARCHAR(100) NOT NULL,
    is_granted      BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT role_permissions_unique UNIQUE (role_id, permission_key)
);

CREATE INDEX IF NOT EXISTS idx_role_permissions_role ON role_permissions(role_id);

-- ---------- 2) الأدوار النظامية ----------
-- اسم الدور فريد (يلزم لـ ON CONFLICT). الجدول كان فارغًا فلا تكرارات.
DELETE FROM roles a USING roles b WHERE a.id > b.id AND a.name = b.name;

CREATE UNIQUE INDEX IF NOT EXISTS idx_roles_name ON roles(name);

-- استثناءات المستخدم: مفتاح فريد (مستخدم + صلاحية)
DELETE FROM user_permissions a USING user_permissions b
    WHERE a.id > b.id AND a.user_id = b.user_id AND a.permission_key = b.permission_key;

CREATE UNIQUE INDEX IF NOT EXISTS idx_user_permissions_unique ON user_permissions(user_id, permission_key);

INSERT INTO roles (name, display_name, description, is_system_role, is_active, created_at)
VALUES
    ('ADMIN',       'مدير النظام',   'صلاحية كاملة على كل الشاشات والعمليات.',        TRUE, TRUE, NOW()),
    ('MANAGER',     'مدير',          'كل العمليات التشغيلية دون إدارة المستخدمين.',     TRUE, TRUE, NOW()),
    ('ACCOUNTANT',  'محاسب',         'الفواتير والمدفوعات والمالية والقيود والتقارير.',   TRUE, TRUE, NOW()),
    ('SALESPERSON', 'مندوب مبيعات',  'فواتير البيع والأطراف والعروض.',                    TRUE, TRUE, NOW()),
    ('CASHIER',     'كاشير',         'التحصيل والفواتير وعرض الخزائن.',                  TRUE, TRUE, NOW()),
    ('STOREKEEPER', 'أمين مخزن',    'الأصناف والمخازن والجرد والتسويات.',                TRUE, TRUE, NOW()),
    ('VIEWER',      'مطّلع (عرض فقط)', 'عرض كل الصفحات دون أي تعديل.',                    TRUE, TRUE, NOW())
ON CONFLICT (name) DO UPDATE
    SET display_name = EXCLUDED.display_name,
        description  = EXCLUDED.description,
        is_active    = TRUE;

-- ---------- 3) صلاحيات الأدوار ----------
-- تُزرع من داخل التطبيق (RoleCatalog.SeedRolePermissions) لأنها مرتبطة
-- بقائمة الصلاحيات الحيّة في الكود — فتبقى متزامنة تلقائيًا.
-- 'ADMIN' لا يحتاج صفوفًا: HasPermission ترجع true مباشرةً لأي صلاحية.

-- ---------- 4) ربط المستخدمين الحاليين بأدوارهم ----------
-- المشرف (admin) → ADMIN
UPDATE users SET role = 'ADMIN' WHERE username = 'admin' AND (role IS NULL OR role = '' OR role <> 'ADMIN');

-- مندوب مبيعات موجود مسبقًا → SALESPERSON
UPDATE users SET role = 'SALESPERSON'
WHERE role = 'SALESPERSON' OR (role IS NULL AND username <> 'admin');

-- أي مستخدم آخر بدون دور → VIEWER (أقل صلاحية، يُعدَّل من شاشة الصلاحيات)
UPDATE users SET role = 'VIEWER' WHERE (role IS NULL OR role = '') AND username <> 'admin';

-- ربط role_id بالدور
UPDATE users u
SET role_id = r.id
FROM roles r
WHERE u.role = r.name AND u.role_id IS DISTINCT FROM r.id;

-- ---------- 5) ضمان وجود دور صالح لكل مستخدم ----------
UPDATE users u
SET role_id = r.id
FROM roles r
WHERE r.name = 'VIEWER' AND (u.role_id IS NULL OR u.role_id = 0);

-- ---------- 6) فهرس سريع على user_permissions ----------
CREATE INDEX IF NOT EXISTS idx_user_permissions_user ON user_permissions(user_id);

COMMIT;

-- تقرير
SELECT r.name AS role, r.display_name,
       (SELECT COUNT(*) FROM role_permissions rp WHERE rp.role_id = r.id) AS perms,
       (SELECT COUNT(*) FROM users u WHERE u.role = r.name) AS users
FROM roles r ORDER BY r.id;
