class ColaboradorDTO {
  constructor({ id, nome }) {
    this.id = id;
    this.nome = nome;
  }

  static fromModel(model) {
    if (!model) return null;
    return new ColaboradorDTO({
      id: model.id,
      nome: model.nome,
    });
  }

  static fromModelList(models) {
    if (!models) return [];
    return models.map((model) => ColaboradorDTO.fromModel(model));
  }

  toJSON() {
    return {
      id: this.id,
      nome: this.nome,
    };
  }
}

module.exports = ColaboradorDTO;
