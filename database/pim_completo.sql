/* PIM IV - 01_schema.sql | SQL Server 2019+ | idempotente */
SET NOCOUNT ON;
IF DB_ID(N'PimDb') IS NULL CREATE DATABASE PimDb;
GO
USE PimDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

/* ---------- Usuarios ---------- */
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
CREATE TABLE dbo.Usuarios (
    Id                INT IDENTITY(1,1) NOT NULL,
    Nome              NVARCHAR(150) NOT NULL,
    Email             NVARCHAR(200) NOT NULL,
    SenhaHash         NVARCHAR(200) NOT NULL,
    Perfil            NVARCHAR(20)  NOT NULL,
    Ativo             BIT           NOT NULL CONSTRAINT DF_Usuarios_Ativo DEFAULT 1,
    TentativasFalhas  INT           NOT NULL CONSTRAINT DF_Usuarios_Tentativas DEFAULT 0,
    BloqueadoAte      DATETIME2(3)  NULL,
    DataCriacao       DATETIME2(3)  NOT NULL CONSTRAINT DF_Usuarios_DataCriacao DEFAULT SYSUTCDATETIME(),
    DataAtualizacao   DATETIME2(3)  NOT NULL CONSTRAINT DF_Usuarios_DataAtualizacao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT CK_Usuarios_Perfil CHECK (Perfil IN (N'Admin', N'Gestor', N'Colaborador'))
);
GO

/* ---------- RefreshTokens (guardados apenas como hash) ---------- */
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
CREATE TABLE dbo.RefreshTokens (
    Id               INT IDENTITY(1,1) NOT NULL,
    UsuarioId        INT           NOT NULL,
    TokenHash        CHAR(64)      NOT NULL,   -- SHA-256 hex
    DataCriacao      DATETIME2(3)  NOT NULL CONSTRAINT DF_RefreshTokens_DataCriacao DEFAULT SYSUTCDATETIME(),
    DataExpiracao    DATETIME2(3)  NOT NULL,
    DataRevogacao    DATETIME2(3)  NULL,
    SubstituidoPorHash CHAR(64)    NULL,       -- rotação: token que o substituiu
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
    CONSTRAINT FK_RefreshTokens_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
);
GO

/* ---------- Colaboradores ---------- */
IF OBJECT_ID(N'dbo.Colaboradores', N'U') IS NULL
CREATE TABLE dbo.Colaboradores (
    Id              INT IDENTITY(1,1) NOT NULL,
    Nome            NVARCHAR(150) NOT NULL,
    Email           NVARCHAR(200) NULL,
    Cargo           NVARCHAR(100) NULL,
    Ativo           BIT           NOT NULL CONSTRAINT DF_Colaboradores_Ativo DEFAULT 1,
    UsuarioId       INT           NULL,
    DataCriacao     DATETIME2(3)  NOT NULL CONSTRAINT DF_Colaboradores_DataCriacao DEFAULT SYSUTCDATETIME(),
    DataAtualizacao DATETIME2(3)  NOT NULL CONSTRAINT DF_Colaboradores_DataAtualizacao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Colaboradores PRIMARY KEY (Id),
    CONSTRAINT FK_Colaboradores_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
);
GO
-- UsuarioId nulo e único (índice filtrado, pois UNIQUE comum aceitaria só um NULL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Colaboradores_UsuarioId' AND object_id = OBJECT_ID(N'dbo.Colaboradores'))
    CREATE UNIQUE INDEX UX_Colaboradores_UsuarioId ON dbo.Colaboradores(UsuarioId) WHERE UsuarioId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Colaboradores_Email' AND object_id = OBJECT_ID(N'dbo.Colaboradores'))
    CREATE UNIQUE INDEX UX_Colaboradores_Email ON dbo.Colaboradores(Email) WHERE Email IS NOT NULL;
GO

/* ---------- Equipes e N:N ---------- */
IF OBJECT_ID(N'dbo.Equipes', N'U') IS NULL
CREATE TABLE dbo.Equipes (
    Id        INT IDENTITY(1,1) NOT NULL,
    Nome      NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(500) NULL,
    CONSTRAINT PK_Equipes PRIMARY KEY (Id),
    CONSTRAINT UQ_Equipes_Nome UNIQUE (Nome)
);
GO
IF OBJECT_ID(N'dbo.EquipeColaboradores', N'U') IS NULL
CREATE TABLE dbo.EquipeColaboradores (
    EquipeId       INT          NOT NULL,
    ColaboradorId  INT          NOT NULL,
    DataEntrada    DATETIME2(3) NOT NULL CONSTRAINT DF_EquipeColab_DataEntrada DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_EquipeColaboradores PRIMARY KEY (EquipeId, ColaboradorId),
    CONSTRAINT FK_EquipeColab_Equipes FOREIGN KEY (EquipeId) REFERENCES dbo.Equipes(Id) ON DELETE CASCADE,
    CONSTRAINT FK_EquipeColab_Colaboradores FOREIGN KEY (ColaboradorId) REFERENCES dbo.Colaboradores(Id)
    -- sem CASCADE aqui: Colaboradores tem trigger INSTEAD OF DELETE (limitação do SQL Server);
    -- a limpeza é feita dentro da própria trigger.
);
GO

/* ---------- Tarefas ---------- */
IF OBJECT_ID(N'dbo.Tarefas', N'U') IS NULL
CREATE TABLE dbo.Tarefas (
    Id              INT IDENTITY(1,1) NOT NULL,
    Titulo          NVARCHAR(200) NOT NULL,
    Descricao       NVARCHAR(2000) NOT NULL CONSTRAINT DF_Tarefas_Descricao DEFAULT N'',
    Status          NVARCHAR(20)  NOT NULL CONSTRAINT DF_Tarefas_Status DEFAULT N'Pendente',
    Prioridade      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Tarefas_Prioridade DEFAULT N'Media',
    DataPrazo       DATETIME2(3)  NULL,
    ColaboradorId   INT           NULL,
    EquipeId        INT           NULL,
    Excluida        BIT           NOT NULL CONSTRAINT DF_Tarefas_Excluida DEFAULT 0,  -- remoção lógica (sync mobile)
    DataCriacao     DATETIME2(3)  NOT NULL CONSTRAINT DF_Tarefas_DataCriacao DEFAULT SYSUTCDATETIME(),
    DataAtualizacao DATETIME2(3)  NOT NULL CONSTRAINT DF_Tarefas_DataAtualizacao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Tarefas PRIMARY KEY (Id),
    CONSTRAINT FK_Tarefas_Colaboradores FOREIGN KEY (ColaboradorId) REFERENCES dbo.Colaboradores(Id),
    CONSTRAINT FK_Tarefas_Equipes FOREIGN KEY (EquipeId) REFERENCES dbo.Equipes(Id),
    CONSTRAINT CK_Tarefas_Status CHECK (Status IN (N'Pendente', N'EmAndamento', N'Concluida')),
    CONSTRAINT CK_Tarefas_Prioridade CHECK (Prioridade IN (N'Baixa', N'Media', N'Alta', N'Urgente'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tarefas_Status' AND object_id = OBJECT_ID(N'dbo.Tarefas'))
    CREATE INDEX IX_Tarefas_Status ON dbo.Tarefas(Status) INCLUDE (Excluida);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tarefas_ColaboradorId' AND object_id = OBJECT_ID(N'dbo.Tarefas'))
    CREATE INDEX IX_Tarefas_ColaboradorId ON dbo.Tarefas(ColaboradorId) INCLUDE (Status, Excluida);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tarefas_DataAtualizacao' AND object_id = OBJECT_ID(N'dbo.Tarefas'))
    CREATE INDEX IX_Tarefas_DataAtualizacao ON dbo.Tarefas(DataAtualizacao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tarefas_EquipeId' AND object_id = OBJECT_ID(N'dbo.Tarefas'))
    CREATE INDEX IX_Tarefas_EquipeId ON dbo.Tarefas(EquipeId);
GO

/* ---------- Comentários e histórico ---------- */
IF OBJECT_ID(N'dbo.TarefaComentarios', N'U') IS NULL
CREATE TABLE dbo.TarefaComentarios (
    Id          INT IDENTITY(1,1) NOT NULL,
    TarefaId    INT           NOT NULL,
    UsuarioId   INT           NOT NULL,
    Texto       NVARCHAR(1000) NOT NULL,
    DataCriacao DATETIME2(3)  NOT NULL CONSTRAINT DF_TarefaComentarios_DataCriacao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TarefaComentarios PRIMARY KEY (Id),
    CONSTRAINT FK_TarefaComentarios_Tarefas FOREIGN KEY (TarefaId) REFERENCES dbo.Tarefas(Id),
    CONSTRAINT FK_TarefaComentarios_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT CK_TarefaComentarios_Texto CHECK (LEN(LTRIM(RTRIM(Texto))) > 0)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TarefaComentarios_TarefaId' AND object_id = OBJECT_ID(N'dbo.TarefaComentarios'))
    CREATE INDEX IX_TarefaComentarios_TarefaId ON dbo.TarefaComentarios(TarefaId, DataCriacao);
GO
IF OBJECT_ID(N'dbo.TarefaHistorico', N'U') IS NULL
CREATE TABLE dbo.TarefaHistorico (
    Id             INT IDENTITY(1,1) NOT NULL,
    TarefaId       INT          NOT NULL,
    StatusAnterior NVARCHAR(20) NOT NULL,
    StatusNovo     NVARCHAR(20) NOT NULL,
    UsuarioId      INT          NULL,
    DataAlteracao  DATETIME2(3) NOT NULL CONSTRAINT DF_TarefaHistorico_Data DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_TarefaHistorico PRIMARY KEY (Id),
    CONSTRAINT FK_TarefaHistorico_Tarefas FOREIGN KEY (TarefaId) REFERENCES dbo.Tarefas(Id),
    CONSTRAINT FK_TarefaHistorico_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT CK_TarefaHistorico_Status CHECK (
        StatusAnterior IN (N'Pendente', N'EmAndamento', N'Concluida') AND
        StatusNovo     IN (N'Pendente', N'EmAndamento', N'Concluida'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TarefaHistorico_TarefaId' AND object_id = OBJECT_ID(N'dbo.TarefaHistorico'))
    CREATE INDEX IX_TarefaHistorico_TarefaId ON dbo.TarefaHistorico(TarefaId, DataAlteracao);
GO

/* ---------- View ---------- */
CREATE OR ALTER VIEW dbo.vw_TarefasDetalhadas AS
SELECT t.Id, t.Titulo, t.Descricao, t.Status, t.Prioridade, t.DataPrazo, t.Excluida,
       t.DataCriacao, t.DataAtualizacao,
       t.ColaboradorId, c.Nome AS ColaboradorNome, c.Cargo AS ColaboradorCargo,
       t.EquipeId,      e.Nome AS EquipeNome
FROM dbo.Tarefas t
LEFT JOIN dbo.Colaboradores c ON c.Id = t.ColaboradorId
LEFT JOIN dbo.Equipes e       ON e.Id = t.EquipeId;
GO
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
/* PIM IV - 04_seed.sql | idempotente
   Perfis (Admin/Gestor/Colaborador) são restrição CHECK em Usuarios.Perfil, não tabela.
   O usuário Admin (admin@pim.local por padrão) NÃO é criado aqui: o hash BCrypt é gerado
   pela API no primeiro start a partir de ADMIN_EMAIL / ADMIN_PASSWORD.
   Dados abaixo vêm de data/colaboradores.json e data/tarefas.json (status normalizados). */
USE PimDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Colaboradores WHERE Nome = N'Kaio')
    INSERT INTO dbo.Colaboradores (Nome, Cargo) VALUES (N'Kaio', N'Desenvolvedor');
IF NOT EXISTS (SELECT 1 FROM dbo.Colaboradores WHERE Nome = N'Rafael')
    INSERT INTO dbo.Colaboradores (Nome, Cargo) VALUES (N'Rafael', N'Desenvolvedor');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Equipes WHERE Nome = N'Equipe Geral')
    INSERT INTO dbo.Equipes (Nome, Descricao) VALUES (N'Equipe Geral', N'Equipe padrão do sistema');
GO
INSERT INTO dbo.EquipeColaboradores (EquipeId, ColaboradorId)
SELECT e.Id, c.Id FROM dbo.Equipes e CROSS JOIN dbo.Colaboradores c
 WHERE e.Nome = N'Equipe Geral' AND c.Nome IN (N'Kaio', N'Rafael')
   AND NOT EXISTS (SELECT 1 FROM dbo.EquipeColaboradores x WHERE x.EquipeId = e.Id AND x.ColaboradorId = c.Id);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Tarefas WHERE Titulo = N'Tarefa teste')
    INSERT INTO dbo.Tarefas (Titulo, Descricao, Status, ColaboradorId)
    VALUES (N'Tarefa teste', N'', N'Pendente', (SELECT Id FROM dbo.Colaboradores WHERE Nome = N'Kaio'));
IF NOT EXISTS (SELECT 1 FROM dbo.Tarefas WHERE Titulo = N'Tarefa 2')
    INSERT INTO dbo.Tarefas (Titulo, Descricao, Status, ColaboradorId)
    VALUES (N'Tarefa 2', N'Descrição da tarefa 2', N'Concluida', (SELECT Id FROM dbo.Colaboradores WHERE Nome = N'Kaio'));
IF NOT EXISTS (SELECT 1 FROM dbo.Tarefas WHERE Titulo = N'Tarefa 3')
    INSERT INTO dbo.Tarefas (Titulo, Descricao, Status, ColaboradorId)
    VALUES (N'Tarefa 3', N'', N'Pendente', NULL);
GO
