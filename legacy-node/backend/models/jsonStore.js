const fs = require("fs/promises");
const path = require("path");

const dataDir = path.join(__dirname, "../../data");

const getFilePath = (fileName) => path.join(dataDir, fileName);

const readJson = async (fileName) => {
  const filePath = getFilePath(fileName);
  const content = await fs.readFile(filePath, "utf8");
  return JSON.parse(content);
};

const writeJson = async (fileName, data) => {
  await fs.mkdir(dataDir, { recursive: true });
  const filePath = getFilePath(fileName);
  await fs.writeFile(filePath, JSON.stringify(data, null, 2) + "\n", "utf8");
};

const getNextId = (items) => {
  if (!items.length) return 1;
  return Math.max(...items.map((item) => Number(item.id) || 0)) + 1;
};

module.exports = {
  readJson,
  writeJson,
  getNextId,
};
