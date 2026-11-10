/* PIM IV - 02_procedures.sql | idempotente (CREATE OR REPLACE)
   Erros de negócio (SQLSTATE): PM001 não encontrada | PM002 tarefa excluída | PM003 já concluída
                                PM004 colaborador inválido/inativo
   Equivalência com o T-SQL: BEGIN..EXCEPTION = TRY/CATCH (cria subtransação), RAISE = THROW.
   Procedimentos que devolvem linhas (relatórios) são FUNCTIONs RETURNS TABLE: no PostgreSQL uma PROCEDURE não retorna result set. */

CREATE OR REPLACE PROCEDURE pim.sp_concluir_tarefa(p_tarefa_id integer, p_usuario_id integer DEFAULT NULL)
LANGUAGE plpgsql
SET search_path = pim, pg_temp
AS $$
DECLARE
    v_status   varchar(20);
    v_excluida boolean;
BEGIN
    SELECT status, excluida INTO v_status, v_excluida FROM pim.tarefas WHERE id = p_tarefa_id FOR UPDATE;

    IF NOT FOUND        THEN RAISE EXCEPTION 'Tarefa não encontrada.' USING ERRCODE = 'PM001'; END IF;
    IF v_excluida       THEN RAISE EXCEPTION 'Não é possível concluir uma tarefa excluída.' USING ERRCODE = 'PM002'; END IF;
    IF v_status = 'Concluida' THEN RAISE EXCEPTION 'A tarefa já está concluída.' USING ERRCODE = 'PM003'; END IF;

    -- usuário lido pela trigger de histórico (vale só até o fim da transação)
    PERFORM set_config('app.usuario_id', COALESCE(p_usuario_id::text, ''), true);
    UPDATE pim.tarefas SET status = 'Concluida' WHERE id = p_tarefa_id;
EXCEPTION
    WHEN OTHERS THEN
        RAISE;   -- relança o erro original (o PostgreSQL desfaz as alterações do bloco)
END;
$$;

CREATE OR REPLACE PROCEDURE pim.sp_reatribuir_tarefa(p_tarefa_id integer, p_novo_colaborador_id integer DEFAULT NULL, p_usuario_id integer DEFAULT NULL)
LANGUAGE plpgsql
SET search_path = pim, pg_temp
AS $$
DECLARE
    v_excluida boolean;
BEGIN
    SELECT excluida INTO v_excluida FROM pim.tarefas WHERE id = p_tarefa_id FOR UPDATE;

    IF NOT FOUND  THEN RAISE EXCEPTION 'Tarefa não encontrada.' USING ERRCODE = 'PM001'; END IF;
    IF v_excluida THEN RAISE EXCEPTION 'Não é possível reatribuir uma tarefa excluída.' USING ERRCODE = 'PM002'; END IF;
    IF p_novo_colaborador_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM pim.colaboradores WHERE id = p_novo_colaborador_id AND ativo)
    THEN RAISE EXCEPTION 'O colaborador informado não existe ou está inativo.' USING ERRCODE = 'PM004'; END IF;

    PERFORM set_config('app.usuario_id', COALESCE(p_usuario_id::text, ''), true);
    UPDATE pim.tarefas SET colaborador_id = p_novo_colaborador_id WHERE id = p_tarefa_id;
EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$$;

CREATE OR REPLACE FUNCTION pim.sp_resumo_tarefas_por_colaborador()
RETURNS TABLE (colaborador_id integer, colaborador_nome text, total integer, pendentes integer, em_andamento integer, concluidas integer)
LANGUAGE plpgsql STABLE
SET search_path = pim, pg_temp
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.nome::text,
           COUNT(t.id)::integer,
           COALESCE(SUM(CASE WHEN t.status = 'Pendente'    THEN 1 ELSE 0 END), 0)::integer,
           COALESCE(SUM(CASE WHEN t.status = 'EmAndamento' THEN 1 ELSE 0 END), 0)::integer,
           COALESCE(SUM(CASE WHEN t.status = 'Concluida'   THEN 1 ELSE 0 END), 0)::integer
      FROM pim.colaboradores c
      LEFT JOIN pim.tarefas t ON t.colaborador_id = c.id AND NOT t.excluida
     WHERE c.ativo
     GROUP BY c.id, c.nome
     ORDER BY c.nome;
EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$$;

CREATE OR REPLACE FUNCTION pim.sp_tarefas_atrasadas()
RETURNS TABLE (id integer, titulo text, status text, prioridade text, data_prazo timestamptz, dias_atraso integer,
               colaborador_id integer, colaborador_nome text, equipe_id integer, equipe_nome text)
LANGUAGE plpgsql STABLE
SET search_path = pim, pg_temp
AS $$
BEGIN
    RETURN QUERY
    SELECT v.id, v.titulo::text, v.status::text, v.prioridade::text, v.data_prazo,
           GREATEST(EXTRACT(DAY FROM (now() - v.data_prazo))::integer, 0),
           v.colaborador_id, v.colaborador_nome::text, v.equipe_id, v.equipe_nome::text
      FROM pim.vw_tarefas_detalhadas v
     WHERE NOT v.excluida AND v.status <> 'Concluida'
       AND v.data_prazo IS NOT NULL AND v.data_prazo < now()
     ORDER BY v.data_prazo;
EXCEPTION
    WHEN OTHERS THEN
        RAISE;
END;
$$;
