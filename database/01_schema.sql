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
