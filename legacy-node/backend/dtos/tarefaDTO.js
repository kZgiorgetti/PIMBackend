class TarefaDTO {
  constructor({
    id,
    titulo,
    descricao,
    status,
    colaborador_id,
    colaborador_nome,
  }) {
    this.id = id;
    this.titulo = titulo;
    this.descricao = descricao;
    this.status = status;
    this.colaborador_id = colaborador_id;
    this.colaborador_nome = colaborador_nome || "";
  }

  static fromModel(model) {
    if (!model) return null;
    return new TarefaDTO({
      id: model.id,
      titulo: model.titulo,
      descricao: model.descricao || "",
      status: model.status || "pendente",
      colaborador_id: model.colaborador_id || null,
      colaborador_nome: model.colaborador_nome || "",
    });
  }

  static fromModelList(models) {
    if (!models) return [];
    return models.map((model) => TarefaDTO.fromModel(model));
  }

  toJSON() {
    return {
      id: this.id,
      titulo: this.titulo,
      descricao: this.descricao,
      status: this.status,
      colaborador_id: this.colaborador_id,
      colaborador_nome: this.colaborador_nome,
    };
  }
}

module.exports = TarefaDTO;
