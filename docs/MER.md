# MER e dicionário de dados (PostgreSQL / Supabase)

Schema `pim`; nomes em snake_case. Scripts em `database/` (fonte da verdade).

## Diagrama (modelo lógico)

```mermaid
erDiagram
    usuarios ||--o| colaboradores : "vincula (0..1)"
    usuarios ||--o{ refresh_tokens : possui
    usuarios ||--o{ tarefa_comentarios : escreve
    usuarios ||--o{ tarefa_historico : "altera (opcional)"
    colaboradores ||--o{ tarefas : "responsável (opcional)"
    equipes ||--o{ tarefas : "agrupa (opcional)"
    equipes ||--o{ equipe_colaboradores : tem
    colaboradores ||--o{ equipe_colaboradores : participa
    tarefas ||--o{ tarefa_comentarios : recebe
    tarefas ||--o{ tarefa_historico : registra

    usuarios { int id PK
      varchar email UK
      varchar senha_hash
      varchar perfil "Admin|Gestor|Colaborador"
      boolean ativo }
    colaboradores { int id PK
      varchar nome
      int usuario_id FK "nulo e único"
      boolean ativo }
    equipes { int id PK
      varchar nome UK }
    equipe_colaboradores { int equipe_id PK_FK
      int colaborador_id PK_FK }
    tarefas { int id PK
      varchar titulo
      varchar status "Pendente|EmAndamento|Concluida"
      varchar prioridade "Baixa|Media|Alta|Urgente"
      timestamptz data_prazo
      int colaborador_id FK
      int equipe_id FK
      boolean excluida }
    tarefa_comentarios { int id PK
      int tarefa_id FK
      int usuario_id FK }
    tarefa_historico { int id PK
      int tarefa_id FK
      varchar status_anterior
      varchar status_novo }
    refresh_tokens { int id PK
      varchar token_hash UK }
```

## Dicionário resumido (modelo físico: `database/01_schema.sql`)

| Tabela | Colunas principais | Restrições / índices |
|---|---|---|
| usuarios | id, nome, email, senha_hash, perfil, ativo, tentativas_falhas, bloqueado_ate, data_criacao, data_atualizacao | PK; UQ(email); CK(perfil) |
| refresh_tokens | id, usuario_id, token_hash (SHA-256), data_expiracao, data_revogacao, substituido_por_hash | PK; UQ(token_hash); FK usuarios |
| colaboradores | id, nome, email, cargo, ativo, usuario_id | PK; FK usuarios; índices únicos parciais em usuario_id e email (não nulos) |
| equipes | id, nome, descricao | PK; UQ(nome) |
| equipe_colaboradores | equipe_id, colaborador_id, data_entrada | PK composta; FKs |
| tarefas | id, titulo, descricao, status, prioridade, data_prazo, colaborador_id, equipe_id, excluida, data_criacao, data_atualizacao | PK; FKs; CK(status), CK(prioridade); IX status, colaborador_id, data_atualizacao, equipe_id |
| tarefa_comentarios | id, tarefa_id, usuario_id, texto, data_criacao | PK; FKs; CK(texto não vazio) |
| tarefa_historico | id, tarefa_id, status_anterior, status_novo, usuario_id, data_alteracao | PK; FKs; alimentada por trigger |

Objetos:
- **View:** `vw_tarefas_detalhadas`.
- **Procedures** (`CALL`): `sp_concluir_tarefa`, `sp_reatribuir_tarefa`.
- **Funções de relatório** (`SELECT * FROM`): `sp_resumo_tarefas_por_colaborador`, `sp_tarefas_atrasadas`.
- **Triggers:** `trg_tarefas_historico`, `trg_tarefas_data_atualizacao`, `trg_colaboradores_data_atualizacao`, `trg_usuarios_data_atualizacao`, `trg_colaboradores_bloqueia_exclusao_com_tarefas`.
- **Segurança:** RLS ligado em todas as tabelas (sem policies) e permissões das roles `anon`/`authenticated` revogadas (`05_seguranca_supabase.sql`).
