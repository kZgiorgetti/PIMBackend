/* PIM IV - 03_triggers.sql | idempotente (CREATE OR ALTER) */
USE PimDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

-- Histórico de mudança de status. O usuário vem do SESSION_CONTEXT('UsuarioId') (definido pela API/procedures).
CREATE OR ALTER TRIGGER dbo.trg_Tarefas_Historico ON dbo.Tarefas AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(Status) RETURN;
    DECLARE @Uid INT = TRY_CAST(SESSION_CONTEXT(N'UsuarioId') AS INT);
    INSERT INTO dbo.TarefaHistorico (TarefaId, StatusAnterior, StatusNovo, UsuarioId)
    SELECT i.Id, d.Status, i.Status, @Uid
      FROM inserted i JOIN deleted d ON d.Id = i.Id
     WHERE i.Status <> d.Status;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Tarefas_DataAtualizacao ON dbo.Tarefas AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.trg_Tarefas_DataAtualizacao')) > 1 RETURN;  -- evita recursão
    UPDATE t SET DataAtualizacao = SYSUTCDATETIME()
      FROM dbo.Tarefas t JOIN inserted i ON i.Id = t.Id;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Colaboradores_DataAtualizacao ON dbo.Colaboradores AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.trg_Colaboradores_DataAtualizacao')) > 1 RETURN;
    UPDATE c SET DataAtualizacao = SYSUTCDATETIME()
      FROM dbo.Colaboradores c JOIN inserted i ON i.Id = c.Id;
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Usuarios_DataAtualizacao ON dbo.Usuarios AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.trg_Usuarios_DataAtualizacao')) > 1 RETURN;
    UPDATE u SET DataAtualizacao = SYSUTCDATETIME()
      FROM dbo.Usuarios u JOIN inserted i ON i.Id = u.Id;
END;
GO

-- Exclusão de colaborador: com tarefas abertas => apenas inativa; sem => exclui de fato.
CREATE OR ALTER TRIGGER dbo.trg_Colaboradores_BloqueiaExclusaoComTarefas ON dbo.Colaboradores INSTEAD OF DELETE AS
BEGIN
    SET NOCOUNT ON;
    -- 1) com tarefas abertas (não concluídas e não excluídas): só inativa
    UPDATE c SET Ativo = 0
      FROM dbo.Colaboradores c JOIN deleted d ON d.Id = c.Id
     WHERE EXISTS (SELECT 1 FROM dbo.Tarefas t
                    WHERE t.ColaboradorId = c.Id AND t.Excluida = 0 AND t.Status <> N'Concluida');

    -- 2) sem tarefas abertas: remove vínculos e exclui
    DECLARE @Livres TABLE (Id INT PRIMARY KEY);
    INSERT INTO @Livres
    SELECT d.Id FROM deleted d
     WHERE NOT EXISTS (SELECT 1 FROM dbo.Tarefas t
                        WHERE t.ColaboradorId = d.Id AND t.Excluida = 0 AND t.Status <> N'Concluida');

    DELETE ec FROM dbo.EquipeColaboradores ec JOIN @Livres l ON l.Id = ec.ColaboradorId;
    UPDATE t SET ColaboradorId = NULL FROM dbo.Tarefas t JOIN @Livres l ON l.Id = t.ColaboradorId;  -- preserva histórico das tarefas
    DELETE c FROM dbo.Colaboradores c JOIN @Livres l ON l.Id = c.Id;
END;
GO
