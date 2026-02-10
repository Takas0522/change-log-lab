-- Seed data for development and demo
-- Local: password123 (BCrypt cost 11).  CI replaces the placeholder below.

INSERT INTO users (id, email, password_hash, display_name) VALUES
('00000000-0000-0000-0000-000000000001', 'admin@example.com',
 '__SEED_PASSWORD_HASH__', 'Admin User'),
('00000000-0000-0000-0000-000000000002', 'demo@example.com',
 '__SEED_PASSWORD_HASH__', 'Demo User')
ON CONFLICT (email) DO UPDATE SET
  password_hash = EXCLUDED.password_hash,
  display_name = EXCLUDED.display_name;

INSERT INTO todos (id, user_id, title, body, status, due_date) VALUES
('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001',
 'サーバー監視設定', 'Prometheusの監視ルールを設定する', '未着手', NOW() + INTERVAL '7 days'),
('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001',
 'ドキュメント更新', 'READMEを最新の状態に更新する', '着手中', NOW() + INTERVAL '3 days'),
('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001',
 'CI/CDパイプライン構築', 'GitHub Actionsでデプロイパイプラインを構築', '完了', NOW() - INTERVAL '1 day'),
('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000002',
 'デモ環境準備', 'デモ用のテストデータを準備する', '未着手', NOW() + INTERVAL '5 days'),
('10000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000002',
 'プレゼン資料作成', 'デモ発表用のスライドを作成', '着手中', NOW() + INTERVAL '2 days')
ON CONFLICT (id) DO NOTHING;

UPDATE todos SET completed_at = NOW() - INTERVAL '1 day'
WHERE status = '完了' AND completed_at IS NULL;
