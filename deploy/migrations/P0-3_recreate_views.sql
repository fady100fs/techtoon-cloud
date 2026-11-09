-- P0-3: Recreate the 5 views dropped for the decimal migration (2026-11-09).
-- Definitions captured pre-migration (pg_get_viewdef) from prod.
-- v_stock_balance uses 0::numeric (was 0::double precision) so no double remains.

DROP VIEW IF EXISTS public.v_monthly_purchases;
DROP VIEW IF EXISTS public.v_monthly_sales;
DROP VIEW IF EXISTS public.v_party_ledger;
DROP VIEW IF EXISTS public.v_stock_balance;
DROP VIEW IF EXISTS public.v_top_selling_items;

CREATE VIEW public.v_monthly_purchases AS
  SELECT date_trunc('month'::text, date) AS month, count(*) AS invoice_count, sum(net_amount) AS total_net
  FROM invoices
  WHERE type::text = 'purchase'::text AND status::text <> 'cancelled'::text
  GROUP BY (date_trunc('month'::text, date)) ORDER BY (date_trunc('month'::text, date)) DESC;

CREATE VIEW public.v_monthly_sales AS
  SELECT date_trunc('month'::text, date) AS month, count(*) AS invoice_count, sum(net_amount) AS total_net, sum(discount_amount) AS total_discount, sum(tax_amount) AS total_tax
  FROM invoices
  WHERE type::text = 'sale'::text AND status::text <> 'cancelled'::text
  GROUP BY (date_trunc('month'::text, date)) ORDER BY (date_trunc('month'::text, date)) DESC;

CREATE VIEW public.v_party_ledger AS
  SELECT i.party_id, i.date, 'invoice'::text AS source, i.invoice_number, i.type AS invoice_type, i.net_amount AS amount
  FROM invoices i WHERE i.status::text <> 'cancelled'::text
  UNION ALL
  SELECT p.party_id, p.date, 'payment'::text AS source, p.reference_number AS invoice_number, p.payment_type AS invoice_type,
         CASE WHEN p.payment_type::text = 'receipt'::text THEN p.amount ELSE - p.amount END AS amount
  FROM payments p ORDER BY 2 DESC;

CREATE VIEW public.v_stock_balance AS
  SELECT item_id,
         sum(CASE WHEN type::text = 'in'::text THEN quantity ELSE - quantity END) AS balance,
         sum(CASE WHEN type::text = 'in'::text THEN quantity ELSE 0::numeric END) AS total_in,
         sum(CASE WHEN type::text = 'out'::text THEN quantity ELSE 0::numeric END) AS total_out
  FROM inventory_movements GROUP BY item_id;

CREATE VIEW public.v_top_selling_items AS
  SELECT il.item_id, i.name AS item_name, sum(il.quantity) AS total_qty, sum(il.total) AS total_revenue, count(DISTINCT il.invoice_id) AS invoice_count
  FROM invoice_lines il JOIN invoices inv ON il.invoice_id = inv.id JOIN items i ON il.item_id = i.id
  WHERE inv.type::text = 'sale'::text AND inv.status::text <> 'cancelled'::text
  GROUP BY il.item_id, i.name ORDER BY (sum(il.quantity)) DESC;
