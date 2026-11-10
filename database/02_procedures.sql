/* PIM IV - 02_procedures.sql | idempotente (CREATE OR ALTER)
   Erros de negócio: 50001 não encontrada | 50002 tarefa excluída | 50003 já concluída
                     50004 colaborador inválido/inativo                              */
USE PimDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ConcluirTarefa
    @TarefaId  INT,
    @UsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Status NVARCHAR(20), @Excluida BIT;
        SELECT @Status = Status, @Excluida = Excluida
          FROM dbo.Tarefas WITH (UPDLOCK, HOLDLOCK) WHERE Id = @TarefaId;

        IF @Status IS NULL      THROW 50001, N'Tarefa não encontrada.', 1;
        IF @Excluida = 1        THROW 50002, N'Não é possível concluir uma tarefa excluída.', 1;
        IF @Status = N'Concluida' THROW 50003, N'A tarefa já está concluída.', 1;

        EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = @UsuarioId;  -- lido pela trigger de histórico
        UPDATE dbo.Tarefas SET Status = N'Concluida' WHERE Id = @TarefaId;
        EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = NULL;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = NULL;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReatribuirTarefa
    @TarefaId          INT,
    @NovoColaboradorId INT = NULL,   -- NULL = deixar sem responsável
    @UsuarioId         INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Excluida BIT, @Achou INT = 0;
        SELECT @Excluida = Excluida, @Achou = 1
          FROM dbo.Tarefas WITH (UPDLOCK, HOLDLOCK) WHERE Id = @TarefaId;

        IF @Achou = 0    THROW 50001, N'Tarefa não encontrada.', 1;
        IF @Excluida = 1 THROW 50002, N'Não é possível reatribuir uma tarefa excluída.', 1;
        IF @NovoColaboradorId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.Colaboradores WHERE Id = @NovoColaboradorId AND Ativo = 1)
            THROW 50004, N'O colaborador informado não existe ou está inativo.', 1;

        UPDATE dbo.Tarefas SET ColaboradorId = @NovoColaboradorId WHERE Id = @TarefaId;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ResumoTarefasPorColaborador
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT c.Id AS ColaboradorId, c.Nome AS ColaboradorNome,
               COUNT(t.Id)                                          AS Total,
               COALESCE(SUM(CASE WHEN t.Status = N'Pendente'    THEN 1 ELSE 0 END), 0) AS Pendentes,
               COALESCE(SUM(CASE WHEN t.Status = N'EmAndamento' THEN 1 ELSE 0 END), 0) AS EmAndamento,
               COALESCE(SUM(CASE WHEN t.Status = N'Concluida'   THEN 1 ELSE 0 END), 0) AS Concluidas
          FROM dbo.Colaboradores c
          LEFT JOIN dbo.Tarefas t ON t.ColaboradorId = c.Id AND t.Excluida = 0
         WHERE c.Ativo = 1
         GROUP BY c.Id, c.Nome
         ORDER BY c.Nome;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TarefasAtrasadas
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT Id, Titulo, Status, Prioridade, DataPrazo,
               DATEDIFF(DAY, DataPrazo, SYSUTCDATETIME()) AS DiasAtraso,
               ColaboradorId, ColaboradorNome, EquipeId, EquipeNome
          FROM dbo.vw_TarefasDetalhadas
         WHERE Excluida = 0 AND Status <> N'Concluida'
           AND DataPrazo IS NOT NULL AND DataPrazo < SYSUTCDATETIME()
         ORDER BY DataPrazo;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO
