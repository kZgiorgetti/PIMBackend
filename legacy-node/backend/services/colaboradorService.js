const colaboradorModel = require("../models/colaboradorModel");
const ColaboradorDTO = require("../dtos/colaboradorDTO");

const listarColaboradores = async () => {
  try {
    const colaboradores = await colaboradorModel.getAllColaboradores();
    return ColaboradorDTO.fromModelList(colaboradores);
  } catch (error) {
    throw new Error("Erro ao listar colaboradores: " + error.message);
  }
};

const obterColaboradorPorId = async (id) => {
  try {
    const colaborador = await colaboradorModel.getColaboradorById(id);
    if (!colaborador) {
      const error = new Error("Colaborador não encontrado.");
      error.status = 404;
      throw error;
    }
    return ColaboradorDTO.fromModel(colaborador);
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao buscar colaborador: " + error.message);
  }
};

const criarColaborador = async (nome) => {
  try {
    if (!nome || !nome.trim()) {
      const error = new Error("O nome do colaborador é obrigatório.");
      error.status = 400;
      throw error;
    }
    const colaborador = await colaboradorModel.createColaborador(nome.trim());
    return ColaboradorDTO.fromModel(colaborador);
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao criar colaborador: " + error.message);
  }
};

const atualizarColaborador = async (id, nome) => {
  try {
    if (!nome || !nome.trim()) {
      const error = new Error("O nome do colaborador é obrigatório.");
      error.status = 400;
      throw error;
    }
    const changes = await colaboradorModel.updateColaborador(id, nome.trim());
    if (!changes) {
      const error = new Error("Colaborador não encontrado.");
      error.status = 404;
      throw error;
    }
    return new ColaboradorDTO({ id: Number(id), nome: nome.trim() });
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao atualizar colaborador: " + error.message);
  }
};

const deletarColaborador = async (id) => {
  try {
    const changes = await colaboradorModel.deleteColaborador(id);
    if (!changes) {
      const error = new Error("Colaborador não encontrado.");
      error.status = 404;
      throw error;
    }
    return changes;
  } catch (error) {
    if (error.status) throw error;
    if (error.message.includes("FOREIGN KEY")) {
      const fkError = new Error(
        "Não é possível excluir colaborador com tarefas vinculadas.",
      );
      fkError.status = 400;
      throw fkError;
    }
    throw new Error("Erro ao deletar colaborador: " + error.message);
  }
};

module.exports = {
  listarColaboradores,
  obterColaboradorPorId,
  criarColaborador,
  atualizarColaborador,
  deletarColaborador,
};
