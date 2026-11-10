/* PIM IV - 03_triggers.sql | idempotente (CREATE OR REPLACE + DROP TRIGGER IF EXISTS) */

-- Histórico de status. O usuário vem de current_setting('app.usuario_id'), definido pela API/procedures na transação.
CREATE OR REPLACE FUNCTION pim.fn_tarefas_historico() RETURNS trigger
LANGUAGE plpgsql SET search_path = pim, pg_temp
AS $$
BEGIN
    INSERT INTO pim.tarefa_historico (tarefa_id, status_anterior, status_novo, usuario_id)
    VALUES (NEW.id, OLD.status, NEW.status, NULLIF(current_setting('app.usuario_id', true), '')::integer);
    RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_tarefas_historico ON pim.tarefas;
CREATE TRIGGER trg_tarefas_historico AFTER UPDATE ON pim.tarefas
    FOR EACH ROW WHEN (OLD.status IS DISTINCT FROM NEW.status)
    EXECUTE FUNCTION pim.fn_tarefas_historico();

-- data_atualizacao (BEFORE: o valor novo já sai no RETURNING do EF Core)
CREATE OR REPLACE FUNCTION pim.fn_atualiza_data_atualizacao() RETURNS trigger
LANGUAGE plpgsql SET search_path = pim, pg_temp
AS $$
BEGIN
    NEW.data_atualizacao := clock_timestamp();   -- now() seria o início da transação
    RETURN NEW;
END;
$$;
DROP TRIGGER IF EXISTS trg_tarefas_data_atualizacao ON pim.tarefas;
CREATE TRIGGER trg_tarefas_data_atualizacao BEFORE UPDATE ON pim.tarefas
    FOR EACH ROW EXECUTE FUNCTION pim.fn_atualiza_data_atualizacao();
DROP TRIGGER IF EXISTS trg_colaboradores_data_atualizacao ON pim.colaboradores;
CREATE TRIGGER trg_colaboradores_data_atualizacao BEFORE UPDATE ON pim.colaboradores
    FOR EACH ROW EXECUTE FUNCTION pim.fn_atualiza_data_atualizacao();
DROP TRIGGER IF EXISTS trg_usuarios_data_atualizacao ON pim.usuarios;
CREATE TRIGGER trg_usuarios_data_atualizacao BEFORE UPDATE ON pim.usuarios
    FOR EACH ROW EXECUTE FUNCTION pim.fn_atualiza_data_atualizacao();

-- Exclusão de colaborador: com tarefas abertas => apenas inativa (cancela o DELETE); sem => exclui de fato.
-- (PostgreSQL não tem INSTEAD OF DELETE em tabelas; BEFORE DELETE retornando NULL cancela a exclusão da linha.)
CREATE OR REPLACE FUNCTION pim.fn_colaboradores_bloqueia_exclusao() RETURNS trigger
LANGUAGE plpgsql SET search_path = pim, pg_temp
AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM pim.tarefas t
                WHERE t.colaborador_id = OLD.id AND NOT t.excluida AND t.status <> 'Concluida') THEN
        UPDATE pim.colaboradores SET ativo = false WHERE id = OLD.id;
        RETURN NULL;   -- cancela o DELETE
    END IF;

    DELETE FROM pim.equipe_colaboradores WHERE colaborador_id = OLD.id;
    UPDATE pim.tarefas SET colaborador_id = NULL WHERE colaborador_id = OLD.id;   -- preserva o histórico das tarefas
    RETURN OLD;
END;
$$;
DROP TRIGGER IF EXISTS trg_colaboradores_bloqueia_exclusao_com_tarefas ON pim.colaboradores;
CREATE TRIGGER trg_colaboradores_bloqueia_exclusao_com_tarefas BEFORE DELETE ON pim.colaboradores
    FOR EACH ROW EXECUTE FUNCTION pim.fn_colaboradores_bloqueia_exclusao();
