const { readJson, writeJson, getNextId } = require("./jsonStore");

const FILE_NAME = "tarefas.json";
const COLABORADORES_FILE_NAME = "colaboradores.json";

const adicionarNomeColaborador = (tarefa, colaboradores) => {
  const colaborador = colaboradores.find(
    (item) => item.id === tarefa.colaborador_id,
  );
  return {
    ...tarefa,
    colaborador_nome: colaborador ? colaborador.nome : "",
  };
};

const getAllTarefas = async () => {
  const tarefas = await readJson(FILE_NAME);
  const colaboradores = await readJson(COLABORADORES_FILE_NAME);
  return tarefas
    .sort((a, b) => Number(a.id) - Number(b.id))
    .map((tarefa) => adicionarNomeColaborador(tarefa, colaboradores));
};

const getTarefaById = async (id) => {
  const tarefas = await readJson(FILE_NAME);
  const colaboradores = await readJson(COLABORADORES_FILE_NAME);
  const tarefaId = Number(id);
  const tarefa = tarefas.find((item) => item.id === tarefaId);
  return tarefa ? adicionarNomeColaborador(tarefa, colaboradores) : null;
};

const createTarefa = async ({ titulo, descricao, status, colaborador_id }) => {
  const tarefas = await readJson(FILE_NAME);
  const tarefa = {
    id: getNextId(tarefas),
    titulo,
    descricao: descricao || "",
    status: status || "pendente",
    colaborador_id: colaborador_id ? Number(colaborador_id) : null,
  };

  tarefas.push(tarefa);
  await writeJson(FILE_NAME, tarefas);
  return tarefa;
};

const updateTarefa = async ({ id, titulo, descricao, status, colaborador_id }) => {
  const tarefas = await readJson(FILE_NAME);
  const tarefaId = Number(id);
  const index = tarefas.findIndex((tarefa) => tarefa.id === tarefaId);

  if (index === -1) return 0;

  tarefas[index] = {
    id: tarefaId,
    titulo,
    descricao: descricao || "",
    status: status || "pendente",
    colaborador_id: colaborador_id ? Number(colaborador_id) : null,
  };
  await writeJson(FILE_NAME, tarefas);
  return 1;
};

const deleteTarefa = async (id) => {
  const tarefas = await readJson(FILE_NAME);
  const tarefaId = Number(id);
  const filtradas = tarefas.filter((tarefa) => tarefa.id !== tarefaId);

  if (filtradas.length === tarefas.length) return 0;

  await writeJson(FILE_NAME, filtradas);
  return 1;
};

module.exports = {
  getAllTarefas,
  getTarefaById,
  createTarefa,
  updateTarefa,
  deleteTarefa,
};
