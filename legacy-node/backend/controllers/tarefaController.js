const tarefaService = require("../services/tarefaService");

const listar = async (req, res, next) => {
  try {
    const tarefas = await tarefaService.listarTarefas();
    res.json(tarefas);
  } catch (error) {
    next(error);
  }
};

const obter = async (req, res, next) => {
  try {
    const tarefa = await tarefaService.obterTarefaPorId(req.params.id);
    res.json(tarefa);
  } catch (error) {
    next(error);
  }
};

const criar = async (req, res, next) => {
  try {
    const tarefa = await tarefaService.criarTarefa(req.body);
    res.status(201).json(tarefa);
  } catch (error) {
    next(error);
  }
};

const atualizar = async (req, res, next) => {
  try {
    const tarefa = await tarefaService.atualizarTarefa(req.params.id, req.body);
    res.json(tarefa);
  } catch (error) {
    next(error);
  }
};

const remover = async (req, res, next) => {
  try {
    await tarefaService.deletarTarefa(req.params.id);
    res.status(204).end();
  } catch (error) {
    next(error);
  }
};

module.exports = {
  listar,
  obter,
  criar,
  atualizar,
  remover,
};
