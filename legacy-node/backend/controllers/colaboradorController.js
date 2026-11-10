const colaboradorService = require("../services/colaboradorService");

const listar = async (req, res, next) => {
  try {
    const colaboradores = await colaboradorService.listarColaboradores();
    res.json(colaboradores);
  } catch (error) {
    next(error);
  }
};

const obter = async (req, res, next) => {
  try {
    const colaborador = await colaboradorService.obterColaboradorPorId(
      req.params.id,
    );
    res.json(colaborador);
  } catch (error) {
    next(error);
  }
};

const criar = async (req, res, next) => {
  try {
    const colaborador = await colaboradorService.criarColaborador(
      req.body.nome,
    );
    res.status(201).json(colaborador);
  } catch (error) {
    next(error);
  }
};

const atualizar = async (req, res, next) => {
  try {
    const colaborador = await colaboradorService.atualizarColaborador(
      req.params.id,
      req.body.nome,
    );
    res.json(colaborador);
  } catch (error) {
    next(error);
  }
};

const remover = async (req, res, next) => {
  try {
    await colaboradorService.deletarColaborador(req.params.id);
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
