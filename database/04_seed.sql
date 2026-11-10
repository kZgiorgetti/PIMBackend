/* PIM IV - 04_seed.sql | idempotente
   Perfis (Admin/Gestor/Colaborador) são CHECK em usuarios.perfil, não tabela.
   O usuário Admin NÃO é criado aqui: o hash BCrypt é gerado pela API no primeiro start (ADMIN_EMAIL / ADMIN_PASSWORD).
   Dados abaixo vêm do legado (colaboradores.json / tarefas.json), com status normalizados. */

INSERT INTO pim.colaboradores (nome, cargo)
SELECT 'Kaio', 'Desenvolvedor' WHERE NOT EXISTS (SELECT 1 FROM pim.colaboradores WHERE nome = 'Kaio');
INSERT INTO pim.colaboradores (nome, cargo)
SELECT 'Rafael', 'Desenvolvedor' WHERE NOT EXISTS (SELECT 1 FROM pim.colaboradores WHERE nome = 'Rafael');

INSERT INTO pim.equipes (nome, descricao)
SELECT 'Equipe Geral', 'Equipe padrão do sistema' WHERE NOT EXISTS (SELECT 1 FROM pim.equipes WHERE nome = 'Equipe Geral');

INSERT INTO pim.equipe_colaboradores (equipe_id, colaborador_id)
SELECT e.id, c.id FROM pim.equipes e CROSS JOIN pim.colaboradores c
 WHERE e.nome = 'Equipe Geral' AND c.nome IN ('Kaio', 'Rafael')
ON CONFLICT DO NOTHING;

INSERT INTO pim.tarefas (titulo, descricao, status, colaborador_id)
SELECT 'Tarefa teste', '', 'Pendente', (SELECT id FROM pim.colaboradores WHERE nome = 'Kaio')
 WHERE NOT EXISTS (SELECT 1 FROM pim.tarefas WHERE titulo = 'Tarefa teste');
INSERT INTO pim.tarefas (titulo, descricao, status, colaborador_id)
SELECT 'Tarefa 2', 'Descrição da tarefa 2', 'Concluida', (SELECT id FROM pim.colaboradores WHERE nome = 'Kaio')
 WHERE NOT EXISTS (SELECT 1 FROM pim.tarefas WHERE titulo = 'Tarefa 2');
INSERT INTO pim.tarefas (titulo, descricao, status, colaborador_id)
SELECT 'Tarefa 3', '', 'Pendente', NULL
 WHERE NOT EXISTS (SELECT 1 FROM pim.tarefas WHERE titulo = 'Tarefa 3');
