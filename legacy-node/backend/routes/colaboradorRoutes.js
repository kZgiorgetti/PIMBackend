const express = require("express");
const router = express.Router();
const colaboradorController = require("../controllers/colaboradorController");

router.get("/", colaboradorController.listar);
router.get("/:id", colaboradorController.obter);
router.post("/", colaboradorController.criar);
router.put("/:id", colaboradorController.atualizar);
router.delete("/:id", colaboradorController.remover);

module.exports = router;
