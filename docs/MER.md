# MER e dicionário de dados

## Diagrama (modelo lógico)

```mermaid
erDiagram
    Usuarios ||--o| Colaboradores : "vincula (0..1)"
    Usuarios ||--o{ RefreshTokens : possui
    Usuarios ||--o{ TarefaComentarios : escreve
    Usuarios ||--o{ TarefaHistorico : "altera (opcional)"
    Colaboradores ||--o{ Tarefas : "responsável (opcional)"
    Equipes ||--o{ Tarefas : "agrupa (opcional)"
    Equipes ||--o{ EquipeColaboradores : tem
    Colaboradores ||--o{ EquipeColaboradores : participa
    Tarefas ||--o{ TarefaComentarios : recebe
    Tarefas ||--o{ TarefaHistorico : registra

    Usuarios { int Id PK
      nvarchar Email UK
      nvarchar SenhaHash
      nvarchar Perfil "Admin|Gestor|Colaborador"
      bit Ativo }
    Colaboradores { int Id PK
      nvarchar Nome
      int UsuarioId FK "nulo e único"
      bit Ativo }
    Equipes { int Id PK
      nvarchar Nome UK }
    EquipeColaboradores { int EquipeId PK_FK
      int ColaboradorId PK_FK }
    Tarefas { int Id PK
      nvarchar Titulo
      nvarchar Status "Pendente|EmAndamento|Concluida"
      nvarchar Prioridade "Baixa|Media|Alta|Urgente"
      datetime2 DataPrazo
      int ColaboradorId FK
      int EquipeId FK
      bit Excluida }
    TarefaComentarios { int Id PK
      int TarefaId FK
      int UsuarioId FK }
    TarefaHistorico { int Id PK
      int TarefaId FK
      nvarchar StatusAnterior
      nvarchar StatusNovo }
    RefreshTokens { int Id PK
      char TokenHash UK }
```

## Dicionário resumido (modelo físico: ver `database/01_schema.sql`)

| Tabela | Colunas principais | Restrições / índices |
|---|---|---|
| Usuarios | Id, Nome, Email, SenhaHash, Perfil, Ativo, TentativasFalhas, BloqueadoAte, DataCriacao, DataAtualizacao | PK; UQ(Email); CK(Perfil) |
| RefreshTokens | Id, UsuarioId, TokenHash (SHA-256), DataExpiracao, DataRevogacao, SubstituidoPorHash | PK; UQ(TokenHash); FK Usuarios |
| Colaboradores | Id, Nome, Email, Cargo, Ativo, UsuarioId | PK; FK Usuarios; índice único filtrado em UsuarioId e Email (não nulos) |
| Equipes | Id, Nome, Descricao | PK; UQ(Nome) |
| EquipeColaboradores | EquipeId, ColaboradorId, DataEntrada | PK composta; FKs |
| Tarefas | Id, Titulo, Descricao, Status, Prioridade, DataPrazo, ColaboradorId, EquipeId, Excluida, DataCriacao, DataAtualizacao | PK; FKs; CK(Status), CK(Prioridade); IX Status, ColaboradorId, DataAtualizacao, EquipeId |
| TarefaComentarios | Id, TarefaId, UsuarioId, Texto, DataCriacao | PK; FKs; CK(Texto não vazio) |
| TarefaHistorico | Id, TarefaId, StatusAnterior, StatusNovo, UsuarioId, DataAlteracao | PK; FKs; alimentada por trigger |

Objetos: view `vw_TarefasDetalhadas`; procedures `sp_ConcluirTarefa`, `sp_ReatribuirTarefa`, `sp_ResumoTarefasPorColaborador`, `sp_TarefasAtrasadas`; triggers `trg_Tarefas_Historico`, `trg_Tarefas_DataAtualizacao`, `trg_Colaboradores_DataAtualizacao`, `trg_Usuarios_DataAtualizacao`, `trg_Colaboradores_BloqueiaExclusaoComTarefas`.
