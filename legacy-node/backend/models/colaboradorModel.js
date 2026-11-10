const { readJson, writeJson, getNextId } = require("./jsonStore");

const FILE_NAME = "colaboradores.json";
const TAREFAS_FILE_NAME = "tarefas.json";

const getAllColaboradores = async () => {
  const colaboradores = await readJson(FILE_NAME);
  return colaboradores.sort((a, b) => Number(a.id) - Number(b.id));
};

const getColaboradorById = async (id) => {
  const colaboradores = await readJson(FILE_NAME);
  const colaboradorId = Number(id);
  return colaboradores.find((colaborador) => colaborador.id === colaboradorId);
};

const createColaborador = async (nome) => {
  const colaboradores = await readJson(FILE_NAME);
  const colaborador = {
    id: getNextId(colaboradores),
    nome,
  };

  colaboradores.push(colaborador);
  await writeJson(FILE_NAME, colaboradores);
  return colaborador;
};

const updateColaborador = async (id, nome) => {
  const colaboradores = await readJson(FILE_NAME);
  const colaboradorId = Number(id);
  const index = colaboradores.findIndex(
    (colaborador) => colaborador.id === colaboradorId,
  );

  if (index === -1) return 0;

  colaboradores[index] = {
    ...colaboradores[index],
    nome,
  };
  await writeJson(FILE_NAME, colaboradores);
  return 1;
};

const deleteColaborador = async (id) => {
  const colaboradorId = Number(id);
  const tarefas = await readJson(TAREFAS_FILE_NAME);
  const possuiTarefas = tarefas.some(
    (tarefa) => tarefa.colaborador_id === colaboradorId,
  );

  if (possuiTarefas) {
    throw new Error("FOREIGN KEY constraint failed");
  }

  const colaboradores = await readJson(FILE_NAME);
  const filtrados = colaboradores.filter(
    (colaborador) => colaborador.id !== colaboradorId,
  );

  if (filtrados.length === colaboradores.length) return 0;

  await writeJson(FILE_NAME, filtrados);
  return 1;
};

module.exports = {
  getAllColaboradores,
  getColaboradorById,
  createColaborador,
  updateColaborador,
  deleteColaborador,
};
