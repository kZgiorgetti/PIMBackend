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
