/* PIM IV - 05_seguranca_supabase.sql | idempotente
   O Supabase expõe tabelas via Data API (PostgREST) com as roles anon/authenticated.
   Esta API acessa o banco diretamente (role postgres, que ignora RLS); portanto:
   1) RLS ligado em todas as tabelas, sem nenhuma policy => anon/authenticated não leem nem escrevem nada;
   2) permissões revogadas dessas roles (quando existirem; em um PostgreSQL comum são ignoradas). */

ALTER TABLE pim.usuarios             ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.refresh_tokens       ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.colaboradores        ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.equipes              ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.equipe_colaboradores ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.tarefas              ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.tarefa_comentarios   ENABLE ROW LEVEL SECURITY;
ALTER TABLE pim.tarefa_historico     ENABLE ROW LEVEL SECURITY;

DO $$
DECLARE r text;
BEGIN
    FOREACH r IN ARRAY ARRAY['anon', 'authenticated'] LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('REVOKE ALL ON SCHEMA pim FROM %I', r);
            EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA pim FROM %I', r);
            EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA pim FROM %I', r);
            EXECUTE format('REVOKE ALL ON ALL ROUTINES IN SCHEMA pim FROM %I', r);
        END IF;
    END LOOP;
END
$$;
