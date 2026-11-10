const express = require("express");
const path = require("path");
const colaboradorRoutes = require("./routes/colaboradorRoutes");
const tarefaRoutes = require("./routes/tarefaRoutes");
const errorHandler = require("./middlewares/errorHandler");

const app = express();

app.use(express.json());
app.use(express.static(path.join(__dirname, "../frontend")));
app.use(express.static(path.join(__dirname, "..")));

app.use("/api/colaboradores", colaboradorRoutes);
app.use("/api/tarefas", tarefaRoutes);

app.get("/", (req, res) => {
  res.redirect("/pages/colaboradores.html");
});

app.use(errorHandler);

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`Servidor iniciado em http://localhost:${port}`);
});
