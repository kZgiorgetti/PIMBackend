#!/bin/sh
# Gera pim_completo.sql concatenando os scripts em ordem.
cd "$(dirname "$0")" && cat 01_schema.sql 02_procedures.sql 03_triggers.sql 04_seed.sql > pim_completo.sql
