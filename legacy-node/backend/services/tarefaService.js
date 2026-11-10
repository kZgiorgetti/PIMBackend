const tarefaModel = require("../models/tarefaModel");
const colaboradorModel = require("../models/colaboradorModel");
const TarefaDTO = require("../dtos/tarefaDTO");

const validarColaborador = async (colaborador_id) => {
  try {
    if (colaborador_id == null) return true;
    const colaborador =
      await colaboradorModel.getColaboradorById(colaborador_id);
    if (!colaborador) {
      const error = new Error("O colaborador informado não existe.");
      error.status = 400;
      throw error;
    }
    return true;
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao validar colaborador: " + error.message);
  }
};

const listarTarefas = async () => {
  try {
    const tarefas = await tarefaModel.getAllTarefas();
    return TarefaDTO.fromModelList(tarefas);
  } catch (error) {
    throw new Error("Erro ao listar tarefas: " + error.message);
  }
};

const obterTarefaPorId = async (id) => {
  try {
    const tarefa = await tarefaModel.getTarefaById(id);
    if (!tarefa) {
      const error = new Error("Tarefa não encontrada.");
      error.status = 404;
      throw error;
    }
    return TarefaDTO.fromModel(tarefa);
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao buscar tarefa: " + error.message);
  }
};

const criarTarefa = async ({ titulo, descricao, status, colaborador_id }) => {
  try {
    if (!titulo || !titulo.trim()) {
      const error = new Error("O título da tarefa é obrigatório.");
      error.status = 400;
      throw error;
    }
    await validarColaborador(colaborador_id);
    const tarefa = await tarefaModel.createTarefa({
      titulo: titulo.trim(),
      descricao: descricao ? descricao.trim() : "",
      status: status ? status.trim() : "pendente",
      colaborador_id: colaborador_id || null,
    });
    const tarefaCriada = await tarefaModel.getTarefaById(tarefa.id);
    return TarefaDTO.fromModel(tarefaCriada);
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao criar tarefa: " + error.message);
  }
};

const atualizarTarefa = async (
  id,
  { titulo, descricao, status, colaborador_id },
) => {
  try {
    const tarefa = await tarefaModel.getTarefaById(id);
    if (!tarefa) {
      const error = new Error("Tarefa não encontrada.");
      error.status = 404;
      throw error;
    }
    await validarColaborador(colaborador_id);

    const atual = {
      id: Number(id),
      titulo: titulo != null ? titulo.trim() : tarefa.titulo,
      descricao: descricao != null ? descricao.trim() : tarefa.descricao,
      status: status != null ? status.trim() : tarefa.status || "pendente",
      colaborador_id:
        colaborador_id !== undefined ? colaborador_id : tarefa.colaborador_id,
    };

    const changes = await tarefaModel.updateTarefa(atual);
    if (!changes) {
      const error = new Error("Nenhuma alteração realizada.");
      error.status = 400;
      throw error;
    }
    const tarefaAtualizada = await tarefaModel.getTarefaById(id);
    return TarefaDTO.fromModel(tarefaAtualizada);
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao atualizar tarefa: " + error.message);
  }
};

const deletarTarefa = async (id) => {
  try {
    const changes = await tarefaModel.deleteTarefa(id);
    if (!changes) {
      const error = new Error("Tarefa não encontrada.");
      error.status = 404;
      throw error;
    }
    return changes;
  } catch (error) {
    if (error.status) throw error;
    throw new Error("Erro ao deletar tarefa: " + error.message);
  }
};

module.exports = {
  listarTarefas,
  obterTarefaPorId,
  criarTarefa,
  atualizarTarefa,
  deletarTarefa,
};
