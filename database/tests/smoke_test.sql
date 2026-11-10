/* Teste de fumaça do banco (PostgreSQL). Roda dentro de uma transação e desfaz tudo (ROLLBACK).
   Uso: psql -v ON_ERROR_STOP=1 -f database/tests/smoke_test.sql   (após pim_completo.sql) */
BEGIN;
DO $$
DECLARE
    v_user integer; v_colab integer; v_outro integer; v_t integer; v_n integer;
    v_antes timestamptz; v_depois timestamptz; v_sqlstate text; v_uid integer;
BEGIN
    INSERT INTO pim.usuarios (nome, email, senha_hash, perfil) VALUES ('Teste', 'smoke@pim.local', 'x', 'Gestor') RETURNING id INTO v_user;
    INSERT INTO pim.colaboradores (nome) VALUES ('Smoke A') RETURNING id INTO v_colab;
    INSERT INTO pim.colaboradores (nome) VALUES ('Smoke B') RETURNING id INTO v_outro;
    INSERT INTO pim.tarefas (titulo, colaborador_id, data_prazo) VALUES ('Smoke', v_colab, now() - interval '3 days') RETURNING id INTO v_t;

    -- 1) CHECKs
    BEGIN
        INSERT INTO pim.tarefas (titulo, status) VALUES ('x', 'Invalido');
        RAISE EXCEPTION 'CHECK de status não funcionou';
    EXCEPTION WHEN check_violation THEN NULL; END;

    -- 2) trigger de histórico + data_atualizacao (alteração via UPDATE direto, com usuário no contexto)
    SELECT data_atualizacao INTO v_antes FROM pim.tarefas WHERE id = v_t;
    PERFORM pg_sleep(0.05);
    PERFORM set_config('app.usuario_id', v_user::text, true);
    UPDATE pim.tarefas SET status = 'EmAndamento' WHERE id = v_t;
    SELECT data_atualizacao INTO v_depois FROM pim.tarefas WHERE id = v_t;
    IF NOT (v_depois > v_antes) THEN RAISE EXCEPTION 'data_atualizacao não foi atualizada'; END IF;
    SELECT count(*), max(usuario_id) INTO v_n, v_uid FROM pim.tarefa_historico WHERE tarefa_id = v_t AND status_anterior = 'Pendente' AND status_novo = 'EmAndamento';
    IF v_n <> 1 OR v_uid IS DISTINCT FROM v_user THEN RAISE EXCEPTION 'histórico incorreto (n=%, usuario=%)', v_n, v_uid; END IF;

    -- update sem mudar status não gera histórico
    UPDATE pim.tarefas SET titulo = 'Smoke 2' WHERE id = v_t;
    SELECT count(*) INTO v_n FROM pim.tarefa_historico WHERE tarefa_id = v_t;
    IF v_n <> 1 THEN RAISE EXCEPTION 'histórico gerado sem mudança de status'; END IF;

    -- 3) relatórios
    IF NOT EXISTS (SELECT 1 FROM pim.sp_tarefas_atrasadas() WHERE id = v_t AND dias_atraso >= 2) THEN RAISE EXCEPTION 'sp_tarefas_atrasadas não retornou a tarefa'; END IF;
    IF (SELECT em_andamento FROM pim.sp_resumo_tarefas_por_colaborador() WHERE colaborador_id = v_colab) <> 1 THEN RAISE EXCEPTION 'resumo incorreto'; END IF;

    -- 4) sp_concluir_tarefa (e erros)
    CALL pim.sp_concluir_tarefa(v_t, v_user);
    IF (SELECT status FROM pim.tarefas WHERE id = v_t) <> 'Concluida' THEN RAISE EXCEPTION 'não concluiu'; END IF;
    IF NOT EXISTS (SELECT 1 FROM pim.tarefa_historico WHERE tarefa_id = v_t AND status_novo = 'Concluida' AND usuario_id = v_user) THEN RAISE EXCEPTION 'histórico da conclusão ausente'; END IF;
    BEGIN CALL pim.sp_concluir_tarefa(v_t, v_user); RAISE EXCEPTION 'deveria falhar (já concluída)';
    EXCEPTION WHEN SQLSTATE 'PM003' THEN NULL; END;
    BEGIN CALL pim.sp_concluir_tarefa(-1, v_user); RAISE EXCEPTION 'deveria falhar (inexistente)';
    EXCEPTION WHEN SQLSTATE 'PM001' THEN NULL; END;
    IF NOT EXISTS (SELECT 1 FROM pim.sp_resumo_tarefas_por_colaborador() WHERE colaborador_id = v_colab AND concluidas = 1) THEN RAISE EXCEPTION 'resumo pós-conclusão incorreto'; END IF;

    -- 5) sp_reatribuir_tarefa (e erros)
    INSERT INTO pim.tarefas (titulo, colaborador_id) VALUES ('Reatribuir', v_colab) RETURNING id INTO v_t;
    CALL pim.sp_reatribuir_tarefa(v_t, v_outro, v_user);
    IF (SELECT colaborador_id FROM pim.tarefas WHERE id = v_t) <> v_outro THEN RAISE EXCEPTION 'não reatribuiu'; END IF;
    BEGIN CALL pim.sp_reatribuir_tarefa(v_t, -5, v_user); RAISE EXCEPTION 'deveria falhar (colaborador inválido)';
    EXCEPTION WHEN SQLSTATE 'PM004' THEN NULL; END;

    -- 6) exclusão de colaborador com tarefa aberta => apenas inativa
    DELETE FROM pim.colaboradores WHERE id = v_outro;
    IF NOT EXISTS (SELECT 1 FROM pim.colaboradores WHERE id = v_outro AND NOT ativo) THEN RAISE EXCEPTION 'colaborador com tarefa aberta deveria ser inativado'; END IF;

    -- 7) exclusão sem tarefas abertas => exclui; vínculos limpos; tarefas preservadas
    INSERT INTO pim.equipes (nome) VALUES ('Smoke Equipe');
    INSERT INTO pim.equipe_colaboradores (equipe_id, colaborador_id) SELECT id, v_colab FROM pim.equipes WHERE nome = 'Smoke Equipe';
    UPDATE pim.tarefas SET status = 'Concluida', colaborador_id = v_colab WHERE id = v_t;
    DELETE FROM pim.colaboradores WHERE id = v_colab;
    IF EXISTS (SELECT 1 FROM pim.colaboradores WHERE id = v_colab) THEN RAISE EXCEPTION 'colaborador sem tarefas abertas deveria ser excluído'; END IF;
    IF NOT EXISTS (SELECT 1 FROM pim.tarefas WHERE id = v_t AND colaborador_id IS NULL) THEN RAISE EXCEPTION 'tarefa deveria ter sido preservada sem responsável'; END IF;

    -- 8) unicidade parcial de usuario_id em colaboradores
    INSERT INTO pim.colaboradores (nome, usuario_id) VALUES ('Vinc 1', v_user);
    BEGIN INSERT INTO pim.colaboradores (nome, usuario_id) VALUES ('Vinc 2', v_user); RAISE EXCEPTION 'usuario_id deveria ser único';
    EXCEPTION WHEN unique_violation THEN NULL; END;
    INSERT INTO pim.colaboradores (nome) VALUES ('Sem vínculo 1'), ('Sem vínculo 2');   -- vários NULL são permitidos

    RAISE NOTICE 'SMOKE TEST: todas as verificações passaram.';
END
$$;
ROLLBACK;
